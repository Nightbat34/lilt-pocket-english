param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'release'))
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskFramework = Split-Path $taskCompiler
$taskOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
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
$taskArguments += "/resource:$taskRoot\assets\dictionary.bin,dictionary.bin"
$taskArguments += "/resource:$taskRoot\licenses\Wordset-WordNet.txt,Wordset-license.txt"
$taskArguments += "/resource:$taskRoot\licenses\CMUdict.txt,CMUdict-license.txt"
& $taskCompiler $taskArguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output (Join-Path $taskOutput 'PocketEnglish.exe')
