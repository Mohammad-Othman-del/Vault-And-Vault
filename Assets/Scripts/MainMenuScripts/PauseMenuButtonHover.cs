using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class PauseMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public AudioClip hoverSound;
    
    [SerializeField] private float scaleMultiplier = 1.05f;
    [SerializeField] private float animDuration = 0.08f;
    [SerializeField] private Color hoverBgColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Brighter grey
    [SerializeField] private Color hoverTextColor = new Color(0.91f, 0.44f, 0.08f, 1f); // Orange accent

    private Vector3 originalScale;
    private Image bgImage;
    private TextMeshProUGUI textLabel;
    
    private Color originalBgColor;
    private Color originalTextColor;
    private Coroutine currentAnim;
    void Awake()
    {
        originalScale = transform.localScale;
        
        bgImage = GetComponent<Image>();
        if (bgImage != null) originalBgColor = bgImage.color;
        
        textLabel = GetComponentInChildren<TextMeshProUGUI>();
        if (textLabel != null) originalTextColor = textLabel.color;

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

        if (bgImage != null && originalBgColor != default)
        {
            bgImage.color = originalBgColor;
        }

        if (textLabel != null && originalTextColor != default)
        {
            textLabel.color = originalTextColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayHoverSound();
        }
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(Animate(originalScale * scaleMultiplier, hoverBgColor, hoverTextColor));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverState();
    }

    private void PlayClickSound()
    {
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.PlayClickSound();
        }
    }

    IEnumerator Animate(Vector3 targetScale, Color targetBgColor, Color targetTextColor)
    {
        Vector3 startScale = transform.localScale;
        Color startBgColor = bgImage != null ? bgImage.color : Color.white;
        Color startTextColor = textLabel != null ? textLabel.color : Color.white;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animDuration; // Use unscaledTime because Time.timeScale is 0 when paused
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, s);
            if (bgImage != null) bgImage.color = Color.Lerp(startBgColor, targetBgColor, s);
            if (textLabel != null) textLabel.color = Color.Lerp(startTextColor, targetTextColor, s);
            
            yield return null;
        }

        transform.localScale = targetScale;
        if (bgImage != null) bgImage.color = targetBgColor;
        if (textLabel != null) textLabel.color = targetTextColor;
    }
}
