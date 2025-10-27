# Khaikhong

Software Engineer Candidate – Test Project for FlowAccount. The brief centres on designing a Product Variant and Product Bundle platform that exposes resilient backend APIs and a modern frontend (the original assignment called for Angular; this solution documents a Next.js implementation to emphasise architectural reasoning).

## Assessment Brief
- **Product Variant**: A Product Master can expose multiple option dimensions (e.g., colour, size); every resulting variant carries its own SKU, pricing, and inventory.
- **Product Bundle**: Bundles combine multiple products or specific variants into a single sellable package with bespoke pricing and stock logic derived from the component items.
- **Expectations**: Deliver database schema design, API endpoints/business logic, and a frontend that helps teams manage catalogue and orders with senior-level depth.

The sections below summarise the architectural approach implemented in this codebase across both the backend and the Next.js frontend.

## Repository Layout
- `KhaikhongService/` – Production-grade Clean Architecture backend (details below)
- `khaikhong_web/` – Next.js 16 App Router frontend for admin operations and ordering flows

---

## Backend – `KhaikhongService`

Backend service built with ASP.NET Core 9 and Clean Architecture to demonstrate production-grade patterns: CQRS with MediatR, layered separation, RSA-secured authentication, and comprehensive unit testing.

### Architecture
- **Presentation (`Khaikhong.WebAPI`)** – ASP.NET Core Web API hosting, dependency injection bootstrap, middleware, and consistent `ApiResponse<T>` envelopes.
- **Application (`Khaikhong.Application`)** – CQRS commands/queries, validators, and mapping profiles coordinating work through `IUnitOfWork` abstractions.
- **Domain (`Khaikhong.Domain`)** – Framework-agnostic entities, value objects, domain events, and base entity types.
- **Infrastructure (`Khaikhong.Infrastructure`)** – EF Core persistence across identity and business contexts, repository implementations, token generation, and cross-cutting services.
- **Tests (`Khaikhong.Tests`)** – xUnit suites covering application orchestrations, domain rules, and infrastructure behaviors with in-memory providers.

### Design Principles
- Clean Architecture boundaries mandate dependency flow from outer layers inward; business logic never couples to frameworks.
- CQRS + MediatR split reads/writes, with FluentValidation guard clauses and AutoMapper translation layers.
- SOLID/DRY/KISS guide abstraction choices, keeping classes single-purpose and test-friendly.
- Security by design: RSA-signed JWTs, hashed refresh tokens, strict cookie policies, environment-driven secrets.
- Guard clauses, explicit logging metadata, and immutable state favor maintainability and predictable behavior.
- Deployment ready: environment-variable configuration, container-friendly RSA key management, and stateless API host.

### Prerequisites
- [.NET SDK 9.0](https://dotnet.microsoft.com/download)
- MySQL 8.x (or compatible) running locally or accessible via connection string
- Optional: `dotnet-ef` global tool if you plan to add migrations

### Quick Start
```bash
cd KhaikhongService

# restore dependencies
dotnet restore

# build all projects
dotnet build

# run the web API (serves Swagger at /swagger)
dotnet run --project Khaikhong.WebAPI
```

### Configuration
Configuration follows 12-factor conventions:
- `Khaikhong.WebAPI/appsettings.json` – baseline settings.
- `Khaikhong.WebAPI/appsettings.{Environment}.json` – environment overrides (e.g., `Development`).
- `.env` – sensitive secrets (connection strings, RSA keys) loaded via `AddEnvironmentVariables`.

Create a `.env` file at the solution root (`KhaikhongService/`):
```ini
CONNECTIONSTRINGS__DEFAULTCONNECTION=
JWTSETTINGS__PRIVATEKEYBASE64=
JWTSETTINGS__PUBLICKEYBASE64=
```
RSA keys are stored as base64-encoded PEM strings, decoded at startup—no filesystem key dependency.

Run `dotnet test` to execute the xUnit suites:
```bash
cd KhaikhongService
dotnet test
```

### Project Structure
```
KhaikhongService/
├── Khaikhong.Service.sln
├── Khaikhong.WebAPI          # Presentation layer (ASP.NET Core 9)
├── Khaikhong.Application     # CQRS, validators, contracts, DTOs
├── Khaikhong.Domain          # Entities, value objects, enums
├── Khaikhong.Infrastructure  # EF Core repositories, services, UoW
└── Khaikhong.Tests           # Unit/integration test projects
```

### Data Access & Performance Profile
- **Read paths tuned for catalogue scale**: `ProductRepository` and `BundleRepository` disable change tracking, encapsulate reads in `ReadCommitted` transactions, and expand relationships via `AsSplitQuery()` plus filtered `Include` chains (active options/variants only). This guards against Cartesian products while keeping the entire product surface in memory for admin grids without overwhelming the database.
- **Predictable hot-path filters**: Every aggregate is soft-deleted through `is_active`; mirrored indexes (`idx_product_master_is_active`, `idx_bundle_is_active`, etc.) keep those predicates sargable. Additional covering indexes on SKU, stock, and foreign keys fuel fast lookups for pricing validation and stock reconciliation.
- **Projection-first validation**: Guard queries (e.g., `ExistsByNameOrSkuAsync`, `GetActiveVariantsForProductsAsync`) project to lightweight DTOs instead of hydrating full entities, minimising data transfer and deserialisation when validating uniqueness or composing carts.
- **Bulk writes for variant-heavy inserts**: EFCore.BulkExtensions orchestrates hierarchical inserts (`ProductRepository.BulkInsertAsync`) across product masters, options, values, variants, and combinations in batches of 2000 with preserved ordering. Bundle composition reuses the same bulk pipeline and falls back to regular EF inserts only when MySQL returns a statistics mismatch.
- **Transactional guards**: Both the bulk pipelines and read aggregations run inside explicit transactions, guaranteeing consistent snapshots for bundle stock calculations and ensuring partial bulk failures cannot leave orphaned child rows.

### Data Dictionary & Index Strategy

#### `product_master`
- **Columns**: `id (char(36) UUIDv7 PK)`, `name (varchar 255)`, `description (text)`, `base_price (decimal 12,2)`, `sku (varchar 100 nullable)`, `base_stock (int nullable)`, `created_by`, `updated_by`, `created_at (datetime(6))`, `updated_at (datetime(6))`, `is_active (bool)`.
- **Relationships**: `1:M` to `variant_option`, `product_variant`, `bundle_item`, `order_item`.
- **Indexes**: `uq_product_master_sku`, `idx_product_master_is_active`.

#### `variant_option`
- **Columns**: `id (char(36) PK)`, `product_id (FK → product_master.id)`, `name (varchar 100)`, `is_active (bool)`.
- **Relationships**: `1:M` to `variant_option_value`.
- **Indexes**: `idx_variant_option_product_id`.

#### `variant_option_value`
- **Columns**: `id (char(36) PK)`, `option_id (FK → variant_option.id)`, `value (varchar 100)`, `is_active (bool)`.
- **Relationships**: `1:M` to `product_variant_combination`.
- **Indexes**: `idx_variant_option_value_option_id`, `idx_variant_option_value_value`.

#### `product_variant`
- **Columns**: `id (char(36) PK)`, `product_id (FK → product_master.id)`, `sku (varchar 100 nullable)`, `price (decimal 12,2)`, `stock (int)`, `created_by`, `updated_by`, `created_at (datetime(6))`, `updated_at (datetime(6))`, `is_active (bool)`.
- **Relationships**: `1:M` to `product_variant_combination`, `bundle_item`, `order_item`.
- **Indexes**: `uq_product_variant_sku`, `idx_product_variant_product_id`, `idx_product_variant_stock`, `idx_product_variant_is_active`.

#### `product_variant_combination`
- **Columns**: `id (char(36) PK)`, `variant_id (FK → product_variant.id)`, `option_value_id (FK → variant_option_value.id)`, `is_active (bool)`.
- **Purpose**: Bridges variants to explicit option values so the frontend can render full attribute strings per SKU.
- **Indexes**: `idx_variant_combination_variant_id`, `idx_variant_combination_option_value_id`, `uq_variant_option_value` (unique pair).

#### `bundle`
- **Columns**: `id (char(36) PK)`, `name (varchar 255)`, `description (text)`, `price (decimal 12,2)`, `created_by`, `updated_by`, `created_at (datetime(6))`, `updated_at (datetime(6))`, `is_active (bool)`.
- **Relationships**: `1:M` to `bundle_item`.
- **Indexes**: `idx_bundle_is_active`.

#### `bundle_item`
- **Columns**: `id (char(36) PK)`, `bundle_id (FK → bundle.id)`, `product_id (FK → product_master.id)`, `variant_id (FK → product_variant.id nullable)`, `quantity (int)`, `is_active (bool)`.
- **Relationships**: Each row references a base product or a specific variant, enabling mixed bundles and per-component stock reservations.
- **Indexes**: `idx_bundle_item_bundle_id`, `idx_bundle_item_product_variant`.

#### `order`
- **Columns**: `id (char(36) PK)`, `user_id (FK → users.id)`, `created_at (datetime(6))`, `updated_at (datetime(6))`, `is_active (bool)`.
- **Relationships**: `1:M` to `order_item`.
- **Indexes**: `idx_order_user_id`, `idx_order_is_active`.

#### `order_item`
- **Columns**: `id (char(36) PK)`, `order_id (FK → order.id)`, `product_id (FK → product_master.id nullable)`, `variant_id (FK → product_variant.id nullable)`, `bundle_id (FK → bundle.id nullable)`, `quantity (int)`.
- **Relationships**: Polymorphic linkage so audit trails can point back to the precise product, variant, or bundle purchased.
- **Indexes**: `idx_order_item_order_id`, `idx_order_item_product_variant`.

#### `users`
- **Columns**: `id (char(36) PK)`, `email (varchar 255)`, `password_hash (varchar 255)`, `first_name (varchar 100)`, `last_name (varchar 100)`, `role (varchar 50)`, `created_at (datetime(6))`, `updated_at (datetime(6))`, `is_active (bool)`.
- **Indexes**: `uq_users_email`.

#### `refresh_tokens`
- **Columns**: `id (char(36) PK)`, `user_id (FK → users.id)`, `token_id (varchar 64)`, `token_hash (varchar 256)`, `expires_at (datetime(6))`, `revoked_at (datetime(6) nullable)`, `created_at (datetime(6))`, `updated_at (datetime(6))`, `is_active (bool)`.
- **Indexes**: `uq_refresh_tokens_token_id`, composite `(user_id, is_active)` for pruning active tokens.

---

## Frontend – `khaikhong_web`

Next.js 16 (React 19, App Router) replaces the originally requested Angular client to highlight senior-level frontend design for complex catalog flows while keeping interoperability with the ASP.NET Core APIs.

### Stack & Architecture
- **Rendering model**: App Router with server components for shell/layout and client components for data-heavy screens (`app/page.tsx` gates login, admin, and ordering views).
- **State & networking**: `@tanstack/react-query` orchestrates caching/invalidation for `/admin` and consumer flows; Axios clients (`app/lib/ApiClient.ts`) centralise auth headers, token refresh, and error normalisation; Sensitive tokens live in a minimal Zustand store (`app/stores/authStore.ts`) to avoid prop drilling.
- **Styling & UI**: Tailwind CSS v4 utilities, glassmorphic theming, and `react-toastify` for non-blocking feedback. Composite form modals (`app/components/common/Modal.tsx`) encapsulate focus/keyboard handling.
- **Routing & API proxy**: `next.config.ts` rewrites `/api/*` to `http://localhost:5075/api/*`, keeping same-origin cookies while targeting the ASP.NET backend; `certificates/` holds a self-signed pair consumed by `npm run dev` (HTTPS dev server).

### Product Variant & Bundle Experience
- **Authentication-first landing**: The root route provides credential-based access (`login`, `logout`, `getUserProfile`) and eagerly refreshes tokens to demo secure admin tooling.
- **Admin dashboard** (`app/components/admin/AdminDashboard.tsx`):
  - `ProductManager` builds nested option sets, derives variant SKUs/pricing, and maps API payloads back into editable forms (handles bulk option value changes and SKU conflicts with optimistic cache invalidation).
  - `BundleManager` lets admins compose bundles from base products or specific variants, manage per-item quantities, and ensures stock-aware validation before persistence.
- **Consumer ordering panel** (`app/components/order/UserOrderPanel.tsx`): surfaces purchasable variants and bundles, snapshotting stock, auto-clamping quantities, and issuing batch order mutations with conflict messaging.
- **Domain typing**: Shared DTOs in `app/types` keep payloads aligned with backend contracts; helper selectors (e.g. `describeVariant`) guarantee consistent display strings for variant combinations.

### Environment & Local Development
```bash
cd khaikhong_web
npm install

# Backend default: http://localhost:5075 (dotnet run). Override via .env.local when needed:
echo 'NEXT_PUBLIC_API_URL=https://localhost:5075' > .env.local

# Launch HTTPS dev server (uses certificates/localhost.pem|key.pem)
npm run dev
```
- When running without `.env.local`, Axios falls back to the rewrite (`/api/* → http://localhost:5075/api/*`). Setting `NEXT_PUBLIC_API_URL` is recommended for deployed environments or non-default ports.
- React Query caches are namespaced (`["admin-products"]`, `["bundles"]`, etc.) to keep cache invalidations explicit during heavy batch operations—mirroring the backend brief’s focus on scale.

### Testing & Hardening Considerations
- Storybook-style visual tests are not wired yet; manual smoke tests cover login, variant creation, bundle composition, and cart checkout against the live API.
- Next steps include adding Playwright smoke suites and schema-generated TS types to prevent drift as the backend evolves.
