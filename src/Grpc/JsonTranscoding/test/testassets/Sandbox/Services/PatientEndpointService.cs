using static Server.Startup;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Sandbox.Services.Metadata;

namespace Sandbox.Services
{
    public static class PatientEndpointService
    {
        public static void MapPatientEndpoints(this IEndpointRouteBuilder app)
        {

            var patients = new List<Patient>
                            {
                                new Patient(1, "John Doe", "New York", 30),
                                new Patient(2, "Jane Smith", "Los Angeles", 25),
                                new Patient(3, "Sam Brown", "Chicago", 40)
                            };
            //Standard API endpoint

            app.MapGet("/patients", () => patients)
                .WithMetadata(new PathItemTypeAttribute(PathItemType.Standard));

            app.MapGet("/patients/{id}", (int id) =>
            {
                var patient = patients.FirstOrDefault(p => p.id == id);
                return patient is not null ? Results.Ok(patient) : Results.NotFound();
            }).WithMetadata(new PathItemTypeAttribute(PathItemType.Standard));

            app.MapPost("/patients", (Patient patient) =>
            {
                patients.Add(patient);
                return Results.Created($"/patients/{patient.id}", patient);
            }).WithMetadata(new PathItemTypeAttribute(PathItemType.Standard));

            //Webhook endpoint

            app.MapPost("/webhook/patientcreated", (Patient patient) =>
            {
                // Process the webhook payload
                Console.WriteLine($"Webhook received for patient: {patient.Name}");
                return Results.Ok();
            }).WithMetadata(new PathItemTypeAttribute(PathItemType.Webhook));

            app.MapGet("/appDocument.json", (IEnumerable<EndpointDataSource> source) =>
            {
                var json = CustomOpenApiDocumentGenerator.Generate(source);
                return Results.Text(json, "application/json");
            });

        }

        public record Patient(int id, string Name, string Location, int age);
        
    }

      static class CustomOpenApiDocumentGenerator
    {
        public static string Generate(IEnumerable<EndpointDataSource> endpointSources)
        {
            // 1. Build the OpenApiDocument
            var document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = "MinimalAPI | v1",
                    Version = "1.0.0"
                },
                Paths = new OpenApiPaths()
            };

            // 2. Collect endpoints into groups (Standard/Webhook) and populate Paths + Extensions
            var documentGroups = new Dictionary<PathItemType, OpenApiPaths>
            {
                [PathItemType.Standard] = new OpenApiPaths(),
                [PathItemType.Webhook] = new OpenApiPaths()
            };

            foreach (var source in endpointSources)
            {
                foreach (var endpoint in source.Endpoints)
                {
                    var routePattern = (endpoint as RouteEndpoint)?.RoutePattern?.RawText;
                    if (string.IsNullOrWhiteSpace(routePattern)) continue;
                    if (routePattern.Equals("/appDocument.json", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var method = endpoint.Metadata.OfType<HttpMethodMetadata>()
                                 .FirstOrDefault()?.HttpMethods.FirstOrDefault()?.ToUpper() ?? "GET";

                    var type = endpoint.Metadata.OfType<PathItemTypeAttribute>().FirstOrDefault()?.Type
                               ?? PathItemType.Standard;

                    var paths = documentGroups[(PathItemType)type!];

                    if (!paths.TryGetValue(routePattern, out var pathItem))
                    {
                        pathItem = new OpenApiPathItem();
                        paths[routePattern] = pathItem;
                    }

                    var opType = OperationTypeFromString(method);

                    if (!pathItem.Operations.ContainsKey(opType))
                    {
                        pathItem.Operations[opType] = new OpenApiOperation
                        {
                            Summary = endpoint.DisplayName,
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse { Description = "OK" }
                            }
                        };
                    }
                }
            }

            // Merge Standard paths into document.Paths
            foreach (var kv in documentGroups[PathItemType.Standard])
                document.Paths[kv.Key] = kv.Value;

            // Add Webhooks as an extension
            if (documentGroups[PathItemType.Webhook].Any())
            {
                var webhookObj = new OpenApiObject();
                foreach (var kv in documentGroups[PathItemType.Webhook])
                {
                    var pathObj = new OpenApiObject();
                    foreach (var op in kv.Value.Operations)
                    {
                        var opObj = new OpenApiObject
                        {
                            ["summary"] = new OpenApiString(op.Value.Summary ?? ""),
                            ["responses"] = new OpenApiObject
                            {
                                ["200"] = new OpenApiObject
                                {
                                    ["description"] = new OpenApiString("OK")
                                }
                            }
                        };
                        pathObj[op.Key.ToString().ToLower()] = opObj;
                    }
                    webhookObj[kv.Key] = pathObj;
                }
                document.Extensions["webhooks"] = webhookObj;
            }

            // 3. Serialize OpenApiDocument to JSON string
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            var openApiWriter = new OpenApiJsonWriter(writer);
            document.SerializeAsV3(openApiWriter);
            writer.Flush();
            stream.Position = 0;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static OperationType OperationTypeFromString(string method) => method.ToUpper() switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            "PUT" => OperationType.Put,
            "PATCH" => OperationType.Patch,
            "DELETE" => OperationType.Delete,
            _ => OperationType.Get
        };
    }
}
