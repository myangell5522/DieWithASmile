using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoHud
	{
		internal const int BossExtraH = 96;
		internal const int MapExtraH = 120;

		internal static void BossPreview(SpriteBatch spriteBatch, Rectangle view, int y, float fade)
		{
			var box = BossBox(view, y);
			WeDraw.Fill(spriteBatch, box, new Color(12, 14, 18) * (0.55f * fade));
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * (0.7f * fade));
			int row = 38;
			DrawBossStrip(spriteBatch, new Rectangle(box.X + 8, box.Y + 8, box.Width - 16, row), 1f, fade,
				WeText.UI("NeoBossFull"), false);
			float live = LiveHp();
			DrawBossStrip(spriteBatch, new Rectangle(box.X + 8, box.Y + 12 + row, box.Width - 16, row), live, fade,
				WeText.UI("NeoBossLive"), ShowHpText());
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

				DrawFakeMap(spriteBatch, Inset(box, 18), fade);
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

		private static Rectangle BossBox(Rectangle view, int y) =>
			new(view.X + 8, y + 2, view.Width - 16, BossExtraH - 6);

		private static Rectangle MapBox(Rectangle view, int y) =>
			new(view.X + 8, y + 2, view.Width - 16, MapExtraH - 6);

		private static float LiveHp()
		{
			float wave = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.Pi / 2f);
			return MathHelper.Lerp(0.2f, 1f, wave);
		}

		private static bool ShowHpText()
		{
			if (WeNeoFld.TryGetBool(typeof(Main), "ShowBossBarHealthText", out bool on))
				return on;
			Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
			return WeNeoFld.TryGetBool(iface, "ShowBossBarHealthText", out bool v) && v;
		}

		private static void DrawBossStrip(SpriteBatch spriteBatch, Rectangle hit, float life, float fade, string tag, bool nums)
		{
			life = MathHelper.Clamp(life, 0f, 1f);
			var icon = new Rectangle(hit.X, hit.Y + 2, 32, 32);
			DrawBossIcon(spriteBatch, icon, fade);
			var bar = new Rectangle(icon.Right + 8, hit.Y + 10, hit.Width - 44, 16);
			WeDraw.Fill(spriteBatch, bar, new Color(20, 16, 16) * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(bar.X, bar.Y, Math.Max(2, (int)(bar.Width * life)), bar.Height),
				Color.Lerp(new Color(180, 40, 40), new Color(220, 60, 50), life) * fade);
			WeDraw.Fill(spriteBatch, new Rectangle(bar.X, bar.Y, Math.Max(2, (int)(bar.Width * life)), 3),
				Color.White * (0.18f * fade));
			WeDraw.Border(spriteBatch, bar, Color.Black * (0.7f * fade));
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, tag,
				new Vector2(bar.X, hit.Y - 2), Color.White * (0.55f * fade), 0f, Vector2.Zero, new Vector2(0.55f));
			if (nums) {
				string hp = ((int)(life * 1000)).ToString() + " / 1000";
				Vector2 size = FontAssets.MouseText.Value.MeasureString(hp) * 0.58f;
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, hp,
					new Vector2(bar.X + (bar.Width - size.X) * 0.5f, bar.Y + (bar.Height - size.Y) * 0.5f),
					Color.White * fade, 0f, Vector2.Zero, new Vector2(0.58f));
			}
		}

		private static void DrawBossIcon(SpriteBatch spriteBatch, Rectangle dest, float fade)
		{
			Texture2D tex = BossHead();
			WeDraw.Fill(spriteBatch, dest, new Color(18, 20, 26) * fade);
			if (tex != null)
				WeDraw.DrawCover(spriteBatch, tex, dest, Color.White * fade);
			WeDraw.Border(spriteBatch, dest, Color.White * (0.2f * fade));
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
			SetVec(frame, "MinimapPosition", new Vector2(box.X, box.Y));
			SetVec(frame, "Position", new Vector2(box.X, box.Y));
			return frame;
		}

		private static void EndFrame(object frame, object saved)
		{
			if (frame == null || saved is not Vector2 v)
				return;
			SetVec(frame, "MinimapPosition", v);
			SetVec(frame, "Position", v);
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
			MethodInfo m = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			return m?.Invoke(target, null);
		}

		private static void CallDraw(object target, string name, SpriteBatch spriteBatch)
		{
			MethodInfo m = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null, new[] { typeof(SpriteBatch) }, null);
			m?.Invoke(target, new object[] { spriteBatch });
		}

		private static void SetVec(object target, string name, Vector2 value)
		{
			Type type = target.GetType();
			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (p != null && p.CanWrite && p.PropertyType == typeof(Vector2)) {
				p.SetValue(target, value);
				return;
			}

			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (f != null && f.FieldType == typeof(Vector2))
				f.SetValue(target, value);
		}
	}
}
