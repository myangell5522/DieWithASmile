using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace DieWithASmile
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class DieWithASmile : Mod
	{
		/// <summary>
		/// Cross-mod API.
		///   Call("TitleVolumeRows") -> int: extra rows this mod draws under the vanilla title-screen
		///   volume bars this frame (used by Swap My Audio! to place its Audio Device line below them).
		/// </summary>
		public override object Call(params object[] args)
		{
			if (args == null || args.Length == 0 || args[0] is not string command)
				return null;

			return command.ToLowerInvariant() switch {
				"titlevolumerows" => Content.CalamitasMenuVolume.TitleRows,
				_ => null
			};
		}
	}
}
