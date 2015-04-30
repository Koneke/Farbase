using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Farbase
{
    public interface ISelection
    {
        Vector2i GetSelection();
    }

    public class TileSelection : ISelection
    {
        private Tile selected;

        public TileSelection(Tile t)
        {
            selected = t;
        }

        public Vector2i GetSelection()
        {
            return new Vector2i(
                selected.Position.X,
                selected.Position.Y
            );
        }
    }

    public class fbInterface
    {
        private fbGame game;
        private fbEngine engine;

        private const int tileSize = 16;

        private ISelection selection;
        private Tile SelectedTile
        {
            get
            {
                if (selection == null) return null;
                return fbGame.World.Map.At(selection.GetSelection());
            }
        }
        private Unit SelectedUnit
        {
            get
            {
                if (selection == null) return null;
                return SelectedTile.Unit;
            }
        }
        private Station SelectedStation
        {
            get
            {
                if (selection == null) return null;
                return SelectedTile.Station;
            }
        }

        private fbCamera Camera;

        private List<Widget> widgets;
        public Theme DefaultTheme;

        private Widget BuildCard;

        private string tooltip;

        public fbInterface(
            fbGame game,
            fbEngine engine
        ) {
            this.game = game;
            this.engine = engine;
            Camera = new fbCamera(engine);
            widgets = new List<Widget>();

            DefaultTheme = new Theme(
                new ColorSet(
                    Color.White,
                    Color.White,
                    Color.Gray
                ),
                new ColorSet(
                    new Color(0, 0, 0, 0.6f),
                    Color.DarkGray * 0.9f,
                    Color.DarkGray * 0.7f
                ),
                new ColorSet(
                    Color.White * 0.8f,
                    Color.White * 0.8f,
                    Color.White * 0.8f
                )
            );

            SetupUI();
            //SetupTestUI();
        }

        public void SetupUI()
        {
            BuildCard =
                new SideBySideWidgets(engine, this, 5)
                    .Margins(40)
                    .Padding(5)
                    .SetAlign(HAlignment.Right, VAlignment.Bottom);

            foreach (UnitType ut in UnitType.UnitTypes)
            {
                UnitType unitType = ut;
                ((SideBySideWidgets)BuildCard)
                    .AddChild(
                        new TextureButton(
                            ut.Name,
                            () => TryBuildUnit(unitType.Name),
                            engine,
                            this,
                            2f
                        )
                        .Padding(2)
                        .SetCondition(
                            () => game.LocalPlayer.Money > unitType.Cost
                        )
                        .SetTooltip("Build " + ut.Name)
                    );
            }

            widgets.Add(BuildCard);
        }

        public void SetupTestUI()
        {
            ListBox b =
                (ListBox)
                new ListBox(engine, this)
                    .Margins(40)
                    .Padding(10)
                    .SetAlign(HAlignment.Right)
            ;

            b.AddChild(
                new Label(" == test\nlabel == ", engine, this)
                    .Margins(2)
                    .SetAlign(HAlignment.Center)
            );

            b.AddChild(
                new Button("alignment!", null, engine, this)
                    .Margins(2)
                    .Padding(5)
                    .SetAlign(HAlignment.Right)
                    .SetTooltip(
                        "I'M A VERY\nLONG AND COMPLEX\nTOOLTIP"
                    )
            );

            b.AddChild(
                new Button("greyed out", null, engine, this)
                    .Margins(2)
                    .Padding(5)
                    .SetCondition(() => false)
            );

            b.AddChild(
                new Button("lots and lots of text", null, engine, this)
                    .Margins(2)
                    .Padding(5)
            );

            b.AddChild(
                new SideBySideWidgets(engine, this, 7)
                    .AddChild(new Button("foo", null, engine, this).Padding(5))
                    .AddChild(new Button("bar", null, engine, this).Padding(5))
                    .Margins(2)
                    .SetAlign(HAlignment.Center)
                    .SetBorder(0)
            );

            b.AddChild(
                new SideBySideWidgets(engine, this, 7)
                    .AddChild(new CheckBox(engine, this).Padding(2))
                    .AddChild(new Label("test", engine, this))
                    .Margins(2)
                    .SetAlign(HAlignment.Left)
                    .SetBorder(0)
            );

            b.AddChild(
                new SideBySideWidgets(engine, this, 7)
                    .AddChild(new CheckBox(engine, this).Padding(2))
                    .AddChild(new Label("some other option", engine, this))
                    .Margins(2)
                    .SetAlign(HAlignment.Left)
                    .SetBorder(0)
            );

            widgets.Add(b);

            ListBox foo =
                (ListBox)
                new ListBox(engine, this)
                    .SetAlign(HAlignment.Right, VAlignment.Bottom)
                    .Margins(40)
                    .Padding(10)
            ;

            foo.AddChild(
                new Label(" - Another label - ", engine, this)
                    .SetAlign(HAlignment.Center)
            );

            foo.AddChild(
                new Button(
                    "I'm aligned to the bottom!",
                    () => {
                        game.Log.Add("foo!");
                    },
                    engine,
                    this
                )
                    .Margins(2)
                    .Padding(5)
                    .SetAlign(HAlignment.Right, VAlignment.Bottom)
            );

            widgets.Add(foo);
        }

        public void Select(Vector2i position)
        {
            Tile t = fbGame.World.Map.At(position);
            selection = new TileSelection(t);
        }

        public void Draw()
        {
            DrawBackground();

            if (fbGame.World == null) return;

            DrawMap();
            DrawUI();

            DrawWidgets();
        }

        private void DrawBackground()
        {
            Vector2 position =
                -engine.GetTextureSize("background") / 2f +
                engine.GetSize() / 2f;
            position -= Camera.Camera.Position / 10f;

            engine.Draw(
                engine.GetTexture("background"),
                new fbRectangle(position, new Vector2(-1)),
                new Color(0.3f, 0.3f, 0.3f),
                1000
            );
        }

        private void DrawGrid()
        {
            for (int x = 0; x < fbGame.World.Map.Width; x++)
            for (int y = 0; y < fbGame.World.Map.Height; y++)
            {
                engine.Draw(
                    engine.GetTexture("grid"),
                    Camera.WorldToScreen(
                        new fbRectangle(
                            new Vector2(x, y) * tileSize,
                            tileSize,
                            tileSize
                        )
                    )
                );
            }
        }

        private void DrawTile(int x, int y)
        {
            Tile t = fbGame.World.Map.At(x, y);
            fbRectangle destination =
                Camera.WorldToScreen(
                    new fbRectangle(
                        new Vector2(x, y) * tileSize,
                        tileSize,
                        tileSize
                    )
                );

            if (t.Station != null)
            {
                engine.Draw(
                    t.Unit == null
                    ? engine.GetTexture("station")
                    : engine.GetTexture("station-bg"),
                    destination
                );
            }

            if (t.Planet != null)
            {
                engine.Draw(
                    t.Unit == null
                    ? engine.GetTexture("planet")
                    : engine.GetTexture("planet-bg"),
                    destination
                );
            }

            if (t.Unit != null)
                DrawUnit(t.Unit);

            if(
                (SelectedUnit != null && t.Unit == SelectedUnit) ||
                (SelectedTile == t && t.Station != null)
            )
                engine.Draw(
                    engine.GetTexture("selection"),
                    destination
                );
        }

        private void DrawUnit(Unit u)
        {
            fbRectangle destination =
                Camera.WorldToScreen(
                    new fbRectangle(
                        u.Position * tileSize,
                        new Vector2(tileSize)
                    )
                );

            engine.Draw(
                u.UnitType.Texture,
                destination,
                fbGame.World.Players[u.Owner].Color
            );

            for (int i = 0; i < u.Moves; i++)
            {
                Vector2 dingySize = engine.GetTextureSize("move-dingy");

                engine.Draw(
                    engine.GetTexture("move-dingy"),
                    Camera.WorldToScreen(
                        new fbRectangle(
                            u.Position * tileSize
                                + new Vector2(dingySize.X * i, 0),
                            dingySize
                        )
                    )
                );
            }

            for (int i = 0; i < u.Strength; i++)
            {
                Vector2 dingySize = engine.GetTextureSize("strength-dingy");

                engine.Draw(
                    engine.GetTexture("strength-dingy"),
                    Camera.WorldToScreen(
                        new fbRectangle(
                            u.Position * tileSize + new Vector2(0, tileSize)
                                - new Vector2(0, dingySize.Y * (i + 1)),
                            dingySize
                        )
                    )
                );
            }
        }

        private void DrawMap()
        {
            DrawGrid();
            for (int x = 0; x < fbGame.World.Map.Width; x++)
            for (int y = 0; y < fbGame.World.Map.Height; y++)
                DrawTile(x, y);
        }

        private void DrawUI()
        {
            if (tooltip != null)
            {
                Vector2 tooltipSize = engine.DefaultFont.Measure(tooltip);

                Vector2 tooltipPosition =
                    new Vector2(
                        engine.MousePosition.X
                            .Clamp(
                                20,
                                engine.GetSize().X - (tooltipSize.X + 20)
                            ),
                        engine.MousePosition.Y
                            - (tooltipSize.Y + 5)
                    );

                new DrawCall(
                    engine.GetTexture("blank"),
                    new fbRectangle(
                        tooltipPosition,
                        tooltipSize
                    ).Grow(4),
                    -999,
                    DefaultTheme.Background.Color
                ).Draw(engine);

                new TextCall(
                    tooltip,
                    engine.DefaultFont,
                    tooltipPosition,
                    -1000
                ).Draw(engine);
            }

            new TextCall(
                string.Format(
                    "Hi, I am {0}<{1}>",
                    fbGame.World.Players[game.We].Name,
                    game.We
                ),
                engine.DefaultFont,
                new Vector2(10)
            ).Draw(engine);

            if (fbGame.World.PlayerIDs.Count > 0)
            {
                Player current = fbGame.World.Players
                    [fbGame.World.PlayerIDs[fbGame.World.CurrentPlayerIndex]];

                new TextCall(
                    string.Format(
                        "Current player: {0}<{1}>",
                        current.Name,
                        current.ID
                    ),
                    engine.DefaultFont,
                    new Vector2(10, 20)
                ).Draw(engine);
            }

            List<string> logTail;
            lock (game.Log)
            {
                logTail = game.Log
                    .Skip(Math.Max(0, game.Log.Count - 10))
                    .ToList();
            }
            logTail.Reverse();

            Vector2 position = new Vector2(10, engine.GetSize().Y - 10);
            foreach (string message in logTail)
            {
                position -= new Vector2(0, engine.DefaultFont.CharSize.Y + 1);

                new TextCall(
                    message,
                    engine.DefaultFont,
                    position
                ).Draw(engine);
            }

            position = new Vector2(engine.GetSize().X - 10, 0);
            foreach (int id in fbGame.World.PlayerIDs)
            {
                position += new Vector2(0, engine.DefaultFont.CharSize.Y + 1);
                Player p = fbGame.World.Players[id];

                new TextCall(
                    p.Name + ": "+ p.Money + "$",
                    engine.DefaultFont,
                    position
                ).RightAlign().Draw(engine);
            }
        }

        private void DrawWidgets()
        {
            foreach (Widget w in widgets)
                if (w.Visible)
                    w.Render();
        }

        public void Update()
        {
            if (engine.KeyPressed(Keys.Escape)) engine.Exit();
            Camera.Update();

            //no (important) interaction if we're waiting for data.
            if (!engine.NetClient.Ready) return;

            UpdateUI();

            Input();
            HandleEvents();
        }

        public void UpdateUI()
        {
            BuildCard.Visible = SelectedStation != null;

            tooltip = null;

            foreach(Widget w in widgets)
                if (w.IsHovered)
                    tooltip = w.GetHovered().Tooltip;

            //no tooltip from widgets
            if (tooltip == null)
            {
                Vector2? hoveredSquare = ScreenToGrid(engine.MousePosition);
                if (hoveredSquare.HasValue)
                {
                    string unitTooltip = null;
                    string stationTooltip = null;
                    string planetTooltip = null;

                    Tile t = fbGame.World.Map.At(hoveredSquare.Value);
                    if (t.Unit != null)
                        unitTooltip = string.Format(
                            "{0} - {1}\n{2}/{3} moves\n{4}/{5} strength",
                            t.Unit.UnitType.Name,
                            fbGame.World.Players[t.Unit.Owner].Name,
                            t.Unit.Moves,
                            t.Unit.UnitType.Moves,
                            t.Unit.Strength,
                            t.Unit.UnitType.Strength
                        );
                    if (t.Station != null)
                        stationTooltip = "station";
                    if (t.Planet != null)
                        planetTooltip = "planet";

                    string[] tooltips =
                        {
                            unitTooltip,
                            stationTooltip,
                            planetTooltip
                        };

                    if(tooltips.Any(tt => tt != null))
                        tooltip = 
                            string.Join(
                                "\n-\n",
                                tooltips.Where(tt => tt != null)
                            );
                }
            }
        }

        public void HandleEvents()
        {
            //peek, and then let it fall through to the game itself which
            //handles the renaming.
            //we're doing it like this because the game should NOT need
            //a reference to the interface

            foreach (Event e in engine.Peek(NameEvent.EventType))
            {
                NameEvent ne = (NameEvent)e;
                game.Log.Add(
                    string.Format(
                        "{0}<{2}> is now known as {1}<{2}>.",
                        fbGame.World.Players[ne.ID].Name,
                        ne.Name,
                        ne.ID
                    )
                );
            }

            foreach (Event e in engine.Peek(UnitMoveEvent.EventType))
            {
                if (SelectedUnit == null) break;

                //if the moving unit is the one we're having selected,
                //stick to it.

                UnitMoveEvent ume = (UnitMoveEvent)e;
                if (SelectedUnit.ID == ume.ID)
                    selection = new TileSelection(
                        fbGame.World.Map.At(ume.x, ume.y)
                    );
            }
        }

        public void Input()
        {
            if (engine.KeyPressed(Keys.Enter))
            {
                if (game.OurTurn)
                {
                    engine.NetClient.Send(new PassMessage());
                }
                else
                    game.Log.Add("Not your turn!");
            }

            if (engine.KeyPressed(Keys.Space))
            {
                engine.NetClient.ShouldDie = true;
            }

            if (engine.KeyPressed(Keys.G))
            {
                List<string> names =
                    new List<string>
                    {
                        "Captain Zorblax",
                        "Commander Kneckers"
                    };

                List<Color> colors =
                    new List<Color>
                    {
                        Color.Green,
                        Color.CornflowerBlue
                    };

                engine.NetClient.Send(
                    new NameMessage(game.We, names[game.We], colors[game.We])
                );
            }

            if (engine.KeyPressed(Keys.H))
            {
                if (game.OurTurn)
                {
                    engine.NetClient.Send(
                        new DevCommandMessage(0)
                    );
                }
            }

            if (
                game.OurTurn &&
                SelectedUnit != null &&
                SelectedUnit.Owner == fbGame.World.CurrentPlayer.ID
            ) {
                Vector2 moveOrder = Vector2.Zero;

                if (engine.KeyPressed(Keys.NumPad2))
                    moveOrder = new Vector2(0, 1);
                if (engine.KeyPressed(Keys.NumPad4))
                    moveOrder = new Vector2(-1, 0);
                if (engine.KeyPressed(Keys.NumPad8))
                    moveOrder = new Vector2(0, -1);
                if (engine.KeyPressed(Keys.NumPad6))
                    moveOrder = new Vector2(1, 0);

                if (engine.KeyPressed(Keys.NumPad1))
                    moveOrder = new Vector2(-1, 1);
                if (engine.KeyPressed(Keys.NumPad7))
                    moveOrder = new Vector2(-1, -1);
                if (engine.KeyPressed(Keys.NumPad9))
                    moveOrder = new Vector2(1, -1);
                if (engine.KeyPressed(Keys.NumPad3))
                    moveOrder = new Vector2(1, 1);

                if (moveOrder != Vector2.Zero && SelectedUnit.Moves > 0)
                {
                    Unit u = SelectedUnit;

                    if(u.CanMoveTo(u.Position + moveOrder))
                    {
                        int x = (int)(u.Position + moveOrder).X;
                        int y = (int)(u.Position + moveOrder).Y;

                        engine.NetClient.Send(new MoveUnitMessage(u.ID, x, y));

                        u.Moves -= 1;
                        //u.MoveTo(x, y);
                        engine.QueueEvent(new UnitMoveEvent(u.ID, x, y));
                    }
                    else if (u.CanAttack(u.Position + moveOrder))
                    {
                        Vector2 targettile = u.Position + moveOrder;
                        Unit target = fbGame.World.Map.At(
                            (int)targettile.X,
                            (int)targettile.Y
                        ).Unit;

                        engine.NetClient.Send(
                            new AttackMessage(u.ID, target.ID)
                        );
                    }
                }
            }

            /*if (SelectedUnit != null)
            {
                if (engine.KeyPressed(Keys.OemPeriod))
                {
                    int index = CurrentPlayer.Units.IndexOf(SelectedUnit);
                    index = (index + 1) % CurrentPlayer.Units.Count;
                    SelectedUnit = CurrentPlayer.Units[index];
                }

                if (engine.KeyPressed(Keys.OemComma))
                {
                    int index = CurrentPlayer.Units.IndexOf(SelectedUnit);
                    index = (index + CurrentPlayer.Units.Count - 1)
                        % CurrentPlayer.Units.Count;
                    SelectedUnit = CurrentPlayer.Units[index];
                }

                if (engine.KeyPressed(Keys.A))
                {
                    SelectedUnit.MoveTo(
                        SelectedUnit.Position +
                        SelectedUnit.StepTowards(new Vector2(0))
                    );
                    SelectedUnit.Moves -= 1;
                }
            }*/

            /*if (engine.KeyPressed(Keys.W))
                if (
                    SelectedStation != null &&
                    SelectedUnit == null &&
                    CurrentPlayer.Money >= 25
                ) {
                    SpawnUnit(
                        UnitType.GetType("worker"),
                        CurrentPlayer,
                        Selection.Position
                    );
                    CurrentPlayer.Money -= 25;
                }*/

            if (engine.KeyPressed(Keys.B))
                if (
                    SelectedTile.Station != null &&
                    SelectedTile.Unit == null &&
                    fbGame.World.Players[game.We].Money >=
                        UnitType.GetType("scout").Cost
                ) {
                    engine.NetClient.Send(
                        new BuildUnitMessage(
                            "scout",
                            SelectedTile.Position.X,
                            SelectedTile.Position.Y
                        )
                    );
                }

            //handled more gracefully in the future, hopefully...
            bool passClickToWorld = true;

            if (engine.ButtonPressed(0))
            {
                foreach(Widget w in widgets)
                    if (w.IsHovered)
                    {
                        w.OnClick();
                        passClickToWorld = false;
                    }
            }

            if (engine.ButtonPressed(0) && passClickToWorld)
            {
                if (engine.Active && engine.MouseInside)
                {
                    Vector2? square = ScreenToGrid(engine.MousePosition);

                    //null means we clicked on the screen, but outside the grid.
                    if (square != null)
                    {
                        Select(
                            new Vector2i(
                                (int)square.Value.X,
                                (int)square.Value.Y
                                )
                            );
                    }
                }
            }
        }

        private Vector2? ScreenToGrid(Vector2 position)
        {
            Vector2 worldPoint = Camera.ScreenToWorld(position);
            Vector2 square =
                new Vector2(
                    worldPoint.X - (worldPoint.X % tileSize),
                    worldPoint.Y - (worldPoint.Y % tileSize)
                ) / tileSize;

            if(
                square.X >= 0 && square.X < fbGame.World.Map.Width &&
                square.Y >= 0 && square.Y < fbGame.World.Map.Height
            )
                return square;

            return null;
        }

        private void TryBuildUnit(string type)
        {
            if (
                SelectedTile.Station != null &&
                SelectedTile.Unit == null &&
                fbGame.World.Players[game.We].Money >=
                    UnitType.GetType(type).Cost
            ) {
                engine.NetClient.Send(
                    new BuildUnitMessage(
                        type,
                        SelectedTile.Position.X,
                        SelectedTile.Position.Y
                    )
                );
            }
        }
    }

    public class fbCamera
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

            Vector2 bias = position / engine.GetSize();
            Camera.Position -= deltaSize * bias;
            Camera.Size += deltaSize;
        }

        public void Update()
        {
            if (engine.KeyPressed(Keys.OemPlus))
                ZoomAt(engine.GetSize() / 2f, 100f);

            if (engine.KeyPressed(Keys.OemMinus))
                ZoomAt(engine.GetSize() / 2f, -100f);

            Vector2 keyboardScroll = Vector2.Zero;
            if (engine.KeyDown(Keys.Right)) keyboardScroll.X += 1;
            if (engine.KeyDown(Keys.Left)) keyboardScroll.X -= 1;
            if (engine.KeyDown(Keys.Up)) keyboardScroll.Y -= 1;
            if (engine.KeyDown(Keys.Down)) keyboardScroll.Y += 1;

            Camera.Position +=
                keyboardScroll * keyboardScrollSpeed * cameraScaling;

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

        private Rectangle WorldToScreen(Rectangle rectangle)
        {
            rectangle.X -= (int)Camera.Position.X;
            rectangle.Y -= (int)Camera.Position.Y;

            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            rectangle.X = (int)(rectangle.X * scaleFactor.X);
            rectangle.Y = (int)(rectangle.Y * scaleFactor.Y);
            rectangle.Width = (int)(rectangle.Width * scaleFactor.X);
            rectangle.Height = (int)(rectangle.Height * scaleFactor.Y);

            return rectangle;
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
    }
}
