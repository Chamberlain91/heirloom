using System;

namespace Meadows.Drawing.OpenGLES
{
    internal sealed class ESShaderStage : IDisposable
    {
        public readonly uint Handle;

        #region Constructors

        public ESShaderStage(ShaderType type, string source)
        {
            Handle = GLES.CreateShader(type);
            GLES.ShaderSource(Handle, source);
            GLES.CompileShader(Handle);

            // 
            var info = GLES.GetShaderInfoLog(Handle);
            if (!string.IsNullOrWhiteSpace(info)) { Console.WriteLine(info); }

            // Failed to compile
            if (GLES.GetShader(Handle, ShaderParameter.CompileStatus) == 0)
            {
                throw new Exception(info);
            }
        }

        ~ESShaderStage()
        {
            Dispose(false);
        }

        #endregion

        #region Dispose

        private bool _isDisposed;

        private void Dispose(bool disposeManaged)
        {
            if (!_isDisposed)
            {
                if (disposeManaged)
                {
                    // ...
                }

                // Schedule for deletion on a GL thread.
                ESGraphicsBackend.Current.Invoke(() =>
                {
                    Log.Debug($"[Dispose] Shader Stage ({Handle})");
                    GLES.DeleteShader(Handle);
                });

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
