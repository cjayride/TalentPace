$dll = 'D:\AI-Agent-Projects\cjaycraft\_tmp_scan\dlls\TalentTree.dll'
$s = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($dll))
foreach ($needle in @('Points per level', 'Points Per Level', 'PointsPerLevel', 'Base points', 'Points per boss')) {
    $i = 0
    while (($i = $s.IndexOf($needle, $i)) -ge 0) {
        $start = [Math]::Max(0, $i - 10)
        Write-Output $s.Substring($start, [Math]::Min(120, $s.Length - $start)).Replace([char]0, '|')
        $i++
        if ($i -gt 5000000) { break }
    }
}
