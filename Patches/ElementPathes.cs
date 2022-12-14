using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace New_Elements
{
    [HarmonyPatch(typeof(ElementLoader), "FinaliseElementsTable")]
    public static class YellowCakeStoragePatch
    {
        public static void Postfix()
        {
            Element elementByHash = ElementLoader.FindElementByHash(SimHashes.Yellowcake);
            elementByHash.materialCategory = GameTags.ManufacturedMaterial;
            elementByHash.oreTags = new List<Tag>((IEnumerable<Tag>)elementByHash.oreTags)
        {
          GameTags.ManufacturedMaterial
        }.ToArray();
            GameTags.SolidElements.Add(elementByHash.tag);
        }
    }
    [HarmonyPatch(typeof(ElementLoader), "FinaliseElementsTable")]
    public static class YellowCakePatch
    {
        public static void Postfix()
        {
            Element elementByHash = ElementLoader.FindElementByHash(SimHashes.Yellowcake);
            elementByHash.disabled = false;
        }
    }

    [HarmonyPatch(typeof(ElementLoader), "FinaliseElementsTable")]
    public static class PropanePatch
    {
        public static void Postfix()
        {
            Element elementByHash = ElementLoader.FindElementByHash(SimHashes.Propane);
            elementByHash.disabled = false;
        }
    }

    [HarmonyPatch(typeof(ElementLoader), "FinaliseElementsTable")]
    public static class PhosphatePatch
    {
        public static void Postfix()
        {
            Element elementByHash = ElementLoader.FindElementByHash(SimHashes.PhosphateNodules);
            elementByHash.disabled = false;
        }
    }

    [HarmonyPatch(typeof(ElementLoader), "FinaliseElementsTable")]
    public static class CoriumPatch
    {
        public static void Postfix()
        {
            Element elementByHash = ElementLoader.FindElementByHash(SimHashes.Corium);
            elementByHash.radiationPer1000Mass = 300f;
            elementByHash.sublimateId = SimHashes.Fallout;
        }
    }
    [HarmonyPatch(typeof(ElementLoader), "FinaliseElementsTable")]
    public static class RadiumPatch
    {
        public static void Postfix()
        {
            Element elementByHash = ElementLoader.FindElementByHash(SimHashes.Radium);
            elementByHash.radiationPer1000Mass = 150f;
        }
    }
}
