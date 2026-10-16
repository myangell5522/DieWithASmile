using Terraria.Audio;
using Terraria.ID;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Chrome
{
	internal enum WrenchAction
	{
		LogoPos,
		Wallpaper,
		Music,
		Widgets,
		Logo,
		Layout,
		Client,
		Clean
	}

	internal enum WrenchStyle
	{
		Radial = 0,
		Dock = 1,
		Sidebar = 2
	}

	internal static class WrenchHub
	{
		// Six clear destinations. "Move logo only" now lives in the Logo page and "Hide version clutter"
		// in the Widgets page (and the sidebar's quick toggles), so the hub no longer has eight look-alike icons.
		internal static readonly WrenchAction[] Actions =
		{
			WrenchAction.Wallpaper, WrenchAction.Logo, WrenchAction.Music,
			WrenchAction.Widgets, WrenchAction.Layout, WrenchAction.Client
		};

		internal static bool UseDock => WeSave.Data.WrenchStyle == (int)WrenchStyle.Dock;

		internal static bool UseSidebar => WeSave.Data.WrenchStyle == (int)WrenchStyle.Sidebar;

		internal static string LabelKey(WrenchAction action) => action switch {
			WrenchAction.Wallpaper => "TabWallpaper",
			WrenchAction.Music => "TabMusic",
			WrenchAction.Widgets => "TabWidgets",
			WrenchAction.Logo => "TabLogo",
			WrenchAction.Layout => "TabLayout",
			WrenchAction.Client => "TabSettings",
			WrenchAction.LogoPos => "BtnLogoPos",
			_ => "BtnClean"
		};

		internal static void Activate(WrenchAction action)
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			switch (action) {
				case WrenchAction.LogoPos:
					LayoutEditor.Begin(SceneGraph.Logo, logoOnly: true);
					WrenchToolbar.Collapse();
					break;
				case WrenchAction.Wallpaper:
					WePanels.Open(WePanelId.Wallpaper);
					break;
				case WrenchAction.Music:
					WePanels.Open(WePanelId.Music);
					break;
				case WrenchAction.Widgets:
					WePanels.Open(WePanelId.Widgets);
					break;
				case WrenchAction.Logo:
					WePanels.Open(WePanelId.Logo);
					break;
				case WrenchAction.Layout:
					LayoutEditor.Begin();
					WrenchToolbar.Collapse();
					break;
				case WrenchAction.Client:
					WePanels.Open(WePanelId.Client);
					break;
				case WrenchAction.Clean:
					WeSettings.ToggleCleanChrome();
					WeToast.Show(WeSave.Data.CleanChrome ? "ToastCleanOn" : "ToastCleanOff");
					break;
			}
		}

		internal static bool IsOn(WrenchAction action) => action switch {
			WrenchAction.Clean => WeSave.Data.CleanChrome,
			_ => WePanels.Is(PanelOf(action))
		};

		internal static WePanelId PanelOf(WrenchAction action) => action switch {
			WrenchAction.Wallpaper => WePanelId.Wallpaper,
			WrenchAction.Music => WePanelId.Music,
			WrenchAction.Widgets => WePanelId.Widgets,
			WrenchAction.Logo => WePanelId.Logo,
			WrenchAction.Client => WePanelId.Client,
			_ => WePanelId.None
		};

		internal static string IconName(WrenchAction action) => action switch {
			WrenchAction.LogoPos => WeIcons.MoveLogo,
			WrenchAction.Wallpaper => WeIcons.Wallpaper,
			WrenchAction.Music => WeIcons.Music,
			WrenchAction.Widgets => WeIcons.Widget,
			WrenchAction.Logo => WeIcons.Logo,
			WrenchAction.Layout => WeIcons.Layout,
			WrenchAction.Client => WeIcons.Client,
			WrenchAction.Clean => WeIcons.Hide,
			_ => WeIcons.Setting
		};

		internal static string TipKey(WrenchAction action) => action switch {
			WrenchAction.LogoPos => "BtnLogoPos",
			WrenchAction.Wallpaper => "BtnWallpaper",
			WrenchAction.Music => "BtnMusic",
			WrenchAction.Widgets => "BtnWidgets",
			WrenchAction.Logo => "BtnLogo",
			WrenchAction.Layout => "BtnLayout",
			WrenchAction.Client => "BtnClient",
			WrenchAction.Clean => "BtnClean",
			_ => "Wrench"
		};

		internal static int ActiveIndex()
		{
			for (int i = 0; i < Actions.Length; i++) {
				if (IsOn(Actions[i]))
					return i;
			}

			return -1;
		}
	}
}
