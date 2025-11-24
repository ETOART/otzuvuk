using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Toggle))]
public class UIToggleAnimator : MonoBehaviour
{
    [Header("UI Parts")]
    public RectTransform handle;
    public Image handleImage;
    public Image backgroundImage;

    [Header("Move")]
    public Vector2 offPosition;
    public Vector2 onPosition;

    [Header("Colors")]
    public Color handleColorOn = Color.white;
    public Color handleColorOff = Color.black;
    public Color bgColorOn = new Color(0.5f, 0.5f, 0.5f);
    public Color bgColorOff = new Color(0.3f, 0.3f, 0.3f);

    [Header("Settings")]
    public float duration = 0.25f;
    public Ease ease = Ease.InOutSine;

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChanged);
        InitVisual(toggle.isOn);
    }

    private void OnToggleChanged(bool isOn)
    {
        AnimateToggle(isOn);
    }

    private void InitVisual(bool isOn)
    {
        handle.anchoredPosition = isOn ? onPosition : offPosition;
        handleImage.color = isOn ? handleColorOn : handleColorOff;
        backgroundImage.color = isOn ? bgColorOn : bgColorOff;
    }

    private void AnimateToggle(bool isOn)
    {
        handle.DOAnchorPos(isOn ? onPosition : offPosition, duration).SetEase(ease);
        handleImage.DOColor(isOn ? handleColorOn : handleColorOff, duration);
        backgroundImage.DOColor(isOn ? bgColorOn : bgColorOff, duration);
    }
}
