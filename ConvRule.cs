using Newtonsoft.Json;

namespace ConvGun;

public class ConvRule
{
    [JsonProperty("描述")]
    public string Desc { get; set; } = "";

    [JsonProperty("源物品")]
    public int SourceID { get; set; }

    private string _items = "";
    [JsonProperty("物品")]
    public string Items
    {
        get => _items;
        set => _items = value ?? "";
    }

    private string _npcs = "";
    [JsonProperty("怪物")]
    public string Npcs
    {
        get => _npcs;
        set => _npcs = value ?? "";
    }

    [JsonProperty("数量")]
    public int Count { get; set; } = 1;

    private string _cond = "";
    [JsonProperty("条件")]
    public string Cond
    {
        get => _cond;
        set => _cond = value ?? "";
    }

    [JsonIgnore]
    public List<int> itemIds = new();   // 解析后的物品ID列表

    [JsonIgnore]
    public List<int> npcIds = new();    // 解析后的怪物ID列表

    [JsonIgnore]
    public List<int> condIds = new();   // 条件整数ID列表
}