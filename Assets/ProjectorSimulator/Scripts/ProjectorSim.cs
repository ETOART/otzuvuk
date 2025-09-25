#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;

namespace ProjectorSimulator
{
    [ExecuteInEditMode]
    public class ProjectorSim : MonoBehaviour
    {
        public enum CookieSizes { c_128, c_256, c_512, c_1024, c_2048, c_4096 };
        public enum VignetteShape { circle, square };

        public enum ContentType {Images, RT, Video};

        public float aspectRatio = 1.6f;
        public float throwRatio = 1.0f;
        public float shift_v = 0.0f;
        public float shift_h = 0.0f;
        public float keystoneH = 0.0f;
        public float keystoneV = 0.0f;

        public float brightness = 1.0f;
        public float range = 20.0f;

        public ContentType contentType = ContentType.Images;

        // content = Images list
        public List<Texture2D> images = null;
        public int previewImageIndex = 0;
        int slideshowIndex = 0; // start slideshow at first image
        public float imageInterval = 5.0f;
        public bool loop = true;

        // content = RenderTexture
        public RenderTexture renderTexture;
        public float framerate = -1;

        // content = Video
        public VideoClip video;
        public AudioSource audioSource;

        // shared
        public bool playOnAwake = true;

        public CookieSizes cookieSize = CookieSizes.c_256;

        public bool useVignette = false;
        public VignetteShape vignetteShape = VignetteShape.circle;
        public float vignetteSize = 0.5f;
        public float vignetteFade = 0.2f;
        public float vignetteOffsetX = 0.5f;
        public float vignetteOffsetY = 0.5f;
        public bool vignetteForceCircular = true;

        public bool showLightPath = true;
        public Material lightPathMaterial;
        public float lightPathRange = 0.0f;

        // reference to the CookieCreator object
        Cookie cookie;

        // the actual RenderTexture we send to the CookieCreator
        RenderTexture contentRT = null;

        Light[] lights;
        ThrowBuilder tb;

        bool isDirty = false;

        ProjectVideo pv;

        float timeSinceUpdate = 0;

        // Use this for initialization
        void Awake()
        {
            lights = GetComponentsInChildren<Light>(true);
            tb = GetComponentInChildren<ThrowBuilder>(true);
            if (!tb)
                Debug.Log("No ThrowBuilder found! by " + gameObject.name);

            foreach (Light l in lights)
            {
                l.gameObject.SetActive(false);
            }

            // if in editor edit mode, ensure preview image index is valid
            if (!Application.isPlaying)
            {
                previewImageIndex = Mathf.Clamp(previewImageIndex, 0, images.Count - 1); //  don't allow user to choose a value out of range
            }

            switch (contentType)
            {
                case ContentType.Images:
                    // make content rt same size as biggest image
                    int biggest = int.MinValue;
                    int biggestIndex = -1;
                    for (int i = 0; i < images.Count; i++)
                    {
                        Texture2D t = images[i];
                        if (t == null)
                            continue;
                        if (t.width > biggest)
                        {
                            biggest = t.width;
                            biggestIndex = i;
                        }
                    }
                    if (biggestIndex >= 0) // found index
                    {
                        contentRT = CreateRenderTexture(images[biggestIndex].width, images[biggestIndex].height, 24); // TODO: clamp to upper value, no point in being bigger than the cookie!
                        Graphics.Blit(images[Application.isPlaying ? slideshowIndex : previewImageIndex], contentRT);
                    }
                    else // no images to check, make null so we render white
                    {
                        contentRT = null; 
                    }
                    break;
                case ContentType.RT:

                    if (!Application.isPlaying)
                        break;
                    
                    contentRT = CreateRenderTexture(renderTexture.width, renderTexture.height, 24); // TODO: clamp to upper value, no point in being bigger than the cookie!
                    if (!playOnAwake)
                    {
                        this.enabled = false;
                    }
                    
                    break;
                case ContentType.Video:

                    if (!Application.isPlaying)
                        break;
                    
                    if (video == null)
                    {
                        Debug.LogWarning("Projector Simulator: No video assigned to Projector '" + this.name + "', Projector will be disabled.");
                        this.enabled = false;
                        return;
                    }
                    contentRT = CreateRenderTexture((int)video.width, (int)video.height, 24); // TODO: clamp to upper value, no point in being bigger than the cookie!
                    pv = gameObject.AddComponent<ProjectVideo>();
                    pv.Init(video, audioSource, contentRT, loop, playOnAwake);
                    
                    break;
            }

            InitCookie();
            AssignLightCookies();
        }

        void InitCookie()
        {
            int cookieSizeInt = int.Parse(cookieSize.ToString().Substring(2));
            VignetteData vd = new VignetteData(useVignette, vignetteSize, vignetteFade, new Vector2(vignetteOffsetX, vignetteOffsetY), vignetteForceCircular, vignetteShape == VignetteShape.circle);
            cookie = new Cookie(new CookieData(shift_v, shift_h, keystoneH, keystoneV, throwRatio, aspectRatio), cookieSizeInt, lights[0], lights[1], lights[2], vd, contentRT);
        }

        static RenderTexture CreateRenderTexture(int height, int width, int depth)
        {
            RenderTexture result = new RenderTexture(width, height, depth);
            return result;
        }

        void Start()
        {
            BuildLightPath();
        }

        void BuildLightPath()
        {
            // build the light path geometry
            if (tb)
            {
                if (lightPathRange <= 0) // automatically calculate light path distance, max range = range of projector
                    tb.BuildThrow(throwRatio, aspectRatio, new Vector2(shift_h, shift_v), range, true, lightPathMaterial);
                else // use user-defined distance
                    tb.BuildThrow(throwRatio, aspectRatio, new Vector2(shift_h, shift_v), lightPathRange, false, lightPathMaterial);
            }
        }

        // When enabled, turn lights on. Also start slideshow if necessary.
        void OnEnable()
        {
            lights[0].gameObject.SetActive(true);
            lights[1].gameObject.SetActive(true);
            lights[2].gameObject.SetActive(true);

            if (showLightPath)
                tb.gameObject.SetActive(true);
        }

        // When disabled, turn lights off
        void OnDisable()
        {
            foreach (Light l in lights)
            {
                l.gameObject.SetActive(false);
            }

            tb.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (cookie == null)
                return;

            cookie.Cleanup();
        }

        /// <summary>
        /// Allow external scripts to set the slideshow index
        /// </summary>
        /// <param name="newIndex"></param>
        public void SetSlideshowIndex(int newIndex)
        {
            if (newIndex >= 0 && newIndex < images.Count)
            {
                slideshowIndex = newIndex;
            }
            else
            {
                Debug.LogWarning("Projector Simulator: Slideshow index " + newIndex + " is out of range on projector '" + this.name + "'. Argument must be between 0 and " + (GetNumImages() - 1));
                return;
            }

            Graphics.Blit(images[slideshowIndex], contentRT);
            cookie.ForceUpdateCookie();

            // if slideshow is playing, restart the timer
            timeSinceUpdate = 0;
        }

        // Allow external scripts to pause and play the slideshow

        public void Pause()
        {
            playOnAwake = false;

            if (contentType == ContentType.Video)
            {
                pv.GetVideoPlayer().Pause();
            }
        }
        public void Play()
        {
            playOnAwake = true;
            this.enabled = true;

            if (contentType == ContentType.Video)
            {
                pv.PlayVideoProjector();
            }
        }

        /// <summary>
        /// Toggle whether the projector is paused or playing
        /// </summary>
        /// <returns>Returns true if the projector is now paused</returns>
        public bool TogglePause()
        {
            if (playOnAwake)
                Pause();
            else
                Play();
            return playOnAwake;
        }

        /// <summary>
        /// Gives each light a cookie for its relevant channel, using the slideshowIndex value.
        /// </summary>
        void AssignLightCookies()
        {
            lights[0].cookie = cookie.GetRedCookie();
            lights[1].cookie = cookie.GetGreenCookie();
            lights[2].cookie = cookie.GetBlueCookie();
        }

        public void AdvanceSlideshow()
        {
            slideshowIndex++;
            if (slideshowIndex >= images.Count) // reached end?
            {
                if (loop)
                    slideshowIndex = 0;
                else
                    return; // should never normally be hit, unless AdvanceSlideshow is called externally
            }

            Graphics.Blit(images[slideshowIndex], contentRT);
            cookie.ForceUpdateCookie();
        }


        public int GetNumImages()
        {
            if (images != null)
                return images.Count;
            else
                return 0;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            timeSinceUpdate += Time.deltaTime;

            if (contentType == ContentType.Images && timeSinceUpdate < imageInterval)
                return;

            if (contentType != ContentType.Images && timeSinceUpdate < 1 / framerate)
                return;

            if (playOnAwake)
            {
                timeSinceUpdate = 0;

                if (contentType == ContentType.Images)
                {
                    if (!loop && slideshowIndex >= images.Count - 1)
                        return;

                    AdvanceSlideshow();
                }
                else
                {
                    UpdateImage();
                }
            }
        }

        /// <summary>
        /// Recalculate the size and shape of the projected image
        /// </summary>
        public void LightpathChanged()
        {
            if (cookie == null)
                return;

            cookie.Reinitialise(new CookieData(shift_v, shift_h, keystoneH, keystoneV, throwRatio, aspectRatio), new VignetteData(useVignette, vignetteSize, vignetteFade, new Vector2(vignetteOffsetX, vignetteOffsetY), vignetteForceCircular, vignetteShape == VignetteShape.circle));
            BuildLightPath();
        }

        public void UpdateImage() 
        {
            if (cookie == null)
                return;

            if (contentType == ContentType.Images)
            {
                if (images.Count < 1)
                    return;

                previewImageIndex = Mathf.Clamp(previewImageIndex, 0, images.Count - 1);

                // catch the case for when first image is added to projector, contentRT won't be initialised
                if (contentRT == null && images[previewImageIndex] != null)
                {
                    contentRT = CreateRenderTexture(images[previewImageIndex].width, images[previewImageIndex].height, 24);
                    cookie.SetProjectedImage(contentRT);
                }

                Graphics.Blit(images[Application.isPlaying ? slideshowIndex : previewImageIndex], contentRT);
            }
            else
            {
                if (contentType == ContentType.RT)
                {
                    Graphics.Blit(renderTexture, contentRT);
                }
            }
            cookie.ForceUpdateCookie();
        }

        public void UpdateSpotlight() 
        {
            foreach (Light l in lights)
            {
                l.intensity = brightness;
                l.range = range;
            }
        }

        public void UpdateLightpath()
        {
            if (!this.enabled)
                return;

            tb.gameObject.SetActive(showLightPath);
            if (showLightPath)
            {
                BuildLightPath();
            }
        }

        public void MakeDirty()
        {
            isDirty = true;
        }

        private void LateUpdate()
        {
            if (isDirty)
            {                
                isDirty = false;

                if (cookie == null)
                {
                    // this happens when Unity reloads scripts for some reason - initialise the projector
                    InitCookie(); 
                    AssignLightCookies();
                    return;
                }

                LightpathChanged();
                UpdateImage();
            }
        }
    }
}