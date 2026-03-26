# Testing Azure Functions Locally

This guide explains how to test the **CloudZen.Api** Azure Functions project locally using **Postman**.

## Prerequisites

### 1. Postman

Download and install Postman from: https://www.postman.com/downloads/

### 2. Azure Functions Core Tools

Install Azure Functions Core Tools v4 (required for .NET 8):

**Option A - winget (Recommended for Windows):**
```powershell
winget install Microsoft.Azure.FunctionsCoreTools
```

**Option B - npm:**
```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

**Option C - Chocolatey:**
```powershell
choco install azure-functions-core-tools
```

**Option D - Direct Download:**
https://go.microsoft.com/fwlink/?linkid=2174087

### 3. Azurite (Local Storage Emulator)

The project uses `"AzureWebJobsStorage": "UseDevelopmentStorage=true"`, which requires Azurite.

**Option A - Visual Studio:**
Azurite is included with Visual Studio and starts automatically when debugging.

**Option B - npm:**
```bash
npm install -g azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

**Option C - VS Code Extension:**
Install the "Azurite" extension from the VS Code marketplace.

---

## Configuration

### local.settings.json

Ensure your `Api/local.settings.json` file is configured:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
        "BREVO_API_KEY": "your-brevo-api-key-here",
        "EmailSettings:FromEmail": "your-email@example.com",
        "EmailSettings:CcEmail": "cc-email@example.com",
        "RateLimiting:PermitLimit": "10",
        "RateLimiting:WindowSeconds": "60",
        "RateLimiting:QueueLimit": "0",
        "RateLimiting:InactivityTimeoutMinutes": "5",
        "RateLimiting:EnableCircuitBreaker": "false",
        "RateLimiting:CircuitBreakerFailureThreshold": "5",
        "RateLimiting:CircuitBreakerDurationSeconds": "30"
    }
}
```

> ⚠️ **Security Note:** Never commit `local.settings.json` with real API keys to source control. Ensure it's in your `.gitignore` file.

---

## Running the Function

### Option 1: Visual Studio (Recommended)

1. Open the solution in Visual Studio
2. Right-click on `CloudZen.Api` project → **Set as Startup Project**
3. Press **F5** or click the green **Start** button
4. Visual Studio will:
   - Start Azurite automatically
   - Build the project
   - Launch the Azure Functions host
   - Display the function URL in the output window

### Option 2: Command Line

```powershell
# Navigate to the Api folder
cd "C:\DATA\MYSTUFFS\SIDE PROJECTS\CloudZen\Api"

# Build the project
dotnet build

# Start the function
func start
```

### Expected Output

When running successfully, you should see:

```
Azure Functions Core Tools
Core Tools Version:       4.x.x
Function Runtime Version: 4.x.x

Functions:
    SendEmail: [POST] http://localhost:7071/api/send-email

For detailed output, run func with --verbose flag.
```

---

## End-to-End Integration Testing (Blazor + Azure Function)

This section explains how to test the complete flow from the Blazor WebAssembly contact form to the Azure Function backend.

### Architecture Overview

```
┌─────────────────────────┐         ┌─────────────────────────┐
│   Blazor WebAssembly    │         │    Azure Function       │
│   (Frontend)            │         │    (Backend API)        │
│                         │         │                         │
│   ContactForm.razor     │  HTTP   │   SendEmailFunction.cs  │
│         │               │ ──────► │         │               │
│         ▼               │  POST   │         ▼               │
│   ApiEmailService.cs    │         │    Brevo API            │
│                         │         │         │               │
│   localhost:5001        │         │         ▼               │
│                         │         │    Email Sent           │
└─────────────────────────┘         └─────────────────────────┘
                                     localhost:7071
```

### Step 1: Start Both Projects

#### Option A: Visual Studio Multiple Startup Projects (Recommended)

1. Right-click on the **Solution** in Solution Explorer
2. Select **Configure Startup Projects...**
3. Choose **Multiple startup projects**
4. Set both projects to **Start**:

   | Project | Action |
   |---------|--------|
   | CloudZen | Start |
   | CloudZen.Api | Start |

5. Click **OK**
6. Press **F5** to start both projects

#### Option B: Two Terminal Windows

**Terminal 1 - Start Azure Function:**
```powershell
cd "C:\DATA\MYSTUFFS\SIDE PROJECTS\CloudZen\Api"
dotnet build
func start
```

**Terminal 2 - Start Blazor App:**
```powershell
cd "C:\DATA\MYSTUFFS\SIDE PROJECTS\CloudZen"
dotnet run
```

### Step 2: Verify Both Are Running

| Component | URL | Status Check |
|-----------|-----|--------------|
| Azure Function | http://localhost:7071/api/send-email | Terminal shows "Functions: SendEmail" |
| Blazor App | https://localhost:5001 (or similar) | Browser opens the app |

### Step 3: Test the Contact Form

1. Open the Blazor app in your browser (https://localhost:5001)
2. Navigate to the **Contact** section (scroll down or navigate to the contact form)
3. Fill in the form:
   - **Name:** Test User
   - **Email:** test@example.com
   - **Subject:** Integration Test
   - **Message:** This is a test message from the Blazor contact form.
4. Click **Send Message**
5. **Expected Results:**
   - Success toast/message appears in the Blazor app
   - Azure Function terminal shows the request being processed
   - Email is sent (if Brevo API key is valid)

### Step 4: Monitor the Azure Function Logs

Watch the Azure Function terminal for logs:

```
[2024-XX-XX] Executing 'Functions.SendEmail' (Reason='This function was programmatically called via the host APIs.')
[2024-XX-XX] SendEmail function triggered from 127.0.0.1
[2024-XX-XX] Email sent successfully. MessageId: <abc123>
[2024-XX-XX] Executed 'Functions.SendEmail' (Succeeded)
```

### Step 5: Test Error Scenarios

#### Test Validation Errors

1. Leave the **Name** field empty
2. Click **Send Message**
3. **Expected:** Error message "Name is required" appears in the Blazor app

#### Test Rate Limiting

1. Submit the form rapidly (more than 10 times in 60 seconds)
2. **Expected:** After 10 submissions, you see "Rate limit exceeded" error

#### Test Network Error

1. Stop the Azure Function (Ctrl+C in terminal)
2. Try to submit the form in Blazor
3. **Expected:** Error message about connection failure

---

## Testing with Postman (API Only)

### Setting Up Postman

### Import or Create Collection

1. Open Postman
2. Click **Collections** in the sidebar
3. Click **+** to create a new collection
4. Name it: `CloudZen API - Local`

### Create Environment Variables

1. Click **Environments** in the sidebar
2. Click **+** to create a new environment
3. Name it: `CloudZen Local`
4. Add the following variables:

| Variable | Initial Value | Current Value |
|----------|---------------|---------------|
| `baseUrl` | `http://localhost:7071/api` | `http://localhost:7071/api` |
| `requestCount` | `0` | `0` |

5. Click **Save**
6. Select `CloudZen Local` from the environment dropdown (top-right)

---

## Testing the API with Postman

### Available Endpoints

| Function | Method | URL | Description |
|----------|--------|-----|-------------|
| SendEmail | POST | `{{baseUrl}}/send-email` | Send email via Brevo API |

### Create SendEmail Request

1. In your `CloudZen API - Local` collection, click **Add request**
2. Name it: `Send Email`
3. Configure the request:

**Method:** `POST`

**URL:** `{{baseUrl}}/send-email`

**Headers Tab:**
| Key | Value |
|-----|-------|
| `Content-Type` | `application/json` |

**Body Tab:**
- Select **raw**
- Select **JSON** from the dropdown
- Enter:
```json
{
    "fromName": "John Doe",
    "fromEmail": "john@example.com",
    "subject": "Test Subject",
    "message": "This is a test message from the contact form."
}
```

4. Click **Save**
5. Click **Send**

---

## Expected Responses in Postman

### Success (200 OK)

**Status:** `200 OK`

**Body:**
```json
{
    "success": true,
    "message": "Email sent successfully.",
    "messageId": "abc123..."
}
```

### Validation Error (400 Bad Request)

**Status:** `400 Bad Request`

**Body:**
```json
{
    "error": "Name is required."
}
```

### Rate Limited (429 Too Many Requests)

**Status:** `429 Too Many Requests`

**Body:**
```json
{
    "error": "Rate limit exceeded. Try again in 60 seconds."
}
```

**Headers Tab (Response):**
```
Retry-After: 60
```

### Server Error (500 Internal Server Error)

**Status:** `500 Internal Server Error`

**Body:**
```json
{
    "error": "Email service is not configured properly."
}
```

---

## Testing Validation with Postman

The API validates all input fields:

| Field | Rules |
|-------|-------|
| `fromName` | Required, max 100 characters, no dangerous content |
| `fromEmail` | Required, valid email format, max 254 characters |
| `subject` | Required, max 200 characters, no dangerous content |
| `message` | Required, max 5000 characters, no dangerous content |

### Test Invalid Email

1. Create a new request named `Test - Invalid Email`
2. **Method:** `POST`
3. **URL:** `{{baseUrl}}/send-email`
4. **Headers:** `Content-Type: application/json`
5. **Body:**
```json
{
    "fromName": "Test",
    "fromEmail": "invalid-email",
    "subject": "Test",
    "message": "Test"
}
```
6. Click **Send**
7. **Expected Response:** `400 Bad Request`
```json
{
    "error": "Invalid email format."
}
```

### Test Empty Fields

1. Create a new request named `Test - Empty Name`
2. **Method:** `POST`
3. **URL:** `{{baseUrl}}/send-email`
4. **Headers:** `Content-Type: application/json`
5. **Body:**
```json
{
    "fromName": "",
    "fromEmail": "test@example.com",
    "subject": "Test",
    "message": "Test"
}
```
6. Click **Send**
7. **Expected Response:** `400 Bad Request`
```json
{
    "error": "Name is required."
}
```

### Test Missing Body

1. Create a new request named `Test - Empty Body`
2. **Method:** `POST`
3. **URL:** `{{baseUrl}}/send-email`
4. **Headers:** `Content-Type: application/json`
5. **Body:** Leave empty or set to `{}`
6. Click **Send**
7. **Expected Response:** `400 Bad Request`
```json
{
    "error": "Request body is required."
}
```

### Test Message Too Long

1. Create a new request named `Test - Message Too Long`
2. **Method:** `POST`
3. **URL:** `{{baseUrl}}/send-email`
4. **Headers:** `Content-Type: application/json`
5. **Body:** (message exceeds 5000 characters)
```json
{
    "fromName": "Test",
    "fromEmail": "test@example.com",
    "subject": "Test",
    "message": "A very long message... (repeat until > 5000 chars)"
}
```
6. Click **Send**
7. **Expected Response:** `400 Bad Request`

---

## Testing Rate Limiting with Postman

The default configuration allows 10 requests per 60 seconds per client IP.

### Method 1: Manual Testing

1. Open the `Send Email` request
2. Click **Send** rapidly more than 10 times within 60 seconds
3. **Expected Results:**
   - First 10 requests: **200 OK**
   - Requests 11+: **429 Too Many Requests**

### Method 2: Using Collection Runner

1. **Save your request** to the `CloudZen API - Local` collection
2. Click the **three dots (...)** next to your collection
3. Select **Run collection**
4. Configure the runner:
   - **Iterations:** `15`
   - **Delay:** `100 ms` (or `0 ms` for faster testing)
5. Click **Run CloudZen API - Local**
6. **Expected Results:**
   - First 10 iterations: **200 OK** (green)
   - Iterations 11-15: **429 Too Many Requests** (orange/red)

### Method 3: Using Pre-request Script

Add this script to track request numbers:

1. Open your `Send Email` request
2. Go to **Scripts** tab → **Pre-request**
3. Add:
```javascript
// Increment request counter
let requestCount = pm.environment.get("requestCount") || 0;
requestCount++;
pm.environment.set("requestCount", requestCount);
console.log("📤 Request #" + requestCount);
```

4. Go to **Scripts** tab → **Post-response**
5. Add:
```javascript
// Log response status
let requestCount = pm.environment.get("requestCount");
console.log("📥 Request #" + requestCount + " - Status: " + pm.response.code);

if (pm.response.code === 429) {
    console.log("⚠️ Rate limit hit! Retry-After: " + pm.response.headers.get("Retry-After") + " seconds");
}
```

6. Open **Console** (View → Show Postman Console) to see the logs
7. Run the Collection Runner with 15 iterations

### Reset Rate Limit Counter

To reset the request counter in Postman:

1. Go to **Environments**
2. Select `CloudZen Local`
3. Set `requestCount` current value to `0`
4. Click **Save**

To reset the server-side rate limit:
- Wait 60 seconds for the window to reset
- Or restart the Azure Function

---

## Postman Test Scripts (Automated Testing)

Add these test scripts to automate response validation.

### Tests for Success Response

Go to **Scripts** tab → **Post-response** and add:

```javascript
// Test for successful email send
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has success flag", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.eql(true);
});

pm.test("Response has messageId", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.messageId).to.exist;
});

pm.test("Response time is less than 5000ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(5000);
});
```

### Tests for Validation Errors

Create a separate test request with these scripts:

```javascript
// Test for validation error
pm.test("Status code is 400", function () {
    pm.response.to.have.status(400);
});

pm.test("Response has error message", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.error).to.exist;
});
```

### Tests for Rate Limiting

```javascript
// Test for rate limiting
if (pm.response.code === 429) {
    pm.test("Rate limit response is 429", function () {
        pm.response.to.have.status(429);
    });
    
    pm.test("Retry-After header exists", function () {
        pm.expect(pm.response.headers.get("Retry-After")).to.exist;
    });
    
    pm.test("Error message indicates rate limit", function () {
        var jsonData = pm.response.json();
        pm.expect(jsonData.error).to.include("Rate limit");
    });
}
```

---

## Integration Test Checklist

Use this checklist to verify everything works together:

### Setup Verification
- [ ] Azure Functions Core Tools installed (`func --version`)
- [ ] Azurite running or Visual Studio started
- [ ] `local.settings.json` configured with valid Brevo API key
- [ ] `wwwroot/appsettings.Development.json` has `ApiBaseUrl` set

### Azure Function Tests
- [ ] Function starts without errors
- [ ] Postman can reach `http://localhost:7071/api/send-email`
- [ ] Valid request returns `200 OK`
- [ ] Invalid email returns `400 Bad Request`
- [ ] Empty name returns `400 Bad Request`
- [ ] Rate limiting triggers after 10 requests

### Blazor Integration Tests
- [ ] Blazor app starts and loads
- [ ] Contact form is visible and interactive
- [ ] Form validation works (client-side)
- [ ] Successful submission shows success message
- [ ] Invalid input shows error message
- [ ] Network error (API down) shows appropriate error

### Email Delivery Tests
- [ ] Email appears in configured inbox
- [ ] Email content matches form input
- [ ] HTML formatting is correct
- [ ] CC recipient receives email (if configured)

---

## Troubleshooting

### CORS Error in Browser Console

**Symptoms:** Browser console shows "CORS policy" error when Blazor calls the API.

**Cause:** The Azure Function is not allowing requests from the Blazor app origin.

**Solution:** The CORS is already configured in `Program.cs`. Ensure:
1. The Azure Function is running in Development mode
2. The Blazor app is running on one of the allowed origins:
   - `https://localhost:5001`
   - `https://localhost:7001`
   - `http://localhost:5000`

### Blazor App Can't Connect to API

**Symptoms:** "Unable to connect to email service" error in Blazor app.

**Cause:** API URL misconfigured or Azure Function not running.

**Solution:**
1. Verify Azure Function is running at `http://localhost:7071`
2. Check `wwwroot/appsettings.Development.json` exists with:
   ```json
   {
     "ApiBaseUrl": "http://localhost:7071/api"
   }
   ```
3. Restart the Blazor app after creating/modifying the config

### Error: `Could not send request` or `ECONNREFUSED`

**Cause:** The Azure Function is not running.

**Solution:** 
1. Start the function using Visual Studio (F5) or `func start` command
2. Verify the function is running by checking the terminal output
3. Ensure the URL in Postman matches the function URL (default: `http://localhost:7071`)

### Error: `func` command not found

**Cause:** Azure Functions Core Tools not installed or not in PATH.

**Solution:** Install Azure Functions Core Tools and restart your terminal.

### Error: `TimeSpan string could not be parsed`

**Cause:** Invalid TimeSpan format in `host.json`.

**Solution:** Use proper TimeSpan format (e.g., `"365.00:00:00"` instead of `"31536000"`).

### Error: `Brevo API key is not configured`

**Cause:** Missing or invalid `BREVO_API_KEY` in `local.settings.json`.

**Solution:** Add a valid Brevo API key to your configuration.

### Error: `File is locked by .NET Host`

**Cause:** Another instance of the function is running.

**Solution:** Stop all running instances:
```powershell
Get-Process -Name "func" -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Azurite Not Running

**Symptoms:** Errors related to storage or connection refused to 127.0.0.1:10000.

**Solution:** Start Azurite manually:
```bash
azurite --silent --location c:\azurite
```

### Postman Shows "Could not get response"

**Possible Causes:**
1. Function not running
2. Wrong URL or port
3. Firewall blocking the connection

**Solutions:**
1. Verify the function is running in Visual Studio or terminal
2. Check the URL matches exactly (including port 7071)
3. Try disabling firewall temporarily for testing

---

## Debugging Tips

### Enable Verbose Logging

Run with verbose output:
```powershell
func start --verbose
```

### Check Application Insights

Logs are captured via Application Insights when configured. Check the Output window in Visual Studio for detailed logs.

### Test Without Email Sending

To test the API validation without actually sending emails, use an invalid API key. The function will validate input and return appropriate errors before attempting to send.

### Use Postman Console

1. Go to **View** → **Show Postman Console**
2. The console shows detailed request/response information
3. Useful for debugging script errors and viewing logs

### Check Browser Developer Tools

1. Open browser DevTools (F12)
2. Go to **Network** tab
3. Submit the contact form
4. Look for the `send-email` request
5. Check **Headers**, **Payload**, and **Response** tabs

---

## Exporting Postman Collection

To share your test collection:

1. Click the **three dots (...)** next to your collection
2. Select **Export**
3. Choose **Collection v2.1**
4. Save the JSON file
5. Commit to your repository (optional)

To import:
1. Click **Import** in Postman
2. Select the exported JSON file

---

## Security Considerations

1. **Never commit API keys** - Ensure `local.settings.json` is in `.gitignore`
2. **Rotate compromised keys** - If you accidentally commit a key, regenerate it immediately
3. **Use environment variables** - In production, use Azure Key Vault or App Settings
4. **Test rate limiting** - Verify rate limiting works before deploying to production
5. **Don't share Postman environments** with real API keys

---

## Related Documentation

- [Postman Learning Center](https://learning.postman.com/)
- [Azure Functions Core Tools Reference](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite Storage Emulator](https://docs.microsoft.com/azure/storage/common/storage-use-azurite)
- [Brevo (Sendinblue) API Documentation](https://developers.brevo.com/)
- [Azure Functions .NET Isolated Process](https://docs.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Blazor WebAssembly Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
