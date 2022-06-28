using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const float Speed = 1f;
    private Vector2 _direction;
    private WallMap _wallMap;
    private bool _initialized;
    private Vector2 _start;
    private Vector2 _finish;
    private int _step;
    private void Start()
    {
        Show(false);
    }
    private void Update()
    {
        if (!_initialized)
        {
            transform.localPosition = new Vector3(1000, 1000);
            return;
        }
        transform.localPosition = Vector3.MoveTowards(_start, _finish, Speed);
        _start = transform.localPosition;
        if (transform.localPosition == (Vector3)_finish)
            Show(false);
    }
    public void Initialize( Vector2 start, Vector2 bulletVector)
    {
        if (_initialized) return;
        _start = start;
        _finish = bulletVector;
        Show(true);
    }
    private Vector2 GetFinishPoint(List<Player> playersList)
    {
        Vector2 tempPoint = _start;
        while (true)
        {
            tempPoint += _direction;
            foreach (var player in playersList)
            {
                if(player.IsTouching(tempPoint)) return tempPoint;
            }
            if (_wallMap.IsWallPosition(tempPoint.x,tempPoint.y)) return tempPoint;
        }
    }
    private void Show(bool show)
    {
        _initialized = show;
        _step = 1;
    }
}
