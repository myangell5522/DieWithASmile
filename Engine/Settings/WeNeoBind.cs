using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using DieWithASmile.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal enum WeNeoKind
	{
		Header,
		Toggle,
		Cycle,
		Slider,
		Button
	}

	internal sealed class WeNeoOpt
	{
		internal WeNeoCat Cat;
		internal WeNeoKind Kind;
		internal string Section;
		internal string Id;
		internal Func<string> Label;
		internal Func<string> Value;
		internal Func<bool> On;
		internal Func<float> Amount;
		internal Action Click;
		internal Action Right;
		internal Action<float> Slide;
		internal Func<bool> Show = () => true;
	}

	internal static class WeNeoBind
	{
		private static List<WeNeoOpt> _all;

		internal static bool NeedApply =>
			Main.PendingResolutionWidth > 0 &&
			(Main.PendingResolutionWidth != Main.screenWidth || Main.PendingResolutionHeight != Main.screenHeight);

		internal static IReadOnlyList<WeNeoOpt> All => _all ??= Build();

		internal static void Draw(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			string last = "";
			string q = WeNeoMenu.Search;
			bool searching = !string.IsNullOrWhiteSpace(q);
			foreach (WeNeoOpt opt in All) {
				if (!opt.Show())
					continue;
				if (!searching && opt.Cat != WeNeoMenu.Category)
					continue;
				if (searching && opt.Kind == WeNeoKind.Header)
					continue;
				if (searching && !WeNeoShell.Matches(q, opt.Label(), opt.Value?.Invoke() ?? "", opt.Section, opt.Id))
					continue;
				if (!searching && opt.Kind != WeNeoKind.Header && last != opt.Section) {
					WeNeoShell.Header(spriteBatch, view, ref y, opt.Section, fade);
					last = opt.Section;
				}

				if (searching && last != opt.Section) {
					WeNeoShell.Header(spriteBatch, view, ref y, opt.Section, fade);
					last = opt.Section;
				}

				switch (opt.Kind) {
					case WeNeoKind.Header:
						WeNeoShell.Header(spriteBatch, view, ref y, opt.Label(), fade);
						last = opt.Section;
						break;
					case WeNeoKind.Toggle:
						WeNeoShell.Toggle(spriteBatch, view, ref y, opt.Label(), opt.On(), fade);
						break;
					case WeNeoKind.Slider:
						WeNeoShell.Slider(spriteBatch, view, ref y, opt.Label(), opt.Amount(), opt.Value(), fade);
						break;
					default:
						WeNeoShell.Cycle(spriteBatch, view, ref y, opt.Label(), opt.Value(), fade);
						break;
				}
			}

			if (!searching && WeNeoMenu.Category == WeNeoCat.Client)
				WeNeoClient.Draw(spriteBatch, view, ref y, fade);
			else if (searching)
				WeNeoClient.DrawSearch(spriteBatch, view, ref y, q, fade);
		}

		internal static void Click(Rectangle view, ref int y, bool left, bool right)
		{
			string last = "";
			string q = WeNeoMenu.Search;
			bool searching = !string.IsNullOrWhiteSpace(q);
			foreach (WeNeoOpt opt in All) {
				if (!opt.Show())
					continue;
				if (!searching && opt.Cat != WeNeoMenu.Category)
					continue;
				if (searching && opt.Kind == WeNeoKind.Header)
					continue;
				if (searching && !WeNeoShell.Matches(q, opt.Label(), opt.Value?.Invoke() ?? "", opt.Section, opt.Id))
					continue;
				if (opt.Kind == WeNeoKind.Header) {
					y += 26;
					last = opt.Section;
					continue;
				}

				if (!searching && last != opt.Section) {
					y += 26;
					last = opt.Section;
				}

				if (searching && last != opt.Section) {
					y += 26;
					last = opt.Section;
				}

				if (opt.Kind == WeNeoKind.Slider) {
					Rectangle bar = WeNeoShell.SliderBar(view, y);
					y += WeNeoShell.RowStep;
					if (left && bar.Contains(Main.mouseX, Main.mouseY)) {
						WeNeoMenu.SetDrag(opt.Id);
						Slide(opt.Id);
						return;
					}

					continue;
				}

				bool hit = WeNeoShell.HitRow(view, ref y);
				y += WeNeoShell.RowStep;
				if (!hit)
					continue;
				if (right && opt.Right != null) {
					opt.Right();
					Tick();
					return;
				}

				if (left && opt.Click != null) {
					opt.Click();
					Tick();
					return;
				}
			}

			if (!searching && WeNeoMenu.Category == WeNeoCat.Client)
				WeNeoClient.Click(view, ref y, left, right);
			else if (searching)
				WeNeoClient.ClickSearch(view, ref y, q, left, right);
		}

		internal static void Slide(string id)
		{
			foreach (WeNeoOpt opt in All) {
				if (opt.Id != id || opt.Slide == null)
					continue;
				Rectangle view = WeNeoShell.View(WeNeoShell.Panel());
				int dummy = 0;
				Rectangle bar = WeNeoShell.SliderBar(view, dummy);
				float t = MathHelper.Clamp((Main.mouseX - bar.X) / (float)Math.Max(1, bar.Width), 0f, 1f);
				opt.Slide(t);
				return;
			}

			WeNeoClient.Slide(id);
		}

		internal static void ApplyVideo()
		{
			try {
				Main.SetDisplayMode(Main.PendingResolutionWidth, Main.PendingResolutionHeight, Main.graphics.IsFullScreen);
			}
			catch {
			}

			PersistSoft();
			Tick();
		}

		private static void Tick() => SoundEngine.PlaySound(SoundID.MenuTick);

		private static void PersistSoft()
		{
			try {
				Main.SaveSettings();
			}
			catch {
			}
		}

		private static string OnOff(bool on) => Language.GetTextValue(on ? "LegacyMenu.126" : "LegacyMenu.124");

		private static string Menu(int i)
		{
			try {
				if (Lang.menu != null && i >= 0 && i < Lang.menu.Length && Lang.menu[i] != null)
					return Lang.menu[i].Value;
			}
			catch {
			}

			return "";
		}

		private static string L(string key, string fallback)
		{
			string v = Language.GetTextValue(key);
			if (string.IsNullOrEmpty(v) || v == key)
				return fallback;
			return v;
		}

		private static List<WeNeoOpt> Build()
		{
			var list = new List<WeNeoOpt>();
			string g = WeText.UI("NeoSectionGeneral");
			string qlt = WeText.UI("NeoSectionQuality");
			string audio = WeText.UI("NeoCatAudio");

			Toggle(list, WeNeoCat.Game, g, "autosave", () => Nz(Menu(67), "Autosave"), () => Main.autoSave, v => { Main.autoSave = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "autopause", () => Nz(Menu(68), "Autopause"), () => Main.autoPause, v => { Main.autoPause = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "map", () => Nz(Menu(69), "Map"), () => Main.mapEnabled, v => { Main.mapEnabled = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "pass", () => Nz(Menu(70), "Hide passwords"), () => Main.HidePassword, v => { Main.HidePassword = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "bonus", () => L("UI.SetBonusHotkey", "Set bonus"),
				() => WeNeoFld.GetBool(typeof(Main), "ReverseUpDownForArmorSetBonuses"),
				v => WeNeoFld.SetBool(typeof(Main), "ReverseUpDownForArmorSetBonuses", v));
			Toggle(list, WeNeoCat.Game, g, "autofire", () => Nz(Menu(332), "Autofire"),
				() => Main.SettingsEnabled_AutoReuseAllItems,
				v => { Main.SettingsEnabled_AutoReuseAllItems = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Game, g, "doors", () => L("UI.SmartDoors", "Smart doors"),
				() => WeNeoFld.Get("DoorOpeningHelper", "Preference")?.ToString() ?? "",
				() => WeNeoFld.Cycle("DoorOpeningHelper", "Preference", 1),
				() => WeNeoFld.Cycle("DoorOpeningHelper", "Preference", -1));
			Cycle(list, WeNeoCat.Game, g, "hover", () => L("UI.HoverControl", "Hover"),
				() => HoverLabel(),
				() => CycleHover(1), () => CycleHover(-1));
			Slider(list, WeNeoCat.Game, g, "zoom", () => L("UI.Zoom", "Zoom"),
				() => (Main.GameZoomTarget - 1f) / 1f,
				() => (int)Math.Round(Main.GameZoomTarget * 100f) + "%",
				t => { Main.GameZoomTarget = 1f + t; PersistSoft(); });
			Slider(list, WeNeoCat.Game, g, "uiscale", () => L("UI.UIScale", "UI scale"),
				() => MathHelper.Clamp((Main.UIScaleWanted - 0.5f) / 1.5f, 0f, 1f),
				() => (int)Math.Round(Main.UIScaleWanted * 100f) + "%",
				t => { Main.UIScale = 0.5f + t * 1.5f; PersistSoft(); });
			Button(list, WeNeoCat.Game, g, "lang", () => L("LegacyMenu.102", "Language"),
				() => Language.ActiveCulture?.Name ?? "",
				() => WeNeoDeep.OpenLanguage());

			Toggle(list, WeNeoCat.Video, qlt, "full", () => Nz(Menu(237), "Fullscreen"),
				() => Main.graphics.IsFullScreen,
				v => {
					try {
						Main.SetDisplayMode(Main.PendingResolutionWidth > 0 ? Main.PendingResolutionWidth : Main.screenWidth,
							Main.PendingResolutionHeight > 0 ? Main.PendingResolutionHeight : Main.screenHeight, v);
					}
					catch {
					}

					PersistSoft();
				});
			Toggle(list, WeNeoCat.Video, qlt, "borderless", () => Nz(Menu(245).Replace(": Enabled", "").Replace(": Disabled", ""), "Borderless window"),
				() => Main.screenBorderless,
				v => { Main.screenBorderless = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Video, qlt, "res", () => Nz(Menu(51), "Resolution"),
				() => (Main.PendingResolutionWidth > 0 ? Main.PendingResolutionWidth : Main.screenWidth) + "x" +
				      (Main.PendingResolutionHeight > 0 ? Main.PendingResolutionHeight : Main.screenHeight),
				() => CycleRes(1), () => CycleRes(-1));
			Cycle(list, WeNeoCat.Video, qlt, "skip", () => Nz(Menu(247).Split(':')[0], "Frame skip"),
				() => Main.FrameSkipMode.ToString(),
				() => { Main.CycleFrameSkipMode(); PersistSoft(); });
			Cycle(list, WeNeoCat.Video, qlt, "light", () => L("LegacyMenu.370", "Lighting"),
				() => Lighting.Mode.ToString(),
				() => {
					Lighting.Mode = (LightMode)(((int)Lighting.Mode + 1) % 4);
					PersistSoft();
				});
			Cycle(list, WeNeoCat.Video, qlt, "qual", () => Nz(Menu(55), "Quality"),
				() => Main.qaStyle.ToString(),
				() => { Main.qaStyle = (Main.qaStyle + 1) % 4; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "bg", () => Nz(Menu(113), "Backgrounds"),
				() => Main.BackgroundEnabled, v => { Main.BackgroundEnabled = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "gore", () => L("LegacyMenu.371", "Blood and gore"),
				() => ChildSafety.Disabled, v => { ChildSafety.Disabled = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "wobble", () => L("UI.MinersWobble", "Miner's wobble"),
				() => Main.SettingsEnabled_MinersWobble, v => { Main.SettingsEnabled_MinersWobble = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "heat", () => L("UI.HeatDistortion", "Heat distortion"),
				() => Main.UseHeatDistortion, v => { Main.UseHeatDistortion = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "storm", () => L("UI.StormEffects", "Storm effects"),
				() => Main.UseStormEffects, v => { Main.UseStormEffects = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Video, qlt, "waves", () => L("UI.WaveQuality", "Waves"),
				() => Main.WaveQuality.ToString(),
				() => { Main.WaveQuality = (Main.WaveQuality + 1) % 4; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "wind", () => L("UI.WindyDayQuality", "Windy environment"),
				() => Main.SettingsEnabled_TilesSwayInWind, v => { Main.SettingsEnabled_TilesSwayInWind = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "intense", () => L("UI.DisableIntenseVisualEffects", "Intense effects"),
				() => !Main.DisableIntenseVisualEffects, v => { Main.DisableIntenseVisualEffects = !v; PersistSoft(); });
			Slider(list, WeNeoCat.Video, qlt, "par", () => Nz(Menu(52), "Parallax"),
				() => Main.caveParallax,
				() => (int)Math.Round(Main.caveParallax * 100f) + "%",
				t => { Main.caveParallax = t; PersistSoft(); });

			Slider(list, WeNeoCat.Audio, audio, "music", () => Nz(Menu(98), "Music"),
				() => Main.musicVolume, () => Pct(Main.musicVolume), t => { Main.musicVolume = t; PersistSoft(); });
			Slider(list, WeNeoCat.Audio, audio, "sound", () => Nz(Menu(99), "Sound"),
				() => Main.soundVolume, () => Pct(Main.soundVolume), t => { Main.soundVolume = t; PersistSoft(); });
			Slider(list, WeNeoCat.Audio, audio, "amb", () => Nz(Menu(119), "Ambient"),
				() => Main.ambientVolume, () => Pct(Main.ambientVolume), t => { Main.ambientVolume = t; PersistSoft(); });
			Slider(list, WeNeoCat.Audio, audio, "menumusic", () => WeText.UI("MainMenuMusic"),
				() => DieWithASmileSettings.MenuMusicVolume,
				() => Pct(DieWithASmileSettings.MenuMusicVolume),
				t => {
					DieWithASmileSave.Data.MenuMusicVolume = t;
					DieWithASmileSave.Save();
				});
			Toggle(list, WeNeoCat.Audio, audio, "mute", () => WeText.UI("MuteUnfocused"),
				() => WeSave.Data.MuteWhenUnfocused,
				v => WeSettings.ToggleMuteUnfocused());

			Toggle(list, WeNeoCat.Interface, g, "pickup", () => Nz(Menu(71), "Pickup text"),
				() => Main.showItemText, v => { Main.showItemText = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Interface, g, "event", () => L("UI.EventProgressBar", "Event progress"),
				() => Main.invasionProgressMode.ToString(),
				() => { Main.invasionProgressMode = (Main.invasionProgressMode + 1) % 3; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, g, "place", () => L("UI.PlacementPreview", "Placement preview"),
				() => Main.placementPreview, v => { Main.placementPreview = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, g, "newitem", () => L("UI.HighlightNewItems", "Highlight new items"),
				() => WeNeoFld.GetBool(typeof(Main), "HighlightNewItems"),
				v => WeNeoFld.SetBool(typeof(Main), "HighlightNewItems", v));
			Toggle(list, WeNeoCat.Interface, g, "grid", () => L("UI.TileGrid", "Tile grid"),
				() => Main.MouseShowBuildingGrid, v => { Main.MouseShowBuildingGrid = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, g, "gamepad", () => L("UI.GamepadInstructions", "Gamepad hints"),
				() => !Main.GamepadDisableInstructionsDisplay,
				v => { Main.GamepadDisableInstructionsDisplay = !v; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, g, "tips", () => L("UI.HoverTextBoxes", "Hover text boxes"),
				() => Main.SettingsEnabled_OpaqueBoxBehindTooltips,
				v => { Main.SettingsEnabled_OpaqueBoxBehindTooltips = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Interface, g, "minimap", () => L("UI.SelectMapBorder", "Minimap border"),
				() => Main.MinimapFrameManagerInstance?.ActiveSelectionKeyName ?? "",
				() => {
					try {
						Main.MinimapFrameManagerInstance.CycleSelection();
					}
					catch {
					}

					PersistSoft();
				});
			Slider(list, WeNeoCat.Interface, g, "mapscale", () => L("UI.MapScale", "Map scale"),
				() => MathHelper.Clamp((Main.MapScale - 0.5f) / 0.5f, 0f, 1f),
				() => (int)Math.Round(Main.MapScale * 100f) + "%",
				t => { Main.MapScale = 0.5f + t * 0.5f; PersistSoft(); });
			Cycle(list, WeNeoCat.Interface, g, "bars", () => L("UI.SelectHealthStyle", "Health and mana"),
				() => Main.ResourceSetsManager?.ActiveSet?.DisplayedName ?? "",
				() => {
					WeNeoFld.Call(Main.ResourceSetsManager, "CycleSelection");
					PersistSoft();
				});
			Cycle(list, WeNeoCat.Interface, g, "bossbar", () => L("tModLoader.BossBarStyle", "Boss bar"),
				() => BossBarText(),
				() => {
					try {
						object text = WeNeoFld.CallStatic(typeof(BossBarLoader), "InsertMenu", out object onClick);
						if (onClick is Action act)
							act();
					}
					catch {
					}
				});

			Slider(list, WeNeoCat.Cursor, g, "cr", () => WeText.UI("Red"),
				() => Main.mouseColor.R / 255f, () => Main.mouseColor.R.ToString(),
				t => { Main.mouseColor = new Color((int)(t * 255), Main.mouseColor.G, Main.mouseColor.B); PersistSoft(); });
			Slider(list, WeNeoCat.Cursor, g, "cg", () => WeText.UI("Green"),
				() => Main.mouseColor.G / 255f, () => Main.mouseColor.G.ToString(),
				t => { Main.mouseColor = new Color(Main.mouseColor.R, (int)(t * 255), Main.mouseColor.B); PersistSoft(); });
			Slider(list, WeNeoCat.Cursor, g, "cb", () => WeText.UI("Blue"),
				() => Main.mouseColor.B / 255f, () => Main.mouseColor.B.ToString(),
				t => { Main.mouseColor = new Color(Main.mouseColor.R, Main.mouseColor.G, (int)(t * 255)); PersistSoft(); });
			Toggle(list, WeNeoCat.Cursor, g, "smart", () => L("LegacyMenu.370", "Smart cursor toggle"),
				() => Main.cSmartCursorModeIsToggleAndNotHold,
				v => { Main.cSmartCursorModeIsToggleAndNotHold = v; PersistSoft(); });
			Button(list, WeNeoCat.Cursor, g, "ccolor", () => WeText.UI("NeoOpenCursorColor"),
				() => "", () => WeNeoDeep.OpenCursorColor());
			Button(list, WeNeoCat.Cursor, g, "cborder", () => WeText.UI("NeoOpenCursorBorder"),
				() => "", () => WeNeoDeep.OpenCursorBorder());

			Toggle(list, WeNeoCat.Controls, g, "trash", () => L("UI.QuickTrash", "Quick trash"),
				() => !WeNeoFld.GetBool(typeof(Main), "DisableQuickTrash"),
				v => WeNeoFld.SetBool(typeof(Main), "DisableQuickTrash", !v));
			Toggle(list, WeNeoCat.Controls, g, "shifttrash", () => L("UI.DisableLeftShiftTrashCan", "Shift trash"),
				() => !WeNeoFld.GetBool(typeof(Main), "DisableLeftShiftTrashCan"),
				v => WeNeoFld.SetBool(typeof(Main), "DisableLeftShiftTrashCan", !v));
			Button(list, WeNeoCat.Controls, g, "keys", () => WeText.UI("NeoOpenKeybinds"),
				() => ">", () => WeNeoDeep.OpenKeybinds());

			Button(list, WeNeoCat.Mods, g, "mods", () => WeText.UI("NeoOpenMods"),
				() => ">", () => WeNeoDeep.OpenMods());
			Button(list, WeNeoCat.Mods, g, "packs", () => WeText.UI("NeoOpenPacks"),
				() => ">", () => WeNeoDeep.OpenPacks());
			Button(list, WeNeoCat.Mods, g, "tml", () => WeText.UI("NeoOpenTml"),
				() => ">", () => WeNeoDeep.OpenTmlSettings());
			return list;
		}

		private static string Pct(float v) => (int)Math.Round(MathHelper.Clamp(v, 0f, 1f) * 100f) + "%";

		private static string Nz(string a, string b) => string.IsNullOrWhiteSpace(a) ? b : a;

		private static string HoverLabel()
		{
			try {
				return Player.Settings.HoverControl.ToString();
			}
			catch {
				return "";
			}
		}

		private static string BossBarText()
		{
			object result = WeNeoFld.CallStatic(typeof(BossBarLoader), "InsertMenu", out _);
			return result as string ?? "";
		}

		private static void CycleHover(int delta)
		{
			try {
				object cur = Player.Settings.HoverControl;
				Array values = cur.GetType().GetEnumValues();
				int n = values.Length;
				int i = Convert.ToInt32(cur);
				object next = values.GetValue((i + delta + n) % n);
				typeof(Player.Settings).GetProperty("HoverControl")?.SetValue(null, next);
			}
			catch {
			}

			PersistSoft();
		}

		private static void CycleRes(int delta)
		{
			if (Main.numDisplayModes <= 0)
				return;
			int found = 0;
			int w = Main.PendingResolutionWidth > 0 ? Main.PendingResolutionWidth : Main.screenWidth;
			int h = Main.PendingResolutionHeight > 0 ? Main.PendingResolutionHeight : Main.screenHeight;
			for (int i = 0; i < Main.numDisplayModes; i++) {
				if (Main.displayWidth[i] == w && Main.displayHeight[i] == h)
					found = i;
			}

			found = (found + delta + Main.numDisplayModes) % Main.numDisplayModes;
			Main.PendingResolutionWidth = Main.displayWidth[found];
			Main.PendingResolutionHeight = Main.displayHeight[found];
			PersistSoft();
		}

		private static void Toggle(List<WeNeoOpt> list, WeNeoCat cat, string section, string id, Func<string> label, Func<bool> get, Action<bool> set)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Toggle, Section = section, Id = id,
				Label = label, On = get, Value = () => OnOff(get()),
				Click = () => set(!get())
			});
		}

		private static void Cycle(List<WeNeoOpt> list, WeNeoCat cat, string section, string id, Func<string> label, Func<string> value, Action click, Action right = null)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Cycle, Section = section, Id = id,
				Label = label, Value = value, Click = click, Right = right
			});
		}

		private static void Slider(List<WeNeoOpt> list, WeNeoCat cat, string section, string id, Func<string> label, Func<float> amount, Func<string> value, Action<float> slide)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Slider, Section = section, Id = id,
				Label = label, Amount = amount, Value = value, Slide = slide
			});
		}

		private static void Button(List<WeNeoOpt> list, WeNeoCat cat, string section, string id, Func<string> label, Func<string> value, Action click)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Button, Section = section, Id = id,
				Label = label, Value = value, Click = click
			});
		}
	}

	internal static class WeNeoFld
	{
		internal static bool GetBool(Type type, string name, bool fallback = false)
		{
			object v = Get(type, name);
			return v is bool b ? b : fallback;
		}

		internal static void SetBool(Type type, string name, bool value)
		{
			Set(type, name, value);
			try {
				Main.SaveSettings();
			}
			catch {
			}
		}

		internal static object Get(Type type, string name)
		{
			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (f != null)
				return f.GetValue(null);
			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			return p?.GetValue(null);
		}

		internal static object Get(string typeName, string name)
		{
			Type type = typeof(Main).Assembly.GetType("Terraria.GameContent." + typeName)
			            ?? typeof(Main).Assembly.GetType("Terraria." + typeName)
			            ?? typeof(ModLoader).Assembly.GetType("Terraria.GameContent." + typeName);
			return type == null ? null : Get(type, name);
		}

		internal static void Set(Type type, string name, object value)
		{
			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (f != null) {
				f.SetValue(null, value);
				return;
			}

			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (p != null && p.CanWrite)
				p.SetValue(null, value);
		}

		internal static void Cycle(string typeName, string name, int delta)
		{
			Type type = typeof(Main).Assembly.GetType("Terraria.GameContent." + typeName)
			            ?? typeof(Main).Assembly.GetType("Terraria." + typeName);
			if (type == null)
				return;
			object cur = Get(type, name);
			if (cur == null)
				return;
			Array values = cur.GetType().GetEnumValues();
			int n = values.Length;
			int i = Convert.ToInt32(cur);
			Set(type, name, values.GetValue((i + delta + n) % n));
			try {
				Main.SaveSettings();
			}
			catch {
			}
		}

		internal static object Call(object target, string method)
		{
			if (target == null)
				return null;
			MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			return m?.Invoke(target, null);
		}

		internal static object CallStatic(Type type, string method, out object extra)
		{
			extra = null;
			foreach (MethodInfo m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
				if (m.Name != method)
					continue;
				ParameterInfo[] pars = m.GetParameters();
				object[] args = new object[pars.Length];
				try {
					object result = m.Invoke(null, args);
					if (pars.Length > 0)
						extra = args[0];
					return result;
				}
				catch {
				}
			}

			return null;
		}
	}
}
