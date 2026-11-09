# Geometry check for CapHint placement — ported 1:1 from CapHint.cs (1.2.7, legacy Text).
# Coordinates are host-local Unity UI space: x grows right, y grows UP.
# With anchor (0,1) the anchor point is (rect.xMin, rect.yMax); the element's
# top-left in host-local space = (bounds.xMin + anchoredPos.x, bounds.yMax + anchoredPos.y).
# 1.2.7: the block top = the counter's BOTTOM edge (starts below the number), minus Gap.

$sideMargin = 10.0
$gap = 8.0
$buttonH = 34.0
$fails = 0

function Test-Case([string]$name, [double]$pX, [double]$pY, [double]$pW, [double]$pH,
                   [double]$cX, [double]$cYBottom, [double]$cW, [double]$prefH) {
    $rows = [Math]::Max(1, [Math]::Ceiling($prefH / 24.0))   # ~24px per line of legacy text
    $xMin = $pX; $yMin = $pY; $xMax = $pX + $pW; $yMax = $pY + $pH

    $tlX = $cX; $tlY = $cYBottom          # counter bottom-left in host-local space

    $x = [Math]::Max($xMin + $sideMargin, $tlX)
    $top = $tlY - $gap                    # starts BELOW the counter
    $w = [Math]::Min([Math]::Max(420.0, $cW * 1.15), [Math]::Max(420.0, $pW - 2 * $sideMargin))
    $buttonRoom = $buttonH + 8                                   # needsFree = true (worst case)
    $available = $top - $yMin - $sideMargin - $buttonRoom
    $rowSum = 24.0 * $rows
    $h = [Math]::Ceiling($rowSum) + 4
    if ($available -gt 30 -and $h -gt $available) { $h = $available }
    $h = [Math]::Max(24.0, $h)
    $block = $h + $buttonRoom
    $minTop = $yMin + $sideMargin + $block
    $clamped = $top -lt $minTop
    if ($top -lt $minTop) { $top = $minTop }

    # what the code actually writes:
    $anchX = $x - $xMin
    $anchY = $top - $yMax
    $labelTopLocalY = $yMax + $anchY          # == $top
    $labelBottom = $labelTopLocalY - $h
    $labelRight = $xMin + $anchX + $w

    $btnW = [Math]::Min(320.0, [Math]::Max(160.0, ($xMax - $xMin) - 2 * $sideMargin))
    $yForBtn = $top - $h - 8
    $by = [Math]::Min($yForBtn, $yMax - $sideMargin - $buttonH)
    $bx = [Math]::Min($x, $xMax - $btnW - $sideMargin)
    $btnTopLocal = $yMax + ($by - $yMax)      # == $by
    $btnBottom = $btnTopLocal - $buttonH
    $btnRight = $bx + $btnW

    $checks = [ordered]@{
        'label-top<=panel-top'   = $(if ($clamped) { $true } else { $labelTopLocalY -le $yMax + 0.001 })
        'label-top<=counter-bot' = $(if ($clamped) { $true } else { $labelTopLocalY -le $cYBottom + 0.001 })
        'label-left>=panel+M'    = ($xMin + $anchX) -ge ($xMin + $sideMargin) - 0.001
        'label-right<=panel'     = $labelRight -le $xMax + 0.001
        'label-bottom>=panel'    = $labelBottom -ge $yMin - 0.001
        'btn-top<=label-bottom'  = $btnTopLocal -le $labelBottom - 8 + 0.001
        'btn-bottom>=panel'      = $btnBottom -ge $yMin - 0.001
        'btn-right<=panel'       = $btnRight -le $xMax + 0.001
        'anchoredY<=0 (below top)' = $(if ($clamped) { $true } else { $anchY -le 0.001 })
        'anchoredX>=0 (right of left)' = $anchX -ge 0
    }

    $bad = @($checks.Keys | Where-Object { -not $checks[$_] })
    $pass = $bad.Count -eq 0
    Write-Output ("{0,-42} {1}  panel={2}x{3} counterBotY={4:N0} label(x={5:N0},top={6:N0},h={7:N0},w={8:N0}) btn(top={9:N0},bottom={10:N0})" -f `
        $name, $(if($pass){'PASS'}else{'FAIL -> ' + ($bad -join ', ')}), $pW, $pH, $cYBottom, ($xMin+$anchX), $labelTopLocalY, $h, $w, $btnTopLocal, $btnBottom)
    if (-not $pass) { $script:fails++ }
}

Test-Case '1800x800 typical, counter top-left'  -900 -400 1800 800 -860 330 160 140
Test-Case '900x600 narrow'                      -450 -300  900 600 -420 230 120 170
Test-Case '500x400 tiny panel (clamp stress)'   -250 -200  500 400 -230 140 120 400
Test-Case 'counter near bottom'                 -900 -400 1800 800 -860 -360 160 90
Test-Case 'ultrawide 3200x1000'                 -1600 -500 3200 1000 -1560 430 180 150
Test-Case 'long 7-line text on 900x600'         -450 -300  900 600 -420 220 120 210
Test-Case 'counter flush panel left edge'       -900 -400 1800 800 -900 330 900 100

Write-Output ("FAILURES: " + $fails)
