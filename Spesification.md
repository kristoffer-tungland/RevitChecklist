Certainly! Here is your **complete, detailed specification** updated with the requirements for the element selection API endpoint and the rest of your clarified expectations. This version is ready for handover to your development team.

---

# Detailed Specification: Revit Quality Checklist Application

## 1. System Overview

This application provides a quality checklist solution for AEC projects, composed of:

* **Client:** A modern, browser-based web application built with vanilla JavaScript, HTML, and CSS (no frameworks).
* **Server:** A C# HTTP server running as a Revit add-in using only `HttpListener` and standard .NET/Revit APIs. The server is compatible with both .NET Framework 4.8 (Revit 2024 and below) and .NET 8 (Revit 2025+).
* **Storage:** All data (templates and checks) is serialized as JSON and stored in Revit’s `DataStorage` elements within the active model. No external database or file storage is used.

---

## 2. User Workflow

* The user starts the app by pressing a dedicated Ribbon button in Revit.
* The backend HTTP server starts if not already running and opens the client’s default web browser at the local address (e.g., `http://localhost:51789/`).
* The web client authenticates the user automatically using the Revit username—no password or manual login.
* The user can create, edit, and manage checklist templates, and perform new checks based on those templates.
* Each check is linked to one or more Revit elements (selected by the user), and results are stored in the model for traceability.

---

## 3. Functional Requirements

### 3.1. Templates

* Users can **create, edit, duplicate, and delete** checklist templates.
* Templates are organized into **sections**, each containing a list of items/questions.
* **Item types** include: checkbox, text input, number input, and dropdown selection.
* Templates are visually editable via an intuitive drag-and-drop interface for both sections and items.
* Templates are stored as JSON in individual Revit `DataStorage` elements, with metadata including name, description, created/modified dates, and creator (Revit username).
* Templates can be **archived** (marked inactive), making them unavailable for new checks but retaining their data.

### 3.2. Performing Checks

* Users can **start a new check** by selecting a template. The check is a detached copy of the template, unaffected by later template edits.
* Each check is stored as JSON in its own `DataStorage` element.
* A check captures: reference to the template, snapshot of template content, answers to checklist items, comments, checked elements, creator, creation date, and status (draft/completed).
* Checks can be edited while in draft; once marked complete, they become read-only.
* Checks include a list of one or more **Revit element UniqueIds** corresponding to the elements being checked.
* Answers can optionally reference specific element UniqueIds for granular tracking.
* All checks and their details are accessible for review via the client.

### 3.3. Linking to Model Elements

* Each check stores a list of checked Revit element UniqueIds.
* Optionally, answers within a check may also reference individual element UniqueIds.
* All element references use UniqueId (not ElementId) to ensure robustness across file saves, copies, and worksharing.
* If an element referenced in a check is deleted from the model, the system displays a missing element warning in the UI but retains historical data.

### 3.4. User Authentication

* The system uses the active Revit session username for all user attribution.
* No manual login or password is required.
* All actions (create, edit, delete) are permitted for any user; actions are tracked by username.

### 3.5. Application Startup and Shutdown

* A single Ribbon button labeled “Start Quality Checklist App” is provided in Revit.
* Clicking the button starts the backend HTTP server (if not already running) and opens the default browser to the local app URL.
* If the server is already running, only the browser is opened.
* If the server cannot start (e.g., port conflict), an error dialog is shown in Revit with instructions.
* The server shuts down automatically when Revit closes.
* Optionally, a “Stop Server” feature may be added for manual shutdown.

---

## 4. Non-Functional Requirements

* The web client is built with vanilla JavaScript, HTML, and CSS. **No frameworks or external dependencies** are permitted.
* The HTTP server is implemented with `HttpListener` and only built-in .NET/Revit APIs. **No ASP.NET or third-party libraries.**
* All checklist and template data is stored as JSON within Revit’s `DataStorage` elements. No external database or file storage.
* The application is designed for local use on the user’s workstation; no network or cloud functionality is required.
* The system should be responsive and usable on both desktop and tablet browsers.
* The design must support at least 50 concurrent users on a typical workstation.
* Server supports .NET Framework 4.8 (Revit 2024 and below) and .NET 8 (Revit 2025+).

---

## 5. Data Model

### 5.1. Template

* Stored as JSON in a single DataStorage element.
* Fields include:

  * `id` (GUID, internal app-level ID)
  * `dataStorageUniqueId` (Revit DataStorage element UniqueId)
  * `name`
  * `description`
  * `version`
  * `sections` (array of section objects, each with items)
  * `createdBy`, `createdDate`, `modifiedBy`, `modifiedDate`
  * `archived` (bool)

### 5.2. Check

* Stored as JSON in a single DataStorage element.
* Fields include:

  * `id` (GUID, internal app-level ID)
  * `dataStorageUniqueId` (Revit DataStorage element UniqueId)
  * `templateUniqueId` (UniqueId of the template’s DataStorage)
  * `templateSnapshot` (full copy of the template at time of check creation)
  * `checkedElements` (array of Revit UniqueIds)
  * `answers` (array, each with itemId, answer, comment, optional elementUniqueId)
  * `createdBy`, `createdDate`, `modifiedBy`, `modifiedDate`
  * `status` (draft, completed)

### 5.3. General

* All data is versioned (with a `version` field) to support future schema changes.
* Indexing for fast lookups may be handled by a lightweight index DataStorage element.

---

## 6. API Specification

### 6.1. General Endpoints

* **Templates:**

  * `GET /api/templates` — List all templates
  * `POST /api/templates` — Create template
  * `PUT /api/templates/{id}` — Update template
  * `DELETE /api/templates/{id}` — Delete template
  * `POST /api/templates/{id}/archive` — Archive template

* **Checks:**

  * `GET /api/checks` — List all checks
  * `POST /api/checks` — Create new check
  * `PUT /api/checks/{id}` — Update check
  * `GET /api/checks/{id}` — Get check by ID

* **User:**

  * `GET /api/user` — Returns current Revit username

* **App:**

  * `GET /api/status` — Returns server status for diagnostics

* All endpoints accept and return JSON.

* All endpoints are available only on `localhost` and are not network-exposed by default.

### 6.2. Element Selection API

#### 6.2.1. Purpose

* Allows the web client to prompt the user to select one or more elements in the active Revit model.
* Used for associating checklist items or entire checks with Revit elements.

#### 6.2.2. Endpoint

* **Endpoint:** `POST /api/select-elements`
* **Request Payload:**

  ```json
  {
    "count": "single" | "multiple",
    "allowedCategories": ["Doors", "Walls"],  // optional
    "message": "Select elements to include in the check." // optional
  }
  ```
* **Response:**

  ```json
  {
    "status": "ok" | "cancelled" | "error",
    "selectedElementUniqueIds": [
      "8ec9c1c3-5d2f-4fbb-9dc4-b1cfa64cbd72-0001a123",
      "1ff62c84-2e5e-4011-9bf2-3451cd1fbb4d-0001a124"
    ]
  }
  ```

#### 6.2.3. Behavior

* When this endpoint is called:

  * The backend presents a selection prompt to the user in Revit, showing the provided message and enforcing the selection criteria.
  * The user selects the required elements using Revit’s native selection tools.
  * Upon completion, the selected elements’ UniqueIds are returned in the response.
  * If the user cancels the operation, an appropriate response status is returned and no elements are linked.

#### 6.2.4. Constraints

* Only one element selection operation can be active at a time.
* The operation is performed on the main Revit thread.
* If selection fails or is canceled, the response indicates the outcome with status and message.

#### 6.2.5. User Experience

* The user receives a clear, modal prompt in Revit with selection instructions.
* After selection or cancellation, the UI in the web client updates to show the result.

---

## 7. UI Requirements

### 7.1. General

* The client UI must be modern, intuitive, and responsive.
* Design must use accessible color contrast and clear icons.
* No unnecessary clutter—focus on clarity and ease of navigation.

### 7.2. Template Editor

* Users can add, edit, and remove sections and items visually.
* Sections and items must be **rearrangeable by drag-and-drop** using only browser-native APIs.
* Item properties (label, type, required, options) are edited in clear form fields.
* Changes are autosaved or clearly indicated when unsaved.

### 7.3. Checklist Performer

* Clear progress display and section navigation.
* Fast, easy data entry for each item.
* Required fields and validation errors are highlighted.
* Ability to link elements in the model to the current check via element selection (using the `/api/select-elements` endpoint).

### 7.4. Element Linking

* Users can trigger selection of elements in the Revit model, associating them with checks or checklist items.
* The UI displays which elements are linked and highlights if they are missing from the model.

### 7.5. User Display

* The UI shows the current Revit username.
* No login or logout; user changes only if Revit user changes.

---

## 8. Security and Error Handling

* All actions are attributed to the Revit username; no additional user security layers.
* All errors are returned with HTTP status and clear JSON messages.
* The client displays error messages with guidance for resolution.
* If a referenced model element is missing, the UI warns the user and retains historical data.

---

## 9. Integration and Constraints

* All server-side Revit operations (create, update, delete DataStorage) are performed inside a Revit API transaction and on the main thread.
* The server must not block the UI and must gracefully queue or reject requests if a transaction cannot be started.
* No server or background process remains after Revit closes.

---

## 10. Extensibility

* The design supports future expansion, including:

  * Assigning checks to Revit projects
  * PDF or Excel export
  * Integration with BIM 360 or external systems
  * Role-based permissions (if required later)
* All data is versioned and stored as JSON to enable schema migration.

---

**End of Specification**
This document is complete and ready for handover to the development team.
If you need further breakdowns, flow diagrams, or user stories, just ask!
