using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using DieWithASmile.Content;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.Chrome
{
	public class WeMenuFont : ModSystem
	{
		private static bool _busy;

		internal static bool Applies
		{
			get
			{
				if (_busy || Main.dedServ || !Main.gameMenu)
					return false;
				if (!WeModMenu.IsActive || CoolerMenuCompat.WorldGenUiActive)
					return false;
				if (WeModMenu.OnTitle)
					return false;
				if (BlockedUi())
					return false;

				WeType.Ensure();
				return WeType.Active ||
				       Math.Abs(WeLook.FontScaleX - 1f) > 0.01f ||
				       Math.Abs(WeLook.FontScaleY - 1f) > 0.01f;
			}
		}

		public override void Load()
		{
			if (Main.dedServ)
				return;

			On_Utils.DrawBorderString += DrawBorderStringHook;
			On_Utils.DrawBorderStringFourWay += DrawBorderStringFourWayHook;
			On_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float += DrawCodedStringHook;
			HookDrawString();
		}

		public override void Unload()
		{
			On_Utils.DrawBorderString -= DrawBorderStringHook;
			On_Utils.DrawBorderStringFourWay -= DrawBorderStringFourWayHook;
			On_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float -= DrawCodedStringHook;
		}

		internal static Vector2 Draw(
			SpriteBatch spriteBatch,
			DynamicSpriteFont font,
			string text,
			Vector2 position,
			Color color,
			float baseScale,
			Vector2 origin,
			float rotation,
			SpriteEffects effects,
			float depth,
			bool border)
		{
			if (string.IsNullOrEmpty(text) || font == null || color.A < 8)
				return Vector2.Zero;

			_busy = true;
			try {
				return WeLook.DrawStyled(
					spriteBatch, font, text, position, color, baseScale, origin,
					rotation, effects, depth, border, 0);
			}
			finally {
				_busy = false;
			}
		}

		private static bool BlockedUi()
		{
			try {
				UIState state = Main.MenuUI?.CurrentState;
				if (state == null)
					return false;
				Type type = state.GetType();
				return BlockedName(type.Name, type.FullName);
			}
			catch {
				return false;
			}
		}

		private static bool BlockedName(string name, string full)
		{
			string text = string.IsNullOrEmpty(full) ? name ?? "" : full;
			return Contains(text, "Achievement") ||
			       Contains(text, "Workshop") ||
			       Contains(text, "ModBrowser") ||
			       Contains(text, "ModPack");
		}

		private static bool Contains(string text, string token) =>
			text.Contains(token, StringComparison.OrdinalIgnoreCase);

		private static void HookDrawString()
		{
			Type ext = typeof(DynamicSpriteFontExtensionMethods);
			foreach (MethodInfo method in ext.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
				if (method.Name != nameof(DynamicSpriteFontExtensionMethods.DrawString))
					continue;

				ParameterInfo[] pars = method.GetParameters();
				if (pars.Length < 3 || pars[2].ParameterType != typeof(string))
					continue;

				try {
					if (pars.Length == 5)
						MonoModHooks.Add(method, DrawString5);
					else if (pars.Length == 10 && pars[7].ParameterType == typeof(float))
						MonoModHooks.Add(method, DrawString10f);
					else if (pars.Length == 10 && pars[7].ParameterType == typeof(Vector2))
						MonoModHooks.Add(method, DrawString10v);
				}
				catch {
				}
			}
		}

		private static Vector2 DrawBorderStringHook(
			On_Utils.orig_DrawBorderString orig,
			SpriteBatch sb,
			string text,
			Vector2 pos,
			Color color,
			float scale,
			float anchorx,
			float anchory,
			int maxCharactersDisplayed)
		{
			if (!Applies)
				return orig(sb, text, pos, color, scale, anchorx, anchory, maxCharactersDisplayed);

			if (maxCharactersDisplayed >= 0 && text != null && text.Length > maxCharactersDisplayed)
				text = text.Substring(0, maxCharactersDisplayed);

			var font = FontAssets.MouseText.Value;
			if (font == null)
				return orig(sb, text, pos, color, scale, anchorx, anchory, maxCharactersDisplayed);

			Vector2 size = font.MeasureString(text);
			Vector2 origin = size * new Vector2(anchorx, anchory);
			return Draw(sb, font, text, pos, color, scale, origin, 0f, SpriteEffects.None, 0f, true);
		}

		private static void DrawBorderStringFourWayHook(
			On_Utils.orig_DrawBorderStringFourWay orig,
			SpriteBatch sb,
			DynamicSpriteFont font,
			string text,
			float x,
			float y,
			Color textColor,
			Color borderColor,
			Vector2 origin,
			float scale)
		{
			if (!Applies) {
				orig(sb, font, text, x, y, textColor, borderColor, origin, scale);
				return;
			}

			Draw(sb, font, text, new Vector2(x, y), textColor, scale, origin, 0f, SpriteEffects.None, 0f, true);
		}

		private static Vector2 DrawCodedStringHook(
			On_ChatManager.orig_DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float orig,
			SpriteBatch spriteBatch,
			DynamicSpriteFont font,
			string text,
			Vector2 position,
			Color baseColor,
			float rotation,
			Vector2 origin,
			Vector2 baseScale,
			float maxWidth,
			float spread)
		{
			if (!Applies)
				return orig(spriteBatch, font, text, position, baseColor, rotation, origin, baseScale, maxWidth, spread);

			float scale = baseScale.X;
			if (Math.Abs(baseScale.X - baseScale.Y) > 0.01f)
				scale = (baseScale.X + baseScale.Y) * 0.5f;
			return Draw(spriteBatch, font, text, position, baseColor, scale, origin, rotation, SpriteEffects.None, 0f, true);
		}

		private static void DrawString5(
			Action<SpriteBatch, DynamicSpriteFont, string, Vector2, Color> orig,
			SpriteBatch sb,
			DynamicSpriteFont font,
			string text,
			Vector2 pos,
			Color color)
		{
			if (!Applies) {
				orig(sb, font, text, pos, color);
				return;
			}

			Draw(sb, font, text, pos, color, 1f, Vector2.Zero, 0f, SpriteEffects.None, 0f, false);
		}

		private static void DrawString10f(
			Action<SpriteBatch, DynamicSpriteFont, string, Vector2, Color, float, Vector2, float, SpriteEffects, float> orig,
			SpriteBatch sb,
			DynamicSpriteFont font,
			string text,
			Vector2 pos,
			Color color,
			float rotation,
			Vector2 origin,
			float scale,
			SpriteEffects effects,
			float depth)
		{
			if (!Applies) {
				orig(sb, font, text, pos, color, rotation, origin, scale, effects, depth);
				return;
			}

			Draw(sb, font, text, pos, color, scale, origin, rotation, effects, depth, false);
		}

		private static void DrawString10v(
			Action<SpriteBatch, DynamicSpriteFont, string, Vector2, Color, float, Vector2, Vector2, SpriteEffects, float> orig,
			SpriteBatch sb,
			DynamicSpriteFont font,
			string text,
			Vector2 pos,
			Color color,
			float rotation,
			Vector2 origin,
			Vector2 scale,
			SpriteEffects effects,
			float depth)
		{
			if (!Applies) {
				orig(sb, font, text, pos, color, rotation, origin, scale, effects, depth);
				return;
			}

			float baseScale = Math.Abs(scale.X - scale.Y) > 0.01f ? (scale.X + scale.Y) * 0.5f : scale.X;
			Draw(sb, font, text, pos, color, baseScale, origin, rotation, effects, depth, false);
		}
	}
}
