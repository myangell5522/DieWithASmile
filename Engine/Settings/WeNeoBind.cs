using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
		Button,
		Preview
	}

	internal sealed class WeNeoOpt
	{
		internal WeNeoCat Cat;
		internal WeNeoKind Kind;
		internal Func<string> Section;
		internal string Id;
		internal Func<string> Label;
		internal Func<string> Value;
		internal Func<bool> On;
		internal Func<float> Amount;
		internal Action Click;
		internal Action Right;
		internal Action<float> Slide;
		internal Func<bool> Show = () => true;
		internal bool Fat;
		internal int ExtraH;
		internal Action<SpriteBatch, Rectangle, int, float> ExtraDraw;
		internal Func<Rectangle, int, bool, bool, bool> ExtraClick;
	}

	internal static class WeNeoBind
	{
		private static List<WeNeoOpt> _all;
		private static bool _videoDirty;

		internal static bool NeedApply => _videoDirty;

		internal static IReadOnlyList<WeNeoOpt> All => _all ??= Build();

		internal static void ClearDirty() => _videoDirty = false;

		internal static Color BorderColor
		{
			get
			{
				object v = WeNeoFld.Get(typeof(Main), "MouseBorderColor") ?? WeNeoFld.Get(typeof(Main), "mouseBorderColor");
				return v is Color c ? c : Color.Black;
			}
			set
			{
				WeNeoFld.Set(typeof(Main), "MouseBorderColor", value);
				WeNeoFld.Set(typeof(Main), "mouseBorderColor", value);
			}
		}

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
				if (searching && !WeNeoShell.Matches(q, opt.Label?.Invoke() ?? "", opt.Value?.Invoke() ?? "", opt.Section(), opt.Id))
					continue;
				if (opt.Kind != WeNeoKind.Header && last != opt.Section()) {
					WeNeoShell.Header(spriteBatch, view, ref y, opt.Section(), fade);
					last = opt.Section();
				}

				switch (opt.Kind) {
					case WeNeoKind.Header:
						WeNeoShell.Header(spriteBatch, view, ref y, opt.Label(), fade);
						last = opt.Section();
						break;
					case WeNeoKind.Toggle:
						WeNeoShell.Toggle(spriteBatch, view, ref y, opt.Label(), opt.On(), fade);
						break;
					case WeNeoKind.Slider:
						WeNeoShell.Slider(spriteBatch, view, ref y, opt.Label(), opt.Amount(), opt.Value(), fade, opt.Fat);
						break;
					case WeNeoKind.Preview:
						break;
					default:
						WeNeoShell.Cycle(spriteBatch, view, ref y, opt.Label(), opt.Value(), fade);
						break;
				}

				if (opt.ExtraDraw != null) {
					opt.ExtraDraw(spriteBatch, view, y, fade);
					y += opt.ExtraH;
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
				if (searching && !WeNeoShell.Matches(q, opt.Label?.Invoke() ?? "", opt.Value?.Invoke() ?? "", opt.Section(), opt.Id))
					continue;
				if (opt.Kind == WeNeoKind.Header) {
					WeNeoShell.SkipHeader(ref y);
					last = opt.Section();
					continue;
				}

				if (last != opt.Section()) {
					WeNeoShell.SkipHeader(ref y);
					last = opt.Section();
				}

				if (opt.Kind == WeNeoKind.Preview) {
					bool used = opt.ExtraClick != null && opt.ExtraClick(view, y, left, right);
					y += opt.ExtraH;
					if (used) {
						Tick();
						return;
					}

					continue;
				}

				if (opt.Kind == WeNeoKind.Slider) {
					Rectangle bar = WeNeoShell.SliderBar(view, y, opt.Fat);
					y += WeNeoShell.RowStep;
					if (left && bar.Contains(Main.mouseX, Main.mouseY)) {
						WeNeoMenu.SetDrag(opt.Id);
						Slide(opt.Id);
						return;
					}

					if (opt.ExtraH > 0)
						y += opt.ExtraH;
					continue;
				}

				bool hit = WeNeoShell.HitRow(view, ref y);
				y += WeNeoShell.RowStep;
				if (hit) {
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

				if (opt.ExtraH > 0) {
					bool used = opt.ExtraClick != null && opt.ExtraClick(view, y, left, right);
					y += opt.ExtraH;
					if (used) {
						Tick();
						return;
					}
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
				Rectangle bar = WeNeoShell.SliderBar(view, 0, opt.Fat);
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

			_videoDirty = false;
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

		private static void MarkVideo()
		{
			_videoDirty = Main.PendingResolutionWidth != Main.screenWidth ||
			              Main.PendingResolutionHeight != Main.screenHeight;
		}

		private static string T(string key) => WeText.UI(key);

		private static List<WeNeoOpt> Build()
		{
			var list = new List<WeNeoOpt>();
			Func<string> g = () => T("NeoSectionGeneral");
			Func<string> qlt = () => T("NeoSectionQuality");
			Func<string> audio = () => T("NeoCatAudio");
			Func<string> hud = () => T("NeoSectionHud");
			Func<string> color = () => T("NeoSectionColor");
			Func<string> aim = () => T("NeoSectionSmart");
			Func<string> keys = () => T("NeoCatControls");
			Func<string> mods = () => T("NeoCatMods");

			Toggle(list, WeNeoCat.Game, g, "autosave", () => T("NeoAutosave"), () => Main.autoSave, v => { Main.autoSave = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "autopause", () => T("NeoAutopause"), () => Main.autoPause, v => { Main.autoPause = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "map", () => T("NeoMap"), () => Main.mapEnabled, v => { Main.mapEnabled = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Game, g, "pass", () => T("NeoHidePasswords"), () => Main.HidePassword, v => { Main.HidePassword = v; PersistSoft(); });
			MaybeUnfocused(list, g);
			Cycle(list, WeNeoCat.Game, g, "bonus", () => T("NeoSetBonus"),
				() => T(WeNeoFld.GetBool(typeof(Main), "ReverseUpDownForArmorSetBonuses") ? "NeoUp" : "NeoDown"),
				() => WeNeoFld.SetBool(typeof(Main), "ReverseUpDownForArmorSetBonuses",
					!WeNeoFld.GetBool(typeof(Main), "ReverseUpDownForArmorSetBonuses")));
			Toggle(list, WeNeoCat.Game, g, "autofire", () => T("NeoAutofire"),
				() => Main.SettingsEnabled_AutoReuseAllItems,
				v => { Main.SettingsEnabled_AutoReuseAllItems = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Game, g, "doors", () => T("NeoSmartDoors"),
				() => Word(WeNeoFld.Get("DoorOpeningHelper", "Preference")?.ToString()),
				() => WeNeoFld.Cycle("DoorOpeningHelper", "Preference", 1),
				() => WeNeoFld.Cycle("DoorOpeningHelper", "Preference", -1));
			Cycle(list, WeNeoCat.Game, g, "hover", () => T("NeoHover"),
				() => HoverLabel(),
				() => CycleHover(1), () => CycleHover(-1));
			Slider(list, WeNeoCat.Game, g, "zoom", () => T("NeoZoom"),
				() => (Main.GameZoomTarget - 1f) / 1f,
				() => (int)Math.Round(Main.GameZoomTarget * 100f) + "%",
				t => { Main.GameZoomTarget = 1f + t; PersistSoft(); });
			Slider(list, WeNeoCat.Game, g, "uiscale", () => T("NeoUIScale"),
				() => MathHelper.Clamp((Main.UIScaleWanted - 0.5f) / 1.5f, 0f, 1f),
				() => (int)Math.Round(Main.UIScaleWanted * 100f) + "%",
				t => { Main.UIScale = 0.5f + t * 1.5f; PersistSoft(); });
			Cycle(list, WeNeoCat.Game, g, "lang", () => T("NeoLanguage"),
				() => CultureName(),
				() => CycleLanguage(1), () => CycleLanguage(-1));

			Toggle(list, WeNeoCat.Video, qlt, "full", () => T("NeoFullscreen"),
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
			Toggle(list, WeNeoCat.Video, qlt, "borderless", () => T("NeoBorderless"),
				() => Main.screenBorderless,
				v => {
					Main.screenBorderless = v;
					_videoDirty = true;
					PersistSoft();
				});
			Cycle(list, WeNeoCat.Video, qlt, "res", () => T("NeoResolution"),
				() => (Main.PendingResolutionWidth > 0 ? Main.PendingResolutionWidth : Main.screenWidth) + "x" +
				      (Main.PendingResolutionHeight > 0 ? Main.PendingResolutionHeight : Main.screenHeight),
				() => CycleRes(1), () => CycleRes(-1));
			Cycle(list, WeNeoCat.Video, qlt, "skip", () => T("NeoFrameSkip"),
				() => Word(Main.FrameSkipMode.ToString()),
				() => { Main.CycleFrameSkipMode(); PersistSoft(); });
			var light = Cycle(list, WeNeoCat.Video, qlt, "light", () => T("NeoLighting"),
				() => Word(Lighting.Mode.ToString()),
				() => {
					Lighting.Mode = (LightMode)(((int)Lighting.Mode + 1) % 4);
					PersistSoft();
				});
			light.ExtraH = 36;
			light.ExtraDraw = WeNeoShell.LightPreview;
			light.ExtraClick = (view, y, left, right) => WeNeoShell.LightPreviewClick(view, y, left);
			Cycle(list, WeNeoCat.Video, qlt, "qual", () => T("NeoQuality"),
				() => Qual(Main.qaStyle),
				() => { Main.qaStyle = (Main.qaStyle + 1) % 4; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "bg", () => T("NeoBackgrounds"),
				() => Main.BackgroundEnabled, v => { Main.BackgroundEnabled = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "gore", () => T("NeoGore"),
				() => ChildSafety.Disabled, v => { ChildSafety.Disabled = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "wobble", () => T("NeoWobble"),
				() => Main.SettingsEnabled_MinersWobble, v => { Main.SettingsEnabled_MinersWobble = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "heat", () => T("NeoHeat"),
				() => Main.UseHeatDistortion, v => { Main.UseHeatDistortion = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "storm", () => T("NeoStorm"),
				() => Main.UseStormEffects, v => { Main.UseStormEffects = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Video, qlt, "waves", () => T("NeoWaves"),
				() => Wave(Main.WaveQuality),
				() => { Main.WaveQuality = (Main.WaveQuality + 1) % 4; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "wind", () => T("NeoWind"),
				() => Main.SettingsEnabled_TilesSwayInWind, v => { Main.SettingsEnabled_TilesSwayInWind = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Video, qlt, "intense", () => T("NeoIntense"),
				() => !Main.DisableIntenseVisualEffects, v => { Main.DisableIntenseVisualEffects = !v; PersistSoft(); });
			Slider(list, WeNeoCat.Video, qlt, "par", () => T("NeoParallax"),
				() => Main.caveParallax,
				() => (int)Math.Round(Main.caveParallax * 100f) + "%",
				t => { Main.caveParallax = t; PersistSoft(); });

			Slider(list, WeNeoCat.Audio, audio, "music", () => T("NeoMusic"),
				() => Main.musicVolume, () => Pct(Main.musicVolume), t => { Main.musicVolume = t; PersistSoft(); }, true);
			Slider(list, WeNeoCat.Audio, audio, "sound", () => T("NeoSound"),
				() => Main.soundVolume, () => Pct(Main.soundVolume), t => { Main.soundVolume = t; PersistSoft(); }, true);
			Slider(list, WeNeoCat.Audio, audio, "amb", () => T("NeoAmbient"),
				() => Main.ambientVolume, () => Pct(Main.ambientVolume), t => { Main.ambientVolume = t; PersistSoft(); }, true);
			Slider(list, WeNeoCat.Audio, audio, "menumusic", () => WeText.UI("MainMenuMusic"),
				() => DieWithASmileSettings.MenuMusicVolume,
				() => Pct(DieWithASmileSettings.MenuMusicVolume),
				t => {
					DieWithASmileSave.Data.MenuMusicVolume = t;
					DieWithASmileSave.Save();
				}, true);
			Toggle(list, WeNeoCat.Audio, audio, "mute", () => WeText.UI("MuteUnfocused"),
				() => WeSave.Data.MuteWhenUnfocused,
				v => WeSettings.ToggleMuteUnfocused());

			Toggle(list, WeNeoCat.Interface, hud, "pickup", () => T("NeoPickup"),
				() => Main.showItemText, v => { Main.showItemText = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Interface, hud, "event", () => T("NeoEventBar"),
				() => EventBar(Main.invasionProgressMode),
				() => { Main.invasionProgressMode = (Main.invasionProgressMode + 1) % 3; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, hud, "place", () => T("NeoPlacement"),
				() => Main.placementPreview, v => { Main.placementPreview = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, hud, "newitem", () => T("NeoHighlightNew"),
				() => WeNeoFld.GetBool(typeof(Main), "HighlightNewItems"),
				v => WeNeoFld.SetBool(typeof(Main), "HighlightNewItems", v));
			Toggle(list, WeNeoCat.Interface, hud, "grid", () => T("NeoTileGrid"),
				() => Main.MouseShowBuildingGrid, v => { Main.MouseShowBuildingGrid = v; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, hud, "gamepad", () => T("NeoGamepadHints"),
				() => !Main.GamepadDisableInstructionsDisplay,
				v => { Main.GamepadDisableInstructionsDisplay = !v; PersistSoft(); });
			Toggle(list, WeNeoCat.Interface, hud, "tips", () => T("NeoTooltipBoxes"),
				() => Main.SettingsEnabled_OpaqueBoxBehindTooltips,
				v => { Main.SettingsEnabled_OpaqueBoxBehindTooltips = v; PersistSoft(); });
			Cycle(list, WeNeoCat.Interface, hud, "minimap", () => T("NeoMinimap"),
				() => Main.MinimapFrameManagerInstance?.ActiveSelectionKeyName ?? "",
				() => {
					try {
						Main.MinimapFrameManagerInstance.CycleSelection();
					}
					catch {
					}

					PersistSoft();
				});
			Slider(list, WeNeoCat.Interface, hud, "mapscale", () => T("NeoMapScale"),
				() => MathHelper.Clamp((Main.MapScale - 0.5f) / 0.5f, 0f, 1f),
				() => (int)Math.Round(Main.MapScale * 100f) + "%",
				t => { Main.MapScale = 0.5f + t * 0.5f; PersistSoft(); });
			Cycle(list, WeNeoCat.Interface, hud, "bars", () => T("NeoHealthStyle"),
				() => Main.ResourceSetsManager?.ActiveSet?.DisplayedName ?? "",
				() => {
					WeNeoFld.Call(Main.ResourceSetsManager, "CycleSelection");
					PersistSoft();
				});
			Cycle(list, WeNeoCat.Interface, hud, "bossbar", () => T("NeoBossBar"),
				() => BossBarText(),
				() => {
					try {
						WeNeoFld.CallStatic(typeof(BossBarLoader), "InsertMenu", out object onClick);
						if (onClick is Action act)
							act();
					}
					catch {
					}
				});

			Preview(list, WeNeoCat.Cursor, color, "cprev", 48, WeNeoShell.CursorPreview, WeNeoShell.CursorPreviewClick);
			Slider(list, WeNeoCat.Cursor, color, "cr", () => WeText.UI("Red"),
				() => CursorRgb().R / 255f, () => CursorRgb().R.ToString(),
				t => SetCursorRgb(r: (int)(t * 255)));
			Slider(list, WeNeoCat.Cursor, color, "cg", () => WeText.UI("Green"),
				() => CursorRgb().G / 255f, () => CursorRgb().G.ToString(),
				t => SetCursorRgb(g: (int)(t * 255)));
			Slider(list, WeNeoCat.Cursor, color, "cb", () => WeText.UI("Blue"),
				() => CursorRgb().B / 255f, () => CursorRgb().B.ToString(),
				t => SetCursorRgb(b: (int)(t * 255)));
			Cycle(list, WeNeoCat.Cursor, aim, "smart", () => T("NeoSmartCursor"),
				() => T(Main.cSmartCursorModeIsToggleAndNotHold ? "NeoToggle" : "NeoHold"),
				() => { Main.cSmartCursorModeIsToggleAndNotHold = !Main.cSmartCursorModeIsToggleAndNotHold; PersistSoft(); });
			MaybeSmart(list, aim, "blocks", "NeoSmartBlocks", "SmartBlocksEnabled", "UseSmartCursorForCommonBlocks");
			MaybeSmart(list, aim, "axe", "NeoSmartAxe", "SmartAxeAfterPickaxe", "UseSmartAxeAfterPickaxe");
			MaybeLockOn(list, aim);
			Button(list, WeNeoCat.Cursor, color, "ccolor", () => T("NeoOpenCursorColor"),
				() => T("NeoHslHint"), () => WeNeoDeep.OpenCursorColor());
			Button(list, WeNeoCat.Cursor, color, "cborder", () => T("NeoOpenCursorBorder"),
				() => T("NeoHslHint"), () => WeNeoDeep.OpenCursorBorder());

			Toggle(list, WeNeoCat.Controls, keys, "trash", () => T("NeoQuickTrash"),
				() => !WeNeoFld.GetBool(typeof(Main), "DisableQuickTrash"),
				v => WeNeoFld.SetBool(typeof(Main), "DisableQuickTrash", !v));
			Toggle(list, WeNeoCat.Controls, keys, "shifttrash", () => T("NeoShiftTrash"),
				() => !WeNeoFld.GetBool(typeof(Main), "DisableLeftShiftTrashCan"),
				v => WeNeoFld.SetBool(typeof(Main), "DisableLeftShiftTrashCan", !v));
			Button(list, WeNeoCat.Controls, keys, "keys", () => T("NeoOpenKeybinds"),
				() => ">", () => WeNeoDeep.OpenKeybinds());

			Button(list, WeNeoCat.Mods, mods, "mods", () => T("NeoOpenMods"),
				() => ">", () => WeNeoDeep.OpenMods());
			Button(list, WeNeoCat.Mods, mods, "browser", () => T("NeoOpenBrowser"),
				() => ">", () => WeNeoDeep.OpenBrowser());
			Button(list, WeNeoCat.Mods, mods, "packs", () => T("NeoOpenPacks"),
				() => ">", () => WeNeoDeep.OpenPacks());
			Button(list, WeNeoCat.Mods, mods, "modpacks", () => T("NeoOpenModPacks"),
				() => ">", () => WeNeoDeep.OpenModPacks());
			Button(list, WeNeoCat.Mods, mods, "tml", () => T("NeoOpenTml"),
				() => ">", () => WeNeoDeep.OpenTmlSettings());
			return list;
		}

		private static void MaybeUnfocused(List<WeNeoOpt> list, Func<string> section)
		{
			string[] names = { "PauseWhenUnfocused", "pauseWhenUnfocused", "SettingsPauseWhenLostFocus", "PauseOnLostFocus" };
			Type[] types = {
				typeof(Main),
				typeof(ModLoader),
				typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.ModLoader")
			};
			foreach (Type type in types) {
				if (type == null)
					continue;
				foreach (string name in names) {
					if (!WeNeoFld.TryGetBool(type, name, out bool _))
						continue;
					Toggle(list, WeNeoCat.Game, section, "unfocus", () => T("NeoPauseUnfocused"),
						() => WeNeoFld.GetBool(type, name),
						v => WeNeoFld.SetBool(type, name, v));
					return;
				}
			}
		}

		private static void MaybeSmart(List<WeNeoOpt> list, Func<string> section, string id, string key, params string[] fields)
		{
			Type nested = typeof(Main).GetNestedType("SmartCursorSettings", BindingFlags.Public | BindingFlags.NonPublic);
			Type[] types = { nested, typeof(Main) };
			foreach (Type type in types) {
				if (type == null)
					continue;
				foreach (string field in fields) {
					if (!WeNeoFld.TryGetBool(type, field, out bool _))
						continue;
					Type captured = type;
					string capturedField = field;
					Toggle(list, WeNeoCat.Cursor, section, id, () => T(key),
						() => WeNeoFld.GetBool(captured, capturedField),
						v => WeNeoFld.SetBool(captured, capturedField, v));
					return;
				}
			}

			foreach (string field in fields) {
				if (WeNeoFld.Get("SmartCursorSettings", field) is not bool)
					continue;
				string capturedField = field;
				Toggle(list, WeNeoCat.Cursor, section, id, () => T(key),
					() => WeNeoFld.GetBool("SmartCursorSettings", capturedField),
					v => WeNeoFld.Set("SmartCursorSettings", capturedField, v));
				return;
			}
		}

		private static void MaybeLockOn(List<WeNeoOpt> list, Func<string> section)
		{
			Type type = typeof(Main).Assembly.GetType("Terraria.GameInput.LockOnHelper")
			            ?? typeof(Main).Assembly.GetType("Terraria.LockOnHelper");
			if (type == null)
				return;
			string[] names = { "UseMode", "LockOnMode", "Mode" };
			foreach (string name in names) {
				object cur = WeNeoFld.Get(type, name);
				if (cur == null || !cur.GetType().IsEnum)
					continue;
				Cycle(list, WeNeoCat.Cursor, section, "lockon", () => T("NeoLockOn"),
					() => Word(WeNeoFld.Get(type, name)?.ToString()),
					() => WeNeoFld.CycleType(type, name, 1),
					() => WeNeoFld.CycleType(type, name, -1));
				return;
			}
		}

		private static Color CursorRgb() => WeNeoMenu.CursorChip == 1 ? BorderColor : Main.mouseColor;

		private static void SetCursorRgb(int r = -1, int g = -1, int b = -1)
		{
			Color c = CursorRgb();
			if (r >= 0)
				c.R = (byte)r;
			if (g >= 0)
				c.G = (byte)g;
			if (b >= 0)
				c.B = (byte)b;
			if (WeNeoMenu.CursorChip == 1)
				BorderColor = c;
			else
				Main.mouseColor = c;
			PersistSoft();
		}

		private static string Pct(float v) => (int)Math.Round(MathHelper.Clamp(v, 0f, 1f) * 100f) + "%";

		private static string Qual(int n) => T(n switch { 0 => "NeoHigh", 1 => "NeoMedium", 2 => "NeoLow", _ => "NeoAuto" });

		private static string Wave(int n) => T(n switch { 0 => "NeoOff", 1 => "NeoLow", 2 => "NeoMedium", _ => "NeoHigh" });

		private static string EventBar(int n) => T(n switch { 0 => "NeoOff", 1 => "NeoTimed", _ => "NeoOn" });

		private static string Word(string raw)
		{
			string s = (raw ?? "").Replace(" ", "").Replace("_", "");
			if (s.Length == 0)
				return "";
			if (s.Contains("Hold", StringComparison.OrdinalIgnoreCase))
				return T("NeoHold");
			if (s.Contains("Click", StringComparison.OrdinalIgnoreCase))
				return T("NeoClick");
			if (s.Contains("Toggle", StringComparison.OrdinalIgnoreCase))
				return T("NeoToggle");
			if (s.Contains("Subtle", StringComparison.OrdinalIgnoreCase))
				return T("NeoSubtle");
			if (s.Contains("Gamepad", StringComparison.OrdinalIgnoreCase))
				return T("NeoGamepad");
			if (s.Contains("Trippy", StringComparison.OrdinalIgnoreCase))
				return T("NeoLightTrippy");
			if (s.Contains("Retro", StringComparison.OrdinalIgnoreCase))
				return T("NeoLightRetro");
			if (s.Contains("White", StringComparison.OrdinalIgnoreCase))
				return T("NeoLightWhite");
			if (s.Equals("Color", StringComparison.OrdinalIgnoreCase))
				return T("NeoLightColor");
			if (s.Contains("Disabled", StringComparison.OrdinalIgnoreCase) || s.Equals("Off", StringComparison.OrdinalIgnoreCase))
				return T("NeoOff");
			if (s.Contains("Enabled", StringComparison.OrdinalIgnoreCase) || s.Equals("On", StringComparison.OrdinalIgnoreCase))
				return T("NeoOn");
			if (s.Contains("Auto", StringComparison.OrdinalIgnoreCase))
				return T("NeoAuto");
			if (s.Contains("High", StringComparison.OrdinalIgnoreCase))
				return T("NeoHigh");
			if (s.Contains("Medium", StringComparison.OrdinalIgnoreCase) || s.Contains("Mid", StringComparison.OrdinalIgnoreCase))
				return T("NeoMedium");
			if (s.Contains("Low", StringComparison.OrdinalIgnoreCase))
				return T("NeoLow");
			if (s.Contains("Timed", StringComparison.OrdinalIgnoreCase))
				return T("NeoTimed");
			return raw;
		}

		private static string HoverLabel()
		{
			try {
				return Word(Player.Settings.HoverControl.ToString());
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

		private static string CultureName()
		{
			try {
				GameCulture culture = Language.ActiveCulture;
				if (culture == null)
					return "";
				object info = typeof(GameCulture).GetProperty("CultureInfo")?.GetValue(culture);
				if (info is CultureInfo ci)
					return ci.NativeName;
				return culture.Name;
			}
			catch {
				return Language.ActiveCulture?.Name ?? "";
			}
		}

		private static List<GameCulture> Cultures()
		{
			var list = new List<GameCulture>();
			try {
				object raw = WeNeoFld.Get(typeof(GameCulture), "KnownCultures");
				if (raw is IEnumerable e) {
					foreach (object o in e) {
						if (o is GameCulture c)
							list.Add(c);
						else {
							object val = o?.GetType().GetProperty("Value")?.GetValue(o);
							if (val is GameCulture c2)
								list.Add(c2);
						}
					}
				}
			}
			catch {
			}

			return list;
		}

		private static void CycleLanguage(int delta)
		{
			List<GameCulture> list = Cultures();
			if (list.Count == 0) {
				WeNeoDeep.OpenLanguage();
				return;
			}

			int i = 0;
			for (int n = 0; n < list.Count; n++) {
				if (list[n] == Language.ActiveCulture || list[n]?.Name == Language.ActiveCulture?.Name)
					i = n;
			}

			GameCulture next = list[(i + delta + list.Count) % list.Count];
			if (!ApplyCulture(next)) {
				WeNeoDeep.OpenLanguage();
				return;
			}

			PersistSoft();
		}

		private static bool ApplyCulture(GameCulture culture)
		{
			if (culture == null)
				return false;
			try {
				MethodInfo typed = typeof(LanguageManager).GetMethod("SetLanguage", new[] { typeof(GameCulture) });
				if (typed != null) {
					typed.Invoke(LanguageManager.Instance, new object[] { culture });
					return true;
				}

				LanguageManager.Instance.SetLanguage(culture.Name);
				return true;
			}
			catch {
				try {
					LanguageManager.Instance.SetLanguage(culture.Name);
					return true;
				}
				catch {
					return false;
				}
			}
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
			MarkVideo();
			PersistSoft();
		}

		private static void Toggle(List<WeNeoOpt> list, WeNeoCat cat, Func<string> section, string id, Func<string> label, Func<bool> get, Action<bool> set)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Toggle, Section = section, Id = id,
				Label = label, On = get, Value = () => T(get() ? "NeoOn" : "NeoOff"),
				Click = () => set(!get())
			});
		}

		private static WeNeoOpt Cycle(List<WeNeoOpt> list, WeNeoCat cat, Func<string> section, string id, Func<string> label, Func<string> value, Action click, Action right = null)
		{
			var opt = new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Cycle, Section = section, Id = id,
				Label = label, Value = value, Click = click, Right = right
			};
			list.Add(opt);
			return opt;
		}

		private static void Slider(List<WeNeoOpt> list, WeNeoCat cat, Func<string> section, string id, Func<string> label, Func<float> amount, Func<string> value, Action<float> slide, bool fat = false)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Slider, Section = section, Id = id,
				Label = label, Amount = amount, Value = value, Slide = slide, Fat = fat
			});
		}

		private static void Button(List<WeNeoOpt> list, WeNeoCat cat, Func<string> section, string id, Func<string> label, Func<string> value, Action click)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Button, Section = section, Id = id,
				Label = label, Value = value, Click = click
			});
		}

		private static void Preview(List<WeNeoOpt> list, WeNeoCat cat, Func<string> section, string id, int h,
			Action<SpriteBatch, Rectangle, int, float> draw, Func<Rectangle, int, bool, bool> click)
		{
			list.Add(new WeNeoOpt {
				Cat = cat, Kind = WeNeoKind.Preview, Section = section, Id = id,
				Label = () => T("NeoSectionColor"), Value = () => "", ExtraH = h, ExtraDraw = draw,
				ExtraClick = (view, y, left, right) => click(view, y, left)
			});
		}
	}

	internal static class WeNeoFld
	{
		internal static bool TryGetBool(Type type, string name, out bool value)
		{
			object v = Get(type, name);
			if (v is bool b) {
				value = b;
				return true;
			}

			value = false;
			return false;
		}

		internal static bool GetBool(Type type, string name, bool fallback = false)
		{
			object v = Get(type, name);
			return v is bool b ? b : fallback;
		}

		internal static bool GetBool(string typeName, string name, bool fallback = false)
		{
			object v = Get(typeName, name);
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
			if (type == null)
				return null;
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
			if (type == null)
				return;
			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (f != null) {
				f.SetValue(null, value);
				return;
			}

			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (p != null && p.CanWrite)
				p.SetValue(null, value);
		}

		internal static void Set(string typeName, string name, object value)
		{
			Type type = typeof(Main).Assembly.GetType("Terraria.GameContent." + typeName)
			            ?? typeof(Main).Assembly.GetType("Terraria." + typeName);
			if (type != null)
				Set(type, name, value);
			try {
				Main.SaveSettings();
			}
			catch {
			}
		}

		internal static void Cycle(string typeName, string name, int delta)
		{
			Type type = typeof(Main).Assembly.GetType("Terraria.GameContent." + typeName)
			            ?? typeof(Main).Assembly.GetType("Terraria." + typeName);
			if (type != null)
				CycleType(type, name, delta);
		}

		internal static void CycleType(Type type, string name, int delta)
		{
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
