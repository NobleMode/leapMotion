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

    private Rigidbody _containerRb;

    // Start is called before the first frame update
    private void Start()
    {
        if (_leapProvider == null)
        {
            _leapProvider = FindObjectOfType<LeapProvider>();
        }

        if (_container)
        {
            _containerRb = _container.GetComponent<Rigidbody>();
            if (_containerRb == null)
            {
                _containerRb = _container.AddComponent<Rigidbody>();
            }
            _containerRb.isKinematic = true;
            _containerRb.useGravity = false;
            _containerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    public string CurrentGesture { get; private set; } = "None";
    public bool IsHandDetected { get; private set; } = false;

    // Update is called once per frame
    private void Update()
    {
        if (!_leapProvider) return;

        // UI Update
        Frame frame = _leapProvider.CurrentFrame;
        IsHandDetected = frame.Hands.Count > 0;
        CurrentGesture = "None";

        if (IsHandDetected)
        {
            Hand hand = frame.Hands[0];
            CurrentGesture = DetectGesture(hand);
            
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
            detectionText.text = $"Hands Detected: {(IsHandDetected ? "Yes" : "No")}\nGesture: {CurrentGesture}";
        }
    }

    void FixedUpdate()
    {
        UpdateGameplay();
    }

    internal void UpdateGameplay()
    {
        if (!_leapProvider) return;

        Frame frame = _leapProvider.CurrentFrame;
        if (frame.Hands.Count > 0)
        {
            Hand hand = frame.Hands[0];
            if (_container && _containerRb)
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
                Quaternion nextRotation = Quaternion.Slerp(_containerRb.rotation, targetRotation, Time.fixedDeltaTime * _smoothingSpeed);
                _containerRb.MoveRotation(nextRotation);
            }
        }
    }

    private static string DetectGesture(Hand hand)
    {
        bool thumb = hand.GetFinger(Finger.FingerType.THUMB).IsExtended;
        bool index = hand.GetFinger(Finger.FingerType.INDEX).IsExtended;
        bool middle = hand.GetFinger(Finger.FingerType.MIDDLE).IsExtended;
        bool ring = hand.GetFinger(Finger.FingerType.RING).IsExtended;
        bool pinky = hand.GetFinger(Finger.FingerType.PINKY).IsExtended;

        // Start Game: Thumb + Index only (L-shape)
        if (thumb && index && !middle && !ring && !pinky)
        {
            return "StartGame";
        }
        // Victory/Retry: Index + Middle (V-sign)
        // Allow thumb to be whatever, usually V sign has thumb tucked but sometimes not
        if (index && middle && !ring && !pinky)
        {
            return "VictoryRetry";
        }
        // Exit: Thumb + Middle (Rude? or just specific)
        if (thumb && middle && !index && !ring && !pinky)
        {
            return "Exit";
        }

        // Fallback / Neutral
        if (hand.GrabStrength > 0.8f) return "Fist";
        if (hand.PinchStrength > 0.8f) return "Pinch";
        if (hand.GrabStrength < 0.1f) return "Open Hand";

        return "Neutral";
    }
}