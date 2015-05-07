using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Farbase
{
    public class fbInterface
    {
        public fbGame Game;
        public fbEngine Engine;

        private const int tileSize = 16;

        public ISelection Selection;
        private Tile SelectedTile
        {
            get
            {
                if (Selection == null) return null;
                return Game.World.Map.At(Selection.GetSelection());
            }
        }
        public Unit SelectedUnit
        {
            get
            {
                if (Selection == null) return null;
                //works even if the selection is a unitselection
                //since the selectedtile is then the one with the unit on
                return SelectedTile.Unit;
            }
        }
        private Station SelectedStation
        {
            get
            {
                if (Selection == null) return null;
                return SelectedTile.Station;
            }
        }

        private fbCamera Camera;

        private List<Widget> widgets;
        public Theme DefaultTheme;

        private ListBox CardBox;
        private SideBySideWidgets BuildCard;
        private SideBySideWidgets CommandCard;

        private string tooltip;

        public fbInterface(
            fbGame game,
            fbEngine engine
        ) {
            Game = game;
            Engine = engine;
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

            InterfaceEventHandler ieh = new InterfaceEventHandler(game, this);
            engine.Subscribe(ieh, EventType.NameEvent);
            engine.Subscribe(ieh, EventType.BuildUnitEvent);
        }

        public void SetupUI()
        {
            SideBySideWidgets portrait =
                (SideBySideWidgets)
                    new SideBySideWidgets(Engine, this, 5)
                    .AddChild(
                        new Image("empty-portrait", Engine, this)
                            //automatically check the current player property
                            .SetTooltip("@local-player-name")
                    )
                    .AddChild(new Label("@local-player-name", Engine, this))
                    .SetBorder(0)
                    .Margins(2, 0);

            widgets.Add(
                new ListBox(Engine, this)
                    .AddChild(portrait)
                    .AddChild(new Label("$: @local-player-money", Engine, this))
                    .Padding(10)
                    .Margins(60, 40)
            );

            CardBox =
                (ListBox)
                new ListBox(Engine, this, 5)
                    .Margins(40)
                    .SetBorder(0)
                    .BackgroundAlpha(0f)
                    .SetAlign(HAlignment.Right, VAlignment.Bottom)
                ;

            widgets.Add(CardBox);

            CommandCard =
                (SideBySideWidgets)
                new SideBySideWidgets(Engine, this, 5)
                    .Padding(5)
                    .SetAlign(HAlignment.Right, VAlignment.Bottom)
                ;

            CommandCard
                .AddChild(
                    new TextureButton(
                        "station",
                        () =>
                            Engine.Push(
                                new BuildStationEvent(
                                    Game.We,
                                    SelectedTile.Position.X,
                                    SelectedTile.Position.Y
                                )
                            )
                        ,
                        Engine,
                        this,
                        2f
                    )
                    .Padding(2)
                    .SetVisibleCondition(
                        () =>
                            SelectedUnit != null &&
                            SelectedUnit.UnitType.Name == "worker"
                    )
                    .SetEnabledCondition( //enough money for station
                        () =>
                            SelectedTile != null &&
                            SelectedTile.Station == null
                    )
                    .SetTooltip(
                        "Build station - $"
                    )
                )
            ;

            CardBox.AddChild(CommandCard);

            BuildCard =
                (SideBySideWidgets)
                new SideBySideWidgets(Engine, this, 5)
                    .Padding(5)
                    .SetVisibleCondition(() => SelectedStation != null)
                ;

            foreach (UnitType ut in UnitType.UnitTypes)
            {
                UnitType unitType = ut;
                BuildCard
                    .AddChild(
                        new TextureButton(
                            ut.Name,
                            () => Game.Build(unitType, SelectedStation),
                            Engine,
                            this,
                            2f
                        )
                        .Padding(2)
                        .SetEnabledCondition(
                            () => Game.CanBuild(unitType, SelectedStation)
                        )
                        .SetTooltip(
                            string.Format(
                                "Build {0} - {1}$",
                                ut.Name,
                                ut.Cost
                            )
                        )
                    );
            }

            CardBox.AddChild(BuildCard);

            ListBox turnInfo =
                (ListBox)
                new ListBox(Engine, this)
                    .Margins(20)
                    .Padding(10);

            turnInfo.AddChild(
                new Label(
                    "Current player: @current-player-name<@current-player-id>",
                    Engine,
                    this
                )
            );

            widgets.Add(turnInfo);
        }

        public void SetupTestUI()
        {
            ListBox b =
                (ListBox)
                new ListBox(Engine, this)
                    .Margins(40)
                    .Padding(10)
                    .SetAlign(HAlignment.Right)
            ;

            b.AddChild(
                new Label(" == test\nlabel == ", Engine, this)
                    .Margins(2)
                    .SetAlign(HAlignment.Center)
            );

            b.AddChild(
                new Button("alignment!", null, Engine, this)
                    .Margins(2)
                    .Padding(5)
                    .SetAlign(HAlignment.Right)
                    .SetTooltip(
                        "I'M A VERY\nLONG AND COMPLEX\nTOOLTIP"
                    )
            );

            b.AddChild(
                new Button("greyed out", null, Engine, this)
                    .Margins(2)
                    .Padding(5)
                    .SetEnabledCondition(() => false)
            );

            b.AddChild(
                new Button("lots and lots of text", null, Engine, this)
                    .Margins(2)
                    .Padding(5)
            );

            b.AddChild(
                new SideBySideWidgets(Engine, this, 7)
                    .AddChild(new Button("foo", null, Engine, this).Padding(5))
                    .AddChild(new Button("bar", null, Engine, this).Padding(5))
                    .Margins(2)
                    .SetAlign(HAlignment.Center)
                    .SetBorder(0)
            );

            b.AddChild(
                new SideBySideWidgets(Engine, this, 7)
                    .AddChild(new CheckBox(Engine, this).Padding(2))
                    .AddChild(new Label("test", Engine, this))
                    .Margins(2)
                    .SetAlign(HAlignment.Left)
                    .SetBorder(0)
            );

            b.AddChild(
                new SideBySideWidgets(Engine, this, 7)
                    .AddChild(new CheckBox(Engine, this).Padding(2))
                    .AddChild(new Label("some other option", Engine, this))
                    .Margins(2)
                    .SetAlign(HAlignment.Left)
                    .SetBorder(0)
            );

            widgets.Add(b);

            ListBox foo =
                (ListBox)
                new ListBox(Engine, this)
                    .SetAlign(HAlignment.Right, VAlignment.Bottom)
                    .Margins(40)
                    .Padding(10)
            ;

            foo.AddChild(
                new Label(" - Another label - ", Engine, this)
                    .SetAlign(HAlignment.Center)
            );

            foo.AddChild(
                new Button(
                    "I'm aligned to the bottom!",
                    () => {
                        Game.Log.Add("foo!");
                    },
                    Engine,
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
            Tile t = Game.World.Map.At(position);

            if (t.Unit != null)
                Selection = new UnitSelection(t.Unit);
            else
                Selection = new TileSelection(t);
        }

        public void Draw()
        {
            DrawBackground();

            if (Game.World == null) return;

            DrawMap();
            DrawUI();

            DrawWidgets();
        }

        private void DrawBackground()
        {
            Vector2 position =
                -Engine.GetTextureSize("background") / 2f +
                Engine.GetSize() / 2f;
            position -= Camera.Camera.Position / 10f;

            Engine.Draw(
                Engine.GetTexture("background"),
                new fbRectangle(position, new Vector2(-1)),
                new Color(0.3f, 0.3f, 0.3f),
                1000
            );
        }

        private void DrawGrid()
        {
            for (int x = 0; x < Game.World.Map.Width; x++)
            for (int y = 0; y < Game.World.Map.Height; y++)
            {
                Engine.Draw(
                    Engine.GetTexture("grid"),
                    Camera.WorldToScreen(
                        new fbRectangle(
                            new Vector2(x, y) * tileSize,
                            tileSize,
                            tileSize
                        )
                    ),
                    new Color(
                        0.5f,
                        0.5f,
                        0.5f
                    )
                );
            }
        }

        private void DrawTile(int x, int y)
        {
            Tile t = Game.World.Map.At(x, y);
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
                Player owner = Game.World.GetPlayer(t.Station.Owner);

                Engine.Draw(
                    t.Unit == null
                        ? Engine.GetTexture("station")
                        : Engine.GetTexture("station-bg"),
                    destination,
                    owner == null
                        ? Color.Gray
                        : owner.Color
                );
            }

            if (t.Planet != null)
            {
                Engine.Draw(
                    t.Unit == null
                        ? Engine.GetTexture("planet")
                        : Engine.GetTexture("planet-bg"),
                    destination
                );
            }

            if (t.Unit != null)
                DrawUnit(t.Unit);
        }

        public void DrawSelection()
        {
            if (Selection == null) return;
            Tile t = Game.World.Map.At(Selection.GetSelection());

            if (
                (SelectedUnit != null && t.Unit == SelectedUnit) ||
                (SelectedTile == t && t.Station != null)
            ) {
                Vector2 position = t.Position.ToVector2();

                if (Selection.GetSelectionType() == SelectionType.Unit)
                {
                    //no i'm not going to check if it's null, it's not null.
                    //i'll fight you.
                    Debug.Assert(SelectedUnit != null, "SelectedUnit != null");

                    position =
                        SelectedUnit
                            .GetAnimateable()
                            .ApplyAnimations()
                            .Position;
                }

                fbRectangle destination =
                    Camera.WorldToScreen(
                        new fbRectangle(
                            //t.Position * tileSize,
                            position * tileSize,
                            new Vector2(tileSize)
                        )
                    );

                Engine.Draw(
                    Engine.GetTexture("selection"),
                    destination
                );
            }
        }

        private void DrawUnit(Unit unit)
        {
            AnimationValues animationValues = unit
                .GetAnimateable()
                .ApplyAnimations();

            fbRectangle destination =
                Camera.WorldToScreen(
                    new fbRectangle(
                        animationValues.Position * tileSize,
                        new Vector2(tileSize)
                    )
                );

            if(unit.Attacks > 0)
                Engine.Draw(
                    Engine.GetTexture("ui-attackborder"),
                    destination,
                    Color.White
                );

            Engine.Draw(
                Engine.GetTexture(unit.UnitType.Texture),
                destination,
                unit.Owner == -1 //dc:ed
                    ? Color.Gray
                    : Game.World.GetPlayer(unit.Owner).Color
            );

            for (int i = 0; i < unit.Moves; i++)
            {
                Vector2 dingySize = Engine.GetTextureSize("move-dingy");
                Vector2 distancing = new Vector2(Camera.Scale(dingySize.X), 0);

                Engine.Draw(
                    Engine.GetTexture("move-dingy"),
                    new fbRectangle(
                        destination.Position + distancing * i,
                        Camera.Scale(dingySize)
                    )
                );
            }

            for (int i = 0; i < unit.Strength; i++)
            {
                Vector2 dingySize = Engine.GetTextureSize("strength-dingy");
                Vector2 distancing = new Vector2(0, Camera.Scale(dingySize.Y));

                Engine.Draw(
                    Engine.GetTexture("strength-dingy"),
                    new fbRectangle(
                        destination.Position
                            + new Vector2(0, Camera.Scale(tileSize))
                            - distancing * (i + 1),
                        Camera.Scale(dingySize)
                    )
                );
            }
        }

        private void DrawMap()
        {
            DrawGrid();
            for (int x = 0; x < Game.World.Map.Width; x++)
            for (int y = 0; y < Game.World.Map.Height; y++)
                DrawTile(x, y);
        }

        //todo: clean this thing up
        private void DrawUI()
        {
            DrawSelection();

            if (tooltip != null)
            {
                Vector2 tooltipSize = Engine.DefaultFont.Measure(tooltip);

                Vector2 tooltipPosition =
                    new Vector2(
                        Engine.MousePosition.X
                            .Clamp(
                                20,
                                Engine.GetSize().X - (tooltipSize.X + 20)
                            ),
                        Engine.MousePosition.Y
                            - (tooltipSize.Y + 5)
                    );

                new DrawCall(
                    Engine.GetTexture("blank"),
                    new fbRectangle(
                        tooltipPosition,
                        tooltipSize
                    ).Grow(4),
                    -999,
                    DefaultTheme.Background.Color
                ).Draw(Engine);

                new TextCall(
                    tooltip,
                    Engine.DefaultFont,
                    tooltipPosition,
                    -1000
                ).Draw(Engine);
            }

            List<string> logTail;
            lock (Game.Log)
            {
                logTail = Game.Log
                    .Skip(Math.Max(0, Game.Log.Count - 10))
                    .ToList();
            }
            logTail.Reverse();

            Vector2 position = new Vector2(10, Engine.GetSize().Y - 10);
            foreach (string message in logTail)
            {
                position -= new Vector2(0, Engine.DefaultFont.CharSize.Y + 1);

                new TextCall(
                    message,
                    Engine.DefaultFont,
                    position
                ).Draw(Engine);
            }

            position = new Vector2(Engine.GetSize().X - 10, 0);
            foreach (int id in Game.World.Players.Keys)
            {
                Player p = Game.World.Players[id];

                position += new Vector2(0, Engine.DefaultFont.CharSize.Y + 1);

                new TextCall(
                    p.Name + ": "+ p.Money + "$",
                    Engine.DefaultFont,
                    position
                ).RightAlign().Draw(Engine);
            }
        }

        private void DrawWidgets()
        {
            foreach (Widget w in widgets)
                if (w.IsVisible())
                    w.Render();
        }

        public void Update()
        {
            if (Engine.KeyPressed(Keys.Escape))
            {
                Engine.Exit();
            }

            Camera.Update();

            //no (important) interaction if we're waiting for data.
            if (!Game.Ready) return;

            UpdateUI();

            Input();
        }

        public void GenerateTileTooltip(Tile t)
        {
            string unitTooltip = null;
            string stationTooltip = null;
            string planetTooltip = null;

            if (t.Unit != null)
                unitTooltip = string.Format(
                    "{0} - {1}\n{2}/{3} moves\n{4}/{5} strength",
                    t.Unit.UnitType.Name.Capitalize(),
                    Game.World.GetPlayer(t.Unit.Owner).Name,
                    t.Unit.Moves,
                    t.Unit.UnitType.Moves,
                    t.Unit.Strength,
                    t.Unit.UnitType.Strength
                );

            if (t.Station != null)
            {
                stationTooltip =
                    string.Format(
                        "{0} ({1})",
                        "Station",
                        t.Station.Owner == 1
                            ? "derelict"
                            : Game.World.GetPlayer(t.Station.Owner).Name
                    );
            }

            if (t.Planet != null)
                planetTooltip = "Planet";

            string[] tooltips =
                {
                    unitTooltip,
                    stationTooltip,
                    planetTooltip
                };

            if(tooltips.Any(tt => tt != null))
                tooltip = 
                    string.Join(
                        "\n\n",
                        tooltips.Where(tt => tt != null)
                    );
        }

        public void UpdateUI()
        {
            Animateable.UpdateAll(Engine.DeltaTime);

            tooltip = null;

            foreach(Widget w in widgets)
                if (w.IsHovered)
                    tooltip = w.GetHovered().Tooltip;

            //if no tooltip from widgets
            if (tooltip == null)
            {
                Vector2? hoveredSquare = ScreenToGrid(Engine.MousePosition);
                if (hoveredSquare.HasValue)
                    GenerateTileTooltip(Game.World.Map.At(hoveredSquare.Value));
            }
        }

        public void Input()
        {
            if (Engine.KeyPressed(Keys.Enter))
            {
                if (Game.OurTurn)
                {
                    Engine.NetClient.Send(
                        new NetMessage3(NM3MessageType.client_pass)
                    );
                }
                else
                    Game.Log.Add("Not your turn!");
            }

            if (Engine.KeyPressed(Keys.G))
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

                Engine.NetClient.Send(
                    new NetMessage3(
                        NM3MessageType.player_name,
                        Game.We,
                        names[Game.We],
                        ExtensionMethods.ColorToString(colors[Game.We])
                    )
                );
            }

            if (Engine.KeyPressed(Keys.H))
            {
                if (Game.OurTurn)
                {
                    Engine.NetClient.Send(
                        new NetMessage3(
                            NM3MessageType.dev_command,
                            0
                        )
                    );
                }
            }

            if (
                Game.OurTurn &&
                SelectedUnit != null &&
                SelectedUnit.Owner == Game.World.CurrentID
            ) {
                Vector2i moveOrder = null;

                if (Engine.KeyPressed(Keys.NumPad2))
                    moveOrder = new Vector2i(0, 1);
                else if (Engine.KeyPressed(Keys.NumPad4))
                    moveOrder = new Vector2i(-1, 0);
                else if (Engine.KeyPressed(Keys.NumPad8))
                    moveOrder = new Vector2i(0, -1);
                else if (Engine.KeyPressed(Keys.NumPad6))
                    moveOrder = new Vector2i(1, 0);

                else if (Engine.KeyPressed(Keys.NumPad1))
                    moveOrder = new Vector2i(-1, 1);
                else if (Engine.KeyPressed(Keys.NumPad7))
                    moveOrder = new Vector2i(-1, -1);
                else if (Engine.KeyPressed(Keys.NumPad9))
                    moveOrder = new Vector2i(1, -1);
                else if (Engine.KeyPressed(Keys.NumPad3))
                    moveOrder = new Vector2i(1, 1);

                if (moveOrder != null && SelectedUnit.Moves > 0)
                {
                    Unit u = SelectedUnit;

                    if(u.CanMoveTo(u.Position + moveOrder))
                    {
                        int x = (u.Position + moveOrder).X;
                        int y = (u.Position + moveOrder).Y;

                        Engine.NetClient.Send(
                            new NetMessage3(
                                NM3MessageType.unit_move,
                                u.ID,
                                x,
                                y
                            )
                        );

                        u.Moves -= 1;
                        Engine.Push(new UnitMoveEvent(u.ID, x, y));
                    }
                    else if (u.CanAttack(u.Position + moveOrder))
                    {
                        Vector2i targettile = u.Position + moveOrder;
                        Unit target = Game.World.Map.At(
                            targettile.X,
                            targettile.Y
                        ).Unit;

                        Engine.NetClient.Send(
                            new NetMessage3(
                                NM3MessageType.unit_attack,
                                u.ID, target.ID
                            )
                        );
                    }
                }
            }

            if (Engine.KeyPressed(Keys.OemPeriod))
            {
                List<Unit> selectable =
                    Game.LocalPlayer.OwnedUnits
                    .Select(id => Game.World.UnitLookup[id])
                    .Where(u => u.Attacks > 0 || u.Moves > 0)
                    .ToList()
                ;

                if (selectable.Count > 0)
                {
                    if (SelectedUnit != null)
                    {
                        int index = selectable.IndexOf(SelectedUnit);
                        //if the selected is not one of ours, we get -1
                        //which still works with the code (because the
                        //new selected index then becomes 0)
                        //which is absolutely fine
                        //only works with forward selection though.

                        index = (index + 1) % selectable.Count;

                        Select(selectable[index].Position);
                    }
                    else
                    {
                        Select(selectable[0].Position);
                    }
                }

                /*if (engine.KeyPressed(Keys.A))
                {
                    SelectedUnit.MoveTo(
                        SelectedUnit.Position +
                        SelectedUnit.StepTowards(new Vector2(0))
                    );
                    SelectedUnit.Moves -= 1;
                }*/
            }

            //handled more gracefully in the future, hopefully...
            bool passClickToWorld = true;

            if (Engine.ButtonPressed(0))
            {
                foreach(Widget w in widgets)
                    if (w.IsHovered)
                    {
                        w.OnClick();
                        passClickToWorld = false;
                    }
            }

            if (Engine.ButtonPressed(0) && passClickToWorld)
            {
                if (Engine.Active && Engine.MouseInside)
                {
                    Vector2? square = ScreenToGrid(Engine.MousePosition);

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
                square.X >= 0 && square.X < Game.World.Map.Width &&
                square.Y >= 0 && square.Y < Game.World.Map.Height
            )
                return square;

            return null;
        }
    }
}