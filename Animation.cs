using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;
using TShockAPI;

namespace ConvGun;

public enum AnimType { Fly, Spawn }

public class AnimReq
{
    public AnimType Type;
    public int ItemType;
    public Vector2 From;
    public Vector2 To;
    public int NewType;
    public TSPlayer? plr;
    public int SrcStack;
    public int OldType;
    public ConvRule? Rule;
}

public class AnimData
{
    public Queue<AnimReq> Queue = new();
    public long NextFrame = 0;
    public int OwnerIdx = -1;
    public int ItemIdx = -1;
}

public static class Animation
{
    public static List<AnimData> AnimList = new();
    public static void Clear(int idx) => AnimList.RemoveAll(anim => anim.OwnerIdx == idx);
    public static void Update(long timer)
    {
        var toRemove = new List<AnimData>();
        foreach (var anim in AnimList)
        {
            if (anim.Queue.Count == 0)
            {
                toRemove.Add(anim);
                continue;
            }
            if (timer < anim.NextFrame) continue;

            var req = anim.Queue.Dequeue();
            switch (req.Type)
            {
                case AnimType.Fly:
                    PlayFly(req.From, req.To, req.ItemType);
                    break;
                case AnimType.Spawn:
                    int newIdx = Item.NewItem(null, req.From, Vector2.Zero, req.NewType, 1);
                    if (newIdx >= 0)
                    {
                        var newItem = Main.item[newIdx];
                        newItem.velocity = Vector2.Zero;
                        NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, newIdx);
                    }

                    if (req.plr != null && req.Rule != null)
                        ItemSpawn.SendMsg(req.plr, req.OldType, req.SrcStack, req.Rule);
                    break;
            }
            anim.NextFrame = timer + Plugin.Config.DelayFrames;
        }
        foreach (var anim in toRemove) AnimList.Remove(anim);
    }

    private static void PlayFly(Vector2 from, Vector2 to, int itemType)
    {
        var fly = new ParticleOrchestraSettings
        {
            PositionInWorld = from,
            MovementVector = to - from,
            UniqueInfoPiece = itemType,
            IndexOfPlayerWhoInvokedThis = 0
        };
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.ItemTransfer, fly);

        var startFlash = new ParticleOrchestraSettings
        {
            PositionInWorld = from,
            MovementVector = Vector2.Zero,
            UniqueInfoPiece = 0,
            IndexOfPlayerWhoInvokedThis = 0
        };
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.RainbowBoulder4, startFlash);

        var endFlash = new ParticleOrchestraSettings
        {
            PositionInWorld = to,
            MovementVector = Vector2.Zero,
            UniqueInfoPiece = 0,
            IndexOfPlayerWhoInvokedThis = 0
        };
        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.ShimmerArrow, endFlash);
    }
}