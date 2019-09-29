﻿using System;

using Heirloom.Desktop;
using Heirloom.Drawing;

namespace Heirloom.Game.Desktop
{
    public abstract class DesktopGame : AbstractGame
    {
        protected DesktopGame(string title, bool vsync = true, MultisampleQuality multisample = MultisampleQuality.None)
            : base(title)
        {
            Window = new DesktopGameWindow(this, vsync, multisample);
        }

        public GameWindow Window { get; }

        public static void Run<TGame>() where TGame : DesktopGame, new()
        {
            Application.Run(() =>
            {
                // Create a new instance of the game
                var game = new TGame();
                game.Window.Run();
            });
        }

        internal new void Update(RenderContext ctx, float dt)
        {
            base.Update(ctx, dt);
        }

        private class DesktopGameWindow : GameWindow
        {
            public DesktopGameWindow(DesktopGame game, bool vsync, MultisampleQuality multisample)
                : base(game.Title, vsync: vsync, multisample: multisample)
            {
                // 
                Game = game ?? throw new ArgumentNullException(nameof(game));

                // Add input source from this window
                Input.AddInputSource(new StandardDesktopInput(this));
            }

            public DesktopGame Game { get; }

            protected override void Update(RenderContext ctx, float dt)
            {
                Game.Update(ctx, dt);
            }
        }
    }
}
