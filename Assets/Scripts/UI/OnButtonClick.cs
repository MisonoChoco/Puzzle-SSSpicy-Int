using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClickLoader : MonoBehaviour
{
    public string sceneToLoad;

    private void OnMouseDown()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}