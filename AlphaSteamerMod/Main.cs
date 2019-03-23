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

    // 3 times less max coal consumption 
    [HarmonyPatch(typeof(SteamLocoSimulation), "Awake")]
    class SteamLocoSimulation_Awake_Patch
    {
        static void Postfix(SteamLocoSimulation __instance)
        {
            __instance.maxCoalConsumptionRate *= 0.333f;
        }
    }

    // 3 times less coal consumption and reduce coal temperature gain by 30%
    [HarmonyPatch(typeof(SteamLocoSimulation), "SimulateBlowerDraftFireCoalTemp")]
    class SteamLocoSimulation_SimulateBlowerDraftFireCoalTemp_Patch
    {
        static void Postfix(SteamLocoSimulation __instance, float deltaTime)
        {
            if (__instance.fireOn.value > 0.01f && __instance.coalbox.value > 0.01f)
            {
                __instance.coalbox.AddNextValue(__instance.coalConsumptionRate * deltaTime * 0.666f);
            }

            if (__instance.fireOn.value > 0.99f && __instance.coalbox.value > 0.01f)
            {
                __instance.temperature.AddNextValue((650.0f * (__instance.coalbox.value / 350.0f) - __instance.temperature.value / 16.0f) * deltaTime * -0.3f);
            }
        }
    }

    // reduce water consumption by 50%
    [HarmonyPatch(typeof(SteamLocoSimulation), "SimulateSteam")]
    class SteamLocoSimulation_SimulateSteam_Patch
    {
        static void Postfix(SteamLocoSimulation __instance, float deltaTime)
        {
            if (__instance.temperature.value >= 100.0f && (__instance.boilerWater.value > 0.01f && __instance.boilerPressure.value < __instance.boilerPressure.max * 0.999f))
            {
                var waterRemoved = 0.200000002980232f * __instance.temperature.value * deltaTime * 0.5f;

                __instance.boilerWater.AddNextValue(waterRemoved);
            }
        }
    }

    // reduce wheelslip by 10%
    [HarmonyPatch(typeof(TrainCar), "Awake")]
    class TrainCar_Awake_Patch
    {
        static void Postfix(TrainCar __instance)
        {
            if (__instance.carType == TrainCarType.LocoSteamHeavy)
            {
                var drivingForce = __instance.GetComponent<DrivingForce>();

                var wheelslipToFrictionModifierCurve = new AnimationCurve();
                wheelslipToFrictionModifierCurve.AddKey(0f, 0.35f);
                wheelslipToFrictionModifierCurve.AddKey(0.25f, 0.35f);
                wheelslipToFrictionModifierCurve.AddKey(1f, 0.0f);

                drivingForce.wheelslipToFrictionModifierCurve = wheelslipToFrictionModifierCurve;
            }
        }
    }
}