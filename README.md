# AVIF to MP4 Converter

A standalone Windows app that converts AVIF files (including animated AVIF) to MP4. Single `.exe`, no install, no dependencies — ffmpeg is bundled inside.

## Usage

1. Double-click **`Converter.exe`** to launch.
2. Add files via the **Add files...** button, or **drag-and-drop** `.avif` files into the window (multi-select supported).
3. Pick a quality setting (see below).
4. Click **Convert all to MP4**. Output MP4s are saved next to each source file.

## Quality settings

Quality is controlled by **CRF** (Constant Rate Factor). Lower = better quality and bigger files.

| CRF | Result |
| --- | --- |
| 0 | Mathematically lossless (may not play in some Windows apps) |
| 15 | Visually lossless *(default)* |
| 18 | Excellent |
| 23 | Good |
| 28 | Smaller file |

Or tick **Max quality (near-lossless)** to force `-crf 12 -preset veryslow` — visually indistinguishable from source and plays in every player.

## Portability

- Single file — `ffmpeg` is embedded as a resource inside `Converter.exe`.
- No install, no dependencies beyond what ships with Windows 10/11.
- On first run, ffmpeg is extracted to `%TEMP%\avif-to-mp4\` (cached there for reuse).
- To share: just send `Converter.exe`.

## Rebuilding from source (optional)

Source: [`Converter.cs`](Converter.cs).

You need `ffmpeg.exe` — get the static build from [BtbN/FFmpeg-Builds releases](https://github.com/BtbN/FFmpeg-Builds/releases) (file: `ffmpeg-master-latest-win64-gpl.zip`). Place `ffmpeg.exe` next to `Converter.cs`, then compile:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /out:Converter.exe ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /resource:ffmpeg.exe,ffmpeg.exe ^
  Converter.cs
```

`csc.exe` ships with .NET Framework 4.x on every Windows 10/11 — no extra install needed.
