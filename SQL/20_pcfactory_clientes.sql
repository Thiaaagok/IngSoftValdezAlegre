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
