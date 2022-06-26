msbuild .\Analyzers\Analyzers.Vsix\Analyzers.Vsix.csproj /p:Configuration=Release /t:Clean
msbuild .\Analyzers\Analyzers.Vsix\Analyzers.Vsix.csproj /p:Configuration=Release
Push-Location .\Analyzers\Analyzers.Vsix\bin\Release
Start-Process .
Pop-Location