using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Localization;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Chrome
{
	/// <summary>
	/// While our theme is selected, tModLoader's "Switch Menu Theme: ..." line at the bottom of the title
	/// screen gets a living treatment: the theme name runs through a crimson → rose → gold gradient, a shine
	/// sweeps across it, letters breathe, and embers and four-point sparkles drift off it over a soft glow.
	/// Everything is drawn per glyph in the normal UI batch (zero-alpha colours add light under the
	/// premultiplied AlphaBlend state), so it needs no compiled shader and behaves the same on every OS.
	/// Layout and the click area stay exactly vanilla's: only the drawing changes.
	/// </summary>
	internal static class WeThemeLine
	{
		private static readonly Color Crimson = new(170, 24, 52);
		private static readonly Color Rose = new(255, 96, 136);
		private static readonly Color Gold = new(255, 206, 120);

		private static string _prefixLang = "";
		private static string _prefix = "";

		private static string Prefix
		{
			get
			{
				string lang = Language.ActiveCulture?.Name ?? "";
				if (lang != _prefixLang || _prefix.Length == 0) {
					_prefixLang = lang;
					_prefix = Language.GetTextValue("tModLoader.ModMenuSwap") + ": ";
				}

				return _prefix;
			}
		}

		internal static bool TryDraw(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 pos, Color color, Vector2 scale, out Vector2 size)
		{
			size = Vector2.Zero;
			if (sb == null || font == null || string.IsNullOrEmpty(text) || !Main.gameMenu || Main.menuMode != 0)
				return false;
			if (!WeModMenu.IsActive || WeSave.Data.PlainThemeLine)
				return false;
			string prefix = Prefix;
			if (prefix.Length < 3 || !text.StartsWith(prefix, StringComparison.Ordinal) || text.Length <= prefix.Length)
				return false;

			try {
				size = font.MeasureString(text) * scale;
				Paint(sb, font, prefix, text.Substring(prefix.Length), pos, color, scale.X);
				return true;
			}
			catch {
				return false;
			}
		}

		private static Color Gradient(float p)
		{
			p -= MathF.Floor(p);
			// crimson → rose → gold → rose → crimson, so the loop has no seam
			if (p < 0.25f)
				return Color.Lerp(Crimson, Rose, p / 0.25f);
			if (p < 0.5f)
				return Color.Lerp(Rose, Gold, (p - 0.25f) / 0.25f);
			if (p < 0.75f)
				return Color.Lerp(Gold, Rose, (p - 0.5f) / 0.25f);
			return Color.Lerp(Rose, Crimson, (p - 0.75f) / 0.25f);
		}

		private static float Hash(float x)
		{
			float h = MathF.Sin(x * 127.1f) * 43758.5453f;
			return h - MathF.Floor(h);
		}

		private static Color Add(Color c, float a)
		{
			c *= a;
			c.A = 0;
			return c;
		}

		private static void Paint(SpriteBatch sb, DynamicSpriteFont font, string prefix, string name, Vector2 pos, Color vanillaColor, float scale)
		{
			float t = Main.GlobalTimeWrappedHourly;
			Vector2 prefixSize = font.MeasureString(prefix) * scale;
			Vector2 nameSize = font.MeasureString(name) * scale;
			var full = new Rectangle((int)pos.X, (int)pos.Y, (int)(prefixSize.X + nameSize.X), (int)nameSize.Y);
			bool hot = full.Contains(Main.mouseX, Main.mouseY);
			float energy = hot ? 1f : 0.62f;

			// "Switch Menu Theme:" stays calm and readable
			Color prefixColor = hot ? Color.White : new Color(200, 196, 214);
			DrawShadowed(sb, font, prefix, pos, prefixColor * 0.92f, scale);

			Vector2 start = pos + new Vector2(prefixSize.X, 0f);
			Vector2 mid = start + nameSize * 0.5f;

			// soft glow pooled under the name
			Texture2D circle = WeDraw.Circle();
			float pulse = 0.75f + 0.25f * MathF.Sin(t * 1.8f);
			var glowScale = new Vector2((nameSize.X + 60f) / circle.Width, (nameSize.Y + 18f) / circle.Height);
			sb.Draw(circle, mid + new Vector2(0f, 2f), null, Add(Crimson, 0.22f * pulse * energy), 0f, circle.Size() * 0.5f, glowScale, SpriteEffects.None, 0f);
			sb.Draw(circle, mid + new Vector2(0f, 2f), null, Add(Rose, 0.12f * pulse * energy), 0f, circle.Size() * 0.5f, glowScale * new Vector2(0.65f, 0.6f), SpriteEffects.None, 0f);

			// shine band position (0..1 across the name, then a pause)
			float cycle = (t * 0.32f) % 1.6f;
			float shineAt = cycle - 0.2f;

			float x = 0f;
			int n = name.Length;
			for (int i = 0; i < n; i++) {
				string ch = name[i].ToString();
				float w = font.MeasureString(ch).X * scale;
				if (char.IsWhiteSpace(name[i])) {
					x += w;
					continue;
				}

				float u = n <= 1 ? 0f : i / (float)(n - 1);
				float bob = MathF.Sin(t * 2.6f - i * 0.45f) * (hot ? 1.8f : 1.1f);
				var at = new Vector2(start.X + x, start.Y + bob);
				Color c = Gradient(u * 0.8f - t * 0.12f);
				float shine = 1f - MathHelper.Clamp(MathF.Abs(u - shineAt) / 0.08f, 0f, 1f);
				c = Color.Lerp(c, Color.White, shine * 0.85f);

				// halo copies around each glyph
				Color halo = Add(Color.Lerp(c, Rose, 0.4f), (0.18f + 0.3f * shine) * energy);
				for (int k = 0; k < 4; k++) {
					Vector2 off = (k * MathHelper.PiOver2 + MathHelper.PiOver4).ToRotationVector2() * 2.2f;
					sb.DrawString(font, ch, at + off, halo, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				}

				DrawShadowed(sb, font, ch, at, c, scale);
				x += w;
			}

			// embers drifting up off the letters
			int embers = hot ? 14 : 8;
			for (int e = 0; e < embers; e++) {
				float seed = Hash(e * 3.17f + 1.3f);
				float speed = 0.22f + seed * 0.25f;
				float phase = (t * speed + seed) % 1f;
				float ex = start.X + nameSize.X * Hash(e * 7.7f + MathF.Floor(t * speed + seed) * 1.31f);
				float ey = start.Y + nameSize.Y * 0.55f - phase * (nameSize.Y * 1.4f + 8f);
				ex += MathF.Sin(phase * 7f + e) * 3f;
				Color ember = Color.Lerp(Gold, Crimson, phase);
				int px = phase < 0.45f ? 2 : 1;
				WeDraw.Fill(sb, new Rectangle((int)ex, (int)ey, px, px), Add(ember, (1f - phase) * 0.9f * energy));
			}

			// four-point sparkles that bloom and fade at random spots around the name
			for (int k = 0; k < 3; k++) {
				float slot = MathF.Floor(t * 0.9f + k * 0.33f);
				float life = (t * 0.9f + k * 0.33f) - slot;
				float sx = start.X + nameSize.X * Hash(slot * 1.7f + k * 11.1f);
				float sy = start.Y + nameSize.Y * (0.1f + 0.8f * Hash(slot * 2.3f + k * 5.3f));
				float bloom = MathF.Sin(life * MathHelper.Pi);
				DrawSparkle(sb, new Vector2(sx, sy), 2f + 5f * bloom * (hot ? 1.3f : 1f), Add(Color.Lerp(Gold, Color.White, 0.5f), bloom * energy));
			}

			if (hot) {
				// thin glowing underline when the line is hovered (it is clickable)
				int uy = (int)(start.Y + nameSize.Y - 3f);
				WeDraw.Fill(sb, new Rectangle((int)start.X, uy, (int)nameSize.X, 1), Add(Rose, 0.7f));
				WeDraw.Fill(sb, new Rectangle((int)start.X, uy + 1, (int)nameSize.X, 1), Add(Crimson, 0.4f));
			}
		}

		private static void DrawSparkle(SpriteBatch sb, Vector2 c, float r, Color color)
		{
			int len = Math.Max(1, (int)r);
			WeDraw.Fill(sb, new Rectangle((int)c.X - len, (int)c.Y, len * 2 + 1, 1), color);
			WeDraw.Fill(sb, new Rectangle((int)c.X, (int)c.Y - len, 1, len * 2 + 1), color);
			if (len >= 3) {
				WeDraw.Fill(sb, new Rectangle((int)c.X - 1, (int)c.Y - 1, 3, 3), color * 0.8f);
			}
		}

		private static void DrawShadowed(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 pos, Color color, float scale)
		{
			Color shadow = Color.Black * (color.A / 255f * 0.9f);
			for (int k = 0; k < 4; k++) {
				Vector2 off = (k * MathHelper.PiOver2).ToRotationVector2() * 1.5f;
				sb.DrawString(font, text, pos + off, shadow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}

			sb.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}
	}
}
