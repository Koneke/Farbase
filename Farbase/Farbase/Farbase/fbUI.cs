﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum Alignment { Left, Right, Center }

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

        public abstract bool IsInteractive();

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

        protected int depth;
        public int Depth {
            get { return Parent == null ? -1 : Parent.Depth + depth; }
        }

        protected Widget(
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) {
            this.engine = engine;
            this.ui = ui;
            Disabled = false;
            Visible = true;
            this.depth = depth;
        }

        public abstract Vector2 GetSize();
        public abstract Vector2 GetSizeInternal();
        public abstract void Render();

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

        public Widget Margins(int vamount, int hamount)
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

                else if (Alignment == Alignment.Center)
                    ownPosition.X =
                        (engine.GetSize() / 2f - GetSize() / 2f).X;
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

        protected Color GetColor()
        {
            if (Disabled) return Theme.Content.Disabled;

            return IsHovered && IsInteractive()
                ? Theme.Content.Hover
                : Theme.Content.Color;
        }

        protected Color GetBackgroundColor()
        {
            if (Disabled) return Theme.Background.Disabled;

            return IsHovered && IsInteractive()
                ? Theme.Background.Hover
                : Theme.Background.Color;
        }

        protected Color GetBorderColor()
        {
            if (Disabled) return Theme.Borders.Disabled;

            return IsHovered && IsInteractive()
                ? Theme.Borders.Hover
                : Theme.Borders.Color;
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

        public void DrawBackground()
        {
            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition() + TopLeftMargin,
                    GetSizeInternal()
                    ),
                Depth + 1,
                GetBackgroundColor()
            ).Draw(engine);
        }

        public void DrawBorders()
        {
            //we can probably clean this stuff up a bit...

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition() + TopLeftMargin,
                    new Vector2(GetSizeInternal().X, 1)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition()
                        + TopLeftMargin
                        + new Vector2(0, GetSizeInternal().Y - 1),
                    new Vector2(GetSizeInternal().X, 1)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition() + TopLeftMargin,
                    new Vector2(1, GetSizeInternal().Y)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition()
                        + TopLeftMargin
                        + new Vector2(GetSizeInternal().X - 1, 0),
                    new Vector2(1, GetSizeInternal().Y)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);
        }
    }

    public abstract class ContainerWidget : Widget
    {
        public bool AutoSize;
        protected List<Widget> Children;
        protected int ChildLimit;

        protected ContainerWidget(
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
            ChildLimit = -1;
            Children = new List<Widget>();
        }

        public List<Widget> GetChildren() { return Children; }
        public void AddChild(Widget w)
        {
            if (Children.Count < ChildLimit || ChildLimit == -1)
            {
                w.Parent = this;
                Children.Add(w);
            }
            else throw new Exception();
        }
        public void RemoveChild(Widget w) {
            w.Parent = null;
            Children.Remove(w);
        }

        public abstract Vector2 GetChildPosition(Widget child);
    }

    public class ListBox : ContainerWidget
    {
        public ListBox(
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
        }

        public override bool IsInteractive() { return false; }

        public override Vector2 GetSizeInternal()
        {
            Vector2 size = new Vector2(0);
            foreach (Widget w in Children)
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
            if (!Children.Contains(child))
                throw new ArgumentException();

            int childIndex = Children.IndexOf(child);

            Vector2 position =
                GetScreenPosition() + TopLeftMargin + TopLeftPadding;

            for (int i = 0; i < childIndex; i++)
                if(Children[i].Visible)
                    position.Y += Children[i].GetSize().Y;

            if (child.Alignment == Alignment.Right)
            {
                position.X =
                    (GetScreenPosition() + GetSize() - child.GetSize()).X
                        - (RightPadding + RightMargin);
            }

            else if (child.Alignment == Alignment.Center)
            {
                position.X =
                    (GetScreenPosition() +
                        (GetSize() / 2f - child.GetSize() / 2f)).X;
            }

            return position;
        }

        public override void Render()
        {
            DrawBackground();
            DrawBorders();

            foreach (Widget c in Children)
            {
                if (!c.Visible) continue;
                c.Render();
            }
        }
    }

    public class Button : Widget
    {
        private string label;

        public Button(
            fbEngine engine,
            fbInterface ui,
            string label
        ) : base(engine, ui) {
            this.label = label;
        }

        public override bool IsInteractive() { return true; }

        public override Vector2 GetSizeInternal()
        {
            return engine.DefaultFont.Measure(label) + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render()
        {
            new TextCall(
                label,
                engine.DefaultFont,
                GetScreenPosition() + GetSizeInternal() / 2
                    - engine.DefaultFont.Measure(label) / 2
                    + new Vector2(1, 0),
                Depth,
                GetColor()
            ).Draw(engine);

            DrawBackground();
            DrawBorders();
        }
    }

    public class Label : Widget
    {
        private string content;

        public Label(
            string text,
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) : base(engine, ui, depth) {
            content = text;
        }

        public override bool IsInteractive() { return false; }

        public override Vector2 GetSizeInternal()
        {
            return engine.DefaultFont.Measure(content) + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render()
        {
            new TextCall(
                content,
                engine.DefaultFont,
                GetScreenPosition() + GetSizeInternal() / 2
                    - engine.DefaultFont.Measure(content) / 2
                    + new Vector2(1, -1),
                Depth,
                GetColor()
            ).Draw(engine);
        }
    }

    public class WidgetPair : ContainerWidget
    {
        private int internalPadding;

        public WidgetPair(
            fbEngine engine,
            fbInterface ui,
            Widget a = null,
            Widget b = null,
            int internalPadding = 0
        ) : base(engine, ui) {
            ChildLimit = 2;
            if (a != null) AddChild(a);
            if (b != null) AddChild(b);
            this.internalPadding = internalPadding;
        }

        public override bool IsInteractive() { return false; }

        public override Vector2 GetSizeInternal()
        {
            Vector2 size = new Vector2(0);

            foreach (Widget c in Children)
            {
                Vector2 childSize = c.GetSize();

                size.X += childSize.X;

                if (childSize.Y > size.Y)
                    size.Y = childSize.Y;
            }

            //no padding if only one child
            size.X += internalPadding * (Children.Count - 1);

            return size + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render()
        {
            foreach (Widget c in Children)
                if(c.Visible)
                    c.Render();
        }

        public override Vector2 GetChildPosition(Widget child)
        {
            int childIndex = Children.IndexOf(child);
            Vector2 position = GetScreenPosition()
                + TopLeftMargin + TopLeftPadding;

            for (int i = 0; i < childIndex; i++)
                position.X += Children[i].GetSize().X + internalPadding;

            //center vertically
            position.Y +=
                GetSizeInternal().Y / 2 - child.GetSize().Y / 2
                - TopPadding ;

            return position;
        }
    }

    public class CheckBox : Widget
    {
        public bool Checked;

        public CheckBox(
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) : base(engine, ui, depth) {
        }

        public override bool IsInteractive() { return true; }

        public override Vector2 GetSizeInternal()
        {
            return engine.GetTextureSize("check") + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render()
        {
            DrawBorders();
            DrawBackground();

            if(Checked)
                new DrawCall(
                    engine.GetTexture("check"),
                    new fbRectangle(
                        GetScreenPosition()
                            + TopLeftPadding
                            + TopLeftMargin,
                        engine.GetTextureSize("check")
                    ),
                    Depth,
                    GetColor()
                ).Draw(engine);
        }
    }
}
