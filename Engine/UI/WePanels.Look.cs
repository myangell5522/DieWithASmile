using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.UI
{
	/// <summary>What a list row does, so it can show the right affordance on its right edge.</summary>
	internal enum CardKind
	{
		Toggle, // on/off switch: checkbox icon
		Radio,  // one of several: check mark when chosen
		Action, // runs something: chevron
		Cycle   // steps through values: loop arrows
	}

	internal static partial class WePanels
	{
		/// <summary>Terraria panel for a row/card in idle, hover or selected state.</summary>
		private static void Card(SpriteBatch spriteBatch, Rectangle hit, bool hover, bool on) =>
			WeTerra.Panel(spriteBatch, hit, WeTerra.LookOf(hover, on), _fade);

		/// <summary>Dark inset box with an image fitted inside (letterboxed, or cropped when <paramref name="cover"/>).</summary>
		private static void Thumb(SpriteBatch spriteBatch, Rectangle box, Texture2D tex, bool cover = false, float alpha = 1f)
		{
			WeDraw.Fill(spriteBatch, new Rectangle(box.X - 2, box.Y - 2, box.Width + 4, box.Height + 4), Color.Black * (0.85f * _fade));
			WeDraw.Fill(spriteBatch, box, new Color(14, 12, 20) * _fade);
			if (tex == null || tex.IsDisposed)
				return;
			if (cover) {
				WeDraw.DrawCover(spriteBatch, tex, box, Color.White * (alpha * _fade));
				return;
			}

			float scale = Math.Min(box.Width / (float)Math.Max(1, tex.Width), box.Height / (float)Math.Max(1, tex.Height));
			spriteBatch.Draw(tex, box.Center.ToVector2(), null, Color.White * (alpha * _fade), 0f, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		}

		/// <summary>Right-edge affordance for a row: checkbox, check mark, chevron or cycle arrows.</summary>
		private static void Affordance(SpriteBatch spriteBatch, Rectangle hit, CardKind kind, bool on)
		{
			string icon = kind switch {
				CardKind.Toggle => on ? WeIcons.ToggleOn : WeIcons.ToggleOff,
				CardKind.Radio => on ? WeIcons.Check : null,
				CardKind.Cycle => WeIcons.Loop,
				_ => WeIcons.ChevronRight
			};
			if (icon == null)
				return;
			WeTerra.Icon(spriteBatch, icon, new Vector2(hit.Right - 20, hit.Center.Y), 16f, Color.White * _fade);
		}

		/// <summary>Title + muted second line, both trimmed to the row.</summary>
		private static void TwoLine(SpriteBatch spriteBatch, int x, int y, int width, string title, string sub, float alpha = 1f)
		{
			WeTerra.Label(spriteBatch, WeTerra.Fit(title, 0.78f, width), new Vector2(x, y), Color.White * (alpha * _fade), 0.78f);
			if (!string.IsNullOrEmpty(sub))
				WeTerra.Label(spriteBatch, WeTerra.Fit(sub, 0.66f, width), new Vector2(x, y + 21), WeTerra.Muted * (alpha * _fade), 0.66f);
		}

		/// <summary>Section heading with a thin accent rule, used to break long pages into groups.</summary>
		private static void Heading(SpriteBatch spriteBatch, Rectangle panel, int y, string text)
		{
			WeTerra.Label(spriteBatch, text, new Vector2(panel.X + 18, y + 2), WeTerra.Title * _fade, 0.8f);
			float w = WeTerra.Measure(text, 0.8f).X;
			int x = panel.X + 26 + (int)w;
			int right = panel.Right - 20;
			if (right - x > 8) {
				WeDraw.Fill(spriteBatch, new Rectangle(x, y + 12, right - x, 2), WeAccent.Mid * (0.7f * _fade));
				WeDraw.Fill(spriteBatch, new Rectangle(x, y + 14, right - x, 1), Color.Black * (0.6f * _fade));
			}
		}
	}
}
