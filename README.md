# Coursach4 WPF Simulation

This app simulates 4 food kiosks at a football stadium using multithreading (`Task`).

## Features

- 4 kiosk workers in parallel
- Queue limit per kiosk (`>3` means fan leaves)
- Idle cutting vegetables (3 min per portion in simulation time)
- Serving time 6-7 min
- Rejection if no vegetables after service
- Live dashboard with kiosk states and timers
- Full event log in UI
- CSV export button that creates two files:
  - `summary_yyyyMMdd_HHmmss.csv`
  - `events_yyyyMMdd_HHmmss.csv`

## Run

```powershell
cd C:\Users\Vlad\RiderProjects\Coursach4
 dotnet build
 dotnet run --project .\Coursach4\Coursach4.csproj
```

