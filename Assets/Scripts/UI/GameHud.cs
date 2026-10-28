using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FogboundMaze
{
    public sealed partial class GameHud : MonoBehaviour
    {
        private static readonly Color Ink = new(0.92f, 0.95f, 0.91f);
        private static readonly Color Panel = new(0.035f, 0.055f, 0.06f, 0.97f);
        private static readonly Color Accent = new(0.2f, 0.78f, 0.62f);
        private static readonly Color Warning = new(0.95f, 0.55f, 0.18f);

        private GameInput input;
        private Font font;
        private Text healthText;
        private Image healthFill;
        private Health observedHealth;
        private Text levelText;
        private Text weaponText;
        private Text timerText;
        private Text killsText;
        private Text statusText;
        private Text resultTitle;
        private Text resultStats;
        private GameObject crosshair;
        private readonly List<Image> crosshairParts = new();
        private Text entryPrompt;
        private MiniMapHud miniMap;
        private GameObject selectionPanel;
        private GameObject resultPanel;
        private GameObject pausePanel;
        private float crosshairFlash;
        private GameObject mobileRoot;
        private float shotPulse;
        private float lastHealth = -1f;
        private float damagePulse;
        private readonly List<Image> damageEdges = new();
        private Text hitMarker;
        private Text hitDetails;
        private Image gunSlot;
        private Image bladeSlot;
        private Text ammoText;
        private Text weaponStateText;
        private Image reloadFill;
        private GameObject weaponHud;
        private Image fogWash;
        private Image fogVignette;
        private Texture2D fogVignetteTexture;
        private GameObject scopeOverlay;
        private Text scopeStatus;
        private Texture2D scopeTexture;

        public int ExploredCells => miniMap?.VisitedCount ?? 0;

        public static GameHud Create(GameInput input)
        {
            var root = new GameObject("Game HUD");
            var hud = root.AddComponent<GameHud>();
            hud.input = input;
            hud.Build();
            return hud;
        }

        private void Update()
        {
            shotPulse = Mathf.Max(0f, shotPulse - Time.unscaledDeltaTime * 5f);
            crosshair.transform.localScale = Vector3.one * (1f + shotPulse * .35f);
            hitMarker.gameObject.SetActive(crosshair.activeSelf && crosshairFlash > 0f);
            hitDetails.gameObject.SetActive(crosshair.activeSelf && crosshairFlash > 0f);
            damagePulse = Mathf.Max(0f, damagePulse - Time.unscaledDeltaTime * 2f);
            foreach (var edge in damageEdges) edge.color = new Color(.95f,.15f,.24f,damagePulse * .30f);
            if (crosshairFlash > 0f)
            {
                crosshairFlash -= Time.unscaledDeltaTime;
                foreach (var part in crosshairParts)
                    part.color = crosshairFlash > 0f ? Accent : Ink;
            }
            RefreshFogOverlay();
        }

        public void Refresh(GameDirector director, PlayerController player)
        {
            RefreshHealth(player.Health);
            levelText.text = $"LEVEL  {director.CurrentLevel.levelNumber:00}";
            timerText.text = TimeSpan.FromSeconds(director.Elapsed).ToString(@"mm\:ss");
            killsText.text = $"KILLS  {director.Kills:000}";
            RefreshWeaponHud(player.Weapon);
            statusText.text = director.CurrentLevel.miasma && !director.IsPlayerSafe ? "MIASMA EXPOSED"
                : director.IsPlayerInFog ? "LOCAL FOG"
                : ConditionLabel(director.CurrentLevel.environment);
            statusText.color = director.CurrentLevel.miasma && !director.IsPlayerSafe || director.IsPlayerInFog
                ? Warning : Accent;
        }

        public void BindHealth(Health health)
        {
            if (observedHealth != null) observedHealth.Changed -= RefreshHealth;
            observedHealth = health;
            observedHealth.Changed += RefreshHealth;
            RefreshHealth(observedHealth);
        }

        private void RefreshHealth(Health health)
        {
            if (lastHealth >= 0 && health.Current < lastHealth) damagePulse = 1f;
            lastHealth = health.Current;
            healthText.text = $"HP  {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Maximum)}";
            var ratio = health.Maximum > 0f ? Mathf.Clamp01(health.Current / health.Maximum) : 0f;
            // This solid-color Image has no sprite, so size its rect instead of using Filled.
            healthFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        }

        private void OnDestroy()
        {
            if (observedHealth != null) observedHealth.Changed -= RefreshHealth;
            if (fogVignette != null && fogVignette.sprite != null) Destroy(fogVignette.sprite);
            if (fogVignetteTexture != null) Destroy(fogVignetteTexture);
            if (scopeOverlay != null)
            {
                var image = scopeOverlay.GetComponent<Image>();
                if (image != null && image.sprite != null) Destroy(image.sprite);
            }
            if (scopeTexture != null) Destroy(scopeTexture);
        }

        public void ShowWeaponSelection(int level)
        {
            HideCampaignMenu();
            transform.Find("Top Bar").gameObject.SetActive(true);
            if (mobileRoot != null) mobileRoot.SetActive(false);
            selectionPanel.SetActive(true);
            weaponHud.SetActive(false);
            miniMap.gameObject.SetActive(false);
            crosshair.SetActive(false);
            entryPrompt.gameObject.SetActive(false);
            resultPanel.SetActive(false);
            pausePanel.SetActive(false);
            levelText.text = $"LEVEL  {level:00}";
            selectionPanel.transform.Find("Subtitle").GetComponent<Text>().text = LevelBrief(level).Split('\n')[0];
        }

        public void HideWeaponSelection()
        {
            if (mobileRoot != null) mobileRoot.SetActive(true);
            selectionPanel.SetActive(false);
            weaponHud.SetActive(true);
            miniMap.gameObject.SetActive(true);
            crosshair.SetActive(true);
        }

        public void ResetMap(MazeWorld world, PlayerController player, CameraRig cameraRig)
        {
            miniMap.Configure(world, player.transform, cameraRig);
        }

        public void ShowEntryPrompt() => entryPrompt.gameObject.SetActive(true);
        public void HideEntryPrompt() => entryPrompt.gameObject.SetActive(false);

        public void SetPhase(GamePhase phase, int level)
        {
            levelText.text = $"LEVEL  {level:00}";
        }

        public void ShowResult(bool won, int level, float elapsed, int kills)
        {
            if (mobileRoot != null) mobileRoot.SetActive(false);
            resultPanel.SetActive(true);
            weaponHud.SetActive(false);
            crosshair.SetActive(false);
            resultTitle.text = won ? (level == 10 ? "CAMPAIGN CLEARED" : "EXIT REACHED") : "RUN LOST";
            resultTitle.color = won ? Accent : Warning;
            resultStats.text = $"LEVEL {level:00}    {TimeSpan.FromSeconds(elapsed):mm\\:ss}    KILLS {kills:000}";
            var next = resultPanel.transform.Find("Next")?.gameObject;
            if (next != null) next.SetActive(won && level < 10);
        }

        public void SetPause(bool paused)
        {
            if (mobileRoot != null) mobileRoot.SetActive(!paused);
            pausePanel.SetActive(paused);
            crosshair.SetActive(!paused);
        }

        public void FlashCrosshair()
        {
            crosshairFlash = 0.35f;
        }
        public void ConfirmHit(float damage, bool killed)
        {
            FlashCrosshair();
            hitMarker.color = killed ? Warning : Accent;
            hitDetails.color = hitMarker.color;
            hitDetails.text = killed ? "ELIMINATED" : $"HIT  {damage:0}";
        }
        public void PulseShot() => shotPulse = 1f;

        private static string ConditionLabel(DungeonCondition condition) => condition switch
        {
            DungeonCondition.Dim => "DIM LIGHTS",
            DungeonCondition.Emergency => "RED ALERT",
            DungeonCondition.Contaminated => "CONTAMINATED",
            DungeonCondition.DeepCore => "DEEP CORE",
            _ => condition.ToString().ToUpperInvariant()
        };

        private void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            BuildFogOverlay();
            BuildScopeOverlay();
            BuildTopBar();
            BuildWeaponHud();
            miniMap = MiniMapHud.Create(transform, font);
            BuildCrosshair();
            BuildDamageFeedback();
            BuildEntryPrompt();
            BuildSelection();
            BuildResult();
            BuildPause();
            BuildCampaignPanels();
            if (Application.isMobilePlatform)
            {
                mobileRoot = new GameObject("Mobile Controls", typeof(RectTransform));
                mobileRoot.transform.SetParent(transform, false);
                SetRect(mobileRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
                MobileHudBuilder.Build(mobileRoot.transform, input, font);
            }
        }

        private void BuildTopBar()
        {
            var bar = PanelObject("Top Bar", transform, Panel);
            Stretch(bar.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -82f), Vector2.zero);
            healthText = Label("Health", bar.transform, "HP  100 / 100", 25, TextAnchor.MiddleLeft);
            SetRect(healthText.rectTransform, new Vector2(0.02f, 0.44f), new Vector2(0.25f, 1f));
            var healthTrack = PanelObject("Health Track", bar.transform, new Color(0.19f, 0.22f, 0.24f));
            SetRect(healthTrack.GetComponent<RectTransform>(), new Vector2(0.02f, 0.18f), new Vector2(0.25f, 0.31f));
            healthFill = PanelObject("Health Fill", healthTrack.transform, new Color(0.95f, 0.34f, 0.42f)).GetComponent<Image>();
            SetRect(healthFill.rectTransform, Vector2.zero, Vector2.one);
            healthTrack.GetComponent<Image>().raycastTarget = false;
            healthFill.raycastTarget = false;
            timerText = Label("Timer", bar.transform, "00:00", 31, TextAnchor.MiddleCenter);
            SetRect(timerText.rectTransform, new Vector2(0.43f, 0f), new Vector2(0.57f, 1f));
            weaponText = Label("Weapon", bar.transform, "UNARMED", 25, TextAnchor.MiddleCenter);
            SetRect(weaponText.rectTransform, new Vector2(0.59f, 0.42f), new Vector2(0.76f, 1f));
            statusText = Label("Status", bar.transform, string.Empty, 19, TextAnchor.MiddleCenter);
            SetRect(statusText.rectTransform, new Vector2(0.59f, 0f), new Vector2(0.76f, 0.48f));
            levelText = Label("Level", bar.transform, "LEVEL  01", 23, TextAnchor.MiddleCenter, Accent);
            SetRect(levelText.rectTransform, new Vector2(0.77f, 0f), new Vector2(0.84f, 1f));
            killsText = Label("Kills", bar.transform, "KILLS  000", 23, TextAnchor.MiddleCenter);
            SetRect(killsText.rectTransform, new Vector2(0.85f, 0f), new Vector2(0.97f, 1f));
        }

        private void BuildFogOverlay()
        {
            fogWash = PanelObject("Fog Wash",transform,Color.clear).GetComponent<Image>();
            SetRect(fogWash.rectTransform,Vector2.zero,Vector2.one);
            fogWash.raycastTarget = false;
            fogVignette = PanelObject("Fog Vignette",transform,Color.clear).GetComponent<Image>();
            SetRect(fogVignette.rectTransform,Vector2.zero,Vector2.one);
            fogVignette.raycastTarget = false;
            fogVignette.sprite = CreateFogVignetteSprite();
            fogVignette.preserveAspect = false;
            fogVignette.transform.SetAsFirstSibling();
            fogWash.transform.SetAsFirstSibling();
        }

        private Sprite CreateFogVignetteSprite()
        {
            const int size = 64;
            fogVignetteTexture = new Texture2D(size,size,TextureFormat.RGBA32,false)
            {
                name = "Runtime Fog Vignette",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var nx = Mathf.Abs((x + .5f) / size - .5f) * 2f;
                var ny = Mathf.Abs((y + .5f) / size - .5f) * 2f;
                var alpha = Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.48f,1f,Mathf.Max(nx,ny)));
                pixels[y * size + x] = new Color(1f,1f,1f,alpha);
            }
            fogVignetteTexture.SetPixels(pixels);
            fogVignetteTexture.Apply(false,true);
            return Sprite.Create(fogVignetteTexture,new Rect(0,0,size,size),Vector2.one * .5f,100f);
        }

        private void RefreshFogOverlay()
        {
            var active = GameDirector.Instance != null && GameDirector.Instance.Phase == GamePhase.Playing
                && GameDirector.Instance.IsPlayerInFog;
            var pulse = active ? .015f * (Mathf.Sin(Time.unscaledTime * 1.7f) * .5f + .5f) : 0f;
            fogWash.color = active ? new Color(.45f,.58f,.56f,.10f + pulse) : Color.clear;
            fogVignette.color = active ? new Color(.34f,.48f,.46f,.42f + pulse) : Color.clear;
        }

        private void BuildWeaponHud()
        {
            var root = PanelObject("Weapon HUD", transform, new Color(.025f,.04f,.045f,.90f));
            weaponHud = root;
            SetRect(root.GetComponent<RectTransform>(), new Vector2(.72f,.035f), new Vector2(.98f,.205f));
            AddFrame(root);
            gunSlot = PanelObject("Gun Slot", root.transform, new Color(.10f,.18f,.20f,.95f)).GetComponent<Image>();
            SetRect(gunSlot.rectTransform,new Vector2(.04f,.53f),new Vector2(.47f,.93f));
            var gunLabel = Label("Label",gunSlot.transform,"[1]  SNIPER",23,TextAnchor.MiddleCenter);
            SetRect(gunLabel.rectTransform,Vector2.zero,Vector2.one);
            bladeSlot = PanelObject("Blade Slot", root.transform, new Color(.10f,.18f,.20f,.95f)).GetComponent<Image>();
            SetRect(bladeSlot.rectTransform,new Vector2(.53f,.53f),new Vector2(.96f,.93f));
            var bladeLabel = Label("Label",bladeSlot.transform,"[2]  LONG BLADE",21,TextAnchor.MiddleCenter);
            SetRect(bladeLabel.rectTransform,Vector2.zero,Vector2.one);
            ammoText = Label("Ammo",root.transform,"05 / 020",30,TextAnchor.MiddleLeft);
            SetRect(ammoText.rectTransform,new Vector2(.06f,.12f),new Vector2(.48f,.50f));
            weaponStateText = Label("State",root.transform,"READY",21,TextAnchor.MiddleRight,Accent);
            SetRect(weaponStateText.rectTransform,new Vector2(.49f,.12f),new Vector2(.94f,.50f));
            var reloadTrack = PanelObject("Reload Track",root.transform,new Color(.16f,.20f,.21f,.95f));
            SetRect(reloadTrack.GetComponent<RectTransform>(),new Vector2(.05f,.05f),new Vector2(.95f,.10f));
            reloadFill = PanelObject("Reload Fill",reloadTrack.transform,Accent).GetComponent<Image>();
            SetRect(reloadFill.rectTransform,Vector2.zero,new Vector2(0f,1f));
            reloadTrack.GetComponent<Image>().raycastTarget = false;
            reloadFill.raycastTarget = false;
            root.SetActive(false);
        }

        private void RefreshWeaponHud(WeaponController weapon)
        {
            if (weaponHud != null) weaponHud.SetActive(weapon.Type != WeaponType.None && !selectionPanel.activeSelf && !resultPanel.activeSelf);
            var combatVisible = !selectionPanel.activeSelf && !resultPanel.activeSelf && !pausePanel.activeSelf;
            var gunActive = weapon.Type == WeaponType.SniperRifle;
            gunSlot.color = gunActive ? new Color(.10f,.48f,.40f,.98f) : new Color(.10f,.18f,.20f,.95f);
            bladeSlot.color = !gunActive && weapon.Type == WeaponType.LongBlade
                ? new Color(.58f,.30f,.12f,.98f) : new Color(.10f,.18f,.20f,.95f);
            weaponText.text = gunActive ? "SNIPER  /  LONG BLADE" : weapon.Type == WeaponType.LongBlade
                ? "LONG BLADE  /  SNIPER" : "UNARMED";
            if (gunActive)
            {
                ammoText.text = $"{weapon.Ammunition:00} / {weapon.ReserveAmmunition:000}";
                weaponStateText.text = weapon.IsReloading ? $"RELOAD  {weapon.ReloadProgress:P0}"
                    : weapon.IsCyclingBolt ? $"BOLT  {weapon.BoltProgress:P0}"
                    : weapon.EmptyFeedback ? "EMPTY - PRESS R"
                    : weapon.IsAiming && weapon.AimSlowRemaining > 0f ? $"FOCUS  {weapon.AimSlowRemaining:0.0}s"
                    : weapon.ReloadReady ? "READY" : "R  RELOAD";
                weaponStateText.color = weapon.EmptyFeedback ? Warning : weapon.ReloadReady ? Accent : Ink;
                var progress = weapon.IsReloading ? weapon.ReloadProgress
                    : weapon.IsCyclingBolt ? weapon.BoltProgress : 0f;
                reloadFill.rectTransform.anchorMax = new Vector2(progress,1f);
            }
            else
            {
                ammoText.text = $"RANGE  {WeaponController.BladeRange:0.0}m";
                weaponStateText.text = weapon.AttackCooldownRemaining > 0f ? "RECOVERING" : "READY";
                weaponStateText.color = weapon.AttackCooldownRemaining > 0f ? Warning : Accent;
                var cooldown = Mathf.Clamp01(weapon.AttackCooldownRemaining / .72f);
                reloadFill.rectTransform.anchorMax = new Vector2(1f - cooldown,1f);
            }
            RefreshCrosshairStyle(weapon.Type);
            scopeOverlay.SetActive(weapon.IsAiming && combatVisible);
            scopeStatus.text = weapon.AimSlowRemaining > 0f
                ? $"FOCUS TIME 0.5x   {weapon.AimSlowRemaining:0.0}s"
                : "SCOPE STABLE";
            crosshair.SetActive(!weapon.IsAiming && combatVisible);
        }

        private void BuildScopeOverlay()
        {
            scopeOverlay = PanelObject("Sniper Scope",transform,Color.white);
            SetRect(scopeOverlay.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            var image = scopeOverlay.GetComponent<Image>();
            image.raycastTarget = false;
            const int size = 128;
            scopeTexture = new Texture2D(size,size,TextureFormat.RGBA32,false)
            {
                name = "Runtime Sniper Scope",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var nx = ((x + .5f) / size - .5f) * 2f;
                var ny = ((y + .5f) / size - .5f) * 2f;
                var radius = Mathf.Sqrt(nx * nx + ny * ny);
                var alpha = Mathf.SmoothStep(0f,.98f,Mathf.InverseLerp(.72f,.82f,radius));
                pixels[y * size + x] = new Color(.01f,.015f,.015f,alpha);
            }
            scopeTexture.SetPixels(pixels);
            scopeTexture.Apply(false,true);
            image.sprite = Sprite.Create(scopeTexture,new Rect(0,0,size,size),Vector2.one * .5f,100f);
            image.preserveAspect = false;
            ScopeLine("Horizontal",new Vector2(.18f,.499f),new Vector2(.82f,.501f));
            ScopeLine("Vertical",new Vector2(.499f,.18f),new Vector2(.501f,.82f));
            var center = PanelObject("Reticle",scopeOverlay.transform,Accent).GetComponent<Image>();
            center.raycastTarget = false;
            center.rectTransform.anchorMin = center.rectTransform.anchorMax = Vector2.one * .5f;
            center.rectTransform.sizeDelta = new Vector2(7f,7f);
            scopeStatus = Label("Scope Status",scopeOverlay.transform,"FOCUS TIME 0.5x   3.0s",22,TextAnchor.MiddleCenter,Accent);
            SetRect(scopeStatus.rectTransform,new Vector2(.32f,.12f),new Vector2(.68f,.18f));
            scopeOverlay.SetActive(false);
        }

        private void ScopeLine(string name, Vector2 min, Vector2 max)
        {
            var line = PanelObject(name,scopeOverlay.transform,new Color(.78f,.88f,.82f,.72f)).GetComponent<Image>();
            line.raycastTarget = false;
            SetRect(line.rectTransform,min,max);
        }

        private void RefreshCrosshairStyle(WeaponType type)
        {
            if (crosshairParts.Count < 5) return;
            var blade = type == WeaponType.LongBlade;
            crosshairParts[1].gameObject.SetActive(!blade);
            crosshairParts[2].gameObject.SetActive(!blade);
            crosshairParts[3].rectTransform.anchoredPosition = new Vector2(blade ? -27f : -15f,0f);
            crosshairParts[4].rectTransform.anchoredPosition = new Vector2(blade ? 27f : 15f,0f);
            crosshairParts[3].rectTransform.sizeDelta = blade ? new Vector2(18f,4f) : new Vector2(11f,3f);
            crosshairParts[4].rectTransform.sizeDelta = blade ? new Vector2(18f,4f) : new Vector2(11f,3f);
        }

        private void BuildCrosshair()
        {
            var root = new GameObject("Crosshair", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            crosshair = root;
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(52f, 52f);
            CrosshairPart(root.transform, "Center", Vector2.zero, new Vector2(5f, 5f));
            CrosshairPart(root.transform, "Top", new Vector2(0f, 15f), new Vector2(3f, 11f));
            CrosshairPart(root.transform, "Bottom", new Vector2(0f, -15f), new Vector2(3f, 11f));
            CrosshairPart(root.transform, "Left", new Vector2(-15f, 0f), new Vector2(11f, 3f));
            CrosshairPart(root.transform, "Right", new Vector2(15f, 0f), new Vector2(11f, 3f));
            crosshair.SetActive(false);
            hitMarker = Label("Hit Marker", transform, "X", 30, TextAnchor.MiddleCenter, new Color(1f,.83f,.36f));
            hitMarker.raycastTarget = false;
            hitMarker.rectTransform.anchorMin = hitMarker.rectTransform.anchorMax = Vector2.one * .5f;
            hitMarker.rectTransform.sizeDelta = new Vector2(55,55);
            hitMarker.gameObject.SetActive(false);
            hitDetails = Label("Hit Details", transform, "", 22, TextAnchor.MiddleCenter, Accent);
            hitDetails.raycastTarget = false;
            hitDetails.rectTransform.anchorMin = hitDetails.rectTransform.anchorMax = Vector2.one * .5f;
            hitDetails.rectTransform.anchoredPosition = new Vector2(0,-48);
            hitDetails.rectTransform.sizeDelta = new Vector2(230,32);
            hitDetails.gameObject.SetActive(false);
        }

        private void BuildDamageFeedback()
        {
            var bounds = new[] {
                new Vector4(0,0,.012f,1), new Vector4(.988f,0,1,1),
                new Vector4(0,0,1,.018f), new Vector4(0,.982f,1,1)
            };
            foreach (var bound in bounds)
            {
                var edge = PanelObject("Damage Edge",transform,Color.clear).GetComponent<Image>();
                edge.raycastTarget = false;
                SetRect(edge.rectTransform,new Vector2(bound.x,bound.y),new Vector2(bound.z,bound.w));
                damageEdges.Add(edge);
            }
        }

        private void CrosshairPart(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var part = new GameObject(name, typeof(RectTransform), typeof(Image));
            part.transform.SetParent(parent, false);
            var rect = part.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = part.GetComponent<Image>();
            image.color = Ink;
            image.raycastTarget = false;
            crosshairParts.Add(image);
        }

        private void BuildEntryPrompt()
        {
            entryPrompt = Label("Entry Prompt", transform, "ENTER THROUGH THE GREEN GATE", 27, TextAnchor.MiddleCenter, Accent);
            SetRect(entryPrompt.rectTransform, new Vector2(0.26f, 0.77f), new Vector2(0.74f, 0.83f));
            entryPrompt.gameObject.SetActive(false);
        }

        private void BuildSelection()
        {
            selectionPanel = PanelObject("Weapon Selection", transform, Panel);
            AddFrame(selectionPanel);
            var rect = selectionPanel.GetComponent<RectTransform>();
            SetRect(rect,new Vector2(.20f,.19f),new Vector2(.80f,.81f));
            var title = Label("Title", selectionPanel.transform, "DUNGEON LOADOUT READY", 42, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 0.72f), Vector2.one);
            var subtitle = Label("Subtitle", selectionPanel.transform, "START WITH THE BLADE. SWITCH TO THE SNIPER WHEN NEEDED.", 23, TextAnchor.MiddleCenter, new Color(0.80f, 0.87f, 0.86f));
            SetRect(subtitle.rectTransform, new Vector2(0f, 0.61f), new Vector2(1f, 0.76f));
            var pistol = Button("Pistol", selectionPanel.transform, "[1] SNIPER\n5 + 20 rounds  |  80m", new Color(0.12f, 0.42f, 0.55f));
            SetRect(pistol.GetComponent<RectTransform>(), new Vector2(0.08f, 0.23f), new Vector2(0.48f, 0.58f));
            pistol.onClick.AddListener(() => GameDirector.Instance.Player.Weapon.Equip(WeaponType.SniperRifle));
            var machete = Button("Machete", selectionPanel.transform, "LONG BLADE\n70 damage  |  3.4m", new Color(0.55f, 0.25f, 0.12f));
            SetRect(machete.GetComponent<RectTransform>(), new Vector2(0.52f, 0.23f), new Vector2(0.92f, 0.58f));
            machete.onClick.AddListener(() => GameDirector.Instance.Player.Weapon.Equip(WeaponType.LongBlade));
            AddWeaponPreview(pistol, "SniperRiflePreview");
            AddWeaponPreview(machete, "LongBladePreview");
            var controls = Application.isMobilePlatform
                ? "LEFT STICK MOVE  |  SWIPE LOOK  |  FIRE ATTACK\nGUN / BLADE SWITCH  |  R MANUAL RELOAD  |  VIEW CAMERA"
                : "WASD MOVE  |  LEFT CLICK ATTACK  |  RIGHT CLICK SCOPE\n1 / 2 OR WHEEL SWITCH  |  R MANUAL RELOAD  |  V CAMERA";
            var help = Label("Controls", selectionPanel.transform, controls, 22, TextAnchor.MiddleCenter);
            SetRect(help.rectTransform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.23f));
            var deploy = Button("Deploy", selectionPanel.transform, "ENTER DUNGEON", Accent);
            SetRect(deploy.GetComponent<RectTransform>(),new Vector2(.52f,.02f),new Vector2(.92f,.10f));
            deploy.onClick.AddListener(() => GameDirector.Instance.ConfirmLoadout());
            var back = Button("Back To Levels", selectionPanel.transform, "BACK TO LEVELS", new Color(0.16f, 0.22f, 0.23f));
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.08f, 0.02f), new Vector2(0.48f, 0.10f));
            back.onClick.AddListener(() => GameDirector.Instance.ShowLevelSelection());
        }

        private void BuildResult()
        {
            resultPanel = PanelObject("Result", transform, Panel);
            AddFrame(resultPanel);
            var rect = resultPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650f, 400f);
            resultTitle = Label("Result Title", resultPanel.transform, "EXIT REACHED", 48, TextAnchor.MiddleCenter);
            SetRect(resultTitle.rectTransform, new Vector2(0f, 0.62f), Vector2.one);
            resultStats = Label("Result Stats", resultPanel.transform, string.Empty, 24, TextAnchor.MiddleCenter);
            SetRect(resultStats.rectTransform, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.64f));
            var retry = Button("Retry", resultPanel.transform, "RETRY", new Color(0.24f, 0.29f, 0.3f));
            SetRect(retry.GetComponent<RectTransform>(), new Vector2(0.08f, 0.24f), new Vector2(0.48f, 0.40f));
            retry.onClick.AddListener(() => GameDirector.Instance.Retry());
            var next = Button("Next", resultPanel.transform, "NEXT", Accent);
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.52f, 0.24f), new Vector2(0.92f, 0.40f));
            next.onClick.AddListener(() => GameDirector.Instance.NextLevel());
            var levels = Button("Levels", resultPanel.transform, "LEVEL SELECT", new Color(0.16f, 0.22f, 0.23f));
            SetRect(levels.GetComponent<RectTransform>(), new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.20f));
            levels.onClick.AddListener(() => GameDirector.Instance.ShowLevelSelection());
            resultPanel.SetActive(false);
        }

        private void BuildPause()
        {
            pausePanel = PanelObject("Pause", transform, Panel);
            AddFrame(pausePanel);
            var rect = pausePanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 460f);
            var title = Label("Title", pausePanel.transform, "PAUSED", 46, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 0.79f), Vector2.one);
            var resume = Button("Resume", pausePanel.transform, "RESUME", Accent);
            SetRect(resume.GetComponent<RectTransform>(), new Vector2(0.14f, 0.61f), new Vector2(0.86f, 0.76f));
            resume.onClick.AddListener(() => GameDirector.Instance.SetPaused(false));
            var loadout = Button("Loadout", pausePanel.transform, "BACK TO LOADOUT", new Color(0.22f, 0.34f, 0.34f));
            SetRect(loadout.GetComponent<RectTransform>(), new Vector2(0.14f, 0.43f), new Vector2(0.86f, 0.58f));
            loadout.onClick.AddListener(() => GameDirector.Instance.ReturnToLoadout());
            var levels = Button("Levels", pausePanel.transform, "LEVEL SELECT", new Color(0.16f, 0.22f, 0.23f));
            SetRect(levels.GetComponent<RectTransform>(), new Vector2(0.14f, 0.25f), new Vector2(0.86f, 0.40f));
            levels.onClick.AddListener(() => GameDirector.Instance.ShowLevelSelection());
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                var quit = Button("Quit", pausePanel.transform, "QUIT GAME", new Color(0.38f, 0.2f, 0.18f));
                SetRect(quit.GetComponent<RectTransform>(), new Vector2(0.14f, 0.07f), new Vector2(0.86f, 0.22f));
                quit.onClick.AddListener(() => GameDirector.Instance.QuitGame());
            }
            pausePanel.SetActive(false);
        }

        private Text Label(string name, Transform parent, string value, int size, TextAnchor alignment, Color? color = null)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            var text = root.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color ?? Ink;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size;
            LocalizedLabel.Attach(root);
            return text;
        }

        private void AddWeaponPreview(Button button, string resource)
        {
            var texture = Resources.Load<Texture2D>(resource);
            if (texture == null) return;
            var preview = new GameObject("Weapon Preview",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(button.transform,false);
            preview.texture = texture;
            preview.raycastTarget = false;
            SetRect(preview.rectTransform,new Vector2(.22f,.36f),new Vector2(.78f,1f));
            SetRect(button.transform.Find("Label").GetComponent<RectTransform>(),new Vector2(.03f,.02f),new Vector2(.97f,.40f));
        }

        private Button Button(string name, Transform parent, string label, Color color)
        {
            var root = PanelObject(name, parent, color);
            var button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f,1.2f,1.2f);
            colors.pressedColor = new Color(.75f,.75f,.75f);
            button.colors = colors;
            var text = Label("Label", root.transform, label, 27, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -8f));
            return button;
        }

        private static GameObject PanelObject(string name, Transform parent, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = color;
            return root;
        }

        private static void AddFrame(GameObject panel)
        {
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = Accent;
            outline.effectDistance = new Vector2(2f, 2f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var root = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            root.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 min, Vector2 max)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
        }
    }
}
