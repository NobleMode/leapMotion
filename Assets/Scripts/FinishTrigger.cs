using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object colliding is the player/ball
        // We assume the ball has a Rigidbody or is the only moving thing triggering this
        if (other.GetComponent<Rigidbody>() != null)
        {
            GameController gc = FindObjectOfType<GameController>();
            if (gc != null)
            {
                gc.OnLevelComplete();
            }
        }
    }
}
