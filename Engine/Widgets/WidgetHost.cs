using Microsoft.Xna.Framework.Graphics;
using Terraria;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Audio;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Widgets
{
	internal static class WidgetHost
	{
		internal static bool Busy => WeUi.PlayerBusy || WeUi.DiscordBusy;

		internal static void Update()
		{
			if (!Main.gameMenu || !WeModMenu.IsActive)
				return;

			WeUi.UpdateWidgets(WeModMenu.OnTitle);
			if (WeModMenu.OnTitle)
				DiscordFeed.Tick();
		}

		internal static void HandleInput()
		{
			if (!WeModMenu.OnTitle)
				return;
			if (WeUi.Covering || WeUi.AteInput || WeUiChooser.Visible)
				return;
			WeUi.HandleWidgetInput();
		}

		internal static void Draw(SpriteBatch spriteBatch, float fade)
		{
			WeUi.DrawWidgets(spriteBatch, fade);
		}
	}
}
