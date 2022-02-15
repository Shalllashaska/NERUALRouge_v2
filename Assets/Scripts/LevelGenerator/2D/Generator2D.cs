using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator2D : MonoBehaviour {
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

        public static bool Intersect(Room a, Room b) {
            float ax = a.bounds.position.x - (a.bounds.size.x / 2);
            float ay = a.bounds.position.y - (a.bounds.size.y / 2);
            float bx = b.bounds.position.x - (b.bounds.size.x / 2);
            float by = b.bounds.position.y - (b.bounds.size.y / 2);
            return !((ax >= (bx + b.bounds.size.x)) || ((ax + a.bounds.size.x) <= bx)
                                                    || (ay >= (by + b.bounds.size.y)) || ((ay + a.bounds.size.y) <= by));
        }
    }

    [SerializeField]
    Vector2Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector2Int roomMaxSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    GameObject[] roomsPrefabs;
    
    private int[,] roomsSizes = new int[,]
    {
        {2,4},
        {4,2},
        {4,4},
        {4,6},
        {6,2},
        {6,4},
        {6,6},
    };
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;

    Random random;
    Grid2D<CellType> grid;
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    void Start() {
        Generate();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlaceRooms();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Triangulate();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            CreateHallways();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PathfindHallways();
        }
    }

    void Generate() {
        random = new Random(1);
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();
    }
    
    

    void PlaceRooms()
    {
        int numRoom = 1;
        for (int i = 0; i < roomCount; i++)
        {
            int curRooSizeNum = random.Next(0, roomsSizes.GetLength(0) - 1);
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x),
                random.Next(0, size.y)
            );

            Debug.Log(curRooSizeNum);
            Vector2Int roomSize = new Vector2Int(
                roomsSizes[curRooSizeNum,0],
                roomsSizes[curRooSizeNum,1]
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

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y) {
                add = false;
            }

            if (add) {
                rooms.Add(newRoom);
                Debug.Log("Room: "+ numRoom +", Position: " + newRoom.bounds.position + ", Size: " + newRoom.bounds.size);
                PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);
                numRoom++;
                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways() {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

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

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
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

    void PlaceCube(Vector2Int location, Vector2Int size, Material material) {
        
        for (int i = 0; i < roomsSizes.GetLength(0); i++)
        {
            if (size.x == roomsSizes[i, 0] && size.y == roomsSizes[i, 1])
            {
                GameObject go = Instantiate(roomsPrefabs[i], new Vector3(location.x, 0, location.y), Quaternion.identity);
                go.GetComponent<MeshRenderer>().material = material;
                return;
            }
        }
    }

    void PlaceRoom(Vector2Int location, Vector2Int size) {
        PlaceCube(location, size, redMaterial);
    }

    void PlaceHallway(Vector2Int location) {
        PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
    }
}
