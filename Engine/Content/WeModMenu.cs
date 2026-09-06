using Terraria;
using Terraria.ModLoader;
using DieWithASmile.Content;

namespace DieWithASmile.Engine.Content
{
	internal static class WeModMenu
	{
		internal static bool IsActive
		{
			get
			{
				DieWithASmileCalamitasMenu menu = ModContent.GetInstance<DieWithASmileCalamitasMenu>();
				return menu != null && MenuLoader.CurrentMenu == menu;
			}
		}

		internal static bool OnTitle => CoolerMenuCompat.OnTitleLike && IsActive;
	}
}
