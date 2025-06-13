using UnityEngine;

public class ExitButton : MonoBehaviour
{
    private void OnMouseDown()
    {
        QuitGame();
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode
#endif
    }
}