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
        Exit = 2,          // 出口 / 通关目标
        Switch = 3,        // 开关（触发关联目标）
        Door = 4,          // 门（被开关/钥匙打开）
        Key = 5,           // 钥匙 / 可拾取道具
        NPC = 6,           // NPC（可对话，配合 Dialogue System）
        Trigger = 7,       // 触发器区域（进入时触发事件）
        PressurePlate = 8, // 压力板（踩上触发）
        Pushable = 9,      // 可推动的物块
    }
}
