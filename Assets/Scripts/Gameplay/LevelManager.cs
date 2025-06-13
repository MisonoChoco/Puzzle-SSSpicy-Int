using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public SnakeController SnakeInstance { get; private set; }
    public int CurrentLevelIndex { get; private set; }

    [Header("Tile Prefabs (Index matches tile ID)")]
    public GameObject[] tilePrefabs;

    [Header("Level JSONs")]
    public TextAsset[] levelJsonFiles;

    public Transform levelParent;

    public LevelJsonData CurrentLevelData { get; private set; }

    private int[,] groundMap;
    private int[,] objectMap;

    public GameObject[,] spawnedTiles;

    public int LevelCount => levelJsonFiles.Length;

    public Vector2Int GridSize { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        LoadLevel(0);
    }

    public GameObject GetTileObject(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= GridSize.x || pos.y >= GridSize.y)
            return null;
        return spawnedTiles[pos.x, pos.y];
    }

    public void LoadLevel(int index)
    {
        GameManager.Instance.InputLocked = true;
        CurrentLevelIndex = index;

        // Destroy old tiles and clear grid
        if (spawnedTiles != null)
        {
            for (int x = 0; x < spawnedTiles.GetLength(0); x++)
            {
                for (int y = 0; y < spawnedTiles.GetLength(1); y++)
                {
                    if (spawnedTiles[x, y] != null)
                        Destroy(spawnedTiles[x, y]);
                }
            }
        }

        if (index < 0 || index >= levelJsonFiles.Length)
        {
            Debug.LogError("Level index out of range!");
            return;
        }

        // Clear previous level
        foreach (Transform child in levelParent)
            Destroy(child.gameObject);

        var json = levelJsonFiles[index].text;
        CurrentLevelData = JsonConvert.DeserializeObject<LevelJsonData>(json);

        // Set maps and grid size BEFORE calling the coroutine
        GridSize = new Vector2Int(CurrentLevelData.width, CurrentLevelData.height);
        groundMap = CurrentLevelData.ground;
        objectMap = CurrentLevelData.objects;

        spawnedTiles = new GameObject[CurrentLevelData.width, CurrentLevelData.height];

        StartCoroutine(AnimateLevelLoad(() =>
        {
            var snake = Object.FindFirstObjectByType<SnakeController>();
            if (snake != null)
            {
                Vector2Int start = CurrentLevelData.playerStart;
                Vector2Int dir = Vector2Int.right;
                snake.ResetSnake(start, dir);
                snake.transform.position = new Vector3(start.x, start.y, 0);
            }

            GameManager.Instance.InputLocked = false;
        }));
    }

    public IEnumerator AnimateLevelLoad(System.Action onComplete)
    {
        foreach (Transform child in levelParent)
            Destroy(child.gameObject);

        int width = GridSize.x;
        int height = GridSize.y;
        float delayBetweenTiles = 0.02f;

        spawnedTiles = new GameObject[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 spawnPos = new Vector3(x, y - 2f, 1f); // Ground z

                // Ground layer
                int groundId = groundMap[x, y];
                if (groundId >= 0 && groundId < tilePrefabs.Length)
                {
                    GameObject ground = Instantiate(tilePrefabs[groundId], spawnPos, Quaternion.identity, levelParent);
                    ground.transform.DOMoveY(y, 0.3f).SetEase(Ease.OutBack).SetDelay(y * delayBetweenTiles);
                    var sr = ground.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = -y;
                }

                // Object layer
                int objectId = objectMap[x, y];
                if (objectId > 1 && objectId < tilePrefabs.Length)
                {
                    Vector3 objPos = new Vector3(x, y - 2f, GetZDepthByTileID(objectId));
                    GameObject obj = Instantiate(tilePrefabs[objectId], objPos, Quaternion.identity, levelParent);
                    obj.transform.DOMoveY(y, 0.3f).SetEase(Ease.OutBack).SetDelay(y * delayBetweenTiles);
                    var sr = obj.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = -y;
                    spawnedTiles[x, y] = obj;
                }
            }

            yield return new WaitForSeconds(delayBetweenTiles);
        }

        if (groundMap.GetLength(0) != GridSize.x || groundMap.GetLength(1) != GridSize.y)
        {
            Debug.LogError("Ground map size does not match GridSize");
            yield break;
        }

        yield return new WaitForSeconds(0.4f); // Final delay after animation
        onComplete?.Invoke();
    }

    private float GetZDepthByTileID(int id)
    {
        switch (id)
        {
            case 0: return 0f;    // Grass (background)
            case 1: return -0.2f; // Wall (behind objects)
            case 2: return -0.2f; // Spicy
            case 3: return -0.2f; // Banana
            case 4: return -0.3f; // Exit closed
            case 5: return -0.3f; // Exit open
            default: return 0f;
        }
    }

    public int GetTileID(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return -1;
        return objectMap[pos.x, pos.y];
    }

    public void ClearTile(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return;
        objectMap[pos.x, pos.y] = 0;

        if (spawnedTiles[pos.x, pos.y] != null)
        {
            Destroy(spawnedTiles[pos.x, pos.y]);
            spawnedTiles[pos.x, pos.y] = null;
        }
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < GridSize.x && pos.y < GridSize.y;
    }

    public void MoveTile(Vector2Int from, Vector2Int to)
    {
        GameObject tile = GetTileObject(from);
        if (tile == null) return;

        spawnedTiles[from.x, from.y] = null;
        spawnedTiles[to.x, to.y] = tile;

        tile.transform.DOMove(new Vector3(to.x, to.y, 0), 0.15f).SetEase(Ease.Linear);
    }
}