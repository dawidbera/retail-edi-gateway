# EDI & Supply Chain Gateway

## 1. Overview
The **EDI & Supply Chain Gateway** is an enterprise-grade integration middleware designed to orchestrate electronic data exchange (EDI) with suppliers, specifically for high-priority temporary campaigns in retail environments.

It automates procurement, tracks shipping notifications, and manages warehouse slots to ensure on-time delivery for time-critical windows.

## 2. Key Features
* **Campaign Tracking Dashboard:** Monitor fulfillment and delivery status of campaigns.
* **PO Processing:** Simulated outbound EDI transaction queuing (EDIFACT `ORDERS` placeholder).
* **Inbound Message Parsing:** Handle `ORDRSP` (Order Response) and `DESADV` (Despatch Advice) messages.
* **Warehouse Slot Management:** Coordinate truck arrival slots via internal reservation system.
* **WMS Sync Simulation:** Background processing of slot reservations with simulated external integration.
* **Proactive Alerting:** Flag missing responses, shipping delays, or quantity discrepancies.
* **API Security:** Hardened endpoints using API Key authentication.

## 3. Technology Stack
* **Framework:** .NET 8 (ASP.NET Core MVC)
* **Database:** PostgreSQL with Entity Framework Core (EF Core)
* **Observability:** OpenTelemetry (Prometheus metrics, Grafana logs/traces)
* **Architecture:** Clean Architecture (Core, Application, Infrastructure, Web layers)
* **Logging:** Serilog with structured logging

## 4. Environment & Infrastructure Setup

### 4.1 Database (Local PostgreSQL 18.4)
The project is configured to use a local PostgreSQL 18.4 instance.
* **Server:** `localhost:5432`
* **Database:** `edigateway`
* **Username:** `admin`
* **Password:** `adminpassword`

**Initialization:**
1. Create the database and user:
 ```sql
 CREATE USER admin WITH PASSWORD 'adminpassword' SUPERUSER;
 CREATE DATABASE edigateway OWNER admin;
 ```
2. Apply migrations (from the project root):
 ```powershell
 dotnet ef database update -project src\RetailEdiGateway.Infrastructure -startup-project src\RetailEdiGateway.Web
 ```

### 4.2 CI/CD (Jenkins & IIS)
The project includes a `Jenkinsfile` for automated build, test, and deployment to IIS.
* **Jenkins Pipeline:** Create a "Pipeline" project and link it to the Git repository.
* **Credentials:** Add a secret text credential with ID `PROD_DB_CONNECTION_STRING` containing the connection string.
* **IIS Deployment:** The pipeline automatically deploys to `C:\inetpub\wwwroot\RetailEdiGateway` using the `EdiGatewayPool` application pool.

## 5. Getting Started

### Prerequisites
* **.NET 8 SDK**
* **PostgreSQL 16+** (PostgreSQL 18.4 recommended)
* **EF Core CLI Tools:** `dotnet tool install -global dotnet-ef`

### Installation & Execution
1. **Restore & Build:**
 ```powershell
 dotnet restore
 dotnet build
 ```

2. **Run Locally:**
 Set the environment to `Development` to use the local database settings:
 ```powershell
 $env:ASPNETCORE_ENVIRONMENT='Development'
 dotnet run -project src\RetailEdiGateway.Web -urls "http://localhost:5000"
 ```

3. **Run Tests:**
 ```powershell
 dotnet test
 ```

## 5. Project Structure
* `src/RetailEdiGateway.Core`: Domain entities, enums, and core business rules.
* `src/RetailEdiGateway.Application`: Use cases (MediatR), interfaces, and application logic.
* `src/RetailEdiGateway.Infrastructure`: Database implementation (EF Core), external services, and background processors.
* `src/RetailEdiGateway.Web`: MVC/API Controllers, Views, and application configuration.
* `tests/`: Unit and integration tests.

## 6. Architecture & Data Flow

### 6.1 Clean Architecture Dependency Flow
The project is built following Clean Architecture principles, ensuring separation of concerns, testability, and independence from external frameworks:

```mermaid
graph TD
 %% Clean Architecture Layering
 subgraph Presentation ["Presentation Layer"]
 Web[RetailEdiGateway.Web]
 end
 
 subgraph Infra ["Infrastructure Layer"]
 EFCore[EF Core PostgreSQL DbContext]
 EdiServices[EDIFACT Parser & Service Client]
 OTel[OpenTelemetry Stack]
 end

 subgraph App ["Application Layer"]
 MediatR[MediatR Handlers & CQRS]
 DTOs[DTOs & Use Cases]
 FluentVal[FluentValidators]
 end

 subgraph Core ["Core / Domain Layer"]
 Entities[Domain Entities & Value Objects]
 Interfaces[Repository & Service Interfaces]
 end

 %% Dependency Directions (Inner layers do not depend on outer layers)
 Web --> App
 Web --> Infra
 Infra -.-> Interfaces
 App --> Core
```

### 6.2 End-to-End EDI and Supply Chain Flow
The gateway orchestrates communication between the internal ERP system, external suppliers, and the Warehouse Management System (WMS):

```mermaid
sequenceDiagram
 autonumber
 participant ERP as ERP / PIM
 participant GW as EDI Gateway (.NET 8)
 participant SUP as External Supplier
 participant WMS as WMS System

 Note over ERP, SUP: 1. Purchase Order Dispatch
 ERP->>GW: POST /api/v1/orders (PO)
 GW->>GW: Persist PO & Queue Outbox Message
 GW->>SUP: Dispatch EDIFACT ORDERS (Purchase Order)

 Note over GW, SUP: 2. Supplier Acknowledgment
 SUP->>GW: POST /api/v1/edi/inbound (ORDRSP)
 GW->>GW: Parse & Validate quantities/dates
 alt Discrepancy Found
 GW->>GW: Flag Mismatched & Trigger Alert
 else Validation Successful
 GW->>GW: Update PO Status to Confirmed
 end

 Note over SUP, WMS: 3. Advanced Shipping & Slot Booking
 SUP->>GW: POST /api/v1/edi/inbound (DESADV / ASN)
 GW->>GW: Process Shipped SSCC & Pallets
 GW->>WMS: POST /api/v1/logistics/slots (Request Slot Booking)
 WMS->>GW: Booked Slot Confirmation (Bay & Arrival Time)
 GW->>GW: Associate booked slot with DESADV
```

### 6.3 Microservices Architecture & Request Flow
The Gateway operates as a central hub within a distributed environment, coordinating with multiple external services while maintaining its own internal background processing and observability stack.

```mermaid
graph TB
 subgraph ExternalServices ["External Systems"]
 ERP[ERP / PIM System]
 SUP[Supplier EDI Systems]
 WMS_EXT[External WMS]
 end

 subgraph GatewayApp ["Retail EDI Gateway"]
 direction TB
 API[ASP.NET Core Web API]
 DB[(PostgreSQL 18.4)]
 
 subgraph BackgroundWorkers ["Background Services"]
 Outbox[Outbox Processor]
 Alerting[Alerting Service]
 WMSSync[WMS Sync Processor]
 end
 end

 subgraph Observability ["Observability Stack"]
 OTel[OpenTelemetry SDK]
 Prom[Prometheus]
 Jaeger[Jaeger]
 Grafana[Grafana Dashboard]
 end

 %% Request Flows
 ERP -- "1. Send PO" --> API
 API -- "2. Persist" --> DB
 
 DB -- "3. Fetch Pending" --> Outbox
 Outbox -- "4. Dispatch EDI" --> SUP
 
 SUP -- "5. Send ORDRSP/DESADV" --> API
 API -- "6. Update Status" --> DB
 
 DB -- "7. Monitor Deadlines" --> Alerting
 Alerting -- "8. Trigger Notifications" --> API
 
 DB -- "9. Fetch New Slots" --> WMSSync
 WMSSync -- "10. Sync Logistics" --> WMS_EXT

 %% Telemetry Flows
 GatewayApp -- "Metrics/Traces" --> OTel
 OTel --> Prom
 OTel --> Jaeger
 Prom --> Grafana
 Jaeger --> Grafana
```

```
