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
    public List<string> Conds = new();
    public long ExpireFrame;       // 超时帧（30秒后自动取消）
}

public static class RuleMaker
{
    private static Dictionary<string, RuleMakerState> _states = new();
    public static RuleMakerState? GetState(TSPlayer plr)
    {
        _states.TryGetValue(plr.Name, out var state);
        return state;
    }

    public static void SetState(TSPlayer plr, RuleMakerState state)
    {
        if (state == null)
            _states.Remove(plr.Name);
        else
            _states[plr.Name] = state;
    }

    public static void Cancel(TSPlayer plr)
    {
        if (_states.Remove(plr.Name))
            plr.SendMessage(TextGradient("已取消规则添加"), color);
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
        if (_states.ContainsKey(plr.Name))
            Cancel(plr);

        var state = new RuleMakerState
        {
            Step = 1,
            ExpireFrame = Plugin.Timer + 30 * 60
        };
        SetState(plr, state);
        plr.SendMessage(TextGradient("请手持源物品，然后输入 /sg s"), color);
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
        plr.SendMessage(TextGradient($"源物品已设为 {ItemIcon(state.SourceID)}"), color);
        plr.SendMessage(TextGradient("• 手持目标物品后输入 /sg t"), color);
        plr.SendMessage(TextGradient("• 或用微光枪射击目标物品/怪物"), color);
    }

    /// <summary>设置目标物品（手持）</summary>
    public static void SetTargetItem(TSPlayer plr)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 2)
        {
            plr.SendErrorMessage("请先设置源物品");
            return;
        }
        var held = plr.SelectedItem;
        if (held == null || held.type == 0)
        {
            plr.SendErrorMessage("请手持目标物品");
            return;
        }
        state.TargetID = held.type;
        state.IsNpcTarget = false;
        state.Step = 3; // 等待数量和条件
        state.ExpireFrame = Plugin.Timer + 30 * 60; // 重置超时
        plr.SendMessage(TextGradient($"目标物品已设为 {ItemIcon(state.TargetID)}"), color);
        plr.SendMessage(TextGradient("[可选]设置数量: /sg sk 2"), color);
        plr.SendMessage(TextGradient("[可选]设置条件: /sg cd 条件名"), color);
        plr.SendMessage(TextGradient("[可选]查看条件: /sg all"), color);
        plr.SendMessage(TextGradient("[必选]确认完成: /sg ok"), color);
    }

    public static void SetTargetItem(TSPlayer plr, int itemId)
    {
        var state = GetState(plr);
        if (state == null || state.Step != 2) return;
        state.TargetID = itemId;
        state.IsNpcTarget = false;
        state.Step = 3;
        state.ExpireFrame = Plugin.Timer + 30 * 60;
        plr.SendMessage(TextGradient($"目标物品已设为 {ItemIcon(itemId)}"), color);
        plr.SendMessage(TextGradient("[可选]设置数量: /sg sk 2"), color);
        plr.SendMessage(TextGradient("[可选]设置条件: /sg cd 条件名"), color);
        plr.SendMessage(TextGradient("[可选]查看条件: /sg all"), color);
        plr.SendMessage(TextGradient("[必选]确认完成: /sg ok"), color);
    }

    /// <summary>设置目标怪物（通过射击自动识别，或列表选择）</summary>
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
        plr.SendMessage(TextGradient($"目标怪物已设为 {Lang.GetNPCNameValue(npcId)}"), color);
        plr.SendMessage(TextGradient("[可选]设置数量: /sg sk 2"), color);
        plr.SendMessage(TextGradient("[可选]设置条件: /sg cd 条件名"), color);
        plr.SendMessage(TextGradient("[可选]查看条件: /sg all"), color);
        plr.SendMessage(TextGradient("[必选]确认完成: /sg ok"), color);
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
        state.ExpireFrame = Plugin.Timer + 30 * 60; // 重置超时
        plr.SendMessage(TextGradient($"数量已设为 {count}"), color);
        plr.SendMessage(TextGradient("确认完成: /sg ok"), color);
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
        state.Conds.AddRange(conds);
        state.Conds = state.Conds.Distinct().ToList(); // 去重
        state.ExpireFrame = Plugin.Timer + 30 * 60; // 重置超时
        plr.SendMessage(TextGradient($"已添加条件: {string.Join(", ", conds)}"), color);
        plr.SendMessage(TextGradient("确认完成: /sg ok"), color);
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
                if (!existing.NpcIDs.Contains(state.TargetID))
                {
                    existing.NpcIDs.Add(state.TargetID);
                    plr.SendMessage(TextGradient($"已追加怪物 {Lang.GetNPCNameValue(state.TargetID)} 到现有规则中"), color);
                }
                else
                    plr.SendErrorMessage("该怪物已存在于规则中");
            }
            else
            {
                if (!existing.ItemIDs.Contains(state.TargetID))
                {
                    existing.ItemIDs.Add(state.TargetID);
                    plr.SendMessage(TextGradient($"已追加目标 {ItemIcon(state.TargetID)} 到现有规则中"), color);
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
                ItemIDs = state.IsNpcTarget ? new List<int>() : new List<int> { state.TargetID },
                NpcIDs = state.IsNpcTarget ? new List<int> { state.TargetID } : new List<int>(),
                Count = state.Count,
                Cond = condStr
            };
            Plugin.Config.ConvRules.Add(rule);
            string targetDesc = state.IsNpcTarget ? Lang.GetNPCNameValue(state.TargetID) : ItemIcon(state.TargetID);
            plr.SendMessage(TextGradient($"已添加规则: {ItemIcon(state.SourceID)} → {targetDesc} x{state.Count}"), color);
        }
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        SetState(plr, null);
        plr.SendMessage(TextGradient("规则已保存"), color);
    }

    /// <summary>超时检查（在 OnGameUpdate 中调用）</summary>
    public static void CheckTimeouts()
    {
        var expired = _states.Where(kv => Plugin.Timer > kv.Value.ExpireFrame).Select(kv => kv.Key).ToList();
        foreach (var name in expired)
        {
            if (TShock.Players.FirstOrDefault(p => p != null && p.Name == name) is TSPlayer plr)
                plr.SendMessage("规则添加超时，已取消", Color.Red);
            _states.Remove(name);
        }
    }

    /// <summary>清理玩家数据（玩家离开时）</summary>
    public static void Clear(string name) => _states.Remove(name);
}