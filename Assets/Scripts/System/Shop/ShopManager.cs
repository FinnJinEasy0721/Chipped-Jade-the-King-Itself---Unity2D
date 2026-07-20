using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店管理器：单例，管理共享商店画布的显示/隐藏
/// 挂在"商店画布"上，通过 Inspector 绑定 UI 元素
/// B键打开/关闭，CloseButton关闭，打开期间通过EventCenter锁定玩家输入
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI引用")]
    [SerializeField] private GameObject _shopPanel;     // Panel
    [SerializeField] private TMP_Text _npcNameText;     // NPCName
    [SerializeField] private TMP_Text _npcSlogenText;  // NPCSlogen
    [SerializeField] private Button _closeButton;      // CloseButton

    private readonly List<ShopNPC> _nearbyNPCs = new List<ShopNPC>();
    private bool _isShopOpen = false;
    private bool _inDialogue = false;

    public bool IsShopOpen => _isShopOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventCenter.Instance.AddListener(EventName.DialogueStart, OnDialogueStart);
        EventCenter.Instance.AddListener(EventName.DialogueEnd, OnDialogueEnd);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveListener(EventName.DialogueStart, OnDialogueStart);
        EventCenter.Instance.RemoveListener(EventName.DialogueEnd, OnDialogueEnd);
    }

    private void OnDialogueStart() => _inDialogue = true;
    private void OnDialogueEnd() => _inDialogue = false;

    private void Start()
    {
        if (_shopPanel != null) _shopPanel.SetActive(false);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseShop);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.B)) return;

        if (_isShopOpen)
        {
            CloseShop();
        }
        else if (!_inDialogue && _nearbyNPCs.Count > 0)
        {
            // 取最后进入范围的NPC
            OpenShop(_nearbyNPCs[_nearbyNPCs.Count - 1]);
        }
    }

    /// <summary>
    /// 注册NPC（玩家进入范围时调用）
    /// </summary>
    public void RegisterNPC(ShopNPC npc)
    {
        if (!_nearbyNPCs.Contains(npc))
            _nearbyNPCs.Add(npc);
    }

    /// <summary>
    /// 注销NPC（玩家离开范围时调用）
    /// </summary>
    public void UnregisterNPC(ShopNPC npc)
    {
        _nearbyNPCs.Remove(npc);
    }

    private void OpenShop(ShopNPC npc)
    {
        _isShopOpen = true;

        if (_npcNameText != null) _npcNameText.text = npc.NPCName;
        if (_npcSlogenText != null) _npcSlogenText.text = npc.NPCSlogen;
        if (_shopPanel != null) _shopPanel.SetActive(true);

        EventCenter.Instance.Invoke(EventName.ShopOpen);
    }

    /// <summary>
    /// 关闭商店（B键或CloseButton调用）
    /// </summary>
    public void CloseShop()
    {
        _isShopOpen = false;

        if (_shopPanel != null) _shopPanel.SetActive(false);

        EventCenter.Instance.Invoke(EventName.ShopClose);
    }
}
