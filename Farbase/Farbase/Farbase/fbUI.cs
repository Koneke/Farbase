using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum Alignment { Left, Right }

    public abstract class Widget
    {
        protected fbEngine engine;

        public ContainerWidget Parent;

        public int TopMargin, RightMargin, BottomMargin, LeftMargin;
        public int TopPadding, RightPadding, BottomPadding, LeftPadding;

        public Alignment Alignment;

        public Vector2 MarginSize
        {
            get {
                return new Vector2(
                    LeftMargin + RightMargin,
                    TopMargin + BottomMargin
                );
            }
        }

        public Vector2 PaddingSize
        {
            get {
                return new Vector2(
                    LeftPadding + RightPadding,
                    TopPadding + BottomPadding
                );
            }
        }

        public Vector2 TopLeftMargin {
            get { return new Vector2(LeftMargin, TopMargin); }
        }

        public Vector2 TopLeftPadding {
            get { return new Vector2(LeftPadding, TopPadding); }
        }

        public int Depth;

        protected Widget(
            fbEngine engine,
            int depth = -1
        ) {
            this.engine = engine;
            Depth = depth;
        }

        public abstract Vector2 GetSize();
        public abstract Vector2 GetSizeInternal();
        public abstract void Render(Vector2 position);

        public Widget Margins(int t, int r, int b, int l)
        {
            TopMargin = t;
            RightMargin = r;
            BottomMargin = b;
            LeftMargin = l;
            return this;
        }

        public Widget Margins(int amount)
        {
            return Margins(amount, amount, amount, amount);
        }

        public Widget Margins(int hamount, int vamount)
        {
            return Margins(vamount, hamount, vamount, hamount);
        }

        public Widget Padding(int t, int r, int b, int l)
        {
            TopPadding = t;
            RightPadding = r;
            BottomPadding = b;
            LeftPadding = l;
            return this;
        }

        public Widget Padding(int amount)
        {
            return Padding(amount, amount, amount, amount);
        }

        public Widget Padding(int hamount, int vamount)
        {
            return Padding(vamount, hamount, vamount, hamount);
        }

        public Widget SetAlign(Alignment a)
        {
            Alignment = a;
            return this;
        }

        public Vector2 GetScreenPosition()
        {
            Vector2 ownPosition;

            if (Parent != null)
                ownPosition = Parent.GetChildPosition(this);
            else
            {
                ownPosition = new Vector2(0);

                if(Alignment == Alignment.Right)
                    ownPosition.X = (engine.GetSize() - GetSize()).X;
            }

            return ownPosition;
        }

        public bool IsHovered {
            get
            {
                fbRectangle screenRectangle =
                    new fbRectangle(
                        GetScreenPosition() + TopLeftMargin,
                        GetSizeInternal()
                    );
                return
                    screenRectangle
                    .Contains(engine.MousePosition);
            }
        }
    }

    public abstract class ContainerWidget : Widget
    {
        public bool AutoSize;
        protected List<Widget> children;

        protected ContainerWidget(
            fbEngine engine
        ) : base(engine) {
        }

        public List<Widget> GetChildren() { return children; }
        public void AddChild(Widget w)
        {
            w.Parent = this;
            children.Add(w);
        }
        public void RemoveChild(Widget w) {
            w.Parent = null;
            children.Remove(w);
        }

        public abstract Vector2 GetChildPosition(Widget child);
    }

    public class ListBox : ContainerWidget
    {
        public ListBox(
            fbEngine engine
        ) : base(engine) {
            children = new List<Widget>();
        }

        public override Vector2 GetSizeInternal()
        {
            Vector2 size = new Vector2(0);
            foreach (Widget w in children)
            {
                Vector2 childSize = w.GetSize();

                if (childSize.X > size.X)
                    size.X = childSize.X;

                size.Y += childSize.Y;
            }

            return size + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override Vector2 GetChildPosition(Widget child)
        {
            if (!children.Contains(child))
                throw new ArgumentException();

            int childIndex = children.IndexOf(child);

            Vector2 ownPosition = GetScreenPosition();

            Vector2 position = ownPosition + TopLeftMargin + TopLeftPadding;

            for (int i = 0; i < childIndex; i++)
                position.Y += children[i].GetSize().Y;

            if (child.Alignment == Alignment.Right)
                position.X =
                    (ownPosition + GetSize() - child.GetSize()).X
                    - (RightPadding + RightMargin);

            return position;
        }

        public override void Render(Vector2 Position)
        {
            fbRectangle destination = new fbRectangle
            (
                Position + new Vector2(LeftMargin, TopMargin),
                GetSizeInternal()
            );

            new DrawCall(
                engine.GetTexture("blank"),
                destination,
                Depth,
                Color.White
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                destination.Shrink(2),
                Depth,
                Color.Black
            ).Draw(engine);

            Vector2 position = destination.Position;

            foreach (Widget c in children)
            {
                c.Render(GetChildPosition(c));
                position.Y += c.GetSize().Y;
            }
        }
    }

    public class Button : Widget
    {
        //the event that will be generated when the button is pressed?
        private string label;
        private Event reaction;

        public Button(
            fbEngine engine,
            string label, Event reaction
        ) : base(engine) {
            this.label = label;
            this.reaction = reaction;
        }

        public override Vector2 GetSizeInternal()
        {
            return engine.DefaultFont.Measure(label) + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render(Vector2 Position)
        {
            int depth = Parent == null
                ? Depth
                : Parent.Depth - 1;

            fbRectangle destination =
                new fbRectangle(
                    Position + TopLeftMargin,
                    GetSizeInternal()
                );

            new DrawCall(
                engine.GetTexture("blank"),
                destination,
                depth,
                Color.White
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                destination.Shrink(2),
                depth - 1,
                IsHovered
                    ? Color.DarkGray
                    : Color.Black
            ).Draw(engine);

            new TextCall(
                label,
                engine.DefaultFont,
                destination.Position + GetSizeInternal() / 2
                    - engine.DefaultFont.Measure(label) / 2,
                depth - 2
            ).Draw(engine);
        }
    }
}
