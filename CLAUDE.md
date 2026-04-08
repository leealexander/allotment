# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Allotment** is an IoT automation system for a polytunnel (greenhouse) running on a Raspberry Pi. It controls doors, irrigation, and monitors environmental conditions via a web application.

**Core Capabilities:**
- Door control (open/close via GPIO pins)
- Plant irrigation with water pump
- Temperature and humidity monitoring (DHT22 sensor)
- Water level monitoring in water butts (pressure sensor via ESP32)
- Solar panel monitoring
- AutoPilot mode (automatic door control based on temperature)

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 10.0, ASP.NET Core with Razor Pages |
| Language | C# (main app), C++ (ESP32 firmware) |
| Authentication | Azure AD/Entra ID via Microsoft.Identity.Web |
| IoT | System.Device.Gpio, Iot.Device.Bindings (DHT22) |
| Messaging | MQTTnet for sensor communication |
| Modbus | NModbus for solar controller communication |
| Frontend | Bootstrap 5, jQuery, Chart.js |
| Deployment | Docker (ARM32v7 for Raspberry Pi) |
| Data Storage | CSV files (no database) |

## Development Commands

```bash
# Restore dependencies
dotnet restore allotment.sln

# Build debug (uses FakeMachine, no auth required)
dotnet build allotment/allotment.csproj -c Debug

# Build release
dotnet build allotment/allotment.csproj -c Release

# Run locally in development mode
dotnet run --project allotment/allotment.csproj

# Run all tests
dotnet test AllotmentTests/AllotmentTests.csproj

# Run a single test by name
dotnet test AllotmentTests/AllotmentTests.csproj --filter "FullyQualifiedName~TestMethodName"

# Build and push Docker image for Raspberry Pi (ARM32v7)
./BuildAndPush.sh
```

## Key Architecture Patterns

### Machine Abstraction

The `IMachine` interface abstracts all GPIO/hardware operations. Two implementations exist:
- `PiMachine`: Production — real GPIO pins on Raspberry Pi
- `FakeMachine`: Development mock — no hardware needed

Selection is automatic based on environment:
```csharp
services.AddMachine(builder.Environment.IsDevelopment())
```

All hardware actions go through `IMachineControlService`, which wraps `IMachine` with business logic (e.g. auto water-off after a duration, job scheduling).

### Custom Job Scheduler

The project uses a **custom in-process job scheduler** (`IJobManager`) rather than a hosted service loop. Jobs implement `IJobService`:

```csharp
public interface IJobService
{
    Task RunAsync(IRunContext ctx);
}
```

Jobs reschedule themselves via `IRunContext`:
```csharp
ctx.RunAgainIn(TimeSpan.FromMinutes(5));   // relative
ctx.RunAgainAt(nextUtcTime);               // absolute
```

Startup jobs are registered in `Program.cs` as a fluent chain:
```csharp
services.AddJobs()
    .StartWith<MachineStartup>()
    .StartWith<TempMonitor>()
    ...
```

Jobs run in a DI scope (each execution gets fresh scoped services).

### Data Persistence

All data stored as CSV files (no database). Data stores are in `DataStores/` and injected via interfaces. Data is stored in `/data` when running in Docker (mounted volume).

- `TempStore` - Temperature/humidity readings
- `SolarStore` - Solar panel data
- `WaterLevelStore` - Water level readings
- `StateStore` - Application state
- `SettingsStore` - User settings
- `LogsStore` - Activity logs

### Configuration

Authentication is toggled via `AllotmentOptions:Auth.AuthenticationEnabled`. In development this is false, so no Azure AD is needed and `FakeMachine` is used automatically. Key config sections:

- `AllotmentOptions:Auth` - Authentication settings
- `AzureAd` - Azure AD/Entra ID configuration

### API Endpoints

- `GET /api/status` - Returns current machine status (doors, water, temperature, humidity)

## GPIO Pin Configuration

| Pin | Function |
|-----|----------|
| 19 | Door Open |
| 26 | Door Close |
| 13 | Water Pump |
| 6 | Water Sensor Power |
| 12 | DHT22 Temperature/Humidity |

## Important Files for Common Tasks

| Task | Key Files |
|------|-----------|
| Add new sensor | `Machine/Readers/`, `Machine/Monitoring/` |
| Modify door/water behavior | `Machine/MachineControlService.cs`, `Machine/AutoPilot.cs` |
| Add UI page | `Pages/`, `wwwroot/js/site.js` |
| Change data storage | `DataStores/` |
| Add background job | `Jobs/` — implement `IJobService`, register in `Program.cs` |
| Configure settings | `AppSettingsConfig/`, `appsettings.json` |

## ESP32 Pressure Sensor

Located in `Sensors/PressureSensor/` — C++ (PlatformIO/Arduino). Reads water pressure to calculate tank level and communicates via MQTT. Build with PlatformIO (`platformio.ini`).
