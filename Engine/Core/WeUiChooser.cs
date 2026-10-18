using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;
using C = DieWithASmile.Engine.Classic;

namespace DieWithASmile.Engine
{
	/// <summary>
	/// One-time question on the title screen: keep the Classic interface (up to 3.0.24) or the new Pixel one.
	/// Each card shows a small live sample drawn by that interface's own code. The answer is saved and can be
	/// changed any time in Settings. Escape picks Classic, the safe default for people who just want it gone.
	/// </summary>
	internal static class WeUiChooser
	{
		private static float _fade;
		private static bool _mouseHeld;
		private static bool _holdLock;
		private static int _hover = -1;

		internal static bool Visible => (ShouldShow || _fade > 0.02f) && WeModMenu.OnTitle;

		private static bool ShouldShow => !WeUi.Chosen && WeModMenu.OnTitle && Main.gameMenu;

		internal static void Update()
		{
			_fade = MathHelper.Lerp(_fade, ShouldShow ? 1f : 0f, 0.2f);
			if (!ShouldShow && _fade < 0.02f)
				_fade = 0f;
		}

		/// <summary>Returns true while the chooser owns the mouse and keyboard.</summary>
		internal static bool HandleInput(bool escPressed)
		{
			if (!ShouldShow)
				return false;

			Main.blockMouse = true;
			if (escPressed) {
				Pick(WeUi.SkinClassic);
				return true;
			}

			Rectangle card = Card();
			Rectangle[] cells = { Cell(card, 0), Cell(card, 1) };
			_hover = -1;
			for (int i = 0; i < 2; i++) {
				if (cells[i].Contains(Main.mouseX, Main.mouseY))
					_hover = i;
			}

			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			if (pressed && _hover >= 0) {
				Pick(_hover == 0 ? WeUi.SkinClassic : WeUi.SkinPixel);
				WeInput.LockHold(ref _holdLock);
			}

			return true;
		}

		private static void Pick(int skin)
		{
			SoundEngine.PlaySound(SoundID.MenuOpen);
			WeUi.SetSkin(skin);
			// the normal intro card follows, now in the chosen style
			WeUi.SplashOnThemeSelected();
		}

		private static Rectangle Card()
		{
			int w = Math.Min(760, Main.screenWidth - 60);
			int h = Math.Min(430, Main.screenHeight - 60);
			return new Rectangle((Main.screenWidth - w) / 2, (Main.screenHeight - h) / 2, w, h);
		}

		private static Rectangle Cell(Rectangle card, int i)
		{
			int gap = 18;
			int w = (card.Width - 56 - gap) / 2;
			int top = card.Y + 104;
			return new Rectangle(card.X + 28 + i * (w + gap), top, w, card.Bottom - 30 - top);
		}

		internal static void Draw(SpriteBatch sb)
		{
			if (_fade <= 0.01f || !WeModMenu.OnTitle)
				return;
			float f = _fade;
			WeDraw.WithLinear(sb, () => {
				WeDraw.Fill(sb, WeDraw.CoverRect, Color.Black * (0.7f * f));
				Rectangle card = Card();
				WeDraw.Fill(sb, new Rectangle(card.X + 4, card.Y + 6, card.Width, card.Height), Color.Black * (0.35f * f));
				WeDraw.Fill(sb, card, new Color(20, 22, 28) * (0.97f * f));
				WeDraw.Border(sb, card, WeAccent.Mid * f);

				var font = FontAssets.MouseText.Value;
				Text(sb, font, WeText.UI("UiChooseTitle"), new Vector2(card.X + 28, card.Y + 22), WeAccent.Light * f, 1.1f);
				Text(sb, font, Fit(font, WeText.UI("UiChooseSub"), card.Width - 56, 0.8f), new Vector2(card.X + 28, card.Y + 60), new Color(210, 210, 222) * f, 0.8f);

				DrawClassicCell(sb, font, Cell(card, 0), f);
				DrawPixelCell(sb, font, Cell(card, 1), f);
			});
		}

		private static void DrawClassicCell(SpriteBatch sb, ReLogic.Graphics.DynamicSpriteFont font, Rectangle cell, float f)
		{
			bool hover = _hover == 0;
			WeDraw.Fill(sb, cell, (hover ? new Color(34, 38, 48) : new Color(26, 28, 36)) * f);
			WeDraw.Border(sb, cell, (hover ? WeAccent.Light : WeAccent.Mid * 0.7f) * f);

			// sample: the classic round hub buttons with the original smooth icons
			var preview = new Rectangle(cell.X + 14, cell.Y + 14, cell.Width - 28, 120);
			WeDraw.Fill(sb, preview, new Color(12, 13, 18) * f);
			string[] icons = { C.WeIcons.Wallpaper, C.WeIcons.Logo, C.WeIcons.Music, C.WeIcons.Widget };
			float step = preview.Width / (icons.Length + 1f);
			for (int i = 0; i < icons.Length; i++) {
				var at = new Vector2(preview.X + step * (i + 1), preview.Center.Y);
				Texture2D tex = C.WeIcons.Get(icons[i]);
				if (tex != null)
					C.RoundButton.DrawIcon(sb, at, 22f, tex, 0f, f, i == 1);
				else
					C.RoundButton.Draw(sb, at, 22f, f, i == 1);
			}

			DrawLabels(sb, font, cell, preview.Bottom, WeText.UI("UiClassic"), WeText.UI("UiClassicDesc"), hover, f);
		}

		private static void DrawPixelCell(SpriteBatch sb, ReLogic.Graphics.DynamicSpriteFont font, Rectangle cell, float f)
		{
			bool hover = _hover == 1;
			WeDraw.Fill(sb, cell, (hover ? new Color(34, 38, 48) : new Color(26, 28, 36)) * f);
			WeDraw.Border(sb, cell, (hover ? WeAccent.Light : WeAccent.Mid * 0.7f) * f);

			// sample: Terraria panels with the pixel-art icons
			var preview = new Rectangle(cell.X + 14, cell.Y + 14, cell.Width - 28, 120);
			WeDraw.Fill(sb, preview, new Color(12, 13, 18) * f);
			string[] icons = { WeIcons.Wallpaper, WeIcons.Logo, WeIcons.Music, WeIcons.Widget };
			float step = preview.Width / (icons.Length + 1f);
			for (int i = 0; i < icons.Length; i++) {
				var at = new Vector2(preview.X + step * (i + 1), preview.Center.Y);
				var slot = new Rectangle((int)at.X - 22, (int)at.Y - 22, 44, 44);
				WeTerra.Slot(sb, slot, icons[i], false, i == 1, f);
			}

			DrawLabels(sb, font, cell, preview.Bottom, WeText.UI("UiPixel"), WeText.UI("UiPixelDesc"), hover, f);
		}

		private static void DrawLabels(SpriteBatch sb, ReLogic.Graphics.DynamicSpriteFont font, Rectangle cell, int top, string title, string desc, bool hover, float f)
		{
			Text(sb, font, Fit(font, title, cell.Width - 28, 0.95f), new Vector2(cell.X + 14, top + 14), (hover ? Color.White : new Color(236, 236, 244)) * f, 0.95f);
			string[] lines = Wrap(font, desc, cell.Width - 28, 0.74f);
			for (int i = 0; i < lines.Length && i < 3; i++)
				Text(sb, font, lines[i], new Vector2(cell.X + 14, top + 44 + i * 20), new Color(196, 196, 210) * f, 0.74f);

			var button = new Rectangle(cell.X + 14, cell.Bottom - 44, cell.Width - 28, 32);
			WeDraw.Fill(sb, button, (hover ? WeAccent.Mid : new Color(36, 40, 50)) * f);
			WeDraw.Border(sb, button, WeAccent.Light * (hover ? 1f : 0.6f) * f);
			string pick = WeText.UI("UiPick");
			Vector2 size = font.MeasureString(pick) * 0.82f;
			Text(sb, font, pick, new Vector2(button.Center.X - size.X * 0.5f, button.Center.Y - size.Y * 0.5f + 2f), Color.White * f, 0.82f);
		}

		private static void Text(SpriteBatch sb, ReLogic.Graphics.DynamicSpriteFont font, string text, Vector2 pos, Color color, float scale) =>
			ChatManager.DrawColorCodedStringWithShadow(sb, font, text ?? "", pos, color, 0f, Vector2.Zero, new Vector2(scale));

		private static string Fit(ReLogic.Graphics.DynamicSpriteFont font, string text, float width, float scale)
		{
			if (string.IsNullOrEmpty(text) || font.MeasureString(text).X * scale <= width)
				return text ?? "";
			string t = text;
			while (t.Length > 1 && font.MeasureString(t + "...").X * scale > width)
				t = t[..^1];
			return t + "...";
		}

		private static string[] Wrap(ReLogic.Graphics.DynamicSpriteFont font, string text, float width, float scale)
		{
			var lines = new System.Collections.Generic.List<string>();
			string line = "";
			foreach (string word in (text ?? "").Split(' ')) {
				string next = line.Length == 0 ? word : line + " " + word;
				if (line.Length > 0 && font.MeasureString(next).X * scale > width) {
					lines.Add(line);
					line = word;
				}
				else
					line = next;
			}

			if (line.Length > 0)
				lines.Add(line);
			return lines.ToArray();
		}
	}
}
