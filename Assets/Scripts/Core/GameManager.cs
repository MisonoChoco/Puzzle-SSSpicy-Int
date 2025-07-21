using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    private static void InitDOTween()
    {
        DOTween.Init();
    }

    public static GameManager Instance { get; private set; }
    public bool CanExit { get; set; } = true;
    public bool InputLocked { get; set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void LockInputTemporarily(float duration)
    {
        StartCoroutine(LockFor(duration));
    }

    private IEnumerator LockFor(float duration)
    {
        InputLocked = true;
        yield return new WaitForSeconds(duration);
        InputLocked = false;
    }

    public void RestartLevel()
    {
        StartCoroutine(RestartLevelCoroutine());
    }

    private IEnumerator RestartLevelCoroutine()
    {
        InputLocked = true;

        // Get references before reload
        var snake = LevelManager.Instance.SnakeInstance;
        int currentIndex = LevelManager.Instance.CurrentLevelIndex;
        Vector2Int startPos = LevelManager.Instance.CurrentLevelData.playerStart;
        Vector2Int startDir = Vector2Int.right;

        // First, completely reset the snake to stop all activities
        if (snake != null)
        {
            snake.ResetSnake(startPos, startDir);
        }

        // Small delay to ensure all cleanup is complete
        yield return new WaitForEndOfFrame();

        // Reload level data (tilemap, food, obstacles)
        LevelManager.Instance.LoadLevel(currentIndex);

        // Another small delay to ensure level loading is complete
        yield return new WaitForEndOfFrame();

        // Final reset of snake position (in case level loading moved things)
        if (snake != null)
        {
            snake.ResetSnake(startPos, startDir);
        }

        InputLocked = false;
    }

    public void WinLevel()
    {
        Debug.Log("You Win!");
        int nextIndex = LevelManager.Instance.CurrentLevelIndex + 1;
        if (nextIndex < LevelManager.Instance.LevelCount)
        {
            LevelManager.Instance.LoadLevel(nextIndex);
        }
        else
        {
            Debug.Log("All levels completed!");
        }
    }

    public void SetExitState(bool open)
    {
        foreach (Transform child in LevelManager.Instance.levelParent)
        {
            var tile = child.GetComponent<TileBehavior>();
            if (tile != null && tile.type == TileBehavior.TileType.Exit)
                tile.SetExitState(open);
        }
        CanExit = true;
    }
}