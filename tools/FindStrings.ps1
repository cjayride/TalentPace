$dll = 'D:\AI-Agent-Projects\cjaycraft\_tmp_scan\dlls\TalentTree.dll'
$s = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($dll))
$need = @('points per boss', 'Points per boss', 'Base points', 'bonus points', 'tt_', 'Config')
foreach ($n in $need) {
    if ($s.Contains($n)) { Write-Output "found: $n" }
}
# numeric defaults near PointsPerBoss in IL - search for readable config keys
$rx = [regex]::Matches($s, 'Talent[A-Za-z]+')
$rx | ForEach-Object { $_.Value } | Sort-Object -Unique | Select-Object -First 40
