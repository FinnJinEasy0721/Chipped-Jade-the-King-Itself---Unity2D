using UnityEngine;
using System;

/// <summary>
/// 攻击判定框组件
/// 挂载在攻击检测子对象上（有 Collider2D isTrigger + DetectionZone）
/// </summary>
[RequireComponent(typeof(DetectionZone))]
public class AttackHitbox : MonoBehaviour
{
    private DetectionZone _detectionZone;
    private bool _hasHit;

    /// <summary>命中目标时触发，参数为目标碰撞体</summary>
    public event Action<Collider2D> OnHit;

    private void Awake()
    {
        _detectionZone = GetComponent<DetectionZone>();
        AutoSetTargetTag();
    }

    /// <summary>
    /// 根据自身 Tag 自动设置要检测的目标 Tag
    /// </summary>
    private void AutoSetTargetTag()
    {
        switch (gameObject.tag)
        {
            case "PlayerAttackBox":
                _detectionZone.targetTag = "EnemyHurtBox";
                break;
            case "EnemyAttackBox":
                _detectionZone.targetTag = "PlayerHurtBox";
                break;
        }
    }

    private void OnEnable()
    {
        _hasHit = false;
        _detectionZone.OnTargetEnter += HandleTargetEnter;
    }

    private void OnDisable()
    {
        _detectionZone.OnTargetEnter -= HandleTargetEnter;
    }

    private void HandleTargetEnter(Collider2D target)
    {
        if (_hasHit) return;
        _hasHit = true;
        OnHit?.Invoke(target);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 兜底：攻击框启用时若已与目标重叠，OnTriggerEnter2D 不会再触发
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        if (_hasHit) return;
        if (!other.CompareTag(_detectionZone.targetTag)) return;
        HandleTargetEnter(other);
    }

    /// <summary>启用攻击框（新一击）</summary>
    public void Activate()
    {
        gameObject.SetActive(true);
    }

    /// <summary>禁用攻击框</summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
