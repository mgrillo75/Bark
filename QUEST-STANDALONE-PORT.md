# Bark Quest Standalone Port

This repository is **not directly deployable** to standalone Meta Quest 3 in its current form.

## Why the current build does not transfer to Quest

1. Bark is a **PC MelonLoader mod**, not a Quest-native package.
   - `MelonInfo`, `MelonGame`, and `GorillaMod` are used in [Plugin.cs](./Plugin.cs).
   - The current build expects a Windows Gorilla Tag install and Windows-managed assemblies in [Directory.Build.props](./Directory.Build.props).
2. Bark depends heavily on **GorillaLibrary** and its modded-gamemode helpers.
   - Repo audit count: `28` files reference GorillaLibrary, modded game mode attributes, or `InputTracker`.
3. Bark depends heavily on **MelonLoader config/runtime APIs**.
   - Repo audit count: `25` files reference MelonLoader lifecycle or `MelonPreferences`.
4. Bark ships an embedded **Unity asset bundle**.
   - `Plugin.cs` loads `Resources/barkbundle` through `AssetBundle.LoadFromStream()` in [Tools/AssetUtils.cs](./Tools/AssetUtils.cs).
   - Unity asset bundles are platform-specific, so the current embedded bundle should be treated as a PC asset until rebuilt for Android/Quest.
5. Bark uses multiplayer/game-mode integration that assumes the current PC mod stack.
   - Repo audit count: `6` files reference Photon networking entrypoints directly.

## What a real Quest port requires

1. Pick the actual Quest runtime/package path for Gorilla Tag.
   - Current generic Quest installer path is `QuestPatcher`: https://github.com/Lauriethefish/QuestPatcher
   - Generic Quest native mod template found during research: https://github.com/Lauriethefish/quest-mod-template
   - `scotland2` is not the target here; its own repo describes it as Beat Saber-specific: https://github.com/sc2ad/scotland2
2. Replace the PC plugin lifecycle.
   - `MelonLoader` startup, config, and Harmony bootstrapping need Quest-native equivalents.
3. Replace or re-implement the GorillaLibrary-dependent surface.
   - At minimum: modded-lobby detection, input abstraction, rig/player helpers, and network property helpers.
4. Rebuild the Bark UI asset bundle for Android/Quest.
5. Package the result as a Quest-native mod and install it with QuestPatcher.

## What has already been prepared on this laptop

The following tools were downloaded to:

`C:\Users\MiguelGrillo\Downloads\Bark-Quest-Setup`

- `QuestPatcher\QuestPatcher.exe`
- `platform-tools\adb.exe`

You can validate that setup with:

```powershell
.\Test-Quest-Standalone-Environment.ps1
```

## Practical next engineering steps

1. Confirm Quest developer-mode + USB debugging access with `adb`.
2. Confirm QuestPatcher can see the Gorilla Tag app on the headset.
3. Create a **new Quest-native Bark port project** instead of trying to reuse `Bark.csproj`.
4. Port Bark subsystem-by-subsystem in this order:
   - plugin/bootstrap
   - config/preferences
   - gesture/input tracking
   - menu/UI
   - one simple movement module (`Fly` or `SpeedBoost`)
   - multiplayer/property sync
   - remaining modules
5. Rebuild the asset bundle for Android and test on-device after each subsystem.

## Recommended first milestone

Do **not** try to port all of Bark at once.

The first realistic Quest milestone is:

1. Quest-native plugin loads
2. Menu appears on chest-beat
3. One simple module toggles successfully

Once that works, the rest of the port becomes incremental instead of speculative.
