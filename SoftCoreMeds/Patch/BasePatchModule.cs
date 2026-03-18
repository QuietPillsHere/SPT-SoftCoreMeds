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

        protected override MethodBase GetTargetMethod() 
        {
            throw new NotImplementedException();
        }

        public static void DebugLog(string logContent)
        {
            if (Plugin.EnableLog?.Value == true)
            {
                string logFlag = IsPatchByPreFix ? "PreFix" : "PostFix";
                Logger.LogDebug($"{logFlag}: {logContent}");
            }
        }

    }
}
