using CitiesHarmony.API;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace PopulationDemographics
{
    /// <summary>
    /// Harmony patching for building AI
    /// </summary>
    internal class BuildingAIPatch
    {
        private const string HarmonyId = "com.github.rcav8tr.PopulationDemographics";

        /// <summary>
        /// create Harmony patches
        /// </summary>
        public static bool CreatePatches()
        {
            try
            {
                // check Harmony
                if (!HarmonyHelper.IsHarmonyInstalled)
                {
                    ColossalFramework.UI.UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage("Missing Dependency",
                        "The Population Demographics mod requires the 'Harmony (Mod Dependency)' mod.  " + Environment.NewLine + Environment.NewLine +
                        "Please subscribe to the 'Harmony (Mod Dependency)' mod and restart the game.", error: false);
                    return false;
                }

                // create a patch for ResidentialBuildingAI.GetColor
                if (!CreatePrefixPatch(typeof(ResidentialBuildingAI), "GetColor", BindingFlags.Instance | BindingFlags.Public, typeof(BuildingAIPatch), "BuildingAIGetColor")) { return false; }

                // if the AI is valid, create a patch for OrphanageAI.GetColor from the CimCare mod
                if (BuildingAIIsValid("CimCareMod.AI.OrphanageAI", out Type type))
                {
                    if (!CreatePrefixPatch(type, "GetColor", BindingFlags.Instance | BindingFlags.Public, typeof(BuildingAIPatch), "BuildingAIGetColorCimCare")) { return false; }
                }

                // if the AI is valid, create a patch for NursingHomeAI.GetColor from the CimCare mod
                if (BuildingAIIsValid("CimCareMod.AI.NursingHomeAI", out type))
                {
                    if (!CreatePrefixPatch(type, "GetColor", BindingFlags.Instance | BindingFlags.Public, typeof(BuildingAIPatch), "BuildingAIGetColorCimCare")) { return false; }
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
                return false;
            }

            // success
            return true;
        }

        /// <summary>
        /// create a prefix patch (i.e. called before the base processing)
        /// </summary>
        /// <param name="originalClassType">type of the class to be patched</param>
        /// <param name="originalMethodName">name of the method to be patched</param>
        /// <param name="bindingFlags">bindings flags of the method to be patched</param>
        /// <param name="prefixType">type that contains the prefix method</param>
        /// <param name="prefixMethodName">name of the prefix method</param>
        /// <returns>success status</returns>
        public static bool CreatePrefixPatch(Type originalClassType, string originalMethodName, BindingFlags bindingFlags, Type prefixType, string prefixMethodName)
        {
            // get the original method
            MethodInfo originalMethod = originalClassType.GetMethod(originalMethodName, bindingFlags);
            if (originalMethod == null)
            {
                LogUtil.LogError($"Unable to find original method {originalClassType.Name}.{originalMethodName}.");
                return false;
            }

            // get the prefix method
            MethodInfo prefixMethod = prefixType.GetMethod(prefixMethodName, BindingFlags.Static | BindingFlags.Public);
            if (prefixMethod == null)
            {
                LogUtil.LogError($"Unable to find patch prefix method {prefixType.Name}.{prefixMethodName}.");
                return false;
            }

            // create the patch
            new Harmony(HarmonyId).Patch(originalMethod, new HarmonyMethod(prefixMethod), null);

            // success
            return true;
        }

        /// <summary>
        /// return whether or not the building AI is valid, also return the building AI as a Type
        /// </summary>
        private static bool BuildingAIIsValid(string buildingAI, out Type type)
        {
            // initialize output
            type = null;

            // loop over all the assemblies
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // loop over all types in the assembly
                foreach (Type t in assembly.GetTypes())
                {
                    // check if the type is the one being validated
                    if (t.FullName.StartsWith(buildingAI))
                    {
                        // type must derive from CommonBuildingAI
                        if (t.IsSubclassOf(typeof(CommonBuildingAI)))
                        {
                            // string building AI is valid
                            type = t;
                            return true;
                        }
                        else
                        {
                            // string building AI was found, but it is not valid
                            LogUtil.LogError($"Building AI [{buildingAI}] does not derive from CommonBuildingAI.");
                            return false;
                        }
                    }
                }
            }

            // if got here, then the string building AI was not found
            // this is not an error, it just means the mod is not subscribed
            return false;
        }

        /// <summary>
        /// return the color of the building
        /// </summary>
        /// <returns>whether or not to do base processing</returns>
        public static bool BuildingAIGetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, ref Color __result)
        {
            // get building color only for Population (i.e. Density) info view and when Population Demographics panel is visible
            if (infoMode == InfoManager.InfoMode.Density && PopulationDemographicsLoading.panel.isVisible)
            {
                return PopulationDemographicsLoading.panel.GetBuildingColor(buildingID, ref data, ref __result);
            }

            // do base processing
            return true;
        }

        /// <summary>
        /// return the color of the CimCare building
        /// this separate routine is required because the Harmony patcher requires the capitalization of the parameters to exactly match the original routine
        /// and the capitalization of "buildingId" in OrphanageAI.GetColor and NursingHomeAI.GetColor is different than all the other GetColor routines
        /// </summary>
        /// <returns>whether or not to do base processing</returns>
        public static bool BuildingAIGetColorCimCare(ushort buildingId, ref Building data, InfoManager.InfoMode infoMode, ref Color __result)
        {
            // simply call the main routine
            return BuildingAIGetColor(buildingId, ref data, infoMode, ref __result);
        }

        /// <summary>
        /// remove Harmony patches
        /// </summary>
        public static void RemovePatches()
        {
            try
            {
                // remove Harmony patches
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    new Harmony(HarmonyId).UnpatchAll(HarmonyId);
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // ignore missing Harmony, rethrow all others
                if (!ex.FileName.ToUpper().Contains("HARMONY"))
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }
    }
}
