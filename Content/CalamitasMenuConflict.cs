using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace DieWithASmile.Content
{
	public class CalamitasMenuConflict : ModSystem
	{
		internal const string EntropyMod = "CalamityEntropy";
		internal const string OurMod = "DieWithASmile";

		private static readonly string[] StrongNeedles =
		{
			"malicious code",
			"cannot load these two mods",
			"oh my god, the end of a calamity",
			"easily resolved this issue",
			"please wait for 10 seconds",
			"main game interface",
			"click the left mouse button",
			"恶意代码",
			"无法同时加载",
			"点击鼠标左键",
			"等待10秒",
			"等待 10 秒"
		};

		private static readonly string[] WeakNeedles =
		{
			"DieWithASmile",
			"The End Of A Calamity Menu Theme"
		};

		private static readonly List<FieldInfo> _scareFlags = new();
		private static bool _neutralized;
		private static MethodInfo _findMods;
		private static MethodInfo _deleteMod;

		internal static bool Blocking => false;

		internal static bool OverlayActive => false;

		public override void Load()
		{
			On_Utils.DrawBorderString += DrawBorderStringHook;
			On_Utils.DrawBorderStringFourWay += DrawBorderStringFourWayHook;
			On_ChatManager.DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float += DrawCodedStringHook;
			HookEntropyFonts();
		}

		public override void PostSetupContent()
		{
			CacheDeleteApi();
			FinishPendingDelete();
			NeutralizeEntropyScare();
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (!_neutralized)
				NeutralizeEntropyScare();
			ClearScareFlags();
		}

		private static void HookEntropyFonts()
		{
			try {
				Type ext = typeof(DynamicSpriteFontExtensionMethods);
				foreach (MethodInfo method in ext.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
					if (method.Name != "DrawString")
						continue;
					ParameterInfo[] pars = method.GetParameters();
					if (pars.Length < 3 || pars[2].ParameterType != typeof(string))
						continue;
					MonoModHooks.Modify(method, il => SkipScareDrawString(il, pars));
				}
			}
			catch {
			}
		}

		private static void SkipScareDrawString(ILContext il, ParameterInfo[] pars)
		{
			int textArg = -1;
			for (int i = 0; i < pars.Length; i++) {
				if (pars[i].ParameterType == typeof(string)) {
					textArg = i;
					break;
				}
			}

			if (textArg < 0)
				return;

			var cursor = new ILCursor(il);
			cursor.Goto(0);
			MethodInfo check = typeof(CalamitasMenuConflict).GetMethod(nameof(IsScareText), BindingFlags.NonPublic | BindingFlags.Static);
			if (check == null)
				return;

			ILLabel run = cursor.DefineLabel();
			cursor.Emit(OpCodes.Ldarg, textArg);
			cursor.Emit(OpCodes.Call, check);
			cursor.Emit(OpCodes.Brfalse, run);
			cursor.Emit(OpCodes.Ret);
			cursor.MarkLabel(run);
		}

		private static Vector2 DrawBorderStringHook(
			On_Utils.orig_DrawBorderString orig,
			SpriteBatch sb,
			string text,
			Vector2 pos,
			Color color,
			float scale,
			float anchorx,
			float anchory,
			int maxCharactersDisplayed)
		{
			if (IsScareText(text))
				return Vector2.Zero;
			return orig(sb, text, pos, color, scale, anchorx, anchory, maxCharactersDisplayed);
		}

		private static void DrawBorderStringFourWayHook(
			On_Utils.orig_DrawBorderStringFourWay orig,
			SpriteBatch sb,
			ReLogic.Graphics.DynamicSpriteFont font,
			string text,
			float x,
			float y,
			Color textColor,
			Color borderColor,
			Vector2 origin,
			float scale)
		{
			if (IsScareText(text))
				return;
			orig(sb, font, text, x, y, textColor, borderColor, origin, scale);
		}

		private static Vector2 DrawCodedStringHook(
			On_ChatManager.orig_DrawColorCodedStringWithShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float orig,
			SpriteBatch spriteBatch,
			ReLogic.Graphics.DynamicSpriteFont font,
			string text,
			Vector2 position,
			Color baseColor,
			float rotation,
			Vector2 origin,
			Vector2 baseScale,
			float maxWidth,
			float spread)
		{
			if (IsScareText(text))
				return Vector2.Zero;
			return orig(spriteBatch, font, text, position, baseColor, rotation, origin, baseScale, maxWidth, spread);
		}

		private static bool IsScareText(string text)
		{
			if (string.IsNullOrEmpty(text))
				return false;
			for (int i = 0; i < StrongNeedles.Length; i++) {
				if (text.IndexOf(StrongNeedles[i], StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;
		}

		private static void NeutralizeEntropyScare()
		{
			if (_neutralized)
				return;
			if (!ModLoader.TryGetMod(EntropyMod, out Mod entropy) || entropy.Code == null)
				return;

			_scareFlags.Clear();
			Type[] types;
			try {
				types = entropy.Code.GetTypes();
			}
			catch (ReflectionTypeLoadException ex) {
				types = ex.Types;
			}
			catch {
				return;
			}

			foreach (Type type in types) {
				if (type == null)
					continue;

				bool scareType = IsScareTypeName(type.Name);
				bool strongHit = false;
				foreach (MethodInfo method in SafeMethods(type)) {
					bool strong = MethodHasNeedle(method, StrongNeedles);
					bool weak = scareType && MethodHasNeedle(method, WeakNeedles);
					if (!strong && !weak)
						continue;

					if (strong)
						strongHit = true;
					if (IsLifecycle(method))
						continue;
					PatchSkip(method);
				}

				if (typeof(Mod).IsAssignableFrom(type))
					continue;
				if (type.Name is "EModSys" or "EModPlayer" or "EGlobalNPC" or "EGlobalItem")
					continue;
				if (scareType || strongHit)
					CollectFlags(type);
			}

			_neutralized = true;
			ClearScareFlags();
		}

		private static IEnumerable<MethodInfo> SafeMethods(Type type)
		{
			MethodInfo[] methods;
			try {
				methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			}
			catch {
				yield break;
			}

			for (int i = 0; i < methods.Length; i++) {
				MethodInfo method = methods[i];
				if (method == null || method.IsAbstract || method.ContainsGenericParameters)
					continue;
				yield return method;
			}
		}

		private static bool IsScareTypeName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return false;
			return name.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
			       name.Contains("DieWith", StringComparison.OrdinalIgnoreCase) ||
			       name.Contains("Malicious", StringComparison.OrdinalIgnoreCase) ||
			       name.Contains("MenuFix", StringComparison.OrdinalIgnoreCase) ||
			       name.Contains("CompatScreen", StringComparison.OrdinalIgnoreCase) ||
			       name.Contains("HackDetect", StringComparison.OrdinalIgnoreCase) ||
			       name.Contains("Scare", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsLifecycle(MethodInfo method)
		{
			if (method.Name.Contains('<'))
				return false;
			return method.Name is "Load" or "Unload" or "PostSetupContent" or "SetStaticDefaults" or "SetupContent";
		}

		private static bool MethodHasNeedle(MethodInfo method, string[] needles)
		{
			System.Reflection.MethodBody body;
			try {
				body = method.GetMethodBody();
			}
			catch {
				return false;
			}

			byte[] il = body?.GetILAsByteArray();
			if (il == null || il.Length == 0)
				return false;

			Module module = method.Module;
			for (int i = 0; i < il.Length - 4; i++) {
				if (il[i] != 0x72)
					continue;

				int token = BitConverter.ToInt32(il, i + 1);
				string text;
				try {
					text = module.ResolveString(token);
				}
				catch {
					continue;
				}

				if (string.IsNullOrEmpty(text))
					continue;
				for (int n = 0; n < needles.Length; n++) {
					if (text.IndexOf(needles[n], StringComparison.OrdinalIgnoreCase) >= 0)
						return true;
				}

				i += 4;
			}

			return false;
		}

		private static void PatchSkip(MethodInfo method)
		{
			try {
				MonoModHooks.Modify(method, il => SkipBody(il, method));
			}
			catch {
			}
		}

		private static void SkipBody(ILContext il, MethodInfo method)
		{
			var cursor = new ILCursor(il);
			cursor.Goto(0);
			ParameterInfo[] pars = method.GetParameters();
			int origArg = method.IsStatic ? 0 : 1;
			if (pars.Length > 0 && pars[0].ParameterType.Name.StartsWith("orig_", StringComparison.Ordinal)) {
				MethodInfo invoke = pars[0].ParameterType.GetMethod("Invoke");
				if (invoke != null) {
					cursor.Emit(OpCodes.Ldarg, origArg);
					ParameterInfo[] invokePars = invoke.GetParameters();
					for (int i = 0; i < invokePars.Length; i++)
						cursor.Emit(OpCodes.Ldarg, origArg + 1 + i);
					cursor.Emit(OpCodes.Callvirt, invoke);
					cursor.Emit(OpCodes.Ret);
					return;
				}
			}

			if (method.ReturnType == typeof(void)) {
				cursor.Emit(OpCodes.Ret);
				return;
			}

			if (method.ReturnType == typeof(bool)) {
				cursor.Emit(OpCodes.Ldc_I4_1);
				cursor.Emit(OpCodes.Ret);
			}
		}

		private static void CollectFlags(Type type)
		{
			FieldInfo[] fields;
			try {
				fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch {
				return;
			}

			for (int i = 0; i < fields.Length; i++) {
				FieldInfo field = fields[i];
				if (field == null || field.IsLiteral)
					continue;
				if (field.FieldType == typeof(bool) || field.FieldType == typeof(int) || field.FieldType == typeof(float))
					_scareFlags.Add(field);
			}
		}

		private static void ClearScareFlags()
		{
			for (int i = 0; i < _scareFlags.Count; i++) {
				FieldInfo field = _scareFlags[i];
				try {
					if (field.FieldType == typeof(bool))
						field.SetValue(null, false);
					else if (field.FieldType == typeof(int))
						field.SetValue(null, 0);
					else if (field.FieldType == typeof(float))
						field.SetValue(null, 0f);
				}
				catch {
				}
			}
		}

		private static void CacheDeleteApi()
		{
			const BindingFlags stat = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			Type organizer = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
			if (organizer == null)
				return;
			foreach (MethodInfo method in organizer.GetMethods(stat)) {
				if (method.Name == "FindMods" && _findMods == null)
					_findMods = method;
				if (method.Name == "DeleteMod")
					_deleteMod = method;
			}
		}

		private static string PendingPath =>
			Path.Combine(DieWithASmileSave.RootFolder, "pending-remove.txt");

		private static void FinishPendingDelete()
		{
			string pending = null;
			try {
				if (File.Exists(PendingPath))
					pending = File.ReadAllText(PendingPath).Trim();
			}
			catch {
			}

			if (string.IsNullOrEmpty(pending) || pending == OurMod)
				return;
			if (ModLoader.HasMod(pending))
				return;

			try {
				if (_findMods != null && _deleteMod != null) {
					object raw = _findMods.GetParameters().Length == 0
						? _findMods.Invoke(null, null)
						: _findMods.Invoke(null, new object[] { false });
					if (raw is Array mods) {
						foreach (object local in mods) {
							if (local == null)
								continue;
							string localName = local.GetType().GetProperty("Name")?.GetValue(local) as string;
							if (localName != pending)
								continue;
							_deleteMod.Invoke(null, new[] { local });
							break;
						}
					}
				}
			}
			catch {
			}

			try {
				if (File.Exists(PendingPath))
					File.Delete(PendingPath);
			}
			catch {
			}
		}
	}
}
