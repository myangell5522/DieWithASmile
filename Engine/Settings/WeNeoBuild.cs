using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.Settings
{
	internal static class WeNeoBuild
	{
		private const BindingFlags Stat = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
		private const BindingFlags Inst = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		internal static void Build(string folder, bool reload)
		{
			if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
				return;
			LeaveTo(WeNeoPage.Develop);
			if (!CallBuild("Build", new object[] { folder, reload }))
				CallBuild("Build", new object[] { folder });
		}

		internal static void BuildAll()
		{
			LeaveTo(WeNeoPage.Develop);
			CallBuild("BuildAll", new object[] { false });
		}

		internal static void CreateMod()
		{
			LeaveTo(WeNeoPage.Develop);
			OpenField("createMod", "createModID");
		}

		internal static void Publish(WeDevItem item)
		{
			if (item == null)
				return;
			LeaveTo(WeNeoPage.Develop);
			try {
				if (!SteamClient()) {
					WeOs.Reveal("https://steamcommunity.com/app/1281930/workshop/");
					return;
				}

				object local = item.Local ?? FindLocal(item.Name);
				if (local == null)
					return;
				if (!ModLoader.TryGetMod(item.Name, out _)) {
					WeTml.SetEnabledName(item.Name, true);
					try {
						EventInfo ev = typeof(ModLoader).GetEvent("OnSuccessfulLoad", Stat);
						if (ev != null) {
							Action again = null;
							again = () => {
								try {
									ev.RemoveEventHandler(null, again);
								}
								catch {
								}

								Main.QueueMainThreadAction(() => Publish(item));
							};
							ev.AddEventHandler(null, again);
						}
					}
					catch {
					}

					WeTml.Reload();
					return;
				}

				string icon = Path.Combine(item.Folder, "icon_workshop.png");
				if (!File.Exists(icon))
					icon = Path.Combine(item.Folder, "icon.png");
				Type helper = typeof(ModLoader).Assembly.GetType("Terraria.Social.Steam.WorkshopHelper");
				MethodInfo pub = helper?.GetMethod("PublishMod", Stat);
				if (pub == null)
					return;
				ParameterInfo[] ps = pub.GetParameters();
				object[] args = ps.Length >= 2 ? new[] { local, icon } : new[] { local };
				if (ps.Length > 2) {
					args = new object[ps.Length];
					args[0] = local;
					args[1] = icon;
				}

				pub.Invoke(null, args);
			}
			catch {
			}
		}

		internal static void Extract(WeLocalMod mod)
		{
			if (mod == null)
				return;
			LeaveTo(WeNeoPage.Mods);
			if (CallOn("extractMod", "Show", mod.Raw, mod.Name) || CallOn("extractMod", "Extract", mod.Raw, mod.Name))
				return;
			OpenField("extractMod", "extractModID");
		}

		internal static void OpenConfig(WeLocalMod mod)
		{
			if (mod == null)
				return;
			LeaveTo(WeNeoPage.Mods);
			Mod loaded = null;
			try {
				ModLoader.TryGetMod(mod.Name, out loaded);
			}
			catch {
			}

			object[] hints = loaded != null
				? new object[] { loaded, mod.Raw, mod.Name }
				: new object[] { mod.Raw, mod.Name };
			CallOn("modConfig", "SetMod", hints);
			CallOn("modConfig", "Set", hints);
			if (OpenField("modConfig", "modConfigID"))
				return;
			CallOn("modConfigList", "SetMod", hints);
			CallOn("modConfigList", "Set", hints);
			OpenField("modConfigList", "modConfigListID");
		}

		internal static void Delete(WeLocalMod mod)
		{
			if (mod == null || string.IsNullOrEmpty(mod.Path) || !File.Exists(mod.Path))
				return;
			try {
				WeTml.SetEnabled(mod, false);
				File.Delete(mod.Path);
			}
			catch {
			}

			WeTml.Invalidate();
		}

		internal static void OpenCsproj(WeDevItem item)
		{
			if (item == null || !item.Csproj)
				return;
			try {
				string file = Path.Combine(item.Folder, item.Name + ".csproj");
				if (!File.Exists(file)) {
					string[] found = Directory.GetFiles(item.Folder, "*.csproj");
					file = found.Length > 0 ? found[0] : "";
				}

				if (string.IsNullOrEmpty(file))
					return;
				Process.Start(new ProcessStartInfo("explorer", file) { UseShellExecute = true });
			}
			catch {
				WeFiles.OpenFolder(item.Folder);
			}
		}

		internal static void Porter(WeDevItem item)
		{
			if (item == null)
				return;
			try {
				string csproj = Path.Combine(item.Folder, item.Name + ".csproj");
				if (!File.Exists(csproj)) {
					string[] found = Directory.GetFiles(item.Folder, "*.csproj");
					csproj = found.Length > 0 ? found[0] : "";
				}

				if (string.IsNullOrEmpty(csproj))
					return;
				if (CallSource("UpgradeCsproj", csproj) || CallSource("RunTModPorter", csproj) || CallSource("Port", csproj))
					return;
				string tml = Path.GetDirectoryName(typeof(ModLoader).Assembly.Location) ?? "";
				string bat = Path.Combine(tml, "tModPorter", "tModPorter.bat");
				string sh = Path.Combine(tml, "tModPorter", "tModPorter.sh");
				string porter = File.Exists(bat) ? bat : sh;
				if (!File.Exists(porter))
					return;
				Process.Start(new ProcessStartInfo(porter, "\"" + csproj + "\"") {
					WorkingDirectory = tml,
					UseShellExecute = true
				});
			}
			catch {
			}
		}

		private static bool CallSource(string method, string csproj)
		{
			try {
				Assembly asm = typeof(ModLoader).Assembly;
				foreach (string typeName in new[] {
					         "Terraria.ModLoader.Core.SourceManagement",
					         "Terraria.ModLoader.UI.SourceManagement",
					         "Terraria.ModLoader.Core.ModCompile"
				         }) {
					Type type = asm.GetType(typeName);
					if (type == null)
						continue;
					foreach (MethodInfo m in type.GetMethods(Stat | Inst)) {
						if (!string.Equals(m.Name, method, StringComparison.OrdinalIgnoreCase))
							continue;
						ParameterInfo[] ps = m.GetParameters();
						if (ps.Length == 1 && ps[0].ParameterType == typeof(string)) {
							m.Invoke(m.IsStatic ? null : Activator.CreateInstance(type), new object[] { csproj });
							return true;
						}

						if (ps.Length == 0 && m.IsStatic) {
							m.Invoke(null, null);
							return true;
						}
					}
				}
			}
			catch {
			}

			return false;
		}

		private static void LeaveTo(WeNeoPage page)
		{
			SoundEngine.PlaySound(SoundID.MenuOpen);
			WeNeoMenu.BeginDeepLink(WeNeoCat.Mods, page);
		}

		private static bool CallBuild(string method, object[] args)
		{
			try {
				object inst = Field("buildMod");
				if (inst == null)
					return false;
				foreach (MethodInfo m in inst.GetType().GetMethods(Inst)) {
					if (m.Name != method)
						continue;
					ParameterInfo[] ps = m.GetParameters();
					if (ps.Length == args.Length) {
						m.Invoke(inst, args);
						return true;
					}

					if (method == "BuildAll" && ps.Length == 1 && ps[0].ParameterType == typeof(bool)) {
						m.Invoke(inst, new object[] { false });
						return true;
					}

					if (method == "Build" && ps.Length == 1 && args.Length >= 1) {
						m.Invoke(inst, new[] { args[0] });
						return true;
					}
				}
			}
			catch {
			}

			return false;
		}

		private static bool CallOn(string field, string method, params object[] hints)
		{
			try {
				object inst = Field(field);
				if (inst == null)
					return false;
				foreach (MethodInfo m in inst.GetType().GetMethods(Inst)) {
					if (!string.Equals(m.Name, method, StringComparison.OrdinalIgnoreCase))
						continue;
					ParameterInfo[] ps = m.GetParameters();
					if (ps.Length == 0) {
						m.Invoke(inst, null);
						return true;
					}

					object[] args = new object[ps.Length];
					for (int i = 0; i < ps.Length; i++) {
						Type pt = ps[i].ParameterType;
						args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : (pt.IsValueType ? Activator.CreateInstance(pt) : null);
						foreach (object hint in hints) {
							if (hint == null)
								continue;
							if (pt.IsInstanceOfType(hint)) {
								args[i] = hint;
								break;
							}

							if (pt == typeof(string) && hint is string)
								args[i] = hint;
						}
					}

					m.Invoke(inst, args);
					return true;
				}
			}
			catch {
			}

			return false;
		}

		private static bool OpenField(string state, string id)
		{
			try {
				Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
				if (iface == null)
					return false;
				object ui = iface.GetField(state, Stat)?.GetValue(null);
				if (ui is UIState menu) {
					if (Main.gameMenu) {
						Main.MenuUI.SetState(menu);
						Main.menuMode = 888;
					}

					return true;
				}

				object mode = iface.GetField(id, Stat)?.GetValue(null);
				if (mode is int n) {
					Main.menuMode = n;
					return true;
				}
			}
			catch {
			}

			return false;
		}

		private static object Field(string name)
		{
			Type iface = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.Interface");
			return iface?.GetField(name, Stat)?.GetValue(null);
		}

		private static bool SteamClient()
		{
			try {
				Type steamed = typeof(ModLoader).Assembly.GetType("Terraria.Social.Steam.SteamedWraps");
				object v = steamed?.GetProperty("SteamClient", Stat)?.GetValue(null) ?? steamed?.GetField("SteamClient", Stat)?.GetValue(null);
				if (v is bool b)
					return b;
			}
			catch {
			}

			return true;
		}

		private static object FindLocal(string name)
		{
			foreach (WeLocalMod mod in WeTml.LocalMods()) {
				if (string.Equals(mod.Name, name, StringComparison.OrdinalIgnoreCase))
					return mod.Raw;
			}

			return null;
		}
	}
}
