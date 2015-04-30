using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum Alignment { Left, Right }

    public class ColorSet
    {
        public Color Color, Hover, Disabled;

        public ColorSet(Color c, Color h, Color d)
        {
            Color = c;
            Hover = h;
            Disabled = d;
        }
    }

    public class Theme
    {
        public ColorSet Content;
        public ColorSet Background;
        public ColorSet Borders;

        public Theme(
            ColorSet cs,
            ColorSet bgcs,
            ColorSet bcs
        ) {
            Content = cs;
            Background = bgcs;
            Borders = bcs;
        }
    }

    public abstract class Widget
    {
        protected fbInterface ui;
        protected fbEngine engine;

        public ContainerWidget Parent;

        public bool Visible;
        public bool Disabled; //for interactive widgets

        public int TopMargin, RightMargin, BottomMargin, LeftMargin;
        public int TopPadding, RightPadding, BottomPadding, LeftPadding;

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

        public Alignment Alignment;

        private Theme theme;
        public Theme Theme {
            get { return theme ?? ui.DefaultTheme; }
        }

        public int Depth;

        protected Widget(
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) {
            this.engine = engine;
            this.ui = ui;
            Visible = true;
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

        public Widget SetTheme(Theme cs)
        {
            theme = cs;
            return this;
        }

        protected Color GetColor(bool ignoreHover = false)
        {
            if (Disabled) return Theme.Content.Disabled;

            return IsHovered && !ignoreHover
                ? theme.Content.Hover
                : theme.Content.Color;
        }

        protected Color GetBackgroundColor(bool ignoreHover = false)
        {
            if (Disabled) return Theme.Background.Disabled;

            return IsHovered && !ignoreHover
                ? theme.Background.Hover
                : theme.Background.Color;
        }

        protected Color GetBorderColor(bool ignoreHover = false)
        {
            if (Disabled) return Theme.Borders.Disabled;

            return IsHovered && !ignoreHover
                ? theme.Borders.Hover
                : theme.Borders.Color;
        }

        public Widget SetVisible(bool visible)
        {
            Visible = visible;
            return this;
        }

        public Widget SetDisabled(bool disabled)
        {
            Disabled = disabled;
            return this;
        }
    }

    public abstract class ContainerWidget : Widget
    {
        public bool AutoSize;
        protected List<Widget> children;

        protected ContainerWidget(
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
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
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
            children = new List<Widget>();
        }

        public override Vector2 GetSizeInternal()
        {
            Vector2 size = new Vector2(0);
            foreach (Widget w in children)
            {
                if (!w.Visible) continue;

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
            {
                if(children[i].Visible)
                    position.Y += children[i].GetSize().Y;
            }

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
                GetBorderColor(true)
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                destination.Shrink(2),
                Depth,
                GetBackgroundColor(true)
            ).Draw(engine);

            Vector2 position = destination.Position;

            foreach (Widget c in children)
            {
                if (!c.Visible) continue;
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
            fbInterface ui,
            string label,
            Event reaction
        ) : base(engine, ui) {
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
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                destination.Shrink(2),
                depth - 1,
                GetBackgroundColor()
            ).Draw(engine);

            new TextCall(
                label,
                engine.DefaultFont,
                destination.Position + GetSizeInternal() / 2
                    - engine.DefaultFont.Measure(label) / 2,
                depth - 2,
                GetColor()
            ).Draw(engine);
        }
    }
}
