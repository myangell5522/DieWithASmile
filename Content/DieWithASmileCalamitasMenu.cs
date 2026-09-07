using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using DieWithASmile.Engine.Audio;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Grab;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Content
{
	public class DieWithASmileCalamitasMenu : ModMenu
	{
		private const string EmptyTexturePath = "DieWithASmile/Assets/Textures/Menu/Empty";

		private Asset<Texture2D> _emptyTexture;
		private static bool _ticked;

		public override string DisplayName => Language.GetTextValue("Mods.DieWithASmile.DisplayName");

		public override ModSurfaceBackgroundStyle MenuBackgroundStyle
		{
			get
			{
				if (WeSave.Data.Wallpaper == WallpaperKind.Nested)
					return ModContent.GetInstance<CalamitasMenuBackgroundStyle>();
				if (WeSettings.HasCustomSky)
					return ModContent.GetInstance<WeBackgroundStyle>();
				return null;
			}
		}

		public override Asset<Texture2D> SunTexture => HideSunMoon ? _emptyTexture : base.SunTexture;

		public override Asset<Texture2D> MoonTexture => HideSunMoon ? _emptyTexture : base.MoonTexture;

		public override int Music
		{
			get
			{
				if (WeSave.Data.Music == MusicKind.Silence)
					return 0;
				if (WeSave.Data.Music == MusicKind.Custom)
					return WePlaylist.MenuMusicId;
				return 50;
			}
		}

		private static bool HideSunMoon =>
			!SceneGraph.Visible(SceneGraph.SunMoon) || WeSettings.HideSunMoon;

		public override void Load()
		{
			_emptyTexture = ModContent.Request<Texture2D>(EmptyTexturePath);
			WeIcons.Load();
			WePresetLogos.Load();
			WePackedMusic.EnsureExtracted(Mod);
			WePlaylist.Load(Mod);
			WeSpectrum.Load();
			CalamitasMenuLogo.Load();
		}

		public override void OnSelected()
		{
			CalamitasMenuPersist.OnOurMenuSelected();
			WePersist.OnSelected();
			WeSpectrum.Reset();
			WeArt.Scan();
			WeNestedPacks.EnsureWallpaper();
			CalamitasMenuBackgroundStyle.ResetFade();
			WeCatalog.Refresh();
			WeCatalog.DropMissing();
			WeLibrary.ScanIntoSave();
			WeSplash.OnThemeSelected();
			WePlaylist.OnThemeSelected();
			WePlayerUI.Reset();
			WePanels.Close();
			LayoutEditor.Reset();
			WrenchToolbar.OnThemeSelected();
			WeType.Scan();
		}

		public override void OnDeselected()
		{
			if (Main.gameMenu && Main.menuMode != 0) {
				LayoutEditor.Cancel(false);
				WePanels.Close();
				WeSplash.Hide();
				return;
			}

			WePersist.OnDeselected();
			CalamitasMenuPersist.OnOurMenuDeselected();
			WePlaylist.Silence();
			LayoutEditor.Cancel(false);
			WePanels.Close();
			WeSplash.Hide();
		}

		public override void Update(bool isOnTitleScreen)
		{
			if (!Main.gameMenu)
				return;

			if (!isOnTitleScreen && !CoolerMenuCompat.OnTitleLike)
				return;

			Tick();
		}

		internal void Tick()
		{
			if (!Main.gameMenu || _ticked)
				return;

			_ticked = true;
			if (!CoolerMenuCompat.MenuBackdropActive)
				return;

			WeMenuHost.TickLogic();
			CalamitasMenuBackgroundStyle.DrewThisFrame = false;
			CalamitasMenuBackgroundStyle.UpdateFade();
			if (!CoolerMenuCompat.OnTitleLike)
				return;

			DieWithASmileSettings.TickScenes();
		}

		public override bool PreDrawLogo(
			SpriteBatch spriteBatch,
			ref Vector2 logoDrawCenter,
			ref float logoRotation,
			ref float logoScale,
			ref Color drawColor)
		{
			Tick();
			if (!CoolerMenuCompat.MenuBackdropActive)
				return false;

			WeLook.StabilizeLogo(ref logoRotation, ref logoScale);
			if (WeSave.Data.Wallpaper == WallpaperKind.Nested && !CalamitasMenuBackgroundStyle.DrewThisFrame)
				CalamitasMenuBackgroundStyle.Draw(spriteBatch);

			WeBackgroundStyle.Draw(spriteBatch);
			WeBackgroundStyle.DrawAtmosphere(spriteBatch);
			if (WeSave.Data.Logo is LogoKind.Custom or LogoKind.Hidden or LogoKind.Borrowed or LogoKind.Preset) {
				if (WeSave.Data.Logo != LogoKind.Hidden && SceneGraph.Visible(SceneGraph.Logo))
					WeLogo.DrawCustom(spriteBatch, 1f, logoRotation, logoScale);
				return false;
			}

			return WeLogo.ShouldDrawVanilla(ref logoDrawCenter, ref logoScale);
		}

		public override void PostDrawLogo(
			SpriteBatch spriteBatch,
			Vector2 logoDrawCenter,
			float logoRotation,
			float logoScale,
			Color drawColor)
		{
			Tick();
			if (!CoolerMenuCompat.MenuBackdropActive) {
				_ticked = false;
				return;
			}

			WeMenuHost.DrawOverlay(spriteBatch);
			_ticked = false;
		}
	}
}
