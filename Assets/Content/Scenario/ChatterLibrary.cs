using System;
using UnityEngine;

public enum ChatterKey
{
    SensorContact,
    EnemyDetected, 
    ReinforcementsRequested, 
    ReinforcementsArrived 
}

[Serializable]
public struct ChatterEntry
{
    public ChatterKey   key;
    public RadioContact contact;
    [TextArea]
    public string       message;
    public AudioClip    clip;
}

[CreateAssetMenu(menuName = "SpaceGuns/Chatter Library")]
public class ChatterLibrary : ScriptableObject
{
    [SerializeField] private ChatterEntry[] entries;

    public bool TryGet(ChatterKey key, out ChatterEntry entry)
    {
        foreach (var e in entries)
        {
            if (e.key == key) { entry = e; return true; }
        }
        entry = default;
        return false;
    }
}
