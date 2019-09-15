﻿using System;
using System.Runtime.CompilerServices;

using Heirloom.Math;

namespace Heirloom.Drawing
{
    public delegate void LayoutCharacterCallback(string text, int index, ref CharacterLayoutState state);

    public delegate void DrawTextCallback(string text, int index, ref CharacterRenderState state);

    public struct CharacterRenderState
    {
        /// <summary>
        /// The current character.
        /// </summary>
        public UnicodeCharacter Character;

        /// <summary>
        /// The position of the glyph.
        /// </summary>
        public Vector Position;

        /// <summary>
        /// The color of the glyph.
        /// </summary>
        public Color Color;
    }

    public struct CharacterLayoutState
    {
        /// <summary>
        /// The current character.
        /// </summary>
        public UnicodeCharacter Character;

        /// <summary>
        /// The position of the glyph.
        /// </summary>
        public Vector Position;

        /// <summary>
        /// The metrics of the glyph being rendered.
        /// </summary>
        public GlyphMetrics Metrics { get; internal set; }
    }

    /// <summary>
    /// Controls how text is aligned to the layout rectangle.
    /// </summary>
    public enum TextAlign
    {
        /// <summary>
        /// Text is aligned to the left.
        /// </summary>
        Left,

        /// <summary>
        /// Text is aligned to the center.
        /// </summary>
        Center,

        /// <summary>
        /// Text is aligned to the right.
        /// </summary>
        Right
    }

    public static class TextRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnicodeCharacter GetCharacter(this string text, int i)
        {
            return (UnicodeCharacter) char.ConvertToUtf32(text, i);
        }

        #region Draw Text (Extension Methods)

        public static Vector DrawText(this RenderContext ctx, string text, Vector position, Font font, int size)
        {
            return DrawText(ctx, text, position, font, size, TextAlign.Left, null);
        }

        public static Vector DrawText(this RenderContext ctx, string text, Vector position, Font font, int size, DrawTextCallback callback)
        {
            return DrawText(ctx, text, position, font, size, TextAlign.Left, callback);
        }

        public static Vector DrawText(this RenderContext ctx, string text, Vector position, Font font, int size, TextAlign align)
        {
            return DrawText(ctx, text, position, font, size, align, null);
        }

        public static Vector DrawText(this RenderContext ctx, string text, Vector position, Font font, int size, TextAlign align, DrawTextCallback callback)
        {
            var bounds = GetAnchoredTextRect(text, font, size, position, align);
            return DrawText(ctx, text, bounds, font, size, align, callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector DrawText(this RenderContext ctx, string text, Rectangle bounds, Font font, int size)
        {
            return DrawText(ctx, text, bounds, font, size, TextAlign.Left, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector DrawText(this RenderContext ctx, string text, Rectangle bounds, Font font, int size, DrawTextCallback callback)
        {
            return DrawText(ctx, text, bounds, font, size, TextAlign.Left, callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector DrawText(this RenderContext ctx, string text, Rectangle bounds, Font font, int size, TextAlign align)
        {
            return DrawText(ctx, text, bounds, font, size, align, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector DrawText(this RenderContext ctx, string text, Rectangle bounds, Font font, int size, TextAlign align, DrawTextCallback callback)
        {
            if (font == null) { throw new ArgumentNullException(nameof(font)); }
            if (size < 1) { throw new ArgumentException("Font size must be greater than zero.", nameof(size)); }

            // Remember context state
            var color = ctx.Color;

            // Select atlas
            var atlas = FontManager.GetAtlas(font, size);

            // Character render state
            var state = new CharacterRenderState { Color = color };

            // Layout text
            LayoutText(text, bounds, align, atlas, (string _, int index, ref CharacterLayoutState layout) =>
            {
                // Set initial state
                state.Character = layout.Character;
                state.Position = layout.Position;
                state.Color = color;

                // Process character (per character animation, etc)
                callback?.Invoke(text, index, ref state);

                // Compute transform
                // todo: I think we need to consider pixel scale, but if we don't then the divide can be removed.
                var x = Calc.Floor((state.Position.X + layout.Metrics.Offset.X) / ctx.ApproximatePixelScale) * ctx.ApproximatePixelScale;
                var y = Calc.Floor((state.Position.Y + layout.Metrics.Offset.Y + atlas.Metrics.Ascent) / ctx.ApproximatePixelScale) * ctx.ApproximatePixelScale;

                // Get glyph image
                var image = atlas.GetImage(state.Character);

                // If has image data, draw to surface
                if (image != null)
                {
                    // Draw to surface
                    ctx.Color = state.Color;
                    ctx.Draw(image, Matrix.CreateTranslation(x, y));
                }
            });

            // Restore context state
            ctx.Color = color;

            return state.Position;
        }

        #endregion 

        #region Layout

        private static Rectangle GetAnchoredTextRect(in string text, in Font font, in int size, in Vector position, in TextAlign align)
        {
            var textSize = font.MeasureText(text, size);

            var pos = position;

            switch (align)
            {
                default:
                case TextAlign.Left:
                    // nothing to do
                    break;

                case TextAlign.Center:
                    pos.X -= textSize.Width / 2F;
                    break;

                case TextAlign.Right:
                    pos.X -= textSize.Width;
                    break;
            }

            return new Rectangle(pos, textSize);
        }

        /// <summary>
        /// Performs the layout of text within the given bounds with the specified font and size, invoking the callback at each location.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bounds"></param>
        /// <param name="font"></param>
        /// <param name="size"></param>
        /// <param name="characterCallback"></param>
        public static void LayoutText(string text, Rectangle bounds, Font font, int size, LayoutCharacterCallback characterCallback)
        {
            // Validate arguments
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            if (font == null) { throw new ArgumentNullException(nameof(font)); }
            if (size < 1) { throw new ArgumentException("Font size must be greater than zero.", nameof(size)); }
            if (characterCallback == null) { throw new ArgumentNullException(nameof(characterCallback)); }

            // Get atlas, layout text
            var atlas = FontManager.GetAtlas(font, size);
            LayoutText(text, bounds, TextAlign.Left, atlas, characterCallback);
        }

        internal static void LayoutText(string text, Rectangle bounds, TextAlign align, FontAtlas atlas, LayoutCharacterCallback characterCallback)
        {
            // Extract atlas properties for brevity
            var fontSize = atlas.FontSize;
            var font = atlas.Font;

            // Create character layout state
            var state = new CharacterLayoutState
            {
                Position = bounds.Position,
                Character = (UnicodeCharacter) 0
            };

            // Find the first break point (if none, set to -1)
            var nextBreak = FindNextBreak(in text, 0, state.Character, in state.Position, in bounds, atlas, out var lineWidth);
            var offsetX = ComputeAlignmentOffset(bounds, align, lineWidth);
            state.Position.X += offsetX; // First line alignment offset

            // For each character
            for (var i = 0; i < text.Length; i++)
            {
                // Beyond bottom of layout box, can't render anymore!
                // This is a 'truncate' vertical overflow mode
                if (state.Position.Y > bounds.Bottom)
                {
                    break;
                }

                // 
                var previous = state.Character;
                state.Character = GetCharacter(text, i);

                // Apply kerning with previous character
                state.Position.X += font.GetKerning(previous, state.Character, fontSize);

                // Get the relevant glyph, if exists (should always exist?)
                if (atlas.TryGetGlyph(state.Character, out var glyph))
                {
                    // Get metrics for glyph as the desired font size
                    state.Metrics = glyph.GetMetrics(fontSize);

                    // Process character, if kept, advance pen position
                    characterCallback(text, i, ref state);

                    // Apply horizontal advance
                    state.Position.X += state.Metrics.AdvanceWidth;
                }

                // We should break (newline, edge of bounds, etc)
                if (nextBreak == i)
                {
                    // Line feed
                    state.Position.Y += atlas.Metrics.LineAdvance;
                    state.Position.X = bounds.Left;

                    // Find the next break point
                    nextBreak = FindNextBreak(in text, i + 1, state.Character, in state.Position, in bounds, atlas, out lineWidth);
                    offsetX = ComputeAlignmentOffset(bounds, align, lineWidth);
                    state.Position.X += offsetX;
                }
            }
        }

        private static float ComputeAlignmentOffset(in Rectangle bounds, TextAlign align, float lineWidth)
        {
            var offset = 0F;
            if (align != TextAlign.Left) { offset = bounds.Width - lineWidth; }
            if (align == TextAlign.Center) { offset /= 2F; }
            return offset;
        }

        #endregion

        #region Line Break Checking

        // checks if character should break (newline or word too long, etc)
        internal static int FindNextBreak(in string text, int index, UnicodeCharacter previous, in Vector position, in Rectangle bounds, FontAtlas atlas, out float width)
        {
            var opportunity = -1;
            var opportunityEdge = 0F;

            var edge = position.X;
            var prevEdge = edge;

            // For each character in the future, do we see a possible break?
            for (var i = index; i < text.Length; i++)
            {
                var character = GetCharacter(text, i);
                var breakCategory = GetBreakCategory(character);
                var glyph = atlas.GetGlyph(character);

                // Add kerning
                var kerning = atlas.Font.GetKerning(previous, character, atlas.FontSize);
                edge += kerning;

                // Character is definintely a break (newline, etc)
                if (breakCategory == TextBreakCategory.Mandatory)
                {
                    width = prevEdge - position.X;
                    return i;
                }

                // Character could be a break if the next word violates the bounds (space, dash, etc)
                if (breakCategory == TextBreakCategory.Opportunity)
                {
                    // Mark the opportunity index
                    opportunityEdge = prevEdge;
                    opportunity = i;
                }

                if (glyph != null)
                {
                    // Advance right edge
                    var metrics = glyph.GetMetrics(atlas.FontSize);
                    edge += metrics.AdvanceWidth;

                    // We found a break opportunity, we need to now check if we violate the bounds
                    if (opportunity >= 0)
                    {
                        // Violated bounds (within a tolerance approximated by character width)
                        if (edge > (bounds.Right + (metrics.AdvanceWidth / 10F)))
                        {
                            width = opportunityEdge - position.X;
                            return opportunity;
                        }
                    }
                }

                // 
                previous = character;
                prevEdge = edge;
            }

            // No allowable break
            width = prevEdge - position.X;
            return -1;
        }

        // classifies a character into its break category
        internal static TextBreakCategory GetBreakCategory(UnicodeCharacter character)
        {
            var c = (char) character;

            // Break on whitespaces
            if (char.IsWhiteSpace(c))
            {
                if (c == '\n' || c == '\r') { return TextBreakCategory.Mandatory; }
                else { return TextBreakCategory.Opportunity; }
            }

            // Opportunity to break on dashes
            if (c == '-') { return TextBreakCategory.Opportunity; }

            // Shouldn't break
            return TextBreakCategory.None;
        }

        #endregion
    }
}
