# Installs Talent Pace 1.3.13 into both Gale 4.4.56 profiles once Valheim exits.
# Never kills anything. Backs up the current DLL first, then verifies SHA.
$ErrorActionPreference = 'Stop'
$dll = 'D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll'
$profilesRoot = 'C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles'
$profiles = @(
  'cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.56',
  'cjayride-beta_test_cjaycraft_ultimate_modpack_dedicated_server-4.4.56'
)
$log = 'D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\dist\install-1.3.13.log'
$deadline = (Get-Date).AddHours(4)

function Say($m) {
  $line = (Get-Date).ToString('HH:mm:ss') + '  ' + $m
  Add-Content -LiteralPath $log -Value $line
  Write-Output $line
}

New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
Set-Content -LiteralPath $log -Value 'waiting for Valheim to exit...'
while (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue) {
  if ((Get-Date) -gt $deadline) {
    Say 'TIMEOUT: Valheim still running after 4 hours; nothing installed.'
    exit 1
  }
  Start-Sleep -Seconds 5
}
Say 'Valheim exited. Installing 1.3.13.'
Start-Sleep -Seconds 3

if (-not (Test-Path -LiteralPath $dll)) {
  Say ('MISSING build dll: ' + $dll)
  exit 1
}

$srcSha = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
$srcVer = (Get-Item -LiteralPath $dll).VersionInfo.FileVersion
Say ('source ver=' + $srcVer + ' sha=' + $srcSha)

foreach ($name in $profiles) {
  $dstDir = Join-Path $profilesRoot (Join-Path $name 'BepInEx\plugins\cjayride-TalentPace')
  $dst = Join-Path $dstDir 'Cjayride.TalentPace.dll'
  if (-not (Test-Path -LiteralPath $dstDir)) {
    Say ('SKIP missing plugin folder: ' + $dstDir)
    continue
  }
  $bak = Join-Path $dstDir 'Cjayride.TalentPace.dll.bak-1.3.12'
  if ((Test-Path -LiteralPath $dst) -and -not (Test-Path -LiteralPath $bak)) {
    Copy-Item -LiteralPath $dst -Destination $bak -Force
    Say ('backed up old dll -> ' + $bak)
  }
  Copy-Item -LiteralPath $dll -Destination $dst -Force
  $dstSha = (Get-FileHash -LiteralPath $dst -Algorithm SHA256).Hash
  $dstVer = (Get-Item -LiteralPath $dst).VersionInfo.FileVersion
  Say ('installed ' + $name + ' ver=' + $dstVer + ' sha=' + $dstSha + ' match=' + ($dstSha -eq $srcSha))
}

Say 'DONE'
