using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using static ConvGun.Plugin;

namespace ConvGun;

/// <summary>
/// 转换规则（一个源物品对应多个目标物品和怪物）
/// </summary>
public class ConvRule
{
    [JsonProperty("描述")]
    public string Desc { get; set; } = "";
    [JsonProperty("源物品")]
    public int SourceID { get; set; }
    [JsonProperty("物品")]
    public List<int> ItemIDs { get; set; } = new();
    [JsonProperty("怪物")]
    public List<int> NpcIDs { get; set; } = new();
    [JsonProperty("数量")]
    public int Count { get; set; } = 1;
    [JsonProperty("条件")]
    public string Cond { get; set; } = "";   // 多个条件用逗号分隔，如 "晚上,血月"
    [JsonIgnore]
    public List<string> CondList => string.IsNullOrEmpty(Cond) ? new List<string>() : Cond.Split(',').Select(c => c.Trim()).ToList();
}

internal class Configuration
{
    #region 配置项成员
    [JsonProperty("进度参考", Order = -100)]
    public List<string> Reference = new();
    [JsonProperty("插件开关", Order = 0)]
    public bool Enabled { get; set; } = true;
    [JsonProperty("碰撞体积", Order = 1)]
    public int Hitbox { get; set; } = 12;
    [JsonProperty("上升格数", Order = 2)]
    public int Height { get; set; } = 5;
    [JsonProperty("冷却秒数", Order = 3)]
    public int Sec { get; set; } = 2;
    [JsonProperty("启用动画", Order = 4)]
    public bool UseAnim { get; set; } = true;
    [JsonProperty("动画帧数", Order = 5)]
    public int DelayFrames { get; set; } = 60;
    [JsonProperty("怪物生成偏移(格数)", Order = 7)]
    public int SpawnOffset { get; set; } = 10;
    [JsonProperty("怪物生成间隔(帧)", Order = 8)]
    public int SpawnDelay { get; set; } = 15;
    [JsonProperty("转换规则列表", Order = 10)]
    public List<ConvRule> ConvRules { get; set; } = new();
    #endregion

    #region 预设参数方法
    public void SetDefault()
    {
        ConvRules.Clear();

        AddMixedRule(ItemID.PlatinumCoin, new List<int> { ItemID.GoldCoin }, new List<int> { NPCID.ShimmerSlime }, 0, 2);
        AddMixedRule(ItemID.PlatinumCoin, new List<int>(), new List<int> { NPCID.Zombie, NPCID.DemonEye }, 43, 2);

        // 迁移自定义微光转换表
        AddItemRule(ItemID.RodofDiscord, ItemID.RodOfHarmony, 21);
        AddItemRule(ItemID.Clentaminator, ItemID.Clentaminator2, 21);
        AddItemRule(ItemID.BottomlessBucket, ItemID.BottomlessShimmerBucket, 21);
        AddItemRule(ItemID.BottomlessShimmerBucket, ItemID.BottomlessBucket, 21);
        AddItemRule(ItemID.JungleKey, ItemID.PiranhaGun, 15);
        AddItemRule(ItemID.CorruptionKey, ItemID.ScourgeoftheCorruptor, 15);
        AddItemRule(ItemID.CrimsonKey, ItemID.VampireKnives, 15);
        AddItemRule(ItemID.HallowedKey, ItemID.RainbowGun, 15);
        AddItemRule(ItemID.FrozenKey, ItemID.StaffoftheFrostHydra, 15);
        AddItemRule(ItemID.DungeonDesertKey, ItemID.StormTigerStaff, 15);
        AddItemRule(ItemID.CobaltOre, ItemID.Hellstone, 11);
        AddItemRule(ItemID.Amber, ItemID.Diamond);
        AddItemRule(ItemID.ShadowGreaves, ItemID.DemoniteBar, 5);
        AddItemRule(ItemID.ShadowScalemail, ItemID.DemoniteBar, 5);
        AddItemRule(ItemID.ShadowHelmet, ItemID.DemoniteBar, 5);
        AddSwapRule(ItemID.DemoniteOre, ItemID.CrimtaneOre);
        AddSwapRule(ItemID.Compass, ItemID.DepthMeter);
        AddSwapRule(ItemID.LifeformAnalyzer, ItemID.Radar);
        AddItemRule(ItemID.Radar, ItemID.TallyCounter, 9);
        AddSwapRule(ItemID.TallyCounter, ItemID.LifeformAnalyzer);
        AddSwapRule(ItemID.DPSMeter, ItemID.MetalDetector);
        AddSwapRule(ItemID.MetalDetector, ItemID.Stopwatch);
        AddSwapRule(ItemID.Stopwatch, ItemID.DPSMeter);
        AddSwapRule(ItemID.MagicConch, ItemID.DemonConch);
        AddSwapRule(ItemID.SunStone, ItemID.MoonStone, 16);
        AddSwapRule(ItemID.TorchGodsFavor, ItemID.ArtisanLoaf);
        AddSwapRule(ItemID.ScarabFishingRod, ItemID.FiberglassFishingPole);
        AddItemRule(ItemID.FiberglassFishingPole, ItemID.BloodFishingRod);
        AddItemRule(ItemID.BloodFishingRod, ItemID.ScarabFishingRod);
        AddSwapRule(ItemID.Stinkbug, ItemID.LadyBug);
        AddCycleRule(ItemID.ShadowFlameBow, ItemID.ShadowFlameHexDoll, ItemID.ShadowFlameKnife, 11);
        AddSwapRule(ItemID.CrossNecklace, ItemID.PhilosophersStone, 11);
        AddSwapRule(ItemID.MagicDagger, ItemID.CrystalSerpent, 11);
        AddSwapRule(ItemID.Marrow, ItemID.Uzi, 11);
        AddSwapRule(ItemID.DaedalusStormbow, ItemID.FlyingKnife, 11);
        AddCycleRule(ItemID.MonkStaffT1, ItemID.DD2PhoenixBow, ItemID.MonkStaffT2, ItemID.DD2SquireDemonSword, 11);
        AddSwapRule(ItemID.DarkShard, ItemID.LightShard, 11);
        AddSwapRule(ItemID.AncientBattleArmorMaterial, ItemID.FrostCore, 11);
        AddSwapRule(ItemID.Ichor, ItemID.CursedFlame, 11);
        AddSwapRule(ItemID.Vertebrae, ItemID.RottenChunk);
        AddSwapRule(ItemID.ViciousMushroom, ItemID.VileMushroom);
        AddSwapRule(ItemID.CloudinaBottle, ItemID.TsunamiInABottle);
        AddSwapRule(ItemID.SandstorminaBottle, ItemID.BlizzardinaBottle);
        AddSwapRule(ItemID.IceSkates, ItemID.FlowerBoots);
        AddSwapRule(ItemID.SharkFin, ItemID.Feather);
        AddSwapRule(ItemID.JungleRose, ItemID.NaturesGift);
        AddSwapRule(ItemID.ShadowScale, ItemID.TissueSample, 5);
        AddSwapRule(ItemID.RainbowBrick, ItemID.EchoBlock, 13);
        AddItemRule(ItemID.PaladinsHammer, ItemID.PaladinsShield, 15);
        AddSwapRule(ItemID.TatteredCloth, ItemID.FlinxFur);
        AddSwapRule(ItemID.CorruptSeeds, ItemID.CrimsonSeeds);
        AddSwapRule(ItemID.HermesBoots, ItemID.SailfishBoots);
        AddSwapRule(ItemID.Tabi, ItemID.BlackBelt, 15);
        AddSwapRule(ItemID.ClimbingClaws, ItemID.ShoeSpikes);
        AddSwapRule(ItemID.WarTable, ItemID.DefendersForge, 33);
        AddSwapRule(ItemID.DryBomb, ItemID.ScarabBomb);
        AddItemRule(ItemID.TempleKey, ItemID.SolarTablet, 16);
        AddCycleRule(ItemID.BrickLayer, ItemID.ExtendoGrip, ItemID.PaintSprayer, ItemID.PortableCementMixer);

        Reference =
        [
            "0 无 | 1 克眼 | 2 史王 | 3 世吞 | 4 克脑 | 5 世吞或克脑 | 6 巨鹿 | 7 蜂王 | 8 骷髅王前 | 9 骷髅王后",
            "10 肉前 | 11 肉后 | 12 毁灭者 | 13 双子魔眼 | 14 机械骷髅王 | 15 世花 | 16 石巨人 | 17 史后 | 18 光女 | 19 猪鲨",
            "20 拜月 | 21 月总 | 22 哀木 | 23 南瓜王 | 24 尖叫怪 | 25 冰雪女王 | 26 圣诞坦克 | 27 火星飞碟 | 28 小丑",
            "29 日耀柱 | 30 星旋柱 | 31 星云柱 | 32 星尘柱 | 33 一王后 | 34 三王后 | 35 一柱后 | 36 四柱后",
            "37 哥布林 | 38 海盗 | 39 霜月 | 40 血月 | 41 雨天 | 42 白天 | 43 夜晚 | 44 大风天 | 45 万圣节 | 46 圣诞节 | 47 派对",
            "48 旧日一 | 49 旧日二 | 50 旧日三 | 51 醉酒种子 | 52 十周年 | 53 ftw种子 | 54 蜜蜂种子 | 55 饥荒种子",
            "56 颠倒种子 | 57 陷阱种子 | 58 天顶种子",
            "59 森林 | 60 丛林 | 61 沙漠 | 62 雪原 | 63 洞穴 | 64 海洋 | 65 地表 | 66 太空 | 67 地狱 | 68 神圣 | 69 蘑菇",
            "70 腐化 | 71 猩红 | 72 邪恶 | 73 地牢 | 74 墓地 | 75 蜂巢 | 76 神庙 | 77 沙尘暴 | 78 天空 | 79 微光",
            "80 满月 | 81 亏凸月 | 82 下弦月 | 83 残月 | 84 新月 | 85 娥眉月 | 86 上弦月 | 87 盈凸月"
        ];

        // 动态构建条件名称数组
        BuildCondNames();
    }
    #endregion

    #region 自动描述（使用动态构建的 CondNames）
    public void AutoRuleDesc()
    {
        if (ConvRules == null || !ConvRules.Any()) return;
        foreach (var rule in ConvRules)
        {
            if (rule.ItemIDs.Count == 0 && rule.NpcIDs.Count == 0) continue;
            string targetsDesc = string.Join(", ",
                rule.ItemIDs.Select(id => $"{Lang.GetItemNameValue(id)}x{rule.Count}")
                .Concat(rule.NpcIDs.Select(id => $"{Lang.GetNPCNameValue(id)}x{rule.Count}")));

            string condStr = "无条件";
            if (!string.IsNullOrEmpty(rule.Cond))
            {
                var conds = rule.Cond.Split(',').Select(c => c.Trim()).ToList();
                condStr = string.Join("、", conds.Select(c => int.TryParse(c, out int idx) && idx >= 0 && idx < CondNames.Length ? CondNames[idx] : c)) + "条件下";
            }
            rule.Desc = $"{condStr} 将 {Lang.GetItemNameValue(rule.SourceID)} 转换为 {targetsDesc}";
        }
    }
    #endregion

    #region 读取与写入
    public void Write()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    public static Configuration Read()
    {
        if (!File.Exists(ConfigPath))
        {
            var cfg = new Configuration();
            cfg.SetDefault();
            cfg.AutoRuleDesc();
            cfg.Write();
            return cfg;
        }
        try
        {
            string json = File.ReadAllText(ConfigPath);
            var cfg = JsonConvert.DeserializeObject<Configuration>(json)!;
            cfg.BuildCondNames();   // 重新构建条件名称数组
            cfg.AutoRuleDesc();
            return cfg;
        }
        catch (JsonReaderException ex)
        {
            string json = File.ReadAllText(ConfigPath);
            string[] lines = json.Split('\n');
            int line = ex.LineNumber;
            int idx = Math.Max(0, Math.Min(line - 2, lines.Length - 1));
            string text = lines[idx].Trim();
            throw new Exception($"位置: 第 {line - 1} 行\n内容: {text ?? string.Empty}\n路径: {FormatPath(ex.Path ?? string.Empty)}", ex);
        }
    }

    private static string FormatPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return Regex.Replace(path, @"\[(\d+)\]", match =>
        {
            int index = int.Parse(match.Groups[1].Value);
            return $":第{index + 1}项";
        });
    }
    #endregion

    #region 辅助添加规则方法
    private void AddItemRule(int source, int target, int condIdx = 0, int count = 1)
    {
        string condStr = condIdx == 0 ? "" : condIdx.ToString();
        ConvRules.Add(new ConvRule
        {
            SourceID = source,
            ItemIDs = new List<int> { target },
            NpcIDs = new List<int>(),
            Count = count,
            Cond = condStr
        });
    }

    private void AddNpcRule(int source, int npc, int condIdx = 0, int count = 1)
    {
        string condStr = condIdx == 0 ? "" : condIdx.ToString();
        ConvRules.Add(new ConvRule
        {
            SourceID = source,
            ItemIDs = new List<int>(),
            NpcIDs = new List<int> { npc },
            Count = count,
            Cond = condStr
        });
    }

    private void AddSwapRule(int id1, int id2, int condIdx = 0)
    {
        AddItemRule(id1, id2, condIdx);
        AddItemRule(id2, id1, condIdx);
    }

    private void AddCycleRule(int id1, int id2, int id3, int condIdx = 0)
    {
        AddItemRule(id1, id2, condIdx);
        AddItemRule(id2, id3, condIdx);
        AddItemRule(id3, id1, condIdx);
    }

    private void AddCycleRule(int id1, int id2, int id3, int id4, int condIdx = 0)
    {
        AddItemRule(id1, id2, condIdx);
        AddItemRule(id2, id3, condIdx);
        AddItemRule(id3, id4, condIdx);
        AddItemRule(id4, id1, condIdx);
    }

    /// <summary>添加混合规则（同时包含物品和怪物）</summary>
    private void AddMixedRule(int source, List<int> items, List<int> npcs, int condIdx = 0, int count = 1)
    {
        string condStr = condIdx == 0 ? "" : condIdx.ToString();
        ConvRules.Add(new ConvRule
        {
            SourceID = source,
            ItemIDs = items,
            NpcIDs = npcs,
            Count = count,
            Cond = condStr
        });
    }
    #endregion

    #region 条件数字 -> 中文描述映射（0-87）
    [JsonIgnore]
    private string[] CondNames = new string[88];
    /// <summary>从 Reference 中解析条件名称</summary>
    private void BuildCondNames()
    {
        CondNames = new string[88];
        for (int i = 0; i < CondNames.Length; i++)
            CondNames[i] = "未知";

        int idx = 0;
        foreach (string line in Reference)
        {
            string[] parts = line.Split('|');
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                // 正则匹配：开头的数字，后面可能跟着空格，然后是名称
                var match = Regex.Match(trimmed, @"^(\d+)\s*(.*)$");
                if (match.Success && idx < CondNames.Length)
                {
                    // 数字部分可以忽略，直接取名称
                    string name = match.Groups[2].Value.Trim();
                    if (string.IsNullOrEmpty(name))
                        name = "未知";
                    CondNames[idx] = name;
                    idx++;
                }
            }
        }
    }
    #endregion
}