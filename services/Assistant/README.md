# Assistant Service

The AI shopping assistant backend: an MCP *client* (not a server, unlike `services/Mcp`) that connects to the [MCP server](../Mcp/README.md), lets Claude decide which commerce tool to call for a given message, executes it, and feeds the result back — a real agentic tool-use loop, not a scripted one.

## Architecture

```
Angular chat widget ──► Gateway ──► Assistant.Api
                                        │
                          ┌─────────────┴─────────────┐
                          ▼                            ▼
                 Anthropic Messages API      MCP client → Mcp.Server → Gateway → Catalog/Cart/Order
```

`AssistantService` is the loop:

1. Append the user's message to that conversation's history (in-memory, per `conversationId`).
2. Ask the MCP server for its current tool list, convert each to Anthropic's `tools` schema.
3. Send the conversation + tools to Claude.
4. If Claude's response contains `tool_use` blocks, execute each via the MCP client, append the results as a `tool_result` message, and go back to step 3.
5. Once Claude responds with no more tool calls, return the text.
6. Bail out after 5 iterations with a fallback message if it never converges.

## The bearer-token handling, precisely

`get-order-status` and `add-to-cart` (see [Mcp's README](../Mcp/README.md)) declare a `bearerToken` parameter in their MCP schema. Claude never sees it: `AssistantService` strips `bearerToken` out of the JSON schema before it's sent to Anthropic, and injects the caller's real JWT (extracted from the `Authorization` header on `POST /api/chat`) into the tool arguments only when actually invoking one of those two tools. The model reasons about *which* tool to call and *what* order ID or product ID to use — never about the token.

## Verified in this session

Ran `Mcp.Server` and `Assistant.Api` together locally and drove real requests with a manually-minted JWT (signed with the same dev key `Identity` uses — no live SQL Server was needed to get a valid token):

- No token → `401`
- Blank message → `400` with a clean error body
- Valid token + real message → passed auth, `AssistantService` successfully listed tools from the live MCP server, built the Anthropic request, and **reached the real Anthropic API** — which responded with a well-formed `authentication_error` ("x-api-key header is required"), confirming the request shape is correct. It failed only because no real `ANTHROPIC_API_KEY` was available in this sandbox. The endpoint returns a clean `502` in that case, not a stack trace (an actual bug this testing caught and fixed — see `Program.cs`'s `/api/chat` handler).

What's *not* verified: an actual Claude reply, i.e. a real end-to-end tool-use conversation. That needs a real `ANTHROPIC_API_KEY`.

## Configuration

| Setting | Purpose |
|---------|---------|
| `Jwt:Key`/`Issuer`/`Audience` | Validates JWTs from Identity (doesn't issue its own) |
| `Mcp:BaseUrl` | Where the MCP server is |
| `Anthropic:ApiKey` | **Required** — blank by default, must be supplied via env var/secret, never committed |
| `Anthropic:Model` | Defaults to `claude-sonnet-5` |

## Running locally (without Docker)

Requires the MCP server (and everything behind it) running, and a real `ANTHROPIC_API_KEY`.

```bash
dotnet restore
Anthropic__ApiKey=sk-ant-... dotnet run --project Assistant.Api
```

## Running via Docker Compose

```bash
export ANTHROPIC_API_KEY=sk-ant-...
docker-compose up --build
```

`docker-compose.yml` requires `ANTHROPIC_API_KEY` to be set — compose fails fast with a clear message if it isn't, rather than starting an assistant service that can't do anything.

## Tests

```bash
dotnet test
```

Covers `AssistantService`'s loop with `IAnthropicClient` and `IMcpToolClient` mocked: text-only replies skip tool calls entirely, a tool-use round-trip executes the right tool with the right arguments, `bearerToken` is both stripped from what Claude sees *and* injected when actually calling an auth-required tool, and the 5-iteration cap is enforced when the model never stops calling tools.
