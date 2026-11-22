using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace WeaponGraybox
{
    public sealed class GrayboxHud : MonoBehaviour
    {
        TMP_FontAsset font;
        RectTransform root, selection, ready, result;
        TextMeshProUGUI playerText, bossText, feedback, resultText, pauseText, phaseText;
        UnityEngine.UI.Image[] weaponButtons, modeButtons;
        GrayboxGame Game { get { return GrayboxGame.Instance; } }
        readonly Color panel = new Color(.08f, .11f, .15f, .96f);
        readonly Color normal = new Color(.19f, .24f, .29f);
        readonly Color selected = new Color(.17f, .47f, .5f);

        void Start()
        {
            // Windows graybox uses the locally installed CJK font; no reference art is required.
            font = TMP_FontAsset.CreateFontAsset("Microsoft YaHei", "Regular", 48);
            if (!font) font = TMP_FontAsset.CreateFontAsset("SimHei", "Regular", 48);
            if (!font) font = TMP_FontAsset.CreateFontAsset("Arial", "Regular", 48);
            if (!font) font = TMP_Settings.defaultFontAsset;
            if (font) font.isMultiAtlasTexturesEnabled = true;
            var canvasObject = new GameObject("GrayboxHUD", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            root = canvasObject.GetComponent<RectTransform>();
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1; canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            if (!FindAnyObjectByType<EventSystem>())
            {
                var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventObject.transform.SetParent(transform, false);
                eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var topLeft = Panel("PlayerStatus", root, new Vector2(0, 1), new Vector2(28, -28), new Vector2(490, 170), panel);
            playerText = Label("PlayerText", topLeft, new Vector2(20, -15), new Vector2(455, 145), 21, "");
            var topRight = Panel("BossStatus", root, new Vector2(1, 1), new Vector2(-28, -28), new Vector2(490, 170), panel, new Vector2(1, 1));
            bossText = Label("BossText", topRight, new Vector2(20, -15), new Vector2(455, 145), 21, "");
            phaseText = Label("Phase", root, new Vector2(0, -30), new Vector2(520, 35), 20, "战斗灰盒");
            phaseText.rectTransform.anchorMin = phaseText.rectTransform.anchorMax = new Vector2(.5f, 1);
            phaseText.rectTransform.pivot = new Vector2(.5f, 1); phaseText.alignment = TextAlignmentOptions.Center;
            var toolbar = Panel("Tools", root, new Vector2(.5f, 1), new Vector2(0, -80), new Vector2(490, 50), Color.clear, new Vector2(.5f, 1));
            var p = Button("PauseBoss", toolbar, new Vector2(0, 0), new Vector2(145, 42), "暂停 Boss", () => Game.ToggleBossPause());
            pauseText = p.GetComponentInChildren<TextMeshProUGUI>();
            Button("Hitboxes", toolbar, new Vector2(160, 0), new Vector2(145, 42), "碰撞区域", () => Game.ToggleHitboxes());
            Button("Selection", toolbar, new Vector2(320, 0), new Vector2(145, 42), "重新选角", () => Game.ReturnToSelection());
            var bottom = Panel("Readout", root, new Vector2(.5f, 0), new Vector2(0, 24), new Vector2(1544, 132), panel, new Vector2(.5f, 0));
            Label("Controls", bottom, new Vector2(20, -12), new Vector2(1500, 30), 20, "A / D 移动    S 蓄力    J 轻攻击    K 跳跃    L 冲刺    U 重攻击    Esc 重新选角");
            feedback = Label("Feedback", bottom, new Vector2(20, -48), new Vector2(1500, 44), 20, "");
            Label("Scope", bottom, new Vector2(20, -98), new Vector2(1500, 25), 15, "青色轮廓：刃   粉色轮廓：柄   黄色：霸体   红色：破势   |   原型参数，尚未完成平衡；手柄、多人和成长卡后续接入。");
            CreateSelection();
            ready = Panel("ReadyDialog", root, new Vector2(.5f, .5f), Vector2.zero, new Vector2(590, 240), panel, new Vector2(.5f, .5f));
            Label("ReadyTitle", ready, new Vector2(28, -24), new Vector2(535, 45), 30, "已抵达准备区域");
            Label("ReadyInfo", ready, new Vector2(28, -80), new Vector2(535, 55), 21, "确认后进入测试 Boss 房间。\n按 Enter 或点击确认；离开区域可继续练习。");
            Button("ConfirmStart", ready, new Vector2(28, -165), new Vector2(535, 48), "确认，开始 Boss 战", () => Game.ConfirmStart());
            result = Panel("ResultDialog", root, new Vector2(.5f, .5f), Vector2.zero, new Vector2(610, 300), panel, new Vector2(.5f, .5f));
            resultText = Label("ResultText", result, new Vector2(28, -25), new Vector2(550, 170), 25, "");
            Button("ReturnLobby", result, new Vector2(28, -220), new Vector2(550, 50), "返回大厅，再试一次", () => Game.RestartLobby());
        }
        void CreateSelection()
        {
            selection = Panel("SelectionPanel", root, new Vector2(.5f, .5f), new Vector2(0, -10), new Vector2(720, 465), panel, new Vector2(.5f, .5f));
            Label("Title", selection, new Vector2(30, -24), new Vector2(660, 50), 34, "武器本体 · 战斗灰盒");
            Label("Subtitle", selection, new Vector2(30, -85), new Vector2(660, 58), 21, "先选武器，在大厅练习。\n三种形体用于验证刃柄比例，不代表最终美术。");
            weaponButtons = new UnityEngine.UI.Image[3];
            string[] names = { "1  刀", "2  剑", "3  矛" };
            for (int i = 0; i < 3; i++)
            {
                int choice = i;
                weaponButtons[i] = Button("Weapon" + i, selection, new Vector2(30 + i * 225, -165), new Vector2(210, 62), names[i], () => Game.SelectWeapon(choice));
            }
            modeButtons = new UnityEngine.UI.Image[2];
            modeButtons[0] = Button("HardMode", selection, new Vector2(30, -250), new Vector2(325, 50), "高难度 · 较少血量", () => Game.SetPartyMode(false));
            modeButtons[1] = Button("PartyMode", selection, new Vector2(370, -250), new Vector2(320, 50), "派对 · 较多血量", () => Game.SetPartyMode(true));
            Label("PrototypeValues", selection, new Vector2(30, -315), new Vector2(660, 34), 17, "两种模式当前仅比较玩家血量；数值为灰盒试验值。");
            Button("EnterLobby", selection, new Vector2(30, -375), new Vector2(660, 56), "进入大厅  /  Enter", () => Game.ConfirmSelection());
        }
        void Update()
        {
            if (!Game || !selection) return;
            selection.gameObject.SetActive(Game.Phase == GamePhase.Selection);
            ready.gameObject.SetActive(Game.PlayerReady);
            bool ended = Game.Phase == GamePhase.Victory || Game.Phase == GamePhase.Defeat;
            result.gameObject.SetActive(ended);
            resultText.text = Game.Phase == GamePhase.Victory ? "击败测试 Boss\n\n首轮战斗验证完成。\n本轮尚未接入奖励卡与技能树。" : "本次测试结束\n\n可回大厅重新练习，\n观察碰刃时机和冲刺保护窗口。";
            playerText.text = Game.HudText; bossText.text = Game.BossText; feedback.text = Game.FeedbackText;
            pauseText.text = Game.BossPaused ? "继续 Boss" : "暂停 Boss";
            phaseText.text = Game.Phase == GamePhase.Lobby ? "大厅 · 无限血量 · 最右侧准备 →" : Game.Phase == GamePhase.Combat ? "测试 Boss · 键盘单人" : "战斗灰盒 · 第一轮";
            for (int i = 0; i < weaponButtons.Length; i++) weaponButtons[i].color = Game.SelectedWeapon == i ? selected : normal;
            modeButtons[0].color = !Game.PartyMode ? selected : normal; modeButtons[1].color = Game.PartyMode ? selected : normal;
        }
        RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, Vector2? pivot = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot ?? new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            obj.GetComponent<UnityEngine.UI.Image>().color = color;
            return rect;
        }
        TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, string text)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = obj.GetComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = fontSize; label.text = text;
            label.color = new Color(.88f, .93f, .95f); label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Truncate;
            return label;
        }
        UnityEngine.UI.Image Button(string name, Transform parent, Vector2 position, Vector2 size, string caption, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(name, parent, new Vector2(0, 1), position, size, normal);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            var nav = button.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = nav;
            button.onClick.AddListener(action);
            var text = Label("Caption", rect, Vector2.zero, size, 22, caption); text.alignment = TextAlignmentOptions.Center;
            return rect.GetComponent<UnityEngine.UI.Image>();
        }
    }
}
