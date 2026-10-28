using System.Collections.Generic;
using UnityEngine;

namespace FogboundMaze
{
    public sealed class MazeWorld : MonoBehaviour
    {
        public const float CellSize = 5f;
        private const float WallHeight = 3.4f;
        private const float WallThickness = 0.28f;

        private readonly List<Vector3> safeLights = new();
        private readonly List<Vector3> fogZones = new();
        private Material floorMaterial;
        private Material wallMaterial;
        private Material trimMaterial;
        private Material wallBaseMaterial;
        private Material entryMaterial;
        private Material fogMaterial;
        private Transform generatedRoot;

        public MazeLayout Layout { get; private set; }
        public IReadOnlyList<Vector3> SafeLights => safeLights;
        public IReadOnlyList<Vector3> FogZones => fogZones;
        public Vector3 StartPosition => CellToWorld(Layout.Start) + Vector3.back * (CellSize * 0.9f);
        public Vector3 EntryPosition => CellToWorld(Layout.Start);
        public Vector3 ExitPosition => CellToWorld(Layout.Goal) + Vector3.right * CellSize;
        public GameObject StartGate { get; private set; }

        public void Build(LevelDefinition level)
        {
            Clear();
            Layout = MazeGenerator.GenerateForLevel(level);
            generatedRoot = new GameObject("Generated Maze").transform;
            generatedRoot.SetParent(transform, false);
            CreateMaterials();

            for (var x = 0; x < Layout.Width; x++)
            {
                for (var y = 0; y < Layout.Height; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var center = CellToWorld(cell);
                    CreateFloor(center);
                    if (!Layout[cell].IsOpen(MazeDirection.South) && cell != Layout.Start)
                        CreateWall(center + Vector3.back * CellSize * 0.5f, new Vector3(CellSize, WallHeight, WallThickness));
                    if (!Layout[cell].IsOpen(MazeDirection.West))
                        CreateWall(center + Vector3.left * CellSize * 0.5f, new Vector3(WallThickness, WallHeight, CellSize));
                    if (y == Layout.Height - 1 && !Layout[cell].IsOpen(MazeDirection.North))
                        CreateWall(center + Vector3.forward * CellSize * 0.5f, new Vector3(CellSize, WallHeight, WallThickness));
                    if (x == Layout.Width - 1 && cell != Layout.Goal && !Layout[cell].IsOpen(MazeDirection.East))
                        CreateWall(center + Vector3.right * CellSize * 0.5f, new Vector3(WallThickness, WallHeight, CellSize));
                }
            }

            CreateStagingArea();
            CreateEntryMarkers();
            CreateExit();
            CreateDungeonLights(level);
            CreateLocalFogZones(level);
            if (level.miasma)
            {
                CreateSafeLights(level);
            }

            StaticBatchingUtility.Combine(generatedRoot.gameObject);
        }

        public Vector3 CellToWorld(Vector2Int cell) => new(cell.x * CellSize, 0f, cell.y * CellSize);

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(world.x / CellSize), 0, Layout.Width - 1),
                Mathf.Clamp(Mathf.RoundToInt(world.z / CellSize), 0, Layout.Height - 1));
        }

        public bool IsInside(Vector3 world)
        {
            return world.x >= -CellSize * 0.5f && world.z >= -CellSize * 0.5f
                && world.x <= (Layout.Width - 0.5f) * CellSize
                && world.z <= (Layout.Height - 0.5f) * CellSize;
        }

        public bool IsWithinSafeLight(Vector3 position, float radius)
        {
            var radiusSquared = radius * radius;
            foreach (var light in safeLights)
            {
                var flat = position - light;
                flat.y = 0f;
                if (flat.sqrMagnitude <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsInsideFogZone(Vector3 position)
        {
            foreach (var center in fogZones)
            {
                var delta = position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude <= CellSize * CellSize * .7f) return true;
            }
            return false;
        }

        public float LocalVisibilityMultiplier(Vector3 position) => IsInsideFogZone(position) ? .55f : 1f;

        public List<Vector2Int> FindPath(Vector3 from, Vector3 to)
        {
            return MazePathfinder.FindPath(Layout, WorldToCell(from), WorldToCell(to));
        }

        public void OpenStartGate()
        {
            if (StartGate != null)
            {
                StartGate.SetActive(false);
            }
        }

        private void CreateFloor(Vector3 center)
        {
            var floor = RuntimeArt.Primitive(PrimitiveType.Cube, "Floor", generatedRoot, center - Vector3.up * 0.15f,
                new Vector3(CellSize, 0.3f, CellSize), floorMaterial);
            floor.isStatic = true;
        }

        private void CreateWall(Vector3 center, Vector3 scale)
        {
            var wall = RuntimeArt.Primitive(PrimitiveType.Cube, "Wall", generatedRoot,
                center + Vector3.up * WallHeight * 0.5f, scale, wallMaterial);
            wall.isStatic = true;
            RuntimeArt.Primitive(PrimitiveType.Cube, "Wall Trim", wall.transform,
                new Vector3(0f, 0.49f, 0f), new Vector3(1.02f, 0.04f, 1.02f), trimMaterial, false).isStatic = true;
            RuntimeArt.Primitive(PrimitiveType.Cube, "Wall Base", wall.transform,
                new Vector3(0f, -0.42f, 0f), new Vector3(1.02f, 0.055f, 1.02f), wallBaseMaterial, false).isStatic = true;
            // Shallow supports add depth without narrowing any walkable corridor.
            var alongX = scale.x > scale.z;
            foreach (var offset in new[] { -2.2f, 2.2f })
            {
                var support = center + Vector3.up * WallHeight * .5f
                    + (alongX ? Vector3.right : Vector3.forward) * offset;
                RuntimeArt.Primitive(PrimitiveType.Cube,"Wall Support",generatedRoot,support,
                    alongX ? new Vector3(.13f,WallHeight,.36f) : new Vector3(.36f,WallHeight,.13f),
                    wallBaseMaterial,false).isStatic = true;
            }
        }

        private void CreateStagingArea()
        {
            var start = CellToWorld(Layout.Start);
            // One enclosed cell outside the south opening; walls must not extend into the maze.
            var center = start + Vector3.back * CellSize;
            var stage = RuntimeArt.Primitive(PrimitiveType.Cube, "Staging Ground", generatedRoot,
                center - Vector3.up * 0.15f,
                new Vector3(CellSize, 0.3f, CellSize), floorMaterial);
            stage.isStatic = true;
            CreateWall(center + Vector3.back * CellSize * 0.5f, new Vector3(CellSize, WallHeight, WallThickness));
            CreateWall(center + Vector3.left * CellSize * 0.5f, new Vector3(WallThickness, WallHeight, CellSize));
            CreateWall(center + Vector3.right * CellSize * 0.5f, new Vector3(WallThickness, WallHeight, CellSize));
            StartGate = RuntimeArt.Primitive(PrimitiveType.Cube, "Start Gate", generatedRoot,
                start + Vector3.back * CellSize * 0.5f + Vector3.up * WallHeight * 0.5f,
                new Vector3(CellSize, WallHeight, WallThickness * 1.4f), trimMaterial);
            StartGate.GetComponent<Renderer>().enabled = false;

            var frameColor = RuntimeArt.Material("Entry Frame", new Color(0.12f, 0.86f, 0.57f), 0.2f, 0.55f);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Entry Left", generatedRoot,
                start + new Vector3(-CellSize * 0.45f, 1.65f, -CellSize * 0.5f),
                new Vector3(0.18f, 3.3f, 0.18f), frameColor, false);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Entry Right", generatedRoot,
                start + new Vector3(CellSize * 0.45f, 1.65f, -CellSize * 0.5f),
                new Vector3(0.18f, 3.3f, 0.18f), frameColor, false);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Entry Header", generatedRoot,
                start + new Vector3(0f, 3.25f, -CellSize * 0.5f),
                new Vector3(CellSize * 0.94f, 0.18f, 0.18f), frameColor, false);
            var entryLamp = new GameObject("Entry Light");
            entryLamp.transform.SetParent(generatedRoot, false);
            entryLamp.transform.position = start + new Vector3(0f, 2.7f, -CellSize * 0.5f);
            var entryLight = entryLamp.AddComponent<Light>();
            entryLight.type = LightType.Point;
            entryLight.color = new Color(0.55f, 0.95f, 0.76f);
            entryLight.range = 7f;
            entryLight.intensity = 0.85f;

            var triggerObject = new GameObject("Entry Trigger");
            triggerObject.transform.SetParent(generatedRoot, false);
            triggerObject.transform.position = start + Vector3.up;
            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(CellSize * 0.8f, 2f, CellSize * 0.8f);
            var entryBody = triggerObject.AddComponent<Rigidbody>();
            entryBody.isKinematic = true;
            entryBody.useGravity = false;
            triggerObject.AddComponent<MazeEntryTrigger>();
        }

        private void CreateEntryMarkers()
        {
            entryMaterial ??= RuntimeArt.Material("Entry Direction", new Color(0.15f, 0.9f, 0.56f), 0.1f, 0.55f);
            var start = CellToWorld(Layout.Start);
            CreateFloorArrow(start + Vector3.back * CellSize * 0.5f, Vector3.forward);
            var path = MazePathfinder.FindPath(Layout, Layout.Start, Layout.Goal);
            if (path.Count > 1)
            {
                var next = CellToWorld(path[1]);
                var direction = (next - start).normalized;
                CreateFloorArrow(start + direction * 1.2f, direction);
            }
        }

        private void CreateFloorArrow(Vector3 center, Vector3 direction)
        {
            var root = new GameObject("Entry Direction Arrow").transform;
            root.SetParent(generatedRoot, false);
            root.position = center + Vector3.up * 0.035f;
            root.rotation = Quaternion.LookRotation(direction);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Shaft", root,
                new Vector3(0f, 0f, -0.32f), new Vector3(0.12f, 0.025f, 0.8f), entryMaterial, false);
            for (var side = -1; side <= 1; side += 2)
            {
                var wing = RuntimeArt.Primitive(PrimitiveType.Cube, "Arrow Head", root,
                    new Vector3(side * 0.23f, 0f, 0.28f), new Vector3(0.12f, 0.025f, 0.56f), entryMaterial, false);
                wing.transform.localRotation = Quaternion.Euler(0f, side * 45f, 0f);
            }
        }

        private void CreateExit()
        {
            var goal = CellToWorld(Layout.Goal);
            var outside = goal + Vector3.right * CellSize;
            RuntimeArt.Primitive(PrimitiveType.Cube, "Exit Ground", generatedRoot,
                outside - Vector3.up * 0.15f, new Vector3(CellSize, 0.3f, CellSize), floorMaterial);
            CreateWall(outside + Vector3.right * CellSize * 0.5f, new Vector3(WallThickness, WallHeight, CellSize));
            CreateWall(outside + Vector3.forward * CellSize * 0.5f, new Vector3(CellSize, WallHeight, WallThickness));
            CreateWall(outside + Vector3.back * CellSize * 0.5f, new Vector3(CellSize, WallHeight, WallThickness));
            var exitFrame = RuntimeArt.Material("Exit Frame", new Color(0.24f, 0.88f, 0.68f), 0.2f, 0.6f);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Exit Left", generatedRoot,
                goal + new Vector3(CellSize * 0.5f, 1.6f, -CellSize * 0.45f),
                new Vector3(0.18f, 3.2f, 0.18f), exitFrame, false);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Exit Right", generatedRoot,
                goal + new Vector3(CellSize * 0.5f, 1.6f, CellSize * 0.45f),
                new Vector3(0.18f, 3.2f, 0.18f), exitFrame, false);
            RuntimeArt.Primitive(PrimitiveType.Cube, "Exit Header", generatedRoot,
                goal + new Vector3(CellSize * 0.5f, 3.16f, 0f),
                new Vector3(0.18f, 0.18f, CellSize * 0.94f), exitFrame, false);
            var beacon = RuntimeArt.Primitive(PrimitiveType.Cylinder, "Exit Beacon", generatedRoot,
                goal + Vector3.right * CellSize * 0.65f + Vector3.up * 1.7f,
                new Vector3(0.55f, 1.7f, 0.55f), trimMaterial, false);
            var point = beacon.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(0.3f, 1f, 0.75f);
            point.range = 10f;
            point.intensity = 3f;

            var triggerObject = new GameObject("Exit Trigger");
            triggerObject.transform.SetParent(generatedRoot, false);
            triggerObject.transform.position = goal + Vector3.right * CellSize * 0.65f + Vector3.up;
            var trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2f, 2f, CellSize * 0.8f);
            var exitBody = triggerObject.AddComponent<Rigidbody>();
            exitBody.isKinematic = true;
            exitBody.useGravity = false;
            triggerObject.AddComponent<MazeExitTrigger>();
        }

        private void CreateSafeLights(LevelDefinition level)
        {
            foreach (var cell in SafeLightLayout.Generate(Layout, level.seed))
                CreateSafeLight(CellToWorld(cell));
        }

        private void CreateDungeonLights(LevelDefinition level)
        {
            if (level.environment == DungeonCondition.Blackout || level.environment == DungeonCondition.DeepCore) return;
            var count = Mathf.Clamp((level.width * level.height) / 18, 4, 11);
            var random = new System.Random(level.seed ^ 0x51A7);
            var used = new HashSet<Vector2Int>();
            var color = level.environment switch
            {
                DungeonCondition.Emergency or DungeonCondition.Lockdown => new Color(1f,.18f,.12f),
                DungeonCondition.Contaminated => new Color(.42f,1f,.54f),
                DungeonCondition.Dim => new Color(.55f,.72f,.74f),
                _ => new Color(.72f,.90f,.84f)
            };
            for (var i = 0; i < count * 5 && used.Count < count; i++)
            {
                var cell = new Vector2Int(random.Next(Layout.Width), random.Next(Layout.Height));
                if (cell == Layout.Start || cell == Layout.Goal || !used.Add(cell)) continue;
                var fixture = new GameObject("Dungeon Ceiling Light");
                fixture.transform.SetParent(generatedRoot, false);
                fixture.transform.position = CellToWorld(cell) + Vector3.up * 3.05f;
                var light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.range = level.environment == DungeonCondition.Powered ? 8.5f : 6.5f;
                light.intensity = level.environment == DungeonCondition.Powered ? 1.15f : .78f;
                light.shadows = LightShadows.None;
            }
        }

        private void CreateLocalFogZones(LevelDefinition level)
        {
            if (level.localFogZones <= 0) return;
            fogMaterial ??= CreateFogMaterial();
            var random = new System.Random(level.seed ^ 0xF09);
            var used = new HashSet<Vector2Int>();
            for (var attempt = 0; attempt < level.localFogZones * 8 && used.Count < level.localFogZones; attempt++)
            {
                var cell = new Vector2Int(random.Next(Layout.Width), random.Next(Layout.Height));
                if (cell == Layout.Start || cell == Layout.Goal || !used.Add(cell)) continue;
                var center = CellToWorld(cell);
                fogZones.Add(center);
                var root = new GameObject("Local Fog Zone");
                root.transform.SetParent(generatedRoot, false);
                root.transform.position = center + Vector3.up * 1.1f;
                var particles = root.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.04f, .16f);
                main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
                main.startColor = new Color(.62f,.78f,.72f,.30f);
                main.maxParticles = 130;
                var emission = particles.emission;
                emission.rateOverTime = 20f;
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(CellSize * .88f, 1.9f, CellSize * .88f);
                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = fogMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                particles.Play();
            }
        }

        private static Material CreateFogMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "Local Dungeon Fog" };
            material.color = new Color(.62f,.78f,.72f,.42f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.color);
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            return material;
        }

        private void CreateSafeLight(Vector3 position)
        {
            safeLights.Add(position);
            var post = RuntimeArt.Primitive(PrimitiveType.Cylinder, "Safe Light", generatedRoot,
                position + Vector3.up * 1.5f, new Vector3(0.12f, 1.5f, 0.12f), trimMaterial, false);
            var light = post.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.35f);
            light.range = 9f;
            light.intensity = 2.8f;
        }

        private void CreateMaterials()
        {
            floorMaterial ??= RuntimeArt.MaterialFromResource("FogboundFloor", new Color(0.11f, 0.14f, 0.15f));
            wallMaterial ??= RuntimeArt.MaterialFromResource("FogboundWall", new Color(0.24f, 0.27f, 0.25f));
            trimMaterial ??= RuntimeArt.MaterialFromResource("FogboundHazard", new Color(0.72f, 0.52f, 0.18f));
            wallBaseMaterial ??= RuntimeArt.Material("Wall Base", new Color(0.42f, 0.48f, 0.43f), 0f, 0.18f);
        }

        private void Clear()
        {
            safeLights.Clear();
            fogZones.Clear();
            if (generatedRoot != null)
            {
                generatedRoot.gameObject.SetActive(false);
                Destroy(generatedRoot.gameObject);
            }
        }
    }

    public sealed class MazeEntryTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
                GameDirector.Instance?.BeginRun();
        }
    }

    public sealed class MazeExitTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
                GameDirector.Instance?.CompleteLevel();
        }
    }
}
