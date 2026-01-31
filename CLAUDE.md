# CLAUDE.md - AI Assistant Guidelines for Allotment Project

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
| Framework | .NET 8.0, ASP.NET Core with Razor Pages |
| Language | C# (main app), C++ (ESP32 firmware) |
| Authentication | Azure AD/Entra ID via Microsoft.Identity.Web |
| IoT | System.Device.Gpio, Iot.Device.Bindings (DHT22) |
| Messaging | MQTTnet for sensor communication |
| Modbus | NModbus for solar controller communication |
| Frontend | Bootstrap 5, jQuery, Chart.js |
| Deployment | Docker (ARM32v7 for Raspberry Pi) |
| Data Storage | CSV files (no database) |

## Repository Structure

```
allotment/
├── allotment/                  # Main ASP.NET Core application
│   ├── Machine/                # Hardware control layer
│   │   ├── IMachine.cs         # Hardware abstraction interface
│   │   ├── PiMachine.cs        # Real GPIO implementation
│   │   ├── FakeMachine.cs      # Mock for development
│   │   ├── MachineControlService.cs  # Main control service
│   │   ├── AutoPilot.cs        # Automatic door control
│   │   ├── Readers/            # Sensor reading classes
│   │   └── Monitoring/         # Background monitoring services
│   ├── DataStores/             # File-based data persistence
│   ├── Pages/                  # Razor Pages (UI)
│   ├── Services/               # Business logic services
│   ├── Jobs/                   # Background job scheduling
│   ├── ApiModels/              # API request/response DTOs
│   ├── AppSettingsConfig/      # Configuration binding classes
│   ├── wwwroot/                # Static assets (CSS, JS)
│   ├── Program.cs              # Application entry point
│   └── Dockerfile              # Container configuration
├── AllotmentTests/             # Unit tests (MSTest + Moq)
├── Sensors/PressureSensor/     # ESP32 firmware (PlatformIO)
├── allotment.sln               # Visual Studio solution file
└── BuildAndPush.sh             # Docker build/push script
```

## Development Commands

### Building and Running

```bash
# Restore dependencies
dotnet restore allotment.sln

# Build debug (uses FakeMachine, no auth required)
dotnet build allotment/allotment.csproj -c Debug

# Build release
dotnet build allotment/allotment.csproj -c Release

# Run locally in development mode
dotnet run --project allotment/allotment.csproj

# Run tests
dotnet test AllotmentTests/AllotmentTests.csproj
```

### Docker Deployment

```bash
# Build and push Docker image for Raspberry Pi (ARM32v7)
./BuildAndPush.sh

# Manual Docker build
docker build \
    --build-arg IMAGE_TAG_APPEND="-bullseye-slim-arm32v7" \
    -t leepaulalexander/allotment:latest \
    -f ./allotment/Dockerfile .
```

## Key Architecture Patterns

### Machine Abstraction

The `IMachine` interface abstracts hardware operations:
- `PiMachine`: Production implementation using real GPIO pins
- `FakeMachine`: Development mock for testing without hardware

Selection is automatic based on environment:
```csharp
services.AddMachine(builder.Environment.IsDevelopment())
```

### Background Jobs

Jobs are registered in `Program.cs` and start automatically:
- `MachineStartup` - Hardware initialization
- `TempMonitor` - Temperature readings every minute
- `SolarMonitor` - Solar panel data monitoring
- `AutoPilot` - Automatic door control (every 5 mins when enabled)
- `WaterLevelMonitor` - Water level after irrigation

### Data Persistence

All data is stored as CSV files (no database):
- `TempStore` - Temperature/humidity readings
- `SolarStore` - Solar panel data
- `WaterLevelStore` - Water level readings
- `StateStore` - Application state
- `SettingsStore` - User settings
- `LogsStore` - Activity logs

Data is stored in `/data` when running in Docker (mounted volume).

## GPIO Pin Configuration

| Pin | Function | Description |
|-----|----------|-------------|
| 19 | Door Open | Activates door opening mechanism |
| 26 | Door Close | Activates door closing mechanism |
| 13 | Water Pump | Controls irrigation pump |
| 6 | Water Sensor Power | Powers pressure sensor |
| 12 | DHT22 | Temperature/humidity sensor (1-Wire) |

## Configuration

### Development Mode (`appsettings.Development.json`)
- Authentication disabled
- Uses `FakeMachine` (no real hardware)
- Detailed error pages enabled
- Verbose logging

### Production Mode (`appsettings.json`)
- Azure AD authentication required
- Uses `PiMachine` (real GPIO)
- Production logging levels

Key configuration sections:
- `AllotmentOptions:Auth` - Authentication settings
- `AzureAd` - Azure AD/Entra ID configuration
- `Logging` - Log level configuration

## API Endpoints

Minimal REST API:
- `GET /api/status` - Returns current machine status (doors, water, temperature, humidity)

## Code Conventions

1. **Namespace**: All code under `Allotment` namespace
2. **Nullable**: Enabled project-wide
3. **Implicit usings**: Enabled
4. **Dependency Injection**: Used throughout for testability
5. **Async patterns**: Background jobs use async/await
6. **Interface abstraction**: Hardware operations abstracted via interfaces

## Testing

- Framework: MSTest with Moq for mocking
- Test project: `AllotmentTests/`
- Coverage: Minimal (acknowledged as area for improvement)

```bash
dotnet test AllotmentTests/AllotmentTests.csproj -v normal
```

## Important Files for Common Tasks

| Task | Key Files |
|------|-----------|
| Add new sensor | `Machine/Readers/`, `Machine/Monitoring/` |
| Modify door behavior | `Machine/MachineControlService.cs`, `Machine/AutoPilot.cs` |
| Add UI page | `Pages/`, `wwwroot/js/site.js` |
| Change data storage | `DataStores/` |
| Modify startup | `Program.cs` |
| Add background job | `Jobs/`, register in `Program.cs` |
| Configure settings | `AppSettingsConfig/`, `appsettings.json` |

## Known Limitations

This is a personal "skunk works" project, so production-quality features are minimal:
- No CI/CD pipeline
- Limited unit test coverage
- No code quality analysis (SonarQube)
- File-based storage instead of proper database
- Manual deployment process

## ESP32 Pressure Sensor (PlatformIO)

Located in `Sensors/PressureSensor/`:
- C++ code for Arduino/ESP32
- Reads water pressure to calculate tank level
- Communicates via MQTT
- Build with PlatformIO (`platformio.ini`)
