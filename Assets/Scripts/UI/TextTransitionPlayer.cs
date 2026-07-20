using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TextTransition
{
    [System.Serializable]
    public class TextSegment
    {
        [TextArea(2, 6)]
        public string text;
    }

    public enum TypewriterDirection
    {
        LeftToRight,
        RightToLeft
    }

    public class TextTransitionPlayer : MonoBehaviour
    {
        [Header("文字段落配置")]
        [SerializeField] private List<TextSegment> textSegments = new List<TextSegment>();

        [Header("模糊配置")]
        [SerializeField] private float blurMaxIntensity = 1.0f;
        [SerializeField] private float blurInDuration = 0.5f;
        [SerializeField] private float blurOutDuration = 0.5f;

        [Header("遮罩配置")]
        [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.6f);
        [SerializeField] private float overlayFadeInDuration = 0.3f;
        [SerializeField] private float overlayFadeOutDuration = 0.3f;

        [Header("文字动画配置")]
        [SerializeField] private float textFadeInDuration = 0.5f;
        [SerializeField] private float textFadeOutDuration = 0.5f;
        [SerializeField] private float textHoldDuration = 1.5f;
        [SerializeField] private float segmentGapDuration = 0.5f;
        [SerializeField] private float typewriterSpeed = 30f;
        [SerializeField] private TypewriterDirection typewriterDirection = TypewriterDirection.LeftToRight;

        [Header("字体样式")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private float fontSize = 48f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private float characterSpacing = 0f;

        [Header("自动播放")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private bool destroyOnComplete = false;
        [SerializeField] private bool pauseGameWhilePlaying = true;

        [Header("UI 引用")]
        [SerializeField] private Image blurImage;
        [SerializeField] private Image overlayImage;
        [SerializeField] private TextMeshProUGUI textComponent;

        private Material blurMaterial;
        private Coroutine transitionCoroutine;
        private float savedTimeScale = 1f;

        private static readonly int BlurSizeID = Shader.PropertyToID("_BlurSize");

        private void Awake()
        {
            if (blurImage != null)
            {
                blurMaterial = new Material(blurImage.material);
                blurImage.material = blurMaterial;
            }

            if (overlayImage != null)
            {
                Color c = overlayColor;
                c.a = 0f;
                overlayImage.color = c;
            }

            if (textComponent != null)
            {
                if (fontAsset != null)
                    textComponent.font = fontAsset;
                textComponent.fontSize = fontSize;
                textComponent.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
                textComponent.characterSpacing = characterSpacing;
                textComponent.text = string.Empty;
            }

            SetBlurIntensity(0f);
        }

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        public void Play()
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(PlayTransition());
        }

        private IEnumerator PlayTransition()
        {
            if (startDelay > 0f)
                yield return new WaitForSecondsRealtime(startDelay);

            if (pauseGameWhilePlaying)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            // 阶段1: 模糊淡入 + 遮罩淡入
            float blurTimer = 0f;
            float overlayTimer = 0f;
            float blurDur = Mathf.Max(0.001f, blurInDuration);
            float overlayDur = Mathf.Max(0.001f, overlayFadeInDuration);

            while (blurTimer < blurDur || overlayTimer < overlayDur)
            {
                if (blurTimer < blurDur)
                {
                    blurTimer += Time.unscaledDeltaTime;
                    SetBlurIntensity(Mathf.Lerp(0f, blurMaxIntensity, blurTimer / blurDur));
                }
                if (overlayTimer < overlayDur)
                {
                    overlayTimer += Time.unscaledDeltaTime;
                    float a = Mathf.Lerp(0f, overlayColor.a, overlayTimer / overlayDur);
                    SetOverlayAlpha(a);
                }
                yield return null;
            }

            SetBlurIntensity(blurMaxIntensity);
            SetOverlayAlpha(overlayColor.a);

            // 阶段2: 逐段显示文字
            for (int i = 0; i < textSegments.Count; i++)
            {
                yield return StartCoroutine(PlayTextSegment(textSegments[i].text));

                if (i < textSegments.Count - 1 && segmentGapDuration > 0f)
                    yield return new WaitForSecondsRealtime(segmentGapDuration);
            }

            // 阶段3: 遮罩淡出 + 模糊淡出
            blurTimer = 0f;
            overlayTimer = 0f;
            float blurOutDur = Mathf.Max(0.001f, blurOutDuration);
            float overlayOutDur = Mathf.Max(0.001f, overlayFadeOutDuration);

            while (blurTimer < blurOutDur || overlayTimer < overlayOutDur)
            {
                if (blurTimer < blurOutDur)
                {
                    blurTimer += Time.unscaledDeltaTime;
                    SetBlurIntensity(Mathf.Lerp(blurMaxIntensity, 0f, blurTimer / blurOutDur));
                }
                if (overlayTimer < overlayOutDur)
                {
                    overlayTimer += Time.unscaledDeltaTime;
                    float a = Mathf.Lerp(overlayColor.a, 0f, overlayTimer / overlayOutDur);
                    SetOverlayAlpha(a);
                }
                yield return null;
            }

            SetBlurIntensity(0f);
            SetOverlayAlpha(0f);
            SetTextAlpha(0f);

            // 动画结束后禁用全屏图片的射线检测，避免遮挡其他UI点击
            if (blurImage != null) blurImage.raycastTarget = false;
            if (overlayImage != null) overlayImage.raycastTarget = false;

            if (pauseGameWhilePlaying)
                Time.timeScale = savedTimeScale;

            if (destroyOnComplete)
                Destroy(gameObject);
        }

        private IEnumerator PlayTextSegment(string text)
        {
            textComponent.text = text;
            textComponent.maxVisibleCharacters = 0;
            textComponent.isRightToLeftText = typewriterDirection == TypewriterDirection.RightToLeft;
            SetTextAlpha(0f);

            float fadeTimer = 0f;
            float fadeDur = Mathf.Max(0.001f, textFadeInDuration);
            int totalChars = text.Length;
            float charTimer = 0f;
            float charInterval = typewriterSpeed > 0f ? 1f / typewriterSpeed : 0f;

            while (fadeTimer < fadeDur || textComponent.maxVisibleCharacters < totalChars)
            {
                if (fadeTimer < fadeDur)
                {
                    fadeTimer += Time.unscaledDeltaTime;
                    SetTextAlpha(Mathf.Lerp(0f, 1f, fadeTimer / fadeDur));
                }
                else
                {
                    SetTextAlpha(1f);
                }

                if (charInterval > 0f)
                {
                    charTimer += Time.unscaledDeltaTime;
                    int visibleCount = Mathf.FloorToInt(charTimer / charInterval);
                    textComponent.maxVisibleCharacters = Mathf.Clamp(visibleCount, 0, totalChars);
                }
                else
                {
                    textComponent.maxVisibleCharacters = totalChars;
                }

                yield return null;
            }

            SetTextAlpha(1f);
            textComponent.maxVisibleCharacters = totalChars;

            if (textHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(textHoldDuration);

            // 淡出
            float outTimer = 0f;
            float outDur = Mathf.Max(0.001f, textFadeOutDuration);
            while (outTimer < outDur)
            {
                outTimer += Time.unscaledDeltaTime;
                SetTextAlpha(Mathf.Lerp(1f, 0f, outTimer / outDur));
                yield return null;
            }

            SetTextAlpha(0f);
            textComponent.text = string.Empty;
        }

        private void SetBlurIntensity(float intensity)
        {
            if (blurMaterial != null)
                blurMaterial.SetFloat(BlurSizeID, intensity);
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (overlayImage != null)
            {
                Color c = overlayColor;
                c.a = alpha;
                overlayImage.color = c;
            }
        }

        private void SetTextAlpha(float alpha)
        {
            if (textComponent != null)
            {
                Color c = textColor;
                c.a = alpha;
                textComponent.color = c;
            }
        }
    }
}
