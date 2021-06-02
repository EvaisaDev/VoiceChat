using System;
using System.Collections.Generic;
using System.Text;
using Rewired;
using RoR2;
using UnityEngine;
using UnityEngine.Events;

namespace Evaisa.VoiceChat
{
	public class InputResponse : MonoBehaviour
	{
		private void Update()
		{
			Player player = MPEventSystemManager.combinedEventSystem.player;
			int i = 0;
			while (i < this.inputActionNames.Length)
			{
				if (player.GetButtonDown(this.inputActionNames[i]))
				{
					UnityEvent unityEvent = this.onPress;
					if (unityEvent == null)
					{
						return;
					}
					unityEvent.Invoke();
					return;
				}
				else if (player.GetButtonUp(this.inputActionNames[i]))
				{
					UnityEvent unityEvent = this.onRelease;
					if (unityEvent == null)
					{
						return;
					}
					unityEvent.Invoke();
					return;
				}
				else
				{
					i++;
				}
			}
		}

		public string[] inputActionNames;

		public UnityEvent onPress;

		public UnityEvent onRelease;
	}
}
