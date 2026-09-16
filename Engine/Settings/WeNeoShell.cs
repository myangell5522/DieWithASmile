using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoShell
	{
		internal const int RowH = 40;
		internal const int RowStep = 44;
		internal const int HeaderStep = 34;
		internal const int SideW = 228;
		internal const float Type = 0.88f;
		internal const float TypeSmall = 0.72f;
		private const int SideRow = 38;
		private static readonly WeNeoCat[] Cats =
		{
			WeNeoCat.Game, WeNeoCat.Video, WeNeoCat.Audio, WeNeoCat.Interface,
			WeNeoCat.Cursor, WeNeoCat.Controls, WeNeoCat.Mods, WeNeoCat.Client
		};

		internal static Rectangle Panel()
		{
			int pad = Math.Max(16, Math.Min(Main.screenWidth, Main.screenHeight) / 32);
			return new Rectangle(pad, pad, Main.screenWidth - pad * 2, Main.screenHeight - pad * 2);
		}

		internal static Rectangle SearchBox(Rectangle panel) =>
			new(panel.X + 16, panel.Y + 14, panel.Width - SideW - 128, 34);

		internal static Rectangle CloseBox(Rectangle panel) =>
			new(panel.Right - 48, panel.Y + 14, 32, 32);

		internal static Rectangle Side(Rectangle panel) =>
			new(panel.X + 8, panel.Y + 58, SideW, panel.Height - 76);

		internal static Rectangle View(Rectangle panel) =>
			new(panel.X + SideW + 22, panel.Y + 58, panel.Width - SideW - 40, panel.Height - 128);

		internal static Rectangle DoneBox(Rectangle panel)
		{
			int w = 124;
			return new Rectangle(panel.Right - w - 16, panel.Bottom - 52, w, 34);
		}

		internal static Rectangle ApplyBox(Rectangle panel)
		{
			int w = 124;
			return new Rectangle(panel.Right - w * 2 - 28, panel.Bottom - 52, w, 34);
		}

		internal static void Draw(SpriteBatch spriteBatch)
		{
			float fade = WeNeoMenu.Fade;
			WeDraw.Fill(spriteBatch, WeDraw.CoverRect, Color.Black * (0.55f * fade));
			Rectangle panel = Panel();
			WeDraw.Fill(spriteBatch, panel, new Color(12, 14, 18) * (0.94f * fade));
			WeDraw.Border(spriteBatch, panel, WeAccent.Mid * (0.55f * fade));
			DrawSearch(spriteBatch, panel, fade);
			DrawClose(spriteBatch, panel, fade);
			DrawSide(spriteBatch, panel, fade);
			WeDraw.Fill(spriteBatch, new Rectangle(panel.X + SideW + 12, panel.Y + 58, 1, panel.Height - 128), WeAccent.Mid * (0.35f * fade));
			Rectangle view = View(panel);
			WeDraw.WithClip(spriteBatch, view, () => DrawBody(spriteBatch, view, fade));
			DrawScroll(spriteBatch, view, fade);
			if (WeNeoBind.NeedApply)
				DrawBtn(spriteBatch, ApplyBox(panel), WeText.UI("NeoApply"), fade);
			DrawBtn(spriteBatch, DoneBox(panel), WeText.UI("Done"), fade);
		}

		internal static void Handle(bool pressed, bool right)
		{
			Rectangle panel = Panel();
			Rectangle view = View(panel);
			WeNeoMenu.SetViewHeight(view.Height);
			WeNeoMenu.Wheel(view);

			if (WeNeoMenu.Drag != null) {
				if (WeNeoMenu.Drag.StartsWith("neo", StringComparison.Ordinal)) {
					if (WeNeoMenu.PumpDrag(view)) {
						int fireY = view.Y - (int)WeNeoMenu.Scroll;
						WeNeoBind.Click(view, ref fireY, true, false);
					}

					return;
				}

				if (WeInput.LeftDown)
					WeNeoBind.Slide(WeNeoMenu.Drag);
				else
					WeNeoMenu.SetDrag(null);
				return;
			}

			if (WeNeoMenu.SearchFocus) {
				string next = Main.GetInputText(WeNeoMenu.Search) ?? "";
				if (next != WeNeoMenu.Search) {
					WeNeoMenu.SetSearch(next);
					WeNeoMenu.SetScroll(0f);
				}

				if (Main.inputTextEnter || Main.inputTextEscape)
					WeNeoMenu.SetSearchFocus(false);
			}

			if (!pressed && !right)
				return;

			if (CloseBox(panel).Contains(Main.mouseX, Main.mouseY) && pressed) {
				WeNeoMenu.Close();
				return;
			}

			if (DoneBox(panel).Contains(Main.mouseX, Main.mouseY) && pressed) {
				WeNeoMenu.Close();
				return;
			}

			if (WeNeoBind.NeedApply && ApplyBox(panel).Contains(Main.mouseX, Main.mouseY) && pressed) {
				WeNeoBind.ApplyVideo();
				return;
			}

			Rectangle search = SearchBox(panel);
			if (search.Contains(Main.mouseX, Main.mouseY) && pressed) {
				WeNeoMenu.SetSearchFocus(true);
				return;
			}

			if (pressed)
				WeNeoMenu.SetSearchFocus(false);

			int catY = Side(panel).Y + 6;
			foreach (WeNeoCat cat in Cats) {
				var hit = new Rectangle(Side(panel).X + 6, catY, Side(panel).Width - 12, SideRow);
				if (pressed && hit.Contains(Main.mouseX, Main.mouseY)) {
					WeNeoMenu.SelectCat(cat);
					return;
				}

				catY += SideRow + 6;
			}

			if (!view.Contains(Main.mouseX, Main.mouseY))
				return;

			if (pressed && WeNeoMenu.ScrollTrack(view).Contains(Main.mouseX, Main.mouseY)) {
				WeNeoMenu.SetDrag("neo-scroll");
				WeNeoMenu.PumpDrag(view);
				return;
			}

			if (pressed && WeNeoMenu.Chrome > 0 && Main.mouseY >= view.Y + WeNeoMenu.Chrome) {
				WeNeoMenu.SetDrag("neo-hold");
				return;
			}

			int y = view.Y - (int)WeNeoMenu.Scroll;
			WeNeoBind.Click(view, ref y, pressed, right);
		}

		private static void DrawScroll(SpriteBatch spriteBatch, Rectangle view, float fade)
		{
			if (WeNeoMenu.MaxScroll(view) < 8f)
				return;
			Rectangle track = WeNeoMenu.ScrollTrack(view);
			Rectangle thumb = WeNeoMenu.ScrollThumb(view);
			if (track.Height < 8 || thumb.Height < 4)
				return;
			WeDraw.Fill(spriteBatch, track, new Color(16, 18, 22) * (0.7f * fade));
			bool hover = thumb.Contains(Main.mouseX, Main.mouseY) || WeNeoMenu.Drag == "neo-scroll";
			WeDraw.Fill(spriteBatch, thumb, (hover ? WeAccent.Light : WeAccent.Mid) * (0.85f * fade));
		}

		private static void DrawSearch(SpriteBatch spriteBatch, Rectangle panel, float fade)
		{
			Rectangle box = SearchBox(panel);
			bool hover = box.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, box, new Color(20, 22, 28) * ((hover || WeNeoMenu.SearchFocus ? 0.98f : 0.88f) * fade));
			WeDraw.Border(spriteBatch, box, (WeNeoMenu.SearchFocus ? WeAccent.Light : WeAccent.Mid) * fade);
			string shown = WeNeoMenu.Search;
			Color col = Color.White;
			if (string.IsNullOrEmpty(shown)) {
				shown = WeText.UI("NeoSearch");
				col = Color.White * 0.45f;
			}

			if (WeNeoMenu.SearchFocus && (Main.GameUpdateCount / 20) % 2 == 0)
				shown += "|";
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, shown,
				new Vector2(box.X + 10, box.Y + 8), col * fade, 0f, Vector2.Zero, new Vector2(0.8f));
		}

		private static void DrawClose(SpriteBatch spriteBatch, Rectangle panel, float fade)
		{
			Rectangle box = CloseBox(panel);
			bool hover = box.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, box, new Color(28, 30, 36) * ((hover ? 1f : 0.8f) * fade));
			WeDraw.Border(spriteBatch, box, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, "x",
				new Vector2(box.X + 10, box.Y + 6), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.86f));
		}

		private static void DrawSide(SpriteBatch spriteBatch, Rectangle panel, float fade)
		{
			Rectangle side = Side(panel);
			int y = side.Y + 6;
			foreach (WeNeoCat cat in Cats) {
				var hit = new Rectangle(side.X + 6, y, side.Width - 12, SideRow);
				bool on = WeNeoMenu.Category == cat;
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (on ? WeAccent.Deep : new Color(18, 20, 26)) * ((hover || on ? 0.98f : 0.82f) * fade));
				if (on)
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 4, hit.Height), WeAccent.Hover * fade);
				else if (hover)
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 3, hit.Height), WeAccent.Light * (0.7f * fade));
				WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : WeAccent.Mid) * (0.45f * fade));
				Texture2D icon = WeIcons.Get(IconOf(cat));
				if (icon != null) {
					float s = 18f / Math.Max(icon.Width, icon.Height);
					spriteBatch.Draw(icon, new Vector2(hit.X + 20, hit.Center.Y), null, WeAccent.Icon(hover, on) * fade, 0f, icon.Size() * 0.5f, s, SpriteEffects.None, 0f);
				}

				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(KeyOf(cat)),
					new Vector2(hit.X + 36, hit.Y + 9), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.82f));
				y += SideRow + 6;
			}
		}

		private static void DrawBody(SpriteBatch spriteBatch, Rectangle view, float fade)
		{
			WeNeoMenu.SetViewHeight(view.Height);
			float page = WeNeoMenu.PageFade;
			int y = view.Y - (int)WeNeoMenu.Scroll + (int)((1f - page) * 12f);
			WeNeoBind.Draw(spriteBatch, view, ref y, fade * MathHelper.Clamp(page, 0.15f, 1f));
			WeNeoMenu.SetContentHeight(y - (view.Y - (int)WeNeoMenu.Scroll) + 12);
		}

		private static void DrawBtn(SpriteBatch spriteBatch, Rectangle hit, string text, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * Type;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(Type));
		}

		internal static void Header(SpriteBatch spriteBatch, Rectangle view, ref int y, string title, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, "+  " + title,
				new Vector2(view.X + 8, y + 6), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(0.92f));
			y += HeaderStep;
		}

		internal static void SkipHeader(ref int y) => y += HeaderStep;

		internal static Rectangle Row(Rectangle view, int y) =>
			new(view.X + 4, y, view.Width - 8, RowH);

		internal static Rectangle PaintRow(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			Rectangle hit = Row(view, y);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Color fill = hover ? Color.Lerp(new Color(22, 24, 30), WeAccent.Deep, 0.52f) : new Color(22, 24, 30);
			WeDraw.Fill(spriteBatch, hit, fill * fade);
			if (hover) {
				WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 3, hit.Height), WeAccent.Hover * fade);
				WeDraw.Border(spriteBatch, hit, WeAccent.Light * (0.4f * fade));
			}

			return hit;
		}

		internal static void Toggle(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, bool on, float fade)
		{
			Rectangle hit = PaintRow(spriteBatch, view, y, fade);
			Label(spriteBatch, label, hit, fade);
			var pill = new Rectangle(hit.Right - 78, hit.Y + (hit.Height - 24) / 2, 66, 24);
			WeDraw.Fill(spriteBatch, pill, (on ? WeAccent.Mid : new Color(40, 44, 52)) * fade);
			WeDraw.Border(spriteBatch, pill, (on ? WeAccent.Light : Color.White * 0.28f) * fade);
			string word = WeText.UI(on ? "NeoOn" : "NeoOff");
			Vector2 size = FontAssets.MouseText.Value.MeasureString(word) * TypeSmall;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, word,
				new Vector2(pill.X + (pill.Width - size.X) * 0.5f, pill.Y + (pill.Height - size.Y) * 0.5f),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(TypeSmall));
			y += RowStep;
		}

		internal static void Cycle(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, string value, float fade)
		{
			Rectangle hit = PaintRow(spriteBatch, view, y, fade);
			Label(spriteBatch, label, hit, fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(value ?? "") * Type;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, value ?? "",
				new Vector2(hit.Right - 14 - size.X, hit.Y + 10), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(Type));
			y += RowStep;
		}

		internal static void Slider(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, float t, string value, float fade, bool fat = false)
		{
			Rectangle hit = PaintRow(spriteBatch, view, y, fade);
			Label(spriteBatch, label, hit, fade);
			Rectangle bar = SliderBarInner(hit, fat);
			int h = fat ? 10 : 7;
			var track = new Rectangle(bar.X, hit.Y + (hit.Height - h) / 2, bar.Width, h);
			WeDraw.Fill(spriteBatch, track, Color.White * (0.12f * fade));
			WeDraw.Fill(spriteBatch, new Rectangle(track.X, track.Y, Math.Max(1, (int)(track.Width * MathHelper.Clamp(t, 0f, 1f))), track.Height), WeAccent.Mid * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, value ?? "",
				new Vector2(track.Right + 8, hit.Y + 10), Color.White * (0.85f * fade), 0f, Vector2.Zero, new Vector2(TypeSmall));
			y += RowStep;
		}

		internal static void Button(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, string value, float fade)
		{
			Cycle(spriteBatch, view, ref y, label, value, fade);
		}

		internal static void LightPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			int mode = (int)Lighting.Mode;
			int w = 52;
			for (int i = 0; i < 4; i++) {
				var cell = new Rectangle(view.X + 10 + i * (w + 8), y + 4, w, 24);
				Color fill = i switch {
					0 => new Color(70, 150, 255),
					1 => Color.White,
					2 => new Color(186, 140, 88),
					_ => new Color(214, 72, 214)
				};
				WeDraw.Fill(spriteBatch, cell, fill * fade);
				if (i == mode)
					WeDraw.Border(spriteBatch, cell, WeAccent.Light * fade);
			}
		}

		internal static bool LightPreviewClick(Rectangle view, int y, bool left)
		{
			if (!left)
				return false;
			int w = 52;
			for (int i = 0; i < 4; i++) {
				var cell = new Rectangle(view.X + 10 + i * (w + 8), y + 4, w, 24);
				if (!cell.Contains(Main.mouseX, Main.mouseY))
					continue;
				Lighting.Mode = (LightMode)i;
				try {
					Main.SaveSettings();
				}
				catch {
				}

				return true;
			}

			return false;
		}

		internal static void CursorPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var swatch = new Rectangle(view.X + 10, y + 6, 36, 36);
			WeDraw.Fill(spriteBatch, swatch, Main.mouseColor * fade);
			WeDraw.Border(spriteBatch, swatch, WeNeoBind.BorderColor * fade);
			int chipW = Math.Max(90, (view.Width - 64) / 2);
			DrawChip(spriteBatch, new Rectangle(swatch.Right + 12, y + 8, chipW, 32), 0, Main.mouseColor, WeText.UI("NeoFill"), fade);
			DrawChip(spriteBatch, new Rectangle(swatch.Right + 20 + chipW, y + 8, chipW, 32), 1, WeNeoBind.BorderColor, WeText.UI("NeoBorder"), fade);
		}

		internal static bool CursorPreviewClick(Rectangle view, int y, bool left)
		{
			if (!left)
				return false;
			var swatch = new Rectangle(view.X + 10, y + 6, 36, 36);
			int chipW = Math.Max(90, (view.Width - 64) / 2);
			var fill = new Rectangle(swatch.Right + 12, y + 8, chipW, 32);
			var border = new Rectangle(swatch.Right + 20 + chipW, y + 8, chipW, 32);
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
			WeDraw.Fill(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 8, 16, 16), color * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 30, hit.Y + 7), Color.White * fade, 0f, Vector2.Zero, new Vector2(TypeSmall));
		}

		private static void Label(SpriteBatch spriteBatch, string label, Rectangle hit, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 14, hit.Y + 10), Color.White * fade, 0f, Vector2.Zero, new Vector2(Type));
		}

		internal static bool HitRow(Rectangle view, ref int y) =>
			Row(view, y).Contains(Main.mouseX, Main.mouseY);

		internal static Rectangle SliderBar(Rectangle view, int y, bool fat = false) =>
			SliderBarInner(Row(view, y), fat);

		private static Rectangle SliderBarInner(Rectangle hit, bool fat)
		{
			int w = fat ? 168 : 132;
			return new Rectangle(hit.Right - w - 62, hit.Y + 6, w, hit.Height - 12);
		}

		internal static string KeyOf(WeNeoCat cat) => cat switch {
			WeNeoCat.Video => "NeoCatVideo",
			WeNeoCat.Audio => "NeoCatAudio",
			WeNeoCat.Interface => "NeoCatInterface",
			WeNeoCat.Cursor => "NeoCatCursor",
			WeNeoCat.Controls => "NeoCatControls",
			WeNeoCat.Mods => "NeoCatMods",
			WeNeoCat.Client => "NeoCatClient",
			_ => "NeoCatGame"
		};

		internal static string IconOf(WeNeoCat cat) => cat switch {
			WeNeoCat.Video => WeIcons.Layout,
			WeNeoCat.Audio => WeIcons.Music,
			WeNeoCat.Interface => WeIcons.Widget,
			WeNeoCat.Cursor => WeIcons.MoveLogo,
			WeNeoCat.Controls => WeIcons.Setting,
			WeNeoCat.Mods => WeIcons.Upload,
			WeNeoCat.Client => WeIcons.Client,
			_ => WeIcons.Setting
		};

		internal static bool Matches(string query, params string[] parts)
		{
			if (string.IsNullOrWhiteSpace(query))
				return true;
			string q = query.Trim();
			foreach (string part in parts) {
				if (!string.IsNullOrEmpty(part) && part.Contains(q, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}
	}
}
