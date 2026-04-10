using Microsoft.Xna.Framework;
using Terraria;

namespace ConvGun;

public class NpcSpawnTask
{
    public int Type;
    public Vector2 Pos;
    public long StartFrame;
    public int OwnerIdx = -1;
}

public static class NpcSpawn
{
    private static List<NpcSpawnTask> NpcQueue = new();
    public static void Clear(int idx) => NpcQueue.RemoveAll(t => t.OwnerIdx == idx);
    public static void SpawnNpc(int type, int total, Vector2 pos, int ownerIdx = -1)
    {
        int offsetPx = Plugin.Config.SpawnOffset * 16;
        for (int i = 0; i < total; i++)
        {
            Vector2 spawnPos = pos;
            if (offsetPx > 0)
            {
                float offX = Main.rand.Next(-offsetPx, offsetPx + 1);
                float offY = Main.rand.Next(-offsetPx, offsetPx + 1);
                spawnPos = pos + new Vector2(offX, offY);
                spawnPos.X = Math.Clamp(spawnPos.X, 32, (Main.maxTilesX - 1) * 16);
                spawnPos.Y = Math.Clamp(spawnPos.Y, 32, (Main.maxTilesY - 1) * 16);
            }

            NpcQueue.Add(new NpcSpawnTask
            {
                Type = type,
                Pos = spawnPos,
                StartFrame = Plugin.Timer + i * Plugin.Config.SpawnDelay,
                OwnerIdx = ownerIdx
            });
        }
    }

    public static void Update(long timer)
    {
        for (int i = 0; i < NpcQueue.Count; i++)
        {
            var task = NpcQueue[i];
            if (timer >= task.StartFrame)
            {
                int npcIdx = NPC.NewNPC(null, (int)task.Pos.X, (int)task.Pos.Y, task.Type);
                if (npcIdx >= 0)
                {
                    var npc = Main.npc[npcIdx];
                    npc.active = true;
                    npc.netUpdate = true;
                    NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, null, npcIdx);
                }
                NpcQueue.RemoveAt(i);
                i--;
            }
        }
    }
}