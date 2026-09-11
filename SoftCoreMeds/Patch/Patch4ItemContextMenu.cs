using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SoftCoreMeds.Component;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using static EFT.Player;
using static EFT.Player.MedsController;
using static UnityEngine.EventSystems.PointerEventData;
using LoggerInstance = BepInEx.Logging.Logger;

namespace SoftCoreMeds.Patch
{
    /// <summary>
    /// Patch inventory item context menu click event, to add logics for surgical kit and stim
    /// </summary>
    internal class Patch4ItemContextMenu : BasePatchModule
    {
        /// <summary>
        /// add logics to default EFT item click event
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            IsPatchByPreFix = true;
            return AccessTools.Method(
                typeof(InventoryItemContextInteractions), // class
                nameof(InventoryItemContextInteractions.ExecuteInteractionInternal), // method
                new Type[] { typeof(EItemInfoButton) } // parameter
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchWhenItemOnClick));

        /// <summary>
        /// imp
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="button"></param>
        /// <param name="position"></param>
        /// <param name="doubleClick"></param>
        [PatchPrefix]
        public static void Prefix(InventoryItemContextInteractions __instance, EItemInfoButton interaction)
        {
            if (!Plugin.EnableSurgeryPatch.Value && !Plugin.EnableStimulatorPatch.Value)
            {
                return;
            }

            if (!IsSurgeryKit(__instance.Item.StringTemplateId))
            {
                DebugLog("Skip for none patch item");
                return;
            }

            if (__instance.Item.TryGetItemComponent<UIContextComponent>(out var component))
            {
                //flag for downstream patch method (PatchSurgeryRestoreByBatch)
                component.ConsumMethod = interaction;
            }
            else
            {
                __instance.Item.Components.Add(new UIContextComponent 
                {
                    ConsumMethod = interaction,
                });
            }

            DebugLog("Complete");
        }
    }
}
