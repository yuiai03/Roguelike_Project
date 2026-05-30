param(
    [Parameter(Mandatory = $true)]
    [string]$InputDocx,

    [Parameter(Mandatory = $true)]
    [string]$OutputDocx
)

$ErrorActionPreference = "Stop"

function Remove-Diacritics {
    param([string]$Text)
    if ($null -eq $Text) { return "" }
    $normalized = $Text.Normalize([Text.NormalizationForm]::FormD)
    $builder = New-Object System.Text.StringBuilder
    foreach ($char in $normalized.ToCharArray()) {
        $category = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($char)
        if ($category -ne [Globalization.UnicodeCategory]::NonSpacingMark) {
            [void]$builder.Append($char)
        }
    }
    return $builder.ToString().Normalize([Text.NormalizationForm]::FormC)
}

function To-AsciiUpper {
    param([string]$Text)
    return (Remove-Diacritics $Text).ToUpperInvariant()
}

function Ensure-Style {
    param(
        [Parameter(Mandatory = $true)] $Document,
        [Parameter(Mandatory = $true)] [string]$StyleName
    )

    try {
        return $Document.Styles.Item($StyleName)
    }
    catch {
        $wdStyleTypeParagraph = 1
        $style = $Document.Styles.Add($StyleName, $wdStyleTypeParagraph)
        $style.BaseStyle = "Caption"
        $style.Font.Name = "Times New Roman"
        $style.Font.Size = 12
        $style.Font.Color = 0
        $style.ParagraphFormat.Alignment = 1
        $style.ParagraphFormat.SpaceAfter = 6
        return $style
    }
}

function Find-ParagraphByAscii {
    param(
        [Parameter(Mandatory = $true)] $Document,
        [Parameter(Mandatory = $true)] [string]$Needle
    )

    foreach ($paragraph in $Document.Paragraphs) {
        $content = $paragraph.Range.Text.Trim("`r", "`n", " ")
        if ((To-AsciiUpper $content) -eq $Needle) {
            return $paragraph
        }
    }
    return $null
}

function Normalize-FigureCaption {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][int]$ChapterNumber,
        [Parameter(Mandatory = $true)][int]$FigureIndex
    )

    $trimmed = $Text.Trim()
    $prefix = ($trimmed -split '\s+', 2)[0]
    if ([string]::IsNullOrWhiteSpace($prefix)) { $prefix = "Hinh" }

    $body = $trimmed -replace '^\S+\s+\d+(?:[-\.]\d+)+\s*:\s*', ''
    if ($body -eq $trimmed) {
        $body = $trimmed -replace '^\S+\s+\d+(?:[-\.]\d+)*\s*:\s*', ''
    }
    if ([string]::IsNullOrWhiteSpace($body)) { $body = "Minh hoa" }

    return "$prefix $ChapterNumber.$FigureIndex`: $body"
}

$word = $null
$document = $null

try {
    Copy-Item -LiteralPath $InputDocx -Destination $OutputDocx -Force

    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0

    $document = $word.Documents.Open($OutputDocx)
    $null = Ensure-Style -Document $document -StyleName "FigureCaption"

    $lofHeading = Find-ParagraphByAscii -Document $document -Needle "DANH MUC HINH VE"
    $chapter1Heading = $null
    foreach ($paragraph in $document.Paragraphs) {
        $asciiContent = To-AsciiUpper ($paragraph.Range.Text.Trim("`r", "`n", " "))
        if ($asciiContent -like "CHUONG 1*") {
            $chapter1Heading = $paragraph
            break
        }
    }

    if ($null -eq $lofHeading) { throw "Khong tim thay DANH MUC HINH VE." }
    if ($null -eq $chapter1Heading) { throw "Khong tim thay CHUONG 1." }

    $wdActiveEndAdjustedPageNumber = 1
    $chapterNumber = 0
    $figureIndexByChapter = @{}
    $insideFigureCatalog = $false
    $figureEntries = New-Object System.Collections.Generic.List[object]

    foreach ($paragraph in $document.Paragraphs) {
        $content = $paragraph.Range.Text.Trim("`r", "`n", " ")
        if ([string]::IsNullOrWhiteSpace($content)) { continue }

        $asciiContent = To-AsciiUpper $content

        if ($asciiContent -eq "DANH MUC HINH VE") {
            $insideFigureCatalog = $true
            continue
        }
        if ($insideFigureCatalog -and $asciiContent -like "CHUONG 1*") {
            $insideFigureCatalog = $false
        }
        if ($insideFigureCatalog) { continue }

        if ($asciiContent -match '^CHUONG\s+(\d+)') {
            $chapterNumber = [int]$Matches[1]
            if (-not $figureIndexByChapter.ContainsKey($chapterNumber)) {
                $figureIndexByChapter[$chapterNumber] = 0
            }
            continue
        }

        if ($asciiContent -match '^HINH\s+\d') {
            if ($chapterNumber -le 0) { continue }

            $figureIndexByChapter[$chapterNumber]++
            $normalized = Normalize-FigureCaption -Text $content -ChapterNumber $chapterNumber -FigureIndex $figureIndexByChapter[$chapterNumber]
            $paragraph.Range.Text = $normalized
            $paragraph.Range.Style = "FigureCaption"
            $pageNumber = $paragraph.Range.Information($wdActiveEndAdjustedPageNumber)

            $entry = [PSCustomObject]@{
                Caption = $normalized
                Page    = $pageNumber
            }
            [void]$figureEntries.Add($entry)
        }
    }

    $catalogRange = $document.Range($lofHeading.Range.End, $chapter1Heading.Range.Start)
    $catalogText = ""
    foreach ($entry in $figureEntries) {
        $catalogText += "`r$($entry.Caption)`t$($entry.Page)"
    }
    $catalogText += "`r"
    $catalogRange.Text = $catalogText

    $document.Save()
    $document.Close()
    $word.Quit()

    Write-Output "output=$OutputDocx"
    Write-Output "figures=$($figureEntries.Count)"
}
finally {
    if ($document -ne $null) {
        try { $document.Close($false) } catch {}
    }
    if ($word -ne $null) {
        try { $word.Quit() } catch {}
    }
}
