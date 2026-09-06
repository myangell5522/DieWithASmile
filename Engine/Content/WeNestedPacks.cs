using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using DieWithASmile.Content;
using DieWithASmile.Engine.Core;

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
		internal const string NestSoul = "nest-soul";

		internal const string Classic = "dwas-classic";
		internal const string Gothic = "dwas-gothic";
		internal const string Orbit = "dwas-orbit";
		internal const string Hands = "dwas-hands";
		internal const string Sticker = "dwas-sticker";

		internal static readonly string[] WallpaperIds =
		{
			NestCalamitas, NestDontForget, NestComeAlong, NestMeadow, NestYharim, NestWitch, NestSoul
		};

		internal static readonly string[] LogoIds =
		{
			Classic, Gothic, Orbit, Hands, Sticker
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
				case NestSoul:
					scene = MenuScene.Soul;
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
			NestSoul => "SceneSoul",
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
				NestSoul => "DieWithASmile/Assets/Textures/Menu/SoulOfTheUniverse",
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

		internal static void EnsureHostedDefaults()
		{
			WeSaveData data = WeSave.Data;
			if (data.PackedHosted)
				return;

			data.PackedHosted = true;
			data.Wallpaper = WallpaperKind.Nested;
			data.WallpaperId = NestCalamitas;
			data.Logo = LogoKind.Preset;
			data.LogoId = Classic;
			data.Music = MusicKind.Custom;
			data.PlayerWidget = true;
			WeSave.Save();
			ApplyScene(NestCalamitas);
			ApplyLogo(Classic);
		}
	}
}
