using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[AddComponentMenu("Audio/Sound Distance Control")]
public class Sound_Dist_Controll : MonoBehaviour
{
    [Tooltip("Scrollbars that control corresponding audio sources. Range 0..1.")]
    [SerializeField] private List<Scrollbar> scrollbars = new List<Scrollbar>();

    [Tooltip("AudioSources whose maxDistance will be controlled (matched by index to scrollbars).")]
    [SerializeField] private List<AudioSource> audioSources = new List<AudioSource>();

    [Tooltip("Mapping range: scrollbar 0..1 -> minDistance..maxDistance")]
    [SerializeField] private float minDistance = 0f;

    [SerializeField] private float maxDistance = 100f;

    [Header("Persistence")]
    [Tooltip("Optional Setting_Saver component used to persist scrollbar values to StreamingAssets.")]
    [SerializeField] private Setting_Saver settingSaver;

    // Stored listeners so we can unregister them on destroy
    private readonly List<UnityAction<float>> listeners = new List<UnityAction<float>>();

    private void Reset()
    {
        // Try to auto-assign common cases if lists are empty
        if (scrollbars.Count == 0)
            scrollbars.AddRange(GetComponentsInChildren<Scrollbar>(true));
        if (audioSources.Count == 0)
            audioSources.AddRange(GetComponentsInChildren<AudioSource>(true));
        if (settingSaver == null)
            settingSaver = GetComponent<Setting_Saver>();
    }

    private void Start()
    {
        // Ensure lists exist
        if (scrollbars == null) scrollbars = new List<Scrollbar>();
        if (audioSources == null) audioSources = new List<AudioSource>();

        // If a saver is assigned, try to load previous values before we add listeners.
        if (settingSaver != null)
        {
            try
            {
                var loaded = settingSaver.Load();
                if (loaded != null && loaded.Count > 0)
                {
                    int assignCount = Mathf.Min(loaded.Count, scrollbars.Count);
                    for (int i = 0; i < assignCount; i++)
                    {
                        var sb = scrollbars[i];
                        if (sb != null)
                        {
                            // assign normalized value (0..1)
                            sb.value = Mathf.Clamp01(loaded[i]);
                        }
                    }

                    Debug.Log($"Sound_Dist_Controll: Loaded {loaded.Count} saved scrollbar values.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Sound_Dist_Controll: Failed to load saved values: {ex}");
            }
        }

        // Clear any previous listeners data
        listeners.Clear();

        int pairs = Mathf.Min(scrollbars.Count, audioSources.Count);
        if (pairs == 0)
        {
            Debug.LogWarning("Sound_Dist_Controll: No valid scrollbar/audioSource pairs found.", this);
            return;
        }

        for (int i = 0; i < pairs; i++)
        {
            int idx = i; // capture local copy for closure
            var sb = scrollbars[idx];
            if (sb == null) { listeners.Add(null); continue; }

            // Listener applies value to audio source and then triggers save via settingSaver (if assigned)
            UnityAction<float> action = (value) =>
            {
                ApplyValueToSource(idx, value);
                // Save current normalized values for all scrollbars (persisting full list)
                if (settingSaver != null)
                    SaveCurrentValues();
            };

            listeners.Add(action);
            sb.onValueChanged.AddListener(action);

            // initial sync
            ApplyValueToSource(idx, sb.value);
        }

        if (scrollbars.Count != audioSources.Count)
        {
            Debug.LogWarning($"Sound_Dist_Controll: scrollbars.Count ({scrollbars.Count}) != audioSources.Count ({audioSources.Count}). Only first {pairs} pairs will be processed.", this);
        }
    }

    private void OnDestroy()
    {
        // Unregister listeners safely
        int pairs = Mathf.Min(scrollbars.Count, listeners.Count);
        for (int i = 0; i < pairs; i++)
        {
            var sb = scrollbars[i];
            var listener = listeners[i];
            if (sb != null && listener != null)
                sb.onValueChanged.RemoveListener(listener);
        }

        listeners.Clear();
    }

    // Maps scrollbar normalized value (0..1) to min..max and applies to the AudioSource.maxDistance
    private void ApplyValueToSource(int index, float normalizedValue)
    {
        if (index < 0 || index >= audioSources.Count) return;
        var src = audioSources[index];
        if (src == null) return;

        float clamped = Mathf.Clamp01(normalizedValue);
        float mapped = Mathf.Lerp(minDistance, maxDistance, clamped);
        src.maxDistance = mapped;
    }

    // Public method to force refresh for all pairs (useful if values changed from other scripts)
    public void Refresh()
    {
        int pairs = Mathf.Min(scrollbars.Count, audioSources.Count);
        for (int i = 0; i < pairs; i++)
        {
            var sb = scrollbars[i];
            if (sb != null)
                ApplyValueToSource(i, sb.value);
        }
    }

    // Collects current normalized values from scrollbars and uses Setting_Saver to persist them.
    private void SaveCurrentValues()
    {
        if (settingSaver == null || scrollbars == null) return;
        var values = scrollbars.Select(sb => sb != null ? sb.value : 0f).ToList();
        settingSaver.Save(values);
    }
}
