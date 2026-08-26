using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float scaleMultiplier = 1.08f;
    [SerializeField] private float animDuration = 0.12f;
    [SerializeField] private Color hoverTextColor = new Color(0.90980393f, 0.43921569f, 0.08235294f, 1f);

    private Vector3 originalScale;
    private TextMeshProUGUI label;
    private Color originalTextColor;
    private Coroutine currentAnim;

    void Awake()
    {
        originalScale = transform.localScale;
        label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) originalTextColor = label.color;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    void OnEnable()
    {
        ResetHoverState();
    }

    void OnDisable()
    {
        ResetHoverState();
    }

    public void ResetHoverState()
    {
        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }

        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }

        if (label != null && originalTextColor != default)
        {
            label.color = originalTextColor;
        }
    }

    private void PlayClickSound()
    {
        GameAudioManager.Instance.PlayClickSound();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GameAudioManager.Instance.PlayHoverSound();
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(Animate(originalScale * scaleMultiplier, hoverTextColor));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverState();
    }

    IEnumerator Animate(Vector3 targetScale, Color targetColor)
    {
        Vector3 startScale = transform.localScale;
        Color startColor = label != null ? label.color : Color.white;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.localScale = Vector3.Lerp(startScale, targetScale, s);
            if (label != null) label.color = Color.Lerp(startColor, targetColor, s);
            yield return null;
        }

        transform.localScale = targetScale;
        if (label != null) label.color = targetColor;
    }
}
