using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.AspNetCore.DataProtection.Azure.Blob;

namespace AzureBlob
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference("key-container");

            // The container must exist before calling the DataProtection APIs.
            // The specific file within the container does not have to exist,
            // as it will be created on-demand.

            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            // Configure

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddDataProtection()
                .PersistKeysToAzureBlobStorage(container, "keys.xml");

            var services = serviceCollection.BuildServiceProvider();
            var loggerFactory = services.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(Microsoft.Extensions.Logging.LogLevel.Trace);

            // Run a sample payload

            var protector = services.GetDataProtector("sample-purpose");
            var protectedData = protector.Protect("Hello world!");
            Console.WriteLine(protectedData);
        }
    }
}
