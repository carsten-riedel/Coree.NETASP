param (
    [string]$PAT,
    [Alias("NUGET_PAT")]
    [string]$SECRET_NUGET_APIKEY,
    [Alias("NUGET_TEST_PAT")]
    [string]$SECRET_INTTESTNUGET_APIKEY
)

$ErrorActionPreference = 'Stop'

$version    = $PSVersionTable.PSVersion.ToString()
$datetime   = Get-Date -f 'yyyyMMdd_HHmmss'
$filename   = "cicd-${version}-${datetime}.log"
$Transcript = Join-Path -Path "$PSScriptRoot" -ChildPath $filename
Start-Transcript -Path "$Transcript"

$NUGET_PAT = $SECRET_NUGET_APIKEY
$NUGET_TEST_PAT = $SECRET_INTTESTNUGET_APIKEY

. "$PSScriptRoot/cicd_util.ps1"
. "$PSScriptRoot/cicd_prebuild_enviroment_prepare.ps1"
. "$PSScriptRoot/cicd_prebuild_enviroment_check.ps1"
. "$PSScriptRoot/cicd_prebuild_envars_prepare.ps1"
. "$PSScriptRoot/cicd_prebuild_envars_check.ps1"
. "$PSScriptRoot/cicd_build_clean.ps1"
. "$PSScriptRoot/cicd_build_config.ps1"
. "$PSScriptRoot/cicd_build.ps1"
. "$PSScriptRoot/cicd_deploy.ps1"
. "$PSScriptRoot/cicd_postdeploy_clean.ps1"
. "$PSScriptRoot/cicd_postdeploy_run.ps1"

#git status --porcelain $sourceCodeFolder

Stop-Transcript
