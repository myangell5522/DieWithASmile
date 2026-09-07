using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using DieWithASmile.Content;
using DieWithASmile.Engine.Chrome;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;

namespace DieWithASmile.Engine.Content
{
	internal static class WeNestedPacks
	{
		internal const string NestCalamitas = "nest-calamitas";
		internal const string NestDontForget = "nest-dontforget";
		internal const string NestComeAlong = "nest-comealong";
		internal const string NestMeadow = "nest-meadow";
		internal const string NestYharim = "nest-yharim";
		internal const string NestWitch = "nest-witch";
		internal const string NestFreedom = "nest-freedom";
		internal const string NestSoul = "nest-soul";

		internal const string Classic = "dwas-classic";
		internal const string Gothic = "dwas-gothic";
		internal const string Orbit = "dwas-orbit";
		internal const string Hands = "dwas-hands";
		internal const string Sticker = "dwas-sticker";

		internal static readonly string[] WallpaperIds =
		{
			NestCalamitas, NestDontForget, NestComeAlong, NestMeadow, NestYharim, NestWitch, NestFreedom
		};

		internal static readonly string[] LogoIds =
		{
			Gothic, Orbit
		};

		internal static bool IsWallpaper(string id) => TryScene(id, out _);

		internal static bool IsLogo(string id) => TryLogo(id, out _);

		internal static bool TryScene(string id, out MenuScene scene)
		{
			scene = MenuScene.Calamitas;
			switch (id) {
				case NestCalamitas:
					scene = MenuScene.Calamitas;
					return true;
				case NestDontForget:
					scene = MenuScene.DontForget;
					return true;
				case NestComeAlong:
					scene = MenuScene.ComeAlong;
					return true;
				case NestMeadow:
					scene = MenuScene.Meadow;
					return true;
				case NestYharim:
					scene = MenuScene.Yharim;
					return true;
				case NestWitch:
					scene = MenuScene.Witch;
					return true;
				case NestFreedom:
					scene = MenuScene.Freedom;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryLogo(string id, out MenuLogo logo)
		{
			logo = MenuLogo.Classic;
			switch (id) {
				case Classic:
					logo = MenuLogo.Classic;
					return true;
				case Gothic:
					logo = MenuLogo.Gothic;
					return true;
				case Orbit:
					logo = MenuLogo.Orbit;
					return true;
				case Hands:
					logo = MenuLogo.Hands;
					return true;
				case Sticker:
					logo = MenuLogo.Sticker;
					return true;
				default:
					return false;
			}
		}

		internal static string SceneTitleKey(string id) => id switch {
			NestDontForget => "SceneDontForget",
			NestComeAlong => "SceneComeAlong",
			NestMeadow => "SceneMeadow",
			NestYharim => "SceneYharim",
			NestWitch => "SceneWitch",
			NestFreedom => "SceneFreedom",
			_ => "SceneCalamitas"
		};

		internal static string LogoTitleKey(string id) => id switch {
			Gothic => "LogoGothic",
			Orbit => "LogoOrbit",
			Hands => "LogoHands",
			Sticker => "LogoSticker",
			_ => "LogoClassic"
		};

		internal static Texture2D WallpaperPreview(string id)
		{
			string path = id switch {
				NestDontForget => "DieWithASmile/Assets/Textures/Menu/DeltaruneHeartsBackground",
				NestComeAlong => "DieWithASmile/Assets/Textures/Menu/ComeAlongBackground",
				NestMeadow => "DieWithASmile/Assets/Textures/Menu/MeadowArt1",
				NestYharim => "DieWithASmile/Assets/Textures/Menu/YharimArt",
				NestWitch => "DieWithASmile/Assets/Textures/Menu/WitchArt",
				NestFreedom => "DieWithASmile/Assets/Textures/Menu/Freedom/Background",
				_ => "DieWithASmile/Assets/Textures/Menu/CalamitasBackground"
			};
			Asset<Texture2D> asset = ModContent.Request<Texture2D>(path);
			Texture2D tex = asset?.Value;
			return tex == null || tex.IsDisposed ? null : tex;
		}

		internal static void ApplyScene(string id)
		{
			if (TryScene(id, out MenuScene scene))
				DieWithASmileSettings.SetLockedScene(scene);
		}

		internal static void ApplyLogo(string id)
		{
			if (TryLogo(id, out MenuLogo logo))
				DieWithASmileSettings.SetLogo(logo);
		}

		internal const int CurrentPackLook = 302;
		internal const string DefaultTrackId = "freedom";

		internal static void ApplyPackLook(bool save = true)
		{
			WeSaveData data = WeSave.Data;
			data.PackedHosted = true;
			data.PackLook = CurrentPackLook;
			data.Wallpaper = WallpaperKind.Nested;
			data.WallpaperId = NestFreedom;
			data.Logo = LogoKind.Preset;
			data.LogoId = WePresetLogos.Watermelon2;
			data.Music = MusicKind.Custom;
			data.LastTrackId = DefaultTrackId;
			data.LoopEnabled = false;
			data.ShuffleEnabled = false;
			data.LoopedTrackId = "";
			data.PlayerWidget = true;
			data.WrenchStyle = (int)WrenchStyle.Dock;
			data.CleanChrome = true;
			data.SplashDismissed = false;
			data.KeepMenuSelected = true;
			ApplyStarterLayout(data);
			if (save)
				WeSave.Save();
			ApplyScene(NestFreedom);
			CalamitasMenuBackgroundStyle.SnapLockedScenes();
		}

		private static void ApplyStarterLayout(WeSaveData data)
		{
			SceneGraph.EnsureRecords(data);
			Place(data, SceneGraph.Logo, 0.185625f, 0.13333334f, 0.76f);
			Place(data, SceneGraph.MenuButtons, 0.17375f, 0.2822222f);
			Place(data, SceneGraph.Wrench, 0.158125f, 0.9122222f);
			Place(data, SceneGraph.Player, 0.1625f, 0.8155556f);
		}

		private static void Place(WeSaveData data, string id, float x, float y, float scale = 1f)
		{
			WeElementRecord element = data.Elements.Find(item => item.Id == id);
			if (element == null) {
				element = new WeElementRecord { Id = id, Visible = true, Scale = 1f };
				data.Elements.Add(element);
			}

			element.Customized = true;
			element.Visible = true;
			element.Scale = scale;
			element.AnchorX = x;
			element.AnchorY = y;
		}

		internal static void EnsureWallpaper()
		{
			WeSaveData data = WeSave.Data;
			if (data.Wallpaper != WallpaperKind.Nested)
				return;
			if (string.IsNullOrEmpty(data.WallpaperId) || !IsWallpaper(data.WallpaperId))
				data.WallpaperId = NestFreedom;
		}

		internal static void EnsureHostedDefaults()
		{
			WeSaveData data = WeSave.Data;
			if (data.PackedHosted && data.PackLook >= CurrentPackLook)
				return;

			WeSettings.ResetLookToPack(wipeFiles: false);
		}

		internal static string MigrateWallpaperId(string id)
		{
			if (id == NestSoul)
				return NestFreedom;
			return id;
		}

		internal static string MigrateLogoId(string id)
		{
			if (id == Hands)
				return WePresetLogos.Watermelon2;
			if (id == Sticker)
				return WePresetLogos.Watermelon1;
			if (id == Classic)
				return "";
			return id;
		}
	}
}
