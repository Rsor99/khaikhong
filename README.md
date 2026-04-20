# Khaikhong

A product catalog and ordering platform designed to support complex product variants and bundles, backed by a scalable API and modern web interface.

The system enables businesses to manage product configurations, pricing, inventory, and bundled offerings while maintaining performance, consistency, and extensibility.

---

## Overview

This platform supports two core domain capabilities:

- **Product Variants**: A product can have multiple configurable dimensions (e.g., size, color), with each variant maintaining its own SKU, pricing, and stock.
- **Product Bundles**: Multiple products or variants can be combined into a single offering, with pricing and stock derived from underlying components.

The system is built with a focus on maintainability, performance, and clear separation of concerns across backend and frontend layers.

---

## Repository Layout
- `KhaikhongService/` – ASP.NET Core backend (Clean Architecture)
- `khaikhong_web/` – Next.js frontend for admin and ordering flows

---

## Backend – `KhaikhongService`

ASP.NET Core 9 backend designed with production-grade architecture patterns, focusing on scalability, testability, and security.

### Architecture

- **Presentation (`Khaikhong.WebAPI`)**
  - API endpoints, middleware, dependency injection
  - Standardized response structure

- **Application (`Khaikhong.Application`)**
  - CQRS (Commands & Queries)
  - Validation, DTOs, orchestration

- **Domain (`Khaikhong.Domain`)**
  - Core business logic
  - Entities, value objects, domain rules

- **Infrastructure (`Khaikhong.Infrastructure`)**
  - Database access (EF Core)
  - Authentication, external services

- **Tests (`Khaikhong.Tests`)**
  - Unit and integration testing

---

### Design Principles

- Clean Architecture with strict dependency boundaries
- CQRS pattern to separate read/write concerns
- Strong validation using FluentValidation
- Secure authentication using RSA-based JWT
- Environment-based configuration (12-factor approach)
- Transactional consistency for critical operations

---

### Key Features

- Product variant management with flexible option modeling
- Bundle composition with stock-aware validation
- Bulk operations for high-volume variant insertion
- Optimized read queries with projection and indexing
- Transaction-safe inventory and order processing

---

### Data Access & Performance

- Read operations optimized with no-tracking queries
- Use of `AsSplitQuery` to avoid Cartesian explosion
- Indexed filtering (`is_active`) for predictable performance
- Projection-based queries to reduce memory overhead
- Bulk insert support for large product configurations

---

### Run Backend

```bash
cd KhaikhongService
dotnet restore
dotnet build
dotnet run --project Khaikhong.WebAPI
```
