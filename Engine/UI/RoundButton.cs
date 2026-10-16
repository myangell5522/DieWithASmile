using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.UI
{
	internal static class RoundButton
	{
		internal static readonly Color Panel = new Color(29, 27, 32) * 0.86f;

		internal static Rectangle Hit(Vector2 center, float radius) =>
			new((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2f), (int)(radius * 2f));

		internal static bool Hover(Vector2 center, float radius) =>
			Hit(center, radius).Contains(Main.mouseX, Main.mouseY);

		/// <summary>Inventory-slot style button (vanilla rounded panel), the same hit box as before.</summary>
		internal static void Draw(SpriteBatch spriteBatch, Vector2 center, float radius, float alpha, bool active = false)
		{
			bool hover = Hover(center, radius);
			Rectangle box = Hit(center, radius);
			WeTerra.Panel(spriteBatch, box, WeTerra.LookOf(hover, active), alpha);
		}

		internal static void DrawLetter(SpriteBatch spriteBatch, Vector2 center, float radius, string letter, float alpha, bool active = false)
		{
			Draw(spriteBatch, center, radius, alpha, active);
			var font = FontAssets.DeathText.Value;
			float scale = radius / 28f;
			Vector2 size = font.MeasureString(letter) * scale;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch,
				font,
				letter,
				center - size * 0.5f + new Vector2(0f, 2f * scale),
				WeAccent.Icon(Hover(center, radius), active) * alpha,
				0f,
				Vector2.Zero,
				new Vector2(scale));
		}

		internal static void DrawIcon(SpriteBatch spriteBatch, Vector2 center, float radius, Texture2D icon, float rotation, float alpha, bool active = false)
		{
			Draw(spriteBatch, center, radius, alpha, active);
			if (icon == null || icon.IsDisposed)
				return;

			bool hover = Hover(center, radius);
			// Pixel icons keep their own colours; hover just lifts the sprite one pixel like vanilla buttons.
			Vector2 at = hover ? center - new Vector2(0f, 1f) : center;
			WeTerra.Icon(spriteBatch, icon, at, radius * 1.25f, Color.White * alpha, rotation);
		}

		internal static void DrawWrench(SpriteBatch spriteBatch, Vector2 center, float radius, float rotation, float alpha, bool active)
		{
			Draw(spriteBatch, center, radius, alpha, active);
			Texture2D pixel = WeDraw.Pixel;
			Color color = WeAccent.Glyph(Hover(center, radius) || active, active) * alpha;
			Vector2 dir = rotation.ToRotationVector2();
			Vector2 perp = new(-dir.Y, dir.X);
			DrawThick(spriteBatch, pixel, center - dir * radius * 0.42f, center + dir * radius * 0.42f, 5.5f, color);
			Vector2 head = center + dir * radius * 0.28f;
			DrawThick(spriteBatch, pixel, head - perp * radius * 0.28f, head + perp * radius * 0.28f, 4.2f, color);
			DrawThick(spriteBatch, pixel, head + perp * radius * 0.22f, head + perp * radius * 0.22f + dir * radius * 0.18f, 3.4f, color);
			DrawThick(spriteBatch, pixel, head - perp * radius * 0.22f, head - perp * radius * 0.22f + dir * radius * 0.18f, 3.4f, color);
		}

		internal static void DrawThick(SpriteBatch spriteBatch, Texture2D pixel, Vector2 from, Vector2 to, float thickness, Color color)
		{
			Vector2 delta = to - from;
			float length = delta.Length();
			if (length < 0.5f)
				return;

			spriteBatch.Draw(
				pixel,
				from,
				null,
				color,
				delta.ToRotation(),
				new Vector2(0f, 0.5f),
				new Vector2(length / pixel.Width, thickness / pixel.Height),
				SpriteEffects.None,
				0f);
		}

		internal static void Tooltip(SpriteBatch spriteBatch, Vector2 center, float radius, string text, float alpha) =>
			DrawTooltip(spriteBatch, center, radius, text, alpha, Vector2.Zero);

		internal static void TooltipRadial(SpriteBatch spriteBatch, Vector2 center, Vector2 anchor, float radius, string text, float alpha)
		{
			Vector2 dir = center - anchor;
			if (dir.LengthSquared() < 1f)
				dir = new Vector2(0f, -1f);
			else
				dir.Normalize();
			DrawTooltip(spriteBatch, center, radius, text, alpha, dir);
		}

		private static void DrawTooltip(SpriteBatch spriteBatch, Vector2 center, float radius, string text, float alpha, Vector2 dir)
		{
			if (!Hover(center, radius) || string.IsNullOrEmpty(text) || alpha < 0.4f)
				return;

			WeDraw.WithoutClip(spriteBatch, () => PaintTooltip(spriteBatch, center, radius, text, alpha, dir));
		}

		private static void PaintTooltip(SpriteBatch spriteBatch, Vector2 center, float radius, string text, float alpha, Vector2 dir)
		{
			var font = FontAssets.MouseText.Value;
			Vector2 size = font.MeasureString(text) * 0.82f;
			Rectangle rect;
			if (dir.LengthSquared() > 0.01f) {
				Vector2 tip = center + dir * (radius + 12f + size.Length() * 0.12f);
				rect = new Rectangle(
					(int)(tip.X - size.X * 0.5f - 10f),
					(int)(tip.Y - size.Y * 0.5f - 5f),
					(int)size.X + 20,
					(int)size.Y + 10);
			}
			else {
				int width = (int)size.X + 20;
				int height = (int)size.Y + 10;
				int x = (int)(center.X - radius - 16f - width);
				if (x < 8)
					x = (int)(center.X + radius + 16f);
				rect = new Rectangle(x, (int)(center.Y - height * 0.5f), width, height);
			}

			rect.X = (int)MathHelper.Clamp(rect.X, 8, Main.screenWidth - rect.Width - 8);
			rect.Y = (int)MathHelper.Clamp(rect.Y, 8, Main.screenHeight - rect.Height - 8);
			WeTerra.Panel(spriteBatch, rect, new Color(23, 25, 52) * (0.94f * alpha), Color.Black * alpha);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch,
				font,
				text,
				new Vector2(rect.X + 10, rect.Y + 5),
				Color.White * alpha,
				0f,
				Vector2.Zero,
				new Vector2(0.82f));
		}
	}
}
