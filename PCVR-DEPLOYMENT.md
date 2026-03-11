# Bark PCVR Deployment

This repository is set up for **Windows PCVR Gorilla Tag**, not standalone Quest Android. Use your Meta Quest 3 over **Link, Air Link, or Steam Link** and run the **Steam** build of Gorilla Tag on the PC.

## Local setup

1. Install Gorilla Tag from Steam.
2. Install the PC loader/runtime expected by this branch so the game directory contains the MelonLoader managed assemblies.
3. Place `GorillaLibrary.dll` and `GorillaLibrary.GameModes.dll` in the game's `Mods` folder.
4. Copy `Directory.Build.user.props.example` to `Directory.Build.user.props` if you need to override any local paths.
5. Run `.\Test-PCVR-Environment.ps1` and resolve any missing path checks before building.

## Build and package

- `dotnet build -c Release`
  Builds Bark and copies `Bark.dll` into the configured `BarkDeployPath`, which defaults to the game's `Mods` folder.
- `.\MakeRelease.ps1`
  Validates the environment, builds Release, and creates `artifacts\release\Bark-v<version>-pcvr.zip`.

The zip contains a top-level `Mods` folder so it can be extracted directly into the Gorilla Tag install directory.

## Install and verify on Quest 3

1. Connect the Meta Quest 3 to the PC with Link, Air Link, or Steam Link.
2. Launch Gorilla Tag on the PC.
3. Join a modded lobby.
4. Beat on your chest four times in alternating order to open Bark.
5. Smoke test the core modules:
   - Fly
   - Platforms
   - No Collide
   - Checkpoint
   - Teleport
   - Boxing
   - One networked module with another player

If `dotnet build` fails before compilation starts, use the environment check output to fix the local game/mod dependency paths first.
