using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoCursor
	{
		internal const int Height = 156;

		internal static void Draw(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			int cellW = (view.Width - 28) / 3;
			string[] names = { WeText.UI("NeoCursorNormal"), WeText.UI("NeoCursorThick"), WeText.UI("NeoCursorSmart") };
			for (int i = 0; i < 3; i++) {
				var cell = new Rectangle(view.X + 8 + i * (cellW + 6), y + 4, cellW, 78);
				bool hover = cell.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, cell, new Color(16, 18, 24) * fade);
				WeDraw.Border(spriteBatch, cell, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
				DrawKind(spriteBatch, cell.Center.ToVector2() + new Vector2(0f, -6f), i, fade);
				Vector2 size = FontAssets.MouseText.Value.MeasureString(names[i]) * WeNeoShell.TypeSmall;
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, names[i],
					new Vector2(cell.Center.X - size.X * 0.5f, cell.Bottom - 18),
					Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			}

			int chipY = y + 88;
			int chipW = 120;
			DrawChip(spriteBatch, new Rectangle(view.X + 8, chipY, chipW, 32), 0, Main.mouseColor, WeText.UI("NeoFill"), fade);
			DrawChip(spriteBatch, new Rectangle(view.X + 16 + chipW, chipY, chipW, 32), 1, WeNeoBind.BorderColor, WeText.UI("NeoBorder"), fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoCursorHint"),
				new Vector2(view.X + 8, chipY + 38), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
		}

		internal static bool Click(Rectangle view, int y, bool left)
		{
			if (!left)
				return false;
			int chipW = 120;
			int chipY = y + 88;
			var fill = new Rectangle(view.X + 8, chipY, chipW, 32);
			var border = new Rectangle(view.X + 16 + chipW, chipY, chipW, 32);
			if (fill.Contains(Main.mouseX, Main.mouseY)) {
				WeNeoMenu.SetCursorChip(0);
				return true;
			}

			if (border.Contains(Main.mouseX, Main.mouseY)) {
				WeNeoMenu.SetCursorChip(1);
				return true;
			}

			return false;
		}

		private static void DrawChip(SpriteBatch spriteBatch, Rectangle hit, int index, Color color, string label, float fade)
		{
			bool on = WeNeoMenu.CursorChip == index;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (on ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
			WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : WeAccent.Mid) * fade);
			if (on)
				WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 3, hit.Height), WeAccent.Hover * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 8, 16, 16), color * fade);
			WeDraw.Border(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 8, 16, 16), Color.White * (0.35f * fade));
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 30, hit.Y + 7), Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
		}

		private static void DrawKind(SpriteBatch spriteBatch, Vector2 center, int kind, float fade)
		{
			if (kind == 2) {
				DrawSmart(spriteBatch, center, fade);
				return;
			}

			float scale = kind == 1 ? 1.7f : 1.15f;
			Texture2D tex = CursorAt(0) ?? CursorTex();
			Color fill = Main.mouseColor * fade;
			Color edge = WeNeoBind.BorderColor * fade;
			if (tex != null && !tex.IsDisposed) {
				Vector2 origin = tex.Size() * 0.5f;
				for (int ox = -1; ox <= 1; ox++) {
					for (int oy = -1; oy <= 1; oy++) {
						if (ox == 0 && oy == 0)
							continue;
						spriteBatch.Draw(tex, center + new Vector2(ox, oy) * scale, null, edge, 0f, origin, scale, SpriteEffects.None, 0f);
					}
				}

				spriteBatch.Draw(tex, center, null, fill, 0f, origin, scale, SpriteEffects.None, 0f);
			}
			else {
				var box = new Rectangle((int)center.X - 8, (int)center.Y - 8, 16, 16);
				WeDraw.Fill(spriteBatch, box, fill);
				WeDraw.Border(spriteBatch, box, edge);
			}
		}

		private static void DrawSmart(SpriteBatch spriteBatch, Vector2 center, float fade)
		{
			int tile = 12;
			for (int i = -1; i <= 1; i++) {
				for (int j = -1; j <= 1; j++) {
					var cell = new Rectangle(
						(int)center.X + i * tile - tile / 2,
						(int)center.Y + j * tile - tile / 2,
						tile, tile);
					WeDraw.Border(spriteBatch, cell, Color.White * (0.28f * fade));
				}
			}

			Color fill = Main.mouseColor * fade;
			Color edge = WeNeoBind.BorderColor * fade;
			Texture2D tex = SmartTex();
			if (tex != null && !tex.IsDisposed) {
				float scale = 1.15f;
				Vector2 origin = tex.Size() * 0.5f;
				for (int ox = -1; ox <= 1; ox++) {
					for (int oy = -1; oy <= 1; oy++) {
						if (ox == 0 && oy == 0)
							continue;
						spriteBatch.Draw(tex, center + new Vector2(ox, oy) * scale, null, edge, 0f, origin, scale, SpriteEffects.None, 0f);
					}
				}

				spriteBatch.Draw(tex, center, null, fill, 0f, origin, scale, SpriteEffects.None, 0f);
				return;
			}

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Vector2 diamond = new(18f, 18f);
			spriteBatch.Draw(pixel, center, null, edge, MathHelper.PiOver4, new Vector2(0.5f, 0.5f), diamond + new Vector2(2f), SpriteEffects.None, 0f);
			spriteBatch.Draw(pixel, center, null, fill, MathHelper.PiOver4, new Vector2(0.5f, 0.5f), diamond, SpriteEffects.None, 0f);
		}

		private static Texture2D SmartTex()
		{
			int[] prefer = { 2, 15, 16, 13, 11, 12, 1 };
			foreach (int i in prefer) {
				Texture2D tex = CursorAt(i);
				if (LooksSmart(tex))
					return tex;
			}

			for (int i = 1; i < 32; i++) {
				Texture2D tex = CursorAt(i);
				if (LooksSmart(tex))
					return tex;
			}

			int[] extra = { 2, 15, 16, 13 };
			foreach (int i in extra) {
				Texture2D tex = ExtraAt(i);
				if (LooksSmart(tex))
					return tex;
			}

			return null;
		}

		private static bool LooksSmart(Texture2D tex)
		{
			if (tex == null || tex.IsDisposed || tex.Width < 8 || tex.Height < 8)
				return false;
			return tex.Width >= tex.Height * 0.72f;
		}

		private static Texture2D CursorAt(int index)
		{
			try {
				object arr = typeof(TextureAssets).GetField("Cursors")?.GetValue(null)
				             ?? typeof(TextureAssets).GetProperty("Cursors")?.GetValue(null);
				if (arr is Array list && index >= 0 && index < list.Length)
					return AssetTex(list.GetValue(index));
			}
			catch {
			}

			return null;
		}

		private static Texture2D ExtraAt(int index)
		{
			try {
				object arr = typeof(TextureAssets).GetField("Extra")?.GetValue(null)
				             ?? typeof(TextureAssets).GetProperty("Extra")?.GetValue(null);
				if (arr is Array list && index >= 0 && index < list.Length)
					return AssetTex(list.GetValue(index));
			}
			catch {
			}

			return null;
		}

		private static Texture2D CursorTex()
		{
			Texture2D from = CursorAt(0);
			if (from != null)
				return from;
			try {
				object single = typeof(TextureAssets).GetField("Cursor")?.GetValue(null)
				                ?? typeof(TextureAssets).GetProperty("Cursor")?.GetValue(null);
				Texture2D tex = AssetTex(single);
				if (tex != null)
					return tex;
			}
			catch {
			}

			return TextureAssets.MagicPixel.Value;
		}

		private static Texture2D AssetTex(object asset)
		{
			if (asset is Texture2D direct)
				return direct;
			if (asset == null)
				return null;
			try {
				return asset.GetType().GetProperty("Value")?.GetValue(asset) as Texture2D;
			}
			catch {
				return null;
			}
		}
	}
}
