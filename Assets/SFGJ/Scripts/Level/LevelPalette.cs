using UnityEngine;

namespace SFGJ.Level
{
    /// <summary>
    /// 关卡元素的配色与中文名，供编辑器可视化与运行时色块共用。
    /// </summary>
    public static class LevelPalette
    {
        public static Color GetColor(LevelElementType type)
        {
            switch (type)
            {
                case LevelElementType.Wall:          return new Color(0.42f, 0.42f, 0.46f);
                case LevelElementType.PlayerSpawn:   return new Color(0.25f, 0.80f, 0.35f);
                case LevelElementType.Portal:        return new Color(1.00f, 0.82f, 0.20f);
                case LevelElementType.Switch:        return new Color(1.00f, 0.55f, 0.15f);
                case LevelElementType.Door:          return new Color(0.60f, 0.40f, 0.22f);
                case LevelElementType.Key:           return new Color(0.20f, 0.80f, 0.90f);
                case LevelElementType.NPC:           return new Color(0.30f, 0.55f, 1.00f);
                case LevelElementType.Trigger:       return new Color(0.62f, 0.40f, 1.00f);
                case LevelElementType.PressurePlate: return new Color(1.00f, 0.45f, 0.70f);
                case LevelElementType.Pushable:      return new Color(0.82f, 0.68f, 0.45f);
                default:                              return Color.gray;
            }
        }

        public static string GetLabel(LevelElementType type)
        {
            switch (type)
            {
                case LevelElementType.Wall:          return "墙";
                case LevelElementType.PlayerSpawn:   return "出生点";
                case LevelElementType.Portal:        return "传送点";
                case LevelElementType.Switch:        return "开关";
                case LevelElementType.Door:          return "门";
                case LevelElementType.Key:           return "钥匙";
                case LevelElementType.NPC:           return "NPC";
                case LevelElementType.Trigger:       return "触发器";
                case LevelElementType.PressurePlate: return "压力板";
                case LevelElementType.Pushable:      return "推块";
                default:                              return type.ToString();
            }
        }

        /// <summary>该类型是否生成实心碰撞（不可通行）。</summary>
        public static bool IsSolid(LevelElementType type)
        {
            return type == LevelElementType.Wall
                || type == LevelElementType.Door
                || type == LevelElementType.Pushable
                || type == LevelElementType.Key
                || type == LevelElementType.NPC;
        }
    }
}
