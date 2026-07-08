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
