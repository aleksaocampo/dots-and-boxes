using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prefabs")]
    public GameObject dotPrefab;
    public GameObject horizontalEdgePrefab;
    public GameObject verticalEdgePrefab;
    public GameObject boxPrefab;

    [Header("Parents")]
    public Transform dotsParent;
    public Transform horizontalParent;
    public Transform verticalParent;
    public Transform boxesParent;

    private int[,] horizontalEdges = new int[4, 3];
    private int[,] verticalEdges = new int[3, 4];
    private int[,] boxes = new int[3, 3];

    private int currentPlayer = 1;
    private int player1Score = 0;
    private int player2Score = 0;

    private float spacing = 1.5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateBoard();
    }

    void CreateBoard()
    {
        // Create dots
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                Vector3 pos = new Vector3(c * spacing, -r * spacing, 0);
                Instantiate(dotPrefab, pos, Quaternion.identity, dotsParent);
            }
        }

        // Create horizontal edges
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Vector3 pos = new Vector3(c * spacing + spacing / 2f, -r * spacing, 0);
                GameObject edge = Instantiate(horizontalEdgePrefab, pos, Quaternion.identity, horizontalParent);
                edge.GetComponent<Edge>().Initialize(r, c, true);
            }
        }

        // Create vertical edges
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                Vector3 pos = new Vector3(c * spacing, -r * spacing - spacing / 2f, 0);
                GameObject edge = Instantiate(verticalEdgePrefab, pos, Quaternion.identity, verticalParent);
                edge.GetComponent<Edge>().Initialize(r, c, false);
            }
        }

        // Create box visuals
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Vector3 pos = new Vector3(c * spacing + spacing / 2f, -r * spacing - spacing / 2f, 1);
                Instantiate(boxPrefab, pos, Quaternion.identity, boxesParent);
            }
        }
    }

    public void TryPlaceEdge(Edge edge)
{
    // Place the edge
    if (edge.isHorizontal)
    {
        if (horizontalEdges[edge.row, edge.col] != 0)
            return;

        horizontalEdges[edge.row, edge.col] = currentPlayer;
    }
    else
    {
        if (verticalEdges[edge.row, edge.col] != 0)
            return;

        verticalEdges[edge.row, edge.col] = currentPlayer;
    }

    // Set edge color
    Color playerColor = currentPlayer == 1 ? Color.blue : Color.red;
    edge.SetColor(playerColor);

    // Check for **any box completed**
    bool anyBoxCompleted = false;

    if (edge.isHorizontal)
    {
        anyBoxCompleted |= CheckHorizontal(edge.row, edge.col);
    }
    else
    {
        anyBoxCompleted |= CheckVertical(edge.row, edge.col);
    }

    // Only switch turn if no box was completed
    if (!anyBoxCompleted)
        SwitchTurn();

    Debug.Log("P1: " + player1Score + " | P2: " + player2Score + " | Current: " + currentPlayer);
}

    void SwitchTurn()
    {
        currentPlayer = currentPlayer == 1 ? 2 : 1;
    }

    bool CheckHorizontal(int row, int col)
    {
        bool scored = false;

        if (row < 3)
            scored |= CheckBox(row, col);

        if (row > 0)
            scored |= CheckBox(row - 1, col);

        return scored;
    }

    bool CheckVertical(int row, int col)
    {
        bool scored = false;

        if (col < 3)
            scored |= CheckBox(row, col);

        if (col > 0)
            scored |= CheckBox(row, col - 1);

        return scored;
    }

    bool CheckBox(int row, int col)
{
    if (boxes[row, col] != 0)
        return false; // Already claimed

    int top = horizontalEdges[row, col];
    int bottom = horizontalEdges[row + 1, col];
    int left = verticalEdges[row, col];
    int right = verticalEdges[row, col + 1];

    if (top == 0 || bottom == 0 || left == 0 || right == 0)
        return false; // Not all edges filled

    // Check for same player
    if (top == bottom && bottom == left && left == right)
    {
        boxes[row, col] = top; // Assign box to that player

        if (top == 1) player1Score++;
        else player2Score++;

        return true; // Completed box → extra turn
    }

    return false; // Mixed colors → box incomplete
}
}