using Microsoft.Xna.Framework.Graphics;
using Terraria;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Audio;
using DieWithASmile.Engine.UI;
using DieWithASmile.Engine.Settings;

namespace DieWithASmile.Engine.Widgets
{
	internal static class WidgetHost
	{
		internal static bool Busy => WePlayerUI.Busy || DiscordWidget.Busy;

		internal static void Update()
		{
			if (!Main.gameMenu || !WeModMenu.IsActive)
				return;

			WePlayerUI.Update();
			if (!WeModMenu.OnTitle)
				return;

			QuoteWidget.Refresh();
			DiscordFeed.Tick();
			DiscordWidget.Tick();
			DiscordWidget.TickInput();
		}

		internal static void HandleInput()
		{
			if (!WeModMenu.OnTitle)
				return;
			if (WePanels.Covering || WePanels.AteInput || WeNeoMenu.Covering)
				return;
			WePlayerUI.HandleInput();
			DiscordWidget.HandleInput();
		}

		internal static void Draw(SpriteBatch spriteBatch, float fade)
		{
			WePlayerUI.Draw(spriteBatch, fade);
			ClockWidget.Draw(spriteBatch, fade);
			QuoteWidget.Draw(spriteBatch, fade);
			MoonWidget.Draw(spriteBatch, fade);
			DiscordWidget.Draw(spriteBatch, fade);
		}
	}
}
