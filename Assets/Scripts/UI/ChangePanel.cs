using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 面板切换控制器，管理主菜单面板与制作组面板之间的横向滑动切换
/// </summary>
public class ChangePanel : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private RectTransform _mainPanel;
    [SerializeField] private RectTransform _gameMakerPanel;

    [Header("动效参数")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _slideDistance = 2560f;

    private CanvasGroup _mainPanelCG;
    private CanvasGroup _gameMakerPanelCG;
    private RectTransform _currentPanel;
    private bool _isAnimating;

    private void Start()
    {
        _mainPanelCG = GetOrCreateCanvasGroup(_mainPanel);
        _gameMakerPanelCG = GetOrCreateCanvasGroup(_gameMakerPanel);

        // 初始状态：主面板可见，制作组面板隐藏在屏幕右侧
        _gameMakerPanelCG.alpha = 0f;
        _gameMakerPanelCG.blocksRaycasts = false;
        _gameMakerPanelCG.interactable = false;
        _gameMakerPanel.anchoredPosition = new Vector2(_slideDistance, 0);
        _currentPanel = _mainPanel;

        // 绑定"制作人"按钮 → 显示制作组面板
        var makerBtn = _mainPanel.Find("制作人")?.GetComponent<Button>();
        if (makerBtn != null)
            makerBtn.onClick.AddListener(ShowGameMakerPanel);

        // 绑定"返回按钮" → 返回主面板
        var backBtn = _gameMakerPanel.Find("返回按钮")?.GetComponent<Button>();
        if (backBtn != null)
            backBtn.onClick.AddListener(ShowMainPanel);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _currentPanel == _gameMakerPanel && !_isAnimating)
            ShowMainPanel();
    }

    public void ShowGameMakerPanel()
    {
        if (_isAnimating || _currentPanel == _gameMakerPanel) return;
        StartCoroutine(SlideTransition(_mainPanel, _gameMakerPanel, 1));
    }

    public void ShowMainPanel()
    {
        if (_isAnimating || _currentPanel == _mainPanel) return;
        StartCoroutine(SlideTransition(_gameMakerPanel, _mainPanel, -1));
    }

    /// <summary>
    /// 横向滑动切换：from 面板向 direction 方向滑出，to 面板从反方向滑入
    /// </summary>
    private IEnumerator SlideTransition(RectTransform from, RectTransform to, int direction)
    {
        _isAnimating = true;

        var fromCG = GetOrCreateCanvasGroup(from);
        var toCG = GetOrCreateCanvasGroup(to);

        toCG.blocksRaycasts = true;
        toCG.interactable = true;

        Vector2 fromStart = from.anchoredPosition;
        Vector2 toStart = new Vector2(_slideDistance * direction, 0);
        to.anchoredPosition = toStart;

        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            // EaseOutCubic
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            from.anchoredPosition = Vector2.Lerp(fromStart, new Vector2(-_slideDistance * direction, 0), eased);
            to.anchoredPosition = Vector2.Lerp(toStart, Vector2.zero, eased);

            fromCG.alpha = 1f - eased;
            toCG.alpha = eased;

            yield return null;
        }

        // 收尾：两个面板都归位(0,0)，用 alpha 控制可见性
        from.anchoredPosition = Vector2.zero;
        fromCG.alpha = 0f;
        fromCG.blocksRaycasts = false;
        fromCG.interactable = false;

        to.anchoredPosition = Vector2.zero;
        toCG.alpha = 1f;

        _currentPanel = to;
        _isAnimating = false;
    }

    private static CanvasGroup GetOrCreateCanvasGroup(RectTransform target)
    {
        var cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
