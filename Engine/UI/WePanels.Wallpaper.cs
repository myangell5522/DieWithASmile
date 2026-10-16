using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Content;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Grab;

namespace DieWithASmile.Engine.UI
{
	internal static partial class WePanels
	{
		private struct WallHit
		{
			internal Rectangle Rect;
			internal string Slider;
			internal Action Left;
			internal Action Right;
		}

		private static readonly List<WallHit> WallHits = new();
		private static float _wallHeight;
		private static Rectangle _wallView;

		private static void DrawWallpaper(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			WallHits.Clear();
			_wallView = View(panel);
			int start = y;

			// 1. Where the sky comes from.
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionSky"));
			DrawSkyChips(spriteBatch, panel, ref y);
			if (WeSave.Data.Wallpaper is WallpaperKind.Color or WallpaperKind.Gradient) {
				DrawRgbArmed(spriteBatch, panel, ref y, "wallA", WeSettings.WallpaperColorA);
				if (WeSave.Data.Wallpaper == WallpaperKind.Gradient)
					DrawRgbArmed(spriteBatch, panel, ref y, "wallB", WeSettings.WallpaperColorB);
			}

			// 2. Scenes shipped with this theme.
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionPack"));
			foreach (string id in WeNestedPacks.VisibleWallpaperIds())
				DrawNestedCard(spriteBatch, panel, ref y, id);

			// 3. Backgrounds pulled in automatically from other installed mods.
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionFromMods"));
			DrawBorrowSection(spriteBatch, panel, ref y, WeCatalog.Skies, WeOfferKind.Sky);

			// 4. Your own pictures and GIFs.
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionImages"));
			DrawHint(spriteBatch, panel, ref y, WeText.UI("AnimHint"));
			DrawTwin(spriteBatch, panel, ref y, WeText.UI("ImportImage"), WeText.UI("OpenFolder"),
				() => WeArt.TryImportWallpaper(),
				() => WeFiles.OpenFolder(WeSave.WallpaperFolder), WeIcons.Upload, WeIcons.Folder);
			WeLayerRecord selected = WeSettings.SelectedLayer();
			foreach (WeArtRecord record in WeSave.Data.Wallpapers)
				DrawLibRow(spriteBatch, panel, ref y, record, selected != null && selected.ArtId == record.Id);

			// 5. Mixing: stack pictures and effects on top of the sky.
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionLayers"));
			DrawHint(spriteBatch, panel, ref y, WeText.UI("PlaceHint"));
			DrawTwin(spriteBatch, panel, ref y, WeText.UI("AddImageLayer"), WeText.UI("AddEffectLayer"),
				() => WeSettings.AddImageLayer(WeSave.Data.Wallpaper == WallpaperKind.Image ? WeSave.Data.WallpaperId : ""),
				() => WeSettings.AddEffectLayer(WeFxKind.Stars), WeIcons.Image, WeIcons.Sparkle);
			if (WeSave.Data.Layers.Count == 0)
				DrawHint(spriteBatch, panel, ref y, WeText.UI("EmptyLayers"));
			else {
				foreach (WeLayerRecord layer in WeSave.Data.Layers)
					DrawLayerBlock(spriteBatch, panel, ref y, layer);
			}

			// 6. Final touches.
			DrawSection(spriteBatch, panel, ref y, WeText.UI("SectionAdjust"));
			DrawArmedSlider(spriteBatch, panel, ref y, "dim", WeSave.Data.WallpaperDim, WeText.UI("WallpaperDim"));
			DrawArmedSlider(spriteBatch, panel, ref y, "vignette", WeSave.Data.WallpaperVignette, WeText.UI("WallpaperVignette"));
			DrawHitCard(spriteBatch, panel, ref y, WeText.UI("WallpaperParallax"), WeSave.Data.WallpaperParallax, () => {
				WeSave.Data.WallpaperParallax = !WeSave.Data.WallpaperParallax;
				WeSave.Save();
			});
			_wallHeight = Math.Max(0, y - start) + 12;
		}

		private static void ClickWallpaper(Rectangle panel, ref int y)
		{
			_ = panel;
			_ = y;
			Point mouse = new(Main.mouseX, Main.mouseY);
			if (!_wallView.Contains(mouse) && _wallView.Width > 0)
				return;

			bool right = WeInput.RightDown;
			for (int i = WallHits.Count - 1; i >= 0; i--) {
				WallHit hit = WallHits[i];
				if (!hit.Rect.Contains(mouse))
					continue;
				if (!string.IsNullOrEmpty(hit.Slider)) {
					if (!right) {
						_dragSlider = hit.Slider;
						ApplySlider(hit.Slider);
					}

					return;
				}

				if (right && hit.Right != null) {
					hit.Right();
					return;
				}

				hit.Left?.Invoke();
				return;
			}
		}


		private static void DrawSkyChips(SpriteBatch spriteBatch, Rectangle panel, ref int y)
		{
			int gap = 6;
			int w = Math.Max(40, (panel.Width - 32 - gap * 2) / 3);
			int x = panel.X + 16;
			DrawChip(spriteBatch, new Rectangle(x, y, w, 32), WeText.UI("SkyVanilla"), WeSave.Data.Wallpaper == WallpaperKind.Vanilla,
				WeSettings.SetWallpaperVanilla);
			DrawChip(spriteBatch, new Rectangle(x + w + gap, y, w, 32), WeText.UI("SkyColor"), WeSave.Data.Wallpaper == WallpaperKind.Color,
				() => WeSettings.SetWallpaperColor(false));
			DrawChip(spriteBatch, new Rectangle(x + (w + gap) * 2, y, w, 32), WeText.UI("SkyGradient"), WeSave.Data.Wallpaper == WallpaperKind.Gradient,
				() => WeSettings.SetWallpaperColor(true));
			y += 40;
		}

		private static void DrawChip(SpriteBatch spriteBatch, Rectangle hit, string text, bool on, Action click)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			string label = WeTerra.Fit(text, 0.7f, hit.Width - 10);
			WeTerra.LabelCentered(spriteBatch, label, hit.Center.ToVector2(), (on || hover ? Color.White : new Color(230, 230, 240)) * _fade, 0.7f);
			Arm(hit, click);
		}

		private static void DrawNestedCard(SpriteBatch spriteBatch, Rectangle panel, ref int y, string id)
		{
			Rectangle hit = Row(panel, y, 56);
			bool on = WeSave.Data.Wallpaper == WallpaperKind.Nested && WeSave.Data.WallpaperId == id;
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			DrawBadge(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 14, 28, 28), WePresetLogos.PackIcon(), null);
			Thumb(spriteBatch, new Rectangle(hit.X + 44, hit.Y + 8, 72, 40), WeNestedPacks.WallpaperPreview(id), cover: true);
			TwoLine(spriteBatch, hit.X + 126, hit.Y + 8, hit.Width - 170,
				WeText.UI(WeNestedPacks.SceneTitleKey(id)), WeText.UI("LogoFromPack") + "  ·  " + WeText.UI("BorrowKindSky"));
			Affordance(spriteBatch, hit, CardKind.Radio, on);
			string nestId = id;
			Arm(hit, () => {
				WeSettings.SetWallpaperNested(nestId);
				WeToast.Show("ToastWallpaper");
			});
			y += 62;
		}

		private static void DrawTwin(SpriteBatch spriteBatch, Rectangle panel, ref int y, string a, string b, Action left, Action right, string iconA = null, string iconB = null)
		{
			Rectangle l = new(panel.X + 16, y, (panel.Width - 40) / 2, 32);
			Rectangle r = new(l.Right + 8, y, l.Width, 32);
			DrawMini(spriteBatch, l, a, iconA);
			DrawMini(spriteBatch, r, b, iconB);
			Arm(l, left);
			Arm(r, right);
			y += 40;
		}

		private static void DrawHitCard(SpriteBatch spriteBatch, Rectangle panel, ref int y, string text, bool on, Action click, CardKind kind = CardKind.Toggle)
		{
			Rectangle hit = Row(panel, y, 36);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, kind == CardKind.Radio && on);
			WeTerra.Label(spriteBatch, WeTerra.Fit(text, 0.82f, hit.Width - 56), new Vector2(hit.X + 12, hit.Y + 9), Color.White * _fade, 0.82f);
			Affordance(spriteBatch, hit, kind, on);
			Arm(hit, click);
			y += 42;
		}

		private static void DrawLayerBlock(SpriteBatch spriteBatch, Rectangle panel, ref int y, WeLayerRecord layer)
		{
			Rectangle hit = Row(panel, y, 40);
			Rectangle trash = TrashRect(hit);
			Rectangle body = new(hit.X, hit.Y, Math.Max(8, trash.X - hit.X - 8), hit.Height);
			bool on = layer.Id == WeSave.Data.SelectedLayerId;
			bool hover = body.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, body, hover, on);

			int textX = body.X + 36;
			var dest = new Rectangle(body.X + 8, hit.Y + 4, 48, 32);
			if (layer.Kind == WeLayerKind.Image) {
				WeArtRecord art = WeSave.Data.Wallpapers.Find(item => item.Id == layer.ArtId);
				Thumb(spriteBatch, dest, WeArt.Preview(art, false), cover: true);
				textX = dest.Right + 10;
			}
			else
				WeTerra.Icon(spriteBatch, WeIcons.Sparkle, new Vector2(body.X + 20, body.Center.Y), 16f, Color.White * _fade);

			string title = LayerPretty(layer) + (layer.Foreground ? "  ·  " + WeText.UI("LayerForeground") : "");
			WeTerra.Label(spriteBatch, WeTerra.Fit(title, 0.8f, body.Right - textX - 30), new Vector2(textX, hit.Y + 11), Color.White * _fade, 0.8f);
			WeTerra.Icon(spriteBatch, on ? WeIcons.ChevronLeft : WeIcons.ChevronRight, new Vector2(body.Right - 16, body.Center.Y), 16f, Color.White * (0.8f * _fade));
			DrawTrashSlot(spriteBatch, trash, WeText.UI("RemoveLayer"));
			Arm(body, () => WeSettings.SelectLayer(layer.Id));
			string id = layer.Id;
			Arm(trash, () => {
				WeSettings.RemoveLayer(id);
				WeToast.Show("ToastLayerGone");
			});
			y += 46;

			if (!on)
				return;

			DrawHitCard(spriteBatch, panel, ref y, WeText.UI("LayerForeground"), layer.Foreground, WeSettings.ToggleSelectedForeground);
			if (layer.Kind == WeLayerKind.Image) {
				DrawHitCard(spriteBatch, panel, ref y, WeText.UI(FitKey(layer.Fit)), true, WeSettings.CycleSelectedFit, CardKind.Cycle);
				DrawHitCard(spriteBatch, panel, ref y, WeText.UI("CenterImage"), false, WeSettings.CenterWallpaperPan, CardKind.Action);
			}
			else
				DrawHitCard(spriteBatch, panel, ref y, WeText.UI(FxKey(layer.Effect)), true, WeSettings.CycleSelectedEffect, CardKind.Cycle);

			DrawArmedSlider(spriteBatch, panel, ref y, "lop", layer.Opacity, WeText.UI("LayerOpacity"));
			DrawArmedSlider(spriteBatch, panel, ref y, "lpar", layer.Parallax, WeText.UI("LayerParallax"));
			DrawArmedSlider(spriteBatch, panel, ref y, "lzm", (layer.Zoom - 0.6f) / 1.2f, WeText.UI("LayerZoom"));
			DrawTwin(spriteBatch, panel, ref y, WeText.UI("LayerUp"), WeText.UI("LayerDown"),
				() => WeSettings.MoveSelectedLayer(-1),
				() => WeSettings.MoveSelectedLayer(1), WeIcons.Upload, WeIcons.Layers);
		}

		private static void DrawLibRow(SpriteBatch spriteBatch, Rectangle panel, ref int y, WeArtRecord record, bool on)
		{
			Rectangle hit = Row(panel, y, 52);
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			Card(spriteBatch, hit, hover, on);
			Thumb(spriteBatch, new Rectangle(hit.X + 8, hit.Y + 6, 64, 40), WeArt.Preview(record, false), cover: true);
			TwoLine(spriteBatch, hit.X + 82, hit.Y + 7, hit.Width - 130, PrettyFile(record.FileName), WeText.UI("RightClickDelete"));
			Affordance(spriteBatch, hit, CardKind.Radio, on);
			string artId = record.Id;
			Arm(hit,
				() => WeSettings.AssignSelectedImage(artId),
				() => WeArt.Delete(record, false));
			y += 58;
		}

		private static void DrawArmedSlider(SpriteBatch spriteBatch, Rectangle panel, ref int y, string key, float value, string label)
		{
			Rectangle bar = new(panel.X + 90, y + 4, panel.Width - 160, 16);
			WeTerra.Label(spriteBatch, WeTerra.Fit(label, 0.7f, 68), new Vector2(panel.X + 18, y + 2), Color.White * (0.85f * _fade), 0.7f);
			bool hover = bar.Contains(Main.mouseX, Main.mouseY) || _dragSlider == key;
			WeTerra.Slider(spriteBatch, bar, value, hover, _fade);
			WeTerra.Label(spriteBatch, (int)Math.Round(MathHelper.Clamp(value, 0f, 1f) * 100f) + "%", new Vector2(bar.Right + 10, y + 2), WeTerra.Muted * _fade, 0.66f);
			ArmSlider(new Rectangle(panel.X + 90, y + 4, panel.Width - 160, 16), key);
			y += 22;
		}

		private static void DrawRgbArmed(SpriteBatch spriteBatch, Rectangle panel, ref int y, string key, Color color)
		{
			DrawArmedSlider(spriteBatch, panel, ref y, key + "R", color.R / 255f, WeText.UI("Red"));
			DrawArmedSlider(spriteBatch, panel, ref y, key + "G", color.G / 255f, WeText.UI("Green"));
			DrawArmedSlider(spriteBatch, panel, ref y, key + "B", color.B / 255f, WeText.UI("Blue"));
			var swatch = new Rectangle(panel.Right - 52, y - 70, 28, 62);
			WeDraw.Fill(spriteBatch, new Rectangle(swatch.X - 2, swatch.Y - 2, swatch.Width + 4, swatch.Height + 4), Color.Black * _fade);
			WeDraw.Fill(spriteBatch, swatch, color * _fade);
			y += 8;
		}

		private static void Arm(Rectangle rect, Action left, Action right = null)
		{
			rect = Rectangle.Intersect(rect, _wallView);
			if (rect.Width < 2 || rect.Height < 2)
				return;
			WallHits.Add(new WallHit { Rect = rect, Left = left, Right = right });
		}

		private static void ArmSlider(Rectangle rect, string key)
		{
			rect = Rectangle.Intersect(rect, _wallView);
			if (rect.Width < 2 || rect.Height < 2)
				return;
			WallHits.Add(new WallHit { Rect = rect, Slider = key });
		}

		private static string LayerPretty(WeLayerRecord layer)
		{
			if (layer.Kind == WeLayerKind.Effect)
				return WeText.UI(FxKey(layer.Effect));
			return PrettyFile(FileNameOf(layer.ArtId));
		}

		private static string PrettyFile(string file)
		{
			if (string.IsNullOrEmpty(file))
				return WeText.UI("ImageLayer");
			string name = Path.GetFileNameWithoutExtension(file);
			string ext = Path.GetExtension(file);
			if (name.Length >= 24 && HexName(name)) {
				string kind = string.IsNullOrEmpty(ext) ? "IMG" : ext.Trim('.').ToUpperInvariant();
				return kind + "  ·  " + name[..8];
			}

			return Ellipsize(file, 24);
		}

		private static bool HexName(string name)
		{
			foreach (char c in name) {
				bool hex = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
				if (!hex)
					return false;
			}

			return true;
		}
	}
}
