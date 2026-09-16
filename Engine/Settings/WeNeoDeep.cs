using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoDeep
	{
		internal static void OpenKeybinds()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Controls);
			if (Main.gameMenu)
				Main.menuMode = MenuID.KeybindSettings;
			else {
				IngameOptions.Close();
				IngameFancyUI.OpenKeybinds();
			}
		}

		internal static void OpenLanguage()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Game);
			Main.menuMode = MenuID.LanguageSelect;
		}

		internal static void OpenCursorColor()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Cursor);
			Main.menuMode = MenuID.CursorColor;
		}

		internal static void OpenCursorBorder()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Cursor);
			Main.menuMode = MenuID.CursorEdgeColor;
		}

		internal static void OpenMods()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Mods);
			if (!OpenInterface("modsMenu", "modsMenuID"))
				Main.menuMode = 888;
		}

		internal static void OpenBrowser()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Mods);
			if (!OpenInterface("modBrowser", "modBrowserID") &&
			    !OpenInterface("modBrowserMenu", "modBrowserMenuID"))
				Main.menuMode = 888;
		}

		internal static void OpenModPacks()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Mods);
			if (!OpenInterface("modPacksMenu", "modPacksMenuID") &&
			    !OpenInterface("modPacks", "modPacksID"))
				Main.menuMode = 888;
		}

		internal static void OpenTmlSettings()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Mods);
			OpenInterface("tModLoaderSettings", "tModLoaderSettingsID");
		}

		internal static void OpenPacks()
		{
			WeNeoMenu.BeginDeepLink(WeNeoCat.Mods);
			try {
				Type type = typeof(UIResourcePackSelectionMenu);
				ConstructorInfo ctor = null;
				foreach (ConstructorInfo c in type.GetConstructors()) {
					if (ctor == null || c.GetParameters().Length > ctor.GetParameters().Length)
						ctor = c;
				}

				if (ctor == null)
					return;
				ParameterInfo[] pars = ctor.GetParameters();
				object[] args = new object[pars.Length];
				for (int i = 0; i < pars.Length; i++) {
					Type pt = pars[i].ParameterType;
					if (typeof(UIState).IsAssignableFrom(pt))
						args[i] = null;
					else if (pt.Name.Contains("AssetSource"))
						args[i] = typeof(Main).GetProperty("AssetSourceController", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)?.GetValue(Main.instance);
					else if (pt.Name.Contains("ResourcePack")) {
						object ctrl = typeof(Main).GetProperty("AssetSourceController", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)?.GetValue(Main.instance);
						args[i] = ctrl?.GetType().GetProperty("ActiveResourcePackList")?.GetValue(ctrl);
					}
					else
						args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
				}

				if (ctor.Invoke(args) is UIState state)
					OpenState(state);
			}
			catch {
			}
		}

		private static bool OpenInterface(string stateName, string idName)
		{
			try {
				Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
				if (iface == null)
					return false;
				const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
				object state = iface.GetField(stateName, flags)?.GetValue(null);
				if (state is UIState ui) {
					OpenState(ui);
					return true;
				}

				object id = iface.GetField(idName, flags)?.GetValue(null);
				if (id is int mode) {
					Main.menuMode = mode;
					return true;
				}
			}
			catch {
			}

			return false;
		}

		private static void OpenState(UIState state)
		{
			if (state == null)
				return;
			if (Main.gameMenu) {
				Main.MenuUI.SetState(state);
				Main.menuMode = 888;
			}
			else
				IngameFancyUI.OpenUIState(state);
		}
	}
}
