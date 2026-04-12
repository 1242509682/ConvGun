using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using static ConvGun.Utils;

namespace ConvGun;

/// <summary>
/// 规则制作状态
/// </summary>
public class RuleMakerState
{
    public int Step; // 0空闲, 1等待源物品, 2等待目标, 3等待数量/条件
    public int SourceID;
    public int TargetID;
    public bool IsNpcTarget;       // true=怪物, false=物品
    public int Count = 1;
    public float Luck = 0;  // 默认0
    public List<string> Conds = new();
    public long ExpireFrame;       // 超时帧（30秒后自动取消）
}

public static class RuleMaker
{
    private static Dictionary<string, RuleMakerState> states = new();
    public static RuleMakerState? GetState(TSPlayer plr)
    {
        states.TryGetValue(plr.Name, out var state);
        return state;
    }

    public static void SetState(TSPlayer plr, RuleMakerState state)
    {
        if (state == null)
            states.Remove(plr.Name);
        else
            states[plr.Name] = state;
    }

    public static void Cancel(TSPlayer plr)
    {
        if (states.Remove(plr.Name))
            plr.SendMessage(Grad("已取消规则添加"), color);
        else
            plr.SendErrorMessage("当前没有进行中的规则添加");
    }

    /// <summary>开始添加规则（进入等待源物品）</summary>
    public static void Start(TSPlayer plr)
    {
        if (!CmdMain.IsAdmin(plr))
        {
            plr.SendErrorMessage("你没有权限使用此命令");
            return;
        }

        // 如果已有进行中的规则，先取消清理
        if (states.ContainsKey(plr.Name))
            Cancel(plr);

        // 显示添加规则子指令帮助
        var sb = new StringBuilder();
        sb.AppendLine("[c/61E26C:=== 添加规则流程 ===]");
        sb.AppendLine("/sg s - 设置源物品（手持）");
        sb.AppendLine("/sg sk <数量> - 设置数量（默认1）");
        sb.AppendLine("/sg cd <条件> - 设置条件（如 晚上 血月）");
        sb.AppendLine("/sg lk <幸运> - 设置幸运值（如0.5，影响掉落）");
        sb.AppendLine("/sg ok - 完成并保存规则");
        sb.AppendLine("/sg no - 取消添加");
        plr.SendMessage(Grad(sb.ToString()), color);

        var state = new RuleMakerState
        {
            Step = 1,
            ExpireFrame = Plugin.Timer + 30 * 60
        };
        SetState(plr, state);
        plr.SendMessage(Grad("\n请手持源物品，然后输入 /sg s"), color);
    }

    /// <summary>设置源物品（手持）</summary>
    public static void SetSource(TSPlayer plr)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 1)
        {
            plr.SendErrorMessage("请先输入 /sg a 开始添加规则");
            return;
        }
        var held = plr.SelectedItem;
        if (held == null || held.type == 0)
        {
            plr.SendErrorMessage("请手持一个物品作为源物品");
            return;
        }
        state.SourceID = held.type;
        state.Step = 2;
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        plr.SendMessage(Grad($"\n[c/FB6E62:注:] 源物品已设为 {Icon(state.SourceID)}"), color);
        plr.SendMessage(Grad("请用微光枪射击目标：物品/怪物"), color);
    }

    /// <summary>设置目标物品（通过射击自动识别）</summary>
    public static void SetTargetItem(TSPlayer plr, int itemId)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 2) return;
        state.TargetID = itemId;
        state.IsNpcTarget = false;
        state.Step = 3;
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        plr.SendMessage(Grad($"\n[c/FB6E62:注:] {Icon(state.SourceID)}的转换目标已设为 {Icon(itemId)}"), color);
        // 显示当前状态
        string condShow = state.Conds.Count == 0 ? "无" : string.Join(",", state.Conds);
        plr.SendMessage(Grad($"设置条件({condShow}): /sg cd"), color);
        plr.SendMessage(Grad($"修改数量({state.Count}): /sg sk"), color);
        plr.SendMessage(Grad("确认完成: /sg ok"), color);
    }

    /// <summary>设置目标怪物</summary>
    public static void SetTargetNpc(TSPlayer plr, int npcId)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 2)
        {
            plr.SendErrorMessage("请先设置源物品");
            return;
        }
        state.TargetID = npcId;
        state.IsNpcTarget = true;
        state.Step = 3;
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        plr.SendMessage(Grad($"\n[c/FB6E62:注:] {Icon(state.SourceID)}的转换目标已设为 {Lang.GetNPCNameValue(npcId)}"), color);
        string condShow = state.Conds.Count == 0 ? "无" : string.Join(",", state.Conds);
        plr.SendMessage(Grad($"设置条件({condShow}): /sg cd"), color);
        plr.SendMessage(Grad($"修改数量({state.Count}): /sg sk"), color);
        plr.SendMessage(Grad("确认完成: /sg ok"), color);
    }

    /// <summary>设置数量</summary>
    public static void SetCount(TSPlayer plr, int count)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 3)
        {
            plr.SendErrorMessage("请先设置源物品和目标");
            return;
        }
        if (count < 1) count = 1;
        state.Count = count;
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        plr.SendMessage(Grad($"\n[c/FB6E62:注:] {Icon(state.SourceID)}的转换数量已设为 {count}"), color);
        string condShow = state.Conds.Count == 0 ? "无" : string.Join(",", state.Conds);
        plr.SendMessage(Grad($"设置条件({condShow}): /sg cd"), color);
        plr.SendMessage(Grad($"修改数量({state.Count}): /sg sk"), color);
        plr.SendMessage(Grad($"修改幸运({state.Luck:F2}): /sg lk"), color);
        plr.SendMessage(Grad("确认完成: /sg ok"), color);
    }

    /// <summary>设置幸运值</summary>
    public static void SetLuck(TSPlayer plr, float luck)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 3)
        {
            plr.SendErrorMessage("请先设置源物品和目标");
            return;
        }
        state.Luck = luck;
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        plr.SendMessage(Grad($"\n[c/FB6E62:注:] {Icon(state.SourceID)}的幸运值已设为 {luck:F2}"), color);
        string condShow = state.Conds.Count == 0 ? "无" : string.Join(",", state.Conds);
        plr.SendMessage(Grad($"设置条件({condShow}): /sg cd"), color);
        plr.SendMessage(Grad($"修改数量({state.Count}): /sg sk"), color);
        plr.SendMessage(Grad($"修改幸运({state.Luck:F2}): /sg lk"), color);
        plr.SendMessage(Grad("确认完成: /sg ok"), color);
    }

    /// <summary>添加条件</summary>
    public static void AddCond(TSPlayer plr, List<string> conds)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 3)
        {
            plr.SendErrorMessage("请先设置源物品和目标");
            return;
        }
        var condNames = conds.Select(c => Utils.GetCondName(c)).ToList();
        state.Conds.AddRange(conds);
        state.Conds = state.Conds.Distinct().ToList();
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        string condShow = state.Conds.Count == 0 ? "无" : string.Join(",", state.Conds);
        plr.SendMessage(Grad($"\n[c/FB6E62:注:] 已添加条件 {string.Join(", ", condNames)}"), color);
        plr.SendMessage(Grad($"设置条件({condShow}): /sg cd"), color);
        plr.SendMessage(Grad($"修改数量({state.Count}): /sg sk"), color);
        plr.SendMessage(Grad($"修改幸运({state.Luck:F2}): /sg lk"), color);
        plr.SendMessage(Grad("确认完成: /sg ok"), color);
    }

    /// <summary>完成并保存规则</summary>
    public static void Finish(TSPlayer plr)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 3)
        {
            plr.SendErrorMessage("没有待完成的规则，请先 /sg a 开始");
            return;
        }
        string condStr = string.Join(",", state.Conds);
        var existing = Plugin.Config.ConvRules.FirstOrDefault(r => r.SourceID == state.SourceID && r.Cond == condStr);
        if (existing != null)
        {
            if (state.IsNpcTarget)
            {
                if (!existing.npcIds.Contains(state.TargetID))
                {
                    existing.npcIds.Add(state.TargetID);
                    existing.Npcs = string.Join(",", existing.npcIds); // 同步字符串
                    plr.SendMessage(Grad($"\n已追加怪物 {Lang.GetNPCNameValue(state.TargetID)} 到现有规则中"), color);
                }
                else
                    plr.SendErrorMessage("该怪物已存在于规则中");
            }
            else
            {
                if (!existing.itemIds.Contains(state.TargetID))
                {
                    existing.itemIds.Add(state.TargetID);
                    existing.Items = string.Join(",", existing.itemIds); // 同步字符串
                    plr.SendMessage(Grad($"\n已追加目标 {Icon(state.TargetID)} 到现有规则中"), color);
                }
                else
                    plr.SendErrorMessage("该目标已存在于规则中");
            }
        }
        else
        {
            var rule = new ConvRule
            {
                SourceID = state.SourceID,
                itemIds = state.IsNpcTarget ? new List<int>() : new List<int> { state.TargetID },
                npcIds = state.IsNpcTarget ? new List<int> { state.TargetID } : new List<int>(),
                Count = state.Count,
                Cond = condStr,
                Luck = state.Luck,
                Items = state.IsNpcTarget ? "" : state.TargetID.ToString(),   // 设置JSON字段
                Npcs = state.IsNpcTarget ? state.TargetID.ToString() : ""     // 设置JSON字段
            };
            Plugin.Config.ConvRules.Add(rule);
            string targetDesc = state.IsNpcTarget ? Lang.GetNPCNameValue(state.TargetID) : Icon(state.TargetID);
            plr.SendMessage(Grad($"已添加规则: {Icon(state.SourceID)} → {targetDesc} x {state.Count}"), color);
        }
        Plugin.Config.BuildRuleIdx();
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        SetState(plr, null);
        plr.SendMessage(Grad("[c/FB6E62:注:] 本次自定义规则已保存"), color);
    }

    /// <summary>超时检查（在 OnGameUpdate 中调用）</summary>
    public static void CheckTimeouts()
    {
        if (states.Count == 0) return;
        var expired = states.Where(kv => Plugin.Timer > kv.Value.ExpireFrame).Select(kv => kv.Key).ToList();
        foreach (var name in expired)
        {
            if (TShock.Players.FirstOrDefault(p => p != null && p.Name == name) is TSPlayer plr)
                plr.SendMessage(Grad("[c/FB6E62:注:] 规则添加超时,自动已取消"), color);
            states.Remove(name);
        }
    }

    /// <summary>清理玩家数据（玩家离开时）</summary>
    public static void Clear(string name) => states.Remove(name);
}