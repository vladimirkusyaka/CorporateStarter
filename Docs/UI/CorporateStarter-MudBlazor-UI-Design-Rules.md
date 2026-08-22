# CorporateStarter MudBlazor UI Design Rules

## Purpose

CorporateStarter UI should feel like a dense enterprise operations application: fast, clear, restrained, and built around data work. It should not feel like a marketing site, a demo dashboard, or a decorative admin template.

The design reference uses a grid-first business application style. We should extract the principles, not copy another product pixel-for-pixel.

No private source code should be copied into CorporateStarter. Only architectural and visual principles are used.

## Core Visual Direction

- Use MudBlazor everywhere for interactive UI: layout, inputs, buttons, dialogs, drawers, menus, tables, grids, alerts, tooltips, icons.
- Avoid raw HTML controls when MudBlazor provides an equivalent component.
- Keep the visual language utilitarian and precise.
- Prefer dense, scannable screens over spacious presentation layouts.
- The main data grid is usually the primary object on the screen.
- Decorative gradients, oversized cards, hero blocks, illustrative empty states, and marketing composition are not appropriate.

## Reference Architecture Pattern

The private reference app is built around reusable UI primitives rather than one-off pages. CorporateStarter should follow the same idea with its own MudBlazor components:

- Action area above each grid.
- Search/filter component embedded in the action area.
- Data grid as the dominant central surface.
- Edit surface as a right-side panel with header, content, and footer.
- Confirmation and simple decisions as small dialogs.
- Navigation shell as a persistent layout service/component, not per-page markup.

MudBlazor implementation should not mimic Telerik component names. Use CorporateStarter names and MudBlazor internals:

- reference action panel -> `CsGridToolbar`;
- reference editable grid shell -> `CsEntityPage`;
- reference search box -> `CsSearchBox`;
- reference side bar -> `CsEditDrawer`;
- reference form editor -> `CsFormField` / `CsFormLayout`;
- reference confirmation window -> `CsConfirmDialog`.

## Application Shell

- The shell has three persistent regions:
  - left navigation rail or drawer;
  - thin top bar;
  - central work area.
- Top bar height should be compact, around 32-40 px.
- The page title sits near the top-left of the work area, not in a large hero region.
- User/environment info belongs on the top-right in a compact cluster.
- Navigation should support both compact icon rail and expanded menu states.
- Active navigation item uses a clear blue highlight, not a large card treatment.
- Left navigation uses simple line icons and short labels.

## Spacing And Density

- Use compact spacing by default.
- Page padding: 14-20 px.
- Toolbar padding: 8-12 px vertical, 16 px horizontal.
- Grid row height: about 40-44 px.
- Form row height: about 36-40 px.
- Dialog/drawer field vertical gap: 10-14 px.
- Avoid large vertical whitespace unless the screen is intentionally empty.

## Palette

- Primary action blue: `#0d6efd` or Mud primary tuned close to it.
- Focus ring blue: `rgba(96, 165, 250, 0.35)`.
- Header dark gray: `#3f464d` to `#4b5563`.
- App background: `#f4f5f7`.
- Surface background: `#ffffff`.
- Alternating grid row: `#f3f3f3` or `#f5f5f5`.
- Borders: `#d7dde5` / `#e2e8f0`.
- Text: `#111827`.
- Muted text: `#6b7280`.

Do not let the whole app become a blue-gray theme. Blue is for actions, focus, selection, and active navigation.

## Typography

- Use a normal enterprise UI font stack: system UI, Segoe UI, Arial, sans-serif.
- Body text should be compact and readable, usually 13-14 px.
- Grid cell text can be 13-14 px, with medium weight for important identifiers.
- Page titles are small and confident, not hero-sized.
- Dialog titles are 16-18 px.
- Labels are 12-13 px.
- Do not use negative letter spacing.

## Buttons And Toolbar

- Toolbar is a horizontal strip above the grid.
- Left side contains primary commands:
  - Add;
  - Edit;
  - Delete;
  - Refresh;
  - optional expand/open command.
- Right side contains search/filter controls.
- Icon buttons are preferred for common commands.
- Use tooltips on icon-only buttons.
- Primary command buttons use blue outline or filled blue depending on importance.
- Disabled buttons must visibly fade but preserve layout.
- Toolbar buttons should be square-ish, around 38-46 px.
- Do not put command buttons inside decorative cards.
- When the toolbar contains search, reserve the left area for commands and push search to the right.
- Toolbar should remain a single row on desktop and gracefully wrap or compress on narrow screens.
- Icon-only mode is allowed for dense screens, but every icon-only command must have a tooltip.
- Add/Edit/Delete/Refresh/Open are standard commands and should have consistent order across all entity pages.

## Search

- Search lives on the right side of the toolbar.
- Label text such as `Find` may sit immediately before the search box.
- Search field is compact and wide enough for entity names.
- Search button/icon sits at the far right of the search input.
- Search should not dominate the toolbar.
- Search input minimum desktop width should be about 320-360 px.
- On narrower screens, hide the `Find` label first, then let the input shrink.
- The search button/icon belongs visually inside or immediately attached to the input field.
- Search is a page-level grid operation, not a standalone card or form.

## Data Grid

- The grid occupies most of the central work area.
- Header row is dark gray with white text.
- Header cells include sorting/filter icons when applicable.
- Grid rows use alternating light backgrounds.
- Selected row uses light blue highlight.
- Boolean values are shown as checkboxes.
- Column separators should be visible but subtle.
- Pagination sits at the bottom-right.
- Item count sits near the bottom center or bottom-right.
- Empty space below short grids should still feel like part of the grid area.
- Grid should support:
  - sorting;
  - filtering;
  - paging;
  - row selection;
  - double-click edit where appropriate.

MudBlazor mapping:

- Prefer `MudDataGrid<T>` for entity screens when filtering/sorting/paging are needed.
- Use `Dense="true"`.
- Use `Hover="true"`.
- Use `Striped="true"` if custom alternating rows are not implemented.
- Use single-selection for edit/delete workflows.
- Use a shared wrapper/component for consistent toolbar and grid styling.
- Use `FixedHeader="true"` when the grid needs to fill the available area.
- Use `RowsPerPage` values appropriate for dense desktop work: 10, 25, 50, 100.
- Treat row selection as part of the edit workflow: selected row stays highlighted while edit drawer is open.

## Edit Dialogs And Drawers

- Editing should usually open a right-side drawer/panel for enterprise data forms.
- Center dialogs are acceptable for small confirmations or compact forms.
- Right drawer width: 360-440 px for simple entities, 520-720 px for complex forms.
- The background content may remain visible but inactive.
- The selected grid row should remain highlighted while editing.
- Drawer title should be the entity display name for edit mode or `New {Entity}` for create mode.
- Close icon sits top-right.
- Footer actions are pinned at the bottom:
  - Cancel on the left or secondary position;
  - Save on the right as primary.
- Save button uses filled primary blue.
- Cancel uses outlined/default.
- The edit drawer is structurally divided into:
  - header: title and close/actions;
  - content: form fields;
  - footer: pinned action buttons.
- Drawer must not jump or resize while the form validates.
- Drawer opening should not obscure the left navigation or top bar; it overlays the work area from the right.
- If overlay is used, it should be subtle and should communicate that the underlying grid is inactive.

MudBlazor mapping:

- Use `MudDrawer` or a reusable right-panel component for edit forms.
- Use `MudDialog` for confirm delete and small modal tasks.
- Do not build edit surfaces with ad hoc fixed divs if Mud can express the layout.
- Prefer a reusable `CsEditDrawer` over direct `MudDrawer` usage on every page.
- `CsEditDrawer` should expose `Title`, `IsOpen`, `IsBusy`, `ChildContent`, `Actions`, `Cancel`, and `Save`.

## Forms

- Forms use label + compact field alignment.
- In right-side edit panels, labels may be left-aligned in a fixed-width column.
- Required fields should be visually clear but not noisy.
- Focused fields should have a visible blue border and subtle focus ring.
- Avoid excessive helper text.
- Validation messages are compact and placed near the field.
- Checkbox fields should align with the input column, not float randomly.

MudBlazor mapping:

- Use `MudForm`.
- Use `MudTextField`, `MudSelect`, `MudCheckBox`, `MudDatePicker`, `MudNumericField`.
- Use `Variant="Variant.Outlined"` for form fields.
- Use `Margin="Margin.Dense"` for enterprise density.
- Override Mud internals through CSS isolation with `::deep` only in shared UI components or layout-specific CSS.
- Prefer consistent label width in edit drawers, especially for simple entity maintenance screens.
- For compact right drawers, labels can be left of fields on desktop and above fields on narrow widths.
- Boolean fields use `MudCheckBox` aligned to the input column.
- Select-like fields should use `MudSelect` or `MudAutocomplete`, not free text.

## Login Gate

- The application may render the main shell behind a locked overlay.
- If unauthenticated or refresh fails, show a mask over the main window.
- The mask should be gray and mostly opaque, with the main app visible enough to communicate context but inaccessible.
- Login dialog should be compact, polished, and MudBlazor-only.
- Use `MudPaper`, `MudStack`, `MudTextField`, `MudButton`, `MudAlert`.
- No browser storage for access or refresh tokens.
- Login UI must not imply security by itself; API authorization remains authoritative.

Login visual rules:

- Dialog width around 380-420 px.
- Internal padding around 24-30 px.
- Two fields with generous but not loose spacing.
- Focused input has blue border and subtle blue glow.
- Label becomes slightly smaller and stronger when active/shrunk.
- Login button aligns right and uses a login icon.

## Component Structure

For Blazor UI components, use three files:

- `{Component}.razor` for markup;
- `{Component}.razor.cs` for code-behind;
- `{Component}.razor.css` for isolated styles.

Do not put meaningful logic in `.razor` when it belongs in code-behind.
Do not put broad app styling in random page CSS.
Prefer shared UI components for repeated layouts.

Recommended shared components:

- `CsAppShell`
- `CsLoginGate`
- `CsEntityPage`
- `CsGridToolbar`
- `CsSearchBox`
- `CsEditDrawer`
- `CsConfirmDialog`
- `CsFormLayout`
- `CsFormField`
- `CsGridBooleanColumn`
- `CsGridAuditColumns`

## CorporateStarter Entity Page Pattern

Every CRUD entity page should follow this structure:

1. Page title row.
2. Toolbar row:
   - left: Add, Edit, Delete, Refresh;
   - right: Find label, search input, search button.
3. Main grid:
   - sortable/filterable columns;
   - selected row highlight;
   - boolean checkbox display;
   - pagination.
4. Edit surface:
   - right drawer for create/edit;
   - selected row remains visible behind it;
   - pinned footer with Cancel/Save.
5. Delete confirmation:
   - small centered Mud dialog.

Suggested `CsEntityPage` slots:

- `Title`
- `ToolbarActions`
- `Search`
- `Grid`
- `EditDrawer`
- `Footer`

The component should own layout, spacing, and shell CSS. Entity pages should provide data and columns, not reinvent the page structure.

## MudBlazor Mapping Checklist

Use this mapping when translating the reference style into CorporateStarter:

- Left icon rail/menu: `MudDrawer`, `MudNavMenu`, `MudNavGroup`, `MudNavLink`, `MudIconButton`.
- Top bar: `MudAppBar`, `MudText`, `MudSpacer`, `MudMenu`, `MudBadge`.
- Toolbar: `MudPaper` or `MudStack` as container, `MudIconButton`, `MudButton`, `MudTooltip`.
- Search: `MudTextField` with adornment icon or adjacent `MudIconButton`.
- Grid: `MudDataGrid<T>`, `PropertyColumn`, `TemplateColumn`, `PagerContent`.
- Edit drawer: reusable component based on `MudDrawer` or fixed overlay using Mud components only.
- Form fields: `MudForm`, `MudTextField`, `MudSelect`, `MudAutocomplete`, `MudCheckBox`.
- Confirm delete: `MudDialog`.
- Busy state: `MudProgressLinear` for page/grid, `MudProgressCircular` only for local button/content waits.

## Source Privacy Rule

Private reference repositories are read-only inspiration. Do not:

- copy source files;
- copy proprietary component names into public code;
- copy logos, watermarks, client/product names, or business-specific labels;
- preserve unique CSS class names from the reference app;
- include private paths in public project files;
- publish archives containing reference files.

It is acceptable to use observed patterns such as "right-side edit drawer with pinned footer" or "grid-first CRUD page with action toolbar" because these are generic UI architecture patterns.

## Things To Avoid

- Raw `<input>`, `<button>`, or hand-built controls when MudBlazor has a suitable component.
- Landing-page layouts inside the application shell.
- Oversized cards for operational screens.
- Large rounded decorative panels.
- Random one-off CSS per page.
- Colorful dashboards before basic CRUD ergonomics are solid.
- Hiding table/grid functions behind ambiguous icons without tooltips.
- Modal dialogs for everything when a right edit drawer is better.
- Storing tokens in `localStorage` or `sessionStorage`.

## Implementation Rule For Codex

When asked to design or implement CorporateStarter UI:

1. First inspect existing `.razor`, `.razor.cs`, and `.razor.css`.
2. Preserve MudBlazor as the component language.
3. Use the entity page pattern unless the user explicitly asks for a different workflow.
4. Keep density high and visual noise low.
5. If adding a reusable component, explain the component boundary before implementing it.
6. Verify with `dotnet build`.
7. If running the UI locally is possible, inspect the rendered result before claiming it is polished.
