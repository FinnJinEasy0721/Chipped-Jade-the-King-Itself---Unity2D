using UnityEngine;
using System;

/// <summary>
/// 通用检测区域脚本，挂载在带有Collider2D(isTrigger)的子对象上
/// 检测带有指定Tag的目标是否进入区域，并记录最后已知位置
/// </summary>
public class DetectionZone : MonoBehaviour
{
    [Tooltip("要检测的目标Tag（目视/攻击启动/转身检测器用\"Player\"，受击框用\"PlayerHitbox\"）")]
    public string targetTag = "Player";

    /// <summary>目标是否在区域内</summary>
    public bool TargetInZone { get; private set; }

    /// <summary>区域内的目标碰撞体</summary>
    public Collider2D TargetCollider { get; private set; }

    /// <summary>目标最后已知位置</summary>
    public Vector2 LastKnownPosition { get; private set; }

    /// <summary>目标进入区域时触发</summary>
    public event Action<Collider2D> OnTargetEnter;

    /// <summary>目标离开区域时触发</summary>
    public event Action<Collider2D> OnTargetExit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            TargetInZone = true;
            TargetCollider = other;
            LastKnownPosition = other.transform.position;
            OnTargetEnter?.Invoke(other);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            LastKnownPosition = other.transform.position;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            TargetInZone = false;
            TargetCollider = null;
            OnTargetExit?.Invoke(other);
        }
    }
}
