using TheImitationGame.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

var allowAnyOrigin = "_allowAnyOrigin";

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: allowAnyOrigin,
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

// Add SignalR services
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(allowAnyOrigin);

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<GameHub>("/game-hub");

app.Run();