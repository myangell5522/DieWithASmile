using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Chrome;
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
		private const int CardH = 80;
		private const int IconPx = 64;
		private static string _info;
		private static string _infoTitle;
		private static string _selected;
		private static string _clickName;
		private static int _clickTick;
		private static int _packDrag = -1;
		private static int _filter;

		internal static void Draw(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			int origin = y + (int)WeNeoMenu.Scroll;
			int cy = origin;
			if (WeNeoMenu.Page != WeNeoPage.Hub)
				DrawBack(spriteBatch, view, ref cy, fade);

			if (WeNeoMenu.Page != WeNeoPage.Hub) {
				switch (WeNeoMenu.Page) {
					case WeNeoPage.Mods:
						DrawModsChrome(spriteBatch, view, ref cy, fade);
						y = DrawClippedList(spriteBatch, view, origin, cy, fade, DrawModsList);
						break;
					case WeNeoPage.Browser:
						DrawBrowserChrome(spriteBatch, view, ref cy, fade);
						y = DrawClippedList(spriteBatch, view, origin, cy, fade, DrawBrowserList);
						break;
					case WeNeoPage.ModPacks:
						DrawModPacksChrome(spriteBatch, view, ref cy, fade);
						y = DrawClippedList(spriteBatch, view, origin, cy, fade, DrawModPacksList);
						break;
					case WeNeoPage.Resources:
						DrawResourcesChrome(spriteBatch, view, ref cy, fade);
						y = DrawClippedList(spriteBatch, view, origin, cy, fade, DrawResourcesList);
						break;
					case WeNeoPage.Develop:
						DrawDevelopChrome(spriteBatch, view, ref cy, fade);
						y = DrawClippedList(spriteBatch, view, origin, cy, fade, DrawDevelopList);
						break;
					case WeNeoPage.Worlds:
						DrawWorlds(spriteBatch, view, ref cy, fade);
						y = cy - (int)WeNeoMenu.Scroll;
						WeNeoMenu.SetChrome(cy - origin);
						break;
					case WeNeoPage.Logs:
						DrawLogs(spriteBatch, view, ref cy, fade);
						y = cy - (int)WeNeoMenu.Scroll;
						WeNeoMenu.SetChrome(Math.Min(cy - origin, 40));
						break;
					default:
						DrawHub(spriteBatch, view, ref y, fade);
						WeNeoMenu.SetChrome(0);
						break;
				}

				return;
			}

			DrawHub(spriteBatch, view, ref y, fade);
			WeNeoMenu.SetChrome(0);
		}

		private delegate void WeListDraw(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade);

		private static int DrawClippedList(SpriteBatch spriteBatch, Rectangle view, int origin, int chromeBottom, float fade, WeListDraw drawList)
		{
			WeNeoMenu.SetChrome(chromeBottom - origin);
			var list = new Rectangle(view.X, chromeBottom, Math.Max(1, view.Width - 12), Math.Max(1, view.Bottom - chromeBottom));
			int end = chromeBottom - (int)WeNeoMenu.Scroll;
			WeDraw.WithClip(spriteBatch, list, () => {
				int yy = chromeBottom - (int)WeNeoMenu.Scroll;
				drawList(spriteBatch, view, ref yy, fade);
				end = yy;
			});
			return end;
		}

		internal static void Click(Rectangle view, ref int y, bool left, bool right)
		{
			int origin = y + (int)WeNeoMenu.Scroll;
			if (WeNeoMenu.Page != WeNeoPage.Hub) {
				var back = BackBox(view, origin);
				if (left && back.Contains(Main.mouseX, Main.mouseY)) {
					WeNeoMenu.SetPage(WeNeoPage.Hub);
					Tick();
					return;
				}

				y = origin + 36;
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
					new Vector2(hit.X + 48, hit.Y + 16), Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.Type));
				string count = HubCount(tiles[i].page);
				if (!string.IsNullOrEmpty(count))
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, FontAssets.MouseText.Value, count,
						new Vector2(hit.X + 48, hit.Y + 42), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
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

		private static string HubCount(WeNeoPage page)
		{
			try {
				switch (page) {
					case WeNeoPage.Mods: {
						List<WeLocalMod> mods = WeTml.LocalMods();
						int on = 0;
						for (int i = 0; i < mods.Count; i++) {
							if (mods[i].Enabled)
								on++;
						}

						return string.Format(WeText.UI("NeoEnabledCount"), on, mods.Count);
					}
					case WeNeoPage.Resources: {
						List<WeResPack> packs = WeTml.ResourcePacks();
						int on = 0;
						for (int i = 0; i < packs.Count; i++) {
							if (packs[i].Enabled)
								on++;
						}

						return on + " / " + packs.Count;
					}
					case WeNeoPage.ModPacks:
						return WeTml.ModPackItems().Count.ToString();
					case WeNeoPage.Develop:
						return WeTml.DevItems().Count.ToString();
					case WeNeoPage.Browser: {
						int n = 0;
						foreach (WeLocalMod mod in WeTml.LocalMods()) {
							if (mod.Workshop)
								n++;
						}

						return n.ToString();
					}
				}
			}
			catch {
			}

			return "";
		}

		private static ModsBar LayoutMods(Rectangle view, ref int y)
		{
			var bar = new ModsBar();
			y += 22;
			bar.Dirty = WeTml.ReloadNeeded;
			if (bar.Dirty)
				y += 28;
			int bw = (view.Width - 24) / 4;
			bar.Tools = new Rectangle[6];
			for (int i = 0; i < 4; i++)
				bar.Tools[i] = new Rectangle(view.X + 4 + i * (bw + 4), y, bw, 30);
			y += 38;
			int hw = (view.Width - 16) / 2;
			bar.Tools[4] = new Rectangle(view.X + 4, y, hw - 4, 30);
			bar.Tools[5] = new Rectangle(view.X + view.Width / 2 + 4, y, hw - 4, 30);
			y += 38;
			int chip = Math.Max(60, Math.Min(88, (view.Width - 170) / 4));
			bar.Filters = new Rectangle[4];
			for (int i = 0; i < 4; i++)
				bar.Filters[i] = new Rectangle(view.X + 4 + i * (chip + 4), y, chip, 26);
			y += 34;
			if (!string.IsNullOrEmpty(_info)) {
				bar.Info = new Rectangle(view.X + 4, y, view.Width - 8, 72);
				y += 80;
			}

			return bar;
		}

		private static void DrawModsChrome(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			bool live = WeNeoMenu.InGame;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI(live ? "NeoModsInGame" : "NeoModsHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			ModsBar bar = LayoutMods(view, ref y);
			if (bar.Dirty) {
				var banner = new Rectangle(view.X + 4, bar.Tools[0].Y - 28, view.Width - 8, 24);
				WeDraw.Fill(spriteBatch, banner, new Color(70, 42, 18) * fade);
				WeDraw.Border(spriteBatch, banner, new Color(220, 160, 70) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoReloadNeed"),
					new Vector2(banner.X + 8, banner.Y + 4), new Color(255, 220, 160) * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			}

			string[] tools = { "NeoEnableAll", "NeoDisableAll", "NeoReload", "NeoModsFolder", "NeoSavePack", "NeoConfigFolder" };
			for (int i = 0; i < 6; i++)
				DrawBtn(spriteBatch, bar.Tools[i], WeText.UI(tools[i]), fade);

			string[] filters = { "NeoFilterAll", "NeoFilterOn", "NeoFilterOff", "NeoFilterClient" };
			for (int i = 0; i < 4; i++)
				DrawChip(spriteBatch, bar.Filters[i], WeText.UI(filters[i]), _filter == i, fade);

			List<WeLocalMod> all = WeTml.LocalMods();
			int on = 0;
			for (int i = 0; i < all.Count; i++) {
				if (all[i].Enabled)
					on++;
			}

			string count = string.Format(WeText.UI("NeoEnabledCount"), on, all.Count);
			Vector2 cs = FontAssets.MouseText.Value.MeasureString(count) * WeNeoShell.TypeSmall;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, count,
				new Vector2(view.Right - 22 - cs.X, bar.Filters[0].Y + 4), Color.White * (0.6f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));

			if (bar.Info.Width > 0) {
				WeDraw.Fill(spriteBatch, bar.Info, new Color(18, 20, 26) * fade);
				WeDraw.Border(spriteBatch, bar.Info, WeAccent.Mid * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(_infoTitle, 42),
					new Vector2(bar.Info.X + 10, bar.Info.Y + 6), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Wrap(_info, 54),
					new Vector2(bar.Info.X + 10, bar.Info.Y + 24), Color.White * (0.82f * fade), 0f, Vector2.Zero, new Vector2(0.62f));
			}
		}

		private static void DrawModsList(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			List<WeLocalMod> shown = ShownMods();
			if (shown.Count == 0) {
				DrawEmpty(spriteBatch, view, ref y, WeText.UI("NeoEmptyMods"), fade);
				return;
			}

			foreach (WeLocalMod mod in shown)
				DrawModCard(spriteBatch, view, ref y, mod, fade);
		}

		private static void ClickMods(Rectangle view, ref int y, bool left)
		{
			ModsBar bar = LayoutMods(view, ref y);
			int chromeBottom = y;
			if (left && Main.mouseY < chromeBottom) {
				if (!WeNeoMenu.InGame) {
					if (bar.Tools[0].Contains(Main.mouseX, Main.mouseY)) {
						WeTml.SetAll(true);
						Tick();
						return;
					}

					if (bar.Tools[1].Contains(Main.mouseX, Main.mouseY)) {
						WeTml.SetAll(false);
						Tick();
						return;
					}

					if (bar.Tools[2].Contains(Main.mouseX, Main.mouseY)) {
						WeTml.Reload();
						return;
					}

					if (bar.Tools[4].Contains(Main.mouseX, Main.mouseY)) {
						WeTml.SavePack();
						Tick();
						return;
					}
				}

				if (bar.Tools[3].Contains(Main.mouseX, Main.mouseY)) {
					WeFiles.OpenFolder(WeTml.ModsFolder());
					Tick();
					return;
				}

				if (bar.Tools[5].Contains(Main.mouseX, Main.mouseY)) {
					WeFiles.OpenFolder(WeTml.ConfigsFolder());
					Tick();
					return;
				}

				for (int i = 0; i < 4; i++) {
					if (!bar.Filters[i].Contains(Main.mouseX, Main.mouseY))
						continue;
					_filter = i;
					Tick();
					return;
				}

				return;
			}

			int ly = chromeBottom - (int)WeNeoMenu.Scroll;
			List<WeLocalMod> shown = ShownMods();
			if (shown.Count == 0) {
				SkipEmpty(view, ref ly);
				return;
			}

			foreach (WeLocalMod mod in shown) {
				ModCard card = NextModCard(view, ref ly);
				if (!left || !card.Hit.Contains(Main.mouseX, Main.mouseY))
					continue;
				if (card.Pill.Contains(Main.mouseX, Main.mouseY)) {
					ToggleMod(mod);
					return;
				}

				if (card.Gear.Contains(Main.mouseX, Main.mouseY)) {
					WeFiles.OpenFolder(WeTml.ConfigsFolder());
					Tick();
					return;
				}

				if (card.Ask.Contains(Main.mouseX, Main.mouseY)) {
					if (_infoTitle == mod.Display && !string.IsNullOrEmpty(_info)) {
						_info = null;
						_infoTitle = null;
					}
					else {
						_infoTitle = mod.Display;
						_info = string.IsNullOrWhiteSpace(mod.Description) ? mod.Display : mod.Description;
					}

					Tick();
					return;
				}

				int now = (int)Main.GameUpdateCount;
				bool dbl = _clickName == mod.Name && now - _clickTick < 24;
				_clickName = mod.Name;
				_clickTick = now;
				_selected = mod.Name;
				if (dbl) {
					ToggleMod(mod);
					return;
				}

				Tick();
				return;
			}
		}

		private static void ToggleMod(WeLocalMod mod)
		{
			if (WeNeoMenu.InGame)
				return;
			WeTml.SetEnabled(mod, !mod.Enabled);
			Tick();
		}

		private static List<WeLocalMod> ShownMods()
		{
			var list = new List<WeLocalMod>();
			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, mod.Display, mod.Name, mod.Author, mod.Version))
					continue;
				if (_filter == 1 && !mod.Enabled)
					continue;
				if (_filter == 2 && mod.Enabled)
					continue;
				if (_filter == 3 && !IsClient(mod.Side))
					continue;
				list.Add(mod);
			}

			list.Sort((a, b) => string.Compare(a.Display, b.Display, StringComparison.OrdinalIgnoreCase));
			return list;
		}

		private static void DrawModCard(SpriteBatch spriteBatch, Rectangle view, ref int y, WeLocalMod mod, float fade)
		{
			ModCard card = NextModCard(view, ref y);
			bool hover = card.Hit.Contains(Main.mouseX, Main.mouseY) || _selected == mod.Name;
			Color edge = mod.Edge.A == 0 ? WeAccent.Mid : mod.Edge;
			Texture2D art = PlayModArt(mod);
			WeDraw.Fill(spriteBatch, card.Hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
			if (art != null)
				WeDraw.DrawCover(spriteBatch, art, card.Hit, Color.White * (0.22f * fade));
			else
				Shimmer(spriteBatch, card.Hit, edge, fade);
			WeDraw.Fill(spriteBatch, card.Hit, new Color(12, 14, 18) * (0.35f * fade));
			if (hover)
				WeDraw.Fill(spriteBatch, new Rectangle(card.Hit.X, card.Hit.Y, 4, card.Hit.Height), edge * fade);
			WeDraw.Border(spriteBatch, card.Hit, (hover ? edge : edge * 0.7f) * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(card.Hit.X, card.Hit.Y, 3, card.Hit.Height), edge * fade);

			var icon = new Rectangle(card.Hit.X + 8, card.Hit.Y + 8, IconPx, IconPx);
			DrawModIcon(spriteBatch, icon, mod, fade);

			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, Trim(mod.Display, 34),
				new Vector2(card.Hit.X + 80, card.Hit.Y + 8), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.8f));
			string meta = JoinMeta(mod);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, Trim(meta, 48),
				new Vector2(card.Hit.X + 80, card.Hit.Y + 30), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));

			DrawBtn(spriteBatch, card.Ask, "?", fade);
			DrawIconBtn(spriteBatch, card.Gear, WeIcons.Get(WeIcons.Setting), fade);
			DrawPill(spriteBatch, card.Pill, WeText.UI(mod.Enabled ? "NeoOn" : "NeoOff"), mod.Enabled, fade);
			if (mod.HasConfig)
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoConfigMark"),
					new Vector2(card.Hit.X + 80, card.Hit.Y + 52), WeAccent.Light * (0.8f * fade), 0f, Vector2.Zero, new Vector2(0.58f));
		}

		private static void Shimmer(SpriteBatch spriteBatch, Rectangle hit, Color edge, float fade)
		{
			float wave = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f);
			WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, hit.Width, hit.Height), edge * (0.08f * wave * fade));
		}

		private static Texture2D PlayModArt(WeLocalMod mod)
		{
			if (mod.Gif == null)
				return mod.Icon != null && !mod.Icon.IsDisposed ? mod.Icon : null;
			bool prev = WeAnim.CanUpload;
			WeAnim.CanUpload = true;
			try {
				mod.Gif.Present();
			}
			finally {
				WeAnim.CanUpload = prev;
			}

			Texture2D cur = mod.Gif.Current();
			if (cur != null && !cur.IsDisposed)
				return cur;
			return mod.Icon != null && !mod.Icon.IsDisposed ? mod.Icon : null;
		}

		private static ModCard NextModCard(Rectangle view, ref int y)
		{
			var hit = new Rectangle(view.X + 4, y, view.Width - 8, CardH);
			int right = hit.Right - 10;
			var pill = new Rectangle(right - 58, hit.Y + 28, 58, 24);
			var ask = new Rectangle(pill.X - 30, hit.Y + 28, 24, 24);
			var gear = new Rectangle(ask.X - 30, hit.Y + 28, 24, 24);
			y += CardH + 6;
			return new ModCard { Hit = hit, Pill = pill, Ask = ask, Gear = gear };
		}

		private static void DrawModIcon(SpriteBatch spriteBatch, Rectangle dest, WeLocalMod mod, float fade)
		{
			if (mod.Name == "DieWithASmile" && WeModListLook.DrawIcon(spriteBatch, dest, fade))
				return;

			Texture2D tex = PlayModArt(mod);
			if (tex != null) {
				WeDraw.DrawCover(spriteBatch, tex, dest, Color.White * fade);
				return;
			}

			WeDraw.Fill(spriteBatch, dest, new Color(40, 44, 52) * fade);
		}

		private static string JoinMeta(WeLocalMod mod)
		{
			string side = SideLabel(mod.Side);
			var bits = new List<string>();
			if (!string.IsNullOrEmpty(mod.Version))
				bits.Add("v" + mod.Version);
			if (!string.IsNullOrEmpty(mod.Author))
				bits.Add(mod.Author);
			if (!string.IsNullOrEmpty(side))
				bits.Add(side);
			return bits.Count == 0 ? mod.Name : string.Join(" · ", bits);
		}

		private static string SideLabel(string side)
		{
			if (string.IsNullOrEmpty(side))
				return "";
			if (IsClient(side) && side.IndexOf("server", StringComparison.OrdinalIgnoreCase) < 0)
				return WeText.UI("NeoSideClient");
			if (side.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0)
				return WeText.UI("NeoSideServer");
			if (side.IndexOf("both", StringComparison.OrdinalIgnoreCase) >= 0)
				return WeText.UI("NeoSideBoth");
			return side;
		}

		private static bool IsClient(string side) =>
			!string.IsNullOrEmpty(side) && side.IndexOf("client", StringComparison.OrdinalIgnoreCase) >= 0;

		private static void DrawBrowserChrome(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoBrowserHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoOpenSteam"), fade);
			y += 40;
		}

		private static void DrawBrowserList(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			int n = 0;
			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (!mod.Workshop || !WeNeoShell.Matches(WeNeoMenu.Search, mod.Display, mod.Name, "workshop", "steam"))
					continue;
				n++;
				var hit = new Rectangle(view.X + 4, y, view.Width - 8, 56);
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
				WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
				var icon = new Rectangle(hit.X + 8, hit.Y + 8, 40, 40);
				DrawModIcon(spriteBatch, icon, mod, fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(mod.Display, 28),
					new Vector2(hit.X + 56, hit.Y + 8), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoInstalled"),
					new Vector2(hit.X + 56, hit.Y + 30), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				if (!string.IsNullOrEmpty(mod.Steam))
					DrawBtn(spriteBatch, new Rectangle(hit.Right - 118, hit.Y + 14, 108, 28), WeText.UI("NeoOpenPage"), fade);
				y += 62;
			}

			if (n == 0)
				DrawEmpty(spriteBatch, view, ref y, WeText.UI("NeoEmptyBrowser"), fade);
		}

		private static void ClickBrowser(Rectangle view, ref int y, bool left)
		{
			y += 22;
			var steam = new Rectangle(view.X + 4, y, view.Width - 8, 32);
			y += 40;
			if (left && steam.Contains(Main.mouseX, Main.mouseY)) {
				WeTml.OpenSteamWorkshop();
				Tick();
				return;
			}

			int ly = y - (int)WeNeoMenu.Scroll;
			int n = 0;
			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (!mod.Workshop || !WeNeoShell.Matches(WeNeoMenu.Search, mod.Display, mod.Name, "workshop", "steam"))
					continue;
				n++;
				var hit = new Rectangle(view.X + 4, ly, view.Width - 8, 56);
				var page = new Rectangle(hit.Right - 118, hit.Y + 14, 108, 28);
				ly += 62;
				if (left && page.Contains(Main.mouseX, Main.mouseY) && !string.IsNullOrEmpty(mod.Steam)) {
					WeTml.OpenSteamPage(mod.Steam);
					Tick();
					return;
				}
			}

			if (n == 0)
				SkipEmpty(view, ref ly);
		}

		private static void DrawModPacksChrome(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoPacksHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoPacksFolder"), fade);
			y += 40;
		}

		private static void DrawModPacksList(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			List<WePackFile> packs = WeTml.ModPackItems();
			if (packs.Count == 0) {
				DrawEmpty(spriteBatch, view, ref y, WeText.UI("NeoEmptyPacks"), fade);
				return;
			}

			foreach (WePackFile pack in packs) {
				var hit = new Rectangle(view.X + 4, y, view.Width - 8, 56);
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
				WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(pack.Name, 28),
					new Vector2(hit.X + 12, hit.Y + 8), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, string.Format(WeText.UI("NeoPackMods"), pack.Count),
					new Vector2(hit.X + 12, hit.Y + 30), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				int bw = 72;
				DrawBtn(spriteBatch, new Rectangle(hit.Right - bw * 2 - 16, hit.Y + 14, bw, 28), WeText.UI("NeoLoadPack"), fade);
				DrawBtn(spriteBatch, new Rectangle(hit.Right - bw - 8, hit.Y + 14, bw, 28), WeText.UI("NeoDeletePack"), fade);
				y += 62;
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

			int ly = y - (int)WeNeoMenu.Scroll;
			List<WePackFile> packs = WeTml.ModPackItems();
			if (packs.Count == 0) {
				SkipEmpty(view, ref ly);
				return;
			}

			foreach (WePackFile pack in packs) {
				var hit = new Rectangle(view.X + 4, ly, view.Width - 8, 56);
				int bw = 72;
				var load = new Rectangle(hit.Right - bw * 2 - 16, hit.Y + 14, bw, 28);
				var del = new Rectangle(hit.Right - bw - 8, hit.Y + 14, bw, 28);
				ly += 62;
				if (!left)
					continue;
				if (load.Contains(Main.mouseX, Main.mouseY) && !WeNeoMenu.InGame) {
					WeTml.LoadPack(pack.Path);
					Tick();
					return;
				}

				if (del.Contains(Main.mouseX, Main.mouseY)) {
					WeTml.DeletePack(pack.Path);
					Tick();
					return;
				}
			}
		}

		private static void DrawResourcesChrome(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoResHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, (view.Width - 16) / 2 - 4, 32), WeText.UI("NeoApplyPacks"), fade);
			DrawBtn(spriteBatch, new Rectangle(view.X + view.Width / 2 + 4, y, (view.Width - 16) / 2 - 4, 32), WeText.UI("NeoResFolder"), fade);
			y += 40;
		}

		private static void DrawResourcesList(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			List<WeResPack> packs = WeTml.ResourcePacks();
			if (packs.Count == 0) {
				DrawEmpty(spriteBatch, view, ref y, WeText.UI("NeoEmptyRes"), fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(WeTml.ResourcePacksFolder(), 64),
					new Vector2(view.X + 8, y), Color.White * (0.5f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				y += 22;
				return;
			}

			for (int i = 0; i < packs.Count; i++) {
				WeResPack pack = packs[i];
				if (!WeNeoShell.Matches(WeNeoMenu.Search, pack.Name))
					continue;
				ResCard card = NextResCard(view, ref y);
				bool hover = card.Hit.Contains(Main.mouseX, Main.mouseY);
				Color edge = pack.Edge.A == 0 ? WeAccent.Mid : pack.Edge;
				WeDraw.Fill(spriteBatch, card.Hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
				WeDraw.Border(spriteBatch, card.Hit, (hover ? edge : edge * 0.7f) * fade);
				WeDraw.Fill(spriteBatch, new Rectangle(card.Hit.X, card.Hit.Y, 3, card.Hit.Height), edge * fade);
				var icon = new Rectangle(card.Hit.X + 8, card.Hit.Y + 8, 48, 48);
				if (pack.Icon != null && !pack.Icon.IsDisposed)
					WeDraw.DrawCover(spriteBatch, pack.Icon, icon, Color.White * fade);
				else
					WeDraw.Fill(spriteBatch, icon, new Color(40, 44, 52) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(pack.Name, 30),
					new Vector2(card.Hit.X + 66, card.Hit.Y + 10), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoActive"),
					new Vector2(card.Hit.X + 66, card.Hit.Y + 34), (pack.Enabled ? WeAccent.Light : Color.White * 0.4f) * fade,
					0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				DrawBtn(spriteBatch, card.Up, WeText.UI("NeoMoveUp"), fade);
				DrawBtn(spriteBatch, card.Down, WeText.UI("NeoMoveDown"), fade);
				DrawPill(spriteBatch, card.Pill, WeText.UI(pack.Enabled ? "NeoOn" : "NeoOff"), pack.Enabled, fade);
			}
		}

		private static void ClickResources(Rectangle view, ref int y, bool left)
		{
			y += 22;
			var apply = new Rectangle(view.X + 4, y, (view.Width - 16) / 2 - 4, 32);
			var folder = new Rectangle(view.X + view.Width / 2 + 4, y, (view.Width - 16) / 2 - 4, 32);
			y += 40;
			if (left && apply.Contains(Main.mouseX, Main.mouseY)) {
				WeTml.ApplyPacks();
				Tick();
				return;
			}

			if (left && folder.Contains(Main.mouseX, Main.mouseY)) {
				WeFiles.OpenFolder(WeTml.ResourcePacksFolder());
				Tick();
				return;
			}

			int ly = y - (int)WeNeoMenu.Scroll;
			List<WeResPack> packs = WeTml.ResourcePacks();
			if (packs.Count == 0) {
				SkipEmpty(view, ref ly);
				ly += 22;
				return;
			}

			int shown = 0;
			for (int i = 0; i < packs.Count; i++) {
				WeResPack pack = packs[i];
				if (!WeNeoShell.Matches(WeNeoMenu.Search, pack.Name))
					continue;
				ResCard card = NextResCard(view, ref ly);
				if (!left)
					continue;
				if (card.Pill.Contains(Main.mouseX, Main.mouseY)) {
					WeTml.TogglePack(pack);
					Tick();
					return;
				}

				if (card.Up.Contains(Main.mouseX, Main.mouseY)) {
					WeTml.MovePack(shown, -1);
					Tick();
					return;
				}

				if (card.Down.Contains(Main.mouseX, Main.mouseY)) {
					WeTml.MovePack(shown, 1);
					Tick();
					return;
				}

				shown++;
			}
		}

		internal static bool TryBeginPackDrag()
		{
			if (WeNeoMenu.Page != WeNeoPage.Resources)
				return false;
			_packDrag = PackAtMouse();
			return _packDrag >= 0;
		}

		internal static void DragPack(Rectangle view, int mouseY)
		{
			if (_packDrag < 0)
				TryBeginPackDrag();
			int over = PackAt(view, mouseY);
			if (_packDrag < 0 || over < 0 || over == _packDrag)
				return;
			int step = over > _packDrag ? 1 : -1;
			while (_packDrag != over) {
				WeTml.MovePack(_packDrag, step);
				_packDrag += step;
			}
		}

		private static int PackAtMouse() => PackAt(WeNeoShell.View(WeNeoShell.Panel()), Main.mouseY);

		private static int PackAt(Rectangle view, int mouseY)
		{
			int y = WeNeoMenu.ListBox(view).Y - (int)WeNeoMenu.Scroll;
			List<WeResPack> packs = WeTml.ResourcePacks();
			int shown = 0;
			for (int i = 0; i < packs.Count; i++) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, packs[i].Name))
					continue;
				ResCard card = NextResCard(view, ref y);
				if (card.Hit.Contains(Main.mouseX, mouseY) || (mouseY >= card.Hit.Y && mouseY < card.Hit.Bottom))
					return shown;
				shown++;
			}

			return -1;
		}

		private static ResCard NextResCard(Rectangle view, ref int y)
		{
			var hit = new Rectangle(view.X + 4, y, view.Width - 8, 64);
			int right = hit.Right - 10;
			var pill = new Rectangle(right - 58, hit.Y + 20, 58, 24);
			var down = new Rectangle(pill.X - 52, hit.Y + 20, 46, 24);
			var up = new Rectangle(down.X - 50, hit.Y + 20, 46, 24);
			y += 70;
			return new ResCard { Hit = hit, Pill = pill, Up = up, Down = down };
		}

		private static void DrawDevelopChrome(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI("NeoDevelopHint"),
				new Vector2(view.X + 8, y), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, view.Width - 8, 32), WeText.UI("NeoOpenSources"), fade);
			y += 40;
		}

		private static void DrawDevelopList(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			List<WeDevItem> items = WeTml.DevItems();
			if (items.Count == 0) {
				DrawEmpty(spriteBatch, view, ref y, WeText.UI("NeoEmptyDev"), fade);
				return;
			}

			foreach (WeDevItem item in items) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, item.Display, item.Name))
					continue;
				var hit = new Rectangle(view.X + 4, y, view.Width - 8, 56);
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(22, 24, 30)) * fade);
				WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(item.Display, 36),
					new Vector2(hit.X + 12, hit.Y + 8), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.78f));
				string sub = item.Name + (item.Csproj ? "  ·  " + WeText.UI("NeoHasCsproj") : "");
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, Trim(sub, 48),
					new Vector2(hit.X + 12, hit.Y + 30), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
				y += 62;
			}
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

			int ly = y - (int)WeNeoMenu.Scroll;
			List<WeDevItem> items = WeTml.DevItems();
			if (items.Count == 0) {
				SkipEmpty(view, ref ly);
				return;
			}

			foreach (WeDevItem item in items) {
				if (!WeNeoShell.Matches(WeNeoMenu.Search, item.Display, item.Name))
					continue;
				var hit = new Rectangle(view.X + 4, ly, view.Width - 8, 56);
				ly += 62;
				if (left && hit.Contains(Main.mouseX, Main.mouseY)) {
					WeFiles.OpenFolder(item.Folder);
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

		private static void DrawEmpty(SpriteBatch spriteBatch, Rectangle view, ref int y, string text, float fade)
		{
			var box = new Rectangle(view.X + 4, y, view.Width - 8, 56);
			WeDraw.Fill(spriteBatch, box, new Color(18, 20, 26) * fade);
			WeDraw.Border(spriteBatch, box, Color.White * (0.12f * fade));
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(box.X + 12, box.Y + 18), Color.White * (0.6f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 64;
		}

		private static void SkipEmpty(Rectangle view, ref int y) => y += 64;

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

		private static void DrawIconBtn(SpriteBatch spriteBatch, Rectangle hit, Texture2D icon, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			if (icon != null && !icon.IsDisposed) {
				float s = 14f / Math.Max(icon.Width, icon.Height);
				spriteBatch.Draw(icon, new Vector2(hit.Center.X, hit.Center.Y), null, WeAccent.Icon(hover) * fade, 0f, icon.Size() * 0.5f, s, SpriteEffects.None, 0f);
			}
		}

		private static void DrawChip(SpriteBatch spriteBatch, Rectangle hit, string text, bool on, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, ((on || hover) ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, ((on || hover) ? WeAccent.Light : WeAccent.Mid) * fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * WeNeoShell.TypeSmall;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
		}

		private static void DrawPill(SpriteBatch spriteBatch, Rectangle hit, string text, bool on, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Color fill = on ? new Color(28, 72, 48) : new Color(42, 32, 36);
			if (hover)
				fill = on ? new Color(36, 92, 58) : WeAccent.Deep;
			WeDraw.Fill(spriteBatch, hit, fill * fade);
			WeDraw.Border(spriteBatch, hit, (on ? new Color(90, 200, 130) : Color.White * 0.2f) * fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * WeNeoShell.TypeSmall;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				(on ? new Color(180, 255, 200) : Color.White * 0.7f) * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
		}

		private static void Tick() => SoundEngine.PlaySound(SoundID.MenuTick);

		private static string Trim(string s, int n) =>
			string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s[..n] + "…";

		private static string Wrap(string s, int n)
		{
			if (string.IsNullOrEmpty(s))
				return "";
			s = s.Replace('\n', ' ').Replace('\r', ' ');
			if (s.Length <= n)
				return s;
			var sb = new StringBuilder();
			int i = 0;
			int lines = 0;
			while (i < s.Length && lines < 3) {
				int take = Math.Min(n, s.Length - i);
				sb.Append(s.AsSpan(i, take));
				i += take;
				lines++;
				if (i < s.Length && lines < 3)
					sb.Append('\n');
			}

			if (i < s.Length)
				sb.Append('…');
			return sb.ToString();
		}

		private struct ModsBar
		{
			internal Rectangle[] Tools;
			internal Rectangle[] Filters;
			internal Rectangle Info;
			internal bool Dirty;
		}

		private struct ModCard
		{
			internal Rectangle Hit;
			internal Rectangle Pill;
			internal Rectangle Ask;
			internal Rectangle Gear;
		}

		private struct ResCard
		{
			internal Rectangle Hit;
			internal Rectangle Pill;
			internal Rectangle Up;
			internal Rectangle Down;
		}
	}

	internal sealed class WeLocalMod
	{
		internal object Raw;
		internal string Name = "";
		internal string Display = "";
		internal string Description = "";
		internal string Version = "";
		internal string Author = "";
		internal string Side = "";
		internal string Steam = "";
		internal string Path = "";
		internal bool Enabled;
		internal bool HasConfig;
		internal bool Workshop;
		internal Color Edge;
		internal Texture2D Icon;
		internal WeClip Gif;
	}

	internal sealed class WeResPack
	{
		internal object Raw;
		internal string Name = "";
		internal string Path = "";
		internal bool Enabled;
		internal Color Edge;
		internal Texture2D Icon;
	}

	internal sealed class WePackFile
	{
		internal string Path = "";
		internal string Name = "";
		internal int Count;
	}

	internal sealed class WeDevItem
	{
		internal string Folder = "";
		internal string Name = "";
		internal string Display = "";
		internal bool Csproj;
	}

	internal static class WeTml
	{
		private static List<WeLocalMod> _mods;
		private static List<WeResPack> _res;
		private static List<WePackFile> _packs;
		private static List<WeDevItem> _dev;
		private static HashSet<string> _want;
		private static HashSet<string> _loaded;
		private static HashSet<string> _resWant;
		private static readonly Dictionary<string, Texture2D> Icons = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, WeClip> Gifs = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, Color> Edges = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, Texture2D> PackIcons = new(StringComparer.OrdinalIgnoreCase);
		private static bool _stale = true;

		internal static void Touch()
		{
			_res = null;
			_packs = null;
			_dev = null;
		}

		internal static bool ReloadNeeded
		{
			get
			{
				EnsureMods();
				if (_want == null || _loaded == null)
					return false;
				foreach (WeLocalMod mod in _mods) {
					bool want = _want.Contains(mod.Name);
					bool live = _loaded.Contains(mod.Name);
					if (want != live)
						return true;
				}

				return false;
			}
		}

		internal static List<WeLocalMod> LocalMods()
		{
			EnsureMods();
			return _mods;
		}

		private static void EnsureMods()
		{
			if (!_stale && _mods != null)
				return;
			_stale = false;
			_mods = ScanMods();
			_loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			try {
				foreach (Mod mod in ModLoader.Mods) {
					if (mod.Name != "ModLoader")
						_loaded.Add(mod.Name);
				}
			}
			catch {
			}

			_want ??= ReadWant(_mods, _loaded);
			foreach (WeLocalMod mod in _mods)
				mod.Enabled = _want.Contains(mod.Name);
		}

		private static List<WeLocalMod> ScanMods()
		{
			var list = new List<WeLocalMod>();
			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
				MethodInfo find = type?.GetMethod("FindMods", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (find != null) {
					object[] args = find.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)).ToArray();
					object result = find.Invoke(null, args);
					if (result is IEnumerable en) {
						foreach (object raw in en) {
							WeLocalMod mod = ReadLocal(raw);
							if (mod != null)
								list.Add(mod);
						}
					}
				}
			}
			catch {
			}

			return list.Count > 0 ? list : FromLoaded(list);
		}

		private static List<WeLocalMod> FromLoaded(List<WeLocalMod> list)
		{
			try {
				foreach (Mod mod in ModLoader.Mods) {
					if (mod.Name == "ModLoader")
						continue;
					var local = new WeLocalMod {
						Name = mod.Name,
						Display = string.IsNullOrEmpty(mod.DisplayName) ? mod.Name : mod.DisplayName,
						Description = Str(mod, "Description") ?? "",
						Version = mod.Version?.ToString() ?? "",
						Side = "Client"
					};
					FillIcon(local, null, mod);
					local.HasConfig = HasConfig(local.Name);
					list.Add(local);
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
			object file = Prop(raw, "modFile") ?? Prop(raw, "ModFile") ?? Prop(raw, "File");
			mod.Name = Str(file, "Name") ?? Str(raw, "Name") ?? "";
			if (string.IsNullOrEmpty(mod.Name) || mod.Name == "ModLoader")
				return null;
			object props = Prop(raw, "properties") ?? Prop(raw, "Properties");
			mod.Display = Str(props, "displayName") ?? Str(props, "DisplayName") ?? mod.Name;
			mod.Description = Str(props, "description") ?? Str(props, "Description") ?? "";
			mod.Version = Str(props, "version") ?? Str(props, "Version") ?? "";
			mod.Author = Str(props, "author") ?? Str(props, "Author") ?? "";
			mod.Side = Str(props, "side") ?? Str(props, "Side") ?? "";
			mod.Path = Str(file, "path") ?? Str(file, "Path") ?? "";
			mod.Steam = SteamId(props, file, mod.Path);
			mod.Workshop = !string.IsNullOrEmpty(mod.Steam) ||
			               (!string.IsNullOrEmpty(mod.Path) &&
			                (mod.Path.IndexOf("workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
				                mod.Path.IndexOf("1281930", StringComparison.OrdinalIgnoreCase) >= 0));
			Mod loaded = null;
			try {
				ModLoader.TryGetMod(mod.Name, out loaded);
			}
			catch {
			}

			FillIcon(mod, file, loaded);
			mod.HasConfig = HasConfig(mod.Name);
			return mod;
		}

		private static string SteamId(object props, object file, string path)
		{
			string[] keys = { "steamid", "steamId", "publishId", "PublishId", "workshopId", "WorkshopId" };
			foreach (string key in keys) {
				string v = Str(props, key) ?? Str(file, key);
				if (LooksId(v))
					return Digits(v);
			}

			return SteamFromPath(path);
		}

		private static string SteamFromPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return "";
			int at = path.IndexOf("1281930", StringComparison.OrdinalIgnoreCase);
			if (at < 0)
				return "";
			string rest = path[(at + 8)..].TrimStart('\\', '/');
			int slash = rest.IndexOfAny(new[] { '\\', '/' });
			string id = slash < 0 ? rest : rest[..slash];
			return LooksId(id) ? id : "";
		}

		private static bool LooksId(string v)
		{
			if (string.IsNullOrEmpty(v))
				return false;
			int n = 0;
			foreach (char c in v) {
				if (char.IsDigit(c))
					n++;
			}

			return n >= 6;
		}

		private static string Digits(string v)
		{
			var sb = new StringBuilder();
			foreach (char c in v) {
				if (char.IsDigit(c))
					sb.Append(c);
			}

			return sb.ToString();
		}

		private static void FillIcon(WeLocalMod mod, object file, Mod loaded)
		{
			if (Gifs.TryGetValue(mod.Name, out WeClip gif))
				mod.Gif = gif;
			if (Icons.TryGetValue(mod.Name, out Texture2D icon))
				mod.Icon = icon;
			if (Edges.TryGetValue(mod.Name, out Color edge))
				mod.Edge = edge;
			if (mod.Gif != null || mod.Icon != null)
				return;

			byte[] gifBytes = null;
			byte[] pngBytes = null;
			IDisposable lease = null;
			try {
				lease = OpenTmod(file);
				gifBytes = BytesOpened(file, "icon.gif") ?? BytesOpened(file, "Icon.gif");
				pngBytes = BytesOpened(file, "icon.png") ?? BytesOpened(file, "Icon.png");
			}
			catch {
			}
			finally {
				try {
					lease?.Dispose();
				}
				catch {
				}
			}

			gifBytes ??= LoadedBytes(loaded, "icon.gif");
			pngBytes ??= LoadedBytes(loaded, "icon.png");

			if (gifBytes != null && WeGif.LooksLike(gifBytes)) {
				try {
					WeClip clip = WeGif.Decode(gifBytes);
					if (clip != null) {
						clip.KeepDelays();
						bool prev = WeAnim.CanUpload;
						WeAnim.CanUpload = true;
						try {
							clip.Present();
						}
						finally {
							WeAnim.CanUpload = prev;
						}

						mod.Gif = clip;
						Gifs[mod.Name] = clip;
						Texture2D cur = clip.Current();
						if (cur != null) {
							mod.Icon = cur;
							Icons[mod.Name] = cur;
						}
					}
				}
				catch {
				}
			}

			if (mod.Icon == null && pngBytes != null) {
				Texture2D tex = TexFrom(pngBytes);
				if (tex != null) {
					mod.Icon = tex;
					Icons[mod.Name] = tex;
				}
			}

			if (mod.Icon != null) {
				mod.Edge = Mean(mod.Icon);
				Edges[mod.Name] = mod.Edge;
			}
		}

		private static byte[] LoadedBytes(Mod mod, string name)
		{
			if (mod == null)
				return null;
			try {
				if (mod.FileExists(name))
					return mod.GetFileBytes(name);
			}
			catch {
			}

			return null;
		}

		private static IDisposable OpenTmod(object file)
		{
			if (file == null)
				return null;
			try {
				MethodInfo open = file.GetType().GetMethod("Open", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
				object result = open?.Invoke(file, null);
				if (result is IDisposable d && !ReferenceEquals(d, file))
					return d;
			}
			catch {
			}

			return null;
		}

		private static byte[] BytesOpened(object file, string name)
		{
			if (file == null || string.IsNullOrEmpty(name))
				return null;
			Type type = file.GetType();
			try {
				MethodInfo has = type.GetMethod("HasFile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
				if (has != null && has.Invoke(file, new object[] { name }) is false)
					return null;
			}
			catch {
			}

			foreach (string method in new[] { "GetBytes", "GetFileBytes" }) {
				try {
					MethodInfo get = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
					if (get?.Invoke(file, new object[] { name }) is byte[] data && data.Length > 0)
						return data;
				}
				catch {
				}
			}

			return null;
		}

		private static Texture2D TexFrom(byte[] data)
		{
			if (data == null || data.Length < 8)
				return null;
			try {
				GraphicsDevice device = Main.instance?.GraphicsDevice ?? Main.graphics?.GraphicsDevice;
				if (device == null)
					return null;
				using var ms = new MemoryStream(data);
				return Texture2D.FromStream(device, ms);
			}
			catch {
				return null;
			}
		}

		private static Color Mean(Texture2D tex)
		{
			try {
				int w = tex.Width;
				int h = tex.Height;
				if (w < 1 || h < 1 || w * h > 262144)
					return WeAccent.Mid;
				var data = new Color[w * h];
				tex.GetData(data);
				int step = Math.Max(1, data.Length / 256);
				long r = 0, g = 0, b = 0;
				int n = 0;
				for (int i = 0; i < data.Length; i += step) {
					Color c = data[i];
					if (c.A < 40)
						continue;
					r += c.R;
					g += c.G;
					b += c.B;
					n++;
				}

				if (n == 0)
					return WeAccent.Mid;
				return new Color((int)(r / n), (int)(g / n), (int)(b / n));
			}
			catch {
				return WeAccent.Mid;
			}
		}

		private static bool HasConfig(string name)
		{
			try {
				string folder = ConfigsFolder();
				if (!Directory.Exists(folder))
					return false;
				foreach (string file in Directory.GetFiles(folder)) {
					if (Path.GetFileName(file).StartsWith(name, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}
			catch {
			}

			return false;
		}

		private static HashSet<string> ReadWant(List<WeLocalMod> mods, HashSet<string> loaded)
		{
			var want = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string json = Path.Combine(ModsFolder(), "enabled.json");
			if (File.Exists(json)) {
				try {
					foreach (string name in Quoted(File.ReadAllText(json))) {
						if (mods.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
							want.Add(name);
					}
				}
				catch {
				}
			}

			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
				MethodInfo load = type?.GetMethod("LoadEnabledMods", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (load != null && load.GetParameters().Length == 0 && load.Invoke(null, null) is IEnumerable names) {
					foreach (object n in names) {
						if (n != null)
							want.Add(n.ToString());
					}
				}
			}
			catch {
			}

			try {
				object em = WeNeoFld.Get(typeof(ModLoader), "EnabledMods") ?? WeNeoFld.Get(typeof(ModLoader), "enabledMods");
				if (em is IEnumerable listed) {
					foreach (object n in listed) {
						if (n != null)
							want.Add(n.ToString());
					}
				}
			}
			catch {
			}

			if (want.Count == 0) {
				foreach (string name in loaded)
					want.Add(name);
			}

			return want;
		}

		internal static void SetEnabled(WeLocalMod mod, bool on)
		{
			EnsureMods();
			if (on)
				_want.Add(mod.Name);
			else
				_want.Remove(mod.Name);
			mod.Enabled = on;
			try {
				SetBool(mod.Raw, "Enabled", on);
			}
			catch {
			}

			WriteWant();
		}

		internal static void SetAll(bool on)
		{
			EnsureMods();
			foreach (WeLocalMod mod in _mods) {
				if (on)
					_want.Add(mod.Name);
				else
					_want.Remove(mod.Name);
				mod.Enabled = on;
				try {
					SetBool(mod.Raw, "Enabled", on);
				}
				catch {
				}
			}

			WriteWant();
		}

		private static void WriteWant()
		{
			if (_want == null)
				return;
			try {
				object em = WeNeoFld.Get(typeof(ModLoader), "EnabledMods") ?? WeNeoFld.Get(typeof(ModLoader), "enabledMods");
				if (em is ICollection<string> set) {
					set.Clear();
					foreach (string name in _want)
						set.Add(name);
				}
			}
			catch {
			}

			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
				type?.GetMethod("SaveEnabledMods", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
			}
			catch {
			}

			try {
				string folder = ModsFolder();
				Directory.CreateDirectory(folder);
				var sb = new StringBuilder();
				sb.Append('[');
				bool first = true;
				foreach (string name in _want) {
					if (!first)
						sb.Append(',');
					first = false;
					sb.Append('"').Append(name).Append('"');
				}

				sb.Append(']');
				File.WriteAllText(Path.Combine(folder, "enabled.json"), sb.ToString());
			}
			catch {
			}
		}

		internal static void Reload()
		{
			try {
				WeNeoMenu.Close();
			}
			catch {
			}

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

		internal static string ConfigsFolder() => Path.Combine(Main.SavePath, "ModConfigs");

		internal static string ModPacksFolder()
		{
			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
				object p = type == null ? null : WeNeoFld.Get(type, "ModPacksDirectory") ?? WeNeoFld.Get(type, "modPacksDirectory");
				if (p is string s && !string.IsNullOrEmpty(s))
					return s;
			}
			catch {
			}

			foreach (string dir in new[] {
				         Path.Combine(Main.SavePath, "ModPacks"),
				         Path.Combine(ModsFolder(), "ModPacks"),
				         Path.Combine(Main.SavePath, "Mods", "ModPacks")
			         }) {
				if (Directory.Exists(dir))
					return dir;
			}

			return Path.Combine(Main.SavePath, "ModPacks");
		}

		internal static List<WePackFile> ModPackItems()
		{
			if (_packs != null)
				return _packs;
			var list = new List<WePackFile>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string folder in new[] {
				         ModPacksFolder(),
				         Path.Combine(Main.SavePath, "ModPacks"),
				         Path.Combine(ModsFolder(), "ModPacks")
			         }) {
				if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
					continue;
				try {
					foreach (string file in Directory.GetFiles(folder, "*.json")) {
						if (!seen.Add(file))
							continue;
						string json = "";
						try {
							json = File.ReadAllText(file);
						}
						catch {
						}

						list.Add(new WePackFile {
							Path = file,
							Name = Path.GetFileNameWithoutExtension(file),
							Count = Quoted(json).Count
						});
					}
				}
				catch {
				}
			}

			_packs = list;
			return _packs;
		}

		internal static void SavePack()
		{
			try {
				string folder = ModPacksFolder();
				Directory.CreateDirectory(folder);
				EnsureMods();
				var names = _mods.Where(m => m.Enabled).Select(m => m.Name);
				File.WriteAllText(Path.Combine(folder, "overlay-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".json"),
					"{\"mods\":[\"" + string.Join("\",\"", names) + "\"]}");
				_packs = null;
			}
			catch {
			}
		}

		internal static void LoadPack(string path)
		{
			try {
				string json = File.ReadAllText(path);
				var want = new HashSet<string>(Quoted(json), StringComparer.OrdinalIgnoreCase);
				EnsureMods();
				_want.Clear();
				foreach (WeLocalMod mod in _mods) {
					bool on = want.Contains(mod.Name);
					if (on)
						_want.Add(mod.Name);
					mod.Enabled = on;
					try {
						SetBool(mod.Raw, "Enabled", on);
					}
					catch {
					}
				}

				WriteWant();
			}
			catch {
			}
		}

		internal static void DeletePack(string path)
		{
			try {
				if (File.Exists(path))
					File.Delete(path);
				_packs = null;
			}
			catch {
			}
		}

		internal static List<WeResPack> ResourcePacks()
		{
			if (_res != null)
				return _res;
			var map = new Dictionary<string, WeResPack>(StringComparer.OrdinalIgnoreCase);
			try {
				object ctrl = typeof(Main).GetProperty("AssetSourceController")?.GetValue(Main.instance);
				if (ctrl != null) {
					object list = Prop(ctrl, "ActiveResourcePackList") ?? Prop(ctrl, "AllPacks");
					AddPackEnum(map, Prop(list, "AllPacks") ?? list, null);
					AddPackEnum(map, Prop(list, "EnabledPacks") ?? Prop(ctrl, "EnabledPacks"), true);
					AddPackEnum(map, Prop(list, "DisabledPacks") ?? Prop(ctrl, "DisabledPacks"), false);
				}
			}
			catch {
			}

			foreach (string folder in ResourceRoots())
				ScanPackDir(map, folder);

			_res = map.Values.ToList();
			_resWant ??= new HashSet<string>(_res.Where(p => p.Enabled).Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
			foreach (WeResPack pack in _res)
				pack.Enabled = _resWant.Contains(pack.Name);
			return _res;
		}

		private static void AddPackEnum(Dictionary<string, WeResPack> map, object src, bool? enabled)
		{
			if (src is not IEnumerable en)
				return;
			foreach (object p in en) {
				string name = Str(p, "Name") ?? Str(p, "FileName") ?? Str(p, "FolderName") ?? "";
				if (string.IsNullOrEmpty(name))
					continue;
				if (!map.TryGetValue(name, out WeResPack pack)) {
					pack = new WeResPack { Name = name };
					map[name] = pack;
				}

				pack.Raw ??= p;
				pack.Path = Str(p, "FullPath") ?? Str(p, "Path") ?? Str(p, "Directory") ?? pack.Path;
				if (enabled.HasValue)
					pack.Enabled = enabled.Value;
				else
					pack.Enabled = BoolOf(p, "IsEnabled") || BoolOf(p, "Enabled") || pack.Enabled;
				FillPackIcon(pack);
			}
		}

		private static void ScanPackDir(Dictionary<string, WeResPack> map, string root)
		{
			if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
				return;
			try {
				foreach (string dir in Directory.GetDirectories(root)) {
					string json = Path.Combine(dir, "pack.json");
					string icon = Path.Combine(dir, "icon.png");
					if (!File.Exists(json) && !File.Exists(icon))
						continue;
					string name = Path.GetFileName(dir);
					if (File.Exists(json)) {
						try {
							foreach (string quoted in Quoted(File.ReadAllText(json))) {
								if (!string.Equals(quoted, "Name", StringComparison.OrdinalIgnoreCase) &&
								    !string.Equals(quoted, "Description", StringComparison.OrdinalIgnoreCase) &&
								    quoted.Length > 1) {
									name = quoted;
									break;
								}
							}
						}
						catch {
						}
					}

					if (!map.TryGetValue(name, out WeResPack pack)) {
						pack = new WeResPack { Name = name, Path = dir };
						map[name] = pack;
					}

					if (string.IsNullOrEmpty(pack.Path))
						pack.Path = dir;
					FillPackIcon(pack);
				}
			}
			catch {
			}
		}

		private static void FillPackIcon(WeResPack pack)
		{
			if (pack.Icon != null)
				return;
			if (PackIcons.TryGetValue(pack.Name, out Texture2D cached)) {
				pack.Icon = cached;
				return;
			}

			string[] files =
			{
				string.IsNullOrEmpty(pack.Path) ? null : Path.Combine(pack.Path, "icon.png"),
				string.IsNullOrEmpty(pack.Path) ? null : Path.Combine(pack.Path, "Icon.png")
			};
			foreach (string file in files) {
				if (string.IsNullOrEmpty(file) || !File.Exists(file))
					continue;
				try {
					byte[] data = File.ReadAllBytes(file);
					Texture2D tex = TexFrom(data);
					if (tex == null)
						continue;
					pack.Icon = tex;
					PackIcons[pack.Name] = tex;
					pack.Edge = Mean(tex);
					return;
				}
				catch {
				}
			}

			try {
				object icon = Prop(pack.Raw, "Icon") ?? Prop(pack.Raw, "icon");
				if (icon is Texture2D tex) {
					pack.Icon = tex;
					pack.Edge = Mean(tex);
				}
			}
			catch {
			}
		}

		private static IEnumerable<string> ResourceRoots()
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			void Add(string dir)
			{
				if (!string.IsNullOrEmpty(dir) && seen.Add(dir)) { }
			}

			Add(Path.Combine(Main.SavePath, "ResourcePacks"));
			try {
				string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				Add(Path.Combine(docs, "My Games", "Terraria", "ResourcePacks"));
				Add(Path.Combine(docs, "My Games", "Terraria", "tModLoader", "ResourcePacks"));
			}
			catch {
			}

			Add(Path.Combine(Main.SavePath, "Workshop"));
			string walk = Main.SavePath;
			for (int i = 0; i < 8 && !string.IsNullOrEmpty(walk); i++) {
				Add(Path.Combine(walk, "steamapps", "workshop", "content", "1281930"));
				walk = Path.GetDirectoryName(walk);
			}

			foreach (string dir in seen)
				yield return dir;
		}

		internal static string ResourcePacksFolder()
		{
			string dir = Path.Combine(Main.SavePath, "ResourcePacks");
			try {
				Directory.CreateDirectory(dir);
			}
			catch {
			}

			return dir;
		}

		internal static void TogglePack(WeResPack pack)
		{
			pack.Enabled = !pack.Enabled;
			_resWant ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (pack.Enabled)
				_resWant.Add(pack.Name);
			else
				_resWant.Remove(pack.Name);
			try {
				SetBool(pack.Raw, "IsEnabled", pack.Enabled);
				SetBool(pack.Raw, "Enabled", pack.Enabled);
			}
			catch {
			}
		}

		internal static void MovePack(int shownIndex, int delta)
		{
			if (_res == null)
				return;
			var visible = new List<int>();
			for (int i = 0; i < _res.Count; i++) {
				if (WeNeoShell.Matches(WeNeoMenu.Search, _res[i].Name))
					visible.Add(i);
			}

			int at = shownIndex;
			int to = at + delta;
			if (at < 0 || to < 0 || at >= visible.Count || to >= visible.Count)
				return;
			int ia = visible[at];
			int ib = visible[to];
			(_res[ia], _res[ib]) = (_res[ib], _res[ia]);
		}

		internal static void ApplyPacks()
		{
			try {
				if (_res != null) {
					foreach (WeResPack pack in _res) {
						bool on = _resWant != null && _resWant.Contains(pack.Name);
						pack.Enabled = on;
						SetBool(pack.Raw, "IsEnabled", on);
						SetBool(pack.Raw, "Enabled", on);
					}
				}

				object ctrl = typeof(Main).GetProperty("AssetSourceController")?.GetValue(Main.instance);
				if (ctrl != null) {
					Type ct = ctrl.GetType();
					ct.GetMethod("Refresh")?.Invoke(ctrl, null);
					MethodInfo use = ct.GetMethod("UseResourcePacks");
					if (use != null) {
						ParameterInfo[] ps = use.GetParameters();
						if (ps.Length == 0)
							use.Invoke(ctrl, null);
						else {
							object list = Prop(ctrl, "ActiveResourcePackList");
							use.Invoke(ctrl, new[] { list });
						}
					}

					ct.GetMethod("RefreshResources")?.Invoke(ctrl, null);
				}

				typeof(Main).GetMethod("SaveSettings")?.Invoke(null, null);
			}
			catch {
			}
		}

		internal static string SourcesFolder()
		{
			object p = WeNeoFld.Get(typeof(ModLoader), "ModSourcePath") ?? WeNeoFld.Get(typeof(ModLoader), "ModSourcesPath");
			return p as string ?? Path.Combine(Main.SavePath, "ModSources");
		}

		internal static List<WeDevItem> DevItems()
		{
			if (_dev != null)
				return _dev;
			var list = new List<WeDevItem>();
			string folder = SourcesFolder();
			if (!Directory.Exists(folder)) {
				_dev = list;
				return _dev;
			}

			try {
				foreach (string dir in Directory.GetDirectories(folder)) {
					string name = Path.GetFileName(dir);
					if (string.IsNullOrEmpty(name) || name.StartsWith('.'))
						continue;
					var item = new WeDevItem {
						Folder = dir,
						Name = name,
						Display = name,
						Csproj = Directory.GetFiles(dir, "*.csproj").Length > 0
					};
					string build = Path.Combine(dir, "build.txt");
					if (File.Exists(build)) {
						try {
							foreach (string line in File.ReadAllLines(build)) {
								int eq = line.IndexOf('=');
								if (eq <= 0)
									continue;
								string key = line[..eq].Trim();
								if (!key.Equals("displayName", StringComparison.OrdinalIgnoreCase))
									continue;
								string val = line[(eq + 1)..].Trim();
								if (!string.IsNullOrEmpty(val))
									item.Display = val;
								break;
							}
						}
						catch {
						}
					}

					list.Add(item);
				}
			}
			catch {
			}

			_dev = list;
			return _dev;
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

		internal static void OpenSteamPage(string id)
		{
			try {
				if (string.IsNullOrEmpty(id))
					OpenSteamWorkshop();
				else
					WeOs.Reveal("https://steamcommunity.com/sharedfiles/filedetails/?id=" + id);
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

		private static List<string> Quoted(string json)
		{
			var list = new List<string>();
			if (string.IsNullOrEmpty(json))
				return list;
			int i = 0;
			while (i < json.Length) {
				int a = json.IndexOf('"', i);
				if (a < 0)
					break;
				int b = a + 1;
				while (b < json.Length && json[b] != '"') {
					if (json[b] == '\\')
						b++;
					b++;
				}

				if (b >= json.Length)
					break;
				string val = json.Substring(a + 1, b - a - 1);
				if (val.Length > 0 && val != "mods" && val != "enabled" && val != "disabled")
					list.Add(val);
				i = b + 1;
			}

			return list;
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
