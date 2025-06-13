using UnityEngine;

public class TileCollisionHandler : MonoBehaviour
{
    private SnakeController snake;
    private Vector2Int lastGridPos = Vector2Int.zero;

    private void Awake()
    {
        snake = GetComponent<SnakeController>();
    }

    private void LateUpdate()
    {
        Vector2Int gridPos = Vector2Int.RoundToInt(transform.position);

        // Prevent repeated triggering on the same tile
        if (gridPos == lastGridPos) return;

        lastGridPos = gridPos;

        int tileID = LevelManager.Instance.GetTileID(gridPos);

        switch (tileID)
        {
            case 2: // Spicy
                GameManager.Instance.LockInputTemporarily(0.5f);
                StartCoroutine(snake.PropelSnakeForwardAsShape());
                LevelManager.Instance.ClearTile(gridPos);
                break;

            case 3: // Banana
                snake.Grow();
                LevelManager.Instance.ClearTile(gridPos);
                break;

            case 4: // Exit
                if (ExitTileController.Instance.IsOpen)
                    GameManager.Instance.WinLevel();
                break;

            case 5: // Pit
                snake.Die();
                break;

                // case 1 (Wall) is handled directly in SnakeController.MoveTo
        }
    }
}