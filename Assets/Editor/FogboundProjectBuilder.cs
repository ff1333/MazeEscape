using System;
using System.Collections.Generic;
using System.IO;
using FogboundMaze;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FogboundProjectBuilder
{
    private const string MainScene = "Assets/Scenes/Main.unity";

    [MenuItem("Maze Escape/Build Project Scene")]
    public static void BuildProjectScene()
    {
        ConfigureProject();
        CreateVisualAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Maze Escape");
        root.AddComponent<GameDirector>();
        EditorSceneManager.SaveScene(scene, MainScene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("FOGBOUND_BUILD_SCENE_PASS");
    }

    [MenuItem("Maze Escape/Validate Release Boundary")]
    public static void ValidateReleaseBoundary()
    {
        var failures = new List<string>();
        var fingerprints = new HashSet<string>();
        var levels = LevelCatalog.CreateDefault();
        if (levels.Count != 10) failures.Add("Campaign must contain ten levels.");

        foreach (var level in levels)
        {
            try
            {
                level.Validate();
                var maze = MazeGenerator.GenerateForLevel(level);
                var path = MazePathfinder.FindPath(maze, maze.Start, maze.Goal);
                if (path.Count == 0) failures.Add($"Level {level.levelNumber} has no route.");
                if (!fingerprints.Add(Fingerprint(maze))) failures.Add($"Level {level.levelNumber} repeats a layout.");
                var metrics = MazeComplexity.Measure(maze, level);
                Debug.Log($"FOGBOUND_LEVEL_{level.levelNumber:00} {maze.Width}x{maze.Height} " +
                          $"route={metrics.SolutionLength} turns={metrics.Turns} deadEnds={metrics.DeadEnds} " +
                          $"junctions={metrics.Junctions} choices={metrics.RouteChoices} " +
                          $"firstChoice={metrics.FirstChoice} maxGap={metrics.LongestChoiceGap} score={metrics.Score:F3}");
            }
            catch (Exception exception)
            {
                failures.Add($"Level {level.levelNumber}: {exception.Message}");
            }
        }

        if (!File.Exists(MainScene)) failures.Add("Main scene is missing.");
        if (EditorBuildSettings.scenes.Length != 1 || EditorBuildSettings.scenes[0].path != MainScene || !EditorBuildSettings.scenes[0].enabled)
            failures.Add("Main scene must be the only enabled build scene.");

        if (File.Exists(MainScene))
        {
            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) failures.Add("Main scene contains a missing script.");
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException("FOGBOUND_VALIDATION_FAIL\n" + string.Join("\n", failures));
        }

        Debug.Log("FOGBOUND_VALIDATION_PASS");
    }

    public static void BuildAndValidate()
    {
        BuildProjectScene();
        ValidateReleaseBoundary();
    }

    private static void ConfigureProject()
    {
        PlayerSettings.companyName = "FF1333";
        PlayerSettings.productName = "Maze Escape";
        PlayerSettings.bundleVersion = FogboundMaze.ReleaseVersion.Current;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.defaultIsFullScreen = false;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ff1333.fogboundmaze");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (settings.Length > 0)
        {
            var serialized = new SerializedObject(settings[0]);
            var inputHandler = serialized.FindProperty("activeInputHandler");
            if (inputHandler != null)
            {
                inputHandler.intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void CreateVisualAssets()
    {
        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Resources/Textures");
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            throw new InvalidOperationException("A supported lit shader is required.");
        }

        CreateTexture("Assets/Resources/Textures/FogboundWall.png", TextureKind.Wall, 811);
        CreateTexture("Assets/Resources/Textures/FogboundFloor.png", TextureKind.Floor, 1217);
        CreateTexture("Assets/Resources/Textures/FogboundHazard.png", TextureKind.Hazard, 2027);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureTexture("Assets/Resources/Textures/FogboundWall.png");
        ConfigureTexture("Assets/Resources/Textures/FogboundFloor.png");
        ConfigureTexture("Assets/Resources/Textures/FogboundHazard.png");

        CreateMaterial("FogboundBase", shader, null, Color.white, 0f, 0.25f);
        CreateMaterial("FogboundWall", shader, "Assets/Resources/Textures/FogboundWall.png", Color.white, 0f, 0.18f);
        CreateMaterial("FogboundFloor", shader, "Assets/Resources/Textures/FogboundFloor.png", Color.white, 0.08f, 0.42f);
        CreateMaterial("FogboundHazard", shader, "Assets/Resources/Textures/FogboundHazard.png", Color.white, 0.35f, 0.35f);
        CreateMaterial("FogboundPlayer", shader, null, new Color(0.08f, 0.43f, 0.58f), 0.2f, 0.38f);
        CreateMaterial("FogboundZombie", shader, null, new Color(0.22f, 0.55f, 0.31f), 0f, 0.18f);

        const string skyPath = "Assets/Resources/FogboundSky.mat";
        var skyShader = Shader.Find("Skybox/Procedural");
        if (AssetDatabase.LoadAssetAtPath<Material>(skyPath) == null && skyShader != null)
        {
            var sky = new Material(skyShader) { name = "Fogbound Sky" };
            sky.SetColor("_SkyTint", new Color(0.22f, 0.34f, 0.36f));
            sky.SetColor("_GroundColor", new Color(0.06f, 0.09f, 0.1f));
            sky.SetFloat("_AtmosphereThickness", 1.25f);
            sky.SetFloat("_Exposure", 0.72f);
            sky.SetFloat("_SunSize", 0.025f);
            AssetDatabase.CreateAsset(sky, skyPath);
        }
    }

    private static void CreateMaterial(string name, Shader shader, string texturePath, Color color, float metallic, float smoothness)
    {
        var path = $"Assets/Resources/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.mainTexture = string.IsNullOrEmpty(texturePath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        material.mainTextureScale = name == "FogboundFloor" ? new Vector2(1.6f, 1.6f) : Vector2.one;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
    }

    private static void CreateTexture(string path, TextureKind kind, int seed)
    {
        if (File.Exists(path)) return;
        const int size = 256;
        var random = new System.Random(seed);
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                Color color;
                if (kind == TextureKind.Hazard)
                {
                    color = ((x + y) / 34) % 2 == 0 ? new Color(0.78f, 0.51f, 0.08f) : new Color(0.08f, 0.09f, 0.085f);
                }
                else
                {
                    var noise = (float)random.NextDouble() * 0.11f - 0.055f;
                    var seam = kind == TextureKind.Wall
                        ? (y % 64 < 3 || (x + (y / 64 % 2) * 32) % 64 < 3)
                        : (x % 64 < 3 || y % 64 < 3);
                    var baseValue = kind == TextureKind.Wall ? 0.34f : 0.18f;
                    if (seam) baseValue -= kind == TextureKind.Wall ? 0.14f : 0.08f;
                    color = new Color(baseValue + noise, baseValue + noise * 0.8f, baseValue + noise * 0.65f);
                }
                pixels[y * size + x] = color;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void ConfigureTexture(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private enum TextureKind { Wall, Floor, Hazard }

    private static string Fingerprint(MazeLayout maze)
    {
        var value = $"{maze.Width}x{maze.Height}:";
        for (var y = 0; y < maze.Height; y++)
        {
            for (var x = 0; x < maze.Width; x++) value += (int)maze[x, y].Passages + ",";
        }
        return value;
    }
}
