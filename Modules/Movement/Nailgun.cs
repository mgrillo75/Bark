using Bark.Extensions;
using Bark.GUI;
using Bark.Interaction;
using Bark.Modules.Teleportation;
using Bark.Tools;
using GorillaLibrary.Models;
using GorillaLocomotion.Climbing;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.XR;

namespace Bark.Modules.Movement
{
    public class NailGun : BarkModule
    {
        public static readonly string DisplayName = "Nail Gun";
        public static GameObject launcherPrefab, nailPrefab;
        public GameObject launcher;
        public GameObject[] nails = new GameObject[0];

        AudioSource audioFire;
        GameObject barrel;
        XRNode hand;

        protected override void OnEnable()
        {

            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                if (!launcherPrefab)
                {
                    launcherPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Nail Gun");
                    nailPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Nail");
                }

                launcher = Instantiate(launcherPrefab);
                audioFire = launcher.GetComponent<AudioSource>();
                barrel = launcher.transform.Find("Barrel").gameObject;

                ReloadConfiguration();
                HideLauncher();

            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void ShowLauncher(InputTracker _)
        {
            launcher.GetComponent<MeshRenderer>().enabled = true;
            GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand);
        }

        void HideLauncher()
        {
            launcher.GetComponent<MeshRenderer>().enabled = false; ;
        }

        void Fire(InputTracker _)
        {
            if (!launcher.activeSelf) return;
            audioFire.Play();
            try
            {
                nails[nextNail]?.Obliterate();
                nails[nextNail] = MakeNail();
                nextNail = MathExtensions.Wrap(nextNail + 1, 0, nails.Length);
                GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand, 1, .25f);
            }
            catch (Exception e) { Logging.Exception(e); }
            HideLauncher();
        }

        int nextNail;
        GameObject MakeNail()
        {
            try
            {
                GameObject nail = Instantiate(nailPrefab);
                Vector3? end = GetEndpoint(barrel.transform.position, barrel.transform.forward);
                if (!end.HasValue) return null;
                nail.transform.position = end.Value;
                nail.transform.rotation = barrel.transform.rotation;
                nail.AddComponent<GorillaClimbable>();
                return nail;
            }
            catch (Exception e) { Logging.Exception(e); }
            return null;
        }

        Vector3? GetEndpoint(Vector3 origin, Vector3 forward)
        {
            UnityEngine.Physics.Raycast(new(origin, forward), out RaycastHit hit, Mathf.Infinity, Teleport.layerMask);
            if (!hit.collider) return null; //if it hits nothing, return null
            return hit.point;
        }

        protected override void Cleanup()
        {
            if (!MenuController.Instance.Built) return;
            UnsubscribeFromEvents();
            launcher?.Obliterate();
            if (nails is null) return;
            foreach (var nail in nails)
                nail?.Obliterate();
        }

        public static MelonPreferences_Entry<int> MaxNailGuns;
        public static MelonPreferences_Entry<string> LauncherHand;
        public static MelonPreferences_Entry<int> GravityMultiplier;

        protected override void ReloadConfiguration()
        {
            ResizeArray(MaxNailGuns.Value * 4);
            UnsubscribeFromEvents();

            hand = LauncherHand.Value == "left"
                ? XRNode.LeftHand : XRNode.RightHand;

            Parent();

            InputTracker grip = GestureTracker.Instance.GetInputTracker("grip", hand);
            InputTracker trigger = GestureTracker.Instance.GetInputTracker("trigger", hand);

            trigger.OnPressed += ShowLauncher;
            trigger.OnReleased += Fire;
        }

        public void ResizeArray(int newLength)
        {
            if (newLength < 0)
            {
                Logging.Warning("Cannot resize array to a negative length.");
                return;
            }

            // Check if the new length is smaller than the current length
            if (newLength < nails.Length)
                for (int i = newLength; i < nails.Length; i++)
                    nails[i]?.Obliterate();

            if (nextNail >= nails.Length)
                nextNail = 0;

            // Resize the array
            Array.Resize(ref nails, newLength);
        }

        void Parent()
        {
            Transform parent = GestureTracker.Instance.rightHand.transform;
            float x = -1;
            if (hand == XRNode.LeftHand)
            {
                parent = GestureTracker.Instance.leftHand.transform;
                x = 1;
            }

            launcher.transform.SetParent(parent, true);
            launcher.transform.localPosition = new Vector3(0.4782f * x, 0.1f, 0.4f);
            launcher.transform.localRotation = Quaternion.Euler(20, 0, 0);
            launcher.transform.localScale = Vector3.one * 18;
        }

        void UnsubscribeFromEvents()
        {
            if (!GestureTracker.Instance) return;
            InputTracker trigger = GestureTracker.Instance.GetInputTracker("trigger", hand);
            trigger.OnPressed -= ShowLauncher;
            trigger.OnReleased -= Fire;
        }

        public static void BindConfigEntries()
        {
            MelonPreferences_Category category = Melon<Plugin>.Instance.CreateCategory(DisplayName, DisplayName);

            MaxNailGuns = category.CreateEntry("max nails", 5, "Max Nails", "Maximum number of nails that can exist at one time (multipled by 4)", false, false, null);
            LauncherHand = category.CreateEntry("nailgun hand", "left", "Nail Gun Hand", "Which hand holds the nail gun", false, false, new ValueList<string>("left", "right"));
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            string h = LauncherHand.Value[..1].ToUpper() + LauncherHand.Value[1..];
            return $"Hold [{h} Trigger] to summon the nailgun. Release [{h} Trigger] to fire a climbable nail. " +
                $"Grip the nail to climb it.";
        }
    }
}
