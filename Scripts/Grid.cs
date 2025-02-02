// Adeer Khan 20244046
// Followed this tutorial: how to make a procedural grid world in under 2 minutes in unity (part 1)(https://www.youtube.com/watch?v=DBjd7NHMgOE&t=4s)
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using Pathfinding;

public class Grid : MonoBehaviour
{
    [Header("Camp Settings")]
    public GameObject campPrefab; // Reference to the camp prefab
    public int maxCampSpawnAttempts = 100; // Maximum attempts to find a valid position

    [Header("Tree Settings")]
    public GameObject[] treePrefabs;
    public float treesNoiseScale = .05f;
    public float treeDensity = .5f;

    [Header("Nature Prefabs")]
    public GameObject[] grassPrefabs;  // Array for grass types
    public GameObject[] flowerPrefabs; // Array for flowers
    public GameObject[] smallPlantPrefabs; // Array for small plants
    public GameObject[] rockPrefabs; // Array for rocks

    [Header("Spawn Controls")]
    [Range(0f, 10f)] public float grassSpawnMultiplier = 1f; 
    [Range(0f, 1f)] public float flowerSpawnChance = 0.2f;  
    [Range(0f, 1f)] public float smallPlantSpawnChance = 0.1f; 
    [Range(0f, 1f)] public float rockSpawnChance = 0.15f; 

    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public float textureTiling = 5f; // How many times to tile the texture across the terrain 
    public GameObject grassPrefab; // If you don't want any grass spawning logic, you can remove this
    public GameObject playerPrefab; // Player prefab
    public float waterLevel = .4f;
    public float scale = .1f;
    public int size = 100;

    [Header("Edge Settings")]
    public Material edgeMaterial; // New field for edge material

    [Header("Grass Settings")]
    public float grassDensity = 0.8f; // Grass density control

    [Header("River Settings")]
    public float riverNoiseScale = .06f;
    public int rivers = 5;

    [Header("Wind Settings")]
    public GameObject[] windPrefabs;
    [Range(0f, 1f)] public float windSpawnChance = 0.05f; // Adjust as needed

    Cell[,] grid;

    void Start() {
        
        float[,] noiseMap = new float[size, size];
        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float noiseValue = Mathf.PerlinNoise(x * scale + xOffset, y * scale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        float[,] falloffMap = new float[size, size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float xv = x / (float)size * 2 - 1;
                float yv = y / (float)size * 2 - 1;
                float v = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Mathf.Pow(v, 3f) / (Mathf.Pow(v, 3f) + Mathf.Pow(2.2f - 2.2f * v, 3f));
            }
        }

        grid = new Cell[size, size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float noiseValue = noiseMap[x, y];
                noiseValue -= falloffMap[x, y];
                bool isWater = noiseValue < waterLevel;
                Cell cell = new Cell();
                cell.isWater = isWater;
                grid[x, y] = cell;
            }
        }

        GenerateRivers(grid);
        DrawTerrainMesh(grid);
        DrawEdgeMesh(grid);

        // Spawn the camp first
        if (SpawnCampOnTerrain(grid, out Vector3 campPosition)) {
            // Spawn player away from the camp
            SpawnPlayerOnTerrain(grid, campPosition);

            // Generate grass, trees, nature elements
            GenerateGrass(grid);
            GenerateTrees(grid);
            GenerateNatureElements(grid);

            // Now generate the wind
            GenerateWind(grid);
        } else {
            Debug.LogError("Failed to spawn the camp. Terrain generation aborted.");
        }
    }

    void GenerateWind(Cell[,] grid) {
        if (windPrefabs == null || windPrefabs.Length == 0) {
            Debug.LogWarning("No wind prefabs assigned. Skipping wind generation.");
            return;
        }

        Debug.Log("Generating wind...");
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater && !cell.isOccupied) {
                    if (Random.value < windSpawnChance) {
                        SpawnWindElement(windPrefabs, x, y);
                    }
                }
            }
        }
    }

    void SpawnWindElement(GameObject[] prefabs, int x, int y) {
        if (prefabs.Length == 0) return;

        float randomOffsetX = Random.Range(-0.4f, 0.4f);
        float randomOffsetZ = Random.Range(-0.4f, 0.4f);
        Vector3 raycastStart = new Vector3(x + randomOffsetX, 50f, y + randomOffsetZ);
        int terrainLayerMask = LayerMask.GetMask("whatIsGround");
        int collisionMask = LayerMask.GetMask("Camp", "Nature");

        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, 100f, terrainLayerMask)) {
            Vector3 spawnPosition = hit.point;
            float checkRadius = 0.5f;
            if (!Physics.CheckSphere(spawnPosition, checkRadius, collisionMask)) {
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);

                instance.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                Debug.Log($"Spawned Wind ({prefab.name}) at {spawnPosition}");
            } else {
                Debug.LogWarning($"Collision detected for Wind at {spawnPosition}. Skipping spawn.");
            }
        } else {
            Debug.LogWarning($"No valid terrain detected under {raycastStart}. Skipping wind spawn.");
        }
    }

    float GetTerrainHeight(Vector3 position) {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(position.x, 100f, position.z), Vector3.down, out hit, Mathf.Infinity)) {
            return hit.point.y; // Return the height of the terrain
        }
        return 0f; 
    }

    bool SpawnCampOnTerrain(Cell[,] grid, out Vector3 campPosition) {
        campPosition = Vector3.zero; 
        int maxAttempts = 1000;
        int campSize = 5; 
        int boundaryMargin = 10; 

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            int randomX = Random.Range(campSize / 2 + boundaryMargin, size - campSize / 2 - boundaryMargin);
            int randomY = Random.Range(campSize / 2 + boundaryMargin, size - campSize / 2 - boundaryMargin);

            if (!grid[randomX, randomY].isWater) {
                bool isSurroundedByTerrain = true;
                for (int offsetX = -campSize / 2; offsetX <= campSize / 2; offsetX++) {
                    for (int offsetY = -campSize / 2; offsetY <= campSize / 2; offsetY++) {
                        int checkX = randomX + offsetX;
                        int checkY = randomY + offsetY;
                        if (checkX < 0 || checkX >= size || checkY < 0 || checkY >= size || grid[checkX, checkY].isWater) {
                            isSurroundedByTerrain = false;
                            break;
                        }
                    }
                    if (!isSurroundedByTerrain) break;
                }

                if (isSurroundedByTerrain) {
                    float terrainHeight = GetTerrainHeight(new Vector3(randomX, 0, randomY));
                    campPosition = new Vector3(randomX, terrainHeight, randomY);
                    GameObject campInstance = Instantiate(campPrefab, campPosition, Quaternion.identity);
                    campInstance.name = "Camp";

                    for (int offsetX = -campSize / 2; offsetX <= campSize / 2; offsetX++) {
                        for (int offsetY = -campSize / 2; offsetY <= campSize / 2; offsetY++) {
                            int markX = randomX + offsetX;
                            int markY = randomY + offsetY;
                            grid[markX, markY].isOccupied = true; 
                        }
                    }

                    Debug.Log($"Camp spawned at: {campPosition}");
                    return true; 
                }
            }
        }

        Debug.LogError("Failed to find a suitable position for the camp.");
        return false; 
    }

    void GenerateNatureElements(Cell[,] grid) {
        Debug.Log("Generating nature elements...");
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater && !cell.isOccupied) {
                    int grassCount = Mathf.FloorToInt(Random.Range(1, grassDensity * 5 * grassSpawnMultiplier));
                    for (int i = 0; i < grassCount; i++) {
                        SpawnNatureElement(grassPrefabs, x, y, "Grass");
                    }

                    if (Random.value < flowerSpawnChance) {
                        SpawnNatureElement(flowerPrefabs, x, y, "Flower");
                    }

                    if (Random.value < smallPlantSpawnChance) {
                        SpawnNatureElement(smallPlantPrefabs, x, y, "Small Plant");
                    }

                    if (Random.value < rockSpawnChance) {
                        SpawnNatureElement(rockPrefabs, x, y, "Rock");
                    }
                }
            }
        }
    }

    void SpawnNatureElement(GameObject[] prefabs, int x, int y, string elementType) {
        if (prefabs.Length == 0) {
            Debug.LogWarning($"{elementType} prefabs are not assigned.");
            return;
        }

        float randomOffsetX = Random.Range(-0.4f, 0.4f);
        float randomOffsetZ = Random.Range(-0.4f, 0.4f);
        Vector3 raycastStart = new Vector3(x + randomOffsetX, 50f, y + randomOffsetZ); 
        int terrainLayerMask = LayerMask.GetMask("whatIsGround");
        int collisionMask = LayerMask.GetMask("Camp", "Nature");

        if (Physics.Raycast(raycastStart, Vector3.down, out RaycastHit hit, 100f, terrainLayerMask)) {
            Vector3 spawnPosition = hit.point;
            float checkRadius = 0.5f; 
            if (Physics.CheckSphere(spawnPosition, checkRadius, collisionMask)) {
                Debug.LogWarning($"Collision detected for {elementType} at {spawnPosition}. Skipping spawn.");
                return;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);

            instance.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
            instance.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            Debug.Log($"Spawned {elementType} ({prefab.name}) at {spawnPosition}");
        } else {
            Debug.LogWarning($"No valid terrain detected at {raycastStart} for {elementType}. Skipping spawn.");
        }
    }

    void SpawnPlayerOnTerrain(Cell[,] grid, Vector3 campPosition) {
        Vector3 playerSpawnPosition = Vector3.zero;
        float maxDistance = float.MinValue;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater && !cell.isOccupied) {
                    Vector3 potentialPosition = new Vector3(x, 1f, y);
                    float distanceToCamp = Vector3.Distance(potentialPosition, campPosition);
                    if (distanceToCamp > maxDistance) {
                        maxDistance = distanceToCamp;
                        playerSpawnPosition = potentialPosition;
                    }
                }
            }
        }

        if (maxDistance > float.MinValue) {
            Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            Debug.Log($"Player spawned at: {playerSpawnPosition}, furthest from camp at: {campPosition}");
        } else {
            Debug.LogError("Failed to find a valid position for the player.");
        }
    }

    void GenerateRivers(Cell[,] grid) {
        float[,] noiseMap = new float[size, size];
        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float noiseValue = Mathf.PerlinNoise(x * riverNoiseScale + xOffset, y * riverNoiseScale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        GridGraph gg = AstarData.active.graphs[0] as GridGraph;
        if (gg == null) {
            Debug.LogError("GridGraph not found. Check AstarPath configuration.");
            return;
        }

        gg.center = new Vector3(size / 2f - .5f, 0, size / 2f - .5f);
        gg.SetDimensions(size, size, 1);
        AstarData.active.Scan(gg);

        int generatedRivers = 0;

        for (int i = 0; i < rivers; i++) {
            GraphNode start = gg.nodes[Random.Range(16, size - 16)];
            GraphNode end = gg.nodes[Random.Range(size * (size - 1) + 16, size * size - 16)];

            if (start == null || end == null) {
                Debug.LogWarning("Failed to find valid river start or end nodes.");
                continue;
            }

            ABPath path = ABPath.Construct((Vector3)start.position, (Vector3)end.position, (Path result) => {
                for (int j = 0; j < result.path.Count; j++) {
                    GraphNode node = result.path[j];
                    int x = Mathf.RoundToInt(((Vector3)node.position).x);
                    int y = Mathf.RoundToInt(((Vector3)node.position).z);

                    if (x < 0 || x >= size || y < 0 || y >= size || grid[x, y].isOccupied)
                        continue;

                    if (j > 0 && Random.value < 0.3f) {
                        int deviationX = Mathf.Clamp(x + Random.Range(-1, 2), 0, size - 1);
                        int deviationY = Mathf.Clamp(y + Random.Range(-1, 2), 0, size - 1);
                        if (!grid[deviationX, deviationY].isOccupied && !grid[deviationX, deviationY].isWater) {
                            grid[deviationX, deviationY].isWater = true;
                            Debug.Log($"River deviation node added at ({deviationX}, {deviationY})");
                        }
                    }

                    grid[x, y].isWater = true;
                    Debug.Log($"River node added at ({x}, {y})");
                }

                generatedRivers++;
            });

            AstarPath.StartPath(path);
            AstarPath.BlockUntilCalculated(path);
        }

        Debug.Log($"{generatedRivers} rivers successfully generated.");
    }

    void DrawTerrainMesh(Cell[,] grid) {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater) {
                    Vector3 a = new Vector3(x - 0.5f, 0, y + 0.5f);
                    Vector3 b = new Vector3(x + 0.5f, 0, y + 0.5f);
                    Vector3 c = new Vector3(x - 0.5f, 0, y - 0.5f);
                    Vector3 d = new Vector3(x + 0.5f, 0, y - 0.5f);

                    // Instead of mapping UVs to (x/size, y/size), tile them more frequently
                    float u0 = x * textureTiling / (float)size;
                    float v0 = y * textureTiling / (float)size;
                    float u1 = (x + 1) * textureTiling / (float)size;
                    float v1 = (y + 1) * textureTiling / (float)size;

                    Vector2 uvA = new Vector2(u0, v1);
                    Vector2 uvB = new Vector2(u1, v1);
                    Vector2 uvC = new Vector2(u0, v0);
                    Vector2 uvD = new Vector2(u1, v0);

                    int vertStart = vertices.Count;
                    vertices.Add(a);
                    vertices.Add(b);
                    vertices.Add(c);
                    vertices.Add(b);
                    vertices.Add(d);
                    vertices.Add(c);

                    uvs.Add(uvA);
                    uvs.Add(uvB);
                    uvs.Add(uvC);
                    uvs.Add(uvB);
                    uvs.Add(uvD);
                    uvs.Add(uvC);

                    // Triangles
                    triangles.Add(vertStart);
                    triangles.Add(vertStart + 1);
                    triangles.Add(vertStart + 2);
                    triangles.Add(vertStart + 3);
                    triangles.Add(vertStart + 4);
                    triangles.Add(vertStart + 5);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        if (meshCollider == null) {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
    // void AddEdgeCollider(Vector3 position, Vector3 scale) {
    //     GameObject colliderObj = new GameObject("EdgeCollider");
    //     colliderObj.transform.position = position;
    //     colliderObj.transform.localScale = scale;
    //     colliderObj.transform.SetParent(transform);

    //     BoxCollider collider = colliderObj.AddComponent<BoxCollider>();
    //     collider.isTrigger = false; 
    // }

    void DrawEdgeMesh(Cell[,] grid) {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater) {
                    // Check left, right, down, and up for water to create edges
                    if (x > 0) {
                        Cell left = grid[x - 1, y];
                        if (left.isWater) {
                            Vector3 a = new Vector3(x - .5f, 0, y + .5f);
                            Vector3 b = new Vector3(x - .5f, 0, y - .5f);
                            Vector3 c = new Vector3(x - .5f, -1, y + .5f);
                            Vector3 d = new Vector3(x - .5f, -1, y - .5f);
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++) {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                            //AddEdgeCollider(new Vector3(x - .5f, 0.5f, y), new Vector3(0.1f, 1f, 1f));
                        }
                    }
                    if (x < size - 1) {
                        Cell right = grid[x + 1, y];
                        if (right.isWater) {
                            Vector3 a = new Vector3(x + .5f, 0, y - .5f);
                            Vector3 b = new Vector3(x + .5f, 0, y + .5f);
                            Vector3 c = new Vector3(x + .5f, -1, y - .5f);
                            Vector3 d = new Vector3(x + .5f, -1, y + .5f);
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++) {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                            //AddEdgeCollider(new Vector3(x + .5f, 0.5f, y), new Vector3(0.1f, 1f, 1f));
                        }
                    }
                    if (y > 0) {
                        Cell down = grid[x, y - 1];
                        if (down.isWater) {
                            Vector3 a = new Vector3(x - .5f, 0, y - .5f);
                            Vector3 b = new Vector3(x + .5f, 0, y - .5f);
                            Vector3 c = new Vector3(x - .5f, -1, y - .5f);
                            Vector3 d = new Vector3(x + .5f, -1, y - .5f);
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++) {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                            //AddEdgeCollider(new Vector3(x, 0.5f, y - .5f), new Vector3(1f, 1f, 0.1f));
                        }
                    }
                    if (y < size - 1) {
                        Cell up = grid[x, y + 1];
                        if (up.isWater) {
                            Vector3 a = new Vector3(x + .5f, 0, y + .5f);
                            Vector3 b = new Vector3(x - .5f, 0, y + .5f);
                            Vector3 c = new Vector3(x + .5f, -1, y + .5f);
                            Vector3 d = new Vector3(x - .5f, -1, y + .5f);
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++) {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                            //AddEdgeCollider(new Vector3(x, 0.5f, y + .5f), new Vector3(1f, 1f, 0.1f));
                        }
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // Create the Edge GameObject
        GameObject edgeObj = new GameObject("Edge");
        edgeObj.transform.SetParent(transform);

        // Add MeshFilter and assign the mesh
        MeshFilter meshFilter = edgeObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Add MeshRenderer and assign the edge material
        MeshRenderer meshRenderer = edgeObj.AddComponent<MeshRenderer>();
        if(edgeMaterial != null)
        {
            meshRenderer.material = edgeMaterial; // Use the edge material
        }
        else
        {
            Debug.LogWarning("Edge material not assigned. Using terrain material as fallback.");
            meshRenderer.material = terrainMaterial; // Fallback to terrain material if edge material is not assigned
        }
    }

    bool IsNearWater(Cell[,] grid, int x, int y) {
        if (x > 0 && grid[x - 1, y].isWater) return true;
        if (x < size - 1 && grid[x + 1, y].isWater) return true;
        if (y > 0 && grid[x, y - 1].isWater) return true;
        if (y < size - 1 && grid[x, y + 1].isWater) return true;
        return false;
    }

    void GenerateTrees(Cell[,] grid) {
        float[,] noiseMap = new float[size, size];
        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float noiseValue = Mathf.PerlinNoise(x * treesNoiseScale + xOffset, y * treesNoiseScale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        int collisionMask = LayerMask.GetMask("Camp", "Nature");

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater && !cell.isOccupied) {
                    float v = Random.Range(0f, treeDensity);
                    if (noiseMap[x, y] < v) {
                        float terrainHeight = GetTerrainHeight(new Vector3(x, 0, y));
                        Vector3 position = new Vector3(x, terrainHeight, y);
                        float checkRadius = 1f; 
                        if (!Physics.CheckSphere(position, checkRadius, collisionMask)) {
                            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                            GameObject tree = Instantiate(prefab, position, Quaternion.identity, transform);
                            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                            tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);

                            cell.hasTree = true;
                            grid[x, y] = cell;
                        } else {
                            Debug.Log($"Collision detected at {position}. Skipping tree spawn.");
                        }
                    }
                }
            }
        }
    }

    void GenerateGrass(Cell[,] grid) {
        if (grassPrefabs == null || grassPrefabs.Length == 0) {
            Debug.LogWarning("No grass prefabs assigned. Skipping grass generation.");
            return;
        }

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Cell cell = grid[x, y];
                if (!cell.isWater) {
                    int grassCount = Mathf.FloorToInt(Random.Range(1, grassDensity * 10));
                    for (int i = 0; i < grassCount; i++) {
                        float randomOffsetX = Random.Range(-0.5f, 0.5f);
                        float randomOffsetZ = Random.Range(-0.5f, 0.5f);

                        Vector3 position = new Vector3(x + randomOffsetX, GetTerrainHeight(new Vector3(x, 0, y)) + 0.1f, y + randomOffsetZ);
                        GameObject grass = Instantiate(grassPrefabs[Random.Range(0, grassPrefabs.Length)], position, Quaternion.identity, transform);
                        grass.transform.localScale = Vector3.one * 0.5f; // Set scale to 0.4
                    }
                }
            }
        }
    }
}