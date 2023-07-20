using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FrooxEngine;

namespace PhotoSFXSettings
{
    public class PhotoSFXSettings : NeosMod
    {
        internal const string VERSION = "0.0.9";

        public override string Name => "PhotoSFXSettings";
        public override string Author => "Meister1593";
        public override string Version => VERSION;
        public override string Link => "https://github.com/Meister1593/PhotoSFXSettings";

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            new Harmony("net.meister1593.PhotoSFXSettings").PatchAll();
        }

        private static ModConfiguration Config;

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> DisableShutterSFX =
            new("Path to screenshots", "Path to screenshots folder in OS", () => true);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> ShutterVolume =
            new("Path to screenshots", "Path to screenshots folder in OS", () => 0.1f);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<string> ShutterSound =
            new("Path to screenshots", "Path to screenshots folder in OS", () => "");

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayCaptureSound))]
        public class SteamScreenshots_AddVRScreenshotToLibrary_Patch
        {
            static bool Prefix()
            {
                if (Config.GetValue(DisableShutterSFX))
                {
                    return true; // Skip playing sound
                }

                var shutterVolume = Config.GetValue(ShutterVolume);
                var shutterSound = Config.GetValue(ShutterSound);
                if (string.IsNullOrEmpty(shutterSound))
                {
                    shutterSound = NeosAssets.Neos.Sound_Effects.Tools.CameraShutter.ToString();
                }

                Msg($"Path to screenshot saved:");
                return false;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayCaptureSound))]
        public static class SteamScreenshots_PlayCaptureSound_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var shutterVolume = Config.GetValue(ShutterVolume);
                var shutterVolumeChangeInstructions = new List<CodeInstruction>
                {
                    new(OpCodes.Ldc_R4, shutterVolume),
                };
                var currentInstructions = instructions.ToList();
                var newInstructions =
                    new List<CodeInstruction>(currentInstructions.Count + shutterVolumeChangeInstructions.Count);

                // I'm looking for a very specific pattern where to insert my instructions.
                for (var i = 0; i < currentInstructions.Count; i++)
                {
                    var currentInstruction = currentInstructions[i];
                    if (currentInstructions[i].opcode == OpCodes.Ldfld)
                    {
                        Msg("Found Ldfld");
                        if ((currentInstructions[i].operand as FieldInfo)?.Name == "_shutterClip")
                        {
                            Msg("Found _shutterClip");
                            var previousInstruction = currentInstructions[i - 1];
                            if (previousInstruction.opcode == OpCodes.Ldarg_0)
                            {
                                Msg("Found Ldarg_0");
                                newInstructions.AddRange(shutterVolumeChangeInstructions);
                            }
                        }
                    }

                    // I'm adding back all read instructions since I'm not removing anything.
                    newInstructions.Add(currentInstruction);
                }

                return newInstructions;
            }
        }
    }
}