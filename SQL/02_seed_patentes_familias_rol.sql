-- ============================================================
--  Seed: Patentes, Familias, Jerarquías y Rol Administrador
--
--  Estructura elegida:
--
--    Rol "Administrador"
--     └── Familia "Administracion"          (vía RolFamilias)
--           ├── Familia "GestionUsuarios"   (vía FamiliaFamilias)
--           │     ├── VerUsuarios
--           │     ├── CrearUsuarios
--           │     ├── EditarUsuarios
--           │     ├── ActDesactivarUsuarios
--           │     └── DesbloquearUsuarios
--           └── Familia "GestionBitacora"   (vía FamiliaFamilias)
--                 ├── VerBitacora
--                 └── ExportarBitacora
--
--  Ventaja: futuras roles pueden reutilizar las familias hoja
--  sin necesidad de duplicar patentes.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── PATENTES ─────────────────────────────────────────────
    -- Los Ids deben coincidir EXACTAMENTE con los valores del enum PatenteEnum.

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'VerUsuarios')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('VerUsuarios',          'Ver usuarios');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'CrearUsuarios')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('CrearUsuarios',        'Crear usuarios');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'EditarUsuarios')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('EditarUsuarios',       'Editar usuarios');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'ActDesactivarUsuarios')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('ActDesactivarUsuarios','Activar / Desactivar usuarios');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'DesbloquearUsuarios')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('DesbloquearUsuarios',  'Desbloquear usuarios');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'VerBitacora')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('VerBitacora',          'Ver bitácora');

    IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'ExportarBitacora')
        INSERT INTO Patentes (Id, Descripcion) VALUES ('ExportarBitacora',     'Exportar bitácora');

    -- ── FAMILIAS ─────────────────────────────────────────────

    IF NOT EXISTS (SELECT 1 FROM Familias WHERE Id = 'GestionUsuarios')
        INSERT INTO Familias (Id, Descripcion) VALUES ('GestionUsuarios',  'Gestión de usuarios');

    IF NOT EXISTS (SELECT 1 FROM Familias WHERE Id = 'GestionBitacora')
        INSERT INTO Familias (Id, Descripcion) VALUES ('GestionBitacora',  'Gestión de bitácora');

    IF NOT EXISTS (SELECT 1 FROM Familias WHERE Id = 'Administracion')
        INSERT INTO Familias (Id, Descripcion) VALUES ('Administracion',   'Administración del sistema');

    -- ── JERARQUÍA DE FAMILIAS ─────────────────────────────────
    -- "Administracion" es padre de las dos familias hoja.

    IF NOT EXISTS (SELECT 1 FROM FamiliaFamilias WHERE IdPadre = 'Administracion' AND IdHijo = 'GestionUsuarios')
        INSERT INTO FamiliaFamilias (IdPadre, IdHijo) VALUES ('Administracion', 'GestionUsuarios');

    IF NOT EXISTS (SELECT 1 FROM FamiliaFamilias WHERE IdPadre = 'Administracion' AND IdHijo = 'GestionBitacora')
        INSERT INTO FamiliaFamilias (IdPadre, IdHijo) VALUES ('Administracion', 'GestionBitacora');

    -- ── JERARQUÍA DE PATENTES ─────────────────────────────────
    -- Familia "GestionUsuarios"

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionUsuarios' AND IdPatente = 'VerUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionUsuarios', 'VerUsuarios');

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionUsuarios' AND IdPatente = 'CrearUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionUsuarios', 'CrearUsuarios');

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionUsuarios' AND IdPatente = 'EditarUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionUsuarios', 'EditarUsuarios');

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionUsuarios' AND IdPatente = 'ActDesactivarUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionUsuarios', 'ActDesactivarUsuarios');

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionUsuarios' AND IdPatente = 'DesbloquearUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionUsuarios', 'DesbloquearUsuarios');

    -- Familia "GestionBitacora"

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionBitacora' AND IdPatente = 'VerBitacora')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionBitacora', 'VerBitacora');

    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = 'GestionBitacora' AND IdPatente = 'ExportarBitacora')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES ('GestionBitacora', 'ExportarBitacora');

    -- ── ROL ADMINISTRADOR ─────────────────────────────────────
    -- El rol ya existe en la BD, pero lo creamos si no está.

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 'Administrador')
        INSERT INTO Roles (Id, Descripcion, Codigo) VALUES ('Administrador', 'Administrador', 'ADM');

    -- ── ROL → FAMILIA ─────────────────────────────────────────
    -- El rol Administrador recibe la familia raíz; el SP hace el resto.

    IF NOT EXISTS (SELECT 1 FROM RolFamilias WHERE IdRol = 'Administrador' AND IdFamilia = 'Administracion')
        INSERT INTO RolFamilias (IdRol, IdFamilia) VALUES ('Administrador', 'Administracion');

    COMMIT TRANSACTION;
    PRINT 'Seed completado correctamente.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
