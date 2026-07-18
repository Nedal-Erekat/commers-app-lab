# Design decisions — why this, not the obvious alternative

Interview-prep reference. Each entry: the decision, the alternative that was considered, and why
this one won — grounded in what's actually in the repo, not idealized reasoning after the fact.

---

## Architecture

**Why microservices instead of a modular monolith?**
The whole point of this project (vs. its sibling [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab),
which *is* a monolith) was to get hands-on with real service boundaries — independent deploys,
independent failure domains, independent scaling. A modular monolith gets you compile-time-enforced
boundaries with none of the operational complexity, and for a catalog this size it's arguably the more
defensible production choice. Honest answer: this is a deliberate learning-lab trade-off, not a claim
that microservices were "correct" for this domain's actual scale.

**Why Clean Architecture (Domain → Application → Infrastructure → Api) per service instead of a
simpler 2-layer split?**
The dependency direction is the thing worth demonstrating: `Domain` has zero references, `Application`
depends only on `Domain`, `Infrastructure` implements `Application`'s interfaces. That means swapping
SQL Server for something else, or Redis for an in-memory cache, touches `Infrastructure` only — proven
concretely by `CachedProductRepository` slotting in as a decorator with no change to `ProductService`
or the controller.

**Why a Gateway instead of letting the Angular apps call each service's own port?**
Two reasons: (1) the frontend needs exactly one CORS origin and one base URL to know about, not five;
(2) it's what makes the MCP server legitimate — `Mcp.Server` calls the *same* Gateway contract the
Angular apps use, so "the AI assistant" isn't a special backdoor with its own privileged access path,
it's just another client.

**Why YARP instead of Ocelot / nginx / a service mesh?**
YARP is a first-class .NET library (config-driven `ReverseProxy` section, no separate binary/process),
which kept the whole stack in one language and one deploy artifact. A service mesh (Istio/Linkerd
sidecars) is the "real" answer at higher scale, but it's infrastructure complexity this project's AKS
footprint (one small node pool) doesn't justify.

---

## Data

**Why one SQL Server instance with per-service databases instead of one shared database?**
Separate *databases* (`CatalogDb`, `IdentityDb`, `CartDb`, `OrderDb`) enforce the real constraint that
matters — no service can join across another's tables, so the only way to get another service's data
is its API. Sharing one SQL Server *container* is a cost/footprint shortcut for local dev; in the
Bicep IaC each becomes a genuinely separate Azure SQL Basic-tier database, so the boundary is real,
only the local topology is consolidated.

**Why copy `ProductName`/`UnitPrice` into `CartItem`/`OrderItem` instead of storing just `ProductId`
and looking it up live?**
This isn't denormalization for performance, it's *correctness*. An order line has to freeze the price
the customer actually paid — if Catalog's price changes next week, `OrderItem.UnitPrice` must not
silently change with it. The snapshot is fetched live at add-to-cart/checkout time
(`ProductCatalogClient`), then owned by Cart/Order forever after.

**Why cache-aside instead of write-through, or ASP.NET's built-in output caching?**
Cache-aside puts the invalidation decision in application code, which is what let the milestone-8
admin CRUD explicitly clear `product_{id}` on update/delete rather than relying on a framework-level
TTL guess. Output caching would've been less code, but it caches HTTP responses, not domain objects —
doesn't compose with the "here's a `Product`, cached or not" abstraction `IProductRepository` gives
the rest of the app.

**Why does the paged-list cache *not* get invalidated on writes, only the single-item cache?**
Honest trade-off, not an oversight: invalidating every page a product could appear on means tracking
page membership per product, real complexity for marginal benefit against a 5-minute TTL. Accepted
staleness window, documented as such.

**Why `Guid` IDs for Cart/Order but plain `int` for Product?**
Carts and orders are created directly by their own service with no coordination need, so a
client-generated GUID avoids a round-trip. Products are only ever written by Catalog itself — single
writer, no distributed-uniqueness problem — so a plain SQL Server auto-increment `int` is simpler and
there's nothing a GUID buys.

**Why `Status` as a bare string instead of a C# enum?**
It crosses a service boundary as JSON, out to the Angular admin app on the status-update `PATCH` and
back. A string sidesteps any enum-serialization/versioning mismatch between what the API and the SPA
each think the underlying values are; validity is enforced with a `ValidStatuses` array in
`OrderService`, not the type system.

---

## Security / Identity

**Why one shared HS256 signing key across services instead of each service validating tokens against
Identity directly (introspection), or full OIDC?**
HS256 with a shared secret means every downstream service validates a JWT with zero network call back
to Identity — no coupling, no latency, no single point of failure at request time. The cost is key
rotation requires updating every service's config simultaneously; acceptable for a lab, not for a
system where services don't trust each other's ops teams equally.

**Why roles embedded as JWT claims instead of a central authorization/policy service?**
`[Authorize(Roles = "Admin")]` reading a claim already on the token means zero extra round-trips per
request. A central PDP (policy decision point) is the right answer once authorization logic gets
complex enough to need central governance — here it's one role, one product line, not worth the
indirection yet.

---

## AI / MCP

**Why build an actual MCP server instead of giving Claude a list of function definitions directly in
the Assistant service?**
MCP's value is that the tool surface is a *protocol*, reusable by any MCP client — not just this one
assistant. `Mcp.Server` doesn't know or care that Claude is the caller; the same four tools would work
from Claude Desktop, another agent, anything speaking Streamable HTTP. Hardcoding function-calling
directly into `Assistant.Api` would've been fewer moving parts, but it's a one-off integration, not a
demonstrable MCP skill.

**Why strip the bearer token from the schema shown to Claude instead of passing it as a normal tool
argument?**
"The model can see a credential" and "the model can use a credential" are different risk surfaces —
prompt injection from a malicious product description or order note could try to get the model to
exfiltrate or misuse a token it can see, even if it's not supposed to. Stripping it from the schema
means the model can request "call add-to-cart with productId=5, quantity=2" but the actual token
substitution happens in `Assistant.Api`, code the model never influences.

---

## Testing / Observability

**Why OpenTelemetry instead of just structured `ILogger` logging?**
Logging tells you *that* something happened per-service; tracing tells you the causal chain *across*
services for one request — the Gateway→Catalog hop sharing a TraceId is the thing a log line alone
can't show. Metrics (request duration histograms, runtime counters) answer "is this healthy," which
neither logs nor traces answer efficiently at scale.

**Why gate the trace console-exporter behind a flag but leave metrics always-on?**
They have different, measured costs: metrics batch-export every 60s regardless of request volume
(cheap), tracing would console-write synchronously *per request*. Proven empirically, not assumed:
~16ms average latency with tracing off, `p(95)` spiking to ~299ms with it on, same load — see
[TESTING.md](TESTING.md).

**Why k6 instead of JMeter/Locust?**
Scripts are plain JavaScript, no separate GUI/XML config, and it's the tool dotnet-scale-lab already
established — reusing a known quantity rather than introducing a second load-testing tool for the same
kind of test.

---

## Infra

**Why Bicep instead of Terraform?**
Native to Azure, no state file to manage/store, first-party `az cli` integration for validate/what-if.
Terraform is the more portable choice if multi-cloud were ever a real requirement — it isn't here.

**Why AKS instead of Azure App Service, given the target stack names both?**
AKS is the closer match to "real microservices" — it's where you'd actually run eight independently
scaled, independently deployed containers with their own health probes, vs. App Service's
one-container-per-plan model. The trade-off is real operational overhead (node pool, ACR, K8s
manifests) that App Service would avoid entirely; AKS was chosen specifically because managing that
overhead is itself the thing worth having on record.

**Why plain Kubernetes manifests instead of Helm?**
Nine services' worth of YAML is still small enough to read in full and diff in a PR — Helm's templating
earns its complexity once you're managing many environments/variants of the same manifests, which this
project doesn't have (one environment, one config).
