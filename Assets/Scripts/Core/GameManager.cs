// Scripts/Gameplay/GameManager.cs
using DG.Tweening;
using System.Collections;
using UnityEngine;

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
        InputLocked = true;

        int currentIndex = LevelManager.Instance.CurrentLevelIndex;

        // Reload level data (tilemap, food, obstacles)
        LevelManager.Instance.LoadLevel(currentIndex);

        // Reset snake to starting position
        var snake = LevelManager.Instance.SnakeInstance;
        if (snake != null)
        {
            Vector2Int startDir = Vector2Int.right;
            Vector2Int startPos = LevelManager.Instance.CurrentLevelData.playerStart;

            // Delay reset until after level load completes (if async or layout update needed)
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

        CanExit = open;
    }
}