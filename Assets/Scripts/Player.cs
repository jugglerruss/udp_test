using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer), typeof(RectTransform))]
public class Player : MonoBehaviour
{
    private const int Speed = 1;
    private const float MaxPosition = 255;
    private const float PositionError = 0.0005f;
    [SerializeField] private Text _idText;
    [SerializeField] private Transform _rotateArrow;
    [SerializeField] private Shadow _shadowPrefab;
    private RectTransform _rectTransform;
    private SpriteRenderer _spriteRenderer;
    private float _tempWidth;
    private float _tempHeight;
    private float _tempWidthHalf;
    private float _tempHeightHalf;
    private int _parentWidth;
    private int _parentHeight;
    private Vector2 _targetPos;
    private Vector2 _diffVector;
    private Vector3 _localPercentVector;
    private float _speedBoosted;
    private Shadow _shadow;
    private Dictionary<int, Vector2> _positionsBuffer;
    public int Id { get; private set; }
    private void Update()
    {
        MovePlayers();
        RotatePlayers();
    }
    public void Initialize(int parentHalfWidth, int parentHalfHeight, int id, Vector3 startPosition)
    {
        _rectTransform = GetComponent<RectTransform>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _parentWidth = parentHalfWidth;
        _parentHeight = parentHalfHeight;
        GetStartSizes();
        Id = id;
        _idText.text = id.ToString();
        _shadow = Instantiate(_shadowPrefab, transform.parent);
        _shadow.SetId(Id);
        float percentX = startPosition.x / MaxPosition;
        float percentY = startPosition.y / MaxPosition;
        _localPercentVector = new Vector3(percentX,percentY);
        _positionsBuffer = new Dictionary<int, Vector2>();
        for (int i = 1; i <= Game.TICK_COUNT; i++)
            _positionsBuffer.Add(i,new Vector2(percentX,percentY)); 
        SetPositionFromServer(1,percentX,percentY);
        _speedBoosted = 1f;
    }
    public void SetPositionFromServer(int tick,float percentX,float percentY)
    {
        var newPosition = GetNewPosition(percentX, percentY);
        _shadow.SetPositionFromServer(newPosition);
        _positionsBuffer[tick] = new Vector2( percentX, percentY );
        if (Game.MyPlayerId == Id)
        {
            _localPercentVector = new Vector3(percentX,percentY);
            if( Math.Abs(transform.localPosition.x - percentX) < PositionError &&  
                Math.Abs(transform.localPosition.y - percentY) < PositionError)
                return;
        }
        transform.localPosition = newPosition;
        _targetPos = newPosition;
        _diffVector = _positionsBuffer[tick]*MaxPosition - _positionsBuffer[Game.GetPrevTick(tick)]*MaxPosition;
    }
    private Vector2 GetNewPosition(float percentX, float percentY)
    {
        return new Vector2(
            _tempWidth * percentX - _tempWidthHalf,
            _tempHeight * percentY - _tempHeightHalf);
    }
    private void MovePlayers()
    {
        if( Game.MyPlayerId == Id || _diffVector.magnitude < 0.001f ) return;
        Vector3 interpolateVector = _targetPos + _diffVector;
        transform.localPosition = Vector2.Lerp(transform.localPosition,interpolateVector,0.2f);
    }
    private void RotatePlayers()
    {
        if( _diffVector.magnitude < 0.001f ) return;
        float angle = Vector2.SignedAngle(Vector2.right, _diffVector.normalized);
        _rotateArrow.rotation = Quaternion.Slerp(_rotateArrow.rotation, Quaternion.AngleAxis(angle, new Vector3(0, 0, 1)), 0.05f);
    }
    public void MovePlayerFromInput(int x,int y, int boost)
    {
        if(Id == 0) return;
        bool isBoosted = boost == 1;
        _localPercentVector = new Vector3(CalculatePos(_localPercentVector.x, x, isBoosted),CalculatePos(_localPercentVector.y, y, isBoosted));
        Vector3 newPosition = GetNewPosition(_localPercentVector.x, _localPercentVector.y);
        transform.localPosition = newPosition;
    }
    public void MoveShadow(int tick)
    {
        var newPosition = GetNewPosition(_positionsBuffer[tick].x, _positionsBuffer[tick].y);
        _shadow.SetPositionFromInput(newPosition);
    }
    private float CalculatePos(float pos, int direction, bool boosted)
    {
        _speedBoosted = 1;
        if (boosted) _speedBoosted = 2;
        float temp = pos * MaxPosition + direction * Speed * _speedBoosted;
        if (temp > MaxPosition)
            pos = MaxPosition;
        else if (temp < 0)
            pos = 0;
        else
            pos = temp;
        return pos/MaxPosition;
    }
    public void SetColor(Color color)
    {
        _spriteRenderer.color = color;
        _shadow.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0.5f);
    }
    private void GetStartSizes()
    {
        Rect rect = _rectTransform.rect;
        _tempWidth = _parentWidth - rect.width;
        _tempHeight = _parentHeight - rect.height;
        _tempWidthHalf = _tempWidth / 2;
        _tempHeightHalf = _tempHeight / 2;
    }
}
