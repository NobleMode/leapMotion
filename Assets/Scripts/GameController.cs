using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private MapGenerator _mapGenerator;
    [SerializeField] private LeapController _leapController;
    [SerializeField] private Vector2Int _mapSize = new Vector2Int(10, 10);
    
    private GameObject _container;
    
    // Start is called before the first frame update
    void Start()
    {
        _mapGenerator.CreateMap(_mapSize, out _container);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
