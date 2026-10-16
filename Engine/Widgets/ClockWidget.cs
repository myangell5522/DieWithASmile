using System;
using System.Globalization;
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
	internal static class ClockWidget
	{
		internal static bool Enabled => WeSave.Data.ClockWidget && SceneGraph.Visible(SceneGraph.Clock);

		internal static Vector2 Anchor => SceneGraph.Pixel(SceneGraph.Clock);

		internal static float Scale => SceneGraph.ScaleOf(SceneGraph.Clock);

		internal static Rectangle HitRect()
		{
			Vector2 pos = Anchor;
			float scale = Scale;
			if (WeSave.Data.ClockAnalog)
				return RoundButton.Hit(pos, 46f * scale);
			return DigitalRect(DateTime.Now);
		}

		private static string TimeText(DateTime now) =>
			now.ToString(WeSave.Data.Clock24h ? "HH:mm:ss" : "hh:mm:ss tt", CultureInfo.CurrentCulture);

		/// <summary>Panel around the digital clock; also its hit box, so dragging and drawing agree.</summary>
		private static Rectangle DigitalRect(DateTime now)
		{
			float s = Scale;
			Vector2 time = FontAssets.DeathText.Value.MeasureString(TimeText(now)) * (0.55f * s);
			float w = time.X + 56f * s;
			float h = time.Y + 14f * s;
			if (WeSave.Data.ClockDate) {
				Vector2 date = FontAssets.MouseText.Value.MeasureString(now.ToString("D", CultureInfo.CurrentCulture)) * (0.74f * s);
				w = Math.Max(w, date.X + 56f * s);
				h += date.Y;
			}

			Vector2 c = Anchor;
			return new Rectangle((int)(c.X - w * 0.5f), (int)(c.Y - h * 0.5f), (int)w, (int)Math.Max(28f, h));
		}

		internal static void Draw(SpriteBatch spriteBatch, float fade)
		{
			if (!Enabled || fade <= 0f)
				return;

			DateTime now = DateTime.Now;
			WeDraw.WithLinear(spriteBatch, () => {
				if (WeSave.Data.ClockAnalog)
					DrawAnalog(spriteBatch, now, fade);
				else
					DrawDigital(spriteBatch, now, fade);
			});
		}

		private static void DrawDigital(SpriteBatch spriteBatch, DateTime now, float fade)
		{
			float s = Scale;
			Rectangle box = DigitalRect(now);
			WeTerra.Window(spriteBatch, box, fade);
			bool day = now.Hour >= 6 && now.Hour < 19;
			WeTerra.Icon(spriteBatch, day ? WeIcons.Sun : WeIcons.Moon, new Vector2(box.X + 22f * s, box.Y + 22f * s), 16f * s, Color.White * fade);

			string time = TimeText(now);
			var font = FontAssets.DeathText.Value;
			float scale = 0.55f * s;
			Vector2 size = font.MeasureString(time) * scale;
			var pos = new Vector2(box.X + 40f * s, box.Y + 6f * s);
			Utils.DrawBorderStringFourWay(spriteBatch, font, time, pos.X, pos.Y, WeLook.Paint(Color.White, fade), Color.Black * fade, Vector2.Zero, scale);
			if (!WeSave.Data.ClockDate)
				return;

			string date = now.ToString("D", CultureInfo.CurrentCulture);
			WeTerra.Label(spriteBatch, date, new Vector2(pos.X, pos.Y + size.Y - 2f), WeLook.Paint(WeTerra.Title, fade), 0.74f * s);
		}

		private static void DrawAnalog(SpriteBatch spriteBatch, DateTime now, float fade)
		{
			Vector2 center = Anchor;
			float radius = 42f * Scale;
			WeTerra.Window(spriteBatch, RoundButton.Hit(center, radius), fade);
			DrawFace(spriteBatch, center, radius, now, fade, 1f);
			if (!WeSave.Data.ClockDate)
				return;

			string date = now.ToString("d", CultureInfo.CurrentCulture);
			Vector2 size = WeTerra.Measure(date, 0.7f * Scale);
			WeTerra.Label(spriteBatch, date, new Vector2(center.X - size.X * 0.5f, center.Y + radius + 6f), Color.White * fade, 0.7f * Scale);
		}

		/// <summary>Clock face: 12 tick blocks and three hands in the accent colours.</summary>
		private static void DrawFace(SpriteBatch spriteBatch, Vector2 center, float radius, DateTime now, float fade, float px)
		{
			for (int i = 0; i < 12; i++) {
				Vector2 at = center + (i * MathHelper.TwoPi / 12f - MathHelper.PiOver2).ToRotationVector2() * radius * 0.78f;
				int size = (int)Math.Max(2f, (i % 3 == 0 ? 4f : 2f) * px * Math.Max(0.5f, radius / 42f));
				WeDraw.Fill(spriteBatch, new Rectangle((int)at.X - size / 2, (int)at.Y - size / 2, size, size), (i % 3 == 0 ? WeTerra.Title : WeTerra.Muted) * fade);
			}

			float hour = (now.Hour % 12 + now.Minute / 60f) / 12f * MathHelper.TwoPi - MathHelper.PiOver2;
			float minute = (now.Minute + now.Second / 60f) / 60f * MathHelper.TwoPi - MathHelper.PiOver2;
			float second = now.Second / 60f * MathHelper.TwoPi - MathHelper.PiOver2;
			float w = Math.Max(0.6f, radius / 42f);
			RoundButton.DrawThick(spriteBatch, WeDraw.Pixel, center, center + hour.ToRotationVector2() * radius * 0.48f, 4f * w, Color.Black * fade);
			RoundButton.DrawThick(spriteBatch, WeDraw.Pixel, center, center + hour.ToRotationVector2() * radius * 0.46f, 2.6f * w, WeAccent.Light * fade);
			RoundButton.DrawThick(spriteBatch, WeDraw.Pixel, center, center + minute.ToRotationVector2() * radius * 0.7f, 2f * w, Color.White * fade);
			RoundButton.DrawThick(spriteBatch, WeDraw.Pixel, center, center + second.ToRotationVector2() * radius * 0.74f, 1f * w, WeAccent.Mid * fade);
			WeDraw.Fill(spriteBatch, new Rectangle((int)center.X - 2, (int)center.Y - 2, 4, 4), Color.Black * fade);
		}

		internal static void DrawPreview(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			if (fade <= 0.02f || box.Width < 8 || box.Height < 8)
				return;

			DateTime now = DateTime.Now;
			Vector2 center = box.Center.ToVector2();
			if (WeSave.Data.ClockAnalog) {
				float radius = Math.Min(box.Width, box.Height) * 0.4f;
				WeTerra.Panel(spriteBatch, RoundButton.Hit(center, radius), WeTerra.WindowBack * fade, Color.Black * fade);
				DrawFace(spriteBatch, center, radius, now, fade, 0.6f);
				return;
			}

			string time = now.ToString(WeSave.Data.Clock24h ? "HH:mm" : "h:mm tt", CultureInfo.CurrentCulture);
			var font = FontAssets.DeathText.Value;
			Vector2 size = font.MeasureString(time);
			float scale = Math.Min((box.Width - 34f) / Math.Max(1f, size.X), (box.Height - 8f) / Math.Max(1f, size.Y));
			scale = Math.Min(scale, 0.46f);
			bool day = now.Hour >= 6 && now.Hour < 19;
			float total = size.X * scale + 22f;
			float x = center.X - total * 0.5f;
			WeTerra.Icon(spriteBatch, day ? WeIcons.Sun : WeIcons.Moon, new Vector2(x + 8f, center.Y), 16f, Color.White * fade);
			Utils.DrawBorderStringFourWay(spriteBatch, font, time, x + 22f, center.Y - size.Y * scale * 0.5f, WeLook.Paint(Color.White, fade), Color.Black * fade, Vector2.Zero, scale);
		}
	}
}
