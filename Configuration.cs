using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using TShockAPI;
using static ConvGun.Plugin;

namespace ConvGun;

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
    public int Height { get; set; } = 10;
    [JsonProperty("冷却秒数", Order = 3)]
    public int Sec { get; set; } = 2;
    [JsonProperty("动画间隔", Order = 4)]
    public int AnimTime { get; set; } = 90;
    [JsonProperty("动画偏移", Order = 5)]
    public int SpawnOff { get; set; } = 5;
    [JsonProperty("延迟动画", Order = 6)]
    public bool Delay { get; set; } = true;
    [JsonProperty("延迟间隔", Order = 7)]
    public int DelayTime { get; set; } = 30;
    [JsonProperty("随机动画", Order = 8)]
    public ParticleOrchestraType[] AnimType { get; set; } =
    [
        ParticleOrchestraType.ShimmerTownNPCSend,
        ParticleOrchestraType.ShimmerTownNPC,
        ParticleOrchestraType.RainbowBoulder1,
        ParticleOrchestraType.RainbowBoulderPetBounce,
        ParticleOrchestraType.StormLightning,
    ];
    [JsonProperty("弹幕特效", Order = 9)]
    public EXC EXProj = new();

    [JsonProperty("转换规则列表", Order = 10)]
    public List<ConvRule> ConvRules { get; set; } = new();

    [JsonIgnore]
    public Dictionary<int, List<ConvRule>> ruleMap = new();
    #endregion

    #region 预设参数方法
    public void SetDefault()
    {
        ConvRules.Clear();

        EXProj = new()
        {
            Types =
            [
                ProjectileID.MagicMissile,ProjectileID.Flamelash,
                ProjectileID.RainbowRodBullet,ProjectileID.WaterStream,
                ProjectileID.IchorSplash,ProjectileID.DD2PhoenixBowShot
            ],
        };
        EXProj.Types.Sort();  // 原地排序

        AB(ItemID.PlatinumCoin,ItemID.LuckyClover, 0,2);
        ABA(ItemID.LuckyClover, ItemID.WiltedClover);

        // 迁移自定义微光转换表
        AB(ItemID.JungleKey, ItemID.PiranhaGun, 15);
        AB(ItemID.CorruptionKey, ItemID.ScourgeoftheCorruptor, 15);
        AB(ItemID.CrimsonKey, ItemID.VampireKnives, 15);
        AB(ItemID.HallowedKey, ItemID.RainbowGun, 15);
        AB(ItemID.FrozenKey, ItemID.StaffoftheFrostHydra, 15);
        AB(ItemID.DungeonDesertKey, ItemID.StormTigerStaff, 15);
        AB(ItemID.CobaltOre, ItemID.Hellstone, 11);
        AB(ItemID.Amber, ItemID.Diamond);
        ABA(ItemID.DemoniteOre, ItemID.CrimtaneOre);
        ABA(ItemID.Compass, ItemID.DepthMeter);

        AB(ItemID.LifeformAnalyzer, ItemID.Radar);
        AB(ItemID.Radar, ItemID.TallyCounter,9);
        AB(ItemID.TallyCounter, ItemID.LifeformAnalyzer);

        ABCA(ItemID.DPSMeter, ItemID.MetalDetector, ItemID.Stopwatch);
        ABA(ItemID.MagicConch, ItemID.DemonConch);
        ABA(ItemID.SunStone, ItemID.MoonStone, 16);
        ABA(ItemID.TorchGodsFavor, ItemID.ArtisanLoaf);
        ABCA(ItemID.ScarabFishingRod, ItemID.FiberglassFishingPole, ItemID.BloodFishingRod);
        ABA(ItemID.Stinkbug, ItemID.LadyBug);
        ABCA(ItemID.ShadowFlameBow, ItemID.ShadowFlameHexDoll, ItemID.ShadowFlameKnife, 11);
        ABA(ItemID.CrossNecklace, ItemID.PhilosophersStone, 11);
        ABA(ItemID.DaedalusStormbow, ItemID.FlyingKnife, 11);
        ABCDA(ItemID.MonkStaffT1, ItemID.DD2PhoenixBow, ItemID.MonkStaffT2, ItemID.DD2SquireDemonSword, 11);
        ABA(ItemID.DarkShard, ItemID.LightShard, 11);
        ABA(ItemID.AncientBattleArmorMaterial, ItemID.FrostCore, 11);
        ABA(ItemID.Ichor, ItemID.CursedFlame, 11);
        ABA(ItemID.Vertebrae, ItemID.RottenChunk);
        ABA(ItemID.ViciousMushroom, ItemID.VileMushroom);
        ABCDA(ItemID.CloudinaBottle, ItemID.TsunamiInABottle, ItemID.SandstorminaBottle, ItemID.BlizzardinaBottle);
        ABA(ItemID.IceSkates, ItemID.FlowerBoots);
        ABA(ItemID.SharkFin, ItemID.Feather);
        ABA(ItemID.JungleRose, ItemID.NaturesGift);
        ABA(ItemID.ShadowScale, ItemID.TissueSample, 5);
        ABA(ItemID.PaladinsHammer, ItemID.PaladinsShield, 15);
        ABA(ItemID.TatteredCloth, ItemID.FlinxFur);
        ABA(ItemID.CorruptSeeds, ItemID.CrimsonSeeds);
        ABA(ItemID.HermesBoots, ItemID.SailfishBoots);
        ABA(ItemID.Tabi, ItemID.BlackBelt, 15);
        ABA(ItemID.ClimbingClaws, ItemID.ShoeSpikes);
        ABA(ItemID.WarTable, ItemID.DefendersForge, 33);
        ABA(ItemID.DryBomb, ItemID.ScarabBomb);
        AB(ItemID.TempleKey, ItemID.SolarTablet, 16);
        ABCDA(ItemID.BrickLayer, ItemID.ExtendoGrip, ItemID.PaintSprayer, ItemID.PortableCementMixer);

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
            if (rule.itemIds.Count == 0 && rule.npcIds.Count == 0) continue;
            string targetsDesc = string.Join(", ",
                rule.itemIds.Select(id => $"{Lang.GetItemNameValue(id)}x{rule.Count}")
                .Concat(rule.npcIds.Select(id => $"{Lang.GetNPCNameValue(id)}x{rule.Count}")));

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
            Utils.InitCondMap(cfg);   // 新增：初始化条件映射表
            cfg.BuildRuleIdx();
            cfg.AutoRuleDesc();
            cfg.Write();
            return cfg;
        }
        try
        {
            string json = File.ReadAllText(ConfigPath);
            var cfg = JsonConvert.DeserializeObject<Configuration>(json)!;
            cfg.BuildCondNames();   // 重新构建条件名称数组
            Utils.InitCondMap(cfg);   // 新增：使用当前配置初始化映射表
            cfg.BuildRuleIdx();
            cfg.AutoRuleDesc();
            cfg.EXProj.Types.Sort();  // 原地排序
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
    private void AB(int source, int target, int condIdx = 0, int count = 1)
    {
        string condStr = condIdx == 0 ? "" : condIdx.ToString();
        ConvRules.Add(new ConvRule
        {
            SourceID = source,
            Items = target.ToString(),      // 单个ID转为字符串
            Npcs = string.Empty,
            Count = count,
            Cond = condStr
        });
    }

    private void ABn(int source, int target, int condIdx = 0, int count = 1)
    {
        string condStr = condIdx == 0 ? "" : condIdx.ToString();
        ConvRules.Add(new ConvRule
        {
            SourceID = source,
            Items = string.Empty,
            Npcs = target.ToString(),
            Count = count,
            Cond = condStr,
            Luck = 0.5f
        });
    }

    /// <summary>单向链规则：A → B → C</summary>
    private void ABC(int a, int b, int c, int condIdx = 0)
    {
        AB(a, b, condIdx);
        AB(b, c, condIdx);
    }

    /// <summary>单向链规则：A → B → C → D</summary>
    private void ABCD(int a, int b, int c, int d, int condIdx = 0)
    {
        AB(a, b, condIdx);
        AB(b, c, condIdx);
        AB(c, d, condIdx);
    }

    private void ABA(int id1, int id2, int condIdx = 0)
    {
        AB(id1, id2, condIdx);
        AB(id2, id1, condIdx);
    }

    private void ABCA(int id1, int id2, int id3, int condIdx = 0)
    {
        AB(id1, id2, condIdx);
        AB(id2, id3, condIdx);
        AB(id3, id1, condIdx);
    }

    private void ABCDA(int id1, int id2, int id3, int id4, int condIdx = 0)
    {
        AB(id1, id2, condIdx);
        AB(id2, id3, condIdx);
        AB(id3, id4, condIdx);
        AB(id4, id1, condIdx);
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

    #region 重建规则索引与条件同步
    public void BuildRuleIdx()
    {
        ruleMap.Clear();
        foreach (var rule in ConvRules)
        {
            // 解析物品ID字符串（格式："73,676" 或 "73" 或 ""）
            rule.itemIds.Clear();
            if (!string.IsNullOrEmpty(rule.Items))
            {
                foreach (var part in rule.Items.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int id))
                        rule.itemIds.Add(id);
                    else
                        TShock.Log.ConsoleError($"[ConvGun] 规则中无效物品ID '{part.Trim()}'，源物品 {rule.SourceID}");
                }
            }

            // 解析怪物ID字符串
            rule.npcIds.Clear();
            if (!string.IsNullOrEmpty(rule.Npcs))
            {
                foreach (var part in rule.Npcs.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int id))
                        rule.npcIds.Add(id);
                    else
                        TShock.Log.ConsoleError($"[ConvGun] 规则中无效怪物ID '{part.Trim()}'，源物品 {rule.SourceID}");
                }
            }

            // 解析条件ID（保持不变）
            rule.condIds.Clear();
            if (!string.IsNullOrEmpty(rule.Cond))
            {
                foreach (var part in rule.Cond.Split(','))
                {
                    int id = Utils.GetCondId(part.Trim());
                    if (id >= 0)
                        rule.condIds.Add(id);
                    else
                        TShock.Log.ConsoleError($"[ConvGun] 规则中未知条件 '{part.Trim()}'，源物品 {rule.SourceID} 将作为无条件规则处理");
                }
            }

            // 构建索引
            if (!ruleMap.ContainsKey(rule.SourceID))
                ruleMap[rule.SourceID] = new List<ConvRule>();
            ruleMap[rule.SourceID].Add(rule);
        }
    }
    #endregion
}