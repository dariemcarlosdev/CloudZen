# N8N Appointment Verify Action Workflow

**Date:** 2026-06-02
**Status:** PENDING IMPLEMENTATION
**Related:** `appointment-verify-booking-changes.md`

---

## Overview

This document describes the n8n workflow required to handle the `action: "verify"` request from the Azure Function proxy. The verify action checks if a booking exists for the given `bookingId` and `email`.

---

## Request Flow

```
Azure Function (/api/book-appointment)
    │
    ▼
POST to n8n Webhook
    │
    ▼
[Verify Action Workflow]
    │
    ├── Switch (action = "verify")
    │
    ├── Database Query (PostgreSQL)
    │   └── SELECT * FROM appointments
    │       WHERE booking_id = $bookingId
    │         AND email = $email
    │
    ├── IF found → Return success
    │   └── { success: true, bookingId, message: "Booking verified", ... }
    │
    └── IF not found → Return failure
        └── { success: false, message: "Booking not found" }
```

---

## Required Nodes

### 1. Webhook Node
- **Name:** `Webhook - Appointment Booking`
- **Path:** `appointment-booking`
- **Method:** POST
- **Response:** JSON

### 2. Switch Node (Action Routing)
- **Name:** `Route by Action`
- **Expression:** `{{ $json.action }}`
- **Cases:**
  - `book` → Book workflow
  - `cancel` → Cancel workflow
  - `reschedule` → Reschedule workflow
  - **`verify`** → **This workflow (NEW)**

### 3. PostgreSQL Node (Database Lookup)
- **Name:** `Verify - Query Appointment`
- **Operation:** Execute Query
- **Query:**
```sql
SELECT 
    id,
    booking_id,
    email,
    name,
    phone,
    business_name,
    appointment_date,
    start_time,
    end_time,
    status,
    created_at
FROM appointments
WHERE booking_id = $1 AND email = $2
LIMIT 1
```
- **Parameters:**
  - `$1` → `{{ $json.bookingId }}`
  - `$2` → `{{ $json.email }}`

### 4. IF Node (Check Results)
- **Name:** `Verify - Found Check`
- **Condition:** `{{ $json.booking_id }}` is not empty

### 5. Set Node (Success Response)
- **Name:** `Verify - Success Response`
- **Properties:**
```json
{
  "success": true,
  "message": "Booking verified",
  "bookingId": "{{ $json.booking_id }}",
  "name": "{{ $json.name }}",
  "email": "{{ $json.email }}",
  "date": "{{ $json.appointment_date }}",
  "time": "{{ $json.start_time }}",
  "endTime": "{{ $json.end_time }}",
  "status": "{{ $json.status }}"
}
```

### 6. Set Node (Failure Response)
- **Name:** `Verify - Not Found Response`
- **Properties:**
```json
{
  "success": false,
  "message": "We couldn't find an appointment with that Booking ID. Please check your confirmation email for the correct Booking ID and try again."
}
```

### 7. Merge Node
- **Name:** `Merge Responses`
- **Mode:** Manual
- **Inputs:** Success path + Failure path

### 8. Webhook Response Node
- **Name:** `Return Response`
- **Response:** JSON

---

## JSON Response Formats

### Success (Found)
```json
{
  "success": true,
  "message": "Booking verified",
  "bookingId": "APT-MN7O3825-TMVP",
  "name": "John Doe",
  "email": "john@example.com",
  "date": "2026-06-15",
  "time": "14:00",
  "endTime": "14:30",
  "status": "confirmed"
}
```

### Failure (Not Found)
```json
{
  "success": false,
  "message": "We couldn't find an appointment with that Booking ID. Please check your confirmation email for the correct Booking ID and try again."
}
```

---

## Implementation Checklist

- [ ] Add verify route in Switch node (if not present)
- [ ] Create database query for booking lookup
- [ ] Add success response node
- [ ] Add failure response node
- [ ] Test with valid booking ID + email
- [ ] Test with invalid booking ID
- [ ] Test with valid booking ID + wrong email

---

## Error Handling

| Error | Response |
|-------|----------|
| Database connection failed | `{ success: false, message: "Our booking system is temporarily unavailable. Please try again later." }` |
| Query timeout | `{ success: false, message: "The request took too long. Please try again." }` |
| Invalid input (missing fields) | Azure Function validates before reaching n8n |

---

## Testing

### Test 1: Valid Booking
```bash
curl -X POST https://your-n8n-webhook/webhook/appointment-booking \
  -H "Content-Type: application/json" \
  -d '{
    "action": "verify",
    "bookingId": "APT-MN7O3825-TMVP",
    "email": "test@example.com"
  }'
```

**Expected:** `{ "success": true, ... }`

### Test 2: Invalid Booking
```bash
curl -X POST https://your-n8n-webhook/webhook/appointment-booking \
  -H "Content-Type: application/json" \
  -d '{
    "action": "verify",
    "bookingId": "APT-INVALID-1234",
    "email": "test@example.com"
  }'
```

**Expected:** `{ "success": false, "message": "Booking not found" }`

---

## Related Documentation

- [Azure Functions - Book Appointment](../01-architecture/AZURE_FUNCTIONS.md)
- [Appointment System Overview](../03-features/02_FEATURE_APPOINTMENT_SYSTEM.md)
- [Appointment Verify Changes Planning](../07-planning/appointment-verify-booking-changes.md)