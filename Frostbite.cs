﻿using STRINGS;
using static SkyLib.OniUtils;
using HarmonyLib;
using Klei.AI;
using UnityEngine;
using PeterHan.PLib.Core;

namespace New_Elements.Diseases
{

    using TempMonitorStateMachine = GameStateMachine<ExternalTemperatureMonitor, ExternalTemperatureMonitor.Instance,
        IStateMachineTarget, object>;

    // Patches for frostbite-related things
    public static class FrostbitePatch
    {
        // Add strings and status items for Frostbite
        public static class Mod_OnLoad
        {
            public static void OnLoad()
            {
                AddStatusItem("FROSTBITTEN", "NAME", "Frostbite", "CREATURES");
                AddStatusItem("FROSTBITTEN",
                    "TOOLTIP",
                    "Current external " + UI.PRE_KEYWORD + "Temperature" + UI.PST_KEYWORD +
                    " is perilously low [<b>{ExternalTemperature}</b> / <b>{TargetTemperature}</b>]",
                    "CREATURES");
                AddStatusItem("FROSTBITTEN", "NOTIFICATION_NAME", "Frostbite", "CREATURES");
                AddStatusItem("FROSTBITTEN", "NOTIFICATION_TOOLTIP", "Freezing " +
                    UI.PRE_KEYWORD + "Temperatures" + UI.PST_KEYWORD +
                    " are hurting these Duplicants:", "CREATURES");

                Strings.Add("STRINGS.DUPLICANTS.ATTRIBUTES.FROSTBITETHRESHOLD.NAME",
                    "Frostbite Threshold");
                Strings.Add("STRINGS.DUPLICANTS.ATTRIBUTES.FROSTBITETHRESHOLD.TOOLTIP",
                    "Determines the " + UI.PRE_KEYWORD + "Temperature" + UI.PST_KEYWORD +
                    " at which a Duplicant will be frostbitten.");
            }
        }

        // Gets the minimum external pressure of the cells occupied by the creature
        private static float GetCurrentExternalPressure(ExternalTemperatureMonitor.Instance instance)
        {
            int cell = Grid.PosToCell(instance.gameObject);
            var area = instance.occupyArea;
            float pressure = Grid.Pressure[cell];
            if (area != null)
            {
                foreach (var offset in area.OccupiedCellsOffsets)
                {
                    int newCell = Grid.OffsetCell(cell, offset);
                    if (Grid.IsValidCell(newCell))
                    {
                        float newPressure = Grid.Pressure[newCell];
                        if (newPressure < pressure)
                            pressure = newPressure;
                    }
                }
            }
            return pressure;
        }

        private static float GetFrostbiteThreshold(ExternalTemperatureMonitor.Instance data)
        {
            return data.attributes.GetValue("FrostbiteThreshold") + GermExposureTuning.
                BASE_FROSTBITE_THRESHOLD;
        }

        // Add Frostbite that is Scalding but for cold
        [HarmonyPatch(typeof(ExternalTemperatureMonitor), "InitializeStates")]
        public static class ExternalTemperatureMonitor_InitializeStates_Patch
        {
            public static TempMonitorStateMachine.State frostbite;
            public static TempMonitorStateMachine.State transitionToFrostbite;

            private static readonly StatusItem Frostbitten = new StatusItem(
                "FROSTBITTEN",
                "CREATURES",
                string.Empty,
                StatusItem.IconType.Exclamation,
                NotificationType.DuplicantThreatening,
                true,
                OverlayModes.None.ID
            )
            {
                resolveTooltipCallback = (str, data) =>
                {
                    float externalTemperature = ((ExternalTemperatureMonitor.Instance)data).AverageExternalTemperature;
                    float frostbiteThreshold = GetFrostbiteThreshold((ExternalTemperatureMonitor.Instance)data);
                    str = str.Replace("{ExternalTemperature}", GameUtil.GetFormattedTemperature(externalTemperature));
                    str = str.Replace("{TargetTemperature}", GameUtil.GetFormattedTemperature(frostbiteThreshold));
                    return str;
                }
            };

            private static bool IsFrostbite(ExternalTemperatureMonitor.Instance data)
            {
                // a bit of a kludge, because for some reason Average External Temperature
                // does not update for Frostbite even though it does for Scalding.
                var exttemp = data.GetCurrentExternalTemperature;
                return exttemp < GetFrostbiteThreshold(data) && exttemp > 0.01f &&
                    GetCurrentExternalPressure(data) >= GermExposureTuning.MIN_PRESSURE;
            }

            public static void Postfix(ExternalTemperatureMonitor __instance)
            {
                Frostbitten.AddNotification();
                frostbite = __instance.CreateState(nameof(frostbite));
                transitionToFrostbite = __instance.CreateState(nameof(transitionToFrostbite));
                __instance.tooCool.Transition(transitionToFrostbite, smi => IsFrostbite(smi) &&
                    smi.timeinstate > 3.0f);
                transitionToFrostbite
                    .Transition(__instance.tooCool, smi => !IsFrostbite(smi))
                    .Transition(frostbite, smi => IsFrostbite(smi) && smi.timeinstate > 1.0);
                frostbite
                   .Transition(__instance.tooCool, smi => !IsFrostbite(smi) && smi.timeinstate > 3.0f) // to leave frostbite state
                   .ToggleExpression(Db.Get().Expressions.Cold) // brr
                   .ToggleThought(Db.Get().Thoughts.Cold) // I'm thinking of brr
                   .ToggleStatusItem(Frostbitten, smi => smi)
                   .Update("ColdDamage", (smi, dt) => smi.ScaldDamage(dt), UpdateRate.SIM_1000ms);
            }
        }

        // Frostbite and scald damage depending on temperature
        [HarmonyPatch(typeof(ExternalTemperatureMonitor.Instance), "ScaldDamage")]
        public static class ExternalTemperatureMonitor_Instance_ScaldDamage_Patch
        {
            public static bool Prefix(ExternalTemperatureMonitor.Instance __instance, float dt,
                float ___lastScaldTime)
            {
                float now = Time.time;
                var hp = __instance.health;
                // Avoid damage for pressures < threshold
                bool pressure = GetCurrentExternalPressure(__instance) > GermExposureTuning.
                    MIN_PRESSURE;
                if (hp != null && now - ___lastScaldTime > 5.0f && pressure)
                {
                    float temp = __instance.AverageExternalTemperature;
                    // For every 5 C outside the limits, damage 1HP more
                    float damage = System.Math.Max(0.0f, GermExposureTuning.DAMAGE_PER_K *
                        (temp - __instance.GetScaldingThreshold())) + System.Math.Max(0.0f,
                        GermExposureTuning.DAMAGE_PER_K * (GetFrostbiteThreshold(__instance) -
                        temp));
                    if (damage > 0.0f)
                        hp.Damage(damage * dt);
                }
                return pressure;
            }
        }

        // Frostbite attribute setup
        [HarmonyPatch(typeof(Database.Attributes), MethodType.Constructor, typeof(ResourceSet))]
        public static class Database_Attributes_Attributes_Patch
        {
            public static void Postfix(Database.Attributes __instance)
            {
                var frostbiteThreshold = new Attribute("FrostbiteThreshold", false,
                    Attribute.Display.General, false);
                frostbiteThreshold.SetFormatter(new StandardAttributeFormatter(
                    GameUtil.UnitClass.Temperature, GameUtil.TimeSlice.None));
                __instance.Add(frostbiteThreshold);
            }
        }

        // Add Atmo Suit frostbite immunity
        [HarmonyPatch(typeof(AtmoSuitConfig), "CreateEquipmentDef")]
        public static class AtmosuitConfig_CreateEquipmentDef_Patch
        {
            public static void Postfix(EquipmentDef __result)
            {
                __result.AttributeModifiers.Add(new AttributeModifier("FrostbiteThreshold",
                    -200.0f, EQUIPMENT.PREFABS.ATMO_SUIT.NAME));
            }
        }

        /*[HarmonyPatch(typeof(CoolVestConfig), "CreateEquipmentDef")]
        public static class CoolvestConfig_CreateEquipmentDef_Patch
        {
            public static void Postfix(EquipmentDef __result)
            {
                __result.AttributeModifiers.Add(new AttributeModifier(Db.Get().Attributes.ScaldingThreshold.Id,
                    +15.0f, EQUIPMENT.PREFABS.COOL_VEST.NAME));
            }
        }*/

        [HarmonyPatch(typeof(WarmVestConfig), "CreateEquipmentDef")]
        public static class WarmvestConfig_CreateEquipmentDef_Patch
        {
            public static void Postfix(EquipmentDef __result)
            {
                __result.AttributeModifiers.Add(new AttributeModifier("FrostbiteThreshold",
                    -15.0f, EQUIPMENT.PREFABS.WARM_VEST.NAME));
            }
        }
    }
    public static class GermExposureTuning
    {
        // The frostbite threshold in K
        internal const float BASE_FROSTBITE_THRESHOLD = 243.1f;

        // The damage for frostbite/scalding per degree K out of range
        internal const float DAMAGE_PER_K = 0.2f;

        // Minimum pressure in grams to get scalded or frostbitten
        internal const float MIN_PRESSURE = 1.0f;
    }
}
