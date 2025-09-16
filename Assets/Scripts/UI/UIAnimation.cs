using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

    public enum AnimType
    {
        Float,
        Scale,
        Move,
        Rotate,
        Fade,
        Bounce,
        Shake,
        Pulse
    }

namespace CrimsonSanctum.UI.Animation
{
    [AddComponentMenu("Crimson Sanctum/UI/UI Animation")]
    public class UIAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private AnimType animType = AnimType.Float;
        [SerializeField] private float effectStrength = 1f;

        [Header("Timing")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private LoopType loopType = LoopType.Yoyo;
        [SerializeField] private Ease easeType = Ease.InOutSine;
        [SerializeField] private bool randomizeStartTime = true;
        [SerializeField] private float maxStartDelay = 1f;
        [SerializeField] private bool isFlipWhenBack = false;

        [Header("Float Settings")]
        [SerializeField] private Vector2 floatDirection = Vector2.up;
        [SerializeField] private float floatDistance = 10f;

        [Header("Scale Settings")]
        [SerializeField] private Vector3 scaleMultiplier = Vector3.one * 1.2f;

        [Header("Move Settings")]
        [SerializeField] private Vector2 moveTarget = Vector2.zero;
        [SerializeField] private bool useRelativeMove = true;

        [Header("Rotate Settings")]
        [SerializeField] private Vector3 rotationAmount = new Vector3(0, 0, 360);

        [Header("Fade Settings")]
        [SerializeField] private float fadeFrom = 1f;
        [SerializeField] private float fadeTo = 0f;

        [Header("Bounce Settings")]
        [SerializeField] private float bounceHeight = 20f;
        [SerializeField] private int bounceCount = 1;

        [Header("Shake Settings")]
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private int shakeVibrato = 10;

        [Header("Pulse Settings")]
        [SerializeField] private float pulseScale = 1.1f;

        private Tween currentTween;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private float originalAlpha;
        private bool isFlipped = false;

        public AnimType AnimType => animType;
        public float EffectStrength => effectStrength;
        public bool IsPlaying => currentTween != null && currentTween.IsActive();

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && (animType == AnimType.Fade))
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            StoreOriginalValues();
        }

        private void Start()
        {
            if (playOnStart)
            {
                if (randomizeStartTime && maxStartDelay > 0f)
                {
                    float randomDelay = Random.Range(0f, maxStartDelay);
                    Invoke(nameof(PlayAnimation), randomDelay);
                }
                else
                {
                    PlayAnimation();
                }
            }
        }

        private void OnDestroy()
        {
            StopAnimation();
        }

        private void StoreOriginalValues()
        {
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localRotation;
            originalAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        }

        public void PlayAnimation()
        {
            StopAnimation();
            currentTween = CreateAnimation();
        }

        public void StopAnimation()
        {
            if (currentTween != null)
            {
                currentTween.Kill();
                currentTween = null;
            }
        }

        public void PauseAnimation()
        {
            if (currentTween != null && currentTween.IsActive())
                currentTween.Pause();
        }

        public void ResumeAnimation()
        {
            if (currentTween != null && currentTween.IsActive())
                currentTween.Play();
        }

        public void ResetToOriginal()
        {
            StopAnimation();
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localScale = originalScale;
            rectTransform.localRotation = originalRotation;
            if (canvasGroup != null)
                canvasGroup.alpha = originalAlpha;
            ResetFlip();
        }

        private Tween CreateAnimation()
        {
            if (rectTransform == null) return null;

            Tween tween = null;

            switch (animType)
            {
                case AnimType.Float:
                    tween = CreateFloatAnimation();
                    break;
                case AnimType.Scale:
                    tween = CreateScaleAnimation();
                    break;
                case AnimType.Move:
                    tween = CreateMoveAnimation();
                    break;
                case AnimType.Rotate:
                    tween = CreateRotateAnimation();
                    break;
                case AnimType.Fade:
                    tween = CreateFadeAnimation();
                    break;
                case AnimType.Bounce:
                    tween = CreateBounceAnimation();
                    break;
                case AnimType.Shake:
                    tween = CreateShakeAnimation();
                    break;
                case AnimType.Pulse:
                    tween = CreatePulseAnimation();
                    break;
            }

            if (tween != null && loop)
            {
                tween.SetLoops(-1, loopType);
                
                // Add flip callback when looping back if enabled
                if (isFlipWhenBack && loopType == LoopType.Yoyo)
                {
                    tween.OnStepComplete(() => {
                        FlipElement();
                    });
                }
            }

            // Add random delay to the tween start if specified
            if (tween != null && randomizeStartTime && maxStartDelay > 0f)
            {
                float randomDelay = Random.Range(0f, maxStartDelay);
                tween.SetDelay(randomDelay);
            }

            return tween;
        }

        private Tween CreateFloatAnimation()
        {
            Vector2 targetOffset = floatDirection.normalized * floatDistance * effectStrength;
            Vector2 targetPosition = (Vector2)originalPosition + targetOffset;
            return rectTransform.DOAnchorPos(targetPosition, duration).SetEase(easeType);
        }

        private Tween CreateScaleAnimation()
        {
            Vector3 targetScale = Vector3.Scale(originalScale, scaleMultiplier) * effectStrength;
            return rectTransform.DOScale(targetScale, duration).SetEase(easeType);
        }

        private Tween CreateMoveAnimation()
        {
            Vector2 target = useRelativeMove ? (Vector2)originalPosition + moveTarget * effectStrength : moveTarget;
            return rectTransform.DOAnchorPos(target, duration).SetEase(easeType);
        }

        private Tween CreateRotateAnimation()
        {
            Vector3 targetRotation = rotationAmount * effectStrength;
            return rectTransform.DORotate(targetRotation, duration, RotateMode.LocalAxisAdd).SetEase(easeType);
        }

        private Tween CreateFadeAnimation()
        {
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            float targetAlpha = Mathf.Lerp(fadeFrom, fadeTo, effectStrength);
            return canvasGroup.DOFade(targetAlpha, duration).SetEase(easeType);
        }

        private Tween CreateBounceAnimation()
        {
            float height = bounceHeight * effectStrength;
            return rectTransform.DOJumpAnchorPos((Vector2)originalPosition, height, bounceCount, duration).SetEase(easeType);
        }

        private Tween CreateShakeAnimation()
        {
            float strength = shakeStrength * effectStrength;
            return rectTransform.DOShakeAnchorPos(duration, strength, shakeVibrato).SetEase(easeType);
        }

        private Tween CreatePulseAnimation()
        {
            Vector3 targetScale = originalScale * (1f + (pulseScale - 1f) * effectStrength);
            return rectTransform.DOScale(targetScale, duration).SetEase(easeType);
        }

        public void SetAnimationType(AnimType newType)
        {
            animType = newType;
            if (IsPlaying)
                PlayAnimation();
        }

        public void SetEffectStrength(float newStrength)
        {
            effectStrength = newStrength;
            if (IsPlaying)
                PlayAnimation();
        }

        public void SetDuration(float newDuration)
        {
            duration = newDuration;
            if (IsPlaying)
                PlayAnimation();
        }
        
        private void FlipElement()
        {
            if (rectTransform == null) return;
            
            isFlipped = !isFlipped;
            Vector3 scale = rectTransform.localScale;
            scale.x = isFlipped ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            rectTransform.localScale = scale;
        }
        
        public void ResetFlip()
        {
            if (rectTransform == null) return;
            
            isFlipped = false;
            Vector3 scale = rectTransform.localScale;
            scale.x = Mathf.Abs(scale.x);
            rectTransform.localScale = scale;
        }
    }
}