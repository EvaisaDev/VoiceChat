using RoR2.ConVar;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Evaisa.VoiceChat
{
    public class SettingsApi
    {

        public static List<Setting> registeredSettings = new List<Setting>();

        public static void Init()
        {
            On.RoR2.Console.Awake += Console_Awake;

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

            List<HGHeaderNavigationController.Header> headerList = controller.headers.ToList();


            GameObject audioPanel = headerList.First(headerItem => headerItem.headerName == "Audio").headerRoot;
            GameObject graphicsPanel = headerList.First(headerItem => headerItem.headerName == "Graphics").headerRoot;

            GameObject sliderSetting = audioPanel.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Slider (Master Volume)").gameObject;
            GameObject boolSetting = audioPanel.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Bool (Audio Focus)").gameObject;
            GameObject carouselSetting = graphicsPanel.transform.Find("Scroll View/Viewport/VerticalLayout/SettingsEntryButton, Carousel (Shadow Quality)").gameObject;

            registeredSettings.ForEach(setting =>
            {
                GameObject subPanelInstance;
                GameObject headerInstance;

                bool firstSetting = false;

                if (!headerList.Any(headerItem => headerItem.headerName == setting.tabName))
                {
                    subPanelInstance = SetupSubPanel(subPanelArea, setting.tabName);

                    headerInstance = SetupHeader(header, setting.tabName);

                    LanguageTextMeshController text = headerInstance.GetComponent<LanguageTextMeshController>();
                    text.token = setting.tabName.ToUpperInvariant();

                    HGHeaderNavigationController.Header headerInfo = new HGHeaderNavigationController.Header();
                    headerInfo.headerButton = headerInstance.GetComponent<HGButton>();
                    headerInfo.headerName = setting.tabName;
                    headerInfo.tmpHeaderText = headerInstance.GetComponentInChildren<HGTextMeshProUGUI>();
                    headerInfo.headerRoot = subPanelInstance;

                    headerList.Add(headerInfo);

                    firstSetting = true;
                }
                else
                {
                    headerInstance = headerList.First(headerItem => headerItem.headerName == setting.tabName).headerButton.gameObject;
                    subPanelInstance = headerList.First(headerItem => headerItem.headerName == setting.tabName).headerRoot;
                }

                if(setting.type == settingType.Boolean)
                {
                    BoolSetting mySetting = (BoolSetting)setting;
                    GenerateBoolSetting(mySetting.name, mySetting.convarID, mySetting.description, boolSetting, subPanelInstance.transform.Find("Scroll View/Viewport/VerticalLayout"), firstSetting, mySetting.defaultValue == "1" ? true : false);
                }
                if(setting.type == settingType.Slider)
                {
                    SliderSetting mySetting = (SliderSetting)setting;
                    GenerateSliderSetting(mySetting.name, mySetting.convarID, mySetting.description, mySetting.formatString, sliderSetting, subPanelInstance.transform.Find("Scroll View/Viewport/VerticalLayout"), firstSetting, mySetting.minValue, mySetting.maxValue, float.Parse( mySetting.defaultValue, CultureInfo.InvariantCulture.NumberFormat));
                }
                if(setting.type == settingType.Carousel)
                {
                    CarouselSetting mySetting = (CarouselSetting)setting;
                    GenerateCarouselSetting(mySetting.name, mySetting.convarID, mySetting.description, carouselSetting, subPanelInstance.transform.Find("Scroll View/Viewport/VerticalLayout"), firstSetting, mySetting.defaultValue, mySetting.options);
                }
            });




            controller.headers = headerList.ToArray();
        }


        private static void GenerateSliderSetting(string Name, string id, string description, string formatString, GameObject settingToInstantiate, Transform panelLayout, bool isFirst, float minValue, float maxValue, float defaultValue)
        {
            GameObject settingInstance = Object.Instantiate(settingToInstantiate, panelLayout);

            SettingsSlider slider = settingInstance.GetComponent<SettingsSlider>();
            slider.settingName = id;
            slider.nameToken = Name;
            slider.settingSource = BaseSettingsControl.SettingSource.ConVar;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.originalValue = TextSerialization.ToStringInvariant(defaultValue);
            slider.formatString = formatString;

            settingInstance.name = string.Format("SettingsEntryButton, Slider ({0})", Name);

            HGButton button = settingInstance.GetComponent<HGButton>();
            if (button)
            {
                button.hoverToken = description;
                if (isFirst)
                {

                    button.defaultFallbackButton = true;
                }
                else
                {
                    button.defaultFallbackButton = false;
                }
            }
        }

        private static void GenerateBoolSetting(string Name, string id, string description, GameObject settingToInstantiate, Transform panelLayout, bool isFirst, bool defaultValue)
        {
            GameObject settingInstance = Object.Instantiate(settingToInstantiate, panelLayout);

            CarouselController boolean = settingInstance.GetComponent<CarouselController>();
            boolean.settingName = id;
            boolean.nameToken = Name;
            boolean.settingSource = BaseSettingsControl.SettingSource.ConVar;
            boolean.originalValue = defaultValue ? "1" : "0";

            settingInstance.name = string.Format("SettingsEntryButton, Slider ({0})", Name);

            HGButton button = settingInstance.GetComponent<HGButton>();
            if (button)
            {
                button.hoverToken = description;
                if (isFirst)
                {

                    button.defaultFallbackButton = true;
                }
                else
                {
                    button.defaultFallbackButton = false;
                }
            }
        }

        private static void GenerateCarouselSetting(string Name, string id, string description, GameObject settingToInstantiate, Transform panelLayout, bool isFirst, string defaultValue, List<string> options)
        {
            GameObject settingInstance = Object.Instantiate(settingToInstantiate, panelLayout);

            CarouselController carousel = settingInstance.GetComponent<CarouselController>();
            carousel.settingName = id;
            carousel.nameToken = Name;
            carousel.settingSource = BaseSettingsControl.SettingSource.ConVar;
            carousel.originalValue = defaultValue;
            var choices = new List<CarouselController.Choice>();

            options.ForEach(option =>
            {
                var choice = new CarouselController.Choice();
                choice.convarValue = option;
                choice.suboptionDisplayToken = option;
                choices.Add(choice);
            });

            var longestString = options.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur); 



            var rectTransformText = carousel.optionalText.GetComponent<RectTransform>();
            rectTransformText.sizeDelta = new Vector2(longestString.Length * 6.25f, rectTransformText.sizeDelta.y);
            rectTransformText.anchoredPosition = new Vector2(-(((longestString.Length * 6.25f) + (64 - longestString.Length)) / 1.5f), 16);

            var leftArrowTransform = carousel.leftArrowButton.GetComponent<RectTransform>();
            leftArrowTransform.anchoredPosition = new Vector2(-((longestString.Length * 8.25f) + (64 - longestString.Length) ), 0);

            carousel.choices = choices.ToArray();

            settingInstance.name = string.Format("SettingsEntryButton, Slider ({0})", Name);

            HGButton button = settingInstance.GetComponent<HGButton>();
            if (button)
            {
                button.hoverToken = description;
                if (isFirst)
                {

                    button.defaultFallbackButton = true;
                }
                else
                {
                    button.defaultFallbackButton = false;
                }
            }
        }

        private static GameObject SetupHeader(Transform parent, string Name)
        {
            GameObject categoryToInstantiate = parent.Find("GenericHeaderButton (Graphics)").gameObject;

            GameObject headerInstance = Object.Instantiate(categoryToInstantiate, parent);

            headerInstance.transform.SetSiblingIndex(parent.childCount - 2);
            headerInstance.name = "GenericHeaderButton (" + Name + ")";

            return headerInstance;
        }

        private static GameObject SetupSubPanel(Transform parent, string Name)
        {
            GameObject subPanelToInstantiate = parent.Find("SettingsSubPanel, Controls (Gamepad)").gameObject;

            GameObject subPanelInstance = Object.Instantiate(subPanelToInstantiate, parent);

            subPanelInstance.name = "SettingsSubPanel, "+Name;

            Transform instanceLayout = subPanelInstance.transform.Find("Scroll View/Viewport/VerticalLayout");

            foreach (Transform child in instanceLayout)
            {
                Object.Destroy(child.gameObject);
            }

            return subPanelInstance;
        }


        private static void Console_Awake(On.RoR2.Console.orig_Awake orig, RoR2.Console self)
        {
            orig(self);

            registeredSettings.ForEach(setting =>
            {
                if(setting.type == settingType.Slider)
                {
                    var sliderSetting = (SliderSetting)setting;
                    sliderSetting.convar = RegisterFloatConvar(self, sliderSetting.convarID, sliderSetting.defaultValue, sliderSetting.description);
                }
                else if (setting.type == settingType.Boolean)
                {
                    var boolSetting = (BoolSetting)setting;
                    boolSetting.convar = RegisterBoolConvar(self, boolSetting.convarID, boolSetting.defaultValue, boolSetting.description);
                }
                else if (setting.type == settingType.Carousel)
                {
                    var carouselSetting = (CarouselSetting)setting;
                    carouselSetting.convar = RegisterStringConvar(self, carouselSetting.convarID, carouselSetting.defaultValue, carouselSetting.description);
                }
            });

            self.SubmitCmd(null, "exec config", false);
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || c == ' ' || c == '-' )
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static BoolSetting RegisterBoolSetting(string name, string description, bool defaultValue, string tabName, bool pushToTop = false)
        {
            var setting = new BoolSetting
            {
                type = settingType.Boolean,
                defaultValue = defaultValue ? "1" : "0",
                name = name,
                description = description,
                convarID = RemoveSpecialCharacters((VoiceChat.modName + "_" + name).ToLower().Replace(" ", "_")),
                tabName = tabName
            };
            if (pushToTop)
            {
                registeredSettings.Insert(0, (Setting)setting);
            }
            else
            {
                registeredSettings.Add((Setting)setting);
            }

            return setting;
        }

        public static CarouselSetting RegisterCarouselSetting(string name, string description, string defaultValue, List<string> options, string tabName, bool pushToTop = false)
        {
            var setting = new CarouselSetting
            {
                type = settingType.Carousel,
                defaultValue = defaultValue,
                name = name,
                description = description,
                options = options,
                convarID = RemoveSpecialCharacters((VoiceChat.modName + "_" + name).ToLower().Replace(" ", "_")),
                tabName = tabName
            };
            if (pushToTop)
            {
                registeredSettings.Insert(0, (Setting)setting);
            }
            else
            {
                registeredSettings.Add((Setting)setting);
            }

            return setting;
        }

        public static SliderSetting RegisterSliderSetting(string name, string description, float defaultValue, float minValue, float maxValue, string formatString, string tabName, bool pushToTop = false)
        {
            var setting = new SliderSetting
            {
                type = settingType.Slider,
                defaultValue = TextSerialization.ToStringInvariant(defaultValue),
                minValue = minValue,
                maxValue = maxValue,
                name = name,
                description = description,
                formatString = formatString,
                convarID = RemoveSpecialCharacters((VoiceChat.modName + "_" + name).ToLower().Replace(" ", "_")),
                tabName = tabName
            };
            if (pushToTop)
            {
                registeredSettings.Insert(0, (Setting)setting);
            }
            else
            {
                registeredSettings.Add((Setting)setting);
            }

            return setting;
        }

        public class Setting
        {
            public settingType type;
            public string defaultValue;
            public string name;
            public string description;
            public string convarID;
            public string tabName;

            public void Destroy()
            {
                registeredSettings.Remove((Setting)this);
            }
        }

        public class BoolSetting : Setting
        {
            public BoolConVar convar;

            public bool GetValue()
            {
                return convar.value;
            }

            public void SetValue(bool value)
            {
                convar.SetString(value ? "true" : "false");
            }

        }

        public class SliderSetting : Setting
        {
            public FloatConVar convar;
            public float minValue;
            public float maxValue;
            public string formatString;

            public float GetValue()
            {
                return convar.value;
            }

            public void SetValue(float value)
            {
                convar.SetString(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public class StringSetting : Setting
        {
            public StringConVar convar;

            public string GetValue()
            {
                return convar.value;
            }

            public void SetValue(string value)
            {
                convar.SetString(value);
            }
        }

        public class CarouselSetting : Setting
        {
            public StringConVar convar;
            public List<String> options;

            public string GetValue()
            {
                return convar.value;
            }
            public void SetValue(string value)
            {
                convar.SetString(value);
            }
        }

        public enum settingType
        {
            Boolean,
            Slider,
            Carousel,
        }

        public static BoolConVar RegisterBoolConvar(RoR2.Console console, string convarID, string defaultValue, string description)
        {
            BoolConVar convar = new BoolConVar(convarID, RoR2.ConVarFlags.Archive, defaultValue, description);

            console.RegisterConVarInternal(convar);
            convar.SetString(defaultValue);

            Debug.Log("Convar Registered: " + convarID);

            return convar;
        }

        public static FloatConVar RegisterFloatConvar(RoR2.Console console, string convarID, string defaultValue, string description)
        {
            FloatConVar convar = new FloatConVar(convarID, RoR2.ConVarFlags.Archive, defaultValue, description);

            console.RegisterConVarInternal(convar);
            convar.SetString(defaultValue);

            Debug.Log("Convar Registered: " + convarID);

            return convar;
        }
        public static StringConVar RegisterStringConvar(RoR2.Console console, string convarID, string defaultValue, string description)
        {
            StringConVar convar = new StringConVar(convarID, RoR2.ConVarFlags.Archive, defaultValue, description);

            console.RegisterConVarInternal(convar);
            convar.SetString(defaultValue);

            Debug.Log("Convar Registered: " + convarID);

            return convar;
        }


    }

}
