using System;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public class fbCamera : IInputSubscriber
    {
        public fbRectangle Camera;
        private fbEngine engine;

        public fbCamera(fbEngine engine)
        {
            this.engine = engine;
            Camera = new fbRectangle(
                Vector2.Zero,
                engine.GetSize()
            );

            new InputSubscriber(this)
                .Subscribe("+camera-up")
                .Subscribe("+camera-down")
                .Subscribe("+camera-left")
                .Subscribe("+camera-right")
                .Register();
        }

        private float cameraScaling
        {
            get { return Camera.Size.X / engine.GetSize().X; }
        }

        private const float keyboardScrollSpeed = 10f;
        private const float mouseScrollSpeed = 8f;
        private const float edgeSize = 10f;

        //position ON SCREEN
        private void ZoomAt(Vector2 position, float amount)
        {
            amount *= -1;

            if (
                position.X < 0 || position.X > engine.GetSize().X ||
                position.Y < 0 || position.Y > engine.GetSize().Y
            )
                throw new ArgumentException("Bad zoom point.");

            Vector2 deltaSize = Camera.Size - Camera.Size + new Vector2(amount);
            deltaSize.Y = deltaSize.X / engine.GetAspectRatio();

            Vector2 minSize = engine.GetSize() / 20f;

            if (Camera.Size.X + deltaSize.X < minSize.X)
                deltaSize.X = minSize.X - Camera.Size.X;
            if (Camera.Size.Y + deltaSize.Y < minSize.Y)
                deltaSize.Y = minSize.Y - Camera.Size.Y;

            Vector2 bias = position / engine.GetSize();
            Camera.Position -= deltaSize * bias;
            Camera.Size += deltaSize;
        }

        public void CenterAt(Vector2 worldPoint)
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            if(engine.MouseWheelDelta != 0)
                ZoomAt(engine.MousePosition, engine.MouseWheelDelta * 1f);

            Vector2 mouseScroll = Vector2.Zero;
            if (engine.Active)
            {
                if (engine.MousePosition.X < edgeSize) mouseScroll.X -= 1;
                if (engine.MousePosition.X > engine.GetSize().X - edgeSize)
                    mouseScroll.X += 1;
                if (engine.MousePosition.Y < edgeSize) mouseScroll.Y -= 1;
                if (engine.MousePosition.Y > engine.GetSize().Y - edgeSize)
                    mouseScroll.Y += 1;
            }

            Camera.Position +=
                mouseScroll * mouseScrollSpeed * cameraScaling;
        }

        public fbRectangle WorldToScreen(fbRectangle rectangle)
        {
            rectangle.Position -= Camera.Position;

            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            rectangle.Position *= scaleFactor;
            rectangle.Size *= scaleFactor;

            return rectangle;
        }

        public Vector2 ScreenToWorld(
            Vector2 position
        ) {
            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            return position / scaleFactor + Camera.Position;
        }

        public Rectangle ScreenToWorld(Rectangle rectangle)
        {
            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            rectangle.X = (int)(rectangle.X / scaleFactor.X);
            rectangle.Y = (int)(rectangle.Y / scaleFactor.Y);
            rectangle.Width = (int)(rectangle.Width / scaleFactor.X);
            rectangle.Height = (int)(rectangle.Height / scaleFactor.Y);

            rectangle.X += (int)Camera.Position.X;
            rectangle.Y += (int)Camera.Position.Y;

            return rectangle;
        }

        public Vector2 Scale(Vector2 scalee)
        {
            return scalee * (1f / cameraScaling);
        }

        public float Scale(float scalee)
        {
            return scalee * (1f / cameraScaling);
        }

        public void ReceiveInput(string s)
        {
            Vector2 keyboardScroll = Vector2.Zero;

            switch (s)
            {
                case "+camera-up": keyboardScroll.Y -= 1; break;
                case "+camera-left": keyboardScroll.X -= 1; break;
                case "+camera-down": keyboardScroll.Y += 1; break;
                case "+camera-right": keyboardScroll.X += 1; break;
                default: throw new ArgumentException();
            }

            Camera.Position +=
                keyboardScroll * keyboardScrollSpeed * cameraScaling;
        }
    }
}