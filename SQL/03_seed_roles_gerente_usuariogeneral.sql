-- ============================================================
--  Seed: Roles "Gerente" y "Usuario General"
--
--  Estructura de permisos:
--
--  Rol "UsuarioGeneral"
--   └── Familia "VisualizacionUsuarios"
--         └── VerUsuarios
--
--  Rol "Gerente"
--   ├── Familia "VisualizacionUsuarios"  (reutilizada)
--   │     └── VerUsuarios
--   ├── Familia "EdicionUsuarios"
--   │     └── EditarUsuarios
--   └── Familia "GestionBitacora"        (reutilizada del Administrador)
--         ├── VerBitacora
--         └── ExportarBitacora
--
--  Nota: Administrador mantiene GestionUsuarios completa (CRUD + bloqueos).
--        Gerente solo puede ver, editar y gestionar la bitácora.
--        UsuarioGeneral solo puede ver la lista de usuarios.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── FAMILIAS NUEVAS ───────────────────────────────────────

    IF NOT EXISTS (SELECT 1 FROM Familias WHERE Id = 'VisualizacionUsuarios')
        INSERT INTO Familias (Id, Descripcion)
        VALUES ('VisualizacionUsuarios', 'Visualización de usuarios');

    IF NOT EXISTS (SELECT 1 FROM Familias WHERE Id = 'EdicionUsuarios')
        INSERT INTO Familias (Id, Descripcion)
        VALUES ('EdicionUsuarios', 'Edición de usuarios');

    -- ── PATENTES EN FAMILIAS NUEVAS ───────────────────────────

    -- VisualizacionUsuarios → VerUsuarios
    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes
                   WHERE IdFamilia = 'VisualizacionUsuarios' AND IdPatente = 'VerUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente)
        VALUES ('VisualizacionUsuarios', 'VerUsuarios');

    -- EdicionUsuarios → EditarUsuarios
    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes
                   WHERE IdFamilia = 'EdicionUsuarios' AND IdPatente = 'EditarUsuarios')
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente)
        VALUES ('EdicionUsuarios', 'EditarUsuarios');

    -- ── ROLES NUEVOS ──────────────────────────────────────────

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 'UsuarioGeneral')
        INSERT INTO Roles (Id, Descripcion, Codigo)
        VALUES ('UsuarioGeneral', 'Usuario General', 'USR');

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 'Gerente')
        INSERT INTO Roles (Id, Descripcion, Codigo)
        VALUES ('Gerente', 'Gerente', 'GER');

    -- ── ROL → FAMILIAS ────────────────────────────────────────

    -- Usuario General: solo ver usuarios
    IF NOT EXISTS (SELECT 1 FROM RolFamilias
                   WHERE IdRol = 'UsuarioGeneral' AND IdFamilia = 'VisualizacionUsuarios')
        INSERT INTO RolFamilias (IdRol, IdFamilia)
        VALUES ('UsuarioGeneral', 'VisualizacionUsuarios');

    -- Gerente: ver + editar usuarios, ver + exportar bitácora
    IF NOT EXISTS (SELECT 1 FROM RolFamilias
                   WHERE IdRol = 'Gerente' AND IdFamilia = 'VisualizacionUsuarios')
        INSERT INTO RolFamilias (IdRol, IdFamilia)
        VALUES ('Gerente', 'VisualizacionUsuarios');

    IF NOT EXISTS (SELECT 1 FROM RolFamilias
                   WHERE IdRol = 'Gerente' AND IdFamilia = 'EdicionUsuarios')
        INSERT INTO RolFamilias (IdRol, IdFamilia)
        VALUES ('Gerente', 'EdicionUsuarios');

    IF NOT EXISTS (SELECT 1 FROM RolFamilias
                   WHERE IdRol = 'Gerente' AND IdFamilia = 'GestionBitacora')
        INSERT INTO RolFamilias (IdRol, IdFamilia)
        VALUES ('Gerente', 'GestionBitacora');

    COMMIT TRANSACTION;
    PRINT 'Roles Gerente y UsuarioGeneral creados correctamente.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
