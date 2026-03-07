using Bark.Modules.Movement;
using Bark.Tools;
using HarmonyLib;
using System;
using UnityEngine;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.Patches
{
    [HarmonyPatch(typeof(GorillaTagManager))]
    [HarmonyPatch(nameof(GorillaTagManager.LocalPlayerSpeed), MethodType.Normal)]
    internal class TagSpeedPatch
    {
        private static void Postfix(ref float[] __result)
        {
            try
            {
                if (!SpeedBoost.active) return;

                for (var i = 0; i < __result.Length; i++)
                    __result[i] *= SpeedBoost.scale;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }

    [HarmonyPatch(typeof(GorillaGameManager))]
    [HarmonyPatch(nameof(GorillaGameManager.LocalPlayerSpeed), MethodType.Normal)]
    internal class GenericSpeedPatch
    {
        private static void Postfix(ref float[] __result)
        {
            try
            {
                if (!SpeedBoost.active) return;

                for (int i = 0; i < __result.Length; i++)
                    __result[i] *= SpeedBoost.scale;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }

    [HarmonyPatch(typeof(GorillaPaintbrawlManager))]
    [HarmonyPatch(nameof(GorillaPaintbrawlManager.LocalPlayerSpeed), MethodType.Normal)]
    internal class BattleSpeedPatch
    {
        private static void Postfix(ref float[] __result)
        {
            try
            {
                if (!SpeedBoost.active) return;

                for (int i = 0; i < __result.Length; i++)
                    __result[i] *= SpeedBoost.scale;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }

    [HarmonyPatch(typeof(GorillaHuntManager))]
    [HarmonyPatch(nameof(GorillaHuntManager.LocalPlayerSpeed), MethodType.Normal)]
    internal class HuntSpeedPatch
    {
        private static void Postfix(ref float[] __result)
        {
            try
            {
                if (!SpeedBoost.active) return;

                for (int i = 0; i < __result.Length; i++)
                    __result[i] *= SpeedBoost.scale;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }

    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("GetSwimmingVelocityForHand", MethodType.Normal)]
    internal class SwimmingVelocityPatch
    {
        private static void Postfix(ref Vector3 swimmingVelocityChange)
        {
            try
            {
                if (!SpeedBoost.active) return;
                swimmingVelocityChange *= SpeedBoost.scale;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
