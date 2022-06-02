using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Plane : MonoBehaviour
{
    [SerializeField] private Player _playerPrefab;
    private RectTransform _rectTransform;
    private List<Player> _playersList;
    private int[] _playersNew;
    private static Dictionary<int,Vector2> _idToDirection;
    private bool _isNextTick;
    private Player _myPlayer;
    public Action<Player> OnSetMyPlayer;
    public List<Wall> WallMap { get; private set; }
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _idToDirection = new Dictionary<int, Vector2>();
        _playersList = new List<Player>();
        _playersNew = new int[256];
        WallMap = new List<Wall>(FindObjectsOfType<Wall>());
        StartCoroutine(MovePlayers());
    }
    private void OnDestroy()
    {
        StopCoroutine(MovePlayers());
    }
    private void SpawnPlayer(int id, Vector3 startPosition)
    {
        if (_playersNew[id] == 0)
        {
            _playersNew[id] = 1;
            return;
        }
        Debug.Log("SpawnPlayer " + id);
        Player player = Instantiate(_playerPrefab, transform);
        player.Initialize( id, startPosition, WallMap);
        player.SetColor(Game.MyPlayerId == id ? Color.red : Color.blue);
        _playersList.Add(player); 
    }
    public void SetDictDirections(byte[] buffer)
    {
        _idToDirection = new Dictionary<int, Vector2>();
        for (var i = 0; i < buffer.Length; i++ )
        {
            int id = buffer[i];
            float x = (float)BitConverter.ToInt16(buffer,i+1) / 100;
            float y = (float)BitConverter.ToInt16(buffer,i+3) / 100;
            i += 4; 
            if(id == 0) continue;
            _idToDirection[id] = new Vector2( x, y );
        }
    }
    private void SetPlayersDirections()
    { 
        Dictionary<int,Vector2> tempDict;
        tempDict = _idToDirection;
        var playerIds = _playersList.Select(p => p.Id).ToArray();
        foreach (var id in playerIds)
        {
            if (!tempDict.ContainsKey(id)) continue;
            Player player = _playersList.First(p => p.Id == id);
            player.SetPositionFromServer(tempDict[id].x,tempDict[id].y);
            tempDict.Remove(id);
        }
        foreach (var item in tempDict)
            SpawnPlayer(item.Key, item.Value);
    }
    private void RemovePlayerFromList(Player player)
    {
        _playersList.Remove(player);
    }
    public void SetInput(int x,int y, int boost)
    {
        if (_myPlayer == null)
        {
            _myPlayer = _playersList.FirstOrDefault(p => p.Id == Game.MyPlayerId);
            OnSetMyPlayer?.Invoke(_myPlayer);
        }
        if(_myPlayer == null) return;
        _myPlayer.MovePlayerFromInput(x,y,boost);
    }
    public void MovePlayerShadows(long tick)
    {
        foreach (var player in _playersList)
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
