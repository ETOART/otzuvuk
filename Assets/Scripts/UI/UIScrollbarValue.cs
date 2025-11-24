using UnityEngine;
using UnityEngine.UI;
using TMPro;

[AddComponentMenu("UI/UIScrollbarValue")]
public class UIScrollbarValue : MonoBehaviour
{
    [Tooltip("Scrollbar to listen to (range 0..1).")]
    [SerializeField] private Scrollbar scrollbar;

    [Tooltip("TextMeshProUGUI that will display the integer value 0..100.")]
    [SerializeField] private TextMeshProUGUI tmpText;


    private void Reset()
    {
        // Try to auto-assign common cases
        if (scrollbar == null) scrollbar = GetComponent<Scrollbar>();
        if (tmpText == null) tmpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        if (scrollbar == null)
            Debug.LogWarning("UIScrollbarValue: Scrollbar reference is missing.", this);
        if (tmpText == null)
            Debug.LogWarning("UIScrollbarValue: TextMeshProUGUI reference is missing.", this);
    }

    private void OnEnable()
    {
        if (scrollbar != null)
        {
            scrollbar.onValueChanged.AddListener(OnScrollbarChanged);
            UpdateText(scrollbar.value); // initial sync
        }
    }

    private void OnDisable()
    {
        if (scrollbar != null)
            scrollbar.onValueChanged.RemoveListener(OnScrollbarChanged);
    }

    private void OnScrollbarChanged(float value)      
    {
        UpdateText(value);
    }

    private void UpdateText(float normalizedValue)
    {
        if (tmpText == null) return;

        // Map 0..1 to 0..100 and show integer
        int intValue = Mathf.Clamp(Mathf.RoundToInt(normalizedValue * 100f), 0, 100);
        tmpText.text = intValue.ToString();
    }

    // Optional public API to force refresh from other scripts
    public void Refresh() => UpdateText(scrollbar != null ? scrollbar.value : 0f);
}
