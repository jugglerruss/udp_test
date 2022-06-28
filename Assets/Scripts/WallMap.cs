using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallMap : MonoBehaviour
{
    private const int MaxPos = 100;
    private bool[,] _map;
    private List<Wall> _walls;
    private void Start()
    {
        _walls = FindObjectsOfType<Wall>().ToList();
        _map = new bool[MaxPos,MaxPos];
        foreach (var wall in _walls)
            foreach (var cell in wall.Cells)
                _map[cell.X, cell.Y] = true;
    }
    public bool IsWallPosition(float x, float y)
    {
        if (x < 0 || y < 0 || x >= MaxPos || y >= MaxPos) return true;
        if (_map[(int)x, (int)y]) return true;
        return false;
    }
}
