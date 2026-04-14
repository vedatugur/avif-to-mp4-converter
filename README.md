# AVIF to MP4 Converter

A standalone Windows app that converts AVIF files (including animated AVIF) to MP4. Single `.exe`, no install, no dependencies — ffmpeg is bundled inside.

## Getting `Converter.exe`

The compiled `Converter.exe` is **not checked into this repository** (see [.gitignore](.gitignore)) — binaries don't belong in source control. You have two options:

- **Download the latest release:** [Converter.exe (latest)](https://github.com/vedatugur/avif-to-mp4-converter/releases/latest/download/Converter.exe). Browse all versions on the [Releases page](https://github.com/vedatugur/avif-to-mp4-converter/releases).
- **Build it yourself** from [`Converter.cs`](Converter.cs) — see [Rebuilding from source](#rebuilding-from-source-optional) below. Takes under a minute and needs nothing beyond what ships with Windows.

## Requirements

- Windows 10 or 11 (64-bit).
- No runtime install needed — uses .NET Framework 4.x which ships with Windows.

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

The `/resource:ffmpeg.exe,ffmpeg.exe` flag embeds `ffmpeg.exe` as a resource inside the output, which is why the final `Converter.exe` is self-contained.

## Troubleshooting

- **"Windows protected your PC" SmartScreen warning** — expected for unsigned binaries. Click *More info → Run anyway*.
- **ffmpeg extraction fails** — make sure `%TEMP%\avif-to-mp4\` is writable, or clear that folder and relaunch.
- **Output won't play in a specific app** — try CRF 15 or higher instead of CRF 0 (lossless uses a profile some Windows apps don't decode).

## License & attribution

- `ffmpeg` is bundled from the [BtbN GPL static builds](https://github.com/BtbN/FFmpeg-Builds/releases). ffmpeg is licensed under the GPL; redistributing `Converter.exe` means you are redistributing ffmpeg and should comply with its license terms.
- No license file ships with this project yet — treat the source as "all rights reserved" unless the author adds one.
