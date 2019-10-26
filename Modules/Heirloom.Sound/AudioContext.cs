﻿using System;

using Heirloom.Sound.Backends.MiniAudio;

namespace Heirloom.Sound
{
    public abstract class AudioContext : IDisposable
    {
        // used because OnSpeakerOutput or OnMicrophoneInput may be called concurrently
        private readonly bool _init = false;

        private float[] _buffer = Array.Empty<float>();
        private readonly int _sampleRate;

        #region Constructors

        internal AudioContext(int sampleRate)
        {
            if (sampleRate <= 0) { throw new InvalidOperationException("Sample rate must greater or equal to 1."); }
            _sampleRate = sampleRate;
            _init = true;
        }

        ~AudioContext()
        {
            Dispose(false);
        }

        #endregion

        #region Mixing

        internal void OnSpeakerOutput(Span<short> output)
        {
            if (!_init) { return; } // Not ready!

            // Ensure buffer is large enough for output
            if (_buffer.Length < output.Length) { Array.Resize(ref _buffer, output.Length); }
            var buffer = new Span<float>(_buffer, 0, output.Length);

            // Process speaker output
            AudioGroup.Default.MixOutput(buffer);

            // Write buffer (float) to device (short)
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = (short) (SoftClip(buffer[i]) * short.MaxValue);
                buffer[i] = 0;
            }
        }

        internal void OnMicrophoneInput(Span<short> input)
        {
            if (!_init) { return; } // Not ready! 

            // Process microphone input
            // TODO: Microphone support
        }

        /// <summary>
        /// Computes soft clamping of audio ( -1.0 to +1.0 ).
        /// </summary>
        private static float SoftClip(float x)
        {
            x /= (float) Math.Sqrt(Math.Sqrt(1 + (x * x * x * x)));
            return x;
        }

        #endregion

        #region Static / Singleton

        private static AudioContext _instance;

        /// <summary>
        /// Gets the audio context instance.
        /// This will initialize with defaults if not explicitly initialized beforehand.
        /// </summary>
        internal static AudioContext Instance
        {
            get
            {
                // If no contxt has been initialized, initialize a default.
                if (_instance == null) { Initialize(44100); }
                return _instance;
            }
        }

        public static int SampleRate => Instance._sampleRate;

        public static int Channels => 2;

        public static void Initialize(int sampleRate)
        {
            if (_instance == null)
            {
                // Create default
                _instance = new MiniAudioContext(sampleRate);

                // Dispose device when process exits, finalizer isn't being called
                // but this reliably is called on Window .NET and Linux Mono 5.2
                // todo: see behaviour on Android, macOS
                AppDomain.CurrentDomain.ProcessExit += (s, e) => _instance.Dispose();
            }
            else
            {
                throw new InvalidOperationException("Audio device already initialized");
            }
        }

        #endregion

        #region IDisposable Support

        private bool _isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Clean managed
                }

                // Clean unmanaged

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
