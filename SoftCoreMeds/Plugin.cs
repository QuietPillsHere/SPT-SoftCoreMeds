using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.UI;
using SoftCoreMeds.Configuration;
using SoftCoreMeds.Patch;
using static GClass2175;

namespace SoftCoreMeds
{
    [BepInPlugin(PluginInfo.PluginID, PluginInfo.Name, PluginInfo.PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableLog { get; set; }

        public static ConfigEntry<bool> EnableSurgeryPatch { get; set; }

        public static ConfigEntry<bool> EnableStimulatorPatch { get; set; }

        public static ConfigEntry<int> StimRestorePercent { get; set;}

        private void Awake()
        {
            // BepIn Configuration Init
            InitialConfiguration();

            // BepIn Plugin Init
            new PatchSurgeryRestoreByBatch().Enable();
            new PatchWhenItemOnClick().Enable();
            new PatchStimulatorRestoreLimb().Enable();
            new PatchStimulatorLimbSelector().Enable();
#if DEBUG
            //Defind Console Commands For Debug :D
            ConsoleScreen.Processor.RegisterCommandGroup<DebugCommands>();
#endif
        }

        public void InitialConfiguration()
        {
            EnableLog = Config.Bind(
                "1. Settings",
                "Enable Debug Log",
                false,
                new ConfigDescription("Disable by default to abvoid log spam, turn it on when you want to submit isss on github")
            );

            EnableSurgeryPatch = Config.Bind(
                "1. Settings",
                "Enable Surgery Feature",
                true,
                new ConfigDescription("Enable for bactch restore blackout limb when using surgical kit, only apply to Sur12 and CMS")
            );

            EnableStimulatorPatch = Config.Bind(
                "1. Settings",
                "Enable Stimulator Feature",
                true,
                new ConfigDescription("Enable for restore blackout limb when using stimualtor by select limb, only apply to eTG-change")
            );

            StimRestorePercent = Config.Bind(
                "2. Balance",
                "Restore Limb Max Health Percent",
                30,
                new ConfigDescription(
                    "Percent For Using STIM To Heal Destory Body Part",
                    new AcceptableValueRange<int>(30, 70)
                )
            );

#if DEBUG
            EnableLog = Config.Bind(
                "1. Settings",
                "Debug Mode",
                true,
                new ConfigDescription(
                    "This Plugin current running on debug mod",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        ReadOnly = true
                    }
                )
            );

#endif
        }

    }
}
