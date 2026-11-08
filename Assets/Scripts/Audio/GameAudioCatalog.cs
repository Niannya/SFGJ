using UnityEngine;

/// <summary>Audio clips used by the scene and interaction audio director.</summary>
public sealed class GameAudioCatalog : ScriptableObject
{
    public AudioClip mainMenu;
    public AudioClip prologueEpilogue;
    public AudioClip innerWorld;
    public AudioClip treatmentRoom;
    public AudioClip storyRoom;
    public AudioClip classroom;
    public AudioClip amusement;
    public AudioClip bedroomDepressed;
    public AudioClip bedroomBright;

    public AudioClip uiClick;
    public AudioClip crowd;
    public AudioClip balloon;
    public AudioClip footstep;
    public AudioClip whoosh;

    public AudioClip RoomClip(int room)
    {
        switch (room)
        {
            case 0: return treatmentRoom;
            case 1:
            case 5: return storyRoom;
            case 2:
            case 4: return classroom;
            case 3:
            case 7: return amusement;
            case 6: return bedroomDepressed;
            case 8: return bedroomBright;
            default: return null;
        }
    }
}
