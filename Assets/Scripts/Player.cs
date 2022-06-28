using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(SpriteRenderer), typeof(RectTransform))]
public class Player : MonoBehaviour
{
    private const float MaxPosition = 100;
    private const int MaxAngle = 360;
    [SerializeField] private float _speed;
    [SerializeField] private int _speedRotation;
    [SerializeField] private float _positionError;
    [SerializeField] private Text _idText;
    [SerializeField] private Transform _rotateArrow;
    [SerializeField] private HealthLine _healthLine;
    [SerializeField] private Shadow _shadowPrefab;
    [SerializeField] private Bullet _bulletPrefab;
    
    private RectTransform _rectTransform;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _targetPos;
    private Vector2 _preTargetPos;
    private Vector2 _diffVector;
    private Shadow _shadow;
    private Dictionary<long, Vector2> _positionsBuffer;
    private float _size;
    private Color _color;

    private WallMap _wallMap;
    public float Interpolation { get; private set; }
    public int Id { get; private set; }
    public Vector2 LocalPosition { get; private set; }
    public Vector3 PreLastServerPosition{ get; private set; }
    public Vector3 ServerPosition{ get; private set; }
    private int Angle{ get; set; }
    private int ServerAngle{ get; set; }
    private Bullet _bullet;
    private List<Player> _playersList;
    
    private void FixedUpdate()
    {
        MovePlayers();
        RotatePlayers();
    }
    public void Initialize( int id, Vector3 startPosition, int angle, WallMap wallMap, List<Player> playersList, float health)
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
        _wallMap = wallMap;
        _size = transform.localScale.x;
        Angle = angle;
        _bullet = Instantiate(_bulletPrefab, transform.position, Quaternion.identity, transform.parent);
        _playersList = playersList;
        _playersList.Remove(this);
        _healthLine.SetHealth(health);
    }
    public void SetPositionFromServer(Vector2 position, int angle)
    {
        ServerPosition = position;
        ServerAngle = angle;
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
        Angle = ServerAngle;
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
            if (!((_positionsBuffer[Game.LocalTickFromServer] - (Vector2)ServerPosition).magnitude > _positionError))
                return;
            _positionsBuffer[Game.LocalTickFromServer] = ServerPosition;
            _positionsBuffer[Game.TickLocal] = ServerPosition;
            SetToServerPosition();
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
        Angle = ServerAngle;
        Interpolation = 0;
    }
    private void MovePlayers()
    {
        if (Interpolation < 1f)
        {
            transform.localPosition += (Vector3)_diffVector / 5;
            Interpolation += 0.2f;
            return;
        }
        transform.localPosition = _targetPos;
    }
    private void RotatePlayers()
    {
        _rotateArrow.rotation = Quaternion.Slerp(_rotateArrow.rotation, Quaternion.AngleAxis(Angle, new Vector3(0, 0, 1)), 0.2f);
    }
    public void MovePlayerFromInput(int x,int y, int boost)
    {
        if(Id == 0) return;
        Interpolation = 0;
        bool isBoosted = boost == 1;
        LocalPosition = GetPosition(LocalPosition, x, y, isBoosted);
        _preTargetPos = _targetPos;
        _targetPos = LocalPosition;
        if(!_positionsBuffer.ContainsKey(Game.TickLocal))
            _positionsBuffer.Add(Game.TickLocal, LocalPosition);
        if (_positionsBuffer.ContainsKey(Game.TickLocal) && _positionsBuffer.ContainsKey(Game.TickLocal - 1) && _positionsBuffer[Game.TickLocal - 1] != Vector2.zero)
            _diffVector = _targetPos - _preTargetPos;
        else
            _diffVector = Vector2.zero;
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
        var angle = Angle;
        var calculatedPos = CalculatePos(ref angle,position, y, x, boostSpeed);
        var directionVector = new MapCell(x, y);
        calculatedPos = CheckWalls(ref angle, position, calculatedPos, directionVector, boostSpeed);
        Angle = angle;
        return calculatedPos;
    }
    private Vector2 CheckWalls(ref int angle,Vector2 position, Vector2 calculatedPos, MapCell direction, int boostSpeed)
    {
        Vector2 maybePos;
        int tempAngle;
        if (IsTouchWallPosition(calculatedPos))
        {
            for (int i = 1; i < 90; i++)
            {
                tempAngle = angle;
                if(GetWallPosition(i) != position)
                {
                    angle = tempAngle;
                    return maybePos;
                }
                tempAngle = angle;
                if(GetWallPosition(-i) != position)
                {
                    angle = tempAngle;
                    return maybePos;
                }
            }
            Debug.Log("Cant find angle");
        }
        return calculatedPos;
        
        Vector2 GetWallPosition( int scaleRotate)
        {
            maybePos = CalculatePos(ref tempAngle,position, direction.Y, scaleRotate, boostSpeed,2);
            return !IsTouchWallPosition(maybePos) ? maybePos : position;
        }
        bool IsTouchWallPosition( Vector2 pos)
        {
            for (int i = -3; i < 3; i++)
            {
                var dir = pos + DirectionFromAngle(Angle + 45 * i) * _size / 2;
                if (_wallMap.IsWallPosition(dir.x,dir.y)) return true;
            }
            return false;
        }
    }
    private Vector2 CalculatePos(ref int angle,Vector2 pos, int forward, int rotate, int boostSpeed, int rotationSpeed = 5)
    {
        angle -= rotate * rotationSpeed;
        if (angle > MaxAngle) angle -= MaxAngle;
        if (angle < 0) angle += MaxAngle;
        Vector2 tempPos = pos + forward * _speed * boostSpeed * DirectionFromAngle(angle);
        pos = tempPos;
        if (tempPos.x > MaxPosition)
            pos.x = MaxPosition;
        else if (tempPos.x < 0)
            pos.x = 0;
        
        if (tempPos.y > MaxPosition)
            pos.y = MaxPosition;
        else if (tempPos.y < 0)
            pos.y = 0;
        return pos;
    }
    public void Fire(Vector2 bulletVector, bool dmged, float health)
    {
        if (bulletVector != new Vector2(105, 105))
        {
            var pos = ServerPosition;
            _bullet.Initialize(pos,  bulletVector);
        }
        if (dmged)
        {
            _healthLine.SetHealth(health);
            if(health == 0) _color = Color.clear;
            StartCoroutine(Blink());
        }
    }
    public void SetColor(Color color, bool init = false)
    {
        if (init) _color = color;
        _spriteRenderer.color = color;
        _shadow.SetColor(color);
    }
    public bool IsTouching(Vector2 pos)
    {
        return ((Vector2)ServerPosition - pos).magnitude <= _size / 2;
    }
    private Vector2 DirectionFromAngle(float angleInDegrees)
    {
        return new Vector2(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad)).normalized;
    }
    private IEnumerator Blink()
    {
        SetColor(Color.yellow);
        yield return new WaitForSeconds(0.2f);
        SetColor(_color);
    }
}
