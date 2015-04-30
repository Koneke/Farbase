using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum HAlignment { Left, Right, Center }
    public enum VAlignment { Top, Bottom, Center }

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

        public bool Disabled
        {
            get {
                return condition != null && !condition();
            }
        }

        public Func<bool> condition; 

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

        public HAlignment HAlignment;
        public VAlignment VAlignment;

        private int borderWidth;
        //private float backgroundAlpha

        private Theme theme;
        public Theme Theme {
            get { return theme ?? ui.DefaultTheme; }
        }

        protected int depth;
        public int Depth {
            get { return Parent == null ? -1 : Parent.Depth + depth; }
        }

        public string Tooltip;

        protected Widget(
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) {
            this.engine = engine;
            this.ui = ui;
            Visible = true;
            this.depth = depth;
            borderWidth = 1;
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

        public Widget SetAlign(HAlignment a)
        {
            HAlignment = a;
            return this;
        }

        public Widget SetAlign(HAlignment ha, VAlignment va)
        {
            HAlignment = ha;
            VAlignment = va;
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

                if(HAlignment == HAlignment.Right)
                    ownPosition.X = (engine.GetSize() - GetSize()).X;
                else if (HAlignment == HAlignment.Center)
                    ownPosition.X =
                        (engine.GetSize() / 2f - GetSize() / 2f).X;

                if(VAlignment == VAlignment.Bottom)
                    ownPosition.Y = (engine.GetSize() - GetSize()).Y;
                else if (VAlignment == VAlignment.Center)
                    ownPosition.Y =
                        (engine.GetSize() / 2f - GetSize() / 2f).Y;
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

        public Widget SetCondition(Func<bool> enabledCondition)
        {
            condition = enabledCondition;
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
                    new Vector2(GetSizeInternal().X, borderWidth)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition()
                        + TopLeftMargin
                        + new Vector2(0, GetSizeInternal().Y - borderWidth),
                    new Vector2(GetSizeInternal().X, borderWidth)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition() + TopLeftMargin,
                    new Vector2(borderWidth, GetSizeInternal().Y)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);

            new DrawCall(
                engine.GetTexture("blank"),
                new fbRectangle(
                    GetScreenPosition()
                        + TopLeftMargin
                        + new Vector2(GetSizeInternal().X - borderWidth, 0),
                    new Vector2(borderWidth, GetSizeInternal().Y)
                ),
                Depth,
                GetBorderColor()
            ).Draw(engine);
        }

        public Widget SetBorder(int thickness)
        {
            borderWidth = thickness;
            return this;
        }

        public Widget SetTooltip(string tooltip)
        {
            Tooltip = tooltip;
            return this;
        }

        public virtual void OnClick() { }
    }

    public abstract class ContainerWidget : Widget
    {
        public bool AutoSize;
        protected List<Widget> Children;

        protected ContainerWidget(
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
            Children = new List<Widget>();
        }

        public List<Widget> GetChildren() { return Children; }
        public ContainerWidget AddChild(Widget w)
        {
            w.Parent = this;
            Children.Add(w);

            return this;
        }
        public ContainerWidget RemoveChild(Widget w) {
            w.Parent = null;
            Children.Remove(w);

            return this;
        }

        public abstract Vector2 GetChildPosition(Widget child);

        public override void OnClick()
        {
            foreach(Widget c in Children)
                if (c.IsHovered) c.OnClick();
        }

        public Widget GetHovered()
        {
            if (!IsHovered) return null;

            foreach(Widget c in Children)
                if (c.IsHovered)
                {
                    if (c is ContainerWidget)
                        return ((ContainerWidget)c).GetHovered();
                    return c;
                }
            return this;
        }
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

            if (child.HAlignment == HAlignment.Right)
            {
                position.X =
                    (GetScreenPosition() + GetSize() - child.GetSize()).X
                        - (RightPadding + RightMargin);
            }

            else if (child.HAlignment == HAlignment.Center)
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
        private Action reaction;

        public Button(
            string label,
            Action reaction,
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
            this.label = label;
            this.reaction = reaction;
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
                    + TopLeftMargin,
                Depth,
                GetColor()
            ).Draw(engine);

            DrawBackground();
            DrawBorders();
        }

        public override void OnClick()
        {
            if (reaction == null) return;
            reaction();
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

        public override void OnClick() { }
    }

    public class SideBySideWidgets : ContainerWidget
    {
        private int internalPadding;

        public SideBySideWidgets(
            fbEngine engine,
            fbInterface ui,
            int internalPadding = 0
        ) : base(engine, ui) {
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
            DrawBorders();
            DrawBackground();

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

        public override void OnClick()
        {
            Checked = !Checked;
        }
    }

    public class TextureButton : Button
    {
        private string texture;
        private float scale;

        public TextureButton(
            string texture,
            Action reaction,
            fbEngine engine,
            fbInterface ui,
            float scale = 1f
        ) : base("", reaction, engine, ui) {
            this.texture = texture;
            this.scale = scale;
        }

        public override Vector2 GetSizeInternal()
        {
            return engine.GetTextureSize(texture) * scale + PaddingSize;
        }

        public override void Render()
        {
            DrawBackground();
            DrawBorders();

            new DrawCall(
                engine.GetTexture(texture),
                new fbRectangle(
                    GetScreenPosition() + TopLeftMargin + TopLeftPadding,
                    engine.GetTextureSize(texture) * scale
                ),
                Depth,
                Color.White
            ).Draw(engine);
        }
    }
}
