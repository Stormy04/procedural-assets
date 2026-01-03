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
    private List<DungeonRoom> rooms = new List<DungeonRoom>();
    [Header("Corridor Settings")]
    [Range(1, 5)]
    public int corridorWidth = 3;
    [Header("Player Settings")]
    public GameObject playerPrefab;
    [Header("Collectible Settings")]
    public GameObject collectiblePrefab;
    public int minCollectiblesPerRoom = 1;
    public int maxCollectiblesPerRoom = 3;
    [System.Serializable]
    public class DungeonRoom
    {
        public RectInt bounds;
        public RoomType type;
    }

    public enum RoomType
    {
        Start,
        Empty,
        Treasure,
        Combat,
        Exit
    }

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
        AssignRoomTypes();
        BuildRooms();
        BuildCorridors();
        BuildWalls();
        SpawnCollectibles();
        SpawnPlayer();

        Debug.Log("Dungeon Seed: " + seed);
    }
    void GenerateRooms()
    {
        int attempts = 0;

        while (rooms.Count < maxRooms && attempts < maxRooms * 5)
        {
            int roomWidth = Random.Range(minRoomSize, maxRoomSize);
            int roomHeight = Random.Range(minRoomSize, maxRoomSize);

            int x = Random.Range(1, dungeonWidth - roomWidth - 1);
            int y = Random.Range(1, dungeonHeight - roomHeight - 1);

            RectInt newRoomRect = new RectInt(x, y, roomWidth, roomHeight);

            bool overlaps = false;
            foreach (var room in rooms)
            {
                RectInt expandedRoom = new RectInt(
                    room.bounds.xMin - 1,
                    room.bounds.yMin - 1,
                    room.bounds.width + 2,
                    room.bounds.height + 2
                );

                if (newRoomRect.Overlaps(expandedRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                rooms.Add(new DungeonRoom
                {
                    bounds = newRoomRect,
                    type = RoomType.Empty
                });
            }

            attempts++;
        }
    }



    void BuildRooms()
    {
        foreach (var room in rooms)
        {
            for (int x = room.bounds.xMin; x < room.bounds.xMax; x++)
            {
                for (int z = room.bounds.yMin; z < room.bounds.yMax; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    floorPositions.Add(gridPos);

                    Vector3 floorWorldPos = new Vector3(x, 0, z);
                    GameObject floor = Instantiate(
                        floorPrefab,
                        floorWorldPos,
                        Quaternion.identity,
                        transform
                    );

                    // OPTIONAL: color floors by room type (debug / visual clarity)
                    Renderer r = floor.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.material.color = GetRoomColor(room.type);
                    }
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
                rooms[i].bounds.x + rooms[i].bounds.width / 2,
                rooms[i].bounds.y + rooms[i].bounds.height / 2
            );

            Vector2Int centerB = new Vector2Int(
                rooms[i + 1].bounds.x + rooms[i + 1].bounds.width / 2,
                rooms[i + 1].bounds.y + rooms[i + 1].bounds.height / 2
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

        // Find the start room
        DungeonRoom startRoom = rooms.Find(r => r.type == RoomType.Start);
        if (startRoom == null)
        {
            Debug.LogWarning("No Start Room found! Spawning player in first room.");
            startRoom = rooms[0];
        }

        RectInt bounds = startRoom.bounds;

        // Clamp to valid floor positions
        int x = Mathf.Clamp(Random.Range(bounds.xMin + 1, bounds.xMax - 1), 0, dungeonWidth - 1);
        int z = Mathf.Clamp(Random.Range(bounds.yMin + 1, bounds.yMax - 1), 0, dungeonHeight - 1);

        Vector3 spawnPos = new Vector3(x, 1f, z); // Y = 1 for player height

        // Optional: raycast down to make sure player is above floor
        if (!Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            spawnPos.y = 1f; // fallback
        }
        else
        {
            spawnPos.y = hit.point.y + 1f; // place above floor
        }

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // Assign camera
        ThirdPersonController controller = playerInstance.GetComponent<ThirdPersonController>();
        if (controller != null && Camera.main != null)
            controller.cameraTransform = Camera.main.transform;
    }



    void SpawnCollectibles()
    {
        if (collectiblePrefab == null) return;

        foreach (DungeonRoom room in rooms)
        {
            if (room.type == RoomType.Empty || room.type == RoomType.Start)
                continue;

            RectInt bounds = room.bounds;

            int collectibleCount =
                room.type == RoomType.Treasure ?
                Random.Range(3, 6) :
                Random.Range(1, 3);

            HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < collectibleCount; i++)
            {
                int attempts = 0;

                while (attempts < 10)
                {
                    int x = Random.Range(bounds.xMin + 1, bounds.xMax - 1);
                    int z = Random.Range(bounds.yMin + 1, bounds.yMax - 1);

                    Vector2Int gridPos = new Vector2Int(x, z);

                    if (!floorPositions.Contains(gridPos) || usedPositions.Contains(gridPos))
                    {
                        attempts++;
                        continue;
                    }

                    usedPositions.Add(gridPos);
                    Instantiate(collectiblePrefab, new Vector3(x, 0.5f, z), Quaternion.identity, transform);
                    break;
                }
            }
        }
    }

    void AssignRoomTypes()
    {
        if (rooms.Count < 2) return;

        // START room → first room
        rooms[0].type = RoomType.Start;

        // EXIT room → farthest from start
        DungeonRoom startRoom = rooms[0];
        DungeonRoom exitRoom = startRoom;
        float maxDistance = 0f;

        foreach (var room in rooms)
        {
            float dist = Vector2.Distance(
                startRoom.bounds.center,
                room.bounds.center
            );

            if (dist > maxDistance)
            {
                maxDistance = dist;
                exitRoom = room;
            }
        }

        exitRoom.type = RoomType.Exit;

        // Assign remaining rooms
        foreach (var room in rooms)
        {
            if (room.type != RoomType.Empty) continue;

            float roll = Random.value;

            if (roll < 0.25f)
                room.type = RoomType.Treasure;
            else if (roll < 0.55f)
                room.type = RoomType.Combat;
            else
                room.type = RoomType.Empty;
        }
    }
    Color GetRoomColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return Color.green;
            case RoomType.Exit: return Color.red;
            case RoomType.Treasure: return Color.yellow;
            case RoomType.Combat: return Color.magenta;
            default: return Color.gray;
        }
    }

}
