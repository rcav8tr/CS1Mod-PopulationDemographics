using UnityEngine;
using CitiesHarmony.API;
using HarmonyLib;
using System.Reflection;

namespace PopulationDemographics
{
    /// <summary>
    /// Harmony patching
    /// </summary>
    internal class HarmonyPatcher
    {
        private const string HarmonyId = "com.github.rcav8tr.PopulationDemographics";

        /// <summary>
        /// create Harmony patches
        /// </summary>
        public static void CreatePatches()
        {
            // check Harmony
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                ColossalFramework.UI.UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Missing Dependency", 
                    "The Population Demographics mod requires the 'Harmony (Mod Dependency)' mod.  \n\nPlease subscribe to the 'Harmony (Mod Dependency)' mod and restart the game.", error: false);
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
            new Harmony(HarmonyId).Patch(originalMethod, null, new HarmonyMethod(postfixMethod));

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
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                new Harmony(HarmonyId).UnpatchAll();
            }
        }
    }
}
