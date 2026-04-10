using System.Text;
using Terraria;
using TShockAPI;
using static ConvGun.Utils;

namespace ConvGun;

public static class CmdMain
{
    #region 权限检查
    public static string perm = "sg.use";
    public static bool IsAdmin(TSPlayer plr)
    {
        return plr.HasPermission("sg.admin");
    }
    #endregion

    #region 主指令
    public static void SgCmd(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            Help(args.Player);
            return;
        }

        var plr = args.Player;
        var sub = args.Parameters[0].ToLower();

        switch (sub)
        {
            case "add":
            case "a":
                RuleMaker.Start(plr);
                break;
            case "s":
            case "set":
            case "src":
                RuleMaker.SetSource(plr);
                break;
            case "t":
            case "tgt":
            case "target":
                RuleMaker.SetTargetItem(plr);
                break;
            case "sk":
            case "count":
                if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int c))
                    RuleMaker.SetCount(plr, c);
                else
                    plr.SendErrorMessage("用法: /sg sk <数量>");
                break;
            case "cd":
            case "cond":
                if (args.Parameters.Count > 1)
                {
                    var conds = args.Parameters.Skip(1).ToList();
                    RuleMaker.AddCond(plr, conds);
                }
                else
                    plr.SendErrorMessage("用法: /sg cd <条件1> [条件2] ...");
                break;
            case "all":
                ListConditions(args);
                break;
            case "ok":
            case "done":
                RuleMaker.Finish(plr);
                break;
            case "no":
            case "cancel":
                RuleMaker.Cancel(plr);
                break;
            case "list":
            case "ls":
                ListRules(args);
                break;
            case "reset":
            case "rs":
            case "r":
                ClearRules(args);
                break;
            case "fix":
            case "f":
                ResetRules(args);
                break;
            case "info":
            case "if":
            case "i":
                ShowInfo(args);
                break;
            case "help":
            case "?":
            default:
                Help(plr);
                break;
        }
    }
    #endregion

    #region 帮助菜单
    private static void Help(TSPlayer plr)
    {
        if (plr.RealPlayer)
            plr.SendMessage("\n[i:3455][c/AD89D5:微光][c/D68ACA:转][c/DF909A:换][c/E5A894:枪][i:3454] " +
                           "[i:3456][C/F2F2C7:开发] [C/BFDFEA:by] [c/00FFFF:羽学] [i:3459]", color);
        else
            plr.SendMessage($"[c/AD89D5:微光转换枪] [c/BFDFEA:by 羽学]", color);


        var sb = new StringBuilder();
        if (IsAdmin(plr))
        {
            sb.AppendLine($"/sg a - 开始添加规则");
            sb.AppendLine($"/sg s - 设置源物品（手持）");
            sb.AppendLine($"/sg t - 设置目标物品（手持或射击）");
            sb.AppendLine($"/sg sk <数量> - 设置数量（默认1）");
            sb.AppendLine($"/sg cd <条件...> - 设置条件（如 晚上 血月）");
            sb.AppendLine($"/sg ok - 完成并保存规则");
            sb.AppendLine($"/sg no - 取消添加");
            sb.AppendLine($"/sg rs - 清空所有规则");
            sb.AppendLine($"/sg fix - 恢复默认规则");
        }
        sb.AppendLine($"/sg all - 查看所有可用条件");
        sb.AppendLine($"/sg ls [页数] - 列出所有规则");
        sb.AppendLine($"/sg if - 查看插件状态");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(sb.ToString()), color);
        else
            plr.SendMessage(sb.ToString(), color);
    }
    #endregion

    #region 列出所有可用条件（参考配置中的 Reference）
    private static void ListConditions(CommandArgs args)
    {
        var refs = Plugin.Config.Reference;
        if (refs == null || refs.Count == 0)
        {
            args.Player.SendInfoMessage("暂无条件参考");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[c/47D3C3:可用条件参考（数字或中文）]");
        foreach (var line in refs)
        {
            // 按 '|' 分割，去除空格，过滤空项
            var parts = line.Split('|').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
            // 每6个一组显示
            for (int i = 0; i < parts.Count; i += 6)
            {
                var group = parts.Skip(i).Take(6);
                sb.AppendLine(string.Join(" ", group));
            }
        }
        args.Player.SendMessage(TextGradient(sb.ToString()), color);
    }
    #endregion

    #region 列出规则
    private static void ListRules(CommandArgs args)
    {
        var rules = Plugin.Config.ConvRules;
        if (rules.Count == 0)
        {
            args.Player.SendInfoMessage("暂无转换规则");
            return;
        }

        int page = 1;
        if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int p))
            page = p;
        if (page < 1) page = 1;

        int perPage = 8;
        int total = (int)Math.Ceiling((double)rules.Count / perPage);
        if (page > total) page = total;

        int start = (page - 1) * perPage;
        int end = Math.Min(start + perPage, rules.Count);

        var sb = new StringBuilder();
        sb.AppendLine($"[c/47D3C3:转换规则列表 (第{page}/{total}页)]");

        for (int i = start; i < end; i++)
        {
            var r = rules[i];
            string cond = !string.IsNullOrEmpty(r.Cond) ? $"[c/FFAA6D:({r.Cond})]" : string.Empty;
            string targets = string.Join(", ",
                r.ItemIDs.Select(id => ItemIcon(id, r.Count))
                .Concat(r.NpcIDs.Select(id => $"{Lang.GetNPCNameValue(id)}x{r.Count}")));
            sb.AppendLine($"[c/55CDFF:{i + 1:00}.] {ItemIcon(r.SourceID)} {cond} → {targets}");
        }

        if (total > 1)
            sb.AppendLine($"[c/FFFF00:/sg ls {page + 1} 查看下一页]");

        args.Player.SendMessage(TextGradient(sb.ToString()), color);
    }
    #endregion

    #region 清空规则
    private static void ClearRules(CommandArgs args)
    {
        if (!IsAdmin(args.Player))
        {
            args.Player.SendErrorMessage("你没有权限使用此命令");
            return;
        }

        Plugin.Config.ConvRules.Clear();
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        args.Player.SendMessage(TextGradient("已清空所有转换规则"), color);
    }
    #endregion

    #region 恢复默认规则
    private static void ResetRules(CommandArgs args)
    {
        if (!IsAdmin(args.Player))
        {
            args.Player.SendErrorMessage("你没有权限使用此命令");
            return;
        }

        Plugin.Config.SetDefault();
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        args.Player.SendMessage(TextGradient("已恢复所有默认转换规则"), color);
    }
    #endregion

    #region 查看状态
    private static void ShowInfo(CommandArgs args)
    {
        var plr = args.Player;
        var cfg = Plugin.Config;
        var sb = new StringBuilder();
        sb.AppendLine($"[c/AD89D5:微光转换枪]");
        sb.AppendLine($"插件开关: {(cfg.Enabled ? "[c/61E26C:开启]" : "[c/FF716D:关闭]")}");
        sb.AppendLine($"启用动画: {(cfg.UseAnim ? "[c/61E26C:开启]" : "[c/FF716D:关闭]")}");
        sb.AppendLine($"动画帧数: {cfg.DelayFrames}");
        sb.AppendLine($"规则数量: {cfg.ConvRules.Count}");
        sb.AppendLine($"冷却秒数: {cfg.Sec}");
        sb.AppendLine($"碰撞体积: {cfg.Hitbox}");
        sb.AppendLine($"怪物偏移: {cfg.SpawnOffset}格, 间隔: {cfg.SpawnDelay}帧");

        if (plr.RealPlayer)
            plr.SendMessage(TextGradient(sb.ToString()), color);
        else
            plr.SendMessage(sb.ToString(), color);
    }
    #endregion
}