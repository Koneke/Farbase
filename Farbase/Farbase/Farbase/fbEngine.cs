using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Farbase
{
    public interface IInputSubscriber
    {
        void ReceiveInput(string s);
    }

    public class InputSubscriber
    {
        public static List<InputSubscriber> Subscribers =
            new List<InputSubscriber>();

        private IInputSubscriber subscriber;
        private List<string> subscriptions;

        //we can probably remove engine from this by simply
        //supplying an engine reference in update
        public InputSubscriber(IInputSubscriber sub)
        {
            subscriber = sub;
            subscriptions = new List<string>();
        }

        public void Register()
        {
            if (!Subscribers.Contains(this))
                Subscribers.Add(this);
        }

        public void Unregister()
        {
            Subscribers.Remove(this);
        }

        public void Update(fbEngine engine)
        {
            if (subscriptions == null)
                return;

            foreach (string binding in subscriptions)
            {
                if (binding[0] == '+')
                {
                    string _binding = binding.Substring(1, binding.Length - 1);
                    if (engine.BindingHeld(_binding))
                        subscriber.ReceiveInput(binding);
                }
                else
                {
                    if (engine.BindingPressed(binding))
                        subscriber.ReceiveInput(binding);
                }
            }
        }

        public void UnsubscribeAll()
        {
            subscriptions.Clear();
        }

        public void Unsubscribe(string s)
        {
            subscriptions.Remove(s.ToLower());
        }

        public InputSubscriber Subscribe(string s)
        {
            subscriptions.Add(s.ToLower());
            return this;
        }
    }

    public class KeyBinding
    {
        //c:d - ctrl-d
        //ac:s - ctrl-alt-s
        //e - e
        public bool Ctrl;
        public bool Shift;
        public bool Alt;
        public Keys Key;

        public bool Pressed;
        public bool Held;
        public bool Released;

        public KeyBinding(
            bool ctrl,
            bool alt,
            bool shift,
            Keys key
        ) {
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
            Key = key;
        }
    }

    public class fbEngine
    {
        private fbApplication app;

        public Font DefaultFont;

        public Dictionary<string, Texture2D> Textures;
        public string GraphicsSet = "16";

        private KeyboardState? ks, oks;
        private MouseState? ms, oms;
        private Dictionary<string, List<KeyBinding>> keyBindings;

        public int DeltaTime;

        private Dictionary<EventType, List<fbEventHandler>> subscribers;

        private List<DrawCall> drawCalls;

        public fbNetClient NetClient;

        // === === === === === === === === === ===

        public fbEngine(fbApplication app)
        {
            this.app = app;
            drawCalls = new List<DrawCall>();
            subscribers = new Dictionary<EventType, List<fbEventHandler>>();
            keyBindings = new Dictionary<string, List<KeyBinding>>();
            ReadKeybindingsFromFile("cfg/keybindings.cfg");
        }

        // === === === === === === === === === ===

        public void Push(Event e)
        {
            if (!subscribers.ContainsKey(e.GetEventType()))
            {
                throw new Exception("No subscriber to event.");
            }

            foreach (
                fbEventHandler handler in
                subscribers[e.GetEventType()]
            ) {
                handler.Handle(e);
            }
        }

        public void Subscribe(fbEventHandler handler, EventType eventType)
        {
            if (!subscribers.ContainsKey(eventType))
                subscribers.Add(eventType, new List<fbEventHandler>());

            subscribers[eventType].Add(handler);
        }

        public void Unsubscribe(fbEventHandler handler, EventType eventType)
        {
            subscribers[eventType].Remove(handler);
        }

        public void StartNetClient()
        {
            NetClient = new fbNetClient();
            Thread networkingThread = new Thread(NetClient.Start);
            networkingThread.Start();
        }

        public void SetSize(int width, int height)
        {
            app.Graphics.PreferredBackBufferWidth = width;
            app.Graphics.PreferredBackBufferHeight = height;
            app.Graphics.ApplyChanges();
        }

        public Vector2 GetSize()
        {
            return new Vector2(
                app.Graphics.PreferredBackBufferWidth,
                app.Graphics.PreferredBackBufferHeight
            );
        }

        public float GetAspectRatio()
        {
            return (float)
                app.Graphics.PreferredBackBufferWidth /
                app.Graphics.PreferredBackBufferHeight;
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
                texture = Texture2D.FromStream(app.GraphicsDevice, stream);
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

        public void Update(GameTime gameTime)
        {
            DeltaTime = gameTime.ElapsedGameTime.Milliseconds;

            oms = ms;
            ms = Mouse.GetState();

            oks = ks;
            ks = Keyboard.GetState();

            if (oks == null)
                return;

            foreach (string bindName in keyBindings.Keys)
            {
                foreach (KeyBinding binding in keyBindings[bindName])
                {
                    bool lastFrame =
                        binding.Ctrl ==
                            (oks.Value.IsKeyDown(Keys.LeftControl) ||
                             oks.Value.IsKeyDown(Keys.RightControl)) &&
                        binding.Alt ==
                            (oks.Value.IsKeyDown(Keys.LeftAlt) ||
                             oks.Value.IsKeyDown(Keys.RightAlt)) &&
                        binding.Shift ==
                            (oks.Value.IsKeyDown(Keys.LeftShift) ||
                             oks.Value.IsKeyDown(Keys.RightShift)) &&
                        oks.Value.IsKeyDown(binding.Key);

                    bool thisFrame =
                        binding.Ctrl ==
                            (ks.Value.IsKeyDown(Keys.LeftControl) ||
                             ks.Value.IsKeyDown(Keys.RightControl)) &&
                        binding.Alt ==
                            (ks.Value.IsKeyDown(Keys.LeftAlt) ||
                             ks.Value.IsKeyDown(Keys.RightAlt)) &&
                        binding.Shift ==
                            (ks.Value.IsKeyDown(Keys.LeftShift) ||
                             ks.Value.IsKeyDown(Keys.RightShift)) &&
                        ks.Value.IsKeyDown(binding.Key);

                    binding.Pressed = !lastFrame && thisFrame;
                    binding.Held = lastFrame && thisFrame;
                    binding.Released = lastFrame && !thisFrame;
                }
            }

            foreach (InputSubscriber subscriber in InputSubscriber.Subscribers)
            {
                subscriber.Update(this);
            }
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
            app.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullCounterClockwise
            );

            foreach(DrawCall dc in drawCalls.OrderByDescending(dc => dc.Depth))
            {
                if(dc.Source != null)
                    app.SpriteBatch.Draw(
                        dc.Texture,
                        FromfbRectangle(dc.Destination),
                        FromfbRectangle(dc.Source),
                        dc.Coloring
                    );
                else
                    app.SpriteBatch.Draw(
                        dc.Texture,
                        FromfbRectangle(dc.Destination),
                        dc.Coloring
                    );
            }

            drawCalls.Clear();
            app.SpriteBatch.End();
        }

        public bool Active { get { return app.IsActive; } }

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

        private void ReadKeybindingsFromFile(string path)
        {
            foreach(string ln in File.ReadLines(path))
            {
                if (ln == "") continue;

                string line = ln.ToLower();

                if (!line.Contains(',')) //we always need a name
                    throw new FormatException();

                List<string> split = line.Split(',').ToList();

                string name = split[split.Count - 1];
                name = name.Trim(new[] { '\t', ' ' });

                if (!keyBindings.ContainsKey(name))
                    keyBindings.Add(name, new List<KeyBinding>());

                split.RemoveAt(split.Count - 1);
                List<string> cmds = split;

                foreach (string cmd in cmds)
                {
                    string keyString;
                    bool c, a, s;
                    if (cmd.Contains(':'))
                    {
                        string modifiers = cmd.Split(':')[0];
                        keyString = cmd.Split(':')[1];
                        c = modifiers.Contains('c');
                        a = modifiers.Contains('a');
                        s = modifiers.Contains('s');
                    }
                    else
                    {
                        keyString = cmd;
                        c = a = s = false;
                    }

                    keyString = keyString.Trim(new[] { '\t', ' ' });
                    Keys key = (Keys)Enum.Parse(typeof (Keys), keyString, true);

                    keyBindings[name].Add(
                        new KeyBinding(c, a, s, key)
                    );
                }
            }
        }

        public bool BindingPressed(string name)
        {
            return keyBindings[name.ToLower()].Any(kb => kb.Pressed);
        }
        public bool BindingHeld(string name)
        {
            return keyBindings[name.ToLower()].Any(kb => kb.Held);
        }
        public bool BindingReleased(string name)
        {
            return keyBindings[name.ToLower()].Any(kb => kb.Released);
        }

        public void Exit()
        {
            app.Exit();
        }
    }
}
