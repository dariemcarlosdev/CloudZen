> **Document**: Appointment System Feature
> **Scope**: Multi-step booking system — schedule, cancel, reschedule appointments via n8n workflow automation
> **Audience**: AI assistants, developers
> **Last Updated**: March 2026

---

# Appointment System Feature

## Table of Contents

- [Overview](#overview)
- [Quick Reference](#quick-reference)
- [Architecture](#architecture)
- [User Flows](#user-flows)
- [Components](#components)
- [API Integration](#api-integration)
- [Request/Response](#requestresponse)
- [Configuration](#configuration)
- [Booking ID Format](#booking-id-format)
- [Time Zones](#time-zones)
- [Available Time Slots](#available-time-slots)
- [Entry Points](#entry-points)
- [UI Design](#ui-design)
- [Scope Boundaries](#scope-boundaries)
- [Related Docs](#related-docs)

---

## Overview

Multi-step booking system allowing users to schedule, cancel, and reschedule appointments with CloudZen. Integrates with n8n workflow automation for calendar management.

---

## Quick Reference

| Item | Value |
|------|-------|
| **Endpoint** | `POST /api/book-appointment` |
| **Actions** | `book`, `cancel`, `reschedule` |
| **Backend Function** | `Api/Features/Booking/BookAppointmentFunction.cs` |
| **Orchestrator Component** | `Features/Booking/Components/BookingContact.razor` |
| **External Integration** | n8n -> Google Calendar + Email |
| **Entry Point** | Navbar "Let's Talk" -> `/contact` |

---

## Architecture

### End-to-End Flow

```
Blazor WASM -> Azure Function -> n8n Webhook -> Google Calendar + Email notifications
```

### Action Discriminator Pattern

The system uses a single endpoint (`POST /api/book-appointment`) with an `action` field discriminator to route all booking operations:

```
WASM UI -> /api/book-appointment -> Azure Function -> N8N Switch Node
                                     (validate)       |-- "book"       -> Create event
                                     (transform)      |-- "cancel"     -> Delete event
                                                      +-- "reschedule" -> Update event
```

The Azure Function transforms WASM-friendly field names to N8N's expected payload format (`N8nAppointmentPayload`). The WASM model uses user-friendly names (`name`, `email`, `phone`) while N8N expects (`userName`, `userEmail`, `userPhone`). The Function owns this translation.

### Required Fields Per Action

| Field              | book | cancel | reschedule |
|--------------------|:----:|:------:|:----------:|
| `action`           | Yes  | Yes    | Yes        |
| `bookingId`        | --   | Yes    | Yes        |
| `name/email/phone` | Yes  | email  | email      |
| `date/time`        | Yes  | --     | --         |
| `newDate/newTime`  | --   | --     | Yes        |

### Backend Components

| Component | Purpose |
|-----------|---------|
| `BookAppointmentFunction.cs` | Azure Function HTTP trigger |
| n8n Workflow | Calendar event creation, email notifications |
| `N8N_WEBHOOK_URL` | Secret webhook endpoint |

---

## User Flows

### Schedule Appointment

| Step | Action | Component |
|------|--------|-----------|
| 1 | User clicks "Let's Talk" in navbar | `Header.razor` |
| 2 | Navigate to `/contact` | `Contact.razor` page |
| 3 | Select date from calendar | `BookingCalendar.razor` |
| 4 | Select time slot | `BookingTimeSlots.razor` |
| 5 | Fill contact details | `BookingDetailsForm.razor` |
| 6 | Submit booking | `AppointmentService` |
| 7 | Show confirmation with booking ID | `BookingConfirmation.razor` |

### Cancel Appointment

| Step | Action | Component |
|------|--------|-----------|
| 1 | User navigates to manage appointment | Link in confirmation email |
| 2 | Enter booking ID and email | `ManageAppointmentCancel.razor` |
| 3 | Confirm cancellation | Warning displayed |
| 4 | Submit cancellation | `AppointmentService` |
| 5 | Show success confirmation | Success state UI |

### Reschedule Appointment

| Step | Action | Component |
|------|--------|-----------|
| 1 | User navigates to manage appointment | Link in confirmation email |
| 2 | Enter booking ID and email (Step 1) | `ManageAppointmentReschedule.razor` |
| 3 | Select new date/time (Step 2) | `BookingCalendar.razor` + `BookingTimeSlots.razor` |
| 4 | Confirm new time | `AppointmentService` |
| 5 | Show success confirmation | Success state UI |

---

## Components

### Booking Flow

| Component | Location | Purpose |
|-----------|----------|---------|
| `BookingContact.razor` | `Features/Booking/Components/` | Main orchestrator (3-step wizard) |
| `BookingSidebar.razor` | `Features/Booking/Components/` | Selection summary display |
| `BookingCalendar.razor` | `Features/Booking/Components/` | Date picker with availability |
| `BookingTimeSlots.razor` | `Features/Booking/Components/` | Time slot selection |
| `BookingTimeZonePicker.razor` | `Features/Booking/Components/` | Time zone selection dropdown |
| `BookingDetailsForm.razor` | `Features/Booking/Components/` | Contact info form |
| `BookingConfirmation.razor` | `Features/Booking/Components/` | Success state with booking ID |

### Manage Appointment

| Component | Location | Purpose |
|-----------|----------|---------|
| `ManageAppointmentCancel.razor` | `Features/Booking/Components/` | Cancel flow UI |
| `ManageAppointmentCancel.razor.cs` | `Features/Booking/Components/` | Cancel logic (code-behind) |
| `ManageAppointmentReschedule.razor` | `Features/Booking/Components/` | Reschedule flow UI (2-step wizard) |
| `ManageAppointmentReschedule.razor.cs` | `Features/Booking/Components/` | Reschedule logic (code-behind) |

### Services

| Service | Location | Purpose |
|---------|----------|---------|
| `AppointmentService` | `Features/Booking/Services/` | API client for booking operations |
| `IAppointmentService` | `Features/Booking/Services/` | Service interface |
| `BookingService` | `Features/Booking/Services/` | Calendar logic, date availability, formatting |
| `IBookingService` | `Features/Booking/Services/` | Service interface |
| `GoogleCalendarUrlService` | `Features/Booking/Services/` | Generate "Add to Calendar" links |

### Models

| Model | Location | Purpose |
|-------|----------|---------|
| `BookingFormModel` | `Features/Booking/Models/` | New booking form data |
| `CancelFormModel` | `Features/Booking/Models/` | Cancel form data |
| `RescheduleFormModel` | `Features/Booking/Models/` | Reschedule form data |
| `BookAppointmentRequest` | `Features/Booking/Models/` | API request for booking |
| `CancelAppointmentRequest` | `Features/Booking/Models/` | API request for cancel |
| `RescheduleAppointmentRequest` | `Features/Booking/Models/` | API request for reschedule |
| `AppointmentResult` | `Features/Booking/Models/` | Result type with `Ok()`/`Fail()` |

---

## API Integration

| Action | Method | Endpoint | Body Field |
|--------|--------|----------|------------|
| **Book** | POST | `/api/book-appointment` | `action: "book"` |
| **Cancel** | POST | `/api/book-appointment` | `action: "cancel"` |
| **Reschedule** | POST | `/api/book-appointment` | `action: "reschedule"` |

---

## Request/Response

### Book Request

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `action` | string | Yes | `"book"` |
| `name` | string | Yes | Max 100 chars |
| `email` | string | Yes | Valid email |
| `phone` | string | Yes | E.164 format (starts with `+`) |
| `businessName` | string | Yes | Max 200 chars |
| `date` | string | Yes | `YYYY-MM-DD` |
| `time` | string | Yes | `HH:mm` (24-hour) |
| `endTime` | string | Yes | `HH:mm` (24-hour) |
| `reason` | string | No | Default: "CloudZen Virtual Meeting" |

### Cancel Request

| Field | Type | Required |
|-------|------|----------|
| `action` | string | Yes (`"cancel"`) |
| `bookingId` | string | Yes |
| `email` | string | Yes |

### Reschedule Request

| Field | Type | Required |
|-------|------|----------|
| `action` | string | Yes (`"reschedule"`) |
| `bookingId` | string | Yes |
| `email` | string | Yes |
| `newDate` | string | Yes |
| `newTime` | string | Yes |
| `newEndTime` | string | Yes |

### Response

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Operation result |
| `action` | string | Action performed |
| `bookingId` | string | Booking ID (on book/reschedule success) |
| `message` | string | User-friendly message |

---

## Configuration

### Frontend (Blazor)

| Setting | File | Purpose |
|---------|------|---------|
| `BookingService.ApiBaseUrl` | `appsettings.json` | API endpoint |
| `BookingService.BookAppointmentEndpoint` | `appsettings.json` | Endpoint path |

### Backend (Azure Function)

| Setting | Location | Purpose |
|---------|----------|---------|
| `N8N_WEBHOOK_URL` | Azure Portal / Key Vault | n8n workflow webhook |

---

## Booking ID Format

```
APT-XXXXXXXX-XXXX
```

Example: `APT-MN7O3825-TMVP`

---

## Time Zones

- User selects time zone via `BookingTimeZonePicker`
- Default: Browser's local time zone
- Display format: `GMT-05:00 America/New_York (EST)`
- All times sent to API in 24-hour format

---

## Available Time Slots

| Slot | Display |
|------|---------|
| 10:00 AM | 10:00 AM - 10:30 AM |
| 10:30 AM | 10:30 AM - 11:00 AM |
| 12:00 PM | 12:00 PM - 12:30 PM |
| ... | 30-minute increments |

---

## Entry Points

| Location | CTA Text | Icon | Action |
|----------|----------|------|--------|
| Navbar (Desktop) | "Let's Talk" | -- | Navigate to `/contact` |
| Navbar (Mobile) | "Let's Talk" | -- | Navigate to `/contact` |
| Confirmation Email | "Manage Appointment" | -- | Link to manage page |

---

## UI Design

### Sidebar Colors (Dark Teal)

| Element | Class |
|---------|-------|
| Background | `bg-gradient-to-br from-teal-cyan-aqua-900 to-teal-cyan-aqua-800` |
| Heading | `text-white` |
| Secondary text | `text-teal-cyan-aqua-100` |
| Badge | `bg-white/10 text-teal-cyan-aqua-200` |
| Icons | `text-teal-cyan-aqua-300` |

> See [02_ui_color_design_system.md](../06-patterns/02_ui_color_design_system.md) for full color reference.

---

## Scope Boundaries

This document covers the appointment booking feature only. The following are **not** covered here:

- **Email sending infrastructure** — See the contact/email feature docs for Brevo SMTP details.
- **n8n workflow internals** — This doc describes the contract (webhook URL, payload format) but not the n8n node configuration itself.
- **Authentication/authorization** — The booking system is publicly accessible; no user login is required.
- **Payment processing** — No payment is collected during booking.
- **Admin dashboard** — There is no admin UI for managing appointments; management happens via n8n and Google Calendar directly.

---

## Related Docs

- [API_ENDPOINTS.md](../01-architecture/API_ENDPOINTS.md) — Full endpoint specification
- [COMPONENT_ARCHITECTURE.md](../01-architecture/COMPONENT_ARCHITECTURE.md) — Component patterns
- [02_ui_color_design_system.md](../06-patterns/02_ui_color_design_system.md) — Sidebar styling

---

*Last Updated: March 2026*
