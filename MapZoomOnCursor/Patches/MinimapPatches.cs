using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MapZoomOnCursor.Patches
{
    [HarmonyPatch(typeof(Minimap), "UpdateMap")]
    public static class MinimapUpdateMapPatch
    {
        private static readonly FieldInfo LargeZoomField = AccessTools.Field(typeof(Minimap), "m_largeZoom");
        private static readonly FieldInfo MapOffsetField = AccessTools.Field(typeof(Minimap), "m_mapOffset");
        private static readonly MethodInfo CenterMapMethod = AccessTools.Method(typeof(Minimap), "CenterMap", new[] { typeof(Vector3) });
        private static readonly MethodInfo ScreenToWorldPointMethod = AccessTools.Method(typeof(Minimap), "ScreenToWorldPoint", new[] { typeof(Vector3) });

        private static float _zoomBeforeVanilla;
        private static Vector3 _worldBeforeZoom;
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

            _zoomBeforeVanilla = (float)LargeZoomField.GetValue(__instance);

            // Capture cursor world point BEFORE vanilla changes the zoom.
            // The uvRect still reflects last frame's CenterMap call, matching current zoom.
            _worldBeforeZoom = (Vector3)ScreenToWorldPointMethod.Invoke(__instance, new object[] { (Vector3)ZInput.mousePosition });
            _scrollActive = true;
        }

        [HarmonyPostfix]
        public static void Postfix(Minimap __instance, Player player)
        {
            if (!_scrollActive)
                return;
            _scrollActive = false;

            float vanillaNewZoom = (float)LargeZoomField.GetValue(__instance);

            if (vanillaNewZoom == _zoomBeforeVanilla)
                return;

            // Vanilla has applied zoom and called CenterMap (updating uvRect for new zoom).
            // Get cursor world point AFTER zoom change at the same screen position.
            Vector3 worldAfterZoom = (Vector3)ScreenToWorldPointMethod.Invoke(__instance, new object[] { (Vector3)ZInput.mousePosition });

            // Zoom in: correct offset so the cursor point stays fixed.
            // Zoom out: no correction (vanilla center-zoom behavior).
            if (vanillaNewZoom > _zoomBeforeVanilla)
                return;

            Vector3 diff = _worldBeforeZoom - worldAfterZoom;
            Vector3 offset = (Vector3)MapOffsetField.GetValue(__instance);
            offset += diff;
            MapOffsetField.SetValue(__instance, offset);

            // Re-center the map so the uvRect reflects our new offset this frame
            CenterMapMethod.Invoke(__instance, new object[] { player.transform.position + offset });
        }
    }
}
