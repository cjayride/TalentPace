$files = @(
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg',
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft-server\config\cjayride.talentpace.cfg',
  'C:\Users\cjrob\AppData\Roaming\com.kesomannen.gale\valheim\profiles\cjayride-beta_test_cjaycraft_ultimate_modpack-4.4.44\BepInEx\config\cjayride.talentpace.cfg'
)

$section = @'

[Description text]

## Comma-separated font preference for the talent window description text; the first installed one is used. 'Valheim-AveriaSansLibre' = the Talent Tree window's own font (renders like the window title). Set 'List installed fonts' to true once, start the game, and the BepInEx log prints every Windows font name this machine offers (then set this back to false). Empty = built-in default list (Verdana, Segoe UI, Arial, Tahoma).
# Setting type: String
# Default value: Valheim-AveriaSansLibre
Font names = Valheim-AveriaSansLibre

## Normal, Bold, Italic, or BoldAndItalic for the description text.
# Setting type: FontStyle
# Default value: Normal
Font style = Normal

## Description text size in points. 0 = automatic (60% of the window title, 15-20).
# Setting type: Int32
# Default value: 0
# Acceptable value range: From 0 to 40
Font size = 0

## One-time helper: when true, the next game start logs every font name available on this machine to the BepInEx log, so you can fill the Font names setting. Set back to false afterwards.
# Setting type: Boolean
# Default value: False
List installed fonts = False
'@

$enc = [Text.UTF8Encoding]::new($false)
foreach ($f in $files) {
  if (-not (Test-Path $f)) { Write-Output ('MISSING: ' + $f); continue }
  $t = [IO.File]::ReadAllText($f)
  if ($t.Contains('[Description text]')) { Write-Output ('SKIP (section exists): ' + $f); continue }
  Copy-Item $f ($f + '.bak-nofontcfg') -Force
  $t = $t.Replace('plugin Talent Pace v1.2.8', 'plugin Talent Pace v1.3.1')
  $t = $t.TrimEnd() + "`r`n" + $section.TrimStart() + "`r`n"
  [IO.File]::WriteAllText($f, $t, $enc)
  $check = Select-String -Path $f -Pattern 'Font names =' | Select-Object -ExpandProperty Line
  Write-Output ('APPENDED: ' + $f + '  |  ' + $check)
}
