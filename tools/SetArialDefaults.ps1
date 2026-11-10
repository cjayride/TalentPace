$files = @(
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg',
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft-server\config\cjayride.talentpace.cfg',
  'C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\BepInEx\config\cjayride.talentpace.cfg'
)
$enc = [Text.UTF8Encoding]::new($false)
foreach ($f in $files) {
  if (-not (Test-Path $f)) { Write-Output ('MISSING: ' + $f); continue }
  if (-not (Test-Path ($f + '.bak-arial'))) { Copy-Item $f ($f + '.bak-arial') -Force }
  $lines = [IO.File]::ReadAllLines($f)
  for ($i = 0; $i -lt $lines.Count; $i++) {
    $l = $lines[$i]
    if ($l -eq 'Font names = Valheim-AlveriaSansLibre' -or $l -eq 'Font names = Valheim-AveriaSansLibre') { $lines[$i] = 'Font names = Arial' }
    if ($l -eq '# Default value: Valheim-AlveriaSansLibre' -or $l -eq '# Default value: Valheim-AveriaSansLibre') { $lines[$i] = '# Default value: Arial' }
    if ($l -eq 'Font size = 0') { $lines[$i] = 'Font size = 21' }
    if ($l -eq '# Default value: 0' -and $i + 1 -lt $lines.Count -and $lines[$i + 1] -eq '# Acceptable value range: From 0 to 40') { $lines[$i] = '# Default value: 21' }
    if ($l.StartsWith('## Settings file was created by plugin Talent Pace v')) { $lines[$i] = '## Settings file was created by plugin Talent Pace v1.3.2' }
  }
  [IO.File]::WriteAllLines($f, $lines, $enc)
  Select-String -Path $f -Pattern '^Font names =|^Font size =|# Default value: Arial' | ForEach-Object { Write-Output ((Split-Path $f -Leaf) + '  ' + $_.Line) }
}
