using System.Text.Json;
using WeaponGraybox;

var outcomes = new List<object>();
var failures = 0;
void Check(string name, Action probe)
{
    try { probe(); outcomes.Add(new { name, passed = true }); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failures++; outcomes.Add(new { name, passed = false, error = e.Message }); Console.WriteLine("FAIL " + name + ": " + e.Message); }
}
void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
void Near(float actual, float expected) { Require(Math.Abs(actual - expected) < .002f, actual + " != " + expected); }
FighterState Fighter(string id = "test", CombatConfig c = null) => new FighterState(c ?? new CombatConfig(), 200, id);
HitSpec Hit(float damage = 10) => new HitSpec(damage, 3);
ParryPair Pair(FighterState a, FighterState d, bool success, bool precise = false, long id = 1) =>
    new ParryPair { Attacker = a, Defender = d, DefenseSucceeded = success, Precise = precise, AttackInstanceId = id, FailedDefenseHit = Hit() };

Check("parry protects HP only, not posture or control", () =>
{
    var f = Fighter(); f.GrantParryProtection(.06f); var hit = f.ApplyHit(Hit());
    Near(hit.HpDamage, 0); Near(hit.PostureAdded, 3);
    Require(hit.AllowKnockback && hit.AllowInterruption, "HP-only protection blocked control");
    f.Tick(.07f); Require(f.ApplyHit(Hit()).HpDamage > 0, "parry HP protection leaked");
});
Check("normal dash has no invulnerability in its middle", () =>
{
    var f = Fighter(); Require(f.TryStartDash(1), "dash rejected"); f.Tick(.1f);
    var hit = f.ApplyHit(Hit());
    Require(hit.HpDamage > 0 && hit.PostureAdded > 0 && hit.AllowKnockback && hit.AllowInterruption, "normal dash protected hit");
});
Check("charged dash protects all four effects only in middle", () =>
{
    var f = Fighter(); f.StartCharge(); f.Tick(.7f); Require(f.TryStartDash(1), "charged dash rejected");
    Require(f.DashIsCharged && !f.HasDashProtection, "startup protected or not charged");
    f.Tick(.08f); var hit = f.ApplyHit(Hit());
    Near(hit.HpDamage, 0); Near(hit.PostureAdded, 0);
    Require(!hit.AllowKnockback && !hit.AllowInterruption, "middle control leak");
    f.Tick(.16f); Require(!f.HasDashProtection && f.DashActive, "recovery still protected");
    Require(f.ApplyHit(Hit()).HpDamage > 0, "recovery prevented HP damage");
});
Check("normal and charged dash consume the same two slots", () =>
{
    var f = Fighter(); f.TryStartDash(1); f.Tick(.31f);
    f.StartCharge(); f.Tick(.7f); f.TryStartDash(2);
    Require(f.DashCharges == 0 && f.DashIsCharged && !f.ChargeHeld, "resource split or charge retained");
    Require(!f.TryStartDash(3), "third concurrent dash accepted");
});
Check("duplicate dash command cannot consume or create another dash", () =>
{
    var f = Fighter(); f.TryStartDash(5); f.CancelDash();
    Require(!f.TryStartDash(5) && !f.TryStartDash(4), "duplicate command accepted"); Near(f.DashCharges, 1);
});
Check("duplicate tick ID cannot accelerate recharge", () =>
{
    var f = Fighter(); f.TryStartDash(1); f.Tick(.5f, 10); f.Tick(.5f, 10);
    Near(f.RechargeRemaining, 1); Near(f.DashCharges, 1);
    f.Tick(1, 11); Near(f.DashCharges, 2);
});
Check("serial recovery grants one slot per deadline and respects capacity", () =>
{
    var f = Fighter(); f.TryStartDash(1); f.Tick(.31f); f.TryStartDash(2);
    f.Tick(1.19f); Near(f.DashCharges, 1); f.Tick(1.5f); Near(f.DashCharges, 2);
    f.Tick(100); Near(f.DashCharges, 2);
});
Check("early dash exit removes only dash protection", () =>
{
    var f = Fighter(); f.StartCharge(); f.Tick(.7f); f.TryStartDash(1); f.Tick(.08f);
    f.GrantArmor(1); f.GrantParryProtection(.2f); f.CancelDash();
    Require(!f.HasDashProtection && f.IsArmored && f.ParryProtectionRemaining > 0, "wrong source removed");
});
Check("charge is cleared by release and non-charge action", () =>
{
    var f = Fighter(); f.StartCharge(); f.Tick(.7f); f.PerformUnchargedAction();
    Require(!f.IsFullyCharged, "movement left charge"); f.StartCharge(); f.Tick(.7f); f.ReleaseCharge();
    Require(!f.ChargeHeld, "release left charge");
});
Check("incomplete charge never grants dash enhancement", () =>
{
    var f = Fighter(); f.StartCharge(); f.Tick(.2f); f.TryStartDash(1);
    Require(!f.DashIsCharged && !f.ChargeHeld, "partial charge enhanced dash");
});
Check("break duration is fixed under repeated hits", () =>
{
    var f = Fighter(); f.SetPostureFromScript(100); f.Tick(.5f); var before = f.BreakRemaining;
    var hit = f.ApplyHit(Hit()); Near(f.BreakRemaining, before); Near(hit.PostureAdded, 0);
    Require(hit.HpDamage > 6.5f, "break did not increase damage");
    f.Tick(.61f); Require(!f.IsBroken && f.CanAct && f.IsPostureLocked, "break did not end on schedule");
});
Check("break exit restores control but locks zero posture, not HP", () =>
{
    var f = Fighter(); f.SetPostureFromScript(100); f.Tick(1.11f);
    Near(f.Posture, 0); Require(f.CanAct && f.PostBreakLockRemaining > 0, "bad recovery state");
    var hit = f.ApplyHit(Hit()); Near(hit.PostureAdded, 0); Require(hit.HpDamage > 0, "recovery gained HP invulnerability");
    f.Tick(.81f); Near(f.ApplyHit(Hit()).PostureAdded, 3);
});
Check("armor takes HP damage but locks posture and control", () =>
{
    var f = Fighter(); f.SetPostureFromScript(50); f.GrantArmor(3);
    var hit = f.ApplyHit(Hit()); Require(hit.HpDamage > 0, "armor blocked HP"); Near(f.Posture, 50);
    Require(!hit.AllowKnockback && !hit.AllowInterruption, "armor lost control immunity");
    f.Tick(2); Near(f.Posture, 50);
});
Check("special full posture waits until armor expires to break", () =>
{
    var f = Fighter(); f.GrantArmor(1); f.SetPostureFromScript(100);
    Require(!f.IsBroken, "premature break"); f.Tick(1);
    Require(f.IsBroken && f.BreakRemaining > 1, "deferred break absent or shortened");
});
Check("blade and hilt change HP only", () =>
{
    var a = Fighter("a"); var b = Fighter("b");
    var blade = a.ApplyHit(new HitSpec(10, 3, HitZone.Blade)); var hilt = b.ApplyHit(new HitSpec(10, 3, HitZone.Hilt));
    Require(hilt.HpDamage > 10 && blade.HpDamage < 10, "region damage wrong"); Near(blade.PostureAdded, hilt.PostureAdded);
});
Check("one attacker two successful defenders gives one attacker increment", () =>
{
    var a = Fighter("a"); var b = Fighter("b"); var c = Fighter("c");
    var r = ParryExchange.Resolve(new[] { Pair(a, b, true), Pair(a, c, true) });
    Require(r.SuccessfulPairs == 2 && r.AttackerPressureSettlements == 1, "pair result lost or duplicate pressure");
    Near(a.Posture, 24); Near(b.Posture, 8); Near(c.Posture, 8);
});
Check("new parry protection cannot swallow same-exchange failed defense", () =>
{
    var a = Fighter("a"); var b = Fighter("b"); var c = Fighter("c");
    var r = ParryExchange.Resolve(new[] { Pair(a, b, true), Pair(a, c, false) });
    Near(b.HP, 200); Require(c.HP < 200 && r.FailedPairs == 1, "failed defense was erased");
});
Check("two attacks one defense preserves one failed hit", () =>
{
    var a = Fighter("a"); var b = Fighter("b"); var d = Fighter("d");
    var r = ParryExchange.Resolve(new[] { Pair(a, d, true), Pair(b, d, false, false, 2) });
    Require(d.HP < 200 && r.SuccessfulPairs == 1 && r.FailedPairs == 1, "one success globally blocked both");
});
Check("simultaneous failed hits use pre-exchange break damage in either order", () =>
{
    float[] damage = new float[2];
    for (int order = 0; order < 2; order++)
    {
        var a = Fighter("a"); var b = Fighter("b"); var d = Fighter("d");
        d.SetPostureFromScript(99);
        var small = Pair(a, d, false, false, 1); small.FailedDefenseHit = Hit(10);
        var large = Pair(b, d, false, false, 2); large.FailedDefenseHit = Hit(100);
        var r = ParryExchange.Resolve(order == 0 ? new[] { small, large } : new[] { large, small });
        damage[order] = r.TotalFailedHpDamage;
        Near(r.TotalFailedHpDamage, 71.5f); Near(d.HP, 128.5f); Near(d.Posture, 100);
        Require(d.IsBroken && r.FailedPairs == 2, "simultaneous pressure did not commit a break");
        Near(d.BreakRemaining, d.Config.BreakDuration);
    }
    Near(damage[0], damage[1]);
});
Check("ordinary sequential hits still observe a break caused by the previous hit", () =>
{
    var d = Fighter("d"); d.SetPostureFromScript(99);
    Near(d.ApplyHit(Hit(10)).HpDamage, 6.5f);
    Require(d.IsBroken, "first ordinary hit did not break");
    Near(d.ApplyHit(Hit(100)).HpDamage, 97.5f);
    Near(d.HP, 96);
});
Check("simultaneous failed hits preserve existing protection and control states in either order", () =>
{
    foreach (string mode in new[] { "broken", "armor", "chargedDash", "hpOnly" })
    {
        for (int order = 0; order < 2; order++)
        {
            var a = Fighter("a"); var b = Fighter("b"); var d = Fighter("d");
            if (mode == "broken") d.SetPostureFromScript(100);
            if (mode == "armor") { d.SetPostureFromScript(50); d.GrantArmor(1); }
            if (mode == "chargedDash") { d.StartCharge(); d.Tick(.7f); d.TryStartDash(1); d.Tick(.08f); }
            if (mode == "hpOnly") { d.TryStartDash(1); d.GrantParryProtection(.1f); }
            var small = Pair(a, d, false, false, 1); small.FailedDefenseHit = Hit(10);
            var large = Pair(b, d, false, false, 2); large.FailedDefenseHit = Hit(100);
            var r = ParryExchange.Resolve(order == 0 ? new[] { small, large } : new[] { large, small });
            Near(r.TotalFailedHpDamage, mode == "broken" ? 107.25f : mode == "armor" ? 71.5f : 0);
            if (mode == "broken") { Near(d.Posture, 100); Near(d.BreakRemaining, d.Config.BreakDuration); }
            if (mode == "armor") { Near(d.Posture, 50); Require(d.IsArmored, "armor removed by batch"); }
            if (mode == "chargedDash") { Near(d.Posture, 0); Require(d.HasDashProtection, "middle protection interrupted"); }
            if (mode == "hpOnly") { Near(d.Posture, 6); Require(!d.DashActive, "HP-only protection blocked interruption"); }
        }
    }
});
Check("lethal simultaneous hits have order-independent posture and cleanup", () =>
{
    for (int order = 0; order < 2; order++)
    {
        var a = Fighter("a"); var b = Fighter("b"); var d = Fighter("d");
        d.SetPostureFromScript(50); d.StartCharge();
        var small = Pair(a, d, false, false, 1); small.FailedDefenseHit = Hit(1);
        var lethal = Pair(b, d, false, false, 2); lethal.FailedDefenseHit = Hit(400);
        var r = ParryExchange.Resolve(order == 0 ? new[] { small, lethal } : new[] { lethal, small });
        Near(r.TotalFailedHpDamage, 200); Near(d.HP, 0); Near(d.Posture, 50);
        Require(!d.IsAlive && !d.ChargeHeld && !d.DashActive, "lethal batch cleanup failed");
    }
});
Check("pre-existing HP protection still applies to failed defense", () =>
{
    var a = Fighter("a"); var b = Fighter("b"); var d = Fighter("d"); d.GrantParryProtection(.1f);
    ParryExchange.Resolve(new[] { Pair(a, d, true), Pair(b, d, false, false, 2) }); Near(d.HP, 200);
});
Check("duplicate pair in one exchange is settled once", () =>
{
    var a = Fighter("a"); var d = Fighter("d"); var p = Pair(a, d, true, true);
    var r = ParryExchange.Resolve(new[] { p, p }); Near(a.Posture, 40); Near(d.Posture, 8); Near(r.SuccessfulPairs, 1);
});
Check("armored incoming attack cannot be parried", () =>
{
    var a = Fighter("a"); var d = Fighter("d"); a.GrantArmor(1);
    var r = ParryExchange.Resolve(new[] { Pair(a, d, true) });
    Require(r.SuccessfulPairs == 0 && d.HP < 200, "armor attack was parried");
});
Check("lobby infinite HP does not grant posture immunity", () =>
{
    var f = Fighter(); f.InfiniteHealth = true; var hit = f.ApplyHit(new HitSpec(10000, 5));
    Near(f.HP, 200); Near(f.Posture, 5); Require(hit.AllowKnockback, "lobby became armor");
});
Check("large tick preserves break and recovery timeline", () =>
{
    var f = Fighter(); f.SetPostureFromScript(100); f.Tick(1.5f);
    Require(f.CanAct && f.PostBreakLockRemaining > .39f && f.PostBreakLockRemaining < .41f, "timer lost boundary");
});
Check("invalid numeric state is rejected", () =>
{
    bool rejected = false; try { Fighter("x", new CombatConfig { MaxPosture = float.NaN }); } catch (ArgumentOutOfRangeException) { rejected = true; }
    Require(rejected, "NaN accepted");
});

string output = args.Length > 0 ? args[0] : "rule-results.json";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
File.WriteAllText(output, JsonSerializer.Serialize(new { suite = "engine_independent_combat_invariants", passed = outcomes.Count - failures, failed = failures, checks = outcomes }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"RESULT {outcomes.Count - failures}/{outcomes.Count} passed");
return failures == 0 ? 0 : 1;
