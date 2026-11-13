# Lance les tests EditMode de LevelUp en batchmode (éditeur Unity FERMÉ).
# Usage :  powershell -File tools\run-tests.ps1 [-Build]
#   -Build : enchaîne sur le build Windows (LevelUp\Build\Windows\LevelUp.exe)
param([switch]$Build)

$ErrorActionPreference = "Stop"
$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
$project = Join-Path $PSScriptRoot "..\LevelUp" | Resolve-Path
$out = Join-Path $env:TEMP "levelup-tests"
New-Item -ItemType Directory -Force $out | Out-Null
$xml = Join-Path $out "results.xml"
$log = Join-Path $out "tests.log"

if (Get-Process Unity -ErrorAction SilentlyContinue) {
    Write-Host "Ferme l'éditeur Unity avant de lancer les tests." -ForegroundColor Yellow
    exit 2
}

Write-Host "Tests EditMode (environ 5 min)..."
$p = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
    "-batchmode", "-projectPath", "`"$project`"", "-runTests", "-testPlatform", "EditMode",
    "-testResults", "`"$xml`"", "-logFile", "`"$log`"")

$errors = Select-String -Path $log -Pattern "error CS" | Select-Object -Unique -First 20
if ($errors) {
    Write-Host "Erreurs de compilation :" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host $_.Line }
    exit 1
}

[xml]$results = Get-Content $xml
$run = $results.'test-run'
Write-Host ("{0} tests : {1} réussis, {2} en échec" -f $run.total, $run.passed, $run.failed)
$results.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
    Write-Host ("  ECHEC " + $_.fullname) -ForegroundColor Red
}
if ([int]$run.failed -gt 0) { exit 1 }

if ($Build) {
    Write-Host "Build Windows..."
    $b = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
        "-batchmode", "-quit", "-projectPath", "`"$project`"",
        "-executeMethod", "LevelUp.EditorTools.AndroidBuilder.BuildWindows",
        "-logFile", "`"$(Join-Path $out 'build.log')`"")
    if ($b.ExitCode -ne 0) { Write-Host "Build en échec (voir $out\build.log)" -ForegroundColor Red; exit 1 }
    Write-Host "Build prêt : LevelUp\Build\Windows\LevelUp.exe"
}
exit 0
