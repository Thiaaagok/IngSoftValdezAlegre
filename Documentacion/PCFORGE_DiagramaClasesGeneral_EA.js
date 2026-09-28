!INC Local Scripts.EAConstants-JScript

/*
 * ===========================================================================
 *  PCFORGE — DIAGRAMA DE CLASES GENERAL (entidades de negocio, BE)
 *            + DIAGRAMA DEL PATRÓN BUILDER
 *  Trabajo de Diploma — UAI — Valdez / Alegre
 * ===========================================================================
 *
 *  QUÉ HACE
 *  --------
 *  Crea dentro del modelo abierto un paquete "PCFORGE - Diagrama de clases
 *  general" con dos diagramas:
 *
 *    ├─ Diagrama de clases general (BE)
 *    │    Las 17 entidades del negocio y las 8 enumeraciones, tal como están
 *    │    en el código (mismos atributos, mismos tipos), con el sufijo 96VA,
 *    │    más Usuario96VA (capa SER), que las compras referencian.
 *    │    Incluye la relación nueva Computadora → ModeloEstandar (ModeloOrigen).
 *    │
 *    └─ Patrón Builder — armado de computadoras
 *         Builder, ConcreteBuilders, Director, Product y Clientes.
 *
 *  Convenciones del diagrama general (las mismas del diagrama anterior):
 *    · atributos con visibilidad privada ( - ), operaciones públicas ( + );
 *    · atributos calculados marcados como derivados ( / );
 *    · cada clase que usa una enumeración la apunta con una dependencia «use»;
 *    · composición (rombo lleno) donde la parte no existe sin el todo;
 *      agregación (rombo vacío) donde la parte existe por su cuenta.
 *
 *  Es independiente de los otros scripts: se puede correr solo.
 *
 *  CÓMO SE USA
 *  -----------
 *   1. Abrir el proyecto en Enterprise Architect.
 *   2. Specialize > Tools > Scripting.
 *   3. Si "New JScript" aparece en gris: clic derecho sobre el espacio vacío de
 *      la lista > New Group... (tipo Normal), y clic derecho sobre el grupo
 *      nuevo > New JScript.
 *   4. Pegar TODO este archivo, guardar (Ctrl+S) y Run Script.
 * ===========================================================================
 */

var NOMBRE_RAIZ  = "PCFORGE - Diagrama de clases general";
var CLASE_ANCHO  = 270;
var ENUM_ANCHO   = 200;

// ══════════════════════════════════════════════════════════════════════════
//  ENTIDADES DE NEGOCIO (BE)
//  Extraídas del código fuente (proyecto BE). Cada atributo:
//    [nombre, tipo, derivado (calculado), constante]
// ══════════════════════════════════════════════════════════════════════════

var CLASES = [
    { n: "Cliente96VA", x: 40, y: 40, ester: "",
      at: [ ["Dni", "string", false, false],
          ["Nombre", "string", false, false],
          ["Apellido", "string", false, false],
          ["Telefono", "string", false, false],
          ["Direccion", "string", false, false],
          ["NombreCompleto", "string", true, false] ] },
    { n: "Componente96VA", x: 340, y: 660, ester: "",
      at: [ ["Codigo", "string", false, false],
          ["Descripcion", "string", false, false],
          ["Tipo", "TipoComponente96VA", false, false],
          ["Marca", "string", false, false],
          ["Modelo", "string", false, false],
          ["PrecioUnitario", "decimal", false, false],
          ["Stock", "int", false, false],
          ["StockMinimo", "int", false, false],
          ["StockReservado", "int", false, false],
          ["StockLibre", "int", true, false],
          ["BajoStock", "bool", true, false],
          ["BajaLogica", "bool", false, false] ] },
    { n: "ComponenteHistorico96VA", x: 40, y: 930, ester: "",
      at: [ ["IdHistorico", "int", false, false],
          ["CodigoComponente", "string", false, false],
          ["Fecha", "DateTime", false, false],
          ["Hora", "TimeSpan", false, false],
          ["Descripcion", "string", false, false],
          ["Tipo", "TipoComponente96VA", false, false],
          ["Marca", "string", false, false],
          ["Modelo", "string", false, false],
          ["PrecioUnitario", "decimal", false, false],
          ["Stock", "int", false, false],
          ["StockMinimo", "int", false, false],
          ["BajaLogica", "bool", false, false],
          ["Activo", "bool", false, false],
          ["FechaHora", "DateTime", true, false] ] },
    { n: "Computadora96VA", x: 340, y: 420, ester: "",
      at: [ ["Id", "int", false, false],
          ["Nombre", "string", false, false],
          ["TipoConfiguracion", "TipoConfiguracion96VA", false, false],
          ["Componentes", "List<Componente96VA>", false, false],
          ["ModeloOrigen", "ModeloEstandar96VA", false, false],
          ["PrecioTotal", "decimal", true, false] ] },
    { n: "ControlCalidad96VA", x: 940, y: 680, ester: "",
      at: [ ["Encendido", "bool", false, false],
          ["Conexiones", "bool", false, false],
          ["SistemaOperativo", "bool", false, false],
          ["Drivers", "bool", false, false],
          ["Observaciones", "string", false, false],
          ["Fecha", "DateTime?", false, false],
          ["Responsable", "string", false, false],
          ["Registrado", "bool", true, false],
          ["Aprobado", "bool", true, false] ] },
    { n: "DetalleComponente96VA", x: 940, y: 940, ester: "",
      at: [ ["Componente", "Componente96VA", false, false],
          ["Cantidad", "int", false, false] ] },
    { n: "FacturaCompra96VA", x: 1240, y: 1170, ester: "",
      at: [ ["NumeroFactura", "string", false, false],
          ["NumeroCompra", "string", false, false],
          ["FechaEmision", "DateTime", false, false],
          ["FechaEntrega", "DateTime", false, false],
          ["ComponentesRecibidos", "List<DetalleComponente96VA>", false, false],
          ["Total", "decimal", false, false],
          ["Observaciones", "string", false, false] ] },
    { n: "FacturaVenta96VA", x: 1240, y: 40, ester: "",
      at: [ ["NumeroFactura", "string", false, false],
          ["NumeroOrden", "int", false, false],
          ["NumeroVenta", "int", false, false],
          ["FechaEmision", "DateTime", false, false],
          ["Total", "decimal", false, false] ] },
    { n: "LineaEnsamblaje96VA", x: 1240, y: 290, ester: "",
      at: [ ["Id", "int", false, false],
          ["Nombre", "string", false, false],
          ["Descripcion", "string", false, false],
          ["Disponible", "bool", false, false] ] },
    { n: "ModeloEstandar96VA", x: 640, y: 520, ester: "",
      at: [ ["Id", "int", false, false],
          ["Nombre", "string", false, false],
          ["Descripcion", "string", false, false],
          ["Componentes", "List<Componente96VA>", false, false],
          ["PrecioTotal", "decimal", true, false] ] },
    { n: "OrdenCompra96VA", x: 1240, y: 940, ester: "",
      at: [ ["Id", "string", false, false],
          ["NumeroCompra", "int", false, false],
          ["ComponentesFaltantes", "List<DetalleComponente96VA>", false, false],
          ["FechaLimite", "DateTime", false, false],
          ["RepositorSolicitante", "Usuario96VA", false, false],
          ["Estado", "EstadoOrdenCompra96VA", false, false],
          ["FechaCierre", "DateTime?", false, false] ] },
    { n: "OrdenProduccion96VA", x: 940, y: 290, ester: "",
      at: [ ["NumeroOrden", "int", false, false],
          ["Venta", "Venta96VA", false, false],
          ["NumeroVenta", "int", false, false],
          ["FechaRegistro", "DateTime", false, false],
          ["FechaEntregaEstimada", "DateTime", false, false],
          ["Estado", "EstadoOrdenProduccion96VA", false, false],
          ["LineaEnsamblaje", "LineaEnsamblaje96VA", false, false],
          ["FechaInicioPrevista", "DateTime?", false, false],
          ["ResponsableTecnico", "string", false, false],
          ["ControlCalidad", "ControlCalidad96VA", false, false],
          ["NumeroSerie", "string", false, false],
          ["FechaCierre", "DateTime?", false, false],
          ["Cliente", "Cliente96VA", true, false],
          ["Computadora", "Computadora96VA", true, false],
          ["PrecioTotal", "decimal", true, false],
          ["TotalAbonado", "decimal", true, false],
          ["SaldoPendiente", "decimal", true, false] ] },
    { n: "Pago96VA", x: 640, y: 40, ester: "",
      at: [ ["Id", "int", false, false],
          ["NumeroVenta", "int", false, false],
          ["Tipo", "TipoPago96VA", false, false],
          ["NumeroRecibo", "string", false, false],
          ["Monto", "decimal", false, false],
          ["FormaPago", "FormaPago96VA", false, false],
          ["Referencia", "string", false, false],
          ["Fecha", "DateTime", false, false],
          ["Usuario", "string", false, false] ] },
    { n: "PedidoCotizacion96VA", x: 1540, y: 660, ester: "",
      at: [ ["Numero", "string", false, false],
          ["NumeroCompra", "string", false, false],
          ["ComponentesPedidos", "List<DetalleComponente96VA>", false, false],
          ["FechaEmision", "DateTime", false, false],
          ["Estado", "EstadoCotizacion96VA", false, false],
          ["Proveedor", "Proveedor96VA", false, false],
          ["Costo", "decimal", false, false],
          ["Condiciones", "string", false, false],
          ["GerenteAprobador", "Usuario96VA", false, false] ] },
    { n: "Proveedor96VA", x: 1840, y: 660, ester: "",
      at: [ ["Id", "int", false, false],
          ["Nombre", "string", false, false],
          ["Cuit", "string", false, false],
          ["Email", "string", false, false],
          ["Telefono", "string", false, false],
          ["Direccion", "string", false, false] ] },
    { n: "Recibo96VA", x: 640, y: 290, ester: "",
      at: [ ["Id", "string", false, false],
          ["Pago", "Pago96VA", false, false],
          ["Venta", "Venta96VA", false, false],
          ["FechaEmision", "DateTime", false, false],
          ["MontoAbonado", "decimal", false, false],
          ["SaldoPendiente", "decimal", false, false],
          ["FechaEntregaEstimada", "DateTime", false, false] ] },
    { n: "Venta96VA", x: 340, y: 40, ester: "",
      at: [ ["NumeroVenta", "int", false, false],
          ["Cliente", "Cliente96VA", false, false],
          ["Computadora", "Computadora96VA", false, false],
          ["FechaVenta", "DateTime", false, false],
          ["FechaEntregaEstimada", "DateTime", false, false],
          ["Estado", "EstadoVenta96VA", false, false],
          ["UsuarioRegistro", "string", false, false],
          ["NumeroOrdenProduccion", "int?", false, false],
          ["Pagos", "List<Pago96VA>", false, false],
          ["PrecioTotal", "decimal", true, false],
          ["PorcentajeSena", "decimal", false, true],
          ["MontoSenaRequerido", "decimal", true, false],
          ["Sena", "Pago96VA", true, false],
          ["TieneSena", "bool", true, false],
          ["TotalAbonado", "decimal", true, false],
          ["SaldoPendiente", "decimal", true, false],
          ["ListaParaProducir", "bool", true, false] ] },
    { n: "Usuario96VA", x: 1540, y: 380, ester: "SER",
      at: [ ["Dni", "string", false, false],
          ["Nombre", "string", false, false],
          ["Apellido", "string", false, false],
          ["IdRol", "string", false, false] ] }
];

var ENUMERACIONES = [
    { n: "TipoComponente96VA", x: 40, y: 620, lit: ["Procesador", "MemoriaRAM", "Disco", "PlacaMadre", "Fuente", "Gabinete", "PlacaDeVideo", "Refrigeracion", "Otro"] },
    { n: "TipoConfiguracion96VA", x: 40, y: 450, lit: ["Estandar", "Configurable"] },
    { n: "EstadoVenta96VA", x: 40, y: 250, lit: ["Pendiente", "Senada", "EnProduccion", "Entregada", "Anulada"] },
    { n: "EstadoOrdenProduccion96VA", x: 1240, y: 460, lit: ["Pendiente", "Planificada", "EnEnsamblaje", "Finalizada", "Entregada", "EnRevision"] },
    { n: "TipoPago96VA", x: 940, y: 40, lit: ["Sena", "SaldoFinal"] },
    { n: "FormaPago96VA", x: 940, y: 140, lit: ["Efectivo", "Transferencia", "Tarjeta"] },
    { n: "EstadoCotizacion96VA", x: 1840, y: 900, lit: ["PorAprobar", "Aprobado", "Desaprobada"] },
    { n: "EstadoOrdenCompra96VA", x: 1540, y: 1170, lit: ["Pendiente", "Enviada", "Finalizada", "RecibidaParcial"] }
];

// ══════════════════════════════════════════════════════════════════════════
//  RELACIONES ENTRE ENTIDADES
//  [origen, destino, tipo, cardOrigen, cardDestino, rol]
//    rol: la propiedad del código que implementa la relación.
//    tipo: "asociacion" | "composicion" | "agregacion"
//    En composición y agregación, el ORIGEN es la parte y el DESTINO el todo:
//    el rombo se dibuja del lado del destino.
// ══════════════════════════════════════════════════════════════════════════
var RELACIONES = [
    // ── Venta (RFN1) ──────────────────────────────────────────────────────
    ["Venta96VA",            "Cliente96VA",          "asociacion",  "0..*", "1",    "Cliente"],
    ["Computadora96VA",      "Venta96VA",            "composicion", "1",    "1",    "Computadora"],
    ["Pago96VA",             "Venta96VA",            "composicion", "0..2", "1",    "Pagos"],
    ["Recibo96VA",           "Pago96VA",             "asociacion",  "0..1", "1",    "Pago"],
    ["Recibo96VA",           "Venta96VA",            "asociacion",  "0..*", "1",    "Venta"],
    ["Componente96VA",       "Computadora96VA",      "agregacion",  "1..*", "0..*", "Componentes"],
    // Nueva: de qué modelo del catálogo salió la computadora (patrón Builder)
    ["Computadora96VA",      "ModeloEstandar96VA",   "asociacion",  "0..*", "0..1", "ModeloOrigen"],
    ["Componente96VA",       "ModeloEstandar96VA",   "agregacion",  "1..*", "0..*", "Componentes"],
    ["ComponenteHistorico96VA", "Componente96VA",    "asociacion",  "0..*", "1",    "CodigoComponente"],

    // ── Producción (RFN1) ─────────────────────────────────────────────────
    ["OrdenProduccion96VA",  "Venta96VA",            "asociacion",  "0..1", "1",    "Venta"],
    ["OrdenProduccion96VA",  "LineaEnsamblaje96VA",  "asociacion",  "0..*", "0..1", "LineaEnsamblaje"],
    ["ControlCalidad96VA",   "OrdenProduccion96VA",  "composicion", "1",    "1",    "ControlCalidad"],
    ["FacturaVenta96VA",     "OrdenProduccion96VA",  "asociacion",  "0..1", "1",    "NumeroOrden"],

    // ── Compras (RFN2) ────────────────────────────────────────────────────
    ["DetalleComponente96VA", "Componente96VA",      "asociacion",  "0..*", "1",    "Componente"],
    ["DetalleComponente96VA", "OrdenCompra96VA",     "composicion", "1..*", "1",    "ComponentesFaltantes"],
    ["DetalleComponente96VA", "PedidoCotizacion96VA","composicion", "1..*", "1",    "ComponentesPedidos"],
    ["DetalleComponente96VA", "FacturaCompra96VA",   "composicion", "1..*", "1",    "ComponentesRecibidos"],
    ["PedidoCotizacion96VA", "OrdenCompra96VA",      "asociacion",  "0..*", "1",    "NumeroCompra"],
    ["PedidoCotizacion96VA", "Proveedor96VA",        "asociacion",  "0..*", "1",    "Proveedor"],
    ["PedidoCotizacion96VA", "Usuario96VA",          "asociacion",  "0..*", "0..1", "GerenteAprobador"],
    ["OrdenCompra96VA",      "Usuario96VA",          "asociacion",  "0..*", "1",    "RepositorSolicitante"],
    ["FacturaCompra96VA",    "OrdenCompra96VA",      "asociacion",  "0..*", "1",    "NumeroCompra"]
];

// Dependencias «use» hacia las enumeraciones.
var USA_ENUM = [
    ["Componente96VA",          "TipoComponente96VA"],
    ["ComponenteHistorico96VA", "TipoComponente96VA"],
    ["Computadora96VA",         "TipoConfiguracion96VA"],
    ["Venta96VA",               "EstadoVenta96VA"],
    ["OrdenProduccion96VA",     "EstadoOrdenProduccion96VA"],
    ["Pago96VA",                "TipoPago96VA"],
    ["Pago96VA",                "FormaPago96VA"],
    ["PedidoCotizacion96VA",    "EstadoCotizacion96VA"],
    ["OrdenCompra96VA",         "EstadoOrdenCompra96VA"]
];

// ══════════════════════════════════════════════════════════════════════════
//  PATRÓN BUILDER (capa BLL, carpeta Armado)
//  op: [visibilidad, firma, abstracta]
// ══════════════════════════════════════════════════════════════════════════
var BUILDER = {
    clases: [
        { n: "IComputadoraBuilder96VA", tipo: "Interface", x: 380, y: 40, abstracta: false,
          rol: "Builder", at: [],
          op: [["Public", "Reiniciar() : void", false],
               ["Public", "ColocarProcesador(procesador : Componente96VA) : void", false],
               ["Public", "ColocarPlacaMadre(placaMadre : Componente96VA) : void", false],
               ["Public", "ColocarMemoriaRam(memoria : Componente96VA) : void", false],
               ["Public", "ColocarAlmacenamiento(disco : Componente96VA) : void", false],
               ["Public", "ColocarPlacaDeVideo(placaDeVideo : Componente96VA) : void", false],
               ["Public", "ColocarFuente(fuente : Componente96VA) : void", false],
               ["Public", "ColocarGabinete(gabinete : Componente96VA) : void", false],
               ["Public", "ColocarRefrigeracion(refrigeracion : Componente96VA) : void", false],
               ["Public", "ColocarAdicional(adicional : Componente96VA) : void", false],
               ["Public", "ObtenerComputadora() : Computadora96VA", false]] },

        { n: "ComputadoraBuilderBase96VA", tipo: "Class", x: 380, y: 340, abstracta: true,
          rol: "Builder (base con las reglas comunes)",
          at: [["Private", "_bahias", "Dictionary<TipoComponente96VA, List<Componente96VA>>"]],
          op: [["Public", "Reiniciar() : void", false],
               ["Public", "ColocarProcesador(procesador : Componente96VA) : void", false],
               ["Public", "ColocarPlacaMadre(placaMadre : Componente96VA) : void", false],
               ["Public", "ColocarMemoriaRam(memoria : Componente96VA) : void", false],
               ["Public", "ColocarAlmacenamiento(disco : Componente96VA) : void", false],
               ["Public", "ColocarPlacaDeVideo(placaDeVideo : Componente96VA) : void", false],
               ["Public", "ColocarFuente(fuente : Componente96VA) : void", false],
               ["Public", "ColocarGabinete(gabinete : Componente96VA) : void", false],
               ["Public", "ColocarRefrigeracion(refrigeracion : Componente96VA) : void", false],
               ["Public", "ColocarAdicional(adicional : Componente96VA) : void", false],
               ["Public", "ObtenerComputadora() : Computadora96VA", false],
               ["Protected", "TipoDeConfiguracion() : TipoConfiguracion96VA", true],
               ["Protected", "NombrarEquipo(piezas : IReadOnlyList<Componente96VA>) : string", true],
               ["Protected", "ModeloDeOrigen() : ModeloEstandar96VA", false],
               ["Private", "Colocar(bahia : TipoComponente96VA, pieza : Componente96VA) : void", false]] },

        { n: "ComputadoraEstandarBuilder96VA", tipo: "Class", x: 200, y: 720, abstracta: false,
          rol: "ConcreteBuilder",
          at: [["Private", "_modelo", "ModeloEstandar96VA"]],
          op: [["Public", "ComputadoraEstandarBuilder96VA(modelo : ModeloEstandar96VA)", false],
               ["Protected", "TipoDeConfiguracion() : TipoConfiguracion96VA", false],
               ["Protected", "NombrarEquipo(piezas : IReadOnlyList<Componente96VA>) : string", false],
               ["Protected", "ModeloDeOrigen() : ModeloEstandar96VA", false]] },

        { n: "ComputadoraConfigurableBuilder96VA", tipo: "Class", x: 620, y: 720, abstracta: false,
          rol: "ConcreteBuilder", at: [],
          op: [["Protected", "TipoDeConfiguracion() : TipoConfiguracion96VA", false],
               ["Protected", "NombrarEquipo(piezas : IReadOnlyList<Componente96VA>) : string", false]] },

        { n: "ArmadorComputadora96VA", tipo: "Class", x: 1000, y: 40, abstracta: false,
          rol: "Director",
          at: [["Private", "_builder", "IComputadoraBuilder96VA"],
               ["Private", "Receta", "PasoArmado96VA[]"]],
          op: [["Public", "ArmadorComputadora96VA(builder : IComputadoraBuilder96VA)", false],
               ["Public", "Armar(piezas : IEnumerable<Componente96VA>) : void", false],
               ["Public", "ArmarDesdeModelo(modelo : ModeloEstandar96VA) : void", false],
               ["Public", "Pasos() : IReadOnlyList<PasoArmado96VA>", false],
               ["Public", "EsObligatorio(tipo : TipoComponente96VA) : bool", false],
               ["Public", "AdmiteVarios(tipo : TipoComponente96VA) : bool", false],
               ["Public", "Faltantes(piezas : IEnumerable<Componente96VA>) : List<TipoComponente96VA>", false]] },

        { n: "PasoArmado96VA", tipo: "Class", x: 1480, y: 40, abstracta: false,
          rol: "", at: [["Public", "Tipo", "TipoComponente96VA"],
                        ["Public", "Obligatorio", "bool"],
                        ["Public", "AdmiteVarios", "bool"]], op: [] },

        // El Product es la MISMA Computadora96VA del diagrama general: no se
        // duplica el elemento, se lo vuelve a poner en este diagrama.
        { n: "Computadora96VA", reusar: true, x: 1000, y: 460, rol: "Product" },

        { n: "VentasBLL96VA", tipo: "Class", x: 1480, y: 300, abstracta: false,
          rol: "Client",
          at: [],
          op: [["Public", "ArmarComputadora(tipo : TipoConfiguracion96VA, modelo : ModeloEstandar96VA, piezasElegidas : IEnumerable<Componente96VA>) : Computadora96VA", false],
               ["Public", "RegistrarVenta(cliente : Cliente96VA, computadora : Computadora96VA, fechaEntregaEstimada : DateTime) : Venta96VA", false]] },

        { n: "ModelosEstandarBLL96VA", tipo: "Class", x: 1480, y: 520, abstracta: false,
          rol: "Client",
          at: [],
          op: [["Public", "Crear(modelo : ModeloEstandar96VA) : void", false],
               ["Public", "Modificar(modelo : ModeloEstandar96VA) : void", false],
               ["Private", "Validar(modelo : ModeloEstandar96VA) : void", false]] }
    ],

    // [origen, destino, tipo, etiqueta]
    relaciones: [
        ["ComputadoraBuilderBase96VA",         "IComputadoraBuilder96VA",   "Realisation",    ""],
        ["ComputadoraEstandarBuilder96VA",     "ComputadoraBuilderBase96VA","Generalization", ""],
        ["ComputadoraConfigurableBuilder96VA", "ComputadoraBuilderBase96VA","Generalization", ""],
        ["ArmadorComputadora96VA",             "IComputadoraBuilder96VA",   "Association",    "builder"],
        ["ArmadorComputadora96VA",             "PasoArmado96VA",            "Association",    "receta"],
        ["ComputadoraBuilderBase96VA",         "Computadora96VA",           "Dependency",     "«create»"],
        ["VentasBLL96VA",                      "ArmadorComputadora96VA",    "Dependency",     "«use»"],
        ["VentasBLL96VA",                      "ComputadoraEstandarBuilder96VA",     "Dependency", "«create»"],
        ["VentasBLL96VA",                      "ComputadoraConfigurableBuilder96VA", "Dependency", "«create»"],
        ["ModelosEstandarBLL96VA",             "ArmadorComputadora96VA",    "Dependency",     "«use»"],
        ["ModelosEstandarBLL96VA",             "ComputadoraEstandarBuilder96VA",     "Dependency", "«create»"]
    ],

    nota: "PATRÓN BUILDER (GoF) — roles en PCFORGE\n\n" +
          "Builder ........... IComputadoraBuilder96VA\n" +
          "ConcreteBuilder ... ComputadoraEstandarBuilder96VA\n" +
          "                    ComputadoraConfigurableBuilder96VA\n" +
          "Director .......... ArmadorComputadora96VA\n" +
          "Product ........... Computadora96VA\n" +
          "Client ............ VentasBLL96VA, ModelosEstandarBLL96VA\n\n" +
          "El Director coloca las piezas siempre en el mismo orden (su receta).\n" +
          "Con el builder estándar sale un equipo de catálogo, con nombre y\n" +
          "ModeloOrigen; con el configurable, un equipo a medida nombrado\n" +
          "por su procesador y su placa de video."
};

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

/** Alto de una caja según cuántas filas tiene. */
function altoCaja(filas) { return 46 + Math.max(1, filas) * 15; }

/**
 * Atributo. "derivado" = propiedad calculada del código (se muestra con "/").
 * Si la versión de EA no expone IsDerived, se antepone la barra al nombre.
 */
function atributo(elem, nombre, tipo, visibilidad, derivado, constante) {
    var a = elem.Attributes.AddNew(nombre, tipo == null ? "" : tipo);
    a.Visibility = visibilidad;
    if (constante) { try { a.IsConst = true; a.IsStatic = true; } catch (eC) {} }
    a.Update();
    if (derivado) {
        try { a.IsDerived = true; a.Update(); }
        catch (eD) { a.Name = "/" + nombre; a.Update(); }
    }
    elem.Attributes.Refresh();
    return a;
}

/** Operación a partir de su firma: "Nombre(p : Tipo, q : Tipo) : Retorno". */
function operacion(elem, visibilidad, firma, abstracta) {
    var iAbre = firma.indexOf("("), iCierra = firma.lastIndexOf(")");
    var nombre = trim(firma.substring(0, iAbre));
    var params = firma.substring(iAbre + 1, iCierra);
    var cola = firma.substring(iCierra + 1);
    var retorno = "";
    var iDosP = cola.indexOf(":");
    if (iDosP >= 0) retorno = trim(cola.substring(iDosP + 1));

    var m = elem.Methods.AddNew(nombre, retorno);
    m.Visibility = visibilidad;
    if (abstracta) m.Abstract = true;
    m.Update();
    elem.Methods.Refresh();

    if (trim(params) != "") {
        // Se corta por comas de nivel 0 (no las de Dictionary<A, B>).
        var partes = [], nivel = 0, actual = "";
        for (var i = 0; i < params.length; i++) {
            var ch = params.charAt(i);
            if (ch == "<") nivel++;
            if (ch == ">") nivel--;
            if (ch == "," && nivel == 0) { partes.push(actual); actual = ""; }
            else actual += ch;
        }
        if (trim(actual) != "") partes.push(actual);

        for (var k = 0; k < partes.length; k++) {
            var nt = trim(partes[k]).split(" : ");
            var par = m.Parameters.AddNew(trim(nt[0]), nt.length > 1 ? trim(nt[1]) : "");
            par.Position = k;
            par.Update();
        }
        m.Parameters.Refresh();
        m.Update();
    }
    return m;
}

/** Relación entre entidades, con cardinalidades y el rol que la implementa. */
function relacionar(origen, destino, tipo, cardOrigen, cardDestino, rol) {
    var esTodoParte = (tipo == "composicion" || tipo == "agregacion");
    var c = origen.Connectors.AddNew("", esTodoParte ? "Aggregation" : "Association");
    c.SupplierID = destino.ElementID;
    c.ClientEnd.Cardinality = cardOrigen;
    c.SupplierEnd.Cardinality = cardDestino;

    if (esTodoParte) {
        // La parte (origen) apunta al todo (destino); el rombo va en el todo.
        c.Subtype = (tipo == "composicion") ? "Strong" : "Weak";
        c.SupplierEnd.Aggregation = (tipo == "composicion") ? 2 : 1;
        if (rol) c.ClientEnd.Role = rol;
    } else {
        c.Direction = "Source -> Destination";
        if (rol) c.SupplierEnd.Role = rol;
    }
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

/** Conector simple (dependencias, realizaciones, generalizaciones). */
function conectar(origen, destino, tipo, nombre) {
    var c = origen.Connectors.AddNew("", tipo);
    c.SupplierID = destino.ElementID;
    if (nombre != null && nombre != "") {
        if (nombre.charAt(0) == "«") c.Stereotype = nombre.substring(1, nombre.length - 1);
        else c.Name = nombre;
    }
    c.Update();
    origen.Connectors.Refresh();
    return c;
}

// ══════════════════════════════════════════════════════════════════════════
//  GENERADORES
// ══════════════════════════════════════════════════════════════════════════

/** Diagrama de clases general: entidades de negocio + enumeraciones. */
function generarGeneral(pkg) {
    var dia = nuevoDiagrama(pkg, "Diagrama de clases general (BE)", "Logical");
    var mapa = {};
    var totalAt = 0;

    for (var i = 0; i < CLASES.length; i++) {
        var cl = CLASES[i];
        var e = nuevoElemento(pkg, cl.n, "Class", cl.ester, "");
        for (var a = 0; a < cl.at.length; a++) {
            var at = cl.at[a];
            atributo(e, at[0], at[1], "Private", at[2], at[3]);
            totalAt++;
        }
        operacion(e, "Public", "ToString() : string", false);
        e.Update();
        ponerEnDiagrama(dia, e, cl.x, cl.y, CLASE_ANCHO, altoCaja(cl.at.length + 1));
        mapa[cl.n] = e;
    }

    for (var n = 0; n < ENUMERACIONES.length; n++) {
        var en = ENUMERACIONES[n];
        var ee = nuevoElemento(pkg, en.n, "Enumeration", "", "");
        for (var l = 0; l < en.lit.length; l++) atributo(ee, en.lit[l], "", "Public", false, false);
        ee.Update();
        ponerEnDiagrama(dia, ee, en.x, en.y, ENUM_ANCHO, altoCaja(en.lit.length));
        mapa[en.n] = ee;
    }

    var rel = 0;
    for (var r = 0; r < RELACIONES.length; r++) {
        var x = RELACIONES[r];
        if (mapa[x[0]] == null || mapa[x[1]] == null) {
            log("    ! relacion ignorada: " + x[0] + " -> " + x[1]);
            continue;
        }
        relacionar(mapa[x[0]], mapa[x[1]], x[2], x[3], x[4], x[5]);
        rel++;
    }

    var usos = 0;
    for (var u = 0; u < USA_ENUM.length; u++) {
        var y = USA_ENUM[u];
        if (mapa[y[0]] == null || mapa[y[1]] == null) {
            log("    ! «use» ignorado: " + y[0] + " -> " + y[1]);
            continue;
        }
        conectar(mapa[y[0]], mapa[y[1]], "Dependency", "«use»");
        usos++;
    }

    log("  Diagrama general: " + CLASES.length + " clases, " + ENUMERACIONES.length +
        " enumeraciones, " + totalAt + " atributos, " + rel + " relaciones, " + usos + " «use».");
    return mapa;
}

/** Diagrama del patrón Builder. Reutiliza la Computadora96VA del general. */
function generarBuilder(pkg, mapaGeneral) {
    var dia = nuevoDiagrama(pkg, "Patron Builder - armado de computadoras", "Logical");
    var mapa = {};

    for (var i = 0; i < BUILDER.clases.length; i++) {
        var cl = BUILDER.clases[i];
        var e;
        var filas;

        if (cl.reusar) {
            e = mapaGeneral[cl.n];
            if (e == null) { log("    ! no se encontro " + cl.n + " para reutilizar"); continue; }
            filas = e.Attributes.Count + e.Methods.Count;
        } else {
            e = nuevoElemento(pkg, cl.n, cl.tipo, "", cl.rol ? "Rol en el patron: " + cl.rol : "");
            if (cl.abstracta) { e.Abstract = "1"; e.Update(); }
            for (var a = 0; a < cl.at.length; a++)
                atributo(e, cl.at[a][1], cl.at[a][2], cl.at[a][0], false, false);
            for (var o = 0; o < cl.op.length; o++)
                operacion(e, cl.op[o][0], cl.op[o][1], cl.op[o][2]);
            e.Update();
            filas = cl.at.length + cl.op.length;
        }

        var ancho = (cl.n == "VentasBLL96VA") ? 520 : 360;
        ponerEnDiagrama(dia, e, cl.x, cl.y, ancho, altoCaja(filas));
        mapa[cl.n] = e;
    }

    var hechas = 0;
    for (var r = 0; r < BUILDER.relaciones.length; r++) {
        var x = BUILDER.relaciones[r];
        if (mapa[x[0]] == null || mapa[x[1]] == null) {
            log("    ! relacion ignorada: " + x[0] + " -> " + x[1]);
            continue;
        }
        conectar(mapa[x[0]], mapa[x[1]], x[2], x[3]);
        hechas++;
    }

    var nota = nuevoElemento(pkg, "", "Note", "", BUILDER.nota);
    ponerEnDiagrama(dia, nota, 40, 1060, 520, 200);

    log("  Patron Builder: " + BUILDER.clases.length + " clases, " + hechas + " relaciones.");
}

// ══════════════════════════════════════════════════════════════════════════
//  PROGRAMA PRINCIPAL
// ══════════════════════════════════════════════════════════════════════════

function main() {
    try { Repository.EnsureOutputVisible("Script"); } catch (eOut) {}
    Session.Output("=======================================================");
    Session.Output(" PCFORGE - Diagrama de clases general + patron Builder");
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

    var pkgBE = nuevoPaquete(raiz, "BE - Entidades de negocio");
    var mapa = generarGeneral(pkgBE);

    var pkgBuilder = nuevoPaquete(raiz, "BLL - Armado (patron Builder)");
    generarBuilder(pkgBuilder, mapa);

    try { Repository.RefreshModelView(0); } catch (eRef) {}

    log("");
    log("=======================================================");
    log(" LISTO. Se generaron 2 diagramas en:");
    log("   " + nombre);
    log("=======================================================");
}

main();
