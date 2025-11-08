using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var serviceDefaults = builder.AddProject("servicedefaults", "../TheImitationGame.ServiceDefaults/TheImitationGame.ServiceDefaults.csproj");

// Add the API project
var api = builder.AddProject("api", "../TheImitationGame.Api/TheImitationGame.Api.csproj")
    .WithReference(serviceDefaults);

// Add the React/npm app (adjust path if your react app directory differs)
var react = builder.AddNpmApp("react", "../TheImitationGame.Web")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT", port: 6969)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
