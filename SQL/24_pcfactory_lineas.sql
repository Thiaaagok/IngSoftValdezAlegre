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
