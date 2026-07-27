# LaptopTracker — READ FIRST
## Architecture (confirmed 2026-07-26)
- PROJECT ROOT (edit here, .csproj lives here): C:\LaptopTracker\LaptopTracker\
- PUBLISH TARGET (what actually runs): C:\LaptopTracker\publish\
- A watchdog scheduled task "LaptopTracker-App" runs
  C:\HealthCheck\watchdog_laptoptracker.ps1, which loops
  dotnet C:\LaptopTracker\publish\LaptopTracker.dll --urls http://0.0.0.0:5016
- Editing source changes NOTHING live until you publish to C:\LaptopTracker\publish.
## Deploy — the ONLY correct way
- Run:  C:\LaptopTracker\deploy.ps1
- It stops the task, kills stray dotnet processes, publishes to the correct
  folder, restarts the task, and prints the live DLL timestamp.
- Confirm the printed timestamp is NEW before claiming any change is live.
## Never
- Never `dotnet publish -o publish` (relative) — that created the dead folder
  that broke this project for a full session. That folder is now deleted.
- Never trust "it should work" — verify visually through the tunnel:
  https://puzzled-plane-1cfdm0z-5016.euw.devtunnels.ms
- Never edit: backup_views_*, manual_fix_backup_*, _root_cshtml_dump\
## Access
- Users reach the app ONLY via the dev tunnel. Windows Auth cannot work
  through it — identity is KID entry stored in session.
