using System;
using System.Reflection;

namespace DieWithASmile.Engine.Core
{
	internal static class WeSteam
	{
		private const ulong RestrictedAccount = 76561198980296428UL;
		private static bool _resolved;
		private static bool _hide;

		internal static bool HidesArtistScenes
		{
			get
			{
				if (_resolved)
					return _hide;

				ulong id = ReadSteamId64();
				if (id == 0)
					return false;

				_hide = id == RestrictedAccount;
				_resolved = true;
				return _hide;
			}
		}

		private static ulong ReadSteamId64()
		{
			try {
				foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
					string name = asm.GetName().Name ?? "";
					if (name != "Steamworks.NET" && name != "tModLoader" && name != "Terraria" &&
					    name.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) < 0)
						continue;

					Type user = asm.GetType("Steamworks.SteamUser");
					if (user == null)
						continue;

					MethodInfo get = user.GetMethod("GetSteamID", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
					if (get == null)
						continue;

					object sid = get.Invoke(null, null);
					if (sid == null)
						continue;

					FieldInfo field = sid.GetType().GetField("m_SteamID", BindingFlags.Public | BindingFlags.Instance);
					if (field?.GetValue(sid) is ulong value && value != 0)
						return value;
				}
			}
			catch {
			}

			return 0;
		}
	}
}
