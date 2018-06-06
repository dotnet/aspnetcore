# Swaggatherer (Swagger + Gatherer)

This is a cli tool that can generate a routing benchmark using a Swagger 2.0 spec as an input. 

## Usage

Generate a benchmark from a swagger file:
```
dotnet run -- -i swagger.json -o MyGeneratedBenchark.generated.cs
```

Generate a benchmark from a directory of swagger files:
```
dotnet run -- -d /some/directory -o MyGeneratedBenchark.generated.cs
```

The directory mode will recursively search for `.json` files.

## Resources

A big repository of swagger docs: https://github.com/APIs-guru/openapi-directory
Swagger editor + yaml <-> json conversion tool: https://editor2.swagger.io
Azure's official swagger docs: https://github.com/Azure/azure-rest-api-specs