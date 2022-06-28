using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    private RectTransform _rectTransform;
    public float PosX;
    public float PosY;
    public float Width { get; private set; }
    public float Height { get; private set; }
    public List<MapCell> Cells;
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Width = _rectTransform.rect.width * transform.localScale.x;
        Height = _rectTransform.rect.height * transform.localScale.y;
        PosX = transform.localPosition.x;
        PosY = transform.localPosition.y;
        Cells = SetCells();
    }
    private List<MapCell> SetCells()
    {
        List<MapCell> cells = new List<MapCell>();
        for (int i = (int)PosX; i < (int)(PosX + Width); i++)
            for (int j = (int)PosY; j < (int)(PosY + Height); j++)
                cells.Add(new MapCell(i,j));
        return cells;
    }
    public bool CheckIn(Vector2 pointPosition)
    {
        return !(pointPosition.x < PosX) && !(pointPosition.x > PosX + Width) && !(pointPosition.y < PosY) && !(pointPosition.y > PosY + Height);
    }
}
