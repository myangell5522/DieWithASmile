using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.UI
{
	/// <summary>
	/// Terraria-style drawing kit shared by every panel, widget and hub style.
	/// Panels are the vanilla rounded UIPanel textures (PanelBackground + PanelBorder), tinted with the
	/// accent palette; icons are 16x16 pixel art drawn with point sampling at whole-number scales so they
	/// stay crisp like vanilla item sprites.
	/// </summary>
	internal static class WeTerra
	{
		private const int Corner = 12;
		private const int Bar = 4;

		private static Asset<Texture2D> _back;
		private static Asset<Texture2D> _border;

		internal enum Look
		{
			Idle,
			Hover,
			On
		}

		// ---- palette -----------------------------------------------------------------------------

		internal static Color WindowBack => Color.Lerp(new Color(30, 26, 44), WeAccent.Deep, 0.5f) * 0.96f;
		internal static Color WindowBorder => Color.Black;
		internal static Color RowBack => Color.Lerp(new Color(52, 46, 70), WeAccent.Dark, 0.4f) * 0.9f;
		internal static Color RowHover => Color.Lerp(new Color(70, 62, 92), WeAccent.Mid, 0.45f) * 0.95f;
		internal static Color RowOn => Color.Lerp(WeAccent.Mid, new Color(90, 80, 120), 0.2f) * 0.95f;
		internal static Color Well => Color.Lerp(new Color(16, 14, 24), WeAccent.Deep, 0.25f) * 0.92f;
		internal static Color Text => Color.White;
		internal static Color Muted => new(196, 194, 214);
		internal static Color Title => Color.Lerp(WeAccent.Light, Color.White, 0.35f);

		internal static Color BackOf(Look look) => look switch {
			Look.On => RowOn,
			Look.Hover => RowHover,
			_ => RowBack
		};

		internal static Color BorderOf(Look look) => look switch {
			Look.On => WeAccent.Light,
			Look.Hover => Color.Lerp(WeAccent.Light, Color.White, 0.4f),
			_ => Color.Black
		};

		internal static Look LookOf(bool hover, bool on) => on ? Look.On : hover ? Look.Hover : Look.Idle;

		// ---- panels ------------------------------------------------------------------------------

		private static Texture2D BackTex => Tex(ref _back, "Images/UI/PanelBackground");
		private static Texture2D BorderTex => Tex(ref _border, "Images/UI/PanelBorder");

		private static Texture2D Tex(ref Asset<Texture2D> slot, string path)
		{
			try {
				slot ??= Main.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
				Texture2D tex = slot?.Value;
				return tex == null || tex.IsDisposed ? null : tex;
			}
			catch {
				return null;
			}
		}

		/// <summary>A vanilla-looking rounded panel (same 9-slice as UIPanel).</summary>
		internal static void Panel(SpriteBatch sb, Rectangle r, Color back, Color border)
		{
			if (sb == null || r.Width < 4 || r.Height < 4)
				return;

			Texture2D bg = BackTex;
			Texture2D bd = BorderTex;
			if (bg == null || bd == null || r.Width < 8 || r.Height < 8) {
				WeDraw.Fill(sb, r, back);
				if (border.A > 0)
					WeDraw.Border(sb, r, border);
				return;
			}

			if (back.A > 0)
				Slice(sb, bg, r, back);
			if (border.A > 0)
				Slice(sb, bd, r, border);
		}

		internal static void Panel(SpriteBatch sb, Rectangle r, Look look, float fade) =>
			Panel(sb, r, BackOf(look) * fade, BorderOf(look) * fade);

		internal static void Window(SpriteBatch sb, Rectangle r, float fade)
		{
			// soft drop shadow, then the panel
			Panel(sb, new Rectangle(r.X + 4, r.Y + 6, r.Width, r.Height), Color.Black * (0.35f * fade), Color.Transparent);
			Panel(sb, r, WindowBack * fade, WindowBorder * fade);
		}

		internal static void WellBox(SpriteBatch sb, Rectangle r, float fade) =>
			Panel(sb, r, Well * fade, Color.Black * (0.85f * fade));

		private static void Slice(SpriteBatch sb, Texture2D tex, Rectangle r, Color c)
		{
			// Small panels (pills, mini buttons) shrink the corner instead of falling back to a square box.
			int k = Math.Max(2, Math.Min(Corner, Math.Min(r.Width, r.Height) / 2));
			var p = new Point(r.X, r.Y);
			var q = new Point(r.Right - k, r.Bottom - k);
			int w = q.X - p.X - k;
			int h = q.Y - p.Y - k;
			sb.Draw(tex, new Rectangle(p.X, p.Y, k, k), new Rectangle(0, 0, Corner, Corner), c);
			sb.Draw(tex, new Rectangle(q.X, p.Y, k, k), new Rectangle(Corner + Bar, 0, Corner, Corner), c);
			sb.Draw(tex, new Rectangle(p.X, q.Y, k, k), new Rectangle(0, Corner + Bar, Corner, Corner), c);
			sb.Draw(tex, new Rectangle(q.X, q.Y, k, k), new Rectangle(Corner + Bar, Corner + Bar, Corner, Corner), c);
			if (w > 0) {
				sb.Draw(tex, new Rectangle(p.X + k, p.Y, w, k), new Rectangle(Corner, 0, Bar, Corner), c);
				sb.Draw(tex, new Rectangle(p.X + k, q.Y, w, k), new Rectangle(Corner, Corner + Bar, Bar, Corner), c);
			}

			if (h > 0) {
				sb.Draw(tex, new Rectangle(p.X, p.Y + k, k, h), new Rectangle(0, Corner, Corner, Bar), c);
				sb.Draw(tex, new Rectangle(q.X, p.Y + k, k, h), new Rectangle(Corner + Bar, Corner, Corner, Bar), c);
			}

			if (w > 0 && h > 0)
				sb.Draw(tex, new Rectangle(p.X + k, p.Y + k, w, h), new Rectangle(Corner, Corner, Bar, Bar), c);
		}

		// ---- pixel-rounded shapes ----------------------------------------------------------------

		/// <summary>
		/// Rounded rectangle with crisp, stepped pixel corners (no blur), for capsule/island shapes.
		/// A radius of half the height gives a pill.
		/// </summary>
		internal static void PixelRound(SpriteBatch sb, Rectangle r, int radius, Color color)
		{
			if (sb == null || r.Width < 2 || r.Height < 2 || color.A == 0)
				return;
			radius = Math.Clamp(radius, 0, Math.Min(r.Width, r.Height) / 2);
			if (radius <= 1) {
				WeDraw.Fill(sb, r, color);
				return;
			}

			for (int dy = 0; dy < radius; dy++) {
				float yy = radius - dy - 0.5f;
				int inset = (int)MathF.Round(radius - MathF.Sqrt(Math.Max(0f, radius * radius - yy * yy)));
				int w = r.Width - inset * 2;
				if (w <= 0)
					continue;
				WeDraw.Fill(sb, new Rectangle(r.X + inset, r.Y + dy, w, 1), color);
				WeDraw.Fill(sb, new Rectangle(r.X + inset, r.Bottom - 1 - dy, w, 1), color);
			}

			int mid = r.Height - radius * 2;
			if (mid > 0)
				WeDraw.Fill(sb, new Rectangle(r.X, r.Y + radius, r.Width, mid), color);
		}

		/// <summary>Pixel-rounded box with a border of <paramref name="thickness"/> pixels.</summary>
		internal static void PixelRound(SpriteBatch sb, Rectangle r, int radius, Color fill, Color border, int thickness = 2)
		{
			if (border.A > 0 && thickness > 0) {
				PixelRound(sb, r, radius, border);
				r.Inflate(-thickness, -thickness);
				radius = Math.Max(0, radius - thickness);
			}

			PixelRound(sb, r, radius, fill);
		}

		// ---- icons -------------------------------------------------------------------------------

		/// <summary>Whole-number pixel scale for a 16px icon so it never smears.</summary>
		internal static int IconScale(float size) => Math.Max(1, (int)MathF.Round(size / 16f));

		/// <summary>
		/// Draws a pixel icon crisp (point sampling, integer scale, pixel-snapped). Vanilla Terraria art is
		/// always 2 screen pixels per art pixel, so small requests use the 10x10 mini twin at 2x (20px)
		/// instead of the 16px art at 1:1, and larger ones use the 16px art at 2x or more.
		/// </summary>
		internal static void Icon(SpriteBatch sb, Texture2D icon, Vector2 center, float size, Color color, float rotation = 0f)
		{
			if (sb == null || icon == null || icon.IsDisposed)
				return;

			int dim = Math.Max(1, Math.Max(icon.Width, icon.Height));
			if (dim == 16 && size < 24f) {
				Texture2D mini = WeIcons.MiniFor(icon);
				if (mini != null) {
					icon = mini;
					dim = Math.Max(mini.Width, mini.Height);
				}
			}

			float scale;
			if (dim <= 12)
				scale = size >= 30f ? 3f : 2f;
			else if (dim <= 32)
				scale = IconScale(size) * 16f / dim;
			else
				scale = size / dim; // big art from elsewhere just fits
			var pos = new Vector2(MathF.Round(center.X), MathF.Round(center.Y));
			var origin = new Vector2(icon.Width / 2, icon.Height / 2);
			WeDraw.WithPoint(sb, () => sb.Draw(icon, pos, null, color, rotation, origin, scale, SpriteEffects.None, 0f));
		}

		internal static void Icon(SpriteBatch sb, string name, Vector2 center, float size, Color color, float rotation = 0f)
		{
			if (size < 24f) {
				Texture2D mini = WeIcons.GetMini(name);
				if (mini != null) {
					Icon(sb, mini, center, size, color, rotation);
					return;
				}
			}

			Icon(sb, WeIcons.Get(name), center, size, color, rotation);
		}

		/// <summary>An inventory-slot style square button with a pixel icon in the middle.</summary>
		internal static void Slot(SpriteBatch sb, Rectangle r, string icon, bool hover, bool on, float fade)
		{
			Panel(sb, r, LookOf(hover, on), fade);
			Icon(sb, icon, r.Center.ToVector2(), Math.Min(r.Width, r.Height) * 0.62f, Color.White * fade);
		}

		// ---- text --------------------------------------------------------------------------------

		internal static DynamicSpriteFont Font => FontAssets.MouseText.Value;

		/// <summary>
		/// Set while our own windows draw (settings, hub, intro). Text then uses the menu font the player chose,
		/// when "Use in settings" is on and the font has every glyph of the string. Widgets keep the vanilla font.
		/// </summary>
		internal static bool Themed;

		internal static void WithTheme(Action draw)
		{
			bool was = Themed;
			Themed = true;
			try {
				draw();
			}
			finally {
				Themed = was;
			}
		}

		private static bool UseFont(string text) =>
			Themed && WeSave.Data.FontInPanels && !string.IsNullOrEmpty(text) && text.IndexOf('[') < 0 && WeType.Use(text);

		private static float MatchOf(DynamicSpriteFont vanilla) =>
			vanilla.MeasureString("Ag").Y / Math.Max(1f, WeType.Line);

		internal static Vector2 Measure(string text, float scale)
		{
			if (UseFont(text))
				return WeType.Measure(text) * (scale * MatchOf(Font));
			return Font.MeasureString(text ?? "") * scale;
		}

		internal static void Label(SpriteBatch sb, string text, Vector2 pos, Color color, float scale)
		{
			if (string.IsNullOrEmpty(text))
				return;
			if (UseFont(text)) {
				var sc = new Vector2(scale * MatchOf(Font));
				Color shadow = Color.Black * (color.A / 255f);
				float d = 1.5f * Math.Max(0.6f, scale);
				WeType.Draw(sb, text, pos + new Vector2(-d, 0f), shadow, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				WeType.Draw(sb, text, pos + new Vector2(d, 0f), shadow, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				WeType.Draw(sb, text, pos + new Vector2(0f, -d), shadow, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				WeType.Draw(sb, text, pos + new Vector2(0f, d), shadow, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				WeType.Draw(sb, text, pos, color, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				return;
			}

			ChatManager.DrawColorCodedStringWithShadow(sb, Font, text, pos, color, 0f, Vector2.Zero, new Vector2(scale));
		}

		internal static void LabelCentered(SpriteBatch sb, string text, Vector2 center, Color color, float scale)
		{
			Vector2 size = Measure(text, scale);
			Label(sb, text, center - size * 0.5f + new Vector2(0f, 2f * scale), color, scale);
		}

		/// <summary>Trims text with "..." so it fits <paramref name="width"/> pixels at <paramref name="scale"/>.</summary>
		internal static string Fit(string text, float scale, float width)
		{
			if (string.IsNullOrEmpty(text) || Measure(text, scale).X <= width)
				return text ?? "";
			const string dots = "...";
			string t = text;
			while (t.Length > 1 && Measure(t + dots, scale).X > width)
				t = t[..^1];
			return t + dots;
		}

		/// <summary>Big Terraria title text (Andy font, or the chosen menu font) with the vanilla black border.</summary>
		internal static void Headline(SpriteBatch sb, string text, Vector2 pos, Color color, float scale)
		{
			if (string.IsNullOrEmpty(text))
				return;
			if (UseFont(text)) {
				var sc = new Vector2(scale * MatchOf(FontAssets.DeathText.Value));
				Color rim = Color.Black * (color.A / 255f);
				for (int i = 0; i < 4; i++) {
					Vector2 off = (i * MathHelper.PiOver2).ToRotationVector2() * 2f;
					WeType.Draw(sb, text, pos + off, rim, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				}

				WeType.Draw(sb, text, pos, color, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
				return;
			}

			Utils.DrawBorderStringFourWay(sb, FontAssets.DeathText.Value, text, pos.X, pos.Y, color, Color.Black, Vector2.Zero, scale);
		}

		// ---- controls ----------------------------------------------------------------------------

		/// <summary>Slider track + accent fill + vanilla knob.</summary>
		internal static void Slider(SpriteBatch sb, Rectangle bar, float value, bool hover, float fade)
		{
			value = MathHelper.Clamp(value, 0f, 1f);
			var track = new Rectangle(bar.X, bar.Center.Y - 4, bar.Width, 8);
			WeDraw.Fill(sb, new Rectangle(track.X - 2, track.Y - 2, track.Width + 4, track.Height + 4), Color.Black * (0.9f * fade));
			WeDraw.Fill(sb, track, new Color(30, 28, 40) * fade);
			int fill = Math.Max(0, (int)(track.Width * value));
			if (fill > 0) {
				WeDraw.Fill(sb, new Rectangle(track.X, track.Y, fill, track.Height), WeAccent.Mid * fade);
				WeDraw.Fill(sb, new Rectangle(track.X, track.Y, fill, 2), WeAccent.Light * fade);
			}

			Texture2D knob = TextureAssets.ColorSlider?.Value;
			var at = new Vector2(track.X + fill, track.Center.Y);
			if (knob != null)
				sb.Draw(knob, at, null, (hover ? Color.White : new Color(230, 230, 240)) * fade, 0f, knob.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			else
				WeDraw.Fill(sb, new Rectangle((int)at.X - 3, track.Y - 4, 6, track.Height + 8), Color.White * fade);
		}

		/// <summary>Simple scrollbar indicator on the right edge of a scrolling view.</summary>
		internal static void Scrollbar(SpriteBatch sb, Rectangle view, float scroll, float max, float fade)
		{
			if (max < 1f || view.Height < 40)
				return;
			var track = new Rectangle(view.Right - 8, view.Y + 4, 6, view.Height - 8);
			WeDraw.Fill(sb, track, Color.Black * (0.45f * fade));
			float content = view.Height + max;
			int thumbH = Math.Max(24, (int)(track.Height * (view.Height / content)));
			int thumbY = track.Y + (int)((track.Height - thumbH) * MathHelper.Clamp(scroll / max, 0f, 1f));
			WeDraw.Fill(sb, new Rectangle(track.X, thumbY, track.Width, thumbH), WeAccent.Light * (0.85f * fade));
			WeDraw.Fill(sb, new Rectangle(track.X, thumbY, 2, thumbH), Color.White * (0.5f * fade));
		}

		/// <summary>Vanilla-like hover tooltip box.</summary>
		internal static void Tooltip(SpriteBatch sb, Rectangle anchor, string text, float fade, bool preferLeft = false)
		{
			if (string.IsNullOrEmpty(text) || fade < 0.3f)
				return;
			const float scale = 0.85f;
			Vector2 size = Measure(text, scale);
			int w = (int)size.X + 20;
			int h = (int)size.Y + 12;
			int x = preferLeft ? anchor.X - w - 8 : anchor.Right + 8;
			if (x < 6 || x + w > Main.screenWidth - 6)
				x = preferLeft ? anchor.Right + 8 : anchor.X - w - 8;
			int y = anchor.Center.Y - h / 2;
			var rect = new Rectangle(
				Math.Clamp(x, 6, Math.Max(6, Main.screenWidth - w - 6)),
				Math.Clamp(y, 6, Math.Max(6, Main.screenHeight - h - 6)),
				w, h);
			Panel(sb, rect, new Color(23, 25, 52) * (0.94f * fade), Color.Black * fade);
			Label(sb, text, new Vector2(rect.X + 10, rect.Y + 7), Color.White * fade, scale);
		}
	}
}
