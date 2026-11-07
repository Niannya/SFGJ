using UnityEngine;

public class Round2SceneController : MonoBehaviour
{
    [Header("需要失活的对象（比如第一轮的敌人）")]
    public GameObject objectToDeactivate;

    [Header("需要激活的对象（比如第二轮的Boss或新地形）")]
    public GameObject objectToActivate;

    // 用于防止重复触发（可选，看你的具体需求）
    private bool isTriggered = false;

    /// <summary>
    /// 触发第二轮的核心方法
    /// </summary>
    public void TriggerRound2()
    {
        if (isTriggered) return; // 如果已经触发过了，直接跳过
        isTriggered = true;

        // 1. 失活旧对象
        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(false);
            Debug.Log($"已将 {objectToDeactivate.name} 失活");
        }
        else
        {
            Debug.LogWarning("未指定需要失活的对象！");
        }

        // 2. 激活新对象
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log($"已将 {objectToActivate.name} 激活");
        }
        else
        {
            Debug.LogWarning("未指定需要激活的对象！");
        }
    }
}