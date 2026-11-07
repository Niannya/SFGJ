namespace SFGJ.Level
{
    /// <summary>
    /// 关卡元素类型。
    /// 覆盖老式俯视角解密 RPG 的常见元素，按需扩展即可。
    /// </summary>
    public enum LevelElementType
    {
        Wall = 0,          // 墙壁 / 障碍物（不可通行）
        PlayerSpawn = 1,   // 玩家出生点
        Portal = 2,        // 传送点（进入时传送到绑定的目标传送点；入口/出口统一为此类型）
        Switch = 3,        // 开关（触发关联目标）
        Door = 4,          // 门（被开关/钥匙打开）
        Key = 5,           // 钥匙（交互：先对话，对话结束后触发事件）
        NPC = 6,           // NPC（交互：只触发对话）
        Trigger = 7,       // 触发器区域（进入时自动执行事件）
        PressurePlate = 8, // 压力板（踩上触发）
        Pushable = 9,      // 可推动的物块
    }
}
