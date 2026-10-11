using UnityEngine;
using UnityEngine.UI;

namespace FogboundMaze
{
    public sealed partial class GameHud
    {
        private GameObject titlePanel;
        private GameObject levelPanel;
        private Text campaignSummary;
        private readonly Button[] levelButtons = new Button[10];
        private readonly Text[] levelStates = new Text[10];

        private void BuildCampaignPanels()
        {
            titlePanel = PanelObject("Title Screen", transform, new Color(0.02f, 0.035f, 0.04f, 0.54f));
            SetRect(titlePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var title = Label("Game Title", titlePanel.transform, "MAZE\nESCAPE", 100, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.65f, 0.87f));
            var subtitle = Label("Campaign", titlePanel.transform, "SURVIVAL CAMPAIGN", 27, TextAnchor.MiddleLeft, Accent);
            SetRect(subtitle.rectTransform, new Vector2(0.08f, 0.45f), new Vector2(0.50f, 0.51f));
            var start = Button("Start", titlePanel.transform, "START", Accent);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0.08f, 0.32f), new Vector2(0.32f, 0.41f));
            start.onClick.AddListener(() => GameDirector.Instance.ShowLevelSelection());
            var guide = Button("Guide", titlePanel.transform, "FIELD GUIDE", new Color(.19f,.28f,.30f));
            SetRect(guide.GetComponent<RectTransform>(), new Vector2(.08f,.21f), new Vector2(.32f,.30f));
            guide.onClick.AddListener(() => GameDirector.Instance.ShowGuide());
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                var quit = Button("Quit", titlePanel.transform, "QUIT", new Color(0.14f, 0.19f, 0.20f));
                SetRect(quit.GetComponent<RectTransform>(), new Vector2(0.08f, 0.10f), new Vector2(0.32f, 0.19f));
                quit.onClick.AddListener(() => GameDirector.Instance.QuitGame());
            }
            var version = Label("Version", titlePanel.transform, "v" + ReleaseVersion.Display, 20, TextAnchor.MiddleRight, new Color(0.70f, 0.77f, 0.76f));
            SetRect(version.rectTransform, new Vector2(0.7f, 0.025f), new Vector2(0.95f, 0.075f));

            levelPanel = PanelObject("Level Select", transform, new Color(0.025f, 0.04f, 0.045f, 0.94f));
            SetRect(levelPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var heading = Label("Title", levelPanel.transform, "SELECT LEVEL", 52, TextAnchor.MiddleLeft);
            SetRect(heading.rectTransform, new Vector2(0.08f, 0.80f), new Vector2(0.60f, 0.92f));
            campaignSummary = Label("Progress", levelPanel.transform, string.Empty, 25, TextAnchor.MiddleRight, Accent);
            SetRect(campaignSummary.rectTransform, new Vector2(0.6f, 0.81f), new Vector2(0.92f, 0.90f));
            var names = new[] { "ENTRY", "CROSSROADS", "PURSUIT", "DENSE FOG", "ELITES", "NIGHTFALL", "MIASMA", "DEEP ZONE", "LOCKDOWN", "LAST EXIT" };
            for (var i = 0; i < 10; i++)
            {
                var number = i + 1;
                var button = Button($"Level {number:00}", levelPanel.transform, string.Empty, Panel);
                var x = 0.08f + (i % 5) * 0.17f;
                var y = 0.51f - (i / 5) * 0.25f;
                SetRect(button.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(x + 0.155f, y + 0.21f));
                var label = button.transform.Find("Label").GetComponent<Text>();
                label.text = $"{number:00}\n{names[i]}";
                label.fontSize = label.resizeTextMaxSize = 30;
                SetRect(label.rectTransform, new Vector2(0.05f, 0.29f), new Vector2(0.95f, 0.97f));
                var state = Label("State", button.transform, string.Empty, 20, TextAnchor.MiddleCenter);
                SetRect(state.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.27f));
                levelButtons[i] = button;
                levelStates[i] = state;
                button.onClick.AddListener(() => GameDirector.Instance.SelectLevel(number));
            }
            var back = Button("Back", levelPanel.transform, "BACK", new Color(0.16f, 0.22f, 0.23f));
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.08f, 0.10f), new Vector2(0.26f, 0.18f));
            back.onClick.AddListener(() => GameDirector.Instance.ShowTitle());
            BuildGuide();
            HideCampaignMenu();
        }

        public void ShowCampaignMenu(GamePhase phase, CampaignProgress progress)
        {
            selectionPanel.SetActive(false);
            resultPanel.SetActive(false);
            pausePanel.SetActive(false);
            crosshair.SetActive(false);
            entryPrompt.gameObject.SetActive(false);
            miniMap.gameObject.SetActive(false);
            transform.Find("Top Bar").gameObject.SetActive(false);
            if (mobileRoot != null) mobileRoot.SetActive(false);
            titlePanel.SetActive(phase == GamePhase.Title);
            levelPanel.SetActive(phase == GamePhase.LevelSelect);
            guidePanel.SetActive(phase == GamePhase.Guide);
            campaignSummary.text = $"{progress.CompletedCount:00} / 10 CLEARED";
            for (var i = 0; i < 10; i++)
            {
                var unlocked = progress.IsUnlocked(i + 1);
                var completed = progress.IsCompleted(i + 1);
                levelButtons[i].interactable = unlocked;
                levelButtons[i].GetComponent<Image>().color = completed
                    ? new Color(0.08f, 0.27f, 0.23f) : unlocked
                    ? new Color(0.16f, 0.34f, 0.38f) : new Color(0.105f, 0.125f, 0.14f);
                levelStates[i].text = completed ? "CLEARED" : unlocked ? "ENTER" : "LOCKED";
                levelStates[i].color = completed ? Accent : unlocked ? Ink : new Color(0.61f, 0.67f, 0.69f);
            }
        }

        private void HideCampaignMenu()
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            if (levelPanel != null) levelPanel.SetActive(false);
            if (guidePanel != null) guidePanel.SetActive(false);
        }
    }
}
