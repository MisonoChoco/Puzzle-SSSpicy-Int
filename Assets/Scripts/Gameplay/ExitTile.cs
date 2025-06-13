using UnityEngine;

public class ExitTileController : MonoBehaviour
{
    public static ExitTileController Instance { get; private set; }

    public SpriteRenderer sr;
    public Sprite openSprite;
    public Sprite closedSprite;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        sr.sprite = open ? openSprite : closedSprite;
    }
}