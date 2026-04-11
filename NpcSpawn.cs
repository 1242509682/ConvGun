using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;

namespace ConvGun;

public class NpcTask
{
    public int Type;
    public Vector2 Pos;
    public long Start;
}

public static class NpcSpawn
{
    private static readonly Dictionary<int, List<NpcTask>> NpcTasks = new();

    public static void Clear(int owner)
    {
        if (NpcTasks.ContainsKey(owner))
            NpcTasks.Remove(owner);
    }

    public static void AddTask(int owner, int type, int total, Vector2 pos)
    {
        if (!NpcTasks.ContainsKey(owner))
            NpcTasks[owner] = new List<NpcTask>();

        int delay = 0; // 延迟帧计数器
        int step = Plugin.Config.SpawnDelay;
        int offPx = Plugin.Config.SpawnOffset * 16;
        for (int i = 0; i < total; i++)
        {
            Vector2 spaPos = pos;
            if (offPx > 0)
            {
                float offX = Main.rand.Next(-offPx, offPx + 1);
                float offY = Main.rand.Next(-offPx, offPx + 1);
                spaPos = pos + new Vector2(offX, offY);
                spaPos.X = Math.Clamp(spaPos.X, 32, (Main.maxTilesX - 1) * 16);
                spaPos.Y = Math.Clamp(spaPos.Y, 32, (Main.maxTilesY - 1) * 16);
            }

            NpcTasks[owner].Add(new NpcTask
            {
                Type = type,
                Pos = spaPos,
                Start = Plugin.Timer + delay,
            });

            delay += step;
        }
    }

    public static void Update(long timer)
    {
        foreach (var kv in NpcTasks.ToList())
        {
            int owner = kv.Key;
            var tasks = kv.Value;
            bool changed = false;

            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                var task = tasks[i];
                if (timer >= task.Start)
                {
                    int npcIdx = NPC.NewNPC(null, (int)task.Pos.X, (int)task.Pos.Y, task.Type);
                    if (npcIdx >= 0)
                    {
                        var npc = Main.npc[npcIdx];
                        npc.active = true;
                        npc.netUpdate = true;
                        NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npcIdx);

                        Animations.Effect(owner, task.Pos);

                        var settings = new ParticleOrchestraSettings
                        {
                            PositionInWorld = task.Pos,
                            MovementVector = new Vector2(npc.width, npc.height),
                            UniqueInfoPiece = task.Type,
                            IndexOfPlayerWhoInvokedThis = (byte)owner
                        };
                        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.InScreenDungeonSpawn, settings);
                    }
                    tasks.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed && tasks.Count == 0)
                NpcTasks.Remove(owner);
        }
    }
}