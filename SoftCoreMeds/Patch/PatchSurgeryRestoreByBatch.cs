using BepInEx.Logging;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SoftCoreMeds.Component;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static EFT.Player;
using static EFT.Player.MedsController;
using LoggerInstance = BepInEx.Logging.Logger;
using PlayerHealthController = GClass3010;

namespace SoftCoreMeds.Patch
{
    /// <summary>
    /// Make Surgical Kit great again
    /// </summary>
    internal class PatchSurgeryRestoreByBatch : BasePatchModule
    {
        /// <summary>
        /// Overwrite default EFT Meds Consum Imp
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod() 
        {
            IsPatchByPreFix = false;
            return AccessTools.Method(
                typeof(PlayerHealthController),
                nameof(PlayerHealthController.method_7),
                new Type[] { typeof(Item), typeof(EBodyPart), typeof(bool), typeof(EBodyPart?).MakeByRefType() }
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

        private static readonly List<EBodyPart> _nextLimb2Restore = new ();

        private static PlayerHealthController _instance;

        private static MedicalItemClass _medicalItem;

        [PatchPostfix]
        public static void PostFix(PlayerHealthController __instance, Item item, EBodyPart bodyPart, bool fastSearch, ref EBodyPart? damagedBodyPart, ref bool __result)
        {
            if (!Plugin.EnableSurgeryPatch.Value)
            {
                return;
            }

            DebugLog($"Init, Param [type = {item.GetType().FullName}, BodyPart = {bodyPart}, ReturnBodyPart = {(damagedBodyPart)}, Result = {__result}]");

            if (fastSearch)
            {
                // skip dry run
                DebugLog("skip ui dry run");
                return;
            }

            if (!IsPatchItem(item.StringTemplateId))
            {
                // skip other stim
                DebugLog("skip for none surgical kit");
                return;
            }

            if (item is not MedicalItemClass medicalItem)
            {
                // skip food and drink
                DebugLog("skip food and drink, or stim");
                return;
            }

            // i'm soooooooooooo lost, this code just to find out what's EFT Dev doing
            DebugLog($"print component type = {medicalItem.MedKitComponent?.IMedkitResource?.GetType().FullName}");
            DebugLog($"resource count = {medicalItem.MedKitComponent?.HpResource}");

            if (medicalItem.MedKitComponent?.HpResource <= 1)
            {
                DebugLog("skip for no resource left");
                return;
            }

            if (medicalItem.MedKitComponent?.IMedkitResource is not MedicalTemplateClass medicalResource)
            {
                DebugLog("skip for unknow condition");
                return;
            }

            _instance = __instance;
            _medicalItem = medicalItem;
            _nextLimb2Restore.Clear();

            var uiComponent = medicalItem.GetItemComponent<UIContextComponent>();
            var healAll = bodyPart == EBodyPart.Common;
            if (uiComponent != null)
            {
                DebugLog("get ui context from item");
                healAll = uiComponent.input == UnityEngine.EventSystems.PointerEventData.InputButton.Left && uiComponent.DoubleClick;
                medicalItem.Components.Remove(uiComponent);
            }

            if (healAll)
            {
                // add restore all destory limb event
                var destoryedLimbs = GClass3058.RealBodyParts.Where(_ => __instance.IsBodyPartDestroyed(_)).Distinct();
                _nextLimb2Restore.AddRange(destoryedLimbs);
                __instance.BodyPartRestoredEvent -= RestoreNextLimb;
                __instance.BodyPartRestoredEvent += RestoreNextLimb;
            }
            else
            {
                __instance.BodyPartRestoredEvent -= RestoreNextLimb;
            }

            DebugLog("Complete");
        }

        public static bool IsPatchItem(string itemTemplateId)
        {
            return itemTemplateId == _surv12Kit || itemTemplateId == _cmsKit;
        }

        public static void RestoreNextLimb(EBodyPart body, ValueStruct bodyPartHealth)
        {
            DebugLog($"restore event, BodyPart = {body}, Current = {bodyPartHealth.Current}, Minimum = {bodyPartHealth.Minimum}, Maximum = {bodyPartHealth.Maximum}, AtMinimum = {bodyPartHealth.AtMinimum}, AtMaximum = {bodyPartHealth.AtMaximum}");

            if (!_medicalItem.HealthEffectsComponent.DamageEffects.TryGetValue(EDamageEffectType.DestroyedPart, out var penaltyRange))
            {
                DebugLog("can't resolve surgical penalty factor");
                return;
            }

            DebugLog($"surgical penalty factor = {penaltyRange.HealthPenaltyMin}, {penaltyRange.HealthPenaltyMax}");

            var penaltyValue = UnityEngine.Random.Range(penaltyRange.HealthPenaltyMin, penaltyRange.HealthPenaltyMax) / 100f;
            foreach (var nextlimb in _nextLimb2Restore)
            {
                DebugLog($"loop {nextlimb}, MaxHpResource = {_medicalItem.MedKitComponent.MaxHpResource}, HpResourceRate = {_medicalItem.MedKitComponent.HpResourceRate}");

                if (_medicalItem.MedKitComponent.HpResource <= 0)
                {
                    DebugLog("loop end for no resource left");
                    break;
                }

                if (!_instance.IsBodyPartDestroyed(nextlimb))
                {
                    DebugLog($"loop skip for healthy limb = {nextlimb}");
                    continue;
                }

                if (_instance.RestoreBodyPart(nextlimb, penaltyValue))
                {
                    _medicalItem.MedKitComponent.HpResource -= 1;
                    DebugLog($"loop {nextlimb}, restore success, resource = {_medicalItem.MedKitComponent.HpResource}");
                }
            }
            _instance.BodyPartRestoredEvent -= RestoreNextLimb;
        }

    }
}
