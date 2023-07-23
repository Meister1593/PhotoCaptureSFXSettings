using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;

namespace PhotoSFXSettings
{
    public class PhotoSFXSettings : NeosMod
    {
        internal const string VERSION = "0.0.9";

        private static ModConfiguration Config;

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> DisableShutterSFX =
            new("Disable shutter SFX", "Enable or disable shutter sfx", () => true);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> DisableTimerPhotoSFX =
            new("Disable Timer shutter SFX", "Enable or disable timer shutter sfx", () => true);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> ShutterVolume =
            new("Shutter volume", "Shutter volume", () => 0.1f);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> TimerPhotoShutterVolume =
            new("Timer shutter volume", "Timer shutter volume", () => 0.1f);

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<string> ShutterSound =
            new("Shutter sound", "Shutter sound Record URI", () => "");

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<string> TimerShutterSound =
            new("Timer shutter sound", "Timer shutter sound Record URI", () => "");

        public override string Name => "PhotoSFXSettings";
        public override string Author => "Meister1593";
        public override string Version => VERSION;
        public override string Link => "https://github.com/Meister1593/PhotoSFXSettings";

        public override void OnEngineInit()
        {
            Config = GetConfiguration();
            new Harmony("net.meister1593.PhotoSFXSettings").PatchAll();
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayCaptureSound))]
        public class SteamScreenshots_PlayCaptureSound_SoundDisable_Patch
        {
            static bool Prefix()
            {
                if (Config.GetValue(DisableShutterSFX))
                {
                    return true; // Skip playing sound
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayTimerStartSound))]
        public class SteamScreenshots_PlayCaptureSound_TimerSoundDisable_Patch
        {
            static bool Prefix()
            {
                if (Config.GetValue(DisableTimerPhotoSFX))
                {
                    return true; // Skip playing sound
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), "OnAttach")]
        public class SteamScreenshots_OnAttach_ChangeShutterSound_Patch
        {
            static void Postfix(ref AssetRef<AudioClip> ____shutterClip, ref SyncRef<Slot> ____previewRoot)
            {
                var shutterSound = Config.GetValue(ShutterSound);
                if (string.IsNullOrEmpty(shutterSound))
                {
                    Msg("Not modifying sound of shutter");
                    return;
                }

                var shutterUri = new Uri(shutterSound);

                ____shutterClip.Target = ____previewRoot.Target.AttachAudioClip(shutterUri);
                Msg("Modified sound of shutter");
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), "OnAttach")]
        public class SteamScreenshots_OnAttach_ChangeTimerShutterSound_Patch
        {
            static void Postfix(ref AssetRef<AudioClip> ____timerStartClip, ref SyncRef<Slot> ____previewRoot)
            {
                var timerShutterSound = Config.GetValue(TimerShutterSound);
                if (string.IsNullOrEmpty(timerShutterSound))
                {
                    Msg("Not modifying sound of timer shutter");
                    return;
                }

                var timerShutterUri = new Uri(timerShutterSound);

                ____timerStartClip.Target = ____previewRoot.Target.AttachAudioClip(timerShutterUri);
                Msg("Modified sound of timer shutter");
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayCaptureSound))]
        public static class SteamScreenshots_PlayCaptureSound_ShutterVolume_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var shutterVolume = Config.GetValue(ShutterVolume);
                var currentInstructions = instructions.ToList();
                var clipRef = new AssetRef<AudioClip>();
                // Looking for specific call to find argument to modify volume
                for (var i = 0; i < currentInstructions.Count; i++)
                {
                    if (currentInstructions[i].opcode != OpCodes.Ldfld) continue;
                    Msg("Found Ldfld");

                    if ((currentInstructions[i].operand as FieldInfo)?.Name != "_shutterClip") continue;
                    Msg("Found _shutterClip");

                    if (currentInstructions[i - 1].opcode != OpCodes.Ldarg_0) continue;
                    Msg("Found Ldarg_0");

                    if (currentInstructions[i + 1].opcode != OpCodes.Callvirt) continue;
                    Msg("Found Callvirt");

                    if (currentInstructions[i + 2].opcode != OpCodes.Ldc_R4) continue;
                    currentInstructions[i + 2].operand = shutterVolume;
                    Msg("Modified volume operand to new float value");
                }

                Msg("Finished patching shutter volume");
                return currentInstructions;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayTimerStartSound))]
        public static class SteamScreenshots_PlayCaptureSound_TimerShutterVolume_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var shutterVolume = Config.GetValue(ShutterVolume);
                var currentInstructions = instructions.ToList();
                var clipRef = new AssetRef<AudioClip>();
                // Looking for specific call to find argument to modify volume
                for (var i = 0; i < currentInstructions.Count; i++)
                {
                    if (currentInstructions[i].opcode != OpCodes.Ldfld) continue;
                    Msg("Found Ldfld");

                    if ((currentInstructions[i].operand as FieldInfo)?.Name != "_timerStartClip") continue;
                    Msg("Found _timerStartClip");

                    if (currentInstructions[i - 1].opcode != OpCodes.Ldarg_0) continue;
                    Msg("Found Ldarg_0");

                    if (currentInstructions[i + 1].opcode != OpCodes.Callvirt) continue;
                    Msg("Found Callvirt");

                    if (currentInstructions[i + 2].opcode != OpCodes.Ldc_R4) continue;
                    currentInstructions[i + 2].operand = shutterVolume;
                    Msg("Modified timer volume operand to new float value");
                }

                Msg("Finished patching timer shutter volume");
                return currentInstructions;
            }
        }
    }
}