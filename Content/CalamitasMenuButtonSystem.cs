using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DieWithASmile.Content
{
	public class CalamitasMenuButtonSystem : ModSystem
	{
		internal const float LeftMenuButtonX = 340f;

		internal static Color HoverColorDark => CalamitasMenuAccent.Dark;

		internal static Color HoverColorLight => CalamitasMenuAccent.Light;

		internal static Rectangle MenuDrawBounds = Rectangle.Empty;
		internal static float VanillaMenuY = 220f;

		internal static Color GetAnimatedHoverColor() => CalamitasMenuAccent.Hover;

		internal static void DrawWavedHoverText(
			SpriteBatch spriteBatch,
			DynamicSpriteFont spriteFont,
			string text,
			Vector2 position,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects = SpriteEffects.None,
			float layerDepth = 0f)
		{
			if (string.IsNullOrEmpty(text))
				return;

			Vector2 size = spriteFont.MeasureString(text);
			float timed = Main.GlobalTimeWrappedHourly * 0.65f;
			float cycle = timed % 2f;
			float front = cycle % 1f;
			bool lightSweeping = cycle < 1f;
			const float band = 0.22f;

			for (int i = 0; i < text.Length; i++) {
				float x0 = i == 0 ? 0f : spriteFont.MeasureString(text.Substring(0, i)).X;
				string glyph = text.Substring(i, 1);
				float x1 = spriteFont.MeasureString(text.Substring(0, i + 1)).X;
				float center = (x0 + x1) * 0.5f;
				float t = size.X <= 1f ? 0f : center / size.X;

				float blend = 1f - MathHelper.SmoothStep(front - band, front + band, t);
				float waveT = lightSweeping ? blend : 1f - blend;
				Color local = Color.Lerp(CalamitasMenuAccent.Dark, CalamitasMenuAccent.Light, waveT);
				local.A = color.A;

				spriteBatch.DrawString(
					spriteFont,
					glyph,
					position,
					local,
					rotation,
					origin - new Vector2(x0, 0f),
					scale,
					effects,
					layerDepth);
			}
		}
	}
}
