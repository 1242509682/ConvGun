using System.Text;
using Terraria;
using TShockAPI;
using static ConvGun.Utils;

namespace ConvGun;

public static class CmdMain
{
    #region 权限检查
    public static string perm = "sg.use";
    public static bool IsAdmin(TSPlayer plr) => plr.HasPermission("sg.admin");
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
            case "d":
            case "del":
                DeleteRulesBySource(args);
                break;
            case "s":
            case "set":
            case "src":
                RuleMaker.SetSource(plr);
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
                {
                    ListConditions(args);

                    plr.SendMessage(Grad("正确用法: /sg cd 1 2 3 ..."), color);
                }

                break;
            case "lk":
            case "luck":
                if (args.Parameters.Count > 1 && float.TryParse(args.Parameters[1], out float lk))
                    RuleMaker.SetLuck(plr, lk);
                else
                    plr.SendErrorMessage("用法: /sg lk <幸运值> (如 0.5)");
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
            sb.AppendLine($"/sg d - 删手持物品所有规则");
            sb.AppendLine($"/sg rs - 清空所有规则");
            sb.AppendLine($"/sg fix - 恢复默认规则");
        }
        sb.AppendLine($"/sg all - 查看所有可用条件");
        sb.AppendLine($"/sg ls [页数] - 列出所有规则");
        sb.AppendLine($"/sg if - 查看插件状态");

        if (plr.RealPlayer)
            plr.SendMessage(Grad(sb.ToString()), color);
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
        args.Player.SendMessage(Grad(sb.ToString()), color);
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
        Plugin.Config.BuildRuleIdx();
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        args.Player.SendMessage(Grad("已清空所有转换规则"), color);
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
        Plugin.Config.BuildRuleIdx();
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        args.Player.SendMessage(Grad("已恢复所有默认转换规则"), color);
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
        sb.AppendLine($"动画帧数: {cfg.AnimTime}");
        sb.AppendLine($"规则数量: {cfg.ConvRules.Count}");
        sb.AppendLine($"冷却秒数: {cfg.Sec}");
        sb.AppendLine($"碰撞体积: {cfg.Hitbox}");
        sb.AppendLine($"生成偏移: {cfg.SpawnOff}格, 间隔: {cfg.DelayTime}帧");

        if (plr.RealPlayer)
            plr.SendMessage(Grad(sb.ToString()), color);
        else
            plr.SendMessage(sb.ToString(), color);
    }
    #endregion

    #region 删除手持物品的所有规则
    private static void DeleteRulesBySource(CommandArgs args)
    {
        if (!IsAdmin(args.Player))
        {
            args.Player.SendErrorMessage("你没有权限使用此命令");
            return;
        }
        var plr = args.Player;
        var held = plr.SelectedItem;
        if (held == null || held.type == 0)
        {
            plr.SendErrorMessage("请手持一个物品作为源物品");
            return;
        }
        int srcId = held.type;
        var toRemove = Plugin.Config.ConvRules.Where(r => r.SourceID == srcId).ToList();
        if (toRemove.Count == 0)
        {
            plr.SendMessage(Grad($"没有找到源物品为 {Icon(srcId)} 的规则"), color);
            return;
        }
        foreach (var rule in toRemove)
            Plugin.Config.ConvRules.Remove(rule);
        Plugin.Config.BuildRuleIdx();
        Plugin.Config.AutoRuleDesc();
        Plugin.Config.Write();
        plr.SendMessage(Grad($"已删除 {toRemove.Count} 条源物品为 {Icon(srcId)} 的规则"), color);
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

        // 按条件分组（空串为无条件）
        var groups = rules.GroupBy(r => string.IsNullOrEmpty(r.Cond) ? "" : r.Cond)
                          .OrderBy(g => g.Key)
                          .ToList();

        // 检查是否为 all 模式
        bool isAll = args.Parameters.Count > 1 && args.Parameters[1].Equals("all", StringComparison.OrdinalIgnoreCase);

        if (isAll)
        {
            // 全量输出所有分组
            var sb = new StringBuilder();

            for (int idx = 0; idx < groups.Count; idx++)
            {
                var grps = groups[idx];
                string condKeys = grps.Key;
                string condShows = string.IsNullOrEmpty(condKeys) ? "无条件" : $"{Utils.GetCondName(condKeys)}";
                sb.AppendLine($"\n [i:3455][c/AD89D5:转换][c/D68ACA:规][c/DF909A:则][c/E5A894:表][i:3454]  - {condShows}\n");

                var grpLists = grps.ToList();

                // 构建正向与反向映射（仅物品）
                var fw = new Dictionary<int, HashSet<int>>();
                var rv = new Dictionary<int, HashSet<int>>();
                foreach (var r in grpLists)
                {
                    foreach (int t in r.itemIds)
                    {
                        if (!fw.ContainsKey(r.SourceID))
                            fw[r.SourceID] = new HashSet<int>();
                        fw[r.SourceID].Add(t);

                        if (!rv.ContainsKey(t))
                            rv[t] = new HashSet<int>();
                        rv[t].Add(r.SourceID);
                    }
                }

                var done = new HashSet<(int, int)>();

                // 双向规则
                var bidir = new List<string>();
                foreach (var src in fw.Keys)
                {
                    foreach (int tgt in fw[src])
                    {
                        if (src < tgt && fw.ContainsKey(tgt) && fw[tgt].Contains(src))
                        {
                            bidir.Add($"{Icon(src)} ←→ {Icon(tgt)}");
                            done.Add((src, tgt));
                            done.Add((tgt, src));
                        }
                    }
                }

                // 单向规则（物品 + 怪物）
                var unidir = new List<string>();
                foreach (var r in grpLists)
                {
                    foreach (int t in r.itemIds)
                    {
                        if (done.Contains((r.SourceID, t))) continue;
                        unidir.Add($"{Icon(r.SourceID)} → {Icon(t, r.Count)}");
                    }
                }
                foreach (var r in grpLists)
                {
                    foreach (int n in r.npcIds)
                        unidir.Add($"{Icon(r.SourceID)} → {Lang.GetNPCNameValue(n)}x{r.Count}");
                }

                // 输出双向规则
                if (bidir.Count > 0)
                {
                    sb.AppendLine("[c/61E26C:双向转换：]");
                    for (int i = 0; i < bidir.Count; i += 3)
                    {
                        var row = bidir.Skip(i).Take(3);
                        sb.AppendLine(string.Join(" | ", row));
                    }
                }

                // 输出单向规则
                if (unidir.Count > 0)
                {
                    sb.AppendLine("[c/FFAA6D:单向转换：]");
                    for (int i = 0; i < unidir.Count; i += 3)
                    {
                        var row = unidir.Skip(i).Take(3);
                        sb.AppendLine(string.Join(" | ", row));
                    }
                }
            }

            if (args.Player.RealPlayer)
                args.Player.SendMessage(Grad(sb.ToString()), color);
            else
                args.Player.SendMessage(sb.ToString(), color);
            return;
        }

        // 原有翻页逻辑
        int pg = 1;
        if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int p))
            pg = p;
        if (pg < 1) pg = 1;
        if (pg > groups.Count) pg = groups.Count;

        var grp = groups[pg - 1];
        string condKey = grp.Key;
        string condShow = string.IsNullOrEmpty(condKey) ? "无条件" : $"{Utils.GetCondName(condKey)}";

        var sb2 = new StringBuilder($"    [i:3455][c/AD89D5:转换][c/D68ACA:规][c/DF909A:则][c/E5A894:表][i:3454] ");

        var grpList = grp.ToList();

        // 构建正向与反向映射（仅物品）
        var fw2 = new Dictionary<int, HashSet<int>>();
        var rv2 = new Dictionary<int, HashSet<int>>();
        foreach (var r in grpList)
        {
            foreach (int t in r.itemIds)
            {
                if (!fw2.ContainsKey(r.SourceID))
                    fw2[r.SourceID] = new HashSet<int>();
                fw2[r.SourceID].Add(t);

                if (!rv2.ContainsKey(t))
                    rv2[t] = new HashSet<int>();
                rv2[t].Add(r.SourceID);
            }
        }

        var done2 = new HashSet<(int, int)>();

        // 双向规则
        var bidir2 = new List<string>();
        foreach (var src in fw2.Keys)
        {
            foreach (int tgt in fw2[src])
            {
                if (src < tgt && fw2.ContainsKey(tgt) && fw2[tgt].Contains(src))
                {
                    bidir2.Add($"{Icon(src)} ←→ {Icon(tgt)}");
                    done2.Add((src, tgt));
                    done2.Add((tgt, src));
                }
            }
        }

        // 单向规则（物品 + 怪物）
        var unidir2 = new List<string>();
        foreach (var r in grpList)
        {
            foreach (int t in r.itemIds)
            {
                if (done2.Contains((r.SourceID, t))) continue;
                unidir2.Add($"{Icon(r.SourceID)} → {Icon(t, r.Count)}");
            }
        }
        foreach (var r in grpList)
        {
            foreach (int n in r.npcIds)
                unidir2.Add($"{Icon(r.SourceID)} → {Lang.GetNPCNameValue(n)}x{r.Count}");
        }

        // 输出双向规则（每行3列）
        if (bidir2.Count > 0)
        {
            sb2.AppendLine("[c/61E26C:双向转换：]");
            for (int i = 0; i < bidir2.Count; i += 3)
            {
                var row = bidir2.Skip(i).Take(3);
                sb2.AppendLine(string.Join(" | ", row));
            }
            sb2.AppendLine();
        }

        // 输出单向规则（每行3列）
        if (unidir2.Count > 0)
        {
            sb2.AppendLine("    [i:3455][c/AD89D5:转换][c/D68ACA:规][c/DF909A:则][c/E5A894:表][i:3454] [c/FFAA6D:单向转换：]");
            for (int i = 0; i < unidir2.Count; i += 3)
            {
                var row = unidir2.Skip(i).Take(3);
                sb2.AppendLine(string.Join(" | ", row));
            }
        }

        if (groups.Count > 1)
        {
            sb2.AppendLine($"\n当前 第{pg}/[c/47D3C3:{groups.Count}页] - {condShow} 组 ");
            sb2.AppendLine($"查看下一页 /sg ls {pg + 1}");
            sb2.AppendLine($"查看  全部 /sg ls all");
        }

        if (args.Player.RealPlayer)
            args.Player.SendMessage(Grad(sb2.ToString()), color);
        else
            args.Player.SendMessage(sb2.ToString(), color);
    }
    #endregion
}