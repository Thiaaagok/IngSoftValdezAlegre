-- 37_serializacion.sql
-- A03 Serialización: patente Serializar para el Administrador.
--
-- La serialización no crea tablas: graba y lee archivos XML. Lo único que necesita
-- la base es la patente, y los eventos van a la Bitacora que ya existe
-- (módulo Serializacion, categorías Serializacion y Deserializacion).
-- Idempotente.
--
-- Agrega filas en Patentes y RolPatentes, que tienen dígito verificador: después
-- de correrlo hay que recalcularlo, y cerrar sesión para tomar la patente nueva.

BEGIN TRANSACTION;
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'Serializar')
        INSERT INTO Patentes (Id, Descripcion)
        VALUES ('Serializar', 'Serializar y des-serializar objetos');

    DECLARE @IdRolAdmin NVARCHAR(450);
    SELECT @IdRolAdmin = Id FROM Roles WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
        RAISERROR('No se encontró el rol Administrador (por Descripcion o Codigo=ADM).', 16, 1);

    IF NOT EXISTS (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'Serializar')
        INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'Serializar');

    COMMIT TRANSACTION;
    PRINT '   Patente Serializar asignada al Administrador.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Error sembrando la patente Serializar: ' + ERROR_MESSAGE();
END CATCH
GO

PRINT '>>> 37: A03 Serialización lista.';
PRINT '    Recordá: cerrar sesión, volver a entrar y recalcular el dígito verificador.';
GO
