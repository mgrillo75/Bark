using MelonLoader;
using System.Reflection;

namespace Bark.Patches
{
    /// <summary>
    /// This class handles applying harmony patches to the game.
    /// You should not need to modify this class.
    /// </summary>
    public class HarmonyPatches
    {
        private static HarmonyLib.Harmony instance;

        public static bool IsPatched { get; private set; }

        internal static void ApplyHarmonyPatches()
        {
            if (!IsPatched)
            {
                instance ??= new HarmonyLib.Harmony(Melon<Plugin>.Instance.Info.Name);

                instance.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance != null && IsPatched)
            {
                instance.UnpatchSelf();
                IsPatched = false;
            }
        }
    }
}
