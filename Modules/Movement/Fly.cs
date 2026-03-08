using Bark.Extensions;
using Bark.GUI;
using GorillaLibrary.Utilities;
using MelonLoader;
using UnityEngine;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.Modules.Movement
{
    public class Fly : BarkModule
    {
        public static readonly string DisplayName = "Fly";
        float speedScale = 10, acceleration = .01f;
        Vector2 xz;
        float y;
        void FixedUpdate()
        {
            // nullify gravity by adding it's negative value to the player's velocity
            var rb = Player.Instance.bodyCollider.attachedRigidbody;
            if (BarkModule.enabledModules.ContainsKey(Bubble.DisplayName)
                && !BarkModule.enabledModules[Bubble.DisplayName])
                rb.AddForce(-UnityEngine.Physics.gravity * rb.mass * Player.Instance.scale);

            xz = InputUtility.LeftStickAxis.GetValue();
            y = InputUtility.RightStickAxis.GetValue().y;

            Vector3 inputDirection = new(xz.x, y, xz.y);

            // Get the direction the player is facing but nullify the y axis component
            var playerForward = Player.Instance.bodyCollider.transform.forward;
            playerForward.y = 0;

            // Get the right vector of the player but nullify the y axis component
            var playerRight = Player.Instance.bodyCollider.transform.right;
            playerRight.y = 0;

            var velocity =
                inputDirection.x * playerRight +
                y * Vector3.up +
                inputDirection.z * playerForward;
            velocity *= Player.Instance.scale * speedScale;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velocity, acceleration);
        }

        public override string GetDisplayName()
        {
            return "Fly";
        }

        public override string Tutorial()
        {
            return "Use left stick to fly horizontally, and right stick to fly vertically.";
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            ReloadConfiguration();
        }

        public static MelonPreferences_Entry<int> Speed;
        public static MelonPreferences_Entry<int> Acceleration;
        protected override void ReloadConfiguration()
        {
            speedScale = Speed.Value * 2;
            acceleration = Acceleration.Value;
            if (acceleration == 10)
                acceleration = 1;
            else
                acceleration = MathExtensions.Map(Acceleration.Value, 0, 10, 0.0075f, .25f);
        }

        public static void BindConfigEntries()
        {
            MelonPreferences_Category category = Melon<Plugin>.Instance.CreateCategory(DisplayName, DisplayName);

            Speed = category.CreateEntry("speed", 5, "Speed", "How fast you fly", false, false, null);
            Acceleration = category.CreateEntry("acceleration", 5, "Acceleration", "How fast you accelerate", false, false, null);
        }

        protected override void Cleanup() { }
    }
}
