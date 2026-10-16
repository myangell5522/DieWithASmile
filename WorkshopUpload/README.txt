WorkshopUpload - animated logo for the Steam Workshop page
=========================================================

This folder is excluded from the mod build (see buildIgnore in build.txt), so nothing here
ends up inside the .tmod file.

Files
  animated-logo-480.gif   Recommended. 480x480, 32-frame loop, about 640 KB.
  animated-logo-640.gif   Bigger, sharper copy (about 900 KB) for the screenshot gallery.
  animated-logo-240.gif   Small copy, the same file the in-game mod list uses.
  still-logo-512.png      First frame as a still image (for places that do not accept GIFs).

How to use it
  Easiest: publish from tModLoader as usual. workshop.json already points the preview image at
  Logo/mod-icon.gif, which is the same animated 480 px logo, so it is uploaded automatically.

  Only the picture, without republishing the mod: double-click publish_logo.bat.
    - It downloads Valve's official steamcmd into WorkshopUpload\steamcmd\ the first time.
    - It asks for the Workshop item ID (the number after "?id=" on the mod's page) and your
      Steam account name. steamcmd itself then asks for the password and Steam Guard code.
    - It changes only the main preview image; mod files, title, description and tags stay.

  Gallery: on the Workshop page, "Add/edit images & videos" also accepts animated-logo-640.gif.
  Steam's limit for the main preview image is 1 MB, which is why the 480 version is used.

All frames are scaled with nearest-neighbour from the 80x80 pixel art, so every pixel stays crisp.
