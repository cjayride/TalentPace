# Installs Talent Pace 1.3.7 into the 4.4.46 Gale client AND dedicated-server profiles
# once Valheim exits. Never kills anything. Backs up current DLLs first, then verifies.
# No config touches. Retries copies for files still mapped by Gale after game exit.
$ErrorActionPreference = 'Stop'
$dll  = 'D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\bin\Release\net481\Cjayride.TalentPace.dll'
$base = 'C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles'
$targets = @(
  (Join-Path $base 'cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.46\BepInEx\plugins\cjayride-TalentPace'),
  (Join-Path $base 'cjayride-beta_test_cjaycraft_ultimate_modpack_dedicated_server-4.4.46\BepInEx\plugins\cjayride-TalentPace')
)
$log  = 'D:\AI-Agent-Projects\cjaycraft\Cjayride.TalentPace\dist\install-1.3.7.log'
$deadline = (Get-Date).AddHours(6)

function Say($m) { $line = (Get-Date).ToString('HH:mm:ss') + '  ' + $m; Add-Content -LiteralPath $log -Value $line; Write-Output $line }

Set-Content -LiteralPath $log -Value 'waiting for Valheim to exit...'
while (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue) {
  if ((Get-Date) -gt $deadline) { Say 'TIMEOUT: Valheim still running after 6 hours; nothing installed.'; exit 1 }
  Start-Sleep -Seconds 5
}
Say 'Valheim exited. Installing.'
Start-Sleep -Seconds 3

foreach ($dst in $targets) {
  $d = Join-Path $dst 'Cjayride.TalentPace.dll'
  $bak = $d + '.bak-1.3.6'
  if (-not (Test-Path $bak)) {
    try { Copy-Item -LiteralPath $d -Destination $bak -Force; Say ('backed up -> ' + $bak) }
    catch { Say ('backup skipped (' + $dst + '): ' + $_.Exception.Message) }
  }
  $tries = 0
  while ($true) {
    try { Copy-Item -LiteralPath $dll -Destination $d -Force; break }
    catch {
      $tries++
      if ($tries -gt 60) { Say ('FAILED after 5 minutes: ' + $dst + ' - ' + $_.Exception.Message); break }
      Start-Sleep -Seconds 5
    }
  }
  if ((Get-Item $d).VersionInfo.FileVersion -eq '1.3.7.0') {
    Say ('installed ' + (Split-Path (Split-Path (Split-Path $dst) -Parent) -Parent) + ' ver=1.3.7.0 sha=' + (Get-FileHash $d).Hash.Substring(0,16))
  }
}
Say 'DONE'

