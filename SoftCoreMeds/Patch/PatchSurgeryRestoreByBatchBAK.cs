using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static EFT.Player;
using static EFT.Player.MedsController;
using LoggerInstance = BepInEx.Logging.Logger;

namespace SoftCoreMeds.Patch
{
    /// <summary>
    /// Make Surgical Kit great again
    /// </summary>
    internal class PatchSurgeryRestoreByBatchBAK : ModulePatch
    {
        /// <summary>
        /// Overwrite default EFT Meds Consum Imp
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod() 
        {
            return AccessTools.Method(
                typeof(ObservedMedsControllerClass), // class
                nameof(ObservedMedsControllerClass.Start), // method
                new Type[] { typeof(GStruct382<EBodyPart>), typeof(float), typeof(Action) } // parameter
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchSurgeryRestoreByBatch));

        /// <summary>
        /// Item Id: Surv12 field surgical kit 
        /// </summary>
        private const string _surv12Kit = "5d02797c86f774203f38e30a";

        /// <summary>
        /// Item Id: CMS surgical kit
        /// </summary>
        private const string _cmsKit = "5d02778e86f774203e7dedbe";

        [PatchPrefix]
        public static void Prefix(ObservedMedsControllerClass __instance, GStruct382<EBodyPart> bodyParts, float amount, Action callback)
        {
            Logger.LogInfo("PrePatch: Init");

            if (!(__instance.MedsController_0.Item is MedicalItemClass))
            {
                // skip food and drink
                Logger.LogInfo("PrePatch: skip food and drink, or stim");
                return;
            }

            var surgicalItem = __instance.MedsController_0.Item as MedicalItemClass;
            if (surgicalItem?.TemplateId.StringID != _surv12Kit && surgicalItem?.TemplateId.StringID != _cmsKit)
            {
                // skip other stim
                Logger.LogInfo("PrePatch: skip for none surgical kit");
                return;
            }

            //__instance.MedsController_0.OnOutUseEvent += () => 
            //{
            //    bodyParts.Length
            //};

            Logger.LogInfo("PrePatch: Complete");
            return;
        }

        public static bool IsPatchItem(string itemTemplateId)
        {
            return itemTemplateId == _surv12Kit || itemTemplateId == _cmsKit;
        }

    }
}
