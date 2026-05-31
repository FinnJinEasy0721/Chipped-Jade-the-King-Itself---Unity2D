using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人有限状态机
/// 管理状态切换与动画播放，动画Clip通过检查器手动赋值，懒加载模式
/// 需要一个基础AnimatorController，其中包含Idle/Patrol/Chase/Attack/Hurt/Die状态（占位Clip即可）
/// 运行时自动用检查器赋值的Clip覆盖占位Clip
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyFSM : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        AttackCooldown,
        Hurt,
        Die
    }

    [Header("动画剪辑（检查器手动赋值，名称对应AnimatorController中的状态名）")]
    public AnimationClip IdleClip;
    public AnimationClip PatrolClip;
    public AnimationClip ChaseClip;
    public AnimationClip AttackClip;
    public AnimationClip HurtClip;
    public AnimationClip DieClip;

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
    public EnemyState PreviousState { get; private set; } = EnemyState.Idle;

    private Animator _animator;
    private AnimatorOverrideController _overrideController;
    private bool _clipsApplied;

    /// <summary>AttackClip 的播放时长（秒），供AI控制器用作攻击计时</summary>
    public float AttackClipLength => AttackClip != null ? AttackClip.length : 1f;

    /// <summary>HurtClip 的播放时长（秒），供AI控制器用作受击计时</summary>
    public float HurtClipLength => HurtClip != null ? HurtClip.length : 0.5f;

    /// <summary>DieClip 的播放时长（秒），供AI控制器用作死亡计时</summary>
    public float DieClipLength => DieClip != null ? DieClip.length : 1f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 懒加载：首次播放动画时将检查器赋值的Clip覆盖到AnimatorController
    /// </summary>
    private void EnsureClipsApplied()
    {
        if (_clipsApplied) return;
        _clipsApplied = true;

        if (_animator.runtimeAnimatorController == null) return;

        _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _overrideController;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        _overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            var replacement = GetClipForName(overrides[i].Key.name);
            if (replacement != null)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, replacement);
            }
        }
        _overrideController.ApplyOverrides(overrides);
    }

    private AnimationClip GetClipForName(string name)
    {
        return name switch
        {
            "Idle" => IdleClip,
            "Patrol" => PatrolClip,
            "Chase" => ChaseClip,
            "Attack" => AttackClip,
            "Hurt" => HurtClip,
            "Die" => DieClip,
            _ => null
        };
    }

    /// <summary>
    /// 切换状态并播放对应动画（同一状态不会重复切换，Die状态不可切出）
    /// </summary>
    public void ChangeState(EnemyState newState)
    {
        if (CurrentState == newState) return;
        if (CurrentState == EnemyState.Die) return;

        PreviousState = CurrentState;
        CurrentState = newState;
        PlayStateAnimation(newState);
    }

    /// <summary>
    /// 强制切换状态（忽略同状态检查，用于初始化）
    /// </summary>
    public void ForceChangeState(EnemyState newState)
    {
        if (CurrentState == EnemyState.Die) return;

        PreviousState = CurrentState;
        CurrentState = newState;
        PlayStateAnimation(newState);
    }

    private void PlayStateAnimation(EnemyState state)
    {
        EnsureClipsApplied();
        // AttackCooldown 逻辑状态，播放 Idle 动画
        string animName = state == EnemyState.AttackCooldown ? "Idle" : state.ToString();
        _animator.Play(animName, 0, 0f);
    }

    /// <summary>
    /// 当前动画是否播放完毕
    /// </summary>
    public bool IsCurrentAnimationFinished()
    {
        return _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
    }

    /// <summary>
    /// 获取当前动画归一化播放进度 (0~1)
    /// </summary>
    public float GetAnimationNormalizedTime()
    {
        return _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
}
