using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private void Start()
    {
        string spawnPointName = SceneTransitionData.TargetSpawnPointName;
        if (string.IsNullOrEmpty(spawnPointName)) return;
        SceneTransitionData.TargetSpawnPointName = null;

        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint == null) return;

        transform.position = spawnPoint.transform.position;
    }
}
