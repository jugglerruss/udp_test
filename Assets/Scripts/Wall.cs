using UnityEngine;

public class Wall : MonoBehaviour
{
    private RectTransform _rectTransform;
    public float PosX;
    public float PosY;
    public float Width { get; private set; }
    public float Height { get; private set; }
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        Width = _rectTransform.rect.width * transform.localScale.x;
        Height = _rectTransform.rect.height * transform.localScale.y;
        PosX = transform.localPosition.x;
        PosY = transform.localPosition.y;
    }
    public bool CheckIn(Vector2 pointPosition)
    {
        return !(pointPosition.x < PosX) && !(pointPosition.x > PosX + Width) && !(pointPosition.y < PosY) && !(pointPosition.y > PosY + Height);
    }
}
