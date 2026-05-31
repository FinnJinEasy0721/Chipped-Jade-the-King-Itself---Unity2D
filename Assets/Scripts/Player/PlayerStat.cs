using UnityEngine;
using System.IO;
using System;

/// <summary>
/// PlayerStat.cs
/// 玩家属性管理脚本（独立于GameData脚本）
/// 负责读取/存储玩家等级、经验、金币、祝福类型等数据（使用JSON自包含保存）
/// 计算所有属性（固定属性、等级表、祝福加成/减益、暴击、当前攻击力等）
/// 提供伤害计算公式、祝福切换、生命值管理等
/// 所有数据基于提供的“玩家属性.md”和“祝福设计.md”实现
/// 可独立运行（无需其他管理器脚本）
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(PlayerStateMachine))]
public class PlayerStat : MonoBehaviour
{
    [System.Serializable]
    public class PlayerData
    {
        public int Player_Level = 1; // 等级
        public int Player_Exp = 0; // 经验
        public int Player_Coin = 500; // 初始身上金币
        public int Merchant_Coin = 0; // 投资商人金币
        public int Wandering_Affinity = 0; // 流浪商人好看度
        public int Wandering_Exp = 0; // 流浪商人经验
        public int Merchant_Affinity = 0; // 商人亲密度
        public int Merchant_Exp = 0; // 商人经验
        public string Current_Blessing = "No_Blessing"; // 用于JSON保存
    }

    public PlayerData playerData = new PlayerData();

    // 当前祝福类型（枚举）
    public enum BlessingType
    {
        No_Blessing,
        AP_Blessing,
        LifeSteal_Blessing,
        ThreeHit_Blessing,
        LowHurt_Blessing,
        SuperHit_Blessing,
        Invincible_Blessing,
        Lightning_Blessing,
        Knockback_Blessing
    }
    public BlessingType currentBlessing = BlessingType.No_Blessing;

    // 当前生命值（变化属性）
    public int Curr_HP = 100;
    public int Curr_ExtraHealth = 0; // 额外生命（天使等触发）

    // 固定属性（可外部调整）
    public float Base_PlayerSpeed = 2f;
    public float Base_JumpForce = 2f;
    public float Base_CritRate = 0.05f; // 5%
    public float Base_CritMulti = 1.5f; // 150%
    public float HeavyAttackCD = 2f; // 重击冷却

    // 等级表（直接来自“玩家属性.md”）
    private static readonly int[] MaxHPTable = { 0, 100, 120, 150, 200, 250, 300 }; // index 0无效，1~5
    private readonly int[] BaseAP1Table = { 0, 10, 15, 18, 20, 25, 30 };
    private readonly int[] BaseAP2Table = { 0, 12, 18, 25, 30, 35, 40 };
    private readonly int[] BaseAP3Table = { 0, 14, 20, 28, 35, 45, 55 };
    private readonly int[] BaseHeavyAPTable = { 0, 0, 0, 30, 40, 50, 60 };
    private readonly bool[] CanJumpIITable = { false, false, true, true, true, true };
    private readonly bool[] CanComboTable = { false, false, true, true, true, true };
    private readonly bool[] CanHeavyTable = { false, false, false, true, true, true };
    private readonly float[] LoseCoinRateTable = { 0f, 0.9f, 0.8f, 0.7f, 0.65f, 0.6f };

    // 等级所需总经验（来自“玩家属性.md”）
    private readonly int[] LevelExpRequirements = { 0, 0, 200, 400, 600, 1000 }; // index1=0（已1级），index2=200（升2级）...

    // 运行时变量
    private float heavyAttackTimer = 0f; // 重击冷却定时器
    private string savePath;  // JSON保存路径

    private void Awake()
    {
        savePath = Application.persistentDataPath + "/player.json";
        LoadData();
        UpdateStats(); // 首次刷新所有属性
    }

    private void Update()
    {
        Debug.Log(Curr_HP);
    }

    /// <summary>
    /// 从JSON加载玩家数据（自包含，无需GameData脚本）
    /// 如果文件不存在则创建默认数据
    /// </summary>
    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
            currentBlessing = (BlessingType)System.Enum.Parse(typeof(BlessingType), playerData.Current_Blessing);
        }
        else
        {
            playerData = new PlayerData();
            SaveData();
        }
    }

    public void SaveData()
    {
        playerData.Current_Blessing = currentBlessing.ToString();
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    /// <summary>
    /// 刷新所有当前属性（等级变化、祝福加成）
    /// 严格遵循“未暴击伤害计算公式”和“暴击伤害计算公式”
    /// </summary>
    public void UpdateStats()
    {
        int level = Mathf.Clamp(playerData.Player_Level, 1, 5);
        Curr_HP = Mathf.Clamp(Curr_HP, 0, MaxHPTable[level] + Curr_ExtraHealth);
    }

    /// <summary>
    /// 切换祝福（来自“祝福设计.md”），立即刷新属性加成
    /// </summary>
    public void ChangeBlessing(BlessingType newBlessing)
    {
        currentBlessing = newBlessing;
        UpdateStats();
        Debug.Log($"【祝福切换】当前祝福变为：{newBlessing}（AP/暴击等已刷新）");
        // 实际项目中可触发事件：Player_ChangeBlessing
    }

    /// <summary>
    /// 获取当前等级基础攻击力（1=第一段，2=第二段，3=第三段，0=重击）
    /// </summary>
    public int GetBaseAttackPower(int comboStage)
    {
        int level = Mathf.Clamp(playerData.Player_Level, 1, 5);
        return comboStage switch
        {
            1 => BaseAP1Table[level],
            2 => BaseAP2Table[level],
            3 => BaseAP3Table[level],
            0 => BaseHeavyAPTable[level],
            _ => BaseAP1Table[level]
        };
    }

    /// <summary>
    /// 计算最终伤害（包含祝福加成/减益 + 暴击）
    /// 严格按照公式实现，支持所有祝福的AP/暴击修改
    /// </summary>
    public int GetAttackDamage(int comboStage, out bool isCrit)
    {
        int baseAP = GetBaseAttackPower(comboStage);
        float apMultiplier = 1f;

        // 祝福AP加成/减益（来自祝福设计.md）
        switch (currentBlessing)
        {
            case BlessingType.AP_Blessing:
            case BlessingType.ThreeHit_Blessing:
            case BlessingType.LowHurt_Blessing:
            case BlessingType.Invincible_Blessing:
                apMultiplier += 0.1f;
                break;
            case BlessingType.LifeSteal_Blessing:
                apMultiplier -= 0.1f;
                break;
            case BlessingType.SuperHit_Blessing:
                apMultiplier += 2f; // +200%
                break;
            case BlessingType.Lightning_Blessing:
                apMultiplier += 0.4f;
                break;
            case BlessingType.Knockback_Blessing:
                apMultiplier -= 0.1f;
                break;
        }

        int currAP = Mathf.RoundToInt(baseAP * apMultiplier);

        // 暴击概率与效果
        float critRate = Base_CritRate;
        float critMulti = Base_CritMulti;

        if (currentBlessing == BlessingType.SuperHit_Blessing)
        {
            critRate += 0.5f;
            critMulti += 1f; // +100%
        }
        else if (currentBlessing == BlessingType.Lightning_Blessing)
        {
            critRate = 0f; // 无法暴击
        }

        isCrit = UnityEngine.Random.value < critRate;
        int finalDamage = isCrit ? Mathf.RoundToInt(currAP * critMulti) : currAP;

        // 生命偷取祝福（伤害敌人时恢复当前生命值10%）
        if (currentBlessing == BlessingType.LifeSteal_Blessing && finalDamage > 0)
        {
            int heal = Mathf.RoundToInt(Curr_HP * 0.1f);
            Curr_HP = Mathf.Min(Curr_HP + heal, MaxHPTable[playerData.Player_Level] + Curr_ExtraHealth);
        }

        return finalDamage;
    }

    public bool CanCombo() => CanComboTable[Mathf.Clamp(playerData.Player_Level, 1, 5)];
    public bool CanHeavy() => CanHeavyTable[Mathf.Clamp(playerData.Player_Level, 1, 5)];
    public bool CanJumpII() => CanJumpIITable[Mathf.Clamp(playerData.Player_Level, 1, 5)];
    public float GetLoseCoinRate() => LoseCoinRateTable[Mathf.Clamp(playerData.Player_Level, 1, 5)];

    /// <summary>
    /// 重击冷却计时（每帧调用）
    /// </summary>
    public void UpdateTimer()
    {
        if (heavyAttackTimer > 0f) heavyAttackTimer -= Time.deltaTime;
    }

    public bool CanHeavyAttack() => heavyAttackTimer <= 0f && CanHeavy();

    public void StartHeavyCD() => heavyAttackTimer = HeavyAttackCD;

    /// <summary>
    /// 主动使用祝福技能（L键触发后调用）
    /// 仅播放动画，技能效果为占位（实际项目可扩展敌人交互）
    /// 技能效果描述来自“祝福设计.md”，实际项目中可触发事件：Player_UseBlessingSkill，敌人可监听并响应不同祝福的效果
    /// 用事件驱动解耦
    /// </summary>
    public void UseBlessingSkill()
    {
        switch (currentBlessing)
        {
            case BlessingType.ThreeHit_Blessing:
                Debug.Log("【三袭三生】技能触发：第三次连击后每秒造成敌人最大生命值5%伤害（持续3秒，冷却60秒）");
                break;
            case BlessingType.LowHurt_Blessing:
                Debug.Log("【禾萎卸攻】技能触发：减少敌人当前攻击力20%（持续5秒，冷却60秒）");
                break;
            case BlessingType.SuperHit_Blessing:
                Debug.Log("【碎玉焚心】技能开启：暴击率+50%、暴击效果+100%（持续30秒，可长按K 2秒关闭）");
                break;
            case BlessingType.Invincible_Blessing:
                Debug.Log("【父佑青御】技能触发：自身无敌1秒（下次第三连击刷新，冷却60秒）");
                break;
            case BlessingType.Lightning_Blessing:
                Debug.Log("【惊世先生】技能触发：对最近三名敌人造成300点电击并眩晕3秒（冷却20秒）");
                break;
            case BlessingType.Knockback_Blessing:
                Debug.Log("【却敌安邦】技能触发：击退范围内所有敌人并眩晕2秒（冷却20秒）");
                break;
            default:
                Debug.Log("当前祝福无主动技能（或为被动祝福）");
                break;
        }
    }

    /// <summary>
    /// 公开获取最大生命值
    /// </summary>
    public int GetMaxHP()
    {
        int level = Mathf.Clamp(playerData.Player_Level, 1, 5);
        return MaxHPTable[level] + Curr_ExtraHealth;
    }

    /// <summary>
    /// 玩家受到伤害（外部敌人调用）
    /// 纯粹的数据层，只关心数值变化，不处理状态转换（由PlayerStateMachine负责）
    /// </summary>
    public void TakeDamage(int damage)
    {
        // 优先扣除额外生命
        if (Curr_ExtraHealth > 0 && damage > 0)
        {
            if (Curr_ExtraHealth >= damage)
            {
                Curr_ExtraHealth -= damage;
                damage = 0;
            }
            else
            {
                damage -= Curr_ExtraHealth;
                Curr_ExtraHealth = 0;
            }
        }
        // 再扣除当前生命
        if (damage > 0)
        {
            Curr_HP = Mathf.Max(0, Curr_HP - damage);
        }
        //Debug.Log($"玩家受到伤害，当前HP：{Curr_HP}，额外生命：{Curr_ExtraHealth}");
    }

    /// <summary>
    /// 增加经验并检查升级
    /// </summary>
    public void AddExp(int exp)
    {
        playerData.Player_Exp += exp;
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        bool leveledUp = false;
        while (playerData.Player_Level < 5 && playerData.Player_Exp >= LevelExpRequirements[playerData.Player_Level + 1])
        {
            playerData.Player_Level++;
            leveledUp = true;
        }
        if (leveledUp)
        {
            UpdateStats();
            Curr_HP = MaxHPTable[playerData.Player_Level]; // 升级回满血
            Debug.Log($"【玩家升级】当前等级：{playerData.Player_Level}，最大生命值：{MaxHPTable[playerData.Player_Level]}");
            // 实际项目中可触发事件：Player_LevelUp
        }
    }

    /// <summary>
    /// 死亡时掉落金币比例（根据等级）
    /// </summary>
    public int CalculateDroppedCoin()
    {
        int drop = Mathf.RoundToInt(playerData.Player_Coin * GetLoseCoinRate());
        playerData.Player_Coin -= drop;
        return drop;
    }
}