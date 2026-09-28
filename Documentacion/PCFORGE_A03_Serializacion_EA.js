!INC Local Scripts.EAConstants-JScript

/*
 * ===========================================================================
 *  PCFORGE - A03 SERIALIZACION (documentacion principal)
 *  Script para Sparx Enterprise Architect (ventana Scripting, JScript)
 * ===========================================================================
 *
 *  QUE HACE
 *  --------
 *  Crea dentro del modelo abierto el paquete "PCFORGE - A03 Serializacion":
 *
 *    +- 00 Modelo de datos a serializar   (clases, fundamento y XML de ejemplo)
 *    +- 01 Descomposicion funcional        (los 8 pasos con sus datos)
 *    +- 02 DCU Serializacion
 *    +- 03 CU-SER01 Serializar objetos      (pasos 1 a 4)
 *    +- 04 CU-SER02 Des-serializar objetos  (pasos 5 a 8)
 *
 *  Y dentro de cada caso de uso, sus 5 diagramas:
 *    . ECU  especificacion (tambien en las notas del caso de uso)
 *    . DS   secuencia del sistema (UI, BLL, MPP, DAL, SER, bitacora)
 *    . DC   clases del caso de uso, por capas
 *    . DER  tablas del caso de uso, en notacion de Martin
 *    . GUI  boceto de la pantalla
 *
 *  CONVENCIONES (las del resto del documento de PCFORGE)
 *  ------------
 *    . Clases con el sufijo 96VA y estereotipos <<UI>>, <<BLL>>, <<MPP>>,
 *      <<DAL>>, <<SER>> y <<BE>>. Los ID de los ECU con 06AV.
 *    . Tablas y columnas con los nombres reales de la base (99_full_install).
 *    . Los DER quedan en notacion de Martin (Information Engineering).
 *    . Los fragmentos alt/loop de las secuencias se dibujan a mano: EA no
 *      los ubica bien por automatizacion.
 *
 *  COMO SE USA
 *  -----------
 *   1. Abrir el proyecto en EA.
 *   2. Specialize > Tools > Scripting (o View > Scripting en EA viejos).
 *   3. En un grupo propio (Normal): click derecho > New JScript, pegar
 *      este archivo completo y guardar.
 *   4. Click derecho sobre el script > Run. El avance sale en la pestana
 *      "Script" de la ventana Output.
 *
 *  Si "New JScript" aparece en gris, usar la version _Standalone.js, que
 *  corre desde la consola de Windows sin la ventana Scripting.
 *
 *  Si se corre dos veces, crea un paquete nuevo con sufijo " (2)": nunca
 *  pisa lo anterior.
 * ===========================================================================
 */

// ══════════════════════════════════════════════════════════════════════════
//  HELPERS DE LA API DE EA
// ══════════════════════════════════════════════════════════════════════════

function log(msg) { Session.Output(msg); }

function trim(s) { return String(s).replace(/^\s+/, "").replace(/\s+$/, ""); }

function nuevoPaquete(padre, nombre) {
    var p = padre.Packages.AddNew(nombre, "Package");
    p.Update();
    padre.Packages.Refresh();
    return p;
}

function nuevoDiagrama(pkg, nombre, tipo) {
    var d = pkg.Diagrams.AddNew(nombre, tipo);
    d.Update();
    pkg.Diagrams.Refresh();
    return d;
}

function nuevoElemento(pkg, nombre, tipo, estereotipo, notas) {
    var e = pkg.Elements.AddNew(nombre, tipo);
    if (estereotipo != null && estereotipo != "") e.Stereotype = estereotipo;
    if (notas != null && notas != "") e.Notes = notas;
    e.Update();
    pkg.Elements.Refresh();
    return e;
}

/** En EA el eje vertical es NEGATIVO hacia abajo: top = -y, bottom = -(y+alto). */
function ponerEnDiagrama(dia, elem, x, y, ancho, alto) {
    var geo = "l=" + x + ";r=" + (x + ancho) + ";t=" + (-y) + ";b=" + (-(y + alto)) + ";";
    var o = dia.DiagramObjects.AddNew(geo, "");
    o.ElementID = elem.ElementID;
    o.Update();
    dia.DiagramObjects.Refresh();
    return o;
}

function altoCaja(filas) { return 46 + Math.max(1, filas) * 15; }

function visibilidad(v) {
    if (v == "-") return "Private";
    if (v == "#") return "Protected";
    return "Public";
}

function atributo(elem, vis, nombre, tipo, estatico) {
    var a = elem.Attributes.AddNew(nombre, tipo == null ? "" : tipo);
    a.Visibility = visibilidad(vis);
    if (estatico) { try { a.IsStatic = true; } catch (eS) {} }
    a.Update();
    elem.Attributes.Refresh();
    return a;
}

/** Corta por comas de nivel 0 (no las de List<A, B>). */
function partirParametros(params) {
    var partes = [], nivel = 0, actual = "";
    for (var i = 0; i < params.length; i++) {
        var ch = params.charAt(i);
        if (ch == "<") nivel++;
        if (ch == ">") nivel--;
        if (ch == "," && nivel == 0) { partes.push(actual); actual = ""; }
        else actual += ch;
    }
    if (trim(actual) != "") partes.push(actual);
    return partes;
}

/** Operación a partir de su firma: "Nombre(p : Tipo, q : Tipo) : Retorno". */
function operacion(elem, vis, firma, estatico) {
    var iAbre = firma.indexOf("("), iCierra = firma.lastIndexOf(")");
    var nombre = trim(firma.substring(0, iAbre));
    var params = firma.substring(iAbre + 1, iCierra);
    var cola = firma.substring(iCierra + 1);
    var retorno = "";
    var iDosP = cola.indexOf(":");
    if (iDosP >= 0) retorno = trim(cola.substring(iDosP + 1));

    var m = elem.Methods.AddNew(nombre, retorno);
    m.Visibility = visibilidad(vis);
    if (estatico) { try { m.IsStatic = true; } catch (eS) {} }
    m.Update();
    elem.Methods.Refresh();

    var lista = partirParametros(params);
    for (var k = 0; k < lista.length; k++) {
        var nt = trim(lista[k]).split(" : ");
        var par = m.Parameters.AddNew(trim(nt[0]), nt.length > 1 ? trim(nt[1]) : "");
        par.Position = k;
        par.Update();
    }
    if (lista.length > 0) { m.Parameters.Refresh(); m.Update(); }
    return m;
}

/** Conector simple: dependencias, generalizaciones, asociaciones sin cardinalidad. */
function conectar(origen, destino, tipo, nombre, estereotipo) {
    var c = origen.Connectors.AddNew(nombre == null ? "" : nombre, tipo);
    c.SupplierID = destino.ElementID;
    if (estereotipo != null && estereotipo != "") c.Stereotype = estereotipo;
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/**
 * Relación entre clases con cardinalidades.
 *   tipo: "asociacion" | "composicion" | "agregacion" | "dependencia"
 * En composición y agregación el ORIGEN es la parte y el DESTINO el todo:
 * el rombo queda del lado del destino.
 */
function relacionar(origen, destino, tipo, cardOrigen, cardDestino, nombre) {
    if (tipo == "dependencia") return conectar(origen, destino, "Dependency", nombre, "");

    var todoParte = (tipo == "composicion" || tipo == "agregacion");
    var c = origen.Connectors.AddNew(nombre == null ? "" : nombre, todoParte ? "Aggregation" : "Association");
    c.SupplierID = destino.ElementID;
    if (cardOrigen)  c.ClientEnd.Cardinality = cardOrigen;
    if (cardDestino) c.SupplierEnd.Cardinality = cardDestino;
    if (todoParte) {
        c.Subtype = (tipo == "composicion") ? "Strong" : "Weak";
        c.SupplierEnd.Aggregation = (tipo == "composicion") ? 2 : 1;
    }
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/** Mensaje de un diagrama de secuencia, con su número de orden. */
function mensaje(origen, destino, texto, nro) {
    var c = origen.Connectors.AddNew(texto, "Sequence");
    c.SupplierID = destino.ElementID;
    c.SequenceNo = nro;
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/** Une una lista con saltos de línea, para las notas. */
function lineas(lista) {
    var s = "";
    for (var i = 0; i < lista.length; i++) s += "-" + lista[i] + "\n";
    return s;
}

/**
 * Marco del sistema de un diagrama de casos de uso ("System Boundary").
 * Si la versión de EA no lo permite por automatización, se sigue sin él.
 */
function marcoSistema(pkg, dia, nombre, x, y, ancho, alto) {
    try {
        var b = nuevoElemento(pkg, nombre, "Boundary", "", "");
        ponerEnDiagrama(dia, b, x, y, ancho, alto);
        return b;
    } catch (eB) {
        log("    (no se pudo crear el marco del sistema: agregarlo a mano desde el Toolbox)");
        return null;
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  GENERADORES
// ══════════════════════════════════════════════════════════════════════════

/** Tipo de elemento de EA para una línea de vida. */
function crearLineaDeVida(pkg, nombre, clase) {
    if (clase == "actor")    return nuevoElemento(pkg, nombre, "Actor", "", "");
    // La pantalla va como línea de vida con estereotipo «boundary»: EA la
    // dibuja con el círculo, igual que el FormLogin de los diagramas del TP.
    if (clase == "boundary") return nuevoElemento(pkg, nombre, "Sequence", "boundary", "");
    if (clase == "externo")  return nuevoElemento(pkg, nombre, "Sequence", "", "");
    return nuevoElemento(pkg, nombre, "Sequence", "", "");
}

/** Diagrama de secuencia (del sistema o de roles). */
function generarSecuencia(pkg, titulo, lifelines, mensajes, existentes) {
    var dia = nuevoDiagrama(pkg, titulo, "Sequence");
    var mapa = {};
    var x = 40;
    for (var i = 0; i < lifelines.length; i++) {
        var nombre = lifelines[i][0], clase = lifelines[i][1];
        if (mapa[nombre] != null) continue;
        var e = (existentes != null && existentes[nombre] != null)
              ? existentes[nombre] : crearLineaDeVida(pkg, nombre, clase);
        var ancho = (clase == "actor") ? 90 : Math.max(130, nombre.length * 7 + 20);
        ponerEnDiagrama(dia, e, x, 40, ancho, 60);
        mapa[nombre] = e;
        x += ancho + 50;
    }
    var hechos = 0;
    for (var m = 0; m < mensajes.length; m++) {
        var o = mapa[mensajes[m][0]], d = mapa[mensajes[m][1]];
        if (o == null || d == null) {
            log("    ! mensaje con linea de vida desconocida: " + mensajes[m][2]);
            continue;
        }
        mensaje(o, d, mensajes[m][2], m + 1);
        hechos++;
    }
    return hechos;
}

/**
 * Diagrama de clases a partir del catálogo CLASES.
 *   ubicaciones: [nombre, x, y, filtro de operaciones (null = todas)]
 *   relaciones:  [origen, destino, tipo, cardOrigen, cardDestino]
 */
function generarClases(pkg, titulo, ubicaciones, relaciones) {
    var dia = nuevoDiagrama(pkg, titulo, "Logical");
    var mapa = {};
    var miembros = 0;

    for (var i = 0; i < ubicaciones.length; i++) {
        var u = ubicaciones[i];
        var def = CLASES[u[0]];
        if (def == null) { log("    ! clase sin definir: " + u[0]); continue; }

        var e = nuevoElemento(pkg, u[0], def.tipo ? def.tipo : "Class", def.ester ? def.ester : "", "");
        var filas = 0;
        for (var a = 0; a < def.at.length; a++) {
            atributo(e, def.at[a][0], def.at[a][1], def.at[a][2], def.at[a][3]);
            filas++;
        }
        for (var o = 0; o < def.op.length; o++) {
            var op = def.op[o];
            var nom = trim(op[1].substring(0, op[1].indexOf("(")));
            if (u[3] != null && !contiene(u[3], nom)) continue;
            operacion(e, op[0], op[1], op[2]);
            filas++;
        }
        e.Update();
        miembros += filas;

        var ancho = def.ancho ? def.ancho : 300;
        ponerEnDiagrama(dia, e, u[1], u[2], ancho, altoCaja(filas));
        mapa[u[0]] = e;
    }

    var hechas = 0;
    for (var r = 0; r < relaciones.length; r++) {
        var x = relaciones[r];
        if (mapa[x[0]] == null || mapa[x[1]] == null) {
            log("    ! relacion ignorada: " + x[0] + " -> " + x[1]);
            continue;
        }
        relacionar(mapa[x[0]], mapa[x[1]], x[2], x[3], x[4], x.length > 5 ? x[5] : "");
        hechas++;
    }
    return { miembros: miembros, relaciones: hechas, diagrama: dia };
}

function contiene(lista, valor) {
    for (var i = 0; i < lista.length; i++) if (lista[i] == valor) return true;
    return false;
}

/**
 * Tabla de base de datos al estilo del modelado de datos de EA:
 * columnas «column», clave primaria como operación «PK» y claves foráneas
 * como operaciones «FK», que es como EA las guarda y las dibuja.
 */
function crearTabla(pkg, nombre) {
    var def = TABLAS[nombre];
    var t = nuevoElemento(pkg, nombre, "Class", "table", "");
    try { t.Gentype = "SQL Server 2012"; t.Update(); } catch (eG) {}

    var pks = [];
    for (var i = 0; i < def.cols.length; i++) {
        var c = def.cols[i];            // [nombre, tipo, largo, notNull, esPK]
        var a = t.Attributes.AddNew(c[0], c[1]);
        a.Stereotype = "column";
        a.Visibility = "Public";
        if (c[2] != null && c[2] != "") {
            var partes = String(c[2]).split(",");
            if (partes.length == 2) {
                try { a.Precision = partes[0]; a.Scale = partes[1]; } catch (eP) {}
            } else {
                try { a.Length = partes[0]; } catch (eL) {}
            }
        }
        // En las columnas, EA usa AllowDuplicates como "NOT NULL".
        try { a.AllowDuplicates = c[3] ? true : false; } catch (eN) {}
        a.Update();
        if (c[4]) pks.push(c);
    }
    t.Attributes.Refresh();

    // Una sola operacion «PK» con todas las columnas de la clave (PK compuesta).
    if (pks.length > 0) {
        var m = t.Methods.AddNew("PK_" + nombre, "");
        m.Stereotype = "PK";
        m.Visibility = "Public";
        m.Update();
        for (var p = 0; p < pks.length; p++) {
            var par = m.Parameters.AddNew(pks[p][0], pks[p][1]);
            par.Position = p;
            par.Update();
        }
        m.Parameters.Refresh();
        t.Methods.Refresh();
    }

    for (var f = 0; f < def.fks.length; f++) {
        var fk = def.fks[f];            // [nombreFK, columna, tablaDestino]
        var colTipo = "int";
        for (var k = 0; k < def.cols.length; k++) if (def.cols[k][0] == fk[1]) colTipo = def.cols[k][1];
        var mf = t.Methods.AddNew(fk[0], "");
        mf.Stereotype = "FK";
        mf.Visibility = "Public";
        mf.Update();
        var pf = mf.Parameters.AddNew(fk[1], colTipo);
        pf.Update();
        mf.Parameters.Refresh();
    }
    t.Methods.Refresh();
    t.Update();
    return t;
}

/**
 * Notacion de Martin (Information Engineering, "pata de gallo") en los
 * conectores del diagrama. EA la guarda en el estilo del diagrama como
 * TConnectorNotation; si la version no deja cambiarla por automatizacion,
 * se avisa para hacerlo a mano.
 */
function notacionMartin(dia) {
    var valor = "TConnectorNotation=Information Engineering;";
    try {
        var s = dia.StyleEx == null ? "" : String(dia.StyleEx);
        if (s.indexOf("TConnectorNotation=") >= 0) s = s.replace(/TConnectorNotation=[^;]*;/, valor);
        else s = s + (s == "" || s.charAt(s.length - 1) == ";" ? "" : ";") + valor;
        dia.StyleEx = s;
        dia.Update();
        return true;
    } catch (eN) {
        log("    ! no se pudo poner la notacion de Martin: en el diagrama, Properties >");
        log("      Connectors > Connector Notation = Information Engineering");
        return false;
    }
}

/** DER: tablas del caso de uso y sus claves foráneas, en notacion de Martin. */
function generarDER(pkg, titulo, tablas) {
    var dia = nuevoDiagrama(pkg, titulo, "Logical");
    notacionMartin(dia);
    var mapa = {};
    for (var i = 0; i < tablas.length; i++) {
        var nombre = tablas[i][0];
        var t = crearTabla(pkg, nombre);
        var def = TABLAS[nombre];
        var filas = def.cols.length + 2 + (def.fks.length > 0 ? def.fks.length + 1 : 0);
        ponerEnDiagrama(dia, t, tablas[i][1], tablas[i][2], 320, altoCaja(filas) + 20);
        mapa[nombre] = t;
    }

    var fks = 0;
    for (var n in mapa) {
        var def2 = TABLAS[n];
        for (var f = 0; f < def2.fks.length; f++) {
            var fk = def2.fks[f];
            var destino = mapa[fk[2]];
            if (destino == null) continue;          // la tabla referida no está en este DER
            var c = mapa[n].Connectors.AddNew("", "Association");
            c.SupplierID = destino.ElementID;
            c.Stereotype = "FK";
            c.ClientEnd.Role = fk[0];
            c.SupplierEnd.Role = "PK_" + fk[2];
            // [nombreFK, columna, tabla, cardinalidad hija, cardinalidad padre]
            c.ClientEnd.Cardinality = fk.length > 3 && fk[3] ? fk[3] : "0..*";
            c.SupplierEnd.Cardinality = fk.length > 4 && fk[4] ? fk[4] : "1";
            try { c.StyleEx = "FKINFO=SRC=" + fk[0] + ":DST=PK_" + fk[2] + ":;"; } catch (eS) {}
            c.Update();
            mapa[n].Connectors.Refresh();
            fks++;
        }
    }
    return fks;
}

/** Diagrama de casos de uso de UN caso de uso (como los del TP). */
function generarDCUdeCU(pkg, cu) {
    var dia = nuevoDiagrama(pkg, "Diagrama CU " + cu.nombreCompleto, "Use Case");
    marcoSistema(pkg, dia, SISTEMA, 260, 20, 380, 360);
    var actor = nuevoElemento(pkg, cu.actor, "Actor", "", "");
    ponerEnDiagrama(dia, actor, 20, 130, 80, 110);
    var uc = nuevoElemento(pkg, cu.nombreCompleto, "UseCase", "", cu.objetivo ? cu.objetivo : "");
    ponerEnDiagrama(dia, uc, 360, 150, 180, 80);
    conectar(actor, uc, "Association", "", "");
}

/** Diagramas completos de un caso de uso: CU, secuencia, clases y DER. */
function generarCasoDeUso(pkgPadre, cu) {
    var pkg = nuevoPaquete(pkgPadre, cu.nombreCompleto);
    log(cu.nombreCompleto);

    generarDCUdeCU(pkg, cu);

    var msj = generarSecuencia(pkg, "Diagrama de secuencia de sistema " + cu.nombreCompleto,
                               cu.lifelines, cu.mensajes);
    log("  DS  listo (" + msj + " mensajes)");

    var dc = generarClases(pkg, "Diagrama de clases " + cu.nombreCompleto, cu.clases, cu.relaciones);
    log("  DC  listo (" + dc.miembros + " miembros, " + dc.relaciones + " relaciones)");

    var fks = generarDER(pkg, "Diagrama entidad-relacion " + cu.nombreCompleto, cu.tablas);
    log("  DER listo (" + cu.tablas.length + " tablas, " + fks + " claves foraneas)");
    log("");
}

/** Raíz con nombre único, para poder correr el script varias veces. */
function paqueteRaiz(nombreBase) {
    var modelo = Repository.Models.GetAt(0);
    if (modelo == null) {
        Session.Prompt("No hay ningun modelo abierto en Enterprise Architect.", promptOK);
        return null;
    }
    var nombre = nombreBase, intento = 1, existe = true;
    while (existe) {
        existe = false;
        for (var i = 0; i < modelo.Packages.Count; i++)
            if (modelo.Packages.GetAt(i).Name == nombre) existe = true;
        if (existe) { intento++; nombre = nombreBase + " (" + intento + ")"; }
    }
    var raiz = nuevoPaquete(modelo, nombre);
    log("Paquete: " + nombre);
    log("");
    return raiz;
}


// ══════════════════════════════════════════════════════════════════════════
//  GENERADORES PROPIOS DE LA DOCUMENTACION A03
// ══════════════════════════════════════════════════════════════════════════

/** Une lineas con CRLF, que es el salto que EA usa en las notas. */
function texto(lista) {
    var s = "";
    for (var i = 0; i < lista.length; i++) s += (i > 0 ? "\r\n" : "") + lista[i];
    return s;
}

/** Nota suelta en un diagrama; si se pasa "a", queda unida a ese elemento. */
function nota(pkg, dia, contenido, x, y, ancho, alto, a) {
    var n = nuevoElemento(pkg, "", "Note", "", contenido);
    ponerEnDiagrama(dia, n, x, y, ancho, alto);
    if (a != null) {
        try { conectar(n, a, "NoteLink", "", ""); } catch (eL) {}
    }
    return n;
}

/**
 * Diagrama de casos de uso general.
 *   d.actores:    [nombre, x, y]
 *   d.casos:      [nombre, x, y, notas]
 *   d.relaciones: [origen, destino, "asociacion" | "include" | "extend"]
 * Devuelve el mapa nombre -> elemento, para reutilizar actor y casos de uso
 * en los demas diagramas.
 */
function generarDCU(pkg, d) {
    var dia = nuevoDiagrama(pkg, d.titulo, "Use Case");
    marcoSistema(pkg, dia, d.sistema, d.marco[0], d.marco[1], d.marco[2], d.marco[3]);
    var mapa = {};
    for (var a = 0; a < d.actores.length; a++) {
        var e = nuevoElemento(pkg, d.actores[a][0], "Actor", "", "");
        ponerEnDiagrama(dia, e, d.actores[a][1], d.actores[a][2], 80, 110);
        mapa[d.actores[a][0]] = e;
    }
    for (var c = 0; c < d.casos.length; c++) {
        var u = nuevoElemento(pkg, d.casos[c][0], "UseCase", "", d.casos[c][3]);
        ponerEnDiagrama(dia, u, d.casos[c][1], d.casos[c][2], 230, 80);
        mapa[d.casos[c][0]] = u;
    }
    for (var r = 0; r < d.relaciones.length; r++) {
        var x = d.relaciones[r];
        if (x[2] == "asociacion") conectar(mapa[x[0]], mapa[x[1]], "Association", "", "");
        else conectar(mapa[x[0]], mapa[x[1]], "UseCase", "", x[2]);
    }
    log("  " + d.titulo + " (" + d.casos.length + " casos de uso)");
    return { diagrama: dia, elementos: mapa };
}

/**
 * Especificacion del caso de uso: el caso de uso (ya creado en el DCU) con
 * la especificacion completa en una nota. El mismo texto queda en las notas
 * del caso de uso, que es lo que toma el generador de documentos de EA.
 */
function generarECU(pkg, titulo, casoDeUso, actor, especificacion) {
    var dia = nuevoDiagrama(pkg, titulo, "Use Case");
    ponerEnDiagrama(dia, actor, 30, 60, 80, 110);
    ponerEnDiagrama(dia, casoDeUso, 180, 75, 230, 80);
    var lineasTexto = especificacion.split("\r\n").length;
    nota(pkg, dia, especificacion, 460, 20, 620, 40 + lineasTexto * 15, casoDeUso);
    log("  " + titulo);
}

/**
 * Diagrama de actividad con una calle por participante.
 *   calles: [nombre, x]
 *   nodos:  [id, calle, y, texto, clase, dx opcional]
 *           clase: inicio | fin | accion | decision | dato
 *   flujos: [origen, destino, guarda]
 *   notas:  [id del nodo, x, y, ancho, alto, texto]
 */
function generarActividad(pkg, titulo, calles, anchoCalle, altoCalle, nodos, flujos, notas) {
    var dia = nuevoDiagrama(pkg, titulo, "Activity");
    for (var c = 0; c < calles.length; c++) {
        var p = nuevoElemento(pkg, calles[c][0], "ActivityPartition", "", "");
        ponerEnDiagrama(dia, p, calles[c][1], 10, anchoCalle, altoCalle);
    }
    var mapa = {};
    for (var i = 0; i < nodos.length; i++) {
        var n = nodos[i];
        var bx = calles[n[1]][1];
        var tipo = "Action", est = "", ancho = 230, alto = 55, dx = 20;
        if (n[4] == "decision") { tipo = "Decision"; ancho = 50; alto = 50; dx = 110; }
        else if (n[4] == "inicio" || n[4] == "fin") { tipo = "StateNode"; ancho = 30; alto = 30; dx = 120; }
        else if (n[4] == "dato") { tipo = "Object"; est = "datastore"; ancho = 150; alto = 55; dx = 20; }
        if (n.length > 5 && n[5] != null) dx = n[5];

        var e = nuevoElemento(pkg, n[3], tipo, est, "");
        if (n[4] == "inicio") { try { e.Subtype = 100; e.Update(); } catch (e1) {} }
        if (n[4] == "fin")    { try { e.Subtype = 101; e.Update(); } catch (e2) {} }
        ponerEnDiagrama(dia, e, bx + dx, n[2], ancho, alto);
        mapa[n[0]] = { e: e, clase: n[4] };
    }
    var hechos = 0;
    for (var f = 0; f < flujos.length; f++) {
        var o = mapa[flujos[f][0]], d = mapa[flujos[f][1]];
        if (o == null || d == null) { log("    ! flujo ignorado: " + flujos[f][0] + " -> " + flujos[f][1]); continue; }
        var t = (o.clase == "dato" || d.clase == "dato") ? "ObjectFlow" : "ControlFlow";
        var con = conectar(o.e, d.e, t, "", "");
        if (flujos[f][2] != "") { try { con.TransitionGuard = flujos[f][2]; con.Update(); } catch (eG) {} }
        hechos++;
    }
    for (var k = 0; k < notas.length; k++) {
        var x = notas[k];
        nota(pkg, dia, x[5], x[1], x[2], x[3], x[4], mapa[x[0]] == null ? null : mapa[x[0]].e);
    }
    log("  " + titulo + " (" + nodos.length + " nodos, " + hechos + " flujos, " + notas.length + " notas)");
    return dia;
}

/**
 * Boceto de pantalla (GUI) con los elementos de interfaz de EA: una
 * "Screen" y adentro sus controles ("GUIElement"), cada uno con su
 * estereotipo (button, textbox, combobox, list, label).
 *   pantalla.controles: [texto, estereotipo, x, y, ancho, alto]  (relativos a la pantalla)
 * Si la version de EA no trae estos elementos, se avisa y se sigue.
 */
function generarGUI(pkg, titulo, pantalla) {
    var dia = nuevoDiagrama(pkg, titulo, "Custom");
    var x0 = 30, y0 = 30;
    try {
        var s = nuevoElemento(pkg, pantalla.nombre, "Screen", "", pantalla.notas);
        ponerEnDiagrama(dia, s, x0, y0, pantalla.ancho, pantalla.alto);
        for (var i = 0; i < pantalla.controles.length; i++) {
            var c = pantalla.controles[i];
            var g = s.Elements.AddNew(c[0], "GUIElement");
            g.Stereotype = c[1];
            g.Update();
            s.Elements.Refresh();
            ponerEnDiagrama(dia, g, x0 + c[2], y0 + c[3], c[4], c[5]);
        }
        log("  " + titulo + " (" + pantalla.controles.length + " controles)");
    } catch (eS) {
        log("    ! no se pudo dibujar la pantalla con elementos de interfaz de EA;");
        log("      se deja la descripcion en una nota: " + titulo);
        nota(pkg, dia, pantalla.nombre + "\r\n\r\n" + pantalla.notas, x0, y0, 520, 300, null);
    }
    if (pantalla.leyenda) nota(pkg, dia, pantalla.leyenda, x0 + pantalla.ancho + 30, y0, 330, pantalla.altoLeyenda, null);
    return dia;
}


// ══════════════════════════════════════════════════════════════════════════
//  CATALOGO DE CLASES (PCFORGE, sufijo 96VA como en el resto del documento)
//    at: [visibilidad, nombre, tipo, estatico]
//    op: [visibilidad, firma, estatica]
//  Las clases que ya existen llevan solo los metodos que usa la serializacion.
// ══════════════════════════════════════════════════════════════════════════
var CLASES = {

    // ── Interfaz ──────────────────────────────────────────────────────────
    "SerializacionControl96VA": { ester: "UI", ancho: 440,
        at: [["-", "_bll", "SerializacionBLL96VA", false],
             ["-", "_filasObjetos", "BindingList<ObjetoVm>", false],
             ["-", "_serializados", "List<object>", false],
             ["-", "_paquete", "PaqueteSerializado96VA", false]],
        op: [["-", "SeleccionarClase() : void", false],
             ["-", "SeleccionarDestino() : void", false],
             ["-", "Serializar() : void", false],
             ["-", "VerificarSerializacion() : void", false],
             ["-", "SeleccionarOrigen() : void", false],
             ["-", "CargarArchivos() : void", false],
             ["-", "SeleccionarArchivo() : void", false],
             ["-", "Deserializar() : void", false],
             ["-", "VerificarDeserializacion() : void", false],
             ["-", "MostrarObjetos(objetos : List<object>) : void", false],
             ["-", "MostrarInformeSer() : void", false],
             ["-", "MostrarInformeDes() : void", false],
             ["-", "ActualizarPasos() : void", false]] },

    // ── Logica de negocio ────────────────────────────────────────────────
    "SerializacionBLL96VA": { ester: "BLL", ancho: 860,
        at: [["+", "Extension", "string", true],
             ["+", "VersionFormato", "int", true],
             ["-", "SalHash", "string", true],
             ["-", "Serializador", "SerializadorXmlSER96VA", true],
             ["-", "Encriptador", "EncriptacionSER96VA", true],
             ["-", "_ventas", "VentasBLL96VA", false]],
        op: [["+", "ObtenerObjetos(clase : ClaseSerializable96VA) : List<object>", false],
             ["+", "ProponerNombreArchivo(clase : ClaseSerializable96VA, ahora : DateTime) : string", true],
             ["+", "ValidarDestino(ruta : string) : void", true],
             ["+", "Serializar(clase : ClaseSerializable96VA, objetos : List<object>, ruta : string) : PaqueteSerializado96VA", false],
             ["+", "ArmarPaquete(clase : ClaseSerializable96VA, objetos : IList<object>, generadoPor : string, ahora : DateTime) : PaqueteSerializado96VA", true],
             ["+", "ValidarObjetos(clase : ClaseSerializable96VA, objetos : IList<object>) : void", true],
             ["+", "VerificarSerializacion(ruta : string, originales : IList<object>) : InformeVerificacion96VA", false],
             ["+", "VerificarContraOriginales(ruta : string, originales : IList<object>) : InformeVerificacion96VA", true],
             ["+", "ListarArchivos(carpeta : string) : List<string>", false],
             ["+", "LeerContenido(ruta : string) : string", false],
             ["+", "Deserializar(ruta : string) : PaqueteSerializado96VA", false],
             ["+", "LeerPaquete(ruta : string) : PaqueteSerializado96VA", true],
             ["+", "VerificarDeserializacion(paquete : PaqueteSerializado96VA, ruta : string) : InformeVerificacion96VA", false],
             ["+", "VerificarContraBase(paquete : PaqueteSerializado96VA, ruta : string, buscarActual : Func<string, object>) : InformeVerificacion96VA", true],
             ["+", "CalcularHash(paquete : PaqueteSerializado96VA) : string", true],
             ["+", "EsIntegro(paquete : PaqueteSerializado96VA) : bool", true],
             ["+", "ClaveDe(objeto : object) : string", true],
             ["+", "Comparar(esperado : object, obtenido : object) : ResultadoVerificacion96VA", true],
             ["-", "CrearBuscador(clase : ClaseSerializable96VA) : Func<string, object>", false],
             ["-", "Registrar(categoria : CategoriaBitacora96VA, criticidad : CriticidadBitacora96VA, descripcion : string) : void", true],
             ["-", "ExigirPermiso() : void", true]] },

    "VentasBLL96VA": { ester: "BLL", ancho: 330, at: [],
        op: [["+", "ObtenerTodas() : List<Venta96VA>", false],
             ["+", "ObtenerPorNumero(numeroVenta : int) : Venta96VA", false]] },

    "ClientesBLL96VA": { ester: "BLL", ancho: 300, at: [],
        op: [["+", "ObtenerTodos() : List<Cliente96VA>", false],
             ["+", "ObtenerPorDni(dni : string) : Cliente96VA", false]] },

    "ModelosEstandarBLL96VA": { ester: "BLL", ancho: 330, at: [],
        op: [["+", "ObtenerTodos() : List<ModeloEstandar96VA>", false]] },

    "BitacoraBLL96VA": { ester: "BLL", ancho: 900, at: [],
        op: [["+", "Registrar(categoria : CategoriaBitacora96VA, criticidad : CriticidadBitacora96VA, descripcion : string, modulo : ModuloBitacora96VA, usuarioDni : string) : bool", false]] },

    // ── Mapeo y acceso a datos ───────────────────────────────────────────
    "VentasMPP96VA": { ester: "MPP", ancho: 330, at: [],
        op: [["+", "ObtenerTodas() : List<Venta96VA>", false],
             ["+", "ObtenerPorNumero(numero : int) : Venta96VA", false],
             ["-", "MapearVenta(row : DataRow) : Venta96VA", false]] },

    "VentasDAL96VA": { ester: "DAL", ancho: 400, at: [],
        op: [["+", "ObtenerVentas() : DataTable", false],
             ["+", "ObtenerVentaPorNumero(numero : int) : DataTable", false],
             ["+", "ObtenerComputadoraPorId(id : int) : DataTable", false],
             ["+", "ObtenerComponentesDeComputadora(id : int) : DataTable", false],
             ["+", "ObtenerPagosPorVenta(numeroVenta : int) : DataTable", false]] },

    // ── Servicios ────────────────────────────────────────────────────────
    "SerializadorXmlSER96VA": { ester: "SER", ancho: 360, at: [],
        op: [["+", "Serializar<T>(objeto : T, ruta : string) : void", false],
             ["+", "Deserializar<T>(ruta : string) : T", false],
             ["+", "ATexto<T>(objeto : T) : string", false],
             ["+", "LeerTexto(ruta : string) : string", false]] },

    "EncriptacionSER96VA": { ester: "SER", ancho: 300, at: [],
        op: [["+", "Encriptar(texto : string) : string", false]] },

    "UsuarioSesion96VA": { ester: "SER", ancho: 330,
        at: [["+", "UsuarioActual", "Usuario96VA", false]],
        op: [["+", "Instancia() : UsuarioSesion96VA", true],
             ["+", "TienePermiso(patente : PatenteEnum96VA) : bool", false]] },

    // ── Entidades propias de la serializacion ────────────────────────────
    "PaqueteSerializado96VA": { ester: "BE", ancho: 300, op: [],
        at: [["+", "Clase", "ClaseSerializable96VA", false],
             ["+", "Version", "int", false],
             ["+", "FechaGeneracion", "DateTime", false],
             ["+", "GeneradoPor", "string", false],
             ["+", "Cantidad", "int", false],
             ["+", "Hash", "string", false],
             ["+", "Objetos", "List<object>", false]] },

    "InformeVerificacion96VA": { ester: "BE", ancho: 360,
        at: [["+", "Ruta", "string", false],
             ["+", "Clase", "ClaseSerializable96VA", false],
             ["+", "Cantidad", "int", false],
             ["+", "ArchivoIntegro", "bool", false],
             ["+", "ErrorComparacion", "string", false],
             ["+", "Resultados", "List<ResultadoVerificacion96VA>", false]],
        op: [["+", "Cuantos(estado : EstadoVerificacion96VA) : int", false],
             ["+", "TodoCoincide() : bool", false]] },

    "ResultadoVerificacion96VA": { ester: "BE", ancho: 300, op: [],
        at: [["+", "Clave", "string", false],
             ["+", "Descripcion", "string", false],
             ["+", "Estado", "EstadoVerificacion96VA", false],
             ["+", "Detalle", "string", false]] },

    "ClaseSerializable96VA": { tipo: "Enumeration", ancho: 220, op: [],
        at: [["+", "Venta", "", false]] },

    "EstadoVerificacion96VA": { tipo: "Enumeration", ancho: 220, op: [],
        at: [["+", "Coincide", "", false],
             ["+", "Difiere", "", false],
             ["+", "NoExisteEnBase", "", false],
             ["+", "FaltaEnArchivo", "", false]] },

    // ── Entidades del RFN1 que se serializan ─────────────────────────────
    "Venta96VA": { ester: "BE", ancho: 300, op: [],
        at: [["+", "NumeroVenta", "int", false],
             ["+", "FechaVenta", "DateTime", false],
             ["+", "FechaEntregaEstimada", "DateTime", false],
             ["+", "Estado", "EstadoVenta96VA", false],
             ["+", "UsuarioRegistro", "string", false],
             ["+", "NumeroOrdenProduccion", "int?", false]] },

    "Cliente96VA": { ester: "BE", ancho: 240, op: [],
        at: [["+", "Dni", "string", false],
             ["+", "Nombre", "string", false],
             ["+", "Apellido", "string", false],
             ["+", "Telefono", "string", false],
             ["+", "Direccion", "string", false]] },

    "Computadora96VA": { ester: "BE", ancho: 300, op: [],
        at: [["+", "Id", "int", false],
             ["+", "Nombre", "string", false],
             ["+", "TipoConfiguracion", "TipoConfiguracion96VA", false],
             ["+", "PrecioPactado", "decimal?", false]] },

    "Componente96VA": { ester: "BE", ancho: 260, op: [],
        at: [["+", "Codigo", "string", false],
             ["+", "Descripcion", "string", false],
             ["+", "Tipo", "TipoComponente96VA", false],
             ["+", "Marca", "string", false],
             ["+", "Modelo", "string", false],
             ["+", "PrecioUnitario", "decimal", false],
             ["+", "Stock", "int", false],
             ["+", "StockMinimo", "int", false],
             ["+", "StockReservado", "int", false],
             ["+", "BajaLogica", "bool", false]] },

    "Pago96VA": { ester: "BE", ancho: 260, op: [],
        at: [["+", "Id", "int", false],
             ["+", "NumeroVenta", "int", false],
             ["+", "Tipo", "TipoPago96VA", false],
             ["+", "NumeroRecibo", "string", false],
             ["+", "Monto", "decimal", false],
             ["+", "FormaPago", "FormaPago96VA", false],
             ["+", "Referencia", "string", false],
             ["+", "Fecha", "DateTime", false],
             ["+", "Usuario", "string", false]] },

    "ModeloEstandar96VA": { ester: "BE", ancho: 260, op: [],
        at: [["+", "Id", "int", false],
             ["+", "Nombre", "string", false],
             ["+", "Descripcion", "string", false]] }
};

// ══════════════════════════════════════════════════════════════════════════
//  CATALOGO DE TABLAS (las de SQL/99_full_install.sql)
//    cols: [nombre, tipo, largo | "precision,escala", NOT NULL, PK]
//    fks:  [nombre de la FK, columna, tabla referida, card. hija, card. padre]
// ══════════════════════════════════════════════════════════════════════════
var TABLAS = {
    "Ventas": {
        cols: [["NumeroVenta", "int", "", true, true],
               ["DniCliente", "nvarchar", "20", true, false],
               ["IdComputadora", "int", "", true, false],
               ["FechaVenta", "datetime", "", true, false],
               ["FechaEntregaEstimada", "date", "", true, false],
               ["Estado", "int", "", true, false],
               ["UsuarioRegistro", "nvarchar", "150", false, false]],
        fks: [["FK_Venta_Cliente", "DniCliente", "Clientes", "0..*", "1"],
              ["FK_Venta_Computadora", "IdComputadora", "Computadoras", "0..1", "1"]] },

    "Clientes": {
        cols: [["Dni", "nvarchar", "20", true, true],
               ["Nombre", "nvarchar", "100", true, false],
               ["Apellido", "nvarchar", "100", true, false],
               ["Telefono", "nvarchar", "50", false, false],
               ["Direccion", "nvarchar", "200", false, false]],
        fks: [] },

    "Computadoras": {
        cols: [["Id", "int", "", true, true],
               ["Nombre", "nvarchar", "150", false, false],
               ["TipoConfiguracion", "int", "", true, false],
               ["PrecioTotal", "decimal", "12,2", true, false],
               ["IdModeloOrigen", "int", "", false, false]],
        fks: [["FK_Computadora_ModeloOrigen", "IdModeloOrigen", "ModelosEstandar", "0..*", "0..1"]] },

    "ComputadoraComponentes": {
        cols: [["IdComputadora", "int", "", true, true],
               ["CodigoComponente", "nvarchar", "50", true, true],
               ["Cantidad", "int", "", true, false]],
        fks: [["FK_CompComp_Comp", "IdComputadora", "Computadoras", "1..*", "1"],
              ["FK_CompComp_Componente", "CodigoComponente", "Componentes", "0..*", "1"]] },

    "Componentes": {
        cols: [["Codigo", "nvarchar", "50", true, true],
               ["Descripcion", "nvarchar", "200", true, false],
               ["Tipo", "int", "", true, false],
               ["Marca", "nvarchar", "100", false, false],
               ["Modelo", "nvarchar", "100", false, false],
               ["PrecioUnitario", "decimal", "12,2", true, false],
               ["Stock", "int", "", true, false],
               ["StockMinimo", "int", "", true, false],
               ["StockReservado", "int", "", true, false],
               ["Bit_Lo_Bo", "bit", "", true, false]],
        fks: [] },

    "Pagos": {
        cols: [["Id", "int", "", true, true],
               ["NumeroVenta", "int", "", true, false],
               ["Tipo", "int", "", true, false],
               ["NumeroRecibo", "nvarchar", "30", false, false],
               ["Monto", "decimal", "12,2", true, false],
               ["FormaPago", "int", "", true, false],
               ["Referencia", "nvarchar", "100", false, false],
               ["Fecha", "datetime", "", true, false],
               ["Usuario", "nvarchar", "150", false, false]],
        fks: [["FK_Pago_Venta", "NumeroVenta", "Ventas", "0..*", "1"]] },

    "ModelosEstandar": {
        cols: [["Id", "int", "", true, true],
               ["Nombre", "nvarchar", "150", true, false],
               ["Descripcion", "nvarchar", "300", false, false]],
        fks: [] },

    "ModeloEstandarComponentes": {
        cols: [["IdModelo", "int", "", true, true],
               ["CodigoComponente", "nvarchar", "50", true, true]],
        fks: [["FK_ModeloComp_Modelo", "IdModelo", "ModelosEstandar", "1..*", "1"],
              ["FK_ModeloComp_Comp", "CodigoComponente", "Componentes", "0..*", "1"]] },

    // Sin FK: registra el DNI del usuario como texto.
    "Bitacora": {
        cols: [["Id", "nvarchar", "50", true, true],
               ["Codigo", "nvarchar", "50", false, false],
               ["Categoria", "nvarchar", "50", false, false],
               ["Criticidad", "nvarchar", "50", false, false],
               ["Descripcion", "nvarchar", "max", false, false],
               ["Fecha", "datetime", "", true, false],
               ["Modulo", "nvarchar", "50", false, false],
               ["UsuarioDni", "nvarchar", "20", false, false]],
        fks: [] }
};


// ══════════════════════════════════════════════════════════════════════════
//  A03 SERIALIZACION
//  Que se serializa: la Venta del RFN1 como legajo completo (Venta, Cliente,
//  Computadora con sus Componentes y Modelo de origen, y Pagos). Es la unica
//  clase serializable. Formato XML (XmlSerializer) con
//  hash SHA-256 para verificar que el archivo no se altero.
// ══════════════════════════════════════════════════════════════════════════

var NOMBRE_RAIZ = "PCFORGE - A03 Serializacion";
var SISTEMA = "PCFORGE";
var ACTOR = "Administrador";
var CU1 = "CU-SER01 Serializar objetos";
var CU2 = "CU-SER02 Des-serializar objetos";

// ── Modelo de datos a serializar ─────────────────────────────────────────
var MODELO = {
    ubicaciones: [
        ["PaqueteSerializado96VA", 40,  40,  null],
        ["ClaseSerializable96VA",  40,  260, null],
        ["Venta96VA",              420, 40,  null],
        ["Cliente96VA",            800, 40,  null],
        ["Pago96VA",               800, 250, null],
        ["Computadora96VA",        420, 300, null],
        ["Componente96VA",         420, 500, null],
        ["ModeloEstandar96VA",     800, 520, null]
    ],
    relaciones: [
        ["Venta96VA",              "Cliente96VA",           "asociacion",  "0..*", "1",    "Cliente"],
        ["Pago96VA",               "Venta96VA",             "composicion", "0..*", "1",    "Pagos"],
        ["Computadora96VA",        "Venta96VA",             "composicion", "1",    "1",    "Computadora"],
        ["Componente96VA",         "Computadora96VA",       "agregacion",  "1..*", "0..*", "Componentes"],
        ["Computadora96VA",        "ModeloEstandar96VA",    "asociacion",  "0..*", "0..1", "ModeloOrigen"],
        ["PaqueteSerializado96VA", "Venta96VA",             "dependencia", null,   null,   "Objetos"],
        ["PaqueteSerializado96VA", "ClaseSerializable96VA", "dependencia", null,   null,   "Clase"]
    ]
};

var FUNDAMENTO = texto([
    "QUE SE SERIALIZA Y POR QUE",
    "",
    "Clase serializable: Venta96VA (la unica), como legajo completo:",
    "  Venta -> Cliente, Computadora (-> Componentes, ModeloOrigen) y Pagos.",
    "- Es el objeto central del RFN1, el requerimiento de la Entrega 1.",
    "- Es un grafo de objetos con colecciones: se serializa una composicion,",
    "  no una clase plana.",
    "- VentasBLL96VA ya arma el grafo completo: no hace falta SQL nuevo.",
    "- No tiene referencias circulares: XmlSerializer lo recorre sin marcar",
    "  las clases BE con atributos.",
    "",
    "FORMATO: XML con XmlSerializer (.NET Framework 4.8)",
    "- Es legible: en el paso Verificar se muestra el archivo tal cual.",
    "- Solo pide propiedades publicas y constructor sin parametros.",
    "- BinaryFormatter se descarta: Microsoft lo declaro obsoleto e inseguro",
    "  para des-serializar archivos que vienen de afuera.",
    "- JSON con DataContractJsonSerializer (el de GestorIdioma) obliga a marcar",
    "  las clases con [DataContract] y guarda las fechas como /Date(...)/.",
    "",
    "INTEGRIDAD",
    "El paquete guarda un hash SHA-256 (EncriptacionSER96VA) del propio XML,",
    "calculado con una sal fija para que no alcance con recalcularlo a mano.",
    "Al verificar se recalcula: si no coincide, el archivo se modifico.",
    "El hash se toma del objeto reconstruido: reindentar el archivo no lo",
    "invalida; cambiar un dato, si.",
    "",
    "NO SE SERIALIZAN las propiedades calculadas (PrecioTotal, SaldoPendiente,",
    "StockLibre, NombreCompleto): se recalculan al reconstruir el objeto."
]);

var EJEMPLO_XML = texto([
    "EJEMPLO DE ARCHIVO: Venta_20260928_153000.xml",
    "",
    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
    "<PaqueteSerializado xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">",
    "  <Clase>Venta</Clase>",
    "  <Version>1</Version>",
    "  <FechaGeneracion>2026-09-28T15:30:00</FechaGeneracion>",
    "  <GeneradoPor>30111222</GeneradoPor>",
    "  <Cantidad>1</Cantidad>",
    "  <Hash>7wz0Bobo...RTUSRh4=</Hash>",
    "  <Objetos>",
    "    <Venta>",
    "      <NumeroVenta>12</NumeroVenta>",
    "      <Cliente><Dni>40111222</Dni><Nombre>Ana</Nombre>...</Cliente>",
    "      <Computadora>",
    "        <Id>31</Id><Nombre>Gamer Pro</Nombre>",
    "        <TipoConfiguracion>Estandar</TipoConfiguracion>",
    "        <Componentes><Componente06AV>...</Componente06AV></Componentes>",
    "        <PrecioPactado>1250000.00</PrecioPactado>",
    "      </Computadora>",
    "      <Estado>Senada</Estado>",
    "      <NumeroOrdenProduccion xsi:nil=\"true\" />",
    "      <Pagos><Pago06AV>...</Pago06AV></Pagos>",
    "    </Venta>",
    "  </Objetos>",
    "</PaqueteSerializado>"
]);

// ── Descomposicion funcional (los 8 pasos de la planilla) ────────────────
var CALLES = [["Administrador", 40], ["Sistema PCFORGE", 460]];
var ANCHO_CALLE = 420, ALTO_CALLE = 960;
var X_NOTAS = 910;

var PASOS = [
    ["inicio", 0, 30,  "",                                      "inicio"],
    ["p1",     0, 80,  "1. Seleccionar clase y objetos",        "accion"],
    ["p2",     0, 180, "2. Seleccionar ubicacion de destino",   "accion"],
    ["p3",     1, 280, "3. Serializar",                         "accion"],
    ["p4",     1, 380, "4. Verificar serializacion",            "accion"],
    ["p5",     0, 480, "5. Seleccionar ubicacion de origen",    "accion"],
    ["p6",     0, 580, "6. Seleccionar archivo",                "accion"],
    ["p7",     1, 680, "7. Des-serializar",                     "accion"],
    ["p8",     1, 780, "8. Verificar des-serializacion",        "accion"],
    ["fin",    1, 890, "",                                      "fin"],
    ["bd1",    1, 80,  "Base de datos PCFORGE",                 "dato", 265],
    ["xml",    1, 330, "Archivo XML",                           "dato", 265],
    ["bit",    1, 480, "Bitacora",                              "dato", 20],
    ["bd2",    1, 780, "Base de datos PCFORGE",                 "dato", 265]
];

var FLUJOS = [
    ["inicio", "p1", ""], ["bd1", "p1", ""], ["p1", "p2", ""], ["p2", "p3", ""],
    ["p3", "xml", ""], ["p3", "bit", ""], ["p3", "p4", ""], ["xml", "p4", ""],
    ["p4", "p5", ""], ["p5", "p6", ""], ["p6", "p7", ""], ["xml", "p7", ""],
    ["p7", "bit", ""], ["p7", "p8", ""], ["bd2", "p8", ""], ["p8", "fin", ""]
];

var NOTAS_PASOS = [
    ["p1", X_NOTAS, 70,  420, 80, texto(["1. Seleccion de la clase-objeto",
        "Clase: Venta (ClaseSerializable96VA, unica clase serializable)",
        "Objetos (seleccion multiple): NumeroVenta, FechaVenta,",
        "Cliente (Dni, Apellido, Nombre), Estado, Total"])],
    ["p2", X_NOTAS, 170, 420, 65, texto(["2. Seleccion de la ubicacion",
        "Carpeta de destino (ruta)",
        "Nombre de archivo: <Clase>_<aaaaMMdd_HHmmss>.xml"])],
    ["p3", X_NOTAS, 255, 420, 80, texto(["3. Serializar",
        "PaqueteSerializado96VA: Clase, Version, FechaGeneracion,",
        "GeneradoPor (DNI), Cantidad, Hash (SHA-256), Objetos",
        "Salida: archivo .xml y evento en la Bitacora"])],
    ["p4", X_NOTAS, 370, 420, 65, texto(["4. Verificar",
        "Archivo: ruta, tamano y contenido XML",
        "Por objeto: Clave, Estado (Coincide / Difiere), Detalle"])],
    ["p5", X_NOTAS, 470, 420, 50, texto(["5. Seleccion de la ubicacion de origen",
        "Carpeta de origen (ruta)"])],
    ["p6", X_NOTAS, 570, 420, 50, texto(["6. Seleccionar el archivo",
        "Archivo .xml: nombre, fecha de modificacion, tamano"])],
    ["p7", X_NOTAS, 660, 420, 80, texto(["7. Des-serializar",
        "PaqueteSerializado96VA reconstruido (mismos atributos)",
        "y sus objetos: Venta96VA con Cliente, Computadora y Pagos",
        "Salida: objetos en pantalla y evento en la Bitacora"])],
    ["p8", X_NOTAS, 760, 420, 80, texto(["8. Verificar",
        "ArchivoIntegro: hash recalculado = Hash del archivo",
        "Por objeto contra la base: Clave, Estado (Coincide /",
        "Difiere / NoExisteEnBase), Detalle"])]
];

// ── Casos de uso ─────────────────────────────────────────────────────────
var ECU1 = texto([
    "ID Y NOMBRE: CU-SER01-06AV Serializar objetos",
    "",
    "DESCRIPCION: El administrador elige una clase del sistema y uno o mas",
    "objetos de esa clase; el sistema los guarda en un archivo XML en la",
    "ubicacion elegida, con un hash SHA-256 que permite verificar despues",
    "que el archivo no se altero.",
    "",
    "ACTOR PRINCIPAL: Administrador.",
    "",
    "PRECONDICIONES: El administrador inicio sesion y su rol tiene la patente",
    "Serializar. Existe al menos un objeto de la clase elegida.",
    "",
    "CONDICION: La carpeta de destino existe y se puede escribir en ella.",
    "",
    "ESCENARIO PRINCIPAL:",
    "1. El administrador ingresa a Administracion > Serializacion.",
    "2. El sistema muestra la clase serializable, Venta, ya elegida.",
    "3. El administrador selecciona la clase Venta. (paso 1)",
    "4. El sistema lista las ventas con numero, fecha, cliente, estado y total.",
    "5. El administrador selecciona una o mas ventas.",
    "6. El administrador selecciona la carpeta de destino. (paso 2)",
    "7. El sistema propone el nombre <Clase>_<aaaaMMdd_HHmmss>.xml.",
    "8. El administrador presiona \"Serializar\". (paso 3)",
    "9. El sistema arma el paquete (clase, version, fecha, usuario, cantidad",
    "   y objetos), calcula el hash SHA-256 y graba el archivo XML.",
    "10. El sistema registra el evento en la bitacora e informa la ruta y la",
    "    cantidad de objetos serializados.",
    "11. El administrador presiona \"Verificar\". (paso 4)",
    "12. El sistema lee el archivo, lo des-serializa, compara cada objeto con",
    "    el original y muestra el XML y el resultado: todos coinciden.",
    "",
    "ESCENARIOS ALTERNATIVOS:",
    "2a. Sin la patente Serializar la opcion no aparece en el menu; si se",
    "    invoca igual, el sistema informa que no tiene permiso y termina.",
    "4a. La clase no tiene objetos: el sistema lo informa y deshabilita",
    "    \"Serializar\".",
    "8a. No hay objetos seleccionados: el sistema pide elegir al menos uno.",
    "8b. La carpeta no existe o no se puede escribir: el sistema lo informa",
    "    y vuelve al paso 6.",
    "8c. Ya existe un archivo con ese nombre: el sistema pide confirmar si lo",
    "    reemplaza; si no, vuelve al paso 6.",
    "9a. Falla la grabacion (disco lleno, archivo en uso): el sistema informa",
    "    el error, lo registra en la bitacora y no deja un archivo a medias.",
    "12a. Un objeto no coincide o el archivo no se puede leer: el sistema marca",
    "     la verificacion como fallida y muestra que objeto difiere.",
    "",
    "POSTCONDICIONES: Queda un archivo XML con los objetos elegidos y su hash,",
    "verificado contra los originales, y el evento en la bitacora. La base de",
    "datos no se modifica."
]);

var ECU2 = texto([
    "ID Y NOMBRE: CU-SER02-06AV Des-serializar objetos",
    "",
    "DESCRIPCION: El administrador elige una carpeta y un archivo XML generado",
    "por el sistema; el sistema reconstruye los objetos, verifica que el",
    "archivo no se haya alterado y los compara con su version actual en la",
    "base de datos.",
    "",
    "ACTOR PRINCIPAL: Administrador.",
    "",
    "PRECONDICIONES: El administrador inicio sesion y su rol tiene la patente",
    "Serializar. Existe al menos un archivo generado con CU-SER01.",
    "",
    "CONDICION: El archivo tiene el formato del paquete serializado (version 1).",
    "",
    "ESCENARIO PRINCIPAL:",
    "1. El administrador ingresa a Administracion > Serializacion, pestana",
    "   Des-serializar.",
    "2. El administrador selecciona la carpeta de origen. (paso 5)",
    "3. El sistema lista los archivos .xml con nombre, fecha y tamano.",
    "4. El administrador selecciona un archivo. (paso 6)",
    "5. El sistema muestra el contenido XML del archivo.",
    "6. El administrador presiona \"Des-serializar\". (paso 7)",
    "7. El sistema lee el archivo, reconstruye el paquete y sus objetos y los",
    "   muestra: clase, cantidad, fecha de generacion y usuario que lo genero.",
    "8. El sistema registra el evento en la bitacora.",
    "9. El administrador presiona \"Verificar\". (paso 8)",
    "10. El sistema recalcula el hash y lo compara con el del archivo: el",
    "    archivo esta integro.",
    "11. El sistema busca cada objeto en la base por su clave y lo compara:",
    "    informa si Coincide, Difiere (con los campos distintos) o No existe",
    "    en la base.",
    "",
    "ESCENARIOS ALTERNATIVOS:",
    "3a. La carpeta no tiene archivos .xml: el sistema lo informa.",
    "7a. El archivo no es un paquete serializado del sistema o esta mal",
    "    formado: el sistema informa que el formato no es valido, lo registra",
    "    en la bitacora y no muestra objetos.",
    "7b. La version del archivo no es compatible: el sistema lo informa y",
    "    termina.",
    "10a. El hash no coincide: el sistema informa que el archivo se modifico",
    "     despues de generarse, marca la verificacion como fallida y registra",
    "     el evento con criticidad alta. Igual muestra la comparacion.",
    "11a. No hay conexion con la base: el sistema informa el error; la",
    "     verificacion de integridad del archivo ya quedo hecha.",
    "",
    "POSTCONDICIONES: Los objetos del archivo quedan reconstruidos en memoria",
    "y verificados. La base de datos no se modifica: des-serializar no importa",
    "ni pisa datos."
]);

var DCU = {
    titulo: "A03 - DCU Serializacion",
    sistema: SISTEMA,
    marco: [230, 30, 360, 330],
    actores: [[ACTOR, 40, 130]],
    casos: [[CU1, 290, 80, ECU1],
            [CU2, 290, 230, ECU2]],
    relaciones: [[ACTOR, CU1, "asociacion"],
                 [ACTOR, CU2, "asociacion"]]
};

// Lineas de vida comunes a las dos secuencias.
var UI = "SerializacionControl96VA", BLL = "SerializacionBLL96VA", SES = "UsuarioSesion96VA",
    VBLL = "VentasBLL96VA", VMPP = "VentasMPP96VA", VDAL = "VentasDAL96VA",
    XML = "SerializadorXmlSER96VA", ENC = "EncriptacionSER96VA", BIT = "BitacoraBLL96VA",
    ARCH = "Archivo XML";

// En las secuencias se usan los nombres de linea de vida del resto del
// documento (UI_96VA, VentasBLL_96VA, DAL_96VA); en los DC, los de las clases.
var NOMBRE_DS = {};
NOMBRE_DS[UI] = "UI_96VA";                 NOMBRE_DS[BLL] = "SerializacionBLL_96VA";
NOMBRE_DS[SES] = "UsuarioSesion_96VA";     NOMBRE_DS[VBLL] = "VentasBLL_96VA";
NOMBRE_DS[VMPP] = "VentasMPP_96VA";        NOMBRE_DS[VDAL] = "DAL_96VA";
NOMBRE_DS[XML] = "SerializadorXmlSER_96VA"; NOMBRE_DS[ENC] = "EncriptacionSER_96VA";
NOMBRE_DS[BIT] = "BitacoraBLL_96VA";

function nombreDS(n) { return NOMBRE_DS[n] != null ? NOMBRE_DS[n] : n; }

/** Pasa lineas de vida y mensajes a los nombres de la secuencia. */
function paraDS(lista, columnas) {
    var r = [];
    for (var i = 0; i < lista.length; i++) {
        var fila = [];
        for (var k = 0; k < lista[i].length; k++)
            fila.push(k < columnas ? nombreDS(lista[i][k]) : lista[i][k]);
        r.push(fila);
    }
    return r;
}

var LIFELINES = [
    [ACTOR, "actor"], [UI, "boundary"], [BLL, "clase"], [SES, "clase"],
    [VBLL, "clase"], [VMPP, "clase"], [VDAL, "clase"],
    [XML, "clase"], [ENC, "clase"], [BIT, "clase"], [ARCH, "externo"]
];

// Posiciones del DC (una fila por capa: UI, BLL, SER, BE).
var DC_Y_UI = 40, DC_Y_BLL = 300, DC_Y_SER = 760, DC_Y_BE = 920;

function ubicacionesDC(opsUI, opsBLL, opVentas, opsMPP, opsDAL) {
    return [
        [UI,                          40,   DC_Y_UI,  opsUI],
        [BLL,                         40,   DC_Y_BLL, opsBLL],
        [VBLL,                        940,  DC_Y_BLL, opVentas],
        [BIT,                         940,  640,      null],
        [VMPP,                        1320, DC_Y_BLL, opsMPP],
        [VDAL,                        1320, 440,      opsDAL],
        [XML,                         40,   DC_Y_SER, null],
        [ENC,                         440,  DC_Y_SER, null],
        [SES,                         780,  DC_Y_SER, null],
        ["PaqueteSerializado96VA",    40,   DC_Y_BE,  null],
        ["InformeVerificacion96VA",   380,  DC_Y_BE,  null],
        ["ResultadoVerificacion96VA", 760,  DC_Y_BE,  null],
        ["ClaseSerializable96VA",     1100, DC_Y_BE,  null],
        ["EstadoVerificacion96VA",    1100, 1040,     null],
        ["Venta96VA",                 1360, DC_Y_BE,  null]
    ];
}

var RELACIONES_DC = [
    [UI,   BLL,  "dependencia", null, null, "usa"],
    [UI,   "InformeVerificacion96VA", "dependencia", null, null, "muestra"],
    [BLL,  XML,  "dependencia", null, null, "usa"],
    [BLL,  ENC,  "dependencia", null, null, "usa"],
    [BLL,  SES,  "dependencia", null, null, "usa"],
    [BLL,  VBLL, "dependencia", null, null, "usa"],
    [BLL,  BIT,  "dependencia", null, null, "usa"],
    [BLL,  "PaqueteSerializado96VA",  "dependencia", null, null, "crea"],
    [BLL,  "InformeVerificacion96VA", "dependencia", null, null, "crea"],
    [BLL,  "ClaseSerializable96VA",   "dependencia", null, null, ""],
    [VBLL, VMPP, "dependencia", null, null, "usa"],
    [VMPP, VDAL, "dependencia", null, null, "usa"],
    [VMPP, "Venta96VA", "dependencia", null, null, "mapea"],
    ["ResultadoVerificacion96VA", "InformeVerificacion96VA", "composicion", "0..*", "1", "Resultados"],
    ["ResultadoVerificacion96VA", "EstadoVerificacion96VA",  "dependencia", null, null, ""],
    ["PaqueteSerializado96VA",    "Venta96VA",               "dependencia", null, null, "Objetos"]
];

// DER comun a los dos casos de uso: lo que se lee para armar (CU-SER01) o
// comparar (CU-SER02) los objetos, y la Bitacora, donde se registra el evento.
var TABLAS_DER = [
    ["Clientes",                  40,  40],
    ["Ventas",                    440, 40],
    ["Pagos",                     840, 40],
    ["ModelosEstandar",           40,  360],
    ["Computadoras",              440, 360],
    ["Bitacora",                  840, 360],
    ["ComputadoraComponentes",    440, 660],
    ["Componentes",               840, 660]
];

var CASOS = [

// ──────────────────────────────────────────────────────────── CU-SER01 ───
{
    nombre: CU1, id: "CU-SER01",
    titulo: "Serializar objetos",
    ecu: ECU1,
    mensajes: [
        [ACTOR, UI,   "abrir Serializacion()"],
        [UI,    UI,   "MostrarClases()"],
        [ACTOR, UI,   "SeleccionarClase(Venta)"],
        [UI,    BLL,  "ObtenerObjetos(ClaseSerializable96VA.Venta)"],
        [BLL,   BLL,  "ExigirPermiso()"],
        [BLL,   SES,  "TienePermiso(Serializar)"],
        [BLL,   VBLL, "ObtenerTodas()"],
        [VBLL,  VMPP, "ObtenerTodas()"],
        [VMPP,  VDAL, "ObtenerVentas()"],
        [VDAL,  VMPP, ":DataTable"],
        [VMPP,  VMPP, "MapearVenta(row)"],
        [VMPP,  VBLL, ":List<Venta96VA>"],
        [VBLL,  BLL,  ":List<Venta96VA>"],
        [BLL,   UI,   ":List<object>"],
        [UI,    UI,   "MostrarObjetos(objetos)"],
        [ACTOR, UI,   "SeleccionarObjetos(ventas)"],
        [ACTOR, UI,   "SeleccionarDestino(carpeta)"],
        [UI,    BLL,  "ProponerNombreArchivo(Venta)"],
        [BLL,   UI,   ":Venta_aaaaMMdd_HHmmss.xml"],
        [ACTOR, UI,   "Serializar()"],
        [UI,    BLL,  "Serializar(Venta, ventas, ruta)"],
        [BLL,   BLL,  "ExigirPermiso()"],
        [BLL,   BLL,  "ArmarPaquete(Venta, ventas, dni, ahora)"],
        [BLL,   BLL,  "ValidarObjetos(Venta, ventas)"],
        [BLL,   BLL,  "CalcularHash(paquete)"],
        [BLL,   XML,  "ATexto(paquete)"],
        [XML,   BLL,  ":xml"],
        [BLL,   ENC,  "Encriptar(xml)"],
        [ENC,   BLL,  ":hash SHA-256"],
        [BLL,   BLL,  "ValidarDestino(ruta)"],
        [BLL,   XML,  "Serializar(paquete, ruta)"],
        [XML,   ARCH, "escribir(xml)"],
        [BLL,   BIT,  "Registrar(Serializacion, Baja, descripcion, Serializacion, dni)"],
        [BLL,   UI,   ":PaqueteSerializado96VA"],
        [UI,    ACTOR, ":archivo generado (ruta y cantidad de objetos)"],
        [ACTOR, UI,   "Verificar()"],
        [UI,    BLL,  "VerificarSerializacion(ruta, ventas)"],
        [BLL,   BLL,  "LeerPaquete(ruta)"],
        [BLL,   XML,  "Deserializar<PaqueteSerializado96VA>(ruta)"],
        [XML,   ARCH, "leer()"],
        [XML,   BLL,  ":PaqueteSerializado96VA"],
        [BLL,   BLL,  "EsIntegro(paquete)"],
        [BLL,   BLL,  "Comparar(original, leido)"],
        [BLL,   UI,   ":InformeVerificacion96VA"],
        [UI,    BLL,  "LeerContenido(ruta)"],
        [BLL,   XML,  "LeerTexto(ruta)"],
        [BLL,   UI,   ":xml"],
        [UI,    UI,   "MostrarInformeSer()"],
        [UI,    ACTOR, ":verificacion correcta (todos los objetos coinciden)"]
    ],
    clases: ubicacionesDC(
        ["SeleccionarClase", "SeleccionarDestino", "Serializar", "VerificarSerializacion",
         "MostrarObjetos", "MostrarInformeSer", "ActualizarPasos"],
        ["ObtenerObjetos", "ProponerNombreArchivo", "ValidarDestino", "Serializar", "ArmarPaquete",
         "ValidarObjetos", "VerificarSerializacion", "VerificarContraOriginales", "LeerPaquete",
         "LeerContenido", "CalcularHash", "EsIntegro", "ClaveDe", "Comparar", "Registrar", "ExigirPermiso"],
        ["ObtenerTodas"],
        ["ObtenerTodas", "MapearVenta"],
        ["ObtenerVentas", "ObtenerComputadoraPorId", "ObtenerComponentesDeComputadora", "ObtenerPagosPorVenta"]),
    gui: {
        nombre: "SerializacionControl - pestana Serializar",
        ancho: 860, alto: 560,
        notas: "Administracion > Serializacion. Requiere la patente Serializar.",
        controles: [
            ["Serializacion:   [ Serializar ]   Des-serializar", "label", 20, 15, 420, 25],
            ["1. Clase:", "label", 20, 60, 90, 25],
            ["Venta", "combobox", 120, 60, 240, 25],
            ["Objetos de la clase (seleccion multiple): N | Fecha | Cliente | Estado | Total", "list", 20, 100, 820, 170],
            ["2. Destino:", "label", 20, 290, 90, 25],
            ["C:\\PCFORGE\\Serializacion\\Venta_20260928_153000.xml", "textbox", 120, 290, 560, 25],
            ["Examinar...", "button", 700, 290, 140, 25],
            ["3. Serializar", "button", 120, 335, 170, 32],
            ["4. Verificar", "button", 310, 335, 170, 32],
            ["Contenido XML del archivo (solo lectura)", "textbox", 20, 385, 520, 160],
            ["Verificacion: Clave | Estado | Detalle", "list", 560, 385, 280, 160]
        ],
        leyenda: texto([
            "PASOS EN LA PANTALLA",
            "1. Combo Clase y grilla de objetos (seleccion multiple).",
            "2. Destino: carpeta con Examinar; el nombre lo propone el sistema.",
            "3. Serializar: habilitado con al menos un objeto y destino.",
            "4. Verificar: habilitado despues de serializar; muestra el XML",
            "   y el resultado por objeto (Coincide / Difiere)."
        ]),
        altoLeyenda: 110
    }
},

// ──────────────────────────────────────────────────────────── CU-SER02 ───
{
    nombre: CU2, id: "CU-SER02",
    titulo: "Des-serializar objetos",
    ecu: ECU2,
    mensajes: [
        [ACTOR, UI,   "abrir pestana Des-serializar()"],
        [ACTOR, UI,   "SeleccionarOrigen(carpeta)"],
        [UI,    BLL,  "ListarArchivos(carpeta)"],
        [BLL,   BLL,  "ExigirPermiso()"],
        [BLL,   SES,  "TienePermiso(Serializar)"],
        [BLL,   ARCH, "listar(*.xml)"],
        [BLL,   UI,   ":List<string>"],
        [UI,    UI,   "CargarArchivos()"],
        [ACTOR, UI,   "SeleccionarArchivo(archivo)"],
        [UI,    BLL,  "LeerContenido(ruta)"],
        [BLL,   XML,  "LeerTexto(ruta)"],
        [XML,   ARCH, "leer()"],
        [BLL,   UI,   ":xml"],
        [ACTOR, UI,   "Deserializar()"],
        [UI,    BLL,  "Deserializar(ruta)"],
        [BLL,   BLL,  "LeerPaquete(ruta)"],
        [BLL,   XML,  "Deserializar<PaqueteSerializado96VA>(ruta)"],
        [XML,   ARCH, "leer()"],
        [XML,   BLL,  ":PaqueteSerializado96VA"],
        [BLL,   BIT,  "Registrar(Deserializacion, Baja, descripcion, Serializacion, dni)"],
        [BLL,   UI,   ":PaqueteSerializado96VA"],
        [UI,    UI,   "MostrarObjetos(paquete.Objetos)"],
        [ACTOR, UI,   "Verificar()"],
        [UI,    BLL,  "VerificarDeserializacion(paquete, ruta)"],
        [BLL,   BLL,  "EsIntegro(paquete)"],
        [BLL,   BLL,  "CalcularHash(paquete)"],
        [BLL,   XML,  "ATexto(paquete)"],
        [XML,   BLL,  ":xml"],
        [BLL,   ENC,  "Encriptar(xml)"],
        [ENC,   BLL,  ":hash SHA-256"],
        [BLL,   BLL,  "CrearBuscador(Venta)"],
        [BLL,   VBLL, "ObtenerPorNumero(numeroVenta)"],
        [VBLL,  VMPP, "ObtenerPorNumero(numero)"],
        [VMPP,  VDAL, "ObtenerVentaPorNumero(numero)"],
        [VDAL,  VMPP, ":DataTable"],
        [VMPP,  VBLL, ":Venta96VA"],
        [VBLL,  BLL,  ":Venta96VA"],
        [BLL,   BLL,  "Comparar(delArchivo, actual)"],
        [BLL,   UI,   ":InformeVerificacion96VA"],
        [UI,    UI,   "MostrarInformeDes()"],
        [UI,    ACTOR, ":archivo integro y comparacion con la base"]
    ],
    clases: ubicacionesDC(
        ["SeleccionarOrigen", "CargarArchivos", "SeleccionarArchivo", "Deserializar",
         "VerificarDeserializacion", "MostrarInformeDes", "ActualizarPasos"],
        ["ListarArchivos", "LeerContenido", "Deserializar", "LeerPaquete", "VerificarDeserializacion",
         "VerificarContraBase", "CrearBuscador", "CalcularHash", "EsIntegro", "ClaveDe", "Comparar",
         "Registrar", "ExigirPermiso"],
        ["ObtenerPorNumero"],
        ["ObtenerPorNumero", "MapearVenta"],
        ["ObtenerVentaPorNumero", "ObtenerComputadoraPorId", "ObtenerComponentesDeComputadora", "ObtenerPagosPorVenta"]),
    gui: {
        nombre: "SerializacionControl - pestana Des-serializar",
        ancho: 860, alto: 540,
        notas: "Administracion > Serializacion. Requiere la patente Serializar.",
        controles: [
            ["Serializacion:   Serializar   [ Des-serializar ]", "label", 20, 15, 420, 25],
            ["5. Carpeta de origen:", "label", 20, 60, 140, 25],
            ["C:\\PCFORGE\\Serializacion", "textbox", 170, 60, 510, 25],
            ["Examinar...", "button", 700, 60, 140, 25],
            ["6. Archivos .xml: Nombre | Fecha | Tamano", "list", 20, 100, 400, 170],
            ["Vista previa del XML (solo lectura)", "textbox", 440, 100, 400, 170],
            ["7. Des-serializar", "button", 20, 290, 170, 32],
            ["8. Verificar", "button", 210, 290, 170, 32],
            ["Archivo integro: SI (el hash SHA-256 coincide)", "label", 440, 293, 400, 25],
            ["Objetos reconstruidos: Clase | Clave | Descripcion", "list", 20, 340, 400, 180],
            ["Verificacion contra la base: Clave | Estado | Detalle", "list", 440, 340, 400, 180]
        ],
        leyenda: texto([
            "PASOS EN LA PANTALLA",
            "5. Carpeta de origen con Examinar.",
            "6. Lista de archivos .xml; al elegir uno se ve el XML.",
            "7. Des-serializar: muestra los objetos reconstruidos.",
            "8. Verificar: integridad del archivo (hash) y, por objeto,",
            "   Coincide / Difiere / No existe en la base."
        ]),
        altoLeyenda: 110
    }
}

];

// ══════════════════════════════════════════════════════════════════════════
//  PROGRAMA PRINCIPAL
// ══════════════════════════════════════════════════════════════════════════
function main() {
    try { Repository.EnsureOutputVisible("Script"); } catch (eOut) {}
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - A03 Serializacion");
    Session.Output("=======================================================");

    var raiz = paqueteRaiz(NOMBRE_RAIZ);
    if (raiz == null) return;
    try { raiz.Notes = FUNDAMENTO; raiz.Update(); } catch (eN) {}

    // 00 Modelo de datos
    var pkgModelo = nuevoPaquete(raiz, "00 Modelo de datos a serializar");
    var m = generarClases(pkgModelo, "A03 - Modelo de datos a serializar", MODELO.ubicaciones, MODELO.relaciones);
    var altoFund = FUNDAMENTO.split("\r\n").length * 15 + 30;
    nota(pkgModelo, m.diagrama, FUNDAMENTO, 1120, 40, 500, altoFund, null);
    nota(pkgModelo, m.diagrama, EJEMPLO_XML, 1120, 80 + altoFund, 500, EJEMPLO_XML.split("\r\n").length * 15 + 30, null);
    log("  A03 - Modelo de datos a serializar (" + m.miembros + " miembros, " + m.relaciones + " relaciones)");

    // 01 Descomposicion funcional
    var pkgDesc = nuevoPaquete(raiz, "01 Descomposicion funcional");
    generarActividad(pkgDesc, "A03 - Descomposicion funcional", CALLES, ANCHO_CALLE, ALTO_CALLE,
                     PASOS, FLUJOS, NOTAS_PASOS);

    // 02 DCU (el actor y los casos de uso se reutilizan en los demas diagramas)
    var pkgDCU = nuevoPaquete(raiz, "02 DCU Serializacion");
    var dcu = generarDCU(pkgDCU, DCU);
    nota(pkgDCU, dcu.diagrama, "Patente requerida: Serializar (rol Administrador).", 630, 60, 260, 45, null);

    log("");
    for (var c = 0; c < CASOS.length; c++) {
        var cu = CASOS[c];
        var pkg = nuevoPaquete(raiz, (c + 3 < 10 ? "0" : "") + (c + 3) + " " + cu.nombre);
        log(cu.nombre);

        generarECU(pkg, cu.id + " - ECU " + cu.titulo, dcu.elementos[cu.nombre], dcu.elementos[ACTOR], cu.ecu);

        var existentes = {};
        existentes[ACTOR] = dcu.elementos[ACTOR];
        var n = generarSecuencia(pkg, cu.id + " - DS " + cu.titulo,
                                 paraDS(LIFELINES, 1), paraDS(cu.mensajes, 2), existentes);
        log("  DS  listo (" + n + " mensajes)");

        var dc = generarClases(pkg, cu.id + " - DC " + cu.titulo, cu.clases, RELACIONES_DC);
        log("  DC  listo (" + dc.miembros + " miembros, " + dc.relaciones + " relaciones)");

        var fks = generarDER(pkg, cu.id + " - DER " + cu.titulo, TABLAS_DER);
        log("  DER listo (" + TABLAS_DER.length + " tablas, " + fks + " claves foraneas, notacion de Martin)");

        generarGUI(pkg, cu.id + " - GUI " + cu.titulo, cu.gui);
        log("");
    }

    try { Repository.RefreshModelView(0); } catch (eRef) {}
    log("=======================================================");
    log(" LISTO. 13 diagramas en: " + NOMBRE_RAIZ);
    log(" Revisar a mano: fragmentos alt/loop de las secuencias y la");
    log(" notacion de Martin en los DER (si el aviso aparecio arriba).");
    log("=======================================================");
}

main();
