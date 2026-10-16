using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.UI
{
	internal static partial class WePanels
	{
		private static bool _factoryArmed;

		private static int LooksGroupH(int n) => 32 + 40 + HintH + n * 42 + 14;

		private static int FactoryGroupH() => 32 + 42 + HintH + 14;

		private static void DrawGroupShell(SpriteBatch spriteBatch, Rectangle panel, int y, int h, string title)
		{
			if (spriteBatch == null)
				return;
			var card = new Rectangle(panel.X + 10, y, panel.Width - 20, h);
			WeTerra.WellBox(spriteBatch, card, _fade);
			WeTerra.Label(spriteBatch, title, new Vector2(card.X + 12, y + 8), WeTerra.Title * _fade, 0.8f);
		}

		private static void ClientLooks(SpriteBatch spriteBatch, Rectangle panel, ref int y, bool click)
		{
			WeLookFile[] looks = click ? WePresets.Copy() : null;
			int n = click ? looks.Length : WePresets.All.Count;
			int top = y;
			int h = LooksGroupH(n);
			DrawGroupShell(spriteBatch, panel, top, h, WeText.UI("Looks"));
			y = top + 32;
			if (click) {
				if (ClickRow(panel, ref y, out int which)) {
					if (which == 0)
						WePresets.SaveCurrent();
					else
						WeFiles.OpenFolder(WeSave.PresetFolder);
				}

				SkipHint(ref y);
				foreach (WeLookFile look in looks) {
					if (!ClickLook(panel, ref y, out bool trash))
						continue;
					if (trash)
						WePresets.Delete(look);
					else
						WePresets.Load(look);
				}

				y = top + h + 10;
				return;
			}

			DrawButtonRow(spriteBatch, panel, ref y, WeText.UI("SaveLook"), WeText.UI("OpenFolder"), WeIcons.Star, WeIcons.Folder);
			DrawHint(spriteBatch, panel, ref y, WeText.UI("LookHint"));
			foreach (WeLookFile look in WePresets.All)
				DrawCard(spriteBatch, panel, ref y, look.Name, false, CardKind.Action);
			y = top + h + 10;
		}

		private static void ClientHub(SpriteBatch spriteBatch, Rectangle panel, ref int y, bool click)
		{
			const int h = 140;
			int top = y;
			DrawGroupShell(spriteBatch, panel, top, h, WeText.UI("HubStyle"));
			int inner = top + 32;
			const int styles = 3;
			int cellW = (panel.Width - 40 - 8 * (styles - 1)) / styles;
			for (int i = 0; i < styles; i++) {
				var cell = new Rectangle(panel.X + 20 + i * (cellW + 8), inner, cellW, 96);
				if (click) {
					if (cell.Contains(Main.mouseX, Main.mouseY))
						WeSettings.SetWrenchStyle(i);
				}
				else
					DrawHubCell(spriteBatch, cell, i);
			}

			y = top + h + 10;
		}

		private static void DrawHubCell(SpriteBatch spriteBatch, Rectangle hit, int style)
		{
			bool on = WeSave.Data.WrenchStyle == style;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			var preview = new Rectangle(hit.X + 8, hit.Y + 8, hit.Width - 16, hit.Height - 30);
			Thumb(spriteBatch, preview, null);
			WeDraw.WithClip(spriteBatch, preview, () => WrenchToolbar.DrawStylePreview(spriteBatch, preview, style, _fade, on));
			string title = WeText.UI(style == 2 ? "HubStyleSidebar" : style == 1 ? "HubStyleDock" : "HubStyleRadial");
			WeTerra.Label(spriteBatch, WeTerra.Fit(title, 0.7f, hit.Width - 34), new Vector2(hit.X + 10, hit.Bottom - 20), Color.White * _fade, 0.7f);
			if (on)
				WeTerra.Icon(spriteBatch, WeIcons.Check, new Vector2(hit.Right - 14, hit.Bottom - 13), 16f, Color.White * _fade);
		}

		// ---- Window group ---------------------------------------------------------------------------
		// One layout function runs in three modes so drawing, clicking and measuring can never disagree.
		private const int ModeDraw = 0;
		private const int ModeClick = 1;
		private const int ModeMeasure = 2;

		private static Texture2D _iconPreview;
		private static string _iconPreviewFile = "";

		private static bool UCard(SpriteBatch sb, Rectangle panel, ref int y, int mode, string text, bool on, CardKind kind = CardKind.Toggle)
		{
			if (mode == ModeClick)
				return ClickCard(panel, ref y);
			if (mode == ModeDraw)
				DrawCard(sb, panel, ref y, text, on, kind);
			else
				y += 42;
			return false;
		}

		private static int URow(SpriteBatch sb, Rectangle panel, ref int y, int mode, string a, string b, string ia, string ib)
		{
			if (mode == ModeClick)
				return ClickRow(panel, ref y, out int which) ? which : -1;
			if (mode == ModeDraw)
				DrawButtonRow(sb, panel, ref y, a, b, ia, ib);
			else
				y += 40;
			return -1;
		}

		private static void UHint(SpriteBatch sb, Rectangle panel, ref int y, int mode, string text)
		{
			if (mode == ModeDraw)
				DrawHint(sb, panel, ref y, text);
			else
				SkipHint(ref y);
		}

		private static void ChromePreset(int i, out Color cap, out Color bor, out Color txt)
		{
			switch (i) {
				case 0:
					cap = Color.Lerp(WeAccent.Deep, Color.Black, 0.35f);
					bor = WeAccent.Mid;
					txt = Color.White;
					break;
				case 1:
					cap = new Color(58, 12, 20);
					bor = new Color(196, 48, 60);
					txt = new Color(255, 224, 224);
					break;
				case 2:
					cap = new Color(16, 16, 24);
					bor = new Color(44, 44, 60);
					txt = new Color(230, 230, 240);
					break;
				default:
					cap = new Color(240, 240, 240);
					bor = new Color(200, 200, 200);
					txt = new Color(30, 30, 30);
					break;
			}
		}

		private static readonly string[] ChromePresetKeys = { "ChromeTheme", "ChromeCalamity", "ChromeNight", "ChromeLight" };

		private static string TitleLabel(int mode) => mode switch {
			ClientChrome.TitlePlain => "Terraria",
			ClientChrome.TitleTheme => WeText.UI("TitleTheme"),
			_ => WeText.UI("TitleVanilla")
		};

		private static int WindowGroupH(Rectangle panel)
		{
			int y = 0;
			WindowBody(null, panel, ref y, ModeMeasure);
			return y + 14;
		}

		private static void ClientWindow(SpriteBatch spriteBatch, Rectangle panel, ref int y, bool click)
		{
			int top = y;
			int h = WindowGroupH(panel);
			if (!click)
				DrawGroupShell(spriteBatch, panel, top, h, WeText.UI("GroupWindow"));
			int inner = top;
			WindowBody(spriteBatch, panel, ref inner, click ? ModeClick : ModeDraw);
			y = top + h + 10;
		}

		/// <summary>Lays out the Window group from <paramref name="y"/> (the top of the shell).</summary>
		private static void WindowBody(SpriteBatch sb, Rectangle panel, ref int y, int mode)
		{
			y += 32;
			var preview = new Rectangle(panel.X + 20, y, panel.Width - 40, 86);
			if (mode == ModeDraw)
				DrawWindowPreview(sb, preview);
			y += 94;

			bool colors = ClientChrome.ColorsSupported;
			bool dark = ClientChrome.DarkSupported;
			WeSaveData data = WeSave.Data;
			if (colors) {
				if (UCard(sb, panel, ref y, mode, WeText.UI("ChromeCustom"), data.ChromeCustom)) {
					if (data.ChromeCustom)
						ClientChrome.ResetColors();
					else {
						data.ChromeCustom = true;
						WeSave.Save();
						ClientChrome.Apply();
					}

					WeToast.Show("ToastChrome");
					return;
				}

				if (data.ChromeCustom) {
					int chipW = (panel.Width - 56) / 3;
					for (int i = 0; i < 3; i++) {
						var chip = new Rectangle(panel.X + 20 + i * (chipW + 8), y, chipW, 30);
						if (mode == ModeClick && chip.Contains(Main.mouseX, Main.mouseY)) {
							_clientChip = i;
							return;
						}

						if (mode == ModeDraw) {
							Color c = i == 1 ? WeSettings.BorderColor : i == 2 ? WeSettings.TitleTextColor : WeSettings.CaptionColor;
							DrawWindowChip(sb, chip, i, c, WeText.UI(i == 1 ? "ChipBorder" : i == 2 ? "ChipTitle" : "ChipCaption"));
						}
					}

					y += 38;
					string key = _clientChip == 1 ? "border" : _clientChip == 2 ? "title" : "caption";
					if (mode == ModeClick)
						ClickRgb(panel, ref y, key);
					else if (mode == ModeDraw) {
						Color c = _clientChip == 1 ? WeSettings.BorderColor : _clientChip == 2 ? WeSettings.TitleTextColor : WeSettings.CaptionColor;
						DrawRgb(sb, panel, ref y, key, c);
					}
					else
						y += 74;

					// quick looks
					int cellW = (panel.Width - 40 - 6 * 3) / 4;
					for (int i = 0; i < 4; i++) {
						var cell = new Rectangle(panel.X + 20 + i * (cellW + 6), y, cellW, 30);
						ChromePreset(i, out Color cap, out Color bor, out Color txt);
						if (mode == ModeClick && cell.Contains(Main.mouseX, Main.mouseY)) {
							WeSettings.SetChromeRgb("caption", cap.R, cap.G, cap.B);
							WeSettings.SetChromeRgb("border", bor.R, bor.G, bor.B);
							WeSettings.SetChromeRgb("title", txt.R, txt.G, txt.B);
							WeToast.Show("ToastChrome");
							return;
						}

						if (mode == ModeDraw) {
							bool hover = cell.Contains(Main.mouseX, Main.mouseY);
							WeTerra.PixelRound(sb, cell, 6, cap * _fade, (hover ? Color.White : bor) * _fade, 2);
							string label = WeTerra.Fit(WeText.UI(ChromePresetKeys[i]), 0.62f, cell.Width - 8);
							WeTerra.LabelCentered(sb, label, cell.Center.ToVector2(), txt * _fade, 0.62f);
						}
					}

					y += 38;
				}
			}

			if (dark && UCard(sb, panel, ref y, mode, WeText.UI("DarkTitleBar"), data.DarkTitleBar)) {
				data.DarkTitleBar = !data.DarkTitleBar;
				data.ChromeCustom = true;
				WeSave.Save();
				ClientChrome.Apply();
				WeToast.Show("ToastChrome");
				return;
			}

			if (WeOs.IsWindows && !colors)
				UHint(sb, panel, ref y, mode, WeText.UI("TitleColorsWin11"));
			else if (!WeOs.IsWindows)
				UHint(sb, panel, ref y, mode, WeText.UI("ChromeOsHint"));

			int row = URow(sb, panel, ref y, mode, WeText.UI("PickIcon"), WeText.UI("UseModIcon"), WeIcons.Image, WeIcons.Star);
			if (row == 0) {
				if (WeFiles.TryPickIcon(out string path))
					ClientChrome.SetIcon(path);
				return;
			}

			if (row == 1) {
				ClientChrome.UseModIcon();
				return;
			}

			int titleMode = data.WindowTitleMode;
			if (UCard(sb, panel, ref y, mode, WeText.UI("WindowTitle") + ":  " + TitleLabel(titleMode), false, CardKind.Cycle)) {
				ClientChrome.SetTitleMode((titleMode + 1) % 3);
				return;
			}

			if (UCard(sb, panel, ref y, mode, WeText.UI("ResetChrome"), false, CardKind.Action)) {
				ClientChrome.Reset();
				WeToast.Show("ToastReset");
				return;
			}

			if (UCard(sb, panel, ref y, mode, WeText.UI("ShowHelp"), false, CardKind.Action)) {
				WeSplash.Show();
				return;
			}

			if (colors)
				UHint(sb, panel, ref y, mode, WeText.UI("BorderlessHint"));
		}

		private static void DrawWindowChip(SpriteBatch spriteBatch, Rectangle hit, int index, Color color, string label)
		{
			bool on = _clientChip == index;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			var sw = new Rectangle(hit.X + 8, hit.Y + 7, 16, 16);
			WeDraw.Fill(spriteBatch, new Rectangle(sw.X - 2, sw.Y - 2, sw.Width + 4, sw.Height + 4), Color.Black * _fade);
			WeDraw.Fill(spriteBatch, sw, color * _fade);
			WeTerra.Label(spriteBatch, WeTerra.Fit(label, 0.68f, hit.Width - 38), new Vector2(hit.X + 30, hit.Y + 7), Color.White * _fade, 0.68f);
		}

		/// <summary>A small mock of the game window: title bar with the real title, icon and colours.</summary>
		private static void DrawWindowPreview(SpriteBatch spriteBatch, Rectangle box)
		{
			WeSaveData data = WeSave.Data;
			bool custom = data.ChromeCustom && ClientChrome.ColorsSupported;
			bool darkBar = !WeOs.IsWindows || !data.ChromeCustom || data.DarkTitleBar;
			Color cap = custom ? WeSettings.CaptionColor : darkBar ? new Color(32, 32, 32) : new Color(240, 240, 240);
			Color bor = custom ? WeSettings.BorderColor : darkBar ? new Color(52, 52, 52) : new Color(200, 200, 200);
			Color txt = custom ? WeSettings.TitleTextColor : darkBar ? Color.White : new Color(32, 32, 32);

			WeDraw.Fill(spriteBatch, new Rectangle(box.X + 3, box.Y + 5, box.Width, box.Height), Color.Black * (0.35f * _fade));
			WeDraw.Fill(spriteBatch, box, bor * _fade);
			var bar = new Rectangle(box.X + 2, box.Y + 2, box.Width - 4, 28);
			WeDraw.Fill(spriteBatch, bar, cap * _fade);
			var body = new Rectangle(box.X + 2, bar.Bottom, box.Width - 4, box.Bottom - 2 - bar.Bottom);
			WeDraw.Fill(spriteBatch, body, new Color(14, 12, 22) * _fade);
			// a hint of the menu inside the window
			for (int i = 0; i < 3; i++) {
				int w = 70 - i * 14;
				WeDraw.Fill(spriteBatch, new Rectangle(body.Center.X - w / 2, body.Y + 12 + i * 12, w, 5), (i == 1 ? WeAccent.Mid : WeTerra.RowBack) * _fade);
			}

			var icon = new Rectangle(bar.X + 8, bar.Y + 6, 16, 16);
			Texture2D tex = IconPreview();
			if (tex != null)
				spriteBatch.Draw(tex, icon, Color.White * _fade);
			else
				WeTerra.Icon(spriteBatch, WeIcons.Client, icon.Center.ToVector2(), 16f, Color.White * _fade);

			string title = data.WindowTitleMode == ClientChrome.TitleVanilla ? CurrentTitle() : ClientChrome.TitleText(data.WindowTitleMode);
			WeTerra.Label(spriteBatch, WeTerra.Fit(title, 0.66f, bar.Width - 110), new Vector2(icon.Right + 8, bar.Y + 7), txt * _fade, 0.66f);

			// minimise / maximise / close glyphs
			int gx = bar.Right - 72;
			int cy = bar.Center.Y;
			WeDraw.Fill(spriteBatch, new Rectangle(gx, cy, 10, 1), txt * _fade);
			WeDraw.Border(spriteBatch, new Rectangle(gx + 24, cy - 5, 10, 10), txt * (0.9f * _fade));
			for (int i = 0; i < 9; i++) {
				WeDraw.Fill(spriteBatch, new Rectangle(gx + 48 + i, cy - 4 + i, 1, 1), txt * _fade);
				WeDraw.Fill(spriteBatch, new Rectangle(gx + 56 - i, cy - 4 + i, 1, 1), txt * _fade);
			}

			if (!WeOs.IsWindows) {
				string os = WeOs.IsMac ? "macOS" : "Linux";
				Vector2 size = WeTerra.Measure(os, 0.58f);
				var tag = new Rectangle(body.Right - (int)size.X - 16, body.Bottom - 22, (int)size.X + 10, 18);
				WeTerra.PixelRound(spriteBatch, tag, 5, WeTerra.RowBack * _fade, Color.Black * _fade, 1);
				WeTerra.Label(spriteBatch, os, new Vector2(tag.X + 5, tag.Y + 2), WeTerra.Muted * _fade, 0.58f);
			}
		}

		private static string CurrentTitle()
		{
			try {
				string t = Main.instance?.Window?.Title;
				return string.IsNullOrWhiteSpace(t) ? "Terraria" : t;
			}
			catch {
				return "Terraria";
			}
		}

		private static Texture2D IconPreview()
		{
			string file = WeSave.Data.WindowIconFile ?? "";
			if (file == _iconPreviewFile)
				return _iconPreview != null && !_iconPreview.IsDisposed ? _iconPreview : null;

			_iconPreviewFile = file;
			_iconPreview?.Dispose();
			_iconPreview = null;
			if (file.Length == 0)
				return null;
			try {
				string path = System.IO.Path.Combine(WeSave.IconFolder, file);
				if (!System.IO.File.Exists(path) || path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
					return null;
				using var stream = System.IO.File.OpenRead(path);
				_iconPreview = Texture2D.FromStream(Main.instance.GraphicsDevice, stream);
			}
			catch {
				_iconPreview = null;
			}

			return _iconPreview;
		}

		// ---- Menu text group: preview, button style, animation, colour, font --------------------------
		private static int MenuGroupH(Rectangle panel)
		{
			int y = 0;
			MenuBody(null, panel, ref y, ModeMeasure);
			return y + 14;
		}

		private static void ClientMenu(SpriteBatch spriteBatch, Rectangle panel, ref int y, bool click)
		{
			int top = y;
			int h = MenuGroupH(panel);
			if (!click)
				DrawGroupShell(spriteBatch, panel, top, h, WeText.UI("GroupMenu"));
			int inner = top;
			MenuBody(spriteBatch, panel, ref inner, click ? ModeClick : ModeDraw);
			y = top + h + 10;
		}

		private static void SubLabel(SpriteBatch sb, Rectangle panel, ref int y, int mode, string text)
		{
			if (mode == ModeDraw)
				WeTerra.Label(sb, text, new Vector2(panel.X + 20, y + 2), WeTerra.Muted * _fade, 0.66f);
			y += 22;
		}

		private static void MenuBody(SpriteBatch sb, Rectangle panel, ref int y, int mode)
		{
			y += 32;
			var preview = new Rectangle(panel.X + 20, y, panel.Width - 40, 58);
			if (mode == ModeDraw)
				DrawMenuLive(sb, preview);
			y += 66;

			// button style
			SubLabel(sb, panel, ref y, mode, WeText.UI("ButtonStyle"));
			string sample = WeText.UI("MenuPreview");
			int styleW = (panel.Width - 40 - 8 * 3) / 4;
			for (int i = 0; i < 4; i++) {
				var cell = new Rectangle(panel.X + 20 + i * (styleW + 8), y, styleW, 60);
				if (mode == ModeClick && cell.Contains(Main.mouseX, Main.mouseY)) {
					WeSettings.SetButtonStyle(i);
					return;
				}

				if (mode != ModeDraw)
					continue;
				bool on = WeSave.Data.ButtonStyle == i;
				Card(sb, cell, cell.Contains(Main.mouseX, Main.mouseY), on);
				int style = i;
				WeDraw.WithClip(sb, cell, () => WeLook.DrawPreview(sb, sample, new Vector2(cell.Center.X, cell.Y + 22), WeLook.MenuIdle, _fade, 0.26f, style, WeLook.FxNone));
				WeTerra.LabelCentered(sb, WeTerra.Fit(WeText.UI(i switch { 1 => "BtnOutline", 2 => "BtnAccent", 3 => "BtnPlate", _ => "BtnVanilla" }), 0.58f, cell.Width - 6),
					new Vector2(cell.Center.X, cell.Bottom - 12), Color.White * _fade, 0.58f);
			}

			y += 68;

			// animation
			SubLabel(sb, panel, ref y, mode, WeText.UI("TextFxTitle"));
			const int cols = 3;
			int fxW = (panel.Width - 40 - 8 * (cols - 1)) / cols;
			for (int i = 0; i < WeLook.FxCount; i++) {
				var cell = new Rectangle(panel.X + 20 + i % cols * (fxW + 8), y + i / cols * 54, fxW, 48);
				if (mode == ModeClick && cell.Contains(Main.mouseX, Main.mouseY)) {
					WeSave.Data.TextFx = i;
					WeSave.Save();
					return;
				}

				if (mode != ModeDraw)
					continue;
				bool on = WeLook.TextFx == i;
				bool hover = cell.Contains(Main.mouseX, Main.mouseY);
				Card(sb, cell, hover, on);
				int fx = i;
				WeDraw.WithClip(sb, cell, () => WeLook.DrawPreview(sb, sample, new Vector2(cell.Center.X, cell.Y + 19),
					hover ? WeAccent.Hover : WeLook.MenuIdle, _fade, 0.25f, null, fx, hover));
				WeTerra.LabelCentered(sb, WeTerra.Fit(WeText.UI(WeLook.FxKey(i)), 0.58f, cell.Width - 6),
					new Vector2(cell.Center.X, cell.Bottom - 11), Color.White * _fade, 0.58f);
			}

			y += (WeLook.FxCount + cols - 1) / cols * 54 + 4;

			if (UCard(sb, panel, ref y, mode, WeText.UI("ThemeLineFx"), !WeSave.Data.PlainThemeLine)) {
				WeSave.Data.PlainThemeLine = !WeSave.Data.PlainThemeLine;
				WeSave.Save();
				return;
			}

			// colour
			if (UCard(sb, panel, ref y, mode, WeText.UI("MenuTextCustom"), WeSave.Data.MenuTextCustom)) {
				WeSettings.ToggleMenuTextCustom();
				return;
			}

			if (WeSave.Data.MenuTextCustom) {
				if (mode == ModeClick)
					ClickRgb(panel, ref y, "menu");
				else if (mode == ModeDraw)
					DrawRgb(sb, panel, ref y, "menu", WeSettings.MenuTextColor);
				else
					y += 74;
			}

			// font
			SubLabel(sb, panel, ref y, mode, WeText.UI("FontTitle"));
			if (UCard(sb, panel, ref y, mode, WeText.UI("FontVanilla"), string.IsNullOrEmpty(WeSave.Data.FontFile), CardKind.Radio)) {
				WeType.Clear();
				return;
			}

			WeFontOffer[] offers = SnapshotFonts();
			foreach (WeFontOffer offer in offers) {
				if (mode == ModeDraw) {
					DrawFontRow(sb, panel, ref y, offer);
					continue;
				}

				if (mode == ModeMeasure) {
					y += 50;
					continue;
				}

				if (!ClickFont(panel, ref y, out bool trash))
					continue;
				if (trash)
					WeType.Delete(offer);
				else {
					WeType.Select(offer.FileName);
					if (!WeType.Active)
						WeToast.Show("ToastFontFail");
				}

				return;
			}

			int row = URow(sb, panel, ref y, mode, WeText.UI("ImportFont"), WeText.UI("OpenFolder"), WeIcons.Font, WeIcons.Folder);
			if (row == 0) {
				WeType.TryImport();
				return;
			}

			if (row == 1) {
				WeFiles.OpenFolder(WeSave.FontFolder);
				return;
			}

			UHint(sb, panel, ref y, mode, WeText.UI("FontHint"));
			if (UCard(sb, panel, ref y, mode, WeText.UI("FontInPanels"), WeSave.Data.FontInPanels)) {
				WeSave.Data.FontInPanels = !WeSave.Data.FontInPanels;
				WeSave.Save();
				return;
			}

			if (mode == ModeClick) {
				ClickSlider(panel, ref y, "fw");
				ClickSlider(panel, ref y, "fh");
			}
			else if (mode == ModeDraw) {
				DrawSlider(sb, panel, ref y, "fw", (WeLook.FontScaleX - 0.5f) / 1.3f, WeText.UI("FontWidth"));
				DrawSlider(sb, panel, ref y, "fh", (WeLook.FontScaleY - 0.5f) / 1.3f, WeText.UI("FontHeight"));
			}
			else
				y += 44;
		}

		private static void DrawMenuLive(SpriteBatch spriteBatch, Rectangle box)
		{
			WeTerra.PixelRound(spriteBatch, box, 8, new Color(10, 9, 16) * _fade, WeAccent.Mid * (0.55f * _fade), 2);
			string sample = WeText.UI("MenuPreview");
			float wave = (MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f) + 1f) * 0.5f;
			Color idle = WeLook.MenuIdle;
			Color hover = WeAccent.Hover;
			WeDraw.WithClip(spriteBatch, box, () => {
				WeLook.DrawPreview(spriteBatch, sample, new Vector2(box.X + box.Width * 0.28f, box.Center.Y), idle, _fade, 0.38f, null, WeLook.TextFx);
				WeLook.DrawPreview(spriteBatch, sample, new Vector2(box.X + box.Width * 0.72f, box.Center.Y),
					Color.Lerp(idle, hover, 0.35f + 0.65f * wave), _fade, 0.38f, null, WeLook.TextFx, true);
			});
		}

		private static void DrawFontRow(SpriteBatch spriteBatch, Rectangle panel, ref int y, WeFontOffer offer)
		{
			Rectangle hit = Row(panel, y, 44);
			bool on = string.Equals(WeSave.Data.FontFile, offer.FileName, StringComparison.OrdinalIgnoreCase);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			var aa = new Rectangle(hit.X + 8, hit.Y + 6, 56, 32);
			WeTerra.PixelRound(spriteBatch, aa, 4, new Color(10, 9, 16) * _fade);
			Texture2D prev = WeType.PreviewOf(offer.FileName);
			if (prev != null) {
				float s = Math.Min((aa.Width - 6f) / Math.Max(1, prev.Width), (aa.Height - 6f) / Math.Max(1, prev.Height));
				spriteBatch.Draw(prev, aa.Center.ToVector2(), null, Color.White * _fade, 0f, prev.Size() * 0.5f, s, SpriteEffects.None, 0f);
			}

			string tag = offer.BuiltIn ? WeText.UI("FontBuiltIn") : WeText.UI("RightClickDelete");
			TwoLine(spriteBatch, aa.Right + 12, hit.Y + 3, hit.Width - aa.Width - 60, offer.Family, tag);
			Affordance(spriteBatch, hit, CardKind.Radio, on);
			y += 50;
		}

		private static bool ClickFont(Rectangle panel, ref int y, out bool trash)
		{
			Rectangle hit = Row(panel, y, 44);
			y += 50;
			trash = false;
			if (!hit.Contains(Main.mouseX, Main.mouseY))
				return false;
			if (Main.mouseRight) {
				trash = true;
				return true;
			}

			return true;
		}

		private static WeFontOffer[] SnapshotFonts()
		{
			var copy = new WeFontOffer[WeType.All.Count];
			for (int i = 0; i < copy.Length; i++)
				copy[i] = WeType.All[i];
			return copy;
		}

		private static void ClientAccent(SpriteBatch spriteBatch, Rectangle panel, ref int y, bool click)
		{
			const int h = 118;
			int top = y;
			DrawGroupShell(spriteBatch, panel, top, h, WeText.UI("Accent"));
			int inner = top + 32;
			const int cols = 4;
			const int gap = 6;
			int cellW = (panel.Width - 40 - gap * (cols - 1)) / cols;
			const int cellH = 32;
			if (click) {
				for (int i = 0; i < WeAccent.Palettes.Length; i++) {
					int col = i % cols;
					int row = i / cols;
					var hit = new Rectangle(panel.X + 20 + col * (cellW + gap), inner + row * (cellH + gap), cellW, cellH);
					if (hit.Contains(Main.mouseX, Main.mouseY))
						WeAccent.Set(i);
				}

				y = top + h;
				return;
			}

			for (int i = 0; i < WeAccent.Palettes.Length; i++) {
				int col = i % cols;
				int row = i / cols;
				var hit = new Rectangle(panel.X + 20 + col * (cellW + gap), inner + row * (cellH + gap), cellW, cellH);
				AccentSwatch swatch = WeAccent.Palettes[i];
				bool on = i == WeAccent.Index;
				bool hoverSwatch = hit.Contains(Main.mouseX, Main.mouseY);
				WeTerra.Panel(spriteBatch, hit, swatch.Mid * _fade, (on ? Color.White : hoverSwatch ? swatch.Light : Color.Black) * _fade);
				WeLook.DrawPreview(
					spriteBatch, "Aa", hit.Center.ToVector2(), Color.White, _fade * 0.9f, 0.22f, 0);
			}

			y = top + h;
		}

		private static void ClientFactory(SpriteBatch spriteBatch, Rectangle panel, ref int y, bool click)
		{
			int h = FactoryGroupH();
			int top = y;
			DrawGroupShell(spriteBatch, panel, top, h, WeText.UI("ResetFactory"));
			y = top + 32;
			if (click) {
				if (ClickCard(panel, ref y)) {
					if (!_factoryArmed)
						_factoryArmed = true;
					else {
						_factoryArmed = false;
						WeSettings.ResetToPackDefaults();
						WeToast.Show("ToastFactoryReset");
					}
				}
				else
					_factoryArmed = false;

				SkipHint(ref y);
				y = top + h + 16;
				return;
			}

			DrawCard(spriteBatch, panel, ref y, WeText.UI(_factoryArmed ? "ResetFactorySure" : "ResetFactory"), _factoryArmed, CardKind.Action);
			DrawHint(spriteBatch, panel, ref y, WeText.UI("ResetFactoryHint"));
			y = top + h + 16;
		}
	}
}
