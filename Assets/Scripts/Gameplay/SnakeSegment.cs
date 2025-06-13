using System.Collections.Generic;
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    public SpriteRenderer Renderer;

    public Sprite straightVertical, straightHorizontal;
    public Sprite cornerTL, cornerTR, cornerBL, cornerBR;
    public Sprite tailSprite;

    public void UpdateSprite(List<Transform> segments, int index)
    {
        if (index == 0 || segments.Count < 2) return;

        // TAIL
        if (index == segments.Count - 1)
        {
            Renderer.sprite = tailSprite;

            Vector2Int tailDir = Vector2Int.RoundToInt(segments[index - 1].position - segments[index].position);

            // Since the tail tip faces right, rotate accordingly:
            if (tailDir == Vector2Int.up)
                transform.rotation = Quaternion.Euler(0, 0, -90);
            else if (tailDir == Vector2Int.right)
                transform.rotation = Quaternion.Euler(0, 0, 180);
            else if (tailDir == Vector2Int.down)
                transform.rotation = Quaternion.Euler(0, 0, 90);
            else if (tailDir == Vector2Int.left)
                transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                transform.rotation = Quaternion.identity;

            return;
        }

        Vector2Int prev = Vector2Int.RoundToInt(segments[index - 1].position - segments[index].position);
        Vector2Int next = Vector2Int.RoundToInt(segments[index + 1].position - segments[index].position);

        if (prev == next)
        {
            // Straight segment
            if (prev.x != 0)
            {
                Renderer.sprite = straightHorizontal;
                transform.rotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                Renderer.sprite = straightVertical;
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
        else
        {
            // Corner segment
            if ((prev == Vector2Int.up && next == Vector2Int.right) || (prev == Vector2Int.right && next == Vector2Int.up))
            {
                Renderer.sprite = cornerTR;
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if ((prev == Vector2Int.up && next == Vector2Int.left) || (prev == Vector2Int.left && next == Vector2Int.up))
            {
                Renderer.sprite = cornerTL;
                transform.rotation = Quaternion.Euler(0, 0, 90);
            }
            else if ((prev == Vector2Int.down && next == Vector2Int.right) || (prev == Vector2Int.right && next == Vector2Int.down))
            {
                Renderer.sprite = cornerBR;
                transform.rotation = Quaternion.Euler(0, 0, -90);
            }
            else if ((prev == Vector2Int.down && next == Vector2Int.left) || (prev == Vector2Int.left && next == Vector2Int.down))
            {
                Renderer.sprite = cornerBL;
                transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            else
            {
                // Unexpected fallback
                Renderer.sprite = straightHorizontal;
                transform.rotation = Quaternion.identity;
            }
        }
    }

    private float GetRotationAngle(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 0;
        if (dir == Vector2Int.right) return -90;
        if (dir == Vector2Int.down) return 180;
        if (dir == Vector2Int.left) return 90;
        return 0;
    }
}