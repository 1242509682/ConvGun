using Microsoft.Xna.Framework;
using Terraria;
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
    public override string Name => PluginName;
    public override string Author => "羽学";
    public override Version Version => new(1, 0, 0);
    public override string Description => "使用微光枪命中掉落物时，按规则转换为指定物品/怪物，支持条件与动画";
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
            Config.Write();
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[{PluginName}] 配置文件加载失败：\n{ex.Message}");
        }
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
        if (plr == null || !plr.Active) return;

        if (ProjCD.TryGetValue(proj.owner, out long last) && Timer - last < Config.Sec * 60) return;

        var box = proj.Hitbox;
        box.Inflate(Config.Hitbox, Config.Hitbox);

        // 提前获取玩家状态，仅当需要设置目标怪物时才遍历 NPC
        var makerState = RuleMaker.GetState(plr);
        if (makerState != null && makerState.Step == 2)
        {
            for (int n = 0; n < Main.maxNPCs; n++)
            {
                var npc = Main.npc[n];
                if (!npc.active) continue;
                if (npc.townNPC || npc.friendly ||
                    npc.SpawnedFromStatue ||
                    npc.type == NPCID.TargetDummy ||
                    npc.type == NPCID.WallofFlesh) continue;

                if (box.Intersects(npc.Hitbox))
                {
                    RuleMaker.SetTargetNpc(plr, npc.type);
                    return; // 命中怪物且处于规则制作状态，直接返回，不执行转换
                }
            }
        }

        var items = Main.item.AsSpan();
        for (int i = 0; i < items.Length; i++)
        {
            ref WorldItem item = ref items[i];
            if (item == null || !item.active) continue;

            if (!box.Intersects(item.Hitbox)) continue;

            // 规则制作：命中物品
            if (makerState != null && makerState.Step == 2)
            {
                RuleMaker.SetTargetItem(plr, item.type);
                return;
            }

            // 正常转换逻辑：先检查是否有匹配规则，再更新冷却
            int itemType = item.type;
            // 获取所有匹配的规则
            var rules = Config.ConvRules.Where(r => r.SourceID == itemType &&
            (string.IsNullOrEmpty(r.Cond) || CheckConds(r.CondList, plr.TPlayer))).ToList();
            if (rules.Count == 0) continue; // 没有规则，跳过

            ProjCD[proj.owner] = Timer;

            int srcStack = item.stack;
            int oldType = item.type;
            Vector2 pos = item.Center;

            // 删除原物品
            item.TurnToAir();
            NetMessage.SendData((int)PacketTypes.UpdateItemDrop, -1, -1, null, i);

            // 无动画：每个规则分别生成
            if (!Config.UseAnim)
            {
                foreach (var rule in rules)
                    ItemSpawn.SpawnAll(rule, pos, plr, oldType, srcStack);
                return;
            }

            // 动画模式
            bool animated = false;
            foreach (var rule in rules)
            {
                int perCount = rule.Count;
                int total = perCount * srcStack;

                // 处理物品
                foreach (int id in rule.ItemIDs)
                {
                    if (!animated && id == rule.ItemIDs.FirstOrDefault())
                    {
                        // 第一个规则的第一个物品：播放飞行动画（只生成1个）
                        Vector2 fromPos = pos;
                        Vector2 toPos = fromPos - new Vector2(0, Config.Height * 16);
                        var anim = new AnimData();
                        anim.OwnerIdx = plr.Index;
                        anim.ItemIdx = i;
                        anim.Queue.Enqueue(new AnimReq { Type = AnimType.Fly, ItemType = oldType, From = fromPos, To = toPos });
                        anim.Queue.Enqueue(new AnimReq { plr = plr, Type = AnimType.Spawn, ItemType = oldType, NewType = id, From = toPos, SrcStack = srcStack, OldType = oldType, Rule = rule });
                        anim.NextFrame = Timer;
                        Animation.AnimList.Add(anim);
                        animated = true;

                        // 如果数量大于1，多余部分原地生成
                        if (total > 1)
                            ItemSpawn.SpawnItem(id, total - 1, pos);
                    }
                    else
                    {
                        // 其他规则或剩余数量原地生成
                        ItemSpawn.SpawnItem(id, total, pos);
                    }
                }
                // 处理怪物（始终原地生成，延迟队列）
                foreach (int npcId in rule.NpcIDs)
                    NpcSpawn.SpawnNpc(npcId, total, pos, plr.Index);
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

        Animation.Update(Timer);
        NpcSpawn.Update(Timer);
        RuleMaker.CheckTimeouts();

        if (Timer % 600 == 0) ProjCD.Clear();
    }
    #endregion

    #region 玩家离开事件
    private void OnServerLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr != null) RuleMaker.Clear(plr.Name);

        if (ProjCD.ContainsKey(args.Who))
            ProjCD.Remove(args.Who);

        Animation.Clear(args.Who);
        NpcSpawn.Clear(args.Who);
    }
    #endregion
}