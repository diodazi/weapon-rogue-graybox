using UnityEngine;

namespace WeaponGraybox
{
    [CreateAssetMenu(menuName = "Weapon Graybox/Prototype Settings")]
    public sealed class GrayboxSettings : ScriptableObject
    {
        [TextArea] public string Status = "PROTOTYPE_ONLY: these values are experiments, not approved balance. Dash slots 2 and recharge 1.5s follow the current baseline.";
        public CombatConfig Rules = new CombatConfig();
        public float HardPlayerHP = 100, PartyPlayerHP = 240, BossHP = 420;
        public float MoveSpeed = 5.5f, MoveAcceleration = 48, JumpImpulse = 8.5f;
        public float DashSpeed = 15, AngularGain = 30, AngularAcceleration = 18000;
        public float LightStartup = .18f, LightActive = .26f, LightRecovery = .28f;
        public float HeavyStartup = .36f, HeavyActive = .32f, HeavyRecovery = .40f;
        public float LightDamage = 12, HeavyDamage = 24, OrdinaryHitPosture = 3;
        public float HitStun = .13f, ParryRecovery = .23f, Knockback = 3.4f;
        public float CancelWindowStart = .02f, CancelWindowEnd = .10f;
        public float DeriveBuffer = .16f, ApproachImpulse = 1.1f;
        public float BossStartup = .70f, BossActive = .32f, BossRecovery = .65f;
        public float BossThinkDelay = .65f, BossMoveSpeed = 2.4f, BossDamage = 18;
        public float BossAttackRange = 2.8f, BossArmorDuration = 3.2f;
        public float NormalParryInputEarliest = .08f, NormalParryInputLate = .10f;
        public float PreciseParryInputLead = .24f, PreciseParryInputLate = .04f;
    }
}
