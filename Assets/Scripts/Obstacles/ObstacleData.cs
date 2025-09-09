using UnityEngine;

[CreateAssetMenu(fileName = "NewObstacleData", menuName = "CrimsonSanctum/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    public string obstacleName = "Unnamed Obstacle";
    public AudioClip hitSFX;
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.2f;
    public string flavorText = "A deadly surprise.";
}