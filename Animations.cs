using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;
using TShockAPI;
using static ConvGun.Utils;

namespace ConvGun;

internal class Animations
{
    #region 动画任务（物品）
    private class AnimTask
    {
        public int OldType;
        public int NewType;
        public int Stack;
        public Vector2 Pos;
        public long Start;
    }
    private static readonly Dictionary<int, List<AnimTask>> AnimTasks = new();
    public static void Clear(int owner)
    {
        if (AnimTasks.ContainsKey(owner))
            AnimTasks.Remove(owner);
    }

    public static void AddTask(int owner, int oldType, int newType, int stack, Vector2 pos, int delay = 0)
    {
        if (!AnimTasks.ContainsKey(owner))
            AnimTasks[owner] = new List<AnimTask>();

        AnimTasks[owner].Add(new AnimTask
        {
            OldType = oldType,
            NewType = newType,
            Stack = stack,
            Pos = pos,
            Start = Plugin.Timer + delay
        });
    }

    public static void Update(long timer)
    {
        foreach (var kv in AnimTasks.ToList())
        {
            int who = kv.Key;
            var plr = TShock.Players[who];
            if (plr == null || !plr.Active)
            {
                AnimTasks.Remove(who);
                continue;
            }

            var tasks = kv.Value;
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                var task = tasks[i];
                if (timer - task.Start >= Plugin.Config.DelayFrames)
                {
                    Effect(who, task.Pos);
                    Plugin.SpawnItem(task.NewType, task.Stack, task.Pos, who);
                    tasks.RemoveAt(i);
                }
            }

            if (tasks.Count == 0)
                AnimTasks.Remove(who);
        }
    }
    #endregion

    #region 特效方法
    public static void Fly(int owner, Vector2 from, Vector2 to, int itemType)
    {
        Effect(owner, from);
        var settings = new ParticleOrchestraSettings
        {
            PositionInWorld = from,
            MovementVector = to - from,
            UniqueInfoPiece = itemType,
            IndexOfPlayerWhoInvokedThis = (byte)owner
        };
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.ItemTransfer, settings);
    }

    public static void Effect(int owner, Vector2 pos)
    {
        var settings = new ParticleOrchestraSettings
        {
            PositionInWorld = pos,
            MovementVector = Vector2.Zero,
            UniqueInfoPiece = (int)color2.PackedValue,
            IndexOfPlayerWhoInvokedThis = (byte)owner
        };
        var rand = Plugin.Config.AnimType[Main.rand.Next(Plugin.Config.AnimType.Length)];
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(rand, settings);
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.DeadCellsMushroomBoiTargetFound, settings);
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.HeroicisSetSpawnSound, settings);
    }
    #endregion
}
