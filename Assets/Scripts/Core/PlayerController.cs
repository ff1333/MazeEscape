using UnityEngine;

namespace FogboundMaze
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        private CharacterController controller;
        private GameInput input;
        private CameraRig cameraRig;
        private float verticalSpeed;
        private Vector3 lastGroundedPosition;
        private float nextFootstepNoise;

        public Health Health { get; private set; }
        public WeaponController Weapon { get; private set; }

        public void Initialize(GameInput gameInput, CameraRig rig)
        {
            input = gameInput;
            cameraRig = rig;
            controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Health.ResetHealth(100f);
            Weapon = gameObject.AddComponent<WeaponController>();
            Weapon.Initialize(this, rig.Camera);
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalSpeed = 0f;
            lastGroundedPosition = position;
        }

        private void Update()
        {
            if (input == null || GameDirector.Instance == null)
            {
                return;
            }

            if (GameDirector.Instance.Phase != GamePhase.Playing
                && !(GameDirector.Instance.Phase == GamePhase.Staging && GameDirector.Instance.LoadoutConfirmed))
            {
                return;
            }
            cameraRig.TickLook(input.Look, input.ToggleViewPressed);

            // Recovery is a fallback for an unexpected physics escape, not a substitute for walls.
            if (transform.position.y < -5f)
                Teleport(lastGroundedPosition + Vector3.up * 0.1f);

            var rotation = Quaternion.Euler(0f, cameraRig.Yaw, 0f);
            var direction = rotation * new Vector3(input.Move.x, 0f, input.Move.y);
            var speed = input.SprintHeld ? 7.2f : 4.8f;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Mathf.Clamp01(15f * Time.deltaTime));

            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (controller.isGrounded && Physics.Raycast(transform.position + Vector3.up * 0.2f,
                Vector3.down, out var ground, 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && ground.normal.y > 0.9f)
                lastGroundedPosition = transform.position;

            if (input.SprintHeld && direction.sqrMagnitude > .1f && controller.isGrounded
                && GameDirector.Instance.Phase == GamePhase.Playing && Time.time >= nextFootstepNoise)
            {
                nextFootstepNoise = Time.time + .48f;
                NoiseSystem.Emit(transform.position, 10f, NoiseKind.Sprint, this);
            }

            if (input.SelectGunPressed) Weapon.Equip(WeaponType.SniperRifle);
            if (input.SelectBladePressed) Weapon.Equip(WeaponType.LongBlade);
            Weapon.Tick(input.AttackHeld, input.AttackPressed, input.ReloadPressed, input.AimHeld);
        }
    }
}
