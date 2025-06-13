// Scripts/Gameplay/Obstacle.cs
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public enum ObstacleType
    { Wall, Ice, Fire }

    public ObstacleType type;
}