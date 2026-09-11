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
using EFT.HealthSystem;
using LoggerInstance = BepInEx.Logging.Logger;
using PlayerHealthController = EFT.HealthSystem.ActiveHealthController;
using StimBuff = EFT.HealthSystem.EffectsSettings.StimulatorSettings.StimulatorBuffSettings;
using StimEffect = EFT.HealthSystem.ActiveHealthController.Effect<EFT.HealthSystem.StimulatorStore>;
using MedEffect = EFT.HealthSystem.ActiveHealthController.Effect<EFT.HealthSystem.SyncHealthPacket.SyncAddEffect.ExtraDataUnion>;

namespace SoftCoreMeds.Patch
{
    /// <summary>
    /// Make STIM great again
    /// </summary>
    internal class PatchStimulatorRestoreLimb : BasePatchModule
    {
        /// <summary>
        /// Overwrite default EFT Meds Consum Imp
        /// </summary>
        /// <returns></returns>
        protected override MethodBase GetTargetMethod()
        {
            IsPatchByPreFix = true;
            return AccessTools.Method(
                typeof(PlayerHealthController),
                nameof(PlayerHealthController.TryGetBodyPartToApply),
                new Type[] { typeof(Item), typeof(EBodyPart), typeof(bool), typeof(EBodyPart?).MakeByRefType() }
            );
        }

        private static new readonly ManualLogSource Logger = LoggerInstance.CreateLogSource(nameof(PatchStimulatorRestoreLimb));

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
                DebugLog("skip ui dry run");
                return;
            }

            DebugLog(item);

            if (item is not Stimulator stimItem || stimItem == null)
            {
                // skip food
                DebugLog("skip none stim");
                return;
            }

            if (!Plugin.Check4PatchStimId(stimItem.TemplateId._stringID))
            {
                // skip other stim
                DebugLog($"skip other stim, id = {stimItem?.TemplateId._stringID}");
                return;
            }

            // register debuff for limb heal
            var stimulatorSetting = Singleton<GlobalConfiguration>.Instance.Health.Effects.Stimulator;
            if (stimulatorSetting.Buffs.TryAdd(_patchStimDebuffKey, _patchStimDebuff))
            {
                DebugLog($"init debuffsetting = {_patchStimDebuffKey}");
            }

            // get stimbuff from stim item
            var buffContent = item.GetItemComponent<HealthEffectsComponent>();
            DebugLog(buffContent, stimItem);

            // check stim type for safe side
            if (buffContent?._template is not StimulatorTemplate effectTemplate || effectTemplate == null)
            {
                DebugLog($"error component interfeace = {buffContent?._template.GetType().FullName}");
                return;
            }

            DebugLog($"buffsetting = {buffContent?.StimulatorBuffs}, {buffContent?._template?.GetType().FullName}");

            // is palyer use stim for limb heal
            if (HealthHelper.RealBodyParts.Contains(bodyPart) && __instance.IsBodyPartDestroyed(bodyPart))
            {
                DebugLog($"restore body part = {bodyPart}, current buff key = {effectTemplate.StimulatorBuffs}, backup buff key = {_originStimBuffKey}");

                // restore limb base and set penalty
                //var healthPenalty = UnityEngine.Random.Range(penaltyRange.HealthPenaltyMin, penaltyRange.HealthPenaltyMax) / 100f;
                var healRatio = Plugin.StimRestorePercent.Value/ 100F;
                __instance.RestoreBodyPart(bodyPart, healRatio);

                // deplete Energy and Hydration for heal, deplete vale equals limb maxhealth
                var bodyPartHealth = __instance.GetBodyPartHealth(bodyPart);
                var energyPenalty = -bodyPartHealth.Maximum * (1F - __instance._skills.MetabolismRatioPlus);
                var hydrationPenalty = -bodyPartHealth.Maximum * (1F - __instance._skills.MetabolismRatioPlus);

                __instance.ChangeEnergy(energyPenalty);
                __instance.ChangeHydration(hydrationPenalty);

                // remove stim current buff, for next step
                var activateEffects = __instance.FindActiveEffects<StimEffect>(EBodyPart.Common).Where(effect => Plugin.Check4PatchStimId(effect._store.ItemTemplateId));
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
                DebugLog($"item current buff = {effectTemplate.StimulatorBuffs}, backup buff key = {_originStimBuffKey}");
                effectTemplate.StimulatorBuffs = _originStimBuffKey ?? effectTemplate.StimulatorBuffs;
            }

            DebugLog($"execute complete");
        }

        public static void ResetBuffTemplate(Item item)
        {
            if (item is not Stimulator stimItem || stimItem == null)
            {
                return;
            }

            if (!Plugin.Check4PatchStimId(stimItem.TemplateId._stringID))
            {
                return;
            }

            if (!item.TryGetItemComponent<HealthEffectsComponent>(out var buffContent))
            {
                return;
            }

            if (buffContent?._template is StimulatorTemplate effectTemplate)
            {
                effectTemplate.StimulatorBuffs = _originStimBuffKey ?? "BuffseTGchange";
            }
        }

        public static void DebugLog(Item item)
        {
            DebugLog($"itemType: {item.GetType().FullName}");

            if (item is Meds medicalItem)
            {
                foreach (var comp in medicalItem.Components)
                {
                    DebugLog($"components type: {comp.GetType().FullName}");
                }
            }
        }

        public static void DebugLog(StimEffect effect)
        {
            DebugLog($"debug#4: effect = {effect.GetType().FullName}, {effect.Id}, {effect.State}");
            DebugLog($"debug#5: {string.Join(", ", effect.DisplayableVariations.SelectMany(_ => _.Buffs).Select(_ => _.NameDisplay))}");
        }

        public static void DebugLog(HealthEffectsComponent buffContent, Stimulator stimItem)
        {
            if (buffContent == null)
            {
                DebugLog($"error component in stim effect model ({string.Join(", ", stimItem.Components.Select(item => item.GetType().FullName))})");
            }
            else
            {
                DebugLog(string.Join(", ", buffContent.BuffSettings.Select(_ => $"BuffName = {_.BuffName} ({_.Value})")));
            }
        }
    }
}
