using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TShockAPI;
using static ConvGun.Utils;

namespace ConvGun;

public static class ItemSpawn
{
    public static void SpawnItem(int type, int total, Vector2 pos)
    {
        int remain = total;
        while (remain > 0)
        {
            int stack = Math.Min(remain, ContentSamples.ItemsByType[type].maxStack);
            int newIdx = Item.NewItem(null, pos, Vector2.Zero, type, stack);
            if (newIdx >= 0)
            {
                var newItem = Main.item[newIdx];
                newItem.velocity = Vector2.Zero;
                NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, newIdx);
            }
            remain -= stack;
        }
    }

    public static void SpawnAll(ConvRule rule, Vector2 pos, TSPlayer plr, int oldType, int srcStack)
    {
        int total = rule.Count * srcStack;
        foreach (int id in rule.ItemIDs)
            SpawnItem(id, total, pos);
        foreach (int id in rule.NpcIDs)
            NpcSpawn.SpawnNpc(id, total, pos, plr.Index);
        SendMsg(plr, oldType, srcStack, rule);
    }

    public static void SendMsg(TSPlayer plr, int oldType, int srcStack, ConvRule rule)
    {
        int stack = rule.Count * srcStack;
        string desc = string.Join(", ",
            rule.ItemIDs.Select(id => ItemIcon(id,stack))
            .Concat(rule.NpcIDs.Select(id => $"{Lang.GetNPCNameValue(id)}x{stack}")));

        plr.SendMessage(TextGradient($"{plr.Name} 使用 {ItemIcon(plr.SelectedItem.type)} 将 {ItemIcon(oldType, srcStack)} 转换为 {desc}"), color);
    }
}