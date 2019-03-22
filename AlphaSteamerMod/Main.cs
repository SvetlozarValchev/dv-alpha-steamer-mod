using System;
using System.Collections.Generic;
using System.Reflection;
using UnityModManagerNet;
using Harmony12;
using UnityEngine;
using System.Reflection.Emit;

namespace AlphaSteamerMod
{
    public class Main
    {
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }
    }

    // set max speed simulation to 150
    [HarmonyPatch(typeof(SteamLocoSimulation), "InitComponents")]
    class SteamLocoSimulation_InitComponents_Patch
    {
        static void Prefix(SteamLocoSimulation __instance)
        {
            __instance.speed = new SimComponent("Speed", 0.0f, 150f, 1f, 0.0f);
        }
    }

    // set speed curve to 150 and torque by 30%
    [HarmonyPatch(typeof(LocoControllerSteam), "Awake")]
    class LocoControllerSteam_Awake_Patch
    {
        static void Postfix(LocoControllerSteam __instance)
        {
            var tractionTorqueCurve = new AnimationCurve();
            tractionTorqueCurve.AddKey(0f, 1f);
            tractionTorqueCurve.AddKey(50f, 0.85f);
            tractionTorqueCurve.AddKey(150f, 0f);

            __instance.tractionTorqueCurve = tractionTorqueCurve;

            __instance.tractionTorqueMult *= 1.3f;
        }
    }

    // reduce max coal consumption 3 times
    [HarmonyPatch(typeof(SteamLocoSimulation), "Awake")]
    class SteamLocoSimulation_Awake_Patch
    {
        static void Postfix(SteamLocoSimulation __instance)
        {
            __instance.maxCoalConsumptionRate *= 0.333f;
        }
    }

    // reduce coal consumption 3 times
    [HarmonyPatch(typeof(SteamLocoSimulation), "SimulateBlowerDraftFireCoalTemp")]
    class SteamLocoSimulation_SimulateBlowerDraftFireCoalTemp_Patch
    {
        static void Postfix(SteamLocoSimulation __instance, float deltaTime)
        {
            if (__instance.fireOn.value > 0.0f && __instance.coalbox.value > 0.0f)
            {
                __instance.coalbox.AddNextValue(__instance.coalConsumptionRate * deltaTime * 0.666f);
            }
        }
    }

    // reduce water consumption 2 times
    [HarmonyPatch(typeof(SteamLocoSimulation), "SimulateSteam")]
    class SteamLocoSimulation_SimulateSteam_Patch
    {
        static void Postfix(SteamLocoSimulation __instance, float deltaTime)
        {
            if (__instance.temperature.value >= 100.0f && (__instance.boilerWater.value > 0.0f && __instance.boilerPressure.value < __instance.boilerPressure.max * 0.999f))
            {
                var waterRemoved = 0.200000002980232f * __instance.temperature.value * deltaTime * 0.5f;

                __instance.boilerWater.AddNextValue(waterRemoved);
            }
        }
    }
}