# AGENTS.md

## Project

.NET Framework 4.8.1 WinForms desktop app (PC Factory: inventory, sales, production, purchasing). UI text and domain vocabulary are Spanish. Public types carry a `06AV` suffix. **No lint, no typecheck, no test framework, no CI.** Building with msbuild is the only automated verification.

## Build

```cmd
msbuild IngSoftValdezAlegre.sln /p:Configuration=Release
```

- `msbuild` and `nuget` are **not on PATH**. MSBuild lives at `C:\Program Files\Microsoft Visual Studio\<ver>\Community\MSBuild\Current\Bin\MSBuild.exe` (locate with `vswhere`); `nuget` must be run via its own install or Visual Studio's "Restore Packages".
- The `.sln` maps every project's `Debug|Any CPU` → `Release|Any CPU`. Always pass `Configuration=Release`.
- NuGet is legacy `packages.config`, one dependency: `ReaLTaiizor` 3.8.1.8 (WinForms theming, UI project only).
- **All 7 csproj files are legacy (non-SDK) with explicit `<Compile Include="...">` lists.** A new `.cs` file does not compile until you add it to the owning csproj. Verified dead code proves this: `BLL\RolNegocio06AV.cs`, `SER\Usuario.cs`, `SER\UsuarioSesion.cs`, `SER\Exportar\*.cs` exist on disk but are not listed in their csproj and never build. New `.sql` files must be added as `<Content Include="..\SQL\...">` to `Instalador\Instalador.csproj` (6 of 36 SQL files are currently not shipped, incl. all three `99_*` aggregators).
- Entry point: `IngSoftValdezAlegre\Program.cs` → `FRMLogin`. It only calls `EnableVisualStyles`, `SetCompatibleTextRenderingDefault`, `Application.Run(new FRMLogin())`. **There is no global exception handler anywhere in the repo.**
- If a build fails with `MSB3021`/`MSB3027` "archivo bloqueado", the app is still running and holding the DLLs — close it. Use `msbuild /t:Compile` to verify compilation without the copy step.

## Layering

```
SER  (session, permissions, i18n, DV integrity, crypto)   ← base layer
BE   (entities)                              → SER
DAL  (raw ADO.NET, returns DataTable)        → nothing
MPP  (DataRow ↔ entity)                      → BE, DAL, SER
BLL  (validation, business rules, audit)     → BE, MPP, SER
UI   (WinForms)                               → BE, BLL, SER
```

Files are named `<Entity><Layer>06AV.cs`. Stored procedures are `sp_<Entity>_<Action>` with PascalCase Spanish verbs — e.g. `sp_Clientes_ObtenerTodos`, `sp_Componentes_ReservarStock`. Outliers: `sp_ObtenerPatentesUsuario` (verb-first) and `sp_OP_*` for production orders.

## Encoding trap

Source files are **UTF-8, some with BOM, with inconsistent line endings per file** (135 files CRLF, 65 LF, none mixed). PowerShell 5.1's `Get-Content` mangles them into mojibake — always pass `-Encoding UTF8`. When rewriting a file by script, detect and preserve both the BOM and that file's line-ending style, and never `rstrip()` a line you read without normalizing `\r\n` first (that silently converts a CRLF file to LF).

## Database

- Connection resolution (`DAL\Conexion.cs`): `conexion.config` (raw text file next to the exe, contents *is* the connection string) → `ConfigurationManager.ConnectionStrings["IngSoft"]` → hardcoded `Server=.;Database=IngSoftValdezAlegre;Integrated Security=true`. Every step is wrapped in a bare `catch {}` that silently falls through.
- **DAL never translates exceptions** — raw `SqlException` propagates into BLL. The only `catch` blocks in DAL are the two silent swallows in `ResolverCadena` and the rollback rethrow in `GuardarDV`.
- `sp_*` DAL methods take `Dictionary<string,object>` and use `AddWithValue`; `OUTPUT` params need an explicit `SqlParameter`. Connections are opened/closed manually, **not** via `using` — most DAL code leaks the connection if an exception is thrown between `Open()` and `Close()`.
- Transactions exist in exactly one place: `IntegridadDAL06AV.GuardarDV`. Elsewhere atomicity is delegated to the stored procedure (detail rows are passed as XML).
- `UsuariosDAL06AV` and `BitacoraDAL06AV` are built on inline SQL rather than procedures; `IntegridadDAL06AV` does inline DDL plus `BACKUP`/`RESTORE` with the DB name interpolated from the parsed connection string (guarded by a table whitelist).
- `SQL\*.sql` are idempotent (`IF NOT EXISTS` / `CREATE OR ALTER`), numbered `00_`–`11_` (security) and `20_`–`38_` (PC Factory). `99_full_install.sql` is a hand-maintained aggregate that deliberately excludes `07_tabla_dv.sql`, `08_patente_reparacion_dv.sql` and `09_fix_patentes_duplicadas_rol.sql`. `99_recrear_todo_desde_cero.sql` is destructive (`ALTER DATABASE ... SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE`).

## Namespace and exception traps

- `BLL\UsuariosBLL06AV.cs` declares `namespace SER`, not `BLL`. Access it with `using SER;`.
- `BLL\Excepciones\ExcepcionesUsuario.cs` declares `namespace SER.Excepciones`. Two unrelated exception families exist: `BLL.Excepciones.PcFactoryException06AV` (`PCF_*` codes) for the PC Factory domain, and `SER.Excepciones.UsuarioException` (`USR_*`) for users/sessions.
- `IngSoftValdezAlegre\FRMCambiarContrasenia.cs` sits in the root folder but declares `namespace IngSoftValdezAlegre.Controles`.

## Permissions and the DV (Dígito Verificador)

- **`PatenteEnum06AV` member names must equal the `Patentes.Id` values in the database.** `UsuarioSesion06AV.TienePermiso()` does `patente.ToString()` and string-compares against the permissions loaded at login from `sp_ObtenerPatentesUsuario`. A renamed enum member silently loses access.
- DV is a checksum stored per protected table in `DV(Tabla, DVH, DVV)` as hex; the DB-wide value is the in-memory sum of those rows (`SER\Integridad\MotorDigitoVerificador06AV.cs`). The protected-table whitelist is `IntegridadDAL06AV.TablasProtegidas`; `Bitacora` and `IntentosLogin` are deliberately excluded. A protected table that does not exist yet is silently skipped during recalculation.
- `IntegridadBLL06AV.RecalcularSeguro()` is literally `try { Recalcular(); } catch { }` — recalculation failures are invisible. Any write to a protected table that bypasses the BLL desyncs the baseline and the next login reports false corruption.
- `FRMLogin` runs the integrity check **before** authenticating: `Verificar()`; if there is no baseline it calls the unguarded `Recalcular()`; if inconsistent it requires `PatenteEnum06AV.RepararIntegridad` and otherwise exits.

### Audit convention — follow the pattern already in the file

There is no `try/finally`. Audit is a plain call on the success path *after* the write; if the write throws, the audit and the DV recalculation never run. Two coexisting styles:

- 9 PC Factory BLLs (`Clientes`, `Componentes`, `Proveedores`, `LineasEnsamblaje`, `OrdenProduccion`, `Ventas`, `Entregas`, `CompraInsumos`, `ModelosEstandar`) call `AuditoriaPcFactory06AV.Alta/Modificacion/Baja(...)`, which internally does the recalc then the bitácora write, both inside bare `catch {}`.
- The 4 security BLLs (`Usuarios`, `Roles`, `Familias`, `Patentes`) call `BitacoraBLL06AV` directly and wrap `RecalcularSeguro()` in their own private `RecalcularIntegridad()` helper.

`BLL\ComponentesBLL06AV.SumarStock` is a known gap: it writes a protected table with neither audit nor recalc, and only works when called from an enclosing audited flow.

## Domain model worth knowing before editing

- **RFN1:** a sale does not manufacture anything. It only *reserves* components; the real stock deduction happens when the production order is closed (CU06), and delivery/final payment is a separate circuit (CU07, `EntregasBLL06AV`). Sales never touch physical stock.
- **RFN2:** a component cannot sit in two open purchase orders at once, and while a supplier's offer is open that supplier cannot quote the same order twice. Enforced explicitly in `CompraInsumosBLL06AV` and mirrored in the UI (disabled cards, not hidden ones).
- Enum numeric values are persisted. `EstadoOrdenCompra06AV` members must never be reordered — append new ones at the end.
- Soft deletes: components use `Bit_Lo_Bo = 1`; their history lives in `Componentes_C` and is written by a DB trigger, never by application code.

## UI

- `Controles\AbmBaseControl06AV` is the CRUD base: it builds the whole UI in code (`InicializarAbm()`, `AgregarCampo(...)`), provides the grid/form toggle and the standard buttons. Only 5 of 24 controls derive from it; the rest derive from `UserControl` or from the wizard base `UI\AsistenteBase06AV`. Copy `Controles\ClientesControl.cs` for a new ABM screen. The `.Designer.cs` files are stubs — no designer work is needed.
- **i18n**: `Resources\Idiomas\{es,en,pt}.json`, 836 keys each, parsed by a hand-rolled JSON reader in `GestorIdioma06AV` (no Newtonsoft/System.Text.Json despite what a stale doc comment claims). `GestorIdioma06AV.Instancia.Obtener("clave" [, args])` returns the raw key when missing, so **all three files must be updated together** or EN/PT users see raw keys. Keys are case-insensitive; PC Factory keys use a `pcf_` prefix.
- Dark/light theme is `Tema.ToggleTema()` / `TemaChanged`; controls subscribe to it.
- Screens are registered in the `FRMMain` sidebar via `Item(clave, icono, patente, soloAdmin, factory)` and filtered by `sesion.TienePermiso(...)`.

## Installer

`Instalador` is a `WinExe` that attaches a console via P/Invoke for its scriptable mode. The trigger flag is **`--consola`** (not `--console`). Also accepts `--test`, `--silent`, `--help`, `--servidor`, `--bd`, `--usuario`, `--password`, `--scripts`. It runs every `*.sql` in the folder in filename order with no prefix filtering, writes `conexion.config` next to each discovered exe, and patches the `IngSoft` entry in the exe.config as backup. `IngSoftValdezAlegre_Setup.iss` is a separate manual Inno Setup step and is not part of the msbuild build.

## Code comments

The codebase was stripped of comments; only the comments documenting data/business invariants remain (~282 lines in 39 files). Do not assume a method is self-explanatory — the domain rules above are the replacement. Do not reintroduce descriptive comments; keep any new comment that records a non-obvious constraint.
