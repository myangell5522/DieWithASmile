using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using DieWithASmile.Engine.Content;

namespace DieWithASmile.Content
{
	/// <summary>
	/// Adds a "Main Menu Music" slider under Ambient on the vanilla title-screen Volume page (menuMode 26).
	///
	/// Layout (vanilla, fixed pixels): Music/Sound/Ambient bars at y = 332/362/392, Back button from y = 470.
	/// Our row goes at 422, one vanilla step (30px) below Ambient. Swap My Audio! draws its Audio Device line
	/// below the last volume row; it asks us through Mod.Call("TitleVolumeRows") how many rows we added, so the
	/// two never overlap whichever order the hooks run in.
	///
	/// The in-game (Esc) Volume page is left alone: menu music isn't playing in a world, and its only free row
	/// is the spacer under Ambient, which Swap My Audio! uses for the device picker.
	/// </summary>
	public class CalamitasMenuVolume : ModSystem
	{
		private const int TitleVolumeMenuMode = 26;
		private const int RowStep = 30;
		private const int LockId = 5; // vanilla title bars use 2, 3 and 4

		private static int _depth;
		private static int _bars;
		private static bool _dirty;

		/// <summary>Extra rows this mod draws under the vanilla title volume bars this frame (0 or 1).</summary>
		internal static int TitleRows => ShowTitleRow ? 1 : 0;

		private static bool ShowTitleRow =>
			Main.gameMenu && Main.menuMode == TitleVolumeMenuMode && WeModMenu.IsActive;

		public override void Load()
		{
			if (Main.dedServ)
				return;

			On_Main.DrawMenu += DrawMenuHook;
			On_IngameOptions.DrawValueBar += DrawValueBarHook;
		}

		public override void Unload()
		{
			SaveIfDirty();
		}

		private static void DrawMenuHook(On_Main.orig_DrawMenu orig, Main self, GameTime time)
		{
			_bars = 0;
			_depth = 0;
			orig(self, time);
			if (!Main.mouseLeft)
				SaveIfDirty();
		}

		private static float DrawValueBarHook(On_IngameOptions.orig_DrawValueBar orig, SpriteBatch sb, float scale, float perc, int lockState, Utils.ColorLerpMethod colorMethod)
		{
			_depth++;
			try {
				Vector2 row = IngameOptions.valuePosition;
				float result = orig(sb, scale, perc, lockState, colorMethod);

				// Only count vanilla's own three bars, not bars other mods draw from inside their hooks.
				if (_depth != 1 || !ShowTitleRow)
					return result;

				_bars++;
				if (_bars == 3)
					DrawMenuMusicRow(sb, row);

				return result;
			}
			finally {
				_depth--;
			}
		}

		private static void DrawMenuMusicRow(SpriteBatch sb, Vector2 ambientRow)
		{
			// Vanilla reads IngameOptions.inBar right after this call to decide whether Ambient is hovered.
			// Our bar overwrites it, so keep Ambient's value and put it back afterwards.
			bool ambientInBar = IngameOptions.inBar;
			float volume = DieWithASmileSettings.MenuMusicVolume;
			var barPos = new Vector2(ambientRow.X, ambientRow.Y + RowStep);

			// Label in the same column/style as vanilla's "Music"/"Sound"/"Ambient" labels (x = center + 7, 12px above the bar).
			string label = CalamitasMenuText.UI("MainMenuMusic") + " " + Math.Round(volume * 100f) + "%";
			Color text = Color.White;
			Utils.DrawBorderStringFourWay(sb, FontAssets.DeathText.Value, label,
				Main.screenWidth / 2 + 7, barPos.Y - 12f, text, Color.Black, Vector2.Zero, 0.5f);

			IngameOptions.valuePosition = barPos;
			float dragged = IngameOptions.DrawValueBar(sb, 1f, volume);
			bool hovered = IngameOptions.inBar;
			IngameOptions.inBar = ambientInBar;

			// Same pattern vanilla uses for its bars: hovering claims rightHover, vanilla then copies it into
			// rightLock at the end of the block, and a held mouse keeps dragging while the lock is ours.
			if (hovered || IngameOptions.rightLock == LockId) {
				IngameOptions.rightHover = LockId;
				if (Main.mouseLeft && IngameOptions.rightLock == LockId)
					SetVolume(dragged);
			}
		}

		private static void SetVolume(float value)
		{
			value = Math.Clamp(value, 0f, 1f);
			if (Math.Abs(DieWithASmileSave.Data.MenuMusicVolume - value) < 0.0005f)
				return;

			DieWithASmileSave.Data.MenuMusicVolume = value;
			_dirty = true; // written once on release, not every frame of the drag
		}

		private static void SaveIfDirty()
		{
			if (!_dirty)
				return;

			_dirty = false;
			DieWithASmileSave.Save();
		}
	}
}
