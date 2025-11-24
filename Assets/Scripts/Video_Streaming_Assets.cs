using System.IO;
using UnityEngine;
using UnityEngine.Video;
using ProjectorSimulator;

[DefaultExecutionOrder(-100)] // try to run before ProjectorSim Awake
[AddComponentMenu("Video/Video StreamingAssets (Simple)")]
public class Video_Streaming_Assets : MonoBehaviour
{
    [Tooltip("Optional subfolder inside StreamingAssets (no leading/trailing slashes).")]
    [SerializeField] private string subfolder = "";

    [Tooltip("File name to look for. Can include extension (e.g. 'clip.mp4') or just base name (e.g. 'clip').")]
    [SerializeField] private string fileName = "";

    [Tooltip("ProjectorSim instance to assign the video to. If null the first ProjectorSim found in scene will be used.")]
    [SerializeField] private ProjectorSim targetProjector;

    [Tooltip("RenderTexture width used for the created texture (simple default).")]
    [SerializeField] private int defaultWidth = 1280;

    [Tooltip("RenderTexture height used for the created texture (simple default).")]
    [SerializeField] private int defaultHeight = 720;

    [Tooltip("If true the VideoPlayer will play automatically when assigned.")]
    [SerializeField] private bool playOnLoad = true;

    private static readonly string[] DefaultExtensions = { ".mp4", ".ogv", ".webm", ".mov", ".m4v" };

    private void Reset()
    {
        if (targetProjector == null)
            targetProjector = GetComponentInChildren<ProjectorSim>();
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        if (targetProjector == null)
            targetProjector = FindObjectOfType<ProjectorSim>();

        if (targetProjector == null)
        {
            Debug.LogWarning("Video_Streaming_Assets: No ProjectorSim found to assign video to.", this);
            return;
        }

        string basePath = Application.streamingAssetsPath;
        if (!string.IsNullOrEmpty(subfolder))
            basePath = Path.Combine(basePath, subfolder);

        string foundPath = null;

        // If filename has extension, try direct
        if (Path.HasExtension(fileName))
        {
            string candidate = Path.Combine(basePath, fileName);
            if (File.Exists(candidate))
                foundPath = candidate;
        }
        else
        {
            // try default extensions
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

        if (string.IsNullOrEmpty(foundPath))
        {
            Debug.Log($"Video_Streaming_Assets: File '{fileName}' not found in StreamingAssets/{subfolder}");
            return;
        }

        // Create a RenderTexture and a VideoPlayer that renders into it.
        var rt = new RenderTexture(defaultWidth, defaultHeight, 0);
        rt.name = $"VideoRT_{fileName}";

        var vpOwner = this.gameObject;
        var vp = vpOwner.GetComponent<VideoPlayer>();
        if (vp == null) vp = vpOwner.AddComponent<VideoPlayer>();

        vp.playOnAwake = false;
        vp.isLooping = targetProjector != null ? targetProjector.loop : false;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.source = VideoSource.Url;

        string uri = foundPath;
        if (!uri.StartsWith("file://"))
            uri = "file://" + uri;
        vp.url = uri;

        // If ProjectorSim exposes an AudioSource, connect audio routing
        if (targetProjector != null && targetProjector.audioSource != null)
        {
            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            vp.SetTargetAudioSource(0, targetProjector.audioSource);
        }
        else
        {
            vp.audioOutputMode = VideoAudioOutputMode.None;
        }

        // Assign the RenderTexture to ProjectorSim as a renderTexture and set contentType to RT
        targetProjector.contentType = ProjectorSim.ContentType.RT;
        targetProjector.renderTexture = rt;
        targetProjector.playOnAwake = playOnLoad;

        // Optionally start playback
        if (playOnLoad)
            vp.Play();
    }
}
