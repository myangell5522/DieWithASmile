using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoShell
	{
		internal const int RowH = 28;
		internal const int RowStep = 30;
		internal const int SideW = 188;
		private static readonly WeNeoCat[] Cats =
		{
			WeNeoCat.Game, WeNeoCat.Video, WeNeoCat.Audio, WeNeoCat.Interface,
			WeNeoCat.Cursor, WeNeoCat.Controls, WeNeoCat.Mods, WeNeoCat.Client
		};

		internal static Rectangle Panel()
		{
			int pad = Math.Max(18, Math.Min(Main.screenWidth, Main.screenHeight) / 28);
			return new Rectangle(pad, pad, Main.screenWidth - pad * 2, Main.screenHeight - pad * 2);
		}

		internal static Rectangle SearchBox(Rectangle panel) =>
			new(panel.X + 16, panel.Y + 14, panel.Width - SideW - 120, 28);

		internal static Rectangle CloseBox(Rectangle panel) =>
			new(panel.Right - 42, panel.Y + 14, 26, 26);

		internal static Rectangle Side(Rectangle panel) =>
			new(panel.X + 8, panel.Y + 52, SideW, panel.Height - 68);

		internal static Rectangle View(Rectangle panel) =>
			new(panel.X + SideW + 20, panel.Y + 52, panel.Width - SideW - 36, panel.Height - 108);

		internal static Rectangle DoneBox(Rectangle panel)
		{
			int w = 110;
			return new Rectangle(panel.Right - w - 16, panel.Bottom - 44, w, 28);
		}

		internal static Rectangle ApplyBox(Rectangle panel)
		{
			int w = 110;
			return new Rectangle(panel.Right - w * 2 - 28, panel.Bottom - 44, w, 28);
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
			WeDraw.Fill(spriteBatch, new Rectangle(panel.X + SideW + 12, panel.Y + 52, 1, panel.Height - 108), WeAccent.Mid * (0.35f * fade));
			Rectangle view = View(panel);
			WeDraw.WithClip(spriteBatch, view, () => DrawBody(spriteBatch, view, fade));
			if (WeNeoBind.NeedApply)
				DrawBtn(spriteBatch, ApplyBox(panel), WeText.UI("NeoApply"), fade);
			DrawBtn(spriteBatch, DoneBox(panel), WeText.UI("Done"), fade);
		}

		internal static void Handle(bool pressed, bool right)
		{
			Rectangle panel = Panel();
			Rectangle view = View(panel);
			WeNeoMenu.Wheel(view);

			if (WeNeoMenu.Drag != null) {
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
				var hit = new Rectangle(Side(panel).X + 6, catY, Side(panel).Width - 12, 28);
				if (pressed && hit.Contains(Main.mouseX, Main.mouseY)) {
					WeNeoMenu.SelectCat(cat);
					return;
				}

				catY += 32;
			}

			if (!view.Contains(Main.mouseX, Main.mouseY))
				return;

			int y = view.Y - (int)WeNeoMenu.Scroll;
			WeNeoBind.Click(view, ref y, pressed, right);
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
				new Vector2(box.X + 10, box.Y + 6), col * fade, 0f, Vector2.Zero, new Vector2(0.72f));
		}

		private static void DrawClose(SpriteBatch spriteBatch, Rectangle panel, float fade)
		{
			Rectangle box = CloseBox(panel);
			bool hover = box.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, box, new Color(28, 30, 36) * ((hover ? 1f : 0.8f) * fade));
			WeDraw.Border(spriteBatch, box, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, "x",
				new Vector2(box.X + 8, box.Y + 4), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
		}

		private static void DrawSide(SpriteBatch spriteBatch, Rectangle panel, float fade)
		{
			Rectangle side = Side(panel);
			int y = side.Y + 6;
			foreach (WeNeoCat cat in Cats) {
				var hit = new Rectangle(side.X + 6, y, side.Width - 12, 28);
				bool on = WeNeoMenu.Category == cat;
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (on ? WeAccent.Deep : new Color(18, 20, 26)) * ((hover || on ? 0.98f : 0.82f) * fade));
				if (on)
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 3, hit.Height), WeAccent.Light * fade);
				WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : WeAccent.Mid) * (0.45f * fade));
				Texture2D icon = WeIcons.Get(IconOf(cat));
				if (icon != null) {
					float s = 16f / Math.Max(icon.Width, icon.Height);
					spriteBatch.Draw(icon, new Vector2(hit.X + 18, hit.Center.Y), null, WeAccent.Icon(hover, on) * fade, 0f, icon.Size() * 0.5f, s, SpriteEffects.None, 0f);
				}

				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(KeyOf(cat)),
					new Vector2(hit.X + 32, hit.Y + 6), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.72f));
				y += 32;
			}
		}

		private static void DrawBody(SpriteBatch spriteBatch, Rectangle view, float fade)
		{
			int y = view.Y - (int)WeNeoMenu.Scroll;
			WeNeoBind.Draw(spriteBatch, view, ref y, fade);
			WeNeoMenu.SetContentHeight(y - (view.Y - (int)WeNeoMenu.Scroll) + 8);
		}

		private static void DrawBtn(SpriteBatch spriteBatch, Rectangle hit, string text, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * 0.72f;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(0.72f));
		}

		internal static void Header(SpriteBatch spriteBatch, Rectangle view, ref int y, string title, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, "+  " + title,
				new Vector2(view.X + 8, y + 4), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(0.78f));
			y += 26;
		}

		internal static Rectangle Row(Rectangle view, int y) =>
			new(view.X + 4, y, view.Width - 8, RowH);

		internal static void Toggle(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, bool on, float fade)
		{
			Rectangle hit = Row(view, y);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, WeNeoMenu.RowFill(hover) * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 10, hit.Y + 6), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.7f));
			var box = new Rectangle(hit.Right - 28, hit.Y + 7, 14, 14);
			WeDraw.Fill(spriteBatch, box, (on ? WeAccent.Mid : new Color(40, 44, 52)) * fade);
			WeDraw.Border(spriteBatch, box, (on || hover ? WeAccent.Light : Color.White * 0.35f) * fade);
			y += RowStep;
		}

		internal static void Cycle(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, string value, float fade)
		{
			Rectangle hit = Row(view, y);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, WeNeoMenu.RowFill(hover) * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 10, hit.Y + 6), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.7f));
			Vector2 size = FontAssets.MouseText.Value.MeasureString(value ?? "") * 0.7f;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, value ?? "",
				new Vector2(hit.Right - 12 - size.X, hit.Y + 6), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(0.7f));
			y += RowStep;
		}

		internal static void Slider(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, float t, string value, float fade)
		{
			Rectangle hit = Row(view, y);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, WeNeoMenu.RowFill(hover) * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 10, hit.Y + 6), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.7f));
			var bar = new Rectangle(hit.Right - 168, hit.Y + 11, 110, 6);
			WeDraw.Fill(spriteBatch, bar, Color.White * (0.12f * fade));
			WeDraw.Fill(spriteBatch, new Rectangle(bar.X, bar.Y, Math.Max(1, (int)(bar.Width * MathHelper.Clamp(t, 0f, 1f))), bar.Height), WeAccent.Mid * fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, value ?? "",
				new Vector2(bar.Right + 8, hit.Y + 6), Color.White * (0.85f * fade), 0f, Vector2.Zero, new Vector2(0.62f));
			y += RowStep;
		}

		internal static void Button(SpriteBatch spriteBatch, Rectangle view, ref int y, string label, string value, float fade)
		{
			Cycle(spriteBatch, view, ref y, label, value, fade);
		}

		internal static bool HitRow(Rectangle view, ref int y) =>
			Row(view, y).Contains(Main.mouseX, Main.mouseY);

		internal static Rectangle SliderBar(Rectangle view, int y)
		{
			Rectangle hit = Row(view, y);
			return new Rectangle(hit.Right - 168, hit.Y + 6, 110, 16);
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
