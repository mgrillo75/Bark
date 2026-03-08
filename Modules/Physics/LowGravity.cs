using Bark.GUI;
using MelonLoader;
using UnityEngine;

namespace Bark.Modules.Physics
{
    public class LowGravity : BarkModule
    {
        public static readonly string DisplayName = "Gravity";
        public static LowGravity Instance;
        Vector3 baseGravity;
        public float gravityScale = .25f;
        public bool active { get; private set; }

        void Awake()
        {
            Instance = this;
            baseGravity = UnityEngine.Physics.gravity;
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            ReloadConfiguration();
            active = true;
        }

        protected override void Cleanup()
        {
            if (!active) return;
            UnityEngine.Physics.gravity = baseGravity;
            active = false;
        }

        protected override void ReloadConfiguration()
        {
            gravityScale = Multiplier.Value / 5f;
            gravityScale = Mathf.Pow(gravityScale, 2f);
            UnityEngine.Physics.gravity = baseGravity * gravityScale;
        }

        public static MelonPreferences_Entry<int> Multiplier;
        public static void BindConfigEntries()
        {
            MelonPreferences_Category category = Melon<Plugin>.Instance.CreateCategory(DisplayName, DisplayName);

            Multiplier = category.CreateEntry("multiplier", 2, "Multiplier", "How strong gravity will be (0 = no gravity, 5 = normal gravity, 10 = 2x jupiter gravity)", false, false, null);
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Changes the strength of gravity. \n\nYou can modify the strength in the settings menu.";
        }

    }
}
