using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapController : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private LeapProvider _leapProvider;
    [SerializeField] private float _smoothingSpeed = 10f;

    // Start is called before the first frame update
    void Start()
    {
        if (_leapProvider == null)
        {
            _leapProvider = FindObjectOfType<LeapProvider>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_leapProvider) return;

        Frame frame = _leapProvider.CurrentFrame;
        if (frame.Hands.Count > 0)
        {
            Hand hand = frame.Hands[0];
            if (_container)
            {
                Vector3 handEuler = hand.Rotation.eulerAngles;

                // Convert to -180 to 180 range for easier clamping
                float x = (handEuler.x > 180) ? handEuler.x - 360 : handEuler.x;
                float z = (handEuler.z > 180) ? handEuler.z - 360 : handEuler.z;
                float y = (handEuler.y > 180) ? handEuler.y - 360 : handEuler.y;

                float clampedX = Mathf.Clamp(x, -25f, 25f);
                float clampedZ = Mathf.Clamp(z, -25f, 25f);
                float clampedY = Mathf.Clamp(y, -5f, 5f);

                // Apply clamped X and Z, but reduce the Y rotation influence significantly
                Quaternion targetRotation = Quaternion.Euler(clampedX, clampedY, clampedZ);
                _container.transform.rotation = Quaternion.Slerp(_container.transform.rotation, targetRotation, Time.deltaTime * _smoothingSpeed);
            }
        }
    }
}