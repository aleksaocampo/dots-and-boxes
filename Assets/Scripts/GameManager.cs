using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/**
GAME MANAGER:
- creates the game board and centeres it in the screen
- overall game logic (place an edge, keep track of who completed
what boxes, updates score, etc.)
**/

public class GameManager : NetworkBehaviour
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

    public float spacing = 1.5f;

    // these arrays only exist on the server to track game state
    // regular arrays do not need to be synced across network
    public int[,] horizontalEdges = new int[4, 3];
    public int[,] verticalEdges = new int[3, 4];
    public int[,] boxes = new int[3, 3]; // 0 = unclaimed, 1 or 2 = player

    /**
    NETWORK VARIABLES:
    - automatically synchronized across all clients by Netcode
    - when the server changes value, updates are reflected to clients
    - use OnValueChanged to subscribe and react to changes
    **/
    public NetworkVariable<int> CurrentPlayer = new NetworkVariable<int>(1);
    public NetworkVariable<int> Player1Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> Player2Score = new NetworkVariable<int>(0);

    // used for finding edges in the board itself
    private Dictionary<ulong, int> clientToPlayerMap = new Dictionary<ulong, int>();
    private Dictionary<string, Edge> edgeCache = new Dictionary<string, Edge>();

    void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        // have to ensure the server is running first before creating the board
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // keep track of the number of players and assign player numbers
    private void OnClientConnected(ulong clientId)
    {
        int playerNumber = clientToPlayerMap.Count + 1;
        clientToPlayerMap[clientId] = playerNumber;
    }

    // create the board and center it in screen
    public void CreateBoard()
    {
        int rows = 4;
        int columns = 4;

        float offsetX = (columns - 1) * spacing / 2f;
        float offsetY = (rows - 1) * spacing / 2f;

        // create dots on board
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 dotPos = new Vector3(c * spacing - offsetX, -r * spacing + offsetY, -0.1f);
                var dot = Instantiate(dotPrefab, dotPos, Quaternion.identity, dotsParent);
                dot.GetComponent<SpriteRenderer>().sortingOrder = 1;
                dot.GetComponent<NetworkObject>().Spawn();
            }
        }

        // add horizontal edges on board
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns - 1; c++)
            {
                Vector3 pos = new Vector3(c * spacing + spacing / 2 - offsetX, -r * spacing + offsetY, 0);
                var edge = Instantiate(horizontalEdgePrefab, pos, Quaternion.identity, horizontalParent);
                edge.GetComponent<SpriteRenderer>().sortingOrder = 0;
                var edgeScript = edge.GetComponent<Edge>();
                edgeScript.Initialize(r, c, true);
                edge.GetComponent<NetworkObject>().Spawn();
                edgeCache[$"H_{r}_{c}"] = edgeScript;
            }
        }

        // add vertical edges on board
        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 pos = new Vector3(c * spacing - offsetX, -r * spacing - spacing / 2 + offsetY, 0);
                var edge = Instantiate(verticalEdgePrefab, pos, Quaternion.identity, verticalParent);
                edge.GetComponent<SpriteRenderer>().sortingOrder = 0;
                var edgeScript = edge.GetComponent<Edge>();
                edgeScript.Initialize(r, c, false);
                edge.GetComponent<NetworkObject>().Spawn();
                edgeCache[$"V_{r}_{c}"] = edgeScript;
            }
        }

        // create boxes
        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < columns - 1; c++)
            {
                Vector3 pos = new Vector3(c * spacing + spacing / 2 - offsetX, -r * spacing - spacing / 2 + offsetY, 1);
                var box = Instantiate(boxPrefab, pos, Quaternion.identity, boxesParent);
                box.GetComponent<SpriteRenderer>().color = Color.clear;
                box.GetComponent<SpriteRenderer>().sortingOrder = 0;
                box.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

    /**
    SERVER RPC (Remote Procedure Call):
    - server then changes NetworkVariables, which reflects on clients side
    - prevents cheating - all game logic runs on the authoritative server
    **/
    [ServerRpc(RequireOwnership = false)]
    public void RequestEdgePlacementServerRpc(int row, int col, bool isHorizontal, ulong clientId)
    {
        int player = CurrentPlayer.Value;

        // Validate the requesting client is the current player
        if (!clientToPlayerMap.ContainsKey(clientId)) return;

        int requestingPlayer = clientToPlayerMap[clientId];
        if (requestingPlayer != player) return;

        // check if edge is already taken and store who owns it
        if (isHorizontal)
        {
            if (horizontalEdges[row, col] != 0) return;
            horizontalEdges[row, col] = player;
        }
        else
        {
            if (verticalEdges[row, col] != 0) return;
            verticalEdges[row, col] = player;
        }

        Edge e = FindEdge(row, col, isHorizontal);
        if (e != null)
        {
            e.placedBy.Value = player;
        }

        // check for completed boxes
        int boxesCompleted = CheckForCompletedBoxes(row, col, isHorizontal, player);

        // switch turn only if no boxes completed (if box completed, player goes again)
        if (boxesCompleted == 0)
        {
            // server updates CurrentPlayer, all clients' UI reflects this
            CurrentPlayer.Value = player == 1 ? 2 : 1;
        }
    }

    // score depends on who completes box, and update accordingly
    private int CheckForCompletedBoxes(int row, int col, bool isHorizontal, int player)
{
    int boxesCompleted = 0;

    if (isHorizontal)
    {
        if (row > 0 && IsBoxComplete(row - 1, col, player))
        {
            boxes[row - 1, col] = player;
            boxesCompleted++;
            Debug.Log($"[GM] Player {player} completed box at {row-1},{col}");
        }
        if (row < 3 && IsBoxComplete(row, col, player))
        {
            boxes[row, col] = player;
            boxesCompleted++;
            Debug.Log($"[GM] Player {player} completed box at {row},{col}");
        }
    }
    else
    {
        if (col > 0 && IsBoxComplete(row, col - 1, player))
        {
            boxes[row, col - 1] = player;
            boxesCompleted++;
            Debug.Log($"[GM] Player {player} completed box at {row},{col-1}");
        }
        if (col < 3 && IsBoxComplete(row, col, player))
        {
            boxes[row, col] = player;
            boxesCompleted++;
            Debug.Log($"[GM] Player {player} completed box at {row},{col}");
        }
    }

    // update NetworkVariable scores
    // UIManager is subscribed to these variables and will update the UI when they change
    if (boxesCompleted > 0)
    {
        if (player == 1)
        {
            Player1Score.Value += boxesCompleted;
            // Debug.Log($"[GM] Player1Score updated: {Player1Score.Value}");
        }
        else
        {
            Player2Score.Value += boxesCompleted;
            // Debug.Log($"[GM] Player2Score updated: {Player2Score.Value}");
        }
    }

    return boxesCompleted;
}
    // will return true of a box is completed by a player
    private bool IsBoxComplete(int boxRow, int boxCol, int player)
    {
        if (horizontalEdges[boxRow, boxCol] != player) return false;
        if (horizontalEdges[boxRow + 1, boxCol] != player) return false;
        if (verticalEdges[boxRow, boxCol] != player) return false;
        if (verticalEdges[boxRow, boxCol + 1] != player) return false;
        return true;
    }

    private Edge FindEdge(int row, int col, bool isHorizontal)
    {
        string key = isHorizontal ? $"H_{row}_{col}" : $"V_{row}_{col}";
        edgeCache.TryGetValue(key, out Edge edge);
        return edge;
    }
}