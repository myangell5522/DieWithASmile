using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Core
{
	internal static class WeLook
	{
		internal static bool LogoPulse => !WeSave.Data.DisableLogoPulse;

		internal static Color MenuIdle => WeSave.Data.MenuTextCustom
			? new Color(WeSave.Data.MenuTextR, WeSave.Data.MenuTextG, WeSave.Data.MenuTextB)
			: Color.White;

		internal static float FontScaleX => Math.Clamp(WeSave.Data.FontScaleX <= 0.01f ? 1f : WeSave.Data.FontScaleX, 0.5f, 1.8f);
		internal static float FontScaleY => Math.Clamp(WeSave.Data.FontScaleY <= 0.01f ? 1f : WeSave.Data.FontScaleY, 0.5f, 1.8f);

		internal static bool CustomMenuDraw
		{
			get
			{
				WeType.Ensure();
				return WeType.Active || WeSave.Data.ButtonStyle != 0 || TextFx != FxNone ||
				       Math.Abs(FontScaleX - 1f) > 0.01f || Math.Abs(FontScaleY - 1f) > 0.01f;
			}
		}

		// ---- animated text effects (main-menu buttons) ---------------------------------------------
		internal const int FxNone = 0;
		internal const int FxWave = 1;
		internal const int FxShimmer = 2;
		internal const int FxGlow = 3;
		internal const int FxRainbow = 4;
		internal const int FxEmber = 5;
		internal const int FxCount = 6;

		internal static int TextFx => Math.Clamp(WeSave.Data.TextFx, 0, FxCount - 1);

		internal static string FxKey(int fx) => fx switch {
			FxWave => "FxWave",
			FxShimmer => "FxShimmer",
			FxGlow => "FxGlow",
			FxRainbow => "FxRainbow",
			FxEmber => "FxEmber",
			_ => "FxNone"
		};

		private static int _fxDepth;
		private static bool _fxHot;
		private static int? _fxOverride;

		/// <summary>Turns effects on for the strings drawn until <see cref="EndFx"/> (menu buttons only).</summary>
		internal static void BeginFx(bool hot, int? fx = null)
		{
			_fxDepth++;
			_fxHot = hot;
			_fxOverride = fx;
		}

		internal static void EndFx()
		{
			_fxDepth = Math.Max(0, _fxDepth - 1);
			if (_fxDepth == 0)
				_fxOverride = null;
		}

		private static int ActiveFx => _fxDepth > 0 ? Math.Clamp(_fxOverride ?? TextFx, 0, FxCount - 1) : FxNone;

		internal static string ButtonKey() => WeSave.Data.ButtonStyle switch {
			1 => "BtnOutline",
			2 => "BtnAccent",
			3 => "BtnPlate",
			_ => "BtnVanilla"
		};

		internal static void StabilizeLogo(ref float rotation, ref float bounce)
		{
			if (LogoPulse)
				return;
			rotation = 0f;
			bounce = 1f;
		}

		internal static Color Paint(Color fallback, float fade)
		{
			Color src = WeSave.Data.MenuTextCustom ? MenuIdle : fallback;
			return src * fade;
		}

		internal static Color IdleOr(Color color)
		{
			if (!WeSave.Data.MenuTextCustom || color.A < 16)
				return color;
			return new Color(MenuIdle.R, MenuIdle.G, MenuIdle.B, color.A);
		}

		internal static Vector2 MenuScale(float baseScale) =>
			new(baseScale * FontScaleX, baseScale * FontScaleY);

		internal static Vector2 Measure(DynamicSpriteFont vanilla, string text, float baseScale)
		{
			if (string.IsNullOrEmpty(text) || vanilla == null)
				return Vector2.Zero;
			Vector2 scale = MenuScale(baseScale);
			if (WeType.Use(text)) {
				float match = vanilla.MeasureString("Ag").Y / Math.Max(1f, WeType.Line);
				Vector2 m = WeType.Measure(text);
				return new Vector2(m.X * scale.X * match, m.Y * scale.Y * match);
			}

			Vector2 size = vanilla.MeasureString(text);
			return new Vector2(size.X * scale.X, size.Y * scale.Y);
		}

		internal static Vector2 DrawStyled(
			SpriteBatch spriteBatch,
			DynamicSpriteFont vanilla,
			string text,
			Vector2 position,
			Color color,
			float baseScale,
			Vector2 origin,
			float rotation,
			SpriteEffects effects,
			float layerDepth,
			bool borderPass,
			int? styleOverride = null)
		{
			if (string.IsNullOrEmpty(text) || color.A < 8 || vanilla == null)
				return Vector2.Zero;

			Vector2 scale = MenuScale(baseScale);
			bool atlas = WeType.Use(text);
			if (atlas) {
				float match = vanilla.MeasureString("Ag").Y / Math.Max(1f, WeType.Line);
				scale *= match;
				Vector2 oldSize = vanilla.MeasureString(text);
				Vector2 newSize = WeType.Measure(text);
				if (oldSize.X > 0.5f && oldSize.Y > 0.5f && newSize.X > 0.5f)
					origin = new Vector2(origin.X / oldSize.X * newSize.X, origin.Y / oldSize.Y * newSize.Y);
			}

			int style = styleOverride ?? WeSave.Data.ButtonStyle;
			if (style == 3)
				DrawPlate(spriteBatch, vanilla, text, position, origin, scale, color, atlas);

			bool thick = style == 1;
			bool accentRim = style == 2;
			bool vanillaRim = style == 0 && (borderPass || atlas);
			int fx = ActiveFx;
			// Waving letters carry their own outline (drawn per glyph in DrawFx).
			if ((thick || accentRim || vanillaRim) && fx != FxWave) {
				float spread = thick ? 3.4f : 2.2f;
				Color rim = accentRim
					? WeAccent.Mid * (color.A / 255f)
					: Color.Black * (color.A / 255f);
				DrawOutline(spriteBatch, vanilla, text, position, origin, scale, rotation, effects, layerDepth, rim, spread, atlas);
				if (thick)
					DrawOutline(spriteBatch, vanilla, text, position, origin, scale, rotation, effects, layerDepth, rim, 1.6f, atlas);
			}

			if (fx != FxNone) {
				DrawFx(spriteBatch, vanilla, text, position, color, origin, scale, rotation, effects, layerDepth, atlas, fx,
					thick || accentRim || vanillaRim, thick ? 3.4f : 2.2f,
					accentRim ? WeAccent.Mid * (color.A / 255f) : Color.Black * (color.A / 255f), thick);
			}
			else
				DrawRaw(spriteBatch, vanilla, text, position, color, rotation, origin, scale, effects, layerDepth, atlas);
			Vector2 size = atlas ? WeType.Measure(text) : vanilla.MeasureString(text);
			return new Vector2(size.X * scale.X, size.Y * scale.Y);
		}

		private static float Width(DynamicSpriteFont vanilla, string text, bool atlas) =>
			string.IsNullOrEmpty(text) ? 0f : atlas ? WeType.Measure(text).X : vanilla.MeasureString(text).X;

		/// <summary>
		/// Per-glyph effects. Each character is drawn on its own with an offset/colour; glow and embers use
		/// zero-alpha colours, which add light under the premultiplied AlphaBlend state the UI uses.
		/// The outline was already drawn for the whole word, so for moving glyphs it is redrawn per glyph.
		/// </summary>
		private static void DrawFx(
			SpriteBatch spriteBatch, DynamicSpriteFont vanilla, string text, Vector2 position, Color color,
			Vector2 origin, Vector2 scale, float rotation, SpriteEffects effects, float layerDepth, bool atlas, int fx,
			bool rimmed, float spread, Color rim, bool thick)
		{
			float t = Main.GlobalTimeWrappedHourly;
			float amp = _fxHot ? 1.6f : 1f;
			float a = color.A / 255f;
			// Vanilla draws the black outline as separate dark copies of the string: those only move with the
			// wave and never get colour, glow or sparks.
			bool shadowPass = color.R + color.G + color.B < 60;
			float lineH = (atlas ? WeType.Line : vanilla.LineSpacing) * scale.Y;
			int n = text.Length;
			for (int i = 0; i < n; i++) {
				char ch = text[i];
				if (char.IsWhiteSpace(ch))
					continue;
				string glyph = ch.ToString();
				float before = Width(vanilla, text.Substring(0, i), atlas);
				Vector2 o = new(origin.X - before, origin.Y);
				Vector2 at = position;
				Color c = color;
				switch (shadowPass && fx != FxWave ? FxNone : fx) {
					case FxWave:
						at.Y += MathF.Sin(t * 4f - i * 0.55f) * lineH * 0.06f * amp;
						break;
					case FxShimmer: {
						float band = MathF.Max(0f, MathF.Sin(i * 0.42f - t * 3.2f));
						band = band * band * band * band;
						c = Color.Lerp(color, Color.Lerp(WeAccent.Light, Color.White, 0.5f) * a, band * 0.9f * Math.Min(1f, amp));
						break;
					}
					case FxRainbow: {
						float hue = (t * 0.18f + i * 0.055f) % 1f;
						Color rb = Main.hslToRgb(hue, 0.85f, _fxHot ? 0.72f : 0.66f);
						c = Color.Lerp(color, rb * a, 0.85f);
						break;
					}
					case FxEmber: {
						float flick = 0.5f + 0.5f * MathF.Sin(t * 9f + i * 1.7f) * MathF.Sin(t * 5.3f + i * 0.9f);
						c = Color.Lerp(color, new Color(255, 170, 70) * a, 0.25f + 0.35f * flick * Math.Min(1f, amp));
						break;
					}
				}

				if (fx == FxGlow && !shadowPass) {
					float pulse = 0.65f + 0.35f * MathF.Sin(t * 2.4f + i * 0.35f);
					Color halo = WeAccent.Light * (0.32f * pulse * amp * a);
					halo.A = 0;
					float r = 2.6f * Math.Max(0.6f, scale.Y);
					for (int k = 0; k < 8; k++) {
						Vector2 off = (k * MathHelper.PiOver4).ToRotationVector2() * r;
						DrawRaw(spriteBatch, vanilla, glyph, at + off, halo, rotation, o, scale, effects, layerDepth, atlas);
					}
				}

				if (fx == FxWave && rimmed) {
					DrawOutline(spriteBatch, vanilla, glyph, at, o, scale, rotation, effects, layerDepth, rim, spread, atlas);
					if (thick)
						DrawOutline(spriteBatch, vanilla, glyph, at, o, scale, rotation, effects, layerDepth, rim, 1.6f, atlas);
				}

				DrawRaw(spriteBatch, vanilla, glyph, at, c, rotation, o, scale, effects, layerDepth, atlas);

				if (fx == FxEmber && !shadowPass) {
					// two little sparks per letter drifting up and fading
					Vector2 size = new(Width(vanilla, glyph, atlas) * scale.X, lineH);
					Vector2 top = at - new Vector2(o.X * scale.X, o.Y * scale.Y);
					for (int k = 0; k < 2; k++) {
						float seed = (i * 0.618f + k * 0.37f) % 1f;
						float phase = (t * (0.45f + seed * 0.3f) + seed) % 1f;
						float x = top.X + size.X * (0.2f + 0.6f * ((seed * 7.3f) % 1f)) + MathF.Sin(phase * 6f + i) * 2f;
						float y = top.Y + size.Y * 0.35f - phase * lineH * 0.55f * amp;
						Color spark = Color.Lerp(new Color(255, 210, 120), new Color(255, 90, 40), phase) * ((1f - phase) * a);
						spark.A = 0;
						int px = phase < 0.5f ? 2 : 1;
						WeDraw.Fill(spriteBatch, new Rectangle((int)x, (int)y, px, px), spark);
					}
				}
			}
		}

		internal static void DrawPreview(
			SpriteBatch spriteBatch,
			string text,
			Vector2 center,
			Color color,
			float fade,
			float baseScale,
			int? style = null,
			int? fx = null,
			bool hot = false)
		{
			var font = FontAssets.DeathText.Value;
			if (font == null || string.IsNullOrEmpty(text))
				return;
			Vector2 origin = font.MeasureString(text) * 0.5f;
			BeginFx(hot, fx);
			try {
				DrawStyled(
					spriteBatch, font, text, center, color * fade, baseScale, origin,
					0f, SpriteEffects.None, 0f, true, style);
			}
			finally {
				EndFx();
			}
		}

		private static void DrawRaw(
			SpriteBatch spriteBatch,
			DynamicSpriteFont vanilla,
			string text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float layerDepth,
			bool atlas)
		{
			if (atlas) {
				WeType.Draw(spriteBatch, text, position, color, rotation, origin, scale, effects, layerDepth);
				return;
			}

			spriteBatch.DrawString(vanilla, text, position, color, rotation, origin, scale, effects, layerDepth);
		}

		private static void DrawOutline(
			SpriteBatch spriteBatch,
			DynamicSpriteFont vanilla,
			string text,
			Vector2 position,
			Vector2 origin,
			Vector2 scale,
			float rotation,
			SpriteEffects effects,
			float layerDepth,
			Color rim,
			float spread,
			bool atlas)
		{
			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if (x == 0 && y == 0)
						continue;
					DrawRaw(
						spriteBatch, vanilla, text, position + new Vector2(x, y) * spread, rim,
						rotation, origin, scale, effects, layerDepth, atlas);
				}
			}
		}

		private static void DrawPlate(
			SpriteBatch spriteBatch,
			DynamicSpriteFont vanilla,
			string text,
			Vector2 position,
			Vector2 origin,
			Vector2 scale,
			Color color,
			bool atlas)
		{
			Vector2 size = atlas ? WeType.Measure(text) : vanilla.MeasureString(text);
			Vector2 scaled = new(size.X * scale.X, size.Y * scale.Y);
			Vector2 topLeft = position - new Vector2(origin.X * scale.X, origin.Y * scale.Y);
			var plate = new Rectangle(
				(int)MathF.Floor(topLeft.X - 16f),
				(int)MathF.Floor(topLeft.Y - 5f),
				Math.Max(8, (int)MathF.Ceiling(scaled.X + 32f)),
				Math.Max(8, (int)MathF.Ceiling(scaled.Y + 10f)));
			float a = color.A / 255f;
			WeDraw.Fill(spriteBatch, plate, new Color(12, 14, 20) * (0.72f * a));
			WeDraw.Border(spriteBatch, plate, WeAccent.Mid * a);
		}
	}
}
