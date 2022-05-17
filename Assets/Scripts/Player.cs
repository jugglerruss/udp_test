using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer), typeof(RectTransform))]
public class Player : MonoBehaviour
{
    private const int Speed = 2;
    private const float MaxPosition = 255;
    private const float PositionError = 0.02f; 
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
    private Vector2 _preTargetPos;
    private Vector2 _diffVector;
    private Shadow _shadow;
    private Dictionary<long, Vector2> _positionsBuffer;
    private int _deleteCounter;
    
    public Action<Player> OnDisconnect;
    public float Interpolation { get; private set; }
    public int Id { get; private set; }
    public Vector2 LocalPercentPosition { get; private set; }
    public Vector2 LocalPercentVector { get; private set; }
    public Vector3 PreLastServerPercentVector{ get; private set; }
    public Vector3 ServerPercentVector{ get; private set; }
    private void FixedUpdate()
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
        LocalPercentVector = new Vector2(percentX,percentY);
        ServerPercentVector = new Vector2(percentX,percentY);
        _positionsBuffer = new Dictionary<long, Vector2> { { Game.TickLocal - 1, LocalPercentVector } ,{ Game.TickLocal, LocalPercentVector } };
        _targetPos = GetNewPosition(LocalPercentVector);
        _preTargetPos = _targetPos;
        transform.localPosition = _targetPos;
    }
    public void SetPositionFromServer(long tick,float percentX,float percentY)
    {
        _deleteCounter = 0;
        ServerPercentVector = new Vector3(percentX,percentY);
        var newPosition = GetNewPosition( ServerPercentVector);
        _shadow.SetPositionFromServer(newPosition);
        if (Game.MyPlayerId == Id)
        {  
            if ( _positionsBuffer.ContainsKey(Game.LocalTickFromServer)){
                PreLastServerPercentVector = _positionsBuffer[Game.LocalTickFromServer-1]; 
                if ((_positionsBuffer[Game.LocalTickFromServer] - (Vector2)ServerPercentVector).magnitude > PositionError)
                {
                    Debug.Log("magnitude " + (_positionsBuffer[Game.LocalTickFromServer] - (Vector2)ServerPercentVector).magnitude);
                    _positionsBuffer[Game.LocalTickFromServer] = ServerPercentVector;
                    _positionsBuffer[Game.TickLocal] = ServerPercentVector;
                    LocalPercentVector = ServerPercentVector;
                    transform.localPosition = newPosition;
                    Interpolation = 0; 
                }
            }
            else
            {
                _positionsBuffer.Add(Game.LocalTickFromServer,ServerPercentVector);
                PreLastServerPercentVector = _positionsBuffer[Game.LocalTickFromServer-1]; 
                transform.localPosition = newPosition;
                LocalPercentVector = ServerPercentVector;
                _targetPos = newPosition;
                Interpolation = 0;
            }
        } 
        else
        {
            if (!_positionsBuffer.ContainsKey(Game.TickServer))
            {
                _positionsBuffer.Add(Game.TickServer, new Vector2( percentX, percentY ));
            } 
            _diffVector = Vector2.zero;
            if (_positionsBuffer.ContainsKey(Game.TickServer - 1))
            {
                _diffVector = GetNewPosition(_positionsBuffer[Game.TickServer]) - GetNewPosition(_positionsBuffer[Game.TickServer-1]);
            }
            Debug.Log($"_diffVector {_diffVector} _positionsBuffer[Game.TickServer] - _positionsBuffer[Game.TickServer-1] { _positionsBuffer[Game.TickServer] - _positionsBuffer[Game.TickServer-1]}"); 
            _targetPos = newPosition; 
            Interpolation = 0;
        }
    }
    private Vector2 GetNewPosition(Vector2 percentPos)
    {
        return new Vector2(
            _tempWidth * percentPos.x - _tempWidthHalf,
            _tempHeight * percentPos.y - _tempHeightHalf);
    }
    private Vector2 GetPercentPosition(Vector3 localPos)
    {
        return new Vector2(
            (localPos.x + _tempWidthHalf)/_tempWidth,
            (localPos.y + _tempHeightHalf)/ _tempHeight ); 
    }
    private void MovePlayers()
    {
        if (Interpolation < 1f)
        {
            transform.localPosition += (Vector3)_diffVector / 5;
            Interpolation += 0.2f;
            LocalPercentPosition = GetPercentPosition(transform.localPosition);
        }
        Debug.Log(Interpolation);
    }
    private void RotatePlayers()
    {
        if( _diffVector.magnitude < 0.001f ) return; 
        float angle = Vector2.SignedAngle(Vector2.right, _diffVector.normalized);
        _rotateArrow.rotation = Quaternion.Slerp(_rotateArrow.rotation, Quaternion.AngleAxis(angle, new Vector3(0, 0, 1)), 0.02f);
    }
    public void MovePlayerFromInput(int x,int y, int boost)
    {
        if(Id == 0) return;
        Interpolation = 0;
        _diffVector = Vector2.zero;
        bool isBoosted = boost == 1;
        LocalPercentVector = new Vector3(CalculatePos(LocalPercentVector.x, x, isBoosted),CalculatePos(LocalPercentVector.y, y, isBoosted));
        _preTargetPos = _targetPos;
        _targetPos = GetNewPosition(LocalPercentVector);
        if(!_positionsBuffer.ContainsKey(Game.TickLocal))
            _positionsBuffer.Add(Game.TickLocal, LocalPercentVector);
        if (_positionsBuffer.ContainsKey(Game.TickLocal) && _positionsBuffer.ContainsKey(Game.TickLocal - 1) && _positionsBuffer[Game.TickLocal - 1] != Vector2.zero)
        {
             
            _diffVector = _targetPos - _preTargetPos;
        }
    }
    public void MoveShadow(long tick)
    {
        if(!_positionsBuffer.ContainsKey(tick))
            _shadow.SetPositionFromInput(GetNewPosition(_positionsBuffer[tick]));
    }
    private float CalculatePos(float pos, int direction, bool boosted)
    {
        int boostSpeed = 1;
        if (boosted) boostSpeed = 2;
        float temp = pos * MaxPosition + direction * Speed * boostSpeed;
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
        _shadow.SetColor(color);
    }
    private void GetStartSizes()
    {
        Rect rect = _rectTransform.rect;
        _tempWidth = _parentWidth - rect.width;
        _tempHeight = _parentHeight - rect.height;
        _tempWidthHalf = _tempWidth / 2;
        _tempHeightHalf = _tempHeight / 2;
    }
    public void NoData()
    {
        _deleteCounter++;
        if (_deleteCounter > 50)
        {
            OnDisconnect?.Invoke(this);
            Destroy(gameObject);
            Destroy(_shadow.gameObject);
        }
            
    }
}
