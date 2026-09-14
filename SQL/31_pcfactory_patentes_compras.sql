-- ============================================================
--  31_pcfactory_patentes_compras.sql
--  Patentes de ACCIÓN del circuito de compras (RFN2).
--
--  Reemplazan la validación por nombre de rol que hacía
--  BLL/RolNegocio06AV.cs (eliminada). La autorización del sistema
--  se maneja siempre por patentes (Composite: Rol → Familias → Patentes).
--
--  Separación de funciones del RFN2:
--    - RegistrarOrdenCompra → la registra el Repositor.
--    - AprobarCotizacion    → la aprueba/desaprueba el Gerente de Compras.
--  Asignando cada patente a un rol distinto se mantiene la separación;
--  el rol Administrador recibe ambas para poder operar el circuito completo.
--
--  Los Ids deben coincidir EXACTAMENTE con los valores del enum
--  PatenteEnum06AV (SER/Enums/PatenteEnum06AV.cs).
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── PATENTES ─────────────────────────────────────────────
    DECLARE @Patentes TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
    INSERT INTO @Patentes (Id, Descripcion) VALUES
        ('RegistrarOrdenCompra', 'Registrar una orden de compra'),
        ('AprobarCotizacion',    'Aprobar o desaprobar una cotización');

    INSERT INTO Patentes (Id, Descripcion)
    SELECT p.Id, p.Descripcion
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);

    -- ── ROL ADMINISTRADOR → PATENTES DIRECTAS ────────────────
    DECLARE @IdRolAdmin NVARCHAR(450);

    SELECT @IdRolAdmin = Id
    FROM   Roles
    WHERE  Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
        RAISERROR('No se encontró el rol Administrador (por Descripcion o Codigo=ADM).', 16, 1);

    INSERT INTO RolPatentes (IdRol, IdPatente)
    SELECT @IdRolAdmin, p.Id
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM RolPatentes rp
                       WHERE rp.IdRol = @IdRolAdmin AND rp.IdPatente = p.Id);

    COMMIT TRANSACTION;
    PRINT 'Seed de patentes de acción de compras completado (asignadas al rol Administrador).';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed de patentes de compras: ' + ERROR_MESSAGE();
END CATCH
GO
