# Appointment System — Optimised v3

**Workflow ID:** `TqXh2eMbTmy40s7P`
**Trigger:** Webhook — HTTP POST
**Status:** Inactive
**Last Updated:** March 28, 2026

---

## Overview

This is a full-featured, production-grade appointment management system built entirely in n8n. A single webhook endpoint handles **three actions** — `book`, `cancel`, and `reschedule` — routing each through its own dedicated pipeline.

Each action performs a coordinated sequence: validate the request, write to a PostgreSQL database, sync a Google Calendar, respond immediately to the caller, then fire async notifications (Email, WhatsApp, SMS) without blocking the HTTP response.

A separate **error-handling pipeline** runs in parallel, catching any node failure and logging it to the database while also alerting the owner by email.

---

## Webhook Endpoint

| Environment | URL |
|---|---|
| **Production** | `POST https://cloudzen-n8n.pikapod.net/webhook/appointments` |
| **Test** | `POST https://cloudzen-n8n.pikapod.net/webhook-test/appointments` |

**No authentication required** on the webhook itself.

### Request Body Schema

```json
{
  "action":    "book | cancel | reschedule",
  "name":      "Jane Doe",
  "email":     "jane@example.com",
  "phone":     "+13055551234",
  "date":      "2026-04-10",
  "time":      "14:00",
  "endTime":   "14:30",
  "reason":    "Consultation",

  "bookingId": "APT-XXXXX-XXXX",

  "newDate":    "2026-04-12",
  "newTime":    "10:00",
  "newEndTime": "10:30"
}
```

> `bookingId`, `newDate`, `newTime`, `newEndTime` are only required for `cancel` and `reschedule`.

---

## Full Flow Diagram

```
                         ┌──────────────────────┐
                         │  Webhook — POST /     │
                         │   appointments        │
                         └──────────┬───────────┘
                                    │
                                    ▼
                           Prepare Base Data
                                    │
                                    ▼
                           Route by Action
                        ┌───────────┼────────────┐
                        ▼           ▼            ▼
                      [book]    [cancel]    [reschedule]
                        │           │            │
              ┌─────────┘  ┌────────┘   ┌────────┘
              ▼            ▼            ▼
    Check Slot — EXISTS  Cancel —    Reschedule —
                         Update DB   Update DB
              │            │            │
              ▼            ▼            ▼
          Slot Free?    Cancel —     Reschedule —
         ┌────┴────┐    Row Returned? Row Returned?
         ▼         ▼    ┌────┴────┐   ┌────┴────┐
    Generate    Respond  ▼         ▼   ▼         ▼
    Booking ID  Slot   Delete   Respond  Update  Respond
         │      Taken  Calendar Not     Calendar Not
         ▼             Event    Found   Event    Found
    Create Calendar         │                │
    Event                   ▼                ▼
         │           ┌─────────────┐  ┌─────────────┐
         ▼           │Respond +    │  │Respond +    │
    Save Booking     │Notify Async │  │Notify Async │
    to DB            └──────┬──────┘  └──────┬──────┘
         │                  │                │
    ┌────┴────┐        ┌─────────┐     ┌─────────┐
    ▼         ▼        ▼         ▼     ▼         ▼
Respond  Notify     Email     WA    Email      WA
Booked   Booked    User      User  User       User
(Async)  Async
    └───────┬───────┘
            ▼
  ┌─────────────────────┐
  │ Email User — Booked │
  │ Email Owner — Booked│
  │ SMS — Booked        │
  │ WhatsApp — Booked   │
  └─────────────────────┘

─────────────────────────────────────────────────────
Error Trigger (parallel, always listening)
    │
    ▼
Log Error to DB ──► Email Owner — Error
─────────────────────────────────────────────────────
```

---

## Nodes — Detailed Reference

---

### ENTRY POINTS

---

### 1. Webhook — Appointment System
**Type:** Webhook Trigger
**HTTP Method:** POST
**Path:** `/appointments`
**Response Mode:** Controlled by `Respond to Webhook` nodes (not auto-respond)

This is the single entry point for all appointment operations. It receives the JSON body and passes it downstream. The `responseMode: responseNode` setting means n8n will **not** auto-reply — the response is sent explicitly later by one of the `Respond —` nodes, allowing full control over HTTP response content and status codes.

---

### 2. Error Trigger
**Type:** Error Trigger
**Runs:** Automatically when any node in the workflow throws an unhandled error

This node runs on a **separate, parallel execution path** that is always active alongside the main flow. It does not intercept errors mid-flow — instead, n8n invokes it after a failure, passing the full error context. It feeds directly into the error-handling pipeline (Log → Email).

---

### SHARED ENTRY PROCESSING

---

### 3. Prepare Base Data
**Type:** Code (JavaScript)
**Runs after:** Webhook — Appointment System

The workflow's **normalisation layer**. Extracts and sanitises all fields from the raw webhook body into a clean, consistently-named object that every downstream node can rely on.

**What it does:**
- Reads `body.*` from the incoming webhook payload
- Constructs ISO 8601 datetime strings (`startDateTime`, `endDateTime`, `newStartDateTime`, `newEndDateTime`) by combining `date + 'T' + time + ':00'`
- Provides safe defaults (empty strings) for every field — preventing null reference errors further down

**Output shape:**
```json
{
  "action": "book",
  "bookingId": "",
  "userName": "Jane Doe",
  "userEmail": "jane@example.com",
  "userPhone": "+13055551234",
  "appointmentDate": "2026-04-10",
  "appointmentTime": "14:00",
  "appointmentReason": "Consultation",
  "startDateTime": "2026-04-10T14:00:00",
  "endDateTime": "2026-04-10T14:30:00",
  "newDate": "",
  "newTime": "",
  "newStartDateTime": "",
  "newEndDateTime": ""
}
```

> **Design note:** Centralising field parsing here means only this one node needs updating if the incoming schema changes. All other nodes consume clean, predictable field names.

---

### 4. Route by Action
**Type:** Switch
**Runs after:** Prepare Base Data

The **main router**. Reads `$json.action` and directs execution to one of three branches:

| Output | Condition | Destination |
|---|---|---|
| `book` | `action == "book"` | Check Slot — EXISTS |
| `cancel` | `action == "cancel"` | Cancel — Update DB + Lookup |
| `reschedule` | `action == "reschedule"` | Reschedule — Update DB + Lookup |

If `action` matches none of the three values, execution falls through to **Respond — Invalid Action**, returning a 400-style JSON error.

---

### ERROR HANDLING PIPELINE

---

### 5. Log Error to DB
**Type:** PostgreSQL
**Runs after:** Error Trigger

Persists the error to a `public.workflow_errors` table for auditing and debugging. Uses parameterised-style injection to capture:

- `workflow`: hardcoded as `"appointment-system"`
- `action`: extracted from the last known `Prepare Base Data` output (or `"unknown"` if unavailable)
- `booking_id`: extracted from the last known data (or empty string)
- `error_message`: `$json.error.message`
- `payload`: full JSON-stringified error object
- `created_at`: `NOW()`

> **Design note:** Extracting `action` and `booking_id` from deep within `$json.execution.data.resultData.runData` makes this node resilient to failures at any point — even before `Prepare Base Data` has run.

---

### 6. Email Owner — Error
**Type:** Gmail
**Runs after:** Log Error to DB

Sends an HTML alert email to the workflow owner when any node fails.

**Email content:**
- Error message text
- Name of the failing node
- Timestamp of failure (`$now`)

**To address:** Configured as `your-email@gmail.com` — update with the real owner email.

---

### BOOK PIPELINE

---

### 7. Check Slot — EXISTS
**Type:** PostgreSQL
**Runs after:** Route by Action [book]

Performs a lightweight `EXISTS` query to check whether the requested slot is already occupied.

**SQL logic:**
```sql
SELECT EXISTS (
  SELECT 1 FROM public.appointments
  WHERE appointment_date = '{{ $json.appointmentDate }}'
    AND appointment_time = '{{ $json.appointmentTime }}'
    AND status IN ('confirmed', 'rescheduled')
) AS slot_taken;
```

Returns a single boolean field `slot_taken: true/false`. Using `EXISTS` (rather than `COUNT`) is intentionally efficient — the DB stops scanning as soon as it finds one match.

---

### 8. Slot Free?
**Type:** IF (Conditional)
**Runs after:** Check Slot — EXISTS

Evaluates `slot_taken == false`.

- **True (slot is free) →** Generate Booking ID
- **False (slot is taken) →** Respond — Slot Taken

---

### 9. Respond — Slot Taken
**Type:** Respond to Webhook
**Runs after:** Slot Free? [false]

Immediately returns a JSON error to the HTTP caller.

```json
{ "success": false, "message": "This time slot is already booked. Please choose a different time." }
```

Execution ends here for this branch.

---

### 10. Generate Booking ID
**Type:** Code (JavaScript)
**Runs after:** Slot Free? [true]

Generates a unique, human-readable booking ID using a combination of a **base-36 timestamp** and **4 random characters**.

**Format:** `APT-{BASE36_TIMESTAMP}-{4_RANDOM_CHARS}`
**Example:** `APT-M5GXQK4Z-A3F7`

```javascript
const ts   = Date.now().toString(36).toUpperCase();
const rand = Math.random().toString(36).substring(2, 6).toUpperCase();
item.json.bookingId = `APT-${ts}-${rand}`;
```

> **Design note:** This approach avoids a DB round-trip to generate an ID (no `SEQUENCE` or `UUID` call needed). The timestamp component makes IDs naturally sortable by creation time, while the random suffix reduces collision risk.

---

### 11. Create Calendar Event
**Type:** Google Calendar
**Runs after:** Generate Booking ID
**Calendar:** ClouZen Booking Events

Creates a Google Calendar event for the appointment.

**Event fields:**
- **Summary:** `Appointment: [APT-XXXXX-XXXX]`
- **Start/End:** ISO 8601 datetimes from `Prepare Base Data`
- **Attendees:** User's email — Google Calendar automatically sends them an invite
- **Description:** Full booking details (ID, client name, email, phone, reason)
- **sendUpdates:** `"all"` — ensures all attendees receive calendar notifications

Returns the Google Calendar event object, including the `event.id` used by the next node to store a reference for future updates/deletions.

---

### 12. Save Booking to DB
**Type:** PostgreSQL
**Runs after:** Create Calendar Event

Inserts a new row into `public.appointments`, storing both the booking data and the Google Calendar `event_id` for future cancel/reschedule operations.

**Inserted fields:** `booking_id`, `event_id` (from Google Calendar), `user_name`, `user_email`, `user_phone`, `appointment_date`, `appointment_time`, `reason`, `status = 'confirmed'`, `created_at`, `updated_at`

Uses `RETURNING booking_id` to confirm the insert succeeded.

---

### 13. Respond — Booked
**Type:** Respond to Webhook
**Runs after:** Save Booking to DB

Immediately sends the HTTP response to the caller with the confirmed booking details.

```json
{
  "success": true,
  "action": "book",
  "bookingId": "APT-M5GXQK4Z-A3F7",
  "message": "Appointment confirmed! Booking ID: APT-M5GXQK4Z-A3F7"
}
```

> **Key design:** This node and **Notify — Booked (Async)** are wired in **parallel** from `Save Booking to DB`. The HTTP response is returned immediately without waiting for emails/WhatsApp/SMS to finish — keeping API latency low.

---

### 14. Notify — Booked (Async)
**Type:** Code (JavaScript)
**Runs after:** Save Booking to DB (parallel with Respond — Booked)

A **data assembly node** that consolidates fields from multiple upstream nodes (`Prepare Base Data`, `Generate Booking ID`) into a single clean object, passed to all four notification channels in parallel.

**Output includes:** `userName`, `userEmail`, `userPhone`, `bookingId`, `date`, `time`, `reason`, `wa_to` (prefixed with `whatsapp:`), `wa_from`

> **Design note:** Without this node, each notification node would need to reference multiple upstream nodes individually using `$('node name').item.json`. Centralising it here keeps the notification nodes simple and avoids repetitive cross-node references.

---

### 15. Email User — Booked
**Type:** Gmail
**Runs after:** Notify — Booked (Async)

Sends a styled HTML confirmation email to the user with their booking details.

**Content includes:**
- Booking ID, Date, Time, Reason (in a styled summary card)
- A "Manage Appointment" CTA button linking to the booking portal
- CC to the calendar event creator (owner)

---

### 16. Email Owner — Booked
**Type:** Gmail
**Runs after:** Notify — Booked (Async)

Sends a separate internal notification email to the business owner with the full client details (name, email, phone, date, time, booking ID).

**To address:** Dynamically pulled from `$('Create Calendar Event').item.json.creator.email` — the Google Calendar account that created the event.

---

### 17. SMS — Booked *(disabled)*
**Type:** Twilio (SMS)
**Runs after:** Notify — Booked (Async)
**Status:** 🔴 Disabled

Sends a short SMS booking confirmation to the user's phone. Currently disabled — enable by toggling the node active and setting the `from` number.

**Message:** `Hi {name}, your booking (ID: {bookingId}) is confirmed! See you soon.`

---

### 18. WhatsApp — Booked *(disabled)*
**Type:** Twilio (WhatsApp)
**Runs after:** Notify — Booked (Async)
**Status:** 🔴 Disabled

Sends a WhatsApp booking confirmation. Currently disabled.

**Message:** `Hi {name}, your booking (ID: {bookingId}) at CloudZen is confirmed! 🚀`

---

### CANCEL PIPELINE

---

### 19. Cancel — Update DB + Lookup
**Type:** PostgreSQL
**Runs after:** Route by Action [cancel]

Performs a single atomic `UPDATE ... RETURNING` query — it updates the status to `'cancelled'` and returns the affected row in one operation, eliminating a separate `SELECT` call.

**SQL logic:**
```sql
UPDATE public.appointments
SET status = 'cancelled', updated_at = NOW()
WHERE booking_id = '{{ $json.bookingId }}'
  AND status != 'cancelled'
RETURNING booking_id, event_id, user_name, user_email, user_phone,
          appointment_date, appointment_time;
```

If no row is found (wrong ID or already cancelled), `RETURNING` yields nothing — handled by the next node.

---

### 20. Cancel — Row Returned?
**Type:** IF (Conditional)
**Runs after:** Cancel — Update DB + Lookup

Checks whether `booking_id` is non-empty in the `RETURNING` result.

- **True (found) →** Delete Calendar — Cancel
- **False (not found) →** Respond — Cancel Not Found

---

### 21. Respond — Cancel Not Found
**Type:** Respond to Webhook
**Runs after:** Cancel — Row Returned? [false]

```json
{ "success": false, "message": "Booking ID not found or already cancelled." }
```

---

### 22. Delete Calendar — Cancel
**Type:** Google Calendar
**Runs after:** Cancel — Row Returned? [true]

Deletes the Google Calendar event associated with the booking using `event_id` returned from the DB.

- **sendUpdates:** `"all"` — automatically emails the attendee a cancellation notice via Google Calendar

---

### 23. Respond — Cancelled
**Type:** Respond to Webhook
**Runs after:** Delete Calendar — Cancel (parallel with Notify — Cancelled)

```json
{ "success": true, "action": "cancel", "message": "Appointment cancelled successfully." }
```

---

### 24. Notify — Cancelled (Async)
**Type:** Code (JavaScript)
**Runs after:** Delete Calendar — Cancel (parallel with Respond — Cancelled)

Same data-assembly pattern as **Notify — Booked (Async)**. Merges fields from `Prepare Base Data` and `Cancel — Update DB + Lookup` into a clean object for notification nodes.

> **Note:** User details (`user_name`, `user_phone`) are sourced from the DB `RETURNING` clause rather than the original request body — this ensures the notification uses the stored data, even if the caller submitted slightly different casing.

---

### 25. Email User — Cancelled
**Type:** Gmail
**Runs after:** Notify — Cancelled (Async)

Sends a cancellation confirmation email to the user with a styled card showing their booking ID, and a red "Book a New Appointment" CTA button.

---

### 26. WhatsApp — Cancelled *(disabled)*
**Type:** Twilio (WhatsApp)
**Runs after:** Notify — Cancelled (Async)
**Status:** 🔴 Disabled

Sends a WhatsApp cancellation message when enabled.

**Message:** `Hi {user_name}, your appointment (ID: {booking_id}) has been cancelled. We hope to see you again soon!`

> **Note:** This node references `$json.user_name` and `$json.user_phone` (snake_case) rather than the camelCase fields from `Notify — Cancelled (Async)`. This is a minor inconsistency — update to `$json.userName` and `$json.userPhone` when enabling.

---

### RESCHEDULE PIPELINE

---

### 27. Reschedule — Update DB + Lookup
**Type:** PostgreSQL
**Runs after:** Route by Action [reschedule]

Updates the appointment to the new date/time, sets status to `'rescheduled'`, and — critically — **resets both reminder flags** so the Reminders workflow will send fresh reminders for the new datetime.

```sql
UPDATE public.appointments
SET appointment_date    = '{{ $json.newDate }}',
    appointment_time    = '{{ $json.newTime }}',
    status              = 'rescheduled',
    reminder_24h_sent   = false,
    reminder_1h_sent    = false,
    updated_at          = NOW()
WHERE booking_id = '{{ $json.bookingId }}'
  AND status NOT IN ('cancelled')
RETURNING booking_id, event_id, user_name, user_email, user_phone,
          appointment_date::text, appointment_time::text;
```

> **Integration note:** The `reminder_24h_sent = false` / `reminder_1h_sent = false` reset is the integration point between this workflow and the **Appointment Reminders** workflow — ensuring the user receives reminders for their new slot without manual intervention.

---

### 28. Reschedule — Row Returned?
**Type:** IF (Conditional)
**Runs after:** Reschedule — Update DB + Lookup

Same guard pattern as the cancel pipeline.

- **True (found) →** Update Calendar — Reschedule
- **False (not found/cancelled) →** Respond — Reschedule Not Found

---

### 29. Respond — Reschedule Not Found
**Type:** Respond to Webhook
**Runs after:** Reschedule — Row Returned? [false]

```json
{ "success": false, "message": "Booking ID not found or already cancelled." }
```

---

### 30. Update Calendar — Reschedule
**Type:** Google Calendar
**Runs after:** Reschedule — Row Returned? [true]

Updates the existing Google Calendar event (identified by `event_id`) with the new start and end times using `DateTime.fromISO()` for proper timezone handling.

> **Note:** Only `start` and `end` are updated. The event title, attendees, and description remain unchanged — preserving the booking ID reference in the calendar.

---

### 31. Respond — Rescheduled
**Type:** Respond to Webhook
**Runs after:** Update Calendar — Reschedule (parallel with Notify — Rescheduled)

```json
{ "success": true, "action": "reschedule", "message": "Appointment rescheduled successfully." }
```

---

### 32. Notify — Rescheduled (Async)
**Type:** Code (JavaScript)
**Runs after:** Update Calendar — Reschedule (parallel with Respond — Rescheduled)

Same data-assembly pattern. Output includes new date/time fields (`newDate`, `newTime`, `newStartDateTime`, `newEndDateTime`) for the rescheduled notification messages.

---

### 33. Email User — Rescheduled
**Type:** Gmail
**Runs after:** Notify — Rescheduled (Async)

Sends a rescheduling confirmation email to the user, showing the new date and time with a "Manage Appointment" CTA.

---

### 34. WhatsApp — Rescheduled *(disabled)*
**Type:** Twilio (WhatsApp)
**Runs after:** Notify — Rescheduled (Async)
**Status:** 🔴 Disabled

Sends a WhatsApp reschedule confirmation when enabled.

**Message:** `Hi {user_name}! Rescheduled to {appointment_date} at {appointment_time}. Booking ID: {booking_id}`

> **Note:** Same snake_case inconsistency as WhatsApp — Cancelled. Update field names when enabling.

---

## Node Summary Table

| # | Node | Type | Pipeline |
|---|------|------|----------|
| 1 | Webhook — Appointment System | Webhook Trigger | Entry |
| 2 | Error Trigger | Error Trigger | Error |
| 3 | Log Error to DB | PostgreSQL | Error |
| 4 | Email Owner — Error | Gmail | Error |
| 5 | Prepare Base Data | Code | Shared |
| 6 | Route by Action | Switch | Shared |
| 7 | Check Slot — EXISTS | PostgreSQL | Book |
| 8 | Slot Free? | IF | Book |
| 9 | Respond — Slot Taken | Respond to Webhook | Book |
| 10 | Generate Booking ID | Code | Book |
| 11 | Create Calendar Event | Google Calendar | Book |
| 12 | Save Booking to DB | PostgreSQL | Book |
| 13 | Respond — Booked | Respond to Webhook | Book |
| 14 | Notify — Booked (Async) | Code | Book |
| 15 | Email User — Booked | Gmail | Book |
| 16 | Email Owner — Booked | Gmail | Book |
| 17 | SMS — Booked 🔴 | Twilio SMS | Book |
| 18 | WhatsApp — Booked 🔴 | Twilio WhatsApp | Book |
| 19 | Cancel — Update DB + Lookup | PostgreSQL | Cancel |
| 20 | Cancel — Row Returned? | IF | Cancel |
| 21 | Respond — Cancel Not Found | Respond to Webhook | Cancel |
| 22 | Delete Calendar — Cancel | Google Calendar | Cancel |
| 23 | Respond — Cancelled | Respond to Webhook | Cancel |
| 24 | Notify — Cancelled (Async) | Code | Cancel |
| 25 | Email User — Cancelled | Gmail | Cancel |
| 26 | WhatsApp — Cancelled 🔴 | Twilio WhatsApp | Cancel |
| 27 | Reschedule — Update DB + Lookup | PostgreSQL | Reschedule |
| 28 | Reschedule — Row Returned? | IF | Reschedule |
| 29 | Respond — Reschedule Not Found | Respond to Webhook | Reschedule |
| 30 | Update Calendar — Reschedule | Google Calendar | Reschedule |
| 31 | Respond — Rescheduled | Respond to Webhook | Reschedule |
| 32 | Notify — Rescheduled (Async) | Code | Reschedule |
| 33 | Email User — Rescheduled | Gmail | Reschedule |
| 34 | WhatsApp — Rescheduled 🔴 | Twilio WhatsApp | Reschedule |

🔴 = currently disabled

---

## Database Schema Requirements

### `public.appointments`

| Column | Type | Notes |
|---|---|---|
| `booking_id` | `text` | Primary key — generated by n8n (`APT-XXXX`) |
| `event_id` | `text` | Google Calendar event ID for sync |
| `user_name` | `text` | Client name |
| `user_email` | `text` | Client email |
| `user_phone` | `text` | Client phone (E.164 recommended) |
| `appointment_date` | `date` | Appointment date |
| `appointment_time` | `time` | Appointment time |
| `appointment_at` | `timestamptz` | Full timestamp (used by Reminders workflow) |
| `reason` | `text` | Reason for appointment |
| `status` | `text` | `confirmed`, `rescheduled`, `cancelled` |
| `reminder_24h_sent` | `boolean` | Reset to `false` on reschedule |
| `reminder_1h_sent` | `boolean` | Reset to `false` on reschedule |
| `created_at` | `timestamptz` | Auto-set on insert |
| `updated_at` | `timestamptz` | Updated on every write |

### `public.workflow_errors`

| Column | Type | Notes |
|---|---|---|
| `workflow` | `text` | Workflow name |
| `action` | `text` | Last known action |
| `booking_id` | `text` | Last known booking ID |
| `error_message` | `text` | Error message text |
| `payload` | `jsonb` / `text` | Full error object |
| `created_at` | `timestamptz` | Auto-set |

---

## Integration with Appointment Reminders Workflow

This workflow and the **Appointment Reminders — Optimised (24h & 1h)** workflow are designed to work together:

- When a new booking is **confirmed**, `status = 'confirmed'` and both reminder flags default to `false` — the Reminders workflow will pick it up.
- When a booking is **cancelled**, `status = 'cancelled'` — the Reminders workflow excludes cancelled appointments via its `WHERE status IN ('confirmed', 'rescheduled')` filter.
- When a booking is **rescheduled**, `reminder_24h_sent = false` and `reminder_1h_sent = false` are explicitly reset — ensuring fresh reminders are sent for the new time slot.

---

## Setup Checklist

- [ ] **Webhook** is publicly accessible (workflow must be **activated**)
- [ ] Production webhook URL (`https://cloudzen-n8n.pikapod.net/webhook/appointments`) set as `N8N_WEBHOOK_URL` on both CloudZen Azure Function Apps (prod + staging)
- [ ] **PostgreSQL credential** configured and connected to the correct database (Neon Postgres, host `ep-broad-fog-a8m4je85-pooler.eastus2.azure.neon.tech`)
- [ ] Both DB tables (`appointments`, `workflow_errors`) exist with the required columns
- [ ] **Gmail credential** connected (used for all email nodes)
- [ ] **Google Calendar credential** connected; calendar ID updated in `Create Calendar Event`, `Delete Calendar — Cancel`, `Update Calendar — Reschedule`
- [ ] **Twilio credential** configured (for SMS/WhatsApp when enabled)
- [ ] Update `your-email@gmail.com` in **Email Owner — Error** to the real owner address
- [ ] Update `https://your-booking-url.com` placeholder links in all email templates
- [ ] Enable and test **SMS — Booked** and **WhatsApp** nodes when Twilio is ready
- [ ] Fix snake_case field name inconsistency in **WhatsApp — Cancelled** and **WhatsApp — Rescheduled** (`user_name` → `userName`, `user_phone` → `userPhone`)

---

## Key Design Decisions

**Why a single webhook for all three actions?**
One endpoint keeps the API surface minimal. Clients send an `action` field to differentiate intent — simpler to document, version, and secure than three separate routes.

**Why `UPDATE ... RETURNING` instead of `SELECT` then `UPDATE`?**
Combining the update and lookup into one query is atomic — it eliminates a race condition where two requests could both read "not cancelled" before either updates, and halves the number of DB round-trips.

**Why parallel Respond + Notify Async?**
The HTTP response (`Respond — Booked/Cancelled/Rescheduled`) is returned to the caller immediately after the DB/Calendar operations complete. Notification nodes (Email, WhatsApp, SMS) run on a parallel branch without blocking the response — keeping API latency well under 1 second regardless of email delivery speed.

**Why `Notify — * (Async)` Code nodes before the notification channels?**
After the DB and Calendar operations, data is scattered across multiple upstream nodes. These Code nodes consolidate all required fields into a single object, so each notification node has one clean `$json` reference. Without them, Email and WhatsApp nodes would need complex multi-node expressions like `$('Create Calendar Event').item.json.creator.email`.

**Why reset reminder flags on reschedule?**
The Reminders workflow uses `reminder_24h_sent` and `reminder_1h_sent` as idempotency guards. Without resetting them, a rescheduled user would never receive reminders for their new slot — a critical UX failure for a booking system.

**Why `EXISTS` for slot checking instead of `COUNT`?**
`EXISTS` short-circuits on the first matching row. For a high-traffic booking system, this is meaningfully faster than `COUNT(*)` which scans all matching rows before returning.
