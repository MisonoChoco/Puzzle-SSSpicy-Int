using UnityEngine;
using System.Collections.Generic;

public class FlexibleUIController : MonoBehaviour
{
    [Header("Resolution")]
    public float referenceWidth = 1920f;

    public float referenceHeight = 1080f;

    [Header("Background")]
    public Transform background;

    [System.Serializable]
    public class UIElement
    {
        public Transform target;
        public Anchor anchor;
        public Vector2 offset; // offset from the anchor
    }

    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
        TopCenter,
        BottomCenter
    }

    [Header("UI")]
    public List<UIElement> uiElements;

    private void Start()
    {
        AdjustUI();
    }

    private void AdjustUI()
    {
        Camera cam = Camera.main;
        float screenHeight = 2f * cam.orthographicSize;
        float screenWidth = screenHeight * cam.aspect;

        Vector2 center = Vector2.zero;
        Vector2 topLeft = new Vector2(-screenWidth / 2f, screenHeight / 2f);
        Vector2 topRight = new Vector2(screenWidth / 2f, screenHeight / 2f);
        Vector2 bottomLeft = new Vector2(-screenWidth / 2f, -screenHeight / 2f);
        Vector2 bottomRight = new Vector2(screenWidth / 2f, -screenHeight / 2f);
        Vector2 topCenter = new Vector2(0, screenHeight / 2f);
        Vector2 bottomCenter = new Vector2(0, -screenHeight / 2f);

        if (background != null)
        {
            background.localScale = Vector3.one;
            SpriteRenderer bgRenderer = background.GetComponent<SpriteRenderer>();
            if (bgRenderer != null)
            {
                Vector2 size = bgRenderer.bounds.size;
                float scaleX = screenWidth / size.x;
                float scaleY = screenHeight / size.y;
                float scale = Mathf.Max(scaleX, scaleY);
                background.localScale = Vector3.one * scale;
            }
            else
            {
                return;
            }
        }

        foreach (var element in uiElements)
        {
            Vector2 anchorPos = center;

            switch (element.anchor)
            {
                case Anchor.TopLeft: anchorPos = topLeft; break;
                case Anchor.TopRight: anchorPos = topRight; break;
                case Anchor.BottomLeft: anchorPos = bottomLeft; break;
                case Anchor.BottomRight: anchorPos = bottomRight; break;
                case Anchor.Center: anchorPos = center; break;
                case Anchor.TopCenter: anchorPos = topCenter; break;
                case Anchor.BottomCenter: anchorPos = bottomCenter; break;
            }

            Vector3 finalPos = anchorPos + element.offset;
            element.target.position = finalPos;
        }
    }
}