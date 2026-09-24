# AGENTS.md

## Project Overview

.NET Framework 4.8.1 WinForms desktop app — a PC Factory inventory/sales system with a custom security/permissions layer. Language: Spanish. No modern tooling (no lint, no typecheck, no test framework, no CI).

## Build & Run

```
msbuild IngSoftValdezAlegre.sln /p:Configuration=Release
```

- **Quirk:** The `.sln` maps `Debug|Any CPU` → `Release|Any CPU` for all projects. Building "Debug" actually produces Release output. Always pass `Configuration=Release` explicitly.
- NuGet: legacy `packages.config` (UI project only, single dependency: `ReaLTaiizor` for WinForms theming). Run `nuget restore` before building if `packages/` is missing.
- Entry point: `IngSoftValdezAlegre\Program.cs` → `FRMLogin`
- Database: SQL Server. Connection resolved from `conexion.config` (written by Installer) → `App.config` fallback → hardcoded default. No ORM — raw ADO.NET with stored procedures and some inline SQL.

## Architecture (Layer Dependencies)

```
SER (shared domain services, session, i18n, integrity, encryption)
 ↑
BE (entities) ──→ SER
 ↑
DAL (data access, returns DataTables) ──→ nothing
 ↑
MPP (mapper: DataRow ↔ entity) ──→ BE, DAL, SER
 ↑
BLL (business rules, validation, audit) ──→ BE, MPP, SER
 ↑
UI (WinForms) ──→ BE, BLL, SER
```

Key: `DAL` is standalone (no project refs). `SER` is the base layer most projects depend on.

## Naming Conventions

- **All public types end with `06AV`**: `Cliente06AV`, `ClientesBLL06AV`, `FRMLogin`, etc.
- Layer suffixes in class names: `BLL06AV`, `DAL06AV`, `MPP06AV`
- Files: `<Entity><Layer>06AV.cs` (e.g. `ClientesBLL06AV.cs`)
- Stored procedures: `sp_<Entity>_<Action>` (e.g. `sp_Clientes_ObttenerTodos`)
- Enums: domain enums in `BE\Enumeraciones06AV.cs`, permission/audit enums in `SER\Enums\`
- UI forms: `FRM` prefix. UserControls in `Controles\` inherit `AbmBaseControl06AV`

## Critical Gotchas

1. **Namespace/assembly mismatch:** `UsuariosBLL06AV.cs` lives in `BLL\` but declares `namespace SER` (not `BLL`). Use `using SER;` to access it.

2. **Two exception families in different namespaces:**
   - `BLL.Excepciones` — PC Factory domain exceptions (`PcFactoryException06AV` hierarchy, `PCF_*` codes)
   - `SER.Excepciones` — User/session exceptions (`UsuarioException`, `USR_*` codes). Defined in `BLL\ExcepcionesUsuario.cs` but under `SER` namespace.

3. **PatenteEnum06AV must match DB exactly:** `UsuarioSesion06AV.TienePermiso()` uses `.ToString()` to look up permission IDs in the database. Enum member names must match the `Patentes.Id` column values.

4. **Dígito Verificador (DV):** Custom data-integrity system. After every write, `IntegridadBLL06AV.RecalcularSeguro()` recalculates checksums. If a write bypasses audit logging, the DV baseline desyncs and logins detect false corruption. Always go through BLL (which calls audit) — never bypass it.

5. **Audit logging swallows errors:** `AuditoriaPcFactory06AV` wraps everything in `try/catch {}`. Audit failures are silent by design but mean integrity recalculation can silently fail.

6. **Installer is WinExe, not console:** `Instalador\Instalador.csproj` is `WinExe` that allocates a console via P/Invoke for `--console` mode. It runs SQL scripts from `SQL/` in filename order, writes `conexion.config`, and creates desktop shortcuts.

7. **SQL scripts are idempotent:** Use `IF NOT EXISTS` / `CREATE OR ALTER`. Numbered by domain: `00_`–`11_` (security), `20_`–`30_` (PC Factory). `99_full_install.sql` aggregates them all.

## Files to Know

| Path | Purpose |
|------|---------|
| `IngSoftValdezAlegre\Program.cs` | App entry point |
| `IngSoftValdezAlegre\App.config` | Default connection string, app settings |
| `DAL\Conexion.cs` | DB connection singleton, config resolution order |
| `MPP\*MPP06AV.cs` | All data mapping (DataRow ↔ entity) |
| `BLL\ExcepcionesUsuario.cs` | `SER.Excepciones` namespace (despite BLL location) |
| `SER\Usuarios\UsuariosBLL06AV.cs` | `SER` namespace (despite BLL location) |
| `SER\Integridad\MotorDigitoVerificador06AV.cs` | DV calculation engine |
| `BE\Enumeraciones06AV.cs` | All domain enums |
| `SER\Enums\PatenteEnum06AV.cs` | Permission enum (must match DB) |
| `IngSoftValdezAlegre\Common\PdfSimple06AV.cs` | Hand-written PDF generation |
| `SQL\` | Idempotent DB scripts, run in filename order |
| `IngSoftValdezAlegre\Resources\Idiomas\{es,en,pt}.json` | i18n flat JSON (manual parsing, no Newtonsoft) |
