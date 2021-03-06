﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DZAwarenessAIO.Properties;
using DZAwarenessAIO.Utility.Extensions;
using DZAwarenessAIO.Utility.HudUtility.HudElements;
using DZAwarenessAIO.Utility.Logs;
using DZAwarenessAIO.Utility.MenuUtility;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace DZAwarenessAIO.Utility.HudUtility
{
    class HudDisplay
    {
        /// <summary>
        /// The initial drag point
        /// </summary>
        private static Vector2 InitialDragPoint = new Vector2();

        /// <summary>
        /// The x distance from the top left edge
        /// </summary>
        private static float XDistanceFromEdge = 0;

        /// <summary>
        /// The y distance from the top left edge
        /// </summary>
        private static float YDistanceFromEdge = 0;

        /// <summary>
        /// The init point of the class
        /// </summary>
        public static void OnLoad()
        {
            var RootMenu = Variables.Menu;
            var moduleMenu = new Menu("HUD!", "dz191.dza.hud").SetFontStyle(FontStyle.Bold, SharpDX.Color.Red);
            {
                moduleMenu.AddBool("dz191.dza.hud.show", "Show HUD", true).SetTooltip("Shows the DZAwareness Helper hud").ValueChanged += (sender, args) =>
                {
                    /**Dispose of the sprites*/ 
                    if (args.GetNewValue<bool>())
                    {
                        //InitSprites(true);
                        //ImageLoader.InitSprites();
                    }
                    else
                    {
                        //HudVariables.HudSprite.Remove();
                        //ImageLoader.RemoveSprites();
                        //HudVariables.ExpandShrinkButton.Remove();
                    }
                };
                moduleMenu.AddBool("dz191.dza.hud.draggable", "Draggable", true).SetTooltip("If ON = You can Drag the HUD. If OFF = Hud is fixed.");
                moduleMenu.AddSlider("dz191.dza.hud.x", "HUD X", new Tuple<int, int, int>(20, 0, Drawing.Direct3DDevice.Viewport.Width)).SetTooltip("Hud X Position (You can drag it)");
                moduleMenu.AddSlider("dz191.dza.hud.y", "HUD Y", new Tuple<int, int, int>(Drawing.Direct3DDevice.Viewport.Height - HudVariables.CroppedHeight - 20, 0, Drawing.Direct3DDevice.Viewport.Height)).SetTooltip("Hud Y Position (You can drag it)");
                RootMenu.AddSubMenu(moduleMenu);
            }

            InitSprites();

            Game.OnWndProc += OnWndProc;
        }

        /// <summary>
        /// Raises the <see cref="E:WndProc" /> event.
        /// </summary>
        /// <param name="args">The <see cref="WndEventArgs"/> instance containing the event data.</param>
        private static void OnWndProc(WndEventArgs args)
        {
            if (HudVariables.HudSprite == null || !HudVariables.ShouldBeVisible || !MenuExtensions.GetItemValue<bool>("dz191.dza.hud.draggable"))
            {
                return;
            }

            if (HudVariables.IsDragging)
            {
                HudVariables.DraggingPosition.X = (int)(Utils.GetCursorPos().X - XDistanceFromEdge);
                HudVariables.DraggingPosition.Y = (int)(Utils.GetCursorPos().Y - YDistanceFromEdge);
            }

            if (IsInside(Utils.GetCursorPos()) && args.Msg == (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                var list = HudVariables.HudElements.Where(el => el.GetType() == typeof(HudPanel)).ToList();
                if (list.Any())
                {
                    var SecondList = list.Cast<HudPanel>().ToList();
                    if (
                        SecondList.Any(
                            panel =>
                                Helper.IsInside(
                                    Utils.GetCursorPos(), (int) panel.Position.X, (int) panel.Position.Y, panel.Width,
                                    panel.Height)))
                    {
                        //return;
                    }
                }

                if (!HudVariables.IsDragging)
                {
                    if (InitialDragPoint == new Vector2())
                    {
                        InitialDragPoint = HudVariables.CurrentPosition;
                        XDistanceFromEdge = Math.Abs(InitialDragPoint.X - Utils.GetCursorPos().X);
                        YDistanceFromEdge = Math.Abs(InitialDragPoint.Y - Utils.GetCursorPos().Y);

                        HudVariables.DraggingPosition.X = (int)(Utils.GetCursorPos().X - XDistanceFromEdge);
                        HudVariables.DraggingPosition.Y = (int)(Utils.GetCursorPos().Y - YDistanceFromEdge);

                    }

                    HudVariables.IsDragging = true;
                }
            }
            else if (HudVariables.IsDragging && args.Msg == (uint)WindowsMessages.WM_LBUTTONUP)
            {
                HudVariables.HudSprite.PositionUpdate = () => HudVariables.CurrentPosition;

                Variables.Menu.Item("dz191.dza.hud.x")
                    .SetValue(new Slider((int)(Utils.GetCursorPos().X - XDistanceFromEdge), 0, Drawing.Direct3DDevice.Viewport.Width));
                Variables.Menu.Item("dz191.dza.hud.y")
                    .SetValue(new Slider((int)(Utils.GetCursorPos().Y - YDistanceFromEdge), 0, Drawing.Direct3DDevice.Viewport.Height));

                InitialDragPoint = new Vector2();
                XDistanceFromEdge = 0;
                YDistanceFromEdge = 0;
                HudVariables.DraggingPosition = new Vector2();

                HudVariables.IsDragging = false;
            }


            if (args.Msg == (uint) WindowsMessages.WM_LBUTTONUP)
            {
                if (
                    Utils.GetCursorPos()
                        .Distance(new Vector2(HudVariables.CurrentPosition.X + HudVariables.SpriteWidth - 15 + (Resources.Expand.Width * 0.7f) / 2f,
                            HudVariables.CurrentPosition.Y + 3 + (Resources.Expand.Height * 0.7f) / 2f)) < Resources.Expand.Width * 0.7f / 2f)
                {
                    if (HudVariables.CurrentStatus == SpriteStatus.Shrinked)
                    {
                        HudVariables.CurrentStatus = SpriteStatus.Expanded;
                        HudVariables.HudSprite.Crop(0, 0, (int)HudVariables.SpriteWidth, (int)HudVariables.SpriteHeight);

                        HudVariables.ExpandShrinkButton.Remove();
                        HudVariables.ExpandShrinkButton = new Render.Sprite(Resources.Shrink, HudVariables.CurrentPosition)
                        {
                            PositionUpdate = () => new Vector2(HudVariables.CurrentPosition.X + HudVariables.SpriteWidth - 15, HudVariables.CurrentPosition.Y + 3),
                            Scale = new Vector2(0.7f, 0.7f),
                            VisibleCondition = delegate { return HudVariables.ShouldBeVisible; }
                        };

                        HudVariables.ExpandShrinkButton.Add(1);
                    }
                    else
                    {
                        HudVariables.CurrentStatus = SpriteStatus.Shrinked;
                        HudVariables.HudSprite.Crop(0, 0, (int)HudVariables.SpriteWidth, HudVariables.CroppedHeight);

                        HudVariables.ExpandShrinkButton.Remove();
                        HudVariables.ExpandShrinkButton = new Render.Sprite(Resources.Expand, HudVariables.CurrentPosition)
                        {
                            PositionUpdate = () => new Vector2(HudVariables.CurrentPosition.X + HudVariables.SpriteWidth - 15, HudVariables.CurrentPosition.Y + 3),
                            Scale = new Vector2(0.7f, 0.7f),
                            VisibleCondition = delegate { return HudVariables.ShouldBeVisible; }
                        };

                        HudVariables.ExpandShrinkButton.Add(1);
                    }

                }
            }
        }

        /// <summary>
        /// Initializes the sprites.
        /// </summary>
        /// <param name="bypass">if set to <c>true</c> it will bypass the Menu enabled check.</param>
        public static void InitSprites(bool bypass = false)
        {
            try
            {
                if (!MenuExtensions.GetItemValue<bool>("dz191.dza.hud.show") && !bypass)
                {
                    return;
                }

                HudVariables.HudSprite = new Render.Sprite(Resources.TFHelperBG, HudVariables.CurrentPosition)
                {
                    PositionUpdate = () => HudVariables.CurrentPosition,
                    VisibleCondition = delegate { return HudVariables.ShouldBeVisible; },
                };
                HudVariables.HudSprite.Crop(0, 0, (int)HudVariables.SpriteWidth, HudVariables.CroppedHeight);

                HudVariables.ExpandShrinkButton = new Render.Sprite(Resources.Expand, HudVariables.CurrentPosition)
                {
                    PositionUpdate = () => new Vector2(HudVariables.CurrentPosition.X + HudVariables.SpriteWidth - 15, HudVariables.CurrentPosition.Y + 3),
                    Scale = new Vector2(0.7f, 0.7f),
                    VisibleCondition = delegate { return HudVariables.ShouldBeVisible; }
                };

                HudVariables.HudText = new Render.Text("DZAwareness - HUD", 0, 0, 17, SharpDX.Color.White)
                {
                    Centered = true,
                    PositionUpdate = () => new Vector2(HudVariables.CurrentPosition.X + HudVariables.SpriteWidth / 2f, HudVariables.CurrentPosition.Y + 10),
                    VisibleCondition = delegate { return HudVariables.ShouldBeVisible; }
                };

                HudVariables.HudText.Add(1);
                HudVariables.ExpandShrinkButton.Add(1);
                HudVariables.HudSprite.Add(0);
            }
            catch (Exception ex)
            {
                LogHelper.AddToLog(new LogItem("Hud_Init", ex, LogSeverity.Error));
            }
        }

        /// <summary>
        /// Determines whether the specified position is inside the rectangle.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        private static bool IsInside(Vector2 position)
        {
            return Utils.IsUnderRectangle(position, HudVariables.CurrentPosition.X, HudVariables.CurrentPosition.Y, HudVariables.SpriteWidth, (HudVariables.CurrentStatus == SpriteStatus.Shrinked ? HudVariables.CroppedHeight : HudVariables.SpriteHeight));
        }
    }

    /// <summary>
    /// The Hud status enum
    /// </summary>
    enum SpriteStatus
    {
        Expanded, Shrinked
    }
}
