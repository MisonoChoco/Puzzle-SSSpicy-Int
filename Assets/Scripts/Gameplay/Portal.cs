// Scripts/Gameplay/Portal.cs
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Portal destination;

    public void Teleport(GameObject snakeHead)
    {
        snakeHead.transform.position = destination.transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Snake"))
        {
            Teleport(collision.gameObject);
        }
    }
}