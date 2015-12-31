#region

using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;
using SharpDX;
using SharpDX.XInput;
using Color = System.Drawing.Color;

#endregion

namespace ControlBuddy
{
    internal class Program
    {
        public static int[] ControllerArray = {0, 1, 2, 3, 4};
        public static Menu Menu;
        public static Orbwalker.ActiveModes CurrentMode = Orbwalker.ActiveModes.None;
        public static GamepadState Controller;
        public static float MaxD = 0;
        public static uint LastKey;
        public static int MenuCount;

        public static Vector3 PadPos;

        public static Dictionary<Orbwalker.ActiveModes, string> KeyDictionary =
            new Dictionary<Orbwalker.ActiveModes, string>
            {
                {Orbwalker.ActiveModes.Combo, "Orbwalk"},
                {Orbwalker.ActiveModes.Flee, "Flee"},
                {Orbwalker.ActiveModes.LaneClear, "LaneClear"},
                {Orbwalker.ActiveModes.LastHit, "LastHit"}
            };

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            foreach (var c in
                ControllerArray.Select(controlId => new Controller((UserIndex) controlId)).Where(c => c.IsConnected))
            {
                Controller = new GamepadState(c.UserIndex);
            }

            if (Controller == null || !Controller.Connected)
            {
                Chat.Print("No controller detected!");
                return;
            }

            Chat.Print(
                "<b><font color =\"#FFFFFF\">ControlSharp by </font><font color=\"#5C00A3\">Trees</font><font color =\"#FFFFFF\"> loaded!</font></b>");


            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += delegate
            {
                var pos = Player.Instance.Position.WorldToScreen();

                Drawing.DrawText(new Vector2(pos.X - 40, pos.Y - 20), Color.White,
                    string.Format("Mode: {0}", CurrentMode), 15);
                Circle.Draw(SharpDX.Color.White, 50, PadPos);
            };
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            var wp = ObjectManager.Player.Path.ToList();

            //in case you manually click to move
            if (wp.Count > 0 && ObjectManager.Player.Distance(wp[wp.Count - 1]) > 540)
            {
                PadPos = ObjectManager.Player.Position;
                SetOrbwalkingMode(Orbwalker.ActiveModes.None);
                return;
            }

            if (Controller == null || !Controller.Connected)
            {
                Chat.Print("Controller disconnected!");
                Game.OnUpdate -= Game_OnGameUpdate;
                return;
            }

            Controller.Update();
            UpdateStates();

            var p = ObjectManager.Player.ServerPosition.To2D() + Controller.LeftStick.Position/75;
            var pos = new Vector3(p.X, p.Y, ObjectManager.Player.Position.Z);

            PadPos = pos;

            if (ObjectManager.Player.Distance(pos) < 100)
            {
                Console.WriteLine("No distance");
                return;
            }

            SetOrbwalkingPosition(pos);
        }

        private static void UpdateStates()
        {
            //Push any button to cancel mode
            if (Controller.LeftShoulder || Controller.RightShoulder || Controller.Back || Controller.Start ||
                Controller.RightStick.Clicked)
            {
                SetOrbwalkingMode(Orbwalker.ActiveModes.None);
                return;
            }

            if (Controller.DPad.IsAnyPressed() || Controller.IsABXYPressed()) // Change mode command
            {
                if (Controller.DPad.Up || Controller.X)
                {
                    SetOrbwalkingMode(Orbwalker.ActiveModes.Combo);
                }
                else if (Controller.DPad.Left || Controller.A)
                {
                    SetOrbwalkingMode(Orbwalker.ActiveModes.LaneClear);
                }
                else if (Controller.DPad.Right || Controller.Y)
                {
                    CurrentMode = Orbwalker.ActiveModes.Flee;
                    Player.IssueOrder(GameObjectOrder.MoveTo, PadPos);
                }
                else if (Controller.DPad.Down || Controller.B)
                {
                    SetOrbwalkingMode(Orbwalker.ActiveModes.LastHit);
                }
            }

            var s1 = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Summoner1);
            var s2 = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Summoner2);

            if (Controller.LeftTrigger > 0 && s1.State == SpellState.Ready)
            {
                SummonerCastLogic(s1);
                return;
            }

            if (Controller.RightTrigger > 0 && s2.State == SpellState.Ready)
            {
                SummonerCastLogic(s2);
            }
        }

        private static void SummonerCastLogic(SpellDataInst spell)
        {
            switch (spell.Name.ToLower().Replace("summoner", ""))
            {
                case "barrier":
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
                    break;
                case "boost":
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
                    break;
                case "dot":
                    foreach (
                        var enemy in
                            ObjectManager.Get<AIHeroClient>().Where(h => h.IsValidTarget(550) && h.Health < 600)
                        )
                    {
                        ObjectManager.Player.Spellbook.CastSpell(spell.Slot, enemy);
                        break;
                    }
                    break;
                case "flash": //LOL
                    Controller.Update();
                    var pos = ObjectManager.Player.ServerPosition.To2D() + Controller.LeftStick.Position/75;
                    pos.Extend(ObjectManager.Player.ServerPosition.To2D(), 550);
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot, pos.To3D());
                    break;
                case "haste":
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
                    break;
                case "heal":
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
                    break;
                case "mana":
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
                    break;
                case "revive":
                    ObjectManager.Player.Spellbook.CastSpell(spell.Slot);
                    break;
            }
        }

        private static void SetOrbwalkingMode(Orbwalker.ActiveModes mode)
        {
            CurrentMode = mode;

            if (mode == Orbwalker.ActiveModes.LaneClear)
            {
                Orbwalker.ActiveModesFlags = CurrentMode | Orbwalker.ActiveModes.JungleClear;
            }
            else
            {
                Orbwalker.ActiveModesFlags = CurrentMode;
            }

            Console.WriteLine("Setting mode to: {0}", CurrentMode);
        }

        private static void SetOrbwalkingPosition(Vector3 position)
        {
            Orbwalker.OverrideOrbwalkPosition += () => position;
        }
    }
}