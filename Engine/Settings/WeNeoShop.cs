using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal enum WeNeoPage
	{
		Hub,
		Mods,
		Browser,
		ModPacks,
		Resources,
		Develop,
		Worlds,
		Logs
	}

	internal static class WeNeoShop
	{
		private const int TileH = 76;
		private const int RowH = 50;
		private static string _info;

		internal static void Draw(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			if (WeNeoMenu.Page != WeNeoPage.Hub)
				DrawBack(spriteBatch, view, ref y, fade);

			switch (WeNeoMenu.Page) {
				case WeNeoPage.Mods:
					DrawMods(spriteBatch, view, ref y, fade);
					break;
				case WeNeoPage.Browser:
					DrawBrowser(spriteBatch, view, ref y, fade);
					break;
				case WeNeoPage.ModPacks:
					DrawModPacks(spriteBatch, view, ref y, fade);
					break;
				case WeNeoPage.Resources:
					DrawResources(spriteBatch, view, ref y, fade);
					break;
				case WeNeoPage.Develop:
					DrawDevelop(spriteBatch, view, ref y, fade);
					break;
				case WeNeoPage.Worlds:
					DrawWorlds(spriteBatch, view, ref y, fade);
					break;
				case WeNeoPage.Logs:
					DrawLogs(spriteBatch, view, ref y, fade);
					break;
				default:
					DrawHub(spriteBatch, view, ref y, fade);
					break;
			}
		}

		internal static void Click(Rectangle view, ref int y, bool left, bool right)
		{
			if (WeNeoMenu.Page != WeNeoPage.Hub) {
				var back = BackBox(view, y);
				y += 36;
				if (left && back.Contains(Main.mouseX, Main.mouseY)) {
					WeNeoMenu.SetPage(WeNeoPage.Hub);
					Tick();
					return;
				}
			}

			switch (WeNeoMenu.Page) {
				case WeNeoPage.Mods:
					ClickMods(view, ref y, left);
					break;
				case WeNeoPage.Browser:
					ClickBrowser(view, ref y, left);
					break;
				case WeNeoPage.ModPacks:
					ClickModPacks(view, ref y, left);
					break;
				case WeNeoPage.Resources:
					ClickResources(view, ref y, left);
					break;
				case WeNeoPage.Develop:
					ClickDevelop(view, ref y, left);
					break;
				case WeNeoPage.Worlds:
					ClickWorlds(view, ref y, left);
					break;
				case WeNeoPage.Logs:
					ClickLogs(view, ref y, left);
					break;
				default:
					ClickHub(view, ref y, left);
					break;
			}
		}

		internal static void DrawSearch(SpriteBatch spriteBatch, Rectangle view, ref int y, string q, float fade)
		{
			if (WeNeoShell.Matches(q, WeText.UI("NeoOpenMods"), WeText.UI("NeoOpenBrowser"), WeText.UI("NeoHubTitle")))
				DrawHub(spriteBatch, view, ref y, fade);
		}

		internal static void ClickSearch(Rectangle view, ref int y, string q, bool left, bool right)
		{
			if (WeNeoShell.Matches(q, WeText.UI("NeoOpenMods"), WeText.UI("NeoOpenBrowser"), WeText.UI("NeoHubTitle")))
				ClickHub(view, ref y, left);
		}

		private static void DrawHub(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("NeoHubTitle"), fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoHubHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 24;
			(WeNeoPage page, string key, string icon)[] tiles =
			{
				(WeNeoPage.Mods, "NeoOpenMods", WeIcons.Setting),
				(WeNeoPage.Develop, "NeoDevelop", WeIcons.Layout),
				(WeNeoPage.Browser, "NeoOpenBrowser", WeIcons.Upload),
				(WeNeoPage.ModPacks, "NeoOpenModPacks", WeIcons.Widget),
				(WeNeoPage.Worlds, "NeoImportWorlds", WeIcons.Wallpaper),
				(WeNeoPage.Resources, "NeoOpenPacks", WeIcons.Logo)
			};
			int cellW = (view.Width - 20) / 2;
			for (int i = 0; i < tiles.Length; i++) {
				int col = i % 2;
				int row = i / 2;
				var hit = new Rectangle(view.X + 4 + col * (cellW + 8), y + row * (TileH + 8), cellW, TileH);
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
				WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
				if (hover)
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 4, hit.Height), WeAccent.Hover * fade);
				Texture2D icon = WeIcons.Get(tiles[i].icon);
				if (icon != null)
					spriteBatch.Draw(icon, new Vector2(hit.X + 28, hit.Center.Y), null, WeAccent.Icon(hover) * fade, 0f, icon.Size() * 0.5f, 20f / Math.Max(icon.Width, icon.Height), SpriteEffects.None, 0f);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(tiles[i].key),
					new Vector2(hit.X + 48, hit.Y + 26), Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.Type));
			}

			y += 3 * (TileH + 8);
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 34), WeText.UI("NeoLogs"), fade);
			y += 42;
		}

		private static void ClickHub(Rectangle view, ref int y, bool left)
		{
			WeNeoShell.SkipHeader(ref y);
			y += 24;
			WeNeoPage[] pages =
			{
				WeNeoPage.Mods, WeNeoPage.Develop, WeNeoPage.Browser,
				WeNeoPage.ModPacks, WeNeoPage.Worlds, WeNeoPage.Resources
			};
			int cellW = (view.Width - 20) / 2;
			if (left) {
				for (int i = 0; i < pages.Length; i++) {
					int col = i % 2;
					int row = i / 2;
					var hit = new Rectangle(view.X + 4 + col * (cellW + 8), y + row * (TileH + 8), cellW, TileH);
					if (!hit.Contains(Main.mouseX, Main.mouseY))
						continue;
					WeNeoMenu.SetPage(pages[i]);
					WeNeoMenu.SetScroll(0f);
					Tick();
					return;
				}
			}

			y += 3 * (TileH + 8);
			var logs = new Rectangle(view.X + 4, y, view.Width - 8, 34);
			y += 42;
			if (left && logs.Contains(Main.mouseX, Main.mouseY)) {
				WeNeoMenu.SetPage(WeNeoPage.Logs);
				WeNeoMenu.SetScroll(0f);
				Tick();
			}
		}

		private static void DrawMods(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			bool live = WeNeoMenu.InGame;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI(live ? "NeoModsInGame" : "NeoModsHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			int bw = (view.Width - 24) / 4;
			string[] keys = { "NeoEnableAll", "NeoDisableAll", "NeoReload", "NeoModsFolder" };
			for (int i = 0; i < 4; i++)
				DrawBtn(spriteBatch, new Rectangle(view.X + 4 + i * (bw + 4), y, bw, 30), WeText.UI(keys[i]), fade);
			y += 38;
			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, mod.Display, mod.Name))
					continue;
				var hit = new Rectangle(view.X + 4, y, view.Width - 8, RowH);
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
				WeDraw.Border(spriteBatch, hit, (mod.Enabled ? WeAccent.Mid : Color.White * 0.12f) * fade);
				if (mod.Icon != null)
					spriteBatch.Draw(mod.Icon, new Rectangle(hit.X + 8, hit.Y + 7, 36, 36), Color.White * fade);
				else
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 7, 36, 36), new Color(40, 44, 52) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, mod.Display,
					new Vector2(hit.X + 52, hit.Y + 8), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(mod.Enabled ? "NeoOn" : "NeoOff"),
					new Vector2(hit.Right - 70, hit.Y + 14), (mod.Enabled ? WeAccent.Light : Color.White * 0.45f) * fade,
					0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				y += RowH + 4;
			}

			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, (view.Width - 16) / 2 - 4, 32), WeText.UI("NeoSavePack"), fade);
			DrawBtn(spriteBatch, new Rectangle(view.X + view.Width / 2 + 4, y, (view.Width - 16) / 2 - 4, 32), WeText.UI("NeoConfigFolder"), fade);
			y += 40;
			if (!string.IsNullOrEmpty(_info)) {
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, _info,
					new Vector2(view.X + 8, y), Color.White * (0.7f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				y += 48;
			}
		}

		private static void ClickMods(Rectangle view, ref int y, bool left)
		{
			y += 22;
			int bw = (view.Width - 24) / 4;
			Rectangle[] tools =
			{
				new(view.X + 4, y, bw, 30),
				new(view.X + 4 + bw + 4, y, bw, 30),
				new(view.X + 4 + 2 * (bw + 4), y, bw, 30),
				new(view.X + 4 + 3 * (bw + 4), y, bw, 30)
			};
			y += 38;
			if (left && !WeNeoMenu.InGame) {
				if (tools[0].Contains(Main.mouseX, Main.mouseY)) {
					WeTml.SetAll(true);
					Tick();
					return;
				}

				if (tools[1].Contains(Main.mouseX, Main.mouseY)) {
					WeTml.SetAll(false);
					Tick();
					return;
				}

				if (tools[2].Contains(Main.mouseX, Main.mouseY)) {
					WeTml.Reload();
					return;
				}
			}

			if (left && tools[3].Contains(Main.mouseX, Main.mouseY)) {
				WeFiles.OpenFolder(WeTml.ModsFolder());
				Tick();
				return;
			}

			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, mod.Display, mod.Name))
					continue;
				var hit = new Rectangle(view.X + 4, y, view.Width - 8, RowH);
				y += RowH + 4;
				if (!left || !hit.Contains(Main.mouseX, Main.mouseY))
					continue;
				if (hit.X + hit.Width - 90 < Main.mouseX && !WeNeoMenu.InGame) {
					WeTml.SetEnabled(mod, !mod.Enabled);
					Tick();
					return;
				}

				_info = string.IsNullOrWhiteSpace(mod.Description) ? mod.Display : mod.Description;
				Tick();
				return;
			}

			var pack = new Rectangle(view.X + 4, y, (view.Width - 16) / 2 - 4, 32);
			var cfg = new Rectangle(view.X + view.Width / 2 + 4, y, (view.Width - 16) / 2 - 4, 32);
			y += 40;
			if (left && pack.Contains(Main.mouseX, Main.mouseY)) {
				WeTml.SavePack();
				Tick();
			}
			else if (left && cfg.Contains(Main.mouseX, Main.mouseY))
				WeFiles.OpenFolder(Path.Combine(Main.SavePath, "ModConfigs"));
			if (!string.IsNullOrEmpty(_info))
				y += 48;
		}

		private static void DrawBrowser(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoBrowserHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoOpenSteam"), fade);
			y += 40;
			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, mod.Display, "workshop", "steam"))
					continue;
				var hit = new Rectangle(view.X + 4, y, view.Width - 8, 36);
				WeDraw.Fill(spriteBatch, hit, new Color(22, 24, 30) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, mod.Display,
					new Vector2(hit.X + 10, hit.Y + 8), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
				y += 40;
			}
		}

		private static void ClickBrowser(Rectangle view, ref int y, bool left)
		{
			y += 22;
			var steam = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			if (left && steam.Contains(Main.mouseX, Main.mouseY)) {
				WeTml.OpenSteamWorkshop();
				Tick();
			}

			foreach (WeLocalMod _ in WeTml.LocalMods())
				y += 40;
		}

		private static void DrawModPacks(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoPacksHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoPacksFolder"), fade);
			y += 40;
			foreach (string pack in WeTml.ModPacks()) {
				WeNeoShell.Cycle(spriteBatch, view, ref y, Path.GetFileNameWithoutExtension(pack), WeText.UI("NeoLoadPack"), fade);
			}
		}

		private static void ClickModPacks(Rectangle view, ref int y, bool left)
		{
			y += 22;
			var folder = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			if (left && folder.Contains(Main.mouseX, Main.mouseY)) {
				WeFiles.OpenFolder(WeTml.ModPacksFolder());
				Tick();
				return;
			}

			foreach (string pack in WeTml.ModPacks()) {
				bool hit = WeNeoShell.HitRow(view, ref y);
				y += WeNeoShell.RowStep;
				if (left && hit && !WeNeoMenu.InGame) {
					WeTml.LoadPack(pack);
					Tick();
					return;
				}
			}
		}

		private static void DrawResources(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoResHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			foreach (WeResPack pack in WeTml.ResourcePacks())
				WeNeoShell.Toggle(spriteBatch, view, ref y, pack.Name, pack.Enabled, fade);
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoApplyPacks"), fade);
			y += 40;
		}

		private static void ClickResources(Rectangle view, ref int y, bool left)
		{
			y += 22;
			List<WeResPack> packs = WeTml.ResourcePacks();
			for (int i = 0; i < packs.Count; i++) {
				bool hit = WeNeoShell.HitRow(view, ref y);
				y += WeNeoShell.RowStep;
				if (left && hit) {
					WeTml.TogglePack(packs[i]);
					Tick();
					return;
				}
			}

			var apply = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			if (left && apply.Contains(Main.mouseX, Main.mouseY)) {
				WeTml.ApplyPacks();
				Tick();
			}
		}

		private static void DrawDevelop(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoDevelopHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoOpenSources"), fade);
			y += 40;
			foreach (string dir in WeTml.Sources())
				WeNeoShell.Cycle(spriteBatch, view, ref y, Path.GetFileName(dir), "", fade);
		}

		private static void ClickDevelop(Rectangle view, ref int y, bool left)
		{
			y += 22;
			var open = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			if (left && open.Contains(Main.mouseX, Main.mouseY)) {
				WeFiles.OpenFolder(WeTml.SourcesFolder());
				Tick();
				return;
			}

			foreach (string dir in WeTml.Sources()) {
				bool hit = WeNeoShell.HitRow(view, ref y);
				y += WeNeoShell.RowStep;
				if (left && hit) {
					WeFiles.OpenFolder(dir);
					Tick();
					return;
				}
			}
		}

		private static void DrawWorlds(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoWorldsHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoImportWorld"), fade);
			y += 40;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoWorldsFolder"), fade);
			y += 40;
		}

		private static void ClickWorlds(Rectangle view, ref int y, bool left)
		{
			y += 22;
			var import = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			var folder = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			if (!left)
				return;
			if (import.Contains(Main.mouseX, Main.mouseY)) {
				WeTml.ImportWorld();
				Tick();
			}
			else if (folder.Contains(Main.mouseX, Main.mouseY))
				WeFiles.OpenFolder(Main.WorldPath);
		}

		private static void DrawLogs(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoOpenLogs"), fade);
			y += 40;
			foreach (string line in WeTml.LogTail()) {
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(line, 90),
					new Vector2(view.X + 8, y), Color.White * (0.75f * fade), 0f, Vector2.Zero, new Vector2(0.62f));
				y += 18;
			}
		}

		private static void ClickLogs(Rectangle view, ref int y, bool left)
		{
			var open = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			y += WeTml.LogTail().Count * 18;
			if (left && open.Contains(Main.mouseX, Main.mouseY))
				WeFiles.OpenFolder(WeTml.LogsFolder());
		}

		private static void DrawBack(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			DrawBtn(spriteBatch, BackBox(view, y), WeText.UI("NeoBack"), fade);
			y += 36;
		}

		private static Rectangle BackBox(Rectangle view, int y) =>
			new(view.X + 4, y, 120, 30);

		private static void DrawBtn(SpriteBatch spriteBatch, Rectangle hit, string text, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			if (hover)
				WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 3, hit.Height), WeAccent.Hover * fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * WeNeoShell.TypeSmall;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
		}

		private static void Tick() => SoundEngine.PlaySound(SoundID.MenuTick);

		private static string Trim(string s, int n) =>
			string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s[..n] + "…";
	}

	internal sealed class WeLocalMod
	{
		internal object Raw;
		internal string Name = "";
		internal string Display = "";
		internal string Description = "";
		internal bool Enabled;
		internal Texture2D Icon;
	}

	internal sealed class WeResPack
	{
		internal object Raw;
		internal string Name = "";
		internal bool Enabled;
	}

	internal static class WeTml
	{
		internal static List<WeLocalMod> LocalMods()
		{
			var list = new List<WeLocalMod>();
			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
				if (type == null)
					return FromLoaded(list);
				MethodInfo find = type.GetMethod("FindMods", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (find == null)
					return FromLoaded(list);
				object[] args = find.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)).ToArray();
				object result = find.Invoke(null, args);
				if (result is IEnumerable en) {
					foreach (object raw in en) {
						var mod = ReadLocal(raw);
						if (mod != null)
							list.Add(mod);
					}
				}
			}
			catch {
				return FromLoaded(list);
			}

			return list.Count > 0 ? list : FromLoaded(list);
		}

		private static List<WeLocalMod> FromLoaded(List<WeLocalMod> list)
		{
			try {
				foreach (Mod mod in ModLoader.Mods) {
					if (mod.Name == "ModLoader")
						continue;
					list.Add(new WeLocalMod {
						Name = mod.Name,
						Display = string.IsNullOrEmpty(mod.DisplayName) ? mod.Name : mod.DisplayName,
						Enabled = true,
						Description = Str(mod, "Description") ?? "",
						Icon = LogoOf(mod)
					});
				}
			}
			catch {
			}

			return list;
		}

		private static WeLocalMod ReadLocal(object raw)
		{
			if (raw == null)
				return null;
			var mod = new WeLocalMod { Raw = raw };
			mod.Enabled = BoolOf(raw, "Enabled");
			object file = Prop(raw, "modFile") ?? Prop(raw, "ModFile") ?? Prop(raw, "File");
			mod.Name = Str(file, "Name") ?? Str(raw, "Name") ?? "";
			object props = Prop(raw, "properties") ?? Prop(raw, "Properties");
			mod.Display = Str(props, "displayName") ?? Str(props, "DisplayName") ?? mod.Name;
			mod.Description = Str(props, "description") ?? Str(props, "Description") ?? "";
			try {
				Mod loaded = ModLoader.Mods.FirstOrDefault(m => m.Name == mod.Name);
				mod.Icon = LogoOf(loaded);
			}
			catch {
			}

			return string.IsNullOrEmpty(mod.Name) ? null : mod;
		}

		private static Texture2D LogoOf(Mod mod)
		{
			if (mod == null)
				return null;
			try {
				object logo = Prop(mod, "Logo") ?? WeNeoFld.Call(mod, "GetLogo");
				return logo as Texture2D ?? logo?.GetType().GetProperty("Value")?.GetValue(logo) as Texture2D;
			}
			catch {
				return null;
			}
		}

		internal static void SetEnabled(WeLocalMod mod, bool on)
		{
			try {
				SetBool(mod.Raw, "Enabled", on);
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
				type?.GetMethod("SaveEnabledMods", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
			}
			catch {
			}
		}

		internal static void SetAll(bool on)
		{
			foreach (WeLocalMod mod in LocalMods())
				SetEnabled(mod, on);
		}

		internal static void Reload()
		{
			try {
				typeof(ModLoader).GetMethod("Reload", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
			}
			catch {
			}
		}

		internal static string ModsFolder()
		{
			object path = WeNeoFld.Get(typeof(ModLoader), "ModPath");
			return path as string ?? Path.Combine(Main.SavePath, "Mods");
		}

		internal static string ModPacksFolder()
		{
			Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
			object p = type == null ? null : WeNeoFld.Get(type, "ModPacksDirectory") ?? WeNeoFld.Get(type, "modPacksDirectory");
			return p as string ?? Path.Combine(ModsFolder(), "ModPacks");
		}

		internal static IEnumerable<string> ModPacks()
		{
			string folder = ModPacksFolder();
			if (!Directory.Exists(folder))
				yield break;
			foreach (string file in Directory.GetFiles(folder, "*.json"))
				yield return file;
		}

		internal static void SavePack()
		{
			try {
				string folder = ModPacksFolder();
				Directory.CreateDirectory(folder);
				var names = LocalMods().Where(m => m.Enabled).Select(m => m.Name);
				File.WriteAllText(Path.Combine(folder, "overlay-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".json"),
					"{\"mods\":[\"" + string.Join("\",\"", names) + "\"]}");
			}
			catch {
			}
		}

		internal static void LoadPack(string path)
		{
			try {
				string json = File.ReadAllText(path);
				var want = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string name in LocalMods().Select(m => m.Name)) {
					if (json.Contains("\"" + name + "\"", StringComparison.OrdinalIgnoreCase))
						want.Add(name);
				}

				foreach (WeLocalMod mod in LocalMods())
					SetEnabled(mod, want.Contains(mod.Name));
			}
			catch {
			}
		}

		internal static List<WeResPack> ResourcePacks()
		{
			var list = new List<WeResPack>();
			try {
				object ctrl = typeof(Main).GetProperty("AssetSourceController")?.GetValue(Main.instance);
				object packs = ctrl?.GetType().GetProperty("ActiveResourcePackList")?.GetValue(ctrl)
				               ?? ctrl?.GetType().GetProperty("AllPacks")?.GetValue(ctrl);
				object all = packs?.GetType().GetProperty("AllPacks")?.GetValue(packs) ?? packs;
				if (all is IEnumerable en) {
					foreach (object p in en) {
						list.Add(new WeResPack {
							Raw = p,
							Name = Str(p, "Name") ?? Str(p, "FileName") ?? "pack",
							Enabled = BoolOf(p, "IsEnabled") || BoolOf(p, "Enabled")
						});
					}
				}
			}
			catch {
			}

			return list;
		}

		internal static void TogglePack(WeResPack pack)
		{
			try {
				SetBool(pack.Raw, "IsEnabled", !pack.Enabled);
				SetBool(pack.Raw, "Enabled", !pack.Enabled);
			}
			catch {
			}
		}

		internal static void ApplyPacks()
		{
			try {
				object ctrl = typeof(Main).GetProperty("AssetSourceController")?.GetValue(Main.instance);
				ctrl?.GetType().GetMethod("Refresh")?.Invoke(ctrl, null);
				ctrl?.GetType().GetMethod("UseResourcePacks")?.Invoke(ctrl, null);
			}
			catch {
			}
		}

		internal static string SourcesFolder()
		{
			object p = WeNeoFld.Get(typeof(ModLoader), "ModSourcePath") ?? WeNeoFld.Get(typeof(ModLoader), "ModSourcesPath");
			return p as string ?? Path.Combine(Main.SavePath, "ModSources");
		}

		internal static IEnumerable<string> Sources()
		{
			string folder = SourcesFolder();
			if (!Directory.Exists(folder))
				yield break;
			foreach (string dir in Directory.GetDirectories(folder))
				yield return dir;
		}

		internal static void ImportWorld()
		{
			if (!WeFiles.TryPickWorld(out string path))
				return;
			try {
				string dest = WeFiles.UniquePath(Main.WorldPath, Path.GetFileName(path));
				File.Copy(path, dest, overwrite: false);
			}
			catch {
			}
		}

		internal static void OpenSteamWorkshop()
		{
			try {
				WeOs.Reveal("https://steamcommunity.com/app/1281930/workshop/");
			}
			catch {
			}
		}

		internal static string LogsFolder()
		{
			Type log = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Logging");
			object dir = log == null ? null : WeNeoFld.Get(log, "LogDir") ?? WeNeoFld.Get(log, "LogPath");
			if (dir is string s && Directory.Exists(s))
				return s;
			string a = Path.Combine(Main.SavePath, "Logs");
			if (Directory.Exists(a))
				return a;
			return Path.Combine(Main.SavePath, "tModLoader-Logs");
		}

		internal static List<string> LogTail()
		{
			var lines = new List<string>();
			try {
				string folder = LogsFolder();
				string file = Path.Combine(folder, "client.log");
				if (!File.Exists(file)) {
					string[] found = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.log") : Array.Empty<string>();
					file = found.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
				}

				if (string.IsNullOrEmpty(file) || !File.Exists(file))
					return lines;
				using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var sr = new StreamReader(fs);
				var all = new List<string>();
				while (!sr.EndOfStream)
					all.Add(sr.ReadLine());
				int start = Math.Max(0, all.Count - 24);
				for (int i = start; i < all.Count; i++)
					lines.Add(all[i] ?? "");
			}
			catch {
			}

			return lines;
		}

		private static object Prop(object o, string name)
		{
			if (o == null)
				return null;
			return o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o)
			       ?? o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o);
		}

		private static string Str(object o, string name) => Prop(o, name)?.ToString();

		private static bool BoolOf(object o, string name) => Prop(o, name) is bool b && b;

		private static void SetBool(object o, string name, bool value)
		{
			if (o == null)
				return;
			PropertyInfo p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (p != null && p.CanWrite)
				p.SetValue(o, value);
			FieldInfo f = o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			f?.SetValue(o, value);
		}
	}
}
