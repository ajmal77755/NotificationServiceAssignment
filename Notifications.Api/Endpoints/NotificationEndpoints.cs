using Notifications.Api.Contracts;
using Notifications.Application.Notifications;
using Notifications.Domain;
using System.Text.Json;

namespace Notifications.Api.Endpoints
{
    public static class NotificationEndpoints
    {
        public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapPost("/notifications", ReceiveAsync)
                .WithName("ReceiveNotification")
                .Produces<ReceiveNotificationResponse>(StatusCodes.Status202Accepted)
                .ProducesValidationProblem();

            return endpointRouteBuilder;
        }

        private static async Task<IResult> ReceiveAsync(
            JsonElement body,
            ReceiveNotificationHandler handler,
            CancellationToken cancellationToken)
        {

            if(body.ValueKind != JsonValueKind.Object)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "Body", new[] { "Request body must be a JSON object." } }
                });
            }

            var raw = body.GetRawText();
            var levelProperty = body.EnumerateObject().FirstOrDefault(p => p.Name.Equals("level", StringComparison.OrdinalIgnoreCase));

            if(levelProperty.Value.ValueKind != JsonValueKind.String ||
                !LevelParser.Parse(levelProperty.Value.GetString(), out var level))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>()
                {
                    ["level"] = ["Level is required"]
                });
            }

            var result = await handler.HandleAsync(new ReceiveNotificationCommand(raw, level), cancellationToken);
            
            return Results.Accepted(value: new ReceiveNotificationResponse(result.Id, result.Status.ToString()));

        }
    }
}
