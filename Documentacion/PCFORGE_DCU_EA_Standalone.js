//
// ===========================================================================
//  PCFORGE - DIAGRAMAS DE CASOS DE USO GENERALES (RFN1 y RFN2)  (version AUTONOMA, sin ventana Scripting)
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
//        cscript //nologo "C:\Users\Thiago\source\repos\IngSoftValdezAlegre\Documentacion\PCFORGE_DCU_EA_Standalone.js" "C:\Users\Thiago\Documents\PCFORGE.eapx"
//
//   3. Se ve el avance en la consola. Al terminar, abrir el proyecto en EA:
//      el paquete "PCFORGE - DCU generales" ya esta en el Project Browser.
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
 *  PCFORGE — DIAGRAMAS DE CASOS DE USO GENERALES (RFN1 y RFN2)
 *  Trabajo de Diploma — UAI — Valdez / Alegre
 * ===========================================================================
 *
 *  QUÉ HACE
 *  --------
 *  Crea dentro del modelo abierto un paquete "PCFORGE - DCU generales" con
 *  los dos diagramas de casos de uso generales:
 *
 *    ├─ N02.RF1 Diagrama de casos de uso general  (CU01 a CU07)
 *    └─ N02.RF2 Diagrama de casos de uso general  (CU08 a CU12)
 *
 *  En cada uno:
 *    · los actores primarios a la izquierda y los secundarios (los que no
 *      usan la GUI, pero son fuente de información) a la derecha;
 *    · los casos de uso en el centro, en el orden del proceso;
 *    · las relaciones «include» y «extend» que las propias ECU declaran, y
 *      las dependencias "requiere" que salen de las precondiciones.
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
 *
 *  NOTA sobre el marco del sistema
 *  -------------------------------
 *  El rectángulo que encierra los casos de uso (el "system boundary") no se
 *  puede crear por automatización: en EA es un objeto de diagrama, no un
 *  elemento del modelo. Si la cátedra lo pide, se arrastra a mano desde el
 *  Toolbox > Use Case > Boundary y se estira alrededor de los casos de uso.
 * ===========================================================================
 */

// ══════════════════════════════════════════════════════════════════════════
//  CONFIGURACIÓN
// ══════════════════════════════════════════════════════════════════════════

var NOMBRE_RAIZ = "PCFORGE - DCU generales";

var ACTOR_ANCHO = 90,  ACTOR_ALTO = 110;
var CU_ANCHO    = 270, CU_ALTO    = 62;
var X_IZQ       = 40,  X_CU       = 330, X_DER = 780;

// ══════════════════════════════════════════════════════════════════════════
//  DATOS DEL MODELO
//  Actores, casos de uso y relaciones de cada requerimiento funcional.
//  Todo es declarativo: para cambiar algo se edita acá y se vuelve a correr.
// ══════════════════════════════════════════════════════════════════════════

var DIAGRAMAS = [

// ─────────────────────────────────────────────────────────── RFN1 ────────
{
    titulo: "N02.RF1 Diagrama de casos de uso general",
    sistema: "PC Factory - RFN1 Venta de computadoras",

    // lado: "izq" = actor primario (usa la GUI)
    //       "der" = fuente de informacion (no usa la GUI)
    actores: [
        { nombre: "Recepcionista",       lado: "izq", y:  90,
          nota: "Persona - Primario - Usa GUI" },
        { nombre: "Gerente",             lado: "izq", y: 330,
          nota: "Persona - Primario - Usa GUI" },
        { nombre: "Responsable tecnico", lado: "izq", y: 520,
          nota: "Persona - Primario - Usa GUI" },
        { nombre: "Cliente",             lado: "der", y:  90,
          nota: "Persona - No es actor directo - No usa GUI - Fuente de Informacion" },
        { nombre: "BANCO/MP",            lado: "der", y: 380,
          nota: "Sistema - Primario - Fuente de Informacion" }
    ],

    casos: [
        { id: "CU01", nombre: "CU01 Registrar venta",                 y:  40 },
        { id: "CU02", nombre: "CU02 Registrar cliente",               y: 130 },
        { id: "CU03", nombre: "CU03 Registrar sena y emitir recibo",  y: 220 },
        { id: "CU04", nombre: "CU04 Gestionar orden de produccion",   y: 310 },
        { id: "CU05", nombre: "CU05 Asignar linea de ensamblaje",     y: 400 },
        { id: "CU06", nombre: "CU06 Cerrar orden de produccion",      y: 490 },
        { id: "CU07", nombre: "CU07 Entregar computadora",            y: 580 }
    ],

    // quién participa de cada caso de uso
    participa: [
        ["Recepcionista",       "CU01"],
        ["Recepcionista",       "CU02"],
        ["Recepcionista",       "CU03"],
        ["Recepcionista",       "CU07"],
        ["Gerente",             "CU04"],
        ["Gerente",             "CU05"],
        ["Responsable tecnico", "CU06"],
        ["Cliente",             "CU01"],
        ["Cliente",             "CU07"],
        ["BANCO/MP",            "CU03"],
        ["BANCO/MP",            "CU07"]
    ],

    // relaciones entre casos de uso: [origen, destino, estereotipo, etiqueta]
    // "include" y "extend" salen de las ECU; "" es una dependencia simple.
    relaciones: [
        ["CU01", "CU03", "include", ""],
        ["CU02", "CU01", "extend",  "el cliente no esta registrado"],
        ["CU04", "CU03", "",        "requiere venta con sena"],
        ["CU05", "CU04", "",        "requiere orden Pendiente"],
        ["CU06", "CU05", "",        "requiere orden En ensamblaje"],
        ["CU07", "CU06", "",        "requiere orden Finalizada"]
    ]
},

// ─────────────────────────────────────────────────────────── RFN2 ────────
{
    titulo: "N02.RF2 Diagrama de casos de uso general",
    sistema: "PC Factory - RFN2 Compra de insumos",

    actores: [
        { nombre: "Repositor",          lado: "izq", y:  90,
          nota: "Persona - Primario - Usa GUI" },
        { nombre: "Gerente de compras", lado: "izq", y: 330,
          nota: "Persona - Primario - Usa GUI" },
        { nombre: "Proveedor",          lado: "der", y:  90,
          nota: "Persona/Empresa - No es actor directo - No usa GUI - Fuente de Informacion" },
        { nombre: "BANCO/MP",           lado: "der", y: 330,
          nota: "Sistema - Primario - Fuente de Informacion" }
    ],

    casos: [
        { id: "CU08", nombre: "CU08 Registrar orden de compra",            y:  40 },
        { id: "CU09", nombre: "CU09 Registrar/seleccionar proveedor",      y: 130 },
        { id: "CU10", nombre: "CU10 Generar solicitud de cotizacion",      y: 220 },
        { id: "CU11", nombre: "CU11 Aprobar solicitud de cotizacion",      y: 310 },
        { id: "CU12", nombre: "CU12 Recibir insumos y cerrar la orden",    y: 400 }
    ],

    participa: [
        ["Repositor",          "CU08"],
        ["Repositor",          "CU09"],
        ["Repositor",          "CU10"],
        ["Repositor",          "CU12"],
        ["Gerente de compras", "CU11"],
        ["Proveedor",          "CU09"],
        ["Proveedor",          "CU10"],
        ["Proveedor",          "CU12"],
        ["BANCO/MP",           "CU11"]
    ],

    relaciones: [
        ["CU09", "CU08", "",       "requiere orden en curso"],
        ["CU10", "CU09", "",       "requiere proveedor asociado"],
        ["CU11", "CU10", "",       "requiere pedido Por aprobar"],
        ["CU12", "CU11", "",       "requiere cotizacion Aprobada"],
        ["CU11", "CU10", "extend", "desaprobada: nuevo pedido"]
    ]
}

];

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
function conectar(origen, destino, tipo, nombre, estereotipo) {
    var c = origen.Connectors.AddNew(nombre == null ? "" : nombre, tipo);
    c.SupplierID = destino.ElementID;
    if (estereotipo != null && estereotipo != "") c.Stereotype = estereotipo;
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

// ══════════════════════════════════════════════════════════════════════════
//  GENERADOR
// ══════════════════════════════════════════════════════════════════════════

/**
 * Arma un diagrama de casos de uso general.
 * Los actores que usan la GUI van a la izquierda; los que sólo aportan
 * información (cliente, proveedor, banco) van a la derecha, que es como
 * se leen los DCU: el sistema en el medio, quién lo opera de un lado y de
 * dónde viene el resto de la información del otro.
 */
function generarDCU(pkgRaiz, d) {
    var pkg = nuevoPaquete(pkgRaiz, d.titulo);
    var dia = nuevoDiagrama(pkg, d.titulo, "Use Case");

    // Nota con el nombre del sistema, arriba del diagrama.
    var titulo = nuevoElemento(pkg, d.sistema, "Note", "", "");
    ponerEnDiagrama(dia, titulo, X_CU, -60, CU_ANCHO, 40);

    var mapa = {};

    for (var a = 0; a < d.actores.length; a++) {
        var ac = d.actores[a];
        var x = (ac.lado == "izq") ? X_IZQ : X_DER;
        var e = nuevoElemento(pkg, ac.nombre, "Actor", "", ac.nota);
        ponerEnDiagrama(dia, e, x, ac.y, ACTOR_ANCHO, ACTOR_ALTO);
        mapa[ac.nombre] = e;
    }

    for (var c = 0; c < d.casos.length; c++) {
        var cu = d.casos[c];
        var u = nuevoElemento(pkg, cu.nombre, "UseCase", "", "");
        ponerEnDiagrama(dia, u, X_CU, cu.y, CU_ANCHO, CU_ALTO);
        mapa[cu.id] = u;
    }

    var asociaciones = 0;
    for (var p = 0; p < d.participa.length; p++) {
        var o = mapa[d.participa[p][0]], t = mapa[d.participa[p][1]];
        if (o == null || t == null) {
            log("    ! participacion ignorada: " + d.participa[p][0] + " - " + d.participa[p][1]);
            continue;
        }
        conectar(o, t, "Association", "", "");
        asociaciones++;
    }

    var relaciones = 0;
    for (var r = 0; r < d.relaciones.length; r++) {
        var rel = d.relaciones[r];
        var ro = mapa[rel[0]], rt = mapa[rel[1]];
        if (ro == null || rt == null) {
            log("    ! relacion ignorada: " + rel[0] + " -> " + rel[1]);
            continue;
        }
        // «include» y «extend» son dependencias estereotipadas, que es como
        // las define UML y como las dibuja EA.
        conectar(ro, rt, "Dependency", rel[3], rel[2]);
        relaciones++;
    }

    log("  " + d.titulo);
    log("    " + d.actores.length + " actores, " + d.casos.length + " casos de uso, " +
        asociaciones + " asociaciones, " + relaciones + " relaciones entre CU.");
    return dia;
}

// ══════════════════════════════════════════════════════════════════════════
//  PROGRAMA PRINCIPAL
// ══════════════════════════════════════════════════════════════════════════

function main() {
    try { Repository.EnsureOutputVisible("Script"); } catch (eOut) {}
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - Diagramas de casos de uso generales");
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

    for (var k = 0; k < DIAGRAMAS.length; k++) generarDCU(raiz, DIAGRAMAS[k]);

    try { Repository.RefreshModelView(0); } catch (eRef) {}

    log("");
    log("=======================================================");
    log(" LISTO. Se generaron " + DIAGRAMAS.length + " diagramas.");
    log(" Buscalos en el Project Browser, dentro de:");
    log("   " + nombre);
    log("=======================================================");
    log("");
    log(" Si la catedra pide el marco del sistema, se arrastra a mano");
    log(" desde el Toolbox > Use Case > Boundary alrededor de los CU.");
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
        WScript.Echo('  Uso:  cscript //nologo PCFORGE_DCU_EA_Standalone.js "C:\\ruta\\proyecto.eapx"');
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
