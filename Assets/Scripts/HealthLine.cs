using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthLine : MonoBehaviour
{
    private float _health;
    private void Update()
    {
        transform.localScale = new Vector2(_health, 0.1f);
    }
    public void SetHealth(float health)
    {
        _health = health;
    }
}
