using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using LoggerInstance = BepInEx.Logging.Logger;

namespace SoftCoreMeds.Patch
{
    /// <summary>
    /// Make STIM great again: add limb selector in quick slot for stim usage 
    /// </summary>
    internal class PatchStimulatorLimbSelector : BasePatchModule
    {
        /// <summary>
        /// Overwrite default EFT Meds Consum Imp
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            IsPatchByPreFix = false;
            return AccessTools.Method(
                typeof(HealingLimbSelector),
                nameof(HealingLimbSelector.TryGetLimbsToHealByItem),
                new Type[] { typeof(Item), typeof(IHealthController), typeof(List<EBodyPart>).MakeByRefType() }
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchStimulatorLimbSelector));

        [PatchPostfix]
        public static void Postfix(HealingLimbSelector __instance, Item item, IHealthController healthController, ref List<EBodyPart>? result, ref bool __result)
        {
            if (!Plugin.EnableStimulatorPatch.Value)
            {
                return;
            }

            result ??= new List<EBodyPart>();

            DebugLog($"Init, param [Item = {item.StringTemplateId}, Name = {item.Name}, BodyPart = {string.Join("|", result.Select(_ => _.ToString()))}, ItemType = {item.GetType().FullName}]");

            if (!Plugin.Check4PatchStimId(item.StringTemplateId))
            {
                DebugLog("skip none patch stim");
                return;
            }

            if (result?.Count > 0)
            {
                DebugLog("skip unkown condition");
                return;
            }

            foreach (var bodyPart in GClass3058.RealBodyParts)
            {
                if (healthController.IsBodyPartDestroyed(bodyPart))
                {
                    result?.Add(bodyPart);
                }
            }

            __result = result?.Count > 0;

            DebugLog($"execute complete");
        }

    }
}
