using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Chrome
{
	/// <summary>
	/// "Bar" hub style, drawn as a Dynamic-Island capsule: a dark pixel-rounded pill with the section icons
	/// at vanilla pixel density (2x). Hovering morphs it open - it grows away from the nearest screen edge and
	/// spells out the hovered section's name and what it does, so nothing has to be guessed from an icon.
	/// </summary>
	internal static class WrenchDock
	{
		private const float Slot = 46f;
		private const float Pad = 12f;
		private const float RestH = 50f;
		private const float OpenH = 98f;
		private const float Grow = 28f;

		private static float _focus;
		private static float _open;
		private static int _hover = -1;
		private static bool _frameInput;
		private static bool _mouseHeld;
		private static bool _holdLock;

		internal static bool Busy => HoverBar() || _hover >= 0;

		internal static Rectangle HitRect() => Island(_open, WrenchToolbar.Scale);

		internal static void Reset()
		{
			_hover = -1;
			_focus = 0f;
			_open = 0f;
		}

		internal static void Update()
		{
			if (!WeModMenu.OnTitle) {
				_hover = -1;
				_open = 0f;
				return;
			}

			float target = _hover >= 0 ? 1f : 0f;
			_open = MathHelper.Lerp(_open, target, target > _open ? 0.24f : 0.16f);
			if (Math.Abs(_open - target) < 0.01f)
				_open = target;
			if (_hover >= 0)
				_focus = MathHelper.Lerp(_focus, _hover, 0.35f);
		}

		internal static void HandleInput()
		{
			if (_frameInput || LayoutEditor.Editing || WeSplash.Visible)
				return;

			_frameInput = true;
			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			if (!WeModMenu.OnTitle)
				return;

			if (WePanels.Covering) {
				_hover = -1;
				Main.blockMouse = true;
				return;
			}

			_hover = HitIndex();
			if (_hover >= 0 || HoverBar())
				Main.blockMouse = true;

			if (!pressed || _hover < 0)
				return;

			WrenchHub.Activate(WrenchHub.Actions[_hover]);
			WeInput.LockHold(ref _holdLock);
			if (!WeSave.Data.WrenchOpened) {
				WeSave.Data.WrenchOpened = true;
				WeSave.Save();
			}
		}

		internal static void EndFrame() => _frameInput = false;

		internal static void Draw(SpriteBatch spriteBatch)
		{
			if (!WeModMenu.OnTitle || !SceneGraph.Visible(SceneGraph.Wrench))
				return;

			WeDraw.WithLinear(spriteBatch, () => WeTerra.WithTheme(() =>
				Paint(spriteBatch, Island(_open, WrenchToolbar.Scale), _open, _focus, 1f, true, GrowsDown)));
		}

		/// <summary>Static preview for Settings → Hub style.</summary>
		internal static void DrawPreview(SpriteBatch spriteBatch, Rectangle dest, float fade, bool on)
		{
			float open = on ? 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f) : 0f;
			open = MathHelper.Clamp(open, 0f, 1f);
			int n = WrenchHub.Actions.Length;
			float natural = n * Slot + Pad * 2f;
			float s = Math.Min((dest.Width - 8f) / (natural + Grow), (dest.Height - 6f) / OpenH);
			s = Math.Max(0.2f, s);
			float w = (natural + Grow * open) * s;
			float h = MathHelper.Lerp(RestH, OpenH, open) * s;
			var r = new Rectangle((int)(dest.Center.X - w * 0.5f), dest.Y + 3, (int)w, (int)h);
			Paint(spriteBatch, r, open, 2f, fade, false, true, s);
		}

		/// <summary>True when the island should open downward (it sits in the top half of the screen).</summary>
		private static bool GrowsDown => WrenchToolbar.Anchor.Y < Main.screenHeight * 0.5f;

		private static Rectangle Island(float open, float scale)
		{
			int n = WrenchHub.Actions.Length;
			float rest = (n * Slot + Pad * 2f) * scale;
			float w = rest + Grow * scale * Smooth(open);
			float h = MathHelper.Lerp(RestH, OpenH, Smooth(open)) * scale;
			Vector2 anchor = WrenchToolbar.Anchor;
			float restTop = anchor.Y - RestH * scale * 0.5f;
			float top = GrowsDown ? restTop : restTop + RestH * scale - h;
			return new Rectangle((int)(anchor.X - w * 0.5f), (int)top, (int)w, (int)h);
		}

		private static void Paint(SpriteBatch spriteBatch, Rectangle island, float open, float focus, float fade, bool interactive, bool down, float scale = -1f)
		{
			if (scale < 0f)
				scale = WrenchToolbar.Scale;
			float o = Smooth(open);
			int n = WrenchHub.Actions.Length;
			float restH = RestH * scale;
			int radius = (int)MathHelper.Lerp(restH * 0.5f, 22f * scale, o);

			// soft shadow, dark rim, glass body, accent hairline
			WeTerra.PixelRound(spriteBatch, new Rectangle(island.X + 2, island.Y + 4, island.Width, island.Height), radius, Color.Black * (0.35f * fade));
			WeTerra.PixelRound(spriteBatch, island, radius, new Color(10, 8, 16) * (0.94f * fade), Color.Black * fade, 2);
			var inner = new Rectangle(island.X + 2, island.Y + 2, island.Width - 4, island.Height - 4);
			WeTerra.PixelRound(spriteBatch, inner, Math.Max(0, radius - 2), Color.Transparent, WeAccent.Mid * ((0.45f + 0.35f * o) * fade), 1);
			WeDraw.Fill(spriteBatch, new Rectangle(island.X + radius, island.Y + 3, Math.Max(1, island.Width - radius * 2), 1), Color.White * (0.07f * fade));

			// icon row sits at the resting height, at the edge the island grows away from
			float rowY = down ? island.Y + restH * 0.5f : island.Bottom - restH * 0.5f;
			float slot = Slot * scale;
			float rowX = island.Center.X - n * slot * 0.5f;
			for (int i = 0; i < n; i++) {
				WrenchAction action = WrenchHub.Actions[i];
				var center = new Vector2(rowX + slot * (i + 0.5f), rowY);
				float near = o * Smooth(1f - Math.Abs(i - focus));
				bool hot = interactive && i == _hover;
				bool on = WrenchHub.IsOn(action);
				if (near > 0.05f || hot) {
					int pill = (int)(slot - 6f * scale);
					var bg = new Rectangle((int)(center.X - pill * 0.5f), (int)(center.Y - pill * 0.5f), pill, pill);
					WeTerra.PixelRound(spriteBatch, bg, pill / 3, WeTerra.RowHover * (Math.Max(near, hot ? 0.8f : 0f) * fade));
				}

				Vector2 at = hot ? center - new Vector2(0f, 2f) : center;
				// vanilla pixel density: 16px art drawn at 2x (1x only in tiny previews)
				WeTerra.Icon(spriteBatch, WrenchHub.IconName(action), at, scale >= 0.75f ? 32f : 16f, Color.White * fade);
				if (on) {
					var dot = new Rectangle((int)center.X - 2, (int)(center.Y + slot * 0.42f), 4, 2);
					WeDraw.Fill(spriteBatch, dot, WeAccent.Light * fade);
				}
			}

			// the morph: name + what it does, fading in as the island opens
			if (!interactive || o < 0.35f || _hover < 0)
				return;
			float textFade = MathHelper.Clamp((o - 0.35f) / 0.5f, 0f, 1f) * fade;
			WrenchAction cur = WrenchHub.Actions[Math.Clamp(_hover, 0, n - 1)];
			float textTop = down ? island.Y + restH + 2f : island.Y + 8f * scale;
			float maxW = island.Width - 28f * scale;
			string title = WeTerra.Fit(WeText.UI(WrenchHub.LabelKey(cur)), 0.86f, maxW);
			string tip = WeTerra.Fit(WeText.UI(WrenchHub.TipKey(cur)), 0.66f, maxW);
			WeTerra.LabelCentered(spriteBatch, title, new Vector2(island.Center.X, textTop + 9f), WeTerra.Title * textFade, 0.86f);
			if (tip != title)
				WeTerra.LabelCentered(spriteBatch, tip, new Vector2(island.Center.X, textTop + 30f), WeTerra.Muted * textFade, 0.66f);
		}

		private static float Smooth(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		// The hit area is the fully open island, so moving the mouse onto the label never collapses it.
		private static bool HoverBar() =>
			(_open > 0.05f ? Island(1f, WrenchToolbar.Scale) : Island(0f, WrenchToolbar.Scale)).Contains(Main.mouseX, Main.mouseY);

		private static int HitIndex()
		{
			float scale = WrenchToolbar.Scale;
			Rectangle rest = Island(0f, scale);
			Rectangle full = Island(1f, scale);
			int n = WrenchHub.Actions.Length;
			float slot = Slot * scale;
			float rowX = rest.Center.X - n * slot * 0.5f;
			float restH = RestH * scale;
			float rowTop = GrowsDown ? rest.Y : rest.Bottom - restH;
			bool inRow = Main.mouseY >= rowTop && Main.mouseY <= rowTop + restH &&
			             Main.mouseX >= rowX && Main.mouseX < rowX + n * slot;
			if (inRow)
				return Math.Clamp((int)((Main.mouseX - rowX) / slot), 0, n - 1);

			// Over the open label area: keep the current section highlighted.
			if (_hover >= 0 && full.Contains(Main.mouseX, Main.mouseY))
				return _hover;
			return -1;
		}
	}
}
