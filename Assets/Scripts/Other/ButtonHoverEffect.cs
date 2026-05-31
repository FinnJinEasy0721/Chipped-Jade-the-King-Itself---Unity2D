using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("缩放设置")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);

    [Tooltip("缩放速度")]
    public float scaleSpeed = 10f;

    [Header("粒子特效")]
    public ParticleSystem clickParticle;

    private Vector3 targetScale;

    private void Start()
    {
        targetScale = normalScale;
        transform.localScale = normalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    /// <summary>
    /// 鼠标进入
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScale;
    }

    /// <summary>
    /// 鼠标离开
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }

    /// <summary>
    /// 点击按钮
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 播放粒子特效
        if (clickParticle != null)
        {
            clickParticle.Play();
        }
    }
}