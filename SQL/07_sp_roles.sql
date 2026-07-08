-- ============================================================
--  07_sp_roles.sql  —  Procedimientos de la entidad Roles.
--  Consumidos por DAL/RolesDAL06AV.cs (EjecutarSP / EjecutarSPNonQuery).
--  Los nombres de columnas devueltos (Id, Descripcion, Codigo)
--  coinciden con lo que espera MPP/RolesMPP06AV.cs.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion, Codigo FROM Roles ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerPorId
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion, Codigo FROM Roles WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_Agregar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255),
    @Codigo      NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Roles (Id, Descripcion, Codigo) VALUES (@Id, @Descripcion, @Codigo);
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_Modificar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Roles SET Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_Eliminar
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    -- Se limpian primero las relaciones para no violar las FK.
    DELETE FROM RolPatentes  WHERE IdRol = @Id;
    DELETE FROM RolFamilias  WHERE IdRol = @Id;
    DELETE FROM Roles        WHERE Id    = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerPatentes
    @IdRol NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Descripcion
    FROM   Patentes p
    INNER JOIN RolPatentes rp ON rp.IdPatente = p.Id
    WHERE  rp.IdRol = @IdRol;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_AgregarPatente
    @IdRol     NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRol AND IdPatente = @IdPatente)
        INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRol, @IdPatente);
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_QuitarPatente
    @IdRol     NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RolPatentes WHERE IdRol = @IdRol AND IdPatente = @IdPatente;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerFamilias
    @IdRol NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id, f.Descripcion
    FROM   Familias f
    INNER JOIN RolFamilias rf ON rf.IdFamilia = f.Id
    WHERE  rf.IdRol = @IdRol;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_AgregarFamilia
    @IdRol     NVARCHAR(450),
    @IdFamilia NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM RolFamilias WHERE IdRol = @IdRol AND IdFamilia = @IdFamilia)
        INSERT INTO RolFamilias (IdRol, IdFamilia) VALUES (@IdRol, @IdFamilia);
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_QuitarFamilia
    @IdRol     NVARCHAR(450),
    @IdFamilia NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RolFamilias WHERE IdRol = @IdRol AND IdFamilia = @IdFamilia;
END
GO

PRINT 'Procedimientos de Roles creados/actualizados.';
GO
