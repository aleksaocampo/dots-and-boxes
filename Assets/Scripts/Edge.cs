using UnityEngine;
using Unity.Netcode;

/**
EDGE SCRIPT:
- place edges on board
- keep track of ownership for player score
- visual updates refelcted on board
**/

public class Edge : NetworkBehaviour
{
    /** 
    all things relating to edges need to be network variables, so the row/column,
    who it is placed by, etc. these things need to be tracked over the network to accurately
    keep track of player score, and overall ensure game functionality
    **/
    public NetworkVariable<int> row = new NetworkVariable<int>(0);
    public NetworkVariable<int> col = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isHorizontal = new NetworkVariable<bool>(false);
    public NetworkVariable<int> placedBy = new NetworkVariable<int>(0);

    private bool clickProcessing = false;
    private int tempRow, tempCol;
    private bool tempIsHorizontal;

    public void Initialize(int row, int col, bool horizontal)
    {
        tempRow = row;
        tempCol = col;
        tempIsHorizontal = horizontal;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            row.Value = tempRow;
            col.Value = tempCol;
            isHorizontal.Value = tempIsHorizontal;
        }
    }

    private void Start()
    {
        // back when i was debugging, i had collider issues so i am leaving this here just incase
        // ensure collider exists for mouse input
        // if (GetComponent<Collider2D>() == null)
        // {
        //     BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        //     collider.size = new Vector2(0.3f, 0.3f); // reasonable size for clicking
        // }
    }

    private void Awake()
    {
        placedBy.OnValueChanged += OnPlacedByChanged;
    }

    private void OnDestroy()
    {
        placedBy.OnValueChanged -= OnPlacedByChanged;
    }

    private void OnPlacedByChanged(int prev, int current)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (!sr) return;

        Color player1Color;
        Color player2Color;

        // player 1 = red
        ColorUtility.TryParseHtmlString("#CE5959", out player1Color);
        // player 2 = blue
        ColorUtility.TryParseHtmlString("#BACDDB", out player2Color);

        switch (current)
        {
            case 0: sr.color = Color.white; break; // default = white
            case 1: sr.color = player1Color; break;
            case 2: sr.color = player2Color; break;
        }
    }

    // add an edge for corresponding player when mouse is clicked
    private void OnMouseDown()
    {
        if (clickProcessing) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (placedBy.Value != 0) return;

        clickProcessing = true;
        GameManager.Instance.RequestEdgePlacementServerRpc(row.Value, col.Value, isHorizontal.Value, NetworkManager.Singleton.LocalClientId);
        Invoke(nameof(ResetClickProcessing), 0.1f);
    }

    private void ResetClickProcessing()
    {
        clickProcessing = false;
    }
}