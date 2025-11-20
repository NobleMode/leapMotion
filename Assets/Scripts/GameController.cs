using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private MapGenerator _mapGenerator;
    [SerializeField] private LeapController _leapController;
    [SerializeField] private Vector2Int _mapSize = new Vector2Int(10, 10);

    private CurrentGameState _currentGameState = CurrentGameState.MAIN;


    
    // Start is called before the first frame update
    void Start()
    {
        _mapGenerator.CreateMap(_mapSize);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public enum CurrentGameState {
    MAIN,
    GAME,
    PAUSE,
    FINISH
}