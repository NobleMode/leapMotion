using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    private int _totalBalls = 1;
    private int _finishedBalls = 0;
    private System.Collections.Generic.HashSet<int> _finishedBallIds = new System.Collections.Generic.HashSet<int>();

    public void Setup(int totalBalls)
    {
        _totalBalls = totalBalls;
        _finishedBalls = 0;
        _finishedBallIds.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object colliding is the player/ball
        if (other.GetComponent<Rigidbody>() != null)
        {
            int id = other.gameObject.GetInstanceID();
            if (!_finishedBallIds.Contains(id))
            {
                _finishedBallIds.Add(id);
                _finishedBalls++;
                
                // Disable the ball to indicate it has finished
                other.gameObject.SetActive(false);

                if (_finishedBalls >= _totalBalls)
                {
                    GameController gc = FindObjectOfType<GameController>();
                    if (gc != null)
                    {
                        gc.OnLevelComplete();
                    }
                }
            }
        }
    }
}
