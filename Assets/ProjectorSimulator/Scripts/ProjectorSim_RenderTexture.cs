#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ProjectorSimulator
{

    [ExecuteInEditMode]
    public class ProjectorSim_RenderTexture : MonoBehaviour
    {
        public enum CookieSizes { c_128, c_256, c_512, c_1024, c_2048, c_4096 };
        public enum VignetteShape { circle, square };

        [Header("Image size")]
        [Space(10)]
        [Tooltip("Aspect ratio of the projected image (width/height)")]
        public float aspectRatio = 1.6f;
        [Tooltip("Throw ratio of the projector (distance/image width) - smaller is wider")]
        public float throwRatio = 1.0f;
        [Tooltip("Vertical lens shift as a percentage of image width/2 (100% shift = lens level with image edge)")]
        public float shift_v = 0.0f;
        [Tooltip("Horizontal lens shift as a percentage of image width/2 (100% shift = lens level with image edge)")]
        public float shift_h = 0.0f;
        [Tooltip("Use this to make a trapezoidal image square again. Forces fallback to CPU processing.")]
        [Range(-100.0f, 100.0f)]
        public float keystoneH = 0.0f;
        [Tooltip("Use this to make a trapezoidal image square again. Forces fallback to CPU processing.")]
        [Range(-100.0f, 100.0f)]
        public float keystoneV = 0.0f;

        [Header("Brightness")]
        [Space(10)]
        [Tooltip("Allows control of the brightness of the projector")]
        [Range(0.0f, 8.0f)]
        public float brightness = 1.0f;
        [Tooltip("Controls the reach of the projector's light")]
        public float range = 20.0f;

        [Header("Projected content")]
        [Space(10)]
        [Tooltip("Texture to project. If empty, only white will be projected.")]
        public RenderTexture renderTexture;
        [Tooltip("How often the projected image will be updated (-1 = as fast as possible)")]
        public float framerate = 30f;
        [Tooltip("The resolution of the Cookie texture. Higher will have better clarity in the image, lower will be faster to generate.")]
        public CookieSizes cookieSize = CookieSizes.c_256;

        [Header("Vignette")]
        [Space(10)]
        public bool useVignette = false;
        public VignetteShape vignetteShape = VignetteShape.circle;
        [Range(0, 1f)]
        public float vignetteSize = 0.5f;
        [Range(0f, 1f)]
        public float vignetteFade = 0.2f;
        [Range(0f, 1f)]
        public float vignetteOffsetX = 0.5f;
        [Range(0f, 1f)]
        public float vignetteOffsetY = 0.5f;
        [Tooltip("Forces a circular vignette to be a circle instead of an oval. Ignored for square vignette.")]
        public bool vignetteForceCircular = true;

        [Header("Light path")]
        [Space(10)]
        [Tooltip("Toggles light path geometry.")]
        public bool showLightPath = true;
        [Tooltip("The material of the light path.")]
        public Material lightPathMaterial;
        [Tooltip("The distance the light path reaches - 0 or below makes the light stop at the furthest geometry found at the image corners. This is most useful for curved screens when you want the light path to continue beyond the corners.")]
        public float lightPathRange = 0.0f;

        bool isPlaying = true;

        Texture2D convertedTexture;

        // Cookies and lights
        Cookie cookie;
        Light[] lights;
        ThrowBuilder tb;

        // hacky, sorry
        // we always want to update on the first frame (for some reason OnEnable does not count as the first frame)
        int frameCounter = 0;

        // Use this for initialization
        void Awake()
        {
            lights = GetComponentsInChildren<Light>(true);
            tb = GetComponentInChildren<ThrowBuilder>(true);

            // start with all lights off, otherwise first frame will see lights flash as the cookies haven't processed yet
            foreach (Light l in lights)
            {
                l.gameObject.SetActive(false);
            }

            // in the editor, however, we want the first light on, in order to see the size and shape of the image
#if UNITY_EDITOR
            lights[0].gameObject.SetActive(true);
#endif

            int cookieSizeInt = int.Parse(cookieSize.ToString().Substring(2));

            VignetteData vd = new VignetteData(useVignette, vignetteSize, vignetteFade, new Vector2(vignetteOffsetX, vignetteOffsetY), vignetteForceCircular, vignetteShape == VignetteShape.circle);

#if UNITY_EDITOR
            // if in editor play mode, queue the updating images
            if (EditorApplication.isPlaying)
            {
#endif
                // do first cookie as per usual
                cookie = new Cookie(new CookieData(shift_v, shift_h, keystoneH, keystoneV, throwRatio, aspectRatio), cookieSizeInt, lights[0], lights[1], lights[2], vd, renderTexture);

                convertedTexture = new Texture2D(renderTexture.width, renderTexture.height);

                //UpdateImage();

                // garbage collection
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
#if UNITY_EDITOR
            }
            else // EDITOR EDIT MODE - only calculate the white cookie
            {
                // use slot 0 as the preview cookie
                cookie = new Cookie(new CookieData(shift_v, shift_h, keystoneH, keystoneV, throwRatio, aspectRatio), cookieSizeInt, lights[0], lights[1], lights[2], vd, null);
            }
#endif
            AssignLightCookies();
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
            if (frameCounter != 0)
                lights[0].gameObject.SetActive(true);

            if (showLightPath)
                tb.gameObject.SetActive(true);

            // we don't want to turn on the colour projectors in edit mode, as we are just projecting white
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
#endif
                if (frameCounter != 0)
                {
                    lights[1].gameObject.SetActive(true);
                    lights[2].gameObject.SetActive(true);
                }
#if UNITY_EDITOR
            }
#endif

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
#endif
                // stuff to do in play mode
                Invoke("UpdateImage", 1f / framerate);
#if UNITY_EDITOR
            }
#endif
        }

        // When disabled, turn lights off
        void OnDisable()
        {
            foreach (Light l in lights)
            {
                l.gameObject.SetActive(false);
            }

            if (showLightPath)
                tb.gameObject.SetActive(false);

            CancelInvoke("UpdateImage");
        }

        private void OnDestroy()
        {
            if (cookie != null)
                cookie.Cleanup();
        }

        // Allow external scripts to pause and play the projector
        public void Pause()
        {
            isPlaying = false;
            CancelInvoke("UpdateImage");
        }
        public void Play()
        {
            isPlaying = true;
            Invoke("UpdateImage", 1f / framerate);
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

        public void UpdateImage()
        {
            cookie.ForceUpdateCookie();
            AssignLightCookies();

            if (framerate > 0 && isPlaying && this.enabled && !IsInvoking("UpdateImage"))
                Invoke("UpdateImage", 1f / framerate);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
#endif
                if (frameCounter == 0) // update the cookie for the first time
                {
                    frameCounter = 1;
                    UpdateImage();
                }
                else if (frameCounter == 1) // turn the spotlights on after cookies are initialised
                {
                    frameCounter = 2;
                    lights[0].gameObject.SetActive(true);
                    lights[1].gameObject.SetActive(true);
                    lights[2].gameObject.SetActive(true);

                }
                else if (framerate < 0 && isPlaying)
                {
                    UpdateImage();
                }
#if UNITY_EDITOR
            }
#endif
        }
    }

}