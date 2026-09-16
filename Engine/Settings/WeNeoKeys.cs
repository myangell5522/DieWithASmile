using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using DieWithASmile.Engine.Core;
using DieWithASmile.Engine.UI;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoKeys
	{
		private static string _listen;
		private static string _pick = "Up";
		private static int _filter;
		private static readonly string[] Filters = { "All", "Move", "Inv", "Misc", "Mods" };
		private static readonly string[] FilterKeys = { "NeoKeysAll", "NeoKeysMove", "NeoKeysInv", "NeoKeysMisc", "NeoKeysMods" };

		private static readonly string[][] Rows =
		{
			new[] { "Escape", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" },
			new[] { "OemTilde", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0", "OemMinus", "OemPlus", "Back" },
			new[] { "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "OemOpenBrackets", "OemCloseBrackets" },
			new[] { "LeftControl", "A", "S", "D", "F", "G", "H", "J", "K", "L", "OemSemicolon", "OemQuotes", "Enter" },
			new[] { "LeftShift", "Z", "X", "C", "V", "B", "N", "M", "OemComma", "OemPeriod", "OemQuestion", "RightShift" },
			new[] { "LeftAlt", "Space", "RightAlt", "Mouse1", "Mouse2", "Mouse3" }
		};

		internal static bool Capturing => !string.IsNullOrEmpty(_listen);

		internal static void Cancel() => _listen = null;

		internal static void Draw(SpriteBatch spriteBatch, Rectangle view, ref int y, float fade)
		{
			WeNeoShell.Header(spriteBatch, view, ref y, WeText.UI("NeoOpenKeybinds"), fade);
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, WeText.UI(Capturing ? "NeoKeysWaiting" : "NeoKeysHint"),
				new Vector2(view.X + 8, y), Color.White * (0.6f * fade), 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
			y += 22;

			int kbW = (int)(view.Width * 0.62f);
			int listW = view.Width - kbW - 12;
			var kb = new Rectangle(view.X + 4, y, kbW - 8, 210);
			var list = new Rectangle(view.X + kbW + 4, y, listW - 4, 210);
			DrawKeyboard(spriteBatch, kb, fade);
			DrawList(spriteBatch, list, fade);
			y += 218;

			int btnW = (view.Width - 20) / 2;
			DrawBtn(spriteBatch, new Rectangle(view.X + 4, y, btnW - 4, 32), WeText.UI("NeoKeysReset"), fade);
			DrawBtn(spriteBatch, new Rectangle(view.X + 12 + btnW, y, btnW - 4, 32), WeText.UI("NeoKeysResetAll"), fade);
			y += 40;
		}

		internal static bool Click(Rectangle view, ref int y, bool left, bool right)
		{
			WeNeoShell.SkipHeader(ref y);
			y += 22;
			int kbW = (int)(view.Width * 0.62f);
			int listW = view.Width - kbW - 12;
			var kb = new Rectangle(view.X + 4, y, kbW - 8, 210);
			var list = new Rectangle(view.X + kbW + 4, y, listW - 4, 210);
			y += 218;
			int btnW = (view.Width - 20) / 2;
			var reset = new Rectangle(view.X + 4, y, btnW - 4, 32);
			var resetAll = new Rectangle(view.X + 12 + btnW, y, btnW - 4, 32);
			y += 40;

			if (Capturing) {
				if (right || (left && !kb.Contains(Main.mouseX, Main.mouseY) && !list.Contains(Main.mouseX, Main.mouseY))) {
					Cancel();
					return true;
				}
			}

			if (left && ClickKeyboard(kb))
				return true;
			if (left && ClickList(list))
				return true;
			if (left && reset.Contains(Main.mouseX, Main.mouseY)) {
				ResetOne(_pick);
				return true;
			}

			if (left && resetAll.Contains(Main.mouseX, Main.mouseY)) {
				ResetAll();
				return true;
			}

			return false;
		}

		internal static bool TickCapture()
		{
			if (!Capturing)
				return false;

			Keys[] keys = Main.keyState.GetPressedKeys();
			Keys[] prev = Main.oldKeyState.GetPressedKeys();
			foreach (Keys key in keys) {
				if (key == Keys.Escape || key == Keys.None)
					continue;
				bool was = false;
				foreach (Keys p in prev) {
					if (p == key)
						was = true;
				}

				if (was)
					continue;
				Assign(key.ToString());
				return true;
			}

			return false;
		}

		private static void DrawKeyboard(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			WeDraw.Fill(spriteBatch, box, new Color(16, 18, 24) * fade);
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * (0.5f * fade));
			HashSet<string> used = UsedKeys();
			int rowH = (box.Height - 12) / Rows.Length;
			for (int r = 0; r < Rows.Length; r++) {
				string[] row = Rows[r];
				int x = box.X + 6;
				int y = box.Y + 6 + r * rowH;
				int cellW = Math.Max(22, (box.Width - 12) / row.Length - 2);
				for (int c = 0; c < row.Length; c++) {
					string id = row[c];
					int w = cellW;
					if (id is "Space" or "LeftShift" or "RightShift" or "Enter" or "Back" or "Tab")
						w = (int)(cellW * 1.55f);
					var hit = new Rectangle(x, y, Math.Min(w, box.Right - 6 - x), rowH - 3);
					if (hit.Width < 8)
						break;
					bool on = used.Contains(id);
					bool hover = hit.Contains(Main.mouseX, Main.mouseY);
					bool listen = Capturing && hover;
					WeDraw.Fill(spriteBatch, hit, (on || listen ? WeAccent.Deep : new Color(28, 30, 36)) * fade);
					WeDraw.Border(spriteBatch, hit, (on || hover ? WeAccent.Light : Color.White * 0.18f) * fade);
					string label = Short(id);
					Vector2 size = FontAssets.MouseText.Value.MeasureString(label) * 0.52f;
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, FontAssets.MouseText.Value, label,
						new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
						Color.White * fade, 0f, Vector2.Zero, new Vector2(0.52f));
					x += w + 2;
				}
			}
		}

		private static bool ClickKeyboard(Rectangle box)
		{
			int rowH = (box.Height - 12) / Rows.Length;
			for (int r = 0; r < Rows.Length; r++) {
				string[] row = Rows[r];
				int x = box.X + 6;
				int y = box.Y + 6 + r * rowH;
				int cellW = Math.Max(22, (box.Width - 12) / row.Length - 2);
				for (int c = 0; c < row.Length; c++) {
					string id = row[c];
					int w = cellW;
					if (id is "Space" or "LeftShift" or "RightShift" or "Enter" or "Back" or "Tab")
						w = (int)(cellW * 1.55f);
					var hit = new Rectangle(x, y, Math.Min(w, box.Right - 6 - x), rowH - 3);
					if (hit.Contains(Main.mouseX, Main.mouseY)) {
						if (Capturing) {
							Assign(id);
							return true;
						}

						_pick = ActionUsing(id) ?? _pick;
						_listen = _pick;
						return true;
					}

					x += w + 2;
				}
			}

			return false;
		}

		private static void DrawList(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			WeDraw.Fill(spriteBatch, box, new Color(16, 18, 24) * fade);
			WeDraw.Border(spriteBatch, box, WeAccent.Mid * (0.5f * fade));
			int fw = (box.Width - 10) / Filters.Length;
			for (int i = 0; i < Filters.Length; i++) {
				var chip = new Rectangle(box.X + 4 + i * fw, box.Y + 4, fw - 3, 20);
				bool on = _filter == i;
				WeDraw.Fill(spriteBatch, chip, (on ? WeAccent.Deep : new Color(28, 30, 36)) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(FilterKeys[i]),
					new Vector2(chip.X + 4, chip.Y + 2), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.5f));
			}

			List<(string id, string label, string value)> acts = Actions();
			int rowY = box.Y + 28;
			int shown = 0;
			foreach ((string id, string label, string value) in acts) {
				if (rowY + 18 > box.Bottom - 4)
					break;
				var hit = new Rectangle(box.X + 4, rowY, box.Width - 8, 18);
				bool on = _pick == id || _listen == id;
				bool hover = hit.Contains(Main.mouseX, Main.mouseY);
				if (on || hover)
					WeDraw.Fill(spriteBatch, hit, WeAccent.Deep * (0.55f * fade));
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, label,
					new Vector2(hit.X + 4, hit.Y + 1), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.58f));
				Vector2 vs = FontAssets.MouseText.Value.MeasureString(value) * 0.58f;
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, value,
					new Vector2(hit.Right - 4 - vs.X, hit.Y + 1), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(0.58f));
				rowY += 20;
				shown++;
			}
		}

		private static bool ClickList(Rectangle box)
		{
			int fw = (box.Width - 10) / Filters.Length;
			for (int i = 0; i < Filters.Length; i++) {
				var chip = new Rectangle(box.X + 4 + i * fw, box.Y + 4, fw - 3, 20);
				if (chip.Contains(Main.mouseX, Main.mouseY)) {
					_filter = i;
					return true;
				}
			}

			List<(string id, string label, string value)> acts = Actions();
			int rowY = box.Y + 28;
			foreach ((string id, string label, string value) in acts) {
				var hit = new Rectangle(box.X + 4, rowY, box.Width - 8, 18);
				if (hit.Contains(Main.mouseX, Main.mouseY)) {
					_pick = id;
					_listen = id;
					return true;
				}

				rowY += 20;
				if (rowY + 18 > box.Bottom - 4)
					break;
			}

			return false;
		}

		private static List<(string id, string label, string value)> Actions()
		{
			var list = new List<(string, string, string)>();
			string q = WeNeoMenu.Search;
			Dictionary<string, List<string>> map = KeyMap();
			foreach (KeyValuePair<string, List<string>> pair in map) {
				int bucket = Bucket(pair.Key);
				if (_filter != 0 && _filter != 4 && bucket != _filter)
					continue;
				if (_filter == 4)
					continue;
				string label = Nice(pair.Key);
				string value = pair.Value != null && pair.Value.Count > 0 ? Short(pair.Value[0]) : "-";
				if (!WeNeoShell.Matches(q, label, value, pair.Key))
					continue;
				list.Add((pair.Key, label, value));
			}

			if (_filter is 0 or 4) {
				foreach ((string id, string label) in ModBinds()) {
					string value = ModBindValue(id);
					if (!WeNeoShell.Matches(q, label, value, id))
						continue;
					list.Add(("mod:" + id, label, value));
				}
			}

			return list;
		}

		private static int Bucket(string id)
		{
			if (id.Contains("Up", StringComparison.OrdinalIgnoreCase) || id.Contains("Down") || id.Contains("Left") ||
			    id.Contains("Right") || id.Contains("Jump") || id.Contains("Grapple") || id.Contains("Smart"))
				return 1;
			if (id.Contains("Inventory") || id.Contains("Hotbar") || id.Contains("Quick") || id.Contains("Map") ||
			    id.Contains("Chest") || id.Contains("Trash"))
				return 2;
			return 3;
		}

		private static Dictionary<string, List<string>> KeyMap()
		{
			var map = new Dictionary<string, List<string>>();
			try {
				object modes = PlayerInput.CurrentProfile?.InputModes;
				if (modes == null)
					return map;
				object kb = null;
				if (modes is IDictionary dict) {
					foreach (DictionaryEntry e in dict) {
						if (e.Key.ToString()?.Contains("Keyboard") == true)
							kb = e.Value;
					}
				}

				object status = kb?.GetType().GetField("KeyStatus")?.GetValue(kb)
				                ?? kb?.GetType().GetProperty("KeyStatus")?.GetValue(kb);
				if (status is Dictionary<string, List<string>> typed)
					return typed;
				if (status is IDictionary raw) {
					foreach (DictionaryEntry e in raw) {
						if (e.Key is string k) {
							var vals = new List<string>();
							if (e.Value is IEnumerable en) {
								foreach (object o in en)
									vals.Add(o?.ToString() ?? "");
							}

							map[k] = vals;
						}
					}
				}
			}
			catch {
			}

			return map;
		}

		private static HashSet<string> UsedKeys()
		{
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (List<string> vals in KeyMap().Values) {
				foreach (string v in vals)
					set.Add(v);
			}

			return set;
		}

		private static string ActionUsing(string key)
		{
			foreach (KeyValuePair<string, List<string>> pair in KeyMap()) {
				if (pair.Value != null && pair.Value.Exists(v => string.Equals(v, key, StringComparison.OrdinalIgnoreCase)))
					return pair.Key;
			}

			return null;
		}

		private static void Assign(string key)
		{
			string id = _listen ?? _pick;
			if (string.IsNullOrEmpty(id))
				return;
			if (id.StartsWith("mod:")) {
				SetModBind(id[4..], key);
			}
			else {
				Dictionary<string, List<string>> map = KeyMap();
				if (map.TryGetValue(id, out List<string> list)) {
					list.Clear();
					list.Add(key);
				}
			}

			_listen = null;
			try {
				PlayerInput.CurrentProfile?.Save();
			}
			catch {
			}

			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		private static void ResetOne(string id)
		{
			if (string.IsNullOrEmpty(id))
				return;
			try {
				CopyDefault(id);
			}
			catch {
			}

			_listen = null;
		}

		private static void ResetAll()
		{
			try {
				object orig = typeof(PlayerInput).GetField("OriginalProfiles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
				if (orig is IDictionary dict) {
					foreach (DictionaryEntry e in dict) {
						object profile = e.Value;
						profile?.GetType().GetMethod("CopyContentsFrom")?.Invoke(PlayerInput.CurrentProfile, new[] { profile });
						profile?.GetType().GetMethod("CopyFrom")?.Invoke(PlayerInput.CurrentProfile, new[] { profile });
						break;
					}
				}
			}
			catch {
			}

			_listen = null;
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		private static void CopyDefault(string id)
		{
			object orig = typeof(PlayerInput).GetField("OriginalProfiles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
			if (orig is not IDictionary dict)
				return;
			foreach (DictionaryEntry e in dict) {
				object kb = ModeOf(e.Value);
				object status = kb?.GetType().GetField("KeyStatus")?.GetValue(kb) ?? kb?.GetType().GetProperty("KeyStatus")?.GetValue(kb);
				if (status is Dictionary<string, List<string>> typed && typed.TryGetValue(id, out List<string> src) &&
				    KeyMap().TryGetValue(id, out List<string> dst)) {
					dst.Clear();
					dst.AddRange(src);
					return;
				}
			}
		}

		private static object ModeOf(object profile)
		{
			object modes = profile?.GetType().GetField("InputModes")?.GetValue(profile)
			               ?? profile?.GetType().GetProperty("InputModes")?.GetValue(profile);
			if (modes is IDictionary dict) {
				foreach (DictionaryEntry e in dict) {
					if (e.Key.ToString()?.Contains("Keyboard") == true)
						return e.Value;
				}
			}

			return null;
		}

		private static IEnumerable<(string id, string label)> ModBinds()
		{
			Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.KeybindLoader");
			object raw = type == null ? null : WeNeoFld.Get(type, "Keybinds") ?? WeNeoFld.Get(type, "ModKeybinds");
			if (raw is not IEnumerable en)
				yield break;
			foreach (object bind in en) {
				string name = bind.GetType().GetProperty("FullName")?.GetValue(bind)?.ToString()
				              ?? bind.GetType().GetProperty("Name")?.GetValue(bind)?.ToString();
				if (string.IsNullOrEmpty(name))
					continue;
				object display = bind.GetType().GetProperty("DisplayName")?.GetValue(bind);
				string label = display?.GetType().GetProperty("Value")?.GetValue(display)?.ToString() ?? Nice(name);
				yield return (name, label);
			}
		}

		private static string ModBindValue(string id)
		{
			foreach ((string name, string label) in ModBinds()) {
				if (name != id)
					continue;
			}

			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.KeybindLoader");
				object raw = type == null ? null : WeNeoFld.Get(type, "Keybinds") ?? WeNeoFld.Get(type, "ModKeybinds");
				if (raw is IEnumerable en) {
					foreach (object bind in en) {
						string name = bind.GetType().GetProperty("FullName")?.GetValue(bind)?.ToString();
						if (name != id)
							continue;
						MethodInfo get = bind.GetType().GetMethod("GetAssignedKeys", Type.EmptyTypes)
						                 ?? bind.GetType().GetMethod("GetAssignedKeys", new[] { typeof(InputMode) });
						object keys = get?.GetParameters().Length == 0 ? get.Invoke(bind, null) : get?.Invoke(bind, new object[] { InputMode.Keyboard });
						if (keys is IList list && list.Count > 0)
							return Short(list[0]?.ToString() ?? "-");
					}
				}
			}
			catch {
			}

			return "-";
		}

		private static void SetModBind(string id, string key)
		{
			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.KeybindLoader");
				object raw = type == null ? null : WeNeoFld.Get(type, "Keybinds") ?? WeNeoFld.Get(type, "ModKeybinds");
				if (raw is not IEnumerable en)
					return;
				foreach (object bind in en) {
					string name = bind.GetType().GetProperty("FullName")?.GetValue(bind)?.ToString();
					if (name != id)
						continue;
					MethodInfo set = bind.GetType().GetMethod("SetAssignedKeys") ?? bind.GetType().GetMethod("SetKey");
					set?.Invoke(bind, set.GetParameters().Length == 1 ? new object[] { key } : new object[] { InputMode.Keyboard, key });
					var current = bind.GetType().GetField("current") ?? bind.GetType().GetField("_binding");
					return;
				}
			}
			catch {
			}
		}

		private static void DrawBtn(SpriteBatch spriteBatch, Rectangle hit, string text, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * WeNeoShell.TypeSmall;
			ChatManager.DrawColorCodedStringWithShadow(
				spriteBatch, FontAssets.MouseText.Value, text,
				new Vector2(hit.X + (hit.Width - size.X) * 0.5f, hit.Y + (hit.Height - size.Y) * 0.5f),
				Color.White * fade, 0f, Vector2.Zero, new Vector2(WeNeoShell.TypeSmall));
		}

		private static string Short(string id)
		{
			return id switch {
				"LeftControl" => "LCL",
				"RightControl" => "RCL",
				"LeftShift" => "LSH",
				"RightShift" => "RSH",
				"LeftAlt" => "LAL",
				"RightAlt" => "RAL",
				"OemTilde" => "`",
				"OemMinus" => "-",
				"OemPlus" => "=",
				"OemOpenBrackets" => "[",
				"OemCloseBrackets" => "]",
				"OemSemicolon" => ";",
				"OemQuotes" => "'",
				"OemComma" => ",",
				"OemPeriod" => ".",
				"OemQuestion" => "/",
				"Escape" => "ESC",
				"Back" => "BKS",
				"Enter" => "ENT",
				"Tab" => "TAB",
				"Space" => "SPC",
				"Mouse1" => "ML",
				"Mouse2" => "MR",
				"Mouse3" => "MM",
				_ => id.StartsWith("D") && id.Length == 2 ? id[1..] : id.Length > 3 ? id[..3] : id
			};
		}

		private static string Nice(string id)
		{
			if (string.IsNullOrEmpty(id))
				return "";
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < id.Length; i++) {
				if (i > 0 && char.IsUpper(id[i]))
					sb.Append(' ');
				sb.Append(id[i]);
			}

			return sb.ToString();
		}
	}
}
