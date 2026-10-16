using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DieWithASmile.Content
{
	public class DieWithASmileModListStyle : ModSystem
	{
		// Palette lifted from the mod's avatar frame (icon.png) so the card reads as one piece with the logo.
		internal static readonly Color FrameOutline = new(99, 65, 77);    // #63414d
		internal static readonly Color FrameLight = new(234, 226, 238);   // #eae2ee
		internal static readonly Color FramePink = new(208, 169, 189);    // #d0a9bd
		internal static readonly Color FrameShade = new(191, 136, 143);   // #bf888f
		internal static readonly Color FrameTan = new(143, 113, 93);      // #8f715d
		internal static readonly Color CardTop = new(66, 42, 55);
		internal static readonly Color CardBottom = new(38, 24, 33);
		private static readonly Color TextFill = new(255, 242, 246);
		private static readonly Color TextShadow = new(70, 38, 52);

		private static Type _uiModItemType;
		private static PropertyInfo _modNameProperty;
		private static FieldInfo _modNameField;
		private static FieldInfo _displayNameCleanField;

		public override void Load()
		{
			_uiModItemType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem");
			if (_uiModItemType == null) {
				Mod.Logger.Warn("Could not find UIModItem; skipping mods-menu styling.");
				return;
			}

			_modNameProperty = _uiModItemType.GetProperty("ModName", BindingFlags.Public | BindingFlags.Instance);
			_modNameField = _uiModItemType.GetField("_modName", BindingFlags.NonPublic | BindingFlags.Instance);
			_displayNameCleanField = _uiModItemType.GetField("DisplayNameClean", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			TryAddHook(_uiModItemType.GetMethod("OnInitialize", flags), OnInitializeHook);
			TryAddHook(_uiModItemType.GetMethod("SetHoverColors", flags), SetHoverColorsHook);
			TryAddHook(
				_uiModItemType.GetMethod("Draw", flags, binder: null, types: new[] { typeof(SpriteBatch) }, modifiers: null),
				DrawHook);
		}

		private void TryAddHook(MethodInfo method, Delegate hook)
		{
			if (method == null) {
				Mod.Logger.Warn($"Could not hook {hook.Method.Name}: target method not found.");
				return;
			}

			MonoModHooks.Add(method, hook);
		}

		private static void OnInitializeHook(Action<object> orig, object self)
		{
			orig(self);
			if (!IsOurs(self) || self is not UIPanel panel)
				return;

			ApplyPanelColors(panel, hovered: false);
			if (_modNameField?.GetValue(self) is UIText name) {
				string text = ResolveName(self, name);
				if (!string.IsNullOrWhiteSpace(text))
					name.SetText(text);

				name.TextColor = TextFill;
				name.ShadowColor = TextShadow;
			}
		}

		private static void SetHoverColorsHook(Action<object, bool> orig, object self, bool hovered)
		{
			if (!IsOurs(self)) {
				orig(self, hovered);
				return;
			}

			if (self is UIPanel panel)
				ApplyPanelColors(panel, hovered);
		}

		private static void DrawHook(Action<object, SpriteBatch> orig, object self, SpriteBatch spriteBatch)
		{
			if (!IsOurs(self) || self is not UIElement element) {
				orig(self, spriteBatch);
				return;
			}

			string text = null;
			UIText name = _modNameField?.GetValue(self) as UIText;
			if (name != null) {
				text = ResolveName(self, name);
				if (!string.IsNullOrWhiteSpace(text)) {
					name.SetText(text);
					name.TextColor = Color.Transparent;
					name.ShadowColor = Color.Transparent;
				}
			}

			CalculatedStyle dims = element.GetDimensions();
			DrawNeonFrame(spriteBatch, dims, outer: true);
			orig(self, spriteBatch);
			DrawNeonFrame(spriteBatch, dims, outer: false);

			if (name != null && !string.IsNullOrWhiteSpace(text))
				DrawNeonModName(name, spriteBatch, text);
		}

		private static string ResolveName(object item, UIText name)
		{
			string text = StripVersion(name?.Text);
			if (!string.IsNullOrWhiteSpace(text) && !LooksLikeVersionOnly(text))
				return text.Trim();

			if (_displayNameCleanField?.GetValue(item) is string clean && !string.IsNullOrWhiteSpace(clean))
				return StripVersion(clean).Trim();

			string localized = Language.GetTextValue("Mods.DieWithASmile.DisplayName");
			if (!string.IsNullOrWhiteSpace(localized) && localized != "Mods.DieWithASmile.DisplayName")
				return localized;

			return "The End Of A Calamity Menu Theme";
		}

		private static string StripVersion(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return "";

			int index = text.LastIndexOf(" v", StringComparison.Ordinal);
			if (index <= 0)
				return text.Trim();

			string stripped = text[..index].Trim();
			return string.IsNullOrWhiteSpace(stripped) ? text.Trim() : stripped;
		}

		private static bool LooksLikeVersionOnly(string text)
		{
			text = text.Trim();
			return text.StartsWith("v", StringComparison.OrdinalIgnoreCase) && text.Length < 12;
		}

		private static bool IsOurs(object item)
		{
			return _modNameProperty?.GetValue(item) as string == nameof(DieWithASmile);
		}

		private static bool _hovered;

		private static void ApplyPanelColors(UIPanel panel, bool hovered)
		{
			// The backdrop is painted by WeModListLook; the vanilla fill/border would sit on top of it.
			_hovered = hovered;
			panel.BackgroundColor = Color.Transparent;
			panel.BorderColor = Color.Transparent;
		}

		private static void DrawNeonFrame(SpriteBatch spriteBatch, CalculatedStyle dims, bool outer)
		{
			var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
			if (rect.Width < 12 || rect.Height < 12)
				return;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			if (outer) {
				// Soft drop shadow under the card, like vanilla UI panels.
				spriteBatch.Draw(pixel, new Rectangle(rect.X + 3, rect.Bottom, rect.Width - 3, 3), Color.Black * 0.35f);
				spriteBatch.Draw(pixel, new Rectangle(rect.Right, rect.Y + 3, 3, rect.Height), Color.Black * 0.35f);
				return;
			}

			DrawPixelFrame(spriteBatch, rect, _hovered);
		}

		/// <summary>
		/// The avatar's frame, scaled to a card: dark plum outline, light pink top/left bevel, rose bottom/right
		/// bevel, a tan inner line, and notched corners instead of hard square ones.
		/// </summary>
		internal static void DrawPixelFrame(SpriteBatch spriteBatch, Rectangle r, bool hovered)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			const int u = 2; // one art pixel
			Color outline = FrameOutline;
			Color light = hovered ? Color.White : FrameLight;
			Color pink = hovered ? Color.Lerp(FramePink, Color.White, 0.35f) : FramePink;
			Color shade = hovered ? Color.Lerp(FrameShade, Color.White, 0.2f) : FrameShade;

			void Fill(int x, int y, int w, int h, Color c)
			{
				if (w > 0 && h > 0)
					spriteBatch.Draw(px, new Rectangle(x, y, w, h), c);
			}

			// outline (corners notched by one art pixel)
			Fill(r.X + u, r.Y, r.Width - u * 2, u, outline);
			Fill(r.X + u, r.Bottom - u, r.Width - u * 2, u, outline);
			Fill(r.X, r.Y + u, u, r.Height - u * 2, outline);
			Fill(r.Right - u, r.Y + u, u, r.Height - u * 2, outline);
			// bevel: light top/left, rose bottom/right
			Fill(r.X + u, r.Y + u, r.Width - u * 2, u, light);
			Fill(r.X + u, r.Y + u * 2, u, r.Height - u * 4, pink);
			Fill(r.X + u, r.Bottom - u * 2, r.Width - u * 2, u, shade);
			Fill(r.Right - u * 2, r.Y + u * 2, u, r.Height - u * 4, shade);
			// tan inner line
			Fill(r.X + u * 2, r.Y + u * 2, r.Width - u * 4, 1, FrameTan);
			Fill(r.X + u * 2, r.Bottom - u * 2 - 1, r.Width - u * 4, 1, FrameTan);
			Fill(r.X + u * 2, r.Y + u * 2, 1, r.Height - u * 4, FrameTan);
			Fill(r.Right - u * 2 - 1, r.Y + u * 2, 1, r.Height - u * 4, FrameTan);
			// corner sparkle pixels, as on the avatar
			Fill(r.X + u, r.Y + u, u, u, Color.White);
			Fill(r.Right - u * 2, r.Bottom - u * 2, u, u, outline);
		}

		private static void DrawNeonModName(UIText name, SpriteBatch spriteBatch, string text)
		{
			Vector2 pos = name.GetInnerDimensions().Position();
			Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, text, pos.X, pos.Y, TextFill, TextShadow, Vector2.Zero, 1f);
		}
	}
}
