using BepInEx;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using R2API.Networking;
using R2API;
using BepInEx.Configuration;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Path = System.IO.Path;
using System.Diagnostics;
using UnityEngine.Audio;
using UnityEngine.Networking;
using RoR2.ConVar;
using RoR2.UI;
using System.Runtime.CompilerServices;

namespace Evaisa.VoiceChat
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("com.x753.AudioEngineFix")]
    [BepInDependency("com.DrBibop.VRMod", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]
    [R2APISubmoduleDependency(nameof(NetworkingAPI), nameof(LoadoutAPI), nameof(SurvivorAPI), nameof(LanguageAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(EffectAPI))]
    [BepInPlugin(dependencyString, modName, version)]
    public class VoiceChat : BaseUnityPlugin
    {
        public const string dependencyString = "com.evaisa.r2voicechat";
        public const string modName = "r2voicechat";
        public const string version = "1.0.0";

        public static VoiceChat instance;
        public static GameObject audioPlayerPrefab;
        public static float audioMasterMultiplier = 0f;

        public static bool notInitialized = true;
        public static string assemblyLocation;
        public static KeyCode pushToTalkKey;
        public static GameObject voiceController;

        public bool VRModInstalled = false;

        public static SettingsApi.SliderSetting micVolume;
        public static SettingsApi.SliderSetting micBoost;
        public static SettingsApi.SliderSetting voiceVolume;
        public static SettingsApi.BoolSetting voiceActivation;
        public static SettingsApi.SliderSetting voiceSensitivity;
        public static SettingsApi.BoolSetting spatialSound;
        public static SettingsApi.BoolSetting vrControls;
        public static SettingsApi.CarouselSetting microphone;
        public static SettingsApi.CarouselSetting bitRate;
        public static SettingsApi.BoolSetting loopBackAudio;
        public static SettingsApi.BoolSetting lowLatencyMode;

        public VoiceChat()
        {
            
            instance = this;

            Inputs.Init();
            InputBehaviours.Init();
            
            Settings.Init();


            NetworkingAPI.RegisterMessageType<VoiceChatController.ForwardVoiceChat>();
            NetworkingAPI.RegisterMessageType<VoiceChatController.SendVoiceChat>();
            NetworkingAPI.RegisterMessageType<VoiceChatController.PassFrequency>();

            assemblyLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var voiceControllerPrefab = new GameObject();

            voiceControllerPrefab.AddComponent<VoiceChatController>();
            voiceControllerPrefab.AddComponent<NetworkIdentity>();
            voiceControllerPrefab.SetActive(false);

            audioPlayerPrefab = voiceControllerPrefab.InstantiateClone("VoiceChatController", true);

            DestroyImmediate(voiceControllerPrefab);

            if (Microphone.devices.Length > 0)
            {
                microphone = SettingsApi.RegisterCarouselSetting("Microphone", "The microphone you want to use.", Microphone.devices[0], Microphone.devices.ToList(), "Voicechat");
            }
            else
            {
                microphone = SettingsApi.RegisterCarouselSetting("Microphone", "The microphone you want to use.", "null", new List<string>() { "null" }, "Voicechat");
            }

            
            micVolume = SettingsApi.RegisterSliderSetting("Microphone Volume", "The volume of your microphone.", 100, 0, 100, "Voicechat");
            micBoost = SettingsApi.RegisterSliderSetting("Microphone Boost", "Additional boost of your microphone volume.", 0, 0, 100, "Voicechat");
            voiceVolume = SettingsApi.RegisterSliderSetting("Voicechat Volume", "The volume of other people in voicechat.", 100, 0, 100, "Voicechat");
            voiceActivation = SettingsApi.RegisterBoolSetting("Voice Activation", "Use voice activation rather than push to talk.", false, "Voicechat");
            voiceSensitivity = SettingsApi.RegisterSliderSetting("Voice Sensitivity", "The sensitivity of the voice activation. Lower is more sensitive.", 50, 0, 100, "Voicechat");
            spatialSound = SettingsApi.RegisterBoolSetting("[Host Only] Proximity Voicechat", "Use proximity voicechat, voices come from player characters. \n\nThis setting is host only.", false, "Voicechat");
            vrControls = SettingsApi.RegisterBoolSetting("VR Gesture Control", "Enable VR controls, allowing you to voicechat by pressing your controller to your ear.", true, "Voicechat");
            lowLatencyMode = SettingsApi.RegisterBoolSetting("[Host Only] Low Latency Mode", "Experimental Low latency mode, should reduce latency but may cause more strain on the network connection and might cause a hit on audio quality. \n\nThis setting is host only. \n\nThis setting cannot be changed while in a match.", false, "Voicechat");
            bitRate = SettingsApi.RegisterCarouselSetting("[Host Only] Bitrate", "The bit rate of the voice chat, higher increases quality but might introduce latency if it cannot be handled by the connection. \n\nThis setting is host only. \n\nThis setting cannot be changed while in a match.", "22000", new List<string>() { "8000", "11000", "16000", "22000", "32000", "44100", "48000", "64000" }, "Voicechat");
            loopBackAudio = SettingsApi.RegisterBoolSetting("[Debugging] Audio Loopback", "Loops back your own microphone input, note that it is very delayed due to latency optimizations. Mostly meant for debugging.", false, "Voicechat");
            

            SettingsApi.Init();

        }


        public void Awake() {
            
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DrBibop.VRMod")) VRModInstalled = true;
            On.RoR2.Stage.Start += Stage_Start;

            On.RoR2.AudioManager.VolumeConVar.SetString += (orig, self, newValue) =>
            {
                orig(self, newValue);

                if (self.name == "volume_master")
                {
                    audioMasterMultiplier = (float.Parse(newValue, CultureInfo.InvariantCulture) / 100);
                }
            };

            On.RoR2.AudioManager.Awake += (orig, self) =>
            {
                orig(self);
                Print(AudioManager.cvVolumeMaster.GetString());
                audioMasterMultiplier = (float.Parse(AudioManager.cvVolumeMaster.GetString(), CultureInfo.InvariantCulture) / 100);
            };
        }


        private void Stage_Start(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);

            notInitialized = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void UpdateVRPushToTalk()
        {
            if ((bool)(typeof(VRMod.MotionControls).GetProperty("HandsReady", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)))
            {
                Transform Hand1Muzzle = VRMod.MotionControls.GetHandCurrentMuzzle(true);
                Transform Hand2Muzzle = VRMod.MotionControls.GetHandCurrentMuzzle(false);
                Vector3 handPosition = Hand1Muzzle.position + (Hand1Muzzle.forward * -0.2f);
                Vector3 handPosition2 = Hand2Muzzle.position + (Hand2Muzzle.forward * -0.2f);

                Vector3 headPosition = Camera.main.transform.position + Camera.main.transform.forward * -0.2f;
                Vector3 earPosition = headPosition + Camera.main.transform.right * 0.22f;
                Vector3 earPosition2 = headPosition + Camera.main.transform.right * -0.22f;


                if (Vector3.Distance(handPosition, earPosition) < 0.2f || Vector3.Distance(handPosition, earPosition2) < 0.2f || Vector3.Distance(handPosition2, earPosition) < 0.2f || Vector3.Distance(handPosition2, earPosition2) < 0.2f)
                {
                    VoiceChatController.isUsingVRTalk = true;
                }
                else
                {
                    VoiceChatController.isUsingVRTalk = false;
                }
            }
        }



        public void Update()
        {

            if (Run.instance && NetworkServer.active && notInitialized)
            {
                voiceController = Instantiate(audioPlayerPrefab, transform.position, Quaternion.identity);
                voiceController.SetActive(true);
                NetworkServer.Spawn(voiceController);

                notInitialized = false;
            }

            if (VRModInstalled && vrControls.GetValue())
            {
                UpdateVRPushToTalk();
            }

        }

        public static object stringToKeyCode(string stringCode)
        {
            KeyCode keyCode;
            if (Enum.TryParse<KeyCode>(stringCode, out keyCode))
            {
                return keyCode;
            }
            else
            {
                return null;
            }
        }

        public static string keyCodeToString(KeyCode keyCode)
        {
            return keyCode.ToString();
        }


        public float Map(float x, float in_min, float in_max, float out_min, float out_max, bool clamp = false)
        {
            if (clamp) x = Math.Max(in_min, Math.Min(x, in_max));
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
        public static void Print(object content)
        {
            UnityEngine.Debug.Log("[Voice Chat] " + (content.ToString()));
        }

    }

}