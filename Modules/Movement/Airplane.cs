using Bark.GUI;
using Bark.Interaction;
using Bark.Tools;
using GorillaLibrary.Utilities;
using GorillaLocomotion;
using MelonLoader;
using UnityEngine;

namespace Bark.Modules.Movement
{
    public class Airplane : BarkModule
    {
        public static readonly string DisplayName = "Airplane";
        private float speedScale = 10f;
        private readonly float acceleration = .1f;

        void OnGlide(Vector3 direction)
        {
            if (!enabled) return;

            if (
                InputUtility.LeftTrigger.pressed ||
                InputUtility.RightTrigger.pressed ||
                InputUtility.LeftGrip.pressed ||
                InputUtility.RightGrip.pressed) return;

            var player = GTPlayer.Instance;
            if (player.LeftHand.wasColliding || player.RightHand.wasColliding) return;

            if (SteerWith.Value == "head")
                direction = player.headCollider.transform.forward;

            Vector3 velocity = direction * player.scale * speedScale;
            player.SetVelocity(Vector3.Lerp(player.RigidbodyVelocity, velocity, acceleration));
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            ReloadConfiguration();
            GestureTracker.Instance.OnGlide += OnGlide;
        }

        protected override void Cleanup()
        {
            if (!MenuController.Instance.Built) return;
            if (!GestureTracker.Instance) return;
            GestureTracker.Instance.OnGlide -= OnGlide;
        }

        public static MelonPreferences_Entry<int> Speed;
        public static MelonPreferences_Entry<string> SteerWith;
        protected override void ReloadConfiguration()
        {
            speedScale = Speed.Value * 2;
        }

        public static void BindConfigEntries()
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory(DisplayName, DisplayName);
            category.SetFilePath(UserDataPath);

            Speed = category.CreateEntry("speed", 5, "Speed", "How fast you fly", false, true, null);
            SteerWith = category.CreateEntry("steerWith", "wrists", "Steer With", "Which part of your body you use to steer", false, false, new ValueList<string>("wrists", "head"));
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "- To fly, do a T-pose (spread your arms out like wings on a plane). \n" +
                "- To fly up, point your thumbs up. \n" +
                "- To fly down, point your thumbs down.";
        }

    }
}


