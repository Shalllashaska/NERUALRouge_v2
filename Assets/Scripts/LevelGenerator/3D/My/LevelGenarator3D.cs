using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = System.Random;
using RandomUnit = UnityEngine.Random;
using Graphs;
using UnityEditor;
using UnityEditor.PackageManager;

public class LevelGenarator3D : MonoBehaviour
{
    
    enum CellType {
        None,
        Room,
        Hallway,
        Stairs
    }
    
    class Room {
        public BoundsInt bounds;

        public int NumberOfRoom;
        public int RoomSizeNum;
        public bool HaveHallway = false;
        public bool PlayerRoom = false;
        public bool BossRoom = false;
        public bool GoldenRoom = false;
        public Room(Vector3Int location, Vector3Int size) {
            bounds = new BoundsInt(location, size);
        }

        public static bool Intersect(Room a, Room b) {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y)
                || (a.bounds.position.z >= (b.bounds.position.z + b.bounds.size.z)) || ((a.bounds.position.z + a.bounds.size.z) <= b.bounds.position.z));
        }
    }
    
    public Vector3Int size;
    [Range(1, 10)]
    public int multiplier = 1;
    public int roomCount;
    public GameObject[] roomsPrefab;
    public GameObject[] playerRoomsPrefab;
    public GameObject[] bossRoomsPrefab;
    public GameObject[] goldenRoomsPrefab;
    public Transform parentHallways;
    public Transform parentRooms;
    public GameObject hallwayPrefab;
    public GameObject[] stairsPrefab;

    private List<GameObject> spawnedRooms;

    private int[,] roomsSizes = new int[,] //Количество комната и их размеров должно совпадать
    {
        {3,5,3},
        {3,5,7},
        {5,3,5},
        {5,5,5},
        {5,5,7},
        {7,5,3},
        {7,5,7},
        {7,7,7},
        {3,5,11},
        {11,5,3},
    };
    
    private int[,] bossRoomsSizes = new int[,] //Количество комната и их размеров должно совпадать
    {
        {3,5,11},
        {5,3,5},
        {5,5,5},
        {7,5,7},
        {7,7,7},
    };
    
    private Random random;
    private Grid3D<CellType> grid;
    private List<Room> rooms;
    private Delaunay3D delaunay;
    private HashSet<Prim.Edge> selectedEdges;
    
    private int numRoom = 1;

    private int curRoomSizeNum;

    private HashSet<Vector3Int> alreadySet;

    private Room playerRoom;
    
    private void Awake()
    {
        //roomCount = RandomUnit.Range(5, 12);
        random = new Random(468);
        grid = new Grid3D<CellType>(size, Vector3Int.zero);
        rooms = new List<Room>();
        spawnedRooms = new List<GameObject>();
        alreadySet = new HashSet<Vector3Int>();
    }

    void Start()
    {
        PlaceRooms();
        //PickSpawnPlayerRoom();
        Triangulate();
        CreateHallways();
        PathfindHallways();
    }

    private void PlaceRooms()
    {
        bool havePlayerRoom = false;
        bool haveGoldenRoom = false;
        bool haveBossRoom = false;
        while (numRoom < roomCount)
        {
            curRoomSizeNum = random.Next(0, roomsSizes.GetLength(0) - 1);
            
            Vector3Int roomSize = new Vector3Int(
                roomsSizes[curRoomSizeNum,0],
                roomsSizes[curRoomSizeNum,1],
                roomsSizes[curRoomSizeNum,2]
            );
            
            Vector3Int location = new Vector3Int(
                random.Next(0, size.x),
                random.Next(0, size.y),
                random.Next(0, size.z)
            );
            
            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector3Int(-1, -1, -1), roomSize + new Vector3Int(2, 2, 2));
            
            foreach (var room in rooms) {
                if (Room.Intersect(room, buffer)) {
                    add = false;
                    break;
                }
            }
            
            float x = newRoom.bounds.position.x;
            float y = newRoom.bounds.position.y;
            float z = newRoom.bounds.position.z;
            float hx = x + newRoom.bounds.size.x;
            float hy = y + newRoom.bounds.size.y;
            float hz = z + newRoom.bounds.size.z;
            if (x < 0 || hx >= size.x || 
                y < 0 || hy >= size.y ||
                z < 0 || hz >= size.z)
            {
                add = false;
            }

            if (add)
            {
                newRoom.NumberOfRoom = numRoom;
                newRoom.RoomSizeNum = curRoomSizeNum;
                if(random.NextDouble() < 0.25 && !haveGoldenRoom) //спавн золотой комнаты
                {
                    if (!newRoom.PlayerRoom)
                    {
                        newRoom.GoldenRoom = true;
                        PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curRoomSizeNum, 2);
                        haveGoldenRoom = true;
                    }
                }
                else if(numRoom == roomCount - 2 && !haveGoldenRoom)
                {
                    if (!newRoom.PlayerRoom)
                    {
                        newRoom.GoldenRoom = true;
                        PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curRoomSizeNum, 2);
                        haveGoldenRoom = true;
                    }
                }
                else if (random.NextDouble() < 0.25 && !havePlayerRoom) //спавн комнаты игрока
                {
                    if (!newRoom.GoldenRoom)
                    {
                        newRoom.PlayerRoom = true;
                        PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curRoomSizeNum, 1);
                        havePlayerRoom = true;
                        playerRoom = newRoom;
                        playerRoom.PlayerRoom = true;
                    }
                }
                else if(numRoom == roomCount - 1 && !havePlayerRoom)
                {
                    newRoom.PlayerRoom = true;
                    PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curRoomSizeNum, 1);
                    havePlayerRoom = true;
                    playerRoom = newRoom;
                    playerRoom.PlayerRoom = true;
                }
                else
                {
                    PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curRoomSizeNum, 0);
                }
                rooms.Add(newRoom);
                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
                numRoom++;
            }
        }

        while (!haveBossRoom)
        {
            int curBossRoomSizeNum = random.Next(0, bossRoomsSizes.GetLength(0) - 1);
            
            Vector3Int roomSize = new Vector3Int(
                bossRoomsSizes[curBossRoomSizeNum,0],
                bossRoomsSizes[curBossRoomSizeNum,1],
                bossRoomsSizes[curBossRoomSizeNum,2]
            );
            
            Vector3Int location = new Vector3Int(
                random.Next(0, size.x),
                random.Next(0, size.y),
                random.Next(0, size.z)
            );
            
            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector3Int(-1, -1, -1), roomSize + new Vector3Int(2, 2, 2));
            Room bufferPlRoom = new Room(playerRoom.bounds.position + new Vector3Int(-3, -3, -3), playerRoom.bounds.size + new Vector3Int(6, 6, 6));

            if (Room.Intersect(newRoom, bufferPlRoom))
            {
                add = false;
            }

            if (add)
            {
                foreach (var room in rooms)
                {
                    if (Room.Intersect(room, buffer))
                    {
                        add = false;
                        break;
                    }
                }
            }

            float x = newRoom.bounds.position.x;
            float y = newRoom.bounds.position.y;
            float z = newRoom.bounds.position.z;
            float hx = x + newRoom.bounds.size.x;
            float hy = y + newRoom.bounds.size.y;
            float hz = z + newRoom.bounds.size.z;
            if (x < 0 || hx >= size.x || 
                y < 0 || hy >= size.y ||
                z < 0 || hz >= size.z)
            {
                add = false;
            }
            
            if (add)
            {
                newRoom.NumberOfRoom = numRoom;
                newRoom.RoomSizeNum = curBossRoomSizeNum;
                newRoom.BossRoom = true;
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curBossRoomSizeNum, 3);
                rooms.Add(newRoom);
                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
                numRoom++;
                haveBossRoom = true;
            }
        }
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay3D.Triangulate(vertices);
      
    }
    
    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>();
        
        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> minimumSpanningTree = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(minimumSpanningTree);
        
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);
        
        foreach (var edge in remainingEdges) {
            if (random.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        DungeonPathfinder3D aStar = new DungeonPathfinder3D(size);

        foreach (var edge in selectedEdges)
        {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector3Int((int) startPosf.x, (int) startPosf.y, (int) startPosf.z);
            var endPos = new Vector3Int((int) endPosf.x, (int) endPosf.y, (int) endPosf.z);
            
            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder3D.Node a, DungeonPathfinder3D.Node b) =>
            {
                var pathCost = new DungeonPathfinder3D.PathCost();

                var delta = b.Position - a.Position;

                pathCost.cost = Vector3Int.Distance(b.Position, endPos); //heuristic
                
                if (delta.y == 0)
                {
                    //flat hallway
                    pathCost.cost = Vector3Int.Distance(b.Position, endPos); //heuristic

                    if (grid[b.Position] == CellType.Stairs)
                    {
                        return pathCost;
                    }
                    else if (grid[b.Position] == CellType.Room)
                    {
                        pathCost.cost += 5;
                    }
                    else if (grid[b.Position] == CellType.None)
                    {
                        pathCost.cost += 1;
                    }

                    pathCost.traversable = true;
                }
                else
                {
                    //staircase
                    if ((grid[a.Position] != CellType.None && grid[a.Position] != CellType.Hallway)
                        || (grid[b.Position] != CellType.None && grid[b.Position] != CellType.Hallway)) return pathCost;

                    pathCost.cost = 100 + Vector3Int.Distance(b.Position, endPos); //base cost + heuristic

                    int xDir = Mathf.Clamp(delta.x, -1, 1);
                    int zDir = Mathf.Clamp(delta.z, -1, 1);
                    Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                    Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                    if (!grid.InBounds(a.Position + verticalOffset)
                        || !grid.InBounds(a.Position + horizontalOffset)
                        || !grid.InBounds(a.Position + verticalOffset + horizontalOffset))
                    {
                        return pathCost;
                    }

                    if (grid[a.Position + horizontalOffset] != CellType.None
                        || grid[a.Position + horizontalOffset * 2] != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset] != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset * 2] != CellType.None)
                    {
                        return pathCost;
                    }

                    pathCost.traversable = true;
                    pathCost.isStairs = true;
                }
                return pathCost;
            });

            
            
            if (path != null)
            {
                startRoom.HaveHallway = true;
                endRoom.HaveHallway = true;
                Debug.Log("Postiton start room - " +startRoom.NumberOfRoom+ ", end room - "+endRoom.NumberOfRoom+" path Yes");
                int minI = path.Count;
                int maxI = 0;
                for (int i = 0; i < path.Count; i++)
                {
                    var current = path[i];

                    if (grid[current] == CellType.None)
                    {
                        if (i < minI) minI = i;
                        if (i > maxI) maxI = i;
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0)
                    {
                        var prev = path[i - 1];

                        var delta = current - prev;

                        if (delta.y != 0)
                        {
                            int xDir = Mathf.Clamp(delta.x, -1, 1);
                            int zDir = Mathf.Clamp(delta.z, -1, 1);
                            Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                            Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                            grid[prev + horizontalOffset] = CellType.Stairs;
                            grid[prev + horizontalOffset * 2] = CellType.Stairs;
                            grid[prev + verticalOffset + horizontalOffset] = CellType.Stairs;
                            grid[prev + verticalOffset + horizontalOffset * 2] = CellType.Stairs;

                            if (delta.y > 0)
                            {
                                PlaceStairs(prev + horizontalOffset, stairsPrefab[0], xDir, zDir, delta.y);
                                PlaceStairs(prev + horizontalOffset * 2, stairsPrefab[1], xDir, zDir, delta.y);
                                PlaceStairs(prev + verticalOffset + horizontalOffset, stairsPrefab[2], xDir, zDir, delta.y);
                                PlaceStairs(prev + verticalOffset + horizontalOffset * 2, stairsPrefab[3], xDir, zDir, delta.y);
                            }
                            else if (delta.y < 0)
                            {
                                PlaceStairs(prev + horizontalOffset, stairsPrefab[3], xDir, zDir, delta.y);
                                PlaceStairs(prev + horizontalOffset * 2, stairsPrefab[2], xDir, zDir, delta.y);
                                PlaceStairs(prev + verticalOffset + horizontalOffset, stairsPrefab[1], xDir, zDir, delta.y);
                                PlaceStairs(prev + verticalOffset + horizontalOffset * 2, stairsPrefab[0], xDir, zDir, delta.y);
                            }
                            

                        }

                    }
                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway && !alreadySet.Contains(pos))
                    {
                        if (pos == path[minI])
                        {
                            PlaceHallway(pos, true);
                        }
                        else if (pos == path[maxI])
                        {
                            PlaceHallway(pos, true);
                        }
                        else
                        {
                            PlaceHallway(pos, false);
                        }
                        alreadySet.Add(pos);
                    }
                }

            }
            else
            {

                if (!startRoom.HaveHallway) startRoom.HaveHallway = false;
                if (!endRoom.HaveHallway) endRoom.HaveHallway = false;
                Debug.Log("Postiton start room - " +startRoom.NumberOfRoom+ ", end room - "+endRoom.NumberOfRoom+" path NO!!!");
            }
        }
    }
    
    void PlaceStairs(Vector3Int location, GameObject prefab, int xDir, int zDir, int deltaY) {
        if (deltaY > 0)
        {
            if (xDir == 0 && zDir == 1)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
            }
            else if (xDir == 1 && zDir == 0)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
                go.transform.rotation = Quaternion.AngleAxis(90, Vector3.up);
            }
            else if (xDir == -1 && zDir == 0)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
                go.transform.rotation = Quaternion.AngleAxis(270, Vector3.up);
            }
            else if (xDir == 0 && zDir == -1)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
                go.transform.rotation = Quaternion.AngleAxis(180, Vector3.up);
            }
        }
        else
        {
            if (xDir == 0 && zDir == 1)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
                go.transform.rotation = Quaternion.AngleAxis(180, Vector3.up);
            }
            else if (xDir == 1 && zDir == 0)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
                go.transform.rotation = Quaternion.AngleAxis(270, Vector3.up);
            }
            else if (xDir == -1 && zDir == 0)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
                go.transform.rotation = Quaternion.AngleAxis(90, Vector3.up);
            }
            else if (xDir == 0 && zDir == -1)
            {
                GameObject go = Instantiate(prefab, location * multiplier, Quaternion.identity, parentHallways);
            }
        }
    }
    void PlaceHallway(Vector3Int location, bool makeDoor) {
        GameObject go = Instantiate(hallwayPrefab, location * multiplier, Quaternion.identity, parentHallways);
        go.GetComponent<HallwayMaker>().makeRoomDoor = makeDoor;
        
    }
    private void PlaceRoom(Vector3Int location, Vector3Int sizeRoom, int num, int type) //type 0 - обычная room, 1 - player room, 2 - golden room, 3 - boss room
    {
        if (type == 3)
        {
            GameObject go = Instantiate(bossRoomsPrefab[num], multiplier * (location + sizeRoom / 2 ), Quaternion.identity, parentRooms);
            go.name = numRoom + " "+ go.name;
            Debug.Log(numRoom + " "+ go.name);
            spawnedRooms.Add(go);
        }
        else if (type == 2)
        {
            GameObject go = Instantiate(goldenRoomsPrefab[num], multiplier * (location + sizeRoom / 2 ), Quaternion.identity, parentRooms);
            go.name = numRoom +" GOLDEN ROOM "+ go.name;
            Debug.Log(numRoom + " "+ go.name);
            spawnedRooms.Add(go);
        }
        else if (type == 1)
        {
            GameObject go = Instantiate(playerRoomsPrefab[num], multiplier * (location + sizeRoom / 2 ), Quaternion.identity, parentRooms);
            go.name = numRoom +" PLAYER ROOM SPAWN "+ go.name;
            Debug.Log(numRoom + " "+ go.name);
            spawnedRooms.Add(go);
        }
        else if(type == 0)
        {
            GameObject go = Instantiate(roomsPrefab[num], multiplier * (location + sizeRoom / 2 ), Quaternion.identity, parentRooms);
            go.name = numRoom +" "+ go.name;
            Debug.Log(numRoom + " "+ go.name);
            spawnedRooms.Add(go);
        }
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var edge in selectedEdges)
        {
            Gizmos.DrawLine(edge.U.Position * multiplier + Vector3.up * 7, edge.V.Position * multiplier + Vector3.up * 7);
        }
        
        
        Gizmos.color = Color.red;
        foreach (var edge in delaunay.Edges)
        {
            Gizmos.DrawLine(edge.U.Position * multiplier + Vector3.up * 8, edge.V.Position * multiplier + Vector3.up * 8);
        }
    }
}
