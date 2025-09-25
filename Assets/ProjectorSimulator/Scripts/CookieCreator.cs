#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Threading;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace ProjectorSimulator
{
    public struct CookieData
    {
        public float shift_v, shift_h, keystone_h, keystone_v, ratio, aspect;
        public CookieData(float shiftV, float shiftH, float keystoneH, float keystoneV, float throwRatio, float imageAspect)
        {
            shift_v = shiftV;
            shift_h = shiftH;
            keystone_h = keystoneH;
            keystone_v = keystoneV;
            ratio = throwRatio;
            aspect = imageAspect;
        }
    }

    public struct VignetteData
    {
        public bool _enabled;
        public float _radius;
        public float _fadeSize;
        public Vector2 _offset;
        public bool _forceCircle;
        public bool _circular;

        public VignetteData(bool enabled, float radius, float fadeSize, Vector2 offset, bool forceCircular, bool circular)
        {
            _enabled = enabled;
            _radius = radius;
            _fadeSize = fadeSize;
            _offset = offset;
            _forceCircle = forceCircular;
            _circular = circular;
        }
    }

    public class Cookie
    {
        RenderTexture projectedImage = null;
        CookieData data;
        float imageWidth, imageHeight;
        float maxImageEdgeDistance;
        Texture redCookie, greenCookie, blueCookie;

        // used for calculating angle
        const float distance = 10.0f; // throw distance
        int textureSize = 1024; // texture width and height

        Light redLight, greenLight, blueLight;

        VignetteData _vignetteData;
        RenderTexture buf2;
        bool buf1 = true;

        // shaders which strip the individual channels, offloading to gpu
        static Material singlePassR, singlePassG, singlePassB;
        Vector2 cookieSpaceScale, cookieSpaceOffset;

        static Material keystoneShader;
        static Material vignetteShader;

        public Cookie(CookieData cookieData, int cookieSize, Light red, Light green, Light blue, VignetteData vignetteData, RenderTexture imageToProject = null)
        {
            textureSize = cookieSize;
            projectedImage = imageToProject;

            CreateTexture();

            redLight = red;
            greenLight = green;
            blueLight = blue;
            data = cookieData;
            _vignetteData = vignetteData;
            Initialise();
        }

        public void Cleanup()
        {
            RenderTexture temp;

            temp = redCookie as RenderTexture;
            if (temp)
            {
                temp.Release();
                temp = greenCookie as RenderTexture;
                temp.Release();
                temp = blueCookie as RenderTexture;
                temp.Release();
            }

            temp = buf2;
            if (temp)
                temp.Release();
        }

        /// <summary>
        /// Creates the Cookie textures
        /// </summary>
        void CreateTexture()
        {
            redCookie = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            greenCookie = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            blueCookie = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            redCookie.wrapMode = greenCookie.wrapMode = blueCookie.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// Calculates light cone angle, draws the first Cookie, assigns cookie to light.
        /// </summary>
        void Initialise()
        {
            // get shaders
            if (singlePassR == null)
            {
                // on first import the shader fails to be found, so check it's here before continuing
                var shader = Shader.Find("Hidden/ProjectorSimSinglePass");
                if (shader == null)
                    return;

                singlePassR = new Material(Shader.Find("Hidden/ProjectorSimSinglePass"));
                singlePassG = new Material(Shader.Find("Hidden/ProjectorSimSinglePass"));
                singlePassB = new Material(Shader.Find("Hidden/ProjectorSimSinglePass"));

                singlePassR.SetInt("_Channel", 0);
                singlePassG.SetInt("_Channel", 1);
                singlePassB.SetInt("_Channel", 2);

                vignetteShader = new Material(Shader.Find("Hidden/ProjectorSimVignette"));
                keystoneShader = new Material(Shader.Find("Hidden/ProjectorSimKeystone"));
            }


            if (projectedImage == null)
            {
                // create white texture
                Texture2D white = new Texture2D(256, 256, TextureFormat.RGBA32, false, false);
                projectedImage = new RenderTexture(256, 256, 24);
                Graphics.Blit(white, projectedImage);
            }

            // calculate angle of light cone from throw ratio and possible lens shift amount
            imageWidth = distance / data.ratio;
            imageHeight = imageWidth / data.aspect;

            // calculate shift **IN METERS**
            float shift_H = imageWidth * (data.shift_h / 200.0f);
            float shift_V = imageHeight * (data.shift_v / 200.0f);

            // calculate how far the image can move with full lens shift applied (in meters from lens centre)
            float imageLimit_h = (imageWidth / 2.0f) + Mathf.Abs(shift_H);
            float imageLimit_v = (imageHeight / 2.0f) + Mathf.Abs(shift_V);
            maxImageEdgeDistance = Mathf.Max(imageLimit_h, imageLimit_v);

            // Calculate the light angle required
            float spotAngle = Mathf.Atan(maxImageEdgeDistance / distance) * 2 * Mathf.Rad2Deg;
            redLight.spotAngle = greenLight.spotAngle = blueLight.spotAngle = spotAngle;

            // calculate extent of spotlight coverage at our arbitrary distance
            float totalHeight = distance * Mathf.Tan((spotAngle / 2) * Mathf.Deg2Rad) * 2;
            float totalWidth = totalHeight; // totalWidth is the same as the light coverage is square

            // scale is how many of this dimension can we fit into the cookie
            cookieSpaceScale = new Vector2(totalWidth / imageWidth, totalHeight / imageHeight);

            // offset is from bottom left corner of cookie, 1 unit = 1 image width/height
            float imageLeftEdge_m = totalWidth / 2 + (shift_H - imageWidth / 2);
            // clamp to allow a 1px border around image (otherwise even with our 1px border that can disappeasr at low resolutions)
            float pixelSize_m = totalWidth / textureSize;
            imageLeftEdge_m = Mathf.Clamp(imageLeftEdge_m, pixelSize_m, totalWidth - imageWidth - pixelSize_m);
            float imageLeftEdge_scale = imageLeftEdge_m / imageWidth;
            float imageBottomEdge_m = totalHeight / 2 + (shift_V - imageHeight / 2);
            imageBottomEdge_m = Mathf.Clamp(imageBottomEdge_m, pixelSize_m, totalHeight - imageHeight - pixelSize_m);
            float imageBottomEdge_scale = imageBottomEdge_m / imageHeight;

            cookieSpaceOffset = new Vector2(-imageLeftEdge_scale, -imageBottomEdge_scale);

            // init buffer2 in case of vignette or keystone
            buf2 = new RenderTexture(projectedImage.width, projectedImage.height, 0, RenderTextureFormat.ARGB32);

            // draw the cookie(s)
            UpdateCookie();
        }

        /// <summary>
        /// Called when image size/pos is changed and the image shape in the cookie will change
        /// </summary>
        /// <param name="cookieData"></param>
        public void Reinitialise(CookieData cookieData, VignetteData vd)
        {
            data = cookieData;
            _vignetteData = vd;
            Initialise();
        }

        public void ForceUpdateCookie()
        {
            UpdateCookie();
        }

        /// <summary>
        /// Calculates where the image will be in the cookie and creates the whole cookie texture
        /// </summary>
        void UpdateCookie()
        {
            // now only shader-based approach

            // safety nets
            if (projectedImage == null) // should never happen, as we create a white image when null is passed in
            {
                Debug.Log("ProjectedImage is null!");
                return;
            }

            // flag whether to write to buf 1 (write to buf 2 if false)
            buf1 = false;

            if (Mathf.Abs(data.keystone_h) >= 0.01 || Mathf.Abs(data.keystone_v) >= 0.01)
            {
                keystoneShader.SetFloat("_keystoneH", data.keystone_h);
                keystoneShader.SetFloat("_keystoneV", data.keystone_v);
                Graphics.Blit(projectedImage, buf2, keystoneShader);
                buf1 = !buf1;
            }

            if (_vignetteData._enabled)
            {
                vignetteShader.SetFloat("_vignetteOffsetX", _vignetteData._offset.x);
                vignetteShader.SetFloat("_vignetteOffsetY", _vignetteData._offset.y);
                vignetteShader.SetFloat("_vignetteRadius", _vignetteData._radius);
                vignetteShader.SetFloat("_vignetteFadeSize", _vignetteData._fadeSize);
                vignetteShader.SetInt("circle", _vignetteData._circular ? 1 : 0);
                vignetteShader.SetFloat("_aspectRatio", _vignetteData._forceCircle ? data.aspect : 1.0f);

                Graphics.Blit(buf1 ? buf2 : projectedImage, buf1 ? projectedImage : buf2, vignetteShader);

                buf1 = !buf1;
            }

            Shader.SetGlobalInt("_PJSimCookieSize", textureSize);
            Shader.SetGlobalVector("_PJSimTransform", new Vector4(cookieSpaceScale.x, cookieSpaceScale.y, cookieSpaceOffset.x, cookieSpaceOffset.y));
            Graphics.Blit(buf1 ? buf2 : projectedImage, redCookie as RenderTexture, singlePassR);
            Graphics.Blit(buf1 ? buf2 : projectedImage, greenCookie as RenderTexture, singlePassG);
            Graphics.Blit(buf1 ? buf2 : projectedImage, blueCookie as RenderTexture, singlePassB);
        }

        /// <summary>
        /// Set the image to project (does NOT cause a redraw - call Reinitialise to redraw)
        /// </summary>
        /// <param name="image"></param>
        /// <param name="pixels"></param>
        public void SetProjectedImage(RenderTexture image)
        {
            projectedImage = image;
        }
        public void RemoveProjectedImage() { projectedImage = null; }

        public void SetCookieSize(int newSize)
        {
            textureSize = newSize;
            CreateTexture();
            Initialise();
        }

        public Texture GetRedCookie() { return redCookie; }
        public Texture GetGreenCookie() { return greenCookie; }
        public Texture GetBlueCookie() { return blueCookie; }
    }
}