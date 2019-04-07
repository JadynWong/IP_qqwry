function EnsurePsbuildInstalled{  
    [cmdletbinding()]
    param(
        [string]$psbuildInstallUri = 'https://raw.githubusercontent.com/ligershark/psbuild/master/src/GetPSBuild.ps1'
    )
    process{
        if(-not (Get-Command "Invoke-MsBuild" -errorAction SilentlyContinue)){
            'Installing psbuild from [{0}]' -f $psbuildInstallUri | Write-Verbose
            (new-object Net.WebClient).DownloadString($psbuildInstallUri) | iex
        }
        else{
            'psbuild already loaded, skipping download' | Write-Verbose
        }

        # make sure it's loaded and throw if not
        if(-not (Get-Command "Invoke-MsBuild" -errorAction SilentlyContinue)){
            throw ('Unable to install/load psbuild from [{0}]' -f $psbuildInstallUri)
        }
    }
}

function Exec  
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ("Exec: " + $errorMessage)
    }
}

if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

EnsurePsbuildInstalled

exec { & dotnet restore }

Invoke-MSBuild

$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$revision = [convert]::ToInt32($revision, 10)

exec { & dotnet test .\QQWryTest -c Release }

exec { & dotnet pack .\QQWry -o .\artifacts --version-suffix=$revision }

exec { & dotnet pack .\QQWry.DependencyInjection -o .\artifacts --version-suffix=$revision }