using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectorSimulator.DemoScripts
{
    public class ProjectorControl : MonoBehaviour
    {
        ProjectorSimulator.ProjectorSim projector;

        private void Awake()
        {
            projector = GetComponent<ProjectorSimulator.ProjectorSim>();
        }

        // Update is called once per frame
        void Update()
        {
            // SELECT SLIDE WITH NUMBERS
            if (Input.GetKeyDown(KeyCode.Alpha1))
                projector.SetSlideshowIndex(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                projector.SetSlideshowIndex(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                projector.SetSlideshowIndex(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                projector.SetSlideshowIndex(3);
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                projector.SetSlideshowIndex(4);
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                projector.SetSlideshowIndex(5);
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                projector.SetSlideshowIndex(6);
            else if (Input.GetKeyDown(KeyCode.Alpha8))
                projector.SetSlideshowIndex(7);
            else if (Input.GetKeyDown(KeyCode.Alpha9)) // returns warning as there are only 8 images in the test scene
                projector.SetSlideshowIndex(8);

            // TURN PROJECTOR ON/OFF
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (projector.enabled)
                    projector.enabled = false;
                else
                    projector.enabled = true;
            }

            // PLAY/PAUSE SLIDESHOW
            if (Input.GetKeyDown(KeyCode.P))
            {
                projector.TogglePause();
            }

            // NEXT SLIDE
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                projector.AdvanceSlideshow();
            }
        }
    }
}