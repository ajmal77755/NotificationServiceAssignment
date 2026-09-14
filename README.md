# Notification Forwarder

Receives notifications over HTTP, stores them in SQL Server, and forwards those with level `warning` or higher to Discord as a message written by an LLM. At most 10 messages are sent per rolling minute.

## How it works

```
POST /notifications  ->  validate level  ->  save row (Pending / NotRequired)  ->  202 Accepted
Worker (every 2 s)   ->  oldest Pending  ->  wait for a send slot (10 per 60 s)  ->  LLM writes message  ->  Discord webhook  ->  mark Sent
```

The database row is the queue (transactional outbox). Sends are marked `Sent` only after Discord accepts, so nothing is lost on a crash; a failed send is retried on the next poll and marked `Failed` after `Forwarding:MaxAttempts`. If the LLM fails, a plain template message is sent instead.

## Request

Any JSON object containing a `level` field. Everything else is stored verbatim and passed to the LLM.

```json
{ "level": "warning", "service": "billing-api", "metric": "p95_latency_ms", "value": 4800 }
```

Accepted levels (case-insensitive): `debug`, `info`, `warning`/`warn`, `error`/`err`, `critical`/`crit`/`fatal`. Maximum payload 16 KB. Response: `202 { "id": "...", "status": "Pending" | "NotRequired" }`; `400` for a missing or unknown level or a non-object body.

## Configuration

| Key | Purpose | Secret |
|---|---|---|
| `ConnectionStrings:NotificationsDb` | SQL Server connection string | yes |
| `Discord:WebhookUrl` | Discord webhook | yes |
| `Llm:ApiKey`, `Llm:Model` | LLM provider key and model id | yes |
| `OutboundRateLimit:PermitLimit`, `:Window` | 10 per `00:01:00` | no |
| `Forwarding:MaxAttempts`, `:PollInterval` | retries per notification, poll cadence | no |

Secrets go in *Manage User Secrets* (development) or environment variables such as `Discord__WebhookUrl` (production). `appsettings.json` holds placeholders only.

## Run locally

1. Requires .NET 10 SDK and SQL Server LocalDB (ships with Visual Studio). `appsettings.Development.json` already points at `(localdb)\MSSQLLocalDB`.
2. Set the three secrets in *Manage User Secrets* on `Notifications.Api`.
3. Apply the schema: `dotnet ef database update --project Notifications.Infrastructure --startup-project Notifications.Api` (the app also migrates automatically in Development).
4. `dotnet run --project Notifications.Api`, then open `/swagger` or post to `/notifications`. Health at `/health`.

## Tests

`dotnet test` runs tests. Unit tests cover the level rule, the outbox state machine, the rate-limit window (fake clock), the LLM adapter and the fallback. Integration tests boot the real API against a throwaway LocalDB database with WireMock standing in for Discord and a fake LLM, and prove that 12 warnings produce exactly 10 sends before the window slides. LocalDB must be running: `sqllocaldb start MSSQLLocalDB`.

## Structure

```
Notifications.Domain          entities and rules, no dependencies
Notifications.Application     use cases and ports (interfaces)
Notifications.Infrastructure  EF Core / SQL Server, rate limiter, Discord, LLM, worker
Notifications.Api             HTTP endpoint and DI composition
tests/                        one test project per layer plus integration tests
```

## Known constraints

- Single instance: the rate limiter reads `SentAt` from the database and is correct for one running worker. Scaling out needs a shared lock.
- At-least-once delivery: a crash between Discord accepting and the row being saved can send one duplicate.
- Stored payloads may contain personal data supplied by senders; the raw payload is never posted to Discord, and the LLM is instructed not to repeat identifiers. No retention purge yet.
- Not yet implemented: inbound API rate limiting, authentication, `GET /notifications/{id}`.

ID-ware Confidential
