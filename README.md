# RevitChecklist
Quality checklist app for Revit, storing all data as JSON in DataStorage elements.

This repository contains a minimal prototype with a C# `HttpListener` server and
a very simple client. The real implementation is expected to run inside Revit
and use Revit's APIs to store checklist data. The current code is a standalone
skeleton illustrating the overall structure.

## Building the server

```
cd src
dotnet build Server/ChecklistServer/ChecklistServer.csproj -c Release
```

Run the server (for development only):

```
dotnet run --project src/Server/ChecklistServer/ChecklistServer.csproj
```

Open `src/Client/index.html` in a browser to interact with the server.

## Building the Revit add-in

```
dotnet build src/Addin/ChecklistAddin.csproj -c Release
```

The add-in starts the local server when Revit loads and provides a ribbon button
named **Checklist** that opens your default browser to `http://localhost:51789/`.
