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
