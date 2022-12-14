using HarmonyLib;
using Database;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UtilLibs;
using New_Elements.Buildings;

namespace New_Elements
{
    internal class ResearchTreePatches
    {/// <summary>
     /// add research card to research screen
     /// </summary>
    public class Techs
        {
        public static string ChemicalTech = "ChemicalTech";
        public static string PlutoniumTech = "PlutoniumTech";
        public static string MixerTech = "MixerTech";
        public static string OilPlantTech = "OilPlantTech";
        }
    [HarmonyPatch(typeof(ResourceTreeLoader<ResourceTreeNode>), MethodType.Constructor, typeof(TextAsset))]
        public class ResourceTreeLoader_Load_Patch
        {
            public static void Postfix(ResourceTreeLoader<ResourceTreeNode> __instance, TextAsset file)
            {
                if (DlcManager.IsExpansion1Active())
                {
                    TechUtils.AddNode(__instance,
                        Techs.PlutoniumTech,
                        GameStrings.Technology.ColonyDevelopment.RadboltPropulsion,
                        GameStrings.Technology.ColonyDevelopment.CryoFuelPropulsion,
                        GameStrings.Technology.ColonyDevelopment.RadboltPropulsion
                        );
                }

                TechUtils.AddNode(__instance,
                Techs.ChemicalTech,
                GameStrings.Technology.Liquids.LiquidBasedRefinementProcess,
                GameStrings.Technology.Liquids.AdvancedCaffeination,
                GameStrings.Technology.Liquids.LiquidBasedRefinementProcess
                );

                if (DlcManager.IsExpansion1Active())
                {
                    TechUtils.AddNode(__instance,
                    Techs.OilPlantTech,
                    GameStrings.Technology.Power.ValveMiniaturization,
                    GameStrings.Technology.SolidMaterial.SolidManagement,
                    GameStrings.Technology.Power.AdvancedCombustion
                    );
                }
                else
                {
                    TechUtils.AddNode(__instance,
                    Techs.OilPlantTech,
                    GameStrings.Technology.Power.ValveMiniaturization,
                    GameStrings.Technology.SolidMaterial.SolidManagement,
                    GameStrings.Technology.Power.ValveMiniaturization
                    );
                }
                TechUtils.AddNode(__instance,
                    Techs.MixerTech,
                    new[] { Techs.ChemicalTech, GameStrings.Technology.Gases.Catalytics },
                    GameStrings.Technology.Liquids.Jetpacks,
                    GameStrings.Technology.Gases.Catalytics
                    );
            }
        }

        /// <summary>
        /// Add research node to tree
        /// </summary>
        [HarmonyPatch(typeof(Database.Techs), "Init")]
        public class Techs_TargetMethod_Patch
        {
            public static void Postfix(Database.Techs __instance)
            {
                if (DlcManager.IsExpansion1Active())
                {
                    new Tech(Techs.PlutoniumTech, new List<string>
                {
                    ParticleAcceleratorConfig.ID,
                    //AdvancedNuclearReactorConfig.ID
                },
                    __instance
                    //,new Dictionary<string, float>()
                    //{
                    //    {"basic", 50f },
                    //    {"advanced", 50f},
                    //    {"orbital", 400f},
                    //    {"nuclear", 50f}
                    //}
                    );
                }

                new Tech(Techs.ChemicalTech, new List<string>
                {
                    ChemicalPlantConfig.ID,
                },
                __instance
                );

                new Tech(Techs.OilPlantTech, new List<string>
                {
                    OilPlantConfig.ID,
                },
                __instance
                );

                new Tech(Techs.MixerTech, new List<string>
                {
                    MixerConfig.ID,
                },
                __instance
                );
            }
        }

    }
}