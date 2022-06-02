using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer), typeof(RectTransform))]
public class Player : MonoBehaviour
{
    private const float Speed = 0.5f;
    private const float MaxPosition = 100;
    private const float PositionError = 0.5f;
    [SerializeField] private Text _idText;
    [SerializeField] private Transform _rotateArrow;
    [SerializeField] private Shadow _shadowPrefab;
    
    private RectTransform _rectTransform;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _targetPos;
    private Vector2 _preTargetPos;
    private Vector2 _diffVector;
    private Shadow _shadow;
    private Dictionary<long, Vector2> _positionsBuffer;
    private int _deleteCounter;
    private float _size;

    private List<Wall> WallMap;
    public float Interpolation { get; private set; }
    public int Id { get; private set; }
    public Vector2 LocalPosition { get; private set; }
    public Vector3 PreLastServerPosition{ get; private set; }
    public Vector3 ServerPosition{ get; private set; }
    private void FixedUpdate()
    {
        MovePlayers();
        RotatePlayers();
    }
    public void Initialize( int id, Vector3 startPosition, List<Wall> wallMap)
    {
        _rectTransform = GetComponent<RectTransform>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Id = id;
        _idText.text = id.ToString();
        _shadow = Instantiate(_shadowPrefab, transform.parent);
        _shadow.SetId(Id);
        Debug.Log("Create "+ Id);
        float posX = startPosition.x;
        float posY = startPosition.y;
        LocalPosition = new Vector2(posX,posY);
        ServerPosition = new Vector2(posX,posY);
        _positionsBuffer = new Dictionary<long, Vector2> { { Game.TickLocal - 1, LocalPosition } ,{ Game.TickLocal, LocalPosition } };
        _targetPos = LocalPosition;
        _preTargetPos = _targetPos;
        transform.localPosition = _targetPos;
        WallMap = wallMap;
        _size = transform.localScale.x;
    }
    public void SetPositionFromServer(float posX,float posY)
    {
        _deleteCounter = 0;
        ServerPosition = new Vector3(posX,posY);
        _shadow.SetPositionFromServer(ServerPosition);
        if (Game.MyPlayerId == Id)
            SetPositionMyPlayer();
        else
            SetPositionOtherPlayers();
    }
    private void SetPositionOtherPlayers()
    {
        if (_positionsBuffer.ContainsKey(Game.TickServer))
            return;
        _positionsBuffer.Add(Game.TickServer, ServerPosition);
        _targetPos = ServerPosition;
        Interpolation -= 1;
        _diffVector = _targetPos - (Vector2)transform.localPosition;
    }
    private void SetPositionMyPlayer()
    {
        if (_positionsBuffer.ContainsKey(Game.LocalTickFromServer))
        {
            if (_positionsBuffer.ContainsKey(Game.LocalTickFromServer - 1))
                PreLastServerPosition = _positionsBuffer[Game.LocalTickFromServer - 1];
            if ((_positionsBuffer[Game.LocalTickFromServer] - (Vector2)ServerPosition).magnitude > PositionError)
            {
                Debug.Log("magnitude " + (_positionsBuffer[Game.LocalTickFromServer] - (Vector2)ServerPosition).magnitude);
                _positionsBuffer[Game.LocalTickFromServer] = ServerPosition;
                _positionsBuffer[Game.TickLocal] = ServerPosition;
                SetToServerPosition();
            }
        }
        else
        {
            _positionsBuffer.Add(Game.LocalTickFromServer, ServerPosition);
            if (_positionsBuffer.ContainsKey(Game.LocalTickFromServer - 1))
                PreLastServerPosition = _positionsBuffer[Game.LocalTickFromServer - 1];
            SetToServerPosition();
        }
    }
    private void SetToServerPosition()
    {
        transform.localPosition = ServerPosition;
        LocalPosition = ServerPosition;
        _targetPos = ServerPosition;
        Interpolation = 0;
    }
    private void MovePlayers()
    {
        if (Interpolation < 1f)
        {
            transform.localPosition += (Vector3)_diffVector / 5;
            Interpolation += 0.2f;
        }
        else
        {
            transform.localPosition = _targetPos;
        }
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
        bool isBoosted = boost == 1;
        LocalPosition = GetPosition(LocalPosition, x, y, isBoosted);
        Debug.Log("after GetPosition");  
        _preTargetPos = _targetPos;
        _targetPos = LocalPosition;
        if(!_positionsBuffer.ContainsKey(Game.TickLocal))
            _positionsBuffer.Add(Game.TickLocal, LocalPosition);
        if (_positionsBuffer.ContainsKey(Game.TickLocal) && _positionsBuffer.ContainsKey(Game.TickLocal - 1) && _positionsBuffer[Game.TickLocal - 1] != Vector2.zero)
            _diffVector = _targetPos - _preTargetPos;
        else
            _diffVector = Vector2.zero;  
        Debug.Log("_diffVector" + _diffVector); 
        
    }
    public void MoveShadow(long tick)
    {
        if(!_positionsBuffer.ContainsKey(tick))
            _shadow.SetPositionFromInput(_positionsBuffer[tick]);
    }
    private Vector2 GetPosition(Vector2 position, int x,int y, bool boosted)
    {
        int boostSpeed = 1;
        if (boosted) boostSpeed = 2;
        var calculatedPos = new Vector2(CalculatePos(position.x, x, boostSpeed), CalculatePos(position.y, y, boostSpeed));
        var directionVector = new Vector2(x, y);
        var checkedDirectionVector = CheckWalls(calculatedPos, directionVector, _size);
        if( directionVector != checkedDirectionVector)
            calculatedPos = new Vector2(CalculatePos(position.x, (int)checkedDirectionVector.x, boostSpeed), CalculatePos(position.y, (int)checkedDirectionVector.y, boostSpeed));
        return calculatedPos;
    }
    private Vector2 CheckWalls(Vector2 pos, Vector2 direction, float width)
    {
        Debug.Log("CheckWalls");  
        var top = pos + new Vector2(width / 2, width);
        var right = pos + new Vector2(width, width / 2);
        var bottom = pos + new Vector2(width / 2, 0);
        var left = pos + new Vector2(0, width / 2);
        var newDirection = direction; 
        Debug.Log("direction" + direction); 
        
        foreach (var wall in WallMap)
        {
            if (wall.CheckIn(right))
            {
                if (direction.x > 0)
                    newDirection = direction.y == 0 ? new Vector2(0, 1) : new Vector2(0, direction.y);
                if(direction.x == 0) newDirection = new Vector2(-1, direction.y);
            }else if (wall.CheckIn(left))
            {
                if (direction.x < 0)
                    newDirection = direction.y == 0 ? new Vector2(0, -1) : new Vector2(0, direction.y);
                if(direction.x == 0) newDirection = new Vector2(1, direction.y);
            }else if (wall.CheckIn(bottom))
            {
                if (direction.y < 0)
                    newDirection = direction.x == 0 ? new Vector2(1, 0) : new Vector2(direction.x, 0);
                if(direction.y == 0) newDirection = new Vector2(direction.x, 1);
            }else if (wall.CheckIn(top))
            {
                if (direction.y > 0)
                    newDirection = direction.x == 0 ? new Vector2(-1, 0) : new Vector2(direction.x, 0);
                if(direction.y == 0) newDirection = new Vector2(direction.x, -1);
            }
        }
        Debug.Log("newDirection" + newDirection);
        return newDirection;
    }
    private float CalculatePos(float pos, int direction, int boostSpeed)
    {
        float temp = pos + direction * Speed * boostSpeed;
        if (temp > MaxPosition)
            pos = MaxPosition;
        else if (temp < 0)
            pos = 0;
        else
            pos = temp;
        return pos;
    }
    public void SetColor(Color color)
    {
        _spriteRenderer.color = color;
        _shadow.SetColor(color);
    }
}
