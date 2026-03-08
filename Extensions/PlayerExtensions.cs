using Bark.Modules;
using Bark.Networking;
using GorillaLibrary.Extensions;
using GorillaLibrary.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.Extensions
{
    public static class PlayerExtensions
    {
        public static void AddForce(this Player self, Vector3 v)
        {
            self.AddForce(v, ForceMode.VelocityChange);
        }

        public static void SetVelocity(this Player self, Vector3 v)
        {
            self.SetPlayerVelocity(v);
        }

        public static T GetProperty<T>(this VRRig rig, string key)
        {
            if (rig.GetComponent<RigContainer>()?.Creator is NetPlayer netPlayer)
                return netPlayer.GetProperty<T>(key);
            return default;
        }

        public static bool HasProperty(this VRRig rig, string key)
        {
            if (rig.GetComponent<RigContainer>()?.Creator is NetPlayer netPlayer)
                return netPlayer.HasProperty(key);
            return false;
        }

        public static bool ModuleEnabled(this VRRig rig, string mod)
        {
            if (rig.GetComponent<RigContainer>()?.Creator is NetPlayer netPlayer)
                return netPlayer.ModuleEnabled(mod);
            return false;
        }

        public static T GetProperty<T>(this NetPlayer netPlayer, string key)
        {
            NetworkPropertyHandler.Instance.networkedPlayers.TryGetValue(netPlayer, out var np);
            return np != null && np.properties != null && np.properties.ContainsKey(key) ? (T)np.properties[key] : default;
        }

        public static bool HasProperty(this NetPlayer netPlayer, string key)
        {
            NetworkPropertyHandler.Instance.networkedPlayers.TryGetValue(netPlayer, out var np);
            return np != null && np.properties != null && np.properties.ContainsKey(key);
        }

        public static bool ModuleEnabled(this NetPlayer netPlayer, string mod)
        {
            if (!netPlayer.HasProperty(BarkModule.enabledModulesKey)) return false;
            Dictionary<string, bool> enabledMods = netPlayer.GetProperty<Dictionary<string, bool>>(BarkModule.enabledModulesKey);
            if (enabledMods is null || !enabledMods.ContainsKey(mod)) return false;
            return enabledMods[mod];
        }

        public static VRRig Rig(this Photon.Realtime.Player player) => player.AsNetPlayer().Rig();

        public static VRRig Rig(this NetPlayer netPlayer)
        {
            return RigUtility.GetRig(netPlayer)?.Rig;
        }
    }
}
