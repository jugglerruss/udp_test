 using UnityEngine;

 public struct ServerInfo
 {
     public Vector2 Position;
     public int Angle;
     public bool Dmged;
     public Vector2 BulletPos;
     public float Health;
     public ServerInfo(Vector2 position,int angle, bool dmged, Vector2 bulletPos, float health)
     {
         Position = position;
         Angle = angle;
         Dmged = dmged;
         BulletPos = bulletPos;
         Health = health;
     }
 }
