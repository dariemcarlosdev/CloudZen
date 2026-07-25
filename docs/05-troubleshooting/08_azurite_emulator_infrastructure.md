# Issue #8: Azurite Storage Emulator Not Running

## Quick Description
Azure Function fails with storage-related errors or connection refused to `127.0.0.1:10000`.

## Why This Issue Happens
The project uses `"AzureWebJobsStorage": "UseDevelopmentStorage=true"` which requires Azurite to be running.

## Resolution

**Option 1: Start via Visual Studio**
Visual Studio automatically starts Azurite when debugging Azure Functions projects.

**Option 2: Start manually**
```bash
# Install if needed
npm install -g azurite

# Start
azurite --silent --location c:\azurite
```

**Option 3: Use VS Code extension**
Install the "Azurite" extension and start from the command palette.
