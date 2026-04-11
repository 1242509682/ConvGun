using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static ConvGun.Utils;

namespace ConvGun;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    #region 插件信息
    public static string PluginName => "微光转换枪";
    public override string Name => PluginName + "加强版";
    public override string Author => "羽学、星梦";
    public override Version Version => new(1, 0, 2);
    public override string Description => "微光枪命中掉落物时，按规则转换，支持条件与动画；无规则时回退原版微光或分解";
    #endregion

    #region 文件路径
    public static readonly string ConfigPath = Path.Combine(TShock.SavePath, $"{PluginName}.json");
    #endregion

    #region 注册与释放
    public Plugin(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.ProjectileAIUpdate.Register(this, OnProjAI);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
        Commands.ChatCommands.Add(new Command(CmdMain.perm, CmdMain.SgCmd, "sg"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.ProjectileAIUpdate.Deregister(this, OnProjAI);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
            Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == CmdMain.SgCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置加载
    internal static Configuration Config = new();
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendMessage($"[{PluginName}] 配置重载完毕。", color);
    }
    private static void LoadConfig()
    {
        try
        {
            Config = Configuration.Read();
            Utils.InitCondMap(Config);   // 确保映射表与配置一致
            Config.Write();
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{PluginName}] 配置文件加载失败：\n{ex.Message}");
        }
    }
    #endregion

    #region 玩家离开事件
    private void OnServerLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr != null) RuleMaker.Clear(plr.Name);

        if (ProjCD.ContainsKey(args.Who))
            ProjCD.Remove(args.Who);

        Animations.Clear(args.Who);
        NpcSpawn.Clear(args.Who);
    }
    #endregion

    #region 弹幕碰撞检测
    private static readonly Dictionary<int, long> ProjCD = new();
    private void OnProjAI(ProjectileAiUpdateEventArgs args)
    {
        if (!Config.Enabled) return;
        var proj = args.Projectile;
        if (proj == null || !proj.active) return;
        if (proj.type != ProjectileID.ShimmerGunStream) return;

        var plr = TShock.Players[proj.owner];
        if (plr == null || !plr.Active || !plr.HasPermission(CmdMain.perm)) return;

        if (ProjCD.TryGetValue(proj.owner, out long last) && Timer - last < Config.Sec * 60) return;

        var box = proj.Hitbox;
        box.Inflate(Config.Hitbox, Config.Hitbox);
        float rangeSq = (Config.Hitbox * 16 + 100) * (Config.Hitbox * 16 + 100); // 距离平方粗筛

        // 规则制作模式
        var state = RuleMaker.GetState(plr);
        if (state != null && state.Step == 2)
        {
            for (int n = 0; n < Main.maxNPCs; n++)
            {
                var npc = Main.npc[n];
                if (!npc.active) continue;
                if (npc.townNPC || npc.friendly || npc.SpawnedFromStatue ||
                    npc.type == NPCID.TargetDummy || npc.type == NPCID.WallofFlesh) continue;
                if (box.Intersects(npc.Hitbox))
                {
                    RuleMaker.SetTargetNpc(plr, npc.type);
                    return;
                }
            }
        }

        // 物品检测（增加距离过滤）
        for (int i = 0; i < Main.maxItems; i++)
        {
            var item = Main.item[i];
            if (item == null || !item.active) continue;

            // 距离粗筛
            float dx = item.Center.X - proj.Center.X;
            float dy = item.Center.Y - proj.Center.Y;
            if (dx * dx + dy * dy > rangeSq) continue;
            if (!box.Intersects(item.Hitbox)) continue;

            if (state != null && state.Step == 2)
            {
                RuleMaker.SetTargetItem(plr, item.type);
                return;
            }

            int itemType = item.type;
            int srcStack = item.stack;
            Vector2 from = item.Center;
            Vector2 pos = from - new Vector2(0, Config.Height * 16);

            // 1. 自定义规则（使用索引和整数条件）
            if (Config.ruleMap.TryGetValue(itemType, out var candidates))
            {
                // 找出所有匹配当前条件的规则
                var matched = candidates.Where(r => Utils.CheckConds(r.condIds, plr.TPlayer)).ToList();
                if (matched.Count > 0)
                {
                    ProjCD[proj.owner] = Timer;
                    item.TurnToAir();
                    NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, i);
                    Animations.Fly(proj.owner, from, pos, itemType);

                    foreach (var rule in matched)   // 执行所有匹配的规则
                    {
                        int total = rule.Count * srcStack;
                        foreach (int id in rule.itemIds)
                            Animations.AddTask(proj.owner, itemType, id, total, pos);
                        foreach (int npcId in rule.npcIds)
                            NpcSpawn.AddTask(proj.owner, npcId, total, from);
                        SendMsg(plr, itemType, srcStack, rule);
                    }
                    return;
                }
            }

            // 2. 原版微光转换（已使用缓存）
            int origType = Utils.GetShimmerTransform(itemType);
            if (origType != 0 && origType != itemType)
            {
                ProjCD[proj.owner] = Timer;
                item.TurnToAir();
                NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, i);

                int origStack = srcStack;
                if (ItemID.Sets.CommonCoin[itemType])
                {
                    switch (origType)
                    {
                        case ItemID.SilverCoin: origStack = srcStack * 100; break;
                        case ItemID.GoldCoin: origStack = srcStack * 100; break;
                        case ItemID.PlatinumCoin: origStack = srcStack * 100; break;
                    }
                    if (itemType == ItemID.PlatinumCoin && origType == ItemID.GoldCoin)
                        origStack = srcStack / 100;
                    if (origStack < 1) origStack = 1;
                }

                Animations.Fly(proj.owner, from, pos, itemType);
                Animations.AddTask(proj.owner, itemType, origType, origStack, pos);
                plr.SendMessage(Grad($"{plr.Name} 使用 {Icon(plr.SelectedItem.type)} 将 {Icon(itemType, srcStack)} 转换为 {Icon(origType, origStack)}"), color);
                return;
            }

            // 3. 原版分解（已使用缓存）
            var mats = Utils.GetDecraft(itemType, srcStack);
            if (mats.Count > 0)
            {
                ProjCD[proj.owner] = Timer;
                item.TurnToAir();
                NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, i);

                int delay = 0;
                foreach (var mat in mats)
                {
                    Vector2 matPos = pos;
                    int offPx = Config.SpawnOffset * 16;
                    if (offPx > 0)
                    {
                        float offX = Main.rand.Next(-offPx, offPx + 1);
                        float offY = Main.rand.Next(-offPx, offPx + 1);
                        matPos = pos + new Vector2(offX, offY);
                        matPos.X = Math.Clamp(matPos.X, 32, (Main.maxTilesX - 1) * 16);
                        matPos.Y = Math.Clamp(matPos.Y, 32, (Main.maxTilesY - 1) * 16);
                    }
                    Animations.AddTask(proj.owner, itemType, mat.typ, mat.stack, matPos, delay);
                    delay += Config.SpawnDelay;
                }

                Animations.Fly(proj.owner, from, pos, itemType);
                string matsStr = string.Join(" ", mats.Select(m => Icon(m.typ, m.stack)));
                plr.SendMessage(Grad($"{plr.Name} 使用 {Icon(plr.SelectedItem.type)} 将 {Icon(itemType, srcStack)} 分解出\n {matsStr}"), color2);
                return;
            }
        }
    }
    #endregion

    #region 游戏更新
    public static long Timer = 0;
    private void OnGameUpdate(EventArgs args)
    {
        if (!Config.Enabled) return;

        Timer++;

        if (Timer % 600 == 0) ProjCD.Clear();

        Animations.Update(Timer);
        NpcSpawn.Update(Timer);
        RuleMaker.CheckTimeouts();
    }
    #endregion

    #region 物品生成
    public static void SpawnItem(int type, int total, Vector2 pos, int owner)
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
                newItem.playerIndexTheItemIsReservedFor = owner;
                NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, newIdx);
            }
            remain -= stack;
        }
    }
    #endregion

    #region 发送消息
    public static void SendMsg(TSPlayer plr, int oldType, int srcStack, ConvRule rule)
    {
        int stack = rule.Count * srcStack;
        string desc = string.Join("、",
            rule.itemIds.Select(id => Icon(id, stack))
            .Concat(rule.npcIds.Select(id => $"{Lang.GetNPCNameValue(id)}x{stack}")));

        plr.SendMessage(Grad($"{plr.Name} 使用 {Icon(plr.SelectedItem.type)} 将 {Icon(oldType, srcStack)} 转换为 {desc}"), color);
    } 
    #endregion
}