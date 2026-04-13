using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;

namespace ConvGun;

public class NpcTask
{
    public int Owner;
    public int Type;
    public Vector2 Pos;
    public long Start;
}

public static class NpcSpawn
{
    private static List<NpcTask> spaList = new();

    public static void Clear(int owner)
    {
        for (int i = spaList.Count - 1; i >= 0; i--)
        {
            if (spaList[i].Owner == owner)
                spaList.RemoveAt(i);
        }
    }

    public static void AddTask(int owner, int type, int total, Vector2 pos)
    {
        int delay = 0;
        for (int i = total - 1; i >= 0; i--)
        {
            Vector2 spaPos = pos;
            int offPx = Plugin.Config.SpawnOff * 16;
            if (offPx > 0)
            {
                float offX = Main.rand.Next(-offPx, offPx + 1);
                float offY = Main.rand.Next(-offPx, offPx + 1);
                spaPos = pos + new Vector2(offX, offY);
                spaPos.X = Math.Clamp(spaPos.X, 32, (Main.maxTilesX - 1) * 16);
                spaPos.Y = Math.Clamp(spaPos.Y, 32, (Main.maxTilesY - 1) * 16);
            }

            spaList.Add(new NpcTask
            {
                Owner = owner,
                Type = type,
                Pos = spaPos,
                Start = Plugin.Timer + delay
            });
            if (Plugin.Config.Delay)
                delay += Plugin.Config.DelayTime;
        }
    }

    public static void Update(long timer)
    {
        if (spaList.Count == 0) return;

        for (int i = spaList.Count - 1; i >= 0; i--)
        {
            var task = spaList[i];
            if (timer >= task.Start)
            {
                int npcIdx = NPC.NewNPC(null, (int)task.Pos.X, (int)task.Pos.Y, task.Type);
                if (npcIdx >= 0)
                {
                    var npc = Main.npc[npcIdx];
                    npc.active = true;
                    npc.netUpdate = true;
                    NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npcIdx);

                    Animations.Effect(task.Pos);

                    var settings = new ParticleOrchestraSettings
                    {
                        PositionInWorld = task.Pos,
                        MovementVector = new Vector2(npc.width, npc.height),
                        UniqueInfoPiece = task.Type,
                        IndexOfPlayerWhoInvokedThis = (byte)task.Owner
                    };
                    ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.InScreenDungeonSpawn, settings);
                }
                spaList.RemoveAt(i);
            }
        }
    }
}