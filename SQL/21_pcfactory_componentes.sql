-- ============================================================
--  21_pcfactory_componentes.sql
--  Tabla Componentes (PC Factory) + procedimientos ABM.
--  Tipo se guarda como int (ordinal del enum BE.TipoComponente06AV).
--
--  STOCK EN DOS NIVELES (RFN1):
--    Stock : unidades físicas en depósito.
--    StockReservado  : unidades comprometidas por ventas registradas cuya
--                      orden de producción todavía no se cerró.
--    Stock libre      = Stock - StockReservado  (lo vendible).
--
--  La venta RESERVA (CU01) y el cierre de la orden de producción CONSUME
--  la reserva descontando el stock físico (CU06, componentes efectivamente
--  utilizados). Anular la venta o volver atrás la orden LIBERA la reserva.
--
--  BAJA LÓGICA (bitácora de cambios):
--    Bit_Lo_Bo = 0 → componente vigente;  Bit_Lo_Bo = 1 → dado de baja.
--    El borrado FÍSICO está prohibido por el trigger TR_Componentes_BloquearDelete
--    (ver 29_pcfactory_bitacora_componentes.sql): sp_Componentes_Eliminar hace
--    baja lógica. Los listados devuelven solo los vigentes; ObtenerPorCodigo
--    devuelve también los dados de baja para poder detectar códigos repetidos.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Componentes')
CREATE TABLE Componentes (
    Codigo          NVARCHAR(50)   NOT NULL PRIMARY KEY,
    Descripcion     NVARCHAR(200)  NOT NULL,
    Tipo            INT            NOT NULL,
    Marca           NVARCHAR(100)  NULL,
    Modelo          NVARCHAR(100)  NULL,
    PrecioUnitario  DECIMAL(12,2)  NOT NULL CONSTRAINT DF_Comp_Precio DEFAULT (0),
    Stock           INT            NOT NULL CONSTRAINT DF_Comp_Stock   DEFAULT (0),
    StockMinimo     INT            NOT NULL CONSTRAINT DF_Comp_StockMin DEFAULT (0),
    StockReservado  INT            NOT NULL CONSTRAINT DF_Comp_Reserva DEFAULT (0),
    Bit_Lo_Bo       BIT            NOT NULL CONSTRAINT DF_Comp_BitLoBo DEFAULT (0)
);
GO

-- Alta de la columna en bases creadas con una versión anterior del script.
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Componentes') AND name = 'StockMinimo')
    ALTER TABLE Componentes
        ADD StockMinimo INT NOT NULL CONSTRAINT DF_Comp_StockMin DEFAULT (0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Componentes') AND name = 'StockReservado')
    ALTER TABLE Componentes
        ADD StockReservado INT NOT NULL CONSTRAINT DF_Comp_Reserva DEFAULT (0);
GO

-- Baja lógica: reemplaza al borrado físico (ver 29_pcfactory_bitacora_componentes.sql).
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('Componentes') AND name = 'Bit_Lo_Bo')
    ALTER TABLE Componentes
        ADD Bit_Lo_Bo BIT NOT NULL CONSTRAINT DF_Comp_BitLoBo DEFAULT (0);
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario,
           Stock, StockMinimo, StockReservado, Bit_Lo_Bo
    FROM   Componentes
    WHERE  Bit_Lo_Bo = 0
    ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    -- Sin filtro de Bit_Lo_Bo a propósito: la BLL necesita ver el componente
    -- dado de baja para no permitir dar de alta otro con el mismo código.
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario,
           Stock, StockMinimo, StockReservado, Bit_Lo_Bo
    FROM   Componentes WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @Stock INT, @StockMinimo INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, Stock, StockMinimo)
    VALUES (@Codigo, @Descripcion, @Tipo, @Marca, @Modelo, @PrecioUnitario, @Stock, @StockMinimo);
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @Stock INT, @StockMinimo INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET Descripcion = @Descripcion, Tipo = @Tipo, Marca = @Marca, Modelo = @Modelo,
        PrecioUnitario = @PrecioUnitario, Stock = @Stock, StockMinimo = @StockMinimo
    WHERE Codigo = @Codigo;
END
GO

-- Baja lógica: única forma de "borrar" un componente. El trigger
-- TR_Componentes_BloquearDelete (29_pcfactory_bitacora_componentes.sql) impide
-- el DELETE físico, y el trigger de UPDATE deja la baja asentada en Componentes_C.
CREATE OR ALTER PROCEDURE sp_Componentes_BajaLogica
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Componentes WHERE Codigo = @Codigo)
        THROW 51004, 'El componente indicado no existe.', 1;

    UPDATE Componentes
    SET    Bit_Lo_Bo = 1
    WHERE  Codigo = @Codigo AND Bit_Lo_Bo = 0;
END
GO

-- Reactivación manual (deshace la baja lógica sin restaurar una versión vieja).
CREATE OR ALTER PROCEDURE sp_Componentes_Reactivar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Componentes WHERE Codigo = @Codigo)
        THROW 51005, 'El componente indicado no existe.', 1;

    UPDATE Componentes
    SET    Bit_Lo_Bo = 0
    WHERE  Codigo = @Codigo AND Bit_Lo_Bo = 1;
END
GO

-- Se conserva el nombre Eliminar para no romper a la DAL, pero ya no borra:
-- delega en la baja lógica.
CREATE OR ALTER PROCEDURE sp_Componentes_Eliminar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC sp_Componentes_BajaLogica @Codigo;
END
GO

-- ── Stock: reservar / liberar / consumir ─────────────────────

-- CU01: al registrar la venta se reservan las unidades necesarias.
-- Solo reserva si hay stock LIBRE suficiente; si no, no toca nada y avisa.
CREATE OR ALTER PROCEDURE sp_Componentes_ReservarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    StockReservado = StockReservado + @Cantidad
    WHERE  Codigo = @Codigo
      AND (Stock - StockReservado) >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51001, 'Stock libre insuficiente o componente inexistente al reservar.', 1;
END
GO

-- Devuelve unidades reservadas al stock libre (venta anulada, orden vuelta atrás).
CREATE OR ALTER PROCEDURE sp_Componentes_LiberarReserva
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    StockReservado = CASE WHEN StockReservado - @Cantidad < 0
                                 THEN 0 ELSE StockReservado - @Cantidad END
    WHERE  Codigo = @Codigo;
END
GO

-- CU06: al cerrar la orden se descuenta el stock físico de los componentes
-- efectivamente utilizados y se libera la reserva correspondiente.
CREATE OR ALTER PROCEDURE sp_Componentes_ConsumirReserva
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    Stock = Stock - @Cantidad,
           StockReservado  = CASE WHEN StockReservado - @Cantidad < 0
                                  THEN 0 ELSE StockReservado - @Cantidad END
    WHERE  Codigo = @Codigo AND Stock >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51002, 'Stock insuficiente o componente inexistente al consumir la reserva.', 1;
END
GO

-- Descuento directo de stock (se mantiene por compatibilidad con RFN2 / ajustes).
CREATE OR ALTER PROCEDURE sp_Componentes_DescontarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    Stock = Stock - @Cantidad
    WHERE  Codigo = @Codigo AND Stock >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51000, 'Stock insuficiente o componente inexistente al descontar stock.', 1;
END
GO

-- ── RFN2: reposición ─────────────────────────────────────────

-- Componentes que llegaron al mínimo: disparan la orden de compra.
CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerBajoStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario,
           Stock, StockMinimo, StockReservado, Bit_Lo_Bo
    FROM   Componentes
    WHERE  Stock <= StockMinimo
      AND  Bit_Lo_Bo = 0
    ORDER BY Descripcion;
END
GO

-- Suma al stock lo efectivamente recibido del proveedor (cierre de la orden de compra).
CREATE OR ALTER PROCEDURE sp_Componentes_SumarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes SET Stock = Stock + @Cantidad WHERE Codigo = @Codigo;

    IF @@ROWCOUNT = 0
        THROW 51003, 'Componente inexistente al sumar stock.', 1;
END
GO

PRINT 'Tabla Componentes y procedimientos ABM creados/actualizados.';
GO
