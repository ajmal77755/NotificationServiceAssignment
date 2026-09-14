using Notifications.Application.Dependency;
using Notifications.Application.Forwarding;
using Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Notifications.Api.Endpoints;
using Notifications.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddOptions<ForwardingOptions>()
    .Bind(builder.Configuration.GetSection(ForwardingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Notification service API v1"));
}
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.MapNotificationEndpoints();
app.MapHealthChecks("/health");


app.Run();
public partial class Program { }