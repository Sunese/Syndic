# Test Coverage Analysis

## Current State

The Syndic codebase currently has **zero automated tests** — no test projects, no test configuration, no test runner packages, and no CI/CD test steps. This analysis identifies where tests would provide the most value and proposes a prioritized plan.

---

## Priority 1: Backend Unit Tests (High Value, Low Effort)

### 1.1 RSS Parsing — `ChannelDto.TryCreate` and `ItemDto.TryCreate`

**Files:** `Syndic.ReaderService/Rss/ChannelDto.cs`, `Syndic.ReaderService/Rss/ItemDto.cs`

These static factory methods contain the core parsing and validation logic. They are pure functions operating on `SyndicationFeed`/`SyndicationItem` inputs with no I/O dependencies, making them ideal unit test targets.

**Proposed test cases:**
- Valid feed with all fields populated → returns `true` with correct `ChannelDto`
- Feed with no link → throws `InvalidOperationException`
- Feed with `LastUpdatedTime == DateTimeOffset.MinValue` → `Published` is `null`
- Feed with invalid `ImageUrl` → `ImageUrl` is `null`
- Item with both title and description → returns `true`
- Item missing both title and description → returns `false` with error message
- Item with `PublishDate == DateTimeOffset.MinValue` → `PublishedAt` is `null`
- Item with valid `media:content` URL → `ImageUrl` is populated
- Item with invalid `media:content` URL → warning is set, `ImageUrl` is `null`
- Item with no `alternate` link → `Link` is `null`
- Feed with one failing item → entire `TryCreate` returns `false`

### 1.2 `FetchChannelResult` Factory Methods

**File:** `Syndic.ReaderService/Rss/RssParser.cs` (lines 64–83)

**Proposed test cases:**
- `CreateSuccess` → `Success` is `true`, `Channel` is set, `ErrorMessage` is `null`
- `CreateFailure` → `Success` is `false`, `Channel` is `null`, `ErrorMessage` is set
- Both methods correctly propagate `ChannelTitle` and `CustomTitle` from the subscription

### 1.3 Domain Entities

**Files:** `Syndic.ReaderDb/Entities/User.cs`, `Syndic.ReaderDb/Entities/Subscription.cs`, `Syndic.ReaderDb/Entities/Entity.cs`

**Proposed test cases:**
- `User` constructor with empty/null email → throws `ArgumentException`
- `User` constructor with valid email → sets `Email`, `CreatedAt`, and `OIDCSubject`
- `Subscription` constructor → sets `SubscribedAt` to approximately `DateTimeOffset.UtcNow`
- `Entity.Equals` — same ID → `true`; different ID → `false`; non-Entity object → `false`; same reference → `true`
- `Entity.GetHashCode` → consistent with `Id.GetHashCode()`

### 1.4 `HttpContextExtensions.GetUser`

**File:** `Syndic.ReaderService/HttpContextExtensions.cs`

**Proposed test cases:**
- `CurrentUser` item is a valid `User` → returns it
- `CurrentUser` item is missing → throws `InvalidOperationException`
- `CurrentUser` item is wrong type → throws `InvalidCastException`

---

## Priority 2: Backend Integration Tests (High Value, Medium Effort)

### 2.1 `UserMiddleware`

**File:** `Syndic.ReaderService/Middleware/UserMiddleware.cs`

This middleware performs authentication + user creation logic. It needs integration-style tests with a mocked `HttpContext` and an in-memory or SQLite `ReaderDbContext`.

**Proposed test cases:**
- Unauthenticated request → passes through to `_next` without setting `CurrentUser`
- Authenticated user with no `sub` claim → returns 401
- Authenticated user with no `provider` claim and user not in DB → returns 401
- Authenticated user exists in DB → attaches existing user to `context.Items`
- Authenticated user does not exist in DB → creates new user, attaches to `context.Items`

### 2.2 API Endpoint Tests (using `WebApplicationFactory`)

**Files:** `Syndic.ReaderService/Endpoints/SubscriptionEndpoints.cs`, `FeedEndpoints.cs`, `AdminEndpoints.cs`

Use ASP.NET Core's `WebApplicationFactory<T>` with an in-memory database to test the full request pipeline.

**Proposed test cases for Subscriptions:**
- `GET /api/subscriptions` — returns user's subscriptions only (not other users')
- `POST /api/subscriptions` — invalid URL → 400 Bad Request
- `POST /api/subscriptions` — duplicate subscription → 409 Conflict
- `POST /api/subscriptions` — valid URL but unparseable RSS feed → 400
- `POST /api/subscriptions` — valid new subscription → 201 Created
- `DELETE /api/subscriptions/{id}` — non-existent ID → 400
- `DELETE /api/subscriptions/{id}` — valid ID → 200
- **Security bug:** `DELETE /api/subscriptions/{id}` currently does not check that the subscription belongs to the authenticated user. A test should confirm/expose this.

**Proposed test cases for Feed:**
- `GET /api/feed` — unauthenticated → 401
- `GET /api/feed` — authenticated user with subscriptions → returns parsed channels
- `GET /api/feed` — authenticated user with no subscriptions → returns empty channels array

**Proposed test cases for Admin:**
- `GET /api/admin` — non-admin user → 403 Forbidden
- `GET /api/admin` — admin user → 200

### 2.3 EF Configuration Tests

**Files:** `Syndic.ReaderDb/Entities/Configurations/SubscriptionConfiguration.cs`, `UserConfiguration.cs`

**Proposed test cases:**
- Unique constraint on `(UserId, ChannelUrl)` — inserting duplicates throws
- Unique constraint on `User.Email` — inserting duplicate email throws
- Required fields validation (e.g. `Email` is required)

---

## Priority 3: Frontend Tests (Medium Value, Medium Effort)

### 3.1 JWT Minting — `mintInternalJwt`

**File:** `Syndic.Frontend/src/lib/server/jwt.ts`

This is a pure async function, easily testable with `vitest`.

**Proposed test cases:**
- Produced JWT contains correct `sub` (email), `iss`, `aud`, and `provider` claims
- JWT has ~5 minute expiration
- JWT uses `HS256` algorithm with `kid: "internal-auth"`

### 3.2 Server Hooks — `hooks.server.ts`

**File:** `Syndic.Frontend/src/hooks.server.ts`

**Proposed test cases:**
- Unauthenticated request to non-signin route → redirects to `/signin`
- Unauthenticated request to `/signin` → passes through
- Authenticated request → passes through
- Request interceptor: missing token fields → throws 401
- Request interceptor: valid tokens → attaches `Authorization: Bearer <jwt>` header
- Response interceptor: non-OK response → throws error with status and body text

### 3.3 Remote API Layer

**Files:** `Syndic.Frontend/src/remote/subscription.remote.ts`, `feed.remote.ts`

**Proposed test cases:**
- `getSubsRemote`: 403/401 response → throws 403 error
- `getSubsRemote`: server error → throws 500
- `getSubsRemote`: successful response → returns data
- `createSubRemote`: validates URL format via zod schema
- `createSubRemote`: transforms empty `customTitle` to `null`
- `deleteSubRemote`: validates `id` is a valid GUID

---

## Priority 4: Security-Critical Tests

### 4.1 Authorization Boundary Tests

- **DELETE subscription authorization gap:** `SubscriptionEndpoints.cs:80` looks up the subscription by `id` only, without verifying `s.UserId == user.Id`. Any authenticated user can delete any other user's subscription. Tests should cover this to prevent regressions once fixed.
- JWT validation: ensure expired tokens, wrong audience, wrong issuer, and tampered signatures are all rejected.
- Admin endpoint: verify the `MustBeAdmin` policy actually blocks non-admin users.

### 4.2 Input Validation

- `POST /api/subscriptions` with non-URL string, relative URL, `javascript:` URI, extremely long URL
- `User` creation with email-like strings that aren't valid emails

---

## Recommended Test Infrastructure

### Backend (.NET)
- **Framework:** xUnit (standard for modern .NET)
- **Mocking:** NSubstitute or Moq
- **Integration:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)
- **Database:** EF Core In-Memory provider or SQLite in-memory for integration tests
- **Project structure:**
  - `Syndic.ReaderService.Tests` — unit + integration tests for the API
  - `Syndic.ReaderDb.Tests` — entity and configuration tests

### Frontend (SvelteKit)
- **Framework:** Vitest (already compatible with the Vite-based build)
- **Add to `package.json`:** `vitest`, `@testing-library/svelte` (if component tests are desired)
- **Config:** Add `vitest.config.ts` extending the existing Vite config

### CI/CD
- Add a test step to `.github/workflows/deploy.yml` that runs `dotnet test` and `npm test` before the Docker build stage. Fail the pipeline on test failure.

---

## Summary Table

| Area | Test Type | Priority | Effort | Impact |
|------|-----------|----------|--------|--------|
| ChannelDto / ItemDto parsing | Unit | P1 | Low | High — core business logic |
| FetchChannelResult | Unit | P1 | Low | Medium |
| Domain entities (User, Subscription, Entity) | Unit | P1 | Low | Medium |
| HttpContextExtensions | Unit | P1 | Low | Low-Medium |
| UserMiddleware | Integration | P2 | Medium | High — auth/user creation |
| Subscription endpoints | Integration | P2 | Medium | High — CRUD + security gap |
| Feed endpoints | Integration | P2 | Medium | Medium |
| EF configurations | Integration | P2 | Medium | Medium |
| JWT minting (frontend) | Unit | P3 | Low | Medium — security surface |
| Server hooks (frontend) | Unit | P3 | Medium | Medium |
| Remote API layer (frontend) | Unit | P3 | Low | Low-Medium |
| Authorization boundaries | Security | P4 | Medium | High — security critical |
| Input validation | Security | P4 | Low | Medium |
