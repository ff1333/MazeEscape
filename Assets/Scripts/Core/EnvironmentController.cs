using UnityEngine;

namespace FogboundMaze
{
    public sealed class EnvironmentController : MonoBehaviour
    {
        private Light sun;
        private LevelDefinition level;
        private float cycle;
        public float EnemyVisionMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.72f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
        }

        public void Apply(LevelDefinition definition)
        {
            level = definition;
            cycle = 0.18f;
            var sky = Resources.Load<Material>("FogboundSky");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.fog = definition.fogDensity > 0.01f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = definition.fogDensity;
            EnemyVisionMultiplier = Mathf.Lerp(1f, .52f, Mathf.InverseLerp(.002f, .038f, definition.fogDensity));
            RenderSettings.fogColor = new Color(0.12f, 0.16f, 0.17f);
            RenderSettings.ambientLight = new Color(0.2f, 0.24f, 0.25f);
            UpdateLighting();
        }

        private void Update()
        {
            if (level == null || !level.dayNightCycle || GameDirector.Instance?.Phase != GamePhase.Playing) return;
            cycle = Mathf.Repeat(cycle + Time.deltaTime / level.dayDuration, 1f);
            UpdateLighting();
        }

        private void UpdateLighting()
        {
            var daylight = level != null && level.dayNightCycle
                ? Mathf.Clamp01(Mathf.Sin(cycle * Mathf.PI * 2f) * 0.55f + 0.5f)
                : 1f;
            sun.transform.rotation = Quaternion.Euler(cycle * 360f - 90f, 28f, 0f);
            sun.intensity = Mathf.Lerp(0.2f, 1.25f, daylight);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.075f, 0.09f, 0.1f), new Color(0.32f, 0.34f, 0.31f), daylight);
        }
    }
}
