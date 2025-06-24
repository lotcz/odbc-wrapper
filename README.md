# ODBC REST API Wrapper

This project is a .NET 9 Web API server that exposes a REST API endpoint. It accepts SQL queries via HTTP, executes them against an ODBC data source, and returns the results as JSON.

## Features
- Accepts SQL queries via REST POST requests
- Executes queries using ODBC
- Returns results as JSON

## Getting Started
1. Ensure you have .NET 9 SDK installed.
2. Configure your ODBC data source in `appsettings.json` or via environment variables.
3. Build and run the project:
   ```
   dotnet build
   dotnet run
   ```
4. Use a REST client to POST SQL queries to the API endpoint.

## Security Warning
**Do not expose this API to untrusted networks without proper authentication and input validation. Executing arbitrary SQL is dangerous!**

## Publish

    dotnet publish -c Release -r win-x86 --self-contained false -o ./publish