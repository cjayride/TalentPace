param(
    [string]$Version = '1.2.2'
)

$proj = Split-Path $PSScriptRoot -Parent
$pkg = Join-Path $proj 'Package'
$dist = Join-Path $proj 'dist'
$dll = Join-Path $proj 'bin\Release\net481\Cjayride.TalentPace.dll'

if (-not (Test-Path $dll)) {
    throw "Build Release first: $dll"
}

if (-not (Test-Path $dist)) {
    New-Item -ItemType Directory -Path $dist | Out-Null
}

$zipPath = Join-Path $dist "cjayride-TalentPace-$Version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)

function Add-ZipEntry($archive, $entryName, $sourcePath) {
    [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $sourcePath, $entryName)
}

# Thunderstore: flat root only — no BepInEx/plugins paths in the archive.
Add-ZipEntry $zip 'Cjayride.TalentPace.dll' $dll
foreach ($name in @('manifest.json', 'README.md', 'CHANGELOG.md', 'icon.png', 'LICENSE')) {
    Add-ZipEntry $zip $name (Join-Path $pkg $name)
}

$zip.Dispose()
Write-Output (Resolve-Path $zipPath).Path
