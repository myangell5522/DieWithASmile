using Terraria.Localization;

namespace DieWithASmile.Engine.Core
{
	internal static class WeText
	{
		internal static string UI(string key) => Language.GetTextValue("Mods.DieWithASmile.UI." + key);

		internal static string Layer(string id) =>
			Language.GetTextValue("Mods.DieWithASmile.UI.Layer_" + id.Replace('.', '_'));
	}
}
