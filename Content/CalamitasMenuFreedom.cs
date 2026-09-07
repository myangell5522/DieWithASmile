using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace DieWithASmile.Content
{
	internal static class CalamitasMenuFreedom
	{
		private const string Folder = "DieWithASmile/Assets/Textures/Menu/Freedom/";
		private const float FadeSpeed = 0.018f;
		private const float BarFrac = 0.078f;
		private const int CloudCount = 26;
		private const int SkyCount = 16;

		private static readonly Color Bar = new(0x72, 0xAB, 0xF5);
		private static readonly Color Cloud = new(0xFB, 0xFA, 0xF9);
		private static readonly Color SkyWash = new(155, 196, 248);
		private static readonly Color Glow = new(232, 244, 255);
		private static readonly Color Glass = new(245, 250, 255);

		private struct Layer
		{
			public Asset<Texture2D> Art;
			public float Speed;
			public float Phase;
			public float Amp;
			public float Depth;
			public float Overflow;
			public float SmoothX;
			public float SmoothY;
		}

		private struct Wisp
		{
			public float U;
			public float V;
			public float Size;
			public float Drift;
			public float Phase;
			public float Stretch;
		}

		private static Layer[] _layers;
		private static readonly Wisp[] _top = new Wisp[CloudCount];
		private static readonly Wisp[] _bottom = new Wisp[CloudCount];
		private static readonly Wisp[] _skyTop = new Wisp[SkyCount];
		private static readonly Wisp[] _skyBottom = new Wisp[SkyCount];
		private static Asset<Texture2D> _background;
		private static float _scene;
		private static bool _spawned;
		private static bool _motionReady;

		internal static float SceneEase
		{
			get
			{
				float t = MathHelper.Clamp(_scene, 0f, 1f);
				return t * t * (3f - 2f * t);
			}
		}

		internal static void Load()
		{
			_background = ModContent.Request<Texture2D>(Folder + "Background");
			_layers = new[] {
				Make("Entropy", 0.11f, 0.4f, 34f, 0.22f, 1.10f),
				Make("Solyn", 0.085f, 1.9f, 42f, 0.38f, 1.12f),
				Make("Calamitas", 0.10f, 3.1f, 38f, 0.46f, 1.13f),
				Make("Neshumi", 0.13f, 5.0f, 28f, 0.55f, 1.09f)
			};
			_motionReady = false;
		}

		internal static void Unload()
		{
			_background = null;
			_layers = null;
			_spawned = false;
			_motionReady = false;
		}

		internal static void Reset() => _scene = 0f;

		internal static void Snap(bool on) => _scene = on ? 1f : 0f;

		internal static void Update()
		{
			float target = DieWithASmileSettings.UseFreedomScene ? 1f : 0f;
			if (_scene < target)
				_scene = MathHelper.Min(target, _scene + FadeSpeed);
			else if (_scene > target)
				_scene = MathHelper.Max(target, _scene - FadeSpeed);

			if (_layers == null)
				return;

			float time = Main.GlobalTimeWrappedHourly;
			float follow = _motionReady ? 0.078f : 1f;
			for (int i = 0; i < _layers.Length; i++) {
				Layer layer = _layers[i];
				float bob = MathF.Sin(time * layer.Speed + layer.Phase) * layer.Amp
				            + MathF.Sin(time * layer.Speed * 0.41f + layer.Phase * 1.7f) * layer.Amp * 0.34f;
				float sway = MathF.Sin(time * layer.Speed * 0.63f + layer.Phase * 0.8f) * layer.Amp * 0.18f;
				layer.SmoothY = MathHelper.Lerp(layer.SmoothY, bob, follow);
				layer.SmoothX = MathHelper.Lerp(layer.SmoothX, sway, follow);
				_layers[i] = layer;
			}

			_motionReady = true;
		}

		internal static void Draw(SpriteBatch spriteBatch, float fade)
		{
			float alpha = SceneEase * fade;
			if (alpha <= 0.02f || _background?.Value == null || _layers == null)
				return;

			EnsureClouds();
			Point cover = CalamitasMenuDraw.CoverSize;
			Rectangle sky = CalamitasMenuDraw.CoverDestination(_background.Value, 0.16f, 1.06f);
			spriteBatch.Draw(_background.Value, sky, Color.White * alpha);

			int barH = Math.Max(36, (int)(cover.Y * BarFrac));
			var topBar = new Rectangle(-48, -16, cover.X + 96, barH + 18);
			var bottomBar = new Rectangle(-48, cover.Y - barH, cover.X + 96, barH + 18);
			DrawSkyMist(spriteBatch, topBar, true, alpha);
			DrawSkyMist(spriteBatch, bottomBar, false, alpha);
			DrawBar(spriteBatch, topBar, true, alpha);
			DrawBar(spriteBatch, bottomBar, false, alpha);

			for (int i = 0; i < _layers.Length; i++) {
				Layer layer = _layers[i];
				Texture2D tex = layer.Art?.Value;
				if (tex == null)
					continue;

				Vector2 shift = CalamitasMenuParallax.ForDepth(layer.Depth) + new Vector2(layer.SmoothX, layer.SmoothY);
				Rectangle dest = Inflate(sky, layer.Overflow);
				dest.X += (int)shift.X;
				dest.Y += (int)shift.Y;
				DrawLayer(spriteBatch, tex, dest, alpha);
			}
		}

		private static void DrawLayer(SpriteBatch spriteBatch, Texture2D tex, Rectangle dest, float alpha)
		{
			Texture2D glow = CalamitasMenuShine.Texture;
			if (glow != null) {
				Vector2 origin = glow.Size() * 0.5f;
				Vector2 center = dest.Center.ToVector2();
				var scale = new Vector2(
					dest.Width * 1.16f / Math.Max(1, glow.Width),
					dest.Height * 1.08f / Math.Max(1, glow.Height));
				spriteBatch.Draw(glow, center, null, Glow * (0.26f * alpha), 0f, origin, scale, SpriteEffects.None, 0f);
				spriteBatch.Draw(glow, center, null, Cloud * (0.10f * alpha), 0f, origin, scale * 0.62f, SpriteEffects.None, 0f);
			}

			Rectangle fringe = Inflate(dest, 1.016f);
			spriteBatch.Draw(tex, fringe, Color.White * (0.32f * alpha));
			spriteBatch.Draw(tex, dest, Color.White * alpha);
		}

		private static Layer Make(string name, float speed, float phase, float amp, float depth, float overflow) =>
			new() {
				Art = ModContent.Request<Texture2D>(Folder + name),
				Speed = speed,
				Phase = phase,
				Amp = amp,
				Depth = depth,
				Overflow = overflow
			};

		private static Rectangle Inflate(Rectangle dest, float overflow)
		{
			int w = Math.Max(1, (int)(dest.Width * overflow));
			int h = Math.Max(1, (int)(dest.Height * overflow));
			return new Rectangle(
				dest.X + (dest.Width - w) / 2,
				dest.Y + (dest.Height - h) / 2,
				w,
				h);
		}

		private static void DrawBar(SpriteBatch spriteBatch, Rectangle region, bool top, float alpha)
		{
			if (region.Width <= 0 || region.Height <= 0)
				return;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			int step = 2;
			for (int i = 0; i < region.Height; i += step) {
				float along = i / (float)Math.Max(1, region.Height - 1);
				float outer = top ? 1f - along : along;
				float solid = Smooth(MathHelper.Clamp((outer - 0.18f) / 0.42f, 0f, 1f));
				float a = MathHelper.Lerp(0.03f, 1f, solid) * alpha;
				if (a < 0.01f)
					continue;

				Color fill = Color.Lerp(SkyWash, Bar, 0.28f + 0.72f * solid);
				spriteBatch.Draw(pixel, new Rectangle(region.X, region.Y + i, region.Width, step + 1), fill * a);
			}

			DrawGlassRim(spriteBatch, region, top, alpha);
			DrawClouds(spriteBatch, Spill(region, top), top, alpha, 0.28f);
		}

		private static void DrawSkyMist(SpriteBatch spriteBatch, Rectangle bar, bool top, float alpha)
		{
			Rectangle band = Spill(bar, top);
			if (top)
				band.Height = Math.Max(band.Height, bar.Height * 2);
			else {
				int extra = bar.Height;
				band.Y -= extra;
				band.Height += extra;
			}

			DrawClouds(spriteBatch, band, top, alpha, 0.20f, top ? _skyTop : _skyBottom);
		}

		private static void DrawGlassRim(SpriteBatch spriteBatch, Rectangle region, bool top, float alpha)
		{
			Texture2D glow = CalamitasMenuShine.Texture;
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			int inner = top ? region.Bottom : region.Y;
			int fadeH = Math.Max(10, region.Height / 5);
			for (int i = 0; i < fadeH; i++) {
				float t = i / (float)Math.Max(1, fadeH - 1);
				float shade = (1f - t) * (1f - t);
				int y = top ? inner - 1 - i : inner + i;
				spriteBatch.Draw(
					pixel,
					new Rectangle(region.X, y, region.Width, 2),
					new Color(40, 78, 140) * (0.16f * shade * alpha));
			}

			if (glow == null)
				return;

			Vector2 origin = glow.Size() * 0.5f;
			float yPos = inner + (top ? -7f : 7f);
			var wide = new Vector2(region.Width * 1.35f / glow.Width, region.Height * 0.38f / glow.Height);
			spriteBatch.Draw(glow, new Vector2(region.Center.X, yPos), null, Glass * (0.38f * alpha), 0f, origin, wide, SpriteEffects.None, 0f);
			spriteBatch.Draw(glow, new Vector2(region.Center.X, yPos), null, Glow * (0.18f * alpha), 0f, origin, wide * new Vector2(0.92f, 0.45f), SpriteEffects.None, 0f);

			float corner = region.Height * 0.55f / glow.Width;
			spriteBatch.Draw(glow, new Vector2(region.X + region.Height * 0.45f, yPos), null, Glass * (0.22f * alpha), 0f, origin, corner, SpriteEffects.None, 0f);
			spriteBatch.Draw(glow, new Vector2(region.Right - region.Height * 0.45f, yPos), null, Glass * (0.22f * alpha), 0f, origin, corner, SpriteEffects.None, 0f);
		}

		private static Rectangle Spill(Rectangle region, bool top)
		{
			int extra = Math.Max(18, (int)(region.Height * 0.62f));
			return top
				? new Rectangle(region.X, region.Y, region.Width, region.Height + extra)
				: new Rectangle(region.X, region.Y - extra, region.Width, region.Height + extra);
		}

		private static void DrawClouds(SpriteBatch spriteBatch, Rectangle region, bool top, float alpha, float strength, Wisp[] set = null)
		{
			Texture2D glow = CalamitasMenuShine.Texture;
			if (glow == null)
				return;

			float time = Main.GlobalTimeWrappedHourly;
			Vector2 origin = glow.Size() * 0.5f;
			set ??= top ? _top : _bottom;
			for (int i = 0; i < set.Length; i++) {
				Wisp wisp = set[i];
				float x = region.X + ((wisp.U + time * wisp.Drift) % 1.25f - 0.12f) * region.Width;
				float y = region.Y + MathHelper.Clamp(wisp.V + MathF.Sin(time * 0.13f + wisp.Phase) * 0.10f, 0.04f, 0.96f) * region.Height;
				float pulse = 0.62f + 0.38f * MathF.Sin(time * 0.22f + wisp.Phase);
				float sx = (0.62f + wisp.Size) * (region.Height / 64f) * wisp.Stretch;
				float sy = (0.24f + wisp.Size * 0.50f) * (region.Height / 82f);
				spriteBatch.Draw(
					glow,
					new Vector2(x, y),
					null,
					Cloud * (strength * pulse * alpha),
					0f,
					origin,
					new Vector2(sx, sy),
					SpriteEffects.None,
					0f);
				spriteBatch.Draw(
					glow,
					new Vector2(x + 22f, y + (top ? 8f : -8f)),
					null,
					Cloud * (strength * 0.62f * pulse * alpha),
					0f,
					origin,
					new Vector2(sx * 0.58f, sy * 0.74f),
					SpriteEffects.None,
					0f);
			}
		}

		private static void EnsureClouds()
		{
			if (_spawned)
				return;

			_spawned = true;
			Fill(_top, new Random(72006), true);
			Fill(_bottom, new Random(72007), false);
			Fill(_skyTop, new Random(72008), true);
			Fill(_skyBottom, new Random(72009), false);
		}

		private static void Fill(Wisp[] set, Random rng, bool top)
		{
			for (int i = 0; i < set.Length; i++) {
				set[i] = new Wisp {
					U = (float)rng.NextDouble(),
					V = top
						? (float)(0.28 + rng.NextDouble() * 0.62)
						: (float)(0.10 + rng.NextDouble() * 0.62),
					Size = (float)(0.40 + rng.NextDouble() * 0.95),
					Drift = (float)(0.008 + rng.NextDouble() * 0.016) * (rng.Next(2) == 0 ? 1 : -1),
					Phase = (float)rng.NextDouble() * MathHelper.TwoPi,
					Stretch = (float)(1.7 + rng.NextDouble() * 2.0)
				};
			}
		}

		private static float Smooth(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}
	}
}
