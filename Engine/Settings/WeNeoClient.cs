using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoClient
	{
		private static float _fade;

		internal static void Draw(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			_fade = fade;
			DrawLooks(spriteBatch, view, ref y, false);
			DrawHub(spriteBatch, view, ref y, false);
			DrawWindow(spriteBatch, view, ref y, false);
			DrawMenu(spriteBatch, view, ref y, false);
			DrawAccent(spriteBatch, view, ref y, false);
			DrawFactory(spriteBatch, view, ref y, false);
		}

		internal static void Click(Rectangle view, ref int y, bool left, bool right)
		{
			DrawLooks(null, view, ref y, true, left, right);
			DrawHub(null, view, ref y, true, left, right);
			DrawWindow(null, view, ref y, true, left, right);
			DrawMenu(null, view, ref y, true, left, right);
			DrawAccent(null, view, ref y, true, left, right);
			DrawFactory(null, view, ref y, true, left, right);
		}

		internal static void DrawSearch(SpriteBatch spriteBatch, Rectangle view, ref int y, string q, float fade)
		{
			_fade = fade;
			if (Hit(q, WeText.UI("Looks"), WeText.UI("SaveLook")))
				DrawLooks(spriteBatch, view, ref y, false);
			if (Hit(q, WeText.UI("HubStyle"), WeText.UI("HubStyleRadial"), WeText.UI("HubStyleDock")))
				DrawHub(spriteBatch, view, ref y, false);
			if (Hit(q, WeText.UI("GroupWindow"), WeText.UI("DarkTitleBar"), WeText.UI("PickIcon")))
				DrawWindow(spriteBatch, view, ref y, false);
			if (Hit(q, WeText.UI("GroupMenu"), WeText.UI("ImportFont"), WeText.UI("BtnVanilla")))
				DrawMenu(spriteBatch, view, ref y, false);
			if (Hit(q, WeText.UI("Accent")))
				DrawAccent(spriteBatch, view, ref y, false);
			if (Hit(q, WeText.UI("ResetFactory")))
				DrawFactory(spriteBatch, view, ref y, false);
		}

		internal static void ClickSearch(Rectangle view, ref int y, string q, bool left, bool right)
		{
			if (Hit(q, WeText.UI("Looks"), WeText.UI("SaveLook")))
				DrawLooks(null, view, ref y, true, left, right);
			if (Hit(q, WeText.UI("HubStyle"), WeText.UI("HubStyleRadial"), WeText.UI("HubStyleDock")))
				DrawHub(null, view, ref y, true, left, right);
			if (Hit(q, WeText.UI("GroupWindow"), WeText.UI("DarkTitleBar"), WeText.UI("PickIcon")))
				DrawWindow(null, view, ref y, true, left, right);
			if (Hit(q, WeText.UI("GroupMenu"), WeText.UI("ImportFont"), WeText.UI("BtnVanilla")))
				DrawMenu(null, view, ref y, true, left, right);
			if (Hit(q, WeText.UI("Accent")))
				DrawAccent(null, view, ref y, true, left, right);
			if (Hit(q, WeText.UI("ResetFactory")))
				DrawFactory(null, view, ref y, true, left, right);
		}

		internal static void Slide(string id)
		{
			Rectangle view = WeNeoShell.View(WeNeoShell.Panel());
			float t = SliderT(view);
			int v = (int)(t * 255f);
			if (id is "fw" or "fh") {
				WeSettings.SetFontScale(id == "fw", 0.5f + t * 1.3f);
				return;
			}

			if (id.StartsWith("menu")) {
				Color c = WeSettings.MenuTextColor;
				SetRgb(ref c, id, v);
				WeSettings.SetMenuTextRgb(c.R, c.G, c.B);
				return;
			}

			string which = id.StartsWith("caption") ? "caption" : id.StartsWith("border") ? "border" : "title";
			Color cur = which == "caption" ? WeSettings.CaptionColor : which == "border" ? WeSettings.BorderColor : WeSettings.TitleTextColor;
			SetRgb(ref cur, id, v);
			WeSettings.SetChromeRgb(which, cur.R, cur.G, cur.B);
		}

		private static void SetRgb(ref Color c, string id, int v)
		{
			if (id.EndsWith("R"))
				c.R = (byte)v;
			else if (id.EndsWith("G"))
				c.G = (byte)v;
			else
				c.B = (byte)v;
		}

		private static float SliderT(Rectangle view)
		{
			Rectangle bar = WeNeoShell.SliderBar(view, 0);
			return MathHelper.Clamp((Main.mouseX - bar.X) / (float)Math.Max(1, bar.Width), 0f, 1f);
		}

		private static bool Hit(string q, params string[] parts) => WeNeoShell.Matches(q, parts);

		private static void DrawLooks(SpriteBatch spriteBatch, Rectangle view, ref int y, bool click, bool left = false, bool right = false)
		{
			if (!click)
				WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("Looks"), _fade);
			else
				WeNeoShell.SkipHeader(ref y);

			if (click) {
				if (TwinHit(view, y, out int which) && left) {
					if (which == 0)
						WePresets.SaveCurrent();
					else
						WeFiles.OpenFolder(WeSave.PresetFolder);
				}

				y += 42;
				foreach (WeLookFile look in WePresets.Copy()) {
					Rectangle hit = WeNeoShell.Row(view, y);
					y += WeNeoShell.RowStep;
					if (!hit.Contains(Main.mouseX, Main.mouseY))
						continue;
					if (right)
						WePresets.Delete(look);
					else if (left)
						WePresets.Load(look);
				}

				return;
			}

			DrawTwin(spriteBatch, view, ref y, WeText.UI("SaveLook"), WeText.UI("OpenFolder"));
			foreach (WeLookFile look in WePresets.All)
				WeNeoShell.Cycle(spriteBatch, view, ref y, look.Name, "", _fade);
		}

		private static void DrawHub(SpriteBatch spriteBatch, Rectangle view, ref int y, bool click, bool left = false, bool right = false)
		{
			if (!click)
				WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("HubStyle"), _fade);
			else
				WeNeoShell.SkipHeader(ref y);

			int cellW = (view.Width - 16) / 2;
			var leftR = new Rectangle(view.X + 4, y, cellW - 4, 148);
			var rightR = new Rectangle(leftR.Right + 8, y, cellW - 4, 148);
			if (click) {
				if (left && leftR.Contains(Main.mouseX, Main.mouseY))
					WeSettings.SetWrenchStyle(0);
				if (left && rightR.Contains(Main.mouseX, Main.mouseY))
					WeSettings.SetWrenchStyle(1);
				y += 164;
				return;
			}

			DrawHubCell(spriteBatch, leftR, 0);
			DrawHubCell(spriteBatch, rightR, 1);
			y += 164;
		}

		private static void DrawHubCell(SpriteBatch spriteBatch, Rectangle hit, int style)
		{
			bool on = WeSave.Data.WrenchStyle == style;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (on ? WeAccent.Deep : new Color(22, 24, 30)) * _fade);
			WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : WeAccent.Mid) * _fade);
			if (hover || on)
				WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 4, hit.Height), WeAccent.Hover * _fade);
			var canvas = new Rectangle(hit.X + 8, hit.Y + 8, hit.Width - 16, hit.Height - 36);
			WeDraw.Fill(spriteBatch, canvas, new Color(12, 14, 18) * _fade);
			WrenchToolbar.DrawStylePreview(spriteBatch, canvas, style, _fade, on);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI(style == 1 ? "HubStyleDock" : "HubStyleRadial"),
				new Vector2(hit.X + 12, hit.Bottom - 24), Color.White * _fade, 0f, Vector2.Zero, new Vector2(0.86f));
		}

		private static void DrawWindow(SpriteBatch spriteBatch, Rectangle view, ref int y, bool click, bool left = false, bool right = false)
		{
			if (!click)
				WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("GroupWindow"), _fade);
			else
				WeNeoShell.SkipHeader(ref y);

			var preview = new Rectangle(view.X + 4, y, view.Width - 8, 42);
			if (!click)
				DrawWindowPreview(spriteBatch, preview);
			y += 50;

			int chipW = (view.Width - 20) / 3;
			Rectangle[] chips = {
				new(view.X + 4, y, chipW, 32),
				new(view.X + 8 + chipW, y, chipW, 32),
				new(view.X + 12 + chipW * 2, y, chipW, 32)
			};
			if (click) {
				if (left) {
					for (int i = 0; i < 3; i++) {
						if (chips[i].Contains(Main.mouseX, Main.mouseY))
							WeNeoMenu.SetClientChip(i);
					}
				}

				y += 40;
				ClickRgb(view, ref y, WeNeoMenu.ClientChip == 1 ? "border" : WeNeoMenu.ClientChip == 2 ? "title" : "caption", left);
				if (WeNeoShell.HitRow(view, ref y) && left) {
					WeSave.Data.DarkTitleBar = !WeSave.Data.DarkTitleBar;
					WeSave.Data.ChromeCustom = true;
					WeSave.Save();
					ClientChrome.Apply();
				}

				y += WeNeoShell.RowStep;
				if (TwinHit(view, y, out int which) && left) {
					if (which == 0) {
						if (WeFiles.TryPickIcon(out string path))
							ClientChrome.SetIcon(path);
					}
					else {
						ClientChrome.Reset();
					}
				}

				y += 42;
				if (WeNeoShell.HitRow(view, ref y) && left)
					WeSplash.Show();
				y += WeNeoShell.RowStep;
				y += 28;
				return;
			}

			DrawChip(spriteBatch, chips[0], 0, WeSettings.CaptionColor, WeText.UI("ChipCaption"));
			DrawChip(spriteBatch, chips[1], 1, WeSettings.BorderColor, WeText.UI("ChipBorder"));
			DrawChip(spriteBatch, chips[2], 2, WeSettings.TitleTextColor, WeText.UI("ChipTitle"));
			y += 40;
			string key = WeNeoMenu.ClientChip == 1 ? "border" : WeNeoMenu.ClientChip == 2 ? "title" : "caption";
			Color color = WeNeoMenu.ClientChip == 1 ? WeSettings.BorderColor : WeNeoMenu.ClientChip == 2 ? WeSettings.TitleTextColor : WeSettings.CaptionColor;
			DrawRgb(spriteBatch, view, ref y, key, color);
			WeNeoShell.Toggle(spriteBatch, view, ref y, WeText.UI("DarkTitleBar"), WeSave.Data.DarkTitleBar, _fade);
			DrawTwin(spriteBatch, view, ref y, WeText.UI("PickIcon"), WeText.UI("ResetChrome"));
			WeNeoShell.Button(spriteBatch, view, ref y, WeText.UI("ShowHelp"), "", _fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI(WeOs.IsWindows ? "BorderlessHint" : "ChromeOsHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * _fade), 0f, Vector2.Zero, new Vector2(0.72f));
			y += 28;
		}

		private static void DrawWindowPreview(SpriteBatch spriteBatch, Rectangle bar)
		{
			Color cap = WeSettings.CaptionColor;
			if (!WeSave.Data.ChromeCustom)
				cap = WeSave.Data.DarkTitleBar ? new Color(32, 32, 32) : new Color(240, 240, 240);
			WeDraw.Fill(spriteBatch, bar, cap * _fade);
			WeDraw.Border(spriteBatch, bar, WeSettings.BorderColor * _fade);
			Color text = WeSave.Data.ChromeCustom ? WeSettings.TitleTextColor : (WeSave.Data.DarkTitleBar ? Color.White : new Color(32, 32, 32));
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, "Terraria",
				new Vector2(bar.X + 28, bar.Y + 10), text * _fade, 0f, Vector2.Zero, new Vector2(0.86f));
		}

		private static void DrawChip(SpriteBatch spriteBatch, Rectangle hit, int index, Color color, string label)
		{
			bool on = WeNeoMenu.ClientChip == index;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (on ? WeAccent.Deep : new Color(22, 24, 30)) * _fade);
			WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : WeAccent.Mid) * _fade);
			WeDraw.Fill(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 8, 16, 16), color * _fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, label,
				new Vector2(hit.X + 30, hit.Y + 7), Color.White * _fade, 0f, Vector2.Zero, new Vector2(0.72f));
		}

		private static void DrawMenu(SpriteBatch spriteBatch, Rectangle view, ref int y, bool click, bool left = false, bool right = false)
		{
			if (!click)
				WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("GroupMenu"), _fade);
			else
				WeNeoShell.SkipHeader(ref y);

			int styleW = (view.Width - 28) / 4;
			if (click) {
				if (left) {
					for (int i = 0; i < 4; i++) {
						var cell = new Rectangle(view.X + 4 + i * (styleW + 6), y, styleW, 64);
						if (cell.Contains(Main.mouseX, Main.mouseY))
							WeSettings.SetButtonStyle(i);
					}
				}

				y += 74;
				if (WeNeoShell.HitRow(view, ref y) && left)
					WeSettings.ToggleMenuTextCustom();
				y += WeNeoShell.RowStep;
				if (WeSave.Data.MenuTextCustom)
					ClickRgb(view, ref y, "menu", left);
				if (WeNeoShell.HitRow(view, ref y) && left)
					WeType.Clear();
				y += WeNeoShell.RowStep;
				if (TwinHit(view, y, out int which) && left) {
					if (which == 0)
						WeType.TryImport();
					else
						WeFiles.OpenFolder(WeSave.FontFolder);
				}

				y += 42;
				y += 24;
				foreach (WeFontOffer offer in WeType.All) {
					Rectangle hit = WeNeoShell.Row(view, y);
					y += WeNeoShell.RowStep;
					if (!hit.Contains(Main.mouseX, Main.mouseY))
						continue;
					if (right)
						WeType.Delete(offer);
					else if (left)
						WeType.Select(offer.FileName);
				}

				ClickSliderId(view, ref y, "fw", left);
				ClickSliderId(view, ref y, "fh", left);
				return;
			}

			string sample = WeText.UI("MenuPreview");
			for (int i = 0; i < 4; i++) {
				var cell = new Rectangle(view.X + 4 + i * (styleW + 6), y, styleW, 64);
				bool on = WeSave.Data.ButtonStyle == i;
				bool hover = cell.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, cell, (on ? WeAccent.Deep : new Color(22, 24, 30)) * _fade);
				WeDraw.Border(spriteBatch, cell, (on || hover ? WeAccent.Light : WeAccent.Mid) * _fade);
				WeDraw.WithClip(spriteBatch, cell, () => WeLook.DrawPreview(
					spriteBatch, sample, new Vector2(cell.Center.X, cell.Y + 22),
					WeLook.MenuIdle, _fade, 0.28f, i));
				string key = i switch { 1 => "BtnOutline", 2 => "BtnAccent", 3 => "BtnPlate", _ => "BtnVanilla" };
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(key),
					new Vector2(cell.X + 6, cell.Bottom - 18), Color.White * _fade, 0f, Vector2.Zero, new Vector2(0.64f));
			}

			y += 74;
			WeNeoShell.Toggle(spriteBatch, view, ref y, WeText.UI("MenuTextCustom"), WeSave.Data.MenuTextCustom, _fade);
			if (WeSave.Data.MenuTextCustom)
				DrawRgb(spriteBatch, view, ref y, "menu", WeSettings.MenuTextColor);
			WeNeoShell.Toggle(spriteBatch, view, ref y, WeText.UI("FontVanilla"), string.IsNullOrEmpty(WeSave.Data.FontFile), _fade);
			DrawTwin(spriteBatch, view, ref y, WeText.UI("ImportFont"), WeText.UI("OpenFolder"));
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI(WeOs.IsWindows ? "FontHint" : "FontOsHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * _fade), 0f, Vector2.Zero, new Vector2(0.72f));
			y += 24;
			foreach (WeFontOffer offer in WeType.All) {
				Rectangle hit = WeNeoShell.Row(view, y);
				bool on = string.Equals(WeSave.Data.FontFile, offer.FileName, StringComparison.OrdinalIgnoreCase);
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (on ? WeAccent.Deep : new Color(22, 24, 30)) * _fade);
				WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : WeAccent.Mid) * _fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, offer.Family,
					new Vector2(hit.X + 12, hit.Y + 10), Color.White * _fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.Type));
				y += WeNeoShell.RowStep;
			}

			WeNeoShell.Slider(spriteBatch, view, ref y, WeText.UI("FontWidth"), (WeLook.FontScaleX - 0.5f) / 1.3f, "", _fade);
			WeNeoShell.Slider(spriteBatch, view, ref y, WeText.UI("FontHeight"), (WeLook.FontScaleY - 0.5f) / 1.3f, "", _fade);
		}

		private static void DrawAccent(SpriteBatch spriteBatch, Rectangle view, ref int y, bool click, bool left = false, bool right = false)
		{
			if (!click)
				WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("Accent"), _fade);
			else
				WeNeoShell.SkipHeader(ref y);

			const int cols = 4;
			int cellW = (view.Width - 28) / cols;
			const int cellH = 48;
			const int stride = 56;
			int gridH = stride * 2 + 28;
			if (click) {
				if (left) {
					for (int i = 0; i < WeAccent.Palettes.Length; i++) {
						int col = i % cols;
						int row = i / cols;
						var hit = new Rectangle(view.X + 4 + col * (cellW + 6), y + row * stride, cellW, cellH);
						if (hit.Contains(Main.mouseX, Main.mouseY))
							WeAccent.Set(i);
					}
				}

				y += gridH;
				return;
			}

			for (int i = 0; i < WeAccent.Palettes.Length; i++) {
				int col = i % cols;
				int row = i / cols;
				var hit = new Rectangle(view.X + 4 + col * (cellW + 6), y + row * stride, cellW, cellH);
				bool on = i == WeAccent.Index;
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, WeAccent.Palettes[i].Mid * _fade);
				if (on || hover)
					WeDraw.Border(spriteBatch, hit, Color.White * _fade);
			}

			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, AccentName(WeAccent.Index),
				new Vector2(view.X + 8, y + stride * 2 + 4), Color.White * _fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.Type));
			y += gridH;
		}

		private static string AccentName(int i)
		{
			if (i < 0 || i >= WeAccent.Palettes.Length)
				i = 0;
			string key = WeAccent.Palettes[i].Key;
			if (string.IsNullOrEmpty(key))
				return "";
			return WeText.UI("Accent" + char.ToUpperInvariant(key[0]) + key.Substring(1));
		}

		private static void DrawFactory(SpriteBatch spriteBatch, Rectangle view, ref int y, bool click, bool left = false, bool right = false)
		{
			if (!click)
				WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("ResetFactory"), _fade);
			else
				WeNeoShell.SkipHeader(ref y);

			if (click) {
				if (WeNeoShell.HitRow(view, ref y) && left) {
					if (!WeNeoMenu.FactoryArmed)
						WeNeoMenu.SetFactoryArmed(true);
					else {
						WeNeoMenu.SetFactoryArmed(false);
						WeSettings.ResetToPackDefaults();
					}
				}
				else if (left)
					WeNeoMenu.SetFactoryArmed(false);

				y += WeNeoShell.RowStep;
				y += 44;
				return;
			}

			WeNeoShell.Toggle(spriteBatch, view, ref y, WeText.UI(WeNeoMenu.FactoryArmed ? "ResetFactorySure" : "ResetFactory"), WeNeoMenu.FactoryArmed, _fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("ResetFactoryHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * _fade), 0f, Vector2.Zero, new Vector2(0.7f));
			y += 44;
		}

		private static void DrawRgb(SpriteBatch spriteBatch, Rectangle view, ref int y, string key, Color color)
		{
			WeNeoShell.Slider(spriteBatch, view, ref y, WeText.UI("Red"), color.R / 255f, color.R.ToString(), _fade);
			WeNeoShell.Slider(spriteBatch, view, ref y, WeText.UI("Green"), color.G / 255f, color.G.ToString(), _fade);
			WeNeoShell.Slider(spriteBatch, view, ref y, WeText.UI("Blue"), color.B / 255f, color.B.ToString(), _fade);
		}

		private static void ClickRgb(Rectangle view, ref int y, string key, bool left)
		{
			ClickSliderId(view, ref y, key + "R", left);
			ClickSliderId(view, ref y, key + "G", left);
			ClickSliderId(view, ref y, key + "B", left);
		}

		private static void ClickSliderId(Rectangle view, ref int y, string id, bool left)
		{
			Rectangle bar = WeNeoShell.SliderBar(view, y);
			y += WeNeoShell.RowStep;
			if (left && bar.Contains(Main.mouseX, Main.mouseY)) {
				WeNeoMenu.SetDrag(id);
				Slide(id);
			}
		}

		private static void DrawTwin(SpriteBatch spriteBatch, Rectangle view, ref int y, string a, string b)
		{
			int w = (view.Width - 16) / 2;
			var left = new Rectangle(view.X + 4, y, w - 4, 32);
			var right = new Rectangle(left.Right + 8, y, w - 4, 32);
			DrawMini(spriteBatch, left, a);
			DrawMini(spriteBatch, right, b);
			y += 42;
		}

		private static bool TwinHit(Rectangle view, int y, out int which)
		{
			int w = (view.Width - 16) / 2;
			var left = new Rectangle(view.X + 4, y, w - 4, 32);
			var right = new Rectangle(left.Right + 8, y, w - 4, 32);
			which = left.Contains(Main.mouseX, Main.mouseY) ? 0 : right.Contains(Main.mouseX, Main.mouseY) ? 1 : -1;
			return which >= 0;
		}

		private static void DrawMini(SpriteBatch spriteBatch, Rectangle hit, string text)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(32, 36, 44)) * ((hover ? 0.95f : 0.8f) * _fade));
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * _fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * 0.72f;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				Color.White * _fade, 0f, Vector2.Zero, new Vector2(0.72f));
		}
	}
}
