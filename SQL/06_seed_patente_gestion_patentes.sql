-- ============================================================
--  Seed: Patente de acceso a la nueva pantalla de Patentes
--
--    Rol "Administrador"
--     └── GestionarPatentes   (vía RolPatentes, patente directa)
--
--  Misma lógica que 04_seed_patentes_gestion_roles_familias.sql:
--  esta patente no habilita ninguna acción de negocio por sí misma,
--  solo decide si el botón/pantalla de Patentes se muestra en FRMMain.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarPatentes')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarPatentes', 'Gestionar patentes');

    DECLARE @IdRolAdmin NVARCHAR(450);

    SELECT @IdRolAdmin = Id
    FROM Roles
    WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
    BEGIN
        RAISERROR('No se encontró el rol Administrador (buscado por Descripcion o Codigo=ADM).', 16, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarPatentes')
        INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarPatentes');

    COMMIT TRANSACTION;
    PRINT 'Seed de GestionarPatentes completado correctamente.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
