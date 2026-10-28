using System.IO;
using UnityEditor;
using UnityEngine;

public static class FogboundPreviewBuilder
{
    public static void Build()
    {
        foreach (var weapon in new[] { "SniperRifle", "LongBlade" })
        {
            var model = Object.Instantiate(Resources.Load<GameObject>("Weapons/" + weapon));
            model.transform.position = new Vector3(500,500,500);
            model.transform.rotation = Quaternion.Euler(0, -65, weapon == "LongBlade" ? 45 : -15);
            var bounds = model.GetComponentInChildren<Renderer>().bounds;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            var cameraObject = new GameObject("Preview Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.size.x,bounds.size.y) * .65f;
            camera.transform.position = bounds.center + Vector3.back * 5f;
            var lightObject = new GameObject("Preview Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(30,-30,0);
            var target = new RenderTexture(512,512,24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(512,512,TextureFormat.RGBA32,false);
            texture.ReadPixels(new Rect(0,0,512,512),0,0);
            texture.Apply();
            File.WriteAllBytes("Assets/Resources/" + weapon + "Preview.png",texture.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(model);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
        }
        AssetDatabase.Refresh();
        Debug.Log("FOGBOUND_PREVIEWS_PASS");
    }
}
