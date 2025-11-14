using TheImitationGame.Api.Hubs;
using TheImitationGame.Api.Interfaces;
using TheImitationGame.Api.Services;
using TheImitationGame.Api.Stores;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var allowLocalhost5173 = "_allowLocalhost5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: allowLocalhost5173,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

builder.Services.AddSingleton<IGamesStore, InMemoryGamesStore>();
builder.Services.AddSingleton<IImitationGenerator, ImitationGenerator>();
builder.Services.AddSingleton<DefaultPromptGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(allowLocalhost5173);

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapHub<GameHub>("/game-hub");

app.Run();