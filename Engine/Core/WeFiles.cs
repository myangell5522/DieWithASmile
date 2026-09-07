using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DieWithASmile.Engine.Core
{
	internal static class WeFiles
	{
		internal static bool TryPickAudio(out string path) =>
			TryPick(out path, ShowAudio, "Upload a song", ".ogg", ".mp3", ".wav");

		internal static bool TryPickImage(out string path) =>
			TryPick(out path, ShowImage, "Upload an image", ".png", ".jpg", ".jpeg", ".gif");

		internal static bool TryPickIcon(out string path) =>
			TryPick(out path, ShowIcon, "Choose a window icon", ".ico", ".png", ".jpg", ".jpeg");

		internal static bool TryPickFont(out string path) =>
			TryPick(out path, ShowFont, "Choose a font", ".ttf", ".otf");

		internal static void OpenFile(string path)
		{
			try {
				if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
					return;
				WeOs.Reveal(path);
			}
			catch {
			}
		}

		internal static void OpenFolder(string folder)
		{
			try {
				Directory.CreateDirectory(folder);
				WeOs.Reveal(folder);
			}
			catch {
			}
		}

		internal static void ClearFolderContents(string folder)
		{
			try {
				if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
					return;

				foreach (string file in Directory.GetFiles(folder)) {
					try {
						File.Delete(file);
					}
					catch {
					}
				}

				foreach (string dir in Directory.GetDirectories(folder)) {
					try {
						Directory.Delete(dir, recursive: true);
					}
					catch {
					}
				}
			}
			catch {
			}
		}

		private static bool TryPick(out string path, Func<string> windowsPicker, string title, params string[] extensions)
		{
			path = null;
			string picked = null;
			try {
				if (OperatingSystem.IsWindows()) {
					var thread = new Thread(() => {
						try {
							picked = windowsPicker();
						}
						catch {
						}
					});
					thread.SetApartmentState(ApartmentState.STA);
					thread.Start();
					thread.Join();
				}
				else {
					picked = WeOs.PickFile(title, extensions);
				}
			}
			catch {
				return false;
			}

			if (string.IsNullOrEmpty(picked) || !File.Exists(picked))
				return false;

			path = picked;
			return true;
		}

		private static string ShowAudio() => ShowDialog(
			"Audio (*.ogg;*.mp3;*.wav)\0*.ogg;*.mp3;*.wav\0Ogg\0*.ogg\0MP3\0*.mp3\0WAV\0*.wav\0",
			"Upload a song");

		private static string ShowImage() => ShowDialog(
			"Images (*.png;*.jpg;*.jpeg;*.gif)\0*.png;*.jpg;*.jpeg;*.gif\0PNG\0*.png\0JPEG\0*.jpg;*.jpeg\0GIF\0*.gif\0",
			"Upload an image");

		private static string ShowIcon() => ShowDialog(
			"Icons (*.ico;*.png;*.jpg;*.jpeg)\0*.ico;*.png;*.jpg;*.jpeg\0ICO\0*.ico\0PNG\0*.png\0",
			"Choose a window icon");

		private static string ShowFont() => ShowDialog(
			"Fonts (*.ttf;*.otf)\0*.ttf;*.otf\0TrueType\0*.ttf\0OpenType\0*.otf\0",
			"Choose a font");

		private static string ShowDialog(string filter, string title)
		{
			var ofn = new OpenFileName();
			ofn.lStructSize = Marshal.SizeOf<OpenFileName>();
			ofn.lpstrFilter = filter;
			ofn.lpstrFile = new string('\0', 1024);
			ofn.nMaxFile = ofn.lpstrFile.Length;
			ofn.lpstrTitle = title;
			ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800;
			try {
				return GetOpenFileName(ref ofn) ? ofn.lpstrFile.Split('\0')[0] : null;
			}
			catch {
				return null;
			}
		}

		internal static string UniquePath(string folder, string fileName)
		{
			Directory.CreateDirectory(folder);
			string dest = Path.Combine(folder, fileName);
			if (!File.Exists(dest))
				return dest;

			string name = Path.GetFileNameWithoutExtension(fileName);
			string ext = Path.GetExtension(fileName);
			for (int i = 2; i < 100; i++) {
				dest = Path.Combine(folder, $"{name}_{i}{ext}");
				if (!File.Exists(dest))
					return dest;
			}

			return Path.Combine(folder, $"{name}_{Guid.NewGuid():N}{ext}");
		}

		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool GetOpenFileName(ref OpenFileName ofn);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct OpenFileName
		{
			public int lStructSize;
			public IntPtr hwndOwner;
			public IntPtr hInstance;
			public string lpstrFilter;
			public string lpstrCustomFilter;
			public int nMaxCustFilter;
			public int nFilterIndex;
			public string lpstrFile;
			public int nMaxFile;
			public string lpstrFileTitle;
			public int nMaxFileTitle;
			public string lpstrInitialDir;
			public string lpstrTitle;
			public int Flags;
			public short nFileOffset;
			public short nFileExtension;
			public string lpstrDefExt;
			public IntPtr lCustData;
			public IntPtr lpfnHook;
			public string lpTemplateName;
			public IntPtr pvReserved;
			public int dwReserved;
			public int FlagsEx;
		}
	}
}
