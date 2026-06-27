param(
    [string]$ServerRoot = "C:\Program Files (x86)\Steam\steamapps\common\Eco Server"
)

$source = Resolve-Path (Join-Path $PSScriptRoot "..")
$target = Join-Path $ServerRoot "Mods\UserCode\EcoHousingAdvisor"

New-Item -ItemType Directory -Force -Path $target | Out-Null
robocopy $source $target /E /XD .git bin obj Tests /XF *.user | Out-Null
if ($LASTEXITCODE -gt 7) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}

$tests = Join-Path $target "Tests"
if (Test-Path $tests) {
    Remove-Item -LiteralPath $tests -Recurse -Force
}

Write-Host "Eco Housing Advisor installed to $target"
