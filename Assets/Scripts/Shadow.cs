using UnityEngine;
using UnityEngine.UI;

public class Shadow : MonoBehaviour
{
    [SerializeField] private Text _idText;
    public void SetPositionFromServer(Vector3 pos)
    {
        DisableId();
        transform.localPosition = pos;
    }
    public void SetPositionFromInput(Vector3 pos)
    {
        EnableId();
        transform.localPosition = pos;
    }
    public void SetId(int id)
    {
        _idText.text = id.ToString();
    }
    private void DisableId()
    {
        if(_idText.enabled)
            _idText.enabled = false;
    }
    private void EnableId()
    {
        if(!_idText.enabled)
            _idText.enabled = true;
    }
}
