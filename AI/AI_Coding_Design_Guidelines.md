# 📘 AI Coding Design Guidelines Manual

> Applicable to: .NET Web Backend Development (AI Collaboration Mode)  
> Purpose: To ensure AI follows a unified architecture, security, and responsible development standard during collaboration.

---

## 🎯 Principle 1: Centralized Management of Magic Values

- All hardcoded values should be centrally managed.
- Includes configuration values, constants, domain dictionaries, route definitions, etc.
- Configurations should be placed in `appsettings.json` and accessed via `IOptions<T>`.
- Constants should be separated into semantically meaningful static classes.
- Domain values should use enums or strongly typed value objects.
- API routes should be defined and maintained in centralized route modules.

---

## 🎯 Principle 2: Modularity and Object-Oriented Reuse

- Identical logic must not be repeated.
- Reusable logic should be refactored into services, helpers, or extension methods.
- Strict adherence to the Single Responsibility Principle (SRP).
- Prefer reusing existing logic over writing new implementations.
- Avoid premature abstraction or over-design.

---

## 🎯 Principle 3: Respect Module Boundaries, No Cross-Module Modifications

- AI may only modify code within the current module, not others.
- Must not change shared functions, enums, models, or constants from outside modules.
- If dependency conflict is detected, AI should report it and request context extension.

---

## 🎯 Principle 4: Never Alter Database Schema Without Approval

- Never create, remove, or alter database tables or columns without explicit instruction.
- All DDL (Data Definition Language) operations require review by human or DBA agent.
- Schema structures act as shared contracts across modules.
- AI should not generate `ALTER`, `DROP`, `RENAME`, `ADD COLUMN` or similar commands.

---

## 🎯 Principle 5: Face Problems, Never Bypass or Comment Out Errors

- Do not comment out or delete error-prone code to “make the problem go away.”
- Always investigate the root cause and explain the fix clearly.
- If the issue cannot be resolved, AI must report it and request additional context rather than ignoring it.

---

## 🎯 Principle 6: Log Important Events and Failures for Debugging

AI must log key execution points, including but not limited to:

- User actions (e.g., login, password change)
- Critical business steps (e.g., order submission, payment, approval)
- External API/database calls
- Exception handling blocks
- Access control events (e.g., failed logins)
- Data modifications (CRUD operations)
- Background jobs and scheduled task execution
- ML model inference and recommendation outputs

---

## 🎯 Principle 7: Centralized Data Access Layer with Annotations

- All DB access must be done through centralized repositories or data access modules.
- No querying directly from controller or service layers.
- Each query must log the context and execution.
- Every query must include a **clear comment in Chinese** explaining its business intent and use.

---

## 🎯 Principle 8: Always Follow Security Best Practices

- Do not hardcode secrets (passwords, API keys) in code.
- Sanitize and validate all user input; never directly concatenate it into SQL or HTML.
- Do not expose stack traces or sensitive details in error responses.
- Enforce least privilege principle for all resources and API access.
- Log files must not contain personal data, passwords, tokens, etc.
- AI should always ensure auth checks are in place for protected operations.

---

## 🎯 Principle 9: Violation Warning and Manual Confirmation Required

- If AI detects that an action may violate any of these principles, it must stop and issue a warning.
- High-risk actions (e.g., schema edits, deletions, cross-module changes) require manual confirmation.
- No execution should proceed without clear and explicit user approval.
- When possible, AI should propose safer alternatives to fulfill the goal.

---

> 📌 All AI agents must strictly adhere to these principles.  
> If any action would violate them, the AI must immediately halt and escalate the situation to the user for review.