param(
    [Parameter(Mandatory = $true)][string]$version = $(throw "Parameter missing: -version Version"),
    [string]$nugetKey
)

$solutionPath = ".\QQWrySln.sln"
$rootFolder = (Get-Item -Path "./" -Verbose).FullName
$outputFolder = (Join-Path $rootFolder "artifacts")
if (Test-Path $outputFolder) { Remove-Item $outputFolder -Force -Recurse }

Write-Output "Version:$version"

function CheckProcess ([string]$action) {
    if (-Not $?) {
        Write-Host ("$action failed")
        Set-Location $rootFolder
        exit $LASTEXITCODE
    }
}

#build
dotnet build $solutionPath --configuration Release

CheckProcess "build"

#test
dotnet test $solutionPath --configuration Release --no-restore

CheckProcess "test"

#pack
dotnet pack .\QQWry -o $outputFolder -p:Version=$version --configuration Release --no-restore

CheckProcess "pack QQWry"

dotnet pack .\QQWry.DependencyInjection -o $outputFolder -p:Version=$version --configuration Release --no-restore

CheckProcess "pack QQWry.DependencyInjection"

#nuget push
if ($nugetKey) {
    dotnet nuget push "$outputFolder\*.nupkg" --source https://api.nuget.org/v3/index.json --skip-duplicate --api-key $nugetKey

    CheckProcess "nuget push"
}
else {
    Write-Output "Skip nuget push"
}

Set-Location $outputFolder

Get-ChildItem

Set-Location $rootFolder