﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

//we could probably justify namespacing this to Farbase.UI
//not sure on when exactly to split things into different namespaces?
namespace Farbase
{
    public class fbCondition
    {
        public string Error;
        public Func<bool> Condition;

        public fbCondition(
            string error,
            Func<bool> condition
        ) {
            Error = error;
            Condition = condition;
        }
    }

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

    public class SmartText
    {
        public string Text;

        //we need a reference to the ui so
        //we can access the properties in fbGame
        private fbInterface ui;

        public SmartText(
            string text, fbInterface ui
        ) {
            Text = text;
            this.ui = ui;
        }

        public override string ToString()
        {
            string result = Text;

            //while we still have properties to replace...
            while (result.IndexOf('@') != -1)
            {
                //properties go from @ to first non-alpha, non-dash character
                int start = result.IndexOf('@');
                int end = start + 1;
                bool indexing = false;
                while (
                    end < result.Length &&
                    (
                        result[end].IsLetter() ||
                        result[end] == '-' ||
                        (result[end].IsNumber() && indexing) ||
                        result[end] == ':'
                    )
                ) {
                    if (result[end] == ':') indexing = true;
                    end++;
                }

                //from start to end, removing the @
                string propertyString =
                    result.Substring(
                         1 + start,
                        -1 + end - start
                    );

                string propertyName = propertyString;

                int index = -1;
                if (indexing)
                {
                    index = Int32.Parse(propertyString.Split(':')[1]);
                    propertyName = propertyString.Split(':')[0];
                }

                object propValue;

                if (!indexing)
                    propValue = ui.Game.GetProperty(propertyName).GetValue();
                else
                    propValue = ui.Game.GetProperty(propertyName).At(index);


                if (propValue == null) propValue = "undefined";
                else propValue = propValue.ToString();

                result = result.Replace(propertyString, (string)propValue);

                //remove remaining @ from string
                result = result.Remove(start, 1);
            }

            return result;
        }
    }

    public abstract class Widget
    {
        protected fbInterface ui;
        protected fbEngine engine;

        public ContainerWidget Parent;

        public virtual bool IsVisible()
        {
            if (Parent != null && !Parent.IsVisible()) return false;
            if (visibleCondition == null)
                return true;
            return visibleCondition();
        }

        public bool Disabled
        {
            get
            {
                if (!IsVisible()) return true;
                return !Conditions.All(c => c.Condition());
            }
        }

        protected Func<bool> visibleCondition;

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

        public abstract bool IsInteractive();

        protected int MarginTop, MarginRight, MarginBottom, MarginLeft;
        protected int PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;

        protected Vector2 MarginSize
        {
            get {
                return new Vector2(
                    MarginLeft + MarginRight,
                    MarginTop + MarginBottom
                );
            }
        }
        protected Vector2 PaddingSize
        {
            get {
                return new Vector2(
                    PaddingLeft + PaddingRight,
                    PaddingTop + PaddingBottom
                );
            }
        }

        protected Vector2 TopLeftMargin {
            get { return new Vector2(MarginLeft, MarginTop); }
        }
        protected Vector2 TopLeftPadding {
            get { return new Vector2(PaddingLeft, PaddingTop); }
        }

        protected HAlignment HAlignment;
        protected VAlignment VAlignment;

        private int borderWidth = 1;
        private float backgroundAlpha = 1f;

        private Theme theme;
        public Theme Theme {
            get { return theme ?? ui.DefaultTheme; }
        }

        protected int depth;
        public int Depth {
            get { return Parent == null ? -1 : Parent.Depth + depth; }
        }

        private SmartText tooltip;
        private List<fbCondition> Conditions;

        public string GetTooltip()
        {
            string maintip;

            if (tooltip == null)
                return null;

            maintip = tooltip.ToString();

            foreach (fbCondition c in Conditions)
            {
                if (!c.Condition())
                    maintip += "\n" + c.Error;
            }

            return maintip;
        }

        protected Widget(
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) {
            this.engine = engine;
            this.ui = ui;
            this.depth = depth;
            Conditions = new List<fbCondition>();
        }

        public abstract Vector2 GetSize();
        public abstract Vector2 GetSizeInternal();
        public abstract void Render();

        // === === === === === === === === === ===

        public Widget Margins(int t, int r, int b, int l)
        {
            MarginTop = t;
            MarginRight = r;
            MarginBottom = b;
            MarginLeft = l;
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

        // === === === === === === === === === ===

        public Widget Padding(int amount)
        {
            return Padding(amount, amount, amount, amount);
        }

        public Widget Padding(int hamount, int vamount)
        {
            return Padding(vamount, hamount, vamount, hamount);
        }

        public Widget Padding(int t, int r, int b, int l)
        {
            PaddingTop = t;
            PaddingRight = r;
            PaddingBottom = b;
            PaddingLeft = l;
            return this;
        }

        // === === === === === === === === === ===

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

        // === === === === === === === === === ===

        public Widget SetTheme(Theme cs)
        {
            theme = cs;
            return this;
        }

        public Widget SetVisibleCondition(Func<bool> condition)
        {
            visibleCondition = condition;
            return this;
        }

        public Widget AddCondition(fbCondition condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public Widget SetBorder(int thickness)
        {
            borderWidth = thickness;
            return this;
        }

        public Widget SetBackgroundAlpha(float alpha)
        {
            backgroundAlpha = alpha;
            return this;
        }

        public Widget SetTooltip(string tip)
        {
            tooltip = new SmartText(tip, ui);
            return this;
        }

        // === === === === === === === === === ===

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

            Color c = IsHovered && IsInteractive()
                ? Theme.Background.Hover
                : Theme.Background.Color;

            //because 0 0 0 0 == 255 255 255 255 apparently
            //seems fine as long as any single one field isn't 0
            byte alpha = (byte)Math.Max(1, c.A * backgroundAlpha);

            return new Color(
                c.R,
                c.G,
                c.B,
                alpha
            );
        }

        protected Color GetBorderColor()
        {
            if (Disabled) return Theme.Borders.Disabled;

            return IsHovered && IsInteractive()
                ? Theme.Borders.Hover
                : Theme.Borders.Color;
        }

        // === === === === === === === === === ===

        public VAlignment GetVAlignment() { return VAlignment; }
        public HAlignment GetHAlignment() { return HAlignment; }

        // === === === === === === === === === ===

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

        // === === === === === === === === === ===

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

        public virtual void OnClick() { }

        public virtual Widget GetHovered()
        {
            return IsHovered ? this : null;
        }
    }

    public abstract class ContainerWidget : Widget
    {
        public bool AutoSize;
        protected List<Widget> Children;

        protected ContainerWidget(
            fbEngine engine,
            fbInterface ui
            ) : base(engine, ui)
        {
            Children = new List<Widget>();
        }

        public List<Widget> GetChildren()
        {
            return Children;
        }

        public ContainerWidget AddChild(Widget w)
        {
            w.Parent = this;
            Children.Add(w);

            return this;
        }

        public ContainerWidget RemoveChild(Widget w)
        {
            w.Parent = null;
            Children.Remove(w);

            return this;
        }

        public abstract Vector2 GetChildPosition(Widget child);

        public override void OnClick()
        {
            foreach (Widget c in Children)
                if (c.IsVisible() && c.IsHovered)
                    c.OnClick();
        }

        public override Widget GetHovered()
        {
            if (!IsHovered) return null;

            foreach (Widget c in Children)
                if (c.IsVisible() && c.IsHovered)
                    return c.GetHovered();
            return this;
        }
    }

    public class ListBox : ContainerWidget
    {
        private int internalPadding;

        public ListBox(
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
            foreach (Widget w in Children)
            {
                if (!w.IsVisible()) continue;

                Vector2 childSize = w.GetSize();

                if (childSize.X > size.X)
                    size.X = childSize.X;

                size.Y += childSize.Y;
            }

            if(Children.Count > 0)
                size.Y += internalPadding * (Children.Count - 1);

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
                if (Children[i].IsVisible())
                {
                    position.Y += Children[i].GetSize().Y;
                    position.Y += internalPadding;
                }

            if (child.GetHAlignment() == HAlignment.Right)
            {
                position.X =
                    (GetScreenPosition() + GetSize() - child.GetSize()).X
                        - (PaddingRight + MarginRight);
            }

            else if (child.GetHAlignment() == HAlignment.Center)
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
                if (!c.IsVisible()) continue;
                c.Render();
            }
        }
    }

    public class Button : Widget, IInputSubscriber
    {
        private SmartText label;
        private Action reaction;
        private InputSubscriber subscriber;

        public Button(
            string label,
            Action reaction,
            fbEngine engine,
            fbInterface ui
        ) : base(engine, ui) {
            this.label = new SmartText(label, ui);
            this.reaction = reaction;

            subscriber = new InputSubscriber(this);
            subscriber.Register();
        }

        public Widget SetAction(Action action)
        {
            reaction = action;
            return this;
        }

        public override bool IsInteractive() { return true; }

        public override Vector2 GetSizeInternal()
        {
            return engine.DefaultFont.Measure(label.ToString())
                + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render()
        {
            new TextCall(
                label.ToString(),
                engine.DefaultFont,
                GetScreenPosition() + GetSizeInternal() / 2
                    - engine.DefaultFont.Measure(label.ToString()) / 2
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
            if (!Disabled) reaction();
        }

        //conciously ignoring the actual given keybind back,
        //since we only subscribe to one anyways.
        public void ReceiveInput(string s) { OnClick(); }
        public void Subscribe(string s) { subscriber.Subscribe(s); }
    }

    public class Label : Widget
    {
        private SmartText content;

        public Label(
            string text,
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) : base(engine, ui, depth) {
            content = new SmartText(text, ui);
        }

        public override bool IsInteractive() { return false; }

        public override Vector2 GetSizeInternal()
        {
            return engine.DefaultFont.Measure(content.ToString())
                + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
        }

        public override void Render()
        {
            new TextCall(
                content.ToString(),
                engine.DefaultFont,
                GetScreenPosition() + GetSizeInternal() / 2
                    - engine.DefaultFont.Measure(content.ToString()) / 2
                    + TopLeftMargin,
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
                if (!c.IsVisible()) continue;

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
                if(c.IsVisible())
                    c.Render();
        }

        public override Vector2 GetChildPosition(Widget child)
        {
            int childIndex = Children.IndexOf(child);
            Vector2 position = GetScreenPosition()
                + TopLeftMargin + TopLeftPadding;

            for (int i = 0; i < childIndex; i++)
                if(Children[i].IsVisible())
                    position.X += Children[i].GetSize().X + internalPadding;

            //center vertically
            position.Y +=
                GetSizeInternal().Y / 2 - child.GetSize().Y / 2
                - PaddingTop ;

            return position;
        }
    }

    public class CheckBox : Widget, IInputSubscriber
    {
        private InputSubscriber subscriber;
        public bool Checked;

        public CheckBox(
            fbEngine engine,
            fbInterface ui,
            int depth = -1
        ) : base(engine, ui, depth) {
            subscriber = new InputSubscriber(this);
            subscriber.Register();
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
            if(!Disabled) Checked = !Checked;
        }

        public void ReceiveInput(string s) { OnClick(); }
        public void Subscribe(string s) { subscriber.Subscribe(s); }
    }

    public class Image : Widget
    {
        private string texture;
        private float scale;

        public Image(
            string texture,
            fbEngine engine,
            fbInterface ui,
            float scale = 1f
        ) : base(engine, ui) {
            this.texture = texture;
            this.scale = scale;
        }

        public override bool IsInteractive()
        {
            return false;
        }

        public override Vector2 GetSizeInternal()
        {
            return engine.GetTextureSize(texture) * scale + PaddingSize;
        }

        public override Vector2 GetSize()
        {
            return GetSizeInternal() + MarginSize;
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
