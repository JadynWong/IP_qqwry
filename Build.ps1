$rootFolder = (Get-Item -Path "./" -Verbose).FullName
$outputFolder = (Join-Path $rootFolder "artifacts")
if(Test-Path $outputFolder) { Remove-Item $outputFolder -Force -Recurse }

dotnet restore 

$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$NULL -ne $env:APPVEYOR_BUILD_NUMBER];

$revision = [convert]::ToInt32($revision, 10)

Write-Output $revision 

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "-1"}[$NULL -ne $env:APPVEYOR_BUILD_VERSION];

if($version -eq "-1"){
    throw "can't read version"
}

Write-Output $version 

dotnet test .\QQWryTest -c Release

dotnet build QQWrySln.sln

dotnet pack .\QQWry -o $outputFolder -p:Version=$version --version-suffix=$revision

dotnet pack .\QQWry.DependencyInjection -o $outputFolder -p:Version=$version --version-suffix=$revision

Set-Location $outputFolder

Get-ChildItem

Set-Location $rootFolder