using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SnakeState
{
    public Vector2Int headPosition;
    public List<Vector2Int> segmentPositions;
    public Vector2Int direction;
    public bool growThisStep;
    public bool isPropelled;
    public SnakeController.SnakeFace face;
    public List<LevelObjectRestore> levelObjects;

    public SnakeState(Vector2Int headPos, List<Vector2Int> segPos, Vector2Int dir,
                     bool grow, bool propelled, SnakeController.SnakeFace faceState,
                     List<LevelObjectRestore> objects = null)
    {
        this.headPosition = headPos;
        this.segmentPositions = new List<Vector2Int>(segPos);
        this.direction = dir;
        this.growThisStep = grow;
        this.isPropelled = propelled;
        this.face = faceState;
        this.levelObjects = objects != null ? new List<LevelObjectRestore>(objects) : new List<LevelObjectRestore>();
    }
}