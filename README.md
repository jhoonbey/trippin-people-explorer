# TripPin People Explorer

A C# console application that browses, searches, and inspects people from the public [TripPin OData v4 service](https://www.odata.org/odata-services/).

## The Task

The TripPin service exposes a collection of people with names, emails, addresses, and trips. The goal is a clean console UI that lets you:

1. Browse people in a paginated table
2. Filter by first name, last name, gender, or city
3. Drill into a full detail card — profile, addresses, and trips

**Example record from the API:**
```json
{
  "UserName": "russellwhyte",
  "FirstName": "Russell",
  "LastName": "Whyte",
  "Gender": "Male",
  "Emails": ["Russell@example.com", "Russell@contoso.com"],
  "AddressInfo": [{ "Address": "187 Suffolk Ln.", "City": { "Name": "Boise", "CountryRegion": "United States" } }]
}
```

---

## How It Works

OData v4 exposes rich query options in the URL. Rather than concatenating strings, a small fluent builder composes the query, then the service calls it through an HTTP adapter:

```
GET /People?$filter=contains(tolower(FirstName),'scott')&$top=10&$skip=0&$count=true
GET /People?$filter=Gender eq Trippin.PersonGender'Female'&$top=10&$count=true
GET /People?$filter=AddressInfo/any(a:contains(tolower(a/City/Name),'boise'))&$top=10&$count=true
GET /People('russellwhyte')?$expand=Trips
```

- **List** — `$top` + `$skip` for paging; `$count=true` returns the total for the "Showing X–Y of Z" footer
- **Search** — case-insensitive name filter wraps both sides in `tolower(...)`; gender uses the OData qualified enum literal (`Trippin.PersonGender'...'` — namespace confirmed from `$metadata`); city uses an `any` lambda over the `AddressInfo` collection
- **Detail** — `$expand=Trips` pulls the person and their trips in one request
- **Cache** — successful responses are cached by `CachingPeopleService` (a decorator over `IPeopleService`) via `IMemoryCache` (bounded to 500 entries), so navigating back or re-searching — including returning from a person detail view — never fires redundant HTTP calls. `PeopleService` itself is unaware of caching

---

## Solution Structure

```
TripPin.sln
├── src/
│   ├── TripPin.Core/              Models, ODataQueryBuilder, ITripPinApiClient + TripPinApiClient,
│   │                              IPeopleService + PeopleService + CachingPeopleService, Result<T>
│   └── TripPin.Console/           Generic Host entry point
│       ├── Extensions/            ServicesExtensions.cs — all DI wiring in one file
│       └── UI/                    Spectre.Console UI layer
│           ├── IPersonListDisplay.cs / IPersonDetailDisplay.cs  ← display interfaces
│           ├── MenuHandler.cs
│           ├── PersonListDisplay.cs
│           └── PersonDetailDisplay.cs
└── tests/
    └── TripPin.Tests/
        ├── Builders/     ODataQueryBuilder unit tests
        ├── Clients/      TripPinApiClient unit tests
        ├── Services/     PeopleService + CachingPeopleService unit tests
        └── Integration/  Live tests against the real TripPin service (opt-in)
```

**Dependency rule:** `Console → Core ← Tests`. Core never references Console.

---

## Getting Started

### Requirements
- .NET 10 SDK
- Internet access (talks to the live TripPin service)

### 1. Run the app

```bash
dotnet run --project src/TripPin.Console
```

**Main menu:**
```
 _____     _       ____  _
|_   _| __(_)_ __ |  _ \(_)_ __
  | || '__| | '_ \| |_) | | '_ \
  | || |  | | |_) |  __/| | | | |
  |_||_|  |_| .__/|_|   |_|_| |_|
            |_|
People explorer for the TripPin OData v4 service

What would you like to do?
> List people
  Search people
  View a person by user name
  Exit
```

**Search (now includes gender and city):**
```
First name contains (leave blank to skip): Scott
Last name contains (leave blank to skip):
Gender (select Any to skip):
> Any
  Male
  Female
  Unknown
City contains (leave blank to skip): Boise
```

**List view (paginated):**
```
╭─────────────────────────────── People ────────────────────────────────────────╮
│  # │ User name       │ Name              │ Gender │ Emails                    │
├────┼─────────────────┼───────────────────┼────────┼───────────────────────────┤
│  1 │ russellwhyte    │ Russell Whyte     │ Male   │ Russell@example.com, ...  │
│  2 │ scottketchum    │ Scott Ketchum     │ Male   │ ScottKetchum@example.com  │
│  … │                 │                   │        │                           │
╰───────────────────────────────────────────────────────────────────────────────╯
Showing 1-10 of 20

Navigate:
> View a person's details
  Next page
  Back to menu
```

**Detail card:**
```
╭──────────────────────────── Russell Whyte ──────────────────────────────╮
│ User name    russellwhyte                                                │
│ Name         Russell Whyte                                               │
│ Gender       Male                                                        │
│ Age          -                                                           │
│ Favourite    Feature1                                                    │
│ Emails       Russell@example.com                                         │
│              Russell@contoso.com                                         │
╰──────────────────────────────────────────────────────────────────────────╯

              Addresses
 Address           City    Region  Country
 187 Suffolk Ln.   Boise   ID      United States

              Trips (3)
 Name              Dates                         Budget
 Trip in US        2014-01-01 to 2014-01-04       2,800
 Trip in Beijing   2014-02-01 to 2014-02-04      11,000
 ...
```

### 2. Run the tests

**Unit tests only (default — no network required):**
```bash
dotnet test --filter "Category!=Integration"
```
```
Passed!  - Failed: 0, Passed: 23, Skipped: 0, Total: 23, Duration: 80 ms
```

**Integration tests (requires internet — hits the live TripPin service):**
```bash
dotnet test --filter "Category=Integration"
```
```
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5, Duration: 7 s
```

**All tests:**
```bash
dotnet test
```
```
Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28, Duration: 7 s
```

---

## Design Patterns

| Pattern | Where | What it does |
|---|---|---|
| **Adapter** | `TripPinApiClient` implements `ITripPinApiClient` | Isolates raw HTTP + JSON deserialization behind a domain interface; `PeopleService` never touches `HttpClient` |
| **Decorator** | `CachingPeopleService` wraps `IPeopleService` | Adds caching transparently — `PeopleService` stays pure fetch, unaware of `IMemoryCache` |
| **Builder** | `ODataQueryBuilder` | Fluent, composable URL construction — encodes values but preserves OData syntax chars (`(`, `)`, `,`, `'`, `/`, `:`) that are valid in query strings, no raw string concatenation |
| **Result (value object)** | `Result<T>` | Unified error contract at every layer (`ITripPinApiClient` → `IPeopleService` → UI); 404/400 stay in normal control flow, 5xx throw; UI checks `IsSuccess`, no try/catch in display code |
| **Options** | `TripPinOptions` + `ValidateOnStart()` | Typed, validated, init-only config bound at startup — bad config fails fast with a clear message, not deep inside an HTTP call |
| **Factory** | `IHttpClientFactory` (named client `"TripPin"`) | Framework-managed `HttpClient` lifecycle; avoids socket exhaustion that `new HttpClient()` causes |

---

## Key Technical Decisions

| Decision | Why |
|---|---|
| `IHttpClientFactory` + named client | Avoids socket exhaustion; `TripPinApiClient` is the only type that touches HTTP — `PeopleService` never sees transport concerns |
| `AddStandardResilienceHandler()` | Retry, circuit breaker, and timeout built in (.NET 10) — no hand-wired Polly |
| `CachingPeopleService` decorator | `IMemoryCache` injected into the decorator, not into `PeopleService`; TTL from `TripPin:CacheTtlMinutes`; cache bounded to 500 entries (`SizeLimit`); keys are `("people", query)` / `("person", userName)` tuples using `PeopleQuery` record equality — no string-concatenation collision risk. Only successes cached — errors always re-fetch |
| `Result<T>` at every seam | Both `ITripPinApiClient` methods return `Result<T>` — same contract from the HTTP layer through the service layer to the UI. 404 and 400 stay in normal control flow; 5xx throws `TripPinApiException` |
| `ValidateDataAnnotations().ValidateOnStart()` | Fails at startup with a clear message if `BaseUrl` is missing — not deep inside `HttpClient` |
| `ODataQueryBuilder` fluent builder | Encodes query values via `Uri.EscapeDataString` then restores OData syntax chars (`(`, `)`, `,`, `'`, `/`, `:`) that are valid in URI query strings — readable URLs, RFC 3986-compliant, and injection-safe |
| `Trippin.PersonGender'...'` enum literal | OData v4 enum filters require the fully-qualified type name; namespace confirmed from `$metadata`, not assumed |
| `AddressInfo/any(...)` lambda for city | The only correct OData way to filter on a property inside a collection of complex types |
| `tolower(...)` on both sides of `contains` | The TripPin service's `contains` filter is case-sensitive by default |
| `TripPin:RetryCount` + `TripPin:TimeoutSeconds` | Resilience tuning in `appsettings.json` — ops can adjust retry count and total timeout without touching code |
| `[Trait("Category", "Integration")]` | Integration tests opt-in via `--filter` — keeps `dotnet test` hermetic by default, CI stays green offline |
| `ILogger<T>` in client and service | Every outbound request is logged at Debug (URL + status + timing); warnings on rejected filters; errors on unexpected status codes |
| `CancellationToken` threaded throughout | One 30-second token wired to `Ctrl+C` — a dead network can't hang the app forever |
| Three projects instead of five | One boundary (UI-free Core) pays off here; a full Domain/Application/Infrastructure split would just forward calls |

