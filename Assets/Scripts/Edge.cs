using UnityEngine;

public class Edge : MonoBehaviour
{
    public int row;
    public int col;
    public bool isHorizontal;
    private bool isPlaced = false;

    public void Initialize(int r, int c, bool horizontal)
    {
        row = r;
        col = c;
        isHorizontal = horizontal;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Raycast at the mouse position
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Debug.Log("Edge clicked! row=" + row + " col=" + col + " horizontal=" + isHorizontal);

                if (!isPlaced)
                {
                    GameManager.Instance.TryPlaceEdge(this);
                }
            }
        }
    }

    public void SetColor(Color color)
    {
        GetComponent<SpriteRenderer>().color = color;
        isPlaced = true;
    }
}