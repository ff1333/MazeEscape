using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FogboundMaze.Tests
{
    public sealed class MobileHudLayoutTests
    {
        [UnityTest]
        public IEnumerator MoveAreaUsesOnlyLeftHalfAndStartsHidden()
        {
            var canvasObject = new GameObject(
                "Test Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            var inputObject = new GameObject("Input", typeof(GameInput));

            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                MobileHudBuilder.Build(
                    canvasObject.transform,
                    inputObject.GetComponent<GameInput>(),
                    Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                yield return null;

                var area = canvasObject.transform.Find("Move Touch Area")
                    .GetComponent<RectTransform>();
                var visuals = area.Find("Move Stick Visuals");

                Assert.That(area.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(area.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
                Assert.That(visuals.gameObject.activeSelf, Is.False);

                var pointer = new PointerEventData(null)
                {
                    position = RectTransformUtility.WorldToScreenPoint(
                        null,
                        area.TransformPoint(Vector3.zero))
                };
                var stick = area.GetComponent<MobileStick>();
                stick.OnPointerDown(pointer);
                Assert.That(visuals.gameObject.activeSelf, Is.True);

                pointer.position += new Vector2(60f, 0f);
                stick.OnDrag(pointer);
                yield return null;
                Assert.That(inputObject.GetComponent<GameInput>().Move.x,
                    Is.GreaterThan(0f));

                stick.OnPointerUp(pointer);
                yield return null;
                Assert.That(visuals.gameObject.activeSelf, Is.False);
                Assert.That(inputObject.GetComponent<GameInput>().Move,
                    Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.Destroy(canvasObject);
                Object.Destroy(inputObject);
            }
        }

        [UnityTest]
        public IEnumerator CombatButtonsOccupySeparatePositions()
        {
            var canvasObject = new GameObject(
                "Test Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            var inputObject = new GameObject("Input", typeof(GameInput));

            try
            {
                canvasObject.GetComponent<Canvas>().renderMode =
                    RenderMode.ScreenSpaceOverlay;
                MobileHudBuilder.Build(
                    canvasObject.transform,
                    inputObject.GetComponent<GameInput>(),
                    Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                yield return null;

                string[] names =
                {
                    "ATTACK", "SCOPE", "RELOAD", "SNIPER",
                    "BLADE", "SPRINT", "VIEW"
                };

                for (int i = 0; i < names.Length; i++)
                {
                    RectTransform first = canvasObject.transform.Find(names[i])
                        .GetComponent<RectTransform>();
                    for (int j = i + 1; j < names.Length; j++)
                    {
                        RectTransform second = canvasObject.transform.Find(names[j])
                            .GetComponent<RectTransform>();
                        Assert.That(
                            RectTransformUtility.RectangleContainsScreenPoint(
                                first,
                                second.position),
                            Is.False,
                            $"{names[j]} center overlaps {names[i]}");
                    }
                }
            }
            finally
            {
                Object.Destroy(canvasObject);
                Object.Destroy(inputObject);
            }
        }
    }
}
