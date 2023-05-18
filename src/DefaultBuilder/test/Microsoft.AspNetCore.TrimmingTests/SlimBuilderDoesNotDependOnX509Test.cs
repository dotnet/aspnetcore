using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateSlimBuilder(args);
var app = builder.Build();

if (X509Utilities.HasCertificateType) {
    return 1;
}

return 100; // Success