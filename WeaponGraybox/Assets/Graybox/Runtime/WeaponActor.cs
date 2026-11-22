using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeaponGraybox
{
    public sealed class AttackRuntime
    {
        public long Id;
        public double Started;
        public float Startup, Active, Recovery, Damage;
        public bool Heavy, Armored;
        public int Chain;
        public readonly HashSet<string> HitTargets = new HashSet<string>();
        public float Age(FighterState state) { return (float)(state.SimulationTime - Started); }
        public bool ActiveNow(FighterState state) { float t = Age(state); return t >= Startup && t < Startup + Active; }
        public bool Finished(FighterState state) { return Age(state) >= Startup + Active + Recovery; }
    }

    public sealed class WeaponActor : MonoBehaviour
    {
        public FighterState State { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public BoxCollider2D Blade { get; private set; }
        public BoxCollider2D Hilt { get; private set; }
        public AttackRuntime Attack { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsBoss { get; private set; }
        public bool TrainingDummy { get; set; }
        public float Facing { get; private set; } = 1;
        public float MoveInput;
        public bool SuppressBrain;
        public string ActionLabel
        {
            get
            {
                if (!State.IsAlive) return "倒下";
                if (State.IsBroken) return "破势";
                if (State.DashActive) return (State.DashIsCharged ? "蓄力冲刺 · " : "冲刺 · ") + (State.DashPhase == DashPhase.Middle ? "中段" : State.DashPhase == DashPhase.Startup ? "开头" : "结尾");
                if (State.ChargeHeld) return State.IsFullyCharged ? "已蓄满 · 保持 S 按 L" : "原地蓄力";
                if (Attack != null) return Attack.Heavy ? "重攻击" : "轻攻击 · 第 " + (Attack.Chain + 1) + " 段";
                if (State.SimulationTime < stunUntil) return "恢复";
                return "可操作";
            }
        }
        GrayboxGame game;
        GrayboxSettings tuning;
        SpriteRenderer bladeVisual, hiltVisual;
        LineRenderer bladeOutline, hiltOutline;
        Color baseBlade;
        double stunUntil, nextBossAction;
        bool queuedLight, queuedHeavy, queuedJump, queuedDash, queuedCharge, queuedRelease, deriveQueued, armorPhaseUsed;
        long dashCommand;
        int chain, bossMove;
        float dashDirection = 1;
        double lastTrail;

        public void Initialize(GrayboxGame owner, GrayboxSettings settings, int kind, bool boss, float hp, bool infinite)
        {
            game = owner; tuning = settings; IsBoss = boss;
            DisplayName = boss ? "测试 Boss · 长刃" : new[] { "刀", "剑", "矛" }[kind];
            State = new FighterState(settings.Rules, hp, boss ? "boss" : "player") { InfiniteHealth = infinite };
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.gravityScale = 2.5f; Body.mass = boss ? 1.4f : 1;
            Body.linearDamping = 1; Body.angularDamping = 3;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var support = gameObject.AddComponent<CircleCollider2D>(); support.radius = boss ? 1.25f : .95f;
            float bladeLength = boss ? 1.8f : kind == 0 ? 1.15f : kind == 1 ? 1.5f : .55f;
            float hiltLength = boss ? .7f : kind == 2 ? 1.55f : .55f;
            float thickness = boss ? .38f : kind == 0 ? .36f : .23f;
            baseBlade = boss ? new Color(.86f, .48f, .25f) : new Color(.42f, .82f, .88f);
            float junction = !boss && kind == 2 ? .72f : 0;
            Hilt = Region("Hilt", new Vector2(junction - hiltLength * .5f, 0), new Vector2(hiltLength, .25f), new Color(.5f, .44f, .46f), out hiltVisual);
            Blade = Region("Blade", new Vector2(junction + bladeLength * .5f, 0), new Vector2(bladeLength, thickness), baseBlade, out bladeVisual);
            bladeOutline = GrayboxShapes.Outline("BladeOutline", transform, Blade.offset, Blade.size, new Color(.65f, 1, 1));
            hiltOutline = GrayboxShapes.Outline("HiltOutline", transform, Hilt.offset, Hilt.size, new Color(1, .5f, .7f));
            GrayboxShapes.Box("Pivot", transform, Vector2.zero, new Vector2(.12f, .12f), Color.white, 8);
            Facing = boss ? -1 : 1; Body.rotation = Facing > 0 ? 18 : 162;
            nextBossAction = 1;
        }

        BoxCollider2D Region(string name, Vector2 center, Vector2 size, Color color, out SpriteRenderer renderer)
        {
            // Both hit regions belong to the same rotating, force-driven body.
            var region = new GameObject(name); region.transform.SetParent(transform, false);
            var col = region.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.offset = center; col.size = size;
            renderer = GrayboxShapes.Box(name + "Visual", transform, center, size, color, 4).GetComponent<SpriteRenderer>();
            return col;
        }
        public void PressLight() { queuedLight = true; }
        public void PressHeavy() { queuedHeavy = true; }
        public void PressJump() { queuedJump = true; }
        public void PressDash() { queuedDash = true; }
        // Preserve the final key state when several input events arrive before one physics step.
        public void PressCharge() { queuedCharge = true; queuedRelease = false; }
        public void ReleaseCharge() { queuedRelease = true; queuedCharge = false; }
        public void ClearInput()
        {
            MoveInput = 0; queuedLight = queuedHeavy = queuedJump = queuedDash = queuedCharge = queuedRelease = false;
        }
        bool Grounded { get { return Physics2D.OverlapCircle(Body.position + Vector2.down * (IsBoss ? 1.27f : .97f), .1f, 1 << 8) != null; } }
        bool CancelAllowed
        {
            get { return Attack == null || (Attack.Age(State) >= tuning.CancelWindowStart && Attack.Age(State) <= tuning.CancelWindowEnd); }
        }
        public void Step(float dt, long tick)
        {
            State.Tick(dt, tick);
            if (!State.CanAct) { Attack = null; deriveQueued = false; ClearInput(); }
            if (State.IsBroken) State.CancelDash();
            if (Attack != null && Attack.Finished(State))
            {
                Attack = null;
                if (deriveQueued) { deriveQueued = false; StartAttack(false, (chain + 1) % 4); }
            }
            if (IsBoss && !TrainingDummy && !SuppressBrain && State.CanAct) BossBrain();
            if (queuedRelease) State.ReleaseCharge();
            if (queuedCharge && Attack == null && State.SimulationTime >= stunUntil) State.StartCharge();
            if (Mathf.Abs(MoveInput) > .1f || queuedJump) State.PerformUnchargedAction();
            if (queuedDash && CancelAllowed && State.SimulationTime >= stunUntil && State.TryStartDash(++dashCommand))
            {
                Attack = null; deriveQueued = false; dashDirection = Facing;
                game.Report(State.DashIsCharged ? "蓄力冲刺：只有残影中段获得保护" : "普通冲刺：没有无敌");
            }
            if (queuedJump && State.CanAct && !State.DashActive && CancelAllowed && State.SimulationTime >= stunUntil && Grounded)
            {
                Attack = null; State.PerformUnchargedAction(); Body.AddForce(Vector2.up * tuning.JumpImpulse, ForceMode2D.Impulse);
            }
            if (queuedHeavy) RequestAttack(true);
            else if (queuedLight) RequestAttack(false);
            queuedLight = queuedHeavy = queuedJump = queuedDash = queuedCharge = queuedRelease = false;

            if (State.DashActive)
            {
                DriveHorizontal(dashDirection * tuning.DashSpeed, 170);
                Body.AddForce(Vector2.up * (9.81f * Body.gravityScale * Body.mass), ForceMode2D.Force);
                if (State.HasDashProtection && State.SimulationTime - lastTrail > .035)
                {
                    lastTrail = State.SimulationTime; game.Trail(this);
                }
            }
            else if (State.CanAct && State.SimulationTime >= stunUntil && Attack == null)
            {
                float move = State.ChargeHeld ? 0 : MoveInput;
                if (Mathf.Abs(move) > .1f) Facing = Mathf.Sign(move);
                DriveHorizontal(move * (IsBoss ? tuning.BossMoveSpeed : tuning.MoveSpeed), tuning.MoveAcceleration);
            }
            else DriveHorizontal(0, 12);
            if (Attack != null && Attack.Armored && Attack.ActiveNow(State))
                State.GrantArmor(Mathf.Max(.001f, Attack.Startup + Attack.Active - Attack.Age(State)));
            DriveAngle();
            UpdateVisuals();
            if (Body.position.y < -8 || Mathf.Abs(Body.position.x) > 15)
            {
                Body.position = new Vector2(Mathf.Clamp(Body.position.x, -12, 12), -2);
                Body.linearVelocity = Vector2.zero; State.CancelDash();
            }
        }
        void DriveHorizontal(float target, float acceleration)
        {
            Body.AddForce(new Vector2(Mathf.Clamp((target - Body.linearVelocity.x) * 12, -acceleration, acceleration) * Body.mass, 0));
        }
        void DriveAngle()
        {
            float angle = Facing > 0 ? 18 : 162;
            if (Attack != null)
            {
                float age = Attack.Age(State), swing;
                if (age < Attack.Startup) swing = Mathf.Lerp(5, 65, age / Attack.Startup);
                else if (age < Attack.Startup + Attack.Active) swing = Mathf.Lerp(65, -65, (age - Attack.Startup) / Attack.Active);
                else swing = Mathf.Lerp(-65, 0, (age - Attack.Startup - Attack.Active) / Attack.Recovery);
                angle = (Facing > 0 ? 0 : 180) + swing * Facing;
            }
            else if (State.ChargeHeld) angle = Facing > 0 ? 65 : 115;
            float wanted = Mathf.Clamp(Mathf.DeltaAngle(Body.rotation, angle) * tuning.AngularGain, -850, 850);
            float angularAcceleration = Mathf.Clamp((wanted - Body.angularVelocity) * 30, -tuning.AngularAcceleration, tuning.AngularAcceleration);
            Body.AddTorque(angularAcceleration * Mathf.Deg2Rad * Body.inertia);
            Body.angularVelocity = Mathf.Clamp(Body.angularVelocity, -900, 900);
        }
        void RequestAttack(bool heavy)
        {
            if (!State.CanAct || State.DashActive || State.SimulationTime < stunUntil) return;
            if (Attack != null)
            {
                if (!heavy && !Attack.Heavy && Attack.Age(State) >= Attack.Startup + Attack.Active + Attack.Recovery - tuning.DeriveBuffer)
                    deriveQueued = true;
                return;
            }
            StartAttack(heavy, 0);
        }
        public bool StartAttack(bool heavy, int chainIndex = 0, bool armored = false)
        {
            if (!State.CanAct || State.DashActive || Attack != null || State.SimulationTime < stunUntil) return false;
            State.PerformUnchargedAction(); chain = chainIndex;
            Attack = new AttackRuntime
            {
                Id = game.NextAttackId(), Started = State.SimulationTime, Heavy = heavy, Chain = chainIndex,
                Startup = IsBoss ? tuning.BossStartup : heavy ? tuning.HeavyStartup : tuning.LightStartup,
                Active = IsBoss ? tuning.BossActive : heavy ? tuning.HeavyActive : tuning.LightActive,
                Recovery = IsBoss ? tuning.BossRecovery : heavy ? tuning.HeavyRecovery : tuning.LightRecovery,
                Damage = IsBoss ? tuning.BossDamage * (heavy ? 1.3f : 1) : (heavy ? tuning.HeavyDamage : tuning.LightDamage) * (1 + .12f * chainIndex),
                Armored = armored || (!heavy && chainIndex == 3)
            };
            WeaponActor target = IsBoss ? game.Player : game.Boss;
            if (target && !target.TrainingDummy && Vector2.Distance(Body.position, target.Body.position) < 4.5f)
            {
                Facing = Mathf.Sign(target.Body.position.x - Body.position.x);
                if (Facing == 0) Facing = 1;
                Body.AddForce(Vector2.right * Facing * tuning.ApproachImpulse, ForceMode2D.Impulse);
            }
            return true;
        }
        void BossBrain()
        {
            if (!game.Player) return;
            if (!armorPhaseUsed && State.HP < State.MaxHP * .5f)
            {
                armorPhaseUsed = true; State.GrantArmor(tuning.BossArmorDuration);
                game.Report("Boss 进入限时霸体：躲避攻击，仍可造成血量伤害");
            }
            if (Attack != null || State.DashActive || State.SimulationTime < stunUntil) return;
            float dx = game.Player.Body.position.x - Body.position.x;
            Facing = dx >= 0 ? 1 : -1;
            MoveInput = Mathf.Abs(dx) > tuning.BossAttackRange ? Facing : 0;
            if (State.SimulationTime >= nextBossAction && Mathf.Abs(dx) <= tuning.BossAttackRange + .2f)
            {
                StartAttack(bossMove % 3 == 1, 0, bossMove % 3 == 2);
                bossMove++;
                nextBossAction = State.SimulationTime + tuning.BossStartup + tuning.BossActive + tuning.BossRecovery + tuning.BossThinkDelay;
            }
        }
        public void ReactToHit(HitResult result, float direction)
        {
            if (result.AllowKnockback) Body.AddForce(new Vector2(direction * tuning.Knockback, .5f), ForceMode2D.Impulse);
            if (result.AllowInterruption)
            {
                Attack = null; deriveQueued = false;
                stunUntil = Math.Max(stunUntil, State.SimulationTime + tuning.HitStun);
            }
            if (result.BrokePosture || result.Killed) { Attack = null; deriveQueued = false; }
        }
        public void Parried()
        {
            Attack = null; deriveQueued = false;
            stunUntil = Math.Max(stunUntil, State.SimulationTime + tuning.ParryRecovery);
        }
        void UpdateVisuals()
        {
            Color tint = State.IsBroken ? new Color(1, .3f, .35f) : State.IsArmored ? new Color(1, .78f, .2f) : baseBlade;
            if (State.HasDashProtection) tint = new Color(.7f, 1, 1, .35f);
            else if (Attack != null && Attack.Age(State) < Attack.Startup)
                tint = Attack.Armored ? new Color(1, .72f, .1f) : Color.Lerp(tint, Color.white, .5f);
            bladeVisual.color = tint;
            hiltVisual.color = State.HasDashProtection ? new Color(.5f, .7f, .8f, .35f) : new Color(.5f, .44f, .46f);
            bladeOutline.enabled = hiltOutline.enabled = game.ShowHitboxes;
        }
    }
}
