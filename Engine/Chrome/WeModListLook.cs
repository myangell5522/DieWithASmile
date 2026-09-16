using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.Chrome
{
	internal static class WeModListLook
	{
		private const string GifFile = "Assets/Textures/UI/ModList/mod-icon.gif";
		private static readonly Color Accent = new(0, 153, 255);
		private static readonly Color Rest = new(58, 42, 50);
		private static readonly List<Asset<Texture2D>> Frames = new();
		private static Asset<Texture2D> _logoBg;
		private static Asset<Texture2D> _logoWing;
		private static WeClip _logoGif;
		private static bool _hooks;

		internal static void Load(Mod mod)
		{
			if (_hooks || Main.dedServ)
				return;

			Frames.Clear();
			_logoGif?.Dispose();
			_logoGif = null;
			if (mod != null && mod.FileExists(GifFile)) {
				try {
					_logoGif = WeGif.Decode(mod.GetFileBytes(GifFile));
					_logoGif?.KeepDelays();
				}
				catch {
					_logoGif = null;
				}
			}

			_logoBg = Request(mod, "LogoBg");
			_logoWing = Request(mod, "LogoWing");
			for (int i = 0; i < 8; i++) {
				string file = "Assets/Textures/UI/ModList/Frame" + i.ToString("D2") + ".png";
				if (mod == null || !mod.FileExists(file))
					break;
				Frames.Add(ModContent.Request<Texture2D>(
					"DieWithASmile/Assets/Textures/UI/ModList/Frame" + i.ToString("D2"),
					i == 0 ? AssetRequestMode.ImmediateLoad : AssetRequestMode.AsyncLoad));
			}

			MethodInfo drawPanel = typeof(UIPanel).GetMethod("DrawSelf", BindingFlags.NonPublic | BindingFlags.Instance);
			if (drawPanel != null)
				MonoModHooks.Add(drawPanel, DrawPanel);

			MethodInfo drawImage = typeof(UIImage).GetMethod("DrawSelf", BindingFlags.NonPublic | BindingFlags.Instance);
			if (drawImage != null)
				MonoModHooks.Add(drawImage, DrawImage);

			_hooks = true;
		}

		internal static void Unload()
		{
			Frames.Clear();
			_logoBg = null;
			_logoWing = null;
			_logoGif?.Dispose();
			_logoGif = null;
		}

		private static Asset<Texture2D> Request(Mod mod, string name)
		{
			string file = "Assets/Textures/UI/ModList/" + name + ".png";
			if (mod == null || !mod.FileExists(file))
				return null;
			return ModContent.Request<Texture2D>(
				"DieWithASmile/Assets/Textures/UI/ModList/" + name,
				AssetRequestMode.ImmediateLoad);
		}

		private static void DrawPanel(Action<UIPanel, SpriteBatch> orig, UIPanel self, SpriteBatch spriteBatch)
		{
			if (!IsOurs(self)) {
				orig(self, spriteBatch);
				return;
			}

			self.BorderColor = self.IsMouseHovering ? Accent : Accent * 0.78f;
			Color saved = self.BackgroundColor;
			self.BackgroundColor = Color.Transparent;
			DrawBackdrop(spriteBatch, self);
			orig(self, spriteBatch);
			self.BackgroundColor = saved;
		}

		private static void DrawImage(Action<UIImage, SpriteBatch> orig, UIImage self, SpriteBatch spriteBatch)
		{
			if (IsModIcon(self)) {
				CalculatedStyle dim = self.GetDimensions();
				var dest = new Rectangle((int)dim.X, (int)dim.Y, (int)dim.Width, (int)dim.Height);
				if (DrawLogo(spriteBatch, dest, 1f))
					return;
			}

			orig(self, spriteBatch);
		}

		internal static bool DrawIcon(SpriteBatch spriteBatch, Rectangle dest, float alpha)
		{
			if (dest.Width < 8 || dest.Height < 8)
				return false;

			Texture2D frame = CurrentFrame();
			if (frame != null) {
				float scale = Math.Max(
					dest.Width / (float)Math.Max(1, frame.Width),
					dest.Height / (float)Math.Max(1, frame.Height));
				int srcW = Math.Min(frame.Width, Math.Max(1, (int)(dest.Width / scale)));
				int srcH = Math.Min(frame.Height, Math.Max(1, (int)(dest.Height / scale)));
				int srcX = Math.Max(0, (frame.Width - srcW) / 2);
				int srcY = Math.Max(0, (frame.Height - srcH) / 2);
				spriteBatch.Draw(frame, dest, new Rectangle(srcX, srcY, srcW, srcH), Color.White * (0.92f * alpha));
			}
			else {
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				spriteBatch.Draw(pixel, dest, new Color(18, 48, 92) * (0.92f * alpha));
			}

			bool logo = DrawLogo(spriteBatch, dest, alpha);
			Texture2D px = TextureAssets.MagicPixel.Value;
			Color edge = new Color(80, 220, 255) * alpha;
			spriteBatch.Draw(px, new Rectangle(dest.X, dest.Y, dest.Width, 2), edge);
			spriteBatch.Draw(px, new Rectangle(dest.X, dest.Bottom - 2, dest.Width, 2), edge);
			spriteBatch.Draw(px, new Rectangle(dest.X, dest.Y, 2, dest.Height), edge);
			spriteBatch.Draw(px, new Rectangle(dest.Right - 2, dest.Y, 2, dest.Height), edge);
			return logo;
		}

		private static void DrawBackdrop(SpriteBatch spriteBatch, UIPanel panel)
		{
			CalculatedStyle dim = panel.GetDimensions();
			var dest = new Rectangle((int)dim.X, (int)dim.Y, (int)dim.Width, (int)dim.Height);
			if (dest.Width < 2 || dest.Height < 2)
				return;

			int side = dest.Height;
			var logo = new Rectangle(dest.X, dest.Y, Math.Min(side, dest.Width), side);
			if (!DrawLogo(spriteBatch, logo, 0.96f)) {
				Texture2D tex = CurrentFrame();
				if (tex == null)
					return;
				float scale = Math.Max(dest.Width / (float)Math.Max(1, tex.Width), dest.Height / (float)Math.Max(1, tex.Height));
				int srcW = Math.Min(tex.Width, Math.Max(1, (int)(dest.Width / scale)));
				int srcH = Math.Min(tex.Height, Math.Max(1, (int)(dest.Height / scale)));
				int srcX = Math.Max(0, (tex.Width - srcW) / 2);
				int srcY = Math.Max(0, (tex.Height - srcH) / 2);
				spriteBatch.Draw(tex, dest, new Rectangle(srcX, srcY, srcW, srcH), Color.White * 0.92f);
				return;
			}

			if (dest.Width <= logo.Width)
				return;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, new Rectangle(logo.Right, dest.Y, dest.Width - logo.Width, dest.Height), Rest * 0.92f);
		}

		private static bool DrawLogo(SpriteBatch spriteBatch, Rectangle dest, float alpha)
		{
			if (dest.Width < 2 || dest.Height < 2)
				return false;

			Texture2D gif = LogoGif();
			if (gif != null) {
				Blit(spriteBatch, gif, dest, alpha);
				return true;
			}

			Texture2D bg = Tex(_logoBg);
			if (bg == null)
				return false;

			Blit(spriteBatch, bg, dest, alpha);
			Texture2D wing = Tex(_logoWing);
			if (wing == null)
				return true;

			float bob = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f) * dest.Height * 0.032f;
			var wingDest = dest;
			wingDest.Y += (int)MathF.Round(bob);
			Blit(spriteBatch, wing, wingDest, alpha);
			return true;
		}

		private static void Blit(SpriteBatch spriteBatch, Texture2D tex, Rectangle dest, float alpha)
		{
			float scale = Math.Min(dest.Width / (float)Math.Max(1, tex.Width), dest.Height / (float)Math.Max(1, tex.Height));
			int w = Math.Max(1, (int)(tex.Width * scale));
			int h = Math.Max(1, (int)(tex.Height * scale));
			var box = new Rectangle(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
			spriteBatch.Draw(tex, box, Color.White * alpha);
		}

		private static Texture2D LogoGif()
		{
			if (_logoGif == null)
				return null;

			Texture2D cur = _logoGif.Current();
			if (cur != null)
				return cur;

			bool prev = WeAnim.CanUpload;
			WeAnim.CanUpload = true;
			try {
				_logoGif.Present();
			}
			finally {
				WeAnim.CanUpload = prev;
			}

			return _logoGif.Current();
		}

		private static Texture2D Tex(Asset<Texture2D> asset)
		{
			try {
				Texture2D tex = asset?.Value;
				return tex == null || tex.IsDisposed ? null : tex;
			}
			catch {
				return null;
			}
		}

		private static Texture2D CurrentFrame()
		{
			if (Frames.Count == 0)
				return null;

			int index = (int)(Main.GameUpdateCount / 18u) % Frames.Count;
			try {
				Asset<Texture2D> asset = Frames[index];
				if (asset == null)
					return null;
				if (!asset.IsLoaded)
					asset = Frames[0];
				return Tex(asset);
			}
			catch {
				return null;
			}
		}

		private static bool IsModIcon(UIImage self)
		{
			if (self == null)
				return false;

			CalculatedStyle dim = self.GetDimensions();
			if (dim.Width < 48f || dim.Height < 48f)
				return false;

			UIElement row = self;
			for (int i = 0; i < 8 && row != null; i++) {
				if (IsOurs(row)) {
					CalculatedStyle parent = row.GetDimensions();
					return dim.X - parent.X < parent.Width * 0.28f;
				}

				row = row.Parent;
			}

			return false;
		}

		private static bool IsOurs(UIElement self)
		{
			if (self == null)
				return false;
			try {
				PropertyInfo prop = self.GetType().GetProperty("ModName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (prop?.GetValue(self) as string == "DieWithASmile")
					return true;
			}
			catch {
			}

			return false;
		}
	}
}
