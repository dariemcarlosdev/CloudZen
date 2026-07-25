# Appointment Verify Booking Changes - Planning

**Date:** 2026-06-02
**Status:** Azure Function ✅ COMPLETE | N8N Workflow 🔲 PENDING

## Overview

Added booking ID verification before allowing users to proceed to reschedule an appointment. The verification now follows the same Request/Service pattern as BookAsync, CancelAsync, and RescheduleAsync.

## Changes Implemented

### ✅ Completed (Blazor/Frontend)

| File | Change |
|------|--------|
| `Features/Booking/Models/AppointmentRequests.cs` | Added `VerifyBookingRequest` record |
| `Features/Booking/Services/IAppointmentService.cs` | Added `VerifyBookingExistsAsync(VerifyBookingRequest request)` |
| `Features/Booking/Services/AppointmentService.cs` | Implemented verification logic |
| `Features/Booking/Components/ManageAppointmentReschedule.razor.cs` | Updated to call verification |
| `Features/Booking/Components/ManageAppointmentReschedule.razor` | Added loading spinner state |

### ✅ Completed (Azure Functions API)

| File | Change |
|------|--------|
| `Api/Features/Booking/BookAppointmentRequest.cs` | Added "verify" to action documentation |
| `Api/Features/Booking/BookAppointmentFunction.cs` | Added "verify" to validActions array |
| `Api/Features/Booking/BookAppointmentFunction.cs` | Added `ValidateVerifyAction` method |

---

## Pending Changes Checklist

### ✅ Azure Functions / API Side - COMPLETED (2026-06-02)

- [x] **Add "verify" to valid actions** — `BookAppointmentFunction.cs` line 233: `var validActions = new[] { "book", "cancel", "reschedule", "verify" };`
- [x] **Add validation for verify action** — `ValidateVerifyAction` method (lines 343-354)
- [x] **Pass to n8n** — Request body forwarded directly to n8n webhook

### 🔲 N8N Workflow Side - PENDING

- [ ] **Add verify action handler** — Create or update n8n workflow to handle `action: "verify"`
- [ ] **Database lookup** — Add database node to query appointments table by bookingId + email
- [ ] **Return success when found** — Return `{ success: true, message: "Booking verified" }`
- [ ] **Return not found when missing** — Return `{ success: false, message: "Booking not found" }`
- [ ] **Test verify workflow** — Verify n8n returns correct response

---

## Technical Details

### Request Flow

```
Blazor Client
    │
    ▼
AppointmentService.VerifyBookingExistsAsync()
    │
    ▼
Azure Function /api/book-appointment
    │
    ▼
n8n Webhook (verify action)
    │
    ▼
Database Query (bookingId + email)
    │
    ▼
Response: { success: true/false, message: "..." }
```

### Request Pattern

**Blazor Request:**
```csharp
public sealed record VerifyBookingRequest
{
    [JsonPropertyName("bookingId")]
    public required string BookingId { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("action")]
    public string Action => "verify";
}
```

**Azure Function validates** → Forwards to n8n → n8n queries database

### API Endpoint

Current endpoint: `/api/book-appointment` (Azure Functions proxy)

---

## Error Handling

- **Booking not found:** Display "We couldn't find an appointment with that Booking ID. Please check your confirmation email for the correct Booking ID and try again."
- **Network error:** Display "Our booking system is temporarily unreachable. Please try again in a moment."
- **Timeout:** Display "The request took too long. Please try again."

---

## Next Steps

1. ✅ Blazor changes complete (build verified) — 2026-06-02
2. ✅ Azure Function passes verify to n8n — 2026-06-02
3. 🔲 Update n8n workflow to handle verify action with database lookup
4. 🔲 Test end-to-end flow