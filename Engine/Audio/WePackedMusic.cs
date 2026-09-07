using System;
using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using DieWithASmile.Content;
using DieWithASmile.Engine.Core;

namespace DieWithASmile.Engine.Audio
{
	internal static class WePackedMusic
	{
		private static readonly List<MenuTrack> Packed = new();
		private static bool _extracted;

		internal static IReadOnlyList<MenuTrack> Tracks => Packed;

		internal static void ForceExtract(Mod mod)
		{
			_extracted = false;
			Packed.Clear();
			EnsureExtracted(mod);
		}

		internal static void EnsureExtracted(Mod mod)
		{
			WeSave.EnsureFolders();
			if (mod == null)
				return;

			if (!_extracted) {
				_extracted = true;
				Packed.Clear();
			}

			foreach (var built in CalamitasMenuPlaylist.BuiltIn) {
				if (Packed.Exists(track => track.Id == built.Id))
					continue;

				string dest = Extract(mod, built.Path);
				if (string.IsNullOrEmpty(dest))
					continue;

				Packed.Add(new MenuTrack {
					Id = built.Id,
					FileName = Path.GetFileName(dest),
					Path = dest,
					Title = built.Title,
					Artist = string.IsNullOrWhiteSpace(built.CoverArtist) ? built.Artist : built.CoverArtist,
					StartSeconds = built.StartSeconds,
					Packed = true,
					Enabled = true
				});
			}
		}

		internal static IEnumerable<MenuTrack> Enabled()
		{
			WeSaveData data = WeSave.Data;
			foreach (MenuTrack track in Packed) {
				track.Enabled = !data.DisabledTrackIds.Contains(track.Id);
				if (track.Enabled && File.Exists(track.AudioPath))
					yield return track;
			}
		}

		private static string Extract(Mod mod, string assetPath)
		{
			assetPath = (assetPath ?? "").Replace('\\', '/');
			string stem = Path.GetFileName(assetPath);
			byte[] bytes = ReadAsset(mod, assetPath);
			if (bytes == null || bytes.Length == 0)
				return null;

			string ext = GuessExt(mod, assetPath, bytes);
			string dest = Path.Combine(WeSave.PackedFolder, stem + ext);
			try {
				if (!File.Exists(dest) || new FileInfo(dest).Length != bytes.Length)
					File.WriteAllBytes(dest, bytes);
				return dest;
			}
			catch {
				return null;
			}
		}

		private static byte[] ReadAsset(Mod mod, string assetPath)
		{
			string[] names = {
				assetPath + ".ogg",
				assetPath + ".mp3",
				assetPath + ".wav",
				assetPath
			};
			foreach (string name in names) {
				try {
					if (mod.FileExists(name))
						return mod.GetFileBytes(name);
				}
				catch {
				}
			}

			try {
				foreach (string file in mod.GetFileNames()) {
					string n = file.Replace('\\', '/');
					if (n.StartsWith(assetPath, StringComparison.OrdinalIgnoreCase) &&
					    (n.Length == assetPath.Length || n[assetPath.Length] == '.'))
						return mod.GetFileBytes(file);
				}
			}
			catch {
			}

			return null;
		}

		private static string GuessExt(Mod mod, string assetPath, byte[] bytes)
		{
			foreach (string file in SafeNames(mod)) {
				string n = file.Replace('\\', '/');
				if (!n.StartsWith(assetPath, StringComparison.OrdinalIgnoreCase))
					continue;
				string ext = Path.GetExtension(n);
				if (!string.IsNullOrEmpty(ext))
					return ext.ToLowerInvariant();
			}

			if (bytes.Length >= 4 && bytes[0] == 0x4F && bytes[1] == 0x67 && bytes[2] == 0x67 && bytes[3] == 0x53)
				return ".ogg";
			if (bytes.Length >= 3 && bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33)
				return ".mp3";
			if (bytes.Length >= 2 && bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
				return ".mp3";
			if (bytes.Length >= 4 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
				return ".wav";
			return ".ogg";
		}

		private static IEnumerable<string> SafeNames(Mod mod)
		{
			try {
				return mod.GetFileNames();
			}
			catch {
				return Array.Empty<string>();
			}
		}
	}
}
