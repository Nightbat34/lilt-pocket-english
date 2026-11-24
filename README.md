# Lilt Pocket English

A small, quiet, English-first vocabulary companion for Windows. Open it for a word or two, then get back to your day.

一个适合工作碎片时间的英语词汇小窗。默认静音，以音标、英文例句和简单英文解释建立理解，中文释义按需展开。

<img src="assets/suzuha.png" alt="Suzuha Amane application icon" width="120">

## Download and run

Download **PocketEnglish.exe** or **Lilt-Pocket-English-v1.0.0-win-x64.zip** from [V1.0](https://github.com/Nightbat34/lilt-pocket-english/releases/tag/v1.0.0). Double-click the executable. The ZIP contains the same executable plus these instructions.

从 [V1.0 发布页](https://github.com/Nightbat34/lilt-pocket-english/releases/tag/v1.0.0) 下载 EXE 直接运行，或下载 ZIP 后解压运行。无需安装、登录或启动浏览器。

- **Platform:** Windows 10 / 11, x64, with .NET Framework 4.x WPF and System.Speech components available.
- **Window:** 400 × 500 logical pixels; Windows display scaling applies.
- **Sound:** off by default. Optional offline reading uses an installed English Windows voice; silent learning works without one.

## What is included

- **100 built-in cards:** work and code **40%**, cars **35%**, AI and news **25%**.
- **English first:** word, pronunciation, stress cue, a short contextual example, and a simple English explanation. Chinese stays behind “查看中文释义”.
- **Parts of speech:** a visible noun / verb / adjective label; **Uses & word forms** opens the corresponding meanings, examples and inflections. Words with more than one curated use show each sense separately.
- **Double-click to explore:** English words in learning text, examples, definitions and word views open an in-app dictionary page. Nested lookups have a Back button and preserve the original learning card. Very common function words such as “a”, “is” and “are” are skipped.
- **Select and copy:** drag over text, then press Ctrl+C or use the right-click menu. Ctrl+double-click selects a word without opening its dictionary entry.
- **Offline dictionary:** more than 100,000 general entries, plus technical overrides, compressed into the EXE. Plurals, common verb forms and irregular forms can resolve to their headword. An explicit online lookup is available for missing entries; ordinary learning stays offline.
- **Quiet pronunciation practice:** switch to **IPA first**, recall the word, then reveal it.
- **Separate feedback:** **Again** for forgotten meanings, **Sound unsure** for unfamiliar pronunciation, **Got it** for familiar meanings and pronunciation.
- **Spaced reviews:** forgotten words return after 10 minutes; pronunciation practice after a day; familiar cards gradually move to longer intervals. Due reviews are prioritized. Each full block of 20 new built-in cards uses an 8 / 7 / 5 topic mix.
- **Library:** search words, examples and Chinese meanings; filter topics; add your own word or phrase with an English explanation and example.
- **Progress and backups:** local progress, difficult words, JSON export and restore. No daily target or sign-in.

The topic mix applies to the selection of new cards, rather than forcing overdue reviews into a quota. Pronunciation familiarity is self-assessed; silent practice does not measure listening comprehension. Examples are learning scenarios, not current news reports or repair instructions.

## Everyday controls

| Control | Behavior |
| --- | --- |
| **×** | Save progress and exit / 保存并退出 |
| **−** | Hide to the system tray / 隐藏到系统托盘 |
| Tray icon | Restore the window / 恢复窗口 |
| Run the EXE again | Restore the existing instance for the same data directory |
| **Space** | Reveal the word in IPA mode, or toggle Chinese |
| **1 / 2 / 3** | Again / Sound unsure / Got it |
| **⚙** | Quiet mode, keep on top, backup export and restore |

Progress is saved after every answer and when closing. It lives at `%LOCALAPPDATA%\LiltPocketEnglish\progress.json`, with the previous save in `progress.json.bak`. Moving or replacing the EXE does not move or reset your progress. The data format remains version 1.

## Build from source

No NuGet packages are required. From a PowerShell prompt in the repository:

```powershell
.\build.ps1
```

The build uses the Windows .NET Framework C# compiler and creates `release\PocketEnglish.exe`. To choose another output directory:

```powershell
.\build.ps1 -OutputDirectory .\dist\v1.0.0
```

The application icon is embedded in the EXE and used for the window and tray. The Suzuha cutout has a genuine transparent background. `assets/suzuha.ico` contains 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixel images. Regenerate it from the transparent PNG with:

```powershell
.\tools\make-icon.ps1
```

The offline dictionary is packaged as indexed gzip chunks in `assets/dictionary.bin`; only the relevant initial-letter chunk is searched, and results are cached on demand. It is not fully loaded into memory at startup. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for sources, licensing and adaptation details. `tools/build-dictionary.cjs` regenerates the bundle from the named upstream snapshots in `validation/dictionary-source/`.

## Verification

The executable contains isolated checks for the complete vocabulary, topic balance, due review priority, meaning versus pronunciation state, Shanghai calendar boundaries, progress continuity, damaged-file recovery, custom cards, backup validation, parts of speech, corresponding examples, inflection lookup, incidental-word lookup, selectable content and icon alpha transparency.

```powershell
$report = Join-Path $PWD 'core-tests.txt'
Start-Process .\release\PocketEnglish.exe -ArgumentList '--self-test', $report -Wait
Get-Content $report
```

Self-tests use a separate generated data folder beside the report. Normal learning progress is not modified. For an isolated manual session:

```powershell
.\release\PocketEnglish.exe --data-dir "$env:TEMP\Lilt-manual-test"
```

## Icon

The Suzuha Amane (阿万音铃羽) image was supplied for this application by the project owner. Character and image rights remain with their respective rights holders.
