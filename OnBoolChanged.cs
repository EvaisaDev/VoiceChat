using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Evaisa.VoiceChat
{
    class OnBoolChanged : MonoBehaviour
    {
        private CarouselController Carousel;

        private bool lastValue = false;

        public UnityEngine.Events.UnityAction<bool> onValueChanged;

        private void Start()
        {
            Carousel = gameObject.GetComponentInChildren<CarouselController>();
        }

        private void Update()
        {
            if (Carousel != null)
            {
                bool currentValue = false;

                string value = Carousel.GetCurrentValue();

                if (value == "1")
                {
                    currentValue = true;
                }

                if (lastValue != currentValue)
                {
                    onValueChanged.Invoke(lastValue);
                }

                lastValue = currentValue;
            }
        }
    }
}
