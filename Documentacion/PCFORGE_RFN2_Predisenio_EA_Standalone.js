//
// ===========================================================================
//  PCFORGE - PREDISENIO DEL NEGOCIO - RFN2  (version AUTONOMA, sin ventana Scripting)
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
//        cscript //nologo "C:\Users\Thiago\source\repos\IngSoftValdezAlegre\Documentacion\PCFORGE_RFN2_Predisenio_EA_Standalone.js" "C:\Users\Thiago\Documents\PCFORGE.eapx"
//
//   3. Se ve el avance en la consola. Al terminar, abrir el proyecto en EA:
//      el paquete "PCFORGE - N01.RF2 Prediseno del negocio" ya esta en el Project Browser.
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
 *  PCFORGE — PREDISEÑO DEL NEGOCIO, RFN2 (Compra de insumos)
 *  Trabajo de Diploma — UAI — Valdez / Alegre
 * ===========================================================================
 *
 *  QUÉ HACE
 *  --------
 *  Crea dentro del modelo abierto un paquete "PCFORGE - N01.RF2 Prediseno"
 *  con los cinco diagramas de la seccion N01.RF2 del indice de la catedra:
 *
 *    ├─ N01.RF2 Diagrama de roles
 *    ├─ N01.RF2 Diagrama de secuencia de roles
 *    ├─ N01.RF2 Descripcion funcional del proceso
 *    │     (el esquema Entradas / Comportamiento / Salida)
 *    ├─ N01.RF2 Diagrama de actividad  (una calle por rol)
 *    └─ N01.RF2 Diagrama conceptual
 *
 *  Es independiente de PCFORGE_RFN2_EA.js, que genera el diseño del negocio
 *  (los DS / DC / DER / GUI de CU08 a CU12). Se pueden correr por separado y
 *  en cualquier orden.
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
 *  que no pisa lo anterior.
 * ===========================================================================
 */

// ══════════════════════════════════════════════════════════════════════════
//  CONFIGURACIÓN
// ══════════════════════════════════════════════════════════════════════════

var NOMBRE_RAIZ = "PCFORGE - N01.RF2 Prediseno del negocio";

var LIFELINE_ANCHO = 130;   // ancho de cada línea de vida en el DSR
var LIFELINE_SEP   = 40;    // separación entre líneas de vida

// ══════════════════════════════════════════════════════════════════════════
//  DATOS DEL MODELO
//  Todo lo que sigue es DECLARATIVO: para cambiar un rol, un mensaje o una
//  entidad, se edita acá y se vuelve a correr el script.
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

// ══════════════════════════════════════════════════════════════════════════
//  PREDISEÑO DEL NEGOCIO — RFN2
//  Roles, secuencia de roles, actividad y modelo conceptual del proceso.
//  Mismo formato que los diagramas del RFN1 que ya están en el documento.
// ══════════════════════════════════════════════════════════════════════════

/*
 *  Roles intervinientes, tal como están identificados en el documento.
 *    tipo: "Actor"    -> persona que interviene en el proceso
 *          "Artifact" -> sistema externo / fuente de informacion
 */
var ROLES = [
    { nombre: "Repositor",          tipo: "Actor",    x: 380, y:  60,
      nota: "Persona - Primario - Usa GUI" },
    { nombre: "Proveedor",          tipo: "Actor",    x:  80, y:  60,
      nota: "Persona/Empresa - No es Actor directo - No usa GUI - Fuente de Informacion" },
    { nombre: "BANCO/MP",           tipo: "Artifact", x: 660, y:  60,
      nota: "Sistema - Primario - Fuente de Informacion" },
    { nombre: "Gerente de compras", tipo: "Actor",    x: 380, y: 280,
      nota: "Persona - Primario - Usa GUI" }
];

var ROLES_ENLACES = [
    ["Proveedor", "Repositor"],
    ["Repositor", "BANCO/MP"],
    ["Repositor", "Gerente de compras"],
    ["Gerente de compras", "BANCO/MP"]
];

/*
 *  Diagrama de secuencia de roles: el proceso contado entre personas, sin
 *  clases ni pantallas. Los retornos se rotulan ":Algo", igual que en el DSR
 *  del RFN1.
 */
var DSR = {
    lifelines: [
        { nombre: "Repositor",          tipo: "Actor" },
        { nombre: "Proveedor",          tipo: "Actor" },
        { nombre: "Gerente de compras", tipo: "Actor" },
        { nombre: "BANCO/MP",           tipo: "Artifact" }
    ],
    mensajes: [
        ["Repositor",          "Repositor",          "Detectar insumos bajo el minimo()"],
        ["Repositor",          "Repositor",          "Armar orden de compra()"],
        ["Repositor",          "Proveedor",          "Solicitud de cotizacion()"],
        ["Proveedor",          "Repositor",          ":Costo y condiciones"],
        ["Repositor",          "Gerente de compras", "Pedido de cotizacion()"],
        ["Gerente de compras", "Gerente de compras", "Revisar y comparar ofertas()"],
        ["Gerente de compras", "Repositor",          ":Cotizacion aprobada"],
        ["Gerente de compras", "Repositor",          ":Cotizacion desaprobada (alternativa)"],
        ["Repositor",          "Proveedor",          "Orden de compra()"],
        ["Gerente de compras", "BANCO/MP",           "Pago al proveedor()"],
        ["BANCO/MP",           "Gerente de compras", ":Pago confirmado"],
        ["Proveedor",          "Repositor",          "Entrega de insumos()"],
        ["Repositor",          "Repositor",          "Controlar lo recibido contra lo pedido()"],
        ["Repositor",          "Repositor",          "Actualizar stock()"],
        ["Repositor",          "Gerente de compras", ":Orden finalizada o recibida parcial"],
        ["Proveedor",          "Repositor",          ":Factura de compra"]
    ]
};

/*
 *  Diagrama de actividad con calles (una por rol).
 *  nodos: id, calle, y, texto, clase de elemento.
 *    "accion"    -> Activity (redondeado)
 *    "decision"  -> Decision (rombo)
 *    "inicio" / "fin"
 *    "dato"      -> Object «datastore» (la tabla que se lee o escribe)
 *    "doc"       -> Artifact (comprobante que sale del proceso)
 *    "externo"   -> Artifact (evento que llega de un sistema externo)
 */
var CALLES = [
    { nombre: "PROVEEDOR",          x:   40 },
    { nombre: "REPOSITOR",          x:  400 },
    { nombre: "GERENTE DE COMPRAS", x:  760 },
    { nombre: "BANCO/MP",           x: 1120 }
];
var CALLE_ANCHO = 340;
var CALLE_ALTO  = 1420;

var ACTIVIDAD = [
    { id: "inicio",     calle: 1, y:   50, texto: "INICIO",                                  clase: "inicio"   },
    { id: "detectar",   calle: 1, y:  110, texto: "Detectar insumos bajo el minimo",         clase: "accion"   },
    { id: "dsComp1",    calle: 1, y:  110, texto: "Componentes",                             clase: "dato"     },
    { id: "decHay",     calle: 1, y:  185, texto: "Hay faltantes?",                          clase: "decision" },
    { id: "armar",      calle: 1, y:  255, texto: "Armar la orden de compra",                clase: "accion"   },
    { id: "dsOrden",    calle: 1, y:  255, texto: "OrdenesCompra",                           clase: "dato"     },
    { id: "selProv",    calle: 1, y:  325, texto: "Seleccionar proveedor",                   clase: "accion"   },
    { id: "dsProv",     calle: 1, y:  325, texto: "Proveedores",                             clase: "dato"     },
    { id: "decProv",    calle: 1, y:  400, texto: "Proveedor existente?",                    clase: "decision" },
    { id: "regProv",    calle: 0, y:  400, texto: "Registrar proveedor nuevo",               clase: "accion"   },
    { id: "pedirCot",   calle: 1, y:  470, texto: "Generar pedido de cotizacion",            clase: "accion"   },
    { id: "dsCot",      calle: 1, y:  470, texto: "PedidosCotizacion",                       clase: "dato"     },
    { id: "docPedido",  calle: 0, y:  470, texto: "Pedido de cotizacion",                    clase: "doc"      },
    { id: "cotizar",    calle: 0, y:  545, texto: "Cotizar costo y condiciones",             clase: "accion"   },
    { id: "revisar",    calle: 2, y:  545, texto: "Revisar y comparar las ofertas",          clase: "accion"   },
    { id: "decAprueba", calle: 2, y:  620, texto: "Aprueba?",                                clase: "decision" },
    { id: "aprobar",    calle: 2, y:  690, texto: "Aprobar una oferta y desaprobar el resto", clase: "accion"  },
    { id: "enviarOC",   calle: 1, y:  765, texto: "Enviar la orden al proveedor",            clase: "accion"   },
    { id: "docOC",      calle: 0, y:  765, texto: "Orden de compra",                         clase: "doc"      },
    { id: "pagar",      calle: 3, y:  690, texto: "PAGO AL PROVEEDOR CONFIRMADO",            clase: "externo"  },
    { id: "entregar",   calle: 0, y:  840, texto: "Entregar los insumos",                    clase: "accion"   },
    { id: "docRemito",  calle: 0, y:  915, texto: "Remito",                                  clase: "doc"      },
    { id: "controlar",  calle: 1, y:  915, texto: "Controlar lo recibido contra lo pedido",  clase: "accion"   },
    { id: "decTodo",    calle: 1, y:  990, texto: "Llego todo?",                             clase: "decision" },
    { id: "parcial",    calle: 1, y: 1060, texto: "Dejar la orden Recibida parcial",         clase: "accion"   },
    { id: "cerrar",     calle: 2, y: 1060, texto: "Cerrar la orden (Finalizada)",            clase: "accion"   },
    { id: "stock",      calle: 1, y: 1135, texto: "Actualizar el stock con lo recibido",     clase: "accion"   },
    { id: "dsComp2",    calle: 1, y: 1135, texto: "Componentes",                             clase: "dato"     },
    { id: "docFactura", calle: 0, y: 1210, texto: "Factura de compra",                       clase: "doc"      },
    { id: "fin",        calle: 1, y: 1290, texto: "FIN",                                     clase: "fin"      }
];

var FLUJOS = [
    ["inicio",     "detectar",   ""],
    ["dsComp1",    "detectar",   "lee"],
    ["detectar",   "decHay",     ""],
    ["decHay",     "armar",      "SI"],
    ["decHay",     "fin",        "NO"],
    ["armar",      "dsOrden",    "escribe"],
    ["armar",      "selProv",    ""],
    ["dsProv",     "selProv",    "lee"],
    ["selProv",    "decProv",    ""],
    ["decProv",    "regProv",    "NO"],
    ["decProv",    "pedirCot",   "SI"],
    ["regProv",    "pedirCot",   ""],
    ["pedirCot",   "dsCot",      "escribe"],
    ["pedirCot",   "docPedido",  ""],
    ["docPedido",  "cotizar",    ""],
    ["cotizar",    "revisar",    ""],
    ["revisar",    "decAprueba", ""],
    ["decAprueba", "pedirCot",   "NO: desaprobada"],
    ["decAprueba", "aprobar",    "SI"],
    ["aprobar",    "enviarOC",   ""],
    ["aprobar",    "pagar",      ""],
    ["enviarOC",   "docOC",      ""],
    ["docOC",      "entregar",   ""],
    ["pagar",      "entregar",   ""],
    ["entregar",   "docRemito",  ""],
    ["docRemito",  "controlar",  ""],
    ["controlar",  "decTodo",    ""],
    ["decTodo",    "parcial",    "NO"],
    ["decTodo",    "cerrar",     "SI"],
    ["parcial",    "stock",      ""],
    ["cerrar",     "stock",      ""],
    ["stock",      "dsComp2",    "escribe"],
    ["stock",      "docFactura", ""],
    ["docFactura", "fin",        ""]
];

/*
 *  Modelo conceptual del proceso: las entidades del negocio y cómo se
 *  relacionan, sin capas ni operaciones.
 */
var CONCEPTUAL = {
    entidades: [
        ["Repositor",          ["Dni", "Nombre", "Apellido"],                                  40,  40],
        ["GerenteDeCompras",   ["Dni", "Nombre", "Apellido"],                                 760,  40],
        ["OrdenDeCompra",      ["NumeroCompra", "FechaLimite", "Estado", "FechaCierre"],       400, 190],
        ["DetalleDeOrden",     ["Cantidad"],                                                    40, 390],
        ["Insumo",             ["Codigo", "Descripcion", "Marca", "Modelo",
                                "PrecioUnitario", "Stock", "StockMinimo"],                      40, 560],
        ["PedidoDeCotizacion", ["Numero", "FechaEmision", "Costo", "Condiciones", "Estado"],   760, 390],
        ["Proveedor",          ["Nombre", "Cuit", "Email", "Telefono", "Direccion"],          1120, 390],
        ["FacturaDeCompra",    ["NumeroFactura", "FechaEmision", "FechaEntrega",
                                "Total", "Observaciones"],                                     400, 620],
        ["DetalleDeRecepcion", ["Cantidad"],                                                   400, 830]
    ],
    relaciones: [
        ["Repositor",          "OrdenDeCompra",      "genera",   "1", "0..*"],
        ["OrdenDeCompra",      "DetalleDeOrden",     "pide",     "1", "1..*"],
        ["Insumo",             "DetalleDeOrden",     "",         "1", "0..*"],
        ["OrdenDeCompra",      "PedidoDeCotizacion", "recibe",   "1", "0..*"],
        ["Proveedor",          "PedidoDeCotizacion", "oferta",   "1", "0..*"],
        ["GerenteDeCompras",   "PedidoDeCotizacion", "aprueba",  "1", "0..*"],
        ["OrdenDeCompra",      "FacturaDeCompra",    "se cierra con", "1", "0..*"],
        ["FacturaDeCompra",    "DetalleDeRecepcion", "detalla",  "1", "1..*"],
        ["Insumo",             "DetalleDeRecepcion", "",         "1", "0..*"]
    ]
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

/** Une una lista con saltos de línea, para las notas. */
function lineas(lista) {
    var s = "";
    for (var i = 0; i < lista.length; i++) s += "- " + lista[i] + "\n";
    return s;
}

// ══════════════════════════════════════════════════════════════════════════
//  GENERADORES
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

/** N01.RF2 Diagrama de roles: quiénes intervienen y con quién hablan. */
function generarDiagramaRoles(pkgPre) {
    var dia = nuevoDiagrama(pkgPre, "N01.RF2 Diagrama de roles", "Use Case");
    var mapa = {};

    for (var i = 0; i < ROLES.length; i++) {
        var r = ROLES[i];
        var e = nuevoElemento(pkgPre, r.nombre, r.tipo, "", r.nota);
        var ancho = (r.tipo == "Actor") ? 90 : 130;
        var alto  = (r.tipo == "Actor") ? 110 : 70;
        ponerEnDiagrama(dia, e, r.x, r.y, ancho, alto);
        mapa[r.nombre] = e;
    }

    for (var k = 0; k < ROLES_ENLACES.length; k++) {
        var o = mapa[ROLES_ENLACES[k][0]], d = mapa[ROLES_ENLACES[k][1]];
        if (o != null && d != null) conectar(o, d, "Association", "");
    }

    log("  Diagrama de roles listo (" + ROLES.length + " roles).");
    return dia;
}

/** N01.RF2 Diagrama de secuencia de roles: el proceso entre personas. */
function generarDSR(pkgPre) {
    var dia = nuevoDiagrama(pkgPre, "N01.RF2 Diagrama de secuencia de roles", "Sequence");
    var mapa = {};
    var x = 60;

    for (var i = 0; i < DSR.lifelines.length; i++) {
        var lf = DSR.lifelines[i];
        var e = nuevoElemento(pkgPre, lf.nombre, lf.tipo, "", "");
        ponerEnDiagrama(dia, e, x, 40, LIFELINE_ANCHO, 70);
        mapa[lf.nombre] = e;
        x += LIFELINE_ANCHO + LIFELINE_SEP + 40;
    }

    for (var m = 0; m < DSR.mensajes.length; m++) {
        var o = mapa[DSR.mensajes[m][0]], d = mapa[DSR.mensajes[m][1]];
        if (o == null || d == null) {
            log("    ! mensaje del DSR con rol desconocido: " + DSR.mensajes[m][2]);
            continue;
        }
        mensaje(o, d, DSR.mensajes[m][2], m + 1);
    }

    log("  DSR listo (" + DSR.mensajes.length + " mensajes).");
    return dia;
}

/** N01.RF2 Diagrama de actividad, con una calle por rol. */
function generarActividad(pkgPre) {
    var dia = nuevoDiagrama(pkgPre, "N01.RF2 Diagrama de actividad", "Activity");

    // Las calles: una por rol, como columnas.
    for (var c = 0; c < CALLES.length; c++) {
        var p = nuevoElemento(pkgPre, CALLES[c].nombre, "ActivityPartition", "", "");
        ponerEnDiagrama(dia, p, CALLES[c].x, 20, CALLE_ANCHO, CALLE_ALTO);
    }

    var mapa = {};
    for (var i = 0; i < ACTIVIDAD.length; i++) {
        var n = ACTIVIDAD[i];
        var baseX = CALLES[n.calle].x;
        var tipo, esterotipo = "", ancho = 200, alto = 50, dx = 20;

        if (n.clase == "accion")        { tipo = "Activity"; }
        else if (n.clase == "decision") { tipo = "Decision"; ancho = 110; alto = 60; dx = 65; }
        else if (n.clase == "inicio")   { tipo = "StateNode"; ancho = 34; alto = 34; dx = 100; }
        else if (n.clase == "fin")      { tipo = "StateNode"; ancho = 34; alto = 34; dx = 100; }
        else if (n.clase == "dato")     { tipo = "Object"; esterotipo = "datastore"; ancho = 110; alto = 44; dx = 225; }
        else                            { tipo = "Artifact"; ancho = 170; alto = 50; dx = 30; }

        var e = nuevoElemento(pkgPre, n.texto, tipo, esterotipo, "");

        // Nodo inicial / final: en EA se distinguen por el Subtype.
        if (n.clase == "inicio") { try { e.Subtype = 100; e.Update(); } catch (e1) {} }
        if (n.clase == "fin")    { try { e.Subtype = 101; e.Update(); } catch (e2) {} }

        ponerEnDiagrama(dia, e, baseX + dx, n.y, ancho, alto);
        mapa[n.id] = { elem: e, clase: n.clase };
    }

    var hechos = 0;
    for (var f = 0; f < FLUJOS.length; f++) {
        var fl = FLUJOS[f];
        var o = mapa[fl[0]], d = mapa[fl[1]];
        if (o == null || d == null) {
            log("    ! flujo ignorado: " + fl[0] + " -> " + fl[1]);
            continue;
        }
        // Lo que entra o sale de un almacen de datos va punteado.
        var tipo = (o.clase == "dato" || d.clase == "dato") ? "Dependency"
                 : (o.clase == "doc"  || d.clase == "doc" || o.clase == "externo") ? "ObjectFlow"
                 : "ControlFlow";
        conectar(o.elem, d.elem, tipo, fl[2]);
        hechos++;
    }

    log("  Diagrama de actividad listo (" + ACTIVIDAD.length + " nodos, " + hechos + " flujos).");
    return dia;
}

/** N01.RF2 Diagrama conceptual: entidades del negocio y sus relaciones. */
function generarConceptual(pkgPre) {
    var dia = nuevoDiagrama(pkgPre, "N01.RF2 Diagrama conceptual", "Logical");
    var mapa = {};

    for (var i = 0; i < CONCEPTUAL.entidades.length; i++) {
        var ent = CONCEPTUAL.entidades[i];
        var e = nuevoElemento(pkgPre, ent[0], "Class", "", "Entidad del negocio");
        for (var a = 0; a < ent[1].length; a++) atributo(e, ent[1][a], "");
        e.Update();
        ponerEnDiagrama(dia, e, ent[2], ent[3], 230, 46 + ent[1].length * 15);
        mapa[ent[0]] = e;
    }

    var hechas = 0;
    for (var r = 0; r < CONCEPTUAL.relaciones.length; r++) {
        var rel = CONCEPTUAL.relaciones[r];
        var o = mapa[rel[0]], d = mapa[rel[1]];
        if (o == null || d == null) {
            log("    ! relacion conceptual ignorada: " + rel[0] + " -> " + rel[1]);
            continue;
        }
        var c = o.Connectors.AddNew(rel[2], "Association");
        c.SupplierID = d.ElementID;
        c.ClientEnd.Cardinality = rel[3];
        c.SupplierEnd.Cardinality = rel[4];
        c.Update();
        o.Connectors.Refresh();
        hechas++;
    }

    log("  Diagrama conceptual listo (" + CONCEPTUAL.entidades.length +
        " entidades, " + hechas + " relaciones).");
    return dia;
}

// ══════════════════════════════════════════════════════════════════════════
//  PROGRAMA PRINCIPAL
// ══════════════════════════════════════════════════════════════════════════

function main() {
    try { Repository.EnsureOutputVisible("Script"); } catch (eOut) {}
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - Generando el predisenio del RFN2");
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

    var pkgPre = nuevoPaquete(modelo, nombre);
    log("Paquete: " + nombre);
    log("");

    generarDiagramaRoles(pkgPre);
    generarDSR(pkgPre);
    generarECS(pkgPre);          // Descripcion funcional del proceso
    generarActividad(pkgPre);
    generarConceptual(pkgPre);

    try { Repository.RefreshModelView(0); } catch (eRef) {}

    log("");
    log("=======================================================");
    log(" LISTO. Se generaron 5 diagramas.");
    log(" Buscalos en el Project Browser, dentro de:");
    log("   " + nombre);
    log("=======================================================");
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
        WScript.Echo('  Uso:  cscript //nologo PCFORGE_RFN2_Predisenio_EA_Standalone.js "C:\\ruta\\proyecto.eapx"');
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
