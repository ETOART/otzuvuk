using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Toggle Visibility")]
[RequireComponent(typeof(Toggle))]
public class Toggle_Vis : MonoBehaviour
{
    [Tooltip("UI Toggle to listen for changes.")]
    [SerializeField] private Toggle uiToggle;

    [Tooltip("List of GameObjects to enable/disable when toggle changes.")]
    [SerializeField] private List<GameObject> targets = new List<GameObject>();

    private void Reset()
    {
        if (uiToggle == null) uiToggle = GetComponent<Toggle>();
    }

    private void Awake()
    {
        if (uiToggle == null) uiToggle = GetComponent<Toggle>();
        if (uiToggle == null)
            Debug.LogWarning("Toggle_Vis: Toggle component not found.", this);
    }

    private void OnEnable()
    {
        if (uiToggle != null)
        {
            uiToggle.onValueChanged.AddListener(OnToggleChanged);
            // initial sync
            OnToggleChanged(uiToggle.isOn);
        }
    }

    private void OnDisable()
    {
        if (uiToggle != null)
            uiToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var go = targets[i];
            if (go != null)
                go.SetActive(isOn);
        }
    }

    // Public helper to force refresh from other scripts (optional)
    public void Refresh() => OnToggleChanged(uiToggle != null ? uiToggle.isOn : false);
}
