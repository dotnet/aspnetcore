Compile the solution in Release mode (so Kestrel is available in release)
```
build.cmd /p:Configuration=Release
```
To run a specific benchmark add it as parameter
```
dotnet run -c Release RequestParsing
```
To run all use `All` as parameter
```
dotnet run -c Release All
```
Using no parameter will list all available benchmarks
