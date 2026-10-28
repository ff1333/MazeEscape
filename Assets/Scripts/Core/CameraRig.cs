using UnityEngine;

namespace FogboundMaze
{
    public sealed class CameraRig : MonoBehaviour
    {
        private Transform target;
        private Camera viewCamera;
        private GameObject playerVisual;
        private float yaw;
        private float pitch = 4f;
        private bool firstPerson;
        private bool menuView;
        private bool aiming;

        private const float DefaultFieldOfView = 68f;
        private const float ScopeFieldOfView = 24f;

        public Camera Camera => viewCamera;
        public float Yaw => yaw;
        public bool IsFirstPerson => firstPerson;
        public bool IsAiming => aiming;
        public bool UsesFirstPersonView => firstPerson || aiming;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
            playerVisual = target.Find("Visual")?.gameObject;
            if (!TryGetComponent(out viewCamera))
            {
                viewCamera = gameObject.AddComponent<Camera>();
            }
            if (!TryGetComponent<AudioListener>(out _))
            {
                gameObject.AddComponent<AudioListener>();
            }
            gameObject.tag = "MainCamera";
            viewCamera.fieldOfView = DefaultFieldOfView;
            viewCamera.nearClipPlane = 0.03f;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = new Color(0.075f, 0.13f, 0.145f);
            var flashlightObject = new GameObject("Flashlight");
            flashlightObject.transform.SetParent(transform, false);
            var flashlight = flashlightObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.color = new Color(0.84f, 0.92f, 0.83f);
            flashlight.range = 21f;
            flashlight.spotAngle = 62f;
            flashlight.innerSpotAngle = 38f;
            flashlight.intensity = 1.55f;
            flashlight.shadows = LightShadows.Soft;
            yaw = target.eulerAngles.y;
            ResetView();
        }

        public void ResetView()
        {
            menuView = false;
            yaw = target.eulerAngles.y;
            pitch = 4f;
            firstPerson = false;
            aiming = false;
            if (playerVisual != null) playerVisual.SetActive(true);
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + Vector3.up * 1.55f;
            transform.SetPositionAndRotation(focus + rotation * new Vector3(0.55f, 0.15f, -3.2f), rotation);
        }

        public void ShowMenuView(Vector3 stage)
        {
            menuView = true;
            firstPerson = false;
            aiming = false;
            if (playerVisual != null) playerVisual.SetActive(true);
            transform.position = stage + new Vector3(-1.65f, 1.9f, -2.3f);
            transform.LookAt(stage + new Vector3(0.8f, 1.05f, 2.5f));
        }

        public void TickLook(Vector2 look, bool toggle)
        {
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -45f, 60f);
            if (toggle)
            {
                firstPerson = !firstPerson;
            }
        }

        public void SetAiming(bool value)
        {
            aiming = value;
        }

        private void LateUpdate()
        {
            if (target == null || menuView)
            {
                return;
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + Vector3.up * 1.55f;
            viewCamera.fieldOfView = Mathf.MoveTowards(viewCamera.fieldOfView,
                aiming ? ScopeFieldOfView : DefaultFieldOfView, 110f * Time.unscaledDeltaTime);
            if (UsesFirstPersonView)
            {
                if (playerVisual != null && playerVisual.activeSelf) playerVisual.SetActive(false);
                // Keep the eye inside the CharacterController capsule. The previous 0.5 m
                // forward offset could cross a wall before the player's collider did.
                transform.SetPositionAndRotation(focus + rotation * Vector3.forward * 0.04f, rotation);
                return;
            }

            if (playerVisual != null && !playerVisual.activeSelf) playerVisual.SetActive(true);

            var offset = rotation * new Vector3(0.55f, 0.15f, -3.2f);
            var distance = offset.magnitude;
            var desiredDistance = distance;
            if (Physics.SphereCast(focus, 0.18f, offset / distance, out var hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                desiredDistance = Mathf.Max(0.35f, hit.distance - 0.15f);
            }

            var desired = focus + offset.normalized * desiredDistance;
            transform.position = Vector3.Lerp(transform.position, desired, Mathf.Clamp01(18f * Time.unscaledDeltaTime));
            transform.rotation = rotation;
        }
    }
}
