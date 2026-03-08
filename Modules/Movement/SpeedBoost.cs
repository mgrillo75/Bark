using Bark.GUI;
using Bark.Tools;
using MelonLoader;
using System;
using Player = GorillaLocomotion.GTPlayer;

namespace Bark.Modules.Movement
{
    public class SpeedBoost : BarkModule
    {
        public static readonly string DisplayName = "Speed Boost";
        public static float baseVelocityLimit, scale = 1.5f;
        public static bool active = false;

        void FixedUpdate()
        {
            string progress = "";
            try
            {
                progress = "Getting Gamemode\n";
                var gameMode = GorillaGameManager.instance?.GameModeName();
                progress = "Checking status\n";
                if (active && (gameMode is null || gameMode == "NONE" || gameMode == "CASUAL"))
                {
                    progress = "Setting multiplier\n";
                    Player.Instance.jumpMultiplier = 1.3f * scale;
                    Player.Instance.maxJumpSpeed = 8.5f * scale;
                }
            }
            catch (Exception e)
            {
                Logging.Debug("GorillaGameManager.instance is null:", GorillaGameManager.instance is null);
                Logging.Debug("GorillaGameManager.instance.GameModeName() is null:", GorillaGameManager.instance?.GameModeName() is null);
                Logging.Debug(progress);
                Logging.Exception(e);
            }
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            active = true;
            baseVelocityLimit = Player.Instance.velocityLimit;
            ReloadConfiguration();
        }

        protected override void Cleanup()
        {
            if (active)
            {
                scale = 1;
                Player.Instance.velocityLimit = baseVelocityLimit;
                active = false;
            }
        }
        protected override void ReloadConfiguration()
        {
            scale = 1 + Speed.Value / 10f;
            if (enabled)
                Player.Instance.velocityLimit = baseVelocityLimit * scale;
        }

        public static MelonPreferences_Entry<int> Speed;

        public static void BindConfigEntries()
        {
            MelonPreferences_Category category = Melon<Plugin>.Instance.CreateCategory(DisplayName, DisplayName);

            Speed = category.CreateEntry("speed", 5, "Speed", "How fast you run while speed boost is active", false, false, null);
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }
        public override string Tutorial()
        {
            return "Effect: Increases your jump strength.";
        }

    }
}
