﻿using System;

using Heirloom.Math;
using Heirloom.OpenGLES;

namespace Heirloom.Drawing.OpenGLES
{
    internal class Texture : IDisposable
    {
        private bool _isDisposed = false;

        #region Constructors

        public Texture(OpenGLGraphics context, IntSize size)
        {
            Size = size;

            var levels = ComputeMipLevels(size);

            Handle = context.Invoke(() =>
            {
                var handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, handle);

                // 
                GL.TexStorage2D(TextureImageTarget.Texture2D, levels, TextureSizedFormat.RGBA8, size.Width, size.Height);

                // TODO: Somehow configurable?
                GL.SetTextureParameter(TextureTarget.Texture2D, TextureParameter.MagFilter, (int) TextureMagFilter.Nearest);
                GL.SetTextureParameter(TextureTarget.Texture2D, TextureParameter.MinFilter, (int) TextureMinFilter.LinearMipNearest);
                GL.SetTextureParameter(TextureTarget.Texture2D, TextureParameter.WrapS, (int) TextureWrap.Clamp);
                GL.SetTextureParameter(TextureTarget.Texture2D, TextureParameter.WrapT, (int) TextureWrap.Clamp);

                // GL_TEXTURE_MAX_ANISOTROPY
                // GL.SetTextureParameter(TextureTarget.Texture2D, (TextureParameter) 0x84FE, 16F);

                //
                // GL.GenerateMipmap(TextureTarget.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, 0);

                return handle;
            });
        }

        ~Texture()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        internal uint Handle { get; }

        public uint Version { get; private set; }

        public IntSize Size { get; }

        public int Width => Size.Width;

        public int Height => Size.Height;

        #endregion

        /// <summary>
        /// Computes the number of mip levels for a texture of the given size.
        /// </summary>
        private static int ComputeMipLevels(IntSize size)
        {
            var levels = 1;

            var w = size.Width;
            var h = size.Height;

            while (w > 1 && h > 1)
            {
                levels++;
                w /= 2;
                h /= 2;
            }

            return levels;
        }

        /// <summary>
        /// Update the GPU representation of this texture with the given image data.
        /// </summary>
        internal void Update(OpenGLGraphics gfx, Image image)
        {
            // Validate we were provide a non-null image.
            if (image == null) { throw new ArgumentNullException(nameof(image)); }

            // Image provided must be a 'top level' image.
            if (image.Source != null) { throw new ArgumentException($"Image must be the root image", nameof(image)); }

            // Ensure the image provided has matching dimensions to the texture
            if (image.Width != Width || image.Height != Height) { throw new ArgumentException($"Image did not match texture dimensions", nameof(image)); }

            gfx.Invoke(() =>
            {
                // 
                GL.BindTexture(TextureTarget.Texture2D, Handle);

                // Update entire image region
                GL.TexSubImage2D(TextureImageTarget.Texture2D, 0,
                    0, 0, Width, Height, // Sub Region
                    TexturePixelFormat.RGBA, TexturePixelType.UnsignedByte,
                    image.GetPixels());

                // Generate mips
                GL.GenerateMipmap(TextureTarget.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }, false);

            // Store version to mark as updated
            Version = image.Version;
        }

        internal void GenerateMips(uint version)
        {
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.GenerateMipmap(TextureTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Store version to mark as updated
            Version = version;
        }

        #region Dispose

        private void Dispose(bool disposeManaged)
        {
            if (!_isDisposed)
            {
                if (disposeManaged)
                {
                    // TODO: dispose managed objects.
                }

                // TODO: free unmanaged resources
                // Schedule on *some* context for deletion?
                Console.WriteLine("WARN: Disposing Texture! OpenGL Resource Not Deleted.");

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
