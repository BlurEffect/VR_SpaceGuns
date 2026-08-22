using UnityEngine;

public enum RadioSender { BridgeOfficer, WingCommander, ReinforcementLeader }

[CreateAssetMenu(menuName = "SpaceGuns/Radio Contact")]
public class RadioContact : ScriptableObject
{
    public string contactName;
    public Sprite portrait;
}
