using System.Collections;
using UnityEngine;

/// <summary>
/// 攻击回血增益效果：使用后持续一段时间，期间每次攻击命中敌人回复最大生命值的一定比例
/// 使用协程载体组件在目标上运行倒计时，通过监听 EnemyHurt 事件触发回血
/// </summary>
[CreateAssetMenu(fileName = "LifestealBuffEffect_", menuName = "Item/LifestealBuffEffect")]
public class LifestealBuffEffect : ItemEffect
{
    [Range(0f, 1f)]
    public float HealRatio = 0.05f; // 回复最大生命值比例（默认5%）

    public float Duration = 10f; // 持续时间（秒）

    public override void Apply(GameObject target)
    {
        var stat = target.GetComponent<PlayerStat>();
        if (stat == null) return;

        // 查找或添加协程载体组件（已存在则刷新持续时间）
        var runner = target.GetComponent<LifestealBuffRunner>();
        if (runner == null)
            runner = target.AddComponent<LifestealBuffRunner>();

        runner.StartBuff(stat, HealRatio, Duration);

        // 注册到 BuffTracker 显示在 UI 上
        RegisterBuff(target, Duration);
        Debug.Log($"[LifestealBuffEffect] 攻击回血增益启动：最大HP的{HealRatio * 100}%，持续{Duration}秒");
    }

    public override void Remove(GameObject target)
    {
        var runner = target.GetComponent<LifestealBuffRunner>();
        if (runner != null)
            runner.StopBuff();

        UnregisterBuff(target);
    }

    /// <summary>
    /// 协程载体组件：监听 EnemyHurt 事件触发回血，并驱动倒计时
    /// </summary>
    private class LifestealBuffRunner : MonoBehaviour
    {
        private PlayerStat _stat;
        private float _healRatio;
        private float _duration;
        private Coroutine _coroutine;

        private void OnEnable()
        {
            EventCenter.Instance.AddListener<CombatContext>(EventName.EnemyHurt, OnEnemyHurt);
        }

        private void OnDisable()
        {
            EventCenter.Instance.RemoveListener<CombatContext>(EventName.EnemyHurt, OnEnemyHurt);
        }

        public void StartBuff(PlayerStat stat, float healRatio, float duration)
        {
            _stat = stat;
            _healRatio = healRatio;
            _duration = duration;

            // 刷新：停止旧协程再启动新的
            if (_coroutine != null)
                StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(TimerCoroutine());
        }

        public void StopBuff()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            Destroy(this);
        }

        private void OnEnemyHurt(CombatContext ctx)
        {
            // 只有当攻击者是本玩家时才触发回血
            if (ctx.Attacker != gameObject) return;

            int heal = Mathf.RoundToInt(_stat.GetMaxHP() * _healRatio);
            _stat.Heal(heal);
            Debug.Log($"[LifestealBuffEffect] 攻击命中，回复{heal}HP（最大HP的{_healRatio * 100}%）");
        }

        private IEnumerator TimerCoroutine()
        {
            yield return new WaitForSeconds(_duration);
            Debug.Log("[LifestealBuffEffect] 攻击回血增益结束");
            Destroy(this);
        }
    }
}