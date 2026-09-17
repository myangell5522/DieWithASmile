using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
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
		internal const int BossExtraH = 228;
		internal const int MapExtraH = 264;
		internal const int HealthExtraH = 168;
		private const float VanillaBarW = 516f;
		private const int PreviewLifeMax = 200;
		private const int PreviewManaMax = 200;
		private static readonly WePreviewBar Dummy = new();

		private enum HudKind
		{
			Classic,
			Fancy,
			FancyText,
			Bars,
			BarsText,
			BarsFull
		}

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

		internal static void HealthPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var box = HealthBox(view, y);
			DrawCard(spriteBatch, box, fade);
			var inner = Inset(box, 12);
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
			object set = null;
			try {
				set = Main.ResourceSetsManager?.ActiveSet;
			}
			catch {
			}

			float life = LiveHp();
			float mana = LiveHp(1.2f);
			switch (Kind()) {
				case HudKind.Fancy:
					DrawFancyHud(spriteBatch, inner, set, life, mana, fade, false);
					break;
				case HudKind.FancyText:
					DrawFancyHud(spriteBatch, inner, set, life, mana, fade, true);
					break;
				case HudKind.Bars:
					DrawBarsHud(spriteBatch, inner, set, life, mana, fade, false, false);
					break;
				case HudKind.BarsText:
					DrawBarsHud(spriteBatch, inner, set, life, mana, fade, true, false);
					break;
				case HudKind.BarsFull:
					DrawBarsHud(spriteBatch, inner, set, life, mana, fade, true, true);
					break;
				default:
					DrawClassicHud(spriteBatch, inner, set, life, mana, fade);
					break;
			}
		}

		private static void DrawClassicHud(SpriteBatch spriteBatch, Rectangle inner, object set, float life, float mana, float fade)
		{
			int textH = inner.Height >= 96 ? 18 : 0;
			if (textH > 0)
				DrawLifeLabel(spriteBatch, inner.X + 2, inner.Y, life, fade);
			var field = new Rectangle(inner.X, inner.Y + textH, inner.Width, inner.Height - textH);
			int manaW = Math.Clamp(field.Height / 10 + 6, 22, 36);
			var hearts = new Rectangle(field.X, field.Y, Math.Max(40, field.Width - manaW - 6), field.Height);
			var stars = new Rectangle(field.Right - manaW, field.Y, manaW, field.Height);
			DrawHeartGrid(spriteBatch, hearts, set, life, fade, 5, 2, false);
			DrawManaColumn(spriteBatch, stars, set, mana, fade, 10);
		}

		private static void DrawFancyHud(SpriteBatch spriteBatch, Rectangle inner, object set, float life, float mana, float fade, bool text)
		{
			int textH = text ? 18 : 0;
			if (text)
				DrawLifeLabel(spriteBatch, inner.X + 2, inner.Y, life, fade);
			var field = new Rectangle(inner.X, inner.Y + textH, inner.Width, inner.Height - textH);
			int starW = Math.Clamp(field.Height - 8, 26, 40);
			var hearts = new Rectangle(field.X, field.Y, Math.Max(40, field.Width - starW - 8), field.Height);
			var star = new Rectangle(field.Right - starW, field.Y + (field.Height - starW) / 2, starW, starW);
			DrawHeartGrid(spriteBatch, hearts, set, life, fade, 10, 1, true);
			DrawManaColumn(spriteBatch, star, set, mana, fade, 1);
		}

		private static void DrawBarsHud(SpriteBatch spriteBatch, Rectangle inner, object set, float life, float mana, float fade, bool lifeText, bool manaText)
		{
			Texture2D hpFill = SetTex(set, "_hpFill", "hpFill", "HPFill", "_lifeFill") ?? ScanTex(set, "hp", "fill");
			Texture2D mpFill = SetTex(set, "_mpFill", "mpFill", "MPFill", "_manaFill") ?? ScanTex(set, "mp", "fill");
			Texture2D hpLeft = SetTex(set, "_hpPanelLeft", "_panelLeft", "_panelLeftHP", "HP_Panel_Left");
			Texture2D hpMid = SetTex(set, "_hpPanelMiddle", "_panelMiddleHP", "HP_Panel_Middle") ?? ScanTex(set, "panel", "middle", "hp");
			Texture2D hpRight = SetTex(set, "_hpPanelRight", "_panelRightHP", "HP_Panel_Right") ?? ScanTex(set, "panel", "right", "hp");
			Texture2D mpLeft = SetTex(set, "_mpPanelLeft", "_panelLeftMP", "MP_Panel_Left") ?? hpLeft;
			Texture2D mpMid = SetTex(set, "_mpPanelMiddle", "_panelMiddleMP", "MP_Panel_Middle") ?? ScanTex(set, "panel", "middle", "mp");
			Texture2D mpRight = SetTex(set, "_mpPanelRight", "_panelRightMP", "MP_Panel_Right") ?? ScanTex(set, "panel", "right", "mp");
			Texture2D heart = Heart() ?? hpFill;
			Texture2D star = Mana() ?? SetTex(set, "_starFill", "starFill") ?? mpFill;
			int icon = 26;
			int barH = 18;
			if (hpFill != null)
				barH = Math.Clamp(hpFill.Height, 12, 28);
			int rowGap = 12;
			int clusterH = barH * 2 + rowGap;
			int y0 = inner.Y + Math.Max(0, (inner.Height - clusterH) / 2);
			int y1 = y0 + barH + rowGap;
			string lifeTag = LifeText(life);
			string manaTag = ManaText(mana);
			int textW = 0;
			if (lifeText || manaText) {
				float tw = 0f;
				if (lifeText)
					tw = Math.Max(tw, Measure(lifeTag, 0.7f));
				if (manaText)
					tw = Math.Max(tw, Measure(manaTag, 0.7f));
				textW = (int)tw + 10;
			}

			int barW = Math.Clamp(inner.Width - textW - icon - 28, 96, 240);
			int barX = inner.Right - 4 - icon - 6 - barW;
			if (lifeText)
				DrawString(spriteBatch, lifeTag, new Vector2(inner.X + 4, y0 + (barH - 16) * 0.5f), 0.7f, Color.White * fade);
			if (manaText)
				DrawString(spriteBatch, manaTag, new Vector2(inner.X + 4, y1 + (barH - 16) * 0.5f), 0.7f, Color.White * fade);
			var hpBar = new Rectangle(barX, y0, barW, barH);
			var mpBar = new Rectangle(barX, y1, barW, barH);
			DrawHudBar(spriteBatch, hpBar, hpFill, hpLeft, hpMid, hpRight, life, fade, new Color(200, 46, 52));
			DrawHudBar(spriteBatch, mpBar, mpFill, mpLeft, mpMid, mpRight, mana, fade, new Color(50, 110, 230));
			var hpIcon = new Rectangle(hpBar.Right + 4, hpBar.Y + (barH - icon) / 2, icon, icon);
			var mpIcon = new Rectangle(mpBar.Right + 4, mpBar.Y + (barH - icon) / 2, icon, icon);
			DrawFitted(spriteBatch, heart, hpIcon, Color.White * fade, life);
			DrawFitted(spriteBatch, star, mpIcon, Color.White * fade, mana);
		}

		private static void DrawHeartGrid(SpriteBatch spriteBatch, Rectangle area, object set, float life, float fade, int cols, int rows, bool fancy)
		{
			Texture2D fill = fancy
				? SetTex(set, "_heartFill", "heartFill", "HeartFill") ?? Heart()
				: Heart();
			Texture2D left = fancy ? SetTex(set, "_heartLeft", "heartLeft", "HeartLeft") : null;
			Texture2D mid = fancy ? SetTex(set, "_heartMiddle", "heartMiddle", "HeartMiddle") : null;
			Texture2D right = fancy ? SetTex(set, "_heartRight", "heartRight", "HeartRight") : null;
			if (fill == null)
				fill = Heart();
			int n = Math.Max(1, cols * rows);
			float cellW = area.Width / (float)cols;
			float cellH = area.Height / (float)rows;
			float filled = life * n;
			for (int i = 0; i < n; i++) {
				int c = i % cols;
				int r = i / cols;
				var cell = new Rectangle(
					area.X + (int)MathF.Round(c * cellW),
					area.Y + (int)MathF.Round(r * cellH),
					Math.Max(8, (int)MathF.Round(cellW)),
					Math.Max(8, (int)MathF.Round(cellH)));
				Texture2D panel = c == 0 ? left : c == cols - 1 ? right : mid;
				if (panel != null)
					DrawFitted(spriteBatch, panel, Inset(cell, 1), Color.White * fade, 1f);
				DrawFitted(spriteBatch, fill, Inset(cell, fancy ? 3 : 2), Color.White * fade, MathHelper.Clamp(filled - i, 0f, 1f));
			}
		}

		private static void DrawManaColumn(SpriteBatch spriteBatch, Rectangle area, object set, float mana, float fade, int count)
		{
			Texture2D star = SetTex(set, "_starFill", "_starTop", "starFill", "StarFill") ?? Mana();
			if (star == null)
				return;
			count = Math.Max(1, count);
			float cellH = area.Height / (float)count;
			float filled = mana * count;
			for (int i = 0; i < count; i++) {
				var cell = new Rectangle(area.X, area.Y + (int)MathF.Round(i * cellH), area.Width, Math.Max(8, (int)MathF.Round(cellH)));
				DrawFitted(spriteBatch, star, Inset(cell, 1), Color.White * fade, MathHelper.Clamp(filled - i, 0f, 1f));
			}
		}

		private static void DrawHudBar(SpriteBatch spriteBatch, Rectangle bar, Texture2D fill, Texture2D left, Texture2D mid, Texture2D right, float t, float fade, Color fallback)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			WeDraw.Fill(spriteBatch, bar, new Color(16, 14, 18) * (0.55f * fade));
			int x = bar.X;
			int rightW = right != null ? Math.Min(right.Width, bar.Width / 4) : 0;
			int leftW = left != null ? Math.Min(left.Width, bar.Width / 4) : 0;
			if (left != null)
				spriteBatch.Draw(left, new Rectangle(x, bar.Y, leftW, bar.Height), Color.White * fade);
			if (right != null)
				spriteBatch.Draw(right, new Rectangle(bar.Right - rightW, bar.Y, rightW, bar.Height), Color.White * fade);
			int midX = x + leftW;
			int midW = Math.Max(4, bar.Width - leftW - rightW);
			if (mid != null)
				spriteBatch.Draw(mid, new Rectangle(midX, bar.Y, midW, bar.Height), Color.White * fade);
			var inner = new Rectangle(bar.X + Math.Max(2, leftW / 2), bar.Y + 2, Math.Max(4, bar.Width - Math.Max(4, leftW / 2 + rightW / 2)), Math.Max(4, bar.Height - 4));
			int fillW = Math.Max(1, (int)(inner.Width * t));
			if (fill != null && !fill.IsDisposed) {
				int srcW = Math.Max(1, (int)(fill.Width * t));
				spriteBatch.Draw(fill, new Rectangle(inner.X, inner.Y, fillW, inner.Height), new Rectangle(0, 0, srcW, fill.Height), Color.White * fade);
			}
			else {
				WeDraw.Fill(spriteBatch, new Rectangle(inner.X, inner.Y, fillW, inner.Height), fallback * fade);
			}
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
			InfernumTextures(style?.GetType(), out Texture2D frame, out Texture2D fill);
			Texture2D main = frame ?? fill;
			if (main == null)
				return false;
			if (!FitAspect(main, dest, 8, out Rectangle fitted))
				return false;
			if (fill != null && !fill.IsDisposed)
				DrawAspectSlice(spriteBatch, fill, fitted, life, fade);
			if (frame != null && !frame.IsDisposed)
				spriteBatch.Draw(frame, fitted, Color.White * fade);
			else if (fill == null)
				DrawAspectSlice(spriteBatch, main, fitted, life, fade);
			return true;
		}

		private static void InfernumTextures(Type type, out Texture2D frame, out Texture2D fill)
		{
			frame = null;
			fill = null;
			var bars = new List<(string Name, Texture2D Tex)>();
			CollectBarLike(type, bars, 0);
			foreach ((string Name, Texture2D Tex) item in bars) {
				if (frame == null && item.Name.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0)
					frame = item.Tex;
				if (fill == null && item.Name.IndexOf("Fill", StringComparison.OrdinalIgnoreCase) >= 0
				    && item.Name.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) < 0)
					fill = item.Tex;
			}

			if (frame == null && bars.Count > 0)
				frame = bars[0].Tex;
			if (fill == null && bars.Count > 1)
				fill = bars[1].Tex;
		}

		private static void CollectBarLike(Type type, List<(string Name, Texture2D Tex)> into, int depth)
		{
			if (type == null || depth > 3 || into.Count >= 8)
				return;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			foreach (FieldInfo field in type.GetFields(flags)) {
				Texture2D tex = SafeTex(AssetTex(field.GetValue(null)));
				if (!LooksLikeBar(tex, field.Name) || into.Exists(p => p.Tex == tex))
					continue;
				into.Add((field.Name, tex));
			}

			foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
				CollectBarLike(nested, into, depth + 1);
			if (type.BaseType != null && type.BaseType != typeof(object))
				CollectBarLike(type.BaseType, into, depth + 1);
		}

		private static bool LooksLikeBar(Texture2D tex, string name)
		{
			if (tex == null || tex.IsDisposed)
				return false;
			if (tex.Width < 64 || tex.Height < 6 || tex.Height > 96)
				return false;
			if (tex.Width < tex.Height * 2)
				return false;
			if (name.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0
			    || name.IndexOf("Fill", StringComparison.OrdinalIgnoreCase) >= 0
			    || name.IndexOf("Bar", StringComparison.OrdinalIgnoreCase) >= 0
			    || name.IndexOf("HP", StringComparison.OrdinalIgnoreCase) >= 0)
				return true;
			return tex.Width >= 120;
		}

		private static bool FitAspect(Texture2D tex, Rectangle dest, int pad, out Rectangle fitted)
		{
			fitted = Rectangle.Empty;
			if (tex == null || dest.Width < 8 || dest.Height < 8)
				return false;
			int maxW = Math.Max(8, dest.Width - pad * 2);
			int maxH = Math.Max(8, dest.Height - pad * 2);
			float aspect = tex.Width / (float)Math.Max(1, tex.Height);
			int h = maxH;
			int w = Math.Max(8, (int)MathF.Round(h * aspect));
			if (w > maxW) {
				w = maxW;
				h = Math.Max(8, (int)MathF.Round(w / aspect));
			}

			fitted = new Rectangle(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
			return fitted.Width >= 8 && fitted.Height >= 4;
		}

		private static void DrawAspectSlice(SpriteBatch spriteBatch, Texture2D tex, Rectangle dest, float t, float fade)
		{
			t = MathHelper.Clamp(t, 0.02f, 1f);
			int srcW = Math.Max(1, (int)(tex.Width * t));
			int dstW = Math.Max(1, (int)(dest.Width * t));
			spriteBatch.Draw(tex, new Rectangle(dest.X, dest.Y, dstW, dest.Height), new Rectangle(0, 0, srcW, tex.Height), Color.White * fade);
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

		private static void DrawLifeLabel(SpriteBatch spriteBatch, int x, int y, float life, float fade) =>
			DrawString(spriteBatch, LifeText(life), new Vector2(x, y), 0.7f, Color.White * fade);

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

		private static string LifeText(float life) =>
			"Life: " + PreviewCur(PreviewLifeMax, life) + "/" + PreviewLifeMax;

		private static string ManaText(float mana) =>
			"Mana: " + PreviewCur(PreviewManaMax, mana) + "/" + PreviewManaMax;

		private static int PreviewCur(int max, float t) =>
			Math.Max(1, (int)MathF.Round(max * MathHelper.Clamp(t, 0.02f, 1f)));

		private static HudKind Kind()
		{
			string key = ResourceKey();
			if (key.Equals("HorizontalBarsWithFullText", StringComparison.OrdinalIgnoreCase))
				return HudKind.BarsFull;
			if (key.Equals("HorizontalBarsWithText", StringComparison.OrdinalIgnoreCase))
				return HudKind.BarsText;
			if (key.Equals("HorizontalBars", StringComparison.OrdinalIgnoreCase))
				return HudKind.Bars;
			if (key.Equals("NewWithText", StringComparison.OrdinalIgnoreCase))
				return HudKind.FancyText;
			if (key.Equals("New", StringComparison.OrdinalIgnoreCase))
				return HudKind.Fancy;
			return HudKind.Classic;
		}

		private static string ResourceKey()
		{
			try {
				object set = Main.ResourceSetsManager?.ActiveSet;
				if (Prop(set, "ConfigKey") is string cfg && !string.IsNullOrWhiteSpace(cfg))
					return cfg;
				string key = Main.ResourceSetsManager?.ActiveSetKeyName;
				if (!string.IsNullOrWhiteSpace(key))
					return key;
			}
			catch {
			}

			return "Default";
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

		private static Texture2D SetTex(object set, params string[] names)
		{
			if (set == null)
				return null;
			Type type = set.GetType();
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach (string name in names) {
				Texture2D tex = SafeTex(AssetTex(type.GetField(name, flags)?.GetValue(set)
				                                 ?? type.GetProperty(name, flags)?.GetValue(set)));
				if (tex != null)
					return tex;
			}

			foreach (FieldInfo field in type.GetFields(flags)) {
				foreach (string name in names) {
					if (field.Name.IndexOf(name.Trim('_'), StringComparison.OrdinalIgnoreCase) < 0)
						continue;
					Texture2D tex = SafeTex(AssetTex(field.GetValue(set)));
					if (tex != null)
						return tex;
				}
			}

			return null;
		}

		private static Texture2D ScanTex(object set, params string[] needles)
		{
			if (set == null || needles == null || needles.Length == 0)
				return null;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach (FieldInfo field in set.GetType().GetFields(flags)) {
				bool match = true;
				foreach (string needle in needles) {
					if (field.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) {
						match = false;
						break;
					}
				}

				if (!match)
					continue;
				Texture2D tex = SafeTex(AssetTex(field.GetValue(set)));
				if (tex != null)
					return tex;
			}

			return null;
		}

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
			WeDraw.Fill(spriteBatch, box, new Color(18, 22, 28) * fade);
			int step = 8;
			for (int x = box.X; x < box.Right; x += step) {
				for (int y = box.Y; y < box.Bottom; y += step) {
					int h = (x * 13 + y * 7) & 7;
					Color c = h < 2 ? new Color(28, 48, 32) : h < 4 ? new Color(36, 34, 30) : new Color(22, 26, 32);
					if (((x + y) & 24) == 0)
						c = new Color(48, 36, 22);
					WeDraw.Fill(spriteBatch, new Rectangle(x, y, step - 1, step - 1), c * fade);
				}
			}

			WeDraw.Fill(spriteBatch, new Rectangle(box.Center.X - 4, box.Center.Y - 4, 8, 8), new Color(90, 200, 90) * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(box.Center.X + 28, box.Center.Y + 10, 6, 10), new Color(255, 140, 40) * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(box.Right - 36, box.Center.Y + 6, 18, 8), new Color(180, 40, 30) * fade);
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
