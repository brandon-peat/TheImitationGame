using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var serviceDefaults = builder.AddProject("servicedefaults", "../TheImitationGame.ServiceDefaults/TheImitationGame.ServiceDefaults.csproj");

// Add the API project
var api = builder.AddProject("api", "../TheImitationGame.Api/TheImitationGame.Api.csproj")
    .WithReference(serviceDefaults);

await builder.Build().RunAsync();
