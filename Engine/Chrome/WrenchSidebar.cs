using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Chrome
{
	/// <summary>
	/// Third hub style: a labelled Terraria panel docked to the nearest screen edge. Every section is one
	/// click away with its name spelled out (no guessing icons), plus quick toggles for the things people flip
	/// most: version clutter, the music player and muting in the background. Click the chevron to fold it
	/// down to an icon rail. It sits where the hub element is placed in the layout editor.
	/// </summary>
	internal static class WrenchSidebar
	{
		private const int RowH = 34;
		private const int HeadH = 34;
		private const int QuickH = 44;
		private const int WideW = 196;
		private const int SlimW = 50;

		private static float _width = WideW;
		private static int _hover = -1;
		private static int _quickHover = -1;
		private static bool _headHover;
		private static bool _frameInput;
		private static bool _mouseHeld;
		private static bool _holdLock;

		private enum Quick
		{
			Clean,
			Player,
			Mute
		}

		private static readonly Quick[] Quicks = { Quick.Clean, Quick.Player, Quick.Mute };

		internal static bool Busy => Bounds().Contains(Main.mouseX, Main.mouseY);

		internal static Rectangle HitRect() => Bounds();

		private static bool Collapsed => WeSave.Data.SidebarCollapsed;

		internal static void Reset()
		{
			_hover = -1;
			_quickHover = -1;
			_width = Collapsed ? SlimW : WideW;
		}

		internal static void Update()
		{
			float target = Collapsed ? SlimW : WideW;
			_width = MathHelper.Lerp(_width, target, 0.3f);
			if (Math.Abs(_width - target) < 0.5f)
				_width = target;
		}

		private static bool OnLeft => WrenchToolbar.Anchor.X < Main.screenWidth * 0.5f;

		private static Rectangle Bounds()
		{
			int w = (int)Math.Round(_width);
			int h = HeadH + WrenchHub.Actions.Length * RowH + QuickH + 14;
			int x = OnLeft ? 10 : Main.screenWidth - w - 10;
			if (!OnLeft && WePanels.Covering)
				x = Math.Min(x, WePanels.WindowRect.X - w - 8); // sit beside the open window, not under it
			int y = (int)WrenchToolbar.Anchor.Y - h / 2;
			y = Math.Clamp(y, 10, Math.Max(10, Main.screenHeight - h - 10));
			return new Rectangle(x, y, w, h);
		}

		private static Rectangle HeadRect(Rectangle b) => new(b.X + 6, b.Y + 6, b.Width - 12, HeadH - 8);

		private static Rectangle RowRect(Rectangle b, int i) => new(b.X + 6, b.Y + HeadH + i * RowH, b.Width - 12, RowH - 4);

		private static Rectangle QuickRect(Rectangle b, int i)
		{
			int top = b.Y + HeadH + WrenchHub.Actions.Length * RowH + 8;
			if (_width < 120f)
				return new Rectangle(b.X + 9, top + i * 0, 0, 0); // hidden when folded
			int size = 32;
			int gap = (b.Width - 12 - size * Quicks.Length) / Math.Max(1, Quicks.Length - 1);
			return new Rectangle(b.X + 6 + i * (size + gap), top, size, size);
		}

		internal static void HandleInput()
		{
			if (_frameInput || LayoutEditor.Editing || WeSplash.Visible)
				return;
			_frameInput = true;
			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			if (!WeModMenu.OnTitle)
				return;

			Rectangle b = Bounds();
			_hover = -1;
			_quickHover = -1;
			_headHover = HeadRect(b).Contains(Main.mouseX, Main.mouseY);
			for (int i = 0; i < WrenchHub.Actions.Length; i++) {
				if (RowRect(b, i).Contains(Main.mouseX, Main.mouseY))
					_hover = i;
			}

			for (int i = 0; i < Quicks.Length; i++) {
				if (QuickRect(b, i).Contains(Main.mouseX, Main.mouseY))
					_quickHover = i;
			}

			if (!b.Contains(Main.mouseX, Main.mouseY))
				return;
			Main.blockMouse = true;
			if (!pressed)
				return;

			if (_headHover) {
				WeSave.Data.SidebarCollapsed = !WeSave.Data.SidebarCollapsed;
				WeSave.Save();
				SoundEngine.PlaySound(SoundID.MenuTick);
			}
			else if (_hover >= 0)
				WrenchHub.Activate(WrenchHub.Actions[_hover]);
			else if (_quickHover >= 0)
				Toggle(Quicks[_quickHover]);

			WeInput.LockHold(ref _holdLock);
			if (!WeSave.Data.WrenchOpened) {
				WeSave.Data.WrenchOpened = true;
				WeSave.Save();
			}
		}

		internal static void EndFrame() => _frameInput = false;

		private static void Toggle(Quick quick)
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			switch (quick) {
				case Quick.Clean:
					WeSettings.ToggleCleanChrome();
					WeToast.Show(WeSave.Data.CleanChrome ? "ToastCleanOn" : "ToastCleanOff");
					break;
				case Quick.Player:
					WeSettings.SetPlayerWidget(!WeSave.Data.PlayerWidget);
					WeToast.Show(WeSave.Data.PlayerWidget ? "ToastWidgetOn" : "ToastWidgetOff");
					break;
				case Quick.Mute:
					WeSettings.ToggleMuteUnfocused();
					break;
			}
		}

		private static bool QuickOn(Quick quick) => quick switch {
			Quick.Clean => WeSave.Data.CleanChrome,
			Quick.Player => WeSave.Data.PlayerWidget,
			_ => WeSave.Data.MuteWhenUnfocused
		};

		private static string QuickIcon(Quick quick) => quick switch {
			Quick.Clean => WeIcons.Hide,
			Quick.Player => WeIcons.Music,
			_ => WeIcons.Mute
		};

		private static string QuickTip(Quick quick) => quick switch {
			Quick.Clean => "BtnClean",
			Quick.Player => "AddPlayer",
			_ => "MuteUnfocused"
		};

		internal static void Draw(SpriteBatch spriteBatch)
		{
			if (!WeModMenu.OnTitle || !SceneGraph.Visible(SceneGraph.Wrench))
				return;
			WeDraw.WithLinear(spriteBatch, () => Paint(spriteBatch, Bounds(), 1f, interactive: true));
		}

		private static void Paint(SpriteBatch spriteBatch, Rectangle b, float fade, bool interactive)
		{
			bool wide = b.Width >= 120;
			string tip = null;
			Rectangle tipRect = Rectangle.Empty;
			WeTerra.Window(spriteBatch, b, fade);

			Rectangle head = HeadRect(b);
			bool headHover = interactive && _headHover;
			WeTerra.Panel(spriteBatch, head, WeTerra.LookOf(headHover, false), fade);
			bool pointRight = OnLeft == Collapsed;
			WeTerra.Icon(spriteBatch, pointRight ? WeIcons.ChevronRight : WeIcons.ChevronLeft,
				new Vector2(wide ? head.Right - 14 : head.Center.X, head.Center.Y), 16f, Color.White * fade);
			if (wide)
				WeTerra.Label(spriteBatch, WeTerra.Fit(WeText.UI("Wrench"), 0.74f, head.Width - 36), new Vector2(head.X + 10, head.Y + 6), WeTerra.Title * fade, 0.74f);

			for (int i = 0; i < WrenchHub.Actions.Length; i++) {
				WrenchAction action = WrenchHub.Actions[i];
				Rectangle r = RowRect(b, i);
				bool hover = interactive && _hover == i;
				bool on = WrenchHub.IsOn(action);
				WeTerra.Panel(spriteBatch, r, WeTerra.LookOf(hover, on), fade);
				float iconX = wide ? r.X + 18 : r.Center.X;
				WeTerra.Icon(spriteBatch, WrenchHub.IconName(action), new Vector2(iconX, r.Center.Y), 16f, Color.White * fade);
				if (wide) {
					string label = WeTerra.Fit(WeText.UI(WrenchHub.LabelKey(action)), 0.76f, r.Width - 44);
					WeTerra.Label(spriteBatch, label, new Vector2(r.X + 36, r.Y + 7), (hover || on ? Color.White : new Color(232, 232, 242)) * fade, 0.76f);
				}
				else if (hover) {
					tipRect = r;
					tip = WeText.UI(WrenchHub.LabelKey(action));
				}
			}

			if (wide) {
			for (int i = 0; i < Quicks.Length; i++) {
				Rectangle q = QuickRect(b, i);
				bool hover = interactive && _quickHover == i;
				WeTerra.Slot(spriteBatch, q, QuickIcon(Quicks[i]), hover, QuickOn(Quicks[i]), fade);
				if (hover) {
					tipRect = q;
					tip = WeText.UI(QuickTip(Quicks[i]));
				}
			}
			}

			// Tooltips last so later rows never paint over them.
			if (tip != null)
				WeTerra.Tooltip(spriteBatch, tipRect, tip, fade, preferLeft: !OnLeft);
		}

		/// <summary>Small static preview for Settings → Hub style.</summary>
		internal static void DrawPreview(SpriteBatch spriteBatch, Rectangle dest, float fade, bool on)
		{
			int w = Math.Min(dest.Width - 12, 92);
			var b = new Rectangle(dest.X + 6, dest.Y + 4, w, dest.Height - 8);
			WeTerra.Panel(spriteBatch, b, WeTerra.WindowBack * fade, Color.Black * fade);
			int rows = Math.Min(WrenchHub.Actions.Length, Math.Max(1, (b.Height - 10) / 10));
			for (int i = 0; i < rows; i++) {
				var r = new Rectangle(b.X + 4, b.Y + 5 + i * 10, b.Width - 8, 8);
				WeDraw.Fill(spriteBatch, r, (on && i == 1 ? WeAccent.Mid : WeTerra.RowBack) * fade);
				WeDraw.Fill(spriteBatch, new Rectangle(r.X + 2, r.Y + 2, 4, 4), WeAccent.Light * fade);
				WeDraw.Fill(spriteBatch, new Rectangle(r.X + 9, r.Y + 3, Math.Max(4, r.Width - 22 - i * 3), 2), Color.White * (0.7f * fade));
			}
		}
	}
}
