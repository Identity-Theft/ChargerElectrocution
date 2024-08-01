using BepInEx;
using BepInEx.Logging;
using ChargerElectrocution.Patches;

namespace ChargerElectrocution
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class ChargerElectrocution : BaseUnityPlugin
    {
        public static ChargerElectrocution Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ItemChargerElectrocution.Load();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }
    }
}