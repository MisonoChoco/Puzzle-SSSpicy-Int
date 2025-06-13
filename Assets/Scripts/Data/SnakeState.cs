using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SnakeState
{
    public Vector2Int headPosition;
    public List<Vector2Int> segmentPositions;
    public bool growThisStep;
    public bool isPropelled;

    public SnakeState(Vector2Int headPosition, List<Vector2Int> segmentPositions, bool growThisStep, bool isPropelled)
    {
        this.headPosition = headPosition;
        this.segmentPositions = new List<Vector2Int>(segmentPositions); // deep copy
        this.growThisStep = growThisStep;
        this.isPropelled = isPropelled;
    }
}