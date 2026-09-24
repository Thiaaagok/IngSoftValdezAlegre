# PCFORGE — Informe de oportunidades de patrones de diseño

**Alcance:** todo el negocio del sistema (RFN1 Venta de computadoras, RFN2 Compra de insumos y los servicios transversales).
**Base:** código del repo `IngSoftValdezAlegre` (capas BE / DAL / MPP / BLL / SER / UI).
**Este informe dice DÓNDE y POR QUÉ. No implementa nada.**

---

## 1. Resumen ejecutivo

El proyecto ya tiene **7 patrones aplicados** y bien ubicados. Lo que falta no es "meter más patrones": es que hay **cuatro dolores estructurales concretos** que hoy se resuelven con `if` y copiar-pegar, y para cada uno hay un patrón que encaja natural:

| Dolor real en el código | Dónde duele | Patrón que lo resuelve |
|---|---|---|
| Las transiciones de estado están sueltas como `if (x.Estado != Y) throw` en cada método del BLL | `OrdenProduccionBLL06AV`, `VentasBLL06AV`, `CompraInsumosBLL06AV` | **State** |
| El trío patente + auditoría + recálculo de DV se escribe a mano en cada operación, y si alguien lo olvida el DV se desincroniza en silencio | todos los BLL | **Decorator / Proxy** |
| La compensación manual (reservar↔liberar, consumir↔revertir) está copiada en 3 lugares | `VentasBLL06AV`, `OrdenProduccionBLL06AV` | **Command** |
| `EjecutarSP` / `EjecutarSPNonQuery` copiado en 11 de 15 clases DAL | toda la capa DAL | **Template Method** (clase base) |

Si el TD pide un número acotado de patrones nuevos, **estos cuatro son los que se justifican solos con evidencia del código**. El resto del informe agrega candidatos de segundo orden, ordenados por relación valor/esfuerzo.

---

## 2. Inventario: patrones que YA están aplicados

Conviene documentarlos en el TD antes de proponer nuevos — ya están y están bien.

| Patrón | Dónde | Evidencia |
|---|---|---|
| **Composite** | Seguridad | `SER/IComponentePermiso06AV.cs` + `ComponentePermisoCompuesto06AV.cs` → Rol → Familias → Patentes, con `ObtenerPatentes()` recursivo y control de duplicados en `Agregar`/`AgregarRango` |
| **Singleton** | Sesión, conexión, i18n | `UsuarioSesion06AV.Instancia()`, `DAL/Conexion.cs`, `GestorIdioma06AV.Instancia` |
| **Observer** | UI | `GestorIdioma06AV.IdiomaChanged` y `Tema.TemaChanged`; cada control implementa `IIdiomaAplicable06AV` y se suscribe/desuscribe en el `Disposed` |
| **Strategy** | Integridad | `ICalculadorDigito06AV` con tres implementaciones intercambiables: `CalculadorHexadecimal06AV`, `CalculadorModulo1106AV`, `CalculadorSha25606AV` |
| **Template Method** | UI ABM | `AbmBaseControl06AV`: el flujo del ABM es fijo y los 7 pasos variables son `abstract` (`ConstruirCampos`, `CargarDatosEnGrilla`, `Guardar`, `EliminarSeleccion`, …) |
| **Data Mapper / DAO** | Persistencia | La separación DAL (devuelve `DataTable`) ↔ MPP (`DataRow` → entidad) es exactamente Data Mapper; `MapearOrdenCompra`, `MapearCotizacion`, `MapearComponente` |
| **Facade** | Negocio | `CompraInsumosBLL06AV` expone el RFN2 completo (registrar orden → cotizar → aprobar → recibir) por encima de 3 MPP distintos. Vale nombrarlo así en el documento |

---

## 3. Oportunidades por proceso de negocio

### 3.1 RFN1 — Venta y Producción

#### **State** — el candidato más fuerte del proyecto

**Dónde:** `BE/Enumeraciones06AV.cs` define `EstadoVenta06AV` (Pendiente → Señada → EnProducción → Entregada / Anulada) y `EstadoOrdenProduccion06AV` (Pendiente → Planificada → EnEnsamblaje → Finalizada / EnRevisión → Entregada). Las reglas de transición viven desparramadas:

- `OrdenProduccionBLL06AV.AsignarLinea()` → `if (orden.Estado != Pendiente) throw`
- `OrdenProduccionBLL06AV.IniciarEnsamblaje()` → `if (orden.Estado != Planificada) throw`
- `OrdenProduccionBLL06AV.RegistrarControlCalidad()` → `if (estado != EnEnsamblaje && != EnRevision) throw`
- `OrdenProduccionBLL06AV.VolverAtras()` → un `switch (orden.Estado)` con cuatro casos, que es el olor de libro
- `VentasBLL06AV.RegistrarSena()` / `AnularVenta()` → tres `if` de estado cada uno
- Y la UI lo **vuelve a implementar por tercera vez**: `ComprasControl.EstadoDe()` mapea estado → texto → color → paso siguiente con una cascada de `if`; `ProduccionControl` y `VentasControl` hacen lo mismo con sus propios estados.

**Por qué acá:** la misma máquina de estados está escrita tres veces (BLL para validar, UI para pintar, UI para decidir qué botón mostrar). Agregar un estado obliga a tocar los tres lugares y nada garantiza que queden coherentes. Con State, cada estado sabe qué transiciones admite, qué rótulo y color le corresponde, y cuál es la acción siguiente.

**Extensión natural:** `EstadoOrdenCompra06AV` (Pendiente → Enviada → Finalizada / RecibidaParcial) y `EstadoCotizacion06AV` (PorAprobar → Aprobado / Desaprobada) tienen exactamente el mismo problema en `CompraInsumosBLL06AV` y `CotizacionesControl`.

---

#### **Prototype** — modelo estándar → computadora

**Dónde:** `BE/ModeloEstandar06AV.cs` y `BE/Computadora06AV.cs` tienen la misma forma (`List<Componente06AV>` + `PrecioTotal` calculado). En `VentasControl.AplicarModeloSeleccionado()` se copia la lista de componentes del modelo a la computadora de la venta.

**Por qué acá:** "vender un modelo estándar" **es** clonar un prototipo y después, si el cliente lo pide, tocarle una pieza. Hoy la copia es manual y superficial (`List` compartida), lo que es un riesgo real: modificar la computadora de una venta podría tocar el catálogo. Es, además, el patrón que mejor se explica con el caso de negocio de PC Factory.

---

#### **Builder** — armado de la computadora configurable

**Dónde:** `FRMArmarPc06AV` + `VentasControl._componentesElegidos` arman la `Computadora06AV` pieza por pieza. `UI/ChasisPcControl06AV` y `UI/TarjetaComponente06AV` ya modelan la idea de "slots" (procesador, RAM, disco, placa madre, fuente, gabinete…).

**Por qué acá:** hoy nada impide registrar una venta de una computadora sin placa madre ni fuente: la única validación es `Componentes.Count == 0` en `VentasBLL06AV.RegistrarVenta()`. Un Builder concentra el "qué hace falta para que esto sea una PC válida" en un solo lugar, en vez de dejarlo repartido entre la UI y el BLL.

**Nota:** el proyecto ya tiene tres objetos "armados" ad-hoc que son Builders sin nombre — `OrdenArmada06AV`, `RecepcionArmada06AV` y `CotizacionArmada06AV` (los `EventArgs` de los asistentes de UI). Vale unificar el criterio.

---

#### **Abstract Factory / Factory Method** — estándar vs. configurable

**Dónde:** `TipoConfiguracion06AV { Estandar, Configurable }`. En `VentasControl.ActualizarModeloSegunTipo()` la elección del tipo bifurca toda la pantalla (tarjetas de modelos vs. botón al configurador), con `if` en la UI.

**Por qué acá:** es la única variante real de producto del negocio y hoy se resuelve con condicionales en la capa de presentación. Una familia `IArmadorComputadora06AV` (→ `ArmadorEstandar` / `ArmadorConfigurable`) que devuelva su propio validador y su propia forma de calcular precio saca el `if` de la UI. Encaja bien con el Builder de arriba.

---

#### **Strategy** — forma de pago y política de seña

**Dónde:**
- `FormaPago06AV { Efectivo, Transferencia, Tarjeta }`. El campo `referencia` es un `string` libre sin validación en `VentasBLL06AV.RegistrarSena()` y en `EntregasBLL06AV.RegistrarEntrega()`, y el rótulo se resuelve con un `switch` de texto en `ComprobantePcFactory06AV.TextoFormaPago()`.
- `Venta06AV.PorcentajeSena = 0.50m` es una **constante de clase**.

**Por qué acá:** cada forma de pago tiene reglas distintas (transferencia debería exigir CBU/comprobante, tarjeta los últimos 4 dígitos, efectivo nada) y hoy no hay ninguna. Y el 50% de seña es una regla de negocio congelada en el código: si PC Factory decide 30% para modelos estándar y 50% para configurables, hay que recompilar. Dos Strategy chicos, ambos justificables.

---

### 3.2 RFN2 — Compra de insumos

#### **Chain of Responsibility** — las validaciones en cascada

**Dónde:** `CompraInsumosBLL06AV.RegistrarOrdenCompra()` son ~40 líneas de validaciones secuenciales: patente → repositor autenticado → detalle no vacío → cada componente válido → cada cantidad > 0 → fecha límite ≥ hoy → ningún componente en otra OC en curso. `VentasBLL06AV.RegistrarVenta()` tiene la misma estructura (cliente → computadora → fecha → existencia de componente → stock libre).

**Por qué acá:** son las dos operaciones más largas del sistema y su cuerpo es 60% validación. Una cadena de validadores hace cada regla testeable por separado y reordenable, y deja el método de negocio con la lógica que de verdad le corresponde. Es también el lugar donde los mensajes de error están hoy hardcodeados en castellano, salteando `GestorIdioma06AV` — la cadena sería la oportunidad de normalizar eso.

---

#### **Specification** — los filtros de listados

**Dónde, todos hoy como `Where(...)` sueltos y repetidos:**

- `EntregasBLL06AV.Filtrar(ordenes, criterio)` — búsqueda por texto
- `CotizacionesControl.cboFiltro` — Todas / Sin cotizar / Por resolver / Resueltas
- `VentasBLL06AV.ObtenerListasParaProduccion()` y `Venta06AV.ListaParaProducir`
- `ComponentesBLL06AV.ObtenerBajoStock()` / `Componente06AV.BajoStock`
- `ComprasControl.ProveedoresConOfertaAbierta()` y el cálculo de "componentes en trámite"
- `CompraInsumosBLL06AV.ObtenerPendienteDeRecibir()` — pedido menos recibido

**Por qué acá:** son ocho reglas de selección de negocio ("qué está listo para producir", "qué está bajo stock", "qué falta recibir") escritas como LINQ anónimo dentro del método que las usa. Ninguna se puede reutilizar ni testear. Specification las convierte en objetos con nombre de negocio, componibles con `Y`/`O`.

---

#### **Observer de dominio** — el puente que hoy no existe entre RFN1 y RFN2

**Dónde:** `Componente06AV.BajoStock` dice si un componente llegó al punto de reposición, pero **nadie se entera**: el faltante recién aparece cuando un repositor abre Compras y se listan los faltantes contra la base (`ObtenerFaltantes()`). Del otro lado, `OrdenProduccionBLL06AV.RegistrarControlCalidad()` tiene cableadas dentro tres consecuencias del cierre: consumir stock, liberar la línea de ensamblaje y auditar.

**Por qué acá:** el Observer ya existe en el proyecto pero sólo para la UI (idioma/tema). Llevarlo al dominio cierra el circuito de los dos procesos de negocio: `OrdenFinalizada` → stock, líneas y bitácora se suscriben; `StockBajoMinimo` → el módulo de Compras se entera solo. Para el TD es un argumento fuerte, porque **es el único lugar donde los dos RFN se tocan**.

---

### 3.3 Transversales

#### **Decorator / Proxy** — autorización, auditoría e integridad

**Dónde:** el patrón manual se repite en cada método de escritura del BLL:

```
ExigirPatente(PatenteEnum06AV.X, "hacer Y");   // ← copiado en CompraInsumosBLL
...lógica...
AuditoriaPcFactory06AV.Modificacion(...);      // ← copiado en todos los BLL
```

y `AuditoriaPcFactory06AV.Registrar()` es el único lugar que dispara `IntegridadBLL06AV.RecalcularSeguro()`.

**Por qué acá:** el propio `AGENTS.md` del repo lo declara como gotcha: *"si una escritura saltea el logging de auditoría, la línea base del DV se desincroniza y los logins detectan corrupción falsa. Siempre pasar por el BLL"*. Es decir: **hoy la integridad del sistema depende de una convención que el compilador no verifica**. Además `ExigirPatente` está definido como `private static` sólo en `CompraInsumosBLL06AV` — los demás BLL no exigen patente en absoluto, la autorización está únicamente en el menú (`FRMMain`), que es control de UI, no de negocio.

Un decorador de autorización + auditoría sobre la interfaz de cada BLL convierte esa convención en garantía estructural, y de paso uniforma un chequeo que hoy está aplicado en un solo módulo de seis.

---

#### **Command** — compensación y deshacer

**Dónde:** tres pares do/undo escritos a mano:

| Hacer | Deshacer | Dónde |
|---|---|---|
| `ReservarStock` | `LiberarReservas` | `VentasBLL06AV.RegistrarVenta()` y `AnularVenta()` |
| `ConsumirReserva` | `RevertirConsumo` | `OrdenProduccionBLL06AV.RegistrarControlCalidad()` |
| planificar (ocupa línea) | `VolverAtras()` (suelta línea) | `OrdenProduccionBLL06AV` |

**Por qué acá:** los tres implementan la misma idea — ejecutar N operaciones y, si una falla, deshacer las anteriores — con tres `try/catch` distintos y una `List` acumuladora copiada. No hay transacción de base de datos que los cubra (el ADO.NET del proyecto no usa `SqlTransaction`), así que la compensación en memoria **es** el mecanismo de consistencia. Command lo formaliza y, como bonus, `VolverAtras()` deja de ser un `switch` y pasa a ser un `Undo()` real.

---

#### **Template Method** — la capa DAL

**Dónde:** `EjecutarSP(nombreSP, parametros)` y `EjecutarSPNonQuery(...)` están copiados literalmente en 11 de las 15 clases de `DAL/`. Son 30 líneas idénticas por archivo: abrir conexión, armar `SqlCommand`, `AddWithValue` en loop, `Fill`, cerrar.

**Por qué acá:** es la duplicación más grande y más mecánica del proyecto, y el patrón ya está usado en la UI (`AbmBaseControl06AV`), así que es coherente con la arquitectura existente. Una `DalBase06AV` con los dos métodos protegidos elimina ~300 líneas duplicadas y deja un único punto donde después se puede agregar transacciones, timeout o logging.

**Variante:** si preferís la lectura de "Repositorio genérico" para el TD, un `IRepositorio06AV<T>` sobre los 8 MPP que hacen CRUD idéntico (`ObtenerTodos` / `ObtenerPorId` / `Agregar` / `Modificar` / `Eliminar`) cuenta la misma historia. Igual conviene hacer primero la clase base del DAL.

---

#### **Bridge (o Strategy de formato)** — comprobantes y exportación

**Dónde:** hay **dos jerarquías duplicadas** de exportación conviviendo:
- `SER/Exportacion/{ExportacionPDF, ExportacionEXCEL}.cs`
- `SER/Exportar/{ExportarPDF, ExportarEXCEL}.cs`

Y por separado, `IngSoftValdezAlegre/Common/ComprobantePcFactory06AV.cs` genera recibo de seña y factura, hablándole directo a `PdfSimple06AV`, con un `Cabecera()` privado compartido entre los dos comprobantes.

**Por qué acá:** hay dos ejes que hoy están pegados — *qué documento* (recibo de seña, factura de venta, factura de compra, orden de compra, solicitud de cotización) y *en qué formato sale* (PDF, Excel, impresora). Agregar "solicitud de cotización en PDF" hoy implica escribir el documento de cero. Bridge separa los dos ejes; además el `Cabecera()` que ya existe es un Template Method esperando a ser declarado.

Aparte, la duplicación `Exportacion/` vs `Exportar/` hay que resolverla igual, con o sin patrón.

---

#### **Mediator** — navegación entre vistas

**Dónde:** `ComprasControl` coordina hoy **cuatro** sub-vistas (lista, asistente de nueva orden, pedido de cotización, recepción) y arbitra a mano `Visible = false` + `BringToFront()` en cinco métodos distintos (`MostrarLista`, `AbrirFormularioNueva`, `AbrirPedidoCotizacion`, `Recibir`). `VentasControl` hace lo mismo con tres paneles y `ProduccionControl` con los suyos.

**Por qué acá:** cada vez que se agrega una vista hay que acordarse de apagarla en los otros cuatro métodos — es exactamente el bug que produce paneles superpuestos. Un mediador de navegación (o un `PanelSwitcher` simple) lo vuelve una sola llamada. Prioridad baja para el TD, pero es deuda técnica real y ya se nota.

---

#### **Memento** — backup / restauración

**Dónde:** `IntegridadBLL06AV.Respaldar()` / `Restaurar()` / `ListarBackups()` + `SER/Integridad/InfoBackup06AV.cs`. Y en paralelo, `BE/ComponenteHistorico06AV.cs` con la tabla `Componentes_C`.

**Por qué acá:** es Memento de facto — capturar y restaurar estado sin exponer la estructura interna. No requiere refactor: **alcanza con nombrarlo así en el documento del TD**. Costo cero, suma un patrón documentado.

---

#### **Null Object** — menor

**Dónde:** `?? "—"`, `?.Login ?? ""`, `Cliente?.NombreCompleto`, `Componente != null ? d.Componente.Descripcion : ""` repartidos por toda la UI y los MPP.

**Por qué acá:** es ruido visual constante en las pantallas. Valor real bajo; mencionarlo sólo si el TD pide cantidad de patrones.

---

## 4. Priorización

| # | Patrón | Dónde | Valor | Esfuerzo | Riesgo de romper algo |
|---|---|---|---|---|---|
| 1 | **State** | Orden de producción, Venta, Orden de compra, Cotización | Muy alto | Medio | Medio — toca BLL y UI |
| 2 | **Template Method** (DalBase) | Capa DAL completa | Alto | Bajo | Bajo — mecánico |
| 3 | **Decorator/Proxy** | Autorización + auditoría + DV en todos los BLL | Muy alto | Medio | Medio |
| 4 | **Command** | Reserva/consumo de stock, VolverAtras | Alto | Medio | Medio |
| 5 | **Chain of Responsibility** | Validaciones de RegistrarVenta y RegistrarOrdenCompra | Alto | Bajo | Bajo |
| 6 | **Prototype** | ModeloEstándar → Computadora | Medio-alto | Bajo | Bajo |
| 7 | **Observer de dominio** | OrdenFinalizada, StockBajoMinimo | Alto (cierra RFN1↔RFN2) | Medio | Bajo |
| 8 | **Builder + Abstract Factory** | Armado de PC estándar vs. configurable | Medio-alto | Medio | Bajo |
| 9 | **Specification** | 8 filtros de listados | Medio | Bajo | Muy bajo |
| 10 | **Strategy** | Forma de pago, política de seña | Medio | Bajo | Muy bajo |
| 11 | **Bridge** | Comprobantes × formato de salida | Medio | Medio | Bajo |
| 12 | **Memento** | Backup/restore (ya existe, sólo documentarlo) | Bajo | Nulo | Nulo |
| 13 | **Mediator** | Navegación entre sub-vistas | Bajo-medio | Bajo | Bajo |
| 14 | **Null Object** | Nulos en UI | Bajo | Bajo | Muy bajo |

**Si el TD pide pocos y bien justificados:** 1, 3, 4 y 5. Los cuatro se defienden mostrando el código actual al lado.
**Si el TD premia cobertura del catálogo GoF:** sumar 6, 7, 8 y 10, que son baratos y tienen un caso de negocio claro para explicar en la defensa.

---

## 5. Qué NO conviene meter

Anotado explícitamente porque en un TD la tentación de forzar patrones es alta y el tribunal lo nota:

- **Iterator** — C# ya lo resuelve con `IEnumerable`/`foreach`. Implementarlo a mano sería retroceder.
- **Flyweight** — no hay volumen que lo justifique. El catálogo de componentes es de decenas de filas, no de millones.
- **Visitor** — no hay una jerarquía de entidades estable sobre la que agregar operaciones nuevas. Agregaría indirección sin beneficio.
- **Interpreter** — no hay ningún lenguaje ni expresión que parsear. (El parser de JSON de `GestorIdioma06AV` es un parser ad-hoc, no un Interpreter, y conviene reemplazarlo por una librería antes que patronizarlo.)
- **Abstract Factory a nivel de capa de datos** (una fábrica de DAL por motor) — el sistema es SQL Server y sólo SQL Server. Sería arquitectura especulativa.

---

## 6. Observaciones que salieron del análisis (no son patrones, pero conviene anotarlas)

1. **`ExigirPatente` sólo existe en `CompraInsumosBLL06AV`.** Los otros cinco BLL no verifican patentes: la autorización efectiva está en el filtrado del menú de `FRMMain`, que es control de presentación. Un usuario con acceso al binario podría invocar el BLL sin permiso. Es el argumento más fuerte para el Decorator de autorización.
2. **No se usa `SqlTransaction` en ninguna parte.** La consistencia depende íntegramente de la compensación en memoria (ver Command). Vale mencionarlo en el TD como decisión de diseño explícita, o corregirlo.
3. **Duplicación `SER/Exportacion/` vs `SER/Exportar/`** — dos implementaciones del mismo servicio. Hay que borrar una.
4. **`Venta06AV.PorcentajeSena` es `const`** — regla de negocio compilada.
5. **Mensajes de validación hardcodeados en castellano** dentro de los BLL, salteando `GestorIdioma06AV`, mientras la UI sí está internacionalizada en tres idiomas.
6. **`Usuario.cs` / `Usuario06AV.cs` y `UsuarioSesion.cs` / `UsuarioSesion06AV.cs`** conviven en `SER/` — probablemente restos de una versión anterior. Conviene verificar cuál está muerta antes de documentar la arquitectura.
