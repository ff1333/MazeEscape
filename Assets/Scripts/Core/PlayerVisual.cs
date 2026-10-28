using System.Linq;
using UnityEngine;

namespace FogboundMaze
{
    public sealed class PlayerVisual : MonoBehaviour
    {
        private Animation animationPlayer;
        private PlayerController player;
        private GameObject pistol;
        private GameObject knife;
        private GameObject backRifle;
        private GameObject backBlade;
        private Transform upperArmLeft;
        private Transform lowerArmLeft;
        private Transform palmLeft;
        private Transform upperArmRight;
        private Transform lowerArmRight;
        private Transform palmRight;
        private Vector3 previousPosition;
        private float attackUntil;
        private Vector3 gunMuzzleLocal;
        public Transform HeldWeapon => (pistol != null && pistol.activeSelf ? pistol : knife)?.transform;
        public Vector3 GunMuzzle
        {
            get
            {
                return pistol.transform.TransformPoint(gunMuzzleLocal);
            }
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            animationPlayer = GetComponentInChildren<Animation>(true);
            var visual = transform.Find("Visual");
            // The source rig is left-handed. Mirror only its graphics and authored animation.
            visual.localScale = new Vector3(-1f, 1f, 1f);
            foreach (var child in visual.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Rifle") pistol = child.gameObject;
                if (child.name == "Knife")
                {
                    knife = child.gameObject;
                    var mesh = child.GetComponent<MeshFilter>().sharedMesh;
                    var size = mesh.bounds.size;
                    var scale = Vector3.one;
                    var axis = size.x > size.y ? 0 : 1;
                    if (size.z > size[axis]) axis = 2;
                    scale[axis] = 1.8f;
                    child.localScale = Vector3.Scale(child.localScale, scale);
                }
            }
            var torso = visual.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "Torso") ?? visual;
            upperArmLeft = FindBone(visual, "UpperArm.L");
            lowerArmLeft = FindBone(visual, "LowerArm.L");
            palmLeft = FindBone(visual, "Middle1.L");
            upperArmRight = FindBone(visual, "UpperArm.R");
            lowerArmRight = FindBone(visual, "LowerArm.R");
            palmRight = FindBone(visual, "Middle1.R");
            backRifle = CreateBackWeapon("Weapons/SniperRifle", torso,
                new Vector3(-0.24f, 0.02f, -0.18f), Quaternion.Euler(12f, 8f, 48f));
            backBlade = CreateBackWeapon("Weapons/LongBlade", torso,
                new Vector3(0.24f, 0.01f, -0.16f), Quaternion.Euler(6f, 178f, -42f));
            // Bake the socket in the editor; imported meshes are not CPU-readable in release players.
            gunMuzzleLocal = pistol.transform.Find("Muzzle").localPosition;
            previousPosition = transform.position;
        }

        public void Equip(WeaponType weapon)
        {
            if (pistol != null) pistol.SetActive(weapon == WeaponType.SniperRifle);
            if (knife != null) knife.SetActive(weapon == WeaponType.LongBlade);
            if (backRifle != null) backRifle.SetActive(weapon == WeaponType.LongBlade);
            if (backBlade != null) backBlade.SetActive(weapon == WeaponType.SniperRifle);
            attackUntil = 0f;
        }

        public void Attack()
        {
            if (player.Weapon.Type != WeaponType.LongBlade || animationPlayer == null) return;
            attackUntil = Time.time + 0.55f;
            animationPlayer["Slash"].speed = animationPlayer["Slash"].length / 0.55f;
            animationPlayer.CrossFade("Slash", 0.08f);
        }

        public void BoltAction(float duration)
        {
            if (player.Weapon.Type != WeaponType.SniperRifle || animationPlayer == null
                || animationPlayer["Idle_Attack"] == null) return;
            attackUntil = Time.time + duration;
            animationPlayer["Idle_Attack"].speed = animationPlayer["Idle_Attack"].length / duration;
            animationPlayer.CrossFade("Idle_Attack", 0.05f);
        }

        private void LateUpdate()
        {
            if (animationPlayer == null || player.Health == null) return;
            var delta = transform.position - previousPosition;
            delta.y = 0f;
            previousPosition = transform.position;
            var speed = delta.magnitude > 2f ? 0f : delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            if (Time.time >= attackUntil || player.Health.IsDead)
            {
                var suffix = player.Weapon.Type is WeaponType.SniperRifle or WeaponType.LongBlade ? "_Gun" : string.Empty;
                var clip = player.Health.IsDead ? "Death" : speed > 5.8f ? "Run" + suffix
                    : speed > 0.1f ? "Walk" + suffix : "Idle" + suffix;
                if (!animationPlayer.IsPlaying(clip)) animationPlayer.CrossFade(clip, 0.12f);
            }
            if (!player.Health.IsDead) ApplySupportHandPose();
        }

        private void ApplySupportHandPose()
        {
            var held = HeldWeapon;
            if (held == null || player.Weapon.Type == WeaponType.None) return;
            var leftDistance = palmLeft != null ? Vector3.Distance(palmLeft.position, held.position) : float.MaxValue;
            var rightDistance = palmRight != null ? Vector3.Distance(palmRight.position, held.position) : float.MaxValue;
            var upper = leftDistance >= rightDistance ? upperArmLeft : upperArmRight;
            var lower = leftDistance >= rightDistance ? lowerArmLeft : lowerArmRight;
            var palm = leftDistance >= rightDistance ? palmLeft : palmRight;
            if (upper == null || lower == null || palm == null) return;

            Vector3 target;
            if (player.Weapon.Type == WeaponType.SniperRifle)
            {
                var muzzle = GunMuzzle;
                target = Vector3.Lerp(held.position, muzzle, 0.42f) - transform.up * 0.025f;
            }
            else
            {
                target = held.position + transform.up * 0.055f + transform.forward * 0.025f;
            }
            for (var iteration = 0; iteration < 2; iteration++)
            {
                RotateJointToward(lower, palm, target);
                RotateJointToward(upper, palm, target);
            }
        }

        private static void RotateJointToward(Transform joint, Transform end, Vector3 target)
        {
            var current = end.position - joint.position;
            var desired = target - joint.position;
            if (current.sqrMagnitude < 0.000001f || desired.sqrMagnitude < 0.000001f) return;
            joint.rotation = Quaternion.FromToRotation(current, desired) * joint.rotation;
        }

        private static Transform FindBone(Transform root, string name)
            => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);

        private static GameObject CreateBackWeapon(string resource, Transform parent, Vector3 position, Quaternion rotation)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) return null;
            var weapon = Instantiate(prefab, parent);
            weapon.name = "Back " + prefab.name;
            weapon.transform.localPosition = position;
            weapon.transform.localRotation = rotation;
            weapon.transform.localScale = Vector3.one * 0.9f;
            foreach (var child in weapon.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            return weapon;
        }
    }
}
