using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.Layout;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Widgets
{
	internal static class DiscordWidget
	{
		private static readonly Color CardDeep = new Color(18, 20, 24);
		private static readonly Color Online = new Color(35, 165, 89);
		private static readonly Color Idle = new Color(240, 178, 50);
		private static readonly Color Dnd = new Color(242, 63, 66);
		private static readonly Color Offline = new Color(128, 132, 142);
		private static readonly List<(Rectangle Rect, string Channel)> Joins = new();

		private static bool _mouseHeld;
		private static bool _holdLock;
		private static bool _hover;
		private static bool _hoverJoin;
		private static bool _editing;
		private static bool _enterHeld;
		private static string _buffer = "";
		private static string _joinChannel = "";
		private static int _caret;
		private static int _lastWheel;
		private static float _bodyScroll;
		private static float _bodyMax;
		private static Rectangle _card;

		internal static bool Enabled => WeSave.Data.DiscordWidget && SceneGraph.Visible(SceneGraph.Discord);

		internal static bool Editing => _editing;

		internal static bool Busy => Enabled && (_hover || _hoverJoin || _editing);

		internal static Vector2 Anchor => SceneGraph.Pixel(SceneGraph.Discord);

		internal static float Scale => SceneGraph.ScaleOf(SceneGraph.Discord);

		internal const int IdFieldHeight = 44;

		internal static Rectangle HitRect()
		{
			Vector2 size = CardSize();
			Vector2 pos = Anchor;
			return new Rectangle(
				(int)(pos.X - size.X * 0.5f),
				(int)(pos.Y - size.Y * 0.5f),
				(int)size.X,
				(int)size.Y);
		}

		internal static void OpenIdEditor()
		{
			if (!_editing)
				_buffer = DiscordFeed.ExtractId(WeSave.Data.DiscordGuildId ?? "");
			_editing = true;
			_caret = 0;
			_enterHeld = true;
			Main.clrInput();
			if (!WePanels.Is(WePanelId.Widgets))
				WePanels.Open(WePanelId.Widgets);
		}

		internal static void Unfocus()
		{
			if (!_editing)
				return;
			_editing = false;
			PlayerInput.WritingText = false;
			Main.drawingPlayerChat = false;
			CommitBuffer();
		}

		internal static void Tick()
		{
			int wheel = Mouse.GetState().ScrollWheelValue;
			if (!Enabled || WePanels.IsOpen || WePanels.AteInput || LayoutEditor.Editing || WeSplash.Visible) {
				_lastWheel = wheel;
				return;
			}

			bool over = _card.Width > 0 && _card.Contains(Main.mouseX, Main.mouseY);
			int style = Math.Clamp(WeSave.Data.DiscordStyle, 0, 2);
			if (over && (style == 1 || style == 2)) {
				float delta = (wheel - _lastWheel) / 120f * 32f;
				_bodyScroll = MathHelper.Clamp(_bodyScroll - delta, 0f, _bodyMax);
			}

			_lastWheel = wheel;
		}

		internal static void TickInput()
		{
			if (!_editing)
				return;

			if (!WeSave.Data.DiscordWidget || !WePanels.IsOpen || !WePanels.Is(WePanelId.Widgets)) {
				Unfocus();
				return;
			}

			PlayerInput.WritingText = true;
			Main.drawingPlayerChat = false;
			try {
				Main.instance.HandleIME();
			}
			catch {
			}

			string previous = _buffer;
			string next = _buffer ?? "";
			try {
				next = Main.GetInputText(_buffer ?? "") ?? "";
			}
			catch {
			}
			if (next.Length - previous.Length >= 8)
				_buffer = DiscordFeed.ExtractId(next);
			else
				_buffer = DiscordFeed.DigitsOnly(next, 20);

			if (_buffer != previous && DiscordFeed.IsSnowflake(_buffer))
				DiscordFeed.SetGuildId(_buffer);

			_caret++;
			bool enter = Main.inputTextEnter;
			if (enter && !_enterHeld) {
				CommitBuffer();
				_editing = false;
				PlayerInput.WritingText = false;
				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			_enterHeld = enter;
			if (Main.inputTextEscape)
				Unfocus();
		}

		internal static void HandleInput()
		{
			if (!Enabled || WeSplash.Visible || LayoutEditor.Editing || WePanels.AteInput) {
				WeInput.Edge(ref _mouseHeld, ref _holdLock);
				if (WePanels.IsOpen)
					return;
				_hover = _hoverJoin = false;
				return;
			}

			if (WePanels.IsOpen) {
				WeInput.Edge(ref _mouseHeld, ref _holdLock);
				_hover = _hoverJoin = false;
				return;
			}

			bool pressed = WeInput.Edge(ref _mouseHeld, ref _holdLock);
			Point mouse = new(Main.mouseX, Main.mouseY);
			_hoverJoin = false;
			_joinChannel = "";
			for (int i = 0; i < Joins.Count; i++) {
				if (!Joins[i].Rect.Contains(mouse))
					continue;
				_hoverJoin = true;
				_joinChannel = Joins[i].Channel ?? "";
				break;
			}

			_hover = (_card.Width > 0 && _card.Contains(mouse)) || _hoverJoin;
			if (_hover)
				Main.blockMouse = true;
			if (!pressed)
				return;

			if (_hoverJoin) {
				SoundEngine.PlaySound(SoundID.MenuTick);
				DiscordFeed.OpenJoin(_joinChannel);
				WeInput.LockHold(ref _holdLock);
				return;
			}

			if (_hover) {
				SoundEngine.PlaySound(SoundID.MenuTick);
				OpenIdEditor();
				WeInput.LockHold(ref _holdLock);
			}
		}

		internal static void DrawIdField(SpriteBatch spriteBatch, Rectangle hit, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			float pulse = (MathF.Sin(Main.GlobalTimeWrappedHourly * 3.4f) + 1f) * 0.5f;
			bool live = _editing || hover;
			Color fill = new Color(18, 20, 26) * ((live ? 0.96f : 0.82f) * fade);
			Color border = _editing
				? Color.Lerp(WeAccent.Mid, WeAccent.Light, pulse) * fade
				: (hover ? WeAccent.Light : WeAccent.Mid) * fade;
			WeTerra.Panel(spriteBatch, hit, fill, _editing || hover ? border : Color.Black * fade);

			string shownId = _editing ? (_buffer ?? "") : (WeSave.Data.DiscordGuildId ?? "");
			string shown;
			Color color;
			if (string.IsNullOrEmpty(shownId) && !_editing) {
				shown = WeText.UI("DiscordIdPlaceholder");
				color = WeTerra.Muted * fade;
			}
			else {
				shown = shownId;
				if (_editing && (_caret / 20) % 2 == 0)
					shown += "|";
				color = Color.White * fade;
			}

			var font = FontAssets.MouseText.Value;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, Plain(shown),
				new Vector2(hit.X + 12, hit.Y + 12), color, 0f, Vector2.Zero, new Vector2(0.82f));

			if (DiscordFeed.IsSnowflake(shownId)) {
				Texture2D circle = WeDraw.Circle();
				Vector2 pip = new(hit.Right - 18, hit.Y + hit.Height * 0.5f);
				spriteBatch.Draw(circle, pip, null, Online * fade, 0f, circle.Size() * 0.5f, 10f / circle.Width, SpriteEffects.None, 0f);
			}
		}

		internal static string StatusLine()
		{
			if (_editing && !DiscordFeed.IsSnowflake(_buffer) && _buffer.Length > 0)
				return _buffer.Length < 17 ? WeText.UI("DiscordNeedDigits") : WeText.UI("DiscordBadId");

			return DiscordFeed.Status switch {
				DiscordFeedStatus.Loading => WeText.UI("DiscordLoading"),
				DiscordFeedStatus.NeedWidget => WeText.UI("DiscordNeedWidget"),
				DiscordFeedStatus.BadId => WeText.UI("DiscordBadId"),
				DiscordFeedStatus.NetError => WeText.UI("DiscordNetError"),
				DiscordFeedStatus.Typing => WeText.UI("DiscordNeedDigits"),
				DiscordFeedStatus.Ok => OnlineLabel(DiscordFeed.Snap.Presence),
				_ => WeText.UI("DiscordEmpty")
			};
		}

		internal static void Draw(SpriteBatch spriteBatch, float fade)
		{
			_card = Rectangle.Empty;
			Joins.Clear();
			if (!Enabled || fade <= 0f)
				return;

			Vector2 size = CardSize();
			Vector2 pos = Anchor;
			_card = new Rectangle(
				(int)(pos.X - size.X * 0.5f),
				(int)(pos.Y - size.Y * 0.5f),
				(int)size.X,
				(int)size.Y);
			WeDraw.WithLinear(spriteBatch, () =>
				DrawSkin(spriteBatch, _card, Scale, fade, Math.Clamp(WeSave.Data.DiscordStyle, 0, 2), true));
		}

		internal static void DrawPreview(SpriteBatch spriteBatch, Rectangle box, float fade, int style)
		{
			if (fade <= 0.02f || box.Width < 8 || box.Height < 8)
				return;

			style = Math.Clamp(style, 0, 2);
			Vector2 nat = NativeSize(style);
			float s = Math.Min(box.Width / nat.X, box.Height / nat.Y);
			if (s <= 0.02f)
				return;

			int w = Math.Max(12, (int)(nat.X * s));
			int h = Math.Max(12, (int)(nat.Y * s));
			var card = new Rectangle(
				box.X + (box.Width - w) / 2,
				box.Y + (box.Height - h) / 2,
				w, h);
			DrawSkin(spriteBatch, card, s, fade, style, false);
		}

		private static void CommitBuffer()
		{
			string id = DiscordFeed.ExtractId(_buffer);
			_buffer = id;
			DiscordFeed.SetGuildId(id);
		}

		private static Vector2 NativeSize(int style) => style switch {
			1 => new Vector2(420f, 272f),
			2 => new Vector2(300f, 330f),
			_ => new Vector2(256f, 160f)
		};

		private static Vector2 CardSize() => NativeSize(Math.Clamp(WeSave.Data.DiscordStyle, 0, 2)) * Scale;

		// ---- layout constants (unscaled) ----------------------------------------------------------
		private const float Pad = 12f;
		private const float HeadH = 58f;
		private const float BadgeR = 20f;

		private static void DrawSkin(SpriteBatch spriteBatch, Rectangle card, float s, float fade, int style, bool hits)
		{
			WeTerra.Window(spriteBatch, card, fade);
			// Hovering the live widget lights the frame, so it reads as clickable.
			if (hits && _hover && !_hoverJoin)
				WeTerra.Panel(spriteBatch, card, Color.Transparent, WeAccent.Light * (0.9f * fade));

			switch (style) {
				case 1:
					DrawBanner(spriteBatch, card, s, fade, hits);
					break;
				case 2:
					DrawRoster(spriteBatch, card, s, fade, hits);
					break;
				default:
					DrawCompact(spriteBatch, card, s, fade);
					break;
			}
		}

		/// <summary>Server badge, name and a status line with a coloured dot; optional big Join button.</summary>
		private static void DrawHeader(SpriteBatch spriteBatch, Rectangle card, float s, float fade, bool join, bool hits)
		{
			var badge = new Vector2(card.X + (Pad + BadgeR) * s, card.Y + (Pad + BadgeR) * s);
			DrawBadge(spriteBatch, badge, BadgeR * s, fade);
			float textX = badge.X + (BadgeR + 10f) * s;
			float right = card.Right - Pad * s;
			if (join && DiscordFeed.HasGuildId) {
				string label = WeText.UI("DiscordJoin");
				float ts = 0.72f * s;
				int w = (int)(TextW(label, ts) + 44f * s);
				int h = Math.Max(14, (int)(30f * s));
				var rect = new Rectangle((int)(right - w), (int)(badge.Y - h * 0.5f), w, h);
				DrawJoinButton(spriteBatch, rect, label, ts, fade, hits, "", true);
				right = rect.X - 8f * s;
			}

			float width = Math.Max(10f, right - textX);
			Txt(spriteBatch, Ellipsize(Font, DisplayName(), width, 0.88f * s), new Vector2(textX, card.Y + (Pad + 1f) * s), Color.White * fade, 0.88f * s);
			DrawStatusLine(spriteBatch, new Vector2(textX, card.Y + (Pad + 25f) * s), width, s, fade);
			Divider(spriteBatch, card, s, card.Y + HeadH * s, fade);
		}

		private static void DrawStatusLine(SpriteBatch spriteBatch, Vector2 at, float width, float s, float fade)
		{
			bool ok = DiscordFeed.Status == DiscordFeedStatus.Ok;
			Color dot = !DiscordFeed.HasGuildId ? Offline
				: ok ? Online
				: DiscordFeed.Status == DiscordFeedStatus.Loading ? Idle
				: Dnd;
			if (DiscordFeed.Status == DiscordFeedStatus.Loading)
				dot *= 0.55f + 0.45f * (MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) + 1f) * 0.5f;
			Dot(spriteBatch, at + new Vector2(4f * s, 8f * s), 4.5f * s, dot * fade);
			float ts = 0.68f * s;
			string text = ok ? OnlineLabel(DiscordFeed.Snap.Presence) : ShortStatus();
			float x = at.X + 14f * s;
			Txt(spriteBatch, Ellipsize(Font, text, width - 14f * s, ts), new Vector2(x, at.Y), WeTerra.Muted * fade, ts);
			if (!ok)
				return;

			int voice = VoiceCount();
			if (voice <= 0)
				return;
			x += TextW(text, ts) + 12f * s;
			string vtext = WeText.UI("DiscordInVoice").Replace("%n", voice.ToString());
			if (x + 20f * s + TextW(vtext, ts) > at.X + width)
				return;
			WeTerra.Icon(spriteBatch, WeIcons.Speaker, new Vector2(x + 7f * s, at.Y + 8f * s), 16f * s, WeAccent.Light * fade);
			Txt(spriteBatch, vtext, new Vector2(x + 18f * s, at.Y), WeAccent.Light * fade, ts);
		}

		private static void DrawCompact(SpriteBatch spriteBatch, Rectangle card, float s, float fade)
		{
			DrawHeader(spriteBatch, card, s, fade, false, false);
			float top = card.Y + (HeadH + 10f) * s;
			var foot = new Rectangle(
				card.X + (int)(Pad * s), card.Bottom - (int)((Pad + 28f) * s),
				Math.Max(8, card.Width - (int)(Pad * 2f * s)), Math.Max(8, (int)(28f * s)));

			DiscordMember[] members = DiscordFeed.Snap.Members ?? Array.Empty<DiscordMember>();
			if (DiscordFeed.Status == DiscordFeedStatus.Ok && members.Length > 0) {
				float r = 14f * s;
				DrawAvatarStack(spriteBatch, members, new Vector2(card.X + Pad * s + r, top + r + 1f * s), r, card.Right - Pad * s, s, fade);
			}
			else
				DrawBodyNote(spriteBatch, new Vector2(card.X + Pad * s, top + 4f * s), card.Width - Pad * 2f * s, s, fade);

			// Footer: what is going on in voice, or how to set the widget up.
			WeTerra.Panel(spriteBatch, foot, WeTerra.RowBack * fade, Color.Black * (0.8f * fade));
			string icon;
			string line;
			Color color;
			if (!DiscordFeed.HasGuildId) {
				icon = WeIcons.Setting;
				line = WeText.UI("DiscordTapToSet");
				color = WeTerra.Muted;
			}
			else if (DiscordFeed.Status == DiscordFeedStatus.Ok && VoiceCount() > 0) {
				icon = WeIcons.Speaker;
				string room = OccupiedVoiceName();
				line = WeText.UI("DiscordInVoice").Replace("%n", VoiceCount().ToString()) + (room.Length > 0 ? "  ·  # " + room : "");
				color = WeAccent.Light;
			}
			else {
				icon = WeIcons.Mute;
				line = DiscordFeed.Status == DiscordFeedStatus.Ok ? WeText.UI("DiscordVoiceEmpty") : ShortStatus();
				color = WeTerra.Muted;
			}

			float ts = 0.66f * s;
			WeTerra.Icon(spriteBatch, icon, new Vector2(foot.X + 14f * s, foot.Center.Y), 16f * s, Color.White * fade);
			Txt(spriteBatch, Ellipsize(Font, line, foot.Width - 34f * s, ts),
				new Vector2(foot.X + 26f * s, foot.Center.Y - 8f * s), color * fade, ts);
		}

		private static void DrawBanner(SpriteBatch spriteBatch, Rectangle card, float s, float fade, bool hits)
		{
			DrawHeader(spriteBatch, card, s, fade, true, hits);
			var body = new Rectangle(
				card.X + (int)(Pad * s),
				card.Y + (int)((HeadH + 8f) * s),
				Math.Max(8, card.Width - (int)(Pad * 2f * s)),
				Math.Max(8, card.Height - (int)((HeadH + 8f + Pad) * s)));
			List<VoiceBand> bands = VoiceBands();
			if (bands.Count == 0 || DiscordFeed.Status != DiscordFeedStatus.Ok) {
				DrawBodyNote(spriteBatch, new Vector2(body.X, body.Y + 6f * s), body.Width, s, fade);
				DiscordMember[] members = DiscordFeed.Snap.Members ?? Array.Empty<DiscordMember>();
				if (members.Length > 0) {
					float r = 14f * s;
					DrawAvatarStack(spriteBatch, members, new Vector2(body.X + r, body.Y + 50f * s), r, body.Right, s, fade);
				}

				if (hits)
					_bodyMax = 0f;
				return;
			}

			float gap = 6f * s;
			float content = 0f;
			for (int i = 0; i < bands.Count; i++)
				content += BandHeight(bands[i], s) + gap;
			float max = Math.Max(0f, content - gap - body.Height);
			float scroll = hits ? ClampScroll(max) : 0f;
			void Body()
			{
				float y = body.Y - scroll;
				for (int i = 0; i < bands.Count; i++) {
					float h = BandHeight(bands[i], s);
					if (y + h >= body.Y && y <= body.Bottom)
						DrawVoiceBand(spriteBatch, bands[i], new Rectangle(body.X, (int)y, body.Width - (max > 4f ? 8 : 0), (int)h), s, fade, hits);
					y += h + gap;
				}

				if (hits && max > 4f)
					DrawScrollThumb(spriteBatch, body, scroll, max, fade);
			}

			if (hits)
				WeDraw.WithClip(spriteBatch, body, Body);
			else
				Body();
		}

		private static void DrawRoster(SpriteBatch spriteBatch, Rectangle card, float s, float fade, bool hits)
		{
			DrawHeader(spriteBatch, card, s, fade, false, false);
			DiscordMember[] members = DiscordFeed.Snap.Members ?? Array.Empty<DiscordMember>();
			var list = new Rectangle(
				card.X + (int)(Pad * s),
				card.Y + (int)((HeadH + 8f) * s),
				Math.Max(8, card.Width - (int)(Pad * 2f * s)),
				Math.Max(8, card.Height - (int)((HeadH + 8f + Pad) * s)));
			float rowH = 38f * s;
			float gap = 4f * s;
			if (members.Length == 0 || DiscordFeed.Status != DiscordFeedStatus.Ok) {
				DrawBodyNote(spriteBatch, new Vector2(list.X, list.Y + 6f * s), list.Width, s, fade);
				if (hits)
					_bodyMax = 0f;
				return;
			}

			float content = members.Length * (rowH + gap) - gap;
			float max = Math.Max(0f, content - list.Height);
			float scroll = hits ? ClampScroll(max) : 0f;
			void People()
			{
				float y = list.Y - scroll;
				int width = list.Width - (max > 4f ? 8 : 0);
				for (int i = 0; i < members.Length; i++) {
					if (y + rowH < list.Y) {
						y += rowH + gap;
						continue;
					}

					if (y > list.Bottom)
						break;
					DrawPerson(spriteBatch, members[i], new Rectangle(list.X, (int)y, width, (int)rowH), s, fade, hits);
					y += rowH + gap;
				}

				if (hits && max > 4f)
					DrawScrollThumb(spriteBatch, list, scroll, max, fade);
			}

			if (hits)
				WeDraw.WithClip(spriteBatch, list, People);
			else
				People();
		}

		/// <summary>Muted one-liner for the body when there is nothing to list (loading, errors, empty).</summary>
		private static void DrawBodyNote(SpriteBatch spriteBatch, Vector2 at, float width, float s, float fade)
		{
			string icon = DiscordFeed.Status switch {
				DiscordFeedStatus.Loading => WeIcons.Clock,
				DiscordFeedStatus.Ok => WeIcons.Mute,
				_ => DiscordFeed.HasGuildId ? WeIcons.Close : WeIcons.Setting
			};
			string text = DiscordFeed.Status == DiscordFeedStatus.Ok ? WeText.UI("DiscordEmpty") : ShortStatus();
			float ts = 0.68f * s;
			WeTerra.Icon(spriteBatch, icon, new Vector2(at.X + 8f * s, at.Y + 9f * s), 16f * s, Color.White * (0.85f * fade));
			Txt(spriteBatch, Ellipsize(Font, text, width - 24f * s, ts), new Vector2(at.X + 22f * s, at.Y), WeTerra.Muted * fade, ts);
		}

		private static void DrawVoiceBand(SpriteBatch spriteBatch, VoiceBand band, Rectangle rect, float s, float fade, bool hits)
		{
			if (band == null)
				return;

			bool busy = band.People.Count > 0;
			WeTerra.Panel(spriteBatch, rect, (busy ? WeTerra.RowHover : WeTerra.RowBack) * fade, Color.Black * (0.85f * fade));
			float ts = 0.7f * s;
			float headY = rect.Y + 9f * s;
			WeTerra.Icon(spriteBatch, busy ? WeIcons.Speaker : WeIcons.Mute, new Vector2(rect.X + 16f * s, headY + 8f * s), 16f * s, Color.White * fade);
			float right = rect.Right - 8f * s;
			if (DiscordFeed.HasGuildId) {
				string label = WeText.UI("DiscordJoinShort");
				float js = 0.62f * s;
				int w = (int)(TextW(label, js) + 22f * s);
				int h = Math.Max(12, (int)(22f * s));
				var join = new Rectangle((int)(right - w), (int)(headY - 2f * s), w, h);
				DrawJoinButton(spriteBatch, join, label, js, fade, hits, band.Id, false);
				right = join.X - 6f * s;
			}

			string title = "# " + (string.IsNullOrEmpty(band.Name) ? WeText.UI("DiscordVoice") : band.Name);
			string count = busy ? "  " + band.People.Count : "";
			float titleW = right - (rect.X + 30f * s);
			string fitted = Ellipsize(Font, title, titleW - TextW(count, ts), ts);
			Txt(spriteBatch, fitted, new Vector2(rect.X + 30f * s, headY), (busy ? Color.White : WeTerra.Muted) * fade, ts);
			if (busy)
				Txt(spriteBatch, count, new Vector2(rect.X + 30f * s + TextW(fitted, ts), headY), WeAccent.Light * fade, ts);
			if (!busy)
				return;

			float r = 11f * s;
			DrawAvatarStack(spriteBatch, band.People.ToArray(), new Vector2(rect.X + 14f * s + r, rect.Y + 44f * s), r, rect.Right - 8f * s, s, fade);
		}

		private static void DrawPerson(SpriteBatch spriteBatch, DiscordMember member, Rectangle row, float s, float fade, bool hits)
		{
			if (member == null)
				return;

			bool voice = !string.IsNullOrEmpty(member.Voice);
			bool hot = hits && row.Contains(Main.mouseX, Main.mouseY);
			WeTerra.Panel(spriteBatch, row, (hot ? WeTerra.RowHover : WeTerra.RowBack) * fade, Color.Black * (0.8f * fade));
			float right = row.Right - 8f * s;
			if (voice && DiscordFeed.HasGuildId) {
				string label = WeText.UI("DiscordJoinShort");
				float js = 0.6f * s;
				int w = (int)(TextW(label, js) + 20f * s);
				int h = Math.Max(12, (int)(22f * s));
				var join = new Rectangle((int)(right - w), (int)(row.Center.Y - h * 0.5f), w, h);
				DrawJoinButton(spriteBatch, join, label, js, fade, hits, member.ChannelId, false);
				right = join.X - 6f * s;
			}

			float r = 12f * s;
			DrawAvatar(spriteBatch, member, new Vector2(row.X + 8f * s + r, row.Center.Y), r, s, fade);
			float x = row.X + 16f * s + r * 2f;
			float width = right - x;
			if (!voice) {
				Txt(spriteBatch, Ellipsize(Font, member.Name, width, 0.74f * s), new Vector2(x, row.Center.Y - 9f * s), Color.White * fade, 0.74f * s);
				return;
			}

			Txt(spriteBatch, Ellipsize(Font, member.Name, width, 0.72f * s), new Vector2(x, row.Y + 3f * s), Color.White * fade, 0.72f * s);
			WeTerra.Icon(spriteBatch, WeIcons.Speaker, new Vector2(x + 6f * s, row.Y + 27f * s), 16f * s, WeAccent.Light * fade);
			Txt(spriteBatch, Ellipsize(Font, member.Voice, width - 16f * s, 0.6f * s), new Vector2(x + 15f * s, row.Y + 20f * s), WeAccent.Light * fade, 0.6f * s);
		}

		/// <summary>Overlapping avatars with a "+N" chip once they no longer fit.</summary>
		private static void DrawAvatarStack(SpriteBatch spriteBatch, DiscordMember[] members, Vector2 first, float r, float right, float s, float fade)
		{
			if (members == null || members.Length == 0)
				return;
			float step = r * 1.55f;
			int fit = Math.Max(1, (int)((right - first.X - r) / step) + 1);
			int shown = Math.Min(members.Length, fit);
			int rest = members.Length - shown;
			if (rest > 0 && shown > 1) {
				shown--;
				rest++;
			}

			for (int i = shown - 1; i >= 0; i--)
				DrawAvatar(spriteBatch, members[i], first + new Vector2(i * step, 0f), r, s, fade);
			if (rest <= 0)
				return;

			Vector2 at = first + new Vector2(shown * step, 0f);
			Texture2D circle = WeDraw.Circle();
			spriteBatch.Draw(circle, at, null, Color.Black * (0.9f * fade), 0f, circle.Size() * 0.5f, (r * 2f + 4f) / circle.Width, SpriteEffects.None, 0f);
			spriteBatch.Draw(circle, at, null, WeTerra.RowOn * fade, 0f, circle.Size() * 0.5f, r * 2f / circle.Width, SpriteEffects.None, 0f);
			string more = "+" + rest;
			float ts = Math.Min(0.62f * s, r * 1.7f / Math.Max(1f, Font.MeasureString(more).X));
			Vector2 size = Font.MeasureString(more) * ts;
			Txt(spriteBatch, more, at - size * 0.5f + new Vector2(0f, 2f * ts), Color.White * fade, ts);
		}

		private static void DrawJoinButton(SpriteBatch spriteBatch, Rectangle rect, string label, float ts, float fade, bool hits, string channel, bool icon)
		{
			if (rect.Width < 8 || rect.Height < 8)
				return;
			if (hits)
				Joins.Add((rect, channel ?? ""));
			bool hot = hits && rect.Contains(Main.mouseX, Main.mouseY);
			Color back = hot ? Color.Lerp(WeAccent.Mid, WeAccent.Light, 0.55f) : WeAccent.Mid;
			WeTerra.Panel(spriteBatch, rect, back * fade, (hot ? Color.White : Color.Black) * fade);
			float textW = TextW(label, ts);
			float x = rect.Center.X - textW * 0.5f;
			if (icon) {
				x += 11f;
				WeTerra.Icon(spriteBatch, WeIcons.Speaker, new Vector2(x - 14f, rect.Center.Y), 16f, Color.White * fade);
			}

			Txt(spriteBatch, label, new Vector2(x, rect.Center.Y - Font.MeasureString(label).Y * ts * 0.5f + 2f * ts), Color.White * fade, ts);
		}

		private static void DrawScrollThumb(SpriteBatch spriteBatch, Rectangle list, float scroll, float max, float fade)
		{
			var track = new Rectangle(list.Right - 5, list.Y + 2, 4, list.Height - 4);
			WeDraw.Fill(spriteBatch, track, Color.Black * (0.45f * fade));
			int h = Math.Max(12, (int)(track.Height * list.Height / (list.Height + max)));
			int travel = Math.Max(1, track.Height - h);
			int y = track.Y + (int)(travel * (scroll / max));
			WeDraw.Fill(spriteBatch, new Rectangle(track.X, y, track.Width, h), WeAccent.Light * (0.85f * fade));
		}

		private static void DrawBadge(SpriteBatch spriteBatch, Vector2 center, float radius, float fade)
		{
			Texture2D circle = WeDraw.Circle();
			// dark outline + accent ring, like a framed Terraria slot
			spriteBatch.Draw(circle, center, null, Color.Black * fade, 0f, circle.Size() * 0.5f, (radius * 2f + 6f) / circle.Width, SpriteEffects.None, 0f);
			spriteBatch.Draw(circle, center, null, WeAccent.Light * fade, 0f, circle.Size() * 0.5f, (radius * 2f + 3f) / circle.Width, SpriteEffects.None, 0f);
			Texture2D icon = DiscordFeed.Snap.Icon;
			if (icon != null && !icon.IsDisposed) {
				spriteBatch.Draw(circle, center, null, CardDeep * fade, 0f, circle.Size() * 0.5f, radius * 2f / circle.Width, SpriteEffects.None, 0f);
				spriteBatch.Draw(icon, center, null, Color.White * fade, 0f, icon.Size() * 0.5f, radius * 2f / Math.Max(1, icon.Width), SpriteEffects.None, 0f);
				return;
			}

			spriteBatch.Draw(circle, center, null, WeAccent.Deep * fade, 0f, circle.Size() * 0.5f, radius * 2f / circle.Width, SpriteEffects.None, 0f);
			string letter = DisplayName();
			letter = letter.Length > 0 ? char.ToUpperInvariant(letter[0]).ToString() : "D";
			var font = FontAssets.DeathText.Value;
			float scale = radius / 22f;
			Vector2 size = font.MeasureString(letter) * scale;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, font, Plain(letter), center - size * 0.5f + new Vector2(0f, 1f * scale),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(scale));
		}

		private static void DrawAvatar(SpriteBatch spriteBatch, DiscordMember member, Vector2 center, float radius, float s, float fade)
		{
			Texture2D circle = WeDraw.Circle();
			// dark ring so overlapping avatars stay separated
			spriteBatch.Draw(circle, center, null, Color.Black * fade, 0f, circle.Size() * 0.5f, (radius * 2f + 5f) / circle.Width, SpriteEffects.None, 0f);
			spriteBatch.Draw(circle, center, null, CardDeep * fade, 0f, circle.Size() * 0.5f, (radius * 2f + 1f) / circle.Width, SpriteEffects.None, 0f);
			if (member?.Avatar != null && !member.Avatar.IsDisposed) {
				spriteBatch.Draw(
					member.Avatar, center, null, Color.White * fade, 0f,
					member.Avatar.Size() * 0.5f, radius * 2f / Math.Max(1, member.Avatar.Width), SpriteEffects.None, 0f);
			}
			else {
				spriteBatch.Draw(circle, center, null, WeAccent.Dark * fade, 0f, circle.Size() * 0.5f, radius * 2f / circle.Width, SpriteEffects.None, 0f);
				string letter = !string.IsNullOrEmpty(member?.Name) ? char.ToUpperInvariant(member.Name[0]).ToString() : "?";
				float scale = radius / 14f;
				Vector2 size = Font.MeasureString(letter) * scale;
				Txt(spriteBatch, letter, center - size * 0.5f + new Vector2(0f, 2f * scale), Color.White * fade, scale);
			}

			Color pip = member?.Status switch {
				"idle" => Idle,
				"dnd" => Dnd,
				"offline" => Offline,
				_ => Online
			};
			Vector2 pipAt = center + new Vector2(radius * 0.68f, radius * 0.68f);
			Dot(spriteBatch, pipAt, Math.Max(3f, 4.5f * s), Color.Black * fade);
			Dot(spriteBatch, pipAt, Math.Max(2f, 3.2f * s), pip * fade);
		}

		private static void Dot(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
		{
			Texture2D circle = WeDraw.Circle();
			spriteBatch.Draw(circle, center, null, color, 0f, circle.Size() * 0.5f, radius * 2f / circle.Width, SpriteEffects.None, 0f);
		}

		private static void Divider(SpriteBatch spriteBatch, Rectangle card, float s, float y, float fade)
		{
			int x = card.X + (int)(Pad * s);
			int w = Math.Max(1, card.Width - (int)(Pad * 2f * s));
			WeDraw.Fill(spriteBatch, new Rectangle(x, (int)y, w, 2), Color.Black * (0.55f * fade));
			WeDraw.Fill(spriteBatch, new Rectangle(x, (int)y + 2, w, 1), WeAccent.Light * (0.3f * fade));
		}

		private static ReLogic.Graphics.DynamicSpriteFont Font => FontAssets.MouseText.Value;

		private static float TextW(string text, float scale) =>
			string.IsNullOrEmpty(text) ? 0f : Font.MeasureString(Plain(text)).X * scale;

		private static void Txt(SpriteBatch spriteBatch, string text, Vector2 pos, Color color, float scale)
		{
			if (string.IsNullOrEmpty(text))
				return;
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Font, Plain(text), pos, color, 0f, Vector2.Zero, new Vector2(scale));
		}

		private static float ClampScroll(float max)
		{
			_bodyMax = max;
			_bodyScroll = MathHelper.Clamp(_bodyScroll, 0f, _bodyMax);
			return _bodyScroll;
		}

		private static float BandHeight(VoiceBand band, float s) =>
			(band != null && band.People.Count > 0 ? 62f : 36f) * s;

		private static List<VoiceBand> VoiceBands()
		{
			var bands = new List<VoiceBand>();
			var index = new Dictionary<string, VoiceBand>(StringComparer.Ordinal);
			DiscordSnap snap = DiscordFeed.Snap;
			if (snap.Channels != null) {
				for (int i = 0; i < snap.Channels.Length; i++) {
					DiscordChan ch = snap.Channels[i];
					if (ch == null || string.IsNullOrEmpty(ch.Id) || index.ContainsKey(ch.Id))
						continue;
					var band = new VoiceBand {
						Id = ch.Id,
						Name = string.IsNullOrEmpty(ch.Name) ? WeText.UI("DiscordVoice") : ch.Name
					};
					index[ch.Id] = band;
					bands.Add(band);
				}
			}

			DiscordMember[] members = snap.Members ?? Array.Empty<DiscordMember>();
			for (int i = 0; i < members.Length; i++) {
				DiscordMember member = members[i];
				if (member == null || string.IsNullOrEmpty(member.ChannelId))
					continue;
				if (!index.TryGetValue(member.ChannelId, out VoiceBand band)) {
					band = new VoiceBand {
						Id = member.ChannelId,
						Name = string.IsNullOrEmpty(member.Voice) ? WeText.UI("DiscordVoice") : member.Voice
					};
					index[member.ChannelId] = band;
					bands.Add(band);
				}

				band.People.Add(member);
			}

			bands.Sort((a, b) => {
				int occ = (b.People.Count > 0 ? 1 : 0).CompareTo(a.People.Count > 0 ? 1 : 0);
				return occ != 0 ? occ : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			});
			return bands;
		}

		private static string OccupiedVoiceName()
		{
			List<VoiceBand> bands = VoiceBands();
			for (int i = 0; i < bands.Count; i++) {
				if (bands[i].People.Count > 0)
					return bands[i].Name;
			}

			return "";
		}

		private static string DisplayName()
		{
			if (!string.IsNullOrEmpty(DiscordFeed.Snap.Name))
				return DiscordFeed.Snap.Name;
			return WeText.UI("AddDiscord");
		}

		private static string HeaderSub()
		{
			if (DiscordFeed.Status != DiscordFeedStatus.Ok)
				return ShortStatus();
			int voice = VoiceCount();
			if (voice > 0)
				return OnlineLabel(DiscordFeed.Snap.Presence) + "  ·  " + WeText.UI("DiscordInVoice").Replace("%n", voice.ToString());
			return OnlineLabel(DiscordFeed.Snap.Presence);
		}

		private static string CompactSub()
		{
			if (DiscordFeed.Status == DiscordFeedStatus.Ok)
				return OnlineLabel(DiscordFeed.Snap.Presence);
			return ShortStatus();
		}

		private static string BannerEmptyLine()
		{
			if (DiscordFeed.Status == DiscordFeedStatus.Ok)
				return WeText.UI("DiscordVoiceEmpty");
			return ShortStatus();
		}

		private static int VoiceCount()
		{
			int n = 0;
			DiscordMember[] members = DiscordFeed.Snap.Members;
			if (members == null)
				return 0;
			for (int i = 0; i < members.Length; i++) {
				if (!string.IsNullOrEmpty(members[i]?.Voice))
					n++;
			}

			return n;
		}

		private static string ShortStatus()
		{
			if (!DiscordFeed.HasGuildId)
				return WeText.UI("DiscordTapToSet");
			return DiscordFeed.Status switch {
				DiscordFeedStatus.Loading => WeText.UI("DiscordLoading"),
				DiscordFeedStatus.NeedWidget => WeText.UI("DiscordNeedWidgetShort"),
				DiscordFeedStatus.BadId => WeText.UI("DiscordBadId"),
				DiscordFeedStatus.NetError => WeText.UI("DiscordNetError"),
				DiscordFeedStatus.Typing => WeText.UI("DiscordNeedDigits"),
				_ => WeText.UI("DiscordEmpty")
			};
		}

		private static string OnlineLabel(int count) =>
			WeText.UI("DiscordOnline").Replace("%n", count.ToString());

		private static string Plain(string text)
		{
			if (string.IsNullOrEmpty(text))
				return "";
			return text.Replace("[", "(").Replace("]", ")");
		}

		private static string Ellipsize(ReLogic.Graphics.DynamicSpriteFont font, string text, float max, float scale)
		{
			if (string.IsNullOrEmpty(text) || font.MeasureString(text).X * scale <= max)
				return text ?? "";

			string cut = text;
			while (cut.Length > 1 && font.MeasureString(cut + "...").X * scale > max)
				cut = cut[..^1];
			return cut + "...";
		}

		private sealed class VoiceBand
		{
			internal string Id = "";
			internal string Name = "";
			internal readonly List<DiscordMember> People = new();
		}
	}
}
