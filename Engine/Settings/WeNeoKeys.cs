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
		private static int _listScroll;
		private static int _listMax;
		private static Rectangle _listHit;
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

		internal static bool ApplyWheel(int delta)
		{
			if (delta == 0 || WeNeoMenu.Category != WeNeoCat.Controls)
				return false;
			if (_listHit.Width < 8 || !_listHit.Contains(Main.mouseX, Main.mouseY) || _listMax < 1)
				return false;
			_listScroll = Math.Clamp(_listScroll - delta / 6, 0, _listMax);
			return true;
		}

		private static void DrawKeyboard(SpriteBatch spriteBatch, Rectangle box, float fade)
		{
			WeDraw.Fill(spriteBatch, box, new Color(14, 16, 22) * fade);
			WeDraw.Frame(spriteBatch, box, fade * 0.9f);
			WeDraw.Corners(spriteBatch, box, fade, 8);
			HashSet<string> used = UsedKeys();
			string pickKey = PickKey();
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
					bool mine = string.Equals(pickKey, id, StringComparison.OrdinalIgnoreCase);
					bool hover = hit.Contains(Main.mouseX, Main.mouseY);
					bool listen = Capturing && hover;
					Color fill = listen || mine
						? WeAccent.Mid
						: on
							? WeAccent.Deep
							: new Color(30, 32, 40);
					WeDraw.Fill(spriteBatch, hit, fill * fade);
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X + 1, hit.Y + 1, hit.Width - 2, 1), Color.White * (0.2f * fade));
					WeDraw.Fill(spriteBatch, new Rectangle(hit.X + 1, hit.Bottom - 2, hit.Width - 2, 1), Color.Black * (0.35f * fade));
					WeDraw.Border(spriteBatch, hit, (on || hover || mine ? WeAccent.Light : Color.White * 0.16f) * fade);
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
			_listHit = box;
			WeDraw.Fill(spriteBatch, box, new Color(14, 16, 22) * fade);
			WeDraw.Frame(spriteBatch, box, fade * 0.9f);
			int fw = (box.Width - 10) / Filters.Length;
			for (int i = 0; i < Filters.Length; i++) {
				var chip = new Rectangle(box.X + 4 + i * fw, box.Y + 4, fw - 3, 20);
				bool on = _filter == i;
				WeDraw.Fill(spriteBatch, chip, (on ? WeAccent.Deep : new Color(28, 30, 36)) * fade);
				if (on)
					WeDraw.Hairline(spriteBatch, chip, fade);
				WeDraw.Border(spriteBatch, chip, (on ? WeAccent.Light : Color.White * 0.14f) * fade);
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, WeText.UI(FilterKeys[i]),
					new Vector2(chip.X + 4, chip.Y + 2), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.5f));
			}

			List<(string id, string label, string value)> acts = Actions();
			var rows = new Rectangle(box.X + 2, box.Y + 28, box.Width - 4, box.Height - 32);
			_listMax = Math.Max(0, acts.Count * 22 - rows.Height);
			_listScroll = Math.Clamp(_listScroll, 0, _listMax);
			if (acts.Count == 0) {
				string empty = WeText.UI(_filter == 4 ? "NeoKeysNoMods" : "NeoKeysHint");
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch, FontAssets.MouseText.Value, empty,
					new Vector2(rows.X + 8, rows.Y + 8), Color.White * (0.55f * fade), 0f, Vector2.Zero,
					new Vector2(0.58f));
				return;
			}

			WeDraw.WithClip(spriteBatch, rows, () => {
				int rowY = rows.Y - _listScroll;
				foreach ((string id, string label, string value) in acts) {
					if (rowY + 22 < rows.Y) {
						rowY += 22;
						continue;
					}

					if (rowY > rows.Bottom)
						break;
					var hit = new Rectangle(rows.X + 2, rowY, rows.Width - 4, 20);
					bool on = _pick == id || _listen == id;
					bool hover = hit.Contains(Main.mouseX, Main.mouseY);
					if (on || hover)
						WeDraw.Fill(spriteBatch, hit, WeAccent.Deep * ((on ? 0.7f : 0.45f) * fade));
					if (on)
						WeDraw.Fill(spriteBatch, new Rectangle(hit.X, hit.Y, 3, hit.Height), WeAccent.Light * fade);
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, FontAssets.MouseText.Value, label,
						new Vector2(hit.X + 8, hit.Y + 2), Color.White * fade, 0f, Vector2.Zero, new Vector2(0.56f));
					string shown = string.IsNullOrEmpty(value) ? "-" : value;
					Vector2 vs = FontAssets.MouseText.Value.MeasureString(shown) * 0.52f;
					var chip = new Rectangle(hit.Right - (int)vs.X - 14, hit.Y + 2, (int)vs.X + 10, hit.Height - 4);
					WeDraw.Fill(spriteBatch, chip, WeAccent.Deep * (0.85f * fade));
					WeDraw.Border(spriteBatch, chip, WeAccent.Mid * fade);
					ChatManager.DrawColorCodedStringWithShadow(
						spriteBatch, FontAssets.MouseText.Value, shown,
						new Vector2(chip.X + 5, chip.Y + 1), WeAccent.Light * fade, 0f, Vector2.Zero, new Vector2(0.52f));
					rowY += 22;
				}
			});
		}

		private static bool ClickList(Rectangle box)
		{
			int fw = (box.Width - 10) / Filters.Length;
			for (int i = 0; i < Filters.Length; i++) {
				var chip = new Rectangle(box.X + 4 + i * fw, box.Y + 4, fw - 3, 20);
				if (chip.Contains(Main.mouseX, Main.mouseY)) {
					_filter = i;
					_listScroll = 0;
					return true;
				}
			}

			List<(string id, string label, string value)> acts = Actions();
			var rows = new Rectangle(box.X + 2, box.Y + 28, box.Width - 4, box.Height - 32);
			int rowY = rows.Y - _listScroll;
			foreach ((string id, string label, string value) in acts) {
				if (rowY + 22 < rows.Y) {
					rowY += 22;
					continue;
				}

				if (rowY > rows.Bottom)
					break;
				var hit = new Rectangle(rows.X + 2, rowY, rows.Width - 4, 20);
				if (hit.Contains(Main.mouseX, Main.mouseY)) {
					_pick = id;
					_listen = id;
					return true;
				}

				rowY += 22;
			}

			return false;
		}

		private static List<(string id, string label, string value)> Actions()
		{
			var list = new List<(string, string, string)>();
			string q = WeNeoMenu.Search;
			Dictionary<string, List<string>> map = KeyMap();
			foreach (KeyValuePair<string, List<string>> pair in map) {
				if (IsModStatusKey(pair.Key))
					continue;
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

		private static bool IsModStatusKey(string id) =>
			!string.IsNullOrEmpty(id) && (id.Contains('/') || id.Contains(':'));

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
				if (pair.Value == null || !pair.Value.Exists(v => string.Equals(v, key, StringComparison.OrdinalIgnoreCase)))
					continue;
				return IsModStatusKey(pair.Key) ? "mod:" + pair.Key : pair.Key;
			}

			return null;
		}

		private static string PickKey()
		{
			string id = _listen ?? _pick;
			if (string.IsNullOrEmpty(id))
				return "";
			if (id.StartsWith("mod:"))
				id = id[4..];
			if (KeyMap().TryGetValue(id, out List<string> list) && list != null && list.Count > 0)
				return list[0];
			return "";
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
				if (id.StartsWith("mod:")) {
					string full = id[4..];
					if (!CopyDefault(full))
						SetModBind(full, DefaultModBind(full));
				}
				else {
					CopyDefault(id);
				}
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

		private static bool CopyDefault(string id)
		{
			object orig = typeof(PlayerInput).GetField("OriginalProfiles", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
			if (orig is not IDictionary dict)
				return false;
			foreach (DictionaryEntry e in dict) {
				object kb = ModeOf(e.Value);
				object status = kb?.GetType().GetField("KeyStatus")?.GetValue(kb) ?? kb?.GetType().GetProperty("KeyStatus")?.GetValue(kb);
				if (status is Dictionary<string, List<string>> typed && typed.TryGetValue(id, out List<string> src)) {
					Dictionary<string, List<string>> map = KeyMap();
					if (!map.TryGetValue(id, out List<string> dst)) {
						dst = new List<string>();
						map[id] = dst;
					}

					dst.Clear();
					dst.AddRange(src);
					return true;
				}
			}

			return false;
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
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (object bind in RawModBinds()) {
				string id = BindFullName(bind);
				if (string.IsNullOrEmpty(id) || !seen.Add(id))
					continue;
				yield return (id, BindLabel(bind, id));
			}

			foreach (string key in KeyMap().Keys) {
				if (!IsModStatusKey(key) || !seen.Add(key))
					continue;
				yield return (key, Nice(key.Replace('/', ' ')));
			}
		}

		private static IEnumerable<object> RawModBinds()
		{
			Type type = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.KeybindLoader");
			object raw = type == null
				? null
				: WeNeoFld.Get(type, "modKeybinds") ?? WeNeoFld.Get(type, "Keybinds") ?? WeNeoFld.Get(type, "ModKeybinds");
			if (raw == null)
				yield break;
			if (raw is IDictionary dict) {
				foreach (DictionaryEntry e in dict) {
					if (e.Value != null)
						yield return e.Value;
				}

				yield break;
			}

			if (raw is not IEnumerable en)
				yield break;
			foreach (object item in en) {
				if (item == null)
					continue;
				object bind = item;
				if (item.GetType().Name.Contains("KeyValuePair"))
					bind = Inst(item, "Value") ?? item;
				if (bind != null)
					yield return bind;
			}
		}

		private static string BindFullName(object bind)
		{
			string name = Inst(bind, "FullName")?.ToString();
			if (!string.IsNullOrEmpty(name))
				return name;
			object mod = Inst(bind, "Mod");
			string modName = Inst(mod, "Name")?.ToString();
			string key = Inst(bind, "Name")?.ToString();
			if (!string.IsNullOrEmpty(modName) && !string.IsNullOrEmpty(key))
				return modName + "/" + key;
			return key;
		}

		private static string BindLabel(object bind, string id)
		{
			string display = TextOf(Inst(bind, "DisplayName"));
			string modTitle = TextOf(Inst(Inst(bind, "Mod"), "DisplayName")) ?? Inst(Inst(bind, "Mod"), "Name")?.ToString();
			string stem = id;
			int slash = id.IndexOf('/');
			if (slash >= 0 && slash + 1 < id.Length)
				stem = id[(slash + 1)..];
			string bindName = string.IsNullOrEmpty(display) ? Nice(stem) : display;
			if (string.IsNullOrEmpty(modTitle))
				return bindName;
			return modTitle + " · " + bindName;
		}

		private static string ModBindValue(string id)
		{
			if (KeyMap().TryGetValue(id, out List<string> mapped) && mapped != null && mapped.Count > 0)
				return Short(mapped[0]);
			try {
				foreach (object bind in RawModBinds()) {
					if (BindFullName(bind) != id)
						continue;
					MethodInfo get = bind.GetType().GetMethod("GetAssignedKeys", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
					                 ?? bind.GetType().GetMethod("GetAssignedKeys", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(InputMode) }, null);
					object keys = get == null
						? null
						: get.GetParameters().Length == 0
							? get.Invoke(bind, null)
							: get.Invoke(bind, new object[] { InputMode.Keyboard });
					if (keys is IList list && list.Count > 0)
						return Short(list[0]?.ToString() ?? "-");
				}
			}
			catch {
			}

			return "-";
		}

		private static void SetModBind(string id, string key)
		{
			if (string.IsNullOrEmpty(id))
				return;
			Dictionary<string, List<string>> map = KeyMap();
			if (!map.TryGetValue(id, out List<string> list) || list == null) {
				list = new List<string>();
				map[id] = list;
			}

			list.Clear();
			if (!string.IsNullOrEmpty(key))
				list.Add(key);
		}

		private static string DefaultModBind(string id)
		{
			foreach (object bind in RawModBinds()) {
				if (BindFullName(bind) != id)
					continue;
				return Inst(bind, "DefaultBinding")?.ToString() ?? "";
			}

			return "";
		}

		private static object Inst(object target, string name)
		{
			if (target == null || string.IsNullOrEmpty(name))
				return null;
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			Type type = target.GetType();
			return type.GetProperty(name, flags)?.GetValue(target) ?? type.GetField(name, flags)?.GetValue(target);
		}

		private static string TextOf(object value)
		{
			if (value == null)
				return null;
			if (value is string s)
				return string.IsNullOrWhiteSpace(s) ? null : s;
			object inner = Inst(value, "Value");
			return inner is string sv && !string.IsNullOrWhiteSpace(sv) ? sv : null;
		}

		private static void DrawBtn(SpriteBatch spriteBatch, Rectangle hit, string text, float fade)
		{
			bool hover = hit.Contains(Main.mouseX, Main.mouseY);
			WeDraw.Fill(spriteBatch, hit, (hover ? WeAccent.Deep : new Color(28, 30, 38)) * fade);
			WeDraw.Border(spriteBatch, hit, (hover ? WeAccent.Light : WeAccent.Mid) * fade);
			WeDraw.Hairline(spriteBatch, hit, fade);
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
