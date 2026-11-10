# Installs Talent Pace 1.2.8 into the Gale client profile once Valheim exits.
# Never kills anything. Backs up the current DLL/config first, then verifies.
$ErrorActionPreference = 'Stop'
$dll  = 'D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll'
$pack = 'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg'
$prof = 'C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44'
$dst  = Join-Path $prof 'BepInEx\plugins\cjayride-TalentPace'
$log  = 'D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\dist\install-1.2.8.log'
$deadline = (Get-Date).AddHours(4)

function Say($m) { $line = (Get-Date).ToString('HH:mm:ss') + '  ' + $m; Add-Content -LiteralPath $log -Value $line; Write-Output $line }

Set-Content -LiteralPath $log -Value 'waiting for Valheim to exit...'
while (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue) {
  if ((Get-Date) -gt $deadline) { Say 'TIMEOUT: Valheim still running after 4 hours; nothing installed.'; exit 1 }
  Start-Sleep -Seconds 5
}
Say 'Valheim exited. Installing.'
Start-Sleep -Seconds 3

$d = Join-Path $dst 'Cjayride.TalentPace.dll'
$bak = Join-Path $dst 'Cjayride.TalentPace.dll.bak-1.2.7'
if (-not (Test-Path $bak)) { Copy-Item -LiteralPath $d -Destination $bak -Force; Say ('backed up old dll -> ' + $bak) }
Copy-Item -LiteralPath $dll -Destination $d -Force
Say ('installed dll ver=' + (Get-Item $d).VersionInfo.FileVersion + ' sha=' + (Get-FileHash $d).Hash)

$cfgDst = Join-Path $prof 'BepInEx\config\cjayride.talentpace.cfg'
if (Test-Path $cfgDst) {
  $cfgBak = $cfgDst + '.bak-127caps'
  if (-not (Test-Path $cfgBak)) { Copy-Item -LiteralPath $cfgDst -Destination $cfgBak -Force; Say ('backed up profile cfg -> ' + $cfgBak) }
  Copy-Item -LiteralPath $pack -Destination $cfgDst -Force
  Say 'profile cfg replaced with pack cfg (new +5 caps)'
}

Get-ChildItem $dst | ForEach-Object { Say ('  ' + $_.Name + '  ' + $_.Length) }
Say 'DONE'
