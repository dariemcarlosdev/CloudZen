# Appointment Reminders — Optimised (24h & 1h)

**Workflow ID:** `OQEpo4tefYldYgG5`
**Status:** Inactive
**Last Updated:** June 3, 2026

---

## Overview

This workflow automatically sends appointment reminder notifications to users via **Email, WhatsApp, and SMS** at two key intervals:

- **24 hours before** the appointment — full reminder with date, time, and booking ID
- **1 hour before** the appointment — short "see you soon" nudge via WhatsApp and SMS

It runs on a schedule (every hour), queries a PostgreSQL database for upcoming appointments, fans out to multiple notification channels in parallel, then bulk-marks those appointments as reminded to prevent duplicate messages.

---

## Flow Diagram

```
Schedule — Every Hour
        │
        ├──────────────────────────────────┐
        ▼                                  ▼
Query — 24h Due                    Query — 1h Due
        │                                  │
        ▼                                  ▼
Any 24h Due? (IF)                  Any 1h Due? (IF)
        │ [true]                           │ [true]
        ├──────────────┬──────────         ├─────────────┬─────────────
        ▼              ▼      ▼            ▼             ▼             ▼
  24h — Email   24h — WhatsApp  24h — SMS  1h — WhatsApp  1h — SMS  Collect 1h IDs
        │              │      │                       │             │
        └──────────────┴──────┘                       └─────────────┘
                       ▼                                      ▼
              Collect 24h IDs                        Collect 1h IDs
                       │                                      │
                       ▼                                      ▼
            Bulk Mark 24h Sent                     Bulk Mark 1h Sent
```

---

## Nodes

### 1. Schedule — Every Hour
**Type:** Schedule Trigger
**Position:** Entry point of the workflow

Fires the workflow automatically **once every hour**. There are no manual inputs — n8n's scheduler kicks this off continuously, making the entire workflow hands-free.

**Key config:**
- Interval: `hours` (every 1 hour)

---

### 2. Query — 24h Due
**Type:** PostgreSQL
**Runs after:** Schedule — Every Hour

Queries the `public.appointments` table for bookings whose appointment time falls **between 23 and 25 hours from now** and have not yet received a 24h reminder.

**SQL logic:**
- Filters `status IN ('confirmed', 'rescheduled')`
- Filters `reminder_24h_sent = false`
- Window: `NOW() + 23h` to `NOW() + 25h` — the ±1 hour window accounts for the hourly run frequency
- Returns up to **50 rows** ordered by appointment time
- Fields returned: `booking_id`, `user_name`, `user_email`, `user_phone`, `appointment_date`, `appointment_time`

---

### 3. Query — 1h Due
**Type:** PostgreSQL
**Runs after:** Schedule — Every Hour

Queries the same `public.appointments` table for bookings whose appointment time falls **between 50 and 70 minutes from now** and have not yet received a 1h reminder.

**SQL logic:**
- Filters `status IN ('confirmed', 'rescheduled')`
- Filters `reminder_1h_sent = false`
- Window: `NOW() + 50min` to `NOW() + 70min` — same ±10 min buffer around the hourly tick
- Returns up to **50 rows** ordered by appointment time
- Fields returned: `booking_id`, `user_name`, `user_email`, `user_phone`, `appointment_date`, `appointment_time`

---

### 4. Any 24h Due?
**Type:** IF (Conditional)
**Runs after:** Query — 24h Due

A guard node that checks whether the query returned any results. It evaluates whether `booking_id` is **not empty** — if the field exists, there are appointments to remind; if empty, execution stops for the 24h branch.

- **True branch →** fans out to Email, WhatsApp, and SMS nodes
- **False branch →** execution ends silently (no unnecessary downstream calls)

---

### 5. Any 1h Due?
**Type:** IF (Conditional)
**Runs after:** Query — 1h Due

Same guard logic as above, but for the 1h branch. Checks that `booking_id` is not empty before triggering the 1h notification nodes.

- **True branch →** fans out to WhatsApp, SMS, and Collect 1h IDs
- **False branch →** execution ends silently

---

### 6. 24h — Email
**Type:** Gmail
**Runs after:** Any 24h Due? [true]

Sends a personalized HTML reminder email to the user's email address, 24 hours before their appointment.

**Message content:**
- Greeting with the user's name
- Bold appointment date and time
- Booking ID for self-service cancellation/rescheduling
- Friendly sign-off

**Dynamic fields used:** `user_email`, `user_name`, `appointment_date`, `appointment_time`, `booking_id`

---

### 7. 24h — WhatsApp
**Type:** Twilio
**Runs after:** Any 24h Due? [true]

Sends a WhatsApp message to the user's phone number via Twilio's WhatsApp sandbox/business API, 24 hours before their appointment.

**Message content:**
- Name greeting
- Bold appointment date and time (WhatsApp markdown with `*`)
- Booking ID for rescheduling
- Short website redirect prompt

**Dynamic fields used:** `user_phone`, `user_name`, `appointment_date`, `appointment_time`, `booking_id`
**From number:** `whatsapp:+14155238886` (Twilio sandbox number)

---

### 8. 24h — SMS
**Type:** Twilio
**Runs after:** Any 24h Due? [true]

Sends a plain SMS reminder to the user's phone number, 24 hours before their appointment.

**Message content:**
- Same key info as WhatsApp (name, date, time, booking ID) in plain text
- Concise for SMS character limits

**Dynamic fields used:** `user_phone`, `user_name`, `appointment_date`, `appointment_time`, `booking_id`
**From number:** Configured as `+1YOUR_TWILIO_NUMBER` (requires updating with your real Twilio number)

---

### 9. Collect 24h IDs
**Type:** Code (JavaScript)
**Runs after:** 24h — Email, 24h — WhatsApp, 24h — SMS

A JavaScript aggregator node. Since Email, WhatsApp, and SMS all run in parallel and feed into this node from three branches, it collects all incoming items and extracts their `booking_id` values into a single array.

**Output:**
```json
{
  "ids": ["booking-001", "booking-002"],
  "idList": "'booking-001','booking-002'"
}
```

The `idList` string is pre-formatted for safe injection into the SQL `ARRAY[...]` clause in the next node. Empty results return an empty array, preventing unnecessary DB calls.

---

### 10. Bulk Mark 24h Sent
**Type:** PostgreSQL
**Runs after:** Collect 24h IDs

Executes a single bulk `UPDATE` query to mark all reminded appointments with `reminder_24h_sent = true`. This is the **idempotency safeguard** — it ensures that even if the workflow runs again within the same hour, those appointments won't be reminded twice.

**SQL logic:**
```sql
UPDATE public.appointments
SET reminder_24h_sent = true, updated_at = NOW()
WHERE booking_id = ANY(ARRAY[<idList>]::text[]);
```

---

### 11. 1h — WhatsApp
**Type:** Twilio
**Runs after:** Any 1h Due? [true]

Sends a short WhatsApp message telling the user their appointment is **in 1 hour**.

**Message content:**
- Name greeting
- Bold "1 hour" notice with appointment time
- Friendly "See you soon!"

**Note:** No date is mentioned here since the appointment is imminent — only the time matters.

**Dynamic fields used:** `user_phone`, `user_name`, `appointment_time`

---

### 12. 1h — SMS
**Type:** Twilio
**Runs after:** Any 1h Due? [true]

Sends the same short 1-hour notice as above, but as a plain SMS.

**Dynamic fields used:** `user_phone`, `user_name`, `appointment_time`
**From number:** Configured as `+1YOUR_TWILIO_NUMBER` (requires updating with your real Twilio number)

---

### 13. Collect 1h IDs
**Type:** Code (JavaScript)
**Runs after:** Any 1h Due? [true], 1h — WhatsApp, 1h — SMS

Same aggregation logic as **Collect 24h IDs**, but for the 1h branch. Collects all `booking_id` values from the WhatsApp and SMS nodes into a single `idList` string for the bulk update.

---

### 14. Bulk Mark 1h Sent
**Type:** PostgreSQL
**Runs after:** Collect 1h IDs

Executes the same bulk `UPDATE` pattern as **Bulk Mark 24h Sent**, but sets `reminder_1h_sent = true` to prevent duplicate 1h reminders.

---

## Database Schema Requirements

The workflow expects a `public.appointments` table with at minimum these columns:

| Column | Type | Description |
|---|---|---|
| `booking_id` | `text` | Unique booking identifier |
| `user_name` | `text` | User's display name |
| `user_email` | `text` | User's email address |
| `user_phone` | `text` | User's phone number (E.164 format recommended) |
| `appointment_date` | `date` | Date of the appointment |
| `appointment_time` | `time` | Time of the appointment |
| `appointment_at` | `timestamptz` | Full timestamp used for window queries |
| `status` | `text` | `'confirmed'` or `'rescheduled'` to be included |
| `reminder_24h_sent` | `boolean` | Tracks whether 24h reminder was sent |
| `reminder_1h_sent` | `boolean` | Tracks whether 1h reminder was sent |
| `updated_at` | `timestamptz` | Updated on each bulk mark |

---

## Setup Checklist

- [ ] **PostgreSQL credential** configured in n8n and connected to the correct database
- [ ] **Gmail credential** connected to the sending account
- [ ] **Twilio credential** configured with a valid Account SID and Auth Token
- [ ] Replace `+1YOUR_TWILIO_NUMBER` in **24h — SMS** and **1h — SMS** nodes with your actual Twilio number
- [ ] Confirm Twilio WhatsApp sender (`whatsapp:+14155238886`) is your sandbox or approved business number
- [ ] **Activate the workflow** in n8n to enable the hourly schedule trigger

---

## Design Decisions

**Why a ±1h / ±10min query window?**
Since the scheduler runs every hour, appointments near the boundary (e.g., exactly 24h away) could be missed if the query used an exact match. The window ensures no appointment is skipped due to minor timing drift.

**Why parallel notification channels?**
Email, WhatsApp, and SMS fan out simultaneously from the IF node, so all three reminders are sent concurrently rather than sequentially — reducing total execution time.

**Why bulk-mark after all channels?**
The `Collect IDs` → `Bulk Mark Sent` pattern ensures the DB is only updated after all notifications have been dispatched. If any channel node fails, the booking ID won't be collected, so the reminder will be retried on the next run.

**Why no 1h Email?**
The 1h reminder is intentionally limited to WhatsApp and SMS — faster, mobile-native channels — since a 1-hour notice is urgent and email is less likely to be seen in time.
