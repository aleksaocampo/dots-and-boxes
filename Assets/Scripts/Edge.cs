// Edge.cs
using UnityEngine;
using Unity.Netcode;

public class Edge : NetworkBehaviour
{
    public NetworkVariable<int> row = new NetworkVariable<int>(0);
    public NetworkVariable<int> col = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isHorizontal = new NetworkVariable<bool>(false);
    public NetworkVariable<int> placedBy = new NetworkVariable<int>(0);

    private bool clickProcessing = false;
    private int tempRow, tempCol;
    private bool tempIsHorizontal;

    public void Initialize(int r, int c, bool horizontal)
    {
        tempRow = r;
        tempCol = c;
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
        // Ensure collider exists for mouse input
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.3f, 0.3f); // Reasonable size for clicking
        }
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

        switch (current)
        {
            case 0: sr.color = Color.white; break;
            case 1: sr.color = Color.blue; break;
            case 2: sr.color = Color.red; break;
        }
    }

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