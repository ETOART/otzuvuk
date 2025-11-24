using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[AddComponentMenu("Audio/Sound StreamingAssets (Simple)")]
public class Sound_Streaming_Assets : MonoBehaviour
{
    [Tooltip("Optional subfolder inside StreamingAssets (no leading/trailing slashes).")]
    [SerializeField] private string subfolder = "";

    [Tooltip("File name to look for. Can include extension (e.g. 'music.ogg') or just base name (e.g. 'music').")]
    [SerializeField] private string fileName = "";

    [Tooltip("AudioSource that will receive the loaded AudioClip. If null, will try to get one on this GameObject.")]
    [SerializeField] private AudioSource targetAudioSource;

    [Tooltip("Automatically play the clip after loading.")]
    [SerializeField] private bool playOnLoad = true;

    private static readonly string[] DefaultExtensions = { ".ogg", ".wav", ".mp3", ".aiff" };

    private void Reset()
    {
        if (targetAudioSource == null)
            targetAudioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        if (targetAudioSource == null)
            targetAudioSource = GetComponent<AudioSource>();

        string basePath = Application.streamingAssetsPath;
        if (!string.IsNullOrEmpty(subfolder))
            basePath = Path.Combine(basePath, subfolder);

        string foundPath = null;

        // If filename already has extension, try it directly
        if (Path.HasExtension(fileName))
        {
            string candidate = Path.Combine(basePath, fileName);
            if (File.Exists(candidate))
                foundPath = candidate;
        }
        else
        {
            // try default extensions in order
            foreach (var ext in DefaultExtensions)
            {
                string candidate = Path.Combine(basePath, fileName + ext);
                if (File.Exists(candidate))
                {
                    foundPath = candidate;
                    break;
                }
            }
        }

        // If file not found, do nothing (per request)
        if (string.IsNullOrEmpty(foundPath))
        {
            Debug.Log($"Sound_Streaming_Assets: File '{fileName}' not found in StreamingAssets/{subfolder}");
            return;
        }

        // Load and assign clip
        StartCoroutine(LoadAndAssign(foundPath));
    }

    private IEnumerator LoadAndAssign(string path)
    {
        string uri = path;
        if (!uri.StartsWith("http") && !uri.StartsWith("file:"))
            uri = "file://" + uri;

        var audioType = GetAudioTypeFromExtension(Path.GetExtension(path));
        using (var uwr = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
        {
            yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogWarning($"Sound_Streaming_Assets: Failed to load '{path}': {uwr.error}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(uwr);
            if (clip == null)
            {
                Debug.LogWarning($"Sound_Streaming_Assets: Loaded clip is null for '{path}'");
                yield break;
            }

            if (targetAudioSource != null)
            {
                targetAudioSource.clip = clip;
                if (playOnLoad)
                    targetAudioSource.Play();
            }
        }
    }

    private static AudioType GetAudioTypeFromExtension(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return AudioType.UNKNOWN;
        ext = ext.ToLowerInvariant();
        return ext switch
        {
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            ".mp3" => AudioType.MPEG,
            ".aiff" or ".aif" => AudioType.AIFF,
            _ => AudioType.UNKNOWN,
        };
    }
}
