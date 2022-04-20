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
    private static Dictionary<int,int[]> _idToDirection;
    private bool _isSetNewDirections;
    private bool _isProcessingDirections;
    private Player _myPlayer;
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _width = (int)(_rectTransform.rect.width * transform.localScale.x);
        _height = (int)(_rectTransform.rect.height * transform.localScale.y);
        _idToDirection = new Dictionary<int, int[]>();
        _playersList = new List<Player>();
        StartCoroutine(MovePlayers());
    }
    private void OnDestroy()
    {
        StopCoroutine(MovePlayers());
    }
    private Player SpawnPlayer(int id, Vector3 startPosition)
    {
        Player player = Instantiate(_playerPrefab, transform);
        player.Initialize(_width, _height, id, startPosition);
        player.SetColor(Game.MyPlayerId == id ? Color.red : Color.blue);
        _playersList.Add(player);
        return player;
    }
    public void SetDictDirections(int[] buffer)
    {
        _idToDirection = new Dictionary<int, int[]>();
        for (int i = 1; i < buffer.Length; i++ )
        {
            if(buffer[i] == 0) break;
            int id = buffer[i];
            int x = buffer[i+1]+1;
            int y = buffer[i+2]+1;
            i += 2;
            _idToDirection[id] = new[] { x, y };
        }
        _isSetNewDirections = true;
    }
    private void SetPlayersDirections()
    {
        _isProcessingDirections = true;
        Dictionary<int,int[]> tempDict = new Dictionary<int, int[]>(_idToDirection);
        var playerIds = _playersList.Select(p => p.Id).ToArray();
        foreach (var id in playerIds)
        {
            if (tempDict.ContainsKey(id))
            {
                Player player = _playersList.First(p => p.Id == id);
                player.SetPositionFromServer(Game.GetTick(),(float)tempDict[id][0]/255,(float)tempDict[id][1]/255);
            }
            else
            {
                var playerForDelete = _playersList.First(p => p.Id == id);
                Debug.Log(id);
                _playersList.Remove(playerForDelete);
                Destroy(playerForDelete.gameObject);
            }
            tempDict.Remove(id);
        }
        foreach (var item in tempDict)
        {
            SpawnPlayer(item.Key, new Vector3(item.Value[0],item.Value[1]));
        }
        _isProcessingDirections = false;
        _isSetNewDirections = false;
    }
    public void SetInput(int x,int y, int boost)
    {
        if(_myPlayer == null)
            _myPlayer = _playersList.FirstOrDefault(p => p.Id == Game.MyPlayerId);
        if(_myPlayer == null) return;
        _myPlayer.MovePlayerFromInput(x,y,boost);
    }
    public void MovePlayerShadows(int tick)
    {
        foreach (var player in _playersList)
        {
            player.MoveShadow(tick);
        }
    }
    private IEnumerator MovePlayers()
    {
        while (true)
        {
            if(!_isProcessingDirections && _isSetNewDirections)SetPlayersDirections();
            yield return new WaitForSeconds(0.05f);
        }
    }
}
