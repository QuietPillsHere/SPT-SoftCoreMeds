using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
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
    /// Make Surgical Kit great again
    /// </summary>
    internal class PatchWhenItemOnClick : BasePatchModule
    {
        /// <summary>
        /// add logics to default EFT item click event
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod() 
        {
            IsPatchByPreFix = true;
            return AccessTools.Method(
                typeof(GridItemView), // class
                nameof(GridItemView.OnClick), // method
                new Type[] { typeof(InputButton), typeof(Vector2), typeof(bool) } // parameter
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchWhenItemOnClick));

        /// <summary>
        /// Item Id: Surv12 field surgical kit 
        /// </summary>
        private const string _surv12Kit = "5d02797c86f774203f38e30a";

        /// <summary>
        /// Item Id: CMS surgical kit
        /// </summary>
        private const string _cmsKit = "5d02778e86f774203e7dedbe";

        /// <summary>
        /// imp
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="button"></param>
        /// <param name="position"></param>
        /// <param name="doubleClick"></param>
        [PatchPrefix]
        public static void Prefix(GridItemView __instance, InputButton button, Vector2 position, bool doubleClick)
        {
            if (!Plugin.EnableSurgeryPatch.Value)
            {
                return;
            }

            Logger.LogInfo($"Init, buttons = {button}, doubleClick = {doubleClick}");

            if (!IsPatchItem(__instance.Item.StringTemplateId))
            {
                DebugLog("Skip for none patch item");
                return;
            }

            // add mount clic event data to item context, for later use in other patch method (PatchSurgeryRestoreByBatch)
            if (__instance.Item.TryGetItemComponent<UIContextComponent>(out var component))
            {
                DebugLog("create UIContextComponent");
                component.input = button;
                component.DoubleClick = doubleClick;
            }
            else
            {
                DebugLog("update UIContextComponent");
                __instance.Item.Components.Add(new UIContextComponent(__instance.Item, button, doubleClick));
            }

            DebugLog("Complete");
        }

        public static bool IsPatchItem(string itemTemplateId)
        {
            return itemTemplateId == _surv12Kit || itemTemplateId == _cmsKit;
        }

    }
}
