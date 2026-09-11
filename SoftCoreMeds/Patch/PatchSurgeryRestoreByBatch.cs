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
using PlayerHealthController = EFT.HealthSystem.PlayerHealthController;

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
                nameof(PlayerHealthController.TryGetBodyPartToApply),
                new Type[] { typeof(Item), typeof(EBodyPart), typeof(bool), typeof(EBodyPart?).MakeByRefType() }
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchSurgeryRestoreByBatch));

        private static PlayerHealthController _instance;

        private static Meds _medicalItem;

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

            if (!IsSurgeryKit(item.StringTemplateId))
            {
                // skip other stim
                DebugLog("skip for none surgical kit");
                return;
            }

            if (item is not Meds medicalItem || medicalItem == null)
            {
                // skip food and drink
                DebugLog("skip food and drink, or stim");
                return;
            }

            if (medicalItem.MedKitComponent?.HpResource <= 1)
            {
                DebugLog("skip for no resource left");
                return;
            }

            _instance = __instance;
            _medicalItem = medicalItem;

            var uiComponent = medicalItem.GetItemComponent<UIContextComponent>();
            var healAll = bodyPart == EBodyPart.Common;
            if (uiComponent != null)
            {
                DebugLog("get ui context from item");
                // flag heal all limb by double left click
                healAll = uiComponent.input == UnityEngine.EventSystems.PointerEventData.InputButton.Left && uiComponent.DoubleClick;
                // flag heal all limb by context menu click
                healAll = uiComponent.ConsumMethod == EItemInfoButton.Use || uiComponent.ConsumMethod == EItemInfoButton.UseAll;
                // remove ui context component to avoid triggering other interaction logic
                medicalItem.Components.Remove(uiComponent);
            }

            if (healAll)
            {
                // add restore all destory limb event
                var destoryedLimbs = HealthHelper.RealBodyParts.Where(_ => __instance.IsBodyPartDestroyed(_)).Distinct();
                __instance.BodyPartRestoredEvent -= RestoreNextLimb;
                __instance.BodyPartRestoredEvent += RestoreNextLimb;
            }
            else
            {
                __instance.BodyPartRestoredEvent -= RestoreNextLimb;
            }

            DebugLog("Complete");
        }

        public static void RestoreNextLimb(EBodyPart body, ValueStruct bodyPartHealth)
        {
            DebugLog($"restore event, BodyPart = {body}, Current = {bodyPartHealth.Current}, Minimum = {bodyPartHealth.Minimum}, Maximum = {bodyPartHealth.Maximum}, AtMinimum = {bodyPartHealth.AtMinimum}, AtMaximum = {bodyPartHealth.AtMaximum}");

            _instance.BodyPartRestoredEvent -= RestoreNextLimb;

            if (!_medicalItem.HealthEffectsComponent.DamageEffects.TryGetValue(EDamageEffectType.DestroyedPart, out var penaltyRange))
            {
                DebugLog("can't resolve surgical penalty factor");
                return;
            }

            DebugLog($"surgical penalty factor = {penaltyRange.HealthPenaltyMin}, {penaltyRange.HealthPenaltyMax}");

            var penaltyValue = UnityEngine.Random.Range(penaltyRange.HealthPenaltyMin, penaltyRange.HealthPenaltyMax) / 100f;
            var destoryedLimbs = HealthHelper.RealBodyParts.Where(_ => _instance.IsBodyPartDestroyed(_)).Distinct();
            foreach (var nextlimb in destoryedLimbs)
            {
                DebugLog($"loop {nextlimb}, HpResource = {_medicalItem.MedKitComponent.HpResource}");

                if(nextlimb == body)
                {
                    continue;
                }

                if (_medicalItem.MedKitComponent.HpResource < 2)
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
        }

    }
}
