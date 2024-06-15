$ErrorActionPreference = 'Stop'

# Custom function to copy files, excluding specified directories
function Copy-Directory {
    param (
        [string]$sourceDir,
        [string]$destinationDir,
        [string[]]$exclusions
    )

    $sourceDirParam = $sourceDir
    $destinationDirParam = $destinationDir

    # Ensure that $sourceDir and $destinationDir are absolute paths
    if (-not [System.IO.Path]::IsPathRooted($sourceDir)) {
        $sourceDir = Join-Path (Get-Location) $sourceDir
    }

    if (-not [System.IO.Path]::IsPathRooted($destinationDir)) {
        $destinationDir = Join-Path (Get-Location) $destinationDir
    }

    # Ensure paths end with a directory separator for consistent behavior
    $sourceDir = [System.IO.Path]::GetFullPath($sourceDir)
    $destinationDir = [System.IO.Path]::GetFullPath($destinationDir)

    # Get all items in the source directory
    $items = Get-ChildItem -Path $sourceDir -Recurse

    foreach ($item in $items) {
        # Check if the item is in an excluded directory
        $excluded = $false
        foreach ($exclusion in $exclusions) {
            if ($item.FullName -like "*\$exclusion*") {
                $excluded = $true
                break
            }
        }

        if (-not $excluded) {
            $relativePath = [System.IO.Path]::GetRelativePath($sourceDir, $item.FullName)
            $targetPath = Join-Path -Path $destinationDir -ChildPath $relativePath


            $relativeSource = Join-Path -Path $sourceDirParam -ChildPath $relativePath
            $relativeDestination = Join-Path -Path $destinationDirParam -ChildPath $relativePath

            if ($item.PSIsContainer) {
                # Create directory if it doesn't exist
                if (-not (Test-Path -Path $targetPath)) {
                    New-Item -ItemType Directory -Path $targetPath
                }
            } else {
                # Copy file
                Copy-Item -Path $item.FullName -Destination $targetPath -Force
                Write-Output "Copyied: $($relativeSource) --> $($relativeDestination)"
            }
        }
    }
}

function Ensure-CommandAvailability {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )
    process {
        $isAvailible = $false

        try {
            $commandInfo = Get-Command $CommandName -ErrorAction Stop
            $isAvailible = $true
            $output = "Command is available   : {0}" -f $CommandName
            Write-Output $output
        }
        catch {}        
        if ($isAvailible -eq $false)
        {
            $output = "Command is not available : {0}" -f $CommandName
            throw "$output";
        }
    }
}

function Test-CommandAvailability {
    param (
        [string]$CommandName
    )

    try {
        $null = Get-Command $CommandName -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Log-Block {
    param (
        [string]$Stage,
        [string]$Section,
        [string]$Task
    )
    Write-Output "_"
    Write-Output "==============================================================================================================="
    if (-not [string]::IsNullOrEmpty($Stage)) {
        $output =  "Stage: {0} Section: {1} Task: {2} " -f $Stage.PadRight(15), $Section.PadRight(20), $Task.PadRight(35)
        Write-Output $output
    }
    Write-Output "==============================================================================================================="
}

function Ensure-VariableSet {
    param (
        [Parameter(Mandatory = $true)]
        [string]$VariableName,
        
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [AllowNull()]
        [string]$VariableValue
    )
    process {
        if ([string]::IsNullOrEmpty($VariableValue)) {
            $output = "VariableName: {0} is not set." -f $VariableName.PadRight(30)
            Write-Output $output
            throw "$output";
        }
        else {
            Write-Output ("VariableName: {0} is set." -f $VariableName.PadRight(30))
        }
    }
}

function Clear-BinObjDirectories {
    param(
        [string]$sourceDirectory = "source"
    )

    # Define bin and obj folder paths
    $binFolderPath = Join-Path -Path $sourceDirectory -ChildPath "bin"
    $objFolderPath = Join-Path -Path $sourceDirectory -ChildPath "obj"

    # Ensure that $sourceDir and $destinationDir are absolute paths
    if (-not [System.IO.Path]::IsPathRooted($binFolderPath)) {
        $binFolderPath = Join-Path (Get-Location) $binFolderPath
    }

    if (-not [System.IO.Path]::IsPathRooted($objFolderPath)) {
        $objFolderPath = Join-Path (Get-Location) $objFolderPath
    }

    # Function to delete files and directory
    function Delete-DirectoryContents {
        param(
            [System.IO.DirectoryInfo]$directory
        )

        if ($directory.Exists) {
            $files = Get-ChildItem -Path $directory.FullName -Recurse -File
            foreach ($file in $files) {
                try {
                    Remove-Item $file.FullName -Force
                    Write-Output "Deleted file: $($file.FullName)."
                } catch {
                    Write-Output "Could not delete file: $($file.FullName)."
                }
            }

            try {
                Remove-Item $directory.FullName -Recurse -Force
                Write-Output "Deleted directory: $($directory.FullName)."
            } catch {
                Write-Output"Could not delete directory: $($directory.FullName)."
            }
        }
    }

    # Create DirectoryInfo objects
    $binFolder = [System.IO.DirectoryInfo]::new($binFolderPath)
    $objFolder = [System.IO.DirectoryInfo]::new($objFolderPath)

    # Delete contents of bin and obj directories
    Delete-DirectoryContents -directory $binFolder
    Delete-DirectoryContents -directory $objFolder
}

Log-Block -Stage "Prepare" -Section "Commandline" -Task "Check for availability"
Ensure-CommandAvailability -CommandName "dotnet"
Ensure-CommandAvailability -CommandName "git"
Ensure-CommandAvailability -CommandName "curl"

Log-Block -Stage "Prepare" -Section "EnviromentVariables" -Task "Resolve and set"
$gitroot = git rev-parse --show-toplevel 2>&1
Set-Location -Path $gitroot

$gitBranch = git rev-parse --abbrev-ref HEAD
Write-Output ("Name: {0} Value: {1}" -f "`$gitroot".PadRight(20), $gitroot )
Write-Output ("Name: {0} Value: {1}" -f "`$gitBranch".PadRight(20), $gitBranch )
Write-Output ("Name: {0} Value: {1}" -f "GetLocation".PadRight(20), $(Get-Location) )

Log-Block -Stage "Prepare" -Section "Secrets" -Task "Import or commandline"
$SECRETS_PAT = $args[0]
$SECRETS_NUGET_PAT = $args[1]
$SECRETS_NUGET_TEST_PAT = $args[2]

$secretsPath = ".github/workflows/secrets.ps1"

# Check if the secrets file exists before importing
if (Test-Path $secretsPath) {
    . $secretsPath
}

Ensure-VariableSet -VariableName "`$SECRETS_PAT" -VariableValue "$SECRETS_PAT"
Ensure-VariableSet -VariableName "`$SECRETS_NUGET_PAT" -VariableValue "$SECRETS_NUGET_PAT"
Ensure-VariableSet -VariableName "`$SECRETS_NUGET_TEST_PAT" -VariableValue "$SECRETS_NUGET_TEST_PAT"

if (-not (Test-CommandAvailability -CommandName "docfx"))
{
    Log-Block -Stage "Prepare" -Section "Install" -Task "Installing dotnet tool docfx."
    dotnet tool install --global docfx --version 2.74.1
}

Log-Block -Stage "Build" -Section "Clean" -Task "Clean output directorys"

Clear-BinObjDirectories -sourceDirectory "src/Projects/Coree.NETStandard"

Log-Block -Stage "Build" -Section "Restore" -Task "Restoreing nuget packages."
dotnet restore ./src
Log-Block -Stage "Build" -Section "Build" -Task "Building the solution."
dotnet build ./src --no-restore /p:ContinuousIntegrationBuild=true -c Release
Log-Block -Stage "Build" -Section "Pack" -Task "Createing the nuget package."
dotnet pack ./src --no-restore /p:ContinuousIntegrationBuild=true -c Release
Log-Block -Stage "Build" -Section "Docfx" -Task "Generating the docfx files."
docfx src/Projects/Coree.NETStandard/Docfx/build/docfx_local.json

Log-Block -Stage "Copy files" -Section "Docfx" -Task "Copying files from the docfx output to docs/docfx"
Copy-Directory -sourceDir "src/Projects/Coree.NETStandard/Docfx/result/local/" -destinationDir "docs/docfx" -exclusions @('.git', '.github')

Log-Block -Stage "Commit and Push" -Section "Docfx" -Task "Commit and Push docs/docfx"
git config --global user.name 'Updated form Workflow'
git config --global user.email 'carstenriedel@outlook.com'
git add docs/docfx
git commit -m "Updated form Workflow"
git push origin master

Log-Block -Stage "Publish" -Section "Packages" -Task "dotnet nuget push github"
$pattern = "src/Projects/Coree.NETStandard/bin/Pack/Coree.NETStandard.*.nupkg"
$firstFileMatch = Get-ChildItem -Path $pattern -File | Select-Object -First 1
dotnet nuget add source --username carsten-riedel --password $SECRETS_PAT --store-password-in-clear-text --name github "https://nuget.pkg.github.com/carsten-riedel/index.json"
dotnet nuget push "$($firstFileMatch.FullName)" --api-key $SECRETS_PAT --source "github"
Log-Block -Stage "Publish" -Section "Packages" -Task "dotnet nuget push nuget"
dotnet nuget push "$($firstFileMatch.FullName)" --api-key $SECRETS_NUGET_TEST_PAT --source https://apiint.nugettest.org/v3/index.json

Log-Block -Stage "Call" -Section "Dispatch" -Task "dispatching a other job"
curl -X POST -H "Authorization: token $SECRETS_PAT" -H "Accept: application/vnd.github.v3+json" https://api.github.com/repos/carsten-riedel/Coree.NETStandard/dispatches -d '{"event_type": "trigger-other-workflow"}'


Log-Block -Stage "Cleanup" -Section "Packages" -Task "clean old github packages"
$headers = @{
    Authorization = "Bearer $SECRETS_PAT"
}

$GitHubNugetPackagelist = Invoke-RestMethod -Uri "https://api.github.com/users/carsten-riedel/packages/nuget/Coree.NETStandard/versions" -Headers $headers
$GitHubNugetPackagelistOld = $GitHubNugetPackagelist | Sort-Object -Property created_at -Descending | Select-Object -Skip 5
foreach ($item in $GitHubNugetPackagelistOld)
{
    $PackageId = $item.id
    Invoke-RestMethod -Method Delete -Uri "https://api.github.com/users/carsten-riedel/packages/nuget/Coree.NETStandard/versions/$PackageId" -Headers $headers | Out-Null
    Write-Output "Unlisted package Coree.NETStandard $($item.name)"
}

# Log-Block -Stage "Cleanup" -Section "Packages" -Task "clean old nuget.org packages"
# $NugetRegistrationsBaseUrlAPI = (Invoke-RestMethod -Uri 'https://api.nuget.org/v3/index.json' | ForEach-Object { $_.resources } | Where-Object { $_.'@type' -like 'RegistrationsBaseUrl/3.6.0' }).'@Id'
# $NugetPackageList = (Invoke-RestMethod -Uri "$NugetRegistrationsBaseUrlAPI$("Coree.NETStandard".ToLowerInvariant())/index.json").items.items.catalogEntry
# $Listed = $NugetPackageList | Where-Object { $_.'listed' -eq $true }
# $ListedIgnoreNewest = ($Listed[-1..-($Listed.Count)] | Select-Object -Skip 4)
# foreach ($item in $ListedIgnoreNewest)
# {
#    $headers = @{
#        'X-nuget-APIKey' = "$SECRETS_NUGET_PAT"
#    }
#    Invoke-RestMethod -Uri "https://www.nuget.org/api/v2/package/Coree.NETStandard/$($item.version)" -Method Delete -Headers $headers | Out-Null
#    Write-Output "Unlisted package Coree.NETStandard $($item.version)"
# }
