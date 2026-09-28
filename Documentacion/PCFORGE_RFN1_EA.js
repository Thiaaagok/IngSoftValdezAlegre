!INC Local Scripts.EAConstants-JScript

/*
 * ===========================================================================
 *  PCFORGE — GENERADOR DE MODELO RFN1 (Venta de computadoras)
 *  Trabajo de Diploma — UAI — Valdez / Alegre
 * ===========================================================================
 *
 *  QUÉ HACE
 *  --------
 *  Crea dentro del modelo abierto un paquete "PCFORGE - RFN1" con:
 *
 *    N01.RF1 Prediseño
 *       └─ ECS — Esquema Entrada / Comportamiento / Salida
 *
 *    N02.RF1 Diseño
 *       ├─ CU01 Registrar venta ......... DS + DC + DER + GUI
 *       ├─ CU02 Registrar cliente ....... DS + DC + DER + GUI
 *       ├─ CU03 Registrar seña .......... DS + DC + DER + GUI
 *       ├─ CU04 Gestionar orden prod. ... DS + DC + DER + GUI
 *       ├─ CU05 Asignar línea ........... DS + DC + DER + GUI
 *       ├─ CU06 Cerrar orden ............ DS + DC + DER + GUI
 *       └─ CU07 Entregar computadora .... DS + DC + DER + GUI
 *
 *  29 diagramas en total, con los elementos ya posicionados.
 *
 *  CÓMO SE USA
 *  -----------
 *   1. Abrir el proyecto (.eap / .eapx / .qea) en Enterprise Architect.
 *   2. Menú  Specialize > Tools > Scripting   (en EA 15 y anteriores:
 *      Tools > Scripting). Se abre la ventana "Scripting".
 *   3. Clic derecho sobre el grupo "Local Scripts" > New JScript Script.
 *      Ponerle un nombre, por ejemplo "PCFORGE RFN1".
 *   4. Pegar TODO este archivo dentro del script y guardar (Ctrl+S).
 *   5. Clic derecho sobre el script > Run Script.
 *   6. Ver el avance en la ventana  View > System Output > Script  .
 *
 *  Si se ejecuta dos veces crea un paquete nuevo con sufijo numérico, así
 *  que no pisa lo anterior: se puede correr, revisar y borrar el paquete.
 *
 *  NOTAS DE NOTACIÓN
 *  -----------------
 *  · Los DER se generan como clases con estereotipo «table». Para verlos con
 *    la notación de Martin (patas de gallo): clic derecho en el diagrama >
 *    Properties > pestaña Connectors > Connector Notation = "Information
 *    Engineering".
 *  · Los GUI se generan como elementos Boundary con la lista de campos y
 *    botones en las notas. Sobre ese diagrama conviene pegar la captura real
 *    de la pantalla (Insert > Image, o Ctrl+V sobre el diagrama).
 * ===========================================================================
 */

// ══════════════════════════════════════════════════════════════════════════
//  CONFIGURACIÓN
// ══════════════════════════════════════════════════════════════════════════

var NOMBRE_RAIZ = "PCFORGE - RFN1 Venta de computadoras";

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
        "Solicitud de compra del Cliente",
        "Datos del Cliente (Nombre, Apellido, DNI, Teléfono, Email)",
        "Tipo de configuración (Estándar / Configurable)",
        "Componentes elegidos o Modelo estándar",
        "Seña: 50% del precio total (forma de pago)",
        "Saldo pendiente al retirar",
        "Disponibilidad de Líneas de ensamblaje",
        "Resultado del Control de calidad"
    ],
    comportamiento: [
        "Registrar / buscar Cliente por DNI",
        "Armar la Computadora y calcular el precio total",
        "Registrar la Venta y reservar el Stock de componentes",
        "Registrar el pago de la seña y emitir el Recibo",
        "Generar la Orden de producción en estado Pendiente",
        "Asignar Línea de ensamblaje, fecha de inicio y responsable",
        "Registrar el Control de calidad y consumir el Stock reservado",
        "Asignar Número de serie y finalizar la Orden",
        "Cobrar el saldo, emitir la Factura y cerrar la Orden"
    ],
    salidas: [
        "Cliente registrado",
        "Venta registrada con su Computadora",
        "Recibo de seña",
        "Orden de producción (Pendiente → Planificada → En ensamblaje → Finalizada → Entregada)",
        "Stock de componentes actualizado",
        "Número de serie del equipo",
        "Factura de venta",
        "Computadora entregada al Cliente"
    ]
};

/*
 *  Los siete casos de uso del RFN1.
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

// ───────────────────────────────────────────────────────────────── CU01 ───
{
    id: "CU01",
    nombre: "Registrar venta",
    actor: "Recepcionista",
    lifelines: [
        "Recepcionista",
        "UI_96VA",
        "VentasBLL_96VA",
        "VentasMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Recepcionista", "UI_96VA",       "seleccionar Nueva venta()"],
        ["UI_96VA",       "UI_96VA",       "CargarCombos()"],
        ["Recepcionista", "UI_96VA",       "elegirCliente(cliente)"],
        ["Recepcionista", "UI_96VA",       "elegirTipo(Estandar | Configurable)"],
        ["UI_96VA",       "UI_96VA",       "ShowDialog(componentes)"],
        ["UI_96VA",       "UI_96VA",       ":List<Componente96VA>"],
        ["Recepcionista", "UI_96VA",       "RegistrarVenta()"],
        ["UI_96VA",       "VentasBLL_96VA", "RegistrarVenta(cliente, pc, fechaEntrega)"],
        ["VentasBLL_96VA", "VentasBLL_96VA", "ValidarStockDisponible(pc)"],
        ["VentasBLL_96VA", "VentasMPP_96VA", "Agregar(venta)"],
        ["VentasMPP_96VA", "DAL_96VA",      "AgregarVenta(...)"],
        ["DAL_96VA",      "VentasMPP_96VA", ":NumeroVenta"],
        ["VentasBLL_96VA", "VentasMPP_96VA", "ReservarStock(componentes)"],
        ["VentasBLL_96VA", "UI_96VA",       ":Venta96VA"],
        ["UI_96VA",       "Recepcionista", "mostrar confirmación"]
    ],
    clases: {
        "UI":  ["VentasControl", "FRMArmarPc96VA"],
        "BLL": ["VentasBLL96VA", "ComponentesBLL96VA"],
        "MPP": ["VentasMPP96VA", "ComponentesMPP96VA"],
        "DAL": ["VentasDAL96VA", "ComponentesDAL96VA"],
        "BE":  ["Venta96VA", "Computadora96VA", "Componente96VA", "Cliente96VA"]
    },
    tablas: [
        ["Ventas",              ["NumeroVenta : INT (PK)", "DniCliente : NVARCHAR(20) (FK)",
                                 "IdComputadora : INT (FK)", "PrecioTotal : DECIMAL(18,2)",
                                 "FechaEntregaEstimada : DATETIME", "Estado : INT"]],
        ["Computadoras",        ["Id : INT (PK)", "Nombre : NVARCHAR(100)",
                                 "TipoConfiguracion : INT"]],
        ["ComputadoraDetalle",  ["IdComputadora : INT (PK,FK)", "CodigoComponente : NVARCHAR(50) (PK,FK)",
                                 "Cantidad : INT"]],
        ["Componentes",         ["Codigo : NVARCHAR(50) (PK)", "Descripcion : NVARCHAR(200)",
                                 "PrecioUnitario : DECIMAL(18,2)", "Stock : INT",
                                 "StockReservado : INT", "StockMinimo : INT"]],
        ["Clientes",            ["Dni : NVARCHAR(20) (PK)", "Nombre : NVARCHAR(100)",
                                 "Apellido : NVARCHAR(100)"]]
    ],
    relaciones: [
        ["Clientes",           "Ventas",             "1", "0..*"],
        ["Computadoras",       "Ventas",             "1", "1"],
        ["Computadoras",       "ComputadoraDetalle", "1", "1..*"],
        ["Componentes",        "ComputadoraDetalle", "1", "0..*"]
    ],
    gui: {
        pantalla: "Nueva venta",
        controles: [
            "cboCliente : ComboBox — cliente de la venta",
            "btnNuevoCliente : Button — alta rápida (CU02)",
            "Tipo de equipo : TarjetaOpcion — Estándar / Configurable",
            "flpModelos : lista de modelos estándar con su precio",
            "btnArmar : Button — abre el configurador Armá tu PC",
            "dtpEntrega : DateTimePicker — entrega estimada",
            "Ticket : resumen del equipo + total en vivo",
            "btnRegistrar : Button — Registrar venta",
            "btnVolver : Button — Volver"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU02 ───
{
    id: "CU02",
    nombre: "Registrar cliente",
    actor: "Recepcionista",
    lifelines: [
        "Recepcionista",
        "UI_96VA",
        "ClientesBLL_96VA",
        "ClientesMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Recepcionista",   "UI_96VA",         "abrir Nuevo cliente()"],
        ["Recepcionista",   "UI_96VA",         "ingresarDatos(nombre, apellido, dni, tel, email)"],
        ["Recepcionista",   "UI_96VA",         "Guardar()"],
        ["UI_96VA",         "ClientesBLL_96VA", "Crear(cliente)"],
        ["ClientesBLL_96VA", "ClientesBLL_96VA", "Validar(cliente)"],
        ["ClientesBLL_96VA", "ClientesMPP_96VA", "ObtenerPorDni(dni)"],
        ["ClientesMPP_96VA", "ClientesBLL_96VA", "null (no existe)"],
        ["ClientesBLL_96VA", "ClientesMPP_96VA", "Agregar(cliente)"],
        ["ClientesMPP_96VA", "DAL_96VA",        "Agregar(...)"],
        ["ClientesBLL_96VA", "UI_96VA",         "ClienteCreado"],
        ["UI_96VA",         "Recepcionista",   "cliente registrado"]
    ],
    clases: {
        "UI":  ["FRMNuevoCliente96VA", "ClientesControl"],
        "BLL": ["ClientesBLL96VA"],
        "MPP": ["ClientesMPP96VA"],
        "DAL": ["ClientesDAL96VA"],
        "BE":  ["Cliente96VA"]
    },
    tablas: [
        ["Clientes", ["Dni : NVARCHAR(20) (PK)", "Nombre : NVARCHAR(100)", "Apellido : NVARCHAR(100)",
                      "Telefono : NVARCHAR(50)", "Email : NVARCHAR(150)", "Bit_Lo_Bo : BIT"]],
        ["Ventas",   ["NumeroVenta : INT (PK)", "DniCliente : NVARCHAR(20) (FK)"]]
    ],
    relaciones: [
        ["Clientes", "Ventas", "1", "0..*"]
    ],
    gui: {
        pantalla: "Nuevo cliente",
        controles: [
            "txtNombre : TextBox",
            "txtApellido : TextBox",
            "txtDni : TextBox — clave del cliente",
            "txtTelefono : TextBox",
            "txtEmail : TextBox",
            "btnGuardar : Button",
            "btnCancelar : Button"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU03 ───
{
    id: "CU03",
    nombre: "Registrar seña y emitir recibo",
    actor: "Recepcionista",
    lifelines: [
        "Recepcionista",
        "UI_96VA",
        "VentasBLL_96VA",
        "VentasMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Recepcionista", "UI_96VA",       "Registrar seña()"],
        ["UI_96VA",       "UI_96VA",       "mostrar monto = PrecioTotal * 0.50"],
        ["Recepcionista", "UI_96VA",       "elegirFormaPago(Efectivo|Transf.|Tarjeta)"],
        ["Recepcionista", "UI_96VA",       "Confirmar seña()"],
        ["UI_96VA",       "VentasBLL_96VA", "RegistrarSena(numeroVenta, forma, referencia)"],
        ["VentasBLL_96VA", "VentasBLL_96VA", "ValidarMontoSena(venta)"],
        ["VentasBLL_96VA", "VentasMPP_96VA", "AgregarPago(pago)"],
        ["VentasMPP_96VA", "DAL_96VA",      "sp_Pago_Agregar"],
        ["VentasBLL_96VA", "VentasMPP_96VA", "AgregarRecibo(recibo)"],
        ["VentasMPP_96VA", "DAL_96VA",      "sp_Recibo_Agregar"],
        ["VentasBLL_96VA", "VentasMPP_96VA", "CambiarEstado(Senada)"],
        ["VentasBLL_96VA", "UI_96VA",       ":Recibo96VA"],
        ["UI_96VA",       "UI_96VA",       "ExportarRecibo(recibo)"],
        ["UI_96VA",       "Recepcionista", "recibo en PDF"]
    ],
    clases: {
        "UI":  ["VentasControl", "ComprobantePcFactory96VA"],
        "BLL": ["VentasBLL96VA"],
        "MPP": ["VentasMPP96VA"],
        "DAL": ["VentasDAL96VA"],
        "BE":  ["Pago96VA", "Recibo96VA", "Venta96VA"]
    },
    tablas: [
        ["Pagos",   ["Id : INT (PK)", "NumeroVenta : INT (FK)", "Tipo : INT (Sena|SaldoFinal)",
                     "Monto : DECIMAL(18,2)", "FormaPago : INT", "Referencia : NVARCHAR(100)",
                     "Fecha : DATETIME"]],
        ["Recibos", ["Numero : NVARCHAR(30) (PK)", "NumeroVenta : INT (FK)", "IdPago : INT (FK)",
                     "MontoAbonado : DECIMAL(18,2)", "SaldoPendiente : DECIMAL(18,2)",
                     "FechaEmision : DATETIME"]],
        ["Ventas",  ["NumeroVenta : INT (PK)", "PrecioTotal : DECIMAL(18,2)", "Estado : INT"]]
    ],
    relaciones: [
        ["Ventas", "Pagos",   "1", "0..*"],
        ["Ventas", "Recibos", "1", "0..*"],
        ["Pagos",  "Recibos", "1", "0..1"]
    ],
    gui: {
        pantalla: "Registrar seña",
        controles: [
            "FichaDatos : cliente, equipo y total de la venta",
            "Seña a cobrar : monto destacado, 50% del total (no editable)",
            "Forma de pago : TarjetaOpcion — Efectivo / Transferencia / Tarjeta",
            "txtReferencia : TextBox — número de operación",
            "btnConfirmarPago : Button — Confirmar seña",
            "btnVolverPago : Button — Volver"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU04 ───
{
    id: "CU04",
    nombre: "Gestionar orden de produccion",
    actor: "Gerente",
    lifelines: [
        "Gerente",
        "UI_96VA",
        "OrdenProduccionBLL_96VA",
        "ProduccionMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Gerente",                "UI_96VA",                "seleccionar Nueva orden()"],
        ["UI_96VA",                "OrdenProduccionBLL_96VA", "ObtenerVentasDisponibles()"],
        ["OrdenProduccionBLL_96VA", "ProduccionMPP_96VA",     "ObtenerVentasSenadasSinOrden()"],
        ["ProduccionMPP_96VA",     "DAL_96VA",               "sp_Venta_ObtenerSenadas"],
        ["OrdenProduccionBLL_96VA", "UI_96VA",                ":List<Venta96VA>"],
        ["Gerente",                "UI_96VA",                "elegirVenta(venta)"],
        ["Gerente",                "UI_96VA",                "Registrar orden()"],
        ["UI_96VA",                "OrdenProduccionBLL_96VA", "RegistrarOrden(venta, fechaEntrega)"],
        ["OrdenProduccionBLL_96VA", "OrdenProduccionBLL_96VA", "ValidarVentaSenada(venta)"],
        ["OrdenProduccionBLL_96VA", "ProduccionMPP_96VA",     "Agregar(orden)"],
        ["ProduccionMPP_96VA",     "DAL_96VA",               "sp_OP_Agregar"],
        ["DAL_96VA",               "ProduccionMPP_96VA",     ":INSERT OrdenesProduccion"],
        ["DAL_96VA",               "ProduccionMPP_96VA",     ":NumeroOrden"],
        ["OrdenProduccionBLL_96VA", "UI_96VA",                "orden (Estado = Pendiente)"],
        ["UI_96VA",                "Gerente",                "orden registrada"]
    ],
    clases: {
        "UI":  ["ProduccionControl"],
        "BLL": ["OrdenProduccionBLL96VA", "VentasBLL96VA"],
        "MPP": ["ProduccionMPP96VA"],
        "DAL": ["ProduccionDAL96VA"],
        "BE":  ["OrdenProduccion96VA", "Venta96VA", "Cliente96VA"]
    },
    tablas: [
        ["OrdenesProduccion", ["NumeroOrden : INT (PK)", "NumeroVenta : INT (FK)",
                               "FechaRegistro : DATETIME", "FechaEntregaEstimada : DATETIME",
                               "Estado : INT", "IdLinea : INT (FK)",
                               "ResponsableTecnico : NVARCHAR(100)", "NumeroSerie : NVARCHAR(50)"]],
        ["Ventas",            ["NumeroVenta : INT (PK)", "DniCliente : NVARCHAR(20) (FK)",
                               "Estado : INT"]],
        ["Clientes",          ["Dni : NVARCHAR(20) (PK)", "Nombre : NVARCHAR(100)",
                               "Apellido : NVARCHAR(100)"]]
    ],
    relaciones: [
        ["Ventas",   "OrdenesProduccion", "1", "0..1"],
        ["Clientes", "Ventas",            "1", "0..*"]
    ],
    gui: {
        pantalla: "Nueva orden de produccion",
        controles: [
            "Ventas señadas : tarjetas con cliente, equipo y total",
            "FichaDatos : venta elegida (total, seña, saldo, entrega)",
            "dtpEntrega : DateTimePicker — fecha de entrega",
            "btnRegistrar : Button — Registrar orden",
            "btnVolverOrden : Button — Volver"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU05 ───
{
    id: "CU05",
    nombre: "Asignar linea de ensamblaje",
    actor: "Gerente",
    lifelines: [
        "Gerente",
        "UI_96VA",
        "LineasEnsamblajeBLL_96VA",
        "OrdenProduccionBLL_96VA",
        "ProduccionMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Gerente",                 "UI_96VA",                 "Asignar linea()"],
        ["UI_96VA",                 "LineasEnsamblajeBLL_96VA", "ObtenerTodas()"],
        ["LineasEnsamblajeBLL_96VA", "UI_96VA",                 "lineas disponibles"],
        ["Gerente",                 "UI_96VA",                 "elegirLinea(linea)"],
        ["Gerente",                 "UI_96VA",                 "ingresarFechaInicio() y responsable()"],
        ["Gerente",                 "UI_96VA",                 "Asignar()"],
        ["UI_96VA",                 "OrdenProduccionBLL_96VA", "AsignarLinea(nroOrden, idLinea, fecha, resp)"],
        ["OrdenProduccionBLL_96VA", "OrdenProduccionBLL_96VA", "ValidarLineaDisponible(linea)"],
        ["OrdenProduccionBLL_96VA", "ProduccionMPP_96VA",      "Planificar(orden)"],
        ["ProduccionMPP_96VA",      "DAL_96VA",                "sp_OP_Planificar"],
        ["OrdenProduccionBLL_96VA", "ProduccionMPP_96VA",      "OcuparLinea(linea)"],
        ["OrdenProduccionBLL_96VA", "UI_96VA",                 "orden (Estado = Planificada)"],
        ["UI_96VA",                 "Gerente",                 "linea asignada"]
    ],
    clases: {
        "UI":  ["ProduccionControl"],
        "BLL": ["OrdenProduccionBLL96VA", "LineasEnsamblajeBLL96VA"],
        "MPP": ["ProduccionMPP96VA", "LineasEnsamblajeMPP96VA"],
        "DAL": ["ProduccionDAL96VA", "LineasEnsamblajeDAL96VA"],
        "BE":  ["OrdenProduccion96VA", "LineaEnsamblaje96VA"]
    },
    tablas: [
        ["OrdenesProduccion", ["NumeroOrden : INT (PK)", "IdLinea : INT (FK)",
                               "FechaInicioPrevista : DATETIME",
                               "ResponsableTecnico : NVARCHAR(100)", "Estado : INT"]],
        ["LineasEnsamblaje",  ["Id : INT (PK)", "Nombre : NVARCHAR(100)",
                               "Descripcion : NVARCHAR(200)", "Disponible : BIT"]]
    ],
    relaciones: [
        ["LineasEnsamblaje", "OrdenesProduccion", "1", "0..*"]
    ],
    gui: {
        pantalla: "Asignar linea",
        controles: [
            "Lineas disponibles : TarjetaOpcion por línea, con su estado",
            "dtpInicio : DateTimePicker — fecha de inicio prevista",
            "txtResp : TextBox — responsable técnico",
            "btnConfirmarPlan : Button — Asignar",
            "btnVolverPlan : Button — Volver"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU06 ───
{
    id: "CU06",
    nombre: "Cerrar orden de produccion",
    actor: "Responsable tecnico",
    lifelines: [
        "Responsable tecnico",
        "UI_96VA",
        "OrdenProduccionBLL_96VA",
        "ComponentesMPP_96VA",
        "ProduccionMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Responsable tecnico",    "UI_96VA",                "Cerrar orden()"],
        ["UI_96VA",                "UI_96VA",                "mostrar checklist de control de calidad"],
        ["Responsable tecnico",    "UI_96VA",                "marcarChecks(encendido, conexiones, SO, drivers)"],
        ["Responsable tecnico",    "UI_96VA",                "Confirmar cierre()"],
        ["UI_96VA",                "OrdenProduccionBLL_96VA", "RegistrarControlCalidad(nroOrden, control)"],
        ["OrdenProduccionBLL_96VA", "OrdenProduccionBLL_96VA", "EvaluarAprobacion(control)"],
        ["OrdenProduccionBLL_96VA", "ComponentesMPP_96VA",    "ConsumirReserva(componentes)"],
        ["ComponentesMPP_96VA",    "DAL_96VA",               "UPDATE Stock, StockReservado"],
        ["OrdenProduccionBLL_96VA", "OrdenProduccionBLL_96VA", "GenerarNumeroSerie(orden)"],
        ["OrdenProduccionBLL_96VA", "ProduccionMPP_96VA",     "Cerrar(nroOrden, numeroSerie)"],
        ["ProduccionMPP_96VA",     "DAL_96VA",               "sp_OP_Cerrar"],
        ["OrdenProduccionBLL_96VA", "ProduccionMPP_96VA",     "LiberarLinea(linea)"],
        ["OrdenProduccionBLL_96VA", "UI_96VA",                "numeroSerie (Estado = Finalizada)"],
        ["UI_96VA",                "Responsable tecnico",    "orden cerrada"]
    ],
    clases: {
        "UI":  ["ProduccionControl"],
        "BLL": ["OrdenProduccionBLL96VA"],
        "MPP": ["ProduccionMPP96VA", "ComponentesMPP96VA"],
        "DAL": ["ProduccionDAL96VA", "ComponentesDAL96VA"],
        "BE":  ["OrdenProduccion96VA", "ControlCalidad96VA", "Componente96VA"]
    },
    tablas: [
        ["OrdenesProduccion", ["NumeroOrden : INT (PK)", "Estado : INT",
                               "NumeroSerie : NVARCHAR(50)", "FechaCierre : DATETIME"]],
        ["ControlCalidad",    ["NumeroOrden : INT (PK,FK)", "Encendido : BIT", "Conexiones : BIT",
                               "SistemaOperativo : BIT", "Drivers : BIT",
                               "Observaciones : NVARCHAR(500)", "Responsable : NVARCHAR(100)"]],
        ["Componentes",       ["Codigo : NVARCHAR(50) (PK)", "Stock : INT", "StockReservado : INT"]]
    ],
    relaciones: [
        ["OrdenesProduccion", "ControlCalidad", "1", "1"],
        ["OrdenesProduccion", "Componentes",    "1", "1..*"]
    ],
    gui: {
        pantalla: "Cerrar orden",
        controles: [
            "Checklist de control de calidad : 4 items — encendido, conexiones, SO, drivers",
            "Veredicto en vivo : indica si aprueba o vuelve a revisión antes de confirmar",
            "txtRespCc : TextBox — responsable del control",
            "txtObs : TextBox — observaciones (obligatorias si no aprueba)",
            "btnConfirmarCierre : Button — Aprobar y cerrar orden / Mandar a revisión",
            "btnVolverCierre : Button — Volver"
        ]
    }
},

// ───────────────────────────────────────────────────────────────── CU07 ───
{
    id: "CU07",
    nombre: "Entregar computadora",
    actor: "Recepcionista",
    lifelines: [
        "Recepcionista",
        "UI_96VA",
        "EntregasBLL_96VA",
        "VentasMPP_96VA",
        "ProduccionMPP_96VA",
        "DAL_96VA"
    ],
    mensajes: [
        ["Recepcionista",   "UI_96VA",           "Registrar entrega()"],
        ["UI_96VA",         "EntregasBLL_96VA",  "ObtenerOrden(nroOrden)"],
        ["EntregasBLL_96VA", "UI_96VA",           "orden (saldo, numero de serie)"],
        ["Recepcionista",   "UI_96VA",           "elegirFormaPago(forma)"],
        ["Recepcionista",   "UI_96VA",           "Confirmar entrega()"],
        ["UI_96VA",         "EntregasBLL_96VA",  "RegistrarEntrega(nroOrden, forma, referencia)"],
        ["EntregasBLL_96VA", "EntregasBLL_96VA",  "ValidarSaldoPendiente(venta)"],
        ["EntregasBLL_96VA", "VentasMPP_96VA",    "AgregarPago(saldoFinal)"],
        ["VentasMPP_96VA",  "DAL_96VA",          "sp_Pago_Agregar"],
        ["EntregasBLL_96VA", "VentasMPP_96VA",    "AgregarFacturaVenta(factura)"],
        ["VentasMPP_96VA",  "DAL_96VA",          "sp_FacturaVenta_Agregar"],
        ["EntregasBLL_96VA", "ProduccionMPP_96VA", "CambiarEstado(Entregada)"],
        ["EntregasBLL_96VA", "UI_96VA",           ":FacturaVenta96VA"],
        ["UI_96VA",         "Recepcionista",     "factura y equipo entregado"]
    ],
    clases: {
        "UI":  ["EntregasControl", "ComprobantePcFactory96VA"],
        "BLL": ["EntregasBLL96VA", "VentasBLL96VA"],
        "MPP": ["VentasMPP96VA", "ProduccionMPP96VA"],
        "DAL": ["VentasDAL96VA", "ProduccionDAL96VA"],
        "BE":  ["FacturaVenta96VA", "Pago96VA", "OrdenProduccion96VA", "Venta96VA"]
    },
    tablas: [
        ["FacturasVenta",     ["Numero : NVARCHAR(30) (PK)", "NumeroVenta : INT (FK)",
                               "FechaEmision : DATETIME", "Total : DECIMAL(18,2)",
                               "NumeroSerie : NVARCHAR(50)"]],
        ["Pagos",             ["Id : INT (PK)", "NumeroVenta : INT (FK)", "Tipo : INT",
                               "Monto : DECIMAL(18,2)", "FormaPago : INT"]],
        ["OrdenesProduccion", ["NumeroOrden : INT (PK)", "NumeroVenta : INT (FK)", "Estado : INT"]],
        ["Ventas",            ["NumeroVenta : INT (PK)", "PrecioTotal : DECIMAL(18,2)"]]
    ],
    relaciones: [
        ["Ventas",            "FacturasVenta",     "1", "0..1"],
        ["Ventas",            "Pagos",             "1", "1..*"],
        ["Ventas",            "OrdenesProduccion", "1", "0..1"]
    ],
    gui: {
        pantalla: "Registrar entrega",
        controles: [
            "FichaDatos : cliente, DNI, equipo, número de serie, total y abonado",
            "Saldo a cobrar : monto destacado",
            "Forma de pago : TarjetaOpcion — Efectivo / Transferencia / Tarjeta",
            "txtReferencia : TextBox",
            "btnConfirmar : Button — Confirmar entrega",
            "btnVolver : Button — Volver"
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
    "VentasControl": { at: [],
              op: ["CargarVentas() : void", "SeleccionarVenta(numero : int) : void", "AbrirFormVenta() : void", "AplicarModeloSeleccionado() : void", "AbrirAsistenteComponentes() : void", "AbrirNuevoCliente() : void", "RegistrarVenta() : void", "AbrirFormPago() : void", "ConfirmarPago() : void", "AnularVenta() : void"] },
    "FRMArmarPc96VA": { at: [],
              op: ["CargarCatalogo(tipo : TipoComponente96VA) : void", "Elegir(tipo : TipoComponente96VA, componente : Componente96VA) : void", "ArmarChasis() : void", "MostrarPaso() : void"] },
    "FRMNuevoCliente96VA": { at: [],
              op: ["Guardar() : void"] },
    "ClientesControl": { at: [],
              op: ["CargarDatosEnGrilla(grilla : DataGridView) : void", "CargarSeleccionEnCampos() : bool", "Guardar(editando : bool) : bool", "EliminarSeleccion() : void"] },
    "ComprobantePcFactory96VA": { at: [],
              op: ["GenerarReciboSena(venta : Venta96VA, sena : Pago96VA) : string", "GenerarFactura(venta : Venta96VA, orden : OrdenProduccion96VA) : string", "Abrir(ruta : string) : void"] },
    "ProduccionControl": { at: [],
              op: ["CargarOrdenes() : void", "RefrescarTablero(ordenes : List<OrdenProduccion96VA>) : void", "SeleccionarOrden(numero : int) : void", "AbrirFormOrden() : void", "RegistrarOrden() : void", "AbrirFormPlan() : void", "AsignarLinea() : void", "AbrirFormCierre() : void", "CerrarOrden() : void"] },
    "EntregasControl": { at: [],
              op: ["Cargar() : void", "CargarVistas() : void", "SeleccionarOrden(numeroOrden : int) : void", "AbrirFormCobro() : void", "ConfirmarEntrega() : void"] },
    "VentasBLL96VA": { at: [],
              op: ["ObtenerTodas() : List<Venta96VA>", "ObtenerPorNumero(numeroVenta : int) : Venta96VA", "ObtenerRecibos() : List<Recibo96VA>", "ObtenerOrdenDeVenta(numeroVenta : int) : OrdenProduccion96VA", "ObtenerListasParaProduccion() : List<Venta96VA>", "RegistrarVenta(cliente : Cliente96VA, computadora : Computadora96VA, fechaEntregaEstimada : DateTime) : Venta96VA", "RegistrarSena(numeroVenta : int, formaPago : FormaPago96VA, referencia : string) : Pago96VA", "AnularVenta(numeroVenta : int) : void"] },
    "ComponentesBLL96VA": { at: [],
              op: ["ObtenerTodos() : List<Componente96VA>", "ObtenerBajoStock() : List<Componente96VA>", "SumarStock(codigo : string, cantidad : int) : void", "ObtenerPorCodigo(codigo : string) : Componente96VA", "Crear(c : Componente96VA) : void", "Modificar(c : Componente96VA) : void", "Eliminar(codigo : string) : void", "Reactivar(codigo : string) : void", "ActivarHistorico(version : ComponenteHistorico96VA) : void"] },
    "ClientesBLL96VA": { at: [],
              op: ["ObtenerTodos() : List<Cliente96VA>", "ObtenerPorDni(dni : string) : Cliente96VA", "Crear(cliente : Cliente96VA) : void", "Modificar(cliente : Cliente96VA) : void", "Eliminar(dni : string) : void"] },
    "OrdenProduccionBLL96VA": { at: [],
              op: ["ObtenerTodas() : List<OrdenProduccion96VA>", "ObtenerPorNumero(numero : int) : OrdenProduccion96VA", "ObtenerVentasDisponibles() : List<Venta96VA>", "CrearOrden(numeroVenta : int, fechaEntregaEstimada : DateTime) : OrdenProduccion96VA", "AsignarLinea(numeroOrden : int, idLinea : int, fechaInicio : DateTime, responsable : string) : void", "IniciarEnsamblaje(numeroOrden : int) : void", "RegistrarControlCalidad(numeroOrden : int, control : ControlCalidad96VA) : string", "CambiarEstado(numeroOrden : int, estado : EstadoOrdenProduccion96VA) : void", "VolverAtras(numeroOrden : int) : EstadoOrdenProduccion96VA"] },
    "LineasEnsamblajeBLL96VA": { at: [],
              op: ["ObtenerTodas() : List<LineaEnsamblaje96VA>", "ObtenerPorId(id : int) : LineaEnsamblaje96VA", "Crear(l : LineaEnsamblaje96VA) : void", "Modificar(l : LineaEnsamblaje96VA) : void", "Eliminar(id : int) : void"] },
    "EntregasBLL96VA": { at: [],
              op: ["ObtenerPendientesDeEntrega() : List<OrdenProduccion96VA>", "ObtenerEntregadas() : List<OrdenProduccion96VA>", "Filtrar(ordenes : IEnumerable<OrdenProduccion96VA>, criterio : string) : List<OrdenProduccion96VA>", "ObtenerOrden(numeroOrden : int) : OrdenProduccion96VA", "RegistrarEntrega(numeroOrden : int, formaPago : FormaPago96VA, referencia : string) : Pago96VA"] },
    "VentasMPP96VA": { at: [],
              op: ["GuardarComputadora(pc : Computadora96VA) : void", "ObtenerComputadora(id : int) : Computadora96VA", "AgregarVenta(venta : Venta96VA) : void", "ObtenerTodas() : List<Venta96VA>", "ObtenerPorNumero(numero : int) : Venta96VA", "ObtenerParaProduccion() : List<Venta96VA>", "ObtenerPagos(numeroVenta : int) : List<Pago96VA>"] },
    "ComponentesMPP96VA": { at: [],
              op: ["ObtenerTodos() : List<Componente96VA>", "ObtenerPorCodigo(codigo : string) : Componente96VA", "ObtenerBajoStock() : List<Componente96VA>", "Agregar(c : Componente96VA) : void", "Modificar(c : Componente96VA) : void", "Eliminar(codigo : string) : void", "DescontarStock(codigo : string, cantidad : int) : void"] },
    "ClientesMPP96VA": { at: [],
              op: ["ObtenerTodos() : List<Cliente96VA>", "ObtenerPorDni(dni : string) : Cliente96VA", "Agregar(cliente : Cliente96VA) : void", "Modificar(cliente : Cliente96VA) : void", "Eliminar(dni : string) : void"] },
    "ProduccionMPP96VA": { at: [],
              op: ["AgregarOrden(orden : OrdenProduccion96VA) : void", "ObtenerTodas() : List<OrdenProduccion96VA>", "ObtenerPorNumero(numero : int) : OrdenProduccion96VA", "ObtenerPorEstado(estado : EstadoOrdenProduccion96VA) : List<OrdenProduccion96VA>", "ObtenerPorVenta(numeroVenta : int) : OrdenProduccion96VA"] },
    "LineasEnsamblajeMPP96VA": { at: [],
              op: ["ObtenerTodas() : List<LineaEnsamblaje96VA>", "ObtenerPorId(id : int) : LineaEnsamblaje96VA", "Agregar(l : LineaEnsamblaje96VA) : void", "Modificar(l : LineaEnsamblaje96VA) : void"] },
    "VentasDAL96VA": { at: [],
              op: ["AgregarComputadora(nombre : string, tipoConfiguracion : int, precioTotal : decimal) : int", "AgregarComponenteAComputadora(idComputadora : int, codigoComponente : string, cantidad : int) : void", "AgregarVenta(dniCliente : string, idComputadora : int, fechaEntregaEstimada : DateTime, usuario : string) : int", "CambiarEstadoVenta(numero : int, estado : int) : void"] },
    "ComponentesDAL96VA": { at: [],
              op: ["ObtenerTodos() : DataTable", "ObtenerPorCodigo(codigo : string) : DataTable", "ObtenerBajoStock() : DataTable", "SumarStock(codigo : string, cantidad : int) : void", "BajaLogica(codigo : string) : void", "Reactivar(codigo : string) : void", "Eliminar(codigo : string) : void", "ActivarHistorico(idHistorico : int) : void", "DescontarStock(codigo : string, cantidad : int) : void", "ReservarStock(codigo : string, cantidad : int) : void", "LiberarReserva(codigo : string, cantidad : int) : void", "ConsumirReserva(codigo : string, cantidad : int) : void"] },
    "ClientesDAL96VA": { at: [],
              op: ["ObtenerTodos() : DataTable", "ObtenerPorDni(dni : string) : DataTable", "Agregar(dni : string, nombre : string, apellido : string, telefono : string, direccion : string) : void", "Modificar(dni : string, nombre : string, apellido : string, telefono : string, direccion : string) : void", "Eliminar(dni : string) : void"] },
    "ProduccionDAL96VA": { at: [],
              op: ["AgregarOrden(numeroVenta : int, fechaEntregaEstimada : DateTime) : int", "PlanificarOrden(numero : int, idLinea : int, fechaInicio : DateTime, responsable : string) : void", "CambiarEstadoOrden(numero : int, estado : int) : void", "CerrarOrden(numero : int, numeroSerie : string) : void"] },
    "LineasEnsamblajeDAL96VA": { at: [],
              op: ["ObtenerTodas() : DataTable", "ObtenerPorId(id : int) : DataTable", "Agregar(nombre : string, descripcion : string, disponible : bool) : int", "Modificar(id : int, nombre : string, descripcion : string, disponible : bool) : void", "Eliminar(id : int) : void"] },
    "Venta96VA": { at: ["NumeroVenta : int", "Cliente : Cliente96VA", "Computadora : Computadora96VA", "FechaVenta : DateTime", "FechaEntregaEstimada : DateTime", "Estado : EstadoVenta96VA", "UsuarioRegistro : string", "NumeroOrdenProduccion : int?", "Pagos : List<Pago96VA>"],
              op: [] },
    "Computadora96VA": { at: ["Id : int", "Nombre : string", "TipoConfiguracion : TipoConfiguracion96VA", "Componentes : List<Componente96VA>"],
              op: [] },
    "Componente96VA": { at: ["Codigo : string", "Descripcion : string", "Tipo : TipoComponente96VA", "Marca : string", "Modelo : string", "PrecioUnitario : decimal", "Stock : int", "StockMinimo : int", "StockReservado : int", "BajaLogica : bool"],
              op: [] },
    "Cliente96VA": { at: ["Dni : string", "Nombre : string", "Apellido : string", "Telefono : string", "Direccion : string"],
              op: [] },
    "Pago96VA": { at: ["Id : int", "NumeroVenta : int", "Tipo : TipoPago96VA", "NumeroRecibo : string", "Monto : decimal", "FormaPago : FormaPago96VA", "Referencia : string", "Fecha : DateTime", "Usuario : string"],
              op: [] },
    "Recibo96VA": { at: ["Id : string", "Pago : Pago96VA", "Venta : Venta96VA", "FechaEmision : DateTime", "MontoAbonado : decimal", "SaldoPendiente : decimal", "FechaEntregaEstimada : DateTime"],
              op: [] },
    "OrdenProduccion96VA": { at: ["NumeroOrden : int", "Venta : Venta96VA", "NumeroVenta : int", "FechaRegistro : DateTime", "FechaEntregaEstimada : DateTime", "Estado : EstadoOrdenProduccion96VA", "LineaEnsamblaje : LineaEnsamblaje96VA", "FechaInicioPrevista : DateTime?", "ResponsableTecnico : string", "ControlCalidad : ControlCalidad96VA", "NumeroSerie : string", "FechaCierre : DateTime?"],
              op: [] },
    "LineaEnsamblaje96VA": { at: ["Id : int", "Nombre : string", "Descripcion : string", "Disponible : bool"],
              op: [] },
    "ControlCalidad96VA": { at: ["Encendido : bool", "Conexiones : bool", "SistemaOperativo : bool", "Drivers : bool", "Observaciones : string", "Fecha : DateTime?", "Responsable : string"],
              op: [] },
    "FacturaVenta96VA": { at: ["NumeroFactura : string", "NumeroOrden : int", "NumeroVenta : int", "FechaEmision : DateTime", "Total : decimal"],
              op: [] }
};

// ========================================================================
//  DETALLE DEL DIAGRAMA DE CLASES, CASO POR CASO
//
//  Para cada CU se define:
//    metodos : que operaciones de cada clase participan de ESE caso de uso.
//              (una clase como VentasBLL96VA tiene muchas operaciones, pero
//               en el DC de un CU solo deben verse las que ese CU usa).
//    enlaces : las relaciones entre las clases del diagrama, una por una,
//              para que ninguna clase quede suelta.
//              [origen, destino, tipo, etiqueta]
// ========================================================================
var DC = {

"CU01": {
    metodos: {
        "VentasControl":       ["AbrirFormVenta", "AplicarModeloSeleccionado", "AbrirAsistenteComponentes", "AbrirNuevoCliente", "RegistrarVenta"],
        "FRMArmarPc96VA":      ["CargarCatalogo", "Elegir", "ArmarChasis"],
        "VentasBLL96VA":       ["RegistrarVenta", "ObtenerTodas", "ObtenerPorNumero"],
        "ComponentesBLL96VA":  ["ObtenerTodos", "ObtenerPorCodigo", "ObtenerBajoStock"],
        "VentasMPP96VA":       ["GuardarComputadora", "AgregarVenta", "ObtenerPorNumero"],
        "ComponentesMPP96VA":  ["ObtenerTodos", "ObtenerPorCodigo", "DescontarStock"],
        "VentasDAL96VA":       ["AgregarComputadora", "AgregarComponenteAComputadora", "AgregarVenta"],
        "ComponentesDAL96VA":  ["ObtenerTodos", "ReservarStock", "DescontarStock"]
    },
    enlaces: [
        ["VentasControl",      "FRMArmarPc96VA",     "Dependency", "abre"],
        ["VentasControl",      "VentasBLL96VA",      "Dependency", "usa"],
        ["FRMArmarPc96VA",     "ComponentesBLL96VA", "Dependency", "usa"],
        ["VentasBLL96VA",      "ComponentesBLL96VA", "Dependency", "reserva stock"],
        ["VentasBLL96VA",      "VentasMPP96VA",      "Dependency", "usa"],
        ["ComponentesBLL96VA", "ComponentesMPP96VA", "Dependency", "usa"],
        ["VentasMPP96VA",      "VentasDAL96VA",      "Dependency", "usa"],
        ["ComponentesMPP96VA", "ComponentesDAL96VA", "Dependency", "usa"],
        ["VentasMPP96VA",      "Venta96VA",          "Dependency", "mapea"],
        ["VentasMPP96VA",      "Computadora96VA",    "Dependency", "mapea"],
        ["ComponentesMPP96VA", "Componente96VA",     "Dependency", "mapea"],
        ["VentasBLL96VA",      "Cliente96VA",        "Dependency", "usa"],
        ["Venta96VA",          "Cliente96VA",        "Association", ""],
        ["Venta96VA",          "Computadora96VA",    "Association", ""],
        ["Computadora96VA",    "Componente96VA",     "Association", ""]
    ]
},

"CU02": {
    metodos: {},
    enlaces: [
        ["ClientesControl",      "ClientesBLL96VA", "Dependency", "usa"],
        ["ClientesControl",      "FRMNuevoCliente96VA", "Dependency", "abre"],
        ["FRMNuevoCliente96VA",  "ClientesBLL96VA", "Dependency", "usa"],
        ["ClientesBLL96VA",      "ClientesMPP96VA", "Dependency", "usa"],
        ["ClientesMPP96VA",      "ClientesDAL96VA", "Dependency", "usa"],
        ["ClientesMPP96VA",      "Cliente96VA",     "Dependency", "mapea"],
        ["ClientesBLL96VA",      "Cliente96VA",     "Dependency", "maneja"]
    ]
},

"CU03": {
    metodos: {
        "VentasControl":            ["AbrirFormPago", "ConfirmarPago"],
        "ComprobantePcFactory96VA": ["GenerarReciboSena", "Abrir"],
        "VentasBLL96VA":            ["ObtenerPorNumero", "RegistrarSena", "ObtenerRecibos"],
        "VentasMPP96VA":            ["ObtenerPorNumero", "ObtenerPagos"],
        "VentasDAL96VA":            ["CambiarEstadoVenta"]
    },
    enlaces: [
        ["VentasControl",  "VentasBLL96VA",            "Dependency", "usa"],
        ["VentasControl",  "ComprobantePcFactory96VA", "Dependency", "usa"],
        ["VentasBLL96VA",  "VentasMPP96VA",            "Dependency", "usa"],
        ["VentasMPP96VA",  "VentasDAL96VA",            "Dependency", "usa"],
        ["VentasMPP96VA",  "Pago96VA",                 "Dependency", "mapea"],
        ["VentasMPP96VA",  "Venta96VA",                "Dependency", "mapea"],
        ["VentasBLL96VA",  "Recibo96VA",               "Dependency", "genera"],
        ["Pago96VA",       "Venta96VA",                "Association", ""],
        ["Recibo96VA",     "Pago96VA",                 "Association", ""]
    ]
},

"CU04": {
    metodos: {
        "ProduccionControl":      ["CargarOrdenes", "RefrescarTablero", "AbrirFormOrden", "RegistrarOrden"],
        "OrdenProduccionBLL96VA": ["ObtenerTodas", "ObtenerVentasDisponibles", "CrearOrden", "CambiarEstado"],
        "VentasBLL96VA":          ["ObtenerListasParaProduccion", "ObtenerOrdenDeVenta"],
        "ProduccionMPP96VA":      ["AgregarOrden", "ObtenerTodas", "ObtenerPorEstado"],
        "ProduccionDAL96VA":      ["AgregarOrden", "CambiarEstadoOrden"]
    },
    enlaces: [
        ["ProduccionControl",      "OrdenProduccionBLL96VA", "Dependency", "usa"],
        ["OrdenProduccionBLL96VA", "VentasBLL96VA",          "Dependency", "usa"],
        ["OrdenProduccionBLL96VA", "ProduccionMPP96VA",      "Dependency", "usa"],
        ["ProduccionMPP96VA",      "ProduccionDAL96VA",      "Dependency", "usa"],
        ["ProduccionMPP96VA",      "OrdenProduccion96VA",    "Dependency", "mapea"],
        ["VentasBLL96VA",          "Venta96VA",              "Dependency", "maneja"],
        ["OrdenProduccion96VA",    "Venta96VA",              "Association", ""],
        ["Venta96VA",              "Cliente96VA",            "Association", ""]
    ]
},

"CU05": {
    metodos: {
        "ProduccionControl":       ["AbrirFormPlan", "AsignarLinea"],
        "OrdenProduccionBLL96VA":  ["ObtenerPorNumero", "AsignarLinea", "IniciarEnsamblaje", "VolverAtras"],
        "LineasEnsamblajeBLL96VA": ["ObtenerTodas", "ObtenerPorId", "Modificar"],
        "ProduccionMPP96VA":       ["ObtenerPorNumero"],
        "LineasEnsamblajeMPP96VA": ["ObtenerTodas", "ObtenerPorId", "Modificar"],
        "ProduccionDAL96VA":       ["PlanificarOrden", "CambiarEstadoOrden"],
        "LineasEnsamblajeDAL96VA": ["ObtenerTodas", "Modificar"]
    },
    enlaces: [
        ["ProduccionControl",       "OrdenProduccionBLL96VA",  "Dependency", "usa"],
        ["ProduccionControl",       "LineasEnsamblajeBLL96VA", "Dependency", "usa"],
        ["OrdenProduccionBLL96VA",  "ProduccionMPP96VA",       "Dependency", "usa"],
        ["OrdenProduccionBLL96VA",  "LineasEnsamblajeMPP96VA", "Dependency", "ocupa / libera"],
        ["LineasEnsamblajeBLL96VA", "LineasEnsamblajeMPP96VA", "Dependency", "usa"],
        ["ProduccionMPP96VA",       "ProduccionDAL96VA",       "Dependency", "usa"],
        ["LineasEnsamblajeMPP96VA", "LineasEnsamblajeDAL96VA", "Dependency", "usa"],
        ["ProduccionMPP96VA",       "OrdenProduccion96VA",     "Dependency", "mapea"],
        ["LineasEnsamblajeMPP96VA", "LineaEnsamblaje96VA",     "Dependency", "mapea"],
        ["OrdenProduccion96VA",     "LineaEnsamblaje96VA",     "Association", ""]
    ]
},

"CU06": {
    metodos: {
        "ProduccionControl":      ["AbrirFormCierre", "CerrarOrden"],
        "OrdenProduccionBLL96VA": ["ObtenerPorNumero", "RegistrarControlCalidad", "CambiarEstado"],
        "ProduccionMPP96VA":      ["ObtenerPorNumero"],
        "ComponentesMPP96VA":     ["DescontarStock"],
        "ProduccionDAL96VA":      ["CerrarOrden", "CambiarEstadoOrden"],
        "ComponentesDAL96VA":     ["ConsumirReserva", "LiberarReserva", "DescontarStock"]
    },
    enlaces: [
        ["ProduccionControl",      "OrdenProduccionBLL96VA", "Dependency", "usa"],
        ["OrdenProduccionBLL96VA", "ProduccionMPP96VA",      "Dependency", "usa"],
        ["OrdenProduccionBLL96VA", "ComponentesMPP96VA",     "Dependency", "consume reserva"],
        ["ProduccionMPP96VA",      "ProduccionDAL96VA",      "Dependency", "usa"],
        ["ComponentesMPP96VA",     "ComponentesDAL96VA",     "Dependency", "usa"],
        ["ProduccionMPP96VA",      "OrdenProduccion96VA",    "Dependency", "mapea"],
        ["ComponentesMPP96VA",     "Componente96VA",         "Dependency", "mapea"],
        ["OrdenProduccion96VA",    "ControlCalidad96VA",     "Association", ""]
    ]
},

"CU07": {
    metodos: {
        "EntregasControl":          ["Cargar", "SeleccionarOrden", "AbrirFormCobro", "ConfirmarEntrega"],
        "ComprobantePcFactory96VA": ["GenerarFactura", "Abrir"],
        "EntregasBLL96VA":          ["ObtenerPendientesDeEntrega", "ObtenerOrden", "RegistrarEntrega", "Filtrar"],
        "VentasBLL96VA":            ["ObtenerPorNumero", "RegistrarSena"],
        "VentasMPP96VA":            ["ObtenerPorNumero", "ObtenerPagos"],
        "ProduccionMPP96VA":        ["ObtenerPorNumero"],
        "VentasDAL96VA":            ["CambiarEstadoVenta"],
        "ProduccionDAL96VA":        ["CambiarEstadoOrden"]
    },
    enlaces: [
        ["EntregasControl",   "EntregasBLL96VA",          "Dependency", "usa"],
        ["EntregasControl",   "ComprobantePcFactory96VA", "Dependency", "usa"],
        ["EntregasBLL96VA",   "VentasBLL96VA",            "Dependency", "usa"],
        ["EntregasBLL96VA",   "ProduccionMPP96VA",        "Dependency", "usa"],
        ["EntregasBLL96VA",   "FacturaVenta96VA",         "Dependency", "genera"],
        ["VentasBLL96VA",     "VentasMPP96VA",            "Dependency", "usa"],
        ["VentasMPP96VA",     "VentasDAL96VA",            "Dependency", "usa"],
        ["ProduccionMPP96VA", "ProduccionDAL96VA",        "Dependency", "usa"],
        ["VentasMPP96VA",     "Pago96VA",                 "Dependency", "mapea"],
        ["ProduccionMPP96VA", "OrdenProduccion96VA",      "Dependency", "mapea"],
        ["OrdenProduccion96VA", "Venta96VA",              "Association", ""],
        ["FacturaVenta96VA",  "Venta96VA",                "Association", ""]
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
    var dia = nuevoDiagrama(pkgPre, "ECS - Entrada / Comportamiento / Salida (RFN1)", "Activity");

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
    conectar(eSal, eCom, "ControlFlow", "realimentacion (estado de la orden)");

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
    Repository.EnsureOutputVisible("Script");
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - Generando modelo RFN1");
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
    var pkgPre = nuevoPaquete(raiz, "N01.RF1 Prediseno del negocio");
    generarECS(pkgPre);

    // ── Diseño: un subpaquete por caso de uso ────────────────────────────
    var pkgDis = nuevoPaquete(raiz, "N02.RF1 Diseno del negocio");

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

    Repository.RefreshModelView(0);

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

main();
