using UnityEngine;
using DG.Tweening;

public class SnakeMovementTest : MonoBehaviour
{
    public float moveDuration = 0.15f;
    private Vector2Int direction = Vector2Int.right;
    private bool isMoving = false;

    private void Update()
    {
        if (isMoving) return;

        Vector2Int inputDir = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W)) inputDir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S)) inputDir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A)) inputDir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) inputDir = Vector2Int.right;

        if (inputDir != Vector2Int.zero)
        {
            direction = inputDir;
            transform.rotation = Quaternion.Euler(0, 0, GetRotationAngle(direction));
            Move();
        }
    }

    private void Move()
    {
        isMoving = true;

        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int targetPos = currentPos + direction;
        Vector3 targetWorld = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        transform.DOMove(targetWorld, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log($"Moved to {targetPos}");
                isMoving = false;
            });
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