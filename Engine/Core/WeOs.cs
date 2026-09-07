using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DieWithASmile.Engine.Core
{
	internal static class WeOs
	{
		internal static bool IsWindows
		{
			get
			{
				try {
					return OperatingSystem.IsWindows();
				}
				catch {
					return false;
				}
			}
		}

		internal static bool IsMac
		{
			get
			{
				try {
					return OperatingSystem.IsMacOS();
				}
				catch {
					return false;
				}
			}
		}

		internal static void Reveal(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			try {
				if (IsWindows) {
					Process.Start(new ProcessStartInfo {
						FileName = path,
						UseShellExecute = true
					});
					return;
				}

				if (IsMac) {
					Run("open", path);
					return;
				}

				Run("xdg-open", path);
			}
			catch {
			}
		}

		internal static string PickFile(string title, params string[] extensions)
		{
			try {
				string picked = IsMac ? PickMac(title) : PickLinux(title, extensions);
				picked = (picked ?? "").Trim().Trim('"');
				if (string.IsNullOrEmpty(picked) || !File.Exists(picked))
					return null;
				if (extensions == null || extensions.Length == 0)
					return picked;

				string ext = Path.GetExtension(picked);
				foreach (string want in extensions) {
					if (ext.Equals(want, StringComparison.OrdinalIgnoreCase))
						return picked;
				}
			}
			catch {
			}

			return null;
		}

		private static string PickMac(string title)
		{
			string prompt = EscapeApple(title);
			string script =
				"try\n" +
				"POSIX path of (choose file with prompt \"" + prompt + "\")\n" +
				"on error\n" +
				"return \"\"\n" +
				"end try";
			return RunCapture("osascript", "-e", script);
		}

		private static string PickLinux(string title, string[] extensions)
		{
			string filter = LinuxFilter(extensions);
			string zenity = string.IsNullOrEmpty(filter)
				? RunCapture("zenity", "--file-selection", "--title=" + title)
				: RunCapture("zenity", "--file-selection", "--title=" + title, "--file-filter=" + filter);
			if (!string.IsNullOrEmpty(zenity))
				return zenity;

			return RunCapture("kdialog", "--getopenfilename", ".", LinuxGlob(extensions));
		}

		private static string LinuxFilter(string[] extensions)
		{
			if (extensions == null || extensions.Length == 0)
				return "";
			var sb = new StringBuilder("Supported |");
			foreach (string ext in extensions)
				sb.Append(" *").Append(ext.TrimStart('.'));
			return sb.ToString();
		}

		private static string LinuxGlob(string[] extensions)
		{
			if (extensions == null || extensions.Length == 0)
				return "*";
			var sb = new StringBuilder();
			foreach (string ext in extensions) {
				if (sb.Length > 0)
					sb.Append(' ');
				sb.Append('*').Append(ext);
			}

			return sb.ToString();
		}

		private static string EscapeApple(string text)
		{
			if (string.IsNullOrEmpty(text))
				return "";
			return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}

		private static void Run(string fileName, params string[] args)
		{
			using Process proc = Start(fileName, args, capture: false);
			proc?.WaitForExit(15000);
		}

		private static string RunCapture(string fileName, params string[] args)
		{
			using Process proc = Start(fileName, args, capture: true);
			if (proc == null)
				return null;
			string output = proc.StandardOutput.ReadToEnd();
			proc.WaitForExit(120000);
			return proc.ExitCode == 0 ? output : null;
		}

		private static Process Start(string fileName, string[] args, bool capture)
		{
			var info = new ProcessStartInfo(fileName) {
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = capture,
				RedirectStandardError = capture
			};
			if (args != null) {
				foreach (string arg in args)
					info.ArgumentList.Add(arg);
			}

			return Process.Start(info);
		}
	}
}
