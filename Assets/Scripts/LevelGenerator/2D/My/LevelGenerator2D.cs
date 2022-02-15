using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Random = System.Random;
using UnityEngine;
using Graphs;

public class LevelGenerator2D : MonoBehaviour
{
    
    enum CellType {
        None,
        Room,
        Hallway
    }
    
    class Room {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size) {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b)
        {
            float ax = a.bounds.position.x - (a.bounds.size.x / 2);
            float ay = a.bounds.position.y - (a.bounds.size.y / 2);
            float bx = b.bounds.position.x - (b.bounds.size.x / 2);
            float by = b.bounds.position.y - (b.bounds.size.y / 2);
            return !((ax >= (bx + b.bounds.size.x)) || ((ax + a.bounds.size.x) <= bx)
                || (ay >= (by + b.bounds.size.y)) || ((ay + a.bounds.size.y) <= by));
        }
    }

    public GameObject cubePrefab;
    public Transform parentBuffer;
    public Transform parentRooms;
    public Transform parentHallways;
    public Transform parentGrid1;
    public Transform parentGrid2;
    public Material greenMat;
    public GameObject[] roomsPrefabs;
    public string[] roomsSizes;
    
    [SerializeField]
    Vector2Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;
    [SerializeField]
    private Material hallMat;
    
    private Random random;
    private Grid2D<CellType> grid;
    private List<Room> rooms;
    private  Delaunay2D delaunay;
    private HashSet<Prim.Edge> selectedEdges;
    private int curRoomSize;
    private int curRoomSizeNum;
    private List<Vector2Int> anchers;
    int numRoom;

    private List<Prim.Edge> mstTest;
    
    void Start()
    {
        Generate();
    }

    
    void Update()
    {
        
    }
    
    void Generate()
    {
        numRoom = 1;
        random = new Random(756842);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();
        anchers =new List<Vector2Int>();

        PlaceRooms();
        
        Triangulate();
        CreateHallways();
        PathfindHallways();
        DrawGrid();
    }
    
    void PlaceRooms()
    {
       
        for (int i = 0; i < roomCount; i++)
        {
            curRoomSizeNum = random.Next(0, roomsSizes.GetLength(0) - 1);
            curRoomSize = int.Parse(roomsSizes[curRoomSizeNum]);
            Vector2Int roomSize = new Vector2Int(
                curRoomSize / 10,
                curRoomSize % 10
            );
            
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x),
                random.Next(0, size.y)
            );
            
           

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location, roomSize + new Vector2Int(2, 2));

            foreach (var room in rooms) {
                if (Room.Intersect(room, buffer)) {
                    add = false;
                    break;
                }
            }


            float ax = newRoom.bounds.position.x - (newRoom.bounds.size.x / 2);
            float ay = newRoom.bounds.position.y - (newRoom.bounds.size.y / 2);
            float ahx = ax + newRoom.bounds.size.x;
            float ahy = ay + newRoom.bounds.size.y;
            if (ax < 0 || ahx >= size.x || 
                ay < 0 || ahy >= size.y)
            {
                add = false;
            }

            if (add) {
                rooms.Add(newRoom);
                anchers.Add(location);
                Debug.Log("Room: "+ numRoom +", Position: " + newRoom.bounds.position + ", Size: " + newRoom.bounds.size);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size, curRoomSizeNum);
                PlaceCube(buffer.bounds.position, buffer.bounds.size, blueMaterial);
                numRoom++;
                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }
    
    private void DrawGrid()
    {
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.x; j++)
            {
                GameObject go = Instantiate(cubePrefab, new Vector3(i , 0, j), Quaternion.identity, parentGrid1);

                if (grid[i, j] == CellType.None)
                {
                    go.GetComponent<MeshRenderer>().material = greenMat;
                }
                else if(grid[i, j] == CellType.Room){
                    go.GetComponent<MeshRenderer>().material = redMaterial;
                }
                else if (grid[i, j] == CellType.Hallway){
                    go.GetComponent<MeshRenderer>().material = blueMaterial;
                }
            }
        }
    }
    
    
    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        mstTest = mst;
        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);
        
        foreach (var edge in remainingEdges) {
            if (random.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        foreach (var edge in selectedEdges) {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.position; //было не позишон, а центр в этой и след строке
            var endPosf = endRoom.bounds.position;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();
                
                pathCost.cost = Vector2Int.Distance(b.Position, endPos);    //heuristic

                if (grid[b.Position] == CellType.Room) {
                    pathCost.cost += 10;
                } else if (grid[b.Position] == CellType.None) {
                    pathCost.cost += 5;
                } else if (grid[b.Position] == CellType.Hallway) {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            });

            if (path != null) {
                for (int i = 0; i < path.Count; i++) {
                    var current = path[i];

                    if (grid[current] == CellType.None) {
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }
                }

                foreach (var pos in path) {
                    if (grid[pos] == CellType.Hallway) {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }
    
    void PlaceHallway(Vector2Int location) {
        PlaceCube2(location, new Vector2Int(1, 1), hallMat);
    }
    
    void PlaceCube(Vector2Int location, Vector2Int sizeCube, Material material)
    {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x , 0, location.y), Quaternion.identity, parentBuffer);
        go.GetComponent<MeshRenderer>().material = material;
        go.transform.localScale = new Vector3(sizeCube.x, 1, sizeCube.y);
    }
    void PlaceCube2(Vector2Int location, Vector2Int sizeCube, Material material)
    {
        
        
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x -0.5f, 0, location.y -0.5f), Quaternion.identity, parentHallways);
        go.GetComponent<MeshRenderer>().material = material;
        go.transform.localScale = new Vector3(sizeCube.x, 1, sizeCube.y);
    }
    void PlaceRoom(Vector2Int location, Vector2Int sizeRoom, int num)
    {
        GameObject go = Instantiate(roomsPrefabs[num], new Vector3(location.x, 0, location.y), Quaternion.identity, parentRooms);
        go.GetComponent<MeshRenderer>().material = redMaterial;
        go.name = numRoom + go.name;
        return;
    }

    private void OnDrawGizmos()
    {
        if (anchers.Count > 1)
        {
            foreach (Vector2Int point in anchers)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(new Vector3(point.x, 0, point.y), .5f);
            }

            // List<Delaunay2D.Triangle> triangles = delaunay.Triangles;
            //
            // foreach (var triangle in triangles)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawLine(new Vector3(triangle.A.Position.x, 2, triangle.A.Position.y), new Vector3(triangle.B.Position.x, 2, triangle.B.Position.y));
            //     Gizmos.DrawLine(new Vector3(triangle.B.Position.x, 2, triangle.B.Position.y), new Vector3(triangle.C.Position.x, 2, triangle.C.Position.y));
            //     Gizmos.DrawLine(new Vector3(triangle.C.Position.x, 2, triangle.C.Position.y), new Vector3(triangle.A.Position.x, 2, triangle.A.Position.y));
            // }

            foreach (var edge in selectedEdges)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(new Vector3(edge.U.Position.x , 2, edge.U.Position.y), new Vector3(edge.V.Position.x , 2, edge.V.Position.y));
            }
        }
    }
}
