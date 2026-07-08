-- ============================================================
--  08_sp_familias.sql  —  Procedimientos de la entidad Familias.
--  Consumidos por DAL/FamiliaDAL06AV.cs.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion FROM Familias ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerPorId
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion FROM Familias WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_Agregar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Familias (Id, Descripcion) VALUES (@Id, @Descripcion);
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_Modificar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Familias SET Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_Eliminar
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    -- Se limpian las relaciones donde participa la familia.
    DELETE FROM FamiliaPatentes WHERE IdFamilia = @Id;
    DELETE FROM FamiliaFamilias WHERE IdPadre   = @Id OR IdHijo = @Id;
    DELETE FROM RolFamilias     WHERE IdFamilia = @Id;
    DELETE FROM Familias        WHERE Id        = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerPatentes
    @IdFamilia NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Descripcion
    FROM   Patentes p
    INNER JOIN FamiliaPatentes fp ON fp.IdPatente = p.Id
    WHERE  fp.IdFamilia = @IdFamilia;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_AgregarPatente
    @IdFamilia NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente)
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES (@IdFamilia, @IdPatente);
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_QuitarPatente
    @IdFamilia NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM FamiliaPatentes WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerSubfamilias
    @IdPadre NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id, f.Descripcion
    FROM   Familias f
    INNER JOIN FamiliaFamilias ff ON ff.IdHijo = f.Id
    WHERE  ff.IdPadre = @IdPadre;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_AgregarSubfamilia
    @IdPadre NVARCHAR(450),
    @IdHijo  NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM FamiliaFamilias WHERE IdPadre = @IdPadre AND IdHijo = @IdHijo)
        INSERT INTO FamiliaFamilias (IdPadre, IdHijo) VALUES (@IdPadre, @IdHijo);
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_QuitarSubfamilia
    @IdPadre NVARCHAR(450),
    @IdHijo  NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM FamiliaFamilias WHERE IdPadre = @IdPadre AND IdHijo = @IdHijo;
END
GO

PRINT 'Procedimientos de Familias creados/actualizados.';
GO
