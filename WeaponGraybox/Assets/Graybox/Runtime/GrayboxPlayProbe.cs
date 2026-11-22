#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WeaponGraybox.Editor
{
    // Opt-in development probes use the live input queue and real Physics2D contacts.
    // Teleports establish test conditions; they are not used as movement/attack evidence.
    public sealed class GrayboxPlayProbe : MonoBehaviour
    {
        [Serializable] public class Check { public string name, detail; public bool passed; }
        [Serializable] public class Report { public string unity; public int passed, failed; public List<Check> checks = new List<Check>(); }
        Report report = new Report();
        GrayboxGame game;
        void Capture(string path)
        {
            var camera = Camera.main;
            var target = new RenderTexture(1600, 900, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                Canvas.ForceUpdateCanvases(); camera.targetTexture = target; camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous; RenderTexture.active = active;
                target.Release(); Destroy(target); Destroy(pixels);
            }
        }
        void Record(string name, bool ok, string detail = "")
        {
            report.checks.Add(new Check { name = name, passed = ok, detail = detail });
            if (ok) report.passed++; else report.failed++;
            Debug.Log("[PlayProbe] " + (ok ? "PASS " : "FAIL ") + name + " " + detail);
        }
        IEnumerator Start()
        {
            game = GrayboxGame.Instance; report.unity = Application.unityVersion;
            game.AutomatedInput = true;
            IEnumerator routine = Run();
            while (true)
            {
                object next;
                try { if (!routine.MoveNext()) break; next = routine.Current; }
                catch (Exception e) { Record("probe completed without exception", false, e.ToString()); break; }
                yield return next;
            }
            game.ReturnToSelection(); game.AutomatedInput = false;
            Directory.CreateDirectory("Evidence");
            File.WriteAllText(Application.isEditor ? "Evidence/playmode-results.json" : "Evidence/standalone-results.json", JsonUtility.ToJson(report, true));
            Debug.Log("[PlayProbe] COMPLETE " + report.passed + " passed, " + report.failed + " failed");
            if (!Application.isEditor) Application.Quit(report.failed == 0 ? 0 : 1);
            Destroy(this);
        }
        IEnumerator Run()
        {
            game.ReturnToSelection();
            yield return new WaitForSeconds(.3f);
            if (!Application.isEditor)
            {
                Directory.CreateDirectory("Evidence");
                Capture("Evidence/standalone-selection.png");
                yield return new WaitForSeconds(.2f);
            }
            var labels = FindObjectsByType<TMPro.TextMeshProUGUI>();
            Record("Chinese HUD created", labels.Length >= 18 && labels[0].font.HasCharacter('蓄', true, true));
            for (int i = 0; i < 3; i++)
            {
                game.SelectWeapon(i); game.ConfirmSelection(); yield return new WaitForSeconds(.12f);
                Record("weapon selection " + i, game.Player.DisplayName == new[] { "刀", "剑", "矛" }[i]);
                game.ReturnToSelection(); yield return null;
            }
            game.SelectWeapon(1); game.ConfirmSelection(); yield return new WaitForSeconds(.8f);
            Record("lobby infinite HP for both actors", game.Player.State.InfiniteHealth && game.Boss.State.InfiniteHealth);
            game.ConfirmStart(); Record("cannot start outside ready zone", game.Phase == GamePhase.Lobby);
            var player = game.Player;
            float x = player.Body.position.x;
            player.MoveInput = 1; yield return new WaitForSeconds(.4f); player.MoveInput = 0;
            Record("movement uses physics", player.Body.position.x > x + .7f, "dx=" + (player.Body.position.x - x));
            yield return new WaitForSeconds(.2f);
            float y = player.Body.position.y; player.PressJump(); yield return new WaitForSeconds(.17f);
            Record("jump leaves ground", player.Body.position.y > y + .4f, "dy=" + (player.Body.position.y - y));
            yield return new WaitForSeconds(.8f);
            player.PressCharge(); player.ReleaseCharge(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Record("quick S tap does not leave charging held", !player.State.ChargeHeld);
            player.PressCharge(); yield return new WaitForSeconds(.2f); player.MoveInput = -1;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); player.MoveInput = 0;
            Record("moving cancels incomplete charge", !player.State.ChargeHeld);
            yield return new WaitForSeconds(.2f);
            x = player.Body.position.x; player.PressCharge(); yield return new WaitForSeconds(.72f);
            Record("stationary hold reaches full charge", player.State.IsFullyCharged && Mathf.Abs(player.Body.position.x - x) < .2f);
            x = player.Body.position.x; player.PressDash(); yield return new WaitForSeconds(.10f);
            Record("charged dash middle has protection and consumes one slot", player.State.HasDashProtection && player.State.DashCharges == 1 && !player.State.ChargeHeld);
            yield return new WaitForSeconds(.24f);
            Record("charged dash moves and protection expires", Mathf.Abs(player.Body.position.x - x) > 1 && !player.State.HasDashProtection);
            player.PressDash(); yield return new WaitForSeconds(.10f);
            Record("normal dash shares slots and has no protection", player.State.DashCharges == 0 && !player.State.HasDashProtection && player.State.DashActive);
            yield return new WaitForSeconds(1.6f);
            Record("serial recharge awards one slot", player.State.DashCharges == 1);
            player.Body.position = new Vector2(11, -2.5f); player.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate();
            Record("right-hand ready zone is detected", game.PlayerReady);
            game.ConfirmStart(); game.ToggleBossPause(); yield return new WaitForSeconds(.6f);
            Record("confirmed ready zone enters finite-HP battle", game.Phase == GamePhase.Combat && !game.Player.State.InfiniteHealth && !game.Boss.State.InfiniteHealth);
            if (!Application.isEditor)
            {
                Capture("Evidence/standalone-combat.png");
                yield return new WaitForSeconds(.2f);
            }
            // Both support bodies remain separated; actual rotating blade reaches the opponent.
            player = game.Player; var boss = game.Boss;
            player.Body.position = new Vector2(-1.1f, -2.54f); boss.Body.position = new Vector2(1.1f, -2.24f);
            player.Body.linearVelocity = boss.Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(.15f);
            float hp = boss.State.HP;
            player.PressLight(); yield return new WaitForSeconds(.55f);
            float loss = hp - boss.State.HP;
            Record("rotating blade produces real hit once", loss > 0 && loss <= game.Settings.LightDamage * game.Settings.Rules.HiltDamageMultiplier + .01f, "HP loss=" + loss);
            float hitHp = boss.State.HP; yield return new WaitForSeconds(.3f);
            Record("lingering contact does not repeat same attack damage", Mathf.Approximately(boss.State.HP, hitHp));
            // Fixed simulation timing avoids wall-clock coroutine delays changing the input window.
            foreach (float delay in new[] { .55f, .75f, .55f })
            {
                bool separated = delay == .55f && report.checks.Exists(c => c.name == "real blade precise parry");
                game.RestartLobby(); yield return new WaitForSeconds(.6f);
                player = game.Player; boss = game.Boss;
                player.Body.position = new Vector2(separated ? -5 : -1.1f, -2.54f);
                boss.Body.position = new Vector2(1.1f, -2.24f);
                player.Body.linearVelocity = boss.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms(); yield return new WaitForSeconds(.15f);
                boss.StartAttack(false); double started = boss.State.SimulationTime;
                while (boss.State.SimulationTime - started < delay) yield return new WaitForFixedUpdate();
                player.PressLight();
                while (boss.State.SimulationTime - started < 1.2) yield return new WaitForFixedUpdate();
                string expected = delay < .7f ? "精确弹反" : "弹反";
                Record(separated ? "timing without contact does not parry" : delay < .7f ? "real blade precise parry" : "real blade normal parry",
                    separated ? !game.FeedbackText.Contains("弹反") : game.FeedbackText.StartsWith(expected) && (delay < .7f || !game.FeedbackText.StartsWith("精确")), game.FeedbackText);
            }
            game.ReturnToSelection(); game.SetPartyMode(true); game.ConfirmSelection();
            Record("party HP preset selected", Mathf.Approximately(game.Player.State.MaxHP, game.Settings.PartyPlayerHP));
            game.Player.Body.position = new Vector2(11, -2.5f); game.ConfirmStart(); game.ToggleBossPause();
            game.Boss.State.ApplyHit(new HitSpec(9999, 0)); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Record("boss death reaches victory result", game.Phase == GamePhase.Victory);
            game.RestartLobby(); game.Player.Body.position = new Vector2(11, -2.5f); game.ConfirmStart(); game.ToggleBossPause();
            game.Player.State.ApplyHit(new HitSpec(9999, 0)); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Record("player death reaches defeat result", game.Phase == GamePhase.Defeat);
        }
    }
}
#endif
