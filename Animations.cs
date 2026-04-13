using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;
using static ConvGun.Utils;

namespace ConvGun;

public class Animations
{
    #region 物品动画任务（生成转换后的物品）
    private class AnimTask
    {
        public int OldType;
        public int NewType;
        public int Stack;
        public Vector2 To;
        public long Start;
    }
    private static List<AnimTask> anims = new();

    public static void AddTask(int oldType, int newType, int stack, Vector2 to, int delay = 0)
    {
        anims.Add(new AnimTask
        {
            OldType = oldType,
            NewType = newType,
            Stack = stack,
            To = to,
            Start = Plugin.Timer + delay
        });
    }

    public static void Update(long timer)
    {
        if (anims.Count == 0) return;

        for (int i = anims.Count - 1; i >= 0; i--)
        {
            var task = anims[i];
            if (timer - task.Start >= Plugin.Config.AnimTime)
            {
                Effect(task.To);
                Plugin.SpawnItem(task.NewType, task.Stack, task.To);
                anims.RemoveAt(i);
            }
        }
    }
    #endregion

    #region 基础特效
    public static void Fly(Vector2 from, Vector2 to, int itemType)
    {
        Effect(from);
        var settings = new ParticleOrchestraSettings
        {
            PositionInWorld = from,
            MovementVector = to - from,
            UniqueInfoPiece = itemType,
            IndexOfPlayerWhoInvokedThis = 0
        };
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.ItemTransfer, settings);
    }

    public static void Effect(Vector2 pos)
    {
        var settings = new ParticleOrchestraSettings
        {
            PositionInWorld = pos,
            MovementVector = Vector2.Zero,
            UniqueInfoPiece = (int)color2.PackedValue,
            IndexOfPlayerWhoInvokedThis = 0
        };
        var rand = Plugin.Config.AnimType[Main.rand.Next(Plugin.Config.AnimType.Length)];
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(rand, settings);
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.DeadCellsMushroomBoiTargetFound, settings);
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.HeroicisSetSpawnSound, settings);
    }
    #endregion
}