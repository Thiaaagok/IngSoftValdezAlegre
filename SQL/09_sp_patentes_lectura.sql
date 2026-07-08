-- ============================================================
--  09_sp_patentes_lectura.sql  —  Lectura de Patentes.
--  Los de escritura (Agregar/Modificar/Eliminar) están en
--  05_sp_patentes_crud.sql. Acá van los de solo lectura que
--  consume DAL/PatentesDAL06AV.cs.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Patentes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion FROM Patentes ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Patentes_ObtenerPorId
    @Id NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion FROM Patentes WHERE Id = @Id;
END
GO

PRINT 'Procedimientos de lectura de Patentes creados/actualizados.';
GO
