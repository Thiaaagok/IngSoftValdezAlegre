-- ============================================================================
--  40_refactor_dominio_06AV.sql
--  Recrea el esquema afectado por el refactor del modelo de dominio:
--   - Insumos se fusiona en Componentes (Stock + StockMinimo).
--   - Computadoras, OrdenesProduccion, OrdenesCompra y Pagos usan PK string (Id).
--   - Pago referencia a Computadora (no a la orden).
--   - Cotizacion referencia a OrdenCompra por Id y guarda el gerente aprobador.
--   - Tablas nuevas: FacturaCompra(+detalle), FacturaVenta, Recibo.
--
--  ⚠ DESTRUCTIVO: dropea y recrea tablas. Correr sobre una base a recrear.
--     Requiere que YA existan: Usuarios, Clientes, Proveedores, ModelosEstandar,
--     LineasEnsamblaje (esquema base + PC Factory).
--
--  ⚠ Los procedimientos almacenados (sp_Componentes_*, sp_Compras_*,
--     sp_Produccion_*, etc.) deben regenerarse para reflejar estas columnas.
-- ============================================================================
SET NOCOUNT ON;
GO

-- ── 1) DROP en orden de dependencia (hijas primero) ─────────────────────────
IF OBJECT_ID('Recibo','U')                  IS NOT NULL DROP TABLE Recibo;
IF OBJECT_ID('FacturaVenta','U')            IS NOT NULL DROP TABLE FacturaVenta;
IF OBJECT_ID('FacturaCompraDetalle','U')    IS NOT NULL DROP TABLE FacturaCompraDetalle;
IF OBJECT_ID('FacturaCompra','U')           IS NOT NULL DROP TABLE FacturaCompra;
IF OBJECT_ID('Pagos','U')                   IS NOT NULL DROP TABLE Pagos;
IF OBJECT_ID('PedidoCotizacionDetalle','U') IS NOT NULL DROP TABLE PedidoCotizacionDetalle;
IF OBJECT_ID('PedidosCotizacion','U')       IS NOT NULL DROP TABLE PedidosCotizacion;
IF OBJECT_ID('OrdenCompraDetalle','U')      IS NOT NULL DROP TABLE OrdenCompraDetalle;
IF OBJECT_ID('OrdenesCompra','U')           IS NOT NULL DROP TABLE OrdenesCompra;
IF OBJECT_ID('OrdenesProduccion','U')       IS NOT NULL DROP TABLE OrdenesProduccion;
IF OBJECT_ID('ComputadoraComponentes','U')  IS NOT NULL DROP TABLE ComputadoraComponentes;
IF OBJECT_ID('Computadoras','U')            IS NOT NULL DROP TABLE Computadoras;
IF OBJECT_ID('ModeloEstandarComponentes','U') IS NOT NULL DROP TABLE ModeloEstandarComponentes;
IF OBJECT_ID('Insumos','U')                 IS NOT NULL DROP TABLE Insumos;   -- fusionado en Componentes
IF OBJECT_ID('Componentes','U')             IS NOT NULL DROP TABLE Componentes;

IF OBJECT_ID('Seq_NumeroCompra','SO') IS NOT NULL DROP SEQUENCE Seq_NumeroCompra;
IF OBJECT_ID('Seq_NumeroOrden','SO')  IS NOT NULL DROP SEQUENCE Seq_NumeroOrden;
GO

-- Secuencias para los "números de negocio" visibles (correlativos)
CREATE SEQUENCE Seq_NumeroCompra AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE Seq_NumeroOrden  AS INT START WITH 1 INCREMENT BY 1;
GO

-- ── 2) Componentes (absorbe Insumo: Stock + StockMinimo) ─────────────────────
CREATE TABLE Componentes (
    Codigo         NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Descripcion    NVARCHAR(200) NOT NULL,
    Marca          NVARCHAR(100) NULL,
    Modelo         NVARCHAR(100) NULL,
    PrecioUnitario DECIMAL(18,2) NOT NULL CONSTRAINT DF_Comp_Precio DEFAULT (0),
    Stock          INT           NOT NULL CONSTRAINT DF_Comp_Stock  DEFAULT (0),
    StockMinimo    INT           NOT NULL CONSTRAINT DF_Comp_StockMin DEFAULT (0),
    Tipo           INT           NOT NULL CONSTRAINT DF_Comp_Tipo   DEFAULT (0)   -- TipoComponente06AV
);
GO

-- ModeloEstandarComponentes (FK a Componentes; el modelo sigue con Id INT)
CREATE TABLE ModeloEstandarComponentes (
    IdModelo         INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_MEC PRIMARY KEY (IdModelo, CodigoComponente),
    CONSTRAINT FK_MEC_Modelo     FOREIGN KEY (IdModelo)         REFERENCES ModelosEstandar(Id),
    CONSTRAINT FK_MEC_Componente FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── 3) Computadoras (PK string, + Cliente + ModeloOrigen) ────────────────────
CREATE TABLE Computadoras (
    Id                NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "PC-2026-0001"
    DniCliente        NVARCHAR(20)  NOT NULL,               -- FK Clientes
    Nombre            NVARCHAR(200) NULL,
    TipoConfiguracion INT           NOT NULL,               -- 0 Estandar, 1 Configurable
    IdModeloOrigen    INT           NULL,                   -- FK ModelosEstandar (solo si Estandar)
    PrecioTotal       DECIMAL(18,2) NOT NULL CONSTRAINT DF_PC_Precio DEFAULT (0),
    CONSTRAINT FK_PC_Cliente FOREIGN KEY (DniCliente)     REFERENCES Clientes(Dni),
    CONSTRAINT FK_PC_Modelo  FOREIGN KEY (IdModeloOrigen) REFERENCES ModelosEstandar(Id)
);
GO

CREATE TABLE ComputadoraComponentes (
    IdComputadora    NVARCHAR(30) NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    Orden            INT          NOT NULL CONSTRAINT DF_CC_Orden DEFAULT (0),
    CONSTRAINT PK_CC PRIMARY KEY (IdComputadora, CodigoComponente, Orden),
    CONSTRAINT FK_CC_PC   FOREIGN KEY (IdComputadora)    REFERENCES Computadoras(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CC_Comp FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── 4) Pagos (PK string, FK a Computadora en vez de a la orden) ──────────────
CREATE TABLE Pagos (
    Id            NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "PG-..."
    IdComputadora NVARCHAR(30)  NOT NULL,               -- FK Computadoras (la seña se cobra antes de la orden)
    Tipo          INT           NOT NULL,               -- 0 Sena, 1 SaldoFinal
    Monto         DECIMAL(18,2) NOT NULL,
    Fecha         DATETIME      NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (GETDATE()),
    CONSTRAINT FK_Pago_PC FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id)
);
GO

-- ── 5) OrdenesProduccion (PK string Id + NumeroOrden correlativo) ────────────
CREATE TABLE OrdenesProduccion (
    Id                  NVARCHAR(30) NOT NULL PRIMARY KEY,   -- "OP-2026-0001"
    NumeroOrden         INT          NOT NULL CONSTRAINT DF_OP_Num DEFAULT (NEXT VALUE FOR Seq_NumeroOrden),
    IdComputadora       NVARCHAR(30) NOT NULL,               -- FK Computadoras (1 orden por computadora)
    FechaEntrega        DATE         NOT NULL,
    Estado              INT          NOT NULL CONSTRAINT DF_OP_Estado DEFAULT (0),  -- EstadoOrdenProduccion06AV
    IdLinea             INT          NULL,                   -- FK LineasEnsamblaje
    FechaInicioPrevista DATE         NULL,
    ResponsableTecnico  NVARCHAR(150) NULL,
    FechaCierre         DATETIME     NULL,
    CONSTRAINT UQ_OP_Numero UNIQUE (NumeroOrden),
    CONSTRAINT UQ_OP_PC     UNIQUE (IdComputadora),          -- una computadora => una sola orden
    CONSTRAINT FK_OP_PC     FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_OP_Linea  FOREIGN KEY (IdLinea)       REFERENCES LineasEnsamblaje(Id)
);
GO

-- ── 6) OrdenesCompra (PK string Id + NumeroCompra correlativo) ───────────────
CREATE TABLE OrdenesCompra (
    Id           NVARCHAR(30) NOT NULL PRIMARY KEY,   -- "OC-2026-0001"
    NumeroCompra INT          NOT NULL CONSTRAINT DF_OC_Num DEFAULT (NEXT VALUE FOR Seq_NumeroCompra),
    FechaLimite  DATE         NOT NULL,
    DniRepositor NVARCHAR(20) NULL,                   -- FK Usuarios (repositor solicitante)
    Estado       INT          NOT NULL CONSTRAINT DF_OC_Estado DEFAULT (0),  -- EstadoOrdenCompra06AV
    FechaCierre  DATETIME     NULL,
    CONSTRAINT UQ_OC_Numero UNIQUE (NumeroCompra),
    CONSTRAINT FK_OC_Repositor FOREIGN KEY (DniRepositor) REFERENCES Usuarios(Dni)
);
GO

CREATE TABLE OrdenCompraDetalle (
    IdOrdenCompra    NVARCHAR(30) NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    Cantidad         INT          NOT NULL,
    CONSTRAINT PK_OCD PRIMARY KEY (IdOrdenCompra, CodigoComponente),
    CONSTRAINT FK_OCD_OC   FOREIGN KEY (IdOrdenCompra)    REFERENCES OrdenesCompra(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OCD_Comp FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── 7) PedidosCotizacion (FK a OC por Id, 1..N por orden, + gerente aprobador) ─
CREATE TABLE PedidosCotizacion (
    Numero              NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "CO-2026-0001"
    IdOrdenCompra       NVARCHAR(30)  NOT NULL,               -- FK OrdenesCompra
    IdProveedor         INT           NOT NULL,               -- FK Proveedores
    Estado              INT           NOT NULL CONSTRAINT DF_CO_Estado DEFAULT (0),  -- EstadoCotizacion06AV
    Costo               DECIMAL(18,2) NOT NULL CONSTRAINT DF_CO_Costo DEFAULT (0),
    Condiciones         NVARCHAR(500) NULL,
    FechaEmision        DATETIME      NOT NULL CONSTRAINT DF_CO_Fecha DEFAULT (GETDATE()),
    DniGerenteAprobador NVARCHAR(20)  NULL,                   -- FK Usuarios (null hasta aprobar/desaprobar)
    CONSTRAINT FK_CO_OC        FOREIGN KEY (IdOrdenCompra)       REFERENCES OrdenesCompra(Id),
    CONSTRAINT FK_CO_Proveedor FOREIGN KEY (IdProveedor)        REFERENCES Proveedores(Id),
    CONSTRAINT FK_CO_Gerente   FOREIGN KEY (DniGerenteAprobador) REFERENCES Usuarios(Dni)
);
GO

-- ── 8) FacturaCompra (recepción de mercadería) + detalle ─────────────────────
CREATE TABLE FacturaCompra (
    NumeroFactura NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "FC-2026-0001"
    IdOrdenCompra NVARCHAR(30)  NOT NULL,               -- FK OrdenesCompra
    FechaEmision  DATETIME      NOT NULL,
    FechaEntrega  DATETIME      NOT NULL,
    Total         DECIMAL(18,2) NOT NULL CONSTRAINT DF_FC_Total DEFAULT (0),
    Observaciones NVARCHAR(1000) NULL,                  -- diferencias pedido vs recibido
    CONSTRAINT FK_FC_OC FOREIGN KEY (IdOrdenCompra) REFERENCES OrdenesCompra(Id)
);
GO

CREATE TABLE FacturaCompraDetalle (
    NumeroFactura    NVARCHAR(30) NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    Cantidad         INT          NOT NULL,             -- cantidad REAL recibida
    CONSTRAINT PK_FCD PRIMARY KEY (NumeroFactura, CodigoComponente),
    CONSTRAINT FK_FCD_FC   FOREIGN KEY (NumeroFactura)    REFERENCES FacturaCompra(NumeroFactura) ON DELETE CASCADE,
    CONSTRAINT FK_FCD_Comp FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── 9) FacturaVenta (Total snapshot) ─────────────────────────────────────────
CREATE TABLE FacturaVenta (
    NumeroFactura    NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "FV-2026-0001"
    IdOrdenProduccion NVARCHAR(30) NOT NULL,               -- FK OrdenesProduccion
    FechaEmision     DATETIME      NOT NULL,
    Total            DECIMAL(18,2) NOT NULL,               -- SNAPSHOT del PrecioTotal al emitir
    CONSTRAINT UQ_FV_OP UNIQUE (IdOrdenProduccion),        -- una factura por orden
    CONSTRAINT FK_FV_OP FOREIGN KEY (IdOrdenProduccion) REFERENCES OrdenesProduccion(Id)
);
GO

-- ── 10) Recibo (seña; montos snapshot) ───────────────────────────────────────
CREATE TABLE Recibo (
    Id                   NVARCHAR(30)  NOT NULL PRIMARY KEY,   -- "RC-2026-0001"
    IdPago               NVARCHAR(30)  NOT NULL,               -- FK Pagos (la seña que lo originó)
    FechaEmision         DATETIME      NOT NULL,
    MontoAbonado         DECIMAL(18,2) NOT NULL,               -- snapshot
    SaldoPendiente       DECIMAL(18,2) NOT NULL,               -- snapshot
    FechaEntregaEstimada DATE          NULL,
    CONSTRAINT UQ_RC_Pago UNIQUE (IdPago),
    CONSTRAINT FK_RC_Pago FOREIGN KEY (IdPago) REFERENCES Pagos(Id)
);
GO

PRINT 'Esquema de dominio recreado. Regenerar los procedimientos almacenados asociados.';
GO
