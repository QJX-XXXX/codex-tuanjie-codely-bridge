[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$CodelyCliPath,
    [switch]$Force
)

$modulePath = Join-Path $PSScriptRoot 'CodexTuanjieConfig.psm1'
Import-Module $modulePath -Force
Set-TuanjieCodexConfig -ProjectPath $ProjectPath -CodelyCliPath $CodelyCliPath -Force:$Force
