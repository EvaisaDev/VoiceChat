using System;
using System.Collections.Generic;
using System.Text;
using Rewired;
using RoR2.UI;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using BepInEx.Configuration;
using UnityEngine.Events;
using R2API.Utils;
using RoR2.ConVar;
using System.Globalization;

namespace Evaisa.VoiceChat
{
    internal static class Settings
    {
        internal static void Init()
        {
            On.RoR2.UI.MainMenu.SubmenuMainMenuScreen.OnEnter += (orig, self, controller) =>
            {
                orig(self, controller);
                if (self.submenuPanelPrefab.name == "SettingsPanel")
                    SetupSettings(self.submenuPanelInstance);
            };

            On.RoR2.UI.PauseScreenController.OpenSettingsMenu += (orig, self) =>
            {
                orig(self);
                SetupSettings(self.submenuObject);
            };
        }

        internal static void SetupSettings(GameObject panel)
        {
            HGHeaderNavigationController controller = panel.GetComponent<HGHeaderNavigationController>();

            if (!controller)
                return;

            Transform header = panel.transform.Find("SafeArea/HeaderContainer/Header (JUICED)");
            Transform subPanelArea = panel.transform.Find("SafeArea/SubPanelArea");

            if (!header || !subPanelArea)
                return;

            /*
            GameObject subPanelInstance = SetupSubPanel(subPanelArea);

            GameObject headerInstance = SetupHeader(header);

            LanguageTextMeshController text = headerInstance.GetComponent<LanguageTextMeshController>();
            text.token = "VOICECHAT";

            HGHeaderNavigationController.Header headerInfo = new HGHeaderNavigationController.Header();
            headerInfo.headerButton = headerInstance.GetComponent<HGButton>();
            headerInfo.headerName = "Voicechat";
            headerInfo.tmpHeaderText = headerInstance.GetComponentInChildren<HGTextMeshProUGUI>();
            headerInfo.headerRoot = subPanelInstance;
            */

            List<HGHeaderNavigationController.Header> headerList = controller.headers.ToList();

            //headerList.Add(headerInfo);

            SetupKeybindsKeyboard(headerList);
            SetupKeybindsController(headerList);

            /*
            GameObject subPanel = headerList.First(headerItem => headerItem.headerName == "Audio").headerRoot;

            GameObject sliderSetting = subPanel.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Slider (Master Volume)").gameObject;
            GameObject boolSetting = subPanel.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Bool (Audio Focus)").gameObject;
            */

           // SetupOtherOptions(subPanelInstance, sliderSetting, boolSetting);

            controller.headers = headerList.ToArray();
        }
        


        private static GameObject SetupHeader(Transform parent)
        {
            GameObject categoryToInstantiate = parent.Find("GenericHeaderButton (Graphics)").gameObject;

            GameObject headerInstance = Object.Instantiate(categoryToInstantiate, parent);

            headerInstance.transform.SetSiblingIndex(parent.childCount - 2);
            headerInstance.name = "GenericHeaderButton (Voicechat)";

            return headerInstance;
        }

        private static GameObject SetupSubPanel(Transform parent)
        {
            GameObject subPanelToInstantiate = parent.Find("SettingsSubPanel, Controls (Gamepad)").gameObject;

            GameObject subPanelInstance = Object.Instantiate(subPanelToInstantiate, parent);

            subPanelInstance.name = "SettingsSubPanel, Voicechat";

            Transform instanceLayout = subPanelInstance.transform.Find("Scroll View/Viewport/VerticalLayout");

            foreach (Transform child in instanceLayout)
            {
                Object.Destroy(child.gameObject);
            }

            return subPanelInstance;
        }

        private static void SetupKeybindsKeyboard(List<HGHeaderNavigationController.Header> headerList)
        {
            GameObject subPanel = headerList.First(headerItem => headerItem.headerName == "KeyBinding").headerRoot;
            Transform instanceLayout = subPanel.transform.Find("Scroll View/Viewport/VerticalLayout");

            GameObject keyboardBindingSetting = subPanel.transform.parent.Find("SettingsSubPanel, Controls (M&KB)/Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Binding (Jump)").gameObject;

            Inputs.ActionDef[] actionDefs = Inputs.actionDefs;

            foreach (var actionDef in actionDefs)
            {
                if (actionDef.keyboardMap != KeyboardKeyCode.None)
                    AddBindingSetting(actionDef, keyboardBindingSetting, instanceLayout, false);
            }
        }

        private static void SetupKeybindsController(List<HGHeaderNavigationController.Header> headerList)
        {
            GameObject subPanel = headerList.First(headerItem => headerItem.headerName == "Controller").headerRoot;
            Transform instanceLayout = subPanel.transform.Find("Scroll View/Viewport/VerticalLayout");

            GameObject controllerBindingSetting = subPanel.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Binding (Jump)").gameObject;

            Inputs.ActionDef[] actionDefs = Inputs.actionDefs;

            foreach (var actionDef in actionDefs)
            {
                if (actionDef.joystickMap != Inputs.ControllerInput.None)
                    AddBindingSetting(actionDef, controllerBindingSetting, instanceLayout, false);
            }
        }

        private static void SetupOtherOptions(GameObject subPanel, GameObject sliderSetting, GameObject boolSetting)
        {
            Transform instanceLayout = subPanel.transform.Find("Scroll View/Viewport/VerticalLayout");


            AddSliderSetting("Microphone Volume", VoiceChat.modName.ToLower() + "_mic_volume", sliderSetting, instanceLayout, true, 0, 100, 100);
            AddSliderSetting("Microphone Boost", VoiceChat.modName.ToLower() + "_mic_boost", sliderSetting, instanceLayout, false, 0, 100, 0);
            AddSliderSetting("Voicechat Volume", VoiceChat.modName.ToLower() + "_voice_volume", sliderSetting, instanceLayout, false, 0, 100, 100);
            AddBoolSetting("Voice Activation", VoiceChat.modName.ToLower() + "_voice_activation", boolSetting, instanceLayout, false, false);
            AddSliderSetting("Voice Sensitivity", VoiceChat.modName.ToLower() + "_voice_sensitivity", sliderSetting, instanceLayout, false, 0, 100, 50);
            AddBoolSetting("3D Audio", VoiceChat.modName.ToLower() + "_spatial_sound", boolSetting, instanceLayout, false, false);
            /*
            ModSettingsManager.addOption(new ModOption(ModOption.OptionType.Slider, "Microphone Volume", "The volume of your microphone.", "100"));
            ModSettingsManager.addOption(new ModOption(ModOption.OptionType.Slider, "Microphone Boost", "Additional boost of your microphone volume.", "0"));
            ModSettingsManager.addOption(new ModOption(ModOption.OptionType.Slider, "Voicechat Volume", "The volume of other people in voicechat.", "100"));
            ModSettingsManager.addOption(new ModOption(ModOption.OptionType.Bool, "Voice Activation", "Use voice activation rather than push to talk.", "0"));
            ModSettingsManager.addOption(new ModOption(ModOption.OptionType.Slider, "Voice Sensitivity", "The sensitivity of the voice activation. Lower is more sensitive.", "50"));
            ModSettingsManager.addOption(new ModOption(ModOption.OptionType.Bool, "3D Audio", "Use 3D Audio, voices come from player characters. This setting is synchronized with the host.", "0"));

            */
        }

        private static void AddSliderSetting(string Name, string id, GameObject settingToInstantiate, Transform panelLayout, bool isFirst, float minValue, float maxValue, float defaultValue)
        {
            GameObject settingInstance = Object.Instantiate(settingToInstantiate, panelLayout);

            SettingsSlider slider = settingInstance.GetComponent<SettingsSlider>();
            slider.settingName = id;
            slider.nameToken = Name;
            slider.settingSource = BaseSettingsControl.SettingSource.ConVar;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.originalValue = TextSerialization.ToStringInvariant(defaultValue);

            settingInstance.name = string.Format("SettingsEntryButton, Slider ({0})", Name);

            if (isFirst)
            {
                HGButton button = settingInstance.GetComponent<HGButton>();
                if (button)
                    button.defaultFallbackButton = true;
            }
        }

        private static void AddBoolSetting(string Name, string id, GameObject settingToInstantiate, Transform panelLayout, bool isFirst, bool defaultValue)
        {
            GameObject settingInstance = Object.Instantiate(settingToInstantiate, panelLayout);

            /*
            SettingsSlider slider = settingInstance.GetComponent<SettingsSlider>();
            slider.settingName = id;
            slider.nameToken = Name;
            slider.settingSource = BaseSettingsControl.SettingSource.ConVar;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            */

            CarouselController boolean = settingInstance.GetComponent<CarouselController>();
            boolean.settingName = id;
            boolean.nameToken = Name;
            boolean.settingSource = BaseSettingsControl.SettingSource.ConVar;
            boolean.originalValue = defaultValue ? "1" : "0";

            settingInstance.name = string.Format("SettingsEntryButton, Slider ({0})", Name);

            if (isFirst)
            {
                HGButton button = settingInstance.GetComponent<HGButton>();
                if (button)
                    button.defaultFallbackButton = true;
            }
        }

        private static void AddBindingSetting(Inputs.ActionDef actionDef, GameObject settingToInstantiate, Transform panelLayout, bool isFirst)
        {
            GameObject settingInstance = Object.Instantiate(settingToInstantiate, panelLayout);

            InputBindingControl inputBindingControl = settingInstance.GetComponent<InputBindingControl>();
            inputBindingControl.actionName = actionDef.actionName;
            inputBindingControl.axisRange = AxisRange.Full;
            inputBindingControl.Awake();

            settingInstance.name = string.Format("SettingsEntryButton, {1} Binding ({0})", actionDef.actionName, inputBindingControl.inputSource == MPEventSystem.InputSource.MouseAndKeyboard ? "M&K" : "Gamepad");

            if (isFirst)
            {
                HGButton button = settingInstance.GetComponent<HGButton>();
                if (button)
                    button.defaultFallbackButton = true;
            }
        }

    }
}