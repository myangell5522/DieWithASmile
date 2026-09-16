using System;
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
		private static float _page = 1f;
		private static float _scroll;
		private static float _contentH;
		private static int _lastWheel;
		private static int _clientChip;
		private static int _cursorChip;
		private static bool _factoryArmed;
		private static string _search = "";
		private static string _drag;
		private static WeNeoCat _cat = WeNeoCat.Game;
		private static WeNeoCat _returnCat = WeNeoCat.Game;

		internal static bool IsOpen => _open;
		internal static bool Covering => _open || _fade > 0.02f;
		internal static bool AteInput => _ate;
		internal static WeNeoCat Category => _cat;
		internal static float Fade => _fade;
		internal static float PageFade => _page;
		internal static string Search => _search ?? "";
		internal static bool SearchFocus => _searchFocus;
		internal static string Drag => _drag;
		internal static int ClientChip => _clientChip;
		internal static int CursorChip => _cursorChip;
		internal static bool FactoryArmed => _factoryArmed;
		internal static float Scroll => _scroll;
		internal static bool InGame => _inGame;

		internal static void SetContentHeight(float h) => _contentH = h;

		internal static void SetClientChip(int chip) => _clientChip = Math.Clamp(chip, 0, 2);

		internal static void SetCursorChip(int chip) => _cursorChip = Math.Clamp(chip, 0, 1);

		internal static void SetFactoryArmed(bool armed) => _factoryArmed = armed;

		internal static void SetDrag(string id) => _drag = id;

		internal static void SetScroll(float value) => _scroll = value;

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
			if (Main.menuMode != 11)
				return;
			Main.menuMode = 0;
			if (_pendingReturn)
				return;
			Open(WeNeoCat.Game, inGame: false);
		}

		internal static void CatchReturn()
		{
			if (!_pendingReturn)
				return;

			if (Main.gameMenu) {
				if (Main.menuMode == 11) {
					Main.menuMode = 0;
					Open(_returnCat, inGame: false);
					_pendingReturn = false;
				}
				else if (Main.menuMode == 0 && !Main.inFancyUI && !_open) {
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
			_scroll = 0f;
			_page = 1f;
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
			_pendingReturn = false;
			SoundEngine.PlaySound(SoundID.MenuClose);
			if (save)
				Persist();
			if (_inGame) {
				Main.ingameOptionsWindow = false;
				IngameOptions.Close();
			}
		}

		internal static void BeginDeepLink(WeNeoCat cat)
		{
			_returnCat = cat;
			_pendingReturn = true;
			_wasFancy = true;
			_open = false;
			_searchFocus = false;
			_drag = null;
			Persist();
		}

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
				if (_searchFocus)
					_searchFocus = false;
				else if (_open && !WeSplash.Visible)
					Close();
			}

			_esc = esc;
			if (!_open)
				return;

			_ate = true;
			Main.blockMouse = true;
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
			_page = MathHelper.Lerp(_page, 1f, 0.22f);
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

		internal static float MaxScroll(Rectangle view) =>
			Math.Max(0f, _contentH - view.Height);

		internal static void Wheel(Rectangle view)
		{
			int wheel = Mouse.GetState().ScrollWheelValue;
			if (view.Contains(Main.mouseX, Main.mouseY))
				_scroll = MathHelper.Clamp(_scroll - (wheel - _lastWheel) / 120f * 48f, 0f, MaxScroll(view));
			_lastWheel = wheel;
		}

		internal static void SelectCat(WeNeoCat cat)
		{
			if (_cat == cat)
				return;
			_cat = cat;
			_scroll = 0f;
			_page = 0f;
			_factoryArmed = false;
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
