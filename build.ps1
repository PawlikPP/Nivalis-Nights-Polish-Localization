param([string]$OutputDir = "$PSScriptRoot\build")
$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$packageArchive = Join-Path $repoRoot 'Nivalis_Nights_PL_payload.zip'
$compilerCandidates = @(
  "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
  "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $compiler) { throw 'Nie znaleziono csc.exe dla .NET Framework 4.x. Zainstaluj .NET Framework Developer Pack.' }
foreach ($path in @(
  $packageArchive,
  (Join-Path $repoRoot 'src\Installer.cs'),
  (Join-Path $repoRoot 'assets\ikona_spolszczenia.png')
)) { if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Brak wymaganego pliku: $path" } }
$OutputDir = [IO.Path]::GetFullPath($OutputDir)
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$icon = Join-Path $OutputDir 'ikona_spolszczenia.ico'
$exe = Join-Path $OutputDir 'Spolszczenie_Nivalis_Nights.exe'
& (Join-Path $repoRoot 'build_icon.ps1') -Destination $icon
& $compiler /nologo /optimize+ /target:winexe `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
  /r:System.Web.Extensions.dll `
  "/win32icon:$icon" "/resource:$packageArchive,NivalisPayload.zip" `
  "/out:$exe" (Join-Path $repoRoot 'src\Installer.cs')
if ($LASTEXITCODE -ne 0) { throw "Kompilacja nie powiodla sie (kod $LASTEXITCODE)." }
Get-Item -LiteralPath $exe | Select-Object FullName,Length
