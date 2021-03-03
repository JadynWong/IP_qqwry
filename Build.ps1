param(
    [Parameter(Mandatory = $true)][string]$version = $(throw "Parameter missing: -version Version"),
    [string]$nugetKey
)

$rootFolder = (Get-Item -Path "./" -Verbose).FullName
$outputFolder = (Join-Path $rootFolder "artifacts")
if (Test-Path $outputFolder) { Remove-Item $outputFolder -Force -Recurse }

Write-Output "Version:$version"

dotnet build QQWrySln.sln --configuration Release

dotnet test QQWrySln.sln --configuration Release

 dotnet pack .\QQWry -o $outputFolder -p:Version=$version --configuration Release

 dotnet pack .\QQWry.DependencyInjection -o $outputFolder -p:Version=$version --configuration Release

if ($nugetKey) {
    dotnet nuget push "$outputFolder\*.nupkg" --source https://api.nuget.org/v3/index.json --skip-duplicate --api-key $nugetKey
}
else {
    Write-Output "Skip nuget push"
}

Set-Location $outputFolder

Get-ChildItem

Set-Location $rootFolder