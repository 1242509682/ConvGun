using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using TShockAPI;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace ConvGun;

#region 配置类（EXC）
public class EXC
{
    [JsonProperty("启用")] public bool Enabled = true;
    [JsonProperty("弹幕")] public List<int> Types = new();
    [JsonProperty("数量")] public int Cnt = 24;
    [JsonProperty("半径")] public float Rad = 3f;
    [JsonProperty("速度")] public float Spd = 5f;
    [JsonProperty("时间")] public int Life = 120;
    [JsonProperty("延迟")] public int Delay = 10;
    [JsonProperty("旋转")] public float Rot = 3f;
    [JsonProperty("波动幅度")] public float SpdWave = 0.3f;
    [JsonProperty("动态速度")] public bool Dynamic = true;
}
#endregion

public static class EXProj
{
    #region 弹幕内部数据管理（内存）
    private class ProjState
    {
        public int Owner, Idx, Type;
        public Vector2 Cen, Off;
        public float Spd, Rot, Rad;
        public long Expire, StartTime;
        public bool UseDynamic, Luck;
    }

    private static Dictionary<int, List<ProjState>> spaMap = new();
    private static Dictionary<int, List<ProjState>> upMap = new();

    // 获取或创建列表（简化重复代码）
    private static List<ProjState> GetList(Dictionary<int, List<ProjState>> dict, int owner)
    {
        if (!dict.TryGetValue(owner, out var list))
            dict[owner] = list = new List<ProjState>();
        return list;
    }

    public static void Clear()
    {
        foreach (var list in upMap)
            foreach (var p in list.Value)
                if (p.Idx != -1 && Main.projectile[p.Idx]?.active == true)
                    Kill(p.Idx, list.Key);

        spaMap.Clear();
        upMap.Clear();
    }

    public static void ClrPlayer(int owner)
    {
        spaMap.Remove(owner);
        if (upMap.TryGetValue(owner, out var list))
        {
            foreach (var p in list)
                if (p.Idx != -1 && Main.projectile[p.Idx]?.active == true)
                    Kill(p.Idx, owner);

            upMap.Remove(owner);
        }
    } 
    #endregion

    #region 生成弹幕信息方法
    public static void Spawn(int owner, Vector2 from, int extra = 0, bool isLuck = false)
    {
        var exc = Plugin.Config.EXProj;
        if (!exc.Enabled || exc.Types == null || exc.Types.Count == 0 || exc.Cnt <= 0) return;

        long now = Plugin.Timer;
        float radPx = exc.Rad * 16f;
        float incRad = MathHelper.TwoPi / exc.Cnt;
        long expire = extra > 0 ? now + extra + 1 : now + exc.Life;

        var spaList = GetList(spaMap, owner);
        for (int i = 0; i < exc.Cnt; i++)
        {
            float ang = i * incRad;
            Vector2 pos = from + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radPx;
            Vector2 vel = (from - pos).SafeNormalize(Vector2.Zero) * exc.Spd;

            int delay = (exc.Cnt - 1 - i) * exc.Delay;
            int type = exc.Types[Main.rand.Next(exc.Types.Count)];

            spaList.Add(new ProjState
            {
                Owner = owner,
                Idx = -1,
                Cen = from,
                Type = type,
                Spd = exc.Spd,
                Rot = MathHelper.ToRadians(exc.Rot),
                Rad = radPx,
                Off = Vector2.Zero,
                Expire = expire,
                StartTime = now + delay,
                UseDynamic = exc.Dynamic && Main.rand.Next(2) == 0,
                Luck = isLuck
            });

            if (isLuck)
            {
                var set = new ParticleOrchestraSettings { PositionInWorld = pos, MovementVector = vel };
                ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.StormLightning, set);
            }
        }
    } 
    #endregion

    #region 创建弹幕方法
    private static void CreateProj(ProjState p, long timer)
    {
        float ang = (float)Math.Atan2(p.Off.Y, p.Off.X);
        if (ang == 0 && p.Off == Vector2.Zero)
        {
            int idx = (int)((timer - p.StartTime) / Plugin.Config.EXProj.Delay) % Plugin.Config.EXProj.Cnt;
            ang = idx * MathHelper.TwoPi / Plugin.Config.EXProj.Cnt;
        }
        Vector2 pos = p.Cen + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * p.Rad;
        Vector2 vel = (p.Cen - pos).SafeNormalize(Vector2.Zero) * p.Spd;

        int idx2 = Projectile.NewProjectile(new EntitySource_WorldEvent(), pos.X, pos.Y, vel.X, vel.Y, p.Type, 0, 0, p.Owner, 1f, 0f, 0f);
        if (idx2 < 0 || idx2 >= Main.maxProjectiles) return;

        var proj = Main.projectile[idx2];
        proj.penetrate = -1;          // 无限穿透，不会因命中消失
        proj.tileCollide = false;     // 不碰撞物块
        proj.ignoreWater = true;      // 不受水影响
        proj.timeLeft = 36000;        // 极大值，完全由 Expire 控制
        proj.friendly = true;
        proj.hostile = false;
        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, idx2);
        p.Idx = idx2;
        GetList(upMap, p.Owner).Add(p);
    } 
    #endregion

    #region 每帧触发
    public static void Update(long timer)
    {
        // 待生成弹幕处理
        foreach (var kv in spaMap.ToList())
        {
            int owner = kv.Key;
            var list = kv.Value;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var p = list[i];
                if (timer >= p.StartTime)
                {
                    CreateProj(p, timer);
                    list.RemoveAt(i);
                }
            }
            if (list.Count == 0) spaMap.Remove(owner);
        }

        // 运动弹幕处理：超时强制销毁 + 检测不活跃弹幕
        foreach (var kv in upMap.ToList())
        {
            int owner = kv.Key;
            var list = kv.Value;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var s = list[i];
                if (timer >= s.Expire)
                {
                    // 超时，强制清除（无论弹幕是否还活着）
                    if (s.Idx != -1)
                        Kill(s.Idx, owner);

                    list.RemoveAt(i);
                }
                else
                {
                    // 弹幕已经不活跃但还没到 Expire → 异常残留，也清理掉
                    if (s.Idx == -1 || Main.projectile[s.Idx] == null || !Main.projectile[s.Idx].active)
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                    MoveProj(s, timer);
                }
            }
            if (list.Count == 0) upMap.Remove(owner);
        }
    }
    #endregion

    #region 弹幕动画方法（螺旋向心）
    private static void MoveProj(ProjState s, long timer)
    {
        var proj = Main.projectile[s.Idx];
        if (proj == null || !proj.active) return;

        Vector2 toCen = s.Cen - proj.Center;
        if (toCen.LengthSquared() <= 0.01f) return;

        Vector2 curDir = proj.velocity.SafeNormalize(Vector2.Zero);
        Vector2 targetDir = toCen.SafeNormalize(Vector2.Zero);
        float diff = (float)Math.Acos(MathHelper.Clamp(Vector2.Dot(curDir, targetDir), -1f, 1f));
        float step = Math.Min(s.Rot, diff);
        float sign = Math.Sign(Vector2.Dot(curDir, targetDir.RotatedBy(-MathHelper.PiOver2)));
        Vector2 newDir = curDir.RotatedBy(sign * step);

        var exc = Plugin.Config.EXProj;
        float curSpd = s.Spd;
        if (s.UseDynamic)
        {
            float distFactor = MathHelper.Clamp(toCen.Length() / s.Rad, 0.2f, 1.5f);
            float wave = exc.SpdWave > 0f ? 1f + exc.SpdWave * (float)Math.Sin(timer * 0.05f) : 1f;
            curSpd *= distFactor * wave;
        }
        proj.velocity = newDir * curSpd;
        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, s.Idx);
    }
    #endregion

    #region 杀死弹幕方法
    private static void Kill(int idx, int owner)
    {
        if (idx < 0) return;
        var p = Main.projectile[idx];
        if (p == null) return;
        p.active = false;
        p.type = ProjectileID.None;
        p.timeLeft = 0;
        // 先发销毁包，再发一次更新包强制同步
        NetMessage.SendData((int)PacketTypes.ProjectileDestroy, -1, -1, null, idx);
        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, idx);
        TSPlayer.All.RemoveProjectile(idx, owner);
    } 
    #endregion
}