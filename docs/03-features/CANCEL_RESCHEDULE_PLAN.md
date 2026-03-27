# Cancel & Reschedule Appointment — Implementation Plan

## Status: ✅ Implemented

## Problem

The booking feature only supports `action: "book"`. Users need **cancel** and **reschedule** capabilities. The N8N workflow already has a Switch node that routes by `action`, but the WASM frontend and Azure Function only send `"book"`.

## Pattern: Action Discriminator + N8N Switch Router

Single endpoint `/api/book-appointment`, single Azure Function. The `action` field discriminates intent. The Function validates per-action, transforms to N8N schema, and forwards.

```
WASM UI ──→ /api/book-appointment ──→ Azure Function ──→ N8N Switch Node
                                        (validate)         ├─ "book"       → Create event
                                        (transform)        ├─ "cancel"     → Delete event
                                                           └─ "reschedule" → Update event
```

## N8N Expected Payload (Route by Action Node)

All 3 actions use the **same JSON shape** — unused fields are empty strings:

```json
{
  "action": "book | cancel | reschedule",
  "bookingId": "",
  "userName": "",
  "userEmail": "",
  "userPhone": "",
  "appointmentDate": "YYYY-MM-DD",
  "appointmentTime": "HH:mm",
  "appointmentReason": "",
  "startDateTime": "YYYY-MM-DDThh:mm:ss",
  "endDateTime": "YYYY-MM-DDThh:mm:ss",
  "newDate": "",
  "newTime": "",
  "newStartDateTime": "",
  "newEndDateTime": ""
}
```

### Required Fields Per Action

| Field              | book | cancel | reschedule |
|--------------------|:----:|:------:|:----------:|
| `action`           | ✅   | ✅     | ✅         |
| `bookingId`        | —    | ✅     | ✅         |
| `userName`         | ✅   | —      | —          |
| `userEmail`        | ✅   | ✅     | ✅         |
| `userPhone`        | ✅   | —      | —          |
| `appointmentDate`  | ✅   | —      | —          |
| `appointmentTime`  | ✅   | —      | —          |
| `appointmentReason`| ✅   | —      | —          |
| `startDateTime`    | ✅   | —      | —          |
| `endDateTime`      | ✅   | —      | —          |
| `newDate`          | —    | —      | ✅         |
| `newTime`          | —    | —      | ✅         |
| `newStartDateTime` | —    | —      | ✅         |
| `newEndDateTime`   | —    | —      | ✅         |

## Field Mapping Gap (Current WASM/API → N8N)

| Current Model       | N8N Expected        | Transform              |
|---------------------|---------------------|------------------------|
| `name`              | `userName`          | Rename in proxy        |
| `email`             | `userEmail`         | Rename in proxy        |
| `phone`             | `userPhone`         | Rename in proxy        |
| `date`              | `appointmentDate`   | Rename in proxy        |
| `time`              | `appointmentTime`   | Rename in proxy        |
| `reason`            | `appointmentReason` | Rename in proxy        |
| `date` + `time`     | `startDateTime`     | Compute in proxy       |
| `date` + `endTime`  | `endDateTime`       | Compute in proxy       |
| `businessName`      | *(not in N8N)*      | Drop in proxy          |
| *(missing)*         | `bookingId`         | Add to WASM model      |
| *(missing)*         | `newDate/Time/...`  | Add to WASM model      |

> **Transform happens in Azure Function** — it's the proxy layer. WASM keeps user-friendly field names.

## UX: Separate Interfaces Per Action

| Action         | Steps | User Input                          | Reused Components            |
|---------------|-------|-------------------------------------|------------------------------|
| **Book**       | 3     | Date/time → form → confirm          | Calendar, TimeSlots, Sidebar |
| **Cancel**     | 1     | Email + BookingId → confirm         | Sidebar                      |
| **Reschedule** | 2     | Email + BookingId → new date/time   | Calendar, TimeSlots, Sidebar |

## Implementation Tasks

### 1. ✅ Align API Model with N8N Schema
- Created `N8nAppointmentPayload` class in `Api/Features/Booking/` matching N8N JSON exactly
- Updated `BookAppointmentRequest` to add `bookingId`, `newDate`, `newTime`, `newEndTime` fields
- Added WASM→N8N transformation in `BookAppointmentFunction` via `TransformToN8nPayload()`
- Compute `startDateTime`/`endDateTime` from `date` + `time`/`endTime` in factory methods
- Conditional validation per `action` value via `ValidateRequest()` with action-specific validators

### 2. ✅ WASM Request Model Updates
- Added `bookingId`, `newDate`, `newTime`, `newEndTime` to `BookingAppointmentRequest`
- Extended `BookingResult` with `IsNotFound` flag and `NotFound()`, `Ok()` factory methods

### 3. ✅ WASM Service Layer
- Added `CancelAppointmentAsync()` + `RescheduleAppointmentAsync()` to `IAppointmentService`
- Same endpoint, different `action` values — implemented in `AppointmentService.SendRequestAsync()`

### 4. ✅ Cancel UI — `ManageAppointmentCancel.razor`
- Simple form: email + bookingId → confirm
- Error states: not found, already cancelled, network error
- Success confirmation with booking ID display

### 5. ✅ Reschedule UI — `ManageAppointmentReschedule.razor`
- 2-step: enter email+bookingId → select new date/time (reuses `BookingCalendar` + `BookingTimeSlots`)
- Shows old → new time on confirmation

### 6. ✅ Routing & Navigation
- Added route `/manage-appointment` in `Pages/ManageAppointment.razor`
- Added "Manage Appointment" link from `BookingConfirmation`
- Tab navigation between Cancel and Reschedule flows

### 7. Documentation Updates
- Updated this file with implementation status
- TODO: Update `API_ENDPOINTS.md`, `01_azure_functions_proxy_api.md`, `VERTICAL_SLICE_ARCHITECTURE.md`

## Architecture Notes

- **No new Azure Function** — single function, `action` discriminator
- **`businessName`** not in N8N payload — dropped during transformation
- **BookingId format**: `APT-XXXXXXXX-XXXX` (N8N generates on book)
- **N8N owns business logic** (Google Calendar CRUD, Twilio, email) — Azure Function is purely a validating proxy

## Duplicate Model Issue

Both `BookingAppointmentRequest` (WASM) and `BookAppointmentRequest` (API) are nearly identical but:
- They live in separate .NET projects that **cannot share references** (WASM = browser, API = server)
- Neither matches the N8N JSON field names — the Azure Function currently forwards WASM field names as-is
- **Resolution**: Keep WASM model user-friendly. The Azure Function transforms to `N8nAppointmentPayload` before forwarding. This is the correct proxy pattern — the proxy layer owns the translation.

## Related Docs

- [API Endpoints](../01-architecture/API_ENDPOINTS.md)
- [Azure Functions Proxy Pattern](../06-patterns/01_azure_functions_proxy_api.md)
- [Vertical Slice Architecture](../01-architecture/VERTICAL_SLICE_ARCHITECTURE.md)
- [Component Architecture](../01-architecture/COMPONENT_ARCHITECTURE.md)
