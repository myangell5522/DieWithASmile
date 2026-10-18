using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using C = DieWithASmile.Engine.Classic;
using N = DieWithASmile.Engine.UI;
using NC = DieWithASmile.Engine.Chrome;
using NW = DieWithASmile.Engine.Widgets;
using NA = DieWithASmile.Engine.Audio;

namespace DieWithASmile.Engine
{
	/// <summary>
	/// The one place that knows there are two interfaces: Classic (the smooth look up to 3.0.24) and Pixel
	/// (the Terraria pixel-art look from 3.0.25). Every shared system talks to the UI through here, so the
	/// rest of the mod never cares which one is active. Both share the same settings, music and widgets data;
	/// only the drawing and the panels differ.
	/// </summary>
	internal static class WeUi
	{
		internal const int SkinUnchosen = -1;
		internal const int SkinClassic = 0;
		internal const int SkinPixel = 1;

		/// <summary>Classic until the player picks Pixel (also while the chooser is still open).</summary>
		internal static bool Classic => Core.WeSave.Data.UiSkin != SkinPixel;

		internal static bool Chosen => Core.WeSave.Data.UiSkin != SkinUnchosen;

		internal static void SetSkin(int skin)
		{
			skin = skin == SkinPixel ? SkinPixel : SkinClassic;
			bool changed = Core.WeSave.Data.UiSkin != skin;
			// close everything in both interfaces so no half-open state survives the switch
			ClosePanels();
			C.WeSplash.Hide();
			N.WeSplash.Hide();
			C.WrenchToolbar.Collapse();
			NC.WrenchToolbar.Collapse();
			C.WrenchDock.Reset();
			NC.WrenchDock.Reset();
			NC.WrenchSidebar.Reset();
			C.WePlayerUI.Reset();
			NA.WePlayerUI.Reset();
			C.DiscordWidget.Unfocus();
			NW.DiscordWidget.Unfocus();
			Core.WeSave.Data.UiSkin = skin;
			Core.WeSave.Save();
			if (changed)
				Core.WeToast.Show(skin == SkinPixel ? "ToastUiPixel" : "ToastUiClassic");
		}

		internal static void LoadIcons()
		{
			C.WeIcons.Load();
			N.WeIcons.Load();
		}

		// ---- panels ---------------------------------------------------------------------------------
		internal static bool Covering => Classic ? C.WePanels.Covering : N.WePanels.Covering;
		internal static bool PanelOpen => Classic ? C.WePanels.IsOpen : N.WePanels.IsOpen;
		internal static bool AteInput => Classic ? C.WePanels.AteInput : N.WePanels.AteInput;

		internal static void ClosePanels()
		{
			C.WePanels.Close();
			N.WePanels.Close();
		}

		internal static void OpenMusic()
		{
			if (Classic)
				C.WePanels.Open(C.WePanelId.Music);
			else
				N.WePanels.Open(N.WePanelId.Music);
		}

		// ---- intro card -----------------------------------------------------------------------------
		internal static bool SplashVisible => Classic ? C.WeSplash.Visible : N.WeSplash.Visible;

		internal static void SplashShow()
		{
			if (Classic)
				C.WeSplash.Show();
			else
				N.WeSplash.Show();
		}

		internal static void SplashHide()
		{
			C.WeSplash.Hide();
			N.WeSplash.Hide();
		}

		internal static void SplashOnThemeSelected()
		{
			if (Classic)
				C.WeSplash.OnThemeSelected();
			else
				N.WeSplash.OnThemeSelected();
		}

		internal static void SplashDismiss(bool save)
		{
			if (Classic)
				C.WeSplash.Dismiss(save);
			else
				N.WeSplash.Dismiss(save);
		}

		// ---- hub ------------------------------------------------------------------------------------
		internal static bool HubBusy => Classic ? C.WrenchToolbar.Busy : NC.WrenchToolbar.Busy;
		internal static bool HubExpanded => Classic ? C.WrenchToolbar.Expanded : NC.WrenchToolbar.Expanded;
		internal static bool UseSidebar => !Classic && NC.WrenchHub.UseSidebar;
		internal static Rectangle HubHitRect() => Classic ? C.WrenchToolbar.HitRect() : NC.WrenchToolbar.HitRect();

		internal static void HubCollapse()
		{
			C.WrenchToolbar.Collapse();
			NC.WrenchToolbar.Collapse();
		}

		internal static void HubOnThemeSelected()
		{
			if (Classic)
				C.WrenchToolbar.OnThemeSelected();
			else
				NC.WrenchToolbar.OnThemeSelected();
		}

		internal static void HubResetStyles()
		{
			C.WrenchDock.Reset();
			NC.WrenchDock.Reset();
			NC.WrenchSidebar.Reset();
		}

		// ---- music player ---------------------------------------------------------------------------
		internal static bool PlayerBusy => Classic ? C.WePlayerUI.Busy : NA.WePlayerUI.Busy;
		internal static Rectangle PlayerHitRect() => Classic ? C.WePlayerUI.HitRect() : NA.WePlayerUI.HitRect();

		internal static void PlayerReset()
		{
			C.WePlayerUI.Reset();
			NA.WePlayerUI.Reset();
		}

		// ---- widgets --------------------------------------------------------------------------------
		internal static bool DiscordBusy => Classic ? C.DiscordWidget.Busy : NW.DiscordWidget.Busy;
		internal static Rectangle ClockHitRect() => Classic ? C.ClockWidget.HitRect() : NW.ClockWidget.HitRect();
		internal static Rectangle QuoteHitRect() => Classic ? C.QuoteWidget.HitRect() : NW.QuoteWidget.HitRect();
		internal static Rectangle MoonHitRect() => Classic ? C.MoonWidget.HitRect() : NW.MoonWidget.HitRect();
		internal static Rectangle DiscordHitRect() => Classic ? C.DiscordWidget.HitRect() : NW.DiscordWidget.HitRect();

		// ---- per-frame plumbing (called by WeMenuHost / WidgetHost) ----------------------------------
		internal static void UpdateUi()
		{
			if (Classic) {
				C.WrenchToolbar.Update();
				C.WePanels.Update();
				C.WeSplash.Update();
			}
			else {
				NC.WrenchToolbar.Update();
				N.WePanels.Update();
				N.WeSplash.Update();
			}
		}

		internal static void HandleSplashInput()
		{
			if (Classic)
				C.WeSplash.HandleInput();
			else
				N.WeSplash.HandleInput();
		}

		internal static void HandlePanelAndHubInput()
		{
			if (Classic) {
				C.WePanels.HandleInput();
				C.WrenchToolbar.HandleInput();
			}
			else {
				N.WePanels.HandleInput();
				NC.WrenchToolbar.HandleInput();
			}
		}

		internal static void EndFrame()
		{
			C.WePlayerUI.EndFrame();
			NA.WePlayerUI.EndFrame();
			C.WrenchToolbar.EndFrame();
			NC.WrenchToolbar.EndFrame();
			C.WePanels.EndFrame();
			N.WePanels.EndFrame();
		}

		internal static void DrawTitleChrome(SpriteBatch sb)
		{
			if (Classic) {
				C.WrenchToolbar.Draw(sb);
				C.WePanels.Draw(sb);
				if (!WeUiChooser.Visible)
					C.WeSplash.Draw(sb);
				return;
			}

			// The sidebar doubles as navigation while a page is open, so it is drawn above the page's dim.
			if (!NC.WrenchHub.UseSidebar)
				NC.WrenchToolbar.Draw(sb);
			N.WePanels.Draw(sb);
			if (NC.WrenchHub.UseSidebar)
				NC.WrenchToolbar.Draw(sb);
			if (!WeUiChooser.Visible)
				N.WeSplash.Draw(sb);
		}

		internal static void UpdateWidgets(bool onTitle)
		{
			if (Classic)
				C.WePlayerUI.Update();
			else
				NA.WePlayerUI.Update();
			if (!onTitle)
				return;
			C.QuoteWidget.Refresh();
			NW.QuoteWidget.Refresh();
			if (Classic) {
				C.DiscordWidget.Tick();
				C.DiscordWidget.TickInput();
			}
			else {
				NW.DiscordWidget.Tick();
				NW.DiscordWidget.TickInput();
			}
		}

		internal static void HandleWidgetInput()
		{
			if (Classic) {
				C.WePlayerUI.HandleInput();
				C.DiscordWidget.HandleInput();
			}
			else {
				NA.WePlayerUI.HandleInput();
				NW.DiscordWidget.HandleInput();
			}
		}

		internal static void DrawWidgets(SpriteBatch sb, float fade)
		{
			if (Classic) {
				C.WePlayerUI.Draw(sb, fade);
				C.ClockWidget.Draw(sb, fade);
				C.QuoteWidget.Draw(sb, fade);
				C.MoonWidget.Draw(sb, fade);
				C.DiscordWidget.Draw(sb, fade);
				return;
			}

			NA.WePlayerUI.Draw(sb, fade);
			NW.ClockWidget.Draw(sb, fade);
			NW.QuoteWidget.Draw(sb, fade);
			NW.MoonWidget.Draw(sb, fade);
			NW.DiscordWidget.Draw(sb, fade);
		}

		internal static void Unload()
		{
			C.WePlayerUI.Unload();
			NA.WePlayerUI.Unload();
		}
	}
}
