using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ProjectorSimulator
{
    [CustomEditor(typeof(ProjectorSim))]
    public class Projector_Inspector : Editor
    {
        public VisualTreeAsset inspectorXML;

        VisualElement pnlImages, pnlRT, pnlVideo;

        static ProjectorSim lastSelectedProjector = null;

        // from https://stackoverflow.com/questions/76198965/how-to-add-an-array-of-gameobjects-to-an-editor-window-in-the-new-unity-ui-toolk
        public void DrawReorderableList<T>(List<T> sourceList, VisualElement rootVisualElement, bool allowSceneObjects = true) where T : UnityEngine.Object
        {
            var list = new ListView(sourceList)
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showFoldoutHeader = true,
                headerTitle = "Images",
                showAddRemoveFooter = true,
                reorderMode = ListViewReorderMode.Animated,
                makeItem = () => new ObjectField
                {
                    objectType = typeof(T),
                    allowSceneObjects = allowSceneObjects
                },
                bindItem = (element, i) =>
                {
                    ((ObjectField)element).value = sourceList[i];
                    ((ObjectField)element).RegisterValueChangedCallback((value) =>
                    {
                        sourceList[i] = (T)value.newValue;
                        UpdateImage();
                        EditorUtility.SetDirty(GetSelectedProjector());
                    });
                }
            };

            rootVisualElement.Add(list);
        }

        public override VisualElement CreateInspectorGUI()
        {
            // load UI from UXML file
            VisualElement myInspector = inspectorXML.Instantiate();

            // get selected projector reference
            ProjectorSim pj = GetSelectedProjector();

            // in some cases, the inspector is drawn when no object is selected. In this case, display an error message.
            if (pj == null)
            {
                VisualElement root = new VisualElement();
                Label l = new Label("Error: no ProjectorSim object selected");
                root.Add(l);
                return root;
            }

            PropertyField pf;
            Slider s;
            VisualElement ve;

            pf = myInspector.Q("AspectRatio") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            pf = myInspector.Q("ThrowRatio") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            pf = myInspector.Q("LensShiftH") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            pf = myInspector.Q("LensShiftV") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            s = myInspector.Q("KeystoneH") as Slider;
            s.RegisterValueChangedCallback(evt => UpdateImage());

            s = myInspector.Q("KeystoneV") as Slider;
            s.RegisterValueChangedCallback(evt => UpdateImage());

            pf = myInspector.Q("Intensity") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateSpotlight());
            
            pf = myInspector.Q("Range") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateSpotlight());

            pf = myInspector.Q("ContentType") as PropertyField;
            pf.RegisterValueChangeCallback(evt => RedrawInspector());

            pnlImages = myInspector.Q("PnlImages");
            pnlImages.style.display = pj.contentType == ProjectorSim.ContentType.Images ? DisplayStyle.Flex : DisplayStyle.None;

            ve = myInspector.Q("ImagesParent");
            DrawReorderableList(pj.images, ve, false);

            pf = myInspector.Q("Images") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());
            

            pf = myInspector.Q("PreviewImageIndex") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            pf = myInspector.Q("ImageInterval") as PropertyField;
            pf = myInspector.Q("Loop") as PropertyField;

            pnlRT = myInspector.Q("PnlRT");
            pnlRT.style.display = pj.contentType == ProjectorSim.ContentType.RT ? DisplayStyle.Flex : DisplayStyle.None;

            pf = myInspector.Q("RenderTexture") as PropertyField;
            pf = myInspector.Q("Framerate") as PropertyField;

            pnlVideo = myInspector.Q("PnlVideo");
            pnlVideo.style.display = pj.contentType == ProjectorSim.ContentType.Video ? DisplayStyle.Flex : DisplayStyle.None;

            pf = myInspector.Q("PlayOnAwake") as PropertyField;
            pf = myInspector.Q("CookieSize") as PropertyField;

            pf = myInspector.Q("UseVignette") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            pf = myInspector.Q("VignetteShape") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            s = myInspector.Q("VignetteSize") as Slider;
            s.RegisterValueChangedCallback(evt => UpdateImage());

            s = myInspector.Q("VignetteFade") as Slider;
            s.RegisterValueChangedCallback(evt => UpdateImage());

            s = myInspector.Q("VignetteOffsetX") as Slider;
            s.RegisterValueChangedCallback(evt => UpdateImage());

            s = myInspector.Q("VignetteOffsetY") as Slider;
            s.RegisterValueChangedCallback(evt => UpdateImage());

            pf = myInspector.Q("ForceCircular") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateImage());

            pf = myInspector.Q("ShowLightPath") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateLightpath());

            pf = myInspector.Q("LightPathMaterial") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateLightpath());

            pf = myInspector.Q("LightPathRange") as PropertyField;
            pf.RegisterValueChangeCallback(evt => UpdateLightpath());

            // Return the finished Inspector UI.
            return myInspector;
        }

        static ProjectorSim GetSelectedProjector()
        {
            ProjectorSim result = null;
            GameObject selected = Selection.activeGameObject;
            if (selected)
            {
                result = selected.GetComponent<ProjectorSim>();
                lastSelectedProjector = result;
            }
            else
            {
                result = lastSelectedProjector; // in case of locked inspector and selecting several images in the Project tab to drag into inspector
            }
            return result;
        }

        void UpdateImage()
        {
            ProjectorSim pj = GetSelectedProjector();

            if (pj == null)
                return;

            //pj.UpdateImage();
            pj.MakeDirty();
        }

        void UpdateSpotlight()
        {
            var pj = GetSelectedProjector();

            if (pj == null || pj.gameObject.scene.IsValid() == false)
                return;

            pj.UpdateSpotlight();
        }

        void UpdateLightpath()
        {
            var pj = GetSelectedProjector();
            
            if (pj == null || pj.gameObject.scene.IsValid() == false)
                return;

            pj.UpdateLightpath();
        }

        void RedrawInspector() // called when content type changes and we need to display different options
        {
            ProjectorSim pj = GetSelectedProjector();

            if (pj == null)
                return;

            pnlImages.style.display = pj.contentType == ProjectorSim.ContentType.Images ? DisplayStyle.Flex : DisplayStyle.None;
            pnlRT.style.display     = pj.contentType == ProjectorSim.ContentType.RT     ? DisplayStyle.Flex : DisplayStyle.None;
            pnlVideo.style.display  = pj.contentType == ProjectorSim.ContentType.Video  ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += UndoCallback;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoCallback;
        }

        void UndoCallback()
        {
            var pj = GetSelectedProjector();

            pj.UpdateImage();
            pj.UpdateSpotlight();

            if (pj != lastSelectedProjector && lastSelectedProjector != null)
            {
                lastSelectedProjector.UpdateImage();
                lastSelectedProjector.UpdateSpotlight();
            }
        }
    }
}