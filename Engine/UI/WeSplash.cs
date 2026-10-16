using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.UI
{
	internal static class WeSplash
	{
		private static bool _visible;
		private static bool _dontShow;
		private static float _fade;
		private static bool _mouseHeld;
		private static bool _holdLock;

		internal static bool Visible => _visible || _fade > 0.02f;

		internal static void OnThemeSelected()
		{
			if (WeSave.Data.SplashDismissed)
				return;
			_visible = true;
			_dontShow = false;
		}

		internal static void Show()
		{
			_visible = true;
			_dontShow = false;
		}

		internal static void Hide()
		{
			_visible = false;
			_fade = 0f;
		}

		internal static void Update()
		{
			if (!WeModMenu.OnTitle) {
				_fade = 0f;
				return;
			}

			_fade = MathHelper.Lerp(_fade, _visible ? 1f : 0f, 0.2f);
			if (!_visible && _fade < 0.02f)
				_fade = 0f;
		}

		internal static void Dismiss(bool savePreference)
		{
			_visible = false;
			if (savePreference || _dontShow) {
				WeSave.Data.SplashDismissed = true;
				WeSave.Save();
			}

			SoundEngine.PlaySound(SoundID.MenuClose);
			WrenchToolbar.OnThemeSelected();
		}

		internal static void HandleInput()
		{
			if (!_visible)
				return;

			Main.blockMouse = true;
			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			if (!pressed)
				return;

			Rectangle card = Card();
			if (CheckHit(card).Contains(Main.mouseX, Main.mouseY)) {
				_dontShow = !_dontShow;
				SoundEngine.PlaySound(SoundID.MenuTick);
				WeInput.LockHold(ref _holdLock);
				return;
			}

			if (OkHit(card).Contains(Main.mouseX, Main.mouseY)) {
				Dismiss(_dontShow);
				WeInput.LockHold(ref _holdLock);
			}
		}

		internal static void Draw(SpriteBatch spriteBatch)
		{
			if (_fade <= 0.01f)
				return;

			WeDraw.WithLinear(spriteBatch, () => WeTerra.WithTheme(() => {
				WeDraw.Fill(spriteBatch, WeDraw.CoverRect, Color.Black * (0.62f * _fade));
				Rectangle card = Card();
				WeTerra.Window(spriteBatch, card, _fade);

				var font = FontAssets.MouseText.Value;
				WeTerra.Headline(spriteBatch, WeText.UI("SplashTitle"), new Vector2(card.X + 28, card.Y + 16), WeTerra.Title * _fade, 0.62f);
				int y = card.Y + TextTop;
				for (int i = 0; i < Icons.Length; i++) {
					string wrapped = Wrap(font, WeText.UI("Splash" + (i + 1)), TextWidth(card) / TextScale);
					WeTerra.Icon(spriteBatch, Icons[i], new Vector2(card.X + 38, y + 10), 16f, Color.White * _fade);
					string[] lines = wrapped.Split('\n');
					for (int l = 0; l < lines.Length; l++)
						WeTerra.Label(spriteBatch, lines[l], new Vector2(card.X + 60, y + l * font.LineSpacing * TextScale), Color.White * (0.92f * _fade), TextScale);
					y += ParagraphHeight(font, wrapped) + ParagraphGap;
				}

				DrawCheck(spriteBatch, CheckHit(card), _dontShow, WeText.UI("DontShow"), _fade);
				DrawButton(spriteBatch, OkHit(card), WeText.UI("GotIt"), _fade);
			}));
		}

		private static readonly string[] Icons = { WeIcons.Star, WeIcons.Setting, WeIcons.Widget, WeIcons.Puzzle };
		private const float TextScale = 0.82f;
		private const int TextTop = 72;
		private const int ParagraphGap = 18;
		private const int FooterH = 84;

		private static int TextWidth(Rectangle card) => card.Width - 88;

		private static int ParagraphHeight(ReLogic.Graphics.DynamicSpriteFont font, string wrapped)
		{
			int lines = 1;
			foreach (char c in wrapped)
				if (c == '\n')
					lines++;
			return (int)Math.Ceiling(lines * font.LineSpacing * TextScale);
		}

		/// <summary>Card sized to its wrapped text, so long translations never run into each other or the footer.</summary>
		private static Rectangle Card()
		{
			int w = Math.Min(620, Main.screenWidth - 80);
			var font = FontAssets.MouseText.Value;
			int textW = w - 88;
			int h = TextTop + FooterH;
			// Same (themed) measuring as Draw, so the buttons are hit where they are drawn.
			bool was = WeTerra.Themed;
			WeTerra.Themed = true;
			try {
				for (int i = 0; i < Icons.Length; i++)
					h += ParagraphHeight(font, Wrap(font, WeText.UI("Splash" + (i + 1)), textW / TextScale)) + ParagraphGap;
			}
			finally {
				WeTerra.Themed = was;
			}

			h = Math.Min(h, Main.screenHeight - 40);
			return new Rectangle((Main.screenWidth - w) / 2, (Main.screenHeight - h) / 2, w, h);
		}

		private static Rectangle OkHit(Rectangle card) => new(card.Right - 168, card.Bottom - 58, 140, 34);

		private static Rectangle CheckHit(Rectangle card) => new(card.X + 28, card.Bottom - 54, 22, 22);

		private static void DrawButton(SpriteBatch spriteBatch, Rectangle hit, string text, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeTerra.Panel(spriteBatch, hit, WeTerra.LookOf(hover, true), fade);
			WeTerra.LabelCentered(spriteBatch, text, hit.Center.ToVector2(), Color.White * fade, 0.85f);
		}

		private static void DrawCheck(SpriteBatch spriteBatch, Rectangle hit, bool on, string label, float fade)
		{
			WeTerra.Icon(spriteBatch, on ? WeIcons.ToggleOn : WeIcons.ToggleOff, hit.Center.ToVector2(), 16f, Color.White * fade);
			WeTerra.Label(spriteBatch, label, new Vector2(hit.Right + 10, hit.Y + 2), Color.White * fade, 0.8f);
		}

		private static void DrawText(SpriteBatch spriteBatch, ReLogic.Graphics.DynamicSpriteFont font, string text, Vector2 pos, float scale, Color color)
		{
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, pos, color, 0f, Vector2.Zero, new Vector2(scale));
		}

		private static string Wrap(ReLogic.Graphics.DynamicSpriteFont font, string text, float width)
		{
			if (string.IsNullOrEmpty(text) || WeTerra.Measure(text, 1f).X <= width)
				return text;

			string[] words = text.Split(' ');
			var line = "";
			var result = "";
			foreach (string word in words) {
				string next = string.IsNullOrEmpty(line) ? word : line + " " + word;
				if (WeTerra.Measure(next, 1f).X > width && line.Length > 0) {
					result += line + "\n";
					line = word;
				}
				else
					line = next;
			}

			return result + line;
		}
	}
}
