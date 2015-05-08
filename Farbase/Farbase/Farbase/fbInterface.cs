﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum InterfaceMode
    {
        Normal,
        TargettingWarp
    }

    public class fbInterface
    {
        public fbGame Game;
        public fbEngine Engine;

        private const int tileSize = 16;

        public InterfaceMode Mode = InterfaceMode.Normal;

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

        //tbh, these should probably not public
        //but we'll keep it like this for now because
        // 1. easier for us
        // 2. it's not like anything at all in the entire program except
        //    for the main app and parts of the interface is allowed to keep
        //    a fbInterface reference anyways.
        public Dictionary<string, Widget> NamedWidgets;
        public List<Widget> Widgets;

        public Theme DefaultTheme;

        private string tooltip;

        public fbInterface(
            fbGame game,
            fbEngine engine
        ) {
            Game = game;
            Engine = engine;
            Camera = new fbCamera(engine);
            Widgets = new List<Widget>();
            NamedWidgets = new Dictionary<string, Widget>();

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

            LoadUIFromXml("ui/main.xml");

            InterfaceEventHandler ieh = new InterfaceEventHandler(game, this);
            engine.Subscribe(ieh, EventType.NameEvent);
            engine.Subscribe(ieh, EventType.BuildUnitEvent);

            new InterfaceInputHandler(game, this);
        }

        public void LoadUIFromXml(string path)
        {
            XmlDocument document = new XmlDocument();
            using (FileStream stream = File.OpenRead(path))
            {
                document.Load(stream);
            }

            foreach (XmlNode n in document.FirstChild.ChildNodes)
            {
                Widgets.Add(new fbXmlLoader(this).WidgetFromXml(n));
            }

            UIXmlPostLoad();
        }

        public void UIXmlPostLoad()
        {
            ((Button)NamedWidgets["build-station-button"])
                .SetAction(
                    () =>
                        Engine.Push(
                            new BuildStationEvent(
                                Game.We,
                                SelectedTile.Position.X,
                                SelectedTile.Position.Y
                            )
                        )
                )
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
                .SetTooltip("Build station - X$")
            ;

            //Notice!
            //the way we're automating this (because we're fat and lazy)
            //we need to have a build button for each unit type, at least at
            //the moment.
            //we probably want that anyways atm, but it's worth keeping in mind.
            foreach (UnitType ut in UnitType.UnitTypes)
            {
                string widgetName =
                    string.Format(
                        "build-{0}-button",
                        ut.Name.ToLower()
                    );

                ((Button)NamedWidgets[widgetName])
                    .SetAction(
                        () => SelectedStation.StartProject(
                            ProjectType.UnitProject,
                            (int)ut.Type
                        )
                    )
                    .SetEnabledCondition(
                        () => Game.CanBuild(
                            Game.LocalPlayer,
                            ut,
                            SelectedStation
                        )
                    )
                    .SetTooltip(
                        string.Format(
                            "Build {0} - {1}$ ({2} turns)",
                            ut.Name,
                            ut.Cost,
                            ut.ConstructionTime
                        )
                    )
                ;
            }

            ((Button)NamedWidgets["research-fighters-button"])
                .SetAction(
                    () => SelectedStation.StartProject(
                        ProjectType.TechProject,
                        (int)TechID.FighterTech
                    )
                )
                .SetEnabledCondition(
                    () => Game.CanResearch(
                        Game.LocalPlayer,
                        TechID.FighterTech,
                        SelectedStation
                    )
                )
                .SetTooltip(
                    String.Format(
                        "Research {0} - {1}$ ({2} turns)\n{3}",
                        Tech.Techs[TechID.FighterTech].Name,
                        Tech.Techs[TechID.FighterTech].Cost,
                        Tech.Techs[TechID.FighterTech].ResearchTime,
                        Tech.Techs[TechID.FighterTech].Description
                    )
                )
            ;
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
                    Engine.GetTexture("ui-selection"),
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

            if (unit.WarpTarget != null)
            {
                Engine.Draw(
                    Engine.GetTexture("ui-warp-beacon"),
                    Camera.WorldToScreen(
                        new fbRectangle(
                            unit.WarpTarget * tileSize,
                            tileSize
                        )
                    ),
                    Color.White
                );
            }

            for (int i = 0; i < unit.Moves; i++)
            {
                Vector2 dingySize = Engine.GetTextureSize("ui-move-dingy");
                Vector2 distancing = new Vector2(Camera.Scale(dingySize.X), 0);

                Engine.Draw(
                    Engine.GetTexture("ui-move-dingy"),
                    new fbRectangle(
                        destination.Position + distancing * i,
                        Camera.Scale(dingySize)
                    )
                );
            }

            for (int i = 0; i < unit.Strength; i++)
            {
                Vector2 dingySize = Engine.GetTextureSize("ui-strength-dingy");
                Vector2 distancing = new Vector2(0, Camera.Scale(dingySize.Y));

                Engine.Draw(
                    Engine.GetTexture("ui-strength-dingy"),
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

            switch (Mode)
            {
                case InterfaceMode.TargettingWarp:
                    new DrawCall(
                        Engine.GetTexture("ui-target-cursor"),
                        new fbRectangle(
                            Engine.MousePosition,
                            Engine.GetTextureSize("ui-target-cursor")
                        ).Center(),
                        -2000
                    ).Draw(Engine);
                    break;

                default:
                    new DrawCall(
                        Engine.GetTexture("ui-cursor"),
                        new fbRectangle(
                            Engine.MousePosition,
                            new Vector2(24)
                        ),
                        -2000
                    ).Draw(Engine);
                    break;
            }

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
            foreach (Widget w in Widgets)
                if (w.IsVisible())
                    w.Render();
        }

        public void Update()
        {
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
            {
                unitTooltip = string.Format(
                    "{0} - {1}\n{2}/{3} moves\n{4}/{5} strength",
                    t.Unit.UnitType.Name.Capitalize(),
                    Game.World.GetPlayer(t.Unit.Owner).Name,
                    t.Unit.Moves,
                    t.Unit.UnitType.Moves,
                    t.Unit.Strength,
                    t.Unit.UnitType.Strength
                );

                if (t.Unit.WarpTarget != null)
                {
                    unitTooltip += string.Format(
                        "\n<<Warping in {0}...>>",
                        t.Unit.WarpCountdown
                    );
                }
            }

            if (t.Station != null)
            {
                string projectString;

                if (t.Station.Project != null)
                {
                    projectString = string.Format(
                        "{0} ({1} turns left)",
                        t.Station.Project.GetProjectName(),
                        t.Station.Project.Remaining
                    );
                }
                else
                    projectString = "No current project";

                stationTooltip =
                    string.Format(
                        "{0} ({1})\n : {2}",
                        "Station",
                        t.Station.Owner == -1
                            ? "derelict"
                            : Game.World.GetPlayer(t.Station.Owner).Name,
                        projectString
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
            bool worldTooltip = true;

            foreach(Widget w in Widgets)
                if (w.IsHovered)
                {
                    worldTooltip = false;
                    tooltip = w.GetHovered().Tooltip;
                }

            //if no tooltip from widgets
            //if (tooltip == null)
            if (worldTooltip)
            {
                Vector2i hoveredSquare = ScreenToGrid(Engine.MousePosition);
                if (hoveredSquare != null)
                    GenerateTileTooltip(Game.World.Map.At(hoveredSquare));
            }
        }

        public void Input()
        {
            //handled more gracefully in the future, hopefully...
            bool passClickToWorld = true;

            if (Engine.ButtonPressed(0))
            {
                foreach(Widget w in Widgets)
                    if (w.IsHovered)
                    {
                        w.OnClick();
                        passClickToWorld = false;
                    }
            }

            if (Engine.ButtonPressed(0) && passClickToWorld)
                if (Engine.Active && Engine.MouseInside)
                    OnClick();
        }

        private void OnClick()
        {
            Vector2i square = ScreenToGrid(Engine.MousePosition);
            if (square == null) return;

            switch (Mode)
            {
                case InterfaceMode.TargettingWarp:
                    SelectedUnit.WarpTarget = square;
                    SelectedUnit.WarpCountdown = 3;

                    if (square == SelectedUnit.Position)
                    {
                        SelectedUnit.WarpTarget = null;
                        SelectedUnit.WarpCountdown = -1;
                    }

                    Mode = InterfaceMode.Normal;
                    break;

                default:
                case InterfaceMode.Normal:
                    Select(square);
                    break;
            }
        }

        private Vector2i ScreenToGrid(Vector2 position)
        {
            Vector2 worldPoint = Camera.ScreenToWorld(position);
            Vector2i square =
                new Vector2i(
                    (int)(worldPoint.X - (worldPoint.X % tileSize)),
                    (int)(worldPoint.Y - (worldPoint.Y % tileSize))
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