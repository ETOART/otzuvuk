using UnityEngine;
using System.Collections;

namespace ProjectorSimulator.DemoScripts
{
    public class Rotator : MonoBehaviour
    {
        public float rotationSpeed = 10f;
        void Start()
        {

        }
        // Update is called once per frame
        void Update()
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}