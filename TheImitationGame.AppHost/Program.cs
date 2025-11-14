using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the API project
var api = builder.AddProject("api", "../TheImitationGame.Api/TheImitationGame.Api.csproj");

// Add the React app
var react = builder.AddNpmApp("react", "../TheImitationGame.Web")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT", port: 6969)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
