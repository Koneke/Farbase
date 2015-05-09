using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public class InterfaceInputHandler : IInputSubscriber
    {
        private fbGame game;
        private fbInterface ui;
        private fbEngine engine;

        public InterfaceInputHandler(fbInterface ui)
        {
            this.ui = ui;
            game = ui.Game;
            engine = ui.Engine;

            new InputSubscriber(this)
                .Subscribe("move-nw")
                .Subscribe("move-n")
                .Subscribe("move-ne")
                .Subscribe("move-e")
                .Subscribe("move-se")
                .Subscribe("move-s")
                .Subscribe("move-sw")
                .Subscribe("move-w")
                .Subscribe("pass")
                .Subscribe("warp")
                .Subscribe("bombard")
                .Subscribe("dev-login")
                .Subscribe("dev-test")
                .Subscribe("dev-save")
                .Subscribe("dev-spawn-station")
                .Subscribe("dev-spawn-planet")
                .Subscribe("dev-spawn-playerstart")
                .Subscribe("select-next-idle")
                .Subscribe("quit")
                .Register();
        }

        public void ReceiveInput(string s)
        {
            MovementInput(s);
            DevInput(s);

            switch (s)
            {
                case "force-quit":
                    ui.Engine.Exit();
                    break;

                case "quit":
                    if (ui.Mode == InterfaceMode.Normal)
                        ui.Engine.Exit();
                    else ui.Mode = InterfaceMode.Normal;
                    break;

                case "warp":
                    if(ui.SelectedUnit != null)
                        ui.Mode = InterfaceMode.TargettingWarp;
                    break;

                case "bombard":
                    if(ui.SelectedUnit != null)
                        if(
                            ui.SelectedUnit.HasAbility(
                                UnitAbilites.Bombarding
                            ) && ui.SelectedUnit.Attacks > 0
                        )
                            ui.Mode = InterfaceMode.TargettingBombard;
                    break;

                case "pass":
                    if (game.OurTurn)
                    {
                        ui.Engine.NetClient.Send(
                            new NetMessage3(NM3MessageType.client_pass)
                            );
                    }
                    else
                    {
                        game.Log.Add("Not your turn!");
                    }
                    break;

                case "select-next-idle":
                    ui.Mode = InterfaceMode.Normal;

                    List<Unit> selectable =
                        game.World.GetPlayerUnits(game.LocalPlayer.ID)
                            .Select(id => game.World.Units[id])
                            .Where(u => u.Attacks > 0 || u.Moves > 0)
                            .ToList()
                        ;

                    if (selectable.Count > 0)
                    {
                        if (ui.SelectedUnit != null)
                        {
                            int index = selectable.IndexOf(ui.SelectedUnit);
                            //if the selected is not one of ours, we get -1
                            //which still works with the code (because the
                            //new selected index then becomes 0)
                            //which is absolutely fine
                            //only works with forward selection though.

                            index = (index + 1) % selectable.Count;

                            ui.Select(selectable[index].Position);
                        }
                        else
                        {
                            ui.Select(selectable[0].Position);
                        }
                    }
                    break;
            }
        }

        private void DevInput(string s)
        {
            Vector2i hovered = ui.ScreenToGrid(engine.MousePosition);

            switch (s)
            {
                case "dev-spawn-station":
                    if (hovered == null) break;

                    engine.NetClient.Send(
                        new NetMessage3(
                            NM3MessageType.station_create,
                            game.We,
                            -1, //autoassn
                            hovered.X,
                            hovered.Y
                        )
                    );
                    break;

                case "dev-spawn-planet":
                    break;

                case "dev-spawn-playerstart":
                    engine.NetClient.Send(
                        new NetMessage3(
                            NM3MessageType.world_playerstart,
                            hovered.X,
                            hovered.Y
                        )
                    );
                    break;

                case "dev-login":
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

                    ui.Engine.NetClient.Send(
                        new NetMessage3(
                            NM3MessageType.player_name,
                            game.We,
                            names[game.We],
                            ExtensionMethods.ColorToString(colors[game.We])
                        )
                    );
                    break;

                case "dev-save":
                    fbDiskIO.SaveMap(game.World.Map, "save/test.map");
                    break;

                case "dev-test":
                    if (game.OurTurn)
                    {
                        ui.Engine.NetClient.Send(
                            new NetMessage3(
                                NM3MessageType.dev_command,
                                0
                            )
                        );
                    }
                    break;
            }
        }

        private void MovementInput(string s)
        {
            if (
                !game.OurTurn ||
                ui.SelectedUnit == null ||
                ui.SelectedUnit.Owner != game.World.CurrentID
            ) return;

            Vector2i moveOrder;

            switch (s)
            {
                case "move-nw": moveOrder = new Vector2i(-1, -1); break;
                case "move-n":  moveOrder = new Vector2i( 0, -1); break;
                case "move-ne": moveOrder = new Vector2i( 1, -1); break;
                case "move-e":  moveOrder = new Vector2i( 1,  0); break;
                case "move-se": moveOrder = new Vector2i( 1,  1); break;
                case "move-s":  moveOrder = new Vector2i( 0,  1); break;
                case "move-sw": moveOrder = new Vector2i(-1,  1); break;
                case "move-w":  moveOrder = new Vector2i(-1,  0); break;
                default: return;
            }

            Unit u = ui.SelectedUnit;

            if(u.CanMoveTo(u.Position + moveOrder))
            {
                u.Moves -= 1;
                ui.Engine.Push(
                    new UnitMoveEvent(
                        u.ID,
                        u.Position + moveOrder,
                        true
                    )
                );
            }

            if (u.CanAttack(u.Position + moveOrder))
            {
                Vector2i targettile = u.Position + moveOrder;
                Unit target = game.World.Map.At(
                    targettile.X,
                    targettile.Y
                ).Unit;

                ui.Engine.NetClient.Send(
                    new NetMessage3(
                        NM3MessageType.unit_attack,
                        u.ID, target.ID
                    )
                );
            }
        }

    }
}