using UnityEngine;
using UnityEngine.InputSystem;

namespace FogboundMaze
{
    public sealed class GameInput : MonoBehaviour
    {
        private Vector2 mobileMove;
        private Vector2 mobileLook;
        private bool mobileAttack;
        private bool mobileSprint;
        private bool mobileReload;
        private bool mobileGun;
        private bool mobileBlade;
        private bool mobileToggle;
        private bool mobilePause;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool SelectGunPressed { get; private set; }
        public bool SelectBladePressed { get; private set; }
        public bool ToggleViewPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool SprintHeld { get; private set; }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var keyboardMove = Vector2.zero;
            if (keyboard != null)
            {
                keyboardMove.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                keyboardMove.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            }

            Move = Vector2.ClampMagnitude(keyboardMove + mobileMove, 1f);
            Look = (mouse != null ? mouse.delta.ReadValue() * 0.075f : Vector2.zero) + ConsumeMobileLook();
            AttackHeld = (mouse != null && mouse.leftButton.isPressed) || mobileAttack;
            AttackPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame) || mobileAttack;
            ReloadPressed = (keyboard != null && keyboard.rKey.wasPressedThisFrame) || Consume(ref mobileReload);
            var scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            SelectGunPressed = (keyboard != null && keyboard.digit1Key.wasPressedThisFrame)
                || scroll > 0.01f || Consume(ref mobileGun);
            SelectBladePressed = (keyboard != null && keyboard.digit2Key.wasPressedThisFrame)
                || scroll < -0.01f || Consume(ref mobileBlade);
            ToggleViewPressed = (keyboard != null && keyboard.vKey.wasPressedThisFrame) || Consume(ref mobileToggle);
            PausePressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || Consume(ref mobilePause);
            SprintHeld = (keyboard != null && keyboard.leftShiftKey.isPressed) || mobileSprint;
        }

        public void SetMobileMove(Vector2 value) => mobileMove = Vector2.ClampMagnitude(value, 1f);
        public void ResetMobile()
        {
            mobileMove = mobileLook = Vector2.zero;
            mobileAttack = mobileSprint = mobileReload = mobileGun = mobileBlade = mobileToggle = mobilePause = false;
        }
        public void AddMobileLook(Vector2 value) => mobileLook += value;
        public void SetMobileAttack(bool value) => mobileAttack = value;
        public void SetMobileSprint(bool value) => mobileSprint = value;
        public void PressMobileReload() => mobileReload = true;
        public void PressMobileGun() => mobileGun = true;
        public void PressMobileBlade() => mobileBlade = true;
        public void PressMobileToggle() => mobileToggle = true;
        public void PressMobilePause() => mobilePause = true;

        private Vector2 ConsumeMobileLook()
        {
            var value = mobileLook;
            mobileLook = Vector2.zero;
            return value;
        }

        private static bool Consume(ref bool value)
        {
            var current = value;
            value = false;
            return current;
        }
    }
}
