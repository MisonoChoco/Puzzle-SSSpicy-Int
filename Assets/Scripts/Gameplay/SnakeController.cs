using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct SnakeSnapshot
{
    public List<Vector2Int> segmentPositions;
    public int segmentCount;
    public SnakeController.SnakeFace face;
    public List<LevelObjectRestore> restoredObjects;
}

[System.Serializable]
public struct LevelObjectRestore
{
    public Vector2Int position;
    public int objectID;
}

public class SnakeController : MonoBehaviour
{
    [Header("Snake Settings")]
    public GameObject bodySegmentPrefab;

    public float moveDuration = 0.15f;

    [Header("Effects")]
    public GameObject smokePrefab;

    public GameObject propelledEffectPrefab;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip deathClip;
    public AudioClip eatClip;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer headRenderer;

    [SerializeField] private Sprite normalFace;
    [SerializeField] private Sprite propelledFace;
    [SerializeField] private Sprite eatingFace;
    [SerializeField] private Sprite deadFace;
    [SerializeField] private Sprite fruitFell;
    [SerializeField] private Sprite winFace;

    // Snake state
    private List<Transform> segments = new();

    private List<Vector2Int> segmentPositions = new();
    private Vector2Int direction = Vector2Int.right;
    private Vector2Int lastCheckedTile;
    private bool growThisStep = false;
    private bool isMoving = false;
    private bool isPropelled = false;
    private bool isUndoing = false;
    private SnakeFace currentFace = SnakeFace.Normal;

    // Undo system
    private Stack<SnakeState> undoStack = new Stack<SnakeState>();

    private const int MAX_UNDO_STATES = 50;

    // Effects
    private GameObject activePropelledEffect;

    public enum SnakeFace
    {
        Normal,
        Propelled,
        Eating,
        Dead,
        FruitFell,
        Win
    }

    #region Initialization

    private IEnumerator Start()
    {
        yield return null; // Wait 1 frame
        SetFace(SnakeFace.Normal);
        InitializeSnake();
    }

    private void InitializeSnake()
    {
        // Clear old segments
        ClearAllSegments();

        // Reset state
        segments.Clear();
        segmentPositions.Clear();
        undoStack.Clear();

        // Add head
        segments.Add(transform);
        Vector2Int headPos = Vector2Int.RoundToInt(transform.position);
        segmentPositions.Add(headPos);

        // Create initial body segments
        CreateBodySegment(headPos - direction);
        CreateBodySegment(headPos - (direction * 2));

        // Set initial positions instantly
        UpdateAllSegmentPositions(true);
        UpdateSegmentSprites();
    }

    private void CreateBodySegment(Vector2Int position)
    {
        GameObject newSegment = Instantiate(bodySegmentPrefab, (Vector3Int)position, Quaternion.identity);
        newSegment.transform.SetParent(transform.parent);
        segments.Add(newSegment.transform);
        segmentPositions.Add(position);
    }

    private void ClearAllSegments()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null)
                Destroy(segments[i].gameObject);
        }
    }

    #endregion Initialization

    #region Update and Input

    private void Update()
    {
        HandleInput();

        if (!isMoving && !isUndoing)
            CheckCurrentTile();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            TryUndo();
        }
    }

    private void OnEnable()
    {
        UndoSystem.OnUndoPressed += TryUndo;
    }

    private void OnDisable()
    {
        UndoSystem.OnUndoPressed -= TryUndo;
    }

    private void HandleInput()
    {
        if (ShouldBlockInput()) return;

        Vector2Int inputDir = GetInputDirection();

        if (inputDir != Vector2Int.zero)
        {
            ChangeDirection(inputDir);
            AttemptMove();
        }
    }

    private bool ShouldBlockInput()
    {
        return isMoving || isUndoing || isPropelled || GameManager.Instance.InputLocked;
    }

    private Vector2Int GetInputDirection()
    {
        if (Input.GetKeyDown(KeyCode.W) && direction != Vector2Int.down) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) && direction != Vector2Int.up) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) && direction != Vector2Int.right) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) && direction != Vector2Int.left) return Vector2Int.right;
        return Vector2Int.zero;
    }

    private void ChangeDirection(Vector2Int newDirection)
    {
        direction = newDirection;
        transform.rotation = Quaternion.Euler(0, 0, GetRotationAngle(direction));
    }

    private float GetRotationAngle(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 0;
        if (dir == Vector2Int.right) return -90;
        if (dir == Vector2Int.down) return 180;
        if (dir == Vector2Int.left) return 90;
        return 0;
    }

    #endregion Update and Input

    #region Movement System

    private void AttemptMove()
    {
        if (isMoving) return;

        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int targetPos = currentPos + direction;

        // Handle out of bounds
        if (!LevelManager.Instance.IsInBounds(targetPos))
        {
            GameManager.Instance.InputLocked = true;
            MoveTo(targetPos, true); // Force move to trigger death
            return;
        }

        // Handle fruit pushing
        if (TryPushFruit(targetPos)) return;

        // Regular movement
        MoveTo(targetPos);
    }

    private bool TryPushFruit(Vector2Int targetPos)
    {
        GameObject tileObj = LevelManager.Instance.GetTileObject(targetPos);
        TileBehavior tile = tileObj ? tileObj.GetComponent<TileBehavior>() : null;

        if (tile == null || !tile.IsFruit()) return false;

        Vector2Int pushPos = targetPos + direction;

        // Check if fruit falls off map
        if (!LevelManager.Instance.IsInBounds(pushPos))
        {
            SetFace(SnakeFace.FruitFell);
            GameManager.Instance.InputLocked = true;
            StartCoroutine(RestartRoutine());
            return true;
        }

        GameObject pushTileObj = LevelManager.Instance.GetTileObject(pushPos);
        TileBehavior pushTile = pushTileObj ? pushTileObj.GetComponent<TileBehavior>() : null;

        // Push into wall = consume
        if (pushTile != null && pushTile.type == TileBehavior.TileType.Wall)
        {
            ConsumeFruit(tile.type);
            LevelManager.Instance.ClearTile(targetPos);
            MoveTo(targetPos);
            return true;
        }

        // Push into empty space
        if (pushTile == null || pushTile.IsGround())
        {
            tile.transform.DOMove((Vector3Int)pushPos, moveDuration).SetEase(Ease.Linear);
            LevelManager.Instance.MoveTile(targetPos, pushPos);
            MoveTo(targetPos);
            return true;
        }

        // Can't push - blocked
        return true; // Block movement
    }

    private void ConsumeFruit(TileBehavior.TileType fruitType)
    {
        if (fruitType == TileBehavior.TileType.Banana)
            Grow();
        else if (fruitType == TileBehavior.TileType.Spicy)
            StartCoroutine(PropelSnakeForwardAsShape());
    }

    private void MoveTo(Vector2Int targetPos, bool forceMove = false)
    {
        if (isPropelled) return;
        if (isMoving && !forceMove) return;

        // Validation checks (skip if forcing move)
        if (!forceMove)
        {
            if (!IsValidMove(targetPos)) return;
        }

        // Save state for undo before making changes
        SaveCurrentState();

        // Execute movement
        ExecuteMovement(targetPos);
    }

    private bool IsValidMove(Vector2Int targetPos)
    {
        // Wall check
        if (LevelManager.Instance.IsInBounds(targetPos))
        {
            int tileID = LevelManager.Instance.GetTileID(targetPos);
            if (tileID == 4) return false; // Wall
        }

        // Self-collision check
        for (int i = 0; i < segmentPositions.Count; i++)
        {
            bool isTail = i == segmentPositions.Count - 1;
            bool tailWillMove = !growThisStep;

            if (isTail && tailWillMove) continue;

            if (segmentPositions[i] == targetPos) return false;
        }

        return true;
    }

    private void ExecuteMovement(Vector2Int targetPos)
    {
        isMoving = true;

        // Move head
        transform.DOMove(new Vector3(targetPos.x, targetPos.y, transform.position.z), moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(OnMovementComplete);

        // Update positions list
        segmentPositions.Insert(0, targetPos);

        // Move body segments
        MoveBodySegments();

        // Handle growth
        if (growThisStep)
        {
            AddNewSegment();
            growThisStep = false;
        }
        else
        {
            // Remove tail position
            if (segmentPositions.Count > segments.Count)
                segmentPositions.RemoveAt(segmentPositions.Count - 1);
        }

        UpdateSegmentSprites();
    }

    private void MoveBodySegments()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (i < segmentPositions.Count)
            {
                Vector3 targetPos = new Vector3(segmentPositions[i].x, segmentPositions[i].y, segments[i].position.z);
                segments[i].DOMove(targetPos, moveDuration).SetEase(Ease.Linear);
            }
        }
    }

    private void AddNewSegment()
    {
        if (segmentPositions.Count > segments.Count)
        {
            Vector2Int newSegPos = segmentPositions[segments.Count];
            CreateBodySegment(newSegPos);
        }
    }

    private void OnMovementComplete()
    {
        isMoving = false;

        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);

        // Check if out of bounds
        if (!LevelManager.Instance.IsInBounds(currentPos))
        {
            Die();
            return;
        }

        CheckCollision();
    }

    private void UpdateAllSegmentPositions(bool instant = false)
    {
        for (int i = 0; i < segments.Count && i < segmentPositions.Count; i++)
        {
            Vector3 targetPos = new Vector3(segmentPositions[i].x, segmentPositions[i].y, segments[i].position.z);

            if (instant)
                segments[i].position = targetPos;
            else
                segments[i].DOMove(targetPos, moveDuration).SetEase(Ease.Linear);
        }
    }

    #endregion Movement System

    #region Undo System

    private void SaveCurrentState()
    {
        Vector2Int headPos = Vector2Int.RoundToInt(transform.position);
        List<LevelObjectRestore> affectedObjects = new List<LevelObjectRestore>();

        // Save current state
        SnakeState state = new SnakeState(
            headPos,
            new List<Vector2Int>(segmentPositions),
            direction,
            growThisStep,
            isPropelled,
            currentFace,
            affectedObjects
        );

        undoStack.Push(state);

        // Limit undo stack size
        if (undoStack.Count > MAX_UNDO_STATES)
        {
            var temp = undoStack.ToList();
            temp.RemoveAt(0); // Remove oldest
            undoStack.Clear();
            foreach (var s in temp)
                undoStack.Push(s);
        }
    }

    private void TryUndo()
    {
        if (undoStack.Count == 0 || ShouldBlockUndo()) return;

        StartCoroutine(ExecuteUndo());
    }

    private bool ShouldBlockUndo()
    {
        return isMoving || isPropelled || isUndoing || GameManager.Instance.InputLocked;
    }

    private IEnumerator ExecuteUndo()
    {
        isUndoing = true;
        GameManager.Instance.InputLocked = true;

        // Kill all tweens
        DOTween.KillAll();

        // Get previous state
        SnakeState previousState = undoStack.Pop();

        // Restore head
        transform.position = new Vector3(previousState.headPosition.x, previousState.headPosition.y, transform.position.z);
        direction = previousState.direction;
        transform.rotation = Quaternion.Euler(0, 0, GetRotationAngle(direction));

        // Restore face
        SetFace(previousState.face);

        // Restore segment positions
        segmentPositions = new List<Vector2Int>(previousState.segmentPositions);

        // Adjust segment count
        AdjustSegmentCount(previousState.segmentPositions.Count);

        // Restore other state
        growThisStep = previousState.growThisStep;
        isPropelled = previousState.isPropelled;

        // Position segments instantly
        UpdateAllSegmentPositions(true);
        UpdateSegmentSprites();

        yield return new WaitForEndOfFrame();

        isUndoing = false;
        GameManager.Instance.InputLocked = false;
    }

    private void AdjustSegmentCount(int targetCount)
    {
        // Remove excess segments
        while (segments.Count > targetCount)
        {
            int lastIndex = segments.Count - 1;
            if (segments[lastIndex] != null && segments[lastIndex] != transform)
            {
                Destroy(segments[lastIndex].gameObject);
            }
            segments.RemoveAt(lastIndex);
        }

        // Add missing segments
        while (segments.Count < targetCount)
        {
            Vector2Int pos = segmentPositions[segments.Count];
            CreateBodySegment(pos);
        }
    }

    public void ClearUndoStack()
    {
        undoStack.Clear();
    }

    #endregion Undo System

    #region Special Abilities

    public void Grow()
    {
        SetFace(SnakeFace.Eating);
        growThisStep = true;
        PlaySound(eatClip);
        StartCoroutine(ResetFaceAfterDelay(2f));
    }

    public IEnumerator PropelSnakeForwardAsShape()
    {
        yield return new WaitForSeconds(0.1f);
        SetFace(SnakeFace.Propelled);
        StartCoroutine(ResetFaceAfterDelay(2f));

        isMoving = true;
        isPropelled = true;
        GameManager.Instance.InputLocked = true;

        // Cache all parts and their relative positions
        List<Transform> allParts = new List<Transform>(segments);
        Vector2Int headPos = Vector2Int.RoundToInt(transform.position);
        List<Vector2Int> offsets = new List<Vector2Int>();

        foreach (var part in allParts)
        {
            Vector2Int offset = Vector2Int.RoundToInt(part.position) - headPos;
            offsets.Add(offset);
        }

        Vector2Int propelDirection = -direction; // Move backward
        bool isFirstMove = true;

        // Propel until blocked
        while (true)
        {
            Vector2Int nextHeadPos = headPos + propelDirection;
            bool blocked = false;

            // Check if any part will hit a wall
            for (int i = 0; i < offsets.Count; i++)
            {
                Vector2Int nextPos = nextHeadPos + offsets[i];
                if (LevelManager.Instance.GetTileID(nextPos) == 4) // Wall
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked) break;

            // First move effects
            if (isFirstMove)
            {
                SpawnPropelledEffect();
                isFirstMove = false;
            }

            EmitSmoke();

            // Move all parts
            for (int i = 0; i < allParts.Count; i++)
            {
                Vector2Int targetGrid = nextHeadPos + offsets[i];
                Vector3 worldTarget = new Vector3(targetGrid.x, targetGrid.y, allParts[i].position.z);
                allParts[i].DOMove(worldTarget, moveDuration).SetEase(Ease.Linear);
            }

            headPos = nextHeadPos;
            yield return new WaitForSeconds(moveDuration);
        }

        // Update position tracking
        segmentPositions.Clear();
        foreach (var segment in segments)
        {
            segmentPositions.Add(Vector2Int.RoundToInt(segment.position));
        }

        CleanupPropelledEffect();

        isMoving = false;
        isPropelled = false;
        GameManager.Instance.InputLocked = false;
    }

    #endregion Special Abilities

    #region Effects and Audio

    private void SpawnPropelledEffect()
    {
        if (propelledEffectPrefab == null || activePropelledEffect != null) return;

        Vector3 camCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10f));
        activePropelledEffect = Instantiate(propelledEffectPrefab, camCenter, Quaternion.identity);

        // Fade in effect
        SpriteRenderer sr = activePropelledEffect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
            sr.DOFade(1f, 0.3f);
        }

        // Animate text
        Transform textObj = activePropelledEffect.transform.Find("TextObject");
        if (textObj != null)
        {
            textObj.DOScale(1.15f, 0.3f).SetLoops(-1, LoopType.Yoyo);
            textObj.DOPunchPosition(Vector3.up * 0.1f, 0.5f, 4, 0.7f).SetLoops(-1);
        }
    }

    private void EmitSmoke()
    {
        if (segments.Count <= 0 || smokePrefab == null) return;

        Vector3 smokePos = segments[segments.Count - 1].position;
        GameObject smoke = Instantiate(smokePrefab, smokePos, Quaternion.identity);

        SpriteRenderer sr = smoke.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.DOFade(0f, 0.5f);
        }

        Vector3 pushOffset = -new Vector3(direction.x, direction.y, 0) * 0.2f;
        smoke.transform.DOMove(smokePos + pushOffset, 0.5f);

        Destroy(smoke, 0.6f);
    }

    private void CleanupPropelledEffect()
    {
        if (activePropelledEffect != null)
        {
            Destroy(activePropelledEffect);
            activePropelledEffect = null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion Effects and Audio

    #region Face System

    public void SetFace(SnakeFace face)
    {
        currentFace = face;

        switch (face)
        {
            case SnakeFace.Normal: headRenderer.sprite = normalFace; break;
            case SnakeFace.Propelled: headRenderer.sprite = propelledFace; break;
            case SnakeFace.Eating: headRenderer.sprite = eatingFace; break;
            case SnakeFace.Dead: headRenderer.sprite = deadFace; break;
            case SnakeFace.FruitFell: headRenderer.sprite = fruitFell; break;
            case SnakeFace.Win: headRenderer.sprite = winFace; break;
        }
    }

    private IEnumerator ResetFaceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetFace(SnakeFace.Normal);
    }

    #endregion Face System

    #region Collision and Death

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

    public void Die()
    {
        SetFace(SnakeFace.Dead);
        PlaySound(deathClip);
        GameManager.Instance.InputLocked = true;
        ClearUndoStack();
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        GameManager.Instance.RestartLevel();
        SetFace(SnakeFace.Normal);
    }

    #endregion Collision and Death

    #region Utility Methods

    private void UpdateSegmentSprites()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            SnakeSegment segment = segments[i].GetComponent<SnakeSegment>();
            if (segment != null)
                segment.UpdateSprite(segments, i);
        }
    }

    public void ResetSnake(Vector2Int startPos, Vector2Int facingDir)
    {
        DOTween.KillAll();
        ClearUndoStack();

        direction = facingDir;
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0, 0, GetRotationAngle(facingDir));

        InitializeSnake();
    }

    public Vector2Int GetDirection() => direction;

    public bool IsHeadOnTile(Vector2Int tilePos)
    {
        return Vector2Int.RoundToInt(transform.position) == tilePos;
    }

    #endregion Utility Methods
}