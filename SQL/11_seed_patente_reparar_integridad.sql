-- ============================================================
--  11_seed_patente_reparar_integridad.sql
--
--  Crea la patente 'RepararIntegridad' y la asigna al rol
--  Administrador para que el admin pueda abrir el GUI de Reparación
--  del Dígito Verificador cuando el Login detecta una inconsistencia.
--
--  IMPORTANTE — por qué se asigna DIRECTO al rol (RolPatentes) y con
--  búsqueda dinámica del Id:
--    El Id del rol Administrador NO es necesariamente el literal
--    'Administrador' (puede ser un código numérico, con Descripcion =
--    'Administrador' y Codigo = 'ADM'). Los seeds 04/06/27/28 asignan
--    sus patentes al rol encontrado por Descripcion/Codigo. Si esta
--    patente se colgara de la familia 'Administracion' (atada solo al
--    rol de Id literal 'Administrador'), quedaría en un rol DISTINTO al
--    que usa el usuario admin, y el admin no tendría RepararIntegridad.
--    Por eso se usa el MISMO patrón que los otros seeds: rol dinámico +
--    RolPatentes directo.
--
--  El Id de la patente debe coincidir EXACTAMENTE con el valor del enum
--  SER.PatenteEnum06AV.RepararIntegridad ("RepararIntegridad").
--
--  Idempotente y auto-reparador: si en una instalación previa la patente
--  quedó colgada de alguna familia del rol, la limpia para no duplicarla.
-- ============================================================

SET NOCOUNT ON;

-- 1) La patente en sí.
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'RepararIntegridad')
    INSERT INTO Patentes (Id, Descripcion)
    VALUES ('RepararIntegridad', 'Reparar integridad (Dígito Verificador)');

-- 2) Id REAL del rol Administrador (mismo criterio que 04/06/27/28).
DECLARE @IdRolAdmin NVARCHAR(450);
SELECT @IdRolAdmin = Id
FROM   Roles
WHERE  Descripcion = 'Administrador' OR Codigo = 'ADM';

IF @IdRolAdmin IS NULL
BEGIN
    RAISERROR('No se encontró el rol Administrador (Descripcion o Codigo=ADM). Ejecutá antes 02_seed_patentes_familias_rol.sql.', 16, 1);
    RETURN;
END

-- 3) Auto-reparación: quitar la patente de CUALQUIER familia del árbol del
--    rol, para que al asignarla directa no quede duplicada ("ya contenida en
--    este rol"). Cubre instalaciones viejas donde se colgaba de 'Administracion'.
;WITH FamiliasDelRol AS (
    SELECT rf.IdFamilia AS Id
    FROM   RolFamilias rf
    WHERE  rf.IdRol = @IdRolAdmin
    UNION ALL
    SELECT ff.IdHijo
    FROM   FamiliaFamilias ff
    INNER JOIN FamiliasDelRol f ON f.Id = ff.IdPadre
)
DELETE fp
FROM   FamiliaPatentes fp
INNER JOIN FamiliasDelRol fr ON fr.Id = fp.IdFamilia
WHERE  fp.IdPatente = 'RepararIntegridad'
OPTION (MAXRECURSION 1000);

-- 4) Asignación DIRECTA al rol (igual que GestionarRoles, GestionarPatentes, etc.).
IF NOT EXISTS (SELECT 1 FROM RolPatentes
               WHERE IdRol = @IdRolAdmin AND IdPatente = 'RepararIntegridad')
    INSERT INTO RolPatentes (IdRol, IdPatente)
    VALUES (@IdRolAdmin, 'RepararIntegridad');

PRINT 'Patente RepararIntegridad creada y asignada directamente al rol Administrador.';
GO
