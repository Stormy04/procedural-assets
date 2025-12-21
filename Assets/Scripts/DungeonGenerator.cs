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
    public int seed;
    public GameObject wallPrefab;
    HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
    private List<RectInt> rooms = new List<RectInt>();
    [Header("Corridor Settings")]
    [Range(1, 5)]
    public int corridorWidth = 3;
    [Header("Player Settings")]
    public GameObject playerPrefab;

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        ClearDungeon();
        Random.InitState(seed);
        rooms.Clear();
        floorPositions.Clear();

        for (int i = 0; i < maxRooms; i++)
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
            {
                rooms.Add(newRoom);
            }
        }

        BuildRooms();
        BuildCorridors();
        BuildWalls();
        SpawnPlayer();
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

        // Pick a random room
        RectInt room = rooms[Random.Range(0, rooms.Count)];

        // Pick a random position inside the room
        int x = Random.Range(room.xMin, room.xMax);
        int z = Random.Range(room.yMin, room.yMax);

        Vector3 spawnPos = new Vector3(x, 1f, z); // 1 unit above floor
        Instantiate(playerPrefab, spawnPos, Quaternion.identity);
    }

}
