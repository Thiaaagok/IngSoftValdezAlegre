-- ============================================================
--  26_pcfactory_compras.sql   (RFN2: COMPRA DE INSUMOS)
--
--  Reescrito tras la FUSIÓN Componente + Insumo: lo que se compra ya no es
--  un "insumo" aparte, es el mismo Componente06AV (Stock + StockMinimo).
--
--  Circuito: el sistema detecta componentes en o por debajo del mínimo →
--  el repositor arma la ORDEN DE COMPRA → pide COTIZACIÓN a un proveedor →
--  el gerente de compras la aprueba o desaprueba → al recibir la mercadería
--  se registra la FACTURA DE COMPRA y se suma el stock realmente recibido.
--
--  Claves: Id string legible (OC-2026-0001) + número de negocio correlativo.
--  Estados de OC (int):  0 Pendiente, 1 Enviada, 2 Finalizada.
--  Estados de cotización (int): 0 PorAprobar, 1 Aprobado, 2 Desaprobada.
--
--  Requiere: Componentes (21), Proveedores (23) y Usuarios (00_schema).
-- ============================================================

SET NOCOUNT ON;
GO

-- ── Limpieza del modelo viejo basado en Insumos ──────────────
IF OBJECT_ID('FacturaCompraDetalle','U')    IS NOT NULL DROP TABLE FacturaCompraDetalle;
IF OBJECT_ID('FacturaCompra','U')           IS NOT NULL DROP TABLE FacturaCompra;
IF OBJECT_ID('PedidoCotizacionDetalle','U') IS NOT NULL DROP TABLE PedidoCotizacionDetalle;
IF OBJECT_ID('PedidosCotizacion','U')       IS NOT NULL DROP TABLE PedidosCotizacion;
IF OBJECT_ID('OrdenCompraDetalle','U')      IS NOT NULL DROP TABLE OrdenCompraDetalle;
IF OBJECT_ID('OrdenesCompra','U')           IS NOT NULL DROP TABLE OrdenesCompra;
IF OBJECT_ID('Seq_NumeroCompra','SO')       IS NOT NULL DROP SEQUENCE Seq_NumeroCompra;
GO

-- Secuencia del número de compra visible al usuario
CREATE SEQUENCE Seq_NumeroCompra AS INT START WITH 1 INCREMENT BY 1;
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

-- ============================================================
--  SPs · Compras
-- ============================================================
CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Agregar
    @Id NVARCHAR(30), @FechaLimite DATE, @DniRepositor NVARCHAR(20), @NumeroCompra INT OUTPUT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado)
    VALUES (@Id, @FechaLimite, @DniRepositor, 0);
    SELECT @NumeroCompra = NumeroCompra FROM OrdenesCompra WHERE Id=@Id;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenCompra_AgregarDetalle
    @IdOrdenCompra NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad)
    VALUES (@IdOrdenCompra, @CodigoComponente, @Cantidad);
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_CambiarEstado @Id NVARCHAR(30), @Estado INT AS
BEGIN SET NOCOUNT ON; UPDATE OrdenesCompra SET Estado=@Estado WHERE Id=@Id; END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Cerrar @Id NVARCHAR(30), @FechaCierre DATETIME AS
BEGIN SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado=2 /*Finalizada*/, FechaCierre=@FechaCierre WHERE Id=@Id;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Id, NumeroCompra, FechaLimite, DniRepositor, Estado, FechaCierre
    FROM OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenCompra_ObtenerDetalle @IdOrdenCompra NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT d.CodigoComponente, d.Cantidad, c.Descripcion, c.Marca, c.Modelo, c.PrecioUnitario,
           c.Stock, c.StockMinimo, c.Tipo
    FROM OrdenCompraDetalle d JOIN Componentes c ON c.Codigo=d.CodigoComponente
    WHERE d.IdOrdenCompra=@IdOrdenCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_Agregar
    @Numero NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @IdProveedor INT,
    @Costo DECIMAL(18,2), @Condiciones NVARCHAR(500) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones)
    VALUES (@Numero, @IdOrdenCompra, @IdProveedor, 0 /*PorAprobar*/, @Costo, @Condiciones);
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_CambiarEstado
    @Numero NVARCHAR(30), @Estado INT, @DniGerenteAprobador NVARCHAR(20) AS
BEGIN SET NOCOUNT ON;
    UPDATE PedidosCotizacion SET Estado=@Estado, DniGerenteAprobador=@DniGerenteAprobador WHERE Numero=@Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerPorOrden @IdOrdenCompra NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador
    FROM PedidosCotizacion WHERE IdOrdenCompra=@IdOrdenCompra ORDER BY FechaEmision DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador
    FROM PedidosCotizacion ORDER BY FechaEmision DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_FacturaCompra_Agregar
    @NumeroFactura NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @FechaEmision DATETIME,
    @FechaEntrega DATETIME, @Total DECIMAL(18,2), @Observaciones NVARCHAR(1000) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompra (NumeroFactura, IdOrdenCompra, FechaEmision, FechaEntrega, Total, Observaciones)
    VALUES (@NumeroFactura, @IdOrdenCompra, @FechaEmision, @FechaEntrega, @Total, @Observaciones);
END
GO

CREATE OR ALTER PROCEDURE sp_FacturaCompra_AgregarDetalle
    @NumeroFactura NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompraDetalle (NumeroFactura, CodigoComponente, Cantidad)
    VALUES (@NumeroFactura, @CodigoComponente, @Cantidad);
END
GO

PRINT 'Tablas y procedimientos de Compras (RFN2) creados/actualizados.';
GO
