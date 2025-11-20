using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using TMPro;

public class LeapController : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private LeapProvider _leapProvider;
    [SerializeField] private float _smoothingSpeed = 10f;
    [SerializeField] private TextMeshProUGUI detectionText;
    [SerializeField] private TextMeshProUGUI fingerText;

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

        // UI Update
        Frame frame = _leapProvider.CurrentFrame;
        string gesture = "None";
        if (frame.Hands.Count > 0)
        {
            Hand hand = frame.Hands[0];
            gesture = DetectGesture(hand);
            
            if (fingerText)
            {
                string fingerStatus = $"Fingers: {hand.fingers.Length}\n";
                fingerStatus += $"Pinky: {hand.GetFinger(Finger.FingerType.PINKY).IsExtended}\n";
                fingerStatus += $"Ring: {hand.GetFinger(Finger.FingerType.RING).IsExtended}\n";
                fingerStatus += $"Middle: {hand.GetFinger(Finger.FingerType.MIDDLE).IsExtended}\n";
                fingerStatus += $"Index: {hand.GetFinger(Finger.FingerType.INDEX).IsExtended}\n";
                fingerStatus += $"Thumb: {hand.GetFinger(Finger.FingerType.THUMB).IsExtended}";
                fingerText.text = fingerStatus;
            }
        }

        if (detectionText)
        {
            detectionText.text = $"Hands Detected: {(frame.Hands.Count > 0 ? "Yes" : "No")}\nGesture: {gesture}";
        }


    }

    internal void UpdateGameplay()
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

    private string DetectGesture(Hand hand)
    {
        // Simple heuristics for gestures
        if (hand.GrabStrength > 0.8f)
        {
            return "Fist";
        }
        else if (hand.PinchStrength > 0.8f)
        {
            return "Pinch";
        }
        else if (hand.GrabStrength < 0.1f)
        {
            return "Open Hand";
        }
        return "Neutral";
    }
}