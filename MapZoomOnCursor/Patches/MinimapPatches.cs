using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MapZoomOnCursor.Patches
{
    [HarmonyPatch(typeof(Minimap), "UpdateMap")]
    public static class MinimapUpdateMapPatch
    {
        private static readonly FieldInfo LargeZoomField = AccessTools.Field(typeof(Minimap), "m_largeZoom");
        private static readonly FieldInfo MapOffsetField = AccessTools.Field(typeof(Minimap), "m_mapOffset");
        private static readonly MethodInfo CenterMapMethod = AccessTools.Method(typeof(Minimap), "CenterMap", new[] { typeof(Vector3) });

        private static float _zoomBeforeVanilla;
        private static bool _scrollActive;

        [HarmonyPrefix]
        public static void Prefix(Minimap __instance, bool takeInput)
        {
            _scrollActive = false;

            if (!takeInput)
                return;

            if (__instance.m_mode != Minimap.MapMode.Large)
                return;

            if (Minimap.InTextInput())
                return;

            float scroll = ZInput.GetMouseScrollWheel();
            if (scroll == 0f)
                return;

            // Save zoom before vanilla modifies it
            _zoomBeforeVanilla = (float)LargeZoomField.GetValue(__instance);
            _scrollActive = true;
        }

        [HarmonyPostfix]
        public static void Postfix(Minimap __instance, Player player)
        {
            if (!_scrollActive)
                return;
            _scrollActive = false;

            // Vanilla has applied scroll-wheel zoom and called CenterMap.
            // We adjust m_mapOffset so the world point under the cursor stays fixed,
            // then re-call CenterMap so the uvRect updates this frame (no snap).

            float vanillaNewZoom = (float)LargeZoomField.GetValue(__instance);

            if (vanillaNewZoom == _zoomBeforeVanilla)
                return;

            // When zoom changes with the same map center, the world point at normalized
            // cursor position (nx, ny) shifts by:
            //   delta.x = (nx - 0.5) * (z1 - z0) * aspect * textureSize * pixelSize
            //   delta.z = (ny - 0.5) * (z1 - z0) * textureSize * pixelSize
            // We compensate m_mapOffset by the negative of this shift.

            RawImage mapImage = __instance.m_mapImageLarge;
            RectTransform rectTransform = mapImage.transform as RectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, ZInput.mousePosition, null, out Vector2 localPoint))
                return;

            Vector2 normalized = Rect.PointToNormalized(rectTransform.rect, localPoint);

            float nxFromCenter = normalized.x - 0.5f;
            float nyFromCenter = normalized.y - 0.5f;

            float aspect = rectTransform.rect.width / rectTransform.rect.height;
            float zoomDiff = vanillaNewZoom - _zoomBeforeVanilla;
            float scale = __instance.m_textureSize * __instance.m_pixelSize;

            float deltaWorldX = nxFromCenter * zoomDiff * aspect * scale;
            float deltaWorldZ = nyFromCenter * zoomDiff * scale;

            Vector3 offset = (Vector3)MapOffsetField.GetValue(__instance);
            offset.x -= deltaWorldX;
            offset.z -= deltaWorldZ;
            MapOffsetField.SetValue(__instance, offset);

            // Re-center the map so the uvRect reflects our new offset this frame
            CenterMapMethod.Invoke(__instance, new object[] { player.transform.position + offset });
        }
    }
}
