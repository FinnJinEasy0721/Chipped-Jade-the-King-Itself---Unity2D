using UnityEngine;

/// <summary>
/// 场景中可拾取的道具实体，挂载在道具 GameObject 上
/// 提供浮动动画、玩家进入范围提示、F键拾取入背包、E键直接使用
/// </summary>
public class ItemPickup : MonoBehaviour
{
    public ItemData ItemData;

    public float BobAmplitude = 0.2f; // 上下浮动幅度
    public float BobSpeed = 2f; // 上下浮动速度

    private bool _playerInRange; // 玩家是否在交互范围内
    private GameObject _player; // 当前范围内的玩家对象引用
    private Vector3 _originPos; // 初始位置，浮动动画以此为中心

    private void Start()
    {
        _originPos = transform.position;

        // 若配置了道具图标，自动设置 SpriteRenderer
        if (ItemData != null && ItemData.ItemIcon != null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ItemData.ItemIcon;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            _player = other.gameObject;
            // 通知 UI 显示道具提示
            EventCenter.Instance.Invoke<ItemData>(EventName.ShowTips, ItemData);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            _player = null;
            // 通知 UI 隐藏道具提示
            EventCenter.Instance.Invoke(EventName.HideTips);
        }
    }

    private void Update()
    {
        // 道具上下浮动动画
        transform.position = _originPos + Vector3.up * Mathf.Sin(Time.time * BobSpeed) * BobAmplitude;

        // 不在交互范围内则不处理输入
        if (!_playerInRange) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            // F键：拾取存入背包
            if (BagSystem.Instance.AddItem(ItemData))
                Destroy(gameObject);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // E键：直接使用，不存入背包
            BagSystem.Instance.ApplyEffects(ItemData, _player);
            EventCenter.Instance.Invoke<ItemData>(EventName.ItemUse, ItemData);
            Debug.Log($"[ItemPickup] 直接使用 {ItemData.ItemName}");
            Destroy(gameObject);
        }
    }
}
