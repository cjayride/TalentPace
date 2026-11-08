$dll = 'D:\AI-Agent-Projects\cjaycraft\_tmp_scan\dlls\TalentTree.dll'
$asm = [Reflection.Assembly]::LoadFrom($dll)
$asm.GetTypes() | ForEach-Object {
    $t = $_
    $t.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly') |
        Where-Object { $_.Name -match 'Respec|Reset|Clear|Refund|Free' } |
        ForEach-Object { "$($t.FullName)::$($_.Name)" }
}
