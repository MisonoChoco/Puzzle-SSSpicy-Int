using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class SnakeController : MonoBehaviour
{
    public GameObject bodySegmentPrefab;
    public float moveDuration = 0.15f;

    private List<Transform> segments = new();
    private List<Vector2Int> previousPositions = new();
    private Vector2Int direction = Vector2Int.right;
    private Vector2Int lastCheckedTile;
    private bool growThisStep = false;
    private bool isMoving = false;
    public bool isPropelled = false;

    private Stack<SnakeState> undoStack = new Stack<SnakeState>();
    private bool isUndoing = false;

    public GameObject smokePrefab;

    public enum SnakeFace
    {
        Normal,
        Propelled,
        Eating,
        Dead,
        FruitFell,
        Win
    }

    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private Sprite normalFace;
    [SerializeField] private Sprite propelledFace;
    [SerializeField] private Sprite eatingFace;
    [SerializeField] private Sprite deadFace;
    [SerializeField] private Sprite fruitFell;
    [SerializeField] private Sprite winFace;

    private IEnumerator Start()
    {
        yield return null; // wait 1 frame

        SetFace(SnakeFace.Normal);
        InitializeSnake();
    }

    private void InitializeSnake()
    {
        // Clear any old segments (in case this is a restart)
        foreach (var seg in segments)
        {
            if (seg != null && seg != transform)
                Destroy(seg.gameObject);
        }

        segments.Clear();
        previousPositions.Clear();

        // Add head
        segments.Add(transform);
        Vector2Int headPos = Vector2Int.RoundToInt(transform.position);

        // Add body segment 1
        Vector2Int body1Pos = headPos - direction;
        GameObject body1 = Instantiate(bodySegmentPrefab, (Vector3Int)body1Pos, Quaternion.identity);
        body1.transform.SetParent(transform.parent);
        segments.Add(body1.transform);

        // Add tail segment 2
        Vector2Int tailPos = body1Pos - direction;
        GameObject tail = Instantiate(bodySegmentPrefab, (Vector3Int)tailPos, Quaternion.identity);
        tail.transform.SetParent(transform.parent);
        segments.Add(tail.transform);

        // Fill previousPositions to match segment count
        // So each segment has a correct position from the start
        previousPositions.Add(headPos);
        previousPositions.Add(body1Pos);
        previousPositions.Add(tailPos);

        // Instantly set segment positions
        for (int i = 1; i < segments.Count; i++)
        {
            Vector3 targetPos = new Vector3(previousPositions[i].x, previousPositions[i].y, segments[i].position.z);
            segments[i].position = targetPos;
        }
        for (int i = 1; i < segments.Count; i++) //TEST, no game logic here, just for visual update
        {
            Transform segment = segments[i];
            Vector3 pos = new Vector3(previousPositions[i].x, previousPositions[i].y, segment.position.z);

            segment.position = pos;
            segment.GetComponent<SnakeSegment>()?.UpdateSprite(segments, i); // optional sprite update
            Debug.Log($"[Init] Segment {i} position: {segments[i].position} | Target: {previousPositions[i]}");
        }

        UpdateSegmentSprites();
    }

    private void Update()
    {
        HandleInput();

        if (!isMoving)
            CheckCurrentTile();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            UndoMove();
        }
    }

    private void CheckCurrentTile()
    {
        Vector2Int currentGridPos = Vector2Int.RoundToInt(transform.position);
        if (currentGridPos == lastCheckedTile) return;
        lastCheckedTile = currentGridPos;

        GameObject tileObj = LevelManager.Instance.GetTileObject(currentGridPos);
        if (tileObj != null)
        {
            TileBehavior tile = tileObj.GetComponent<TileBehavior>();
            tile?.Interact(this);
        }
    }

    private void HandleInput()
    {
        if (isMoving) return; //only allow one input per move
        if (GameManager.Instance.InputLocked == true) return;

        Vector2Int inputDir = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) && direction != Vector2Int.down) inputDir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) && direction != Vector2Int.up) inputDir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) && direction != Vector2Int.right) inputDir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) && direction != Vector2Int.left) inputDir = Vector2Int.right;

        if (inputDir != Vector2Int.zero)
        {
            direction = inputDir;
            transform.rotation = Quaternion.Euler(0, 0, GetRotationAngle(direction));
            AttemptMove();
        }
    }

    public void ResetSnake(Vector2Int startPos, Vector2Int facingDir)
    {
        // Cancel all active tweens (important to avoid leftover motion)
        DOTween.KillAll();

        // Reset internal state
        direction = facingDir;
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0, 0, GetRotationAngle(facingDir));

        // Destroy all old segments except head
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Destroy(segments[i].gameObject);
            }
        }

        segments.Clear();
        previousPositions.Clear();

        segments.Add(transform);
        previousPositions.Add(startPos);

        // Recreate body segments
        Vector2Int body1Pos = startPos - direction;
        Vector2Int tailPos = body1Pos - direction;

        GameObject body1 = Instantiate(bodySegmentPrefab, (Vector3Int)body1Pos, Quaternion.identity);
        body1.transform.SetParent(transform.parent);
        segments.Add(body1.transform);
        previousPositions.Add(body1Pos);

        GameObject tail = Instantiate(bodySegmentPrefab, (Vector3Int)tailPos, Quaternion.identity);
        tail.transform.SetParent(transform.parent);
        segments.Add(tail.transform);
        previousPositions.Add(tailPos);

        // Force immediate position set (no snapping)
        for (int i = 1; i < segments.Count; i++)
        {
            Vector3 pos = new Vector3(previousPositions[i].x, previousPositions[i].y, transform.position.z);
            segments[i].position = pos;
            segments[i].rotation = Quaternion.identity;
        }

        UpdateSegmentSprites();
    }

    private float GetRotationAngle(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 0;
        if (dir == Vector2Int.right) return -90;
        if (dir == Vector2Int.down) return 180;
        if (dir == Vector2Int.left) return 90;
        return 0;
    }

    private void AttemptMove()
    {
        if (direction == Vector2Int.zero || isMoving)
        {
            Debug.LogWarning("Blocked: direction zero or already moving.");
            return;
        }

        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int targetPos = currentPos + direction;

        if (!LevelManager.Instance.IsInBounds(targetPos))
        {
            Debug.LogWarning($"Out of bounds: {targetPos}");
            //CheckFallDeath();
            return;
        }

        GameObject tileObj = LevelManager.Instance.GetTileObject(targetPos);
        TileBehavior tile = tileObj ? tileObj.GetComponent<TileBehavior>() : null;

        Debug.Log($"Tile at {targetPos}: {(tile != null ? tile.type.ToString() : "null")}");

        // Pushing fruit logic
        if (tile != null && tile.IsFruit())
        {
            Vector2Int pushPos = targetPos + direction;

            if (!LevelManager.Instance.IsInBounds(pushPos))
            {
                SetFace(SnakeFace.FruitFell);
                StartCoroutine(RestartRoutine()); // pushed out = lose
                return;
            }

            GameObject blockTileObj = LevelManager.Instance.GetTileObject(pushPos);
            TileBehavior blockTile = blockTileObj ? blockTileObj.GetComponent<TileBehavior>() : null;

            if (blockTile != null && blockTile.type == TileBehavior.TileType.Wall)
            {
                // Push into wall = consume
                if (tile.type == TileBehavior.TileType.Banana)
                    Grow();
                else if (tile.type == TileBehavior.TileType.Spicy)
                    StartCoroutine(PropelForward());

                LevelManager.Instance.ClearTile(targetPos);
            }
            else if (blockTile == null || blockTile.IsGround())
            {
                tile.transform.DOMove((Vector3Int)pushPos, moveDuration).SetEase(Ease.Linear);
                LevelManager.Instance.MoveTile(targetPos, pushPos);
                MoveTo(targetPos);
                return;
            }
            else
            {
                // Can't push
                isMoving = false;
                return;
            }
        }

        MoveTo(targetPos);
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        GameManager.Instance.RestartLevel();
        SetFace(SnakeFace.Normal);
        yield return null; // wait one frame to ensure everything is reset
    }

    private void MoveTo(Vector2Int targetPos, bool ignoreIsMoving = false)
    {
        if (isPropelled) return;
        if (!LevelManager.Instance.IsInBounds(targetPos)) return;

        if (isMoving && !ignoreIsMoving)
        {
            Debug.LogWarning("[MoveTo] Aborted: already moving.");
            return;
        }

        // WALL CHECK

        int tileID = LevelManager.Instance.GetTileID(targetPos);
        if (tileID == 4 && !ignoreIsMoving) // Wall
        {
            Debug.Log("[MoveTo] Blocked by wall at: " + targetPos);
            return;
        }

        // self movement check
        //for (int i = 0; i < segments.Count; i++)
        //{
        //    Vector2Int segmentPos = previousPositions[i];

        //    // Allow tail overlap if it's about to move
        //    bool isTail = i == segments.Count - 1;
        //    bool tailWillMove = !growThisStep;

        //    if (isTail && tailWillMove)
        //        continue;

        //    if (segmentPos == targetPos)
        //    {
        //        Debug.Log("[MoveTo] Blocked by self at: " + targetPos);
        //        return;
        //    }
        //}

        List<Vector2Int> savedPositions = new List<Vector2Int>(previousPositions);
        undoStack.Push(new SnakeState(previousPositions[0], savedPositions, growThisStep, isPropelled));

        isMoving = true;
        transform.DOKill();

        Vector3 destination = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        Debug.Log($"[MoveTo] Starting tween to {destination}");

        transform.DOMove(destination, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isMoving = false;
                Debug.Log($"[MoveTo] Completed move to {targetPos}");
                CheckCollision();
            });

        // Move body segments
        if (isPropelled) return;
        previousPositions.Insert(0, targetPos);

        for (int i = 1; i < segments.Count; i++)
        {
            segments[i].DOKill(); // Ensure no overlapping tweens
            Vector3 segTarget = new Vector3(previousPositions[i].x, previousPositions[i].y, segments[i].position.z);
            segments[i].DOMove(segTarget, moveDuration).SetEase(Ease.Linear);
        }

        if (growThisStep)
        {
            Vector2Int spawnPos = previousPositions[segments.Count];
            GameObject newSegment = Instantiate(bodySegmentPrefab, (Vector3Int)spawnPos, Quaternion.identity);
            segments.Add(newSegment.transform);
            growThisStep = false;
        }

        if (previousPositions.Count > segments.Count)
            previousPositions.RemoveAt(previousPositions.Count - 1);

        UpdateSegmentSprites();
    }

    private void UpdateSegmentSprites()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            SnakeSegment s = segments[i].GetComponent<SnakeSegment>();
            if (s != null)
                s.UpdateSprite(segments, i);
        }
    }

    public void SetFace(SnakeFace face)
    {
        switch (face)
        {
            case SnakeFace.Normal:
                headRenderer.sprite = normalFace;
                break;

            case SnakeFace.Propelled:
                headRenderer.sprite = propelledFace;
                break;

            case SnakeFace.Eating:
                headRenderer.sprite = eatingFace;
                break;

            case SnakeFace.Dead:
                headRenderer.sprite = deadFace;
                break;

            case SnakeFace.FruitFell:
                headRenderer.sprite = fruitFell;
                break;

            case SnakeFace.Win:
                headRenderer.sprite = winFace;
                break;
        }
    }

    private void UndoMove()
    {
        if (undoStack.Count == 0) return;

        isUndoing = true;

        SnakeState state = undoStack.Pop();

        transform.DOKill();
        foreach (var seg in segments)
            seg.DOKill();

        transform.position = new Vector3(state.headPosition.x, state.headPosition.y, transform.position.z);

        // Reuse or adjust body segments
        int requiredSegments = state.segmentPositions.Count - 1;

        // Add more if needed
        while (segments.Count < requiredSegments)
        {
            GameObject newSeg = Instantiate(bodySegmentPrefab);
            segments.Add(newSeg.transform);
        }

        // Hide extras if too many
        for (int i = 0; i < segments.Count; i++)
        {
            if (i < requiredSegments)
            {
                segments[i].gameObject.SetActive(true);
                Vector2Int segPos = state.segmentPositions[i + 1];
                segments[i].position = new Vector3(segPos.x, segPos.y, segments[i].position.z);
            }
            else
            {
                segments[i].gameObject.SetActive(false);
            }
        }

        previousPositions = new List<Vector2Int>(state.segmentPositions);
        growThisStep = state.growThisStep;
        isPropelled = state.isPropelled;

        UpdateSegmentSprites();
        StartCoroutine(ResumeAfterUndo());
    }

    private IEnumerator ResumeAfterUndo()
    {
        yield return null; // wait one frame
        isUndoing = false;
    }

    public void Grow()
    {
        SetFace(SnakeFace.Eating);
        growThisStep = true;
        StartCoroutine(WaitForNormalFaceChange());
    }

    public IEnumerator PropelSnakeForwardAsShape()
    {
        yield return new WaitForSeconds(0.1f);
        SetFace(SnakeFace.Propelled);
        StartCoroutine(WaitForNormalFaceChange());

        isMoving = true;
        isPropelled = true;
        GameManager.Instance.InputLocked = true;

        // Cache the full snake: head + segments
        List<Transform> allParts = new List<Transform> { transform };
        allParts.AddRange(segments);

        // Store current relative positions
        Vector2Int headPos = Vector2Int.RoundToInt(transform.position);
        List<Vector2Int> offsets = new List<Vector2Int>();

        foreach (var part in allParts)
        {
            Vector2Int offset = Vector2Int.RoundToInt(part.position) - headPos;
            offsets.Add(offset);
        }

        while (true)
        {
            Vector2Int newHeadPos = headPos + direction;

            // Check for wall or out of bounds for any part
            bool blocked = false;
            for (int i = 0; i < offsets.Count; i++)
            {
                Vector2Int checkPos = newHeadPos + offsets[i];
                if (!LevelManager.Instance.IsInBounds(checkPos) ||
                    LevelManager.Instance.GetTileID(checkPos) == 1) // Wall
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked) break;

            // --- Emit smoke at the last segment's current position ---
            if (segments.Count > 0 && smokePrefab != null)
            {
                Vector3 smokePos = segments[segments.Count - 1].position;
                GameObject smoke = Instantiate(smokePrefab, smokePos, Quaternion.identity);
                Destroy(smoke, 1f); // Optional: auto-destroy after 1 second
            }

            // Move everything immediately (no tween)
            for (int i = 0; i < allParts.Count; i++)
            {
                Vector2Int targetPos = newHeadPos + offsets[i];
                allParts[i].position = (Vector3Int)targetPos;
            }

            headPos = newHeadPos;

            yield return new WaitForSeconds(moveDuration);
        }

        isMoving = false;
        isPropelled = false;
        GameManager.Instance.InputLocked = false;
    }

    public void PropelBackward()
    {
        Vector2Int backPos = Vector2Int.RoundToInt(transform.position) - direction;
        transform.DOMove((Vector3Int)backPos, moveDuration).SetEase(Ease.OutBack);
    }

    public IEnumerator PropelForward()
    {
        yield return new WaitForSeconds(0.1f); // optional buffer

        isMoving = true;

        while (true)
        {
            Vector2Int pos = Vector2Int.RoundToInt(transform.position);
            Vector2Int nextPos = pos + -direction;

            // Check bounds
            if (!LevelManager.Instance.IsInBounds(nextPos))
                break;

            int tileID = LevelManager.Instance.GetTileID(nextPos);
            if (tileID == 4) // Wall
                break;

            MoveTo(nextPos, true); // Force through even if isMoving is true
            yield return new WaitUntil(() => !isMoving);
        }

        isMoving = false; // Unstick the snake for player input
    }

    private void CheckCollision()
    {
        Vector2Int pos = Vector2Int.RoundToInt(transform.position);
        GameObject tileGO = LevelManager.Instance.GetTileObject(pos);
        if (tileGO != null)
        {
            var tile = tileGO.GetComponent<TileBehavior>();
            tile?.Interact(this);
        }
    }

    private void CheckFallDeath()
    {
        // Delay this check slightly to ensure all movement completes
        SetFace(SnakeFace.Dead);
        StartCoroutine(CheckFallRoutine());
    }

    private IEnumerator CheckFallRoutine()
    {
        // Wait for all segments to finish moving
        yield return new WaitUntil(() => !isMoving);

        bool anyOnGround = false;

        foreach (var segment in segments)
        {
            Vector2Int pos = Vector2Int.RoundToInt(segment.position);
            GameObject tileObj = LevelManager.Instance.GetTileObject(pos);
            TileBehavior tile = tileObj ? tileObj.GetComponent<TileBehavior>() : null;

            if (tile != null && tile.IsGround())
            {
                anyOnGround = true;
                break;
            }
        }

        if (!anyOnGround)
        {
            GameManager.Instance.InputLocked = true;

            foreach (var segment in segments)
            {
                segment.transform
                    .DOMoveY(segment.transform.position.y - 10f, 0.8f)
                    .SetEase(Ease.InBack)
                    .SetId("SnakeFall");
            }

            // Wait for animation to finish
            yield return new WaitForSeconds(0.85f);

            // Clean up tweens
            DOTween.Kill("SnakeFall");

            // Delay one frame to ensure everything is cleaned
            yield return null;

            GameManager.Instance.InputLocked = false;
            StartCoroutine(RestartRoutine()); // Restart the level
        }
    }

    public Vector2Int GetDirection()
    {
        return direction;
    }

    public bool IsHeadOnTile(Vector2Int tilePos)
    {
        return Vector2Int.RoundToInt(transform.position) == tilePos;
    }

    public void Die()
    {
        SetFace(SnakeFace.Dead);
        Debug.Log("Snake died.");
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator WaitForNormalFaceChange()
    {
        yield return new WaitForSeconds(2f);
        SetFace(SnakeFace.Normal);
    }
}