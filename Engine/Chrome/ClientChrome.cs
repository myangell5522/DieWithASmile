using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.OS;
using SDL2;
using Terraria;
using Terraria.ModLoader;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.Chrome
{
	/// <summary>Re-applies our window title and icon whenever vanilla resets them (language change, etc.).</summary>
	public class ClientChromeSystem : ModSystem
	{
		public override void Load()
		{
			if (!Main.dedServ)
				On_Main.SetTitle += SetTitleHook;
		}

		public override void Unload()
		{
			On_Main.SetTitle -= SetTitleHook;
		}

		private static void SetTitleHook(On_Main.orig_SetTitle orig, Main self)
		{
			orig(self);
			try {
				ClientChrome.ApplyTitle();
				ClientChrome.ApplyIcon();
			}
			catch {
			}
		}
	}

	/// <summary>
	/// The game window's title bar. Colours use DWM and only exist on Windows 11 (dark mode on Windows 10);
	/// on Linux and macOS the desktop draws the title bar, so only the icon and title text can change there.
	/// Icon and title work everywhere: Win32 on Windows, SDL (which FNA runs on) elsewhere.
	/// </summary>
	internal static class ClientChrome
	{
		internal const int TitleVanilla = 0;
		internal const int TitlePlain = 1;
		internal const int TitleTheme = 2;

		private static bool _sdlIconSet;
		private static bool _titleSet;

		private static int WinBuild
		{
			get
			{
				try {
					return Environment.OSVersion.Version.Build;
				}
				catch {
					return 0;
				}
			}
		}

		/// <summary>Caption / border / title-text colours (Windows 11, build 22000+).</summary>
		internal static bool ColorsSupported => WeOs.IsWindows && WinBuild >= 22000;

		/// <summary>Dark title bar (Windows 10 1809+).</summary>
		internal static bool DarkSupported => WeOs.IsWindows && WinBuild >= 17763;

		internal static string TitleText(int mode) => mode switch {
			TitlePlain => "Terraria",
			TitleTheme => "Terraria: " + ThemeName(),
			_ => WeText.UI("TitleVanilla")
		};

		private static string ThemeName()
		{
			try {
				string name = ModContent.GetInstance<global::DieWithASmile.DieWithASmile>()?.DisplayName;
				return string.IsNullOrWhiteSpace(name) ? "The End of a Calamity" : name;
			}
			catch {
				return "The End of a Calamity";
			}
		}

		internal static void SetTitleMode(int mode)
		{
			mode = Math.Clamp(mode, 0, 2);
			int was = WeSave.Data.WindowTitleMode;
			WeSave.Data.WindowTitleMode = mode;
			WeSave.Save();
			if (mode == TitleVanilla) {
				if (was != TitleVanilla || _titleSet)
					Main.changeTheTitle = true; // vanilla picks a fresh random title next frame
				_titleSet = false;
				return;
			}

			ApplyTitle();
		}

		internal static void ApplyTitle()
		{
			int mode = WeSave.Data.WindowTitleMode;
			if (mode == TitleVanilla || Main.instance?.Window == null)
				return;
			try {
				Platform.Get<IWindowService>().SetUnicodeTitle(Main.instance.Window, TitleText(mode));
				_titleSet = true;
			}
			catch {
			}
		}

		/// <summary>Uses the mod's own icon as the window icon.</summary>
		internal static void UseModIcon()
		{
			try {
				byte[] bytes = ModContent.GetInstance<global::DieWithASmile.DieWithASmile>().GetFileBytes("icon.png");
				if (bytes == null || bytes.Length == 0)
					return;
				WeSave.EnsureFolders();
				string dest = Path.Combine(WeSave.IconFolder, "DieWithASmile.png");
				File.WriteAllBytes(dest, bytes);
				WeSave.Data.WindowIconFile = Path.GetFileName(dest);
				WeSave.Save();
				ApplyIcon();
				WeToast.Show("ToastIcon");
			}
			catch {
			}
		}

		/// <summary>Applies the saved window icon, or restores the default when none is set.</summary>
		internal static void ApplyIcon()
		{
			string file = WeSave.Data.WindowIconFile;
			string path = string.IsNullOrEmpty(file) ? null : Path.Combine(WeSave.IconFolder, file);
			bool has = path != null && File.Exists(path);
			if (WeOs.IsWindows) {
				IntPtr hwnd = Hwnd();
				if (hwnd == IntPtr.Zero)
					return;
				if (has)
					ApplySavedIcon(hwnd);
				else
					RestoreIcon(hwnd);
				return;
			}

			if (has)
				SetSdlIcon(path);
			else if (_sdlIconSet)
				RestoreSdlIcon();
		}

		private static void SetSdlIcon(string path)
		{
			GCHandle pin = default;
			Texture2D tex = null;
			try {
				if (Main.instance?.Window == null || Main.instance.GraphicsDevice == null)
					return;
				using (FileStream stream = File.OpenRead(path))
					tex = Texture2D.FromStream(Main.instance.GraphicsDevice, stream);
				int w = tex.Width;
				int h = tex.Height;
				var px = new Color[w * h];
				tex.GetData(px);
				pin = GCHandle.Alloc(px, GCHandleType.Pinned);
				// Color is laid out R,G,B,A in memory: little-endian RGBA32 masks.
				IntPtr surface = SDL.SDL_CreateRGBSurfaceFrom(pin.AddrOfPinnedObject(), w, h, 32, w * 4,
					0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
				if (surface == IntPtr.Zero)
					return;
				SDL.SDL_SetWindowIcon(Main.instance.Window.Handle, surface); // SDL copies the pixels
				SDL.SDL_FreeSurface(surface);
				_sdlIconSet = true;
			}
			catch {
			}
			finally {
				if (pin.IsAllocated)
					pin.Free();
				tex?.Dispose();
			}
		}

		private static void RestoreSdlIcon()
		{
			try {
				if (Main.instance?.Window != null)
					Platform.Get<IWindowService>().SetIcon(Main.instance.Window);
			}
			catch {
			}

			_sdlIconSet = false;
		}

		private const int DwmwaUseImmersiveDarkMode = 20;
		private const int DwmwaBorderColor = 34;
		private const int DwmwaCaptionColor = 35;
		private const int DwmwaTextColor = 36;
		private const uint DwmwaColorDefault = 0xFFFFFFFF;
		private const int WmSetIcon = 0x0080;
		private const int WmGetIcon = 0x007F;
		private const int IconSmall = 0;
		private const int IconBig = 1;
		private const uint ImageIcon = 1;
		private const uint LrLoadFromFile = 0x00000010;

		private static IntPtr _originalSmall;
		private static IntPtr _originalBig;
		private static IntPtr _customIcon;
		private static bool _captured;

		internal static void Apply()
		{
			try {
				ApplyTitle();
				ApplyIcon();
			}
			catch {
			}

			if (!WeOs.IsWindows)
				return;

			try {
				IntPtr hwnd = Hwnd();
				if (hwnd == IntPtr.Zero)
					return;

				WeSaveData data = WeSave.Data;
				if (!data.ChromeCustom) {
					SetAttr(hwnd, DwmwaUseImmersiveDarkMode, 1);
					return;
				}

				SetAttr(hwnd, DwmwaUseImmersiveDarkMode, data.DarkTitleBar ? 1 : 0);
				if (!ColorsSupported)
					return;
				SetColor(hwnd, DwmwaCaptionColor, data.CaptionR, data.CaptionG, data.CaptionB);
				SetColor(hwnd, DwmwaBorderColor, data.BorderR, data.BorderG, data.BorderB);
				SetColor(hwnd, DwmwaTextColor, data.TitleTextR, data.TitleTextG, data.TitleTextB);
			}
			catch {
			}
		}

		/// <summary>Turns custom title-bar colours off and puts the system colours back.</summary>
		internal static void ResetColors()
		{
			WeSave.Data.ChromeCustom = false;
			WeSave.Save();
			if (!WeOs.IsWindows)
				return;
			IntPtr hwnd = Hwnd();
			if (hwnd == IntPtr.Zero)
				return;
			try {
				uint def = DwmwaColorDefault;
				DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref def, sizeof(uint));
				DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref def, sizeof(uint));
				DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref def, sizeof(uint));
				int dark = 1;
				DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
			}
			catch {
			}
		}

		internal static void Reset()
		{
			ResetColors();
			WeSave.Data.WindowIconFile = "";
			WeSave.Save();
			SetTitleMode(TitleVanilla);
			try {
				ApplyIcon();
			}
			catch {
			}
		}

		internal static void SetIcon(string sourcePath)
		{
			try {
				WeSave.EnsureFolders();
				string dest = WeFiles.UniquePath(WeSave.IconFolder, Path.GetFileName(sourcePath));
				File.Copy(sourcePath, dest, overwrite: false);
				WeSave.Data.WindowIconFile = Path.GetFileName(dest);
				WeSave.Save();
				ApplyIcon();
				WeToast.Show("ToastIcon");
			}
			catch {
			}
		}

		internal static void Unload()
		{
			if (_titleSet)
				Main.changeTheTitle = true;
			_titleSet = false;
			if (!WeOs.IsWindows) {
				if (_sdlIconSet)
					RestoreSdlIcon();
				return;
			}

			IntPtr hwnd = Hwnd();
			if (hwnd != IntPtr.Zero)
				RestoreIcon(hwnd);
			DestroyCustom();
		}

		private static void ApplySavedIcon(IntPtr hwnd)
		{
			if (string.IsNullOrEmpty(WeSave.Data.WindowIconFile))
				return;

			string path = Path.Combine(WeSave.IconFolder, WeSave.Data.WindowIconFile);
			if (!File.Exists(path))
				return;

			Capture(hwnd);
			IntPtr icon = LoadIconFile(path);
			if (icon == IntPtr.Zero)
				icon = CreateIconFromImage(path);
			if (icon == IntPtr.Zero)
				return;

			DestroyCustom();
			_customIcon = icon;
			SendMessage(hwnd, WmSetIcon, (IntPtr)IconSmall, icon);
			SendMessage(hwnd, WmSetIcon, (IntPtr)IconBig, icon);
		}

		private static void Capture(IntPtr hwnd)
		{
			if (_captured)
				return;
			_originalSmall = SendMessage(hwnd, WmGetIcon, (IntPtr)IconSmall, IntPtr.Zero);
			_originalBig = SendMessage(hwnd, WmGetIcon, (IntPtr)IconBig, IntPtr.Zero);
			_captured = true;
		}

		private static void RestoreIcon(IntPtr hwnd)
		{
			if (!_captured)
				return;
			SendMessage(hwnd, WmSetIcon, (IntPtr)IconSmall, _originalSmall);
			SendMessage(hwnd, WmSetIcon, (IntPtr)IconBig, _originalBig);
			DestroyCustom();
		}

		private static void DestroyCustom()
		{
			if (_customIcon == IntPtr.Zero)
				return;
			DestroyIcon(_customIcon);
			_customIcon = IntPtr.Zero;
		}

		private static void SetAttr(IntPtr hwnd, int attr, int value)
		{
			try {
				DwmSetWindowAttribute(hwnd, attr, ref value, sizeof(int));
			}
			catch {
			}
		}

		private static void SetColor(IntPtr hwnd, int attr, int r, int g, int b)
		{
			try {
				uint color = (uint)(Math.Clamp(r, 0, 255) | (Math.Clamp(g, 0, 255) << 8) | (Math.Clamp(b, 0, 255) << 16));
				DwmSetWindowAttribute(hwnd, attr, ref color, sizeof(uint));
			}
			catch {
			}
		}

		private static IntPtr Hwnd()
		{
			try {
				IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
				if (h != IntPtr.Zero)
					return h;
			}
			catch {
			}

			try {
				return Main.instance?.Window?.Handle ?? IntPtr.Zero;
			}
			catch {
				return IntPtr.Zero;
			}
		}

		private static IntPtr LoadIconFile(string path)
		{
			if (!path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
				return IntPtr.Zero;
			return LoadImage(IntPtr.Zero, path, ImageIcon, 32, 32, LrLoadFromFile);
		}

		private static IntPtr CreateIconFromImage(string path)
		{
			try {
				using FileStream stream = File.OpenRead(path);
				Texture2D tex = Texture2D.FromStream(Main.instance.GraphicsDevice, stream);
				int size = 32;
				var src = new Color[tex.Width * tex.Height];
				tex.GetData(src);
				var bits = new int[size * size];
				for (int y = 0; y < size; y++) {
					for (int x = 0; x < size; x++) {
						int sx = x * tex.Width / size;
						int sy = y * tex.Height / size;
						Color c = src[sy * tex.Width + sx];
						bits[y * size + x] = (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
					}
				}

				GCHandle handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
				try {
					IntPtr hbmColor = CreateBitmap(size, size, 1, 32, handle.AddrOfPinnedObject());
					IntPtr hbmMask = CreateBitmap(size, size, 1, 1, IntPtr.Zero);
					var info = new IconInfo {
						fIcon = true,
						xHotspot = 0,
						yHotspot = 0,
						hbmMask = hbmMask,
						hbmColor = hbmColor
					};
					IntPtr icon = CreateIconIndirect(ref info);
					DeleteObject(hbmColor);
					DeleteObject(hbmMask);
					tex.Dispose();
					return icon;
				}
				finally {
					handle.Free();
				}
			}
			catch {
				return IntPtr.Zero;
			}
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int size);

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint attrValue, int size);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32.dll")]
		private static extern IntPtr CreateIconIndirect(ref IconInfo piconinfo);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		[StructLayout(LayoutKind.Sequential)]
		private struct IconInfo
		{
			public bool fIcon;
			public int xHotspot;
			public int yHotspot;
			public IntPtr hbmMask;
			public IntPtr hbmColor;
		}
	}
}
