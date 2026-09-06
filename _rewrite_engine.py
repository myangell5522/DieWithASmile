from pathlib import Path

root = Path(r"C:\Users\myangell\Documents\My Games\Terraria\tModLoader\ModSources\DieWithASmile\Engine")
n = 0
for p in root.rglob("*.cs"):
    t = p.read_text(encoding="utf-8")
    o = t
    t = t.replace("namespace WallpaperEngine.Audio", "namespace DieWithASmile.Engine.Audio")
    t = t.replace("using WallpaperEngine.Audio", "using DieWithASmile.Engine.Audio")
    t = t.replace("namespace WallpaperEngine.", "namespace DieWithASmile.Engine.")
    t = t.replace("using WallpaperEngine.", "using DieWithASmile.Engine.")
    t = t.replace("WallpaperEngine/Assets/", "DieWithASmile/Assets/")
    t = t.replace("Mods.WallpaperEngine", "Mods.DieWithASmile")
    t = t.replace('Path.Combine(Main.SavePath, "WallpaperEngine")', 'Path.Combine(Main.SavePath, "DieWithASmile", "Engine")')
    t = t.replace("mod is WallpaperEngineMod", "mod is DieWithASmile")
    t = t.replace("WallpaperEngineMod", "DieWithASmile")
    t = t.replace('"WallpaperEngine"', '"DieWithASmile"')
    t = t.replace("WallpaperEngine-tModLoader", "DieWithASmile-tModLoader")
    if t != o:
        p.write_text(t, encoding="utf-8")
        n += 1
print("rewritten", n)
