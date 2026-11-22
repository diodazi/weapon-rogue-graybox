using System;
using System.Collections.Generic;

namespace WeaponGraybox
{
    // All values below are PROTOTYPE_ONLY except the confirmed two shared dash slots
    // and the PROVISIONAL 1.5-second recharge interval. This is not a balance specification.
    [Serializable]
    public sealed class CombatConfig
    {
        public float MaxPosture = 100f;
        public float BladeDamageMultiplier = 0.65f;
        public float HiltDamageMultiplier = 1.4f;
        public float BrokenDamageMultiplier = 1.5f;
        public float BreakDuration = 1.1f;
        public float PostBreakLockDuration = 0.8f;
        public float PostureDecayDelay = 1.2f;
        public float PostureDecayPerSecond = 20f;
        public float ChargeDuration = 0.65f;
        public int DashCapacity = 2;
        public float DashRechargeSeconds = 1.5f;
        public float DashStartupDuration = 0.05f;
        public float DashMiddleDuration = 0.18f;
        public float DashRecoveryDuration = 0.07f;
        public float ParryProtectionDuration = 0.06f;
        public float ParryAttackerPressure = 24f;
        public float ParryDefenderPressure = 8f;
        public float PreciseParryExtraPressure = 16f;

        public void Validate()
        {
            Positive(MaxPosture, nameof(MaxPosture));
            Positive(BreakDuration, nameof(BreakDuration));
            Positive(ChargeDuration, nameof(ChargeDuration));
            Positive(DashRechargeSeconds, nameof(DashRechargeSeconds));
            Positive(DashStartupDuration, nameof(DashStartupDuration));
            Positive(DashMiddleDuration, nameof(DashMiddleDuration));
            Positive(DashRecoveryDuration, nameof(DashRecoveryDuration));
            Nonnegative(BladeDamageMultiplier, nameof(BladeDamageMultiplier));
            Nonnegative(HiltDamageMultiplier, nameof(HiltDamageMultiplier));
            Nonnegative(BrokenDamageMultiplier, nameof(BrokenDamageMultiplier));
            Nonnegative(PostBreakLockDuration, nameof(PostBreakLockDuration));
            Nonnegative(PostureDecayDelay, nameof(PostureDecayDelay));
            Nonnegative(PostureDecayPerSecond, nameof(PostureDecayPerSecond));
            Nonnegative(ParryProtectionDuration, nameof(ParryProtectionDuration));
            Nonnegative(ParryAttackerPressure, nameof(ParryAttackerPressure));
            Nonnegative(ParryDefenderPressure, nameof(ParryDefenderPressure));
            Nonnegative(PreciseParryExtraPressure, nameof(PreciseParryExtraPressure));
            if (DashCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(DashCapacity));
        }

        internal static void Nonnegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
        }

        internal static void Positive(float value, string name)
        {
            Nonnegative(value, name);
            if (value <= 0f) throw new ArgumentOutOfRangeException(name);
        }
    }

    public enum HitZone { Blade, Hilt }
    public enum DashPhase { None, Startup, Middle, Recovery }

    public struct HitSpec
    {
        public float Damage;
        public float Posture;
        public HitZone Zone;
        public bool Knockback;
        public bool Interrupt;

        public HitSpec(float damage, float posture, HitZone zone = HitZone.Blade,
            bool knockback = true, bool interrupt = true)
        {
            CombatConfig.Nonnegative(damage, nameof(damage));
            CombatConfig.Nonnegative(posture, nameof(posture));
            Damage = damage;
            Posture = posture;
            Zone = zone;
            Knockback = knockback;
            Interrupt = interrupt;
        }
    }

    public struct HitResult
    {
        public float HpDamage;
        public float PostureAdded;
        public bool AllowKnockback;
        public bool AllowInterruption;
        public bool BrokePosture;
        public bool Killed;
        public bool HpProtected;
        public bool FullDashProtection;
    }

    // Engine-independent state. Advance it exactly once per simulation step.
    // The tick-id overload makes duplicate simulation callbacks harmless.
    public sealed class FighterState
    {
        private const double Epsilon = 0.0000001;
        private double _now;
        private double _armorUntil;
        private double _parryUntil;
        private double _breakUntil;
        private double _postBreakLockUntil;
        private double _lastPressureTime = double.NegativeInfinity;
        private double _chargeStartedAt;
        private double _dashStartedAt;
        private double _dashUntil;
        private double _nextRecharge = double.PositiveInfinity;
        private long _lastDashCommand = long.MinValue;
        private long _lastTickId = long.MinValue;
        private bool _pendingArmorBreak;

        public CombatConfig Config { get; private set; }
        public string Id { get; private set; }
        public float MaxHP { get; private set; }
        public float HP { get; private set; }
        public float Posture { get; private set; }
        public bool InfiniteHealth { get; set; }
        public bool ChargeHeld { get; private set; }
        public int DashCharges { get; private set; }
        public bool DashIsCharged { get; private set; }
        public double SimulationTime { get { return _now; } }
        public bool IsAlive { get { return HP > 0f || InfiniteHealth; } }
        public bool IsBroken { get { return _breakUntil > _now + Epsilon; } }
        public bool IsArmored { get { return _armorUntil > _now + Epsilon; } }
        public bool CanAct { get { return IsAlive && !IsBroken; } }
        public bool IsPostureLocked { get { return IsBroken || IsArmored || PostBreakLockRemaining > 0f; } }
        public float BreakRemaining { get { return Remaining(_breakUntil); } }
        public float ArmorRemaining { get { return Remaining(_armorUntil); } }
        public float ParryProtectionRemaining { get { return Remaining(_parryUntil); } }
        public float PostBreakLockRemaining { get { return Remaining(_postBreakLockUntil); } }
        public float RechargeRemaining { get { return DashCharges >= Config.DashCapacity ? 0f : Remaining(_nextRecharge); } }
        public float ChargeProgress { get { return ChargeHeld ? Clamp01((float)(_now - _chargeStartedAt) / Config.ChargeDuration) : 0f; } }
        public bool IsFullyCharged { get { return ChargeHeld && _now - _chargeStartedAt >= Config.ChargeDuration - Epsilon; } }
        public bool DashActive { get { return _dashUntil > _now + Epsilon; } }
        public DashPhase DashPhase
        {
            get
            {
                if (!DashActive) return WeaponGraybox.DashPhase.None;
                double age = _now - _dashStartedAt;
                if (age < Config.DashStartupDuration - Epsilon) return WeaponGraybox.DashPhase.Startup;
                if (age < Config.DashStartupDuration + Config.DashMiddleDuration - Epsilon) return WeaponGraybox.DashPhase.Middle;
                return WeaponGraybox.DashPhase.Recovery;
            }
        }
        public bool HasDashProtection { get { return DashIsCharged && DashPhase == WeaponGraybox.DashPhase.Middle; } }

        public FighterState(CombatConfig config, float maxHp, string id)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            CombatConfig.Positive(maxHp, nameof(maxHp));
            Config = config;
            Id = id ?? "fighter";
            MaxHP = maxHp;
            HP = maxHp;
            DashCharges = config.DashCapacity;
        }

        public void Tick(float deltaTime, long simulationTick)
        {
            CombatConfig.Nonnegative(deltaTime, nameof(deltaTime));
            if (simulationTick <= _lastTickId) return;
            _lastTickId = simulationTick;
            Tick(deltaTime);
        }

        public void Tick(float deltaTime)
        {
            CombatConfig.Nonnegative(deltaTime, nameof(deltaTime));
            double end = _now + deltaTime;
            ProcessTransitions();
            // Split at timer boundaries so a long tick preserves full break and recovery durations.
            while (_now < end - Epsilon)
            {
                double next = end;
                ChooseBoundary(ref next, _armorUntil);
                ChooseBoundary(ref next, _breakUntil);
                ChooseBoundary(ref next, _postBreakLockUntil);
                ChooseBoundary(ref next, _nextRecharge);
                ChooseBoundary(ref next, _lastPressureTime + Config.PostureDecayDelay);
                if (!IsPostureLocked && _now + Epsilon >= _lastPressureTime + Config.PostureDecayDelay)
                    Posture = Math.Max(0f, Posture - (float)(next - _now) * Config.PostureDecayPerSecond);
                _now = next;
                ProcessTransitions();
            }
            _now = end;
            ProcessTransitions();
        }

        public bool StartCharge()
        {
            if (!CanAct || DashActive || ChargeHeld) return false;
            ChargeHeld = true;
            _chargeStartedAt = _now;
            return true;
        }

        // PROTOTYPE_ONLY: releasing S clears charge; a completed dash needs a fresh S press.
        public void ReleaseCharge() { ClearCharge(); }
        public void PerformUnchargedAction() { ClearCharge(); }

        public bool TryStartDash(long commandId)
        {
            // Command IDs are monotonically increasing for this fighter. Bounded state, no growing ID set.
            if (commandId <= _lastDashCommand) return false;
            _lastDashCommand = commandId;
            if (!CanAct || DashActive || DashCharges <= 0) return false;
            bool charged = IsFullyCharged;
            bool wasFull = DashCharges == Config.DashCapacity;
            DashCharges--;
            if (wasFull) _nextRecharge = _now + Config.DashRechargeSeconds;
            DashIsCharged = charged;
            _dashStartedAt = _now;
            _dashUntil = _now + Config.DashStartupDuration + Config.DashMiddleDuration + Config.DashRecoveryDuration;
            ClearCharge();
            return true;
        }

        public void CancelDash()
        {
            _dashUntil = _now;
            DashIsCharged = false;
            // Removing dash protection deliberately does not touch armor or parry protection.
        }

        internal struct HitSnapshot
        {
            internal bool Alive, FullProtection, HpProtected, Broken, Armored, PostureLocked;
            internal float BladeMultiplier, HiltMultiplier, BrokenMultiplier;
        }

        internal HitSnapshot CaptureHitSnapshot()
        {
            bool fullProtection = HasDashProtection;
            return new HitSnapshot
            {
                Alive = IsAlive,
                FullProtection = fullProtection,
                HpProtected = fullProtection || InfiniteHealth || ParryProtectionRemaining > 0f,
                Broken = IsBroken,
                Armored = IsArmored,
                PostureLocked = IsPostureLocked,
                BladeMultiplier = Config.BladeDamageMultiplier,
                HiltMultiplier = Config.HiltDamageMultiplier,
                BrokenMultiplier = Config.BrokenDamageMultiplier
            };
        }

        public HitResult ApplyHit(HitSpec hit)
        {
            HitSnapshot snapshot = CaptureHitSnapshot();
            return CommitHit(PrepareHit(hit, snapshot), snapshot);
        }

        private static HitResult PrepareHit(HitSpec hit, HitSnapshot snapshot)
        {
            CombatConfig.Nonnegative(hit.Damage, nameof(hit.Damage));
            CombatConfig.Nonnegative(hit.Posture, nameof(hit.Posture));
            HitResult result = new HitResult();
            if (!snapshot.Alive) return result;
            result.FullDashProtection = snapshot.FullProtection;
            result.HpProtected = snapshot.HpProtected;
            result.AllowKnockback = hit.Knockback && !snapshot.FullProtection && !snapshot.Armored;
            result.AllowInterruption = hit.Interrupt && !snapshot.FullProtection && !snapshot.Armored && !snapshot.Broken;
            if (!snapshot.HpProtected)
            {
                float regionMultiplier = hit.Zone == HitZone.Hilt ? snapshot.HiltMultiplier : snapshot.BladeMultiplier;
                result.HpDamage = hit.Damage * regionMultiplier * (snapshot.Broken ? snapshot.BrokenMultiplier : 1f);
            }
            if (!snapshot.FullProtection && !snapshot.PostureLocked) result.PostureAdded = hit.Posture;
            return result;
        }

        // Every hit in an exchange reads the same pre-exchange effects. Commit the aggregate
        // once so one hit cannot expose a dash, trigger damage amplification, or kill the
        // fighter before the remaining hits have been evaluated.
        internal HitResult ApplyHitBatch(IList<HitSpec> hits, HitSnapshot snapshot)
        {
            HitResult aggregate = new HitResult();
            double damage = 0d, pressure = 0d;
            foreach (HitSpec hit in hits)
            {
                HitResult prepared = PrepareHit(hit, snapshot);
                damage += prepared.HpDamage;
                pressure += prepared.PostureAdded;
                aggregate.FullDashProtection |= prepared.FullDashProtection;
                aggregate.HpProtected |= prepared.HpProtected;
                aggregate.AllowKnockback |= prepared.AllowKnockback;
                aggregate.AllowInterruption |= prepared.AllowInterruption;
            }
            aggregate.HpDamage = (float)Math.Min(damage, HP);
            aggregate.PostureAdded = (float)Math.Min(pressure, Config.MaxPosture);
            return CommitHit(aggregate, snapshot);
        }

        private HitResult CommitHit(HitResult result, HitSnapshot snapshot)
        {
            if (!snapshot.Alive) return result;
            result.HpDamage = Math.Min(HP, result.HpDamage);
            HP = Math.Max(0f, HP - result.HpDamage);
            result.PostureAdded = AddPressure(result.PostureAdded);
            result.BrokePosture = !snapshot.Broken && IsBroken;
            result.Killed = !IsAlive;
            if (result.AllowInterruption || result.Killed)
            {
                ClearCharge();
                CancelDash();
            }
            return result;
        }

        // Parry exchange pressure is a settlement, not an HP hit; HP-only protection never cancels it.
        public float ApplyParryPressure(float pressure)
        {
            CombatConfig.Nonnegative(pressure, nameof(pressure));
            return AddPressure(pressure);
        }

        public void GrantParryProtection(float duration)
        {
            CombatConfig.Nonnegative(duration, nameof(duration));
            _parryUntil = Math.Max(_parryUntil, _now + duration);
        }

        public void GrantArmor(float duration)
        {
            CombatConfig.Nonnegative(duration, nameof(duration));
            // PROTOTYPE_ONLY: armor cannot cancel an already active fixed-duration break.
            if (IsBroken || !IsAlive) return;
            _armorUntil = Math.Max(_armorUntil, _now + duration);
        }

        public void SetPostureFromScript(float value)
        {
            CombatConfig.Nonnegative(value, nameof(value));
            if (IsBroken || PostBreakLockRemaining > 0f) return;
            Posture = Math.Min(Config.MaxPosture, value);
            _lastPressureTime = _now;
            if (Posture >= Config.MaxPosture)
            {
                if (IsArmored) _pendingArmorBreak = true;
                else BeginBreak();
            }
            else _pendingArmorBreak = false;
        }

        private float AddPressure(float pressure)
        {
            if (pressure <= 0f || IsPostureLocked || !IsAlive) return 0f;
            float before = Posture;
            Posture = Math.Min(Config.MaxPosture, Posture + pressure);
            _lastPressureTime = _now;
            if (Posture >= Config.MaxPosture) BeginBreak();
            return Posture - before;
        }

        private void BeginBreak()
        {
            if (IsBroken || IsArmored || PostBreakLockRemaining > 0f) return;
            _breakUntil = _now + Config.BreakDuration;
            _pendingArmorBreak = false;
            ClearCharge();
            CancelDash();
        }

        private void ProcessTransitions()
        {
            if (_armorUntil > 0d && _armorUntil <= _now + Epsilon)
            {
                _armorUntil = 0d;
                if (_pendingArmorBreak && Posture >= Config.MaxPosture) BeginBreak();
                _pendingArmorBreak = false;
            }
            if (_breakUntil > 0d && _breakUntil <= _now + Epsilon)
            {
                _breakUntil = 0d;
                Posture = 0f;
                _postBreakLockUntil = _now + Config.PostBreakLockDuration;
                _lastPressureTime = _now;
            }
            if (_postBreakLockUntil > 0d && _postBreakLockUntil <= _now + Epsilon)
                _postBreakLockUntil = 0d;
            if (_dashUntil > 0d && _dashUntil <= _now + Epsilon) CancelDash();
            // Serial recharge is PROTOTYPE_ONLY. Each deadline awards exactly one slot.
            while (DashCharges < Config.DashCapacity && _nextRecharge <= _now + Epsilon)
            {
                DashCharges++;
                _nextRecharge = DashCharges < Config.DashCapacity
                    ? _nextRecharge + Config.DashRechargeSeconds : double.PositiveInfinity;
            }
        }

        private void ClearCharge() { ChargeHeld = false; }
        private float Remaining(double until) { return until > _now + Epsilon ? (float)(until - _now) : 0f; }
        private static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
        private void ChooseBoundary(ref double next, double candidate)
        {
            if (candidate > _now + Epsilon && candidate < next) next = candidate;
        }
    }

    public sealed class ParryPair
    {
        public FighterState Attacker;
        public FighterState Defender;
        public long AttackInstanceId;
        public bool DefenseSucceeded;
        public bool Precise;
        public HitSpec FailedDefenseHit;
    }

    public sealed class ParryExchangeResult
    {
        public int SuccessfulPairs;
        public int FailedPairs;
        public int AttackerPressureSettlements;
        public float TotalFailedHpDamage;
    }

    // The caller owns exchange boundaries and supplies only actual blade contacts in valid windows.
    // A new exchange object must never be reused for a lingering collision's duplicate callbacks.
    public static class ParryExchange
    {
        public static ParryExchangeResult Resolve(IList<ParryPair> pairs)
        {
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));
            var result = new ParryExchangeResult();
            var uniquePairs = new HashSet<string>();
            var attackerPressure = new Dictionary<FighterState, float>();
            var defenderPressure = new Dictionary<FighterState, float>();
            var protectedFighters = new HashSet<FighterState>();
            var accepted = new List<ParryPair>();
            foreach (ParryPair pair in pairs)
            {
                if (pair == null || pair.Attacker == null || pair.Defender == null)
                    throw new ArgumentException("A parry pair must contain both participants.", nameof(pairs));
                // IDs identify fighters, not display names. State collections die with this exchange.
                string key = pair.Attacker.Id + "|" + pair.AttackInstanceId + "|" + pair.Defender.Id;
                if (!uniquePairs.Add(key)) continue;
                accepted.Add(pair);
            }
            // First determine every outcome before damage changes armor, break or death state.
            var successes = new List<ParryPair>();
            var failures = new List<ParryPair>();
            foreach (ParryPair pair in accepted)
            {
                if (pair.DefenseSucceeded && !pair.Attacker.IsArmored) successes.Add(pair);
                else failures.Add(pair);
            }
            // Snapshot all recipients before committing any failed hit. Per-recipient batches
            // also prevent a newly triggered break from amplifying another simultaneous hit.
            var hitSnapshots = new Dictionary<FighterState, FighterState.HitSnapshot>();
            var failedHits = new Dictionary<FighterState, List<HitSpec>>();
            foreach (ParryPair pair in failures)
            {
                List<HitSpec> hits;
                if (!failedHits.TryGetValue(pair.Defender, out hits))
                {
                    hits = new List<HitSpec>();
                    failedHits.Add(pair.Defender, hits);
                    hitSnapshots.Add(pair.Defender, pair.Defender.CaptureHitSnapshot());
                }
                hits.Add(pair.FailedDefenseHit);
                result.FailedPairs++;
            }
            // Existing protection is respected; newly-earned parry protection is not yet active.
            foreach (KeyValuePair<FighterState, List<HitSpec>> entry in failedHits)
                result.TotalFailedHpDamage += entry.Key.ApplyHitBatch(entry.Value, hitSnapshots[entry.Key]).HpDamage;
            foreach (ParryPair pair in successes)
            {
                result.SuccessfulPairs++;
                CombatConfig config = pair.Defender.Config;
                float pressure = config.ParryAttackerPressure + (pair.Precise ? config.PreciseParryExtraPressure : 0f);
                float previous;
                attackerPressure.TryGetValue(pair.Attacker, out previous);
                // PROTOTYPE_ONLY: mixed normal/precise outcomes use max, not sum; design remains OPEN.
                attackerPressure[pair.Attacker] = Math.Max(previous, pressure);
                defenderPressure.TryGetValue(pair.Defender, out previous);
                // PROTOTYPE_ONLY: one defender accumulates each distinct incoming attack's pressure.
                defenderPressure[pair.Defender] = previous + config.ParryDefenderPressure;
                protectedFighters.Add(pair.Attacker);
                protectedFighters.Add(pair.Defender);
            }
            foreach (KeyValuePair<FighterState, float> entry in attackerPressure)
            {
                entry.Key.ApplyParryPressure(entry.Value);
                result.AttackerPressureSettlements++;
            }
            foreach (KeyValuePair<FighterState, float> entry in defenderPressure)
                entry.Key.ApplyParryPressure(entry.Value);
            foreach (FighterState fighter in protectedFighters)
                fighter.GrantParryProtection(fighter.Config.ParryProtectionDuration);
            return result;
        }
    }
}
