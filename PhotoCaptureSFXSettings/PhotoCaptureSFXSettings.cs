using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace PhotoCaptureSFXSettings
{
    public class PhotoCaptureSFXSettings : ResoniteMod
    {
        internal const string VERSION = "1.0.0";

        private static ModConfiguration _config;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> DisablePhotoCaptureSFX =
            new("Disable shutter SFX", "Enable or disable PhotoCapture sfx", () => false);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> DisableTimerPhotoCaptureSFX =
            new("Disable Timer shutter SFX", "Enable or disable timer PhotoCapture sfx", () => false);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> PhotoCaptureVolume =
            new("Shutter volume", "PhotoCapture volume", () => 0.1f);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> TimerPhotoCaptureVolume =
            new("Timer shutter volume", "Timer PhotoCapture volume", () => 0.1f);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> PhotoCaptureSound =
            new("Shutter sound", "PhotoCapture sound Record URI", () => "");

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<string> TimerPhotoCaptureSound =
            new("Timer shutter sound", "Timer PhotoCapture sound Record URI", () => "");

        public override string Name => "PhotoCaptureSFXSettings";
        public override string Author => "Meister1593";
        public override string Version => VERSION;
        public override string Link => "https://github.com/Meister1593/PhotoCaptureSFXSettings";

        public override void OnEngineInit()
        {
            _config = GetConfiguration();
            new Harmony("net.meister1593.PhotoCaptureSFXSettings").PatchAll();
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayCaptureSound))]
        public class SteamScreenshots_OnAttach_ChangeShutterSound_Patch
        {
            private static bool Prefix(PhotoCaptureManager instance)
            {
                if (_config.GetValue(DisablePhotoCaptureSFX))
                {
                    Msg("Skipping shutter sfx sound");
                    return false;
                }

                var shutterSound = _config.GetValue(PhotoCaptureSound);
                if (string.IsNullOrEmpty(shutterSound))
                {
                    Msg("Not modifying sound of shutter");
                    return true;
                }

                var shutterUri = new Uri(shutterSound);
                var shutterClip = Traverse.Create(instance).Field<AssetRef<AudioClip>>("_shutterClip").Value;
                shutterClip.Target.Asset.SetURL(shutterUri);
                Msg("Modifying sound of shutter");
                return true;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayTimerStartSound))]
        public class SteamScreenshots_OnAttach_ChangeTimerShutterSound_Patch
        {
            private static bool Prefix(PhotoCaptureManager instance)
            {
                if (_config.GetValue(DisableTimerPhotoCaptureSFX))
                {
                    Msg("Skipping timer shutter sfx sound");
                    return false;
                }


                var timerShutterSound = _config.GetValue(TimerPhotoCaptureSound);
                if (string.IsNullOrEmpty(timerShutterSound))
                {
                    Msg("Not modifying sound of timer shutter");
                    return true;
                }

                var timerShutterUri = new Uri(timerShutterSound);
                var timerStartClip = Traverse.Create(instance).Field<AssetRef<AudioClip>>("_timerStartClip").Value;
                var previewRoot = Traverse.Create(instance).Field<SyncRef<Slot>>("_previewRoot").Value;
                if (timerStartClip.Asset == null)
                {
                    timerStartClip.Target = previewRoot.Target.AttachAudioClip(timerShutterUri);
                }
                else
                {
                    timerStartClip.Target.Asset.SetURL(timerShutterUri);
                }

                Msg("Modifying sound of timer shutter");
                return true;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayCaptureSound))]
        public static class SteamScreenshots_PlayCaptureSound_ShutterVolume_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var shutterVolume = _config.GetValue(PhotoCaptureVolume);
                var currentInstructions = instructions.ToList();
                // Looking for specific call to find argument to modify volume
                for (var i = 0; i < currentInstructions.Count; i++)
                {
                    if (currentInstructions[i].opcode != OpCodes.Ldfld) continue;
                    Msg("Ldfld");

                    if ((currentInstructions[i].operand as FieldInfo)?.Name != "_shutterClip") continue;
                    Msg("_shutterClip");

                    if (currentInstructions[i - 1].opcode != OpCodes.Ldarg_0) continue;
                    Msg("Ldarg_0");

                    if (currentInstructions[i + 1].opcode != OpCodes.Callvirt) continue;
                    Msg("Callvirt");

                    if (currentInstructions[i + 2].opcode != OpCodes.Ldc_R4) continue;
                    currentInstructions[i + 2].operand = shutterVolume;
                    Msg("Modified volume operand to new float value");
                    break;
                }

                Msg("Finished patching shutter volume");
                return currentInstructions;
            }
        }

        [HarmonyPatch(typeof(PhotoCaptureManager), nameof(PhotoCaptureManager.PlayTimerStartSound))]
        public static class SteamScreenshots_PlayCaptureSound_TimerShutterVolume_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var shutterVolume = _config.GetValue(TimerPhotoCaptureVolume);
                var currentInstructions = instructions.ToList();
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
                    break;
                }

                Msg("Finished patching timer shutter volume");
                return currentInstructions;
            }
        }
    }
}