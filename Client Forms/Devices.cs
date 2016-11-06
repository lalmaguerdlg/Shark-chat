﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using NAudio.Wave;
using System.IO;
using System.Media;

namespace Client_Forms {
    static class Speaker {
        private static WaveOut speaker;
        private static WaveFormat format;
        private static BufferedWaveProvider bufferedProvider;
        private static bool readyToPlay = false;
        private static int lastChannels;
        //play audio from buffer
        //http://stackoverflow.com/questions/28792548/how-can-i-play-byte-array-of-audio-raw-data-using-naudio
        public static void Init( int channels ) {
            Dispose();
            lastChannels = channels;
            speaker = new WaveOut();
            format = new WaveFormat( 8000, 16, channels );
            bufferedProvider = new BufferedWaveProvider( format );
            speaker.Init( bufferedProvider );
            speaker.DesiredLatency = 25;
            speaker.Play();
            readyToPlay = true;
        }

        public static void PlayBuffer( byte[] bytes ) {
            if (speaker != null) {
                if (speaker.PlaybackState == PlaybackState.Playing || readyToPlay) {
                    try {
                        bufferedProvider.AddSamples( bytes, 0, bytes.Length );
                    }
                    catch (InvalidOperationException ex) {
                        bufferedProvider.ClearBuffer();
                    }
                }
            }
        }

        public static void ClearBuffer( ) {
            bufferedProvider.ClearBuffer();
        }

        public static void Dispose() {
            if (speaker != null) {
                speaker.Stop();
                speaker.Dispose();
            }
            readyToPlay = false;
        }
    }
    static class Camera {
        private static VideoCaptureDevice VideoSource = null;
        private static FilterInfoCollection VideoDevices;
        private static Action<Bitmap> onNewFrame;
        public static void OnNewFrameCallback( Action<Bitmap> callback ) {
            onNewFrame = callback;
        }
        private static bool canSend = true;
        public static bool CanSend {
            get { return canSend; }
            set { canSend = value; }
        }
        public static string OwnerChat { get; set; }
        public static bool Detect() {
            try {
                if (VideoSource != null) {
                    return true;
                }
                VideoDevices = new FilterInfoCollection( FilterCategory.VideoInputDevice );
                if (VideoDevices.Count > 0) {
                    VideoSource = new VideoCaptureDevice( VideoDevices[0].MonikerString );
                    VideoSource.VideoResolution = VideoSource.VideoCapabilities[0];
                    VideoSource.NewFrame += new NewFrameEventHandler( NewFrameEvent );
                    return true;
                }
                return false;
            }
            catch {
                //Error ocurred...
                return false;
            }
        }

        public static void Start() { VideoSource.Start(); }
        public static void Stop() { VideoSource.Stop(); }
        public static void Release() {
            VideoSource.SignalToStop();
            VideoSource = null;
        }
        public static bool IsRunning {
            get { return VideoSource.IsRunning; }
        }
        private static void NewFrameEvent( object sender, NewFrameEventArgs eventArgs ) {
            onNewFrame( (Bitmap)eventArgs.Frame.Clone() );
        }
    }
    //http://stackoverflow.com/questions/15101889/hearing-the-incoming-audio-from-mic
    static class Microphone {
        private static WaveIn audioInput = null;
        private static Action<byte[]> onAudioIn;
        public static void OnAudioInCallback( Action<byte[]> callback ) {
            onAudioIn = callback;
        }
        public static void StartRecording() {
            if (audioInput == null) {
                audioInput = new WaveIn();
                audioInput.WaveFormat = new WaveFormat( 8000, 16, Channels );
                audioInput.BufferMilliseconds = 25;
                audioInput.DataAvailable += DataAvailable;
                audioInput.StartRecording();
            }
        }
        public static void EndRecording() {
            if (audioInput != null) {
                audioInput.StopRecording();
            }
        }
        public static void Dispose() {
            if (audioInput != null) {
                audioInput.StopRecording();
                audioInput.Dispose();
                audioInput = null;
            }
        }
        public static int Channels {
            get { return WaveIn.GetCapabilities( 0 ).Channels; }
        }
        private static void DataAvailable( object sender, WaveInEventArgs e ) {
            onAudioIn( e.Buffer );
        }
    }
}
