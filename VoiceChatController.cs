using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;
using RoR2.UI;
using UnityEngine.UI;
using System.Globalization;

namespace Evaisa.VoiceChat
{
    
    class VoiceChatController : MonoBehaviour
    {
        static int FREQUENCY = 1000;
        static int bufferLength = 40;
        int lastSample = 0;
        bool micOff = true;
        bool sending = false;
        AudioClip sendingClip;
        public static VoiceChatController instance;
        public bool instanceEnabled = false;
        public List<sampleCollection> sampleBuffer;
        public List<chunkCollection> chunkBuffer;
        public List<audioPlayer> players;
        public float timer = 1f;

        public static bool isPressingPushToTalk = false;
        public static bool isUsingVRTalk = false;

        public float updateStep = 0.1f;

        public float localVolume = 0f;

        public float averageVolume = 0f;

        public void Awake()
        {
            players = new List<audioPlayer>();
            sampleBuffer = new List<sampleCollection>();
            chunkBuffer = new List<chunkCollection>();
            instance = this;

            if (NetworkServer.active) {
                FREQUENCY = int.Parse(VoiceChat.bitRate.GetValue());
                bufferLength = VoiceChat.lowLatencyMode.GetValue() ? 5 : 40;
                new PassFrequency
                {
                    bitrate = int.Parse(VoiceChat.bitRate.GetValue()),
                    lowLatency = VoiceChat.lowLatencyMode.GetValue(),
                }.Send(NetworkDestination.Clients);
            }
        }

        internal struct PassFrequency : INetMessage
        {
            internal int bitrate;
            internal bool lowLatency;
            public void Serialize(NetworkWriter writer)
            {
                writer.Write(bitrate);
                writer.Write(lowLatency);
            }
            public void Deserialize(NetworkReader reader)
            {
                this.bitrate = reader.ReadInt32();
                this.lowLatency = reader.ReadBoolean();
            }
            public void OnReceived()
            {
                FREQUENCY = bitrate;
                bufferLength = lowLatency ? 5 : 40;
            }
        }


        // public List<float> lastVolumeLevels;
        public float oldVolume = 0f;


        public void Update()
        {
            /*
            foreach(var sample in sampleBuffer)
            {
                if (sample.sampleData.Count > 0)
                {
                    if (sample.lastSampleTime != null)
                    {
                        var timeSinceLast = (int)(DateTime.Now - sample.lastSampleTime).TotalMilliseconds;
                        if (timeSinceLast > 100)
                        {
                            //VoiceChat.DebugPrint("Possible desync, forcing audio execution.");
                            HandleSendData(new float[0], sample.id, true);
                        }
                    }
                }
            }
            */


            foreach(var player in players)
            {
                if (player.sources.Count > 0)
                {
                    foreach (var playerController in PlayerCharacterMasterController.instances)
                    {
                        if (playerController.body)
                        {
                            if (playerController.networkUserInstanceId == player.identifier)
                            {
                                foreach(var source in player.sources)
                                {
                                    source.gameObject.transform.localPosition = playerController.body.gameObject.transform.position;
                                }
                            }
                        }
                    }
                    foreach (var user in NetworkUser.readOnlyInstancesList)
                    {
                        if (user.GetCurrentBody())
                        {
                            if (user.netId == player.identifier)
                            {
                                foreach (var source in player.sources)
                                {
                                    source.gameObject.transform.localPosition = user.GetCurrentBody().transform.position;
                                }
                            }
                        }
                    }


                    for (int i = player.sources.Count - 1; i >= 0; i--)
                    {
                        if (player.sources[i])
                        {
                            
                            var source = player.sources[i];
                           
                            if (!source.isPlaying)
                            {
                               // VoiceChat.DebugPrint("Player killed.");
                                player.sources.RemoveAt(i);
                                DestroyImmediate(source.gameObject);
                            }
                        }
                    }
                

                    foreach (var source in player.sources)
                    {
                    
                        if (source.isPlaying)
                        {
                            float clipLoudness = 0f;
                            float[] clipSampleData1 = new float[1024];
                            float[] clipSampleData2 = new float[1024];
                            if (source)
                            {
                                if (source.isPlaying)
                                    source.clip.GetData(clipSampleData1, source.timeSamples);
                            }

                            if (source)
                            {
                                if (source)
                                {
                                    foreach (var sample in clipSampleData1)
                                    {
                                        clipLoudness += Mathf.Abs(sample);
                                    }
                                }
                            }

                            clipLoudness /= 1024;

                            clipLoudness *= 2;


                            if (clipLoudness < 0.2)
                            {
                                clipLoudness = 0f;
                            }


                            var audioLevel = Mathf.Sqrt(clipLoudness);

                            var gradient = new Gradient();

                            var colorKey = new GradientColorKey[2];
                            colorKey[0].color = Color.black;
                            colorKey[0].time = 0.0f;
                            colorKey[1].color = new Color(0.086f, 0.925f, 0.074f);
                            colorKey[1].time = 1.0f;

                            var alphaKey = new GradientAlphaKey[2];
                            alphaKey[0].alpha = 0.24f;
                            alphaKey[0].time = 0.0f;
                            alphaKey[1].alpha = 0.24f;
                            alphaKey[1].time = 1.0f;


                            gradient.SetKeys(colorKey, alphaKey);

                            //VoiceChat.DebugPrint(audioLevel);
                            var allyCards = RoR2.UI.HUD.instancesList[0].allyCardManager.gameObject.GetComponentsInChildren<AllyCardController>();
                            foreach (var allyCard in allyCards)
                            {
                                if (allyCard.sourceMaster.playerCharacterMasterController.networkUserInstanceId == player.identifier)
                                {
                                    var image = allyCard.GetComponent<Image>();
                                    image.color = gradient.Evaluate(audioLevel);
                                }
                            }
                        }
                    }

                }

                // DestroyImmediate(source);




                /*
                float[] clipSampleData1 = new float[1024];
                float[] clipSampleData2 = new float[1024];
                if (player.source1)
                {
                    if(player.source1.isPlaying)
                        player.source1.clip.GetData(clipSampleData1, player.source1.timeSamples);
                }
                if (player.source2)
                {
                    if (player.source2.isPlaying)
                        player.source2.clip.GetData(clipSampleData2, player.source2.timeSamples);
                }
                if (player.source1)
                {
                    if (player.source1.isPlaying)
                    {
                        foreach (var sample in clipSampleData1)
                        {
                            clipLoudness += Mathf.Abs(sample);
                        }
                    }
                }
                if (player.source2)
                {
                    if (player.source2.isPlaying) 
                    { 
                        foreach (var sample in clipSampleData2)
                        {
                            clipLoudness += Mathf.Abs(sample);
                        }
                    }
                }

                clipLoudness /= 1024;

                clipLoudness *= 2;
         

                if(clipLoudness < 0.2)
                {
                    clipLoudness = 0f;
                }


                if (clipLoudness != 0)
                {
                  //  VoiceChat.DebugPrint("Volume: " + clipLoudness);
                }

     
                var audioLevel = Mathf.Sqrt(clipLoudness) / 2;

                var gradient = new Gradient();

                var colorKey = new GradientColorKey[2];
                colorKey[0].color = Color.black;
                colorKey[0].time = 0.0f;
                colorKey[1].color = new Color(0.086f, 0.925f, 0.074f);
                colorKey[1].time = 1.0f;

                var alphaKey = new GradientAlphaKey[2];
                alphaKey[0].alpha = 0.24f;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = 0.24f;
                alphaKey[1].time = 1.0f;


                gradient.SetKeys(colorKey, alphaKey);

                //VoiceChat.DebugPrint(audioLevel);
                var allyCards = RoR2.UI.HUD.instancesList[0].allyCardManager.gameObject.GetComponentsInChildren<AllyCardController>();
                foreach (var allyCard in allyCards)
                {
                    if (allyCard.sourceMaster.playerCharacterMasterController.networkUserInstanceId == player.identifier)
                    {
                        var image = allyCard.GetComponent<Image>();
                        image.color = gradient.Evaluate(audioLevel);
                    }
                }
                */
            }
        }

        public class sampleData
        {
            public List<byte> sample;
            public sampleData(List<byte> sample)
            {
                this.sample = sample;
            }
        }

        public class audioPlayer
        {
            public List<AudioSource> sources;
            public NetworkInstanceId identifier;
            public int currentSource = 2;
            public audioPlayer(NetworkInstanceId identifier)
            {
                sources = new List<AudioSource>();
                this.identifier = identifier;
            }
        }

        public class sampleCollection
        {
            public List<sampleData> sampleData;
            public NetworkInstanceId id;
            public DateTime lastSampleTime;
            public sampleCollection(List<sampleData> sampleData, NetworkInstanceId networkUser)
            {
                this.sampleData = sampleData;
                this.id = networkUser;
            }
        }

        public class chunkData
        {
            public List<byte> chunk;
            public chunkData(List<byte> sample)
            {
                this.chunk = sample;
            }
        }

        public class chunkCollection
        {
            public List<chunkData> chunkData;
            public int chunkID;
            public chunkCollection(List<chunkData> chunkData, int chunkID)
            {
                this.chunkData = chunkData;
                this.chunkID = chunkID;
            }
        }

        private int GetSampleTransmissionCount(int currentMicrophoneSample)
        {
            int sampleTransmissionCount = currentMicrophoneSample - lastSample;
            if (sampleTransmissionCount < 0)
            {
                sampleTransmissionCount = (sendingClip.samples - lastSample) + currentMicrophoneSample;
            }
            return sampleTransmissionCount;
        }

        private byte[] adjustVolume(byte[] audioSamples, float volume)
        {
            byte[] array = new byte[audioSamples.Length];
            for (int i = 0; i < array.Length; i += 2)
            {
                // convert byte pair to int
                short buf1 = audioSamples[i + 1];
                short buf2 = audioSamples[i];

                buf1 = (short)((buf1 & 0xff) << 8);
                buf2 = (short)(buf2 & 0xff);

                short res = (short)(buf1 | buf2);
                res = (short)(res * volume);

                // convert back
                array[i] = (byte)res;
                array[i + 1] = (byte)(res >> 8);

            }
            return array;
        }

        string lastMicrophone = "";

        public bool wasTalking = false;
        public void FixedUpdate()
        {
            if(VoiceChat.microphone.GetValue() != lastMicrophone && VoiceChat.microphone.GetValue() != "null" && lastMicrophone != "")
            {

                if(lastMicrophone != "")
                {
                    if (Microphone.IsRecording(lastMicrophone))
                    {
                        Microphone.End(lastMicrophone);
                    }
                    micOff = true;
                    sending = false;
                    wasTalking = false;
                    lastSample = 0;
                }
            }
            if (Microphone.devices.Length > 0 && Microphone.devices.Contains(VoiceChat.microphone.GetValue()))
            {
                if (micOff)
                {
                    lastSample = 0;
                    sendingClip = Microphone.Start(VoiceChat.microphone.GetValue(), true, 100, FREQUENCY);
                    micOff = false;
                    sending = true;
                    lastMicrophone = VoiceChat.microphone.GetValue();
                }
                else if (sending)
                {
                    
                    if (Microphone.GetPosition(VoiceChat.microphone.GetValue()) > 0)
                    {

                        int pos = Microphone.GetPosition(VoiceChat.microphone.GetValue());
                        int diff = GetSampleTransmissionCount(pos);

                        if (diff > 0)
                        {

                            float[] samples = new float[diff * sendingClip.channels];

                            sendingClip.GetData(samples, lastSample);



                            float[] samplesBoosted = new float[diff * sendingClip.channels];

                            for (int s = 0; s < samples.Length; s++)

                            {
                                samplesBoosted[s] = samples[s] * 10;
                            }



                            for (int s = 0; s < samples.Length; s++)

                            {
                                samples[s] = samples[s] * ((VoiceChat.micVolume.GetValue() / 100) * 2);
                            }

                            for (int s = 0; s < samples.Length; s++)

                            {
                                samples[s] = samples[s] * (1 + ((VoiceChat.micBoost.GetValue() / 100) * 10));
                            }

                            float levelMax = 0;
                            for (int i = 0; i < (diff * sendingClip.channels); i++)
                            {
                                float wavePeak = samplesBoosted[i] * samplesBoosted[i];
                                if (levelMax < wavePeak)
                                {
                                    levelMax = wavePeak;
                                }
                            }

                            var audioLevel = Mathf.Sqrt(levelMax);

                            var isTalking = false;


                            if (timer > -1f)
                            {
                                timer -= Time.deltaTime;
                            }

                            if (audioLevel > oldVolume * (VoiceChat.voiceSensitivity.GetValue() / 100 * 7))
                            {
                                timer = 1.2f;
                                isTalking = true;

                            }


                            oldVolume = audioLevel;


                            if (isPressingPushToTalk || isUsingVRTalk || VoiceChat.voiceActivation.GetValue() && isTalking || VoiceChat.voiceActivation.GetValue() && timer > 0f)
                            {

                                HandleSendData(samples);
                                wasTalking = true;
                                //VoiceChat.DebugPrint("Currently talking!");
                            }
                            else
                            {
                                if (wasTalking)
                                {
                                    VoiceChat.DebugPrint("Cleaning up.");
                                    HandleSendData(new float[0], true);
                                }
                                wasTalking = false;
                            }

                        }
                        lastSample = pos;
                    }
                }
            }
            else
            {
                if(Microphone.devices.Length > 0)
                {
                    if (VoiceChat.microphone.GetValue() != "null")
                    {
                        VoiceChat.microphone.SetValue(Microphone.devices[0]);
                    }
                }
                else
                {
                    if (VoiceChat.microphone.GetValue() != "null")
                    {
                        VoiceChat.microphone.SetValue("null");
                    }
                }
            }
        }

        public void HandleSendData(float[] samples, bool finishBuffer = false)
        {

            byte[] baUnmodified = ToByteArray(samples);



            byte[] ba = adjustVolume(baUnmodified, 1.0f);

            if (ba.Length < 8000)
            {

                byte[] ba_compressed = ba.Length != 0 ? CLZF2.Compress(ba) : ba;

                if (ba.Length == 0)
                {
                    int chunkID = UnityEngine.Random.Range(0, 999999999);
                    var localPlayerMaster = NetworkUser.localPlayers[0];



                    new ForwardVoiceChat
                    {
                        ba = new byte[0],
                        chan = sendingClip.channels,
                        user = localPlayerMaster.netId,
                        chunkID = chunkID,
                        chunkCount = 0,
                        isEndPoint = finishBuffer,
                    }.Send(NetworkDestination.Server);
                }
                else
                {

                    var chunks = ba_compressed.AsChunks(800);

                    int chunkID = UnityEngine.Random.Range(0, 999999999);

                    chunks.ForEachTry(chunk =>
                    {

                        var localPlayerMaster = NetworkUser.localPlayers[0];



                        new ForwardVoiceChat
                        {
                            ba = chunk,
                            chan = sendingClip.channels,
                            user = localPlayerMaster.netId,
                            chunkID = chunkID,
                            chunkCount = chunks.Count(),
                            isEndPoint = finishBuffer,
                        }.Send(NetworkDestination.Server);

                    });
                }
            }
        }

        public void HandleSendData(float[] samples, NetworkInstanceId overrideId, bool finishBuffer = false)
        {

            byte[] ba = ToByteArray(samples);



            if (ba.Length < 8000)
            {

                byte[] ba_compressed = ba.Length != 0 ? CLZF2.Compress(ba) : ba;

                if (ba.Length == 0)
                {
                    int chunkID = UnityEngine.Random.Range(0, 999999999);
                    var localPlayerMaster = NetworkUser.localPlayers[0];



                    new ForwardVoiceChat
                    {
                        ba = new byte[0],
                        chan = sendingClip.channels,
                        user = localPlayerMaster.netId,
                        chunkID = chunkID,
                        chunkCount = 0,
                        isEndPoint = finishBuffer,
                    }.Send(NetworkDestination.Server);
                }
                else
                {

                    var chunks = ba_compressed.AsChunks(800);

                    int chunkID = UnityEngine.Random.Range(0, 999999999);

                    chunks.ForEachTry(chunk =>
                    {

                        var localPlayerMaster = NetworkUser.localPlayers[0];



                        new ForwardVoiceChat
                        {
                            ba = chunk,
                            chan = sendingClip.channels,
                            user = localPlayerMaster.netId,
                            chunkID = chunkID,
                            chunkCount = chunks.Count(),
                            isEndPoint = finishBuffer,
                        }.Send(NetworkDestination.Server);

                    });
                }
            }
        }

        internal struct ForwardVoiceChat : INetMessage
        {

            internal byte[] ba;
            internal int chan;
            internal NetworkInstanceId user;
            internal int chunkID;
            internal int chunkCount;
            internal bool isEndPoint;
            public void Serialize(NetworkWriter writer)
            {
                writer.Write(ba.Length);
                writer.Write(ba, ba.Length);
                writer.Write(user);
                writer.Write(chunkID);
                writer.Write(chunkCount);
                writer.Write(chan);
                writer.Write(isEndPoint);
            }
            public void Deserialize(NetworkReader reader)
            {
                var array_length = reader.ReadInt32();
                this.ba = reader.ReadBytes(array_length);
                this.user = reader.ReadNetworkId();
                this.chunkID = reader.ReadInt32();
                this.chunkCount = reader.ReadInt32();
                this.chan = reader.ReadInt32();
                this.isEndPoint = reader.ReadBoolean();
            }
            public void OnReceived()
            {
                var localizedAudio = VoiceChat.spatialSound.GetValue();
                var localizedAudioDistance = (int)VoiceChat.voiceDistance.GetValue();
                //VoiceChat.DebugPrint("3D Audio? " + localizedAudio);

                new SendVoiceChat
                {
                    ba = ba,
                    chan = chan,
                    user = user,
                    chunkID = chunkID,
                    chunkCount = chunkCount,
                    isEndPoint = isEndPoint,
                    localizedAudio = localizedAudio,
                    localizedAudioDistance = localizedAudioDistance
                }.Send(NetworkDestination.Clients);
            }

        }

        public static void UnrealisticRolloff(AudioSource AS)
        {
            var animCurve = new AnimationCurve(
                new Keyframe(AS.minDistance, 1f),
                new Keyframe(AS.maxDistance, 1f));

            AS.rolloffMode = AudioRolloffMode.Custom;
            animCurve.SmoothTangents(1, .025f);
            AS.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animCurve);

            AS.dopplerLevel = 0f;
            AS.spread = 60f;
        }


        public static void RealisticRolloff(AudioSource AS)
        {
            var animCurve = new AnimationCurve(
                new Keyframe(AS.minDistance, 1f),
                new Keyframe(AS.minDistance + (AS.maxDistance - AS.minDistance) / 4f, .35f),
                new Keyframe(AS.maxDistance, 0f));

            AS.rolloffMode = AudioRolloffMode.Custom;
            animCurve.SmoothTangents(1, .025f);
            AS.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animCurve);

            AS.dopplerLevel = 0f;
            AS.spread = 1f;
        }

        internal struct SendVoiceChat : INetMessage
		{
            internal byte[] ba;
            internal int chan;
            internal NetworkInstanceId user;
            internal int chunkID;
            internal int chunkCount;
            internal bool isEndPoint;
            internal bool localizedAudio;
            internal int localizedAudioDistance;
            public void Serialize(NetworkWriter writer)
            {
                writer.Write(ba.Length);
                writer.Write(ba, ba.Length);
                writer.Write(user);
                writer.Write(chunkID);
                writer.Write(chunkCount);
                writer.Write(chan);
                writer.Write(isEndPoint);
                writer.Write(localizedAudio);
                writer.Write(localizedAudioDistance);
            }
            public void Deserialize(NetworkReader reader)
            {
                var array_length = reader.ReadInt32();
                this.ba = reader.ReadBytes(array_length);
                this.user = reader.ReadNetworkId();
                this.chunkID = reader.ReadInt32();
                this.chunkCount = reader.ReadInt32();
                this.chan = reader.ReadInt32();
                this.isEndPoint = reader.ReadBoolean();
                this.localizedAudio = reader.ReadBoolean();
                this.localizedAudioDistance = reader.ReadInt32();
            }
            public void OnReceived()
			{

                var thisID = chunkID;

                if (user == NetworkUser.localPlayers[0].netId && !VoiceChat.loopBackAudio.GetValue()) return;

                if(instance.chunkBuffer.All(chunkcollection => chunkcollection.chunkID != thisID))
                {
                    var thisChunkCollection = new chunkCollection(new List<chunkData>(), thisID);
                    instance.chunkBuffer.Add(thisChunkCollection);
                }

                var chunkData = new chunkData(ba.ToList());

                instance.chunkBuffer.First(chunkcollection => chunkcollection.chunkID == thisID).chunkData.Add(chunkData);

                //VoiceChat.DebugPrint("End stream? " + isEndPoint);

                if (instance.chunkBuffer.First(chunkcollection => chunkcollection.chunkID == thisID).chunkData.Count == chunkCount || isEndPoint)
                {
                    var chunkDataCombined = new List<byte>();

                    instance.chunkBuffer.First(chunkcollection => chunkcollection.chunkID == thisID).chunkData.ForEachTry(data =>
                    {
                        data.chunk.ForEachTry(data_piece =>
                        {
                            chunkDataCombined.Add(data_piece);
                        });
                    });

                    VoiceChat.DebugPrint("Voicechat Bytes Received: " + chunkDataCombined.ToArray().Length);

                    byte[] ba_decompressed = chunkDataCombined.Count != 0 ? CLZF2.Decompress(chunkDataCombined.ToArray()) : chunkDataCombined.ToArray();




                    instance.chunkBuffer.RemoveAll(chunkcollection => chunkcollection.chunkID == thisID);


                    var sampleData = new sampleData(ba_decompressed.ToList());

                    var thisSampleID = user;

                    if (instance.sampleBuffer.All(chunkcollection => chunkcollection.id != thisSampleID))
                    {
                        var thisSampleCollection = new sampleCollection(new List<sampleData>(), thisSampleID);
                        instance.sampleBuffer.Add(thisSampleCollection);
                    }



                    instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).sampleData.Add(sampleData);



                    //VoiceChat.DebugPrint("BufferLength = " + instance.bufferLength);
                    //VoiceChat.DebugPrint("Finish Up = " + isEndPoint);


                    instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).lastSampleTime = DateTime.Now;

                    if (instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).sampleData.Count >= bufferLength || isEndPoint)
                    {


                        var sampleDataCombined = new List<byte>();

                        /*
                        if (instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).sampleData.Count < instance.bufferLength) {
                            instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).finishBuffer = false;
                        }
                        */

                        instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).sampleData.ForEachTry(data =>
                        {
                            data.sample.ForEachTry(data_piece =>
                            {
                                sampleDataCombined.Add(data_piece);
                            });
                        });

                        //instance.sampleBuffer.First(samplecollection => samplecollection.id == thisSampleID).sampleData.Clear();

                        if(instance.sampleBuffer.RemoveAll(samplecollection => samplecollection.id == thisSampleID) > 0)
                        {
                            VoiceChat.DebugPrint("Sample collection removed!");
                        }


                        ba_decompressed = sampleDataCombined.ToArray();

                        VoiceChat.DebugPrint("Playing " + ba_decompressed.Length + " bytes of voice data!");
                        VoiceChat.DebugPrint("Bitrate/Frequency: "+FREQUENCY);

                        var float_array = ToFloatArray(ba_decompressed);

                        if (float_array.Length < 1) return;

                        for (int s = 0; s < float_array.Length; s++)

                        {
                            float_array[s] = float_array[s] * 5; 
                        }

                        for (int s = 0; s < float_array.Length; s++)

                        {
                            float_array[s] = float_array[s] * ((VoiceChat.voiceVolume.GetValue() / 100) * VoiceChat.audioMasterMultiplier);
                        }

                        bool hasBody = false;
                        CharacterBody body = null;
                        var index = 0;
                        foreach (var user in NetworkUser.readOnlyInstancesList)
                        {
                            VoiceChat.DebugPrint("Player found at index: " + index);
                            if (user.GetCurrentBody())
                            {
                                VoiceChat.DebugPrint("Body found at index: " + index);
                                if (user.netId == thisSampleID)
                                {
                                    //VoiceChat.DebugPrint("yo what the heck!");
                                    hasBody = true;
                                    body = user.GetCurrentBody();
                                }
                            }
                            index++;
                        }


                        var audioClip = AudioClip.Create("", float_array.Length, chan, FREQUENCY, false);
                        audioClip.SetData(float_array, 0);

                        VoiceChat.DebugPrint("3D Audio: " + localizedAudio);

                        if (instance.players.Any(player => player.identifier == thisSampleID))
                        {
                            var player = instance.players.First(playerinstance => playerinstance.identifier == thisSampleID);

                            var gameObject = new GameObject("VoicePlayer");

                            var audioPlayer = Instantiate(gameObject);


                            if (hasBody)
                            {
                                audioPlayer.transform.localPosition = body.gameObject.transform.position;
                            }


                            AudioSource audio1 = audioPlayer.AddComponent<AudioSource>();

                            if (!hasBody || !localizedAudio)
                            {
                                audio1.volume = 1.0f;
                                audio1.spatialBlend = 0.0f;
                                audio1.minDistance = 0.0f;
                                audio1.maxDistance = 99999999f;
                                audio1.panStereo = 0.0f;
                                UnrealisticRolloff(audio1);
                                VoiceChat.DebugPrint("Setting up 2D audio.");
                            }
                            else
                            {
                                audio1.volume = 1.0f;
                                audio1.spatialBlend = 1.0f;
                                audio1.minDistance = 0.0f;
                                audio1.maxDistance = localizedAudioDistance;
                                audio1.panStereo = 0.0f;
                                RealisticRolloff(audio1);
                                VoiceChat.DebugPrint("Setting up 3D audio.");
                            }
                            var extra_time = 0f;
                            foreach( var source in player.sources)
                            {
                                extra_time += source.clip.length - source.time;
                            }
                            player.sources.Add(audio1);
                            audio1.clip = audioClip;
                            audio1.PlayScheduled(AudioSettings.dspTime + extra_time);
                        }
                        else
                        {
                            var gameObject = new GameObject("VoicePlayer");

                            var audioPlayer = Instantiate(gameObject);


                            if (hasBody)
                            {
                                audioPlayer.transform.localPosition = body.gameObject.transform.position;
                            }

                            AudioSource audio1 = audioPlayer.AddComponent<AudioSource>();

                            if (!hasBody || !localizedAudio)
                            {
                                audio1.volume = 1.0f;
                                audio1.spatialBlend = 0.0f;
                                audio1.minDistance = 0.0f;
                                audio1.maxDistance = 99999999f;
                                audio1.panStereo = 0.0f;
                                UnrealisticRolloff(audio1);
                                VoiceChat.DebugPrint("Setting up 2D audio.");
                            }
                            else
                            {
                                audio1.volume = 1.0f;
                                audio1.spatialBlend = 1.0f;
                                audio1.minDistance = 0.0f;
                                audio1.maxDistance = localizedAudioDistance;
                                audio1.panStereo = 0.0f;
                                RealisticRolloff(audio1);
                                VoiceChat.DebugPrint("Setting up 3D audio.");
                            }

                            var player = new audioPlayer(thisSampleID);
                            
                            player.sources.Add(audio1);
                            instance.players.Add(player);
                            audio1.clip = audioClip;

                            audio1.PlayScheduled(AudioSettings.dspTime);
                        }


                        /*
                        AudioSource audio = instance.GetComponent<AudioSource>();

                        if (!audio)
                        {
                            audio = instance.gameObject.AddComponent<AudioSource>();
                            audio.maxDistance = 1000000000;
                        }

                         audio.clip = AudioClip.Create("", f.Length, chan, FREQUENCY, false);
                         audio.clip.SetData(f, 0);
                         audio.volume = VoiceChat.volume;
                         audio.Play();*/



                        //VoiceChat.DebugPrint("we got this far!");

                        

                        //VoiceChat.DebugPrint("Playing " + ba_decompressed.Length + " bytes of audio data.");

                        //SoundPlayer.Play()


                    }
                }
            }

        }

        public static byte[] GetSamplesWaveData(float[] samples, int samplesCount)
        {
            var pcm = new byte[samplesCount * 2];
            int sampleIndex = 0,
                pcmIndex = 0;

            while (sampleIndex < samplesCount)
            {
                var outsample = (short)(samples[sampleIndex] * short.MaxValue);
                pcm[pcmIndex] = (byte)(outsample & 0xff);
                pcm[pcmIndex + 1] = (byte)((outsample >> 8) & 0xff);

                sampleIndex++;
                pcmIndex += 2;
            }

            return pcm;
        }

        public static byte[] ToByteArray(float[] floatArray)
        {
            var byteArray = new byte[floatArray.Length * 4];
            Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }

        public static float[] ToFloatArray(byte[] byteArray)
        {
            var floatArray = new float[byteArray.Length / 4];
            Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);

            return floatArray;
        }
    }
    public static class ArrayExtensions
    {
        /// <summary>
        /// Splits <paramref name="source"/> into chunks of size not greater than <paramref name="chunkMaxSize"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">Array to be split</param>
        /// <param name="chunkMaxSize">Max size of chunk</param>
        /// <returns><see cref="IEnumerable{T}"/> of <see cref="Array"/> of <typeparam name="T"/></returns>
        public static IEnumerable<T[]> AsChunks<T>(this T[] source, int chunkMaxSize)
        {
            var pos = 0;
            var sourceLength = source.Length;
            do
            {
                var len = Math.Min(pos + chunkMaxSize, sourceLength) - pos;
                if (len == 0)
                {
                    yield break; ;
                }
                var arr = new T[len];
                Array.Copy(source, pos, arr, 0, len);
                pos += len;
                yield return arr;
            } while (pos < sourceLength);
        }
    }
}
