-- ============================================================
--  10_seed_admin.sql  —  Usuario administrador inicial.
--
--  Crea un usuario administrador para poder entrar al sistema
--  recién instalado. Si ya existe algún usuario con ese Login o
--  DNI, no hace nada (idempotente).
--
--  Credenciales iniciales:
--     Login:      admin
--     Contraseña: Admin1234
--
--  La contraseña se guarda como hash SHA-256 en Base64, igual que
--  lo genera SER/Encriptador/EncriptacionSER06AV.cs. El usuario se
--  crea con DebeCambiarContrasenia = 1, así el sistema obliga a
--  cambiarla en el primer inicio de sesión.
-- ============================================================

SET NOCOUNT ON;

DECLARE @IdRolAdmin NVARCHAR(450);
SELECT @IdRolAdmin = Id
FROM   Roles
WHERE  Descripcion = 'Administrador' OR Codigo = 'ADM';

IF @IdRolAdmin IS NULL
BEGIN
    RAISERROR('No se encontró el rol Administrador. Ejecutá antes 02_seed_patentes_familias_rol.sql.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Login = 'admin' OR Dni = '99999999')
BEGIN
    INSERT INTO Usuarios
        (Dni, Nombre, Apellido, Email, IdRol, Activo, Bloqueado,
         Login, Contrasenia, DebeCambiarContrasenia, Idioma)
    VALUES
        ('99999999', 'Administrador', 'Sistema', 'admin@local',
         @IdRolAdmin, 1, 0, 'admin',
         'YP50QG5/NT7ZefNQ8vu2ouhpCl+n0bDDKYPR2LP5X2c=', 1, 'es');

    PRINT 'Usuario administrador inicial creado (login: admin / contraseña: Admin1234).';
END
ELSE
BEGIN
    PRINT 'El usuario administrador ya existía: no se creó de nuevo.';
END
GO
