using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace PopulationDemographics
{
    /// <summary>
    /// Harmony patching
    /// </summary>
    public class HarmonyPatcher
    {
        private const string HarmonyId = "com.github.rcav8tr.PopulationDemographics";
        private static Harmony _harmony;

        /// <summary>
        /// create Harmony patches
        /// </summary>
        public static void CreatePatches()
        {
            // initialize Harmony
            _harmony = new Harmony(HarmonyId);
            if (_harmony == null)
            {
                Debug.LogError("Unable to create Harmony instance.");
                return;
            }

            // create the patches
            if (!CreatePostfixPatch<PopulationInfoViewPanel>("UpdatePanel",          BindingFlags.Instance | BindingFlags.NonPublic, "PostfixPopulationInfoViewPanelUpdatePanel"       )) return;
            if (!CreatePostfixPatch<ResidentialBuildingAI  >("SimulationStepActive", BindingFlags.Instance | BindingFlags.NonPublic, "PostfixResidentialBuildingAISimulationStepActive")) return;
            if (!CreatePostfixPatch<District               >("SimulationStep",       BindingFlags.Instance | BindingFlags.Public,    "PostfixDistrictSimulationStep"                   )) return;
        }

        /// <summary>
        /// create a postfix patch (i.e. called after the base processing)
        /// </summary>
        /// <typeparam name="T">type of the AI to be patched</typeparam>
        /// <param name="originalMethodName">name of the AI method to be patched</param>
        /// <param name="bindingFlags">bindings flags of the AI method to be patched</param>
        /// <param name="postfixMethodName">name of the post fix method</param>
        /// <returns>success status</returns>
        private static bool CreatePostfixPatch<T>(string originalMethodName, BindingFlags bindingFlags, string postfixMethodName)
        {
            // get the original method
            MethodInfo originalMethod = typeof(T).GetMethod(originalMethodName, bindingFlags);
            if (originalMethod == null)
            {
                Debug.LogError($"Unable to find method {typeof(T)}.{originalMethodName}.");
                return false;
            }

            // get the postfix method
            MethodInfo postfixMethod = typeof(HarmonyPatcher).GetMethod(postfixMethodName, BindingFlags.Static | BindingFlags.Public);
            if (postfixMethod == null)
            {
                Debug.LogError($"Unable to find method HarmonyPatcher.{postfixMethodName}.");
                return false;
            }

            // create the patch
            _harmony.Patch(originalMethod, null, new HarmonyMethod(postfixMethod));

            // success
            return true;
        }

        /// <summary>
        /// postfix patch for PopulationInfoViewPanel.UpdatePanel
        /// </summary>
        public static void PostfixPopulationInfoViewPanelUpdatePanel()
        {
            PopulationDemographicsLoading.panel.UpdatePanel();
        }

        /// <summary>
        /// postfix patch for ResidentialBuildingAI.SimulationStepActive
        /// this also patches PloppableRICO.GrowableResidentialAI and PloppableRICO.PloppableResidentialAI because they derive from ResidentialBuildingAI
        /// </summary>
        public static void PostfixResidentialBuildingAISimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            PopulationDemographicsLoading.panel.ResidentialSimulationStepActive(buildingID, ref buildingData, true);
        }

        /// <summary>
        /// postfix patch for District.SimulationStep
        /// </summary>
        public static void PostfixDistrictSimulationStep(byte districtID)
        {
            PopulationDemographicsLoading.panel.DistrictSimulationStep(districtID, true);
        }

        /// <summary>
        /// remove Harmony patches
        /// </summary>
        public static void RemovePatches()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchAll();
                _harmony = null;
            }
        }
    }
}
