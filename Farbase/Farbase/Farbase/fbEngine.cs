﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Farbase
{
    public class fbEngine
    {
        public fbApplication App;

        public Font DefaultFont;

        public Dictionary<string, Texture2D> Textures;
        public string GraphicsSet = "16";
        private KeyboardState? ks, oks;
        private MouseState? ms, oms;

        private List<DrawCall> drawCalls;

        public fbEngine(fbApplication app)
        {
            App = app;
            drawCalls = new List<DrawCall>();

        }

        public void SetSize(int width, int height)
        {
            App.Graphics.PreferredBackBufferWidth = width;
            App.Graphics.PreferredBackBufferHeight = height;
            App.Graphics.ApplyChanges();
        }

        public Vector2 GetSize()
        {
            return new Vector2(
                App.Graphics.PreferredBackBufferWidth,
                App.Graphics.PreferredBackBufferHeight
            );
        }

        public float GetAspectRatio()
        {
            return (float)
                App.Graphics.PreferredBackBufferWidth /
                App.Graphics.PreferredBackBufferHeight;
        }

        public Texture2D LoadTexture(string name, string path)
        {
            path = path.Replace("@", GraphicsSet);
            name = name.ToLower();

            if (Textures.ContainsKey(name))
                throw new ArgumentException("Texture key already assigned.");

            path = Directory.GetCurrentDirectory() + "/" + path;
            Texture2D texture;

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                texture = Texture2D.FromStream(App.GraphicsDevice, stream);
            }

            Textures.Add(name, texture);
            return texture;
        }

        public Texture2D GetTexture(string name)
        {
            name = name.ToLower();

            if (!Textures.ContainsKey(name))
                throw new ArgumentException("Sought texture key not assigned.");

            return Textures[name];
        }

        public Vector2 GetTextureSize(string name)
        {
            Texture2D texture = GetTexture(name);
            return new Vector2(texture.Width, texture.Height);
        }

        public void Update()
        {
            oms = ms;
            ms = Mouse.GetState();
            oks = ks;
            ks = Keyboard.GetState();
        }

        public void Draw(DrawCall dc)
        {
            if (dc.Destination.Size.X == -1)
                dc.Destination.Size.X = dc.Texture.Width;
            if (dc.Destination.Size.Y == -1)
                dc.Destination.Size.Y = dc.Texture.Height;
            drawCalls.Add(dc);
        }

        public void Draw(
            Texture2D texture,
            fbRectangle destination,
            Color coloring = default(Color),
            int depth = 0
        ) {
            if (destination.Size.X == -1) destination.Size.X = texture.Width;
            if (destination.Size.Y == -1) destination.Size.Y = texture.Height;

            drawCalls.Add(
                new DrawCall(texture, destination, depth, coloring)
            );
        }

        private Rectangle FromfbRectangle(fbRectangle rectangle)
        {
            return new Rectangle(
                (int)Math.Floor(rectangle.Position.X),
                (int)Math.Floor(rectangle.Position.Y),
                (int)Math.Floor(rectangle.Size.X),
                (int)Math.Floor(rectangle.Size.Y)
            );
        }

        public void Render()
        {
            App.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullCounterClockwise
            );

            foreach(DrawCall dc in drawCalls.OrderByDescending(dc => dc.Depth))
            {
                if(dc.Source != null)
                    App.SpriteBatch.Draw(
                        dc.Texture,
                        FromfbRectangle(dc.Destination),
                        FromfbRectangle(dc.Source),
                        dc.Coloring
                    );
                else
                    App.SpriteBatch.Draw(
                        dc.Texture,
                        FromfbRectangle(dc.Destination),
                        dc.Coloring
                    );
            }

            drawCalls.Clear();
            App.SpriteBatch.End();
        }

        public bool Active { get { return App.IsActive; } }

        public bool MouseInside {
            get
            {
                if (ms == null) return false;
                return
                    ms.Value.X > 0 && ms.Value.X < GetSize().X &&
                    ms.Value.Y > 0 && ms.Value.Y < GetSize().Y;
            }
        }

        public Vector2 MousePosition
        {
            get
            {
                if (ms == null) return Vector2.Zero;
                return new Vector2(
                    ms.Value.X.Clamp(0, (int)GetSize().X),
                    ms.Value.Y.Clamp(0, (int)GetSize().Y)
                );
            }
        }

        public int MouseWheelDelta {
            get {
                if (ms == null || oms == null) return 0;
                return ms.Value.ScrollWheelValue - oms.Value.ScrollWheelValue;
            }
        }

        public bool ButtonPressed(int button)
        {
            if (ms == null || oms == null) return false;

            switch (button)
            {
                case 0:
                    return
                        ms.Value.LeftButton == ButtonState.Pressed &&
                        oms.Value.LeftButton == ButtonState.Released;
                case 1:
                    return
                        ms.Value.MiddleButton == ButtonState.Pressed &&
                        oms.Value.MiddleButton == ButtonState.Released;
                case 2:
                    return
                        ms.Value.RightButton == ButtonState.Pressed &&
                        oms.Value.RightButton == ButtonState.Released;
                default:
                    throw new ArgumentException("Invalid mouse button #.");
            }
        }
        public bool ButtonDown(int button)
        {
            if (ms == null || oms == null) return false;

            switch (button)
            {
                case 0:
                    return ms.Value.LeftButton == ButtonState.Pressed;
                case 1:
                    return ms.Value.MiddleButton == ButtonState.Pressed;
                case 2:
                    return ms.Value.RightButton == ButtonState.Pressed;
                default:
                    throw new ArgumentException("Invalid mouse button #.");
            }
        }

        public bool KeyPressed(Keys key)
        {
            if (ks == null || oks == null) return false;
            return ks.Value.IsKeyDown(key) && !oks.Value.IsKeyDown(key);
        }

        public bool KeyDown(Keys key)
        {
            if (ks == null || oks == null) return false;
            return ks.Value.IsKeyDown(key);
        }

        public void Exit()
        {
            App.Exit();
        }
    }
}
