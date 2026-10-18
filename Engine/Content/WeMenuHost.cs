using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;
using DieWithASmile.Engine.Widgets;
using DieWithASmile.Engine.Audio;
using DieWithASmile.Engine.Grab;
using DieWithASmile.Content;

namespace DieWithASmile.Engine.Content
{
	public class WeMenuHost : ModSystem
	{
		private static bool _esc;
		private static bool _skipCursor;
		private static bool _overlayCursor;

		public override void Load()
		{
			if (Main.dedServ)
				return;

			WeSave.EnsureLoaded();
			On_Main.DrawMenu += DrawMenuHook;
			On_Main.DrawCursor += DrawCursorHook;
			On_Main.DrawThickCursor += DrawThickCursorHook;
			WeBorrowFx.Load();
			WeModListLook.Load(Mod);
		}

		public override void Unload()
		{
			On_Main.DrawMenu -= DrawMenuHook;
			On_Main.DrawCursor -= DrawCursorHook;
			On_Main.DrawThickCursor -= DrawThickCursorHook;
			WeCatalog.Unload();
			WeBorrow.Unload();
			WeBorrowFx.Unload();
			WeModListLook.Unload();
			WeArt.Unload();
			WeUi.Unload();
			DiscordFeed.Unload();
			WePlaylist.Unload();
			WePresetLogos.Unload();
			WeDraw.Unload();
			ClientChrome.Unload();
			WeType.Unload();
		}

		internal static void TickLogic()
		{
			WeToast.Update();
			LayoutEditor.Update();
			WeUi.UpdateUi();
			WeUiChooser.Update();
			WidgetHost.Update();
			WeFx.Update();
			WeCatalog.Pulse();
			WeBorrowFx.Tick();
			WeNestedPacks.EnsureWallpaper();
			WeBackgroundStyle.DrewThisFrame = false;
		}

		internal static void DrawOverlay(SpriteBatch spriteBatch)
		{
			WeWallpaper.DrawFore(spriteBatch);
			WidgetHost.Draw(spriteBatch, 1f);
		}

		internal static void FinishMenuChrome()
		{
			if (!WeModMenu.IsActive || !CoolerMenuCompat.MenuBackdropActive)
				return;

			if (WeModMenu.OnTitle) {
				DrawTitleChrome();
				DrawCursorOnTop();
			}

			WeBackgroundStyle.EndFrame();
			LayoutEditor.EndFrame();
			WeUi.EndFrame();
		}

		private static void DrawTitleChrome()
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			if (spriteBatch == null)
				return;

			TryEnd(spriteBatch);
			try {
				WeDraw.BeginUi(spriteBatch);
				LayoutEditor.Draw(spriteBatch, 1f);
				WeUi.DrawTitleChrome(spriteBatch);
				WeUiChooser.Draw(spriteBatch);
				WeToast.Draw(spriteBatch);
			}
			finally {
				TryEnd(spriteBatch);
			}
		}

		private static void TryEnd(SpriteBatch spriteBatch)
		{
			try {
				spriteBatch.End();
			}
			catch {
			}
		}

		internal static void DrawCursorOnTop()
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			if (spriteBatch == null)
				return;

			bool previous = _overlayCursor;
			_overlayCursor = true;
			TryEnd(spriteBatch);
			try {
				BeginCursor(spriteBatch);
				Vector2 bonus = Main.DrawThickCursor();
				Main.DrawCursor(bonus);
			}
			catch {
				try {
					Main.DrawCursor(Vector2.Zero);
				}
				catch {
				}
			}
			finally {
				TryEnd(spriteBatch);
				_overlayCursor = previous;
			}
		}

		private static void BeginCursor(SpriteBatch spriteBatch)
		{
			spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				DepthStencilState.None,
				RasterizerState.CullCounterClockwise,
				null,
				Main.UIScaleMatrix);
		}

		private static bool OwnsTitleCursor =>
			WeModMenu.IsActive && WeModMenu.OnTitle && CoolerMenuCompat.MenuBackdropActive;

		private static void DrawCursorHook(On_Main.orig_DrawCursor orig, Vector2 bonus, bool smart)
		{
			if (_overlayCursor) {
				orig(bonus, smart);
				return;
			}

			if (_skipCursor)
				return;
			orig(bonus, smart);
		}

		private static Vector2 DrawThickCursorHook(On_Main.orig_DrawThickCursor orig, bool smart)
		{
			if (_overlayCursor)
				return orig(smart);
			if (_skipCursor)
				return Vector2.Zero;
			return orig(smart);
		}

		private static void DrawMenuHook(On_Main.orig_DrawMenu orig, Main self, GameTime time)
		{
			if (Main.gameMenu && WeModMenu.IsActive)
				WePlaylist.Update();

			bool steal = false;
			bool releaseAfterInput = Main.mouseLeftRelease;
			int savedMouseY = Main.mouseY;
			bool remapY = false;
			bool savedMouseLeft = Main.mouseLeft;

			if (WeModMenu.OnTitle) {
				WeAnim.Pulse();
				HandleInput();
				releaseAfterInput = Main.mouseLeftRelease;
				steal = WeUiChooser.Visible || WeUi.SplashVisible || WeUi.Covering || WeUi.AteInput || WeUi.HubBusy || LayoutEditor.Editing || WidgetHost.Busy;
				if (steal) {
					Main.blockMouse = true;
					Main.mouseLeftRelease = false;
					Main.mouseLeft = false;
				}

				MenuButtonHooks.BeginFrame();
				int dy = MenuButtonHooks.MouseRemapY;
				if (dy != 0 && !WeUi.Covering && !WeUi.SplashVisible && !WeUiChooser.Visible && SceneGraph.Visible(SceneGraph.MenuButtons)) {
					Main.mouseY -= dy;
					remapY = true;
				}
			}

			_skipCursor = OwnsTitleCursor;
			try {
				orig(self, time);
			}
			finally {
				_skipCursor = false;
			}

			if (steal) {
				Main.blockMouse = true;
				Main.mouseLeft = savedMouseLeft && WeInput.LeftDown;
				if (WeInput.LeftDown)
					Main.mouseLeftRelease = false;
				else
					Main.mouseLeftRelease = releaseAfterInput;
			}

			if (remapY)
				Main.mouseY = savedMouseY;

			FinishMenuChrome();
		}

		private static void HandleInput()
		{
			bool esc = Main.keyState.IsKeyDown(Keys.Escape);
			bool escPressed = esc && !_esc;
			_esc = esc;
			// The interface chooser comes first and owns all input while it is up.
			if (WeUiChooser.HandleInput(escPressed))
				return;

			if (escPressed) {
				if (WeUi.SplashVisible)
					WeUi.SplashDismiss(false);
				else if (WeUi.PanelOpen)
					WeUi.ClosePanels();
				else if (LayoutEditor.Editing)
					LayoutEditor.Cancel(true);
				else if (WeUi.HubExpanded)
					WeUi.HubCollapse();
			}

			WeUi.HandleSplashInput();
			if (WeUi.SplashVisible)
				return;

			WeUi.HandlePanelAndHubInput();
			LayoutEditor.HandleInput();
			WidgetHost.HandleInput();
		}
	}
}
