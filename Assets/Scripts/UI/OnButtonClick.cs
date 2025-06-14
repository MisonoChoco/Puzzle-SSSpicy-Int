using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClickLoader : MonoBehaviour
{
    public string sceneToLoad;

    private void Update()
    {
        // Handle touch input
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            CheckTouchOrClick(touchPos);
        }

        // Handle mouse click (for desktop or editor testing)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CheckTouchOrClick(mousePos);
        }
    }

    private void CheckTouchOrClick(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapPoint(position);
        if (hit != null && hit.gameObject == gameObject)
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}