using System.IO;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Video/Video Player StreamingAssets (Simple)")]
public class Video_Player_Streaming_Assets : MonoBehaviour
{
    [Tooltip("Optional subfolder inside StreamingAssets (no leading/trailing slashes).")]
    [SerializeField] private string subfolder = "";

    [Tooltip("File name to look for. Can include extension (e.g. 'clip.mp4') or just base name (e.g. 'clip').")]
    [SerializeField] private string fileName = "";

    [Tooltip("VideoPlayer that will receive the VideoClip or URL. If null, the component on this GameObject will be used.")]
    [SerializeField] private VideoPlayer targetVideoPlayer;

    [Tooltip("Optional AudioSource to route video audio to when assigning a VideoClip.")]
    [SerializeField] private AudioSource targetAudioSource;

    [Tooltip("If true the VideoPlayer will play automatically when assigned.")]
    [SerializeField] private bool playOnLoad = true;

    private static readonly string[] DefaultExtensions = { ".mp4", ".ogv", ".webm", ".mov", ".m4v" };

    private void Reset()
    {
        if (targetVideoPlayer == null)
            targetVideoPlayer = GetComponent<VideoPlayer>();
        if (targetAudioSource == null)
            targetAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        if (targetVideoPlayer == null)
            targetVideoPlayer = GetComponent<VideoPlayer>();

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
            Debug.Log($"Video_Player_Streaming_Assets: File '{fileName}' not found in StreamingAssets/{subfolder}");
            return;
        }

        // Try to find an imported VideoClip asset with the same base name (editor or Resources).
        string baseName = Path.GetFileNameWithoutExtension(foundPath);
        VideoClip clip = null;

#if UNITY_EDITOR
        // In editor try to find a VideoClip asset by name
        try
        {
            var guids = AssetDatabase.FindAssets(baseName + " t:VideoClip");
            if (guids != null && guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                clip = AssetDatabase.LoadAssetAtPath<VideoClip>(assetPath);
            }
        }
        catch { /* ignore editor search errors */ }
#endif

        // Try Resources as fallback (runtime-friendly if clip placed in Resources)
        if (clip == null)
        {
            clip = Resources.Load<VideoClip>(baseName);
        }

        if (clip != null && targetVideoPlayer != null)
        {
            // Assign the imported VideoClip to the VideoPlayer
            targetVideoPlayer.source = VideoSource.VideoClip;
            targetVideoPlayer.clip = clip;

            if (targetAudioSource != null)
            {
                targetVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                targetVideoPlayer.SetTargetAudioSource(0, targetAudioSource);
            }
            else
            {
                targetVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            }

            targetVideoPlayer.playOnAwake = false;
            if (playOnLoad)
                targetVideoPlayer.Play();

            Debug.Log($"Video_Player_Streaming_Assets: Assigned VideoClip '{clip.name}' to VideoPlayer.");
            return;
        }

        // If no VideoClip asset found, fall back to URL mode using the file path.
        if (targetVideoPlayer != null)
        {
            string uri = foundPath;
            if (!uri.StartsWith("file://"))
                uri = "file://" + uri;

            targetVideoPlayer.source = VideoSource.Url;
            targetVideoPlayer.url = uri;
            targetVideoPlayer.playOnAwake = false;

            if (targetAudioSource != null)
            {
                targetVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                targetVideoPlayer.SetTargetAudioSource(0, targetAudioSource);
            }
            else
            {
                targetVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            }

            if (playOnLoad)
                targetVideoPlayer.Play();

            Debug.Log($"Video_Player_Streaming_Assets: Assigned URL '{uri}' to VideoPlayer (no VideoClip asset found).");
        }
    }
}
