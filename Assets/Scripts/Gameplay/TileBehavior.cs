using System.Collections;
using UnityEngine;
using static SnakeController;

public class TileBehavior : MonoBehaviour
{
    public enum TileType
    {
        Grass,
        GrassReal,
        Wall,
        Spicy,
        Banana,
        Exit
    }

    public TileType type;

    [Header("Only used if type == Exit")]
    public Sprite exitOpenSprite;

    public Sprite exitClosedSprite;

    private SpriteRenderer spriteRenderer;

    private SnakeController snake;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact(SnakeController snake)
    {
        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int pushDir = snake.GetDirection();
        Vector2Int checkPos = currentPos + pushDir;

        switch (type)
        {
            case TileType.Wall:
                // Wall just blocks
                break;

            case TileType.Banana:
                snake.SetFace(SnakeFace.Eating);
                StartCoroutine(FruitPushCheck());

                break;

            case TileType.Spicy:

                snake.SetFace(SnakeFace.Propelled);
                StartCoroutine(FruitPushCheck());
                StartCoroutine(snake.PropelSnakeForwardAsShape());

                break;

            case TileType.Exit:
                if (GameManager.Instance.CanExit && snake.IsHeadOnTile(currentPos))
                {
                    snake.SetFace(SnakeFace.Win);
                    GameManager.Instance.WinLevel();
                }
                break;

            case TileType.Grass:
            case TileType.GrassReal:
            default:
                // No special interaction
                break;
        }
    }

    public void SetExitState(bool open)
    {
        if (type != TileType.Exit || spriteRenderer == null) return;
        spriteRenderer.sprite = open ? exitOpenSprite : exitClosedSprite;
    }

    public bool IsFruit()
    {
        return type == TileType.Banana || type == TileType.Spicy;
    }

    public bool IsGround()
    {
        return type == TileType.Grass || type == TileType.GrassReal;
    }

    public IEnumerator FruitPushCheck()
    {
        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int pushDir = snake.GetDirection();
        Vector2Int checkPos = currentPos + pushDir;
        if (!LevelManager.Instance.IsInBounds(checkPos)) yield break;

        GameObject targetTileObj = LevelManager.Instance.GetTileObject(checkPos);
        TileBehavior targetTile = targetTileObj?.GetComponent<TileBehavior>();

        if (targetTile != null && targetTile.type == TileType.Wall)
        {
            // Consume if pushed into wall
            LevelManager.Instance.ClearTile(currentPos);
            if (type == TileType.Banana)
            {
                snake.Grow();
            }
            else if (type == TileType.Spicy)
            {
                snake.PropelSnakeForwardAsShape();
            }
        }
        else if (targetTile == null || targetTile.IsGround())
        {
            // Move fruit tile forward
            LevelManager.Instance.MoveTile(currentPos, checkPos);
        }
        yield break;
    }
}