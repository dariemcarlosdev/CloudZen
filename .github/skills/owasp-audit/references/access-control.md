# Broken Access Control (OWASP A01)

Detection patterns and remediation for the #1 OWASP risk category.

## Missing Authorization

### Detection Patterns

```csharp
// ❌ VULNERABLE: No authorization at all
[HttpGet("orders/{id}")]
public async Task<EscrowDto> GetOrder(int id) =>
    await _service.GetByIdAsync(id); // Any anonymous user can access

// ❌ VULNERABLE: Authorization present but no resource-level check
[Authorize]
[HttpGet("orders/{id}")]
public async Task<EscrowDto> GetOrder(int id) =>
    await _service.GetByIdAsync(id); // Any authenticated user can access ANY order

// ❌ VULNERABLE: Relying on UI hiding
// Button hidden in Blazor but API endpoint is unprotected
<AuthorizeView Policy="Admin">
    <button @onclick="DeleteUser">Delete</button> // UI-only protection!
</AuthorizeView>
```

### Remediation

```csharp
// ✅ SECURE: Resource-based authorization
[Authorize]
[HttpGet("orders/{id}")]
public async Task<IActionResult> GetOrder(int id)
{
    var order = await _service.GetByIdAsync(id);
    if (order is null) return NotFound();

    var authResult = await _authService.AuthorizeAsync(
        User, order, new EscrowOwnerRequirement());

    return authResult.Succeeded ? Ok(order) : Forbid();
}

// ✅ SECURE: Custom authorization handler
public sealed class EscrowOwnerHandler
    : AuthorizationHandler<EscrowOwnerRequirement, Escrow>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EscrowOwnerRequirement requirement,
        Escrow order)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (order.BuyerId.ToString() == userId ||
            order.SellerId.ToString() == userId)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

## Insecure Direct Object References (IDOR)

### Detection Patterns

```csharp
// ❌ VULNERABLE: Sequential IDs expose enumeration
[HttpGet("users/{id:int}")]
public async Task<UserDto> GetUser(int id) // Attacker: GET /users/1, /users/2, ...

// ❌ VULNERABLE: No ownership check on update
[HttpPut("orders/{id}")]
public async Task<IActionResult> UpdateEscrow(int id, UpdateEscrowDto dto)
{
    await _service.UpdateAsync(id, dto); // User A can modify User B's order
    return Ok();
}
```

### Remediation

```csharp
// ✅ SECURE: Use GUIDs or opaque identifiers
[HttpGet("users/{id:guid}")]
public async Task<UserDto> GetUser(Guid id)

// ✅ SECURE: Filter by authenticated user
[HttpGet("orders")]
public async Task<List<EscrowDto>> GetMyEscrows()
{
    var userId = User.GetUserId(); // Extension method on ClaimsPrincipal
    return await _service.GetByUserIdAsync(userId);
}

// ✅ SECURE: Verify ownership before mutation
[HttpPut("orders/{id}")]
public async Task<IActionResult> UpdateEscrow(Guid id, UpdateEscrowDto dto)
{
    var order = await _service.GetByIdAsync(id);
    if (order?.OwnerId != User.GetUserId()) return Forbid();
    await _service.UpdateAsync(id, dto);
    return Ok();
}
```

## CORS Misconfiguration

```csharp
// ❌ VULNERABLE: Allow any origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy.AllowAnyOrigin()   // Any website can make requests!
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ✅ SECURE: Explicit origin allowlist
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy.WithOrigins(
            "https://app.myapp.io",
            "https://admin.myapp.io")
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type")
        .AllowCredentials());
});
```

## Privilege Escalation

```csharp
// ❌ VULNERABLE: User can set own role
[HttpPut("users/{id}/role")]
[Authorize] // Any authenticated user can call this!
public async Task SetRole(Guid id, string role)

// ✅ SECURE: Admin-only with policy
[HttpPut("users/{id}/role")]
[Authorize(Policy = "UserAdmin")]
public async Task SetRole(Guid id, string role)

// ✅ SECURE: Policy registration with claims
services.AddAuthorizationBuilder()
    .AddPolicy("UserAdmin", policy =>
        policy.RequireClaim("role", "admin")
              .RequireClaim("scope", "user:manage"));
```

## Blazor-Specific Access Control

```csharp
// ✅ SECURE: AuthorizeRouteView in App.razor
<AuthorizeRouteView RouteData="routeData"
    DefaultLayout="typeof(MainLayout)">
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeRouteView>

// ✅ SECURE: Component-level authorization
@page "/order/admin"
@attribute [Authorize(Policy = "EscrowAdmin")]

// ✅ SECURE: Programmatic auth check in code-behind
[CascadingParameter]
private Task<AuthenticationState> AuthState { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    var state = await AuthState;
    if (!state.User.HasClaim("role", "admin"))
    {
        NavigationManager.NavigateTo("/unauthorized");
        return;
    }
    await LoadAdminDataAsync();
}
```

## Endpoint Audit Checklist

For every endpoint, verify:

| Check | Status |
|-------|--------|
| Has `[Authorize]` or justified `[AllowAnonymous]` | ☐ |
| Resource-level ownership verified (not just role) | ☐ |
| IDOR mitigated (GUIDs or ownership filter) | ☐ |
| Admin endpoints require admin policy | ☐ |
| Mutation endpoints verify ownership before write | ☐ |
| CORS configured with explicit origins | ☐ |
| Rate limiting on sensitive operations | ☐ |
