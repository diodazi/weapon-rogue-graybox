#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
namespace WeaponGraybox.Editor
{
    public sealed class GrayboxParryProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            var g = GrayboxGame.Instance; g.AutomatedInput = true;
            var csv = new StringBuilder("delay,t,pX,pY,pAngle,bX,bY,bAngle,bladeDistance,pAttack,bAttack,feedback\n");
            foreach (float delay in new[] { .35f, .45f, .55f, .65f, .75f })
            {
                g.ReturnToSelection(); g.SelectWeapon(1); g.ConfirmSelection();
                yield return new WaitForSeconds(.6f);
                var p = g.Player; var b = g.Boss;
                p.Body.position = new Vector2(-1.1f,-2.54f); b.Body.position = new Vector2(1.1f,-2.24f);
                p.Body.linearVelocity = b.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms(); yield return new WaitForSeconds(.15f);
                b.StartAttack(false); float begin = (float)b.State.SimulationTime; bool pressed = false;
                for (int i = 0; i < 130; i++)
                {
                    yield return new WaitForFixedUpdate();
                    float t = (float)b.State.SimulationTime - begin;
                    if (!pressed && t >= delay) { p.PressLight(); pressed = true; }
                    csv.AppendLine(string.Join(",", delay,t,p.Body.position.x,p.Body.position.y,p.Body.rotation,b.Body.position.x,b.Body.position.y,b.Body.rotation,Physics2D.Distance(p.Blade,b.Blade).distance,p.Attack!=null,b.Attack!=null,g.FeedbackText));
                }
            }
            File.WriteAllText("Evidence/parry-trajectories.csv", csv.ToString());
            g.ReturnToSelection(); g.AutomatedInput = false; Destroy(this);
        }
    }
}
#endif
