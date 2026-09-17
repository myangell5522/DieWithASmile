using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoHud
	{
		internal const int BossExtraH = 320;
		internal const int MapExtraH = 264;
		internal const int HealthExtraH = 180;
		private const float VanillaBarW = 516f;
		private static readonly WePreviewBar Dummy = new();
		private static readonly List<MapWalker> Walkers = new();
		private static float _mapClock;
		private static int _mapW;
		private static int _mapH;
		private static RenderTarget2D _hudRt;
		private static int _hudRtW;
		private static int _hudRtH;
		private static Asset<Texture2D> _healthBg;

		private static BigProgressBarInfo _fancyInfo;
		private static SpriteBatch _fancyBatch;

		internal static void BossPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var box = BossBox(view, y);
			DrawCard(spriteBatch, box, fade);
			var inner = Inset(box, 12);
			WeDraw.WithClip(spriteBatch, inner, () => {
				int headH = 34;
				DrawEmpressHeader(spriteBatch, new Rectangle(inner.X, inner.Y, inner.Width, headH), fade);
				int gap = 6;
				int rowsTop = inner.Y + headH + 2;
				int rowsH = inner.Bottom - rowsTop;
				int rowH = Math.Max(36, (rowsH - gap) / 2);
				var full = new Rectangle(inner.X, rowsTop, inner.Width, rowH);
				var fight = new Rectangle(inner.X, full.Bottom + gap, inner.Width, inner.Bottom - (full.Bottom + gap));
				DrawBossRow(spriteBatch, full, 1f, fade, WeText.UI("NeoBossFull"), false);
				DrawBossRow(spriteBatch, fight, LiveHp(), fade, WeText.UI("NeoBossFight"), ShowHpText());
			});
		}

		internal static bool BossClick(Rectangle view, int y, bool left)
		{
			if (!left || !BossBox(view, y).Contains(Main.mouseX, Main.mouseY))
				return false;
			try {
				WeNeoFld.CallStatic(typeof(BossBarLoader), "InsertMenu", out object onClick);
				if (onClick is Action act)
					act();
			}
			catch {
			}

			return true;
		}

		internal static void MapPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var box = MapBox(view, y);
			WeDraw.Fill(spriteBatch, box, new Color(8, 10, 14) * (0.7f * fade));
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * fade);
			WeDraw.WithClip(spriteBatch, box, () => {
				object frame = null;
				object saved = null;
				try {
					frame = BeginFrame(box, out saved);
					if (frame != null)
						CallDraw(frame, "DrawBackground", spriteBatch);
				}
				catch {
				}

				DrawFakeMap(spriteBatch, MapHole(frame, box), fade);
				try {
					if (frame != null)
						CallDraw(frame, "DrawForeground", spriteBatch);
				}
				catch {
				}
				finally {
					EndFrame(frame, saved);
				}
			});
		}

		internal static bool MapClick(Rectangle view, int y, bool left)
		{
			if (!left || !MapBox(view, y).Contains(Main.mouseX, Main.mouseY))
				return false;
			try {
				Main.MinimapFrameManagerInstance?.CycleSelection();
			}
			catch {
			}

			try {
				Main.SaveSettings();
			}
			catch {
			}

			return true;
		}

		internal static string ResourceName()
		{
			try {
				var set = Main.ResourceSetsManager?.ActiveSet;
				string shown = set?.DisplayedName;
				if (!string.IsNullOrWhiteSpace(shown))
					return shown;
				string key = Main.ResourceSetsManager?.ActiveSetKeyName;
				if (!string.IsNullOrWhiteSpace(key))
					return key;
			}
			catch {
			}

			return "";
		}

		internal static void CycleResources()
		{
			object mgr = Main.ResourceSetsManager;
			if (mgr == null)
				return;
			if (CallVoid(mgr, "CycleSelection") || CallVoid(mgr, "CycleResourceSet"))
				return;
			try {
				List<string> keys = ResourceKeys(mgr);
				if (keys.Count == 0)
					return;
				string cur = Prop(mgr, "ActiveSetKeyName") as string ?? "";
				int i = keys.FindIndex(k => string.Equals(k, cur, StringComparison.OrdinalIgnoreCase));
				string next = keys[(i + 1 + keys.Count) % keys.Count];
				if (!CallSet(mgr, next))
					SetActiveByIndex(mgr, keys, next);
			}
			catch {
			}
		}

		internal static void Unload()
		{
			RenderTarget2D rt = _hudRt;
			_hudRt = null;
			_hudRtW = 0;
			_hudRtH = 0;
			_healthBg = null;
			if (rt == null || rt.IsDisposed)
				return;
			Main.QueueMainThreadAction(() => {
				try {
					if (!rt.IsDisposed)
						rt.Dispose();
				}
				catch {
				}
			});
		}

		internal static void HealthPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var box = HealthBox(view, y);
			DrawHealthScene(spriteBatch, box, fade);
			var inner = Inset(box, 16);
			WeDraw.WithClip(spriteBatch, inner, () => DrawHealthHud(spriteBatch, inner, fade));
		}

		internal static bool HealthClick(Rectangle view, int y, bool left)
		{
			if (!left || !HealthBox(view, y).Contains(Main.mouseX, Main.mouseY))
				return false;
			CycleResources();
			try {
				Main.SaveSettings();
			}
			catch {
			}

			return true;
		}

		private static void DrawHealthHud(SpriteBatch spriteBatch, Rectangle inner, float fade)
		{
			if (!DrawActiveSet(spriteBatch, inner, fade))
				DrawHudFallback(spriteBatch, inner, fade);
		}

		private static bool DrawActiveSet(SpriteBatch spriteBatch, Rectangle inner, float fade)
		{
			object set = null;
			try {
				set = Main.ResourceSetsManager?.ActiveSet;
			}
			catch {
				return false;
			}

			if (set == null)
				return false;
			MethodInfo draw = set.GetType().GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, Type.EmptyTypes, null);
			if (draw == null)
				return false;
			try {
				if (DrawActiveSetRt(spriteBatch, inner, set, draw, fade))
					return true;
				WithPreviewStats(() => {
					WeDraw.WithTransform(spriteBatch, HealthFitMatrix(inner), () => draw.Invoke(set, null));
				});
				return true;
			}
			catch {
				return false;
			}
		}

		private static bool DrawActiveSetRt(SpriteBatch spriteBatch, Rectangle inner, object set, MethodInfo draw, float fade)
		{
			int w = Math.Max(64, Main.screenWidth);
			const int h = 160;
			RenderTarget2D rt = HudRt(w, h);
			if (rt == null)
				return false;
			bool painted = false;
			if (!WeDraw.WithOffscreen(spriteBatch, rt, () => {
				    WithPreviewStats(() => draw.Invoke(set, null));
				    painted = true;
			    }) || !painted)
				return false;

			Rectangle crop = Rectangle.Intersect(HudCrop(), new Rectangle(0, 0, rt.Width, rt.Height));
			if (crop.Width < 8 || crop.Height < 8)
				return false;
			float scale = Math.Min(inner.Width / (float)crop.Width, inner.Height / (float)crop.Height);
			scale = Math.Clamp(scale, 0.45f, 3f);
			int dw = Math.Max(8, (int)(crop.Width * scale));
			int dh = Math.Max(8, (int)(crop.Height * scale));
			var dest = new Rectangle(inner.X + (inner.Width - dw) / 2, inner.Y + (inner.Height - dh) / 2, dw, dh);
			WeDraw.WithPoint(spriteBatch, () => spriteBatch.Draw(rt, dest, crop, Color.White * fade));
			return true;
		}

		private static RenderTarget2D HudRt(int w, int h)
		{
			w = Math.Max(64, w);
			h = Math.Max(64, h);
			if (_hudRt != null && !_hudRt.IsDisposed && _hudRtW == w && _hudRtH == h)
				return _hudRt;
			try {
				_hudRt?.Dispose();
			}
			catch {
			}

			_hudRt = new RenderTarget2D(
				Main.instance.GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
				RenderTargetUsage.PreserveContents);
			_hudRtW = w;
			_hudRtH = h;
			return _hudRt;
		}

		private static Rectangle HudCrop()
		{
			int sw = Main.screenWidth;
			string key = "";
			try {
				key = Main.ResourceSetsManager?.ActiveSetKeyName ?? "";
			}
			catch {
			}

			bool bars = key.Contains("HorizontalBars", StringComparison.OrdinalIgnoreCase);
			if (!bars) {
				try {
					string shown = Main.ResourceSetsManager?.ActiveSet?.DisplayedName ?? "";
					bars = shown.Contains("Bars", StringComparison.OrdinalIgnoreCase);
				}
				catch {
				}
			}

			if (bars) {
				bool text = key.Contains("Text", StringComparison.OrdinalIgnoreCase) ||
				            key.Contains("Full", StringComparison.OrdinalIgnoreCase);
				if (!text) {
					try {
						string shown = Main.ResourceSetsManager?.ActiveSet?.DisplayedName ?? "";
						text = shown.Contains('2') || shown.Contains('3') ||
						       shown.Contains("Text", StringComparison.OrdinalIgnoreCase);
					}
					catch {
					}
				}

				int bw = text ? 420 : 280;
				int bh = text ? 90 : 80;
				return new Rectangle(Math.Max(0, sw - bw - 8), 4, bw, bh);
			}

			return new Rectangle(Math.Max(0, sw - 310), 0, 300, 90);
		}

		private static void WithPreviewStats(Action draw)
		{
			Player player = null;
			try {
				player = Main.LocalPlayer;
			}
			catch {
			}

			if (player == null) {
				draw();
				return;
			}

			int life0 = player.statLife;
			int lifeMax = player.statLifeMax;
			int lifeMax2 = player.statLifeMax2;
			int mana0 = player.statMana;
			int manaMax = player.statManaMax;
			int manaMax2 = player.statManaMax2;
			bool ghost = player.ghost;
			try {
				player.ghost = false;
				const int maxL = 100;
				const int maxM = 20;
				float wave = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.Pi / 2f);
				player.statLifeMax = maxL;
				player.statLifeMax2 = maxL;
				player.statManaMax = maxM;
				player.statManaMax2 = maxM;
				player.statLife = (int)MathF.Round(MathHelper.Lerp(10f, maxL, wave));
				player.statMana = (int)MathF.Round(MathHelper.Lerp(4f, maxM, wave));
				draw();
			}
			finally {
				player.statLife = life0;
				player.statLifeMax = lifeMax;
				player.statLifeMax2 = lifeMax2;
				player.statMana = mana0;
				player.statManaMax = manaMax;
				player.statManaMax2 = manaMax2;
				player.ghost = ghost;
			}
		}

		private static Matrix HealthFitMatrix(Rectangle inner)
		{
			var bbox = HudCrop();
			float scale = Math.Min(inner.Width / (float)Math.Max(1, bbox.Width), inner.Height / (float)Math.Max(1, bbox.Height));
			scale = Math.Clamp(scale, 0.4f, 3f);
			var origin = bbox.Center.ToVector2();
			var target = inner.Center.ToVector2();
			return Matrix.CreateTranslation(-origin.X, -origin.Y, 0f)
			       * Matrix.CreateScale(scale, scale, 1f)
			       * Matrix.CreateTranslation(target.X, target.Y, 0f);
		}

		private static void DrawHudFallback(SpriteBatch spriteBatch, Rectangle inner, float fade)
		{
			Texture2D heart = Heart();
			Texture2D star = Mana();
			const int step = 26;
			int clusterW = step * 5 + 10 + step;
			int x = inner.X + Math.Max(0, (inner.Width - clusterW) / 2);
			int y = inner.Y + Math.Max(0, (inner.Height - step) / 2);
			if (heart != null) {
				for (int i = 0; i < 5; i++)
					DrawFitted(spriteBatch, heart, new Rectangle(x + i * step, y, step, step), Color.White * fade, 1f);
			}

			if (star != null)
				DrawFitted(spriteBatch, star, new Rectangle(x + step * 5 + 10, y, step, step), Color.White * fade, 1f);
		}


		private static void DrawBossRow(SpriteBatch spriteBatch, Rectangle row, float life, float fade, string tag, bool nums)
		{
			if (row.Width < 16 || row.Height < 20)
				return;
			WeDraw.Fill(spriteBatch, row, new Color(8, 10, 14) * (0.28f * fade));
			WeDraw.Border(spriteBatch, row, WeAccent.Mid * (0.22f * fade));
			DrawString(spriteBatch, tag, new Vector2(row.X + 6, row.Y + 1), 0.52f, Color.White * (0.62f * fade));
			var clip = new Rectangle(row.X + 8, row.Y + 16, row.Width - 16, row.Height - 18);
			if (clip.Height < 10)
				return;
			WeDraw.WithClip(spriteBatch, clip, () => DrawBossStyle(spriteBatch, clip, life, nums, fade));
		}

		private static void DrawBossStyle(SpriteBatch spriteBatch, Rectangle dest, float life, bool nums, float fade)
		{
			ModBossBarStyle style = CurrentStyle();
			life = MathHelper.Clamp(life, 0.02f, 1f);
			if (IsCalamityMod(style) && DrawCalamityNative(spriteBatch, style, dest, life, fade))
				return;
			if (IsInfernumStyle(style) && DrawInfernumNative(spriteBatch, style, dest, life, fade))
				return;
			DrawVanillaFancy(spriteBatch, dest, life, nums);
		}

		private static void DrawVanillaFancy(SpriteBatch spriteBatch, Rectangle dest, float life, bool nums)
		{
			_fancyInfo = new BigProgressBarInfo
			{
				npcIndexToAimAt = 0,
				validatedAtLeastOnce = true,
				showText = nums
			};
			_fancyBatch = spriteBatch;
			Dummy.Life = life;
			object saved = DrawingInfo();
			try {
				SetDrawingInfo(_fancyInfo);
				WeDraw.WithTransform(spriteBatch, FancyFitMatrix(dest), DrawFancyNow);
			}
			catch {
			}
			finally {
				SetDrawingInfo(saved);
				_fancyBatch = null;
			}
		}

		private static void DrawFancyNow()
		{
			if (_fancyBatch == null)
				return;
			Dummy.Draw(ref _fancyInfo, _fancyBatch);
		}

		private static Matrix FancyFitMatrix(Rectangle dest)
		{
			float scale = Math.Clamp((dest.Width - 24) / VanillaBarW, 0.35f, 1.15f);
			var origin = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight - 50f);
			var target = dest.Center.ToVector2();
			return Matrix.CreateTranslation(-origin.X, -origin.Y, 0f)
			       * Matrix.CreateScale(scale, scale, 1f)
			       * Matrix.CreateTranslation(target.X, target.Y, 0f);
		}

		private static bool DrawCalamityNative(SpriteBatch spriteBatch, ModBossBarStyle style, Rectangle dest, float life, float fade)
		{
			Type type = style?.GetType();
			Texture2D main = StaticTex(type, "BossMainHPBar");
			Texture2D sep = StaticTex(type, "BossSeperatorBar");
			if (main == null || main.IsDisposed)
				return false;
			string name = EmpressName();
			int nameW = (int)Measure(name, 0.65f) + 8;
			int sepH = sep != null && !sep.IsDisposed ? sep.Height : 0;
			int nativeH = main.Height + (sepH > 0 ? sepH + 4 : 0);
			int availW = Math.Max(80, dest.Width - 16 - Math.Min(nameW, dest.Width / 3));
			float scale = Math.Min(availW / 400f, dest.Height / (float)Math.Max(8, nativeH));
			scale = Math.Clamp(scale, 0.35f, 1.2f);
			int barW = Math.Max(40, (int)(400f * scale));
			int barH = Math.Max(4, (int)(main.Height * scale));
			int drawSepH = sepH > 0 ? Math.Max(1, (int)(sepH * scale)) : 0;
			int totalH = barH + (drawSepH > 0 ? drawSepH + 4 : 0);
			int x = dest.X + Math.Max(0, (dest.Width - barW - nameW) / 2);
			int y = dest.Y + Math.Max(0, (dest.Height - totalH) / 2);
			if (sep != null && !sep.IsDisposed && drawSepH > 0) {
				spriteBatch.Draw(sep, new Rectangle(x, y, barW, drawSepH), Color.White * fade);
				y += drawSepH + 4;
			}

			int fillW = Math.Max(4, (int)(barW * life));
			int srcW = Math.Max(1, (int)(main.Width * life));
			spriteBatch.Draw(main, new Rectangle(x, y, fillW, barH), new Rectangle(0, 0, srcW, main.Height), Color.White * fade);
			DrawString(spriteBatch, name, new Vector2(x + barW + 6, y + (barH - 16) * 0.5f), 0.65f, Color.White * fade);
			return true;
		}

		private static bool DrawInfernumNative(SpriteBatch spriteBatch, ModBossBarStyle style, Rectangle dest, float life, float fade)
		{
			Type type = style?.GetType();
			Texture2D barFrame = InfernumTex(type, "BarFrame");
			if (barFrame == null)
				return false;
			Texture2D iconFrame = InfernumTex(type, "IconFrame", "IconBase");
			Texture2D tip = InfernumTex(type, "MainBarTip");
			Texture2D percent = InfernumTex(type, "PercentageFrame");
			life = MathHelper.Clamp(life, 0.02f, 1f);
			Texture2D phaseEnd = InfernumTex(type, "PhaseIndicatorEnd");
			Texture2D phaseMid = InfernumTex(type, "PhaseIndicatorMiddle");
			Texture2D phaseStart = InfernumTex(type, "PhaseIndicatorStart");
			Texture2D phaseNotch = InfernumTex(type, "PhaseIndicatorNotch");
			float needW = barFrame.Width;
			float needH = barFrame.Height + 48f;
			float scale = Math.Min(1f, Math.Min((dest.Width - 16) / needW, (dest.Height - 8) / needH));
			scale = Math.Clamp(scale, 0.35f, 1f);
			var center = dest.Center.ToVector2() + new Vector2(0f, 16f * scale);
			Color color = Color.White * fade;
			DrawTex(spriteBatch, barFrame, center, scale, color);
			var leftTip = center + new Vector2(-147f, 0f) * scale;
			var rightTip = center + new Vector2(84f, 0f) * scale;
			var hpLeft = leftTip + new Vector2(10f, 0f) * scale;
			var hpRight = rightTip + new Vector2(14f, 0f) * scale;
			float hpW = Math.Max(4f, Vector2.Distance(hpLeft, hpRight) * life);
			int hpH = Math.Max(4, (int)(33f * scale));
			WeDraw.Fill(spriteBatch, new Rectangle((int)(hpRight.X - hpW), (int)(center.Y - hpH * 0.5f), (int)hpW, hpH), new Color(208, 47, 63) * fade);
			DrawTex(spriteBatch, iconFrame, center, scale, color);
			DrawInfernumHead(spriteBatch, EmpressHead(), center + new Vector2(135f, 0f) * scale, 40f * scale, fade);
			DrawTex(spriteBatch, tip, Vector2.Lerp(rightTip, leftTip, life), scale, color);
			DrawInfernumPhases(spriteBatch, center, scale, life, fade, color, percent, phaseStart, phaseMid, phaseEnd, phaseNotch);
			return true;
		}

		private static Texture2D InfernumTex(Type type, params string[] names)
		{
			foreach (string name in names) {
				Texture2D tex = StaticTex(type, name);
				if (tex != null)
					return tex;
			}

			return null;
		}

		private static void DrawInfernumPhases(SpriteBatch spriteBatch, Vector2 center, float scale, float life, float fade, Color color,
			Texture2D percent, Texture2D phaseStart, Texture2D phaseMid, Texture2D phaseEnd, Texture2D phaseNotch)
		{
			const int count = 4;
			int currentPhase = life >= 0.99f ? 1 : Math.Clamp(1 + (int)((1f - life) * count), 1, count + 1);
			var rightShell = center + new Vector2(114f, -38f) * scale;
			float endW = phaseEnd != null ? phaseEnd.Width * scale : 20f * scale;
			float midW = phaseMid != null ? phaseMid.Width * scale : 12f * scale;
			float shellX = rightShell.X - endW * 0.5f - midW * 0.5f + 4f * scale;
			float notchX = shellX + 6f * scale;
			for (int i = 0; i < count; i++) {
				bool popped = count - i < currentPhase;
				DrawTex(spriteBatch, phaseNotch, new Vector2(notchX, rightShell.Y + 3f * scale), scale, popped ? color * 0.22f : color);
				notchX -= 15f * scale;
				if (i < count - 1) {
					DrawTex(spriteBatch, phaseMid, new Vector2(shellX, rightShell.Y - 9f * scale), scale, color);
					shellX -= 15f * scale;
				}
			}

			DrawTex(spriteBatch, phaseEnd, rightShell, scale, color);
			var leftShell = new Vector2(shellX - 6f * scale, rightShell.Y);
			DrawTex(spriteBatch, phaseStart, leftShell, scale, color);
			var pctPos = leftShell + new Vector2(-30f, -3f) * scale;
			DrawTex(spriteBatch, percent, pctPos, scale, color);
			float shown = MathF.Truncate(life * 10000f) / 100f;
			string pct = shown.ToString("0.00") + "%";
			var font = FontAssets.MouseText.Value;
			Vector2 size = font.MeasureString(pct);
			ChatManager.DrawColorCodedString(spriteBatch, font, pct, pctPos + new Vector2(16f, 6.5f) * scale,
				new Color(210, 158, 68) * fade, 0f, new Vector2(size.X, size.Y * 0.5f), Vector2.One * (0.62f * scale));
		}

		private static void DrawInfernumHead(SpriteBatch spriteBatch, Texture2D head, Vector2 pos, float size, float fade)
		{
			if (head == null || size < 4f)
				return;
			float s = size / Math.Max(1, Math.Max(head.Width, head.Height));
			var origin = new Vector2(head.Width, head.Height) * 0.5f;
			Color glow = new Color(255, 255, 255, 0) * (0.5f * fade * fade);
			for (int i = 0; i < 12; i++) {
				float a = MathHelper.TwoPi * i / 12f;
				var off = new Vector2(MathF.Cos(a), MathF.Sin(a)) * 3f;
				spriteBatch.Draw(head, pos + off, null, glow, 0f, origin, s, SpriteEffects.None, 0f);
			}

			spriteBatch.Draw(head, pos, null, Color.White * fade, 0f, origin, s, SpriteEffects.None, 0f);
		}

		private static void DrawTex(SpriteBatch spriteBatch, Texture2D tex, Vector2 pos, float scale, Color color)
		{
			if (tex == null || tex.IsDisposed)
				return;
			spriteBatch.Draw(tex, pos, null, color, 0f, new Vector2(tex.Width, tex.Height) * 0.5f, scale, SpriteEffects.None, 0f);
		}

		private static void DrawEmpressHeader(SpriteBatch spriteBatch, Rectangle row, float fade)
		{
			Texture2D icon = EmpressHead();
			int iconS = Math.Min(28, row.Height - 4);
			if (icon != null) {
				DrawFitted(spriteBatch, icon, new Rectangle(row.X + 2, row.Y + (row.Height - iconS) / 2, iconS, iconS), Color.White * fade, 1f);
			}

			int textX = row.X + (icon != null ? iconS + 10 : 4);
			DrawString(spriteBatch, EmpressName(), new Vector2(textX, row.Y + (row.Height - 18) * 0.5f), 0.85f, Color.White * fade);
		}

		private static void DrawHealthScene(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			WeDraw.Shadow(spriteBatch, box, fade);
			Texture2D bg = HealthBg();
			if (bg != null)
				WeDraw.DrawCover(spriteBatch, bg, box, Color.White * fade);
			else
				WeDraw.Fill(spriteBatch, box, new Color(12, 14, 18) * (0.88f * fade));
			WeDraw.VignetteBox(spriteBatch, box, 0.58f * fade);
			WeDraw.Frame(spriteBatch, box, fade);
			WeDraw.Corners(spriteBatch, box, fade, 14);
		}

		private static Texture2D HealthBg()
		{
			try {
				_healthBg ??= ModContent.Request<Texture2D>(
					"DieWithASmile/Assets/Textures/UI/HealthPreviewBg", AssetRequestMode.ImmediateLoad);
				if (_healthBg.IsLoaded)
					return _healthBg.Value;
			}
			catch {
			}

			return null;
		}

		private static void DrawCard(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			WeDraw.Shadow(spriteBatch, box, fade);
			WeDraw.Fill(spriteBatch, box, new Color(12, 14, 18) * (0.88f * fade));
			var inset = Inset(box, 3);
			WeDraw.Fill(spriteBatch, inset, WeAccent.Dark * (0.22f * fade));
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * (0.85f * fade));
			WeDraw.Border(spriteBatch, inset, WeAccent.Light * (0.16f * fade));
		}

		private static void DrawFitted(SpriteBatch spriteBatch, Texture2D tex, Rectangle dest, Color color, float amount)
		{
			if (tex == null || tex.IsDisposed || dest.Width < 2 || dest.Height < 2)
				return;
			amount = MathHelper.Clamp(amount, 0f, 1f);
			float s = Math.Min(dest.Width / (float)tex.Width, dest.Height / (float)tex.Height);
			if (s <= 0f)
				return;
			float w = tex.Width * s;
			float h = tex.Height * s;
			var pos = new Vector2(dest.X + (dest.Width - w) * 0.5f, dest.Y + (dest.Height - h) * 0.5f);
			Color empty = color * 0.22f;
			spriteBatch.Draw(tex, pos, null, empty, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
			if (amount <= 0.02f)
				return;
			if (amount >= 0.98f) {
				spriteBatch.Draw(tex, pos, null, color, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
				return;
			}

			int srcW = Math.Max(1, (int)(tex.Width * amount));
			spriteBatch.Draw(tex, pos, new Rectangle(0, 0, srcW, tex.Height), color, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
		}

		private static void DrawString(SpriteBatch spriteBatch, string text, Vector2 pos, float scale, Color color)
		{
			if (string.IsNullOrEmpty(text))
				return;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text, pos, color, 0f, Vector2.Zero, new Vector2(scale));
		}

		private static float Measure(string text, float scale)
		{
			if (string.IsNullOrEmpty(text))
				return 0f;
			try {
				return FontAssets.MouseText.Value.MeasureString(text).X * scale;
			}
			catch {
				return text.Length * 8f * scale;
			}
		}

		private static Rectangle BossBox(Rectangle view, int y) =>
			new(view.X + 8, y + 2, view.Width - 16, BossExtraH - 6);

		private static Rectangle MapBox(Rectangle view, int y)
		{
			int side = FrameSide();
			int max = Math.Max(180, view.Width - 16);
			side = Math.Clamp(side, 180, Math.Min(256, max));
			int x = view.X + Math.Max(8, (view.Width - side) / 2);
			int top = y + Math.Max(2, (MapExtraH - side) / 2);
			return new Rectangle(x, top, side, side);
		}

		private static Rectangle HealthBox(Rectangle view, int y) =>
			new(view.X + 8, y + 2, view.Width - 16, HealthExtraH - 6);

		private static float LiveHp(float phase = 0f)
		{
			float wave = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.Pi / 2f + phase);
			return MathHelper.Lerp(0.22f, 1f, wave);
		}

		private static bool ShowHpText()
		{
			if (WeNeoFld.TryGetBool(typeof(Main), "ShowBossBarHealthText", out bool on))
				return on;
			Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
			return WeNeoFld.TryGetBool(iface, "ShowBossBarHealthText", out bool v) && v;
		}

		private static FieldInfo DrawingInfoField() =>
			typeof(BossBarLoader).GetField("drawingInfo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		private static object DrawingInfo()
		{
			try {
				return DrawingInfoField()?.GetValue(null);
			}
			catch {
				return null;
			}
		}

		private static void SetDrawingInfo(object value)
		{
			try {
				DrawingInfoField()?.SetValue(null, value);
			}
			catch {
			}
		}

		private static ModBossBarStyle CurrentStyle()
		{
			try {
				return BossBarLoader.CurrentStyle;
			}
			catch {
				return null;
			}
		}

		private static bool IsCalamityMod(ModBossBarStyle style)
		{
			if (style == null)
				return false;
			string full = style.GetType().FullName ?? style.GetType().Name;
			string mod = "";
			try {
				mod = style.Mod?.Name ?? "";
			}
			catch {
			}

			if (full.Contains("Overhaul", StringComparison.OrdinalIgnoreCase)
			    || mod.Contains("Overhaul", StringComparison.OrdinalIgnoreCase))
				return false;
			bool calamity = full.Contains("CalamityMod", StringComparison.OrdinalIgnoreCase)
			                || string.Equals(mod, "CalamityMod", StringComparison.OrdinalIgnoreCase);
			if (!calamity)
				return false;
			return StaticTex(style.GetType(), "BossMainHPBar") != null;
		}

		private static bool IsInfernumStyle(ModBossBarStyle style)
		{
			if (style == null)
				return false;
			string full = style.GetType().FullName ?? style.GetType().Name;
			string mod = "";
			try {
				mod = style.Mod?.Name ?? "";
			}
			catch {
			}

			return full.Contains("Infernum", StringComparison.OrdinalIgnoreCase)
			       || mod.Contains("Infernum", StringComparison.OrdinalIgnoreCase);
		}

		private static string EmpressName()
		{
			try {
				string name = Lang.GetNPCNameValue(NPCID.HallowBoss);
				if (!string.IsNullOrWhiteSpace(name))
					return name;
			}
			catch {
			}

			return "Empress of Light";
		}

		private static Texture2D EmpressHead()
		{
			try {
				FieldInfo field = typeof(NPCID.Sets).GetField("BossHeadTextures", BindingFlags.Public | BindingFlags.Static);
				if (field?.GetValue(null) is int[] arr && NPCID.HallowBoss >= 0 && NPCID.HallowBoss < arr.Length) {
					int slot = arr[NPCID.HallowBoss];
					if (slot >= 0 && TextureAssets.NpcHeadBoss != null && slot < TextureAssets.NpcHeadBoss.Length)
						return SafeTex(TextureAssets.NpcHeadBoss[slot]?.Value);
				}
			}
			catch {
			}

			return SafeTex(TextureAssets.NpcHeadBoss?[0]?.Value);
		}

		private static Texture2D Heart() => SafeTex(TextureAssets.Heart?.Value) ?? SafeTex(TextureAssets.Heart2?.Value);

		private static Texture2D Mana() => SafeTex(TextureAssets.Mana?.Value);

		private static Texture2D SafeTex(Texture2D tex) =>
			tex != null && !tex.IsDisposed ? tex : null;

		private static Texture2D StaticTex(Type type, string name)
		{
			if (type == null)
				return null;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			object raw = type.GetField(name, flags)?.GetValue(null)
			             ?? type.GetProperty(name, flags)?.GetValue(null);
			Texture2D tex = SafeTex(AssetTex(raw));
			if (tex != null)
				return tex;
			foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
				tex = StaticTex(nested, name);
				if (tex != null)
					return tex;
			}

			return type.BaseType != null && type.BaseType != typeof(object) ? StaticTex(type.BaseType, name) : null;
		}

		private static void DrawFakeMap(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			if (box.Width < 4 || box.Height < 4)
				return;
			TickWalkers(box);
			WeDraw.WithPoint(spriteBatch, () => DrawVillage(spriteBatch, box, fade));
			DrawWalkers(spriteBatch, box, fade);
		}

		private static void DrawVillage(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			int step = Math.Clamp(box.Width / 48, 4, 6);
			int grassRow = GrassRow(box, step);
			for (int px = box.X; px < box.Right; px += step) {
				int col = (px - box.X) / step;
				int pit = PitCol(box, step);
				for (int py = box.Y; py < box.Bottom; py += step) {
					int row = (py - box.Y) / step;
					Color c = TileColor(col, row, grassRow, pit, box.Width / step);
					WeDraw.Fill(spriteBatch, new Rectangle(px, py, step, step), c * fade);
				}
			}
		}

		private static int GrassRow(Rectangle box, int step) =>
			(int)(box.Height * 0.52f / step);

		private static int PitCol(Rectangle box, int step) =>
			Math.Max(3, (int)(box.Width * 0.16f / step));

		private static Color TileColor(int col, int row, int grass, int pit, int cols)
		{
			bool shaft = col >= pit && col <= pit + 2 && row >= grass;
			if (row < grass) {
				if (HouseAt(col, row, grass, cols))
					return HouseColor(col, row, grass);
				return new Color(90, 122, 223);
			}

			if (row == grass && !shaft)
				return new Color(48, 168, 52);
			if (shaft && row < grass + 8)
				return row == grass ? new Color(36, 150, 70) : new Color(28, 48, 32);
			if (row < grass + 5 && !shaft)
				return new Color(148, 92, 48);
			if (row < grass + 10)
				return new Color(92, 58, 36);
			int speck = (col * 13 + row * 7) & 7;
			if (speck == 0)
				return new Color(48, 36, 22);
			return new Color(14, 12, 14);
		}

		private static bool HouseAt(int col, int row, int grass, int cols)
		{
			return HouseRect(cols, grass, 0).Contains(col, row)
			       || HouseRect(cols, grass, 1).Contains(col, row)
			       || HouseRect(cols, grass, 2).Contains(col, row);
		}

		private static Rectangle HouseRect(int cols, int grass, int index)
		{
			int x = index == 0 ? cols / 3 : index == 1 ? cols / 2 : (cols * 3) / 4;
			int w = index == 1 ? 7 : 6;
			int h = index == 1 ? 6 : 5;
			return new Rectangle(x, grass - h, w, h);
		}

		private static Color HouseColor(int col, int row, int grass)
		{
			if (row <= grass - 5)
				return new Color(92, 54, 40);
			if ((col + row) % 3 == 0)
				return new Color(168, 112, 64);
			return new Color(118, 78, 48);
		}

		private static void TickWalkers(Rectangle box)
		{
			int step = Math.Clamp(box.Width / 48, 4, 6);
			int grassY = box.Y + GrassRow(box, step) * step;
			int pitX = box.X + PitCol(box, step) * step;
			int left = pitX + step * 4;
			int right = box.Right - 18;
			if (Walkers.Count == 0 || _mapW != box.Width || _mapH != box.Height) {
				Walkers.Clear();
				_mapW = box.Width;
				_mapH = box.Height;
				int[] heads = { NPCHeadID.Guide, NPCHeadID.Merchant, NPCHeadID.Nurse, NPCHeadID.Demolitionist, NPCHeadID.Mechanic, NPCHeadID.GoblinTinkerer, NPCHeadID.Dryad };
				for (int i = 0; i < heads.Length; i++) {
					if (TownHead(heads[i]) == null)
						continue;
					float span = Math.Max(20, right - left);
					Walkers.Add(new MapWalker
					{
						X = left + span * ((i + 0.4f) / heads.Length),
						Y = grassY - 8,
						Vx = (i % 2 == 0 ? 0.55f : -0.45f) * (0.8f + i * 0.07f),
						Vy = 0f,
						NextJump = 0.6f + i * 0.55f,
						Head = heads[i]
					});
				}
			}

			float now = Main.GlobalTimeWrappedHourly;
			float dt = now - _mapClock;
			if (dt < 0f || dt > 0.12f)
				dt = 1f / 60f;
			_mapClock = now;
			const float grav = 14f;
			const float jump = -5.6f;
			foreach (MapWalker w in Walkers) {
				w.NextJump -= dt;
				w.Vy += grav * dt;
				w.X += w.Vx * 28f * dt;
				w.Y += w.Vy * 18f * dt;
				float ground = grassY - 8f;
				if (w.Y >= ground) {
					w.Y = ground;
					w.Vy = 0f;
					if (w.NextJump <= 0f) {
						w.Vy = jump;
						w.NextJump = 1.4f + (w.Head % 5) * 0.35f;
					}
				}

				if (w.X < left) {
					w.X = left;
					w.Vx = Math.Abs(w.Vx);
				}
				else if (w.X > right) {
					w.X = right;
					w.Vx = -Math.Abs(w.Vx);
				}
			}
		}

		private static void DrawWalkers(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			foreach (MapWalker w in Walkers) {
				Texture2D head = TownHead(w.Head);
				if (head == null)
					continue;
				const int size = 16;
				var dest = new Rectangle((int)w.X - size / 2, (int)w.Y - size / 2, size, size);
				if (!box.Intersects(dest))
					continue;
				SpriteEffects flip = w.Vx < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				var pos = new Vector2(dest.X + size * 0.5f, dest.Y + size * 0.5f);
				float s = size / (float)Math.Max(1, Math.Max(head.Width, head.Height));
				spriteBatch.Draw(head, pos, null, Color.White * fade, 0f, new Vector2(head.Width, head.Height) * 0.5f, s, flip, 0f);
			}
		}

		private static Texture2D TownHead(int slot)
		{
			try {
				if (TextureAssets.NpcHead != null && slot >= 0 && slot < TextureAssets.NpcHead.Length)
					return SafeTex(TextureAssets.NpcHead[slot]?.Value);
			}
			catch {
			}

			return null;
		}

		private static Rectangle Inset(Rectangle box, int pad) =>
			new(box.X + pad, box.Y + pad, Math.Max(8, box.Width - pad * 2), Math.Max(8, box.Height - pad * 2));

		private static Rectangle MapHole(object frame, Rectangle box)
		{
			if (frame != null && Prop(frame, "MinimapPosition") is Vector2 pos) {
				var hole = new Rectangle((int)MathF.Round(pos.X) - 6, (int)MathF.Round(pos.Y) - 6, 244, 244);
				var clipped = Rectangle.Intersect(hole, box);
				if (clipped.Width >= 16 && clipped.Height >= 16)
					return clipped;
			}

			int pad = FillInset(frame);
			return Inset(box, pad);
		}

		private static int FillInset(object frame)
		{
			if (frame != null) {
				object fill = Prop(frame, "_fillOffset") ?? Prop(frame, "FillOffset");
				if (fill is Vector2 v)
					return Math.Clamp((int)MathF.Max(v.X, v.Y), 24, 48);
			}

			return 36;
		}

		private static int FrameSide()
		{
			try {
				object mgr = Main.MinimapFrameManagerInstance;
				object frame = Prop(mgr, "ActiveSelection") ?? Prop(mgr, "CurrentSelection") ?? Call(mgr, "GetActiveFrame");
				Texture2D tex = FrameTex(frame);
				if (tex != null && !tex.IsDisposed)
					return Math.Max(tex.Width, tex.Height);
			}
			catch {
			}

			return 236;
		}

		private static Texture2D FrameTex(object frame)
		{
			object raw = Prop(frame, "_frameTexture") ?? Prop(frame, "FrameTexture");
			return AssetTex(raw);
		}

		private static object BeginFrame(Rectangle box, out object saved)
		{
			saved = null;
			object mgr = Main.MinimapFrameManagerInstance;
			if (mgr == null)
				return null;
			object frame = Prop(mgr, "ActiveSelection") ?? Prop(mgr, "CurrentSelection") ?? Call(mgr, "GetActiveFrame");
			if (frame == null)
				return null;
			saved = Prop(frame, "MinimapPosition") ?? Prop(frame, "Position");
			Texture2D tex = FrameTex(frame);
			float w = tex != null ? tex.Width : box.Width;
			float h = tex != null ? tex.Height : box.Height;
			var pos = new Vector2(box.X + (box.Width - w) * 0.5f, box.Y + (box.Height - h) * 0.5f);
			if (!SetVec(frame, "FramePosition", pos)) {
				SetVec(frame, "MinimapPosition", pos);
				SetVec(frame, "Position", pos);
			}

			return frame;
		}

		private static void EndFrame(object frame, object saved)
		{
			if (frame == null || saved is not Vector2 v)
				return;
			SetVec(frame, "MinimapPosition", v);
			SetVec(frame, "Position", v);
		}

		private static List<string> ResourceKeys(object mgr)
		{
			var keys = new List<string>();
			object access = Prop(mgr, "accessKeys");
			if (access is IEnumerable list) {
				foreach (object key in list) {
					if (key != null)
						keys.Add(key.ToString());
				}
			}

			if (keys.Count > 0)
				return keys;
			object sets = Prop(mgr, "_sets") ?? Prop(mgr, "Sets");
			if (sets is IDictionary dict) {
				foreach (object key in dict.Keys) {
					if (key != null)
						keys.Add(key.ToString());
				}
			}

			return keys;
		}

		private static bool CallSet(object mgr, string key)
		{
			Type type = mgr.GetType();
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach (string name in new[] { "SetActiveSet", "SetActive", "BindTo" }) {
				MethodInfo m = type.GetMethod(name, flags, null, new[] { typeof(string) }, null);
				if (m == null)
					continue;
				try {
					m.Invoke(mgr, new object[] { key });
					return true;
				}
				catch {
				}
			}

			return false;
		}

		private static void SetActiveByIndex(object mgr, List<string> keys, string next)
		{
			int i = keys.FindIndex(k => string.Equals(k, next, StringComparison.OrdinalIgnoreCase));
			if (i < 0)
				return;
			MethodInfo m = mgr.GetType().GetMethod("SetActiveFrameFromIndex",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null);
			m?.Invoke(mgr, new object[] { i });
		}

		private static object Prop(object target, string name)
		{
			if (target == null)
				return null;
			Type type = target.GetType();
			return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(target)
			       ?? type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(target);
		}

		private static object Call(object target, string name)
		{
			if (target == null)
				return null;
			MethodInfo m = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, Type.EmptyTypes, null);
			return m?.Invoke(target, null);
		}

		private static bool CallVoid(object target, string name)
		{
			if (target == null)
				return false;
			MethodInfo m = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, Type.EmptyTypes, null);
			if (m == null)
				return false;
			m.Invoke(target, null);
			return true;
		}

		private static void CallDraw(object target, string name, SpriteBatch spriteBatch)
		{
			MethodInfo m = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, new[] { typeof(SpriteBatch) }, null);
			m?.Invoke(target, new object[] { spriteBatch });
		}

		private static bool SetVec(object target, string name, Vector2 value)
		{
			Type type = target.GetType();
			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (p != null && p.CanWrite && p.PropertyType == typeof(Vector2)) {
				p.SetValue(target, value);
				return true;
			}

			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (f != null && f.FieldType == typeof(Vector2)) {
				f.SetValue(target, value);
				return true;
			}

			return false;
		}

		private static Texture2D AssetTex(object asset)
		{
			if (asset is Texture2D direct)
				return direct;
			if (asset == null)
				return null;
			try {
				return asset.GetType().GetProperty("Value")?.GetValue(asset) as Texture2D;
			}
			catch {
				return null;
			}
		}

		private sealed class MapWalker
		{
			internal float X;
			internal float Y;
			internal float Vx;
			internal float Vy;
			internal float NextJump;
			internal int Head;
		}

		private sealed class WePreviewBar : IBigProgressBar
		{
			internal float Life = 1f;

			public bool ValidateAndCollectNecessaryInfo(ref BigProgressBarInfo info) => true;

			public void Draw(ref BigProgressBarInfo info, SpriteBatch spriteBatch)
			{
				Texture2D icon = EmpressHead() ?? TextureAssets.MagicPixel.Value;
				Rectangle frame = icon != null ? new Rectangle(0, 0, icon.Width, icon.Height) : Rectangle.Empty;
				float max = 1000f;
				float cur = MathHelper.Clamp(Life, 0.02f, 1f) * max;
				BigProgressBarHelper.DrawFancyBar(spriteBatch, cur, max, icon, frame);
			}
		}
	}
}
