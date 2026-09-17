using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoHud
	{
		internal const int BossExtraH = 200;
		internal const int MapExtraH = 264;
		internal const int HealthExtraH = 120;
		private const float BossScale = 0.8f;
		private const float HealthScale = 0.8f;
		private static readonly WePreviewBar Dummy = new();

		internal static void BossPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var box = BossBox(view, y);
			WeDraw.Fill(spriteBatch, box, new Color(12, 14, 18) * (0.55f * fade));
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * (0.7f * fade));
			int gap = 6;
			int rowH = (box.Height - gap * 3) / 2;
			var full = new Rectangle(box.X + 8, box.Y + gap, box.Width - 16, rowH);
			var live = new Rectangle(box.X + 8, full.Bottom + gap, box.Width - 16, rowH);
			DrawBossRow(spriteBatch, full, 1f, fade, WeText.UI("NeoBossFull"), false);
			DrawBossRow(spriteBatch, live, LiveHp(), fade, WeText.UI("NeoBossLive"), ShowHpText());
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
			WeDraw.Fill(spriteBatch, box, new Color(12, 14, 18) * (0.55f * fade));
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * (0.7f * fade));
			WeDraw.WithClip(spriteBatch, box, () => {
				if (!DrawActiveSet(spriteBatch, box, fade))
					DrawBarResources(spriteBatch, box, LiveHp(), LiveHp(1.2f), fade);
			});
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

		private static void DrawBossRow(SpriteBatch spriteBatch, Rectangle row, float life, float fade, string tag, bool nums)
		{
			WeDraw.Fill(spriteBatch, row, new Color(8, 10, 14) * (0.35f * fade));
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, tag,
				new Vector2(row.X + 4, row.Y + 1), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(0.52f));
			var clip = new Rectangle(row.X + 2, row.Y + 14, row.Width - 4, row.Height - 16);
			if (clip.Height < 8)
				return;
			WeDraw.WithClip(spriteBatch, clip, () => {
				WeDraw.WithTransform(spriteBatch, BossMatrix(clip), () => DrawBossStyle(spriteBatch, life, nums));
			});
		}

		private static void DrawBossStyle(SpriteBatch spriteBatch, float life, bool nums)
		{
			ModBossBarStyle style = null;
			bool prevent = false;
			try {
				style = BossBarLoader.CurrentStyle;
				prevent = style != null && style.PreventDraw;
			}
			catch {
			}

			var info = new BigProgressBarInfo
			{
				npcIndexToAimAt = 0,
				validatedAtLeastOnce = true,
				showText = nums
			};
			Dummy.Life = MathHelper.Clamp(life, 0.02f, 1f);
			object saved = DrawingInfo();
			try {
				SetDrawingInfo(info);
				style?.Draw(spriteBatch, Dummy, info);
				if (prevent)
					DrawModBossSample(spriteBatch, style, Dummy.Life);
				else
					Dummy.Draw(ref info, spriteBatch);
			}
			catch {
				try {
					if (prevent)
						DrawModBossSample(spriteBatch, style, Dummy.Life);
					else
						Dummy.Draw(ref info, spriteBatch);
				}
				catch {
				}
			}
			finally {
				SetDrawingInfo(saved);
			}
		}

		private static Matrix BossMatrix(Rectangle dest)
		{
			Vector2 origin = BossOrigin();
			Vector2 target = dest.Center.ToVector2();
			return Matrix.CreateTranslation(-origin.X, -origin.Y, 0f)
			       * Matrix.CreateScale(BossScale, BossScale, 1f)
			       * Matrix.CreateTranslation(target.X, target.Y, 0f);
		}

		private static Vector2 BossOrigin()
		{
			ModBossBarStyle style = null;
			bool prevent = false;
			try {
				style = BossBarLoader.CurrentStyle;
				prevent = style != null && style.PreventDraw;
			}
			catch {
			}

			if (!prevent)
				return new Vector2(Main.screenWidth * 0.5f, Main.screenHeight - 50f);
			if (IsCalamityStyle(style))
				return new Vector2(Main.screenWidth - 220f, Main.screenHeight - 70f);
			return new Vector2(Main.screenWidth * 0.5f, 50f);
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
			return MathHelper.Lerp(0.2f, 1f, wave);
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

		private static Texture2D BossHead()
		{
			try {
				if (TextureAssets.NpcHeadBoss == null)
					return null;
				for (int i = 0; i < TextureAssets.NpcHeadBoss.Length; i++) {
					Texture2D tex = TextureAssets.NpcHeadBoss[i]?.Value;
					if (tex != null && !tex.IsDisposed)
						return tex;
				}
			}
			catch {
			}

			return null;
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

		private static void DrawBarResources(SpriteBatch spriteBatch, Rectangle box, float life, float mana, float fade)
		{
			int x = box.X + 12;
			int w = Math.Min(220, box.Width - 24);
			int lifeY = box.Y + 28;
			int manaY = box.Y + 62;
			DrawResourceBar(spriteBatch, new Rectangle(x, lifeY, w, 18), life, new Color(200, 46, 52), fade);
			DrawResourceBar(spriteBatch, new Rectangle(x, manaY, w, 18), mana, new Color(50, 110, 230), fade);
		}

		private static void DrawResourceBar(SpriteBatch spriteBatch, Rectangle bar, float t, Color fill, float fade)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			WeDraw.Fill(spriteBatch, bar, new Color(18, 16, 20) * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(bar.X, bar.Y, Math.Max(2, (int)(bar.Width * t)), bar.Height), fill * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(bar.X, bar.Y, Math.Max(2, (int)(bar.Width * t)), 3), Color.White * (0.18f * fade));
			WeDraw.Border(spriteBatch, bar, Color.Black * (0.7f * fade));
		}

		private static bool DrawActiveSet(SpriteBatch spriteBatch, Rectangle box, float fade)
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
				WithPreviewStats(LiveHp(), LiveHp(1.2f), () => {
					WeDraw.WithTransform(spriteBatch, HealthMatrix(box), () => draw.Invoke(set, null));
				});
				return true;
			}
			catch {
				return false;
			}
		}

		private static void WithPreviewStats(float life, float mana, Action draw)
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
				int maxL = Math.Max(100, Math.Max(lifeMax, lifeMax2));
				int maxM = Math.Max(20, Math.Max(manaMax, manaMax2));
				player.statLifeMax = maxL;
				player.statLifeMax2 = maxL;
				player.statManaMax = maxM;
				player.statManaMax2 = maxM;
				player.statLife = Math.Max(1, (int)MathF.Round(maxL * MathHelper.Clamp(life, 0.05f, 1f)));
				player.statMana = Math.Max(0, (int)MathF.Round(maxM * MathHelper.Clamp(mana, 0.05f, 1f)));
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

		private static Matrix HealthMatrix(Rectangle dest)
		{
			var origin = new Vector2(Main.screenWidth, 0f);
			var target = new Vector2(dest.Right - 8, dest.Y + 8);
			return Matrix.CreateTranslation(-origin.X, -origin.Y, 0f)
			       * Matrix.CreateScale(HealthScale, HealthScale, 1f)
			       * Matrix.CreateTranslation(target.X, target.Y, 0f);
		}

		private static bool IsCalamityStyle(ModBossBarStyle style)
		{
			if (style == null)
				return false;
			string name = style.GetType().FullName ?? style.GetType().Name;
			return name.Contains("Calamity", StringComparison.OrdinalIgnoreCase)
			       || name.Contains("BossHealthBar", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsInfernumStyle(ModBossBarStyle style)
		{
			if (style == null)
				return false;
			string name = style.GetType().FullName ?? style.GetType().Name;
			return name.Contains("Infernum", StringComparison.OrdinalIgnoreCase);
		}

		private static void DrawModBossSample(SpriteBatch spriteBatch, ModBossBarStyle style, float life)
		{
			life = MathHelper.Clamp(life, 0.05f, 1f);
			if (IsCalamityStyle(style) && DrawCalamitySample(spriteBatch, style, life))
				return;
			if (DrawStyleBarTextures(spriteBatch, style, life))
				return;
			var info = new BigProgressBarInfo
			{
				npcIndexToAimAt = 0,
				validatedAtLeastOnce = true,
				showText = false
			};
			Dummy.Draw(ref info, spriteBatch);
		}

		private static bool DrawCalamitySample(SpriteBatch spriteBatch, ModBossBarStyle style, float life)
		{
			Type type = style?.GetType();
			Texture2D main = StaticTex(type, "BossMainHPBar");
			Texture2D sep = StaticTex(type, "BossSeperatorBar");
			if (main == null && sep == null)
				return false;
			int x = Main.screenWidth - 420;
			int y = Main.screenHeight - 100;
			const int barW = 400;
			int fill = Math.Max(8, (int)(barW * life));
			if (main != null && !main.IsDisposed)
				spriteBatch.Draw(main, new Rectangle(x, y + 28, fill, Math.Max(4, main.Height)), Color.White);
			if (sep != null && !sep.IsDisposed)
				spriteBatch.Draw(sep, new Rectangle(x, y + 18, barW, 6), new Color(240, 240, 255));
			string pct = (life * 100f).ToString("N1") + "%";
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, pct,
				new Vector2(x, y + 4), Color.White, 0f, Vector2.Zero, Vector2.One);
			return true;
		}

		private static bool DrawStyleBarTextures(SpriteBatch spriteBatch, ModBossBarStyle style, float life)
		{
			var textures = new List<Texture2D>();
			CollectBarTextures(style?.GetType(), textures);
			if (textures.Count == 0)
				return false;
			int x = (int)(Main.screenWidth * 0.5f) - 220;
			int y = IsInfernumStyle(style) ? 28 : 40;
			int drawn = 0;
			foreach (Texture2D tex in textures) {
				if (tex == null || tex.IsDisposed)
					continue;
				int w = Math.Clamp(tex.Width, 80, 520);
				int h = Math.Clamp(tex.Height, 8, 80);
				int fill = drawn == 0 ? Math.Max(8, (int)(w * life)) : w;
				spriteBatch.Draw(tex, new Rectangle(x, y, fill, h), Color.White);
				y += h + 4;
				drawn++;
				if (drawn >= 4)
					break;
			}

			return drawn > 0;
		}

		private static void CollectBarTextures(Type type, List<Texture2D> into)
		{
			if (type == null || into.Count >= 6)
				return;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			foreach (FieldInfo field in type.GetFields(flags)) {
				if (into.Count >= 6)
					break;
				if (field.Name.IndexOf("Bar", StringComparison.OrdinalIgnoreCase) < 0
				    && field.Name.IndexOf("HP", StringComparison.OrdinalIgnoreCase) < 0)
					continue;
				Texture2D tex = AssetTex(field.GetValue(null));
				if (tex != null && !tex.IsDisposed && tex.Width >= 16 && tex.Height >= 4 && !into.Contains(tex))
					into.Add(tex);
			}

			foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
				CollectBarTextures(nested, into);
		}

		private static Texture2D StaticTex(Type type, string name)
		{
			if (type == null)
				return null;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			object raw = type.GetField(name, flags)?.GetValue(null)
			             ?? type.GetProperty(name, flags)?.GetValue(null);
			return AssetTex(raw);
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
				Texture2D icon = BossHead() ?? TextureAssets.MagicPixel.Value;
				Rectangle frame = icon != null ? new Rectangle(0, 0, icon.Width, icon.Height) : Rectangle.Empty;
				float max = 1000f;
				float cur = MathHelper.Clamp(Life, 0.02f, 1f) * max;
				BigProgressBarHelper.DrawFancyBar(spriteBatch, cur, max, icon, frame);
			}
		}
	}
}
