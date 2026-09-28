//
// ===========================================================================
//  PCFORGE - GENERADOR DE MODELO RFN2  (version AUTONOMA, sin ventana Scripting)
//  Trabajo de Diploma - UAI - Valdez / Alegre
// ===========================================================================
//
//  PARA QUE SIRVE ESTA VERSION
//  ---------------------------
//  Es el mismo generador que el script de la ventana "Scripting" de EA, pero
//  no necesita esa ventana para nada. Se ejecuta desde la consola de Windows
//  y le habla a Enterprise Architect por automatizacion (COM). Sirve cuando
//  EA no deja crear scripts nuevos (opcion "New JScript" en gris).
//
//  COMO SE USA
//  -----------
//   1. CERRAR Enterprise Architect (para que el proyecto no quede bloqueado).
//   2. Abrir el Simbolo del sistema (cmd) y escribir:
//
//        cscript //nologo "RUTA\DE\ESTE\ARCHIVO.js" "C:\ruta\a\tu\proyecto.eapx"
//
//      Ejemplo real:
//        cscript //nologo "C:\Users\Thiago\source\repos\IngSoftValdezAlegre\Documentacion\PCFORGE_RFN2_EA_Standalone.js" "C:\Users\Thiago\Documents\PCFORGE.eapx"
//
//   3. Se ve el avance en la consola. Al terminar, abrir el proyecto en EA:
//      el paquete "PCFORGE - RFN2" ya esta en el Project Browser.
//
//  SI DA ERROR "No se puede crear el objeto ActiveX"
//  -------------------------------------------------
//  Es que EA esta instalado en 32 bits y cscript corrio en 64. Usar:
//        C:\Windows\SysWOW64\cscript.exe //nologo "...js" "...eapx"
//
//  Si se ejecuta dos veces crea un paquete nuevo con sufijo numerico, asi
//  que no pisa lo anterior.
// ===========================================================================

// --- Constantes de EA que en la ventana Scripting vienen del !INC ---------
var promptOK = 1;

// --- Sustitutos de Session.Output / Session.Prompt ------------------------
var Session = {
    Output: function (m) { WScript.Echo(m); },
    Prompt: function (m, t) { WScript.Echo(m); }
};

// --- Repositorio de EA: se abre mas abajo, en el arranque -----------------
var Repository = null;


/*
 * ===========================================================================
 *  PCFORGE — GENERADOR DE MODELO RFN2 (Compra de insumos)
 *  Trabajo de Diploma — UAI — Valdez / Alegre
 * ===========================================================================
 *
 *  QUÉ HACE
 *  --------
 *  Crea dentro del modelo abierto un paquete "PCFORGE - RFN2" con:
 *
 *    N01.RF2 Prediseño
 *       └─ Descripción funcional del proceso
 *          (Entradas / Comportamiento / Salida)
 *
 *          El resto del prediseño — roles, secuencia de roles, actividad y
 *          conceptual — lo genera PCFORGE_RFN2_Predisenio_EA.js, aparte.
 *
 *    N02.RF2 Diseño
 *       ├─ CU08 Registrar orden de compra ....... DS + DC + DER + GUI
 *       ├─ CU09 Registrar/seleccionar proveedor . DS + DC + DER + GUI
 *       ├─ CU10 Generar solicitud de cotización . DS + DC + DER + GUI
 *       ├─ CU11 Aprobar solicitud de cotización . DS + DC + DER + GUI
 *       └─ CU12 Recibir insumos y cerrar orden .. DS + DC + DER + GUI
 *
 *  21 diagramas en total, con los elementos ya posicionados, las clases con
 *  sus atributos y operaciones reales, y las relaciones dibujadas.
 *
 *  CÓMO SE USA
 *  -----------
 *   1. Abrir el proyecto (.eap / .eapx / .qea) en Enterprise Architect.
 *   2. Menú  Specialize > Tools > Scripting   (en EA 15 y anteriores:
 *      Tools > Scripting). Se abre la ventana "Scripting".
 *   3. Si "New JScript" aparece en gris, es porque el grupo seleccionado es
 *      de solo lectura: clic derecho sobre el espacio vacío de la lista >
 *      New Group... (tipo Normal), y recién ahí clic derecho sobre el grupo
 *      nuevo > New JScript.
 *   4. Pegar TODO este archivo dentro del script y guardar (Ctrl+S).
 *   5. Clic derecho sobre el script > Run Script.
 *   6. Ver el avance en la ventana  View > System Output > Script  .
 *
 *  Si se ejecuta dos veces crea un paquete nuevo con sufijo numérico, así
 *  que no pisa lo anterior: se puede correr, revisar y borrar el paquete.
 *
 *  DESPUÉS DE CORRER
 *  -----------------
 *  Para que los DER se vean con notación de Martin (patas de gallo), abrir
 *  cada diagrama > clic derecho > Properties > Connectors >
 *  Connector Notation = Information Engineering.
 * ===========================================================================
 */

// ══════════════════════════════════════════════════════════════════════════
//  CONFIGURACIÓN
// ══════════════════════════════════════════════════════════════════════════

var NOMBRE_RAIZ = "PCFORGE - RFN2 Compra de insumos";

// Geometría de los diagramas (en unidades de EA).
var LIFELINE_ANCHO   = 130;   // ancho de cada línea de vida en el DS
var LIFELINE_SEP     = 40;    // separación entre líneas de vida
var CLASE_ANCHO      = 280;
var CLASE_ALTO       = 70;
var CLASE_SEP_X      = 45;
var MAX_MIEMBROS     = 14;   // tope de atributos+operaciones por caja
var CLASE_SEP_Y      = 70;

// ══════════════════════════════════════════════════════════════════════════
//  DATOS DEL MODELO
//  Todo lo que sigue es DECLARATIVO: para cambiar un mensaje, una clase o una
//  columna, se edita acá y se vuelve a correr el script.
// ══════════════════════════════════════════════════════════════════════════

/*
 *  ECS — Esquema Entrada / Comportamiento / Salida del proceso de negocio.
 *  Enfoque sistémico: qué entra al sistema, qué hace el sistema con eso, y
 *  qué sale. Cada bloque es un elemento del diagrama.
 */
var ECS = {
    entradas: [
        "Nivel de stock y stock mínimo de cada insumo",
        "Insumos detectados por debajo del mínimo",
        "Cantidades a solicitar definidas por el Repositor",
        "Fecha límite de entrega",
        "Datos de los Proveedores (Nombre, CUIT, email, teléfono, dirección)",
        "Ofertas de los proveedores (costo y condiciones)",
        "Decisión del Gerente de compras (aprobar / desaprobar)",
        "Cantidades físicamente recibidas en la entrega"
    ],
    comportamiento: [
        "Detectar los insumos con stock por debajo del mínimo",
        "Armar la Orden de compra con los faltantes y la fecha límite",
        "Generar el número de compra",
        "Seleccionar un Proveedor existente o registrar uno nuevo",
        "Registrar el Pedido de cotización en estado Por aprobar",
        "Comparar las ofertas de una misma orden (costo y condiciones)",
        "Aprobar una cotización y desaprobar las demás de esa orden",
        "Enviar la Orden de compra al proveedor adjudicado",
        "Controlar la recepción: contrastar lo pedido contra lo recibido",
        "Registrar la Factura de compra con las cantidades reales",
        "Sumar al stock únicamente lo efectivamente recibido",
        "Cerrar la orden, o dejarla Recibida parcial con el detalle pendiente"
    ],
    salidas: [
        "Orden de compra (Pendiente -> Enviada -> Recibida parcial -> Finalizada)",
        "Pedido de cotización (Por aprobar -> Aprobado / Desaprobado)",
        "Proveedor registrado",
        "Orden de compra enviada al proveedor adjudicado",
        "Factura de compra con las cantidades recibidas",
        "Stock de insumos actualizado",
        "Detalle de lo que quedó pendiente de recibir"
    ]
};

/*
 *  Los cinco casos de uso del RFN2.
 *
 *  Cada uno define:
 *    id, nombre, actor
 *    lifelines : líneas de vida del Diagrama de Secuencia, en orden de
 *                izquierda a derecha: actor, UI_96VA (Boundary), las
 *                clases de BLL / MPP / SER, y DAL_96VA a la derecha.
 *    mensajes  : [origen, destino, texto] en el orden en que ocurren.
 *    clases    : clases del Diagrama de Clases, agrupadas por capa.
 *    tablas    : tablas del DER, con sus columnas.
 *    relaciones: relaciones del DER [tablaOrigen, tablaDestino, cardinalidad].
 *    gui       : pantalla y sus controles.
 */
var CASOS = [

// ───────────────────────────────────────────────────────────────── CU08 ───
{
    id: "CU08",
    nombre: "Registrar orden de compra",
    actor: "Repositor",
    lifelines: [
        "Repositor",
        "UI_96VA",
        "CompraInsumosBLL_96VA",
        "ComponentesBLL_96VA",
        "ComprasMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Repositor",            "UI_96VA",              "seleccionar Nueva orden de compra()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "ObtenerFaltantes()"],
        ["CompraInsumosBLL_96VA", "ComponentesBLL_96VA",  "ObtenerBajoStock()"],
        ["ComponentesBLL_96VA",  "CompraInsumosBLL_96VA", ":List<Componente96VA>"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              "faltantes"],
        ["Repositor",            "UI_96VA",              "elegirInsumos(cantidades)"],
        ["Repositor",            "UI_96VA",              "ingresarFechaLimite(fecha)"],
        ["Repositor",            "UI_96VA",              "Continuar()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "RegistrarOrdenCompra(faltantes, fechaLimite, repositor)"],
        ["CompraInsumosBLL_96VA", "CompraInsumosBLL_96VA", "ExigirPatente(RegistrarOrdenCompra)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "AgregarOrdenCompra(oc)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "AgregarOrdenCompra(id, fechaLimite, dniRepositor)"],
        ["DAL_96VA",             "ComprasMPP_96VA",      ":NumeroCompra"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "AgregarDetalle(idOrdenCompra, codigo, cantidad)"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              ":OrdenCompra96VA (Pendiente)"],
        ["UI_96VA",              "Repositor",            "mostrar numero de compra"]
    ],
    clases: {
        "UI":  ["ComprasControl"],
        "BLL": ["CompraInsumosBLL96VA", "ComponentesBLL96VA"],
        "MPP": ["ComprasMPP96VA", "ComponentesMPP96VA"],
        "DAL": ["ComprasDAL96VA", "ComponentesDAL96VA"],
        "BE":  ["OrdenCompra96VA", "DetalleComponente96VA", "Componente96VA"]
    },
    tablas: [
        ["OrdenesCompra",      ["Id : NVARCHAR(30) (PK)", "NumeroCompra : INT (UQ)",
                                "FechaLimite : DATE", "DniRepositor : NVARCHAR(20) (FK)",
                                "Estado : INT", "FechaCierre : DATETIME"]],
        ["OrdenCompraDetalle", ["IdOrdenCompra : NVARCHAR(30) (PK,FK)",
                                "CodigoComponente : NVARCHAR(50) (PK,FK)", "Cantidad : INT"]],
        ["Componentes",        ["Codigo : NVARCHAR(50) (PK)", "Descripcion : NVARCHAR(200)",
                                "PrecioUnitario : DECIMAL(18,2)", "Stock : INT",
                                "StockMinimo : INT", "StockReservado : INT"]],
        ["Usuarios",           ["Dni : NVARCHAR(20) (PK)", "Nombre : NVARCHAR(100)",
                                "Apellido : NVARCHAR(100)"]]
    ],
    relaciones: [
        ["Usuarios",      "OrdenesCompra",      "1", "0..*"],
        ["OrdenesCompra", "OrdenCompraDetalle", "1", "1..*"],
        ["Componentes",   "OrdenCompraDetalle", "1", "0..*"]
    ],
    gui: {
        pantalla: "Nueva orden de compra",
        controles: [
            "flpFaltantes : lista de insumos bajo el minimo",
            "MedidorReposicion : barra stock actual vs minimo, por insumo",
            "SelectorCantidad : [-] n [+] cuanto reponer de cada insumo",
            "dtpFechaLimite : DateTimePicker - fecha limite de entrega",
            "lblResumen : unidades y renglones seleccionados",
            "btnContinuar : Button - Registrar la orden",
            "btnVolver : Button - Volver"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU09 ───
{
    id: "CU09",
    nombre: "Registrar o seleccionar proveedor",
    actor: "Repositor",
    lifelines: [
        "Repositor",
        "UI_96VA",
        "ProveedoresBLL_96VA",
        "ProveedoresMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Repositor",          "UI_96VA",            "abrirSeleccionDeProveedor()"],
        ["UI_96VA",            "ProveedoresBLL_96VA", "ObtenerTodos()"],
        ["ProveedoresBLL_96VA", "ProveedoresMPP_96VA", "ObtenerTodos()"],
        ["ProveedoresMPP_96VA", "DAL_96VA",           "ObtenerTodos()"],
        ["DAL_96VA",           "ProveedoresMPP_96VA", ":DataTable"],
        ["ProveedoresBLL_96VA", "UI_96VA",            ":List<Proveedor96VA>"],
        ["Repositor",          "UI_96VA",            "seleccionarProveedor(prov)"],
        ["Repositor",          "UI_96VA",            "Nuevo proveedor()"],
        ["UI_96VA",            "UI_96VA",            "ShowDialog()"],
        ["Repositor",          "UI_96VA",            "cargarDatos(nombre, cuit, email, telefono, direccion)"],
        ["UI_96VA",            "ProveedoresBLL_96VA", "Crear(proveedor)"],
        ["ProveedoresBLL_96VA", "ProveedoresBLL_96VA", "ValidarCuitUnico(cuit)"],
        ["ProveedoresBLL_96VA", "ProveedoresMPP_96VA", "Agregar(p)"],
        ["ProveedoresMPP_96VA", "DAL_96VA",           "Agregar(nombre, cuit, email, telefono, direccion)"],
        ["UI_96VA",            "Repositor",          "proveedor confirmado para la orden"]
    ],
    clases: {
        "UI":  ["ComprasControl", "FRMNuevoProveedor96VA", "ProveedoresControl"],
        "BLL": ["ProveedoresBLL96VA", "CompraInsumosBLL96VA"],
        "MPP": ["ProveedoresMPP96VA"],
        "DAL": ["ProveedoresDAL96VA"],
        "BE":  ["Proveedor96VA", "OrdenCompra96VA"]
    },
    tablas: [
        ["Proveedores",        ["Id : INT (PK)", "Nombre : NVARCHAR(150)", "Cuit : NVARCHAR(20) (UQ)",
                                "Email : NVARCHAR(150)", "Telefono : NVARCHAR(50)",
                                "Direccion : NVARCHAR(200)"]],
        ["PedidosCotizacion",  ["Numero : NVARCHAR(30) (PK)", "IdOrdenCompra : NVARCHAR(30) (FK)",
                                "IdProveedor : INT (FK)", "Estado : INT"]],
        ["OrdenesCompra",      ["Id : NVARCHAR(30) (PK)", "NumeroCompra : INT (UQ)",
                                "FechaLimite : DATE", "Estado : INT"]]
    ],
    relaciones: [
        ["Proveedores",   "PedidosCotizacion", "1", "0..*"],
        ["OrdenesCompra", "PedidosCotizacion", "1", "0..*"]
    ],
    gui: {
        pantalla: "Seleccionar proveedor / Nuevo proveedor",
        controles: [
            "cboProveedor : ComboBox - proveedores existentes",
            "lblDatosProveedor : nombre y CUIT del seleccionado",
            "btnNuevoProveedor : Button - abre el alta rapida",
            "txtNombre : TextBox - razon social",
            "txtCuit : TextBox - CUIT (unico)",
            "txtEmail : TextBox - correo de contacto",
            "txtTelefono : TextBox - telefono",
            "txtDireccion : TextBox - direccion",
            "btnGuardar : Button - Guardar proveedor",
            "btnConfirmar : Button - Confirmar proveedor"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU10 ───
{
    id: "CU10",
    nombre: "Generar solicitud de cotizacion",
    actor: "Repositor",
    lifelines: [
        "Repositor",
        "UI_96VA",
        "CompraInsumosBLL_96VA",
        "ComprasMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Repositor",            "UI_96VA",              "abrirMesaDeCotizaciones()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "ObtenerOrdenesCompra()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "ObtenerCotizacionesPorOrden(idOrdenCompra)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "ObtenerCotizacionesPorOrden(id)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "ObtenerCotizacionesPorOrden(id)"],
        ["DAL_96VA",             "ComprasMPP_96VA",      ":DataTable"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              "ofertas de la orden"],
        ["Repositor",            "UI_96VA",              "RegistrarOferta(proveedor, costo, condiciones)"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "RegistrarCotizacion(idOrdenCompra, idProveedor, costo, condiciones)"],
        ["CompraInsumosBLL_96VA", "CompraInsumosBLL_96VA", "ExigirPatente(GestionarCotizaciones)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "AgregarCotizacion(cot)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "AgregarCotizacion(numero, idOrdenCompra, idProveedor, costo, condiciones)"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              ":PedidoCotizacion96VA (Por aprobar)"],
        ["UI_96VA",              "Repositor",            "mostrar la oferta en la mesa comparativa"]
    ],
    clases: {
        "UI":  ["CotizacionesControl", "ComprasControl"],
        "BLL": ["CompraInsumosBLL96VA", "ProveedoresBLL96VA"],
        "MPP": ["ComprasMPP96VA", "ProveedoresMPP96VA"],
        "DAL": ["ComprasDAL96VA", "ProveedoresDAL96VA"],
        "BE":  ["PedidoCotizacion96VA", "DetalleComponente96VA", "Proveedor96VA", "OrdenCompra96VA"]
    },
    tablas: [
        ["PedidosCotizacion",  ["Numero : NVARCHAR(30) (PK)", "IdOrdenCompra : NVARCHAR(30) (FK)",
                                "IdProveedor : INT (FK)", "Estado : INT",
                                "Costo : DECIMAL(18,2)", "Condiciones : NVARCHAR(500)",
                                "FechaEmision : DATETIME",
                                "DniGerenteAprobador : NVARCHAR(20) (FK)"]],
        ["OrdenesCompra",      ["Id : NVARCHAR(30) (PK)", "NumeroCompra : INT (UQ)",
                                "FechaLimite : DATE", "Estado : INT"]],
        ["OrdenCompraDetalle", ["IdOrdenCompra : NVARCHAR(30) (PK,FK)",
                                "CodigoComponente : NVARCHAR(50) (PK,FK)", "Cantidad : INT"]],
        ["Proveedores",        ["Id : INT (PK)", "Nombre : NVARCHAR(150)", "Cuit : NVARCHAR(20) (UQ)"]]
    ],
    relaciones: [
        ["OrdenesCompra", "PedidosCotizacion",  "1", "0..*"],
        ["Proveedores",   "PedidosCotizacion",  "1", "0..*"],
        ["OrdenesCompra", "OrdenCompraDetalle", "1", "1..*"]
    ],
    gui: {
        pantalla: "Mesa de cotizaciones",
        controles: [
            "flpOrdenes : ordenes de compra en curso, con su situacion",
            "TarjetaCotizacion : una por oferta, con proveedor, costo y plazo",
            "cboProveedor : ComboBox - proveedor de la nueva oferta",
            "txtCosto : TextBox - costo ofertado",
            "txtCondiciones : TextBox - plazo y condiciones",
            "btnRegistrarOferta : Button - Cargar oferta",
            "btnNuevoProveedor : Button - alta rapida de proveedor (CU09)",
            "lblMejorOferta : marca la oferta mas conveniente"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU11 ───
{
    id: "CU11",
    nombre: "Aprobar solicitud de cotizacion",
    actor: "Gerente de compras",
    lifelines: [
        "Gerente de compras",
        "UI_96VA",
        "CompraInsumosBLL_96VA",
        "ComprasMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Gerente de compras",   "UI_96VA",              "abrirMesaDeCotizaciones()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "ObtenerCotizacionesPorOrden(idOrdenCompra)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "ObtenerCotizacionesPorOrden(id)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "ObtenerCotizacionesPorOrden(id)"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              "ofertas Por aprobar"],
        ["Gerente de compras",   "UI_96VA",              "revisarDetalle(oferta)"],
        ["Gerente de compras",   "UI_96VA",              "Adjudicar()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "AprobarCotizacion(numeroCotizacion, gerente)"],
        ["CompraInsumosBLL_96VA", "CompraInsumosBLL_96VA", "ExigirPatente(AprobarCotizacion)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "CambiarEstadoCotizacion(numero, Aprobada, dniGerente)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "CambiarEstadoCotizacion(numero, estado, dniGerente)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "CambiarEstadoCotizacion(las demas, Desaprobada)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "CambiarEstadoOrdenCompra(id, Enviada)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "CambiarEstadoOrdenCompra(id, estado)"],
        ["UI_96VA",              "Gerente de compras",   "mostrar la oferta adjudicada"]
    ],
    clases: {
        "UI":  ["CotizacionesControl"],
        "BLL": ["CompraInsumosBLL96VA"],
        "MPP": ["ComprasMPP96VA"],
        "DAL": ["ComprasDAL96VA"],
        "BE":  ["PedidoCotizacion96VA", "OrdenCompra96VA", "Proveedor96VA"]
    },
    tablas: [
        ["PedidosCotizacion", ["Numero : NVARCHAR(30) (PK)", "IdOrdenCompra : NVARCHAR(30) (FK)",
                               "IdProveedor : INT (FK)", "Estado : INT",
                               "Costo : DECIMAL(18,2)",
                               "DniGerenteAprobador : NVARCHAR(20) (FK)"]],
        ["OrdenesCompra",     ["Id : NVARCHAR(30) (PK)", "NumeroCompra : INT (UQ)", "Estado : INT"]],
        ["Proveedores",       ["Id : INT (PK)", "Nombre : NVARCHAR(150)", "Cuit : NVARCHAR(20) (UQ)"]],
        ["Usuarios",          ["Dni : NVARCHAR(20) (PK)", "Nombre : NVARCHAR(100)",
                               "Apellido : NVARCHAR(100)"]]
    ],
    relaciones: [
        ["OrdenesCompra", "PedidosCotizacion", "1", "0..*"],
        ["Proveedores",   "PedidosCotizacion", "1", "0..*"],
        ["Usuarios",      "PedidosCotizacion", "1", "0..*"]
    ],
    gui: {
        pantalla: "Mesa de cotizaciones - aprobacion",
        controles: [
            "flpOrdenes : ordenes con ofertas Por aprobar",
            "TarjetaCotizacion : proveedor, costo, condiciones y plazo",
            "lblDetalleInsumos : insumos y cantidades de la orden",
            "btnAdjudicar : Button - Aprobar esta oferta",
            "btnDesaprobar : Button - Desaprobar",
            "lblEstado : chip Por aprobar / Aprobado / Desaprobado"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU12 ───
{
    id: "CU12",
    nombre: "Recibir insumos y cerrar orden de compra",
    actor: "Repositor",
    lifelines: [
        "Repositor",
        "UI_96VA",
        "CompraInsumosBLL_96VA",
        "ComprasMPP_96VA",
        "ComponentesMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Repositor",            "UI_96VA",              "seleccionar Recepcion de insumos()"],
        ["Repositor",            "UI_96VA",              "buscarOrden(numeroCompra)"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "ObtenerPendienteDeRecibir(idOrdenCompra)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "ObtenerDetalle(id)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "ObtenerFacturasPorOrden(id)"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              ":List<DetalleComponente96VA>"],
        ["Repositor",            "UI_96VA",              "marcarRecibidos(cantidades)"],
        ["Repositor",            "UI_96VA",              "ConfirmarRecepcion()"],
        ["UI_96VA",              "CompraInsumosBLL_96VA", "RegistrarFacturaCompra(id, fechaEntrega, observaciones, recibidos)"],
        ["CompraInsumosBLL_96VA", "CompraInsumosBLL_96VA", "ExigirPatente(RegistrarFacturaCompra)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "AgregarFacturaCompra(f)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "AgregarFacturaCompra(...) + AgregarFacturaCompraDetalle(...)"],
        ["CompraInsumosBLL_96VA", "ComponentesMPP_96VA",  "SumarStock(codigo, cantidadRecibida)"],
        ["ComponentesMPP_96VA",  "DAL_96VA",             "SumarStock(codigo, cantidad)"],
        ["CompraInsumosBLL_96VA", "ComprasMPP_96VA",      "CambiarEstadoOrdenCompra(id, Finalizada | RecibidaParcial)"],
        ["ComprasMPP_96VA",      "DAL_96VA",             "CerrarOrdenCompra(id, fechaCierre)"],
        ["CompraInsumosBLL_96VA", "UI_96VA",              ":FacturaCompra96VA"],
        ["UI_96VA",              "Repositor",            "mostrar estado y lo pendiente"]
    ],
    clases: {
        "UI":  ["ComprasControl"],
        "BLL": ["CompraInsumosBLL96VA", "ComponentesBLL96VA"],
        "MPP": ["ComprasMPP96VA", "ComponentesMPP96VA"],
        "DAL": ["ComprasDAL96VA", "ComponentesDAL96VA"],
        "BE":  ["FacturaCompra96VA", "DetalleComponente96VA", "OrdenCompra96VA", "Componente96VA"]
    },
    tablas: [
        ["FacturaCompra",        ["NumeroFactura : NVARCHAR(30) (PK)",
                                  "IdOrdenCompra : NVARCHAR(30) (FK)", "FechaEmision : DATETIME",
                                  "FechaEntrega : DATETIME", "Total : DECIMAL(18,2)",
                                  "Observaciones : NVARCHAR(1000)"]],
        ["FacturaCompraDetalle", ["NumeroFactura : NVARCHAR(30) (PK,FK)",
                                  "CodigoComponente : NVARCHAR(50) (PK,FK)",
                                  "Cantidad : INT (recibida)"]],
        ["OrdenesCompra",        ["Id : NVARCHAR(30) (PK)", "NumeroCompra : INT (UQ)",
                                  "Estado : INT", "FechaCierre : DATETIME"]],
        ["OrdenCompraDetalle",   ["IdOrdenCompra : NVARCHAR(30) (PK,FK)",
                                  "CodigoComponente : NVARCHAR(50) (PK,FK)",
                                  "Cantidad : INT (pedida)"]],
        ["Componentes",          ["Codigo : NVARCHAR(50) (PK)", "Descripcion : NVARCHAR(200)",
                                  "Stock : INT", "StockMinimo : INT", "StockReservado : INT"]]
    ],
    relaciones: [
        ["OrdenesCompra",  "FacturaCompra",        "1", "0..*"],
        ["FacturaCompra",  "FacturaCompraDetalle", "1", "1..*"],
        ["Componentes",    "FacturaCompraDetalle", "1", "0..*"],
        ["OrdenesCompra",  "OrdenCompraDetalle",   "1", "1..*"],
        ["Componentes",    "OrdenCompraDetalle",   "1", "0..*"]
    ],
    gui: {
        pantalla: "Recepcion de insumos",
        controles: [
            "txtBuscarOrden : TextBox - buscar por numero de compra",
            "FilaRecepcion : una por insumo pedido, con lo que llego",
            "SelectorCantidad : [-] n [+] unidades recibidas, tope lo pedido",
            "Chip de linea : completo / faltan N / no llego",
            "txtObservaciones : TextBox - diferencias pedido vs recibido",
            "lblResumen : recibido total y pendiente",
            "btnConfirmar : Button - Confirmar recepcion",
            "btnVolver : Button - Volver"
        ]
    }
}

];

// ========================================================================
//  MIEMBROS REALES DE CADA CLASE
//  Extraidos del codigo fuente del sistema (capas BE / BLL / MPP / DAL / UI).
//  'at' = atributos, 'op' = operaciones.
// ========================================================================
var MIEMBROS = {
    "ComprasControl": { at: [],
              op: ["CargarFaltantes() : void", "AbrirFormularioNueva() : void", "CrearOrden(orden : OrdenArmada96VA) : void", "CargarOrdenes() : void", "SeleccionarOrden(numero : int) : void", "CargarProveedoresEn(cbo : ComboBox) : void", "PedirCotizacion(prov : Proveedor96VA) : void", "AprobarDesaprobar(cot : PedidoCotizacion96VA, aprobar : bool) : void", "Recibir() : void", "ConfirmarRecepcion(r : RecepcionArmada96VA) : void"] },
    "FRMNuevoProveedor96VA": { at: [],
              op: ["Guardar() : void"] },
    "CotizacionesControl": { at: [],
              op: ["Cargar() : void", "CargarProveedores() : void", "Seleccionar(oc : OrdenCompra96VA) : void", "RefrescarMesa() : void", "RegistrarOferta() : void", "Resolver(cot : PedidoCotizacion96VA, adjudicar : bool) : void", "NuevoProveedor() : void"] },
    "ProveedoresControl": { at: [],
              op: ["CargarDatosEnGrilla(grilla : DataGridView) : void", "CargarSeleccionEnCampos() : bool", "Guardar(editando : bool) : bool", "EliminarSeleccion() : void"] },
    "CompraInsumosBLL96VA": { at: [],
              op: ["ObtenerFaltantes() : List<Componente96VA>", "ObtenerOrdenesCompra() : List<OrdenCompra96VA>", "ObtenerCotizaciones() : List<PedidoCotizacion96VA>", "ObtenerCotizacionesPorOrden(idOrdenCompra : string) : List<PedidoCotizacion96VA>", "RegistrarOrdenCompra(faltantes : List<DetalleComponente96VA>, fechaLimite : DateTime, repositor : Usuario96VA) : OrdenCompra96VA", "RegistrarCotizacion(idOrdenCompra : string, idProveedor : int, costo : decimal, condiciones : string) : PedidoCotizacion96VA", "AprobarCotizacion(numeroCotizacion : string, gerente : Usuario96VA) : void", "DesaprobarCotizacion(numeroCotizacion : string, gerente : Usuario96VA) : void", "ObtenerPendienteDeRecibir(idOrdenCompra : string) : List<DetalleComponente96VA>", "RegistrarFacturaCompra(idOrdenCompra : string, fechaEntrega : DateTime, observaciones : string, recibidos : List<DetalleComponente96VA>) : FacturaCompra96VA"] },
    "ProveedoresBLL96VA": { at: [],
              op: ["ObtenerTodos() : List<Proveedor96VA>", "ObtenerPorId(id : int) : Proveedor96VA", "Crear(p : Proveedor96VA) : void", "Modificar(p : Proveedor96VA) : void", "Eliminar(id : int) : void"] },
    "ComponentesBLL96VA": { at: [],
              op: ["ObtenerTodos() : List<Componente96VA>", "ObtenerBajoStock() : List<Componente96VA>", "SumarStock(codigo : string, cantidad : int) : void", "ObtenerPorCodigo(codigo : string) : Componente96VA", "Crear(c : Componente96VA) : void", "Modificar(c : Componente96VA) : void", "Eliminar(codigo : string) : void", "Reactivar(codigo : string) : void", "ObtenerBitacora(codigo : string, descripcion : string, fechaIni : DateTime?, fechaFin : DateTime?) : List<ComponenteHistorico96VA>", "ActivarHistorico(version : ComponenteHistorico96VA) : void"] },
    "ComprasMPP96VA": { at: [],
              op: ["AgregarOrdenCompra(oc : OrdenCompra96VA) : void", "ObtenerOrdenesCompra() : List<OrdenCompra96VA>", "ObtenerDetalle(idOrdenCompra : string) : List<DetalleComponente96VA>", "CerrarOrdenCompra(id : string, fechaCierre : DateTime) : void", "CambiarEstadoOrdenCompra(id : string, estado : EstadoOrdenCompra96VA) : void", "AgregarCotizacion(cot : PedidoCotizacion96VA) : void", "ObtenerCotizaciones() : List<PedidoCotizacion96VA>", "ObtenerCotizacionesPorOrden(idOrdenCompra : string) : List<PedidoCotizacion96VA>", "CambiarEstadoCotizacion(numero : string, estado : EstadoCotizacion96VA, dniGerenteAprobador : string) : void", "AgregarFacturaCompra(f : FacturaCompra96VA) : void", "ObtenerFacturasPorOrden(idOrdenCompra : string) : List<FacturaCompra96VA>"] },
    "ProveedoresMPP96VA": { at: [],
              op: ["ObtenerTodos() : List<Proveedor96VA>", "ObtenerPorId(id : int) : Proveedor96VA", "ObtenerPorCuit(cuit : string) : Proveedor96VA", "Agregar(p : Proveedor96VA) : void", "Modificar(p : Proveedor96VA) : void", "Eliminar(id : int) : void"] },
    "ComponentesMPP96VA": { at: [],
              op: ["ObtenerTodos() : List<Componente96VA>", "ObtenerPorCodigo(codigo : string) : Componente96VA", "ObtenerBajoStock() : List<Componente96VA>", "Agregar(c : Componente96VA) : void", "Modificar(c : Componente96VA) : void", "SumarStock(codigo : string, cantidad : int) : void", "BajaLogica(codigo : string) : void", "Reactivar(codigo : string) : void", "Eliminar(codigo : string) : void", "ObtenerBitacora(codigo : string, descripcion : string, fechaIni : DateTime?, fechaFin : DateTime?) : List<ComponenteHistorico96VA>", "ActivarHistorico(idHistorico : int) : void", "DescontarStock(codigo : string, cantidad : int) : void", "ReservarStock(codigo : string, cantidad : int) : void", "LiberarReserva(codigo : string, cantidad : int) : void", "ConsumirReserva(codigo : string, cantidad : int) : void"] },
    "ComprasDAL96VA": { at: [],
              op: ["AgregarOrdenCompra(id : string, fechaLimite : DateTime, dniRepositor : string) : int", "AgregarDetalle(idOrdenCompra : string, codigoComponente : string, cantidad : int) : void", "ObtenerOrdenesCompra() : DataTable", "ObtenerDetalle(idOrdenCompra : string) : DataTable", "CambiarEstadoOrdenCompra(id : string, estado : int) : void", "CerrarOrdenCompra(id : string, fechaCierre : DateTime) : void", "AgregarCotizacion(numero : string, idOrdenCompra : string, idProveedor : int, costo : decimal, condiciones : string) : void", "ObtenerCotizaciones() : DataTable", "ObtenerCotizacionesPorOrden(idOrdenCompra : string) : DataTable", "CambiarEstadoCotizacion(numero : string, estado : int, dniGerenteAprobador : string) : void", "AgregarFacturaCompra(numeroFactura : string, idOrdenCompra : string, fechaEmision : DateTime, fechaEntrega : DateTime, total : decimal, observaciones : string) : void", "ObtenerFacturasPorOrden(idOrdenCompra : string) : DataTable", "ObtenerFacturaCompraDetalle(numeroFactura : string) : DataTable", "AgregarFacturaCompraDetalle(numeroFactura : string, codigoComponente : string, cantidad : int) : void"] },
    "ProveedoresDAL96VA": { at: [],
              op: ["ObtenerTodos() : DataTable", "ObtenerPorId(id : int) : DataTable", "ObtenerPorCuit(cuit : string) : DataTable", "Agregar(nombre : string, cuit : string, email : string, telefono : string, direccion : string) : int", "Modificar(id : int, nombre : string, cuit : string, email : string, telefono : string, direccion : string) : void", "Eliminar(id : int) : void"] },
    "ComponentesDAL96VA": { at: [],
              op: ["ObtenerTodos() : DataTable", "ObtenerPorCodigo(codigo : string) : DataTable", "ObtenerBajoStock() : DataTable", "Agregar(codigo : string, descripcion : string, tipo : int, marca : string, modelo : string, precioUnitario : decimal, stock : int, stockMinimo : int) : void", "Modificar(codigo : string, descripcion : string, tipo : int, marca : string, modelo : string, precioUnitario : decimal, stock : int, stockMinimo : int) : void", "SumarStock(codigo : string, cantidad : int) : void", "BajaLogica(codigo : string) : void", "Reactivar(codigo : string) : void", "Eliminar(codigo : string) : void", "ObtenerBitacora(codigo : string, descripcion : string, fechaIni : DateTime?, fechaFin : DateTime?) : DataTable", "ActivarHistorico(idHistorico : int) : void", "DescontarStock(codigo : string, cantidad : int) : void", "ReservarStock(codigo : string, cantidad : int) : void", "LiberarReserva(codigo : string, cantidad : int) : void", "ConsumirReserva(codigo : string, cantidad : int) : void"] },
    "OrdenCompra96VA": { at: ["Id : string", "NumeroCompra : int", "ComponentesFaltantes : List<DetalleComponente96VA>", "FechaLimite : DateTime", "RepositorSolicitante : Usuario96VA", "Estado : EstadoOrdenCompra96VA", "FechaCierre : DateTime?"],
              op: [] },
    "DetalleComponente96VA": { at: ["Componente : Componente96VA", "Cantidad : int"],
              op: [] },
    "Proveedor96VA": { at: ["Id : int", "Nombre : string", "Cuit : string", "Email : string", "Telefono : string", "Direccion : string"],
              op: [] },
    "PedidoCotizacion96VA": { at: ["Numero : string", "NumeroCompra : string", "ComponentesPedidos : List<DetalleComponente96VA>", "FechaEmision : DateTime", "Estado : EstadoCotizacion96VA", "Proveedor : Proveedor96VA", "Costo : decimal", "Condiciones : string", "GerenteAprobador : Usuario96VA"],
              op: [] },
    "FacturaCompra96VA": { at: ["NumeroFactura : string", "NumeroCompra : string", "FechaEmision : DateTime", "FechaEntrega : DateTime", "ComponentesRecibidos : List<DetalleComponente96VA>", "Total : decimal", "Observaciones : string"],
              op: [] },
    "Componente96VA": { at: ["Codigo : string", "Descripcion : string", "Tipo : TipoComponente96VA", "Marca : string", "Modelo : string", "PrecioUnitario : decimal", "Stock : int", "StockMinimo : int", "StockReservado : int", "BajaLogica : bool"],
              op: [] }
};

// ========================================================================
//  DETALLE DEL DIAGRAMA DE CLASES, CASO POR CASO
//
//  Para cada CU se define:
//    metodos : que operaciones de cada clase participan de ESE caso de uso.
//              (una clase como CompraInsumosBLL96VA tiene muchas operaciones,
//               pero en el DC de un CU solo deben verse las que ese CU usa).
//    enlaces : las relaciones entre las clases del diagrama, una por una,
//              para que ninguna clase quede suelta.
//              [origen, destino, tipo, etiqueta]
// ========================================================================
var DC = {

"CU08": {
    metodos: {
        "ComprasControl":        ["AbrirFormularioNueva", "CargarFaltantes", "CrearOrden", "CargarOrdenes"],
        "CompraInsumosBLL96VA":  ["ObtenerFaltantes", "RegistrarOrdenCompra", "ObtenerOrdenesCompra"],
        "ComponentesBLL96VA":    ["ObtenerBajoStock", "ObtenerPorCodigo"],
        "ComprasMPP96VA":        ["AgregarOrdenCompra", "ObtenerOrdenesCompra", "ObtenerDetalle"],
        "ComponentesMPP96VA":    ["ObtenerBajoStock", "ObtenerPorCodigo"],
        "ComprasDAL96VA":        ["AgregarOrdenCompra", "AgregarDetalle", "ObtenerOrdenesCompra"],
        "ComponentesDAL96VA":    ["ObtenerBajoStock"]
    },
    enlaces: [
        ["ComprasControl",       "CompraInsumosBLL96VA",  "Dependency", "usa"],
        ["CompraInsumosBLL96VA", "ComponentesBLL96VA",    "Dependency", "consulta faltantes"],
        ["CompraInsumosBLL96VA", "ComprasMPP96VA",        "Dependency", "usa"],
        ["ComponentesBLL96VA",   "ComponentesMPP96VA",    "Dependency", "usa"],
        ["ComprasMPP96VA",       "ComprasDAL96VA",        "Dependency", "usa"],
        ["ComponentesMPP96VA",   "ComponentesDAL96VA",    "Dependency", "usa"],
        ["ComprasMPP96VA",       "OrdenCompra96VA",       "Dependency", "mapea"],
        ["ComprasMPP96VA",       "DetalleComponente96VA", "Dependency", "mapea"],
        ["ComponentesMPP96VA",   "Componente96VA",        "Dependency", "mapea"],
        ["OrdenCompra96VA",      "DetalleComponente96VA", "Association", ""],
        ["DetalleComponente96VA","Componente96VA",        "Association", ""]
    ]
},

"CU09": {
    metodos: {
        "ComprasControl":        ["CargarProveedoresEn", "SeleccionarOrden"],
        "FRMNuevoProveedor96VA": ["Guardar"],
        "ProveedoresControl":    ["CargarDatosEnGrilla", "Guardar", "EliminarSeleccion"],
        "ProveedoresBLL96VA":    ["ObtenerTodos", "ObtenerPorId", "Crear", "Modificar"],
        "CompraInsumosBLL96VA":  ["ObtenerOrdenesCompra", "RegistrarCotizacion"],
        "ProveedoresMPP96VA":    ["ObtenerTodos", "ObtenerPorId", "ObtenerPorCuit", "Agregar", "Modificar"],
        "ProveedoresDAL96VA":    ["ObtenerTodos", "ObtenerPorCuit", "Agregar", "Modificar"]
    },
    enlaces: [
        ["ComprasControl",        "ProveedoresBLL96VA",   "Dependency", "usa"],
        ["ComprasControl",        "FRMNuevoProveedor96VA","Dependency", "abre"],
        ["ProveedoresControl",    "ProveedoresBLL96VA",   "Dependency", "usa"],
        ["FRMNuevoProveedor96VA", "ProveedoresBLL96VA",   "Dependency", "usa"],
        ["ComprasControl",        "CompraInsumosBLL96VA", "Dependency", "usa"],
        ["ProveedoresBLL96VA",    "ProveedoresMPP96VA",   "Dependency", "usa"],
        ["ProveedoresMPP96VA",    "ProveedoresDAL96VA",   "Dependency", "usa"],
        ["ProveedoresMPP96VA",    "Proveedor96VA",        "Dependency", "mapea"],
        ["CompraInsumosBLL96VA",  "OrdenCompra96VA",      "Dependency", "maneja"],
        ["OrdenCompra96VA",       "Proveedor96VA",        "Association", ""]
    ]
},

"CU10": {
    metodos: {
        "CotizacionesControl":   ["Cargar", "CargarProveedores", "Seleccionar", "RefrescarMesa", "RegistrarOferta", "NuevoProveedor"],
        "ComprasControl":        ["CargarOrdenes", "SeleccionarOrden", "PedirCotizacion"],
        "CompraInsumosBLL96VA":  ["ObtenerOrdenesCompra", "ObtenerCotizacionesPorOrden", "RegistrarCotizacion"],
        "ProveedoresBLL96VA":    ["ObtenerTodos", "ObtenerPorId"],
        "ComprasMPP96VA":        ["AgregarCotizacion", "ObtenerCotizacionesPorOrden", "ObtenerDetalle"],
        "ProveedoresMPP96VA":    ["ObtenerTodos", "ObtenerPorId"],
        "ComprasDAL96VA":        ["AgregarCotizacion", "ObtenerCotizacionesPorOrden"],
        "ProveedoresDAL96VA":    ["ObtenerTodos", "ObtenerPorId"]
    },
    enlaces: [
        ["CotizacionesControl",   "CompraInsumosBLL96VA",   "Dependency", "usa"],
        ["CotizacionesControl",   "ProveedoresBLL96VA",     "Dependency", "usa"],
        ["ComprasControl",        "CompraInsumosBLL96VA",   "Dependency", "usa"],
        ["CompraInsumosBLL96VA",  "ComprasMPP96VA",         "Dependency", "usa"],
        ["ProveedoresBLL96VA",    "ProveedoresMPP96VA",     "Dependency", "usa"],
        ["ComprasMPP96VA",        "ComprasDAL96VA",         "Dependency", "usa"],
        ["ProveedoresMPP96VA",    "ProveedoresDAL96VA",     "Dependency", "usa"],
        ["ComprasMPP96VA",        "PedidoCotizacion96VA",   "Dependency", "mapea"],
        ["ComprasMPP96VA",        "OrdenCompra96VA",        "Dependency", "mapea"],
        ["ProveedoresMPP96VA",    "Proveedor96VA",          "Dependency", "mapea"],
        ["PedidoCotizacion96VA",  "Proveedor96VA",          "Association", ""],
        ["PedidoCotizacion96VA",  "OrdenCompra96VA",        "Association", ""],
        ["PedidoCotizacion96VA",  "DetalleComponente96VA",  "Association", ""]
    ]
},

"CU11": {
    metodos: {
        "CotizacionesControl":   ["Cargar", "Seleccionar", "RefrescarMesa", "Resolver"],
        "CompraInsumosBLL96VA":  ["ObtenerCotizacionesPorOrden", "AprobarCotizacion", "DesaprobarCotizacion"],
        "ComprasMPP96VA":        ["ObtenerCotizacionesPorOrden", "CambiarEstadoCotizacion", "CambiarEstadoOrdenCompra"],
        "ComprasDAL96VA":        ["ObtenerCotizacionesPorOrden", "CambiarEstadoCotizacion", "CambiarEstadoOrdenCompra"]
    },
    enlaces: [
        ["CotizacionesControl",  "CompraInsumosBLL96VA", "Dependency", "usa"],
        ["CompraInsumosBLL96VA", "ComprasMPP96VA",       "Dependency", "usa"],
        ["ComprasMPP96VA",       "ComprasDAL96VA",       "Dependency", "usa"],
        ["ComprasMPP96VA",       "PedidoCotizacion96VA", "Dependency", "mapea"],
        ["ComprasMPP96VA",       "OrdenCompra96VA",      "Dependency", "mapea"],
        ["PedidoCotizacion96VA", "Proveedor96VA",        "Association", ""],
        ["PedidoCotizacion96VA", "OrdenCompra96VA",      "Association", ""]
    ]
},

"CU12": {
    metodos: {
        "ComprasControl":        ["SeleccionarOrden", "Recibir", "ConfirmarRecepcion", "CargarOrdenes"],
        "CompraInsumosBLL96VA":  ["ObtenerPendienteDeRecibir", "RegistrarFacturaCompra", "ObtenerOrdenesCompra"],
        "ComponentesBLL96VA":    ["SumarStock", "ObtenerPorCodigo"],
        "ComprasMPP96VA":        ["ObtenerDetalle", "AgregarFacturaCompra", "ObtenerFacturasPorOrden", "CambiarEstadoOrdenCompra", "CerrarOrdenCompra"],
        "ComponentesMPP96VA":    ["SumarStock", "ObtenerPorCodigo"],
        "ComprasDAL96VA":        ["AgregarFacturaCompra", "AgregarFacturaCompraDetalle", "ObtenerFacturasPorOrden", "CerrarOrdenCompra", "CambiarEstadoOrdenCompra"],
        "ComponentesDAL96VA":    ["SumarStock", "ObtenerPorCodigo"]
    },
    enlaces: [
        ["ComprasControl",        "CompraInsumosBLL96VA",   "Dependency", "usa"],
        ["CompraInsumosBLL96VA",  "ComponentesBLL96VA",     "Dependency", "suma stock"],
        ["CompraInsumosBLL96VA",  "ComprasMPP96VA",         "Dependency", "usa"],
        ["CompraInsumosBLL96VA",  "ComponentesMPP96VA",     "Dependency", "usa"],
        ["ComponentesBLL96VA",    "ComponentesMPP96VA",     "Dependency", "usa"],
        ["ComprasMPP96VA",        "ComprasDAL96VA",         "Dependency", "usa"],
        ["ComponentesMPP96VA",    "ComponentesDAL96VA",     "Dependency", "usa"],
        ["ComprasMPP96VA",        "FacturaCompra96VA",      "Dependency", "mapea"],
        ["ComprasMPP96VA",        "OrdenCompra96VA",        "Dependency", "mapea"],
        ["ComponentesMPP96VA",    "Componente96VA",         "Dependency", "mapea"],
        ["FacturaCompra96VA",     "DetalleComponente96VA",  "Association", ""],
        ["FacturaCompra96VA",     "OrdenCompra96VA",        "Association", ""],
        ["DetalleComponente96VA", "Componente96VA",         "Association", ""]
    ]
}

};

// ══════════════════════════════════════════════════════════════════════════
//  HELPERS DE LA API DE EA
// ══════════════════════════════════════════════════════════════════════════

function log(msg) {
    Session.Output(msg);
}

/** Crea un subpaquete y devuelve el objeto Package. */
function nuevoPaquete(padre, nombre) {
    var p = padre.Packages.AddNew(nombre, "Package");
    p.Update();
    padre.Packages.Refresh();
    return p;
}

/** Crea un diagrama dentro de un paquete. */
function nuevoDiagrama(pkg, nombre, tipo) {
    var d = pkg.Diagrams.AddNew(nombre, tipo);
    d.Update();
    pkg.Diagrams.Refresh();
    return d;
}

/** Crea un elemento dentro de un paquete. */
function nuevoElemento(pkg, nombre, tipo, estereotipo, notas) {
    var e = pkg.Elements.AddNew(nombre, tipo);
    if (estereotipo != null && estereotipo != "") e.Stereotype = estereotipo;
    if (notas != null && notas != "") e.Notes = notas;
    e.Update();
    pkg.Elements.Refresh();
    return e;
}

/**
 * Pone un elemento en un diagrama.
 * En EA el eje vertical es NEGATIVO hacia abajo: top = -y, bottom = -(y+alto).
 */
function ponerEnDiagrama(dia, elem, x, y, ancho, alto) {
    var geo = "l=" + x + ";r=" + (x + ancho) + ";t=" + (-y) + ";b=" + (-(y + alto)) + ";";
    var o = dia.DiagramObjects.AddNew(geo, "");
    o.ElementID = elem.ElementID;
    o.Update();
    dia.DiagramObjects.Refresh();
    return o;
}

/** Conecta dos elementos. Devuelve el conector. */
function conectar(origen, destino, tipo, nombre) {
    var c = origen.Connectors.AddNew(nombre == null ? "" : nombre, tipo);
    c.SupplierID = destino.ElementID;
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/** Conector de secuencia, con su número de orden. */
function mensaje(origen, destino, texto, nro) {
    var c = origen.Connectors.AddNew(texto, "Sequence");
    c.SupplierID = destino.ElementID;
    c.SequenceNo = nro;
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/** Relación del DER, con cardinalidades en los extremos. */
function relacionDer(origen, destino, cardOrigen, cardDestino) {
    var c = origen.Connectors.AddNew("", "Association");
    c.SupplierID = destino.ElementID;
    c.ClientEnd.Cardinality = cardOrigen;
    c.SupplierEnd.Cardinality = cardDestino;
    c.ClientEnd.Navigable = "Non-Navigable";
    c.SupplierEnd.Navigable = "Navigable";
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/** Saca espacios de los dos extremos (JScript no trae String.trim). */
function trim(s) {
    return String(s).replace(/^\s+/, "").replace(/\s+$/, "");
}

/** ¿La lista contiene ese valor? */
function contiene(lista, valor) {
    if (lista == null) return false;
    for (var i = 0; i < lista.length; i++) if (lista[i] == valor) return true;
    return false;
}

/** Agrega un atributo publico a una clase. */
function atributo(elem, nombre, tipo) {
    var a = elem.Attributes.AddNew(nombre, tipo == null ? "" : tipo);
    a.Visibility = "Public";
    a.Update();
    elem.Attributes.Refresh();
    return a;
}

/**
 * Agrega una operacion a una clase a partir de su firma real.
 * Formato esperado:  "RegistrarVenta(cliente : Cliente96VA) : Venta96VA"
 */
function operacion(elem, firma) {
    var iAbre   = firma.indexOf("(");
    var iCierra = firma.lastIndexOf(")");
    if (iAbre < 0 || iCierra < iAbre) return null;

    var nombre  = trim(firma.substring(0, iAbre));
    var params  = firma.substring(iAbre + 1, iCierra);
    var cola    = firma.substring(iCierra + 1);
    var retorno = "void";
    var iDosP   = cola.indexOf(":");
    if (iDosP >= 0) retorno = trim(cola.substring(iDosP + 1));

    var m = elem.Methods.AddNew(nombre, retorno);
    m.Visibility = "Public";
    m.Update();
    elem.Methods.Refresh();

    if (trim(params) != "") {
        var lista = params.split(",");
        for (var i = 0; i < lista.length; i++) {
            var partes = trim(lista[i]).split(" : ");
            var par = m.Parameters.AddNew(trim(partes[0]),
                                          partes.length > 1 ? trim(partes[1]) : "");
            par.Position = i;
            par.Update();
        }
        m.Parameters.Refresh();
        m.Update();
    }
    return m;
}

/**
 * Carga en la clase sus atributos y operaciones reales.
 * "filtro" es la lista de operaciones que participan del caso de uso; si viene
 * en null se muestran todas. Devuelve cuantas filas quedaron en la caja.
 */
function poblarClase(elem, nombre, filtro) {
    var m = MIEMBROS[nombre];
    if (m == null) return 0;

    var cuenta = 0;
    for (var i = 0; i < m.at.length && cuenta < MAX_MIEMBROS; i++) {
        var partes = m.at[i].split(" : ");
        atributo(elem, trim(partes[0]), partes.length > 1 ? trim(partes[1]) : "");
        cuenta++;
    }
    for (var j = 0; j < m.op.length && cuenta < MAX_MIEMBROS; j++) {
        var firma = m.op[j];
        var nom = trim(firma.substring(0, firma.indexOf("(")));
        if (filtro != null && !contiene(filtro, nom)) continue;
        operacion(elem, firma);
        cuenta++;
    }
    return cuenta;
}

/** Une una lista con saltos de línea, para las notas. */
function lineas(lista) {
    var s = "";
    for (var i = 0; i < lista.length; i++) s += "- " + lista[i] + "\n";
    return s;
}

// ══════════════════════════════════════════════════════════════════════════
//  GENERADORES DE DIAGRAMAS
// ══════════════════════════════════════════════════════════════════════════

/**
 * ECS — Esquema Entrada / Comportamiento / Salida.
 * Tres bloques en fila, unidos por flujos: es la lectura sistémica del
 * proceso de negocio antes de entrar al diseño.
 */
function generarECS(pkgPre) {
    var dia = nuevoDiagrama(pkgPre, "N01.RF2 Descripcion funcional del proceso (Entradas / Comportamiento / Salida)", "Activity");

    var eEnt = nuevoElemento(pkgPre, "ENTRADAS", "Object", "input",
                             lineas(ECS.entradas));
    var eCom = nuevoElemento(pkgPre, "COMPORTAMIENTO DEL SISTEMA", "Object", "process",
                             lineas(ECS.comportamiento));
    var eSal = nuevoElemento(pkgPre, "SALIDAS", "Object", "output",
                             lineas(ECS.salidas));

    ponerEnDiagrama(dia, eEnt,  40, 60, 260, 300);
    ponerEnDiagrama(dia, eCom, 360, 60, 300, 300);
    ponerEnDiagrama(dia, eSal, 720, 60, 260, 300);

    conectar(eEnt, eCom, "ControlFlow", "alimenta");
    conectar(eCom, eSal, "ControlFlow", "produce");

    // Realimentación: el estado de la orden vuelve a entrar al sistema.
    conectar(eSal, eCom, "ControlFlow", "realimentacion (stock actualizado / pendiente de recibir)");

    log("  ECS generado.");
    return dia;
}

/** DS — Diagrama de Secuencia del caso de uso. */
function generarDS(pkgCu, cu) {
    var dia = nuevoDiagrama(pkgCu, cu.id + " - DS " + cu.nombre, "Sequence");

    // Las líneas de vida se reparten en fila, en orden de capa:
    // el actor, la UI (dibujada como Boundary, el circulo), las clases de
    // negocio y persistencia, y a la derecha el DAL general.
    var mapa = {};
    var x = 40;
    for (var i = 0; i < cu.lifelines.length; i++) {
        var nombre = cu.lifelines[i];
        if (mapa[nombre] != null) continue;      // no se repite una linea de vida

        var tipo = "Sequence";
        if (i == 0) tipo = "Actor";
        else if (nombre.substring(0, 3) == "UI_") tipo = "Boundary";

        var e = nuevoElemento(pkgCu, nombre, tipo, "", "");
        ponerEnDiagrama(dia, e, x, 40, LIFELINE_ANCHO, 60);
        mapa[nombre] = e;
        x += LIFELINE_ANCHO + LIFELINE_SEP;
    }

    for (var m = 0; m < cu.mensajes.length; m++) {
        var org = mapa[cu.mensajes[m][0]];
        var des = mapa[cu.mensajes[m][1]];
        if (org == null || des == null) {
            log("    ! mensaje con lifeline desconocida: " + cu.mensajes[m][2]);
            continue;
        }
        mensaje(org, des, cu.mensajes[m][2], m + 1);
    }

    return dia;
}

/**
 * DC — Diagrama de Clases del caso de uso, EN CAPAS.
 * Una fila por capa (UI, BLL, MPP, DAL, BE) y una dependencia entre capas
 * contiguas, que es lo que pide la consigna: sólo las clases asociadas al CU.
 */
function generarDC(pkgCu, cu) {
    var dia = nuevoDiagrama(pkgCu, cu.id + " - DC " + cu.nombre, "Logical");
    var extra = (DC[cu.id] != null) ? DC[cu.id] : { metodos: {}, enlaces: [] };

    var capas = ["UI", "BLL", "MPP", "DAL", "BE"];
    var mapa = {};
    var y = 40;
    var totalMiembros = 0;

    for (var c = 0; c < capas.length; c++) {
        var capa = capas[c];
        var nombres = cu.clases[capa];
        if (nombres == null) continue;

        var x = 40;
        var altoFila = CLASE_ALTO;

        for (var i = 0; i < nombres.length; i++) {
            var nom = nombres[i];
            var e = nuevoElemento(pkgCu, nom, "Class", capa, "Capa " + capa);

            // Atributos y operaciones REALES, tomados del codigo fuente.
            var filas = poblarClase(e, nom, extra.metodos[nom]);
            e.Update();
            totalMiembros += filas;
            if (filas == 0) log("    ! sin miembros cargados: " + nom);

            var alto = CLASE_ALTO;
            if (filas > 0) alto = 46 + filas * 15;
            ponerEnDiagrama(dia, e, x, y, CLASE_ANCHO, alto);
            if (alto > altoFila) altoFila = alto;

            mapa[nom] = e;
            x += CLASE_ANCHO + CLASE_SEP_X;
        }
        y += altoFila + CLASE_SEP_Y;
    }

    // Relaciones declaradas para este caso de uso: ninguna clase queda suelta.
    var enlaces = 0;
    for (var r = 0; r < extra.enlaces.length; r++) {
        var en = extra.enlaces[r];
        var o = mapa[en[0]], d = mapa[en[1]];
        if (o == null || d == null) {
            log("    ! enlace ignorado (clase ausente): " + en[0] + " -> " + en[1]);
            continue;
        }
        conectar(o, d, en[2], en[3]);
        enlaces++;
    }

    // Red de seguridad: si el CU no declaro enlaces, al menos se encadenan
    // las capas para que el diagrama no quede sin relaciones.
    if (enlaces == 0) {
        for (var k = 0; k < capas.length - 1; k++) {
            var a = cu.clases[capas[k]], b = cu.clases[capas[k + 1]];
            if (a == null || b == null) continue;
            conectar(mapa[a[0]], mapa[b[0]], "Dependency", "usa");
            enlaces++;
        }
    }

    log("  DC  listo (" + totalMiembros + " miembros, " + enlaces + " relaciones)");
    return dia;
}

/** DER — tablas del caso de uso con sus columnas y relaciones. */
function generarDER(pkgCu, cu) {
    var dia = nuevoDiagrama(pkgCu, cu.id + " - DER " + cu.nombre, "Logical");

    var mapa = {};
    var x = 40, y = 40, porFila = 0;

    for (var i = 0; i < cu.tablas.length; i++) {
        var nombre = cu.tablas[i][0];
        var columnas = cu.tablas[i][1];

        var e = nuevoElemento(pkgCu, nombre, "Class", "table", "Tabla del modelo relacional");
        for (var j = 0; j < columnas.length; j++) {
            // "Nombre : TIPO (PK)"  →  se separa nombre y tipo.
            var partes = columnas[j].split(" : ");
            atributo(e, partes[0], partes.length > 1 ? partes[1] : "");
        }
        e.Update();

        var alto = 60 + columnas.length * 14;
        ponerEnDiagrama(dia, e, x, y, 250, alto);
        mapa[nombre] = e;

        porFila++;
        if (porFila >= 3) { porFila = 0; x = 40; y += 260; }
        else x += 300;
    }

    for (var r = 0; r < cu.relaciones.length; r++) {
        var rel = cu.relaciones[r];
        var o = mapa[rel[0]], d = mapa[rel[1]];
        if (o == null || d == null) continue;
        relacionDer(o, d, rel[2], rel[3]);
    }

    return dia;
}

/** GUI — pantalla del caso de uso con su lista de controles. */
function generarGUI(pkgCu, cu) {
    var dia = nuevoDiagrama(pkgCu, cu.id + " - GUI " + cu.nombre, "Custom");

    var pantalla = nuevoElemento(pkgCu, cu.gui.pantalla, "Boundary", "screen",
                                 "Controles de la pantalla:\n\n" + lineas(cu.gui.controles));
    ponerEnDiagrama(dia, pantalla, 40, 40, 460, 340);

    var actor = nuevoElemento(pkgCu, cu.actor, "Actor", "", "");
    ponerEnDiagrama(dia, actor, 560, 140, 90, 110);
    conectar(actor, pantalla, "Association", "opera");

    return dia;
}


// ══════════════════════════════════════════════════════════════════════════
//  PROGRAMA PRINCIPAL
// ══════════════════════════════════════════════════════════════════════════

function main() {
    try { Repository.EnsureOutputVisible("Script"); } catch (eOut) {}
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - Generando modelo RFN2");
    Session.Output("=======================================================");

    var modelo = Repository.Models.GetAt(0);
    if (modelo == null) {
        Session.Prompt("No hay ningun modelo abierto en Enterprise Architect.", promptOK);
        return;
    }

    // Nombre único, para poder correr el script varias veces sin pisar nada.
    var nombre = NOMBRE_RAIZ;
    var intento = 1;
    var existe = true;
    while (existe) {
        existe = false;
        for (var i = 0; i < modelo.Packages.Count; i++)
            if (modelo.Packages.GetAt(i).Name == nombre) existe = true;
        if (existe) { intento++; nombre = NOMBRE_RAIZ + " (" + intento + ")"; }
    }

    var raiz = nuevoPaquete(modelo, nombre);
    log("Paquete raiz: " + nombre);

    // ── Prediseño ────────────────────────────────────────────────────────
    var pkgPre = nuevoPaquete(raiz, "N01.RF2 Prediseno del negocio");
    generarECS(pkgPre);          // Descripcion funcional del proceso

    // ── Diseño: un subpaquete por caso de uso ────────────────────────────
    var pkgDis = nuevoPaquete(raiz, "N02.RF2 Diseno del negocio");

    for (var c = 0; c < CASOS.length; c++) {
        var cu = CASOS[c];
        log("");
        log(cu.id + " - " + cu.nombre);

        var pkgCu = nuevoPaquete(pkgDis, cu.id + " " + cu.nombre);

        generarDS(pkgCu, cu);   log("  DS  listo (" + cu.mensajes.length + " mensajes)");
        generarDC(pkgCu, cu);
        generarDER(pkgCu, cu);  log("  DER listo (" + cu.tablas.length + " tablas)");
        generarGUI(pkgCu, cu);  log("  GUI listo");
    }

    try { Repository.RefreshModelView(0); } catch (eRef) {}

    log("");
    log("=======================================================");
    log(" LISTO. Se generaron " + (CASOS.length * 4 + 1) + " diagramas.");
    log(" Buscalos en el Project Browser, dentro de:");
    log("   " + nombre);
    log("=======================================================");
    log("");
    log(" Recordatorio: para ver los DER con notacion de Martin,");
    log(" abrir el diagrama > clic derecho > Properties >");
    log(" Connectors > Connector Notation = Information Engineering.");
}

// ===========================================================================
//  ARRANQUE
// ===========================================================================
function arrancar() {
    var args = WScript.Arguments;
    if (args.length < 1) {
        WScript.Echo("");
        WScript.Echo("  Falta indicar el archivo del proyecto de EA.");
        WScript.Echo("");
        WScript.Echo('  Uso:  cscript //nologo PCFORGE_RFN2_EA_Standalone.js "C:\\ruta\\proyecto.eapx"');
        WScript.Echo("");
        WScript.Quit(1);
    }

    var archivo = args(0);

    var fso = new ActiveXObject("Scripting.FileSystemObject");
    if (!fso.FileExists(archivo)) {
        WScript.Echo("  No existe el archivo: " + archivo);
        WScript.Quit(1);
    }
    archivo = fso.GetAbsolutePathName(archivo);

    WScript.Echo("Abriendo Enterprise Architect...");
    try {
        Repository = new ActiveXObject("EA.Repository");
    } catch (e) {
        WScript.Echo("");
        WScript.Echo("  No se pudo crear el objeto EA.Repository.");
        WScript.Echo("  Si EA esta instalado en 32 bits, probar con:");
        WScript.Echo("     C:\\Windows\\SysWOW64\\cscript.exe //nologo ...");
        WScript.Echo("");
        WScript.Echo("  Detalle: " + e.message);
        WScript.Quit(1);
    }

    WScript.Echo("Abriendo el proyecto: " + archivo);
    if (!Repository.OpenFile(archivo)) {
        WScript.Echo("  EA no pudo abrir el proyecto. Verificar que este cerrado en EA.");
        try { Repository.Exit(); } catch (e2) {}
        WScript.Quit(1);
    }

    try {
        main();
    } finally {
        WScript.Echo("Guardando y cerrando el proyecto...");
        try { Repository.CloseFile(); } catch (e3) {}
        try { Repository.Exit(); } catch (e4) {}
        Repository = null;
    }
}

arrancar();
