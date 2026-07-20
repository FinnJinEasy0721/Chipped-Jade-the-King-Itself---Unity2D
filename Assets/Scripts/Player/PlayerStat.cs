using UnityEngine;
using System.IO;
using System;

/// <summary>
/// PlayerStat.cs
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(PlayerStateMachine))]
public class PlayerStat : MonoBehaviour
{
    [System.Serializable] // 用于JSON保存
    public class PlayerData
    {
        public int Player_Level = 1; // 等级
        public int Player_Exp = 0; // 经验
        public int Player_Coin = 500; // 初始身上金币
        public int Merchant_Coin = 0; // 投资商人金币（银行）
        public int Wandering_Affinity = 0; // 流浪商人好看度
        public int Wandering_Exp = 0; // 流浪商人经验
        public int Merchant_Affinity = 0; // 商人亲密度
        public int Merchant_Exp = 0; // 商人经验
        public string Current_Blessing = "No_Blessing"; // 用于JSON保存
    }

    public PlayerData playerData = new PlayerData();

    // 无敌标记（由祝福技能设置）
    private bool _isInvincible = false;

    // 当前生命值（变化属性）
    public int Curr_HP = 100;
    public int Curr_ExtraHealth = 0; // 额外生命（天使等触发）

    // 固定属性（可外部调整）
    public float Base_PlayerSpeed = 2f;
    public float Base_JumpForce = 2f;
    public float Base_CritRate = 0.05f; // 5%
    public float Base_CritMulti = 1.5f; // 150%
    public float HeavyAttackCD = 2f; // 重击冷却

    // 等级表
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
    /// 从JSON加载玩家数据
    /// 如果文件不存在则创建默认数据
    /// </summary>
    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
            // 祝福ID由BlessingManager读取，此处仅加载原始字符串
        }
        else
        {
            playerData = new PlayerData();
            SaveData();
        }
    }

    public void SaveData()
    {
        // Current_Blessing由BlessingManager写入playerData，此处统一保存JSON
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    /// <summary>
    /// 刷新所有当前属性
    /// </summary>
    public void UpdateStats()
    {
        int level = Mathf.Clamp(playerData.Player_Level, 1, 5);
        Curr_HP = Mathf.Clamp(Curr_HP, 0, MaxHPTable[level] + Curr_ExtraHealth);
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
    /// 计算最终伤害（通过BlessingManager的修饰器系统获取加成 + 暴击）
    /// </summary>
    public int GetAttackDamage(int comboStage, out bool isCrit)
    {
        int baseAP = GetBaseAttackPower(comboStage);

        // 从BlessingManager的修饰器系统获取属性加成
        var bm = GetComponent<BlessingManager>();
        float apMultiplier = bm?.Modifiers.GetFinalValue(StatType.AttackPower, 1f) ?? 1f;
        float critRate = bm?.Modifiers.GetFinalValue(StatType.CritRate, Base_CritRate) ?? Base_CritRate;
        float critMulti = bm?.Modifiers.GetFinalValue(StatType.CritMultiplier, Base_CritMulti) ?? Base_CritMulti;

        int currAP = Mathf.RoundToInt(baseAP * apMultiplier);

        isCrit = UnityEngine.Random.value < critRate;
        int finalDamage = isCrit ? Mathf.RoundToInt(currAP * critMulti) : currAP;

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

    /// <summary>重击冷却剩余时间（秒）</summary>
    public float GetHeavyAttackCooldownRemaining() => heavyAttackTimer;

    /// <summary>升至下一级所需总经验（满级返回 -1）</summary>
    public int GetNextLevelExpRequirement()
    {
        if (playerData.Player_Level >= 5) return -1;
        return LevelExpRequirements[playerData.Player_Level + 1];
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
    /// 治疗玩家（回血），上限为最大生命值
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        Curr_HP = Mathf.Min(Curr_HP + amount, GetMaxHP());
    }

    /// <summary>
    /// 设置无敌状态（由祝福技能调用）
    /// </summary>
    public void SetInvincible(bool value) => _isInvincible = value;

    /// <summary>
    /// 玩家受到伤害（外部敌人调用）
    /// 纯粹的数据层，只关心数值变化，不处理状态转换（由PlayerStateMachine负责）
    /// </summary>
    public void TakeDamage(int damage)
    {
        // 无敌状态不受伤
        if (_isInvincible) return;

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
            // 可触发事件：Player_LevelUp
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
