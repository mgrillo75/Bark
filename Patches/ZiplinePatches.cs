using Bark.Modules.Movement;
using Bark.Tools;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using HarmonyLib;
using System;
using UnityEngine;

namespace Bark.Patches
{
    [HarmonyPatch(typeof(GorillaZipline))]
    [HarmonyPatch(nameof(GorillaZipline.Update), MethodType.Normal)]
    public class ZiplineUpdatePatch
    {
        private static void Postfix(GorillaZipline __instance, GorillaHandClimber ___currentClimber)
        {
            if (!Plugin.inRoom) return;

            try
            {
                var rockets = Rockets.Instance;
                if (!rockets || !rockets.enabled || !___currentClimber) return;
                Vector3 curDir = __instance.GetCurrentDirection();
                Vector3 rocketDir = rockets.AddedVelocity();
                float speedDelta = Vector3.Dot(curDir, rocketDir) * Time.deltaTime * rocketDir.magnitude * 1000f;
                __instance.currentSpeed += speedDelta;
            }

            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
