param(
    [Parameter(Mandatory = $true)][string]$Workspace,
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$k7Workspace = [System.IO.Path]::GetFullPath($Workspace)
$k7Scripts = Join-Path $k7Workspace 'work\scripts'
$k7Output = Join-Path $k7Workspace 'outputs\K7_Industrial_Robot'
foreach ($folder in @('work\scripts','outputs\K7_Industrial_Robot\Blender','outputs\K7_Industrial_Robot\FBX','outputs\K7_Industrial_Robot\Textures\4K','outputs\K7_Industrial_Robot\Textures\2K','outputs\K7_Industrial_Robot\Unity\Editor','outputs\K7_Industrial_Robot\Unity\Runtime','outputs\K7_Industrial_Robot\Previews','outputs\K7_Industrial_Robot\Source','outputs\K7_Industrial_Robot\Validation','outputs\K7_Industrial_Robot\Reference')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $k7Workspace $folder) | Out-Null
}
Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.py' | Copy-Item -Destination $k7Scripts
$k7Package = Split-Path -Parent $PSScriptRoot
Copy-Item -LiteralPath (Join-Path $k7Package 'Unity\Editor\K7AssetSetup.cs') -Destination (Join-Path $k7Output 'Unity\Editor')
Copy-Item -LiteralPath (Join-Path $k7Package 'Unity\Runtime\K7Robot.cs') -Destination (Join-Path $k7Output 'Unity\Runtime')
Copy-Item -LiteralPath (Join-Path $k7Package 'Reference\K7_Reference.jpg') -Destination (Join-Path $k7Output 'Reference')
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $k7Output 'Source\rebuild.ps1')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $k7Output 'Source\README.md')
function Invoke-K7Blender([string]$Script) {
    & $Blender -b --factory-startup --python-exit-code 1 --python (Join-Path $k7Scripts $Script)
    if ($LASTEXITCODE -ne 0) { throw "Blender failed: $Script" }
}
function Invoke-K7Python([string]$Script) {
    & $Python (Join-Path $k7Scripts $Script)
    if ($LASTEXITCODE -ne 0) { throw "Python failed: $Script" }
}
Invoke-K7Blender 'build_k7.py'
Invoke-K7Blender 'bake_k7.py'
Invoke-K7Python 'finish_textures.py'
Invoke-K7Blender 'clean_k7.py'
Invoke-K7Python 'patch_caps.py'
Invoke-K7Blender 'finalize_k7.py'
Invoke-K7Blender 'validate_export.py'
Invoke-K7Python 'validate_uv.py'
Invoke-K7Python 'package_k7.py'
Write-Output "Completed: $k7Output"
