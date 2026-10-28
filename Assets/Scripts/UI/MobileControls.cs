using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FogboundMaze
{
    public enum MobileAction
    {
        Attack,
        Aim,
        Sprint,
        Reload,
        Gun,
        Blade,
        Toggle,
        Pause
    }

    public static class MobileHudBuilder
    {
        public static void Build(Transform canvas, GameInput input, Font font)
        {
            CreateLookArea(canvas, input);
            CreateStick(canvas, input);
            CreateButton(canvas, input, font, "FIRE", MobileAction.Attack, new Vector2(0.89f, 0.18f), 150f);
            CreateButton(canvas, input, font, "SCOPE", MobileAction.Aim, new Vector2(0.67f, 0.18f), 110f);
            CreateButton(canvas, input, font, "RUN", MobileAction.Sprint, new Vector2(0.77f, 0.12f), 110f);
            CreateButton(canvas, input, font, "R", MobileAction.Reload, new Vector2(0.93f, 0.36f), 92f);
            CreateButton(canvas, input, font, "GUN", MobileAction.Gun, new Vector2(0.72f, 0.36f), 92f);
            CreateButton(canvas, input, font, "BLADE", MobileAction.Blade, new Vector2(0.82f, 0.36f), 92f);
            CreateButton(canvas, input, font, "VIEW", MobileAction.Toggle, new Vector2(0.82f, 0.50f), 86f);
            CreateButton(canvas, input, font, "II", MobileAction.Pause, new Vector2(0.96f, 0.93f), 74f);
        }

        private static void CreateLookArea(Transform parent, GameInput input)
        {
            var root = new GameObject("Look Area", typeof(RectTransform), typeof(Image), typeof(MobileLookArea));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = Color.clear;
            root.GetComponent<MobileLookArea>().Initialize(input);
            root.transform.SetAsFirstSibling();
        }

        private static void CreateStick(Transform parent, GameInput input)
        {
            var root = new GameObject("Move Stick", typeof(RectTransform), typeof(Image), typeof(MobileStick));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.14f, 0.18f);
            rect.sizeDelta = new Vector2(240f, 240f);
            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            knob.transform.SetParent(root.transform, false);
            knob.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 92f);
            knob.GetComponent<Image>().color = new Color(0.2f, 0.78f, 0.62f, 0.55f);
            root.GetComponent<MobileStick>().Initialize(input, knob.GetComponent<RectTransform>());
        }

        private static void CreateButton(Transform parent, GameInput input, Font font, string label, MobileAction action, Vector2 anchor, float size)
        {
            var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(MobileActionButton));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = Vector2.one * size;
            root.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.08f, 0.58f);
            var textRoot = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textRoot.transform.SetParent(root.transform, false);
            var textRect = textRoot.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textRoot.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 25;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            root.GetComponent<MobileActionButton>().Initialize(input, action);
        }
    }

    public sealed class MobileStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private GameInput input;
        private RectTransform root;
        private RectTransform knob;

        public void Initialize(GameInput target, RectTransform knobTransform)
        {
            input = target;
            root = (RectTransform)transform;
            knob = knobTransform;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out var point)) return;
            var radius = root.rect.width * 0.36f;
            var value = Vector2.ClampMagnitude(point / radius, 1f);
            knob.anchoredPosition = value * radius;
            input.SetMobileMove(value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            knob.anchoredPosition = Vector2.zero;
            input.SetMobileMove(Vector2.zero);
        }
    }

    public sealed class MobileLookArea : MonoBehaviour, IDragHandler
    {
        private GameInput input;
        public void Initialize(GameInput target) => input = target;
        public void OnDrag(PointerEventData eventData) => input.AddMobileLook(eventData.delta * 0.09f);
    }

    public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private GameInput input;
        private MobileAction action;

        public void Initialize(GameInput target, MobileAction mobileAction)
        {
            input = target;
            action = mobileAction;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            switch (action)
            {
                case MobileAction.Attack: input.SetMobileAttack(true); break;
                case MobileAction.Aim: input.SetMobileAim(true); break;
                case MobileAction.Sprint: input.SetMobileSprint(true); break;
                case MobileAction.Reload: input.PressMobileReload(); break;
                case MobileAction.Gun: input.PressMobileGun(); break;
                case MobileAction.Blade: input.PressMobileBlade(); break;
                case MobileAction.Toggle: input.PressMobileToggle(); break;
                case MobileAction.Pause: input.PressMobilePause(); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (action == MobileAction.Attack) input.SetMobileAttack(false);
            if (action == MobileAction.Aim) input.SetMobileAim(false);
            if (action == MobileAction.Sprint) input.SetMobileSprint(false);
        }
    }
}
