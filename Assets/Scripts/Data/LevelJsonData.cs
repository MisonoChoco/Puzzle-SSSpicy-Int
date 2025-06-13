using UnityEngine;

[System.Serializable]
public class LevelJsonData
{
    public int width;
    public int height;
    public int[,] ground;
    public int[,] objects;
    public Vector2Int playerStart;
}