using UnityEngine;

namespace FogboundMaze
{
    public sealed class EnvironmentController : MonoBehaviour
    {
        private Light overhead;
        public float EnemyVisionMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            overhead = new GameObject("Dungeon Ambient Light").AddComponent<Light>();
            overhead.type = LightType.Directional;
            overhead.shadows = LightShadows.Soft;
            overhead.transform.rotation = Quaternion.Euler(70f, 25f, 0f);
        }

        public void Apply(LevelDefinition definition)
        {
            var sky = Resources.Load<Material>("FogboundSky");
            if (sky != null) RenderSettings.skybox = sky;

            var ambient = new Color(.16f,.19f,.19f);
            var lightColor = new Color(.78f,.86f,.82f);
            var intensity = .55f;
            var fogColor = new Color(.10f,.14f,.14f);
            var conditionMultiplier = 1f;
            switch (definition.environment)
            {
                case DungeonCondition.Dim:
                    ambient = new Color(.10f,.13f,.14f); intensity = .34f; conditionMultiplier = .88f;
                    break;
                case DungeonCondition.Emergency:
                    ambient = new Color(.14f,.075f,.06f); lightColor = new Color(1f,.34f,.20f); intensity = .38f;
                    fogColor = new Color(.18f,.08f,.065f); conditionMultiplier = .82f;
                    break;
                case DungeonCondition.Blackout:
                    ambient = new Color(.035f,.045f,.05f); intensity = .08f; conditionMultiplier = .68f;
                    break;
                case DungeonCondition.Contaminated:
                    ambient = new Color(.07f,.12f,.085f); lightColor = new Color(.55f,1f,.62f); intensity = .26f;
                    fogColor = new Color(.09f,.16f,.10f); conditionMultiplier = .70f;
                    break;
                case DungeonCondition.Lockdown:
                    ambient = new Color(.10f,.045f,.045f); lightColor = new Color(1f,.20f,.16f); intensity = .25f;
                    fogColor = new Color(.16f,.055f,.05f); conditionMultiplier = .62f;
                    break;
                case DungeonCondition.DeepCore:
                    ambient = new Color(.03f,.05f,.055f); lightColor = new Color(.30f,.72f,.68f); intensity = .12f;
                    fogColor = new Color(.055f,.10f,.105f); conditionMultiplier = .55f;
                    break;
            }

            overhead.color = lightColor;
            overhead.intensity = intensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = definition.fogDensity > .001f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = definition.fogDensity;
            RenderSettings.fogColor = fogColor;
            var densityMultiplier = Mathf.Lerp(1f, .78f, Mathf.InverseLerp(.002f, .018f, definition.fogDensity));
            EnemyVisionMultiplier = conditionMultiplier * densityMultiplier;
        }
    }
}
