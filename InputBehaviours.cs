using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Evaisa.VoiceChat
{
    internal static class InputBehaviours
    {
        private static GameObject inputManager;

        internal static void Init()
        {
            RoR2.RoR2Application.onLoad += AddRecenterInput;
        }

        private static void AddRecenterInput()
        {
            if (inputManager)
                return;

            inputManager = new GameObject("VoicechatInputManager");

            GameObject inputs = new GameObject("Inputs");
            inputs.transform.SetParent(inputManager.transform);
            inputs.AddComponent<MPEventSystemProvider>().fallBackToMainEventSystem = true;

            InputResponse inputResponse = inputs.AddComponent<InputResponse>();
            inputResponse.inputActionNames = new string[] { "PushToTalkKeyboard" };
            inputResponse.onPress = new UnityEvent();
            inputResponse.onRelease = new UnityEvent();
            inputResponse.onPress.AddListener(() => { VoiceChatController.isPressingPushToTalk = true; });
            inputResponse.onRelease.AddListener(() => { VoiceChatController.isPressingPushToTalk = false; });
            //inputResponse.onPress.AddListener(UnityEngine.XR.InputTracking.Recenter);
            //inputResponse

            Object.DontDestroyOnLoad(inputManager);
        }
    }
}
