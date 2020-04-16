using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Heirloom.Math;

namespace Heirloom.Drawing
{
    public abstract partial class Graphics
    {
        private static readonly Mesh _quadMesh = Mesh.CreateQuad(1, 1);
        private static readonly Mesh _temporaryMesh = new Mesh();

        private readonly Stack<GraphicsState> _stateStack = new Stack<GraphicsState>();

        #region Constructors

        /// <summary>
        /// Constructs a new graphics instance with the specified multisampling quality.
        /// </summary>
        protected Graphics(Surface surface)
        {
            // Create performance tracking object
            Performance = new DrawingPerformance();

            // Creates a dummy surface to represent the window surface
            DefaultSurface = surface;
        }

        /// <summary>
        /// Graphics Finalizer.
        /// </summary>
        ~Graphics()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets how often the default surface is presented to the screen per second.
        /// </summary>
        public float CurrentFPS => Performance.FrameRate.Average;

        /// <summary>
        /// Gets drawing performance information.
        /// </summary>
        public DrawingPerformance Performance { get; }

        /// <summary>
        /// Gets a value determining if this <see cref="Graphics"/> was disposed.
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// Gets the default surface (ie, window) of this render context.
        /// </summary>
        public Surface DefaultSurface { get; }

        /// <summary>
        /// Gets or sets the current surface.
        /// </summary>
        /// <remarks>
        /// When changed, the viewport is automatically reset to the full surface.
        /// </remarks>
        public abstract Surface Surface { get; set; }

        /// <summary>
        /// Gets or sets the active shader.
        /// </summary>
        public abstract Shader Shader { get; set; }

        /// <summary>
        /// Gets or sets the viewport in pixel coordinates.
        /// </summary>
        /// <remarks>
        /// When <see cref="Surface"/> is changed, the viewport is automatically reset to the full surface.
        /// </remarks>
        public abstract IntRectangle Viewport { get; set; }

        /// <summary>
        /// Get or sets the global transform.
        /// </summary>
        public abstract Matrix GlobalTransform { get; set; }

        /// <summary>
        /// Gets the inverse of the current global transform.
        /// </summary>
        public abstract Matrix InverseGlobalTransform { get; }

        /// <summary>
        /// Gets or sets the current blending mode.
        /// </summary>
        public abstract Blending Blending { get; set; }

        /// <summary>
        /// Gets or sets the current blending color.
        /// </summary>
        public abstract Color Color { get; set; }

        #endregion

        #region State Methods

        /// <summary>
        /// Reset current context state to defaults (default surface, full viewport, no transform, alpha and white).
        /// </summary>
        public void ResetState()
        {
            Shader = Shader.Default;
            Surface = DefaultSurface; // also adjusts viewport?
            Viewport = (0, 0, Surface.Width, Surface.Height);

            GlobalTransform = Matrix.Identity;
            Blending = Blending.Alpha;
            Color = Color.White;
        }

        /// <summary>
        /// Save the context state (push it on the state stack).
        /// </summary>
        public void PushState(bool reset = false)
        {
            _stateStack.Push(new GraphicsState
            {
                Shader = Shader,
                Blending = Blending,
                Color = Color,
                Surface = Surface,
                Transform = GlobalTransform,
                Viewport = Viewport
            });

            // If requested, reset state
            if (reset) { ResetState(); }
        }

        /// <summary>
        /// Restore the context state (pop from the state stack).
        /// </summary>
        public void PopState()
        {
            if (_stateStack.Count == 0)
            {
                // todo: Should this throw an exception instead?
                ResetState();
            }
            else
            {
                // Recover state values
                var state = _stateStack.Pop();

                Shader = state.Shader;
                Surface = state.Surface;
                Viewport = state.Viewport;
                GlobalTransform = state.Transform;
                Blending = state.Blending;
                Color = state.Color;
            }
        }

        #endregion

        #region Draw Methods

        /// <summary>
        /// Clears the current surface with the specified color.
        /// </summary>
        public abstract void Clear(Color color);

        /// <summary>
        /// Draws a mesh with the given image to the current surface.
        /// </summary>
        /// <param name="mesh">Some mesh.</param>
        /// <param name="image">Some image.</param>
        /// <param name="transform">Some transform.</param>
        public abstract void DrawMesh(ImageSource image, Mesh mesh, in Matrix transform);

        #endregion

        #region Read Methods

        /// <summary>
        /// Grab the pixels from a subregion of the current surface and return that image. (ie, a screenshot)
        /// </summary>
        /// <param name="region">A region within the currently set surface.</param>
        /// <returns>An image with a copy of the pixels on the surface.</returns>
        public abstract Image GrabPixels(IntRectangle region);

        /// <summary>
        /// Grab the pixels from the current surface and return that image. (ie, a screenshot)
        /// </summary>
        /// <returns>An image with a copy of the pixels on the surface.</returns>
        public Image GrabPixels()
        {
            return GrabPixels((0, 0, Surface.Width, Surface.Height));
        }

        #endregion

        #region Frame Statistics

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessStatistics()
        {
            // Compute statistics (fps, batch count, etc)
            Performance.ComputeStatistics(GetDrawCounts());

            // Draw the statistics overlay
            DrawStatisticsOverlay();
        }

        /// <summary>
        /// Populates and returns drawing metrics.
        /// </summary>
        /// <seealso cref="EndFrame"/>
        /// <remarks>
        /// Counts should be reset in the implementation of <see cref="EndFrame"/>.
        /// </remarks>
        protected abstract DrawCounts GetDrawCounts();

        private void DrawStatisticsOverlay()
        {
            if (Performance.OverlayMode != PerformanceOverlayMode.Disabled)
            {
                var oldColor = Color;
                ResetState();

                // == Measure Step

                // Statistics overlay text
                var text = GetPerformanceOverlayText();

                // Measure the rect needed to fit the text
                var textSize = TextLayout.Measure(text, Font.Default, 16);
                textSize.Inflate(8, 4);

                // Compute the text box at the top right of the screen
                var textBox = new Rectangle(Surface.Width - textSize.Width - 8, 8, textSize.Width, textSize.Height);

                // == Draw Step

                Color = CurrentFPS < 30 ? Color.Red : Color.DarkGray;

                // Draw textbox background
                DrawRect(textBox);

                Color = CurrentFPS < 30 ? Color.Black : Color.Pink;

                // Draw textbox border
                DrawRectOutline(textBox, 1);

                Color = CurrentFPS < 30 ? Color.Black : Color.Pink;

                // Draw text
                DrawText(text, new Vector(Surface.Width - textSize.Width, 12), Font.Default, 16);

                // Restore color and flush
                Color = oldColor;
                Flush();
            }
        }

        private string GetPerformanceOverlayText()
        {
            // todo: Perhaps humanize the numbers such that "215,123" becomes "215K"
            //       to keep things short and easy to read.
            return Performance.OverlayMode switch
            {
                PerformanceOverlayMode.Simple =>
                    $"FPS : {Performance.FrameRate.Average:N0}",

                PerformanceOverlayMode.Standard =>
                    $"Draws   : {Performance.DrawCount.Average,8:N0}\n" +
                    $"Batches : {Performance.BatchCount.Average,8:N0}\n" +
                    $"FPS     : {Performance.FrameRate.Average,8:N0}",

                PerformanceOverlayMode.Full =>
                    $"Draws   : {Performance.DrawCount.Average,8:N0} ± {Performance.DrawCount.Deviation,-8:N0}\n" +
                    $"Batches : {Performance.BatchCount.Average,8:N0} ± {Performance.BatchCount.Deviation,-8:N0}\n" +
                    $"FPS     : {Performance.FrameRate.Average,8:N0} ± {Performance.FrameRate.Deviation,-8:N0}",

                _ => throw new InvalidOperationException(),
            };
        }

        #endregion

        /// <summary>
        /// Present the drawing operations to the screen.
        /// </summary>
        public void RefreshScreen()
        {
            // Commit all pending work
            Flush(true);

            // Computes statistics (possibly drawing overlay)
            ProcessStatistics();

            // Causes the image to appear on screen
            SwapBuffers();

            // Mark the end of a frame
            EndFrame();
        }

        /// <summary>
        /// Causes the back and front buffers to be swapped.
        /// </summary>
        protected abstract void SwapBuffers();

        /// <summary>
        /// Called at the end of frame to do any last minute work (resetting metrics, buffers, etc).
        /// </summary>
        protected abstract void EndFrame();

        /// <summary>
        /// Submit all pending drawing operations, optionally blocking for completion.
        /// </summary>
        protected abstract void Flush(bool blockCompletion = false);

        /// <summary>
        /// Commits pending drawing operations, blocking until all operations complete.
        /// </summary>
        public void Commit()
        {
            Flush(true);
        }

        #region IDisposable Support

        /// <summary>
        /// Dispose and cleanup resources.
        /// </summary>
        protected virtual void Dispose(bool disposeManaged)
        {
            if (!IsDisposed)
            {
                if (disposeManaged)
                {
                    // Managed
                }

                // Unmanaged

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Dispose this graphics context, freeing any resources occupied by it.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Sets <see cref="GlobalTransform"/> to mimic a 2D camera. The center of the camera is set to <paramref name="center"/>.
        /// </summary>
        public void SetCameraTransform(Vector center, float scale = 1F)
        {
            var offset = (Vector) Viewport.Size / 2F;
            GlobalTransform = Matrix.CreateTransform(offset - center, 0, scale);
        }

        /// <summary>
        /// A little structure to keep tracking of counts of a drawn frame.
        /// </summary>
        protected internal struct DrawCounts
        {
            /// <summary>
            /// The number of batches.
            /// </summary>
            public int BatchCount;

            /// <summary>
            /// The number of 'things' drawn.
            /// </summary>
            public int DrawCount;

            /// <summary>
            /// The number of triangles drawn.
            /// </summary>
            /// <remarks>
            /// A simple image (ie, Quad) consists of two triangles.
            /// </remarks>
            public int TriangleCount;
        }
    }
}
