# Convert SVG to PNG using headless msedge or chrome, or Inkscape as fallback.
# Usage: PowerShell -NoProfile -ExecutionPolicy Bypass -File .\scripts\convert_svg_to_png.ps1

$svg = Resolve-Path -Path "docs/architecture/architecture-diagram.svg"
$png = Resolve-Path -Path "docs/architecture/architecture-diagram.png" -ErrorAction SilentlyContinue
if ($null -eq $png) { $png = Join-Path $PWD.Path "docs/architecture/architecture-diagram.png" }

Write-Host "SVG: $svg"
Write-Host "PNG: $png"

function Try-Command {
    param($name)
    try {
        $cmd = Get-Command $name -ErrorAction Stop
        return $cmd.Source
    } catch { return $null }
}

$msedge = Try-Command msedge
$chrome = Try-Command chrome
$inkscape = Try-Command inkscape

if ($msedge) {
    Write-Host "Using msedge at: $msedge"
    & $msedge --headless --disable-gpu --screenshot="$png" --window-size=1200,760 "file:///$($svg.Path)"
    if (Test-Path $png) { Write-Host "Created: $png"; exit 0 }
}

if ($chrome) {
    Write-Host "Using chrome at: $chrome"
    & $chrome --headless --disable-gpu --screenshot="$png" --window-size=1200,760 "file:///$($svg.Path)"
    if (Test-Path $png) { Write-Host "Created: $png"; exit 0 }
}

if ($inkscape) {
    Write-Host "Using Inkscape at: $inkscape"
    & $inkscape "$($svg.Path)" --export-type=png --export-filename="$png"
    if (Test-Path $png) { Write-Host "Created: $png"; exit 0 }
}

Write-Error "No suitable headless browser or Inkscape found in PATH. Install msedge/chrome or Inkscape and re-run this script."; exit 1
