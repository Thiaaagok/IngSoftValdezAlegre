-- ============================================================
--  sp_ObtenerPatentesUsuario
--  Devuelve todas las patentes del rol indicado expandiendo
--  la jerarquía de familias en forma recursiva (N niveles).
--
--  Flujo:
--    1. CTE "FamiliasDelRol"   → familias directas del rol (RolFamilias)
--    2. CTE "FamiliasExpandidas" → expande cada familia hacia sus hijos
--                                  recursivamente (FamiliaFamilias)
--    3. Patentes de esas familias (FamiliaPatentes)
--       UNION
--       Patentes directas del rol (RolPatentes)
-- ============================================================

IF OBJECT_ID('dbo.sp_ObtenerPatentesUsuario', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ObtenerPatentesUsuario;
GO

CREATE PROCEDURE dbo.sp_ObtenerPatentesUsuario
    @IdRol NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    WITH FamiliasDelRol AS (
        -- Familias asignadas directamente al rol
        SELECT rf.IdFamilia AS Id
        FROM   RolFamilias rf
        WHERE  rf.IdRol = @IdRol
    ),
    FamiliasExpandidas AS (
        -- Punto de anclaje: familias directas del rol
        SELECT Id FROM FamiliasDelRol

        UNION ALL

        -- Paso recursivo: hijos de cada familia ya acumulada
        SELECT ff.IdHijo
        FROM   FamiliaFamilias    ff
        INNER JOIN FamiliasExpandidas fe ON fe.Id = ff.IdPadre
    )

    -- Patentes que vienen de las familias (todos los niveles)
    SELECT DISTINCT p.Id, p.Descripcion
    FROM   Patentes         p
    INNER JOIN FamiliaPatentes fp ON fp.IdPatente = p.Id
    INNER JOIN FamiliasExpandidas fe ON fe.Id      = fp.IdFamilia

    UNION

    -- Patentes asignadas directamente al rol (RolPatentes)
    SELECT p.Id, p.Descripcion
    FROM   Patentes   p
    INNER JOIN RolPatentes rp ON rp.IdPatente = p.Id
    WHERE  rp.IdRol = @IdRol;
END;
GO
