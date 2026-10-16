using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.Widgets;
using DieWithASmile.Engine.Audio;
using DieWithASmile.Engine.Grab;

namespace DieWithASmile.Engine.UI
{
	internal enum WePanelId
	{
		None,
		Wallpaper,
		Music,
		Widgets,
		Logo,
		Client
	}

	internal static partial class WePanels
	{
		private static bool _showPackedHidden;
		private static int _contentH;
		private static Rectangle _tipAnchor;
		private static string _tip;

		/// <summary>Queues a tooltip; drawn after the clipped content so it is never cut off.</summary>
		private static void Tip(Rectangle anchor, string text)
		{
			_tipAnchor = anchor;
			_tip = text;
		}
		private static WePanelId _id;
		private static float _fade;
		private static float _scroll;
		private static bool _frameInput;
		private static bool _mouseHeld;
		private static bool _holdLock;
		private static bool _rightHeld;
		private static bool _rightLock;
		private static int _lastWheel;
		private static string _dragSlider;
		private static bool _ateInput;
		private static int _clientChip;
		private const int SlotCount = 5;
		private const int SlotPlayer = 0;
		private const int SlotClock = 1;
		private const int SlotQuote = 2;
		private const int SlotMoon = 3;
		private const int SlotDiscord = 4;
		private static readonly float[] TileOpen = new float[SlotCount];
		private static int _tileHover = -1;
		private const int WidgetTileH = 72;
		private const int WidgetTileGap = 8;
		private const int DiscordStyleH = 80;
		private const int DiscordStyleStep = 86;

		internal static bool IsOpen => _id != WePanelId.None;
		internal static bool Covering => IsOpen || _fade > 0.02f;
		internal static bool Is(WePanelId id) => _id == id;
		internal static bool AteInput => _ateInput;

		internal static void Open(WePanelId id)
		{
			LayoutEditor.Cancel(false);
			_id = id;
			_scroll = 0f;
			WeArt.Scan();
			WeLibrary.ScanIntoSave();
			WeCatalog.Refresh();
			WePresets.Refresh();
			WeType.Scan();
			SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		internal static void Close()
		{
			if (_id == WePanelId.None)
				return;
			_id = WePanelId.None;
			_dragSlider = null;
			WeSave.Holding = false;
			_factoryArmed = false;
			DiscordWidget.Unfocus();
			SoundEngine.PlaySound(SoundID.MenuClose);
		}

		internal static void Update()
		{
			// A drag always ends with the mouse button; never leave saves held back.
			if (!WeInput.LeftDown) {
				_dragSlider = null;
				WeSave.Holding = false;
			}

			if (!WeModMenu.OnTitle) {
				if (IsOpen)
					DiscordWidget.Unfocus();
				_id = WePanelId.None;
			}
			_fade = MathHelper.Lerp(_fade, IsOpen ? 1f : 0f, 0.22f);
			if (!IsOpen && _fade < 0.02f)
				_fade = 0f;
			TickWidgetTiles();
		}

		internal static void HandleInput()
		{
			if (_frameInput)
				return;
			_frameInput = true;

			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			bool rightPressed = WeInput.Edge(WeInput.RightDown, ref _rightHeld, ref _rightLock);
			if (!IsOpen) {
				if (Covering)
					Main.blockMouse = true;
				return;
			}

			_ateInput = true;
			Main.blockMouse = true;
			Rectangle panel = PanelRect();

			int wheel = Mouse.GetState().ScrollWheelValue;
			if (panel.Contains(Main.mouseX, Main.mouseY))
				_scroll = MathHelper.Clamp(_scroll - (wheel - _lastWheel) / 120f * 42f, 0f, MaxScroll());
			_lastWheel = wheel;

			if (_dragSlider != null) {
				if (WeInput.LeftDown) {
					WeSave.Holding = true;
					ApplySlider(_dragSlider);
				}
				else {
					_dragSlider = null;
					WeSave.Holding = false;
				}
				return;
			}

			if (!pressed && !rightPressed)
				return;

			if (!panel.Contains(Main.mouseX, Main.mouseY)) {
				// With the sidebar hub, clicks on it switch sections; let it handle them instead of closing.
				if (WrenchHub.UseSidebar && WrenchSidebar.HitRect().Contains(Main.mouseX, Main.mouseY))
					return;
				if (pressed)
					Close();
				if (pressed)
					WeInput.LockHold(ref _holdLock);
				if (rightPressed)
					WeInput.LockHold(ref _rightLock);
				return;
			}

			if (pressed && HandleChrome(panel)) {
				WeInput.LockHold(ref _holdLock);
				return;
			}

			if (View(panel).Contains(Main.mouseX, Main.mouseY))
				HandleClicks(panel);
			if (pressed)
				WeInput.LockHold(ref _holdLock);
			if (rightPressed)
				WeInput.LockHold(ref _rightLock);
		}

		internal static void EndFrame()
		{
			_frameInput = false;
			_ateInput = false;
		}

		internal static void Draw(SpriteBatch spriteBatch)
		{
			if (_fade <= 0.02f)
				return;

			WeDraw.WithLinear(spriteBatch, () => WeTerra.WithTheme(() => {
				// Wallpaper/logo need to be seen while you change them; the rest gets a deeper dim.
				float dim = _id is WePanelId.Wallpaper or WePanelId.Logo ? 0.12f : 0.32f;
				WeDraw.Fill(spriteBatch, WeDraw.CoverRect, Color.Black * (dim * _fade));
				Rectangle panel = PanelRect();
				WeTerra.Window(spriteBatch, panel, _fade);
				DrawHeader(spriteBatch, panel);
				DrawTabs(spriteBatch, panel);
				Rectangle view = View(panel);
				WeTerra.WellBox(spriteBatch, new Rectangle(view.X - 2, view.Y - 4, view.Width + 4, view.Height + 8), _fade * 0.55f);
				WeDraw.WithClip(spriteBatch, view, () => DrawBody(spriteBatch, panel));
				WeTerra.Scrollbar(spriteBatch, view, _scroll, MaxScroll(), _fade);
				DrawTabTooltip(spriteBatch, panel);
				if (!string.IsNullOrEmpty(_tip) && view.Contains(Main.mouseX, Main.mouseY))
					WeTerra.Tooltip(spriteBatch, _tipAnchor, _tip, _fade, preferLeft: true);
				_tip = null;
			}));
		}

		// ---- window chrome: title, one-line explanation, close button and the section tabs ----------

		private readonly struct Tab
		{
			internal Tab(WePanelId id, string icon, string key)
			{
				Id = id;
				Icon = icon;
				Key = key;
			}

			internal readonly WePanelId Id;
			internal readonly string Icon;
			internal readonly string Key;
		}

		// WePanelId.None marks the Layout tab, which starts the layout editor instead of showing a page.
		private static readonly Tab[] Tabs =
		{
			new(WePanelId.Wallpaper, WeIcons.Wallpaper, "TabWallpaper"),
			new(WePanelId.Logo, WeIcons.Logo, "TabLogo"),
			new(WePanelId.Music, WeIcons.Music, "TabMusic"),
			new(WePanelId.Widgets, WeIcons.Widget, "TabWidgets"),
			new(WePanelId.None, WeIcons.Layout, "TabLayout"),
			new(WePanelId.Client, WeIcons.Client, "TabSettings")
		};

		private const int HeaderH = 58;
		private const int TabsH = 54;

		private static Rectangle CloseRect(Rectangle panel) => new(panel.Right - 40, panel.Y + 12, 28, 28);

		private static Rectangle TabRect(Rectangle panel, int index)
		{
			int gap = 4;
			int w = (panel.Width - 24 - gap * (Tabs.Length - 1)) / Tabs.Length;
			return new Rectangle(panel.X + 12 + index * (w + gap), panel.Y + HeaderH, w, TabsH - 8);
		}

		private static string TabTitle(WePanelId id) => id switch {
			WePanelId.Wallpaper => "TabWallpaper",
			WePanelId.Music => "TabMusic",
			WePanelId.Widgets => "TabWidgets",
			WePanelId.Logo => "TabLogo",
			WePanelId.Client => "TabSettings",
			_ => ""
		};

		private static string TabSub(WePanelId id) => id switch {
			WePanelId.Wallpaper => "SubWallpaper",
			WePanelId.Music => "SubMusic",
			WePanelId.Widgets => "SubWidgets",
			WePanelId.Logo => "SubLogo",
			WePanelId.Client => "SubSettings",
			_ => ""
		};

		private static void DrawHeader(SpriteBatch spriteBatch, Rectangle panel)
		{
			string key = TabTitle(_id);
			if (string.IsNullOrEmpty(key))
				return;
			WeTerra.Icon(spriteBatch, IconOf(_id), new Vector2(panel.X + 30, panel.Y + 24), 32f, Color.White * _fade);
			WeTerra.Headline(spriteBatch, WeText.UI(key), new Vector2(panel.X + 52, panel.Y + 8), WeTerra.Title * _fade, 0.52f);
			string sub = WeTerra.Fit(WeText.UI(TabSub(_id)), 0.68f, panel.Width - 110);
			WeTerra.Label(spriteBatch, sub, new Vector2(panel.X + 54, panel.Y + 36), WeTerra.Muted * _fade, 0.68f);

			Rectangle close = CloseRect(panel);
			bool hover = close.Contains(Main.mouseX, Main.mouseY);
			WeTerra.Slot(spriteBatch, close, WeIcons.Close, hover, false, _fade);
		}

		private static string IconOf(WePanelId id)
		{
			foreach (Tab tab in Tabs) {
				if (tab.Id == id)
					return tab.Icon;
			}

			return WeIcons.Setting;
		}

		private static void DrawTabs(SpriteBatch spriteBatch, Rectangle panel)
		{
			for (int i = 0; i < Tabs.Length; i++) {
				Rectangle r = TabRect(panel, i);
				bool on = Tabs[i].Id != WePanelId.None && Tabs[i].Id == _id;
				bool hover = r.Contains(Main.mouseX, Main.mouseY);
				WeTerra.Panel(spriteBatch, r, WeTerra.LookOf(hover, on), _fade);
				WeTerra.Icon(spriteBatch, Tabs[i].Icon, new Vector2(r.Center.X, r.Y + 15), 16f, Color.White * _fade);
				string label = WeTerra.Fit(WeText.UI(Tabs[i].Key), 0.6f, r.Width - 6);
				WeTerra.LabelCentered(spriteBatch, label, new Vector2(r.Center.X, r.Bottom - 12), (on || hover ? Color.White : WeTerra.Muted) * _fade, 0.6f);
			}
		}

		private static void DrawTabTooltip(SpriteBatch spriteBatch, Rectangle panel)
		{
			if (CloseRect(panel).Contains(Main.mouseX, Main.mouseY))
				WeTerra.Tooltip(spriteBatch, CloseRect(panel), WeText.UI("Close"), _fade, preferLeft: true);
			Rectangle layout = TabRect(panel, 4);
			if (layout.Contains(Main.mouseX, Main.mouseY))
				WeTerra.Tooltip(spriteBatch, layout, WeText.UI("BtnLayout"), _fade, preferLeft: true);
		}

		/// <summary>Close button and section tabs; returns true when the click was used.</summary>
		private static bool HandleChrome(Rectangle panel)
		{
			if (CloseRect(panel).Contains(Main.mouseX, Main.mouseY)) {
				Close();
				return true;
			}

			for (int i = 0; i < Tabs.Length; i++) {
				if (!TabRect(panel, i).Contains(Main.mouseX, Main.mouseY))
					continue;
				if (Tabs[i].Id == WePanelId.None) {
					Close();
					LayoutEditor.Begin();
					WrenchToolbar.Collapse();
				}
				else if (Tabs[i].Id != _id)
					Open(Tabs[i].Id);
				return true;
			}

			return false;
		}

		private static void DrawBody(SpriteBatch spriteBatch, Rectangle panel)
		{
			int top = View(panel).Y - (int)_scroll;
			int y = top;
			DrawBodyInner(spriteBatch, panel, ref y);
			// Real content height, so the scroll range ends where the content does.
			_contentH = Math.Max(0, y - top) + 12;
		}

		private static void DrawBodyInner(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			switch (_id) {
				case WePanelId.Wallpaper:
					DrawWallpaper(spriteBatch, panel, ref y);
					break;
				case WePanelId.Logo:
					DrawLogo(spriteBatch, panel, ref y);
					break;
				case WePanelId.Music:
					DrawMusic(spriteBatch, panel, ref y);
					break;
				case WePanelId.Widgets:
					DrawWidgets(spriteBatch, panel, ref y);
					break;
				case WePanelId.Client:
					DrawClient(spriteBatch, panel, ref y);
					break;
			}
		}

		private static void HandleClicks(Rectangle panel)
		{
			int y = View(panel).Y - (int)_scroll;
			switch (_id) {
				case WePanelId.Wallpaper:
					ClickWallpaper(panel, ref y);
					break;
				case WePanelId.Logo:
					ClickLogo(panel, ref y);
					break;
				case WePanelId.Music:
					ClickMusic(panel, ref y);
					break;
				case WePanelId.Widgets:
					ClickWidgets(panel, ref y);
					break;
				case WePanelId.Client:
					ClickClient(panel, ref y);
					break;
			}
		}


		private static void DrawLogo(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionLogoBasic"));
			DrawCard(spriteBatch, panel, ref y, WeText.UI("BtnLogoPos"), false, CardKind.Action);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("VanillaLogo"), WeSave.Data.Logo == LogoKind.Vanilla, CardKind.Radio);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("HideLogo"), WeSave.Data.Logo == LogoKind.Hidden, CardKind.Radio);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("LogoPulse"), WeLook.LogoPulse);

			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionPack"));
			foreach (string id in WePresetLogos.Ids)
				DrawPresetCard(spriteBatch, panel, ref y, id);

			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionFromMods"));
			DrawHint(spriteBatch, panel, ref y, WeText.UI("BorrowMix"));
			DrawBorrowSection(spriteBatch, panel, ref y, WeCatalog.Logos, WeOfferKind.Logo);

			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionImages"));
			DrawButtonRow(spriteBatch, panel, ref y, WeText.UI("ImportImage"), WeText.UI("OpenFolder"), WeIcons.Upload, WeIcons.Folder);
			DrawHint(spriteBatch, panel, ref y, WeText.UI("AnimHint"));
			foreach (WeArtRecord record in WeSave.Data.Logos)
				DrawArtCard(spriteBatch, panel, ref y, record, WeSave.Data.Logo == LogoKind.Custom && WeSave.Data.LogoId == record.Id, logo: true);
		}

		private static void ClickLogo(Rectangle panel, ref int y)
		{
			// Mirrors DrawLogo row for row.
			SkipSection(ref y);
			if (ClickCard(panel, ref y)) {
				Close();
				LayoutEditor.Begin(SceneGraph.Logo, logoOnly: true);
				WrenchToolbar.Collapse();
				return;
			}

			if (ClickCard(panel, ref y)) {
				WeSettings.SetLogo(LogoKind.Vanilla);
				return;
			}

			if (ClickCard(panel, ref y)) {
				WeSettings.SetLogo(LogoKind.Hidden);
				return;
			}

			if (ClickCard(panel, ref y)) {
				WeSettings.ToggleLogoPulse();
				return;
			}

			SkipSection(ref y);
			foreach (string id in WePresetLogos.Ids) {
				if (!ClickBorrow(panel, ref y))
					continue;
				WeSettings.SetLogo(LogoKind.Preset, id);
				WeToast.Show("ToastLogo");
				return;
			}

			SkipSection(ref y);
			SkipHint(ref y);
			if (ClickBorrowSection(panel, ref y, WeCatalog.Logos, WeOfferKind.Logo))
				return;

			SkipSection(ref y);
			if (ClickRow(panel, ref y, out int which)) {
				if (which == 0)
					WeArt.TryImportLogo();
				else
					WeFiles.OpenFolder(WeSave.LogoFolder);
				return;
			}

			SkipHint(ref y);
			foreach (WeArtRecord record in WeSave.Data.Logos.ToArray()) {
				if (ClickArt(panel, ref y, record, true)) {
					WeSettings.SetLogo(LogoKind.Custom, record.Id);
					return;
				}
			}
		}

		private static void DrawMusic(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionSource"));
			DrawCard(spriteBatch, panel, ref y, WeText.UI("ThemeMusic"), WeSave.Data.Music == MusicKind.Custom, CardKind.Radio);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("VanillaMusic"), WeSave.Data.Music == MusicKind.Vanilla, CardKind.Radio);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("Silence"), WeSave.Data.Music == MusicKind.Silence, CardKind.Radio);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("MuteUnfocused"), WeSave.Data.MuteWhenUnfocused);

			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionPlaylist"));
			DrawButtonRow(spriteBatch, panel, ref y, WeText.UI("ImportSong"), WeText.UI("OpenFolder"), WeIcons.Upload, WeIcons.Folder);
			DrawHint(spriteBatch, panel, ref y, WeText.UI(WeSave.Data.Tracks.Count == 0 && WePackedMusic.Tracks.Count == 0 ? "NoTracks" : "TrackHint"));
			foreach (MenuTrack packed in WePackedMusic.Tracks) {
				if (!PackedVisible(packed))
					continue;
				DrawPackedTrack(spriteBatch, panel, ref y, packed);
			}
			int hiddenPacked = HiddenPackedCount();
			if (hiddenPacked > 0)
				DrawCard(spriteBatch, panel, ref y, string.Format(WeText.UI("ShowHiddenPacked"), hiddenPacked), _showPackedHidden);
			foreach (WeTrackRecord track in WeSave.Data.Tracks)
				DrawTrack(spriteBatch, panel, ref y, track);
		}

		private static void ClickMusic(Rectangle panel, ref int y)
		{
			SkipSection(ref y);
			if (ClickCard(panel, ref y)) {
				WeSettings.SetMusic(MusicKind.Custom);
				WePlaylist.OnThemeSelected();
				WeToast.Show("ToastMusic");
				return;
			}

			if (ClickCard(panel, ref y)) {
				WeSettings.SetMusic(MusicKind.Vanilla);
				WePlaylist.Silence();
				WeToast.Show("ToastMusic");
				return;
			}

			if (ClickCard(panel, ref y)) {
				WeSettings.SetMusic(MusicKind.Silence);
				WePlaylist.Silence();
				WeToast.Show("ToastMusic");
				return;
			}

			if (ClickCard(panel, ref y)) {
				WeSettings.ToggleMuteUnfocused();
				return;
			}

			SkipSection(ref y);
			if (ClickRow(panel, ref y, out int which)) {
				if (which == 0) {
					if (WeFiles.TryPickAudio(out string path))
						WeLibrary.Import(path);
				}
				else
					WeFiles.OpenFolder(WeSave.MusicFolder);
				return;
			}

			SkipHint(ref y);
			foreach (MenuTrack packed in WePackedMusic.Tracks) {
				if (!PackedVisible(packed))
					continue;
				if (ClickPackedTrack(panel, ref y, packed))
					return;
			}
			if (HiddenPackedCount() > 0 && ClickCard(panel, ref y)) {
				_showPackedHidden = !_showPackedHidden;
				return;
			}
			foreach (WeTrackRecord track in WeSave.Data.Tracks.ToArray()) {
				if (ClickTrack(panel, ref y, track))
					return;
			}
		}

		private static void DrawWidgets(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionWidgets"));
			DrawWidgetBand(spriteBatch, panel, ref y, SlotPlayer, WeText.UI("AddPlayer"), WeSave.Data.PlayerWidget, WePlayerUI.DrawPreview);
			DrawWidgetBand(spriteBatch, panel, ref y, SlotClock, WeText.UI("AddClock"), WeSave.Data.ClockWidget, ClockWidget.DrawPreview);
			DrawWidgetBand(spriteBatch, panel, ref y, SlotQuote, WeText.UI("AddQuote"), WeSave.Data.QuoteWidget, QuoteWidget.DrawPreview);
			DrawWidgetBand(spriteBatch, panel, ref y, SlotMoon, WeText.UI("AddMoon"), WeSave.Data.MoonWidget, MoonWidget.DrawPreview);
			DrawWidgetBand(spriteBatch, panel, ref y, SlotDiscord, WeText.UI("AddDiscord"), WeSave.Data.DiscordWidget,
				(sb, box, fade) => DiscordWidget.DrawPreview(sb, box, fade, WeSave.Data.DiscordStyle));

			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionScreen"));
			DrawCard(spriteBatch, panel, ref y, WeText.UI("BtnClean"), WeSave.Data.CleanChrome);
			DrawCard(spriteBatch, panel, ref y, WeText.UI("BtnLayout"), false, CardKind.Action);
			DrawHint(spriteBatch, panel, ref y, WeText.UI("HiddenLayers"));
			foreach (WeElementRecord hidden in SceneGraph.Hidden())
				DrawCard(spriteBatch, panel, ref y, WeText.Layer(hidden.Id) + "  ·  " + WeText.UI("Restore"), false, CardKind.Action);
		}

		private static void ClickWidgets(Rectangle panel, ref int y)
		{
			SkipSection(ref y);
			if (ClickWidgetBand(panel, ref y, SlotPlayer)) {
				WeSettings.SetPlayerWidget(!WeSave.Data.PlayerWidget);
				WeToast.Show(WeSave.Data.PlayerWidget ? "ToastWidgetOn" : "ToastWidgetOff");
			}

			if (ClickWidgetBand(panel, ref y, SlotClock)) {
				WeSettings.SetClockWidget(!WeSave.Data.ClockWidget);
				WeToast.Show(WeSave.Data.ClockWidget ? "ToastWidgetOn" : "ToastWidgetOff");
			}

			if (ClickWidgetBand(panel, ref y, SlotQuote)) {
				WeSettings.SetQuoteWidget(!WeSave.Data.QuoteWidget);
				if (WeSave.Data.QuoteWidget)
					QuoteWidget.EnsureFile();
				WeToast.Show(WeSave.Data.QuoteWidget ? "ToastWidgetOn" : "ToastWidgetOff");
			}

			if (ClickWidgetBand(panel, ref y, SlotMoon)) {
				WeSettings.SetMoonWidget(!WeSave.Data.MoonWidget);
				WeToast.Show(WeSave.Data.MoonWidget ? "ToastWidgetOn" : "ToastWidgetOff");
			}

			bool discordWasOn = WeSave.Data.DiscordWidget;
			if (ClickWidgetBand(panel, ref y, SlotDiscord, discordWasOn)) {
				if (discordWasOn) {
					WeSettings.SetDiscordWidget(false);
					DiscordWidget.Unfocus();
					TileOpen[SlotDiscord] = 0f;
					WeToast.Show("ToastWidgetOff");
				}
				else {
					WeSettings.SetDiscordWidget(true);
					DiscordWidget.OpenIdEditor();
					DiscordFeed.RefreshNow();
					TileOpen[SlotDiscord] = 1f;
					_tileHover = SlotDiscord;
					WeToast.Show("ToastDiscordId", 3.6f);
				}
			}

			SkipSection(ref y);
			if (ClickCard(panel, ref y)) {
				WeSettings.ToggleCleanChrome();
				WeToast.Show(WeSave.Data.CleanChrome ? "ToastCleanOn" : "ToastCleanOff");
				return;
			}

			if (ClickCard(panel, ref y)) {
				Close();
				LayoutEditor.Begin();
				WrenchToolbar.Collapse();
				return;
			}

			SkipHint(ref y);
			foreach (WeElementRecord hidden in SceneGraph.Hidden()) {
				if (ClickCard(panel, ref y)) {
					LayoutEditor.RestoreHidden(hidden.Id);
					return;
				}
			}
		}

		private static void DrawClient(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			ClientLooks(spriteBatch, panel, ref y, false);
			ClientHub(spriteBatch, panel, ref y, false);
			ClientWindow(spriteBatch, panel, ref y, false);
			ClientMenu(spriteBatch, panel, ref y, false);
			ClientAccent(spriteBatch, panel, ref y, false);
			ClientFactory(spriteBatch, panel, ref y, false);
		}

		private static void ClickClient(Rectangle panel, ref int y)
		{
			ClientLooks(null, panel, ref y, true);
			ClientHub(null, panel, ref y, true);
			ClientWindow(null, panel, ref y, true);
			ClientMenu(null, panel, ref y, true);
			ClientAccent(null, panel, ref y, true);
			ClientFactory(null, panel, ref y, true);
		}
		private static void DrawWidgetBand(SpriteBatch spriteBatch, Rectangle panel, ref int y, int slot, string title, bool on, Action<SpriteBatch, Rectangle, float> preview)
		{
			DrawWidgetTile(spriteBatch, panel, ref y, title, on, preview, slot);
			int extra = SettingsExtra(slot);
			if (on && extra > 0) {
				var well = Row(panel, y, extra);
				WeTerra.WellBox(spriteBatch, well, _fade);
				WeDraw.WithClip(spriteBatch, well, () => {
					int inner = well.Y;
					DrawSlotSettings(spriteBatch, panel, ref inner, slot);
				});
				y += extra;
			}

			y += WidgetTileGap;
		}

		private static bool ClickWidgetBand(Rectangle panel, ref int y, int slot, bool discordWasOn = false)
		{
			bool tile = ClickWidgetTile(panel, ref y);
			int extra = SettingsExtra(slot);
			if (TileOn(slot) && SettingsNatural(slot) > 0) {
				int start = y;
				if (!tile && TileOpen[slot] > 0.82f) {
					int inner = start;
					ClickSlotSettings(panel, ref inner, slot, discordWasOn);
				}

				y = start + extra;
			}

			y += WidgetTileGap;
			return tile;
		}

		private static void DrawWidgetTile(SpriteBatch spriteBatch, Rectangle panel, ref int y, string title, bool on, Action<SpriteBatch, Rectangle, float> preview, int slot)
		{
			Rectangle hit = Row(panel, y, WidgetTileH);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY) || _tileHover == slot;
			Card(spriteBatch, hit, hover, on);
			var previewBox = new Rectangle(hit.X + 10, hit.Y + 8, 148, hit.Height - 16);
			Thumb(spriteBatch, previewBox, null);
			preview?.Invoke(spriteBatch, previewBox, _fade * (on ? 1f : 0.42f));
			int textX = previewBox.Right + 14;
			int room = hit.Right - textX - 44;
			WeTerra.Label(spriteBatch, WeTerra.Fit(title, 0.84f, room), new Vector2(textX, hit.Y + 16), (on ? Color.White : WeTerra.Muted) * _fade, 0.84f);
			string hint = on && SettingsNatural(slot) > 0 ? WeText.UI("WidgetHoverSettings") : WeText.UI(on ? "WidgetOn" : "WidgetOff");
			WeTerra.Label(spriteBatch, WeTerra.Fit(hint, 0.66f, room), new Vector2(textX, hit.Y + 40), WeTerra.Muted * _fade, 0.66f);
			Affordance(spriteBatch, hit, CardKind.Toggle, on);
			y += WidgetTileH;
		}

		private static bool ClickWidgetTile(Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, WidgetTileH);
			y += WidgetTileH;
			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		private static Color MutedHint() => new Color(168, 172, 180);

		private static void DrawSlotSettings(SpriteBatch spriteBatch, Rectangle panel, ref int y, int slot)
		{
			switch (slot) {
				case SlotClock:
					DrawCard(spriteBatch, panel, ref y, WeText.UI(WeSave.Data.Clock24h ? "Clock24h" : "Clock12h"), WeSave.Data.Clock24h);
					DrawCard(spriteBatch, panel, ref y, WeText.UI(WeSave.Data.ClockAnalog ? "ClockAnalog" : "ClockDigital"), WeSave.Data.ClockAnalog);
					DrawCard(spriteBatch, panel, ref y, WeText.UI("ClockDate"), WeSave.Data.ClockDate);
					break;
				case SlotQuote:
					DrawButtonRow(spriteBatch, panel, ref y, WeText.UI("OpenQuotes"), WeText.UI("OpenFolder"), WeIcons.Quote, WeIcons.Folder);
					break;
				case SlotDiscord:
					DrawDiscordIdField(spriteBatch, panel, ref y);
					DrawHint(spriteBatch, panel, ref y, DiscordWidget.StatusLine());
					DrawHint(spriteBatch, panel, ref y, WeText.UI("DiscordHint1"));
					DrawHint(spriteBatch, panel, ref y, WeText.UI("DiscordHint2"));
					DrawDiscordStyle(spriteBatch, panel, ref y, 0);
					DrawDiscordStyle(spriteBatch, panel, ref y, 1);
					DrawDiscordStyle(spriteBatch, panel, ref y, 2);
					break;
			}
		}

		private static void ClickSlotSettings(Rectangle panel, ref int y, int slot, bool discordWasOn)
		{
			switch (slot) {
				case SlotClock:
					if (ClickCard(panel, ref y)) {
						WeSave.Data.Clock24h = !WeSave.Data.Clock24h;
						WeSave.Save();
					}

					if (ClickCard(panel, ref y)) {
						WeSave.Data.ClockAnalog = !WeSave.Data.ClockAnalog;
						WeSave.Save();
					}

					if (ClickCard(panel, ref y)) {
						WeSave.Data.ClockDate = !WeSave.Data.ClockDate;
						WeSave.Save();
					}

					break;
				case SlotQuote:
					if (ClickRow(panel, ref y, out int quoteWhich)) {
						if (quoteWhich == 0) {
							QuoteWidget.EnsureFile();
							WeFiles.OpenFile(WeSave.QuotePath);
						}
						else
							WeFiles.OpenFolder(WeSave.RootFolder);
					}

					break;
				case SlotDiscord:
					if (ClickDiscordIdField(panel, ref y))
						DiscordWidget.OpenIdEditor();
					else if (discordWasOn && DiscordWidget.Editing)
						DiscordWidget.Unfocus();
					SkipHint(ref y);
					SkipHint(ref y);
					SkipHint(ref y);
					if (ClickDiscordStyle(panel, ref y))
						WeSettings.SetDiscordStyle(0);
					if (ClickDiscordStyle(panel, ref y))
						WeSettings.SetDiscordStyle(1);
					if (ClickDiscordStyle(panel, ref y))
						WeSettings.SetDiscordStyle(2);
					break;
			}
		}

		private static void TickWidgetTiles()
		{
			if (!IsOpen || _id != WePanelId.Widgets) {
				for (int i = 0; i < SlotCount; i++)
					TileOpen[i] = MathHelper.Lerp(TileOpen[i], 0f, 0.28f);
				_tileHover = -1;
				return;
			}

			Rectangle panel = PanelRect();
			Rectangle view = View(panel);
			int y = view.Y - (int)_scroll + SectionH;
			int hover = -1;
			Point mouse = new(Main.mouseX, Main.mouseY);
			if (view.Contains(mouse)) {
				for (int slot = 0; slot < SlotCount; slot++) {
					int height = BandHeight(slot);
					var band = new Rectangle(panel.X + 16, y, panel.Width - 32, Math.Max(1, height - WidgetTileGap));
					if (band.Contains(mouse))
						hover = slot;
					y += height;
				}
			}

			_tileHover = hover;
			for (int slot = 0; slot < SlotCount; slot++) {
				float prev = TileOpen[slot];
				float target = TileOn(slot) && SettingsNatural(slot) > 0f && hover == slot ? 1f : 0f;
				TileOpen[slot] = MathHelper.Lerp(prev, target, 0.2f);
				if (Math.Abs(TileOpen[slot] - target) < 0.012f)
					TileOpen[slot] = target;
				if (TileOpen[slot] > prev + 0.004f)
					NudgeScrollToSlot(slot);
			}

			_scroll = MathHelper.Clamp(_scroll, 0f, MaxScroll());
		}

		private static void NudgeScrollToSlot(int slot)
		{
			Rectangle view = View(PanelRect());
			int y = view.Y - (int)_scroll + SectionH;
			for (int i = 0; i < slot; i++)
				y += BandHeight(i);
			int bottom = y + BandHeight(slot) - WidgetTileGap;
			if (bottom > view.Bottom - 8)
				_scroll += bottom - (view.Bottom - 8);
			if (y < view.Y + 4)
				_scroll -= view.Y + 4 - y;
			_scroll = MathHelper.Clamp(_scroll, 0f, MaxScroll());
		}

		private static int BandHeight(int slot) =>
			WidgetTileH + WidgetTileGap + (TileOn(slot) ? SettingsExtra(slot) : 0);

		private static int SettingsExtra(int slot)
		{
			if (!TileOn(slot))
				return 0;
			return (int)MathF.Round(SettingsNatural(slot) * TileOpen[slot]);
		}

		private static float SettingsNatural(int slot) => slot switch {
			SlotClock => 126f,
			SlotQuote => 40f,
			SlotDiscord => 52f + HintH * 3 + DiscordStyleStep * 3,
			_ => 0f
		};

		private static bool TileOn(int slot) => slot switch {
			SlotPlayer => WeSave.Data.PlayerWidget,
			SlotClock => WeSave.Data.ClockWidget,
			SlotQuote => WeSave.Data.QuoteWidget,
			SlotMoon => WeSave.Data.MoonWidget,
			SlotDiscord => WeSave.Data.DiscordWidget,
			_ => false
		};

		private static void DrawDiscordStyle(SpriteBatch spriteBatch, Rectangle panel, ref int y, int style)
		{
			Rectangle hit = Row(panel, y, DiscordStyleH);
			bool on = WeSave.Data.DiscordStyle == style;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			var preview = new Rectangle(hit.X + 8, hit.Y + 6, hit.Width - 16, 46);
			Thumb(spriteBatch, preview, null);
			DiscordWidget.DrawPreview(spriteBatch, preview, _fade * (on ? 1f : 0.45f), style);
			string key = style == 1 ? "DiscordStyleBanner" : style == 2 ? "DiscordStyleRoster" : "DiscordStyleCompact";
			WeTerra.Label(spriteBatch, WeText.UI(key), new Vector2(hit.X + 12, hit.Bottom - 22), (on ? Color.White : WeTerra.Muted) * _fade, 0.76f);
			if (on)
				WeTerra.Icon(spriteBatch, WeIcons.Check, new Vector2(hit.Right - 20, hit.Bottom - 15), 16f, Color.White * _fade);
			y += DiscordStyleStep;
		}

		private static bool ClickDiscordStyle(Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, DiscordStyleH);
			y += DiscordStyleStep;
			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		private static void DrawDiscordIdField(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, DiscordWidget.IdFieldHeight);
			DiscordWidget.DrawIdField(spriteBatch, hit, _fade);
			y += 52;
		}

		private static bool ClickDiscordIdField(Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, DiscordWidget.IdFieldHeight);
			y += 52;
			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		private static void DrawCard(SpriteBatch spriteBatch, Rectangle panel, ref int y, string text, bool on, CardKind kind = CardKind.Toggle)
		{
			Rectangle hit = Row(panel, y, 36);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			// Toggles show their state with the checkbox; choices and armed actions light the whole row.
			Card(spriteBatch, hit, hover, kind != CardKind.Toggle && on);
			WeTerra.Label(spriteBatch, WeTerra.Fit(text, 0.82f, hit.Width - 56), new Vector2(hit.X + 12, hit.Y + 9), Color.White * _fade, 0.82f);
			Affordance(spriteBatch, hit, kind, on);
			y += 42;
		}

		private static bool ClickCard(Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, 36);
			y += 42;
			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		private static bool ClickLook(Rectangle panel, ref int y, out bool trash)
		{
			Rectangle hit = Row(panel, y, 36);
			y += 42;
			trash = false;
			if (!hit.Contains(Main.mouseX, Main.mouseY))
				return false;
			if (Main.mouseRight) {
				trash = true;
				return true;
			}

			return true;
		}

		private static void DrawButtonRow(SpriteBatch spriteBatch, Rectangle panel, ref int y, string a, string b, string iconA = null, string iconB = null)
		{
			Rectangle left = new(panel.X + 16, y, (panel.Width - 40) / 2, 32);
			Rectangle right = new(left.Right + 8, y, left.Width, 32);
			DrawMini(spriteBatch, left, a, iconA);
			DrawMini(spriteBatch, right, b, iconB);
			y += 40;
		}

		private static bool ClickRow(Rectangle panel, ref int y, out int which)
		{
			Rectangle left = new(panel.X + 16, y, (panel.Width - 40) / 2, 32);
			Rectangle right = new(left.Right + 8, y, left.Width, 32);
			y += 40;
			which = left.Contains(Main.mouseX, Main.mouseY) ? 0 : right.Contains(Main.mouseX, Main.mouseY) ? 1 : -1;
			return which >= 0;
		}

		private static void DrawMini(SpriteBatch spriteBatch, Rectangle hit, string text, string icon = null)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, false);
			int left = hit.X;
			if (icon != null) {
				WeTerra.Icon(spriteBatch, icon, new Vector2(hit.X + 18, hit.Center.Y), 16f, Color.White * _fade);
				left += 28;
			}

			float room = hit.Right - left - 8;
			string label = WeTerra.Fit(text, 0.74f, room);
			Vector2 size = WeTerra.Measure(label, 0.74f);
			float x = left + (room - size.X) * 0.5f;
			WeTerra.Label(spriteBatch, label, new Vector2(x, hit.Y + (hit.Height - size.Y) * 0.5f + 2f), (hover ? Color.White : new Color(236, 236, 246)) * _fade, 0.74f);
		}

		private static void DrawHint(SpriteBatch spriteBatch, Rectangle panel, ref int y, string text)
		{
			// Fixed height (see SkipHint): one line, or two smaller lines when the text is long.
			float width = panel.Width - 40;
			if (WeTerra.Measure(text, 0.7f).X <= width) {
				WeTerra.Label(spriteBatch, text, new Vector2(panel.X + 18, y + 5), WeTerra.Muted * _fade, 0.7f);
			}
			else {
				SplitTwo(text, 0.6f, width, out string first, out string second);
				WeTerra.Label(spriteBatch, first, new Vector2(panel.X + 18, y), WeTerra.Muted * _fade, 0.6f);
				WeTerra.Label(spriteBatch, second, new Vector2(panel.X + 18, y + 15), WeTerra.Muted * _fade, 0.6f);
			}

			y += HintH;
		}

		private const int HintH = 30;

		private static void SplitTwo(string text, float scale, float width, out string first, out string second)
		{
			string[] words = (text ?? "").Split(' ');
			first = "";
			int i = 0;
			for (; i < words.Length; i++) {
				string next = first.Length == 0 ? words[i] : first + " " + words[i];
				if (first.Length > 0 && WeTerra.Measure(next, scale).X > width)
					break;
				first = next;
			}

			second = WeTerra.Fit(string.Join(" ", words, i, words.Length - i), scale, width);
			first = WeTerra.Fit(first, scale, width);
		}

		private static void SkipHint(ref int y) => y += HintH;

		private static void DrawArtCard(SpriteBatch spriteBatch, Rectangle panel, ref int y, WeArtRecord record, bool on, bool logo)
		{
			Rectangle hit = Row(panel, y, 52);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			Thumb(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 6, 64, 40), WeArt.Preview(record, logo));
			TwoLine(spriteBatch, hit.X + 82, hit.Y + 7, hit.Width - 130, record.FileName, WeText.UI("RightClickDelete"));
			Affordance(spriteBatch, hit, CardKind.Radio, on);
			y += 58;
		}

		private static bool ClickArt(Rectangle panel, ref int y, WeArtRecord record, bool logo)
		{
			Rectangle hit = Row(panel, y, 52);
			y += 58;
			if (Main.mouseRight && hit.Contains(Main.mouseX, Main.mouseY)) {
				WeArt.Delete(record, logo);
				return false;
			}

			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		private static void DrawBorrowSection(SpriteBatch spriteBatch, Rectangle panel, ref int y, IReadOnlyList<WeOffer> offers, WeOfferKind kind)
		{
			DrawHint(spriteBatch, panel, ref y, WeText.UI(
				offers.Count > 0 ? "FromModsHint" : WeCatalog.Ready ? "BorrowEmpty" : "BorrowPending"));
			foreach (WeOffer offer in offers)
				DrawBorrowCard(spriteBatch, panel, ref y, offer, IsBorrowedOn(offer, kind));
		}

		private static bool ClickBorrowSection(Rectangle panel, ref int y, IReadOnlyList<WeOffer> offers, WeOfferKind kind)
		{
			SkipHint(ref y);
			foreach (WeOffer offer in offers) {
				if (!ClickBorrow(panel, ref y))
					continue;
				if (kind == WeOfferKind.Logo) {
					WeSettings.SetLogo(LogoKind.Borrowed, offer.Id);
					WeToast.Show("ToastLogo");
				}
				else {
					WeSettings.SetWallpaperBorrowed(offer.Id);
					WeToast.Show("ToastWallpaper");
				}

				return true;
			}

			return false;
		}

		private static void DrawSection(SpriteBatch spriteBatch, Rectangle panel, ref int y, string text)
		{
			Heading(spriteBatch, panel, y, text);
			y += SectionH;
		}

		private const int SectionH = 26;

		private static void SkipSection(ref int y) => y += SectionH;

		private static bool IsBorrowedOn(WeOffer offer, WeOfferKind kind)
		{
			if (kind == WeOfferKind.Logo)
				return WeSave.Data.Logo == LogoKind.Borrowed && WeSave.Data.LogoId == offer.Id;
			return WeSave.Data.Wallpaper == WallpaperKind.Borrowed && WeSave.Data.WallpaperId == offer.Id;
		}

		private static void DrawPresetCard(SpriteBatch spriteBatch, Rectangle panel, ref int y, string id)
		{
			Rectangle hit = Row(panel, y, 56);
			bool on = WeSave.Data.Logo == LogoKind.Preset && WeSave.Data.LogoId == id;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			DrawBadge(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 14, 28, 28), WePresetLogos.PackIcon(), null);
			Thumb(spriteBatch, new Rectangle(hit.X + 44, hit.Y + 8, 72, 40), WePresetLogos.PreviewOf(id));
			TwoLine(spriteBatch, hit.X + 126, hit.Y + 8, hit.Width - 170,
				WeText.UI(WePresetLogos.TitleKey(id)), WeText.UI("LogoFromPack") + "  ·  " + WeText.UI("BorrowKindLogo"));
			Affordance(spriteBatch, hit, CardKind.Radio, on);
			y += 62;
		}

		private static void DrawBadge(SpriteBatch spriteBatch, Rectangle badge, Texture2D icon, string modName)
		{
			if (icon != null && !icon.IsDisposed) {
				float scale = Math.Min(badge.Width / (float)icon.Width, badge.Height / (float)icon.Height);
				spriteBatch.Draw(icon, badge.Center.ToVector2(), null, Color.White * _fade, 0f, icon.Size() * 0.5f, scale, SpriteEffects.None, 0f);
				return;
			}

			WeTerra.Panel(spriteBatch, badge, (modName == null ? WeAccent.Deep : BadgeColor(modName)) * _fade, Color.Black * _fade);
			WeTerra.Icon(spriteBatch, WeIcons.Puzzle, badge.Center.ToVector2(), 16f, Color.White * _fade);
		}

		private static void DrawBorrowCard(SpriteBatch spriteBatch, Rectangle panel, ref int y, WeOffer offer, bool on)
		{
			Rectangle hit = Row(panel, y, 56);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			Texture2D icon = WeCatalog.ModIcon(offer.ModName);
			DrawBadge(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 14, 28, 28), icon, offer.ModName);

			Texture2D preview = offer.Kind == WeOfferKind.Logo
				? WeCatalog.LogoTexture(offer.Id)
				: WeCatalog.SkyPreview(offer);
			var thumb = new Rectangle(hit.X + 44, hit.Y + 8, 72, 40);
			if (preview != null)
				Thumb(spriteBatch, thumb, preview, cover: offer.Kind != WeOfferKind.Logo);
			else
				Thumb(spriteBatch, thumb, icon, alpha: 0.55f);

			string title = offer.Pending ? WeText.UI("BorrowPending") : offer.MenuTitle;
			string kind = offer.Kind == WeOfferKind.Logo
				? "BorrowKindLogo"
				: offer.UseThemeFx ? "BorrowKindSky" : "BorrowKindStill";
			// Combine in one click: a sky card whose theme also has a logo can take both (right-click).
			bool pair = offer.Kind == WeOfferKind.Sky && WeCatalog.FindLogo(offer.Id) != null;
			string sub = offer.ModTitle + "  ·  " + WeText.UI(kind);
			if (pair && hover)
				sub = WeText.UI("BorrowBothHint");
			TwoLine(spriteBatch, hit.X + 126, hit.Y + 8, hit.Width - 170, title, sub);
			Affordance(spriteBatch, hit, CardKind.Radio, on);
			if (_id == WePanelId.Wallpaper) {
				string borrowId = offer.Id;
				Action both = null;
				if (pair) {
					both = () => {
						WeSettings.SetWallpaperBorrowed(borrowId);
						WeSettings.SetLogo(LogoKind.Borrowed, borrowId);
						WeToast.Show("ToastBorrowBoth");
					};
				}

				Arm(hit, () => {
					WeSettings.SetWallpaperBorrowed(borrowId);
					WeToast.Show("ToastWallpaper");
				}, both);
			}

			y += 62;
		}

		private static bool ClickBorrow(Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, 56);
			y += 62;
			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		private static Color BadgeColor(string name)
		{
			int h = name?.GetHashCode() ?? 0;
			return new Color(
				(byte)(70 + (h & 0x5F)),
				(byte)(70 + ((h >> 7) & 0x5F)),
				(byte)(80 + ((h >> 14) & 0x5F)));
		}

		private static string Ellipsize(string text, int max)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= max)
				return text ?? "";
			return text[..Math.Max(1, max - 1)] + "...";
		}

		private static void DrawTrack(SpriteBatch spriteBatch, Rectangle panel, ref int y, WeTrackRecord track)
		{
			Rectangle hit = Row(panel, y, 36);
			Rectangle trash = TrashRect(hit);
			Rectangle body = new(hit.X, hit.Y, Math.Max(8, trash.X - hit.X - 8), hit.Height);
			bool on = !WeSave.Data.DisabledTrackIds.Contains(track.Id);
			bool playing = WeSave.Data.Music == MusicKind.Custom && WePlaylist.Current?.Id == track.Id;
			bool hover = body.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, body, hover, playing);
			WeTerra.Icon(spriteBatch, playing ? WeIcons.Play : WeIcons.Music, new Vector2(body.X + 20, body.Center.Y), 16f, Color.White * ((on ? 1f : 0.45f) * _fade));
			WeTerra.Label(spriteBatch, WeTerra.Fit(track.Title + "  ·  " + track.Artist, 0.8f, body.Width - 44),
				new Vector2(body.X + 36, hit.Y + 9), Color.White * ((on ? 1f : 0.5f) * _fade), 0.8f);
			DrawTrashSlot(spriteBatch, trash, WeText.UI("DeleteTrack"));
			y += 42;
		}

		private static void DrawTrashSlot(SpriteBatch spriteBatch, Rectangle trash, string tip)
		{
			bool hover = trash.Contains(Main.mouseX, Main.mouseY);
			WeTerra.Slot(spriteBatch, trash, WeIcons.Trash, hover, false, _fade);
			if (hover)
				Tip(trash, tip);
		}

		private static void DrawPackedTrack(SpriteBatch spriteBatch, Rectangle panel, ref int y, MenuTrack track)
		{
			Rectangle hit = Row(panel, y, 36);
			Rectangle trash = TrashRect(hit);
			Rectangle body = new(hit.X, hit.Y, Math.Max(8, trash.X - hit.X - 8), hit.Height);
			bool on = !WeSave.Data.DisabledTrackIds.Contains(track.Id);
			bool playing = WeSave.Data.Music == MusicKind.Custom && WePlaylist.Current?.Id == track.Id;
			bool hover = body.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, body, hover, playing);
			Texture2D badge = WePresetLogos.PackIcon();
			if (playing || badge == null)
				WeTerra.Icon(spriteBatch, playing ? WeIcons.Play : WeIcons.Music, new Vector2(body.X + 20, body.Center.Y), 16f, Color.White * ((on ? 1f : 0.45f) * _fade));
			else {
				float scale = 22f / Math.Max(1, Math.Max(badge.Width, badge.Height));
				spriteBatch.Draw(badge, new Vector2(body.X + 20, body.Center.Y), null, Color.White * ((on ? 1f : 0.45f) * _fade), 0f, badge.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			}

			WeTerra.Label(spriteBatch, WeTerra.Fit(track.Title + "  ·  " + track.Artist, 0.8f, body.Width - 44),
				new Vector2(body.X + 36, hit.Y + 9), Color.White * ((on ? 1f : 0.5f) * _fade), 0.8f);
			bool hoverTrash = trash.Contains(Main.mouseX, Main.mouseY);
			WeTerra.Slot(spriteBatch, trash, on ? WeIcons.Hide : WeIcons.Show, hoverTrash, false, _fade);
			if (hoverTrash)
				Tip(trash, WeText.UI(on ? "HidePackedTrack" : "RestorePackedTrack"));
			y += 42;
		}

		private static bool ClickPackedTrack(Rectangle panel, ref int y, MenuTrack track)
		{
			Rectangle hit = Row(panel, y, 36);
			Rectangle trash = TrashRect(hit);
			Rectangle body = new(hit.X, hit.Y, Math.Max(8, trash.X - hit.X - 8), hit.Height);
			y += 42;
			if (trash.Contains(Main.mouseX, Main.mouseY)) {
				bool hide = !WeSave.Data.DisabledTrackIds.Contains(track.Id);
				WePlaylist.SetPackedEnabled(track, !hide);
				WeToast.Show(hide ? "HidePackedTrack" : "RestorePackedTrack");
				return true;
			}

			if (!body.Contains(Main.mouseX, Main.mouseY))
				return false;

			WePlaylist.PlayPacked(track);
			return true;
		}

		private static bool PackedVisible(MenuTrack track) =>
			_showPackedHidden || !WeSave.Data.DisabledTrackIds.Contains(track.Id);

		private static int HiddenPackedCount()
		{
			int n = 0;
			foreach (MenuTrack packed in WePackedMusic.Tracks) {
				if (WeSave.Data.DisabledTrackIds.Contains(packed.Id))
					n++;
			}

			return n;
		}

		private static bool ClickTrack(Rectangle panel, ref int y, WeTrackRecord track)
		{
			Rectangle hit = Row(panel, y, 36);
			Rectangle trash = TrashRect(hit);
			Rectangle body = new(hit.X, hit.Y, Math.Max(8, trash.X - hit.X - 8), hit.Height);
			y += 42;
			if (trash.Contains(Main.mouseX, Main.mouseY)) {
				WePlaylist.DeleteCustom(track);
				WeToast.Show("ToastTrackDeleted");
				return true;
			}

			if (!body.Contains(Main.mouseX, Main.mouseY))
				return false;

			WePlaylist.PlayTrack(track);
			return true;
		}

		private static Rectangle TrashRect(Rectangle hit) =>
			new(hit.Right - 34, hit.Y + 4, 28, 28);

		private static void DrawRgb(SpriteBatch spriteBatch, Rectangle panel, ref int y, string key, Color color, string label = null)
		{
			if (!string.IsNullOrEmpty(label)) {
				WeTerra.Label(spriteBatch, label, new Vector2(panel.X + 18, y), Color.White * _fade, 0.72f);
				y += 20;
			}

			DrawSlider(spriteBatch, panel, ref y, key + "R", color.R / 255f, WeText.UI("Red"));
			DrawSlider(spriteBatch, panel, ref y, key + "G", color.G / 255f, WeText.UI("Green"));
			DrawSlider(spriteBatch, panel, ref y, key + "B", color.B / 255f, WeText.UI("Blue"));
			var swatch = new Rectangle(panel.Right - 52, y - 70, 28, 62);
			WeDraw.Fill(spriteBatch, new Rectangle(swatch.X - 2, swatch.Y - 2, swatch.Width + 4, swatch.Height + 4), Color.Black * _fade);
			WeDraw.Fill(spriteBatch, swatch, color * _fade);
			y += 8;
		}

		private static void ClickRgb(Rectangle panel, ref int y, string key, bool labeled = false)
		{
			if (labeled)
				y += 20;
			ClickSlider(panel, ref y, key + "R");
			ClickSlider(panel, ref y, key + "G");
			ClickSlider(panel, ref y, key + "B");
			y += 8;
		}

		private static void DrawSlider(SpriteBatch spriteBatch, Rectangle panel, ref int y, string key, float value, string label)
		{
			Rectangle bar = new(panel.X + 90, y + 4, panel.Width - 160, 16);
			WeTerra.Label(spriteBatch, WeTerra.Fit(label, 0.7f, 68), new Vector2(panel.X + 18, y + 2), Color.White * (0.85f * _fade), 0.7f);
			bool hover = bar.Contains(Main.mouseX, Main.mouseY) || _dragSlider == key;
			WeTerra.Slider(spriteBatch, bar, value, hover, _fade);
			WeTerra.Label(spriteBatch, (int)Math.Round(MathHelper.Clamp(value, 0f, 1f) * 100f) + "%", new Vector2(bar.Right + 10, y + 2), WeTerra.Muted * _fade, 0.66f);
			y += 22;
		}

		private static void ClickSlider(Rectangle panel, ref int y, string key)
		{
			Rectangle bar = new(panel.X + 90, y + 4, panel.Width - 160, 16);
			y += 22;
			if (bar.Contains(Main.mouseX, Main.mouseY)) {
				_dragSlider = key;
				WeSave.Holding = true;
				ApplySlider(key);
			}
		}

		private static void ApplySlider(string key)
		{
			Rectangle panel = PanelRect();
			Rectangle bar = new(panel.X + 90, 0, panel.Width - 160, 8);
			float t = MathHelper.Clamp((Main.mouseX - bar.X) / (float)bar.Width, 0f, 1f);
			if (key == "dim") {
				WeSave.Data.WallpaperDim = t;
				WeSave.Save();
				return;
			}

			if (key == "vignette") {
				WeSave.Data.WallpaperVignette = t;
				WeSave.Save();
				return;
			}

			if (key == "fw" || key == "fh") {
				WeSettings.SetFontScale(key == "fw", 0.5f + t * 1.3f);
				return;
			}

			if (key is "lop" or "lpar" or "lzm") {
				WeLayerRecord layer = WeSettings.SelectedLayer();
				if (layer == null)
					return;
				if (key == "lop")
					layer.Opacity = t;
				else if (key == "lpar")
					layer.Parallax = t;
				else
					layer.Zoom = 0.6f + t * 1.2f;
				WeSave.Save();
				return;
			}

			int v = (int)(t * 255f);
			if (key.StartsWith("wallA") || key.StartsWith("wallB")) {
				Color a = WeSettings.WallpaperColorA;
				Color b = WeSettings.WallpaperColorB;
				bool second = key.StartsWith("wallB");
				Color c = second ? b : a;
				if (key.EndsWith("R"))
					c.R = (byte)v;
				else if (key.EndsWith("G"))
					c.G = (byte)v;
				else
					c.B = (byte)v;
				WeSettings.SetWallpaperRgb(second, c.R, c.G, c.B);
				return;
			}

			if (key.StartsWith("menu")) {
				Color c = WeSettings.MenuTextColor;
				if (key.EndsWith("R"))
					c.R = (byte)v;
				else if (key.EndsWith("G"))
					c.G = (byte)v;
				else
					c.B = (byte)v;
				WeSettings.SetMenuTextRgb(c.R, c.G, c.B);
				return;
			}

			string which = key.StartsWith("caption") ? "caption" : key.StartsWith("border") ? "border" : "title";
			Color cur = which == "caption" ? WeSettings.CaptionColor : which == "border" ? WeSettings.BorderColor : WeSettings.TitleTextColor;
			if (key.EndsWith("R"))
				cur.R = (byte)v;
			else if (key.EndsWith("G"))
				cur.G = (byte)v;
			else
				cur.B = (byte)v;
			WeSettings.SetChromeRgb(which, cur.R, cur.G, cur.B);
		}

		private static void DrawAccent(SpriteBatch spriteBatch, Rectangle panel, ref int y, int index)
		{
			Rectangle hit = Row(panel, y, 28);
			WeDraw.Fill(spriteBatch, hit, WeAccent.Palettes[index].Mid * _fade);
			if (index == WeAccent.Index)
				WeDraw.Border(spriteBatch, hit, Color.White * _fade);
			y += 34;
		}

		private static bool ClickAccent(Rectangle panel, ref int y)
		{
			Rectangle hit = Row(panel, y, 28);
			y += 34;
			return hit.Contains(Main.mouseX, Main.mouseY);
		}

		/// <summary>Screen rectangle of the open window (used to keep the sidebar beside it).</summary>
		internal static Rectangle WindowRect => PanelRect();

		private static Rectangle PanelRect()
		{
			// One docked window for every section, on the right so the title screen stays visible.
			int w = Math.Clamp((int)(Main.screenWidth * 0.34f), 420, 520);
			w = Math.Min(w, Main.screenWidth - 24);
			int h = Math.Max(320, Main.screenHeight - 40);
			return new Rectangle(Main.screenWidth - w - 16, 20, w, h);
		}

		private static Rectangle View(Rectangle panel) =>
			new(panel.X + 8, panel.Y + HeaderH + TabsH + 4, panel.Width - 16, panel.Height - HeaderH - TabsH - 16);

		private static Rectangle Row(Rectangle panel, int y, int h) =>
			new(panel.X + 16, y, panel.Width - 32, h);

		private static string LayerTitle(WeLayerRecord layer)
		{
			string name = layer.Kind == WeLayerKind.Effect
				? WeText.UI(FxKey(layer.Effect))
				: FileNameOf(layer.ArtId);
			if (layer.Foreground)
				name += "  ·  " + WeText.UI("LayerForeground");
			return name;
		}

		private static string FileNameOf(string artId)
		{
			if (string.IsNullOrEmpty(artId))
				return WeText.UI("ImageLayer");
			WeArtRecord record = WeSave.Data.Wallpapers.Find(item => item.Id == artId);
			return record == null ? WeText.UI("ImageLayer") : record.FileName;
		}

		private static string FitKey() => FitKey(WeSave.Data.WallpaperFit);

		private static string FitKey(WallpaperFit fit) => fit switch {
			WallpaperFit.Contain => "FitContain",
			WallpaperFit.Stretch => "FitStretch",
			_ => "FitCover"
		};

		private static string FxKey(WeFxKind effect) => effect switch {
			WeFxKind.Dust => "FxDust",
			WeFxKind.Fog => "FxFog",
			WeFxKind.Grain => "FxGrain",
			WeFxKind.Scanlines => "FxScan",
			WeFxKind.Fireflies => "FxFlies",
			WeFxKind.Clouds => "FxClouds",
			WeFxKind.Rain => "FxRain",
			WeFxKind.Beat => "FxBeat",
			_ => "FxStars"
		};

		private static float MaxScroll()
		{
			float content = _id == WePanelId.Wallpaper ? Math.Max(_wallHeight, _contentH) : _contentH;
			return Math.Max(0f, content - View(PanelRect()).Height);
		}

	}
}
