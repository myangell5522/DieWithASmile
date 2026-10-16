using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace DieWithASmile.Engine.UI
{
	internal static class WeIcons
	{
		internal const string Setting = "Setting";
		internal const string MoveLogo = "MoveLogo";
		internal const string Wallpaper = "ChangeWallpaper";
		internal const string Music = "ChangeMusic";
		internal const string Widget = "Widget";
		internal const string Logo = "ChangeLogo";
		internal const string Layout = "MoveElement";
		internal const string Client = "ClientSetting";
		internal const string Hide = "HideVersion";
		internal const string Play = "Play";
		internal const string Pause = "Pause";
		internal const string Prev = "Previous";
		internal const string Next = "Next";
		internal const string Playlist = "Playlist";
		internal const string Upload = "Upload";
		internal const string Shuffle = "Shuffle";
		internal const string Loop = "Loop";
		internal const string Trash = "Trash";
		internal const string Show = "Show";
		internal const string Lock = "Lock";
		internal const string Unlock = "Unlock";
		internal const string Folder = "Folder";
		internal const string Plus = "Plus";
		internal const string Close = "Close";
		internal const string Check = "Check";
		internal const string Reset = "Reset";
		internal const string Palette = "Palette";
		internal const string Layers = "Layers";
		internal const string Puzzle = "Puzzle";
		internal const string Star = "Star";
		internal const string Clock = "Clock";
		internal const string Moon = "Moon";
		internal const string Sun = "Sun";
		internal const string Quote = "Quote";
		internal const string Speaker = "Speaker";
		internal const string Mute = "Mute";
		internal const string Font = "Font";
		internal const string ChevronLeft = "ChevronLeft";
		internal const string ChevronRight = "ChevronRight";
		internal const string Sparkle = "Sparkle";
		internal const string Sidebar = "Sidebar";
		internal const string ToggleOn = "ToggleOn";
		internal const string ToggleOff = "ToggleOff";
		internal const string Image = "Image";

		private static readonly string[] Names =
		{
			Setting, MoveLogo, Wallpaper, Music, Widget, Logo, Layout, Client, Hide,
			Play, Pause, Prev, Next, Playlist, Upload, Shuffle, Loop, Trash,
			Show, Lock, Unlock, Folder, Plus, Close, Check, Reset, Palette, Layers, Puzzle, Star,
			Clock, Moon, Sun, Quote, Speaker, Mute, Font, ChevronLeft, ChevronRight, Sparkle, Sidebar,
			ToggleOn, ToggleOff, Image
		};

		private static readonly Dictionary<string, Asset<Texture2D>> Assets = new();

		internal static void Load()
		{
			Assets.Clear();
			foreach (string name in Names)
				Request(name);
		}

		private static void Request(string name)
		{
			string path = "DieWithASmile/Assets/Textures/UI/Icons/" + name;
			// A missing icon must not throw during Load; callers already fall back to letters.
			if (!ModContent.HasAsset(path))
				return;
			Assets[name] = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
		}

		internal static Texture2D Get(string name)
		{
			if (string.IsNullOrEmpty(name) || !Assets.TryGetValue(name, out Asset<Texture2D> asset))
				return null;
			Texture2D tex = asset?.Value;
			return tex == null || tex.IsDisposed ? null : tex;
		}
	}
}
