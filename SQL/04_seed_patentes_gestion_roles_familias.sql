-- ============================================================
--  Seed: Patentes de acceso a las pantallas de Roles y Familias
--
--  Estructura elegida:
--
--    Rol "Administrador"
--     ├── GestionarRoles      (vía RolPatentes, patente directa)
--     └── GestionarFamilias   (vía RolPatentes, patente directa)
--
--  Estas dos patentes NO habilitan ninguna acción de negocio por sí
--  mismas: solo se usan para decidir si el botón/pantalla de Roles y
--  el de Familias se muestran en FRMMain (ver EsAdministrador/TienePermiso
--  en FRMMain.cs). Quien no las tenga, no puede ni ver esas pantallas.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── PATENTES ─────────────────────────────────────────────
    -- Los Ids deben coincidir EXACTAMENTE con los valores del enum PatenteEnum06AV.

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarRoles')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarRoles',    'Gestionar roles');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarFamilias')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarFamilias', 'Gestionar familias');

    -- ── ROL ADMINISTRADOR → PATENTES DIRECTAS ────────────────
    -- Se asignan directo al rol (RolPatentes), no a través de una familia,
    -- porque son patentes de acceso a pantalla y no de negocio.
    --
    -- El Id del rol Administrador NO es necesariamente el literal
    -- 'Administrador' (en esta base, por ejemplo, es un código numérico
    -- distinto, con Descripcion = 'Administrador' y Codigo = 'ADM').
    -- Por eso se busca el Id real en vez de asumirlo.

    DECLARE @IdRolAdmin NVARCHAR(450);

    SELECT @IdRolAdmin = Id
    FROM Roles
    WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
    BEGIN
        RAISERROR('No se encontró el rol Administrador (buscado por Descripcion o Codigo=ADM).', 16, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarRoles')
        INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarRoles');

    IF NOT EXISTS (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarFamilias')
        INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarFamilias');

    COMMIT TRANSACTION;
    PRINT 'Seed de GestionarRoles / GestionarFamilias completado correctamente.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
