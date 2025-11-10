using Bark.Networking;
using HarmonyLib;
using Photon.Realtime;
using System;
using Object = UnityEngine.Object;

namespace Bark.Patches
{
    [HarmonyPatch(typeof(RigContainer))]
    public class RigContainerPatches
    {
        public static Action<NetPlayer, VRRig> OnRigCached;

        [HarmonyPatch("set_Creator"), HarmonyPostfix]
        public static void CreatorPatch(RigContainer __instance, NetPlayer value)
        {
            if (__instance.GetComponent<NetworkedPlayer>()) return;

            var np = __instance.gameObject.AddComponent<NetworkedPlayer>();
            np.owner = value;
            np.rig = __instance.Rig;

            try
            {
                NetworkPropertyHandler.Instance?.networkedPlayers.Add(value, np);

                Player playerRef = value.GetPlayerRef();
                NetworkPropertyHandler.Instance?.OnPlayerPropertiesUpdate(playerRef, playerRef.CustomProperties);
            }
            catch
            {

            }
        }

        [HarmonyPatch(nameof(RigContainer.OnDisable)), HarmonyPostfix]
        public static void DisablePatch(RigContainer __instance)
        {
            if (__instance.TryGetComponent<NetworkedPlayer>(out var np))
            {
                OnRigCached?.Invoke(np.owner, __instance.Rig);

                try
                {
                    NetworkPropertyHandler.Instance?.networkedPlayers.Remove(np.owner);
                }
                catch
                {

                }

                Object.Destroy(np);
            }
        }
    }
}