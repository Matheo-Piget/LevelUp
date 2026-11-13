# Build Play Store : App Bundle (.aab) signé.
# Les mots de passe sont demandés à l'écran et ne sont écrits nulle part.
#
#   .\tools\build-aab.ps1 -Keystore C:\cles\levelup.keystore -Alias levelup
#
# L'éditeur Unity doit être fermé. Résultat : LevelUp\Build\Android\LevelUp.aab
param(
    [Parameter(Mandatory = $true)][string]$Keystore,
    [Parameter(Mandatory = $true)][string]$Alias,
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
)

function Read-Secret([string]$prompt) {
    $secure = Read-Host -Prompt $prompt -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

if (-not (Test-Path $Keystore)) { Write-Error "Clé introuvable : $Keystore"; exit 1 }

$project = Join-Path (Split-Path $PSScriptRoot -Parent) "LevelUp"
$log = Join-Path $env:TEMP "levelup-aab.log"

$env:LEVELUP_KEYSTORE = (Resolve-Path $Keystore).Path
$env:LEVELUP_KEY_ALIAS = $Alias
$env:LEVELUP_KEYSTORE_PASS = Read-Secret "Mot de passe du fichier de clé"
$env:LEVELUP_KEY_PASS = Read-Secret "Mot de passe de la clé (Entrée = le même)"

try {
    $p = Start-Process -FilePath $Unity -Wait -PassThru -ArgumentList @(
        "-batchmode", "-quit", "-projectPath", "`"$project`"",
        "-executeMethod", "LevelUp.EditorTools.AndroidBuilder.BuildAab", "-logFile", "`"$log`"")
    Select-String -Path $log -Pattern "\[AndroidBuilder\]" | ForEach-Object { $_.Line }
    if ($p.ExitCode -ne 0) { Write-Error "Build échoué (journal : $log)"; exit 1 }
}
finally {
    Remove-Item Env:LEVELUP_KEYSTORE_PASS, Env:LEVELUP_KEY_PASS, Env:LEVELUP_KEYSTORE, Env:LEVELUP_KEY_ALIAS -ErrorAction SilentlyContinue
}
