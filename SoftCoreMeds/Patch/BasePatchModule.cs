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

namespace SoftCoreMeds.Patch
{
    internal class BasePatchModule : ModulePatch
    {
        internal static bool IsPatchByPreFix { get; set; }

        /// <summary>
        /// STIM Item Id: eTG 
        /// </summary>
        private const string _defaultPatchStimItemId = "5c0e534186f7747fa1419867";

        /// <summary>
        /// Item Id: Surv12 field surgical kit 
        /// </summary>
        internal const string Surv12Kit = "5d02797c86f774203f38e30a";

        /// <summary>
        /// Item Id: CMS surgical kit
        /// </summary>
        internal const string CmsKit = "5d02778e86f774203e7dedbe";

        protected override MethodBase GetTargetMethod() 
        {
            throw new NotImplementedException();
        }

        public static bool IsSurgeryKit(string itemTemplateId)
        {
            return itemTemplateId == Surv12Kit || itemTemplateId == CmsKit;
        }

        public static bool IsRegenStim(string itemTemplateId)
        {
            string patchStimId = string.IsNullOrEmpty(Plugin.OverWriteStimId.Value) ? _defaultPatchStimItemId : Plugin.OverWriteStimId.Value.Trim();
            return patchStimId == itemTemplateId;
        }

        public static void DebugLog(string logContent)
        {
            if (Plugin.EnableLog.Value)
            {
                string logFlag = IsPatchByPreFix ? "PreFix" : "PostFix";
                Logger.LogInfo($"{logFlag}: {logContent}");
            }
        }

    }
}
