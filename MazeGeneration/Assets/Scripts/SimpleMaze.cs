using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public struct Cell
{
    public bool maze;
    public bool[] directions;

    public Cell(bool inMaze, int size)
    {
        maze = inMaze;
        directions = new bool[size];
    }
}

public class SimpleMaze : MonoBehaviour
{
    //singleton
    public static SimpleMaze Instance { get; private set; }

    /*  constants    */
    private const int WIDTH = 10, HEIGHT = 10;
    private const float GRID_LAYER = 0f;

    /*  public variables    */
    public List<Vector2> locations; //maze locations
    public Cell[,] cells; //grid cells that are set as maze/normal
    public Vector2 start, end; //starting/ending location for the maze
    public List<GameObject> grid; //represents grid game objects
    [HideInInspector]
    public Vector3[] neighbors; //each array entry represents a direction for a possible neighboring cell
    //[HideInInspector]
    public float verticalOffset, horizontalOffset; //offsets for generating grid squares
    public Color normalColor, startColor, mazeColor, highlightColor; //colors set in editor for maze
    public bool foundEnd; //found an end tile?
    public GameObject mouseCircle; //debug tool

    /*   UI   */
    public Slider sizeSlider;

    /*   prefabs   */
    public GameObject grid_normal, grid_maze;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
        {
            GenerateGrid((int)sizeSlider.value, (int)sizeSlider.value);
        }

        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseCircle.transform.position = new Vector3(mouse.x, mouse.y, 0);

        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Camera.main.orthographicSize = ((int)sizeSlider.value / 2) + 2;
        verticalOffset = Camera.main.orthographicSize;
        horizontalOffset = verticalOffset * (Screen.width / Screen.height);

        int x = (int)pos.x + (int)(horizontalOffset);
        int y = (int)pos.y + (int)(verticalOffset);

        //mouse click is in board bounds
        if (x >= 0 && x < (int)(sizeSlider.value) && y >= 0 && y < (int)(sizeSlider.value))
            DrawCell(x, y);
    }

    void DrawCell(int x, int y)
    {
        for (int i = 0; i < grid.Count; i++)
        {
            int dx = (int)(grid[i].transform.position.x) + (int)(horizontalOffset);
            int dy = (int)(grid[i].transform.position.y) + (int)(verticalOffset);

            if (x == dx && y == dy &&
                grid[i].GetComponent<SpriteRenderer>().color != highlightColor &&
                grid[i].GetComponent<SpriteRenderer>().color != startColor)
            {
                Debug.Log(("DREW CELL AT GRID INT: (" + x +  ", " + y + ")"));
                grid[i].transform.GetComponent<SpriteRenderer>().color = highlightColor;
            }
        }
    }

    #region INIT
    public void Init()
    {
        //setup first cell
        start = new Vector2(UnityEngine.Random.Range(0, (int)sizeSlider.value), 0);
        foundEnd = false;

        //represents the direction of each cell and its array pos in this order: right, up, down, left
        neighbors = new Vector3[]{ new Vector3(1, 0, 0), new Vector3(0, 1, 1), new Vector3(0, -1, 2), new Vector3(-1, 0, 3)};

        locations = new List<Vector2> { start };

        cells = new Cell[(int)sizeSlider.value, (int)sizeSlider.value];

        for (int x = 0; x < (int)sizeSlider.value; x++)
        {
            for (int y = 0; y < (int)sizeSlider.value; y++)
            {
                cells[x, y] = new Cell(false, 4);
            }
        }

        cells[(int)start.x, (int)start.y].maze = true;
        cells[(int)start.x, (int)start.y].directions = new bool[] { false, false, true, false };

        //grid
        grid = new List<GameObject>();

        GenerateMaze();
        GenerateGrid((int)sizeSlider.value, (int)sizeSlider.value);
    }
    public void Restart()
    {
        //setup first cell
        start = new Vector2(UnityEngine.Random.Range(0, (int)sizeSlider.value), 0);
        locations.Clear();
        locations.Add(start);
        foundEnd = false;

        cells = new Cell[(int)sizeSlider.value, (int)sizeSlider.value];

        for (int x = 0; x < (int)sizeSlider.value; x++)
        {
            for (int y = 0; y < (int)sizeSlider.value; y++)
            {
                cells[x, y] = new Cell(false, 4);
            }
        }

        cells[(int)start.x, (int)start.y].maze = true;
        cells[(int)start.x, (int)start.y].directions = new bool[] { false, false, true, false };

        ClearGrid();
    }
    #endregion

    #region UI
    public void RandomizeButton()
    {
        Restart();
        GenerateMaze();
        GenerateGrid((int)sizeSlider.value, (int)sizeSlider.value);
    }
    public void StepButton()
    {
        GenerateMazeStep();
    }
    public void OnSizeChanged()
    {
        Restart();
        GenerateMaze();
        GenerateGrid((int)sizeSlider.value, (int)sizeSlider.value);
    }
    #endregion

    #region Maze Generation
    void GenerateMaze()
    {
        while (locations.Count > 0)
        {
            //first, select most recent cell
            int top = locations.Count - 1;
            Vector2 curr = locations[top];

            //then grab a valid neighbor of the current cell
            Vector2 next = makeConnection(curr);

            //then set the neighbor as the new current cell for the next iteration
            if (next != null && next != Vector2.zero)
                locations.Add(next);
            //if all neighboring tiles are taken then pop the list back until we arrive at a cell with a valid neighbor and register end cell
            else
            {
                //finds a random end cell and sets the direction so its an exit
                if (foundEnd == false)
                    foundEnd = FindEnd(top);

                locations.RemoveAt(top);
            }
        }

        //basic method to generate an end cell, replaced with a method that generates a more random end cell
        //end = new Vector2(UnityEngine.Random.Range(0, WIDTH), HEIGHT - 1);
        //cells[(int)end.x, (int)end.y].directions[1] = true;
    }
    //finds a random end cell and sets the direction so its an exit
    bool FindEnd(int top)
    {
        if ((int)locations[top].x == 0)
        {
            cells[(int)locations[top].x, (int)locations[top].y].directions[3] = true;
            end = locations[top];
            return true;
        }
        else if ((int)locations[top].x == (int)sizeSlider.value - 1)
        {
            cells[(int)locations[top].x, (int)locations[top].y].directions[0] = true;
            end = locations[top];
            return true;
        }
        else if ((int)locations[top].y == 0)
        {
            cells[(int)locations[top].x, (int)locations[top].y].directions[2] = true;
            end = locations[top];
            return true;
        }
        else if ((int)locations[top].y == (int)sizeSlider.value - 1)
        {
            cells[(int)locations[top].x, (int)locations[top].y].directions[1] = true;
            end = locations[top];
            return true;
        }

        return false;
    }
    void GenerateMazeStep()
    {
        if (locations.Count > 0)
        {
            //first, select most recent cell
            int top = locations.Count - 1;
            Vector2 curr = locations[top];

            //then grab a valid neighbor of the current cell
            Vector2 next = makeConnection(curr);

            //then set the neighbor as the new current cell for the next iteration
            if (next != null && next != Vector2.zero)
                locations.Add(next);
            //if all neighboring tiles are taken then pop the list back until we arrive at a cell with a valid neighbor
            else
                locations.RemoveAt(top);

            if (foundEnd == false)
                foundEnd = FindEnd(top);
        }
        else
        {
            Restart();
            GenerateMazeStep();
        }

        //refresh grid
        GenerateGrid((int)sizeSlider.value, (int)sizeSlider.value);
    }
    bool canPlace(int x, int y, int dir)
    {
        return (0 <= x && x < (int)sizeSlider.value &&
            0 <= y && y < (int)sizeSlider.value &&
            cells[x, y].maze == false && 
            cells[x, y].directions[dir] == false);
    }
    //return a random and valid neighboring cell from a given location
    Vector2 makeConnection(Vector2 location)
    {
        //Debug.Log("started shuffle!");
        neighbors = ShuffleNeighbors();
        //Debug.Log("ended shuffle!");

        for (int i = 0; i < neighbors.Length; i++)
        {
            int x = (int)location.x + (int)neighbors[i].x;
            int y = (int)location.y + (int)neighbors[i].y;
            int fromDir = 3 - (int)neighbors[i].z;

            if (canPlace(x, y, fromDir))
            {
                cells[x, y].directions[fromDir] = true;
                cells[(int)location.x, (int)location.y].directions[(int)neighbors[i].z] = true;
                cells[x, y].maze = true;
                return new Vector2 (x, y);
            }
        }

        Debug.Log("Returned NULL for makeConnection()");
        return Vector2.zero;
    }
    Vector3[] ShuffleNeighbors()
    {
        //GUID (global unique identifier) will generate a unique identifier for each array and then order it based on those identifiers, effectively randomizing ever item in the array
        //return neighbors.OrderBy(a => Guid.NewGuid().ToString()).ToArray();

        //more efficient method to randomize array using the fisher-yates algorithm
        Vector3[] newNeighbors = neighbors;
        int count = newNeighbors.Length;
        while (count > 1)
        {
            System.Random r = new System.Random();
            int i = r.Next(count--);
            (newNeighbors[i], newNeighbors[count]) = (newNeighbors[count], newNeighbors[i]);
        }
        return newNeighbors;
    }
    #endregion

    #region Grid Generation
    //draw grid with cell data
    public void GenerateGrid(int rows, int cols)
    {
        ClearGrid();

        //grid offset based on camera size
        Camera.main.orthographicSize = (rows / 2) + 2;
        verticalOffset = (int)Camera.main.orthographicSize;
        horizontalOffset = verticalOffset * (Screen.width / Screen.height);

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if ((x == (int)end.x && y == (int)end.y) ||
                    (x == (int)start.x && y == (int)start.y))
                    DrawGrid(startColor, x, y);
                else if (cells[x, y].maze)
                    DrawGrid(mazeColor, x, y);
                else
                    DrawGrid(normalColor, x, y);
            }
        }


    }
    private void ClearGrid()
    {
        for (int i = 0; i < grid.Count; i++)
            Destroy(grid[i]);

        grid.Clear();
    }
    public void DrawGrid(Color col, int x, int y)
    {
        GameObject go = Instantiate(grid_normal);

        go.transform.SetParent(GameObject.Find("Grid").transform);
        go.transform.GetComponent<SpriteRenderer>().color = col;
        go.transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);

        for (int i = 0; i < cells[x,y].directions.Length; i++)
        {
            if (cells[x, y].directions[i])
            {
                go.transform.GetChild(i).transform.position = new Vector3(x - horizontalOffset, y - verticalOffset, GRID_LAYER);
                go.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        grid.Add(go);
    }
    #endregion
}