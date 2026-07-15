-- ============================================================
--  99_full_install.sql  —  SCRIPT MAESTRO GENERAL
--  IngSoftValdezAlegre — instalación COMPLETA de la base.
--
--  Deja el sistema listo para usar de cero: crea la base (si no
--  existe), todo el esquema (seguridad + auditoría + integridad +
--  PC Factory), los procedimientos almacenados, los seeds y los
--  DATOS DE DEMOSTRACIÓN.
--
--  USO:  abrir en SSMS y ejecutar (F5). No hace falta elegir base:
--        el script la crea y hace USE por sí mismo. Es IDEMPOTENTE,
--        se puede correr varias veces sin romper nada.
--
--  Credenciales iniciales:  Login: admin   Contraseña: Admin1234
--  (el sistema obliga a cambiarla en el primer inicio de sesión).
--
--  Generado a partir de los scripts individuales de la carpeta SQL\.
--  Excluidos a propósito (no aportan al alta limpia):
--    - 07_tabla_dv.sql              (00_schema ya crea la tabla DV)
--    - 08_patente_reparacion_dv.sql (reemplazado por 11, evita duplicar patente)
--    - 09_fix_patentes_duplicadas_rol.sql (mantenimiento puntual, rol fijo)
-- ============================================================

-- ── 0) Crear la base si no existe y posicionarse en ella ──────
IF DB_ID(N'IngSoftValdezAlegre') IS NULL
    CREATE DATABASE [IngSoftValdezAlegre];
GO
USE [IngSoftValdezAlegre];
GO


-- ============================================================
-- ==== 00_schema.sql
-- ==== Esquema base: tablas de seguridad, auditoría e integridad (DV)
-- ============================================================
-- ============================================================
--  00_schema.sql  —  Estructura base de la base de datos
--  IngSoftValdezAlegre (Gestión de Usuarios / Seguridad).
--
--  Este script crea TODAS las tablas del modelo de seguridad
--  (usuarios, roles, familias, patentes y sus relaciones) más
--  las tablas de auditoría (Bitacora, IntentosLogin) y la de
--  integridad (DV). Es idempotente: se puede correr varias veces
--  sin romper nada, porque cada objeto se crea solo si no existe.
--
--  Lo ejecuta el Instalador ANTES que los seeds y procedimientos.
-- ============================================================

SET NOCOUNT ON;
GO

-- ── ROLES ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
CREATE TABLE Roles (
    Id          NVARCHAR(450) NOT NULL PRIMARY KEY,
    Descripcion NVARCHAR(255) NOT NULL,
    Codigo      NVARCHAR(50)  NULL
);
GO

-- ── FAMILIAS ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Familias')
CREATE TABLE Familias (
    Id          NVARCHAR(450) NOT NULL PRIMARY KEY,
    Descripcion NVARCHAR(255) NOT NULL
);
GO

-- ── PATENTES ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Patentes')
CREATE TABLE Patentes (
    Id          NVARCHAR(100) NOT NULL PRIMARY KEY,
    Descripcion NVARCHAR(255) NOT NULL
);
GO

-- ── USUARIOS ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Usuarios')
CREATE TABLE Usuarios (
    Dni                    NVARCHAR(20)  NOT NULL PRIMARY KEY,
    Nombre                 NVARCHAR(100) NOT NULL,
    Apellido               NVARCHAR(100) NOT NULL,
    Email                  NVARCHAR(255) NULL,
    IdRol                  NVARCHAR(450) NULL,
    Activo                 BIT           NOT NULL CONSTRAINT DF_Usuarios_Activo   DEFAULT (1),
    Bloqueado              BIT           NOT NULL CONSTRAINT DF_Usuarios_Bloqueado DEFAULT (0),
    Login                  NVARCHAR(100) NOT NULL,
    Contrasenia            NVARCHAR(255) NOT NULL,
    DebeCambiarContrasenia BIT           NOT NULL CONSTRAINT DF_Usuarios_DebeCambiar DEFAULT (0),
    Idioma                 NVARCHAR(10)  NULL,
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (IdRol) REFERENCES Roles(Id)
);
GO

-- ── RELACIÓN ROL → PATENTES ──────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RolPatentes')
CREATE TABLE RolPatentes (
    IdRol     NVARCHAR(450) NOT NULL,
    IdPatente NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_RolPatentes PRIMARY KEY (IdRol, IdPatente),
    CONSTRAINT FK_RolPatentes_Roles    FOREIGN KEY (IdRol)     REFERENCES Roles(Id),
    CONSTRAINT FK_RolPatentes_Patentes FOREIGN KEY (IdPatente) REFERENCES Patentes(Id)
);
GO

-- ── RELACIÓN ROL → FAMILIAS ──────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RolFamilias')
CREATE TABLE RolFamilias (
    IdRol     NVARCHAR(450) NOT NULL,
    IdFamilia NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_RolFamilias PRIMARY KEY (IdRol, IdFamilia),
    CONSTRAINT FK_RolFamilias_Roles    FOREIGN KEY (IdRol)     REFERENCES Roles(Id),
    CONSTRAINT FK_RolFamilias_Familias FOREIGN KEY (IdFamilia) REFERENCES Familias(Id)
);
GO

-- ── RELACIÓN FAMILIA → PATENTES ──────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FamiliaPatentes')
CREATE TABLE FamiliaPatentes (
    IdFamilia NVARCHAR(450) NOT NULL,
    IdPatente NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_FamiliaPatentes PRIMARY KEY (IdFamilia, IdPatente),
    CONSTRAINT FK_FamiliaPatentes_Familias FOREIGN KEY (IdFamilia) REFERENCES Familias(Id),
    CONSTRAINT FK_FamiliaPatentes_Patentes FOREIGN KEY (IdPatente) REFERENCES Patentes(Id)
);
GO

-- ── JERARQUÍA FAMILIA → FAMILIA ──────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FamiliaFamilias')
CREATE TABLE FamiliaFamilias (
    IdPadre NVARCHAR(450) NOT NULL,
    IdHijo  NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_FamiliaFamilias PRIMARY KEY (IdPadre, IdHijo),
    CONSTRAINT FK_FamiliaFamilias_Padre FOREIGN KEY (IdPadre) REFERENCES Familias(Id),
    CONSTRAINT FK_FamiliaFamilias_Hijo  FOREIGN KEY (IdHijo)  REFERENCES Familias(Id)
);
GO

-- ── AUDITORÍA: BITÁCORA ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bitacora')
CREATE TABLE Bitacora (
    Id          NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Codigo      NVARCHAR(50)  NULL,
    Categoria   NVARCHAR(50)  NULL,
    Criticidad  NVARCHAR(50)  NULL,
    Descripcion NVARCHAR(MAX) NULL,
    Fecha       DATETIME      NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT (GETDATE()),
    Modulo      NVARCHAR(50)  NULL,
    UsuarioDni  NVARCHAR(20)  NULL
);
GO

-- ── AUDITORÍA: INTENTOS DE LOGIN ─────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IntentosLogin')
CREATE TABLE IntentosLogin (
    Id         NVARCHAR(50) NOT NULL PRIMARY KEY,
    UsuarioDni NVARCHAR(20) NOT NULL,
    Fecha      DATETIME     NOT NULL CONSTRAINT DF_IntentosLogin_Fecha DEFAULT (GETDATE()),
    Exitoso    BIT          NOT NULL
);
GO

-- ── INTEGRIDAD: DÍGITO VERIFICADOR ───────────────────────────
-- (La app también la crea en tiempo de ejecución; se incluye acá
--  para dejar la BD completa desde la instalación.)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DV')
CREATE TABLE DV (
    Tabla NVARCHAR(128) NOT NULL PRIMARY KEY,
    DVH   NVARCHAR(64)  NOT NULL,
    DVV   NVARCHAR(64)  NOT NULL
);
GO

PRINT 'Esquema base creado/verificado correctamente.';
GO
GO


-- ============================================================
-- ==== 01_sp_ObtenerPatentesUsuario.sql
-- ==== SP: patentes efectivas de un usuario
-- ============================================================
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
GO


-- ============================================================
-- ==== 02_seed_patentes_familias_rol.sql
-- ==== Seed: patentes, familias y rol Administrador
-- ============================================================
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
GO


-- ============================================================
-- ==== 03_seed_roles_gerente_usuariogeneral.sql
-- ==== Seed: roles Gerente y Usuario General
-- ============================================================
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
GO


-- ============================================================
-- ==== 04_seed_patentes_gestion_roles_familias.sql
-- ==== Seed: patentes de gestión de roles/familias
-- ============================================================
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
GO


-- ============================================================
-- ==== 05_sp_patentes_crud.sql
-- ==== SP: ABM de patentes
-- ============================================================
-- ============================================================
--  ABM de Patentes: altas, modificaciones y bajas.
--
--  Hasta ahora Patentes solo se consumía en modo lectura
--  (sp_Patentes_ObtenerTodos / sp_Patentes_ObtenerPorId). Esta
--  pantalla nueva permite gestionarlas directamente, así que se
--  agregan los 3 procedimientos de escritura que faltaban.
--
--  El Id de una patente debe coincidir EXACTAMENTE con el nombre
--  de un valor del enum PatenteEnum06AV (ver SER/Enums/PatenteEnum06AV.cs),
--  por eso se ingresa a mano en la pantalla (no se genera random como
--  en Roles/Familias) y queda bloqueado para edición una vez creada.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Patentes_Agregar
    @Id NVARCHAR(100),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Patentes (Id, Descripcion) VALUES (@Id, @Descripcion);
END
GO

CREATE OR ALTER PROCEDURE sp_Patentes_Modificar
    @Id NVARCHAR(100),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Patentes SET Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Patentes_Eliminar
    @Id NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Patentes WHERE Id = @Id;
END
GO
GO


-- ============================================================
-- ==== 06_seed_patente_gestion_patentes.sql
-- ==== Seed: patente de gestión de patentes
-- ============================================================
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
GO


-- ============================================================
-- ==== 07_sp_roles.sql
-- ==== SP: ABM de roles
-- ============================================================
-- ============================================================
--  07_sp_roles.sql  —  Procedimientos de la entidad Roles.
--  Consumidos por DAL/RolesDAL06AV.cs (EjecutarSP / EjecutarSPNonQuery).
--  Los nombres de columnas devueltos (Id, Descripcion, Codigo)
--  coinciden con lo que espera MPP/RolesMPP06AV.cs.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion, Codigo FROM Roles ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerPorId
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion, Codigo FROM Roles WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_Agregar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255),
    @Codigo      NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Roles (Id, Descripcion, Codigo) VALUES (@Id, @Descripcion, @Codigo);
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_Modificar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Roles SET Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_Eliminar
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    -- Se limpian primero las relaciones para no violar las FK.
    DELETE FROM RolPatentes  WHERE IdRol = @Id;
    DELETE FROM RolFamilias  WHERE IdRol = @Id;
    DELETE FROM Roles        WHERE Id    = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerPatentes
    @IdRol NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Descripcion
    FROM   Patentes p
    INNER JOIN RolPatentes rp ON rp.IdPatente = p.Id
    WHERE  rp.IdRol = @IdRol;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_AgregarPatente
    @IdRol     NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRol AND IdPatente = @IdPatente)
        INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRol, @IdPatente);
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_QuitarPatente
    @IdRol     NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RolPatentes WHERE IdRol = @IdRol AND IdPatente = @IdPatente;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_ObtenerFamilias
    @IdRol NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id, f.Descripcion
    FROM   Familias f
    INNER JOIN RolFamilias rf ON rf.IdFamilia = f.Id
    WHERE  rf.IdRol = @IdRol;
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_AgregarFamilia
    @IdRol     NVARCHAR(450),
    @IdFamilia NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM RolFamilias WHERE IdRol = @IdRol AND IdFamilia = @IdFamilia)
        INSERT INTO RolFamilias (IdRol, IdFamilia) VALUES (@IdRol, @IdFamilia);
END
GO

CREATE OR ALTER PROCEDURE sp_Roles_QuitarFamilia
    @IdRol     NVARCHAR(450),
    @IdFamilia NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM RolFamilias WHERE IdRol = @IdRol AND IdFamilia = @IdFamilia;
END
GO

PRINT 'Procedimientos de Roles creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 08_sp_familias.sql
-- ==== SP: ABM de familias
-- ============================================================
-- ============================================================
--  08_sp_familias.sql  —  Procedimientos de la entidad Familias.
--  Consumidos por DAL/FamiliaDAL06AV.cs.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion FROM Familias ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerPorId
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Descripcion FROM Familias WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_Agregar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Familias (Id, Descripcion) VALUES (@Id, @Descripcion);
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_Modificar
    @Id          NVARCHAR(450),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Familias SET Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_Eliminar
    @Id NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    -- Se limpian las relaciones donde participa la familia.
    DELETE FROM FamiliaPatentes WHERE IdFamilia = @Id;
    DELETE FROM FamiliaFamilias WHERE IdPadre   = @Id OR IdHijo = @Id;
    DELETE FROM RolFamilias     WHERE IdFamilia = @Id;
    DELETE FROM Familias        WHERE Id        = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerPatentes
    @IdFamilia NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Descripcion
    FROM   Patentes p
    INNER JOIN FamiliaPatentes fp ON fp.IdPatente = p.Id
    WHERE  fp.IdFamilia = @IdFamilia;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_AgregarPatente
    @IdFamilia NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM FamiliaPatentes WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente)
        INSERT INTO FamiliaPatentes (IdFamilia, IdPatente) VALUES (@IdFamilia, @IdPatente);
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_QuitarPatente
    @IdFamilia NVARCHAR(450),
    @IdPatente NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM FamiliaPatentes WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_ObtenerSubfamilias
    @IdPadre NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id, f.Descripcion
    FROM   Familias f
    INNER JOIN FamiliaFamilias ff ON ff.IdHijo = f.Id
    WHERE  ff.IdPadre = @IdPadre;
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_AgregarSubfamilia
    @IdPadre NVARCHAR(450),
    @IdHijo  NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM FamiliaFamilias WHERE IdPadre = @IdPadre AND IdHijo = @IdHijo)
        INSERT INTO FamiliaFamilias (IdPadre, IdHijo) VALUES (@IdPadre, @IdHijo);
END
GO

CREATE OR ALTER PROCEDURE sp_Familias_QuitarSubfamilia
    @IdPadre NVARCHAR(450),
    @IdHijo  NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM FamiliaFamilias WHERE IdPadre = @IdPadre AND IdHijo = @IdHijo;
END
GO

PRINT 'Procedimientos de Familias creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 09_sp_patentes_lectura.sql
-- ==== SP: lectura de patentes
-- ============================================================
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
GO


-- ============================================================
-- ==== 10_seed_admin.sql
-- ==== Seed: usuario admin inicial (admin / Admin1234)
-- ============================================================
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
GO


-- ============================================================
-- ==== 11_seed_patente_reparar_integridad.sql
-- ==== Seed: patente RepararIntegridad (directa al rol admin, lookup dinámico)
-- ============================================================
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
    VALUES ('RepararIntegridad', 'Reparar integridad (Dígito Verificador)
GO


-- ============================================================
-- ==== 20_pcfactory_clientes.sql
-- ==== PC Factory: Clientes + ABM
-- ============================================================
-- ============================================================
--  20_pcfactory_clientes.sql
--  Tabla Clientes (PC Factory) + procedimientos ABM.
--  Consumidos por DAL/ClientesDAL06AV.cs (EjecutarSP / EjecutarSPNonQuery).
--  Los nombres de columnas coinciden con MPP/ClientesMPP06AV.cs.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clientes')
CREATE TABLE Clientes (
    Dni       NVARCHAR(20)  NOT NULL PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL,
    Apellido  NVARCHAR(100) NOT NULL,
    Telefono  NVARCHAR(50)  NULL,
    Direccion NVARCHAR(200) NULL
);
GO

CREATE OR ALTER PROCEDURE sp_Clientes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Dni, Nombre, Apellido, Telefono, Direccion
    FROM   Clientes
    ORDER BY Apellido, Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_ObtenerPorDni
    @Dni NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Dni, Nombre, Apellido, Telefono, Direccion
    FROM   Clientes
    WHERE  Dni = @Dni;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Agregar
    @Dni       NVARCHAR(20),
    @Nombre    NVARCHAR(100),
    @Apellido  NVARCHAR(100),
    @Telefono  NVARCHAR(50),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion)
    VALUES (@Dni, @Nombre, @Apellido, @Telefono, @Direccion);
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Modificar
    @Dni       NVARCHAR(20),
    @Nombre    NVARCHAR(100),
    @Apellido  NVARCHAR(100),
    @Telefono  NVARCHAR(50),
    @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Clientes
    SET    Nombre = @Nombre, Apellido = @Apellido,
           Telefono = @Telefono, Direccion = @Direccion
    WHERE  Dni = @Dni;
END
GO

CREATE OR ALTER PROCEDURE sp_Clientes_Eliminar
    @Dni NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Clientes WHERE Dni = @Dni;
END
GO

PRINT 'Tabla Clientes y procedimientos ABM creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 21_pcfactory_componentes.sql
-- ==== PC Factory: Componentes + ABM
-- ============================================================
-- ============================================================
--  21_pcfactory_componentes.sql
--  Tabla Componentes (PC Factory) + procedimientos ABM.
--  Tipo se guarda como int (ordinal del enum BE.TipoComponente06AV).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Componentes')
CREATE TABLE Componentes (
    Codigo          NVARCHAR(50)   NOT NULL PRIMARY KEY,
    Descripcion     NVARCHAR(200)  NOT NULL,
    Tipo            INT            NOT NULL,
    Marca           NVARCHAR(100)  NULL,
    Modelo          NVARCHAR(100)  NULL,
    PrecioUnitario  DECIMAL(12,2)  NOT NULL CONSTRAINT DF_Comp_Precio DEFAULT (0),
    StockDisponible INT            NOT NULL CONSTRAINT DF_Comp_Stock   DEFAULT (0)
);
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible
    FROM   Componentes ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible
    FROM   Componentes WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @StockDisponible INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible)
    VALUES (@Codigo, @Descripcion, @Tipo, @Marca, @Modelo, @PrecioUnitario, @StockDisponible);
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Tipo INT,
    @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(12,2), @StockDisponible INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET Descripcion = @Descripcion, Tipo = @Tipo, Marca = @Marca, Modelo = @Modelo,
        PrecioUnitario = @PrecioUnitario, StockDisponible = @StockDisponible
    WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Componentes_Eliminar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Componentes WHERE Codigo = @Codigo;
END
GO

-- Descuenta stock de un componente al usarlo en una orden de producción (RFN1).
-- Solo descuenta si hay stock suficiente; si no, no toca nada y avisa con error.
CREATE OR ALTER PROCEDURE sp_Componentes_DescontarStock
    @Codigo   NVARCHAR(50),
    @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Componentes
    SET    StockDisponible = StockDisponible - @Cantidad
    WHERE  Codigo = @Codigo AND StockDisponible >= @Cantidad;

    IF @@ROWCOUNT = 0
        THROW 51000, 'Stock insuficiente o componente inexistente al descontar stock.', 1;
END
GO

PRINT 'Tabla Componentes y procedimientos ABM creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 22_pcfactory_insumos.sql
-- ==== PC Factory: Insumos + ABM
-- ============================================================
-- ============================================================
--  22_pcfactory_insumos.sql
--  Tabla Insumos (PC Factory) + procedimientos ABM + bajo stock.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Insumos')
CREATE TABLE Insumos (
    Codigo      NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Descripcion NVARCHAR(200) NOT NULL,
    Stock       INT           NOT NULL CONSTRAINT DF_Ins_Stock DEFAULT (0),
    StockMinimo INT           NOT NULL CONSTRAINT DF_Ins_Min   DEFAULT (0)
);
GO

CREATE OR ALTER PROCEDURE sp_Insumos_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Stock, StockMinimo FROM Insumos ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Stock, StockMinimo FROM Insumos WHERE Codigo = @Codigo;
END
GO

-- Insumos por debajo (o en) su stock mínimo: usado por RFN2 para resaltarlos.
CREATE OR ALTER PROCEDURE sp_Insumos_ObtenerBajoStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Stock, StockMinimo
    FROM   Insumos WHERE Stock <= StockMinimo ORDER BY Descripcion;
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Stock INT, @StockMinimo INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Insumos (Codigo, Descripcion, Stock, StockMinimo)
    VALUES (@Codigo, @Descripcion, @Stock, @StockMinimo);
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Stock INT, @StockMinimo INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Insumos SET Descripcion = @Descripcion, Stock = @Stock, StockMinimo = @StockMinimo
    WHERE Codigo = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Insumos_Eliminar
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Insumos WHERE Codigo = @Codigo;
END
GO

PRINT 'Tabla Insumos y procedimientos ABM creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 23_pcfactory_proveedores.sql
-- ==== PC Factory: Proveedores + ABM
-- ============================================================
-- ============================================================
--  23_pcfactory_proveedores.sql
--  Tabla Proveedores (PC Factory) + procedimientos ABM.
--  Id es autonumérico; el Agregar devuelve el Id generado.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Proveedores')
CREATE TABLE Proveedores (
    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre    NVARCHAR(150) NOT NULL,
    Cuit      NVARCHAR(20)  NOT NULL,
    Email     NVARCHAR(150) NULL,
    Telefono  NVARCHAR(50)  NULL,
    Direccion NVARCHAR(200) NULL,
    CONSTRAINT UQ_Proveedores_Cuit UNIQUE (Cuit)
);
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorCuit
    @Cuit NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion FROM Proveedores WHERE Cuit = @Cuit;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Agregar
    @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion)
    VALUES (@Nombre, @Cuit, @Email, @Telefono, @Direccion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Proveedores
    SET Nombre = @Nombre, Cuit = @Cuit, Email = @Email,
        Telefono = @Telefono, Direccion = @Direccion
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Proveedores WHERE Id = @Id;
END
GO

PRINT 'Tabla Proveedores y procedimientos ABM creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 24_pcfactory_lineas.sql
-- ==== PC Factory: Líneas de ensamblaje + ABM
-- ============================================================
-- ============================================================
--  24_pcfactory_lineas.sql
--  Tabla LineasEnsamblaje (PC Factory) + procedimientos ABM.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LineasEnsamblaje')
CREATE TABLE LineasEnsamblaje (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(200) NULL,
    Disponible  BIT NOT NULL CONSTRAINT DF_Linea_Disp DEFAULT (1)
);
GO

CREATE OR ALTER PROCEDURE sp_Lineas_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion, Disponible FROM LineasEnsamblaje ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion, Disponible FROM LineasEnsamblaje WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Agregar
    @Nombre NVARCHAR(100), @Descripcion NVARCHAR(200), @Disponible BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible)
    VALUES (@Nombre, @Descripcion, @Disponible);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Modificar
    @Id INT, @Nombre NVARCHAR(100), @Descripcion NVARCHAR(200), @Disponible BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE LineasEnsamblaje
    SET Nombre = @Nombre, Descripcion = @Descripcion, Disponible = @Disponible
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Lineas_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM LineasEnsamblaje WHERE Id = @Id;
END
GO

PRINT 'Tabla LineasEnsamblaje y procedimientos ABM creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 25_pcfactory_produccion.sql
-- ==== PC Factory: Producción (descuento de stock)
-- ============================================================
-- ============================================================
--  25_pcfactory_produccion.sql   (RFN1: Venta / Producción)
--  Computadoras + sus componentes, Órdenes de Producción y Pagos.
--  Estados de OP (int): 0 Pendiente, 1 Planificada, 2 EnEnsamblaje,
--                       3 Finalizada, 4 Entregada.
--  Tipo de pago (int): 0 Sena, 1 SaldoFinal.
-- ============================================================

-- ── Computadora solicitada (estándar o personalizada) ────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Computadoras')
CREATE TABLE Computadoras (
    Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre            NVARCHAR(150) NULL,
    TipoConfiguracion INT           NOT NULL,   -- 0 Estandar, 1 Personalizada
    PrecioTotal       DECIMAL(12,2) NOT NULL CONSTRAINT DF_Comp_Prec DEFAULT (0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComputadoraComponentes')
CREATE TABLE ComputadoraComponentes (
    IdComputadora   INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_CompComp PRIMARY KEY (IdComputadora, CodigoComponente),
    CONSTRAINT FK_CompComp_Comp FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_CompComp_Componente FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── Orden de producción ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesProduccion')
CREATE TABLE OrdenesProduccion (
    NumeroOrden         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DniCliente          NVARCHAR(20)  NOT NULL,
    IdComputadora       INT           NOT NULL,
    FechaEntrega        DATE          NOT NULL,
    Estado              INT           NOT NULL CONSTRAINT DF_OP_Estado DEFAULT (0),
    IdLinea             INT           NULL,
    FechaInicioPrevista DATE          NULL,
    ResponsableTecnico  NVARCHAR(150) NULL,
    CONSTRAINT FK_OP_Cliente FOREIGN KEY (DniCliente) REFERENCES Clientes(Dni),
    CONSTRAINT FK_OP_Computadora FOREIGN KEY (IdComputadora) REFERENCES Computadoras(Id),
    CONSTRAINT FK_OP_Linea FOREIGN KEY (IdLinea) REFERENCES LineasEnsamblaje(Id)
);
GO

-- ── Pagos (seña / saldo final) ───────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Pagos')
CREATE TABLE Pagos (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroOrden INT           NOT NULL,
    Tipo        INT           NOT NULL,   -- 0 Sena, 1 SaldoFinal
    Monto       DECIMAL(12,2) NOT NULL,
    Fecha       DATETIME      NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (GETDATE()),
    CONSTRAINT FK_Pago_OP FOREIGN KEY (NumeroOrden) REFERENCES OrdenesProduccion(NumeroOrden)
);
GO

-- ── SPs Computadora ──────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Computadoras_Agregar
    @Nombre NVARCHAR(150), @TipoConfiguracion INT, @PrecioTotal DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Computadoras (Nombre, TipoConfiguracion, PrecioTotal)
    VALUES (@Nombre, @TipoConfiguracion, @PrecioTotal);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_AgregarComponente
    @IdComputadora INT, @CodigoComponente NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ComputadoraComponentes
                   WHERE IdComputadora = @IdComputadora AND CodigoComponente = @CodigoComponente)
        INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente)
        VALUES (@IdComputadora, @CodigoComponente);
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerComponentes
    @IdComputadora INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Codigo, c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.PrecioUnitario, c.StockDisponible
    FROM   Componentes c
    INNER JOIN ComputadoraComponentes cc ON cc.CodigoComponente = c.Codigo
    WHERE  cc.IdComputadora = @IdComputadora;
END
GO

CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, TipoConfiguracion, PrecioTotal FROM Computadoras WHERE Id = @Id;
END
GO

-- ── SPs Orden de producción ──────────────────────────────────
CREATE OR ALTER PROCEDURE sp_OP_Agregar
    @DniCliente NVARCHAR(20), @IdComputadora INT, @FechaEntrega DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesProduccion (DniCliente, IdComputadora, FechaEntrega, Estado)
    VALUES (@DniCliente, @IdComputadora, @FechaEntrega, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, DniCliente, IdComputadora, FechaEntrega, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico
    FROM   OrdenesProduccion ORDER BY NumeroOrden DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_ObtenerPorNumero
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroOrden, DniCliente, IdComputadora, FechaEntrega, Estado,
           IdLinea, FechaInicioPrevista, ResponsableTecnico
    FROM   OrdenesProduccion WHERE NumeroOrden = @NumeroOrden;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_Planificar
    @NumeroOrden INT, @IdLinea INT, @FechaInicioPrevista DATE, @ResponsableTecnico NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion
    SET IdLinea = @IdLinea, FechaInicioPrevista = @FechaInicioPrevista,
        ResponsableTecnico = @ResponsableTecnico, Estado = 1   -- Planificada
    WHERE NumeroOrden = @NumeroOrden;
END
GO

CREATE OR ALTER PROCEDURE sp_OP_CambiarEstado
    @NumeroOrden INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesProduccion SET Estado = @Estado WHERE NumeroOrden = @NumeroOrden;
END
GO

-- ── SPs Pagos ────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Pagos_Agregar
    @NumeroOrden INT, @Tipo INT, @Monto DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Pagos (NumeroOrden, Tipo, Monto) VALUES (@NumeroOrden, @Tipo, @Monto);
END
GO

CREATE OR ALTER PROCEDURE sp_Pagos_ObtenerPorOrden
    @NumeroOrden INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, NumeroOrden, Tipo, Monto, Fecha FROM Pagos WHERE NumeroOrden = @NumeroOrden;
END
GO

PRINT 'Tablas y procedimientos de Producción (RFN1) creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 26_pcfactory_compras.sql
-- ==== PC Factory: Compras/cotización
-- ============================================================
-- ============================================================
--  26_pcfactory_compras.sql   (RFN2: Compras / Insumos)
--  Órdenes de compra (+ detalle) y pedidos de cotización.
--  Estado OC (int):        0 Pendiente, 1 Enviada, 2 Finalizada.
--  Estado Cotización (int):0 PorAprobar, 1 Aprobado, 2 Desaprobada.
--  La cotización referencia la OC y reutiliza su detalle de insumos.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenesCompra')
CREATE TABLE OrdenesCompra (
    NumeroCompra        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FechaLimite         DATE          NOT NULL,
    RepositorSolicitante NVARCHAR(150) NULL,
    Estado              INT           NOT NULL CONSTRAINT DF_OC_Estado DEFAULT (0),
    FechaCierre         DATETIME      NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrdenCompraDetalle')
CREATE TABLE OrdenCompraDetalle (
    NumeroCompra INT          NOT NULL,
    CodigoInsumo NVARCHAR(50) NOT NULL,
    Cantidad     INT          NOT NULL,
    CONSTRAINT PK_OCD PRIMARY KEY (NumeroCompra, CodigoInsumo),
    CONSTRAINT FK_OCD_OC FOREIGN KEY (NumeroCompra) REFERENCES OrdenesCompra(NumeroCompra),
    CONSTRAINT FK_OCD_Insumo FOREIGN KEY (CodigoInsumo) REFERENCES Insumos(Codigo)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PedidosCotizacion')
CREATE TABLE PedidosCotizacion (
    Numero       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NumeroCompra INT       NOT NULL,
    IdProveedor  INT       NOT NULL,
    FechaEmision DATETIME  NOT NULL CONSTRAINT DF_Cot_Fecha DEFAULT (GETDATE()),
    Estado       INT       NOT NULL CONSTRAINT DF_Cot_Estado DEFAULT (0),
    Costo        DECIMAL(12,2) NULL,
    Condiciones  NVARCHAR(500) NULL,
    CONSTRAINT FK_Cot_OC FOREIGN KEY (NumeroCompra) REFERENCES OrdenesCompra(NumeroCompra),
    CONSTRAINT FK_Cot_Prov FOREIGN KEY (IdProveedor) REFERENCES Proveedores(Id)
);
GO

-- Para bases ya creadas: agrega las columnas de precio/condiciones si faltan.
IF COL_LENGTH('PedidosCotizacion','Costo') IS NULL
    ALTER TABLE PedidosCotizacion ADD Costo DECIMAL(12,2) NULL;
IF COL_LENGTH('PedidosCotizacion','Condiciones') IS NULL
    ALTER TABLE PedidosCotizacion ADD Condiciones NVARCHAR(500) NULL;
GO

-- ── SPs Orden de compra ──────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_OC_Agregar
    @FechaLimite DATE, @RepositorSolicitante NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO OrdenesCompra (FechaLimite, RepositorSolicitante, Estado)
    VALUES (@FechaLimite, @RepositorSolicitante, 0);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_AgregarDetalle
    @NumeroCompra INT, @CodigoInsumo NVARCHAR(50), @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM OrdenCompraDetalle
                   WHERE NumeroCompra = @NumeroCompra AND CodigoInsumo = @CodigoInsumo)
        INSERT INTO OrdenCompraDetalle (NumeroCompra, CodigoInsumo, Cantidad)
        VALUES (@NumeroCompra, @CodigoInsumo, @Cantidad);
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroCompra, FechaLimite, RepositorSolicitante, Estado, FechaCierre
    FROM   OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerPorNumero
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NumeroCompra, FechaLimite, RepositorSolicitante, Estado, FechaCierre
    FROM   OrdenesCompra WHERE NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_ObtenerDetalle
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.CodigoInsumo, d.Cantidad, i.Descripcion, i.Stock, i.StockMinimo
    FROM   OrdenCompraDetalle d
    INNER JOIN Insumos i ON i.Codigo = d.CodigoInsumo
    WHERE  d.NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_CambiarEstado
    @NumeroCompra INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado = @Estado WHERE NumeroCompra = @NumeroCompra;
END
GO

CREATE OR ALTER PROCEDURE sp_OC_Finalizar
    @NumeroCompra INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado = 2, FechaCierre = GETDATE() WHERE NumeroCompra = @NumeroCompra;
END
GO

-- ── SPs Cotización ───────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Cotizacion_Agregar
    @NumeroCompra INT, @IdProveedor INT,
    @Costo DECIMAL(12,2), @Condiciones NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PedidosCotizacion (NumeroCompra, IdProveedor, Estado, Costo, Condiciones)
    VALUES (@NumeroCompra, @IdProveedor, 0, @Costo, @Condiciones);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoNumero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Numero, NumeroCompra, IdProveedor, FechaEmision, Estado, Costo, Condiciones
    FROM   PedidosCotizacion ORDER BY Numero DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerPorNumero
    @Numero INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Numero, NumeroCompra, IdProveedor, FechaEmision, Estado, Costo, Condiciones
    FROM   PedidosCotizacion WHERE Numero = @Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_Cotizacion_CambiarEstado
    @Numero INT, @Estado INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PedidosCotizacion SET Estado = @Estado WHERE Numero = @Numero;
END
GO

-- ── Actualización de stock al recibir ────────────────────────
CREATE OR ALTER PROCEDURE sp_Insumos_SumarStock
    @Codigo NVARCHAR(50), @Cantidad INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Insumos SET Stock = Stock + @Cantidad WHERE Codigo = @Codigo;
END
GO

PRINT 'Tablas y procedimientos de Compras (RFN2) creados/actualizados.';
GO
GO


-- ============================================================
-- ==== 27_pcfactory_patentes.sql
-- ==== PC Factory: patentes del módulo
-- ============================================================
-- ============================================================
--  27_pcfactory_patentes.sql
--  Patentes de acceso a las pantallas del dominio PC Factory.
--
--  Cada patente habilita la visibilidad de un módulo del sidebar
--  (ver ConfigurarModulosPcFactory / AgregarModuloPcFactory en
--   FRMMain.cs). Sin la patente, el módulo NO se agrega al menú.
--
--  Los Ids deben coincidir EXACTAMENTE con los valores del enum
--  PatenteEnum06AV (SER/Enums/PatenteEnum06AV.cs).
--
--  Se asignan directamente al rol Administrador (RolPatentes), igual
--  que GestionarRoles / GestionarFamilias.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- ── PATENTES ─────────────────────────────────────────────
    DECLARE @Patentes TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
    INSERT INTO @Patentes (Id, Descripcion) VALUES
        ('GestionarClientes',          'Gestionar clientes'),
        ('GestionarComponentes',       'Gestionar componentes'),
        ('GestionarInsumos',           'Gestionar insumos'),
        ('GestionarProveedores',       'Gestionar proveedores'),
        ('GestionarLineasEnsamblaje',  'Gestionar líneas de ensamblaje'),
        ('GestionarProduccion',        'Gestionar producción'),
        ('GestionarCompras',           'Gestionar compras');

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
    PRINT 'Seed de patentes PC Factory completado (asignadas al rol Administrador).';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error en el seed de patentes PC Factory: ' + ERROR_MESSAGE();
END CATCH
GO
GO


-- ============================================================
-- ==== 28_pcfactory_modelos.sql
-- ==== PC Factory: catálogo de modelos estándar
-- ============================================================
-- ============================================================
--  28_pcfactory_modelos.sql
--  Catálogo de modelos de computadora ESTÁNDAR (RFN1). Cada modelo
--  tiene un nombre y una lista de componentes predefinidos. Al armar
--  una orden "Estándar" se elige un modelo y se cargan sus componentes.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModelosEstandar')
CREATE TABLE ModelosEstandar (
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre      NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(300) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModeloEstandarComponentes')
CREATE TABLE ModeloEstandarComponentes (
    IdModelo         INT          NOT NULL,
    CodigoComponente NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_ModeloComp PRIMARY KEY (IdModelo, CodigoComponente),
    CONSTRAINT FK_ModeloComp_Modelo FOREIGN KEY (IdModelo)
        REFERENCES ModelosEstandar(Id),
    CONSTRAINT FK_ModeloComp_Comp FOREIGN KEY (CodigoComponente)
        REFERENCES Componentes(Codigo)
);
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Descripcion FROM ModelosEstandar ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoComponente FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Agregar
    @Nombre NVARCHAR(150), @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ModelosEstandar (Nombre, Descripcion) VALUES (@Nombre, @Descripcion);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Descripcion NVARCHAR(300)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ModelosEstandar SET Nombre = @Nombre, Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ModeloEstandarComponentes WHERE IdModelo = @Id;
    DELETE FROM ModelosEstandar WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_AgregarComponente
    @IdModelo INT, @CodigoComponente NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes
                   WHERE IdModelo = @IdModelo AND CodigoComponente = @CodigoComponente)
        INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente)
        VALUES (@IdModelo, @CodigoComponente);
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_QuitarComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

-- Patente de acceso al ABM de modelos estándar (coincide con PatenteEnum06AV).
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarModelosEstandar')
    INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarModelosEstandar', 'Gestionar modelos estándar');
GO

DECLARE @IdRolAdmin NVARCHAR(450);
SELECT @IdRolAdmin = Id FROM Roles WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';
IF @IdRolAdmin IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarModelosEstandar')
    INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarModelosEstandar');
GO

PRINT 'Catálogo de modelos estándar (RFN1) creado/actualizado.';
GO
GO


-- ============================================================
-- ==== 30_seed_demo_pcfactory.sql
-- ==== Datos DEMO de PC Factory (RFN1 y RFN2 de punta a punta)
-- ============================================================
-- ============================================================
--  30_seed_demo_pcfactory.sql
--  Carga de DATOS DE DEMOSTRACIÓN para todo lo nuevo de PC Factory:
--  Componentes, Insumos (con faltantes para RFN2), Proveedores,
--  Líneas de ensamblaje, Clientes y Modelos estándar (con sus
--  componentes). Con esto se puede recorrer RFN1 y RFN2 de punta a punta.
--
--  Requisitos: correr antes 99_pcfactory_all.sql (crea las tablas).
--  Es idempotente: usa "WHERE NOT EXISTS", se puede correr varias veces.
--
--  Referencia de Tipo de componente (enum BE.TipoComponente06AV):
--    0=Procesador 1=MemoriaRAM 2=Disco 3=PlacaMadre 4=Fuente
--    5=Gabinete 6=PlacaDeVideo 7=Refrigeracion 8=Otro
-- ============================================================

SET NOCOUNT ON;
PRINT '>>> Cargando datos de demostración de PC Factory...';

-- ── 1) COMPONENTES ───────────────────────────────────────────
INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible)
SELECT v.Codigo, v.Descripcion, v.Tipo, v.Marca, v.Modelo, v.PrecioUnitario, v.StockDisponible
FROM (VALUES
    (N'CPU-001', N'Procesador 6 núcleos',      0, N'Intel',   N'Core i5-12400',   180.00, 25),
    (N'CPU-002', N'Procesador 6 núcleos',      0, N'AMD',     N'Ryzen 5 5600',    160.00, 20),
    (N'RAM-001', N'Memoria RAM 16GB DDR4',     1, N'Kingston',N'Fury 3200',        55.00, 40),
    (N'RAM-002', N'Memoria RAM 32GB DDR4',     1, N'Corsair', N'Vengeance 3600',  105.00, 15),
    (N'SSD-001', N'Disco SSD 1TB NVMe',        2, N'Samsung', N'980 Pro',          80.00, 30),
    (N'HDD-001', N'Disco HDD 2TB',             2, N'Seagate', N'Barracuda',        60.00, 18),
    (N'MB-001',  N'Placa madre B660',          3, N'ASUS',    N'Prime B660',      130.00, 20),
    (N'MB-002',  N'Placa madre B550',          3, N'Gigabyte',N'B550 Aorus',      120.00, 16),
    (N'PSU-001', N'Fuente 650W 80+ Bronze',    4, N'EVGA',    N'650 BR',           70.00, 22),
    (N'GAB-001', N'Gabinete ATX con vidrio',   5, N'NZXT',    N'H510',             65.00, 25),
    (N'GPU-001', N'Placa de video RTX 3060',   6, N'MSI',     N'Ventus 3060',     330.00, 10),
    (N'COOL-001',N'Cooler para CPU',           7, N'CoolerMaster', N'Hyper 212',   35.00, 30)
) v(Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, StockDisponible)
WHERE NOT EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = v.Codigo);
PRINT '   Componentes cargados.';

-- ── 2) INSUMOS (algunos por debajo del mínimo → faltantes RFN2) ─
INSERT INTO Insumos (Codigo, Descripcion, Stock, StockMinimo)
SELECT v.Codigo, v.Descripcion, v.Stock, v.StockMinimo
FROM (VALUES
    (N'INS-001', N'Pasta térmica',              5,  10),   -- FALTANTE
    (N'INS-002', N'Cables SATA',               50,  20),
    (N'INS-003', N'Tornillos (bolsa x100)',     8,  15),   -- FALTANTE
    (N'INS-004', N'Bridas plásticas',         100,  30),
    (N'INS-005', N'Alcohol isopropílico',       3,  12),   -- FALTANTE
    (N'INS-006', N'Guantes antiestáticos',     40,  10)
) v(Codigo, Descripcion, Stock, StockMinimo)
WHERE NOT EXISTS (SELECT 1 FROM Insumos i WHERE i.Codigo = v.Codigo);
PRINT '   Insumos cargados (3 quedan bajo stock para probar Compras).';

-- ── 3) PROVEEDORES ───────────────────────────────────────────
INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion)
SELECT v.Nombre, v.Cuit, v.Email, v.Telefono, v.Direccion
FROM (VALUES
    (N'Insumos del Sur SRL', N'30-11111111-1', N'ventas@insumosdelsur.com', N'011-4001-1000', N'Av. Mitre 1234, Avellaneda'),
    (N'TecnoParts SA',       N'30-22222222-2', N'compras@tecnoparts.com',   N'011-4002-2000', N'Calle Falsa 456, CABA'),
    (N'Distribuidora Norte', N'30-33333333-3', N'info@distnorte.com',       N'0351-500-3000', N'Bv. San Juan 789, Córdoba'),
    (N'ComponentesYA',       N'30-44444444-4', N'hola@componentesya.com',   N'0341-600-4000', N'Pellegrini 321, Rosario')
) v(Nombre, Cuit, Email, Telefono, Direccion)
WHERE NOT EXISTS (SELECT 1 FROM Proveedores p WHERE p.Cuit = v.Cuit);
PRINT '   Proveedores cargados.';

-- ── 4) LÍNEAS DE ENSAMBLAJE ──────────────────────────────────
INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible)
SELECT v.Nombre, v.Descripcion, v.Disponible
FROM (VALUES
    (N'Línea A', N'Armado general',      CONVERT(BIT,1)),
    (N'Línea B', N'Equipos gamer',       CONVERT(BIT,1)),
    (N'Línea C', N'Equipos de oficina',  CONVERT(BIT,1))
) v(Nombre, Descripcion, Disponible)
WHERE NOT EXISTS (SELECT 1 FROM LineasEnsamblaje l WHERE l.Nombre = v.Nombre);
PRINT '   Líneas de ensamblaje cargadas.';

-- ── 5) CLIENTES ──────────────────────────────────────────────
INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion)
SELECT v.Dni, v.Nombre, v.Apellido, v.Telefono, v.Direccion
FROM (VALUES
    (N'30111222', N'Juan',   N'Pérez',     N'11-5001-0001', N'San Martín 100, CABA'),
    (N'28999888', N'María',  N'Gómez',     N'11-5001-0002', N'Belgrano 200, CABA'),
    (N'33444555', N'Carlos', N'López',     N'351-500-0003', N'Colón 300, Córdoba'),
    (N'27888999', N'Ana',    N'Torres',    N'341-600-0004', N'Córdoba 400, Rosario'),
    (N'31222333', N'Lucía',  N'Fernández', N'11-5001-0005', N'Rivadavia 500, CABA')
) v(Dni, Nombre, Apellido, Telefono, Direccion)
WHERE NOT EXISTS (SELECT 1 FROM Clientes c WHERE c.Dni = v.Dni);
PRINT '   Clientes cargados.';

-- ── 6) MODELOS ESTÁNDAR ──────────────────────────────────────
INSERT INTO ModelosEstandar (Nombre, Descripcion)
SELECT v.Nombre, v.Descripcion
FROM (VALUES
    (N'PC Oficina', N'Equipo para tareas de oficina y navegación'),
    (N'PC Gamer',   N'Equipo de alto rendimiento con placa de video dedicada'),
    (N'PC Básica',  N'Equipo económico para uso general')
) v(Nombre, Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM ModelosEstandar m WHERE m.Nombre = v.Nombre);
PRINT '   Modelos estándar cargados.';

-- Componentes de cada modelo (resuelve el Id del modelo por Nombre).
INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente)
SELECT m.Id, v.Codigo
FROM (VALUES
    -- PC Oficina
    (N'PC Oficina', N'CPU-001'), (N'PC Oficina', N'RAM-001'), (N'PC Oficina', N'SSD-001'),
    (N'PC Oficina', N'MB-002'),  (N'PC Oficina', N'PSU-001'), (N'PC Oficina', N'GAB-001'),
    -- PC Gamer
    (N'PC Gamer', N'CPU-002'), (N'PC Gamer', N'RAM-002'), (N'PC Gamer', N'SSD-001'),
    (N'PC Gamer', N'MB-001'),  (N'PC Gamer', N'PSU-001'), (N'PC Gamer', N'GAB-001'),
    (N'PC Gamer', N'GPU-001'), (N'PC Gamer', N'COOL-001'),
    -- PC Básica
    (N'PC Básica', N'CPU-001'), (N'PC Básica', N'RAM-001'), (N'PC Básica', N'HDD-001'),
    (N'PC Básica', N'MB-002'),  (N'PC Básica', N'PSU-001'), (N'PC Básica', N'GAB-001')
) v(Modelo, Codigo)
JOIN ModelosEstandar m ON m.Nombre = v.Modelo
WHERE EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = v.Codigo)
  AND NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes mc
                  WHERE mc.IdModelo = m.Id AND mc.CodigoComponente = v.Codigo);
PRINT '   Componentes de modelos estándar asociados.';

-- ── Recalcular el Dígito Verificador tras la carga masiva ────
-- (opcional: la app lo recalcula sola; esto lo deja consistente ya mismo)
IF OBJECT_ID('DV') IS NOT NULL
    PRINT '   (El Dígito Verificador se recalculará al operar desde la app.)';

PRINT '>>> Datos de demostración cargados correctamente.';
GO
GO


PRINT '=============================================================';
PRINT ' Instalacion COMPLETA de IngSoftValdezAlegre finalizada.';
PRINT ' Login: admin   Password: Admin1234 (cambio obligatorio).';
PRINT '=============================================================';
GO
