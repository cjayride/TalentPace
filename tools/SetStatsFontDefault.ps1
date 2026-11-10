$files = @(
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg',
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft-server\config\cjayride.talentpace.cfg',
  'C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\BepInEx\config\cjayride.talentpace.cfg'
)
$enc = [Text.UTF8Encoding]::new($false)
foreach ($f in $files) {
  if (-not (Test-Path $f)) { Write-Output ('MISSING: ' + $f); continue }
  if (-not (Test-Path ($f + '.bak-statsfont'))) { Copy-Item $f ($f + '.bak-statsfont') -Force }
  $t = [IO.File]::ReadAllText($f)
  # 1) font value + default comment: any current name -> Stats
  $lines = [IO.File]::ReadAllLines($f)
  $inDesc = $false
  for ($i = 0; $i -lt $lines.Count; $i++) {
    $l = $lines[$i]
    if ($l -eq '[Description text]') { $inDesc = $true }
    elseif ($l.StartsWith('[') -and $l.EndsWith(']')) { $inDesc = $false }
    if (-not $inDesc) {
      if ($l.StartsWith('## Settings file was created by plugin Talent Pace v')) { $lines[$i] = '## Settings file was created by plugin Talent Pace v1.3.5' }
      continue
    }
    if ($l -match '^## Comma-separated font preference') {
      $lines[$i] = "## Font preference for the talent window description text. 'Stats' = the SAME font the Talent Tree Stats window uses (game's AveriaSerifBold; default). 'Valheim-AlveriaSansLibre' = the Talent Tree window title's own font. Any Windows name (Arial, Verdana, ...) = that installed font, first match in the list wins. Set 'List installed fonts' to true once, start the game, and the BepInEx log prints every Windows font name this machine offers (then set this back to false)."
    }
    if ($l -eq '# Default value: Arial' -or $l -eq '# Default value: Valheim-AlveriaSansLibre' -or $l -eq '# Default value: Verdana, Segoe UI, Arial, Tahoma') { $lines[$i] = '# Default value: Stats' }
    if ($l -match '^Font names = ') { $lines[$i] = 'Font names = Stats' }
  }
  [IO.File]::WriteAllLines($f, $lines, $enc)
  Select-String -Path $f -Pattern '^Font names =|^Font size =|# Default value: Stats|created by plugin' | ForEach-Object { Write-Output ((Split-Path (Split-Path $f -Parent) -Leaf) + '  ' + $_.Line) }
}
$a = 'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg'
$b = 'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft-server\config\cjayride.talentpace.cfg'
Write-Output ('packs byte-identical: ' + ((Get-FileHash $a).Hash -eq (Get-FileHash $b).Hash))
