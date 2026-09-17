using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.Settings
{
	internal sealed class WeWorkshopItem
	{
		internal object Raw;
		internal string Id = "";
		internal string Name = "";
		internal string Internal = "";
		internal string Author = "";
		internal string Meta = "";
		internal string IconUrl = "";
		internal Texture2D Icon;
		internal WeClip Gif;
		internal long Score;
		internal DateTime When;
		internal bool Installed;
		internal bool Pending;
		internal bool Subscribed;
	}

	internal static class WeWorkshop
	{
		private const int PageSize = 30;
		private const int MaxItems = 200;
		private static readonly object Gate = new();
		private static readonly HttpClient Http;
		private static readonly Dictionary<string, Texture2D> IconCache = new(StringComparer.Ordinal);
		private static readonly Dictionary<string, byte[]> IconBytes = new(StringComparer.Ordinal);
		private static readonly HashSet<string> IconFetch = new(StringComparer.Ordinal);
		private static List<WeWorkshopItem> _items;
		private static string _live = "\u0001";
		private static string _queued = "";
		private static bool _busy;
		private static bool _again;
		private static bool _hasMore = true;
		private static int _page = 1;
		private static int _sort;
		private static DateTime _typed = DateTime.MinValue;

		static WeWorkshop()
		{
			var handler = new SocketsHttpHandler {
				AutomaticDecompression = DecompressionMethods.All,
				PooledConnectionLifetime = TimeSpan.FromMinutes(2)
			};
			Http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
			Http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) DieWithASmile-tModLoader/3.0.18");
			Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
		}

		internal static bool Busy
		{
			get
			{
				lock (Gate)
					return _busy;
			}
		}

		internal static bool HasMore
		{
			get
			{
				lock (Gate)
					return _hasMore;
			}
		}

		internal static int Sort
		{
			get => _sort;
			set
			{
				int v = Math.Clamp(value, 0, 2);
				lock (Gate) {
					if (_sort == v)
						return;
					_sort = v;
					_items = null;
					_live = "\u0001";
					_page = 1;
					_hasMore = true;
				}
			}
		}

		internal static List<WeWorkshopItem> Items(string search)
		{
			Pump(search ?? "");
			List<WeWorkshopItem> list;
			lock (Gate)
				list = _items == null ? null : new List<WeWorkshopItem>(_items);
			if (list == null)
				return new List<WeWorkshopItem>();
			Stamp(list);
			ApplyIcons(list);
			return list;
		}

		internal static void WantMore()
		{
			lock (Gate) {
				if (_busy || !_hasMore || _items == null || _items.Count >= MaxItems)
					return;
				_page++;
				_busy = true;
				string q = _live;
				int page = _page;
				ThreadPool.QueueUserWorkItem(_ => Run(q, page, true));
			}
		}

		internal static void Subscribe(WeWorkshopItem item)
		{
			if (item == null || string.IsNullOrEmpty(item.Id))
				return;
			if (!TryDownload(item.Raw))
				TrySteamSubscribe(item.Id);
			item.Subscribed = true;
		}

		private static void Pump(string search)
		{
			search ??= "";
			lock (Gate) {
				if (search != _queued) {
					_queued = search;
					_typed = DateTime.UtcNow;
				}

				if (_again && !_busy) {
					_again = false;
					Start(_queued);
					return;
				}

				if (_busy)
					return;
				if (_items != null && search == _live)
					return;
				if ((DateTime.UtcNow - _typed).TotalMilliseconds < 280 && search.Length > 0)
					return;
				Start(search);
			}
		}

		private static void Start(string search)
		{
			_busy = true;
			_live = search;
			_page = 1;
			_hasMore = true;
			string q = search;
			ThreadPool.QueueUserWorkItem(_ => Run(q, 1, false));
		}

		private static void Run(string search, int page, bool append)
		{
			List<WeWorkshopItem> chunk = null;
			try {
				chunk = Fetch(search, page);
			}
			catch {
				chunk = new List<WeWorkshopItem>();
			}
			finally {
				lock (Gate) {
					if (search == _live) {
						if (append && _items != null) {
							var next = new List<WeWorkshopItem>(_items.Count + (chunk?.Count ?? 0));
							var seen = new HashSet<string>(StringComparer.Ordinal);
							foreach (WeWorkshopItem it in _items) {
								seen.Add(it.Id);
								next.Add(it);
							}

							foreach (WeWorkshopItem it in chunk ?? new List<WeWorkshopItem>()) {
								if (seen.Add(it.Id))
									next.Add(it);
							}

							_items = next;
						}
						else
							_items = chunk ?? new List<WeWorkshopItem>();
						_hasMore = chunk != null && chunk.Count >= PageSize && _items.Count < MaxItems;
					}

					_busy = false;
					if (_queued != _live)
						_again = true;
				}
			}
		}

		private static List<WeWorkshopItem> Fetch(string search, int page)
		{
			var map = new Dictionary<string, WeWorkshopItem>(StringComparer.Ordinal);
			TryTml(search, map, page);
			if (map.Count == 0)
				TryHtml(search, map, page);
			if (page <= 1)
				MergePending(map);
			var list = new List<WeWorkshopItem>(map.Values);
			ApplySort(list);
			if (list.Count > PageSize)
				list.RemoveRange(PageSize, list.Count - PageSize);
			return list;
		}

		private static void ApplySort(List<WeWorkshopItem> list)
		{
			int sort = _sort;
			if (sort == 1)
				list.Sort((a, b) => b.When.CompareTo(a.When));
			else if (sort == 2)
				list.Sort((a, b) => b.Score.CompareTo(a.Score));
		}

		private static void Stamp(List<WeWorkshopItem> list)
		{
			List<WeLocalMod> mods;
			try {
				mods = WeTml.LocalMods();
			}
			catch {
				return;
			}

			foreach (WeWorkshopItem item in list) {
				item.Installed = false;
				foreach (WeLocalMod mod in mods) {
					if ((!string.IsNullOrEmpty(item.Id) && string.Equals(mod.Steam, item.Id, StringComparison.Ordinal)) ||
					    (!string.IsNullOrEmpty(item.Internal) && string.Equals(mod.Name, item.Internal, StringComparison.OrdinalIgnoreCase)) ||
					    (!string.IsNullOrEmpty(item.Name) && string.Equals(mod.Display, item.Name, StringComparison.OrdinalIgnoreCase))) {
						item.Installed = true;
						item.Gif ??= mod.Gif;
						if (item.Icon == null && mod.Icon != null && !mod.Icon.IsDisposed)
							item.Icon = mod.Icon;
						break;
					}
				}
			}
		}

		private static void TryTml(string search, Dictionary<string, WeWorkshopItem> map, int page)
		{
			try {
				Assembly asm = typeof(ModLoader).Assembly;
				object query = MakeQuery(asm, search);
				if (query == null)
					return;
				object stream = InvokeBrowser(asm, query) ?? InvokeHelper(asm, query);
				if (stream == null)
					return;
				int skip = Math.Max(0, (page - 1) * PageSize);
				int n = 0;
				var sw = System.Diagnostics.Stopwatch.StartNew();
				foreach (object raw in Enumerate(stream, skip + PageSize)) {
					if (sw.Elapsed.TotalSeconds > 14)
						break;
					if (n++ < skip)
						continue;
					WeWorkshopItem item = ReadItem(raw);
					if (item == null || string.IsNullOrEmpty(item.Id) || map.ContainsKey(item.Id))
						continue;
					map[item.Id] = item;
				}
			}
			catch {
			}
		}

		private static object MakeQuery(Assembly asm, string search)
		{
			Type type = asm.GetType("Terraria.Social.Base.QueryParameters") ??
			            asm.GetType("Terraria.ModLoader.UI.ModBrowser.QueryParameters");
			if (type == null)
				return null;
			object q = Activator.CreateInstance(type);
			SetMember(q, "searchGeneric", search ?? "");
			SetMember(q, "searchText", search ?? "");
			SetEnum(q, "queryType", "SearchAll", "SearchGeneric", "QueryAll");
			SetEnum(q, "updateStatusFilter", "All");
			if (_sort == 1)
				SetEnum(q, "sortingParamater", "RecentlyUpdated", "LastUpdated", "Time", "Hot");
			else if (_sort == 2)
				SetEnum(q, "sortingParamater", "Downloads", "TotalDownloads", "Hot");
			else
				SetEnum(q, "sortingParamater", "Hot", "Downloads", "TotalDownloads");
			SetEnum(q, "sortingParameter", _sort == 1 ? "RecentlyUpdated" : _sort == 2 ? "Downloads" : "Hot");
			return q;
		}

		private static object InvokeBrowser(Assembly asm, object query)
		{
			Type type = asm.GetType("Terraria.Social.Steam.WorkshopBrowserModule");
			if (type == null)
				return null;
			object inst = type.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
			inst ??= Activator.CreateInstance(type, true);
			if (inst == null)
				return null;
			try {
				type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(inst, null);
			}
			catch {
			}

			MethodInfo method = type.GetMethod("QueryBrowser", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (method == null)
				return null;
			ParameterInfo[] ps = method.GetParameters();
			object[] args;
			if (ps.Length == 1)
				args = new[] { query };
			else if (ps.Length >= 2)
				args = new[] { query, CancellationToken.None };
			else
				return null;
			return method.Invoke(inst, args);
		}

		private static object InvokeHelper(Assembly asm, object query)
		{
			Type type = asm.GetType("Terraria.Social.Steam.WorkshopHelper+QueryHelper") ??
			            asm.GetType("Terraria.Social.Steam.WorkshopHelper.QueryHelper");
			MethodInfo method = type?.GetMethod("QueryWorkshop", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (method == null)
				return null;
			ParameterInfo[] ps = method.GetParameters();
			object[] args = ps.Length >= 2 ? new[] { query, CancellationToken.None } : new[] { query };
			return method.Invoke(null, args);
		}

		private static IEnumerable<object> Enumerate(object stream, int cap)
		{
			if (stream == null)
				yield break;
			bool asyncLike = stream.GetType().GetInterface("IAsyncEnumerable`1") != null ||
			                 stream.GetType().Name.Contains("IAsyncEnumerable", StringComparison.Ordinal);
			if (!asyncLike && stream is IEnumerable en && stream is not string) {
				int n = 0;
				foreach (object o in en) {
					if (o == null)
						continue;
					yield return o;
					if (++n >= cap)
						yield break;
				}

				yield break;
			}

			object enumerator = GetAsyncEnumerator(stream);
			if (enumerator == null)
				yield break;
			int count = 0;
			while (count < cap && MoveNext(enumerator)) {
				object cur = CurrentOf(enumerator);
				if (cur != null)
					yield return cur;
				count++;
			}
		}

		private static object GetAsyncEnumerator(object stream)
		{
			try {
				foreach (Type iface in stream.GetType().GetInterfaces()) {
					if (!iface.Name.StartsWith("IAsyncEnumerable", StringComparison.Ordinal))
						continue;
					MethodInfo get = iface.GetMethod("GetAsyncEnumerator");
					if (get == null)
						continue;
					ParameterInfo[] ps = get.GetParameters();
					object[] args = ps.Length == 0 ? Array.Empty<object>() : new object[] { CancellationToken.None };
					return get.Invoke(stream, args);
				}

				MethodInfo own = stream.GetType().GetMethod("GetAsyncEnumerator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (own != null) {
					ParameterInfo[] ps = own.GetParameters();
					object[] args = ps.Length == 0 ? Array.Empty<object>() : new object[] { CancellationToken.None };
					return own.Invoke(stream, args);
				}
			}
			catch {
			}

			return null;
		}

		private static bool MoveNext(object enumerator)
		{
			try {
				MethodInfo move = enumerator.GetType().GetMethod("MoveNextAsync", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (move == null) {
					foreach (Type iface in enumerator.GetType().GetInterfaces()) {
						if (iface.Name.StartsWith("IAsyncEnumerator", StringComparison.Ordinal))
							move = iface.GetMethod("MoveNextAsync");
						if (move != null)
							break;
					}
				}

				if (move == null)
					return false;
				return AwaitBool(move.Invoke(enumerator, null));
			}
			catch {
				return false;
			}
		}

		private static object CurrentOf(object enumerator)
		{
			try {
				PropertyInfo p = enumerator.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (p != null)
					return p.GetValue(enumerator);
				foreach (Type iface in enumerator.GetType().GetInterfaces()) {
					if (!iface.Name.StartsWith("IAsyncEnumerator", StringComparison.Ordinal))
						continue;
					return iface.GetProperty("Current")?.GetValue(enumerator);
				}
			}
			catch {
			}

			return null;
		}

		private static bool AwaitBool(object vt)
		{
			if (vt == null)
				return false;
			if (vt is bool b)
				return b;
			if (vt is Task task) {
				if (!task.Wait(TimeSpan.FromSeconds(10)))
					return false;
				return ResultBool(task);
			}

			Type type = vt.GetType();
			PropertyInfo done = type.GetProperty("IsCompleted") ?? type.GetProperty("IsCompletedSuccessfully");
			PropertyInfo result = type.GetProperty("Result");
			try {
				if (done != null && done.GetValue(vt) is true && result != null)
					return Convert.ToBoolean(result.GetValue(vt), CultureInfo.InvariantCulture);
			}
			catch {
			}

			try {
				MethodInfo asTask = type.GetMethod("AsTask", Type.EmptyTypes);
				if (asTask?.Invoke(vt, null) is Task t2) {
					if (!t2.Wait(TimeSpan.FromSeconds(10)))
						return false;
					return ResultBool(t2);
				}
			}
			catch {
			}

			return false;
		}

		private static bool ResultBool(Task task)
		{
			try {
				PropertyInfo p = task.GetType().GetProperty("Result");
				return p != null && Convert.ToBoolean(p.GetValue(task), CultureInfo.InvariantCulture);
			}
			catch {
				return false;
			}
		}

		private static WeWorkshopItem ReadItem(object raw)
		{
			if (raw == null)
				return null;
			var item = new WeWorkshopItem { Raw = raw };
			item.Name = Str(raw, "DisplayName") ?? Str(raw, "DisplayNameClean") ?? Str(raw, "Name") ?? "";
			item.Internal = Str(raw, "ModName") ?? Str(raw, "Name") ?? "";
			item.Author = Str(raw, "Author") ?? "";
			object pub = Prop(raw, "PublishId") ?? Prop(raw, "publishId");
			item.Id = Digits(Str(pub, "m_ModPubId") ?? pub?.ToString() ?? Str(raw, "PublishId"));
			if (string.IsNullOrEmpty(item.Name))
				item.Name = item.Internal;
			if (string.IsNullOrEmpty(item.Id))
				return null;
			var bits = new List<string>();
			if (!string.IsNullOrEmpty(item.Author))
				bits.Add(item.Author);
			object downloads = Prop(raw, "Downloads") ?? Prop(raw, "Hot");
			if (downloads != null && long.TryParse(downloads.ToString(), out long n) && n > 0)
				bits.Add(string.Format(CultureInfo.InvariantCulture, "{0:n0}", n));
			object time = Prop(raw, "TimeStamp");
			if (time is DateTime dt && dt.Year > 2000)
				bits.Add(dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			item.IconUrl = Str(raw, "ModIconUrl") ?? Str(raw, "modIconUrl") ?? Str(raw, "ModIconURL") ?? "";
			item.Meta = string.Join(" · ", bits);
			item.Installed = Prop(raw, "IsInstalled") is true;
			if (downloads != null && long.TryParse(downloads.ToString(), out long score))
				item.Score = score;
			if (time is DateTime when)
				item.When = when;
			return item;
		}

		private static void TryHtml(string search, Dictionary<string, WeWorkshopItem> map, int page)
		{
			int p = Math.Max(1, page);
			try {
				string url = "https://steamcommunity.com/workshop/browse/?appid=1281930&section=readytouseitems&p=" + p + "&numperpage=30";
				if (string.IsNullOrWhiteSpace(search)) {
					string sort = _sort == 1 ? "mostrecent" : _sort == 2 ? "totaluniquesubscribers" : "trend";
					url += "&browsesort=" + sort;
				}
				else
					url += "&browsesort=textsearch&searchtext=" + Uri.EscapeDataString(search.Trim());
				string html = Http.GetStringAsync(url).GetAwaiter().GetResult();
				if (!string.IsNullOrEmpty(html))
					ParseHtml(html, map);
				if (map.Count == 0 && !string.IsNullOrEmpty(html))
					ParseRss(html, map);
			}
			catch {
			}

			if (map.Count > 0 || page > 1)
				return;
			try {
				string rss = "https://steamcommunity.com/workshop/browse/?appid=1281930&browsesort=trend&section=readytouseitems&rss=1";
				if (!string.IsNullOrWhiteSpace(search))
					rss = "https://steamcommunity.com/workshop/browse/?appid=1281930&browsesort=textsearch&section=readytouseitems&rss=1&searchtext=" +
					      Uri.EscapeDataString(search.Trim());
				string xml = Http.GetStringAsync(rss).GetAwaiter().GetResult();
				if (!string.IsNullOrEmpty(xml))
					ParseRss(xml, map);
			}
			catch {
			}
		}

		private static void ParseHtml(string html, Dictionary<string, WeWorkshopItem> map)
		{
			int i = 0;
			while (i < html.Length) {
				int a = html.IndexOf("filedetails/?id=", i, StringComparison.OrdinalIgnoreCase);
				if (a < 0)
					break;
				int d = a + "filedetails/?id=".Length;
				int e = d;
				while (e < html.Length && char.IsDigit(html[e]))
					e++;
				i = e;
				string id = html.Substring(d, e - d);
				if (id.Length < 6 || map.ContainsKey(id))
					continue;
				string slice = html.Substring(a, Math.Min(3500, html.Length - a));
				string title = Strip(Inner(slice, "workshopItemTitle"));
				if (string.IsNullOrEmpty(title) || title.Length < 2)
					continue;
				if (title.Contains("Steam Workshop", StringComparison.OrdinalIgnoreCase))
					continue;
				string author = Strip(Inner(slice, "workshopItemAuthorName"));
				map[id] = new WeWorkshopItem {
					Id = id,
					Name = title,
					Author = author,
					Meta = author,
					IconUrl = PreviewUrl(slice)
				};
				if (map.Count >= PageSize)
					return;
			}
		}

		private static void ParseRss(string xml, Dictionary<string, WeWorkshopItem> map)
		{
			int i = 0;
			while (i < xml.Length) {
				int a = xml.IndexOf("filedetails/?id=", i, StringComparison.OrdinalIgnoreCase);
				if (a < 0)
					break;
				int d = a + "filedetails/?id=".Length;
				int e = d;
				while (e < xml.Length && char.IsDigit(xml[e]))
					e++;
				i = e;
				string id = xml.Substring(d, e - d);
				if (id.Length < 6 || map.ContainsKey(id))
					continue;
				string title = "";
				int itemAt = xml.LastIndexOf("<item", a, StringComparison.OrdinalIgnoreCase);
				int titleAt = itemAt < 0 ? xml.IndexOf("<title", a, StringComparison.OrdinalIgnoreCase) : xml.IndexOf("<title", itemAt, StringComparison.OrdinalIgnoreCase);
				if (titleAt >= 0 && titleAt < a + 800) {
					int gt = xml.IndexOf('>', titleAt);
					int lt = gt < 0 ? -1 : xml.IndexOf("</title>", gt, StringComparison.OrdinalIgnoreCase);
					if (gt >= 0 && lt > gt)
						title = Strip(xml.Substring(gt + 1, lt - gt - 1).Replace("<![CDATA[", "").Replace("]]>", ""));
				}

				if (string.IsNullOrEmpty(title) || title.Contains("Steam Workshop", StringComparison.OrdinalIgnoreCase))
					continue;
				map[id] = new WeWorkshopItem { Id = id, Name = title };
				if (map.Count >= PageSize)
					return;
			}
		}

		private static string PreviewUrl(string html)
		{
			if (string.IsNullOrEmpty(html))
				return "";
			int tag = html.IndexOf("workshopItemPreviewImage", StringComparison.OrdinalIgnoreCase);
			string area = tag < 0 ? html : html.Substring(Math.Max(0, tag - 280), Math.Min(html.Length - Math.Max(0, tag - 280), 900));
			foreach (string key in new[] { "src=\"", "src='", "data-src=\"" }) {
				int at = area.IndexOf(key, StringComparison.OrdinalIgnoreCase);
				if (at < 0)
					continue;
				int start = at + key.Length;
				int end = area.IndexOf(key[key.Length - 1], start);
				if (end <= start)
					continue;
				string url = area.Substring(start, end - start);
				if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
				    (url.Contains("steam", StringComparison.OrdinalIgnoreCase) || url.Contains("akamai", StringComparison.OrdinalIgnoreCase)))
					return url.Replace("&amp;", "&");
			}

			return "";
		}

		private static void ApplyIcons(List<WeWorkshopItem> list)
		{
			foreach (WeWorkshopItem item in list) {
				if (item.Gif != null || (item.Icon != null && !item.Icon.IsDisposed))
					continue;
				if (IconCache.TryGetValue(item.Id, out Texture2D cached) && cached != null && !cached.IsDisposed) {
					item.Icon = cached;
					continue;
				}

				byte[] data;
				lock (Gate)
					IconBytes.TryGetValue(item.Id, out data);
				if (data != null) {
					if (WeGif.LooksLike(data)) {
						try {
							WeClip clip = WeGif.Decode(data);
							if (clip != null) {
								clip.KeepDelays();
								item.Gif = clip;
							}
						}
						catch {
						}
					}

					Texture2D tex = WeTml.TextureFrom(data);
					if (tex != null) {
						IconCache[item.Id] = tex;
						item.Icon = tex;
					}

					if (item.Gif != null || item.Icon != null) {
						lock (Gate)
							IconBytes.Remove(item.Id);
					}

					continue;
				}

				if (string.IsNullOrEmpty(item.IconUrl) && !string.IsNullOrEmpty(item.Id))
					item.IconUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + item.Id;
				if (string.IsNullOrEmpty(item.IconUrl) || string.IsNullOrEmpty(item.Id))
					continue;
				bool go;
				lock (Gate)
					go = IconFetch.Add(item.Id);
				if (!go)
					continue;
				string id = item.Id;
				string url = item.IconUrl;
				ThreadPool.QueueUserWorkItem(_ => DownloadIcon(id, url));
			}
		}

		private static void DownloadIcon(string id, string url)
		{
			try {
				byte[] data = FetchBytes(url);
				if (data == null)
					return;
				if (LooksHtml(data)) {
					string html = Encoding.UTF8.GetString(data);
					string next = PreviewUrl(html);
					if (string.IsNullOrEmpty(next) || string.Equals(next, url, StringComparison.OrdinalIgnoreCase))
						return;
					data = FetchBytes(next);
					if (data == null || LooksHtml(data))
						return;
				}

				lock (Gate)
					IconBytes[id] = data;
			}
			catch {
			}
		}

		private static byte[] FetchBytes(string url)
		{
			byte[] data = Http.GetByteArrayAsync(url).GetAwaiter().GetResult();
			return data != null && data.Length > 32 ? data : null;
		}

		private static bool LooksHtml(byte[] data)
		{
			int n = Math.Min(data.Length, 80);
			string head = Encoding.UTF8.GetString(data, 0, n).TrimStart();
			return head.StartsWith('<') || head.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
		}

		private static string Inner(string html, string className)
		{
			int at = html.IndexOf(className, StringComparison.OrdinalIgnoreCase);
			if (at < 0)
				return "";
			int gt = html.IndexOf('>', at);
			if (gt < 0)
				return "";
			int lt = html.IndexOf('<', gt + 1);
			if (lt < 0)
				return html[(gt + 1)..];
			return html.Substring(gt + 1, lt - gt - 1);
		}

		private static string Strip(string s)
		{
			if (string.IsNullOrEmpty(s))
				return "";
			var sb = new StringBuilder(s.Length);
			bool hide = false;
			foreach (char c in s) {
				if (c == '<')
					hide = true;
				else if (c == '>')
					hide = false;
				else if (!hide)
					sb.Append(c);
			}

			return WebUtility.HtmlDecode(sb.ToString()).Trim();
		}

		private static void MergePending(Dictionary<string, WeWorkshopItem> map)
		{
			foreach (string root in WorkshopRoots()) {
				if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
					continue;
				try {
					foreach (string dir in Directory.GetDirectories(root)) {
						string id = Path.GetFileName(dir);
						if (string.IsNullOrEmpty(id) || id.Length < 6)
							continue;
						bool digits = true;
						foreach (char c in id) {
							if (!char.IsDigit(c)) {
								digits = false;
								break;
							}
						}

						if (!digits)
							continue;
						string[] tmods = Directory.GetFiles(dir, "*.tmod", SearchOption.TopDirectoryOnly);
						if (tmods.Length == 0)
							continue;
						if (map.ContainsKey(id))
							continue;
						string name = TitleFromWorkshop(dir) ?? Path.GetFileNameWithoutExtension(tmods[0]);
						map[id] = new WeWorkshopItem {
							Id = id,
							Name = name,
							Pending = true,
							Meta = ""
						};
					}
				}
				catch {
				}
			}
		}

		private static string TitleFromWorkshop(string dir)
		{
			string json = Path.Combine(dir, "workshop.json");
			if (!File.Exists(json))
				return null;
			try {
				string text = File.ReadAllText(json);
				foreach (string key in new[] { "title", "displayName", "displayname" }) {
					int at = text.IndexOf('"' + key + '"', StringComparison.OrdinalIgnoreCase);
					if (at < 0)
						continue;
					int colon = text.IndexOf(':', at);
					int q1 = text.IndexOf('"', colon + 1);
					int q2 = q1 < 0 ? -1 : text.IndexOf('"', q1 + 1);
					if (q1 < 0 || q2 < 0)
						continue;
					string v = text.Substring(q1 + 1, q2 - q1 - 1).Trim();
					if (v.Length > 1 && !v.Equals(key, StringComparison.OrdinalIgnoreCase))
						return v;
				}
			}
			catch {
			}

			return null;
		}

		private static IEnumerable<string> WorkshopRoots()
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			void Add(string dir)
			{
				if (!string.IsNullOrEmpty(dir))
					seen.Add(dir);
			}

			try {
				Add(Path.Combine(Main.SavePath, "Workshop", "content", "1281930"));
				string walk = Main.SavePath;
				for (int i = 0; i < 8 && !string.IsNullOrEmpty(walk); i++) {
					Add(Path.Combine(walk, "steamapps", "workshop", "content", "1281930"));
					walk = Path.GetDirectoryName(walk);
				}

				string steam = @"C:\Steam\steamapps\workshop\content\1281930";
				if (Directory.Exists(steam))
					Add(steam);
			}
			catch {
			}

			try {
				Type helper = typeof(ModLoader).Assembly.GetType("Terraria.Social.Steam.WorkshopHelper");
				MethodInfo get = helper?.GetMethod("GetWorkshopFolder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (get != null) {
					ParameterInfo[] ps = get.GetParameters();
					object arg = null;
					if (ps.Length == 1) {
						Type app = ps[0].ParameterType;
						try {
							arg = Activator.CreateInstance(app, (uint)1281930);
						}
						catch {
							try {
								arg = Activator.CreateInstance(app, 1281930);
							}
							catch {
							}
						}
					}

					object folder = get.Invoke(null, arg == null ? Array.Empty<object>() : new[] { arg });
					if (folder is string s)
						Add(Path.Combine(s, "content", "1281930"));
				}
			}
			catch {
			}

			return seen;
		}

		private static bool TryDownload(object raw)
		{
			if (raw == null)
				return false;
			try {
				Type type = typeof(ModLoader).Assembly.GetType("Terraria.Social.Steam.WorkshopBrowserModule");
				object inst = type?.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
				if (type == null || inst == null)
					return false;
				foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
					if (method.Name != "DownloadItem")
						continue;
					ParameterInfo[] ps = method.GetParameters();
					if (ps.Length == 1) {
						method.Invoke(inst, new[] { raw });
						return true;
					}

					if (ps.Length >= 2) {
						method.Invoke(inst, new[] { raw, null });
						return true;
					}
				}
			}
			catch {
			}

			return false;
		}

		private static bool TrySteamSubscribe(string id)
		{
			if (!ulong.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong n) || n == 0)
				return false;
			try {
				foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
					Type ugc = asm.GetType("Steamworks.SteamUGC");
					Type pid = asm.GetType("Steamworks.PublishedFileId_t");
					if (ugc == null || pid == null)
						continue;
					object file;
					try {
						file = Activator.CreateInstance(pid, n);
					}
					catch {
						continue;
					}

					MethodInfo sub = ugc.GetMethod("SubscribeItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { pid }, null);
					if (sub == null)
						continue;
					sub.Invoke(null, new[] { file });
					return true;
				}
			}
			catch {
			}

			try {
				WeOs.Reveal("steam://url/SubscribeToFile/" + id);
				return true;
			}
			catch {
				return false;
			}
		}

		private static void SetMember(object o, string name, object value)
		{
			if (o == null || string.IsNullOrEmpty(name))
				return;
			Type type = o.GetType();
			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (f != null && (value == null || f.FieldType.IsInstanceOfType(value) || f.FieldType == value.GetType())) {
				f.SetValue(o, value);
				return;
			}

			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (p != null && p.CanWrite && (value == null || p.PropertyType.IsInstanceOfType(value) || p.PropertyType == value.GetType()))
				p.SetValue(o, value);
		}

		private static void SetEnum(object o, string name, params string[] labels)
		{
			if (o == null)
				return;
			Type type = o.GetType();
			FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			PropertyInfo p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Type et = f?.FieldType ?? p?.PropertyType;
			if (et == null || !et.IsEnum)
				return;
			object val = null;
			foreach (string label in labels) {
				try {
					val = Enum.Parse(et, label, true);
					break;
				}
				catch {
				}
			}

			val ??= Enum.ToObject(et, 0);
			f?.SetValue(o, val);
			if (p is { CanWrite: true })
				p.SetValue(o, val);
		}

		private static object Prop(object o, string name)
		{
			if (o == null)
				return null;
			return o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o)
			       ?? o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(o);
		}

		private static string Str(object o, string name) => Prop(o, name)?.ToString();

		private static string Digits(string v)
		{
			if (string.IsNullOrEmpty(v))
				return "";
			var sb = new StringBuilder();
			foreach (char c in v) {
				if (char.IsDigit(c))
					sb.Append(c);
			}

			return sb.ToString();
		}
	}
}
