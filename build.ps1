param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'release'))
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskFramework = Split-Path $taskCompiler
$taskOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskDictionary = Join-Path $taskOutput 'dictionary.bin'
$taskDictionaryOutput = [System.IO.File]::Create($taskDictionary)
try { foreach ($taskPart in (Get-ChildItem -LiteralPath (Join-Path $taskRoot 'assets') -Filter 'dictionary-chunk-*.bin' | Sort-Object Name)) { $taskInput = [System.IO.File]::OpenRead($taskPart.FullName); try { $taskInput.CopyTo($taskDictionaryOutput) } finally { $taskInput.Dispose() } } }
finally { $taskDictionaryOutput.Dispose() }
$taskFontResources = Join-Path $taskOutput 'PocketEnglish.g.resources'
$taskFontStream = [System.IO.MemoryStream]::new()
foreach ($taskFontPart in (Get-ChildItem -LiteralPath (Join-Path $taskRoot 'assets\fonts') -Filter 'DoulosSIL-chunk-*.bin' | Sort-Object Name)) { $taskFontInput = [System.IO.File]::OpenRead($taskFontPart.FullName); try { $taskFontInput.CopyTo($taskFontStream) } finally { $taskFontInput.Dispose() } }
$taskFontStream.Position = 0
$taskWriter = [System.Resources.ResourceWriter]::new($taskFontResources)
try { $taskWriter.AddResource('assets/fonts/doulossil-regular.ttf', $taskFontStream, $false); $taskWriter.Generate() }
finally { $taskWriter.Dispose(); $taskFontStream.Dispose() }
$taskIcon = Join-Path $taskRoot 'assets\suzuha.ico'
if (-not (Test-Path -LiteralPath $taskIcon)) { & (Join-Path $taskRoot 'tools\make-icon.ps1') | Out-Null }
$taskReferences = @('System.dll','System.Core.dll','System.IO.Compression.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$taskArguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/codepage:65001',"/out:$taskOutput\PocketEnglish.exe", "/win32manifest:$taskRoot\app.manifest", "/win32icon:$taskIcon", "/resource:$taskIcon,suzuha.ico", "/resource:$taskRoot\assets\suzuha.png,suzuha.png", "/resource:$taskRoot\MainWindow.xaml,MainWindow.xaml", "/resource:$taskRoot\vocabulary.tsv,vocabulary.tsv")
foreach ($taskReference in $taskReferences) { $taskArguments += '/reference:' + (Join-Path $taskFramework $taskReference) }
$taskSpeech = Get-ChildItem -LiteralPath (Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\System.Speech') -Filter 'System.Speech.dll' -Recurse | Select-Object -First 1
if (-not $taskSpeech) { throw 'The Windows System.Speech assembly was not found.' }
$taskArguments += '/reference:' + $taskSpeech.FullName
$taskArguments += Join-Path $taskRoot 'App.cs'
$taskArguments += Join-Path $taskRoot 'SelectableText.cs'
$taskArguments += Join-Path $taskRoot 'Dictionary.cs'
$taskArguments += Join-Path $taskRoot 'Phonetics.cs'
$taskArguments += Join-Path $taskRoot 'UiTypography.cs'
$taskArguments += "/resource:$taskFontResources,PocketEnglish.g.resources"
$taskArguments += "/resource:$taskDictionary,dictionary.bin"
$taskArguments += "/resource:$taskRoot\licenses\Wordset-WordNet.txt,Wordset-license.txt"
$taskArguments += "/resource:$taskRoot\licenses\CMUdict.txt,CMUdict-license.txt"
$taskArguments += "/resource:$taskRoot\licenses\DoulosSIL-OFL.txt,DoulosSIL-license.txt"
$taskArguments += "/resource:$taskRoot\licenses\IPA-dict.txt,IPA-dict-license.txt"
& $taskCompiler $taskArguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output (Join-Path $taskOutput 'PocketEnglish.exe')
