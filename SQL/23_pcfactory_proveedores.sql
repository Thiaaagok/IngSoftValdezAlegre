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
