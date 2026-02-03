using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace MapZoomOnCursor
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MapZoomOnCursorPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.github.mapzoomoncursor";
        public const string PluginName = "MapZoomOnCursor";
        public const string PluginVersion = "1.0.0";

        private Harmony _harmony;

        private void Awake()
        {
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
