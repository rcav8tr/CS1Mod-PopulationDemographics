using UnityEngine;
using CitiesHarmony.API;
using HarmonyLib;
using System.Reflection;
using System;

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
        public static bool CreatePatches()
        {
            // check Harmony
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                ColossalFramework.UI.UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Missing Dependency", 
                    "The Population Demographics mod requires the 'Harmony (Mod Dependency)' mod.  \n\nPlease subscribe to the 'Harmony (Mod Dependency)' mod and restart the game.", error: false);
                return false;
            }

            // create the patches
            if (!CreatePostfixPatch(typeof(PopulationInfoViewPanel       ), "UpdatePanel",          BindingFlags.Instance | BindingFlags.NonPublic, "PostfixPopulationInfoViewPanelUpdatePanel"       )) return false;
            if (!CreatePostfixPatch(typeof(ResidentialBuildingAI         ), "SimulationStepActive", BindingFlags.Instance | BindingFlags.NonPublic, "PostfixResidentialBuildingAISimulationStepActive")) return false;
            if (!CreatePostfixPatch("SeniorCitizenCenterMod.NursingHomeAi", "SimulationStepActive", BindingFlags.Instance | BindingFlags.NonPublic, "PostfixNursingHomeAiSimulationStepActive"        )) return false;
            if (!CreatePostfixPatch(typeof(District                      ), "SimulationStep",       BindingFlags.Instance | BindingFlags.Public,    "PostfixDistrictSimulationStep"                   )) return false;

            // success
            return true;
        }

        /// <summary>
        /// create a postfix patch (i.e. called after the base processing)
        /// </summary>
        /// <param name="originalClassType">type of the class to be patched</param>
        /// <param name="originalMethodName">name of the method to be patched</param>
        /// <param name="bindingFlags">bindings flags of the method to be patched</param>
        /// <param name="postfixMethodName">name of the post fix method</param>
        /// <returns>success status</returns>
        private static bool CreatePostfixPatch(Type originalClassType, string originalMethodName, BindingFlags bindingFlags, string postfixMethodName)
        {
            // get the original method
            MethodInfo originalMethod = originalClassType.GetMethod(originalMethodName, bindingFlags);
            if (originalMethod == null)
            {
                Debug.LogError($"Unable to find method {originalClassType}.{originalMethodName}.");
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
        /// create a postfix patch (i.e. called after the base processing)
        /// </summary>
        /// <param name="originalClassName">name of the class to be patched</param>
        /// <param name="originalMethodName">name of the method to be patched</param>
        /// <param name="bindingFlags">bindings flags of the method to be patched</param>
        /// <param name="postfixMethodName">name of the post fix method</param>
        /// <returns>success status</returns>
        private static bool CreatePostfixPatch(string originalClassName, string originalMethodName, BindingFlags bindingFlags, string postfixMethodName)
        {
            // loop over all the assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // check if the string class name is in the assembly
                // the type name will be defined if the mod is subscribed, even if the mod is not enabled
                // it is okay to patch the class if the mod is not enabled, there simply will be no instances of that type
                Type type = assembly.GetType(originalClassName, false);
                if (type != null)
                {
                    return CreatePostfixPatch(type, originalMethodName, bindingFlags, postfixMethodName);
                }
            }

            // if got here, then the string class name was not found
            // this is not an error, it just means the mod is not subscribed
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
        /// postfix patch for NursingHomeAi.SimulationStepActive (mod)
        /// </summary>
        public static void PostfixNursingHomeAiSimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            // do same logic as for ResidentialBuildingAI
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
            try
            {
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    new Harmony(HarmonyId).UnpatchAll(HarmonyId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
