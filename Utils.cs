using Terraria;
using TShockAPI;
using System.Text;
using Terraria.ID;
using Terraria.Utilities;
using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using System.Text.RegularExpressions;
using static ConvGun.Plugin;

namespace ConvGun;

internal class Utils
{
    #region 单色与随机色
    public static Color color => new(240, 250, 150); // 单色
    public static Color color2 => new(Main.rand.Next(180, 250), // 随机色
                                      Main.rand.Next(180, 250),
                                      Main.rand.Next(180, 250));
    #endregion

    #region 逐行渐变色
    public static void GradMess(StringBuilder Text, TSPlayer? plr = null)
    {
        var mess = Text.ToString();
        var lines = mess.Split('\n');

        var GradMess = new StringBuilder();
        var start = new Color(166, 213, 234);
        var end = new Color(245, 247, 175);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
            {
                float ratio = (float)i / (lines.Length - 1);
                var gradColor = Color.Lerp(start, end, ratio);

                // 将颜色转换为十六进制格式
                string colorHex = $"{gradColor.R:X2}{gradColor.G:X2}{gradColor.B:X2}";

                // 使用颜色标签包装每一行
                GradMess.AppendLine($"[c/{colorHex}:{lines[i]}]");
            }
        }

        if (plr is not null)
        {
            if (plr.RealPlayer)
                plr.SendMessage(GradMess.ToString(), color);
            else
                plr.SendMessage(mess, color);
        }
    }
    #endregion

    #region 渐变色方法
    public static string Grad(string text, TSPlayer? plr = null)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 检查是否包含颜色标签或物品图标标签
        if (text.Contains("[c/") || text.Contains("[i:"))
            return MixedText(text);
        else
            return ApplyGrad(text);
    }
    #endregion

    #region 混合文本（包含颜色标签、物品图标标签和普通文本）
    private static readonly Regex _tagRegex = new Regex(@"(\[c/([0-9a-fA-F]+):([^\]]+)\]|\[i(?:/s\d+)?:\d+\])", RegexOptions.Compiled);
    private static string MixedText(string text)
    {
        var res = new StringBuilder();
        // 匹配颜色标签 [c/颜色:文本] 或 物品图标标签 [i:物品ID] 或 [i/s数量:物品ID]
        var regex = new Regex(@"(\[c/([0-9a-fA-F]+):([^\]]+)\]|\[i(?:/s\d+)?:\d+\])");
        var matches = _tagRegex.Matches(text);
        if (matches.Count == 0) return ApplyGrad(text);
        int idx = 0;
        foreach (Match match in matches.Cast<Match>())
        {
            // 添加标签前的普通文本（应用渐变）
            if (match.Index > idx)
                res.Append(ApplyGrad(text.Substring(idx, match.Index - idx)));

            // 添加标签本身（保持不变）
            res.Append(match.Value);
            idx = match.Index + match.Length;
        }

        // 添加最后一个标签后的普通文本
        if (idx < text.Length) res.Append(ApplyGrad(text.Substring(idx)));
        return res.ToString();
    }
    #endregion

    #region 应用文本渐变方法
    private static string ApplyGrad(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var res = new StringBuilder();
        var start = new Color(166, 213, 234); // 起始色：浅蓝
        var end = new Color(245, 247, 175);   // 结束色：浅黄

        int cnt = text.Count(c => c != '\n' && c != '\r'); // 有效字符数
        if (cnt == 0) return text;

        int idx = 0;
        foreach (char c in text)
        {
            if (c == '\n' || c == '\r') { res.Append(c); continue; }
            float ratio = (float)idx / (cnt - 1);
            var clr = Color.Lerp(start, end, ratio);
            res.Append($"[c/{clr.Hex3()}:{c}]");
            idx++;
        }
        return res.ToString();
    }
    #endregion

    #region 返回物品图标方法
    public static string Icon(int itemID) => $"[i:{itemID}]";
    public static string Icon(int itemID, int stack) => $"[i/s{stack}:{itemID}]";
    #endregion

    #region 进度条件
    private static Dictionary<string, int> condMap = new();  // 条件名 -> idx
    private static string[] condNames = new string[88];      // idx -> 条件名（复用原 CondNames）
    /// <summary>从配置的 Reference 构建映射表（只执行一次）</summary>
    // 在 Utils 类中添加静态初始化方法
    public static void InitCondMap(Configuration cfg)
    {
        condMap.Clear();
        int idx = 0;
        foreach (string line in cfg.Reference)
        {
            string[] parts = line.Split('|');
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                var match = Regex.Match(trimmed, @"^(\d+)\s*(.*)$");
                if (match.Success && idx < condNames.Length)
                {
                    string name = match.Groups[2].Value.Trim();
                    if (string.IsNullOrEmpty(name)) name = "未知";
                    condNames[idx] = name;
                    condMap[name] = idx;
                    condMap[idx.ToString()] = idx;
                    idx++;
                }
            }
        }
    }

    // 修改 BuildCondMap，使其在映射表为空时调用 InitCondMap（兼容旧调用）
    private static void BuildCondMap()
    {
        if (condMap.Count > 0) return;
        if (Plugin.Config == null) return; // 尚未初始化，等待显式调用
        InitCondMap(Plugin.Config);
    }

    public static int GetCondId(string condName)
    {
        BuildCondMap();
        if (condMap.TryGetValue(condName, out int id)) return id;
        TShock.Log.ConsoleError($"[ConvGun] 未知条件名称: {condName}");
        return -1;
    }

    public static string GetCondName(string condStr)
    {
        if (string.IsNullOrEmpty(condStr)) return "";
        var parts = condStr.Split(',');
        var names = new List<string>();
        foreach (var part in parts)
        {
            string trimmed = part.Trim();
            if (int.TryParse(trimmed, out int id) && id >= 0 && id < condNames.Length)
                names.Add(condNames[id]);
            else
                names.Add(trimmed); // 未知或非数字保持原样
        }
        return string.Join(",", names);
    }

    /// <summary>整数条件检查（代替原 CheckCond）</summary>
    public static bool CheckCondById(int condId, Player p)
    {
        switch (condId)
        {
            case 0: return true;
            case 1: return NPC.downedBoss1;
            case 2: return NPC.downedSlimeKing;
            case 3: return NPC.downedBoss2 && (IsDefeated(NPCID.EaterofWorldsHead) || IsDefeated(NPCID.EaterofWorldsBody) || IsDefeated(NPCID.EaterofWorldsTail));
            case 4: return NPC.downedBoss2 && IsDefeated(NPCID.BrainofCthulhu);
            case 5: return NPC.downedBoss2;
            case 6: return NPC.downedDeerclops;
            case 7: return NPC.downedQueenBee;
            case 8: return !NPC.downedBoss3;
            case 9: return NPC.downedBoss3;
            case 10: return !Main.hardMode;
            case 11: return Main.hardMode;
            case 12: return NPC.downedMechBoss1;
            case 13: return NPC.downedMechBoss2;
            case 14: return NPC.downedMechBoss3;
            case 15: return NPC.downedPlantBoss;
            case 16: return NPC.downedGolemBoss;
            case 17: return NPC.downedQueenSlime;
            case 18: return NPC.downedEmpressOfLight;
            case 19: return NPC.downedFishron;
            case 20: return NPC.downedAncientCultist;
            case 21: return NPC.downedMoonlord;
            case 22: return NPC.downedHalloweenTree;
            case 23: return NPC.downedHalloweenKing;
            case 24: return NPC.downedChristmasTree;
            case 25: return NPC.downedChristmasIceQueen;
            case 26: return NPC.downedChristmasSantank;
            case 27: return NPC.downedMartians;
            case 28: return NPC.downedClown;
            case 29: return NPC.downedTowerSolar;
            case 30: return NPC.downedTowerVortex;
            case 31: return NPC.downedTowerNebula;
            case 32: return NPC.downedTowerStardust;
            case 33: return NPC.downedMechBossAny;
            case 34: return NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;
            case 35: return NPC.downedTowerNebula || NPC.downedTowerSolar || NPC.downedTowerStardust || NPC.downedTowerVortex;
            case 36: return NPC.downedTowerNebula && NPC.downedTowerSolar && NPC.downedTowerStardust && NPC.downedTowerVortex;
            case 37: return NPC.downedGoblins;
            case 38: return NPC.downedPirates;
            case 39: return NPC.downedFrost;
            case 40: return Main.bloodMoon;
            case 41: return Main.raining;
            case 42: return Main.dayTime;
            case 43: return !Main.dayTime;
            case 44: return Main.IsItAHappyWindyDay;
            case 45: return Main.halloween;
            case 46: return Main.xMas;
            case 47: return BirthdayParty.PartyIsUp;
            case 48: return DD2Event._downedDarkMageT1;
            case 49: return DD2Event._downedOgreT2;
            case 50: return DD2Event._spawnedBetsyT3;
            case 51: return Main.drunkWorld;
            case 52: return Main.tenthAnniversaryWorld;
            case 53: return Main.getGoodWorld;
            case 54: return Main.notTheBeesWorld;
            case 55: return Main.dontStarveWorld;
            case 56: return Main.remixWorld;
            case 57: return Main.noTrapsWorld;
            case 58: return Main.zenithWorld;
            // 环境条件需要 Player 对象
            case 59: return p != null && p.ShoppingZone_Forest;
            case 60: return p != null && p.ZoneJungle;
            case 61: return p != null && p.ZoneDesert;
            case 62: return p != null && p.ZoneSnow;
            case 63: return p != null && p.ZoneRockLayerHeight;
            case 64: return p != null && p.ZoneBeach;
            case 65: return p != null && (p.position.Y / 16) <= Main.worldSurface;
            case 66: return p != null && (p.position.Y / 16) <= (Main.worldSurface * 0.35);
            case 67: return p != null && (p.position.Y / 16) >= Main.UnderworldLayer;
            case 68: return p != null && p.ZoneHallow;
            case 69: return p != null && p.ZoneGlowshroom;
            case 70: return p != null && p.ZoneCorrupt;
            case 71: return p != null && p.ZoneCrimson;
            case 72: return p != null && (p.ZoneCrimson || p.ZoneCorrupt);
            case 73: return p != null && p.ZoneDungeon;
            case 74: return p != null && p.ZoneGraveyard;
            case 75: return p != null && p.ZoneHive;
            case 76: return p != null && p.ZoneLihzhardTemple;
            case 77: return p != null && p.ZoneSandstorm;
            case 78: return p != null && p.ZoneSkyHeight;
            case 79: return p != null && p.ZoneShimmer;
            case 80: return Main.moonPhase == 0;
            case 81: return Main.moonPhase == 1;
            case 82: return Main.moonPhase == 2;
            case 83: return Main.moonPhase == 3;
            case 84: return Main.moonPhase == 4;
            case 85: return Main.moonPhase == 5;
            case 86: return Main.moonPhase == 6;
            case 87: return Main.moonPhase == 7;
            default: return false;
        }
    }

    /// <summary>批量条件检查（整数版本）</summary>
    public static bool CheckConds(List<int> condIds, Player p)
    {
        foreach (int id in condIds)
        {
            if (id == -1) return false; // 无效条件，永不满足
            if (!CheckCondById(id, p)) return false;
        }
        return true;
    }

    // 是否解锁怪物图鉴以达到解锁物品掉落的程度（用于独立判断克脑、世吞）
    private static bool IsDefeated(int type)
    {
        var unlockState = Main.BestiaryDB.FindEntryByNPCID(type).UIInfoProvider.GetEntryUICollectionInfo().UnlockState;
        return unlockState == Terraria.GameContent.Bestiary.BestiaryEntryUnlockState.CanShowDropsWithDropRates_4;
    }
    #endregion

    #region 原版微光转换 + 缓存
    private static Dictionary<int, int> sCache = new();   // 物品类型 -> 转换结果
    public static int GetShimmerTransform(int type)
    {
        if (sCache.TryGetValue(type, out int val)) return val;
        if (!ContentSamples.ItemsByType.TryGetValue(type, out var item)) return 0;
        int equiv = item.GetShimmerEquivalentType();
        if (equiv == 0) return 0;
        int target = ShimmerTransforms.GetTransformToItem(equiv);
        if (target == 0) return 0;
        if (ShimmerTransforms.IsItemTransformLocked(target)) return 0;
        sCache[type] = target;
        return target;
    }
    #endregion

    #region 原版分解 + 缓存
    private class DecInfo
    {
        public int createStack;
        public List<(int typ, int stack)>? reqs;
    }
    private static Dictionary<int, DecInfo> dCache = new();
    public static List<(int typ, int stack)> GetDecraft(int oldType, int srcStack)
    {
        if (!dCache.TryGetValue(oldType, out var info))
        {
            info = new DecInfo();
            Item item = ContentSamples.ItemsByType[oldType];
            if (item == null) return new List<(int, int)>();
            int equiv = item.GetShimmerEquivalentType(forDecrafting: true);
            int rcpIdx = ShimmerTransforms.GetDecraftingRecipeIndex(equiv);
            if (rcpIdx < 0) return new List<(int, int)>();
            Recipe rcp = Main.recipe[rcpIdx];
            info.createStack = rcp.createItem.stack;
            info.reqs = new List<(int, int)>();
            foreach (var req in rcp.requiredItemQuickLookup)
            {
                if (req.itemIdOrRecipeGroup == 0) break;
                int matType = req.IsRecipeGroup ? req.RecipeGroup.DecraftItemId : req.itemIdOrRecipeGroup;
                info.reqs.Add((matType, req.stack));
            }
            dCache[oldType] = info;
        }
        if (info.reqs == null || info.reqs.Count == 0) return new List<(int, int)>();
        int times = srcStack / info.createStack;
        if (times == 0) return new List<(int, int)>();
        return info.reqs.Select(r => (r.typ, r.stack * times)).ToList();
    }
    #endregion


}
