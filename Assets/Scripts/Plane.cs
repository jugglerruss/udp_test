using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Plane : MonoBehaviour
{
    [SerializeField] private Player _playerPrefab;
    private RectTransform _rectTransform;
    private int _width;
    private int _height;
    private List<Player> _playersList;
    private int[] _playersNew;
    private static Dictionary<int,Vector2> _idToDirection;
    private bool _isSetNewDirections;
    private bool _isProcessingDirections;
    private bool _isNextTick;
    private Player _myPlayer;
    public Action<Player> OnSetMyPlayer;
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _width = (int)(_rectTransform.rect.width * transform.localScale.x);
        _height = (int)(_rectTransform.rect.height * transform.localScale.y);
        _idToDirection = new Dictionary<int, Vector2>();
        _playersList = new List<Player>();
        _playersNew = new int[256];
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
        player.Initialize(_width, _height, id, startPosition);
        player.SetColor(Game.MyPlayerId == id ? Color.red : Color.blue);
        _playersList.Add(player); 
        player.OnDisconnect += RemovePlayerFromList;
    }
    public void SetDictDirections(byte[] buffer)
    {
        _idToDirection = new Dictionary<int, Vector2>();
        for (var i = 0; i < buffer.Length; i++ )
        {
            int id = buffer[i];
            int x = buffer[i+1];
            int y = buffer[i+2];
            i += 2; 
            if(id == 0) continue;
            _idToDirection[id] = new Vector2( x, y );
        }
        _isSetNewDirections = true;
    }
    private void SetPlayersDirections()
    { 
        _isProcessingDirections = true;
        Dictionary<int,Vector2> tempDict;
        tempDict = _idToDirection;
        var playerIds = _playersList.Select(p => p.Id).ToArray();
        foreach (var id in playerIds)
        {
            if (tempDict.ContainsKey(id))
            {
                Player player = _playersList.First(p => p.Id == id);
                player.SetPositionFromServer(Game.TickServer,tempDict[id].x/255,tempDict[id].y/255);
                tempDict.Remove(id);
            }
            else
            {
                var playerForDelete = _playersList.First(p => p.Id == id);
                playerForDelete.NoData();
            }
        }
        foreach (var item in tempDict)
        {
            SpawnPlayer(item.Key, item.Value);
            foreach (var qwe in _idToDirection)
            {
                Debug.Log(qwe.Key + "|" + qwe.Value); 
            }
        }
        _isProcessingDirections = false;
        _isSetNewDirections = false;
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
        Debug.Log("SetInput");
        _myPlayer.MovePlayerFromInput(x,y,boost);
    }
    public void MovePlayerShadows(long tick)
    {
        foreach (var player in _playersList)
        {
            player.MoveShadow(tick);
        }
    }
    public void ChangeTick()
    {
        _isNextTick = true;
    }
    public Player GetPlayer(int id)
    {
        return _playersList.Find(p => p.Id == id);
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
