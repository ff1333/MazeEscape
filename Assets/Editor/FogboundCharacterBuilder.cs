using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FogboundCharacterBuilder
{
    [MenuItem("Maze Escape/Build Character Assets")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var folder = "Assets/Art/QuaterniusZombieApocalypse";
        var materialPath = folder + "/Atlas.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Resources.Load<Material>("FogboundBase"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.color = Color.white;
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Zombie_Atlas.png");
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.15f);
        foreach (var name in new[] { "Characters_Matt", "Zombie_Basic", "Zombie_Chubby" })
        {
            var path = $"{folder}/{name}.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.clipAnimations = importer.defaultClipAnimations
                .Where(clip => clip.takeName.StartsWith("CharacterArmature|", StringComparison.Ordinal)
                    && new[] { "Idle", "Idle_Gun", "Walk", "Walk_Gun", "Run", "Run_Gun", "Slash", "Idle_Attack", "Death" }
                        .Contains(clip.takeName.Split('|').Last()))
                .Select(clip =>
                {
                    clip.name = clip.takeName.Split('|').Last();
                    clip.wrapMode = clip.name == "Death" ? WrapMode.ClampForever
                        : clip.name is "Slash" or "Idle_Attack" ? WrapMode.Once : WrapMode.Loop;
                    clip.loopTime = clip.wrapMode == WrapMode.Loop;
                    return clip;
                }).ToArray();
            importer.SaveAndReimport();
            var resource = name == "Characters_Matt" ? "Survivor" : name == "Zombie_Basic" ? "Zombie" : "EliteZombie";
            var root = new GameObject(resource);
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
            model.name = "Model";
            var weapons = new[] { "Axe", "Guitar", "Knife", "Pistol", "Rifle", "Shotgun", "SMG", "Spear", "WoodenBat_Barbed", "WoodenBat_Saw" };
            foreach (var child in model.GetComponentsInChildren<Transform>(true))
                if (weapons.Contains(child.name)) child.gameObject.SetActive(false);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            var animation = model.GetComponent<Animation>() ?? model.AddComponent<Animation>();
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            foreach (var clip in clips) animation.AddClip(clip, clip.name);
            animation.clip = clips.Single(clip => clip.name == "Idle");
            animation.playAutomatically = true;
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
            // Preserve the FBX unit conversion; runtime animation owns the armature transforms.
            Directory.CreateDirectory("Assets/Resources/Characters");
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/Characters/{resource}.prefab");
            Debug.Log($"CHARACTER_BUILT {resource} clips={clips.Length}");
            UnityEngine.Object.DestroyImmediate(root);
        }
        foreach (var name in new[] { "Pistol", "Knife" })
        {
            var root = new GameObject(name);
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/{name}.fbx"), root.transform);
            model.transform.localScale *= name == "Pistol" ? 0.65f : 1f;
            if (name == "Knife") model.transform.localRotation = Quaternion.Euler(-65f, 0f, -15f);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            Directory.CreateDirectory("Assets/Resources/Weapons");
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/Weapons/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        FogboundWeaponBuilder.Build();
        Debug.Log("FOGBOUND_CHARACTERS_BUILD_PASS");
    }

    public static void Inspect()
    {
        AssetDatabase.Refresh();
        foreach (var file in new[] { "Characters_Matt", "Zombie_Basic", "Zombie_Chubby", "Pistol", "Knife" })
        {
            var path = $"Assets/Art/QuaterniusZombieApocalypse/{file}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = UnityEngine.Object.Instantiate(model);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Debug.Log($"MODEL {file} size={bounds.size} center={bounds.center} renderers={renderers.Length}");
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                Debug.Log($"CLIP {file} {clip.name} length={clip.length}");
            foreach (var material in renderers.SelectMany(r => r.sharedMaterials).Distinct())
                Debug.Log($"MATERIAL {file} {material.name} color={material.color} texture={material.mainTexture}");
            Debug.Log($"BONES {file} " + string.Join(",", instance.GetComponentsInChildren<Transform>().Select(t => t.name)));
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
