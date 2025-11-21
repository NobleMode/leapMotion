using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField][Range(0.5f, 0.95f)] float _mapDistanceModifier = 0.65f; // Map distance modifier
    [SerializeField] float _cameraPadding = 1.75f; // Padding for camera view
    
    [Header("Object References")]
    [SerializeField] GameObject _wallPrefab; // Wall prefab
    [SerializeField] GameObject _ceilingObject;
    [SerializeField] GameObject _pillarPrefab; // Pillar prefab
    [SerializeField] GameObject _floorObject; // Floor object
    [SerializeField] GameObject _finishObject; // Finish object
    [SerializeField] Camera _camera; 
    [SerializeField] GameObject _ballsObject; // Player ball

    [Header("Gameplay Container")]
    [SerializeField] GameObject _container; // Container for the map
    [SerializeField] GameObject _floorContainer; // Container for the floor
    [SerializeField] GameObject _wallsContainer; // Container for the walls
    
    List<GameObject> _walls = new List<GameObject>(); // List of walls
    List<GameObject> _boundaries = new List<GameObject>(); // List of boundaries
    List<GameObject> _pillars = new List<GameObject>(); // List of pillars
    GameObject _ceiling; // Ceiling object
    GameObject _endTile; // Finish object
    GameObject _startTile; // Start object
    GameObject _balls;

    
    List<Vector3> _debugPathPoints; // Debug path points

    public void ClearMap() {
        foreach (var wall in _walls)
        {
            Destroy(wall);
        }
        _walls.Clear();

        foreach (var boundary in _boundaries)
        {
            Destroy(boundary);
        }
        _boundaries.Clear();

        foreach (var pillar in _pillars)
        {
            Destroy(pillar);
        }
        _pillars.Clear();

        Destroy(_ceiling);
        _ceiling = null;

        Destroy(_endTile);
        _endTile = null;

        Destroy(_startTile);
        _startTile = null;

        Destroy(_balls);
        _balls = null;

        _container.SetActive(false);
    }
    
    public void CreateMap(Vector2Int size)
    {
        ClearMap();
        
        _container.SetActive(true);
        
        var cellSize = 5f;
        // _floorObject.transform.localScale = new Vector3(size.x * 0.5f, 1, size.y * 0.5f); // Removed single floor scaling

        // Kruskal's Algorithm Implementation
        var edges = new List<WallEdge>();

        // Generate all potential vertical walls (between columns)
        for (var x = 0; x < size.x - 1; x++)
        {
            for (var y = 0; y < size.y; y++)
            {
                var xPos = (x - size.x / 2.0f + 1) * cellSize;
                var zPos = (y - size.y / 2.0f + 0.5f) * cellSize;
                
                var cellA = x + y * size.x;
                var cellB = (x + 1) + y * size.x;

                edges.Add(new WallEdge 
                { 
                    cellA = cellA, 
                    cellB = cellB, 
                    position = new Vector3(xPos, 2.5f, zPos),
                    rotation = Quaternion.Euler(0, Random.Range(0,1000) % 2 == 0 ? 0 : 180, 0),
                    scale = new Vector3(1f, 5f, 5f),
                    x = x,
                    y = y,
                    isVertical = true
                });
            }
        }

        // Generate all potential horizontal walls (between rows)
        for (var x = 0; x < size.x; x++)
        {
            for (var y = 0; y < size.y - 1; y++)
            {
                var xPos = (x - size.x / 2.0f + 0.5f) * cellSize;
                var zPos = (y - size.y / 2.0f + 1) * cellSize;

                var cellA = x + y * size.x;
                var cellB = x + (y + 1) * size.x;

                edges.Add(new WallEdge 
                { 
                    cellA = cellA, 
                    cellB = cellB, 
                    position = new Vector3(xPos, 2.5f, zPos),
                    rotation = Quaternion.Euler(0, Random.Range(0,1000) % 2 == 0 ? -90 : 90, 0),
                    scale = new Vector3(1f, 5f, 5f),
                    x = x,
                    y = y,
                    isVertical = false
                });
            }
        }

        // Shuffle edges
        for (var i = 0; i < edges.Count; i++)
        {
            var temp = edges[i];
            var randomIndex = Random.Range(i, edges.Count);
            edges[i] = edges[randomIndex];
            edges[randomIndex] = temp;
        }

        // Process edges
        var ds = new DisjointSet(size.x * size.y);
        var vWalls = new bool[size.x - 1][]; // Tracks vertical walls
        for (var index = 0; index < size.x - 1; index++)
        {
            vWalls[index] = new bool[size.y];
        }

        var hWalls = new bool[size.x][]; // Tracks horizontal walls
        for (var index = 0; index < size.x; index++)
        {
            hWalls[index] = new bool[size.y - 1];
        }

        // Adjacency list for the maze graph
        var adj = new List<int>[size.x * size.y];
        for (var i = 0; i < adj.Length; i++) adj[i] = new List<int>();

        foreach (var edge in edges)
        {
            if (ds.Find(edge.cellA) != ds.Find(edge.cellB))
            {
                // If cells are not connected, connect them (remove wall / do not create wall)
                ds.Union(edge.cellA, edge.cellB);
                
                // Add connection to graph
                adj[edge.cellA].Add(edge.cellB);
                adj[edge.cellB].Add(edge.cellA);
            }
            else
            {
                // If cells are already connected, create the wall
                var wall = Instantiate(_wallPrefab, _wallsContainer.transform);
                wall.transform.localPosition = edge.position;
                wall.transform.localRotation = edge.rotation;
                wall.transform.localScale = edge.scale;
                _walls.Add(wall);

                if (edge.isVertical)
                    vWalls[edge.x][edge.y] = true;
                else
                    hWalls[edge.x][edge.y] = true;
            }
        }

        // Create Boundary Walls
        // Left Wall
        var leftWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        leftWall.transform.localPosition = new Vector3(-size.x * cellSize / 2.0f, 2.5f, 0);
        leftWall.transform.localRotation = Quaternion.Euler(0, 0, 0);
        leftWall.transform.localScale = new Vector3(1f, 5f, size.y * cellSize);
        _boundaries.Add(leftWall);

        // Right Wall
        var rightWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        rightWall.transform.localPosition = new Vector3(size.x * cellSize / 2.0f, 2.5f, 0);
        rightWall.transform.localRotation = Quaternion.Euler(0, 0, 0);
        rightWall.transform.localScale = new Vector3(1f, 5f, size.y * cellSize);
        _boundaries.Add(rightWall);

        // Top Wall
        var topWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        topWall.transform.localPosition = new Vector3(0, 2.5f, size.y * cellSize / 2.0f);
        topWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
        topWall.transform.localScale = new Vector3(1f, 5f, size.x * cellSize);
        _boundaries.Add(topWall);

        // Bottom Wall
        var bottomWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        bottomWall.transform.localPosition = new Vector3(0, 2.5f, -size.y * cellSize / 2.0f);
        bottomWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
        bottomWall.transform.localScale = new Vector3(1f, 5f, size.x * cellSize);
        _boundaries.Add(bottomWall);
        
        // Generate Pillars
        for (var x = 0; x <= size.x; x++)
        {
            for (var y = 0; y <= size.y; y++)
            {
                var hasUp = false;
                var hasDown = false;
                var hasLeft = false;
                var hasRight = false;

                // Check Up (+Z)
                if (y < size.y)
                {
                    if (x == 0 || x == size.x) hasUp = true; // Boundary
                    else if (vWalls[x - 1][y]) hasUp = true; // Internal
                }

                // Check Down (-Z)
                if (y > 0)
                {
                    if (x == 0 || x == size.x) hasDown = true; // Boundary
                    else if (vWalls[x - 1][y - 1]) hasDown = true; // Internal
                }

                // Check Right (+X)
                if (x < size.x)
                {
                    if (y == 0 || y == size.y) hasRight = true; // Boundary
                    else if (hWalls[x][y - 1]) hasRight = true; // Internal
                }

                // Check Left (-X)
                if (x > 0)
                {
                    if (y == 0 || y == size.y) hasLeft = true; // Boundary
                    else if (hWalls[x - 1][y - 1]) hasLeft = true; // Internal
                }

                var count = (hasUp ? 1 : 0) + (hasDown ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

                var placePillar = false;
                if (count > 0)
                {
                    if (count == 2)
                    {
                        // Check for straight lines
                        if (hasUp && hasDown) placePillar = false;
                        else if (hasLeft && hasRight) placePillar = false;
                        else placePillar = true; // Corner
                    }
                    else
                    {
                        placePillar = true; // End point (1) or Junction (3, 4)
                    }
                }

                if (placePillar)
                {
                    var pillar = Instantiate(_pillarPrefab, _container.transform);
                    var px = (x - size.x / 2.0f) * cellSize;
                    var pz = (y - size.y / 2.0f) * cellSize;
                    pillar.transform.localPosition = new Vector3(px, 2.5f, pz);
                    var randomYRotation = new float[] { 0f, 90f, 180f, 270f }[UnityEngine.Random.Range(0, 4)];
                    pillar.transform.localRotation = Quaternion.Euler(0, randomYRotation, 0);
                    pillar.transform.localScale = new Vector3(1.15f, 5.15f, 1.15f);
                    _pillars.Add(pillar);
                }
            }
        }



        // Generate Ceiling
        if (_ceilingObject)
        {
            _ceiling = Instantiate(_ceilingObject, _container.transform);
            _ceiling.transform.localPosition = new Vector3(0, 6.5f, 0);
            _ceiling.transform.localRotation = Quaternion.identity;
            // Scale to cover the whole board. 
            // The board size is size.x * cellSize by size.y * cellSize.
            // Assuming the ceiling prefab is a 10x10 plane (standard Unity plane), we need to scale it by 0.1 * dimensions.
            _ceiling.transform.localScale = new Vector3(size.x * cellSize / 10f, 1, size.y * cellSize / 10f);
        }

        // Generate Floor Tiles
        var tiles = new GameObject[size.x * size.y];
        for (var x = 0; x < size.x; x++)
        {
            for (var y = 0; y < size.y; y++)
            {
                var tile = Instantiate(_floorObject, _floorContainer.transform);
                var px = (x - size.x / 2.0f + 0.5f) * cellSize;
                var pz = (y - size.y / 2.0f + 0.5f) * cellSize;
                tile.transform.localPosition = new Vector3(px, 0, pz);
                tile.transform.localScale = new Vector3(1f / 2, 1, 1f / 2);
                
                tiles[x + y * size.x] = tile;
            }
        }

        // Randomize Start and Finish
        // 1. Select a random corner
        // 0: Bottom-Left, 1: Bottom-Right, 2: Top-Left, 3: Top-Right
        var corner = Random.Range(0, 4);
        int startX = 0, startY = 0;
        
        var range = 4; // 4x4 area
        int minX = 0, maxX = range;
        int minY = 0, maxY = range;

        switch (corner)
        {
            case 0: // Bottom-Left
                minX = 0; maxX = Mathf.Min(range, size.x);
                minY = 0; maxY = Mathf.Min(range, size.y);
                break;
            case 1: // Bottom-Right
                minX = Mathf.Max(0, size.x - range); maxX = size.x;
                minY = 0; maxY = Mathf.Min(range, size.y);
                break;
            case 2: // Top-Left
                minX = 0; maxX = Mathf.Min(range, size.x);
                minY = Mathf.Max(0, size.y - range); maxY = size.y;
                break;
            case 3: // Top-Right
                minX = Mathf.Max(0, size.x - range); maxX = size.x;
                minY = Mathf.Max(0, size.y - range); maxY = size.y;
                break;
        }

        startX = Random.Range(minX, maxX);
        startY = Random.Range(minY, maxY);
        var startNode = startX + startY * size.x;

        // 2. Calculate distances from startNode to all other nodes
        var distances = GetDistances(startNode, adj, size.x * size.y);
        
        // 3. Find max distance
        var maxDist = 0;
        for (var i = 0; i < distances.Length; i++)
        {
            if (distances[i] > maxDist) maxDist = distances[i];
        }

        // 4. Filter nodes with distance > 0.5 * maxDist
        var possibleEndNodes = new List<int>();
        var minEndDist = (int)(maxDist * _mapDistanceModifier);
        for (var i = 0; i < distances.Length; i++)
        {
            if (distances[i] >= minEndDist)
            {
                possibleEndNodes.Add(i);
            }
        }

        // 5. Select random end node
        var endNode = possibleEndNodes[Random.Range(0, possibleEndNodes.Count)];

        // Set Start
        _startTile = tiles[startNode];
        
        // Set Finish
        _endTile = tiles[endNode];

        // Replace _endTile with _finishObject
        if (_finishObject)
        {
            var endTilePosition = _endTile.transform.position;
            var endTileScale = _endTile.transform.localScale;
            Destroy(_endTile); // Destroy the original tile
            _endTile = Instantiate(_finishObject, endTilePosition, Quaternion.identity, _floorContainer.transform);
            _endTile.transform.localRotation = Quaternion.identity;
            _endTile.transform.localScale = endTileScale;
        }
        
        
        // Add Finish Trigger
        _endTile.AddComponent<FinishTrigger>();
        var trigger = _endTile.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1, 5, 1); // Tall trigger
        trigger.center = new Vector3(0, 2.5f, 0);
        
        // Calculate Path for Debugging
        _debugPathPoints = GetPath(startNode, endNode, adj, size, cellSize);
        
        _balls = Instantiate(_ballsObject, _startTile.transform.position + new Vector3(0, 2.5f, 0), Quaternion.identity);

        // Adjust Camera Height
        if (_camera)
        {
            float mapWidth = size.x * cellSize;
            float mapHeight = size.y * cellSize; // Z dimension

            float vertFOV = _camera.fieldOfView;
            float aspect = _camera.aspect;

            // Distance required to fit height
            float distV = (mapHeight / 2.0f) / Mathf.Tan(vertFOV * 0.5f * Mathf.Deg2Rad);
            
            // Distance required to fit width
            // tan(horzFOV/2) = aspect * tan(vertFOV/2)
            // dist = (width/2) / tan(horzFOV/2) = (width/2) / (aspect * tan(vertFOV/2))
            float distH = (mapWidth / 2.0f) / (aspect * Mathf.Tan(vertFOV * 0.5f * Mathf.Deg2Rad));

            float requiredDist = Mathf.Max(distV, distH);
            
            // Apply padding
            requiredDist += _cameraPadding * 6f;

            // Set camera position (centered at 0,0,0)
            _camera.transform.position = new Vector3(0, requiredDist, 0);
        }
    }

    private int[] GetDistances(int startNode, List<int>[] adj, int totalNodes)
    {
        var dist = new int[totalNodes];
        for (var i = 0; i < totalNodes; i++) dist[i] = -1;
        
        var q = new Queue<int>();
        q.Enqueue(startNode);
        dist[startNode] = 0;

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            foreach (var v in adj[u])
            {
                if (dist[v] == -1)
                {
                    dist[v] = dist[u] + 1;
                    q.Enqueue(v);
                }
            }
        }
        return dist;
    }

    private List<Vector3> GetPath(int startNode, int endNode, List<int>[] adj, Vector2Int size, float cellSize)
    {
        var totalNodes = size.x * size.y;
        var parent = new int[totalNodes];
        var visited = new bool[totalNodes];
        for (var i = 0; i < totalNodes; i++) parent[i] = -1;

        var q = new Queue<int>();
        q.Enqueue(startNode);
        visited[startNode] = true;

        var found = false;
        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (u == endNode)
            {
                found = true;
                break;
            }

            foreach (var v in adj[u])
            {
                if (!visited[v])
                {
                    visited[v] = true;
                    parent[v] = u;
                    q.Enqueue(v);
                }
            }
        }

        var path = new List<Vector3>();
        if (found)
        {
            var curr = endNode;
            while (curr != -1)
            {
                var pos = GetCellPosition(curr, size, cellSize);
                pos.y = 3.5f; // Set Y to 3.5 as requested
                path.Add(pos);
                curr = parent[curr];
            }
            path.Reverse(); // Start to End
        }
        return path;
    }

    private Vector3 GetCellPosition(int cellIndex, Vector2Int size, float cellSize)
    {
        var x = cellIndex % size.x;
        var y = cellIndex / size.x;
        var px = (x - size.x / 2.0f + 0.5f) * cellSize;
        var pz = (y - size.y / 2.0f + 0.5f) * cellSize;
        return new Vector3(px, 0, pz);
    }

    private void OnDrawGizmos()
    {
        if (_debugPathPoints != null && _debugPathPoints.Count > 1)
        {
            Gizmos.color = Color.red;
            for (var i = 0; i < _debugPathPoints.Count - 1; i++)
            {
                // Transform local points to world space if container moves, 
                // but here we calculated local positions relative to container (mostly).
                // Wait, GetCellPosition returns local coordinates relative to container center?
                // Actually GetCellPosition returns coordinates centered around (0,0,0).
                // If _container is at (0,0,0), then localPosition == worldPosition.
                // To be safe, let's assume we need to transform them if container moves.
                // However, the request implies simple visualization.
                // Let's use _container.transform.TransformPoint if _container is available.
                
                var p1 = _debugPathPoints[i];
                var p2 = _debugPathPoints[i + 1];
                
                if (_container)
                {
                    p1 = _container.transform.TransformPoint(p1);
                    p2 = _container.transform.TransformPoint(p2);
                }

                Gizmos.DrawLine(p1, p2);
            }
        }
    }

    private struct WallEdge
    {
        public int cellA;
        public int cellB;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int x;
        public int y;
        public bool isVertical;
    }

    private class DisjointSet
    {
        private readonly int[] parent;

        public DisjointSet(int count)
        {
            parent = new int[count];
            for (var i = 0; i < count; i++)
                parent[i] = i;
        }

        public int Find(int i)
        {
            if (parent[i] == i)
                return i;
            return parent[i] = Find(parent[i]);
        }

        public void Union(int i, int j)
        {
            var rootI = Find(i);
            var rootJ = Find(j);
            if (rootI != rootJ)
                parent[rootI] = rootJ;
        }
    }
}
