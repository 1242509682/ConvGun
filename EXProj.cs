using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
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
    #region 运行时状态
    private class ProjState
    {
        public int Idx;
        public Vector2 Cen;
        public int Type;
        public float Spd;
        public float Rot;
        public float Rad;
        public Vector2 Off;
        public long Expire;
        public long StartTime;
        public bool UseDynamic;
        public bool Luck;
    }

    private static List<ProjState> upList = new();
    private static List<ProjState> spaList = new();
    #endregion

    #region 对外接口
    public static void Clear() => upList.Clear();

    public static void Spawn(Vector2 from, int extra = 0, bool isLuck = false)
    {
        var exc = Plugin.Config.EXProj;
        if (!exc.Enabled || exc.Types == null || exc.Types.Count == 0 || exc.Cnt <= 0) return;

        long now = Plugin.Timer;
        float radPx = exc.Rad * 16f;
        float incRad = MathHelper.TwoPi / exc.Cnt;
        // 绝对过期时间：分解场景 = now + extra + 1，其他场景 = now + exc.Life
        long expire = extra > 0 ? now + extra + 1 : now + exc.Life;

        for (int i = 0; i < exc.Cnt; i++)
        {
            float ang = i * incRad;
            Vector2 pos = from + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radPx;
            Vector2 vel = from - pos;
            if (vel != Vector2.Zero)
                vel = vel.SafeNormalize(Vector2.Zero) * exc.Spd;

            int delay = (exc.Cnt - 1 - i) * exc.Delay;
            long start = now + delay;

            int type = exc.Types[Main.rand.Next(exc.Types.Count)];

            spaList.Add(new ProjState
            {
                Idx = -1,
                Cen = from,
                Type = type,
                Spd = exc.Spd,
                Rot = MathHelper.ToRadians(exc.Rot),
                Rad = radPx,
                Off = Vector2.Zero,
                Expire = expire,
                StartTime = start,
                UseDynamic = exc.Dynamic ? Main.rand.Next(2) == 0 : false,
                Luck = isLuck
            });

            if (isLuck)
            {
                var set = new ParticleOrchestraSettings
                {
                    PositionInWorld = pos,
                    MovementVector = vel,
                };
                ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.StormLightning, set);
            }
        }
    }

    public static void Update(long timer)
    {
        // 创建新弹幕
        for (int i = spaList.Count - 1; i >= 0; i--)
        {
            var p = spaList[i];
            if (timer >= p.StartTime)
            {
                float ang = (float)Math.Atan2(p.Off.Y, p.Off.X);
                if (ang == 0 && p.Off == Vector2.Zero)
                {
                    int idx = (int)((timer - p.StartTime) / Plugin.Config.EXProj.Delay) % Plugin.Config.EXProj.Cnt;
                    ang = idx * MathHelper.TwoPi / Plugin.Config.EXProj.Cnt;
                }
                Vector2 pos = p.Cen + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * p.Rad;
                Vector2 vel = p.Cen - pos;
                if (vel != Vector2.Zero)
                    vel = vel.SafeNormalize(Vector2.Zero) * p.Spd;

                int idx2 = Projectile.NewProjectile(new EntitySource_WorldEvent(),
                    pos.X, pos.Y, vel.X, vel.Y, p.Type, 0, 0, 0, 0f, 0f, 0f);
                if (idx2 >= 0 && idx2 < Main.maxProjectiles)
                {
                    var proj = Main.projectile[idx2];
                    int life = (int)(p.Expire - timer);
                    if (life < 1) life = 1;
                    proj.timeLeft = life;
                    proj.friendly = true;
                    proj.hostile = false;
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, idx2);
                    p.Idx = idx2;
                    upList.Add(p);
                    spaList.RemoveAt(i);

                    if (p.Luck)
                    {
                        var set = new ParticleOrchestraSettings
                        {
                            PositionInWorld = pos,
                            MovementVector = vel,
                        };
                        ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.StormLightning, set);
                    }
                }
            }
        }

        // 更新弹幕
        if (upList.Count == 0) return;
        var exc = Plugin.Config.EXProj;
        for (int i = upList.Count - 1; i >= 0; i--)
        {
            var s = upList[i];
            if (timer >= s.Expire)
            {
                var p = Main.projectile[s.Idx];
                if (p != null && p.active)
                {
                    p.active = false;
                    NetMessage.SendData((int)PacketTypes.ProjectileDestroy, -1, -1, null, s.Idx);
                }
                upList.RemoveAt(i);
                continue;
            }

            var proj = Main.projectile[s.Idx];
            if (proj == null || !proj.active)
            {
                upList.RemoveAt(i);
                continue;
            }

            // 螺旋向心 + 动态速度
            Vector2 toCen = s.Cen - proj.Center;
            if (toCen.LengthSquared() > 0.01f)
            {
                Vector2 curDir = proj.velocity.SafeNormalize(Vector2.Zero);
                Vector2 targetDir = toCen.SafeNormalize(Vector2.Zero);
                float diff = (float)Math.Acos(MathHelper.Clamp(Vector2.Dot(curDir, targetDir), -1f, 1f));
                float step = Math.Min(s.Rot, diff);
                float sign = Math.Sign(Vector2.Dot(curDir, targetDir.RotatedBy(-MathHelper.PiOver2)));
                Vector2 newDir = curDir.RotatedBy(sign * step);

                float curSpd;
                if (s.UseDynamic)
                {
                    float distFactor = MathHelper.Clamp(toCen.Length() / s.Rad, 0.2f, 1.5f);
                    float waveFactor = 1f;
                    if (exc.SpdWave > 0f)
                        waveFactor = 1f + exc.SpdWave * (float)Math.Sin(timer * 0.05f);
                    curSpd = s.Spd * distFactor * waveFactor;
                }
                else
                {
                    curSpd = s.Spd;
                }

                proj.velocity = newDir * curSpd;
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, s.Idx);
            }
        }
    }
    #endregion
}