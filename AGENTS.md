# Development Guidelines

These instructions apply to the entire repository. Consult `Spesification.md` for the full project requirements.

## General
- Keep the server lightweight using only `HttpListener` and built-in .NET/Revit APIs.
- The client must be vanilla JavaScript, HTML and CSS with **no** external frameworks.
- All data is stored as JSON inside Revit `DataStorage` elements; do not add another storage mechanism.
- The server targets both .NET Framework 4.8 and .NET 8. Ensure compatibility is maintained when modifying `ChecklistServer`.

## Building
- After making changes run `dotnet build` from the repository root to make sure all projects compile.

## Additional notes
- Add new functionality according to the detailed specification in `Spesification.md`.
- Do not include third-party libraries unless explicitly allowed by the specification.
