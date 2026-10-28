using UnityEngine;
using UnityEngine.UI;

namespace FogboundMaze
{
    public sealed partial class GameHud
    {
        private GameObject guidePanel;
        private Text guideTitle;
        private Text guideBody;
        private readonly Button[] guideTabs = new Button[10];

        public static string LevelBrief(int number) => number switch
        {
            1 => "THE FIRST EXIT\nLearn the corridors. Ordinary zombies follow the maze paths. Reach the exit alive.",
            2 => "MORE CROSSROADS\nA larger maze and more dead ends. The map remembers where you have walked.",
            3 => "NOISE DISCIPLINE\nGunfire reaches farther than the blade. Switch weapons to control how many zombies investigate.",
            4 => "LOCAL FOG\nVisible fog pockets reduce both player awareness and zombie sight. Use the explored map to cross them.",
            5 => "RED ALERT\nEmergency lighting and elite zombies increase pressure without changing during the short run.",
            6 => "BLACKOUT\nMost fixed lights are offline. Use the flashlight, quieter attacks and remembered corridors.",
            7 => "CONTAMINATION\nMiasma drains health outside safe lights. Lamps also stand in side routes and dead ends.",
            8 => "DEEP ZONE\nFog pockets, elites and contamination combine while the environment remains consistently readable.",
            9 => "LOCKDOWN\nRed warning lights, frequent enemies and group alerts make noisy routes dangerous.",
            _ => "THE LAST EXIT\nThe largest maze and strongest combined pressure. Clear this exit to finish the campaign."
        };

        private void BuildGuide()
        {
            guidePanel = PanelObject("Field Guide", transform, new Color(0.035f, 0.045f, 0.05f, 0.98f));
            SetRect(guidePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var heading = Label("Heading", guidePanel.transform, "FIELD GUIDE", 52, TextAnchor.MiddleLeft);
            SetRect(heading.rectTransform, new Vector2(.07f,.84f), new Vector2(.9f,.95f));
            for (var i = 0; i < 10; i++)
            {
                var number = i + 1;
                var tab = Button("Chapter " + number, guidePanel.transform, "LEVEL " + number.ToString("00"), Panel);
                var row = i % 5;
                var col = i / 5;
                SetRect(tab.GetComponent<RectTransform>(), new Vector2(.07f + col * .13f,.68f - row * .112f),
                    new Vector2(.19f + col * .13f,.775f - row * .112f));
                tab.onClick.AddListener(() => ShowGuideChapter(number));
                guideTabs[i] = tab;
            }
            guideTitle = Label("Chapter Title", guidePanel.transform, "", 36, TextAnchor.UpperLeft, Accent);
            SetRect(guideTitle.rectTransform, new Vector2(.38f,.62f), new Vector2(.93f,.79f));
            guideBody = Label("Chapter Details", guidePanel.transform, "", 27, TextAnchor.UpperLeft);
            SetRect(guideBody.rectTransform, new Vector2(.38f,.22f), new Vector2(.92f,.62f));
            var back = Button("Back", guidePanel.transform, "BACK", new Color(.18f,.24f,.26f));
            SetRect(back.GetComponent<RectTransform>(), new Vector2(.07f,.07f), new Vector2(.25f,.15f));
            back.onClick.AddListener(() => GameDirector.Instance.ShowTitle());
            ShowGuideChapter(1);
            guidePanel.SetActive(false);
        }

        private void ShowGuideChapter(int number)
        {
            var level = LevelCatalog.CreateDefault()[number - 1];
            var brief = LevelBrief(number).Split('\n');
            guideTitle.text = $"{number:00}  /  {brief[0]}";
            guideBody.text = brief[1] + $"\n\nMAZE  {level.width} x {level.height}     ENEMIES  {level.maxEnemies} MAX"
                + $"\nSPAWN INTERVAL  {level.spawnInterval:0.0}s     ELITE CHANCE  {level.eliteChance:P0}"
                + $"\n\nCONDITION  {ConditionLabel(level.environment)}     FOG ZONES  {level.localFogZones}"
                + (level.miasma ? $"\nMIASMA  {level.miasmaDamagePerSecond:0.0} HP/s     SAFE RADIUS  {level.safeLightRadius:0.0}m" : "\nMIASMA  OFF")
                + "\n\nClear the previous level to unlock the next. Replaying a cleared level keeps your progress.";
            for (var i = 0; i < guideTabs.Length; i++)
                guideTabs[i].GetComponent<Image>().color = i + 1 == number ? new Color(.15f,.40f,.34f) : Panel;
        }
    }
}
