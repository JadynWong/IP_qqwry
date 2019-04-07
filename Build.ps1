if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

dotnet restore 

$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];

$revision = [convert]::ToInt32($revision, 10)

echo $revision 

#dotnet test .\QQWryTest -c Release

dotnet build QQWrySln.sln

dotnet pack .\QQWry -o .\artifacts -p:Version=1.0.$revision --version-suffix=$revision

dotnet pack .\QQWry.DependencyInjection -o .\artifacts -p:Version=1.0.$revision --version-suffix=$revision

Set-Location .\artifacts

Get-ChildItem