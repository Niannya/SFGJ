using UnityEngine;

/// <summary>Enter the trigger to travel to a room in the same scene.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MapPortal : MonoBehaviour
{
    public enum AccessRule { Forward, AfterFirstCycle, EndingOnly }

    [SerializeField] private MapRingController ring;
    [SerializeField] private int originRoom;
    [SerializeField] private int destinationRoom;
    [SerializeField] private Transform destinationSpawn;
    [SerializeField] private AccessRule accessRule;

    public int OriginRoom => originRoom;
    public int DestinationRoom => destinationRoom;
    public Transform DestinationSpawn => destinationSpawn;

    public bool IsAvailable(bool firstCycleComplete, bool endingUnlocked)
    {
        switch (accessRule)
        {
            case AccessRule.Forward: return true;
            case AccessRule.AfterFirstCycle: return firstCycleComplete;
            case AccessRule.EndingOnly: return firstCycleComplete && endingUnlocked;
            default: return false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ring != null && other.GetComponent<PlayerController>() != null)
            ring.EnterRoom(this);
    }
}
