# Testing notes — load testing (k6) and observability (OpenTelemetry)

Milestone 9. Notes on what was built, what was actually run in this sandbox, and what wasn't.

---

## Scope: why this isn't a database benchmark

[dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab) already did the deep k6 work on
raw database/cache throughput (SQL Server indexing, Redis cache-aside, `p(95)` under concurrent load).
This project's k6 tests deliberately don't repeat that — they target the layer that's actually new here:
the **YARP Gateway** sitting in front of every microservice, plus the **OpenTelemetry** distributed
tracing/metrics now wired into all eight services. The question this milestone answers is "does the
reverse-proxy hop and the tracing instrumentation hold up under concurrent load," not "how fast is SQL
Server."

## What actually ran in this sandbox, and why the numbers below aren't from the real Catalog service

This session's Docker daemon could be started, but every image pull failed: MCR/Docker Hub blob
downloads and the official `mssql-server` Ubuntu package both route through hosts this sandbox's
network policy blocks (`production.cloudfront.docker.com`, `pmc-geofence.trafficmanager.net`) — see
the proxy's `recentRelayFailures` for the exact denials. No SQL Server, no Docker Compose stack.

To still get a genuine, live read on the Gateway + OpenTelemetry layer, the k6 runs below hit the real
`Gateway.Api` (built and run from this repo, unmodified) fronting a small throwaway Node stand-in for
Catalog — not part of this repo — that mimics `GET /api/products` and `GET /api/products/search`'s
real response shape with a randomized 5–25ms delay (approximating a DB+Redis round trip). Everything
upstream of that stand-in (YARP routing, CORS, OpenTelemetry instrumentation, the k6 scripts
themselves) is real and unmodified. The scripts in `load-tests/` target Catalog's actual documented
contract and would run unmodified against the real Dockerized stack — they just haven't been, in this
session.

---

## k6 — core concepts

| Concept | What it means |
|---------|--------------|
| Virtual User (VU) | A simulated concurrent user; each VU loops through `export default function` repeatedly |
| Stage | A phase with a target VU count and duration; VU count ramps linearly between stages |
| Threshold | A pass/fail assertion on a metric — the run exits non-zero if breached |
| Check | An inline assertion (e.g. `status === 200`) — doesn't fail the run, just counts |
| `p(95)` | 95th percentile response time — the standard SLA marker; prefer it over `avg`, which hides skew |

Installed via `go install go.k6.io/k6@latest` in this session (k6 v1.8.0) — the project's own Docker/apt
paths were blocked, but `proxy.golang.org` wasn't, so the Go module proxy was the way in.

---

## `load-tests/browse-products.js`

Ramps to 100 VUs hitting `GET /api/products?page=1&pageSize=50` through the Gateway.

**Thresholds:** `p(95) < 500ms`, error rate `< 1%`.

**Actual result (this session, Gateway + stand-in rig):**

```
✓ 'p(95)<500' p(95)=24.93ms
✓ 'rate<0.01' rate=0.00%

http_req_duration: avg=15.79ms min=5.08ms med=15.61ms max=73.58ms p(90)=23.85ms p(95)=24.93ms
http_req_failed:   0.00% (0 out of 5931)
5931 requests over 100s, 0 failures, both thresholds passed.
```

## `load-tests/search-products.js`

Ramps to 50 VUs hitting `GET /api/products/search?q=<random term>` (20 varied terms) through the Gateway.

**Thresholds:** `p(95) < 2000ms` (looser — mirrors dotnet-scale-lab's baseline for a non-cached, full-scan-shaped query), error rate `< 1%`.

**Actual result (this session, Gateway + stand-in rig):**

```
✓ 'p(95)<2000' p(95)=26.56ms
✓ 'rate<0.01' rate=0.00%

http_req_duration{endpoint:search}: avg=17.85ms min=6.6ms med=17.62ms max=133.75ms p(90)=25.62ms p(95)=26.56ms
http_req_failed: 0.00% (0 out of 3957)
3957 requests over 100s, 0 failures, both thresholds passed.
```

Both numbers are dominated by the stand-in's artificial 5–25ms delay, not real SQL Server/Redis
latency — expected, given what's described above. What they do show: the Gateway adds negligible
overhead of its own even at 100 concurrent VUs, and it didn't drop or fail a single request.

---

## Observability — OpenTelemetry

All eight services (`Catalog`, `Identity`, `Cart`, `Order`, `Gateway`, `Mcp.Server`, `Assistant.Api`,
`OrderProcessing.Worker`) now register:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("<service-name>"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation() /* + console exporter if enabled */)
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddConsoleExporter());
```

(`OrderProcessing.Worker` has no ASP.NET Core pipeline, so it registers HttpClient + runtime
instrumentation only.)

**Metrics** always export to console on a periodic interval — cheap, since it's a batch write every
60s, not per-request. **Tracing** only console-exports when `OpenTelemetry:ConsoleExporter=true` is
set, off by default. That's a deliberate trade-off: synchronously writing a console line per span
adds real per-request overhead, and turning it on would quietly skew the very latency numbers a load
test is trying to measure. Flip it on to inspect a handful of requests by hand; leave it off for an
actual load test.

**Verified live in this session** (not just reviewed by inspection): ran `Gateway.Api` standalone
(it has no database dependency, so this needed no Docker) with tracing enabled, then sent both a
single request and a concurrent 5-VU/5s k6 burst through it to the stand-in rig above. Confirmed:

- A real span exports per request, with correct `service.name`, HTTP semantic-convention tags
  (`http.route`, `http.response.status_code`, etc.), and duration.
- Under the concurrent burst, 25 requests produced exactly 25 distinct `TraceId`s — no collisions,
  no cross-request bleed.
- The Gateway's inbound server span and its own outbound `HttpClient` call to the stand-in **share
  the same `TraceId`**, with the client span's `ParentSpanId` correctly set to the server span's
  `SpanId` — genuine parent/child distributed-trace correlation, entirely from the instrumentation
  packages, no custom propagation code written. Example, captured verbatim:

  ```
  Activity.TraceId:      eeeb31a81a94d63cac2e2f5071ac70ee
  Activity.SpanId:       b9deb113ed24dd17
  Activity.Kind:         Server
  Activity.DisplayName:  GET /api/products/{**catch-all}

  Activity.TraceId:      eeeb31a81a94d63cac2e2f5071ac70ee   ← same trace
  Activity.SpanId:       8c1dfd548a032113
  Activity.ParentSpanId: b9deb113ed24dd17                    ← points at the server span above
  Activity.Kind:         Client
  Activity.DisplayName:  GET
  ```

- The overhead claim above is also empirically real, not just theoretical: the same 5-VU burst that
  ran at ~16ms average with tracing off ran with `p(95)` spiking to ~299ms with console tracing on —
  confirming synchronous per-span console export is expensive enough to change your load-test
  numbers, which is exactly why it's off by default.

**Not verified live:** the DB-backed services (Catalog/Identity/Cart/Order) emitting spans that
include real EF Core/SQL Server activity, and cross-service traces spanning more than one real .NET
process — both require the Docker stack this sandbox couldn't provision. The instrumentation code is
identical across all eight services and was proven correct on the one service (Gateway) that could
run standalone; there's no reason to expect the others behave differently, but it wasn't independently
run.

---

## Running the load tests yourself

```bash
docker-compose up --build          # brings up the real stack
go install go.k6.io/k6@latest      # or: apt/brew install k6, or download from GitHub releases
k6 run load-tests/browse-products.js
k6 run load-tests/search-products.js
# add -e BASE_URL=http://<host>:5000 to point elsewhere
```

Set `OpenTelemetry__ConsoleExporter=true` as an env var on any service to see live trace spans in its
container logs for a handful of manual requests — turn it back off before load testing.
