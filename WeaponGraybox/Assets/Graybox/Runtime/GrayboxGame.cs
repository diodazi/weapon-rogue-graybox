using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WeaponGraybox
{
    public enum GamePhase { Selection, Lobby, Combat, Victory, Defeat }

    public sealed class GrayboxGame : MonoBehaviour
    {
        public static GrayboxGame Instance { get; private set; }
        public GrayboxSettings Settings;
        public GamePhase Phase { get; private set; }
        public WeaponActor Player { get; private set; }
        public WeaponActor Boss { get; private set; }
        public bool PartyMode { get; private set; }
        public int SelectedWeapon { get; private set; }
        public bool BossPaused { get; private set; }
        public bool ShowHitboxes { get; private set; } = true;
        public bool AutomatedInput;
        public string FeedbackText { get; private set; } = "选择武器，进入大厅练习。";
        public bool PlayerReady { get { return Phase == GamePhase.Lobby && Player && Player.Body.position.x >= 10 && Player.Body.position.y < -1; } }
        public string HudText { get { return Player ? Status(Player) : "刀 / 剑 / 矛\n首轮：键盘单人灰盒"; } }
        public string BossText { get { return Boss ? Status(Boss) : "大厅最右侧为准备区域\n本次仅验证战斗，成长卡与多人尚未接入"; } }
        long step, attackSerial;
        Transform world;
        GameObject readyZone;
        Camera view;

        void Awake()
        {
            Instance = this;
            if (!Settings) Settings = ScriptableObject.CreateInstance<GrayboxSettings>();
            Time.fixedDeltaTime = 1f / 100f;
            Application.targetFrameRate = 120;
            CreateWorld(); Phase = GamePhase.Selection;
            gameObject.AddComponent<GrayboxHud>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--graybox-self-test") >= 0)
                gameObject.AddComponent<WeaponGraybox.Editor.GrayboxPlayProbe>();
#endif
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
        public long NextAttackId() { return ++attackSerial; }
        public void Report(string message) { FeedbackText = message; Debug.Log("[Graybox] " + message); }
        public void SelectWeapon(int index) { if (Phase == GamePhase.Selection) SelectedWeapon = Mathf.Clamp(index, 0, 2); }
        public void SetPartyMode(bool party) { if (Phase == GamePhase.Selection) PartyMode = party; }
        public void ConfirmSelection() { if (Phase == GamePhase.Selection) RestartLobby(); }
        public void RestartLobby()
        {
            RemoveFighters(); Phase = GamePhase.Lobby; BossPaused = false;
            Player = CreateFighter("Player", SelectedWeapon, false, new Vector2(-8, -2), true);
            Boss = CreateFighter("PracticeWeapon", 1, true, new Vector2(-1, -1.8f), true);
            Boss.TrainingDummy = true; readyZone.SetActive(true);
            Report("大厅：双方无限血量。试打后前往最右侧准备区。");
        }
        public void ReturnToSelection()
        {
            RemoveFighters(); Phase = GamePhase.Selection; readyZone.SetActive(false);
            Report("选择刀、剑或矛。灰盒尺寸与数值均可调整。");
        }
        public void ConfirmStart()
        {
            if (!PlayerReady) return;
            RemoveFighters(); Phase = GamePhase.Combat; readyZone.SetActive(false); BossPaused = false;
            Player = CreateFighter("Player", SelectedWeapon, false, new Vector2(-5, -2), false);
            Boss = CreateFighter("TestBoss", 1, true, new Vector2(4, -1.8f), false);
            Report("测试 Boss：观察前摇后攻击碰刃；黄色霸体攻击需躲避。");
        }
        public void ToggleBossPause()
        {
            if (!Boss || Phase != GamePhase.Combat) return;
            BossPaused = !BossPaused; Boss.MoveInput = 0;
            Report(BossPaused ? "已暂停 Boss 决策；已开始的攻击会正常结束。" : "Boss 决策继续。");
        }
        public void ToggleHitboxes() { ShowHitboxes = !ShowHitboxes; }
        void RemoveFighters()
        {
            if (Player) { Player.State.CancelDash(); Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            if (Boss) { Boss.State.CancelDash(); Boss.gameObject.SetActive(false); Destroy(Boss.gameObject); }
            Player = Boss = null;
        }
        WeaponActor CreateFighter(string name, int weapon, bool boss, Vector2 position, bool infinite)
        {
            var actor = new GameObject(name).AddComponent<WeaponActor>(); actor.transform.position = position;
            actor.Initialize(this, Settings, weapon, boss, boss ? Settings.BossHP : PartyMode ? Settings.PartyPlayerHP : Settings.HardPlayerHP, infinite);
            return actor;
        }
        void Update()
        {
            if (AutomatedInput) return;
            var k = Keyboard.current; if (k == null) return;
            if (Phase == GamePhase.Selection)
            {
                if (k.digit1Key.wasPressedThisFrame) SelectWeapon(0);
                if (k.digit2Key.wasPressedThisFrame) SelectWeapon(1);
                if (k.digit3Key.wasPressedThisFrame) SelectWeapon(2);
                if (k.enterKey.wasPressedThisFrame) ConfirmSelection();
                return;
            }
            if (k.escapeKey.wasPressedThisFrame) { ReturnToSelection(); return; }
            if (!Player || (Phase != GamePhase.Lobby && Phase != GamePhase.Combat)) return;
            if (PlayerReady && k.enterKey.wasPressedThisFrame) { ConfirmStart(); return; }
            Player.MoveInput = (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
            if (k.sKey.wasPressedThisFrame) Player.PressCharge();
            if (k.sKey.wasReleasedThisFrame) Player.ReleaseCharge();
            if (k.jKey.wasPressedThisFrame) Player.PressLight();
            if (k.uKey.wasPressedThisFrame) Player.PressHeavy();
            if (k.kKey.wasPressedThisFrame) Player.PressJump();
            if (k.lKey.wasPressedThisFrame) Player.PressDash();
        }
        void FixedUpdate()
        {
            if (!Player || (Phase != GamePhase.Lobby && Phase != GamePhase.Combat)) return;
            step++;
            Player.Step(Time.fixedDeltaTime, step);
            if (Boss)
            {
                Boss.SuppressBrain = BossPaused;
                if (BossPaused) Boss.MoveInput = 0;
                Boss.Step(Time.fixedDeltaTime, step);
                ResolveContact(Player, Boss);
            }
            if (Phase == GamePhase.Combat)
            {
                // PROTOTYPE_ONLY: simultaneous defeat is a failed run until the design settles it.
                if (!Player.State.IsAlive) Finish(false);
                else if (Boss && !Boss.State.IsAlive) Finish(true);
            }
        }
        void Finish(bool victory)
        {
            Phase = victory ? GamePhase.Victory : GamePhase.Defeat;
            Player.ClearInput(); Player.State.CancelDash(); Boss.State.CancelDash();
            Player.Body.linearVelocity = Boss.Body.linearVelocity = Vector2.zero;
            Report(victory ? "击败测试 Boss。首轮未接入奖励卡；可回大厅继续试打。" : "本次测试结束。回大厅重新练习。");
        }
        static bool Touches(Collider2D a, Collider2D b)
        {
            return a && b && a.enabled && b.enabled && Physics2D.Distance(a, b).isOverlapped;
        }
        bool CanDefend(WeaponActor attacker, WeaponActor defender, AttackRuntime a, AttackRuntime d, out bool precise)
        {
            precise = false;
            if (a == null || d == null || !a.ActiveNow(attacker.State) || attacker.State.IsArmored || !defender.State.CanAct) return false;
            float inputAge = (float)(d.Started - a.Started), contactAge = a.Age(attacker.State);
            if (inputAge < Settings.NormalParryInputEarliest || inputAge > a.Startup + Settings.NormalParryInputLate) return false;
            if (d.Age(defender.State) > d.Startup + d.Active) return false;
            precise = inputAge >= a.Startup - Settings.PreciseParryInputLead &&
                inputAge <= a.Startup + Settings.PreciseParryInputLate &&
                contactAge <= a.Startup + a.Active * .85f;
            return true;
        }
        void ResolveContact(WeaponActor first, WeaponActor second)
        {
            AttackRuntime a = first.Attack, b = second.Attack;
            if (a == null && b == null) return;
            // Both actual blade colliders must overlap; button timing alone never succeeds.
            if (a != null && b != null && Touches(first.Blade, second.Blade))
            {
                WeaponActor attacker = a.Started <= b.Started ? first : second;
                WeaponActor defender = attacker == first ? second : first;
                AttackRuntime attack = attacker.Attack, defense = defender.Attack;
                if (!attack.HitTargets.Contains(defender.State.Id) && CanDefend(attacker, defender, attack, defense, out bool precise))
                {
                    attack.HitTargets.Add(defender.State.Id); defense.HitTargets.Add(attacker.State.Id);
                    ParryExchange.Resolve(new List<ParryPair> { new ParryPair {
                        Attacker = attacker.State, Defender = defender.State, AttackInstanceId = attack.Id,
                        DefenseSucceeded = true, Precise = precise
                    } });
                    attacker.Parried(); defender.Parried();
                    Report((precise ? "精确弹反" : "弹反") + " · 真实碰刃 · 攻击方架势增加");
                    return;
                }
            }
            // Snapshot eligible contacts before applying either hit so callback order cannot erase a trade.
            HitZone? toSecond = HitZoneFor(first, second, a);
            HitZone? toFirst = HitZoneFor(second, first, b);
            if (toSecond.HasValue) ApplyContact(first, second, a, toSecond.Value);
            if (toFirst.HasValue) ApplyContact(second, first, b, toFirst.Value);
        }
        HitZone? HitZoneFor(WeaponActor source, WeaponActor target, AttackRuntime attack)
        {
            if (attack == null || !attack.ActiveNow(source.State) || attack.HitTargets.Contains(target.State.Id)) return null;
            // PROTOTYPE_ONLY: hilt priority when one swing overlaps both target regions.
            if (Touches(source.Blade, target.Hilt)) return HitZone.Hilt;
            if (Touches(source.Blade, target.Blade)) return HitZone.Blade;
            return null;
        }
        void ApplyContact(WeaponActor source, WeaponActor target, AttackRuntime attack, HitZone zone)
        {
            attack.HitTargets.Add(target.State.Id);
            HitResult result = target.State.ApplyHit(new HitSpec(attack.Damage, Settings.OrdinaryHitPosture, zone));
            float direction = Mathf.Sign(target.Body.position.x - source.Body.position.x);
            target.ReactToHit(result, direction == 0 ? source.Facing : direction);
            Report(source.DisplayName + " 命中 " + target.DisplayName + " 的" + (zone == HitZone.Hilt ? "柄" : "刃") +
                " · HP -" + result.HpDamage.ToString("0.0") + " / 架势 +" + result.PostureAdded.ToString("0.0") +
                (result.FullDashProtection ? " · 残影保护" : result.BrokePosture ? " · 破势" : ""));
        }
        string Status(WeaponActor actor)
        {
            FighterState s = actor.State;
            string lockText = s.IsBroken ? "破势 " + s.BreakRemaining.ToString("0.00") + "s" :
                s.IsArmored ? "霸体 " + s.ArmorRemaining.ToString("0.0") + "s" :
                s.PostBreakLockRemaining > 0 ? "架势锁定 " + s.PostBreakLockRemaining.ToString("0.00") + "s" : "架势可变化";
            return actor.DisplayName + (actor.TrainingDummy ? "（练习）" : "") + "   " +
                (s.InfiniteHealth ? "HP ∞" : "HP " + s.HP.ToString("0") + " / " + s.MaxHP.ToString("0")) +
                "\n架势 " + s.Posture.ToString("0") + " / " + s.Config.MaxPosture + "   " + lockText +
                "\n蓄力 " + (s.ChargeProgress * 100).ToString("0") + "%   冲刺 " + s.DashCharges + " / 2" +
                (s.DashCharges < 2 ? "   回充 " + s.RechargeRemaining.ToString("0.0") + "s" : "") +
                "\n" + actor.ActionLabel;
        }
        void CreateWorld()
        {
            world = new GameObject("GrayboxArena").transform;
            view = Camera.main;
            if (!view) { var go = new GameObject("Main Camera"); go.tag = "MainCamera"; view = go.AddComponent<Camera>(); go.AddComponent<AudioListener>(); }
            view.orthographic = true; view.orthographicSize = 8;
            view.transform.position = new Vector3(0, .7f, -10);
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.075f, .09f, .12f);
            for (int x = -14; x <= 14; x++)
                GrayboxShapes.Box("GridX", world, new Vector2(x, .5f), new Vector2(.012f, 12), new Color(.12f, .15f, .19f), -10);
            for (int y = -3; y <= 7; y++)
                GrayboxShapes.Box("GridY", world, new Vector2(0, y), new Vector2(28, .012f), new Color(.12f, .15f, .19f), -10);
            Solid("Floor", new Vector2(0, -4), new Vector2(28, 1));
            Solid("LeftWall", new Vector2(-14, 0), new Vector2(1, 10));
            Solid("RightWall", new Vector2(14, 0), new Vector2(1, 10));
            readyZone = GrayboxShapes.Box("RightReadyZone", world, new Vector2(11.5f, -1.8f), new Vector2(3, 3.4f), new Color(.15f, .37f, .35f, .5f), -5);
            readyZone.SetActive(false);
        }
        void Solid(string name, Vector2 position, Vector2 size)
        {
            var obj = GrayboxShapes.Box(name, world, position, size, new Color(.25f, .28f, .32f), -2);
            obj.layer = 8; obj.AddComponent<BoxCollider2D>();
        }
        public void Trail(WeaponActor actor)
        {
            var ghost = GrayboxShapes.Box("Afterimage", world, actor.Body.position, new Vector2(1.8f, .18f), new Color(.4f, .9f, 1, .2f), 1);
            ghost.transform.rotation = actor.transform.rotation; Destroy(ghost, .12f);
        }
    }
}
