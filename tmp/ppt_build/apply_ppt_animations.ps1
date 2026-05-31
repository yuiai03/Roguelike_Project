param(
    [Parameter(Mandatory = $true)]
    [string]$PptxPath
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName Microsoft.Office.Interop.PowerPoint
Add-Type -AssemblyName office

$ppEffectAppear = [int][Microsoft.Office.Interop.PowerPoint.PpEntryEffect]::ppEffectAppear
$ppEffectFade = [int][Microsoft.Office.Interop.PowerPoint.PpEntryEffect]::ppEffectFade
$ppTransitionFade = [int][Microsoft.Office.Interop.PowerPoint.PpEntryEffect]::ppEffectFade
$ppTransitionSpeedMedium = [int][Microsoft.Office.Interop.PowerPoint.PpTransitionSpeed]::ppTransitionSpeedMedium
$ppAdvanceOnClick = [int][Microsoft.Office.Interop.PowerPoint.PpAdvanceMode]::ppAdvanceOnClick
$ppAdvanceOnTime = [int][Microsoft.Office.Interop.PowerPoint.PpAdvanceMode]::ppAdvanceOnTime
$msoTrue = [int][Microsoft.Office.Core.MsoTriState]::msoTrue
$msoFalse = [int][Microsoft.Office.Core.MsoTriState]::msoFalse

function Get-AnimShapes($slide) {
    $items = @()
    for ($i = 1; $i -le $slide.Shapes.Count; $i++) {
        $shape = $slide.Shapes.Item($i)
        $name = [string]$shape.Name
        if ($name -like 'anim-*') {
            $match = [regex]::Match($name, '^anim-(appear|fade)-(\d+)-')
            if ($match.Success) {
                $items += [pscustomobject]@{
                    Shape = $shape
                    Kind = $match.Groups[1].Value
                    Order = [int]$match.Groups[2].Value
                }
            }
        } else {
            try {
                $shape.AnimationSettings.Animate = $msoFalse
            } catch {
            }
        }
    }
    return $items | Sort-Object Order
}

$pp = $null
$presentation = $null

try {
    $presentation = $null
    $pp = New-Object -ComObject PowerPoint.Application
    $presentation = $pp.Presentations.Open($PptxPath)

    for ($slideIndex = 1; $slideIndex -le $presentation.Slides.Count; $slideIndex++) {
        $slide = $presentation.Slides.Item($slideIndex)
        $transition = $slide.SlideShowTransition
        $transition.EntryEffect = $ppTransitionFade
        $transition.Speed = $ppTransitionSpeedMedium
        $transition.AdvanceOnTime = $msoFalse
        $transition.AdvanceOnClick = $msoTrue

        $animShapes = Get-AnimShapes $slide
        $order = 1
        foreach ($item in $animShapes) {
            $shape = $item.Shape
            $anim = $shape.AnimationSettings
            $anim.Animate = $msoTrue
            $anim.EntryEffect = if ($item.Kind -eq 'fade') { $ppEffectFade } else { $ppEffectAppear }
            $anim.AnimationOrder = $order

            if ($order -eq 1) {
                $anim.AdvanceMode = $ppAdvanceOnClick
            } else {
                $anim.AdvanceMode = $ppAdvanceOnTime
                $anim.AdvanceTime = 0.20
            }

            $order++
        }
    }

    $presentation.Save()
}
finally {
    if ($presentation -ne $null) {
        $presentation.Close()
    }
    if ($pp -ne $null) {
        $pp.Quit()
    }
}
