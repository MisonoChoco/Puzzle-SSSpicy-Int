using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UniversalEffectAnimator : MonoBehaviour
{
    public enum EffectType
    {
        Random,
        FadeInFromTop,
        FadeInFromBottom,
        FadeInFromLeft,
        FadeInFromRight,
        PopUp,
        MergeFromRandom,
        FadeInPlace
    }

    public EffectType selectedEffect = EffectType.PopUp;
    public float duration = 0.5f;
    public float offsetDistance = 100f; // For canvas UI, use pixels
    public bool playOnStart = true;

    private SpriteRenderer spriteRenderer;
    private Graphic uiGraphic; // Image, Text
    private CanvasGroup canvasGroup; // For TMP_Text
    private Vector3 originalPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiGraphic = GetComponent<Graphic>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null && uiGraphic != null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (playOnStart)
            PlayEffect();
    }

    public void PlayEffect()
    {
        EffectType effectToPlay = selectedEffect;

        if (selectedEffect == EffectType.Random)
        {
            // Exclude Random itself
            int randomIndex = Random.Range(1, System.Enum.GetValues(typeof(EffectType)).Length);
            effectToPlay = (EffectType)randomIndex;
            Debug.Log("[UniversalEffectAnimator] Random effect selected: " + effectToPlay);
        }

        switch (effectToPlay)
        {
            case EffectType.FadeInFromTop:
                AnimateFromDirection(Vector2.up);
                break;

            case EffectType.FadeInFromBottom:
                AnimateFromDirection(Vector2.down);
                break;

            case EffectType.FadeInFromLeft:
                AnimateFromDirection(Vector2.left);
                break;

            case EffectType.FadeInFromRight:
                AnimateFromDirection(Vector2.right);
                break;

            case EffectType.PopUp:
                AnimatePopUp();
                break;

            case EffectType.MergeFromRandom:
                AnimateFromDirection(Random.insideUnitCircle.normalized);
                break;

            case EffectType.FadeInPlace:
                AnimateFadeOnly();
                break;
        }
    }

    private void AnimateFromDirection(Vector2 dir)
    {
        ResetAlpha(0f);
        transform.localPosition = originalPosition + (Vector3)(dir * offsetDistance);

        Sequence s = DOTween.Sequence();
        s.Join(transform.DOLocalMove(originalPosition, duration).SetEase(Ease.OutCubic));
        s.Join(FadeTo(1f, duration));
    }

    private void AnimatePopUp()
    {
        ResetAlpha(0f);
        transform.localScale = Vector3.zero;

        Sequence s = DOTween.Sequence();
        s.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutBack));
        s.Join(FadeTo(1f, duration));
    }

    private void AnimateFadeOnly()
    {
        ResetAlpha(0f);
        FadeTo(1f, duration).SetEase(Ease.Linear);
    }

    private void ResetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
        else if (uiGraphic != null)
        {
            var c = uiGraphic.color;
            c.a = alpha;
            uiGraphic.color = c;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }

    private Tween FadeTo(float alpha, float time)
    {
        if (spriteRenderer != null)
            return spriteRenderer.DOFade(alpha, time);
        else if (uiGraphic != null)
            return uiGraphic.DOFade(alpha, time);
        else if (canvasGroup != null)
            return canvasGroup.DOFade(alpha, time);

        return null;
    }
}