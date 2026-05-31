using UnityEngine;

/// <summary>
/// 敌人基础属性，纯数据配置，由子类或AI控制器读取
/// </summary>
public class EnemyStatBase : MonoBehaviour
{
    [Header("基础属性")]
    public int MaxHP;
    [HideInInspector] public int CurHP;
    public float PatrolSpeed;
    public float ChaseSpeed;

    [Header("朝向")]
    public bool FacingRight = true;

    [Header("攻击属性")]
    public float AttackDelay;
    public int AttackPower;
    public float CriticalRate;

    [Header("行为属性")]
    public float IdleToPatrolTimeRangeMin;
    public float IdleToPatrolTimeRangeMax;
    public float PatrolToIdleTimeRangeMin;
    public float PatrolToIdleTimeRangeMax;
    public float KnockbackDistance;

    protected virtual void Awake()
    {
        CurHP = MaxHP;
    }
}
