# Lista de testeos — Familias y Roles

Casos de prueba manuales para el módulo de permisos (Patentes → Familias → Roles).
Pensados para probarse desde la UI (`FamiliasControl` / `RolesControl`), pero cada uno
indica también el método de BLL que se está ejercitando, para poder explicarlo.

Convención de resultado esperado: ✅ = la operación se completa. ❌ = se bloquea y se
muestra un mensaje de error sin romper datos existentes.

## Familias

| ID | Escenario | Pasos | Resultado esperado |
|----|-----------|-------|---------------------|
| F01 | Crear familia con al menos una patente | Nuevo → completar descripción → agregar 1 patente → Guardar | ✅ Se crea la familia con esa patente como hijo |
| F02 | Crear familia sin descripción | Nuevo → dejar descripción vacía → agregar patente → Guardar | ❌ "El campo 'Descripción' no puede estar vacío" |
| F03 | Crear familia sin hijos | Nuevo → completar descripción → Guardar sin agregar nada | ❌ "Una familia debe tener al menos una patente o subfamilia antes de guardar" |
| F04 | Crear familia con descripción duplicada | Nuevo → usar la descripción de una familia ya existente → agregar patente → Guardar | ❌ "Ya existe una familia con la descripción '...'" (`ValidarDescripcionUnica`) |
| F05 | Modificar descripción de una familia existente | Seleccionar familia → cambiar descripción → Guardar | ✅ Se actualiza la descripción, se mantienen sus hijos |
| F06 | Modificar descripción a una ya usada por otra familia | Seleccionar familia A → poner la descripción de la familia B → Guardar | ❌ Mismo mensaje de duplicado que F04 |
| F07 | Agregar patente ya presente en la misma familia | Seleccionar familia → elegir una patente que ya está en su árbol → Agregar patente | ❌ "...porque ya existe en: Familia '...'" (`BuscarRutaPatente`) |
| F08 | Agregar subfamilia que no tiene patentes efectivas | Crear familia vacía-de-patentes (si existiera) → intentar agregarla como subfamilia de otra | ❌ "No se puede agregar una familia sin patentes efectivas" |
| F09 | Agregar una familia como subfamilia de sí misma | Seleccionar familia X → intentar agregarla como su propia subfamilia | ❌ "Una familia no puede contenerse a sí misma" |
| F10 | Generar un ciclo en la jerarquía | Familia A contiene a B → intentar agregar A como subfamilia de B | ❌ "...generaría un ciclo en la jerarquía" (`ContieneFamilia`) |
| F11 | Agregar subfamilia con patente repetida en otra rama | Familia A tiene la patente P → intentar agregarle como subfamilia otra familia que también tiene P | ❌ "...porque la patente '...' ya existe en: ..." |
| F12 | Agregar subfamilia ya asignada directamente a un rol | Familia B está asignada directo a un Rol → intentar agregarla como subfamilia de A | ❌ "...ya está asignada directamente a los roles: ..." |
| F13 | Patente duplicada vía ancestro (no directo) | Familia A contiene a B (subfamilia); intentar agregar a A una patente que ya tiene B | ❌ Bloqueado por `ValidarPatentesEnAncestros`, indica la ruta completa |
| F14 | Patente duplicada vía rol que ya contiene la familia | Rol R contiene a la familia A; intentar agregarle a A una patente que ya tiene en otra rama de R | ❌ Bloqueado por `ValidarPatentesEnRolesQueContienenFamilia` |
| F15 | Quitar patente directa de una familia | Seleccionar familia → seleccionar patente directa en el árbol → Quitar seleccionado | ✅ La patente desaparece del árbol de esa familia |
| F16 | Quitar subfamilia directa | Seleccionar familia → seleccionar subfamilia directa en el árbol → Quitar seleccionado | ✅ La subfamilia (y todo lo que cuelga de ella) desaparece de ese árbol |
| F17 | Intentar quitar un nodo que no es hijo directo (nieto) | Expandir el árbol hasta un nieto (patente dentro de una subfamilia) → Quitar seleccionado | ❌ "Solo se pueden quitar hijos directos de la familia seleccionada" |
| F18 | Eliminar familia sin dependencias | Crear familia "de prueba", sin usarla en nada → Eliminar | ✅ Se elimina, ya no aparece en la grilla ni en los combos |
| F19 | Eliminar familia con hijos propios | Seleccionar una familia que tiene patentes/subfamilias → Eliminar | ❌ "...porque tiene N patente(s) y/o M subfamilia(s) asignadas. Quitá sus hijos antes de eliminarla" |
| F20 | Eliminar familia que es subfamilia de otra | Vaciar los hijos directos de la familia → Eliminar | ❌ "...porque es subfamilia de: '...'. Quitala de esas familias antes de eliminarla" |
| F21 | Eliminar familia asignada directamente a un rol | Vaciar hijos y quitarla como subfamilia, pero dejarla asignada a un Rol → Eliminar | ❌ "...porque está asignada a los roles: '...'. Quitala de esos roles antes de eliminarla" |
| F22 | Eliminar familia ya sin ninguna dependencia (tras F19-F21) | Repetir F19→F20→F21 quitando cada bloqueo y reintentar Eliminar al final | ✅ Ahora sí se elimina |

## Roles

| ID | Escenario | Pasos | Resultado esperado |
|----|-----------|-------|---------------------|
| R01 | Crear rol con al menos un hijo | Nuevo → completar descripción → agregar 1 patente o familia → Guardar | ✅ Se crea el rol con código autogenerado (8 primeros chars del Id en mayúsculas) |
| R02 | Crear rol sin descripción | Nuevo → dejar descripción vacía → Guardar | ❌ "El campo 'Descripción' no puede estar vacío" |
| R03 | Crear rol sin hijos | Nuevo → completar descripción → Guardar sin agregar nada | ❌ "Un rol debe tener al menos una patente o familia antes de guardar" |
| R04 | Crear rol con descripción duplicada | Nuevo → usar la descripción de un rol existente → agregar hijo → Guardar | ❌ "Ya existe un rol con la descripción '...'" |
| R05 | Modificar descripción de un rol existente | Seleccionar rol → cambiar descripción → Guardar | ✅ Se actualiza, se mantienen sus hijos |
| R06 | Agregar patente ya presente (directa o dentro de una familia del rol) | Seleccionar rol → elegir una patente que ya está en su árbol efectivo → Agregar patente | ❌ "...porque ya existe en: Rol '...' > ..." (`BuscarRutaPatente` recursivo) |
| R07 | Agregar familia sin patentes efectivas | Intentar agregar al rol una familia vacía de patentes (si existiera) | ❌ "No se puede agregar una familia sin patentes efectivas" |
| R08 | Agregar familia que ya es subfamilia de otra familia | Familia B es subfamilia de A → intentar agregar B directo a un Rol | ❌ "...ya es subfamilia de: '...'" |
| R09 | Agregar familia con patente que el rol ya tiene | Rol tiene la patente P directa → intentar agregarle una familia que también contiene P | ❌ "...porque la patente '...' ya existe en: ..." |
| R10 | Quitar patente directa del rol | Seleccionar rol → seleccionar patente directa en el árbol → Quitar seleccionado | ✅ Desaparece del árbol de ese rol |
| R11 | Quitar familia directa del rol | Seleccionar rol → seleccionar familia directa en el árbol → Quitar seleccionado | ✅ Esa familia (y todo lo que cuelga) desaparece del árbol del rol |
| R12 | Intentar quitar una patente anidada dentro de una familia del rol | Expandir el árbol hasta una patente dentro de una familia → Quitar seleccionado | ❌ "Solo se pueden quitar hijos directos del rol desde esta pantalla" |
| R13 | Eliminar rol sin hijos | Crear rol "de prueba" sin asignarle nada (no debería poder crearse sin hijos, ver R03; probar con un rol legacy vacío si existe) | ✅ Se elimina |
| R14 | Eliminar rol con hijos propios | Seleccionar un rol con patentes/familias asignadas → Eliminar | ❌ "...porque tiene N patente(s) y/o M familia(s) asignadas. Quitá sus hijos antes de eliminarlo" |
| R15 | Ocultar módulos Roles/Familias/Bitácora para no-administradores | Loguearse con un usuario cuyo rol no sea "Administrador" | ✅ Los botones `rolesBTN`, `familiasBTN`, `bitacoraBTN` quedan deshabilitados y ocultos en `FRMMain` |
| R16 | Patentes efectivas correctas tras Login | Loguearse con un usuario → revisar `UsuarioSesion06AV.Instancia().Patentes` | ✅ Coincide con el árbol completo (patentes directas + de cada familia, recursivo) del rol asignado |

## Extra: singleton de sesión y Relogin

| ID | Escenario | Pasos | Resultado esperado |
|----|-----------|-------|---------------------|
| S01 | Logout normal limpia el singleton | Loguearse → Cerrar Sesión (confirmar) → intentar loguearse de nuevo con cualquier usuario | ✅ El login funciona normalmente: `UsuarioSesion06AV.CerrarSesion()` dejó `UsuarioActual` en `null` |
| S02 | Relogin NO limpia el singleton | Loguearse → menú de usuario → Relogin (confirmar) | ✅ Vuelve a la pantalla de Login, pero `UsuarioSesion06AV.Instancia().UsuarioActual` sigue teniendo el usuario anterior |
| S03 | Ningún login se acepta mientras hay sesión activa | Después de S02, intentar loguearse (mismo usuario o cualquier otro) con credenciales correctas | ❌ "Ya hay una sesión activa en este sistema..." (`SesionActivaException`, chequeado al inicio de `UsuariosBLL06AV.Login`) |
| S04 | El bloqueo es independiente de la contraseña | Repetir S03 con una contraseña incorrecta | ❌ Igual se rechaza por sesión activa (el chequeo de sesión es lo primero que evalúa `Login`, antes de validar credenciales) |
| S05 | Liberar el bloqueo | Cerrar la aplicación y volver a abrirla (el singleton vive en memoria del proceso) | ✅ Al reiniciar el proceso, `UsuarioSesion06AV._Instancia` es `null` de nuevo y el login vuelve a funcionar |

> Nota para la presentación: S01–S05 sirven justamente para mostrar la diferencia entre
> "Cerrar Sesión" (llama a `CerrarSesion()`, libera el singleton) y "Relogin" (no la
> llama, el singleton queda "ocupado"). Es la forma más directa de demostrar que
> `UsuarioSesion06AV` es realmente una única instancia compartida por toda la app y no
> un objeto que se crea de nuevo en cada pantalla.
