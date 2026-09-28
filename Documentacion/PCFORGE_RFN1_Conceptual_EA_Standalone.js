//
// ===========================================================================
//  PCFORGE - N01.RF1 DIAGRAMA CONCEPTUAL  (version AUTONOMA, sin ventana Scripting)
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
//        cscript //nologo "C:\Users\Thiago\source\repos\IngSoftValdezAlegre\Documentacion\PCFORGE_RFN1_Conceptual_EA_Standalone.js" "C:\Users\Thiago\Documents\PCFORGE.eapx"
//
//   3. Se ve el avance en la consola. Al terminar, abrir el proyecto en EA:
//      el paquete "PCFORGE - N01.RF1 Diagrama conceptual" ya esta en el Project Browser.
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
 *  PCFORGE — N01.RF1 DIAGRAMA CONCEPTUAL (Venta de computadoras)
 *  Trabajo de Diploma — UAI — Valdez / Alegre
 * ===========================================================================
 *
 *  QUÉ HACE
 *  --------
 *  Crea dentro del modelo abierto un paquete
 *  "PCFORGE - N01.RF1 Diagrama conceptual" con un único diagrama: las
 *  entidades del negocio del proceso de venta y cómo se relacionan.
 *
 *  Es el nivel conceptual, no el de diseño: entidades y atributos del
 *  negocio, sin capas, sin operaciones y sin tipos de dato. Por eso acá
 *  figuran "Computadora" o "Pago" y no VentasBLL_96VA ni la tabla Ventas.
 *
 *  Es independiente de los otros scripts: se puede correr solo.
 *
 *  CÓMO SE USA
 *  -----------
 *   1. Abrir el proyecto (.eap / .eapx / .qea) en Enterprise Architect.
 *   2. Menú  Specialize > Tools > Scripting.
 *   3. Si "New JScript" aparece en gris, es porque el grupo seleccionado es
 *      de solo lectura: clic derecho sobre el espacio vacío de la lista >
 *      New Group... (tipo Normal), y recién ahí clic derecho sobre el grupo
 *      nuevo > New JScript.
 *   4. Pegar TODO este archivo dentro del script y guardar (Ctrl+S).
 *   5. Clic derecho sobre el script > Run Script.
 * ===========================================================================
 */

// ══════════════════════════════════════════════════════════════════════════
//  CONFIGURACIÓN
// ══════════════════════════════════════════════════════════════════════════

var NOMBRE_RAIZ = "PCFORGE - N01.RF1 Diagrama conceptual";
var ENTIDAD_ANCHO = 230;

// ══════════════════════════════════════════════════════════════════════════
//  DATOS DEL MODELO
//  entidades:  [nombre, [atributos], x, y]
//  relaciones: [origen, destino, nombre, cardOrigen, cardDestino]
// ══════════════════════════════════════════════════════════════════════════

var CONCEPTUAL = {
    entidades: [
        ["Cliente",              ["Dni", "Nombre", "Apellido", "Telefono", "Direccion"],      40,  40],
        ["Recepcionista",        ["Dni", "Nombre", "Apellido"],                              300,  40],
        ["Computadora",          ["Nombre", "TipoConfiguracion", "PrecioTotal"],             580,  40],
        ["Gerente",              ["Dni", "Nombre", "Apellido"],                              880,  40],

        ["Venta",                ["NumeroVenta", "FechaVenta", "FechaEntregaEstimada",
                                  "Estado"],                                                 300, 220],
        ["DetalleDeComputadora", ["Cantidad"],                                               580, 220],
        ["OrdenDeProduccion",    ["NumeroOrden", "FechaRegistro", "FechaEntregaEstimada",
                                  "Estado", "NumeroSerie", "FechaCierre"],                   880, 220],
        ["LineaDeEnsamblaje",    ["Nombre", "Descripcion", "Disponible"],                   1160, 220],

        ["Componente",           ["Codigo", "Descripcion", "Tipo", "Marca", "Modelo",
                                  "PrecioUnitario", "Stock", "StockMinimo",
                                  "StockReservado"],                                         580, 380],

        ["Pago",                 ["Tipo", "Monto", "FormaPago", "Referencia", "Fecha"],       40, 420],
        ["ControlDeCalidad",     ["Encendido", "Conexiones", "SistemaOperativo", "Drivers",
                                  "Observaciones", "Fecha"],                                  880, 480],
        ["ResponsableTecnico",   ["Dni", "Nombre", "Apellido"],                             1160, 480],

        ["Recibo",               ["Numero", "FechaEmision", "MontoAbonado",
                                  "SaldoPendiente"],                                          40, 620],
        ["FacturaDeVenta",       ["NumeroFactura", "FechaEmision", "Total"],                 300, 620]
    ],

    relaciones: [
        ["Cliente",            "Venta",                "solicita",        "1", "0..*"],
        ["Recepcionista",      "Venta",                "registra",        "1", "0..*"],
        ["Venta",              "Computadora",          "vende",           "1", "1"],
        ["Computadora",        "DetalleDeComputadora", "se arma con",     "1", "1..*"],
        ["Componente",         "DetalleDeComputadora", "",                "1", "0..*"],
        ["Venta",              "Pago",                 "se cobra en",     "1", "1..2"],
        ["Pago",               "Recibo",               "respalda",        "1", "0..1"],
        ["Venta",              "OrdenDeProduccion",    "genera",          "1", "0..1"],
        ["Gerente",            "OrdenDeProduccion",    "planifica",       "1", "0..*"],
        ["LineaDeEnsamblaje",  "OrdenDeProduccion",    "ensambla",        "1", "0..*"],
        ["OrdenDeProduccion",  "ControlDeCalidad",     "se cierra con",   "1", "0..1"],
        ["ResponsableTecnico", "ControlDeCalidad",     "realiza",         "1", "0..*"],
        ["OrdenDeProduccion",  "FacturaDeVenta",       "se factura con",  "1", "0..1"]
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

/** Agrega un atributo publico a una entidad. */
function atributo(elem, nombre) {
    var a = elem.Attributes.AddNew(nombre, "");
    a.Visibility = "Public";
    a.Update();
    elem.Attributes.Refresh();
    return a;
}

/** Asociacion con nombre y cardinalidades en los dos extremos. */
function asociar(origen, destino, nombre, cardOrigen, cardDestino) {
    var c = origen.Connectors.AddNew(nombre == null ? "" : nombre, "Association");
    c.SupplierID = destino.ElementID;
    c.ClientEnd.Cardinality = cardOrigen;
    c.SupplierEnd.Cardinality = cardDestino;
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

// ══════════════════════════════════════════════════════════════════════════
//  GENERADOR
// ══════════════════════════════════════════════════════════════════════════

function generarConceptual(pkg) {
    var dia = nuevoDiagrama(pkg, "N01.RF1 Diagrama conceptual", "Logical");
    var mapa = {};

    for (var i = 0; i < CONCEPTUAL.entidades.length; i++) {
        var ent = CONCEPTUAL.entidades[i];
        var e = nuevoElemento(pkg, ent[0], "Class", "", "Entidad del negocio");
        for (var a = 0; a < ent[1].length; a++) atributo(e, ent[1][a]);
        e.Update();
        ponerEnDiagrama(dia, e, ent[2], ent[3], ENTIDAD_ANCHO, 46 + ent[1].length * 15);
        mapa[ent[0]] = e;
    }

    var hechas = 0;
    for (var r = 0; r < CONCEPTUAL.relaciones.length; r++) {
        var rel = CONCEPTUAL.relaciones[r];
        var o = mapa[rel[0]], d = mapa[rel[1]];
        if (o == null || d == null) {
            log("    ! relacion ignorada: " + rel[0] + " -> " + rel[1]);
            continue;
        }
        asociar(o, d, rel[2], rel[3], rel[4]);
        hechas++;
    }

    log("  Diagrama conceptual listo: " + CONCEPTUAL.entidades.length +
        " entidades, " + hechas + " relaciones.");
    return dia;
}

// ══════════════════════════════════════════════════════════════════════════
//  PROGRAMA PRINCIPAL
// ══════════════════════════════════════════════════════════════════════════

function main() {
    try { Repository.EnsureOutputVisible("Script"); } catch (eOut) {}
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - N01.RF1 Diagrama conceptual");
    Session.Output("=======================================================");

    var modelo = Repository.Models.GetAt(0);
    if (modelo == null) {
        Session.Prompt("No hay ningun modelo abierto en Enterprise Architect.", promptOK);
        return;
    }

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
    log("Paquete: " + nombre);
    log("");

    generarConceptual(raiz);

    try { Repository.RefreshModelView(0); } catch (eRef) {}

    log("");
    log("=======================================================");
    log(" LISTO. Buscalo en el Project Browser, dentro de:");
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
        WScript.Echo('  Uso:  cscript //nologo PCFORGE_RFN1_Conceptual_EA_Standalone.js "C:\\ruta\\proyecto.eapx"');
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
