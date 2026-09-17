using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Content;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal enum WeNeoCat
	{
		Game,
		Video,
		Audio,
		Interface,
		Cursor,
		Controls,
		Mods,
		Client
	}

	public class WeNeoMenu : ModSystem
	{
		private static bool _open;
		private static bool _inGame;
		private static bool _pendingReturn;
		private static bool _searchFocus;
		private static bool _esc;
		private static bool _mouseHeld;
		private static bool _holdLock;
		private static bool _rightHeld;
		private static bool _rightLock;
		private static bool _ate;
		private static bool _frameInput;
		private static bool _wasFancy;
		private static float _fade;
		private static float _pageFade = 1f;
		private static float _scroll;
		private static float _scrollWant;
		private static float _scrollVel;
		private static float _contentH;
		private static int _chrome;
		private static int _viewH;
		private static int _lastWheel;
		private static int _dragY;
		private static int _dragStartY;
		private static bool _panned;
		private static int _clientChip;
		private static int _cursorChip;
		private static bool _factoryArmed;
		private static string _search = "";
		private static string _drag;
		private static WeNeoCat _cat = WeNeoCat.Game;
		private static WeNeoCat _returnCat = WeNeoCat.Game;
		private static WeNeoPage _leaf = WeNeoPage.Hub;
		private static WeNeoPage _resumePage = WeNeoPage.Hub;

		internal static bool IsOpen => _open;
		internal static bool Covering => _open || _fade > 0.02f;
		internal static bool AteInput => _ate;
		internal static WeNeoCat Category => _cat;
		internal static WeNeoPage Page => _leaf;
		internal static float Fade => _fade;
		internal static float PageFade => _pageFade;
		internal static string Search => _search ?? "";
		internal static bool SearchFocus => _searchFocus;
		internal static string Drag => _drag;
		internal static int ClientChip => _clientChip;
		internal static int CursorChip => _cursorChip;
		internal static bool FactoryArmed => _factoryArmed;
		internal static float Scroll => _scroll;
		internal static int Chrome => _chrome;
		internal static bool Panned => _panned;
		internal static bool InGame => _inGame;

		internal static void SetContentHeight(float h) => _contentH = h;

		internal static void SetClientChip(int chip) => _clientChip = Math.Clamp(chip, 0, 2);

		internal static void SetCursorChip(int chip) => _cursorChip = Math.Clamp(chip, 0, 1);

		internal static void SetPage(WeNeoPage page)
		{
			_leaf = page;
			SetScroll(0f);
			_pageFade = 0f;
			_chrome = 0;
			WeTml.Touch();
		}

		internal static void SetFactoryArmed(bool armed) => _factoryArmed = armed;

		internal static void SetDrag(string id)
		{
			_drag = id;
			_dragY = Main.mouseY;
			_dragStartY = Main.mouseY;
			_panned = false;
		}

		internal static void SetChrome(int h) => _chrome = Math.Max(0, h);

		internal static void SetViewHeight(int h) => _viewH = Math.Max(1, h);

		internal static void SetScroll(float value)
		{
			float max = MaxScroll();
			_scroll = MathHelper.Clamp(value, 0f, max);
			_scrollWant = _scroll;
			_scrollVel = 0f;
		}

		internal static void AddScroll(float delta)
		{
			_scrollWant = MathHelper.Clamp(_scrollWant + delta, 0f, MaxScroll());
		}

		internal static void SetSearch(string value) => _search = value ?? "";

		internal static void SetSearchFocus(bool focus)
		{
			if (focus && !_searchFocus)
				Main.clrInput();
			_searchFocus = focus;
		}

		public override void Load()
		{
			if (Main.dedServ)
				return;
			On_IngameOptions.Draw += DrawIngameHook;
			On_IngameOptions.MouseOver += MouseOverHook;
		}

		public override void Unload()
		{
			On_IngameOptions.Draw -= DrawIngameHook;
			On_IngameOptions.MouseOver -= MouseOverHook;
		}

		public override void UpdateUI(GameTime gameTime)
		{
			Tick();
		}

		internal static void CatchTitleHub()
		{
			if (!Main.gameMenu || CoolerMenuCompat.WorldGenUiActive)
				return;
			if (Main.menuMode == 11) {
				Main.menuMode = 0;
				if (!_pendingReturn)
					Open(WeNeoCat.Game, inGame: false);
				return;
			}

			if (IsTransientTmlUi())
				return;

			if (IsWorkshopScreen()) {
				DismissFancy();
				if (!_open && !_pendingReturn)
					Open(WeNeoCat.Mods, inGame: false);
			}
		}

		internal static bool OnEsc()
		{
			if (!_open)
				return false;
			if (WeNeoKeys.Capturing) {
				WeNeoKeys.Cancel();
				return true;
			}

			if (_leaf != WeNeoPage.Hub && _cat == WeNeoCat.Mods) {
				SetPage(WeNeoPage.Hub);
				return true;
			}

			if (_searchFocus) {
				_searchFocus = false;
				return true;
			}

			Close();
			return true;
		}

		private static bool IsTransientTmlUi()
		{
			try {
				string name = Main.MenuUI?.CurrentState?.GetType().Name ?? "";
				if (name.IndexOf("Build", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("Progress", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("Publish", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("CreateMod", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("Extract", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("ModConfig", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("InfoMessage", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("Download", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}
			catch {
			}

			try {
				Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
				if (iface == null)
					return false;
				const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
				foreach (string field in new[] { "buildModID", "createModID", "extractModID", "progressID", "errorMessageID", "downloadProgressID" }) {
					object id = iface.GetField(field, flags)?.GetValue(null);
					if (id is int mode && Main.menuMode == mode)
						return true;
				}
			}
			catch {
			}

			return false;
		}

		private static bool IsWorkshopScreen()
		{
			if (Main.menuMode == 1007)
				return false;
			try {
				object state = Main.MenuUI?.CurrentState;
				string name = state?.GetType().Name ?? "";
				if (name.Contains("WorkshopHub") || name == "UIMods" || name.Contains("ModBrowser") ||
				    name.Contains("UIModPacks") || name.Contains("UIModSources"))
					return true;
			}
			catch {
			}

			foreach (int id in WorkshopIds()) {
				if (Main.menuMode == id)
					return true;
			}

			return false;
		}

		private static int[] WorkshopIds()
		{
			var ids = new System.Collections.Generic.List<int>();
			try {
				Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
				if (iface == null)
					return ids.ToArray();
				foreach (System.Reflection.FieldInfo f in iface.GetFields(
					         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
					         System.Reflection.BindingFlags.Static)) {
					if (f.FieldType != typeof(int))
						continue;
					string n = f.Name ?? "";
					if (n.Contains("workshop", StringComparison.OrdinalIgnoreCase) ||
					    n.Contains("modsMenu", StringComparison.OrdinalIgnoreCase) ||
					    n.Contains("modBrowser", StringComparison.OrdinalIgnoreCase) ||
					    n.Contains("modPacks", StringComparison.OrdinalIgnoreCase) ||
					    n.Contains("modSources", StringComparison.OrdinalIgnoreCase))
						ids.Add((int)f.GetValue(null));
				}
			}
			catch {
			}

			return ids.ToArray();
		}

		private static void DismissFancy()
		{
			try {
				Main.MenuUI?.SetState(null);
			}
			catch {
			}

			Main.menuMode = 0;
		}

		internal static void CatchReturn()
		{
			if (!_pendingReturn)
				return;
			if (IsTransientTmlUi())
				return;

			if (Main.gameMenu) {
				if (IsWorkshopScreen() || Main.menuMode == 11 || (Main.menuMode == 0 && !Main.inFancyUI && !_open)) {
					if (IsWorkshopScreen())
						DismissFancy();
					else if (Main.menuMode == 11)
						Main.menuMode = 0;
					Open(_returnCat, inGame: false);
					_pendingReturn = false;
				}

				return;
			}

			bool fancy = Main.inFancyUI;
			if (_wasFancy && !fancy && !Main.gameMenu) {
				Main.ingameOptionsWindow = true;
				Open(_returnCat, inGame: true);
				_pendingReturn = false;
			}

			_wasFancy = fancy;
		}

		internal static void Open(WeNeoCat cat, bool? inGame = null)
		{
			if (CoolerMenuCompat.WorldGenUiActive)
				return;

			bool game = inGame ?? !Main.gameMenu;
			if (!_open)
				SoundEngine.PlaySound(SoundID.MenuOpen);
			_open = true;
			_inGame = game;
			_cat = cat;
			SetScroll(0f);
			_pageFade = 1f;
			_leaf = _pendingReturn ? _resumePage : WeNeoPage.Hub;
			_resumePage = WeNeoPage.Hub;
			_drag = null;
			WeNeoBind.ClearDirty();
			_searchFocus = false;
			_factoryArmed = false;
			_pendingReturn = false;
			LayoutEditor.Cancel(false);
			WePanels.Close();
			WrenchToolbar.Collapse();
			if (WrenchHub.UseDock)
				WrenchDock.Reset();
			WePresets.Refresh();
			WeType.Scan();
			if (game)
				Main.ingameOptionsWindow = true;
		}

		internal static void Close(bool save = true)
		{
			if (!_open)
				return;
			_open = false;
			_searchFocus = false;
			_drag = null;
			_factoryArmed = false;
			_leaf = WeNeoPage.Hub;
			WeNeoKeys.Cancel();
			_pendingReturn = false;
			SoundEngine.PlaySound(SoundID.MenuClose);
			if (save)
				Persist();
			if (_inGame) {
				Main.ingameOptionsWindow = false;
				IngameOptions.Close();
			}
		}

		internal static void BeginDeepLink(WeNeoCat cat, WeNeoPage page = WeNeoPage.Hub)
		{
			_returnCat = cat;
			_resumePage = page;
			_pendingReturn = true;
			_wasFancy = true;
			_open = false;
			_searchFocus = false;
			_drag = null;
			Persist();
		}

		internal static bool NearListEnd() => MaxScroll() - _scroll < 160f;

		internal static void Persist()
		{
			try {
				Main.SaveSettings();
			}
			catch {
			}

			WeSave.Save();
			DieWithASmileSave.Save();
		}

		internal static void HandleInput()
		{
			if (_frameInput)
				return;
			_frameInput = true;

			bool esc = Main.keyState.IsKeyDown(Keys.Escape);
			if (esc && !_esc) {
				if (_open && !WeSplash.Visible && !Main.gameMenu)
					OnEsc();
			}

			_esc = esc;
			if (!_open)
				return;

			_ate = true;
			Main.blockMouse = true;
			if (WeNeoKeys.Capturing)
				WeNeoKeys.TickCapture();
			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			bool right = WeInput.Edge(WeInput.RightDown, ref _rightHeld, ref _rightLock);
			WeNeoShell.Handle(pressed, right);
			if (pressed)
				WeInput.LockHold(ref _holdLock);
			if (right)
				WeInput.LockHold(ref _rightLock);
		}

		internal static void EndFrame()
		{
			_frameInput = false;
			_ate = false;
		}

		internal static void Tick()
		{
			CatchTitleHub();
			CatchReturn();
			_fade = MathHelper.Lerp(_fade, _open ? 1f : 0f, 0.28f);
			_pageFade = MathHelper.Lerp(_pageFade, 1f, 0.22f);
			StepScroll();
			if (!_open && _fade < 0.02f)
				_fade = 0f;
			if (_open && _inGame)
				Main.ingameOptionsWindow = true;
		}

		internal static void Draw(SpriteBatch spriteBatch)
		{
			if (_fade <= 0.02f || spriteBatch == null)
				return;
			WeNeoShell.Draw(spriteBatch);
		}

		internal static float MaxScroll() =>
			Math.Max(0f, _contentH - Math.Max(1, _viewH));

		internal static float MaxScroll(Rectangle view)
		{
			_viewH = Math.Max(1, view.Height);
			return MaxScroll();
		}

		internal static void StepScroll()
		{
			float max = MaxScroll();
			_scrollWant = MathHelper.Clamp(_scrollWant, 0f, max);
			_scrollVel *= 0.82f;
			if (Math.Abs(_scrollVel) < 0.15f)
				_scrollVel = 0f;
			_scrollWant = MathHelper.Clamp(_scrollWant + _scrollVel, 0f, max);
			_scroll = MathHelper.Lerp(_scroll, _scrollWant, 0.34f);
			if (Math.Abs(_scroll - _scrollWant) < 0.35f && _scrollVel == 0f)
				_scroll = _scrollWant;
			_scroll = MathHelper.Clamp(_scroll, 0f, max);
		}

		internal static void Wheel(Rectangle view)
		{
			_viewH = Math.Max(1, view.Height);
			int wheel = Mouse.GetState().ScrollWheelValue;
			int delta = wheel - _lastWheel;
			_lastWheel = wheel;
			if (delta == 0 || !view.Contains(Main.mouseX, Main.mouseY))
				return;
			float notches = delta / 120f;
			_scrollVel -= notches * 22f;
			_scrollWant = MathHelper.Clamp(_scrollWant - notches * 64f, 0f, MaxScroll());
		}

		internal static Rectangle ListBox(Rectangle view) =>
			new(view.X, view.Y + _chrome, Math.Max(1, view.Width - 12), Math.Max(1, view.Height - _chrome));

		internal static Rectangle ScrollTrack(Rectangle view)
		{
			Rectangle list = ListBox(view);
			return new Rectangle(view.Right - 10, list.Y, 8, list.Height);
		}

		internal static Rectangle ScrollThumb(Rectangle view)
		{
			Rectangle track = ScrollTrack(view);
			float max = MaxScroll();
			if (max < 1f || track.Height < 8)
				return Rectangle.Empty;
			int thumbH = Math.Max(28, (int)(track.Height * track.Height / (track.Height + max)));
			thumbH = Math.Min(track.Height, thumbH);
			float t = _scroll / max;
			int thumbY = track.Y + (int)((track.Height - thumbH) * t);
			return new Rectangle(track.X, thumbY, track.Width, thumbH);
		}

		internal static bool PumpDrag(Rectangle view)
		{
			if (string.IsNullOrEmpty(_drag) || !_drag.StartsWith("neo", StringComparison.Ordinal))
				return false;

			if (_drag == "neo-hold") {
				if (Math.Abs(Main.mouseY - _dragStartY) > 8) {
					if (WeNeoShop.TryBeginPackDrag()) {
						_drag = "neo-pack";
						_panned = true;
					}
					else {
						_drag = "neo-pan";
						_panned = true;
						_dragY = Main.mouseY;
					}
				}
			}

			if (_drag == "neo-pan") {
				AddScroll(_dragY - Main.mouseY);
				_scroll = _scrollWant;
				_dragY = Main.mouseY;
				_panned = true;
			}
			else if (_drag == "neo-scroll") {
				Rectangle track = ScrollTrack(view);
				Rectangle thumb = ScrollThumb(view);
				float span = Math.Max(1f, track.Height - Math.Max(1, thumb.Height));
				float t = (Main.mouseY - track.Y - thumb.Height * 0.5f) / span;
				SetScroll(MathHelper.Clamp(t, 0f, 1f) * MaxScroll());
			}
			else if (_drag != null && _drag.StartsWith("neo-pack", StringComparison.Ordinal))
				WeNeoShop.DragPack(view, Main.mouseY);

			if (!WeInput.LeftDown) {
				bool fire = _drag == "neo-hold" && !_panned;
				if (_drag == "neo-pack")
					WeNeoShop.EndPackDrag();
				_drag = null;
				return fire;
			}

			return false;
		}

		internal static void SelectCat(WeNeoCat cat)
		{
			if (_cat == cat)
				return;
			_cat = cat;
			SetScroll(0f);
			_pageFade = 0f;
			_leaf = WeNeoPage.Hub;
			_factoryArmed = false;
			_chrome = 0;
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		internal static Color RowFill(bool hover) =>
			hover ? Color.Lerp(new Color(22, 24, 30), WeAccent.Deep, 0.52f) : new Color(22, 24, 30);

		internal static void DrawLabel(SpriteBatch spriteBatch, string text, Vector2 pos, Color color, float scale)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text ?? "",
				pos, color * _fade, 0f, Vector2.Zero, new Vector2(scale));
		}

		private static void MouseOverHook(On_IngameOptions.orig_MouseOver orig)
		{
			if (Covering)
				return;
			orig();
		}

		private static void DrawIngameHook(On_IngameOptions.orig_Draw orig, Main main, SpriteBatch sb)
		{
			if (CoolerMenuCompat.WorldGenUiActive) {
				orig(main, sb);
				return;
			}

			if (Main.ingameOptionsWindow && !_open && !_pendingReturn)
				Open(WeNeoCat.Game, inGame: true);

			if (!_open && _fade <= 0.02f) {
				orig(main, sb);
				return;
			}

			HandleInput();
			Tick();
			Draw(sb);
			EndFrame();
		}
	}
}
