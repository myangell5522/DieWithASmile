using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Widgets
{
	internal static class QuoteWidget
	{
		private static string _line = "";
		private static DateTime _stamp;

		internal static bool Enabled => WeSave.Data.QuoteWidget && SceneGraph.Visible(SceneGraph.Quote);

		internal static Vector2 Anchor => SceneGraph.Pixel(SceneGraph.Quote);

		internal static float Scale => SceneGraph.ScaleOf(SceneGraph.Quote);

		internal static Rectangle HitRect() => Box();

		/// <summary>The speech-bubble panel; drawing and the layout editor's hit box both use it.</summary>
		private static Rectangle Box()
		{
			Vector2 size = Measure();
			float s = Scale;
			float w = size.X + 52f * s;
			float h = size.Y + 20f * s;
			Vector2 pos = Anchor;
			return new Rectangle((int)(pos.X - w * 0.5f), (int)(pos.Y - h * 0.5f), (int)w, (int)Math.Max(28f, h));
		}

		internal static void EnsureFile()
		{
			try {
				WeSave.EnsureFolders();
				if (File.Exists(WeSave.QuotePath))
					return;
				File.WriteAllText(WeSave.QuotePath,
					"# One quote per line. Lines starting with # are ignored.\n" +
					"# Одна цитата на строку. Строки с # игнорируются.\n");
			}
			catch {
			}
		}

		internal static void Refresh()
		{
			if (!WeSave.Data.QuoteWidget)
				return;

			try {
				if (!File.Exists(WeSave.QuotePath)) {
					_line = "";
					return;
				}

				DateTime stamp = File.GetLastWriteTimeUtc(WeSave.QuotePath);
				if (stamp == _stamp && _line != null)
					return;

				_stamp = stamp;
				_line = "";
				foreach (string raw in File.ReadAllLines(WeSave.QuotePath)) {
					string text = raw.Trim();
					if (text.Length == 0 || text.StartsWith("#"))
						continue;
					_line = text;
					break;
				}
			}
			catch {
				_line = "";
			}
		}

		internal static void Draw(SpriteBatch spriteBatch, float fade)
		{
			if (!Enabled || fade <= 0f)
				return;

			string greeting = Greeting();
			string quote = string.IsNullOrEmpty(_line) ? "" : "\"" + _line + "\"";
			var titleFont = FontAssets.DeathText.Value;
			float s = Scale;
			float titleScale = 0.38f * s;
			float bodyScale = 0.78f * s;
			Vector2 titleSize = titleFont.MeasureString(greeting) * titleScale;
			Rectangle box = Box();

			WeDraw.WithLinear(spriteBatch, () => {
				// Styled like an NPC chat box: panel, a speech icon, greeting in the accent colour, quote below.
				WeTerra.Window(spriteBatch, box, fade);
				WeTerra.Icon(spriteBatch, WeIcons.Quote, new Vector2(box.X + 22f * s, box.Y + 20f * s), 16f * s, Color.White * fade);
				float x = box.X + 40f * s;
				float y = box.Y + 8f * s;
				Utils.DrawBorderStringFourWay(spriteBatch, titleFont, greeting, x, y, WeLook.Paint(WeTerra.Title, fade), Color.Black * fade, Vector2.Zero, titleScale);
				if (string.IsNullOrEmpty(quote))
					return;
				WeTerra.Label(spriteBatch, quote, new Vector2(x, y + titleSize.Y + 2f), WeLook.Paint(Color.White, 0.92f * fade), bodyScale);
			});
		}

		internal static void DrawPreview(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			if (fade <= 0.02f || box.Width < 8 || box.Height < 8)
				return;

			string greeting = Greeting();
			var font = FontAssets.DeathText.Value;
			Vector2 size = font.MeasureString(greeting);
			float scale = Math.Min((box.Width - 40f) / Math.Max(1f, size.X), (box.Height - 12f) / Math.Max(1f, size.Y));
			scale = Math.Min(scale, 0.34f);
			float total = size.X * scale + 22f;
			float x = box.Center.X - total * 0.5f;
			WeTerra.Icon(spriteBatch, WeIcons.Quote, new Vector2(x + 8f, box.Center.Y), 16f, Color.White * fade);
			Utils.DrawBorderStringFourWay(spriteBatch, font, greeting, x + 22f, box.Center.Y - size.Y * scale * 0.5f, WeTerra.Title * fade, Color.Black * fade, Vector2.Zero, scale);
		}

		private static Vector2 Measure()
		{
			string greeting = Greeting();
			var titleFont = FontAssets.DeathText.Value;
			var bodyFont = FontAssets.MouseText.Value;
			float titleScale = 0.38f * Scale;
			float bodyScale = 0.78f * Scale;
			Vector2 title = titleFont.MeasureString(greeting) * titleScale;
			if (string.IsNullOrEmpty(_line))
				return title;
			Vector2 quote = bodyFont.MeasureString("\"" + _line + "\"") * bodyScale;
			return new Vector2(Math.Max(title.X, quote.X), title.Y + quote.Y + 6f);
		}

		private static string Greeting()
		{
			int hour = DateTime.Now.Hour;
			string key = hour < 5 ? "GreetingNight" : hour < 12 ? "GreetingMorning" : hour < 18 ? "GreetingAfternoon" : hour < 22 ? "GreetingEvening" : "GreetingNight";
			return WeText.UI(key);
		}
	}
}
