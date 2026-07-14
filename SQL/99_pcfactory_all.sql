-- ============================================================
--  99_pcfactory_all.sql  — Script MAESTRO de PC Factory.
--  Concatena 20..28 (tablas, SP, patentes). Incluye: descuento de
--  stock, cotización con costo/condiciones y catálogo de modelos
--  estándar. Idempotente. Correlo en SSMS sobre IngSoftValdezAlegre.
-- ============================================================

-- ==== INICIO 20_pcfactory_clientes.sql ====

-- ============================================================
--  20_pcfactory_clientes.sql
--  Tabla Clientes (PC Factory) + procedimientos ABM.
--  Consumidos por DAL/ClientesDAL06AV.cs (EjecutarSP / EjecutarSPNonQuery).
--  Los nombres de columnas coinciden con MPP/ClientesMPP06AV.cs.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clientes')
CREATE TABLE Clientes (
    Dni       NVARCHAR(20)  NOT NULL PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Apellido  NVARCHAR(100) NOT NULL,
    Telefono  NVARCHAR(50)  NULL,
    Direccion NVARCHAR(200) NULL
);
GO

CREATE OR ALTER PROCEDURE sp_Clientes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Dni, Nombre, Apellido, Telefono, Direccion
    FROM   Clientes
    ORDER BY Apellido, Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_ObtenerPorDni
    @Dni NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Dni, Nombre, Apellido, Telefono, Direccion
    FROM   Clientes
    WHERE  Dni = @Dni;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Agregar
    @Dni       NVARCHAR(20),
    @Nombre    NVARCHAR(100),
    @Apellido  NVARCHAR(100),
    @Telefono  NVARCHAR(50),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion)
    VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Direccion);
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Modificar
    @Dni       NVARCHAR(20),
    @Nombre    NVARCHAR(100),
    @Apellido  NVARCHAR(100),
    @Telefono  NVARCHAR(50),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Clientes
    SET    Nombre = @Nombre, Apellido = @Apellido,
           Telefono = @Telefono, Direccion = @Direccion
    WHERE  Dni = @Dni;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Eliminar
    @Dni NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Clientes WHERE Dni = @Dni;
END
GO

PRINT 'Tabla Clientes y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 20_pcfactory_clientes.sql


-- ==== INICIO 21_pcfactory_componentes.sql ====

-- ============================================================
--  21_pcfactory_componentes.sql
--  Tabla Componentes (PC Factory) + procedimientos ABM.
--  Tipo se guarda como int (ordinal del enum BE.TipoComponente06AV).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Componentes')
CREATE TABLE Componentes (
    Codigo          NVARCHAR(50)   NOT NULL PRIMARY KEY,
    Descripcion     NVARCHAR(200)  NOT NULL,
    Tipo            INT            NOT NULL,
    Marca           NVARCHAR(100)  NULL,
    Modelo          NVARCHAR(100)  NULL,
    PrecioUnitario  DECIMAL(12,2)  NOT NULL CONSTRAINT DF_Comp_Precio DEFAULT (0),
    StockDisponible INT            NOT NULL CONSTRAINT DF_Comp_Stock   DEFAULT (0)
);
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible
    FROM   Componentes ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible
    FROM   Componentes WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @StockDisponible INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible)
    VALUES (@Codigo, @Descripcion, @Tipo, @Marca, @Modelo, @PrecioUnitario, @StockDisponible);
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @StockDisponible INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET Descripcion = @Descripcion, Tipo = @Tipo, Marca = @Marca, Modelo = @Modelo,
        PrecioUnitario = @PrecioUnitario, StockDisponible = @StockDisponible
    WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Eliminar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Componentes WHERE Codigo = @Codigo;
END
GO

-- Descuenta stock de un componente al usarlo en una orden de producción (
-- <<< FIN 21_pcfactory_componentes.sql


-- ==== INICIO 22_pcfactory_insumos.sql ====

-- ============================================================
--  22_pcfactory_insumos.sql
--  Tabla Insumos (PC Factory) + procedimientos ABM + bajo stock.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Insumos')
CREATE TABLE Insumos (
    Codigo      NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Descripcion NVARCHAR(200) NOT NULL,
    Stock       INT           NOT NULL CONSTRAINT DF_Ins_Stock DEFAULT (0),
    StockMinimo INT           NOT NULL CONSTRAINT DF_Ins_Min   DEFAULT (0)
);
GO

CREATE OR ALTER PROCEDURE sp_Insumos_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Stock, StockMinimo FROM Insumos ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Stock, StockMinimo FROM Insumos WHERE Codigo = @Codigo;
END
GO

-- Insumos por debajo (o en) su stock mínimo: usado por RFN2 para resaltarlos.
CREATE OR ALTER PROCEDURE sp_Insumos_ObtenerBajoStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Stock, StockMinimo
    FROM   Insumos WHERE Stock <= StockMinimo ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Stock INT, @StockMinimo INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Insumos (Codigo, Descripcion, Stock, StockMinimo)
    VALUES (@Codigo, @Descripcion, @Stock, @StockMinimo);
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Stock INT, @StockMinimo INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Insumos SET Descripcion = @Descripcion, Stock = @Stock, StockMinimo = @StockMinimo
    WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_Eliminar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Insumos WHERE Codigo = @Codigo;
END
GO

PRINT 'Tabla Insumos y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 22_pcfactory_insumos.sql


-- ==== INICIO 23_pcfactory_proveedores.sql ====

-- ============================================================
--  23_pcfactory_proveedores.sql
--  Tabla Proveedores (PC Factory) + procedimientos ABM.
--  Id es autonumérico; el Agregar devuelve el Id generado.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Proveedores')
CREATE TABLE Proveedores (
    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre    NVARCHAR(150) NOT NULL,
    Cuit      NVARCHAR(20)  NOT NULL,
    Email     NVARCHAR(150) NULL,
    Telefono  NVARCHAR(50)  NULL,
    Direccion NVARCHAR(200) NULL,
    CONSTRAINT UQ_Proveedores_Cuit UNIQUE (Cuit)
);
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorCuit
    @Cuit NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores WHERE Cuit = @Cuit;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Agregar
    @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion)
    VALUES (@Nombre, @Cuit, @Email, @Telefono, @Direccion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Proveedores
    SET Nombre = @Nombre, Cuit = @Cuit, Email = @Email,
        Telefono = @Telefono, Direccion = @Direccion
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Proveedores WHERE Id = @Id;
END
GO

PRINT 'Tabla Proveedores y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 23_pcfactory_proveedores.sql


-- ==== INICIO 24_pcfactory_lineas.sql ====

-- ============================================================
--  24_pcfactory_lineas.sql
--  Tabla LineasEnsamblaje (PC Factory) + procedimientos ABM.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LineasEnsamblaje')
CREATE TABLE LineasEnsamblaje (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(200) NULL,
    Disponible  BIT NOT NULL CONSTRAINT DF_Linea_Disp DEFAULT (1)
);
GO

CREATE OR ALTER PROCEDURE sp_Lineas_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion, Disponible FROM LineasEnsamblaje ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion, Disponible FROM LineasEnsamblaje WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Agregar
    @Nombre NVARCHAR(100), @Descripcion NVARCHAR(200), @Disponible BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible)
    VALUES (@Nombre, @Descripcion, @Disponible);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Modificar
    @Id INT, @Nombre NVARCHAR(100), @Descripcion NVARCHAR(200), @Disponible BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE LineasEnsamblaje
    SET Nombre = @Nombre, Descripcion = @Descripcion, Disponible = @Disponible
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM LineasEnsamblaje WHERE Id = @Id;
END
GO

PRINT 'Tabla LineasEnsamblaje y procedimientos ABM creados/actualizados.';
GO

-- <<< FIN 24_pcfactory_lineas.sql


-- ==== INICIO 25_pcfactory_produccion.sql ====

-- ============================================================
--  25_pcfactory_produccion.sql   (RFN1: Venta / Producción)
--  Computadoras + sus componentes, Órdenes de Producción y Pagos.
--  Estados de OP (int): 0 Pendiente, 1 Planificada, 2 EnEnsamblaje,
--                       3 Finalizada, 4 Entregada.
--  Tipo de pago (int): 0 Sena, 1 SaldoFinal.
-- ============================================================

-- ── Computadora solicitada (estándar o personalizada) ────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Computadoras')
CREATE TABLE Computadoras (
    Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre            NVARCHAR(150) NULL,
    TipoConfiguracion INT           NOT NULL,   -- 0 Estandar, 1 Personalizada
    PrecioTotal       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Comp_Prec DEFAULT (0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComputadoraComponentes')
CREATE TABLE ComputadoraComponentes (
    IdComputadora   INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_CompComp PRIMARY KEY (IdComputadora, CodigoComponente),
    CONSTRAINT FK_CompComp_Comp FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_CompComp_Componente FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── Orden de producción ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesProduccion')
CREATE TABLE OrdenesProduccion (
    NumeroOrden         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DniCliente          NVARCHAR(20)  NOT NULL,
    IdComputadora       INT           NOT NULL,
    FechaEntrega        DATE          NOT NULL,
    Estado              INT           NOT NULL CONSTRAINT DF_OP_Estado DEFAULT (0),
    IdLinea             INT           NULL,
    FechaInicioPrevista DATE          NULL,
    ResponsableTecnico  NVARCHAR(150) NULL,
    CONSTRAINT FK_OP_Cliente FOREIGN KEY (DniCliente) REFERENCES Clientes(Dni),
    CONSTRAINT FK_OP_Computadora FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_OP_Linea FOREIGN KEY (IdLinea) REFERENCES LineasEnsamblaje(Id)
);
GO

-- ── Pagos (seña / saldo final) ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Pagos')
CREATE TABLE Pagos (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroOrden INT           NOT NULL,
    Tipo        INT           NOT NULL,   -- 0 Sena, 1 SaldoFinal
    Monto       DECIMAL(12,2) NOT NULL,
    Fecha       DATETIME      NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (GETDATE()),
    CONSTRAINT FK_Pago_OP FOREIGN KEY (NumeroOrden) REFERENCES OrdenesProduccion(NumeroOrden)
);
GO

-- ── SPs Computadora ──────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Computadoras_Agregar
    @Nombre NVARCHAR(150), @TipoConfiguracion INT, @PrecioTotal DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Computadoras (Nombre, TipoConfiguracion, PrecioTotal)
    VALUES (@Nombre, @TipoConfiguracion, @PrecioTotal);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_AgregarComponente
    @IdComputadora INT, @CodigoComponente NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ComputadoraComponentes
                   WHERE IdComputadora = @IdComputadora AND CodigoComponente = @CodigoComponente)
        INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente)
        VALUES (@IdComputadora, @CodigoComponente);
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerComponentes
    @IdComputadora INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Codigo, c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.PrecioUnitario, c.StockDisponible
    FROM   Componentes c
    INNER JOIN ComputadoraComponentes cc ON cc.CodigoComponente = c.Codigo
    WHERE  cc.IdComputadora = @IdComputadora;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, TipoConfiguracion, PrecioTotal FROM Computadoras WHERE Id = @Id;
END
GO

-- ── SPs Orden de producción ──────────────────────────────────
CREATE OR ALTER PROCEDURE sp_OP_Agregar
    @DniCliente NVARCHAR(20), @IdComputadora INT, @FechaEntrega DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesProduccion (DniCliente, IdComputadora, FechaEntrega, Estado)
    VALUES (@DniCliente, @IdComputadora, @FechaEntrega, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, DniCliente, IdComputadora, FechaEntrega, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico
    FROM   OrdenesProduccion ORDER BY NumeroOrden DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerPorNumero
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, DniCliente, IdComputadora, FechaEntrega, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico
    FROM   OrdenesProduccion WHERE NumeroOrden = @NumeroOrden;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_Planificar
    @NumeroOrden INT, @IdLinea INT, @FechaInicioPrevista DATE, @ResponsableTecnico NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion
    SET IdLinea = @IdLinea, FechaInicioPrevista = @FechaInicioPrevista,
        ResponsableTecnico = @ResponsableTecnico, Estado = 1   -- Planificada
    WHERE NumeroOrden = @NumeroOrden;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_CambiarEstado
    @NumeroOrden INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion SET Estado = @Estado WHERE NumeroOrden = @NumeroOrden;
END
GO

-- ── SPs Pagos ────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Pagos_Agregar
    @NumeroOrden INT, @Tipo INT, @Monto DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Pagos (NumeroOrden, Tipo, Monto) VALUES (@NumeroOrden, @Tipo, @Monto);
END
GO

CREATE OR ALTER PROCEDURE sp_Pagos_ObtenerPorOrden
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, NumeroOrden, Tipo, Monto, Fecha FROM Pagos WHERE NumeroOrden = @NumeroOrden;
END
GO

PRINT 'Tablas y procedimientos de Producción (RFN1) creados/actualizados.';
GO

-- <<< FIN 25_pcfactory_produccion.sql


-- ==== INICIO 26_pcfactory_compras.sql ====

-- ============================================================
--  26_pcfactory_compras.sql   (RFN2: Compras / Insumos)
--  Órdenes de compra (+ detalle) y pedidos de cotización.
--  Estado OC (int):        0 Pendiente, 1 Enviada, 2 Finalizada.
--  Estado Cotización (int):0 PorAprobar, 1 Aprobado, 2 Desaprobada.
--  La cotización referencia la OC y reutiliza su detalle de insumos.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesCompra')
CREATE TABLE OrdenesCompra (
    NumeroCompra        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FechaLimite         DATE          NOT NULL,
    RepositorSolicitante NVARCHAR(150) NULL,
    Estado              INT           NOT NULL CONSTRAINT DF_OC_Estado DEFAULT (0),
    FechaCierre         DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenCompraDetalle')
CREATE TABLE OrdenCompraDetalle (
    NumeroCompra INT          NOT NULL,
    CodigoInsumo NVARCHAR(50) NOT NULL,
    Cantidad     INT          NOT NULL,
    CONSTRAINT PK_OCD PRIMARY KEY (NumeroCompra, CodigoInsumo),
    CONSTRAINT FK_OCD_OC FOREIGN KEY (NumeroCompra) REFERENCES OrdenesCompra(NumeroCompra),
    CONSTRAINT FK_OCD_Insumo FOREIGN KEY (CodigoInsumo) REFERENCES Insumos(Codigo)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PedidosCotizacion')
CREATE TABLE PedidosCotizacion (
    Numero       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroCompra INT       NOT NULL,
    IdProveedor  INT       NOT NULL,
    FechaEmision DATETIME  NOT NULL CONSTRAINT DF_Cot_Fecha DEFAULT (GETDATE()),
    Estado       INT       NOT NULL CONSTRAINT DF_Cot_Estado DEFAULT (0),
    Costo        DECIMAL(12,2) NULL,
    Condiciones  NVARCHAR(500) NULL,
    CONSTRAINT FK_Cot_OC FOREIGN KEY (NumeroCompra) REFERENCES OrdenesCompra(NumeroCompra),
    CONSTRAINT FK_Cot_Prov FOREIGN KEY (IdProveedor) REFERENCES Proveedores(Id)
);
GO

-- Para bases ya creadas: agrega las columnas de precio/condiciones si faltan.
IF COL_LENGTH('PedidosCotizacion','Costo') IS NULL
    ALTER TABLE PedidosCotizacion ADD Costo DECIMAL(12,2) NULL;
IF COL_LENGTH('PedidosCotizacion','Condiciones') IS NULL
    ALTER TABLE PedidosCotizacion ADD Condiciones NVARCHAR(500) NULL;
GO

-- ── SPs Orden de compra ──────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_OC_Agregar
    @FechaLimite DATE, @RepositorSolicitante NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesCompra (FechaLimite, RepositorSolicitante, Estado)
    VALUES (@FechaLimite, @RepositorSolicitante, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_AgregarDetalle
    @NumeroCompra INT, @CodigoInsumo NVARCHAR(50), @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM OrdenCompraDetalle
                   WHERE NumeroCompra = @NumeroCompra AND CodigoInsumo = @CodigoInsumo)
        INSERT INTO OrdenCompraDetalle (NumeroCompra, CodigoInsumo, Cantidad)
        VALUES (@NumeroCompra, @CodigoInsumo, @Cantidad);
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroCompra, FechaLimite, RepositorSolicitante, Estado, FechaCierre
    FROM   OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerPorNumero
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroCompra, FechaLimite, RepositorSolicitante, Estado, FechaCierre
    FROM   OrdenesCompra WHERE NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerDetalle
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.CodigoInsumo, d.Cantidad, i.Descripcion, i.Stock, i.StockMinimo
    FROM   OrdenCompraDetalle d
    INNER JOIN Insumos i ON i.Codigo = d.CodigoInsumo
    WHERE  d.NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_CambiarEstado
    @NumeroCompra INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado = @Estado WHERE NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_Finalizar
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado = 2, FechaCierre = GETDATE() WHERE NumeroCompra = @NumeroCompra;
END
GO

-- ── SPs Cotización ───────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Cotizacion_Agregar
    @NumeroCompra INT, @IdProveedor INT,
    @Costo DECIMAL(12,2), @Condiciones NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PedidosCotizacion (NumeroCompra, IdProveedor, Estado, Costo, Condiciones)
    VALUES (@NumeroCompra, @IdProveedor, 0, @Costo, @Condiciones);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Numero, NumeroCompra, IdProveedor, FechaEmision, Estado, Costo, Condiciones
    FROM   PedidosCotizacion ORDER BY Numero DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerPorNumero
    @Numero INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Numero, NumeroCompra, IdProveedor, FechaEmision, Estado, Costo, Condiciones
    FROM   PedidosCotizacion WHERE Numero = @Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_CambiarEstado
    @Numero INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PedidosCotizacion SET Estado = @Estado WHERE Numero = @Numero;
END
GO

-- ── Actualización de stock al recibir ────────────────────────
CREATE OR ALTER PROCEDURE sp_Insumos_SumarStock
    @Codigo NVARCHAR(50), @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Insumos SET Stock = Stock + @Cantidad WHERE Codigo = @Codigo;
END
GO

PRINT 'Tablas y procedimientos de Compras (RFN2) creados/actualizados.';
GO

-- <<< FIN 26_pcfactory_compras.sql


-- ==== INICIO 27_pcfactory_patentes.sql ====

-- ============================================================
--  27_pcfactory_patentes.sql
--  Patentes de acceso a las pantallas del dominio PC Factory.
--
--  Cada patente habilita la visibilidad de un módulo del sidebar
--  (ver ConfigurarModulosPcFactory / AgregarModuloPcFactory en
--   FRMMain.cs). Sin la patente, el módulo NO se agrega al menú.
--
--  Los Ids deben coincidir EXACTAMENTE con los valores del enum
--  PatenteEnum06AV (SER/Enums/PatenteEnum06AV.cs).
--
--  Se asignan directamente al rol Administrador (RolPatentes), igual
--  que GestionarRoles / GestionarFamilias.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── PATENTES ─────────────────────────────────────────────
    DECLARE @Patentes TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
    INSERT INTO @Patentes (Id, Descripcion) VALUES
        ('GestionarClientes',          'Gestionar clientes'),
        ('GestionarComponentes',       'Gestionar componentes'),
        ('GestionarInsumos',           'Gestionar insumos'),
        ('GestionarProveedores',       'Gestionar proveedores'),
        ('GestionarLineasEnsamblaje',  'Gestionar líneas de ensamblaje'),
        ('GestionarProduccion',        'Gestionar producción'),
        ('GestionarCompras',           'Gestionar compras');

    INSERT INTO Patentes (Id, Descripcion)
    SELECT p.Id, p.Descripcion
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);

    -- ── ROL ADMINISTRADOR → PATENTES DIRECTAS ────────────────
    DECLARE @IdRolAdmin NVARCHAR(450);

    SELECT @IdRolAdmin = Id
    FROM   Roles
    WHERE  Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
        RAISERROR('No se encontró el rol Administrador (por Descripcion o Codigo=ADM).', 16, 1);

    INSERT INTO RolPatentes (IdRol, IdPatente)
    SELECT @IdRolAdmin, p.Id
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM RolPatentes rp
                       WHERE rp.IdRol = @IdRolAdmin AND rp.IdPatente = p.Id);

    COMMIT TRANSACTION;
    PRINT 'Seed de patentes PC Factory completado (asignadas al rol Administrador).';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed de patentes PC Factory: ' + ERROR_MESSAGE();
END CATCH
GO

-- <<< FIN 27_pcfactory_patentes.sql


-- ==== INICIO 28_pcfactory_modelos.sql ====

-- ============================================================
--  28_pcfactory_modelos.sql
--  Catálogo de modelos de computadora ESTÁNDAR (RFN1). Cada modelo
--  tiene un nombre y una lista de componentes predefinidos. Al armar
--  una orden "Estándar" se elige un modelo y se cargan sus componentes.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModelosEstandar')
CREATE TABLE ModelosEstandar (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(300) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModeloEstandarComponentes')
CREATE TABLE ModeloEstandarComponentes (
    IdModelo         INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_ModeloComp PRIMARY KEY (IdModelo, CodigoComponente),
    CONSTRAINT FK_ModeloComp_Modelo FOREIGN KEY (IdModelo)
        REFERENCES ModelosEstandar(Id),
    CONSTRAINT FK_ModeloComp_Comp FOREIGN KEY (CodigoComponente)
        REFERENCES Componentes(Codigo)
);
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion FROM ModelosEstandar ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoComponente FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Agregar
    @Nombre NVARCHAR(150), @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ModelosEstandar (Nombre, Descripcion) VALUES (@Nombre, @Descripcion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ModelosEstandar SET Nombre = @Nombre, Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ModeloEstandarComponentes WHERE IdModelo = @Id;
    DELETE FROM ModelosEstandar WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_AgregarComponente
    @IdModelo INT, @CodigoComponente NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes
                   WHERE IdModelo = @IdModelo AND CodigoComponente = @CodigoComponente)
        INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente)
        VALUES (@IdModelo, @CodigoComponente);
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_QuitarComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

-- Patente de acceso al ABM de modelos estándar (coincide con PatenteEnum06AV).
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarModelosEstandar')
    INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarModelosEstandar', 'Gestionar modelos estándar');
GO

DECLARE @IdRolAdmin NVARCHAR(450);
SELECT @IdRolAdmin = Id FROM Roles WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';
IF @IdRolAdmin IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarModelosEstandar')
    INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarModelosEstandar');
GO

PRINT 'Catálogo de modelos estándar (RFN1) creado/actualizado.';
GO

-- <<< FIN 28_pcfactory_modelos.sql


PRINT '>>> PC Factory: todo creado/actualizado.';
GO
