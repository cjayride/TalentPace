# Applies the +5 talent caps to the client and server Talent Pace configs.
# Exact full-line replacement (no regex), so numeric collisions are impossible.
$map = [ordered]@{
  '## Settings file was created by plugin Talent Pace v1.2.7' = '## Settings file was created by plugin Talent Pace v1.2.8'
  '# Default value: 20'  = '# Default value: 25'
  '# Default value: 32'  = '# Default value: 37'
  '# Default value: 48'  = '# Default value: 53'
  '# Default value: 58'  = '# Default value: 63'
  '# Default value: 72'  = '# Default value: 77'
  '# Default value: 88'  = '# Default value: 93'
  '# Default value: 105' = '# Default value: 110'
  '# Default value: 120' = '# Default value: 125'
  'Before any boss = 20'   = 'Before any boss = 25'
  'After Eikthyr = 32'     = 'After Eikthyr = 37'
  'After the Elder = 48'   = 'After the Elder = 53'
  'After Bonemass = 58'    = 'After Bonemass = 63'
  'After Moder = 72'       = 'After Moder = 77'
  'After Yagluth = 88'     = 'After Yagluth = 93'
  'After the Queen = 105'  = 'After the Queen = 110'
  'After the Fader = 120'  = 'After the Fader = 125'
}

$files = @(
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft\config\cjayride.talentpace.cfg',
  'D:\AI-Agent-Projects\cjaycraft\beta_cjaycraft-server\config\cjayride.talentpace.cfg'
)

foreach ($f in $files) {
  Copy-Item -LiteralPath $f -Value ($f + '.bak-127') -Force
  $lines = Get-Content -LiteralPath $f -Encoding UTF8
  $hits = 0
  $out = foreach ($line in $lines) {
    if ($map.Contains($line)) { $script:hits++; $map[$line] } else { $line }
  }
  [IO.File]::WriteAllLines($f, [string[]]$out, (New-Object Text.UTF8Encoding($false)))
  Write-Output ("{0}  replaced={1} (expected 17)" -f (Split-Path $f -Parent), $hits)
}
