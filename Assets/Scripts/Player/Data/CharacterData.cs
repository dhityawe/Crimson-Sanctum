using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Crimson Sanctum/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string Name;
    public string Description;
    public GameObject CharacterPrefab;
}
