using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 对话管理器：单例，管理对话UI的显示、打字机效果、空格推进
/// 挂在"对话画布"上，通过 Inspector 绑定 UI 元素
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI引用")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image speakerPic;         // SpeakerPic
    [SerializeField] private TMP_Text speakerNameText;  // SpeakerName
    [SerializeField] private TMP_Text dialogText;       // DialogText

    [Header("打字机设置")]
    [SerializeField] private float typeSpeed = 0.05f;   // 每字间隔（秒）

    private DialogueData _currentDialogue;
    private int _currentIndex;
    private bool _isTyping;
    private bool _isActive;
    private Coroutine _typeCoroutine;

    /// <summary>
    /// 当前是否有对话正在进行
    /// </summary>
    public bool IsActive => _isActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!_isActive) return;

        // 空格键：推进对话 / 跳过打字机
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Advance();
        }
    }

    /// <summary>
    /// 开始一段对话
    /// </summary>
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.nodes == null || data.nodes.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] 对话数据为空，无法开始对话");
            return;
        }

        _currentDialogue = data;
        _currentIndex = 0;
        _isActive = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        // 通知玩家锁定输入
        EventCenter.Instance.Invoke(EventName.DialogueStart);

        ShowNode(0);
    }

    /// <summary>
    /// 推进对话：打字机进行中则跳过，否则显示下一条
    /// </summary>
    public void Advance()
    {
        if (_isTyping)
        {
            // 跳过打字机，直接显示完整文本
            if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
            if (dialogText != null && _currentDialogue != null)
                dialogText.text = _currentDialogue.nodes[_currentIndex].text;
            _isTyping = false;
            return;
        }

        _currentIndex++;
        if (_currentIndex >= _currentDialogue.nodes.Count)
        {
            EndDialogue();
        }
        else
        {
            ShowNode(_currentIndex);
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    private void EndDialogue()
    {
        _isActive = false;
        _isTyping = false;
        _currentIndex = 0;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // 通知玩家解锁输入
        EventCenter.Instance.Invoke(EventName.DialogueEnd);
    }

    private void ShowNode(int index)
    {
        if (index < 0 || index >= _currentDialogue.nodes.Count)
        {
            EndDialogue();
            return;
        }

        DialogueNode node = _currentDialogue.nodes[index];
        _currentIndex = index;

        // 更新说话人名字
        if (speakerNameText != null)
            speakerNameText.text = node.speakerName;

        // 更新头像
        if (speakerPic != null)
        {
            speakerPic.sprite = node.speakerPortrait;
            speakerPic.enabled = node.speakerPortrait != null;
        }

        // 打字机效果
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeText(node.text));
    }

    private IEnumerator TypeText(string text)
    {
        _isTyping = true;
        if (dialogText != null) dialogText.text = "";

        yield return null; // 等一帧，确保 UI 清空

        if (dialogText != null)
        {
            foreach (char c in text)
            {
                dialogText.text += c;
                yield return new WaitForSeconds(typeSpeed);
            }
        }

        _isTyping = false;
    }
}
