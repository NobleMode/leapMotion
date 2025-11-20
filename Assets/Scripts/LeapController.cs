using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;

public class LeapController : MonoBehaviour
{
    [SerializeField] private GameObject testBlock;
    [SerializeField] private LeapProvider _leapProvider;

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
            if (testBlock)
            {
                testBlock.transform.rotation = hand.Rotation;
            }
        }
    }
}