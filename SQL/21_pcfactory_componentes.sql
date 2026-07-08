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

PRINT 'Tabla Componentes y procedimientos ABM creados/actualizados.';
GO
