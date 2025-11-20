using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField][Range(0.5f, 0.95f)] float _mapDistanceModifier = 0.65f; // Map distance modifier
    
    [Header("Object References")]
    [SerializeField] GameObject _wallPrefab; // Wall prefab
    [SerializeField] GameObject _pillarPrefab; // Pillar prefab
    [SerializeField] GameObject _floorObject; // Floor object
    [SerializeField] GameObject _finishObject; // Finish object
    
    [Header("Gameplay Container")]
    [SerializeField] GameObject _container; // Container for the map
    [SerializeField] GameObject _floorContainer; // Container for the floor
    [SerializeField] GameObject _wallsContainer; // Container for the walls
    
    List<GameObject> _walls = new List<GameObject>(); // List of walls
    List<GameObject> _boundaries = new List<GameObject>(); // List of boundaries
    List<GameObject> _pillars = new List<GameObject>(); // List of pillars
    GameObject _ceilingObject; // Ceiling object
    GameObject _endTile; // Finish object
    GameObject _startTile; // Start object
    
    List<Vector3> _debugPathPoints; // Debug path points
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void CreateMap(Vector2Int size, out GameObject container)
    {
        // Clear existing walls
        if (_walls.Count > 0)
        {
            foreach (var wall in _walls)
            {
                Destroy(wall);
            }
            _walls.Clear();
        }

        // Clear existing boundaries
        if (_boundaries.Count > 0)
        {
            foreach (var boundary in _boundaries)
            {
                Destroy(boundary);
            }
            _boundaries.Clear();
        }

        // Clear existing pillars
        if (_pillars.Count > 0)
        {
            foreach (var pillar in _pillars)
            {
                Destroy(pillar);
            }
            _pillars.Clear();
        }
        
        float cellSize = 5f;
        // _floorObject.transform.localScale = new Vector3(size.x * 0.5f, 1, size.y * 0.5f); // Removed single floor scaling

        // Kruskal's Algorithm Implementation
        List<WallEdge> edges = new List<WallEdge>();

        // Generate all potential vertical walls (between columns)
        for (int x = 0; x < size.x - 1; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                float xPos = (x - size.x / 2.0f + 1) * cellSize;
                float zPos = (y - size.y / 2.0f + 0.5f) * cellSize;
                
                int cellA = x + y * size.x;
                int cellB = (x + 1) + y * size.x;

                edges.Add(new WallEdge 
                { 
                    cellA = cellA, 
                    cellB = cellB, 
                    position = new Vector3(xPos, 2.5f, zPos),
                    rotation = Quaternion.Euler(0, 0, 0),
                    scale = new Vector3(1f, 5f, 5f),
                    x = x,
                    y = y,
                    isVertical = true
                });
            }
        }

        // Generate all potential horizontal walls (between rows)
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y - 1; y++)
            {
                float xPos = (x - size.x / 2.0f + 0.5f) * cellSize;
                float zPos = (y - size.y / 2.0f + 1) * cellSize;

                int cellA = x + y * size.x;
                int cellB = x + (y + 1) * size.x;

                edges.Add(new WallEdge 
                { 
                    cellA = cellA, 
                    cellB = cellB, 
                    position = new Vector3(xPos, 2.5f, zPos),
                    rotation = Quaternion.Euler(0, -90, 0),
                    scale = new Vector3(1f, 5f, 5f),
                    x = x,
                    y = y,
                    isVertical = false
                });
            }
        }

        // Shuffle edges
        for (int i = 0; i < edges.Count; i++)
        {
            WallEdge temp = edges[i];
            int randomIndex = Random.Range(i, edges.Count);
            edges[i] = edges[randomIndex];
            edges[randomIndex] = temp;
        }

        // Process edges
        DisjointSet ds = new DisjointSet(size.x * size.y);
        bool[,] vWalls = new bool[size.x - 1, size.y]; // Tracks vertical walls
        bool[,] hWalls = new bool[size.x, size.y - 1]; // Tracks horizontal walls
        
        // Adjacency list for the maze graph
        List<int>[] adj = new List<int>[size.x * size.y];
        for (int i = 0; i < adj.Length; i++) adj[i] = new List<int>();

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
                GameObject wall = Instantiate(_wallPrefab, _wallsContainer.transform);
                wall.transform.localPosition = edge.position;
                wall.transform.localRotation = edge.rotation;
                wall.transform.localScale = edge.scale;
                _walls.Add(wall);

                if (edge.isVertical)
                    vWalls[edge.x, edge.y] = true;
                else
                    hWalls[edge.x, edge.y] = true;
            }
        }

        // Create Boundary Walls
        // Left Wall
        GameObject leftWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        leftWall.transform.localPosition = new Vector3(-size.x * cellSize / 2.0f, 2.5f, 0);
        leftWall.transform.localRotation = Quaternion.Euler(0, 0, 0);
        leftWall.transform.localScale = new Vector3(1f, 5f, size.y * cellSize);
        _boundaries.Add(leftWall);

        // Right Wall
        GameObject rightWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        rightWall.transform.localPosition = new Vector3(size.x * cellSize / 2.0f, 2.5f, 0);
        rightWall.transform.localRotation = Quaternion.Euler(0, 0, 0);
        rightWall.transform.localScale = new Vector3(1f, 5f, size.y * cellSize);
        _boundaries.Add(rightWall);

        // Top Wall
        GameObject topWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        topWall.transform.localPosition = new Vector3(0, 2.5f, size.y * cellSize / 2.0f);
        topWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
        topWall.transform.localScale = new Vector3(1f, 5f, size.x * cellSize);
        _boundaries.Add(topWall);

        // Bottom Wall
        GameObject bottomWall = Instantiate(_wallPrefab, _wallsContainer.transform);
        bottomWall.transform.localPosition = new Vector3(0, 2.5f, -size.y * cellSize / 2.0f);
        bottomWall.transform.localRotation = Quaternion.Euler(0, 90, 0);
        bottomWall.transform.localScale = new Vector3(1f, 5f, size.x * cellSize);
        _boundaries.Add(bottomWall);
        
        container = _container;

        // Generate Pillars
        for (int x = 0; x <= size.x; x++)
        {
            for (int y = 0; y <= size.y; y++)
            {
                bool hasUp = false;
                bool hasDown = false;
                bool hasLeft = false;
                bool hasRight = false;

                // Check Up (+Z)
                if (y < size.y)
                {
                    if (x == 0 || x == size.x) hasUp = true; // Boundary
                    else if (vWalls[x - 1, y]) hasUp = true; // Internal
                }

                // Check Down (-Z)
                if (y > 0)
                {
                    if (x == 0 || x == size.x) hasDown = true; // Boundary
                    else if (vWalls[x - 1, y - 1]) hasDown = true; // Internal
                }

                // Check Right (+X)
                if (x < size.x)
                {
                    if (y == 0 || y == size.y) hasRight = true; // Boundary
                    else if (hWalls[x, y - 1]) hasRight = true; // Internal
                }

                // Check Left (-X)
                if (x > 0)
                {
                    if (y == 0 || y == size.y) hasLeft = true; // Boundary
                    else if (hWalls[x - 1, y - 1]) hasLeft = true; // Internal
                }

                int count = (hasUp ? 1 : 0) + (hasDown ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

                bool placePillar = false;
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
                    GameObject pillar = Instantiate(_pillarPrefab, _container.transform);
                    float px = (x - size.x / 2.0f) * cellSize;
                    float pz = (y - size.y / 2.0f) * cellSize;
                    pillar.transform.localPosition = new Vector3(px, 2.5f, pz);
                    pillar.transform.localRotation = Quaternion.identity;
                    pillar.transform.localScale = new Vector3(1.15f, 5.15f, 1.15f);
                    _pillars.Add(pillar);
                }
            }
        }

        // Generate Floor Tiles
        GameObject[] tiles = new GameObject[size.x * size.y];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                GameObject tile = Instantiate(_floorObject, _floorContainer.transform);
                float px = (x - size.x / 2.0f + 0.5f) * cellSize;
                float pz = (y - size.y / 2.0f + 0.5f) * cellSize;
                tile.transform.localPosition = new Vector3(px, 0, pz);
                tile.transform.localScale = new Vector3(1f / 2, 1, 1f / 2);
                
                tiles[x + y * size.x] = tile;
            }
        }

        // Randomize Start and Finish
        // 1. Select a random corner
        // 0: Bottom-Left, 1: Bottom-Right, 2: Top-Left, 3: Top-Right
        int corner = Random.Range(0, 4);
        int startX = 0, startY = 0;
        
        int range = 4; // 4x4 area
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
        int startNode = startX + startY * size.x;

        // 2. Calculate distances from startNode to all other nodes
        int[] distances = GetDistances(startNode, adj, size.x * size.y);
        
        // 3. Find max distance
        int maxDist = 0;
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] > maxDist) maxDist = distances[i];
        }

        // 4. Filter nodes with distance > 0.5 * maxDist
        List<int> possibleEndNodes = new List<int>();
        int minEndDist = (int)(maxDist * _mapDistanceModifier);
        for (int i = 0; i < distances.Length; i++)
        {
            if (distances[i] >= minEndDist)
            {
                possibleEndNodes.Add(i);
            }
        }

        // 5. Select random end node
        int endNode = possibleEndNodes[Random.Range(0, possibleEndNodes.Count)];

        // Set Start
        _startTile = tiles[startNode];
        
        // Set Finish
        _endTile = tiles[endNode];
        
        // Optional: Place visual finish object
        Vector3 finishPos = GetCellPosition(endNode, size, cellSize);
        GameObject finishObj = Instantiate(_finishObject, _container.transform);
        finishObj.transform.localPosition = finishPos;
        
        // Calculate Path for Debugging
        _debugPathPoints = GetPath(startNode, endNode, adj, size, cellSize);
    }

    private int[] GetDistances(int startNode, List<int>[] adj, int totalNodes)
    {
        int[] dist = new int[totalNodes];
        for (int i = 0; i < totalNodes; i++) dist[i] = -1;
        
        Queue<int> q = new Queue<int>();
        q.Enqueue(startNode);
        dist[startNode] = 0;

        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (int v in adj[u])
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
        int totalNodes = size.x * size.y;
        int[] parent = new int[totalNodes];
        bool[] visited = new bool[totalNodes];
        for (int i = 0; i < totalNodes; i++) parent[i] = -1;

        Queue<int> q = new Queue<int>();
        q.Enqueue(startNode);
        visited[startNode] = true;

        bool found = false;
        while (q.Count > 0)
        {
            int u = q.Dequeue();
            if (u == endNode)
            {
                found = true;
                break;
            }

            foreach (int v in adj[u])
            {
                if (!visited[v])
                {
                    visited[v] = true;
                    parent[v] = u;
                    q.Enqueue(v);
                }
            }
        }

        List<Vector3> path = new List<Vector3>();
        if (found)
        {
            int curr = endNode;
            while (curr != -1)
            {
                Vector3 pos = GetCellPosition(curr, size, cellSize);
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
        int x = cellIndex % size.x;
        int y = cellIndex / size.x;
        float px = (x - size.x / 2.0f + 0.5f) * cellSize;
        float pz = (y - size.y / 2.0f + 0.5f) * cellSize;
        return new Vector3(px, 0, pz);
    }

    private void OnDrawGizmos()
    {
        if (_debugPathPoints != null && _debugPathPoints.Count > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < _debugPathPoints.Count - 1; i++)
            {
                // Transform local points to world space if container moves, 
                // but here we calculated local positions relative to container (mostly).
                // Wait, GetCellPosition returns local coordinates relative to container center?
                // Actually GetCellPosition returns coordinates centered around (0,0,0).
                // If _container is at (0,0,0), then localPosition == worldPosition.
                // To be safe, let's assume we need to transform them if container moves.
                // However, the request implies simple visualization.
                // Let's use _container.transform.TransformPoint if _container is available.
                
                Vector3 p1 = _debugPathPoints[i];
                Vector3 p2 = _debugPathPoints[i + 1];
                
                if (_container != null)
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
        private int[] parent;

        public DisjointSet(int count)
        {
            parent = new int[count];
            for (int i = 0; i < count; i++)
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
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ)
                parent[rootI] = rootJ;
        }
    }
}
