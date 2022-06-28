using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Plane : MonoBehaviour
{
    [SerializeField] private Player _playerPrefab;
    [SerializeField] private Vector3 _scale;
    [SerializeField] private WallMap _wallMap;
    private RectTransform _rectTransform;
    private RectTransform _rectTransformParent;
    private Vector2 _parentOffsetMax;
    private Vector2 _parentOffsetMin;
    private int[] _playersNew;
    private static Dictionary<int,ServerInfo> _idToDirection;
    private bool _isNextTick;
    private Player _myPlayer;
    public Action<Player> OnSetMyPlayer;
    public Action<bool> OnClickPlane;
    public List<Player> PlayersList { get; private set; }
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _rectTransformParent = transform.parent.GetComponent<RectTransform>();
        _idToDirection = new Dictionary<int, ServerInfo>();
        PlayersList = new List<Player>();
        _playersNew = new int[256];
        _parentOffsetMax = _rectTransformParent.offsetMax;
        _parentOffsetMin = _rectTransformParent.offsetMin;
        StartCoroutine(MovePlayers());
    }
    private void OnDestroy()
    {
        StopCoroutine(MovePlayers());
    }
    private void OnMouseDown()
    {
        if (transform.localScale != _scale)
        {
            _rectTransformParent.offsetMax = new Vector2(0, 0);
            _rectTransformParent.offsetMin = new Vector2(0, 0);
            transform.localScale = _scale;
            OnClickPlane?.Invoke(true);
        }
        else
        {
            _rectTransformParent.offsetMax = _parentOffsetMax;
            _rectTransformParent.offsetMin = _parentOffsetMin;
            transform.localScale = new Vector3(1,1,1);
            OnClickPlane?.Invoke(false);
        }
    }
    private void SpawnPlayer(int id, Vector2 startPosition, int angle, float health)
    {
        if (_playersNew[id] == 0)
        {
            _playersNew[id] = 1;
            return;
        }
        Debug.Log("SpawnPlayer " + id);
        Player player = Instantiate(_playerPrefab, transform);
        player.Initialize( id, startPosition, angle, _wallMap, PlayersList, health);
        player.SetColor(Game.MyPlayerId == id ? Color.red : Color.blue, true);
        PlayersList.Add(player); 
    }
    public void SetDictDirections(byte[] buffer)
    {
        _idToDirection = new Dictionary<int, ServerInfo>();
        for (var i = 0; i < buffer.Length; i++ )
        {
            int id = buffer[i];
            float x = (float)BitConverter.ToInt16(buffer,i+1) / 100;
            float y = (float)BitConverter.ToInt16(buffer,i+3) / 100;
            int angle = BitConverter.ToInt32(buffer,i+5);
            bool dmged = buffer[i+9] == 1;
            float bulletPosX = (float)BitConverter.ToInt16(buffer,i+10) / 100;
            float bulletPosY = (float)BitConverter.ToInt16(buffer,i+12) / 100;
            float health = (float)BitConverter.ToInt16(buffer,i+14) / 100;
            i += 15; 
            if(id == 0) continue;
            _idToDirection[id] = new ServerInfo(new Vector2( x, y ), angle, dmged, new Vector2(bulletPosX, bulletPosY), health);
        }
    }
    private void SetPlayersDirections()
    { 
        Dictionary<int,ServerInfo> tempDict;
        tempDict = _idToDirection;
        var playerIds = PlayersList.Select(p => p.Id).ToArray();
        foreach (var id in playerIds)
        {
            if (!tempDict.ContainsKey(id)) continue;
            Player player = PlayersList.First(p => p.Id == id);
            player.SetPositionFromServer(tempDict[id].Position, tempDict[id].Angle);
            player.Fire(tempDict[id].BulletPos, tempDict[id].Dmged, tempDict[id].Health);
            tempDict.Remove(id);
        }
        foreach (var item in tempDict)
            SpawnPlayer(item.Key, item.Value.Position, item.Value.Angle, item.Value.Health );
    }
    private void RemovePlayerFromList(Player player)
    {
        PlayersList.Remove(player);
    }
    public void SetInput(int x,int y, int boost)
    {
        if (_myPlayer == null)
        {
            _myPlayer = PlayersList.FirstOrDefault(p => p.Id == Game.MyPlayerId);
            OnSetMyPlayer?.Invoke(_myPlayer);
        }
        if(_myPlayer == null) return;
        _myPlayer.MovePlayerFromInput(x,y,boost);
    }
    public void MovePlayerShadows(long tick)
    {
        foreach (var player in PlayersList)
            player.MoveShadow(tick);
    }
    public void ChangeTick()
    {
        _isNextTick = true;
    }
    private IEnumerator MovePlayers()
    {
        while (true)
        {
            yield return new WaitUntil(() => _isNextTick);
            _isNextTick = false;  
            SetPlayersDirections();
        }
    }
}
