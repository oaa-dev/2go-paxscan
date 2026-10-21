# Web Tasks Module (admin portal)

Stack: Next.js 16 App Router (`paxscan-online/frontend/`). All paths below are relative to `paxscan-online/frontend/`.

Task list (filters in URL, pagination), create/edit dialog, delete, and per-task device status history. Data flow: services → React Query hooks → pages; the browser only calls the BFF (`/api/backend/*`).

## Route Files
| File | Type | Notes |
|------|------|-------|
| app/(protected)/tasks/page.tsx | Page (server) | wraps TaskList in Suspense |
| app/(protected)/tasks/task-list.tsx | Client Component | filters ↔ URL params, debounced search, table, pagination |
| app/(protected)/tasks/columns.tsx | Component | column definitions |
| app/(protected)/tasks/actions.tsx | Client Component | row menu: view history, edit, delete (AlertDialog) |
| app/(protected)/tasks/task-form-dialog.tsx | Client Component | react-hook-form + zod; reset on [open, task?.id]; maps 422 errors |
| app/(protected)/tasks/[id]/page.tsx | Page (server) | awaits params inside Suspense (cacheComponents) |
| app/(protected)/tasks/[id]/task-detail.tsx | Client Component | task card + status history (conflict rows highlighted) |
| app/(protected)/layout.tsx | Layout (server) | header; user name streamed in Suspense |

## Connected Files
| Category | File | Notes |
|----------|------|-------|
| Hook | hooks/use-tasks.ts | useTasks, useTask, useCreate/Update/DeleteTask; invalidate ["tasks"] and ["task", id] |
| Service/API Client | services/task-service.ts | axios → /api/backend/v1/tasks |
| Utility | lib/axios.ts | baseURL /api/backend; 401 → /login; apiErrorMessage, apiFieldErrors |
| Utility | lib/query-params.ts | `filter[type|status|q]`, sort, page, per_page (keep in sync with TaskRepository) |
| Zod Schema | lib/validation/task-schema.ts | taskFormSchema (mirrors Laravel rules), loginSchema |
| Type Definition | types/task.ts | wire aliases from types/api.d.ts, labels, ApiEnvelope, Paginated |
| Type Definition | types/api.d.ts | generated: `npm run gen:api` from ../../contracts/openapi.yaml |
| Component | components/task-status-badge.tsx, components/task-type-badge.tsx, components/update-result-badge.tsx | badges |
| Component | components/providers.tsx | QueryClientProvider + Toaster |
| UI | components/ui/* | shadcn base-nova (Base UI: use `render` prop, Select `items` for labels) |

## Tests
| File | Type |
|------|------|
| lib/query-params.test.ts | Vitest unit |
| lib/validation/task-schema.test.ts | Vitest unit |
