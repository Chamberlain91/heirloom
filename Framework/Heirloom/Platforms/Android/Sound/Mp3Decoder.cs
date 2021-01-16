using System;
using System.Runtime.InteropServices;

namespace Heirloom.Sound.Android
{
    public unsafe sealed class Mp3Decoder : IAudioDecoder
    {
        private readonly void* _data;
        private readonly int _dataSize;

        private readonly void* _ptr;
        private readonly ulong _length;

        public Mp3Decoder(byte[] data)
        {
            // Copy mp3 file to unmanaged memory
            _data = (void*) Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, (IntPtr) _data, data.Length);
            _dataSize = data.Length;

            // Initialize MP3 system
            _ptr = NativeDecoder.alloc_mp3_struct();

            // Initialize decoder
            if (!NativeDecoder.drmp3_init_memory(_ptr, _data, _dataSize, null))
            {
                throw new InvalidOperationException("Unable to initialize MP3 decoder");
            }

            // Gets the number of frames
            _length = NativeDecoder.drmp3_get_pcm_frame_count(_ptr);
        }

        ~Mp3Decoder()
        {
            Dispose(disposing: false);
        }

        public int Length => (int) _length;

        public bool IsDisposed => _isDisposed;

        public bool Seek(int offset)
        {
            return NativeDecoder.drmp3_seek_to_pcm_frame(_ptr, (ulong) offset);
        }

        public int Decode(Span<short> samples)
        {
            fixed (short* p_samples = samples)
            {
                return (int) NativeDecoder.drmp3_read_pcm_frames_s16(_ptr, (ulong) (samples.Length / AudioBackend.Channels), p_samples) * AudioBackend.Channels;
            }
        }

        private bool _isDisposed;

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Nothing
                }

                // Free native memory
                Marshal.FreeHGlobal((IntPtr) _data);
                NativeDecoder.free_struct(_ptr);

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
