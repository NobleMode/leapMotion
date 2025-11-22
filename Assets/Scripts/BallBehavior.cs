using UnityEngine;

public class BallBehavior : MonoBehaviour
{
    private Vector3 _lastSafePosition;
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _lastSafePosition = transform.position;
        // Update safe position every 0.5 seconds
        InvokeRepeating(nameof(UpdateSafePosition), 0.5f, 0.5f);
    }

    void Update()
    {
        // Check if ball fell off
        if (transform.position.y < -15f)
        {
            Respawn();
        }
    }

    void UpdateSafePosition()
    {
        // Only update if we are within reasonable bounds and not falling rapidly
        // Floor is at -0.5, so -2 is a safe lower bound buffer
        if (transform.position.y > -2f && transform.position.y < 10f)
        {
            // Check if we are effectively grounded (not moving down fast)
            if (_rb != null && Mathf.Abs(_rb.velocity.y) < 2f)
            {
                _lastSafePosition = transform.position;
            }
        }
    }

    void Respawn()
    {
        transform.position = _lastSafePosition;
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
}
