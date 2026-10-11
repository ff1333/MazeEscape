using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FogboundWeaponBuilder
{
    [MenuItem("Maze Escape/Build SMG And Long Blade")]
    public static void Build()
    {
        const string survivorPath = "Assets/Resources/Characters/Survivor.prefab";
        var survivor = PrefabUtility.LoadPrefabContents(survivorPath);
        foreach (var name in new[] { "SMG", "LongBlade" })
        {
            var source = survivor.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == (name == "SMG" ? "SMG" : "Knife"));
            var root = new GameObject(name);
            var mesh = new GameObject("Model");
            mesh.transform.SetParent(root.transform, false);
            mesh.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            mesh.AddComponent<MeshRenderer>().sharedMaterial = source.GetComponent<MeshRenderer>().sharedMaterial;
            if (name == "SMG")
            {
                mesh.transform.localScale = Vector3.one * 80f;
                mesh.transform.localRotation = Quaternion.AngleAxis(90f, Vector3.up) * Quaternion.AngleAxis(180f, Vector3.right);
                mesh.transform.localPosition = new Vector3(.093f, -.16f, .05f);
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(root.transform, false);
                var vertices = mesh.GetComponent<MeshFilter>().sharedMesh.vertices;
                var front = vertices.Min(v => v.x);
                var tips = vertices.Where(v => v.x < front + .00015f).ToArray();
                var center = tips.Aggregate(Vector3.zero, (sum, v) => sum + v) / tips.Length;
                muzzle.position = mesh.transform.TransformPoint(center) + Vector3.forward * .012f;
                var socket = source.Find("Muzzle");
                if (socket == null)
                {
                    socket = new GameObject("Muzzle").transform;
                    socket.SetParent(source, false);
                }
                socket.localPosition = center;
            }
            else
            {
                mesh.transform.localScale = new Vector3(75f, 126f, 75f);
                mesh.transform.localRotation = Quaternion.Euler(25f, 0f, 180f);
            }
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/Weapons/{name}.prefab");
            Object.DestroyImmediate(root);
        }
        PrefabUtility.SaveAsPrefabAsset(survivor, survivorPath);
        PrefabUtility.UnloadPrefabContents(survivor);
        AssetDatabase.SaveAssets();
        Debug.Log("FOGBOUND_WEAPONS_PASS");
    }
}
