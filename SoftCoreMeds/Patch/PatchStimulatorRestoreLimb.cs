using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LoggerInstance = BepInEx.Logging.Logger;
using HealthEffect = EFT.HealthSystem.ActiveHealthController.GClass3008;
using PlayerHealthController = GClass3010;
using StimBuff = GClass3019.GClass3044.GClass3045;
using StimEffect = EFT.HealthSystem.ActiveHealthController.Effect<GStruct394>;
using MedEffect = EFT.HealthSystem.ActiveHealthController.Effect<GStruct393>;

namespace SoftCoreMeds.Patch
{
    /// <summary>
    /// Make STIM great again
    /// </summary>
    internal class PatchStimulatorRestoreLimb : ModulePatch
    {
        /// <summary>
        /// Overwrite default EFT Meds Consum Imp
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(PlayerHealthController),
                nameof(PlayerHealthController.method_7),
                //nameof(PlayerHealthController.DoMedEffect),
                new Type[] { typeof(Item), typeof(EBodyPart), typeof(bool), typeof(EBodyPart?).MakeByRefType() }
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchStimulatorRestoreLimb));

        /// <summary>
        /// STIM Item Id: Propital 
        /// </summary>
        private const string _patchStimItemId = "5c0e534186f7747fa1419867";

        private static string _originStimBuffKey;

        private const string _patchStimDebuffKey = nameof(PatchStimulatorRestoreLimb);

        private static readonly StimBuff[] _patchStimDebuff = new StimBuff[] 
        {
            new StimBuff { BuffType = EStimulatorBuffType.QuantumTunnelling, AppliesTo = new EBodyPart[]{ EBodyPart.Common }, Duration = 120, Chance = 1F, },
            new StimBuff { BuffType = EStimulatorBuffType.HandsTremor, AppliesTo = new EBodyPart[]{ EBodyPart.Common }, Duration = 120, Chance = 1F, },
            //new StimBuff { BuffType = EStimulatorBuffType.Contusion, AppliesTo = new EBodyPart[]{ EBodyPart.Common }, Duration = 120, Chance = 1F, },
        };

        [PatchPrefix]
        public static void PreFix(PlayerHealthController __instance, Item item, EBodyPart bodyPart, bool fastSearch, ref EBodyPart? damagedBodyPart, ref bool __result)
        {
            if (!Plugin.EnableStimulatorPatch.Value)
            {
                ResetBuffTemplate(item);
                return;
            }

            DebugLog($"Init, Param [type = {item.GetType().FullName}, BodyPart = {bodyPart}, ReturnBodyPart = {(damagedBodyPart.HasValue ? damagedBodyPart.Value : string.Empty)}, Result = {__result}]");

            if (fastSearch)
            {
                // skip dry run
                DebugLog("PrePatch: skip ui dry run");
                return;
            }

            DebugLog(item);

            if (item is not StimulatorItemClass stimItem)
            {
                // skip food
                DebugLog("PrePatch: skip none stim");
                return;
            }

            if (stimItem?.TemplateId.StringID != _patchStimItemId)
            {
                // skip other stim
                DebugLog($"PrePatch: skip other stim, id = {stimItem?.TemplateId.StringID}");
                return;
            }

            // register debuff for limb heal
            var stimulatorSetting = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Stimulator;
            if (stimulatorSetting.Buffs.TryAdd(_patchStimDebuffKey, _patchStimDebuff))
            {
                DebugLog($"PrePatch: init debuffsetting = {_patchStimDebuffKey}");
            }

            // get stimbuff from stim item
            var buffContent = item.GetItemComponent<HealthEffectsComponent>();
            DebugLog(buffContent, stimItem);

            // check stim type for safe side
            if (buffContent?.Ginterface392_0 is StimulatorTemplateClass effectTemplate)
            {
                DebugLog($"PrePatch: buffsetting = {buffContent?.StimulatorBuffs}, {buffContent?.Ginterface392_0?.GetType().FullName}");
            }
            else
            {
                DebugLog($"PrePatch: error component interfeace = {buffContent?.Ginterface392_0.GetType().FullName}");
                return;
            }

            DebugLog(__instance, "BeforePatch");

            // is palyer use stim for limb heal
            if (GClass3058.RealBodyParts.Contains(bodyPart) && __instance.IsBodyPartDestroyed(bodyPart))
            {
                DebugLog($"PrePatch: restore body part = {bodyPart}, current buff key = {effectTemplate.StimulatorBuffs}, backup buff key = {_originStimBuffKey}");

                // restore limb base and set penalty
                //var healthPenalty = UnityEngine.Random.Range(penaltyRange.HealthPenaltyMin, penaltyRange.HealthPenaltyMax) / 100f;
                var healRatio = Plugin.StimRestorePercent.Value/ 100F;
                __instance.RestoreBodyPart(bodyPart, healRatio);

                // deplete Energy and Hydration for heal, deplete vale equals limb maxhealth
                var bodyPartHealth = __instance.GetBodyPartHealth(bodyPart);
                var energyPenalty = -bodyPartHealth.Maximum * (1F - __instance.SkillManager_0.MetabolismRatioPlus);
                var hydrationPenalty = -bodyPartHealth.Maximum * (1F - __instance.SkillManager_0.MetabolismRatioPlus);
                __instance.ChangeEnergy(energyPenalty);
                __instance.ChangeHydration(hydrationPenalty);

                // remove stim current buff, for next step
                var activateEffects = __instance.FindActiveEffects<StimEffect>(EBodyPart.Common).Where(effect => effect.Store.ItemTemplateId == _patchStimItemId);
                foreach (var effect in activateEffects)
                {
                    DebugLog(effect);
                    //remove buff by set state value
                    effect.State = EEffectState.Residued;
                }

                // backup stim origin buff
                _originStimBuffKey ??= effectTemplate.StimulatorBuffs;

                // set stim buff to only side effect
                effectTemplate.StimulatorBuffs = _patchStimDebuffKey;
            }
            else
            {
                // reset stim buff to origin
                DebugLog($"PrePatch: item current buff = {effectTemplate.StimulatorBuffs}, backup buff key = {_originStimBuffKey}");
                effectTemplate.StimulatorBuffs = _originStimBuffKey ?? effectTemplate.StimulatorBuffs;
            }

            DebugLog($"PrePatch: execute complete");
        }

        public static void ResetBuffTemplate(Item item)
        {
            if (item is not StimulatorItemClass stimItem)
            {
                return;
            }

            if (stimItem?.TemplateId.StringID != _patchStimItemId)
            {
                return;
            }

            if (!item.TryGetItemComponent<HealthEffectsComponent>(out var buffContent))
            {
                return;
            }

            if (buffContent?.Ginterface392_0 is StimulatorTemplateClass effectTemplate)
            {
                effectTemplate.StimulatorBuffs = _originStimBuffKey ?? "BuffseTGchange";
            }
        }

        public static void DebugLog(string logContent)
        {
            if (Plugin.EnableLog?.Value is true)
            {
                Logger.LogDebug(logContent);
            }
        }

        public static void DebugLog(Item item)
        {
            DebugLog($"PrePatch itemType: {item.GetType().FullName}");

            if (item is MedicalItemClass medicalItem)
            {
                foreach (var comp in medicalItem.Components)
                {
                    DebugLog($"PrePatch components type: {comp.GetType().FullName}");
                }
            }
        }

        public static void DebugLog(StimEffect effect)
        {
            DebugLog($"PrePatch debug#4: effect = {effect.GetType().FullName}, {effect.Id}, {effect.State}");
            DebugLog($"PrePatch debug#5: {string.Join(", ", effect.DisplayableVariations.SelectMany(_ => _.Buffs).Select(_ => _.NameDisplay))}");
        }


        public static void DebugLog(HealthEffectsComponent buffContent, StimulatorItemClass stimItem)
        {
            if (buffContent == null)
            {
                DebugLog($"PrePatch: error component in stim effect model ({string.Join(", ", stimItem.Components.Select(item => item.GetType().FullName))})");
            }
            else
            {
                DebugLog(string.Join(", ", buffContent.BuffSettings.Select(_ => $"BuffName = {_.BuffName} ({_.Value})")));
            }
        }

        public static void DebugLog(PlayerHealthController __instance, string flagStr)
        {
            foreach (var _ in __instance.List_0)
            {
                DebugLog($"PrePatch debug#0: {_.BodyPart}");
                foreach (var __ in _.List_0)
                {
                    if (__ is GInterface376 effect1)
                    {
                        DebugLog($"PrePatch debug#1: {effect1.BodyPart}, id = {__.Id}, tempid = {effect1.MedItem.StringTemplateId}");
                        if (effect1.MedItem.StringTemplateId == _patchStimItemId)
                        {
                            //__instance.RemoveEffectFromList(__);
                        }
                    }
                    else if (__ is StimEffect effect2)
                    {
                        DebugLog($"PrePatch debug#2: {effect2.BodyPart}, id = {effect2.Id}, tempid = {effect2.Store.ItemTemplateId}");
                        if (effect2.Store.ItemTemplateId == _patchStimItemId)
                        {
                            //__instance.RemoveEffectFromList(__);
                        }
                    }
                    else
                    {
                        DebugLog($"PrePatch debug#3: type = {__.GetType().FullName}");
                    }
                }
            }
        }

    }
}
