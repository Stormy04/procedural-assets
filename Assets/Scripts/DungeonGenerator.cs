using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Size")]
    public int dungeonWidth = 40;
    public int dungeonHeight = 40;

    [Header("Room Settings")]
    public int minRoomSize = 6;
    public int maxRoomSize = 12;
    public int maxRooms = 8;
    public GameObject floorPrefab;
    [Header("Dungeon Settings")]
    public int seed = 0;           // if 0, generate randomly
    public bool useRandomSeed = true;

    public GameObject wallPrefab;
    HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
    private List<RectInt> rooms = new List<RectInt>();
    [Header("Corridor Settings")]
    [Range(1, 5)]
    public int corridorWidth = 3;
    [Header("Player Settings")]
    public GameObject playerPrefab;
    [Header("Collectible Settings")]
    public GameObject collectiblePrefab;
    public int minCollectiblesPerRoom = 1;
    public int maxCollectiblesPerRoom = 3;

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        ClearDungeon();

        // Initialize random seed
        if (useRandomSeed || seed == 0)
            seed = Random.Range(int.MinValue, int.MaxValue);

        Random.InitState(seed);

        rooms.Clear();
        floorPositions.Clear();

        GenerateRooms();
        BuildRooms();
        BuildCorridors();
        BuildWalls();
        SpawnCollectibles();
        SpawnPlayer();

        Debug.Log("Dungeon Seed: " + seed);
    }
    void GenerateRooms()
    {
        int attempts = 0; // to avoid infinite loops
        while (rooms.Count < maxRooms && attempts < maxRooms * 5)
        {
            int roomWidth = Random.Range(minRoomSize, maxRoomSize);
            int roomHeight = Random.Range(minRoomSize, maxRoomSize);

            int x = Random.Range(1, dungeonWidth - roomWidth - 1);
            int y = Random.Range(1, dungeonHeight - roomHeight - 1);

            RectInt newRoom = new RectInt(x, y, roomWidth, roomHeight);

            bool overlaps = false;
            foreach (var room in rooms)
            {
                RectInt expandedRoom = new RectInt(
                    room.xMin - 1,
                    room.yMin - 1,
                    room.width + 2,
                    room.height + 2
                );
                if (newRoom.Overlaps(expandedRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                rooms.Add(newRoom);

            attempts++;
        }
    }


    void BuildRooms()
    {
        foreach (var room in rooms)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int z = room.yMin; z < room.yMax; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    floorPositions.Add(gridPos);

                    Vector3 floorWorldPos = new Vector3(x, 0, z);
                    Instantiate(floorPrefab, floorWorldPos, Quaternion.identity, transform);


                }
            }
        }
    }
    void ClearDungeon()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    void BuildCorridors()
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2Int centerA = new Vector2Int(
    rooms[i].x + rooms[i].width / 2,
    rooms[i].y + rooms[i].height / 2
);

            Vector2Int centerB = new Vector2Int(
                rooms[i + 1].x + rooms[i + 1].width / 2,
                rooms[i + 1].y + rooms[i + 1].height / 2
            );
            if (Random.value < 0.5f)
            {
                CreateHorizontalCorridor(centerA.x, centerB.x, centerA.y);
                CreateVerticalCorridor(centerA.y, centerB.y, centerB.x);
            }
            else
            {
                CreateVerticalCorridor(centerA.y, centerB.y, centerA.x);
                CreateHorizontalCorridor(centerA.x, centerB.x, centerB.y);
            }
        }
    }

    void CreateHorizontalCorridor(int xStart, int xEnd, int z)
    {
        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x++)
        {
            for (int offset = 0; offset < corridorWidth; offset++)
            {
                int zPos = z + offset - corridorWidth / 2; // center corridor
                Vector2Int gridPos = new Vector2Int(x, zPos);
                if (!floorPositions.Contains(gridPos))
                    floorPositions.Add(gridPos);

                Vector3 floorWorldPos = new Vector3(x, 0, zPos);
                Instantiate(floorPrefab, floorWorldPos, Quaternion.identity, transform);
            }
        }

    }

    void CreateVerticalCorridor(int zStart, int zEnd, int x)
    {
        for (int z = Mathf.Min(zStart, zEnd); z <= Mathf.Max(zStart, zEnd); z++)
        {
            for (int offset = 0; offset < corridorWidth; offset++)
            {
                int xPos = x + offset - corridorWidth / 2; // center corridor
                Vector2Int gridPos = new Vector2Int(xPos, z);
                if (!floorPositions.Contains(gridPos))
                    floorPositions.Add(gridPos);

                Vector3 floorWorldPos = new Vector3(xPos, 0, z);
                Instantiate(floorPrefab, floorWorldPos, Quaternion.identity, transform);
            }
        }

    }
    void BuildWalls()
    {
        foreach (Vector2Int floorPos in floorPositions)
        {
            TryPlaceWall(floorPos, Vector2Int.up, 0);
            TryPlaceWall(floorPos, Vector2Int.down, 180);
            TryPlaceWall(floorPos, Vector2Int.left, 270);
            TryPlaceWall(floorPos, Vector2Int.right, 90);
        }
    }
    void TryPlaceWall(Vector2Int floorPos, Vector2Int direction, float rotationY)
    {
        Vector2Int neighborPos = floorPos + direction;

        if (floorPositions.Contains(neighborPos))
            return;

        Vector3 wallWorldPos = new Vector3(
            floorPos.x + direction.x * 0.5f,
            1.25f,
            floorPos.y + direction.y * 0.5f
        );

        Instantiate(
            wallPrefab,
            wallWorldPos,
            Quaternion.Euler(0, rotationY, 0),
            transform
        );
    }
    void SpawnPlayer()
    {
        if (rooms.Count == 0 || playerPrefab == null) return;

        // Pick a room and random position
        RectInt room = rooms[Random.Range(0, rooms.Count)];
        int x = Random.Range(room.xMin, room.xMax);
        int z = Random.Range(room.yMin, room.yMax);
        Vector3 spawnPos = new Vector3(x, 1f, z);

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Assign camera to ThirdPersonController
        ThirdPersonController controller = playerInstance.GetComponent<ThirdPersonController>();
        if (controller != null && Camera.main != null)
            controller.cameraTransform = Camera.main.transform;
    }

    void SpawnCollectibles()
    {
        if (collectiblePrefab == null) return;

        foreach (RectInt room in rooms)
        {
            int collectibleCount = Random.Range(
                minCollectiblesPerRoom,
                maxCollectiblesPerRoom + 1
            );

            HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < collectibleCount; i++)
            {
                int attempts = 0;

                while (attempts < 10)
                {
                    int x = Random.Range(room.xMin + 1, room.xMax - 1);
                    int z = Random.Range(room.yMin + 1, room.yMax - 1);

                    Vector2Int gridPos = new Vector2Int(x, z);

                    if (!floorPositions.Contains(gridPos) || usedPositions.Contains(gridPos))
                    {
                        attempts++;
                        continue;
                    }

                    usedPositions.Add(gridPos);

                    Vector3 worldPos = new Vector3(x, 0.5f, z);
                    Instantiate(collectiblePrefab, worldPos, Quaternion.identity, transform);
                    break;
                }
            }
        }
    }

}
