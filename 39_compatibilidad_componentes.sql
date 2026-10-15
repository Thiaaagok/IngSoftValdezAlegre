-- ============================================================
--  39_compatibilidad_componentes.sql
--  ESPECIFICACIONES TÉCNICAS Y COMPATIBILIDAD DE COMPONENTES (RFN1).
--
--  El armado de la PC ("Armá tu PC" y modelos estándar) valida que las
--  piezas funcionen juntas:
--    · Plataforma y socket: procesador ↔ placa madre ↔ refrigeración.
--    · Memoria: tipo DDR4/DDR5, velocidad, ranuras y capacidad de la placa.
--    · Energía: la fuente cubre el consumo estimado con margen.
--    · Gabinete: admite el formato de la placa (ATX, Micro-ATX, Mini-ITX).
--    · Temperatura y overclock: el cooler soporta el TDP del procesador;
--      procesador desbloqueado en placa que no permite overclock.
--
--  Este script:
--    1. Crea ComponenteEspecificaciones (una fila por componente, opcional).
--    2. Agrega ModeloEstandarComponentes.Cantidad: un modelo estándar puede
--       llevar varios módulos de la misma memoria (p. ej. 2 × 16 GB).
--    3. Crea/redefine los procedimientos almacenados.
--    4. Carga las especificaciones de los componentes de demostración.
--    5. Corrige los modelos de demostración que mezclaban Intel y AMD.
--    6. Crea ReglasCompatibilidad: las reglas del sistema (se activan o
--       desactivan, se elige si bloquean o advierten y se ajustan sus
--       valores) y las reglas propias que el administrador agrega desde la
--       pantalla "Reglas de compatibilidad" (patente GestionarCompatibilidad).
--    7. Vacía la tabla DV: en el próximo ingreso el sistema recalcula la
--       línea base del dígito verificador (FRMLogin → SinLineaBase).
--
--  Es idempotente: se puede correr varias veces.
--  Los componentes sin fila en ComponenteEspecificaciones siguen funcionando:
--  la aplicación estima sus datos a partir de la marca, el modelo y la
--  descripción, y se pueden corregir desde el ABM de componentes.
-- ============================================================

SET NOCOUNT ON;
GO

-- ── 1) Tabla de especificaciones ─────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ComponenteEspecificaciones')
CREATE TABLE ComponenteEspecificaciones (
    CodigoComponente      NVARCHAR(50)  NOT NULL
        CONSTRAINT PK_ComponenteEspecificaciones PRIMARY KEY,
    Plataforma            NVARCHAR(10)  NULL,   -- AMD | Intel (procesador y placa madre)
    Socket                NVARCHAR(100) NULL,   -- AM4, AM5, LGA1700… (refrigeración: lista separada por comas)
    TipoMemoria           NVARCHAR(10)  NULL,   -- DDR4 | DDR5 (placa madre y memoria)
    FrecuenciaMemoriaMhz  INT           NULL,   -- memoria: velocidad · placa: máxima sin overclock
    CapacidadGb           INT           NULL,   -- memoria: capacidad de cada módulo
    SlotsMemoria          INT           NULL,   -- placa madre: ranuras de memoria
    MemoriaMaximaGb       INT           NULL,   -- placa madre: memoria total admitida
    Formato               NVARCHAR(20)  NULL,   -- ATX | MicroATX | MiniITX (placa: el suyo · gabinete: el mayor que admite)
    ConsumoW              INT           NULL,   -- consumo / TDP
    PotenciaW             INT           NULL,   -- fuente: potencia nominal
    TdpSoportadoW         INT           NULL,   -- refrigeración: TDP máximo que disipa
    GraficosIntegrados    BIT           NULL,   -- procesador: tiene salida de video propia
    Desbloqueado          BIT           NULL,   -- procesador: admite overclock (Intel K, AMD Ryzen)
    PermiteOverclock      BIT           NULL,   -- placa madre: chipset con overclock de CPU (Intel Z, AMD B/X)
    CONSTRAINT FK_ComponenteEspecificaciones_Componentes
        FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- ── 2) Cantidad en los componentes de un modelo estándar ─────
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('ModeloEstandarComponentes') AND name = 'Cantidad')
    ALTER TABLE ModeloEstandarComponentes
        ADD Cantidad INT NOT NULL CONSTRAINT DF_ModeloComp_Cantidad DEFAULT (1);
GO

-- ── 3) Procedimientos ────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_Especificaciones_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoComponente, Plataforma, Socket, TipoMemoria, FrecuenciaMemoriaMhz, CapacidadGb,
           SlotsMemoria, MemoriaMaximaGb, Formato, ConsumoW, PotenciaW, TdpSoportadoW,
           GraficosIntegrados, Desbloqueado, PermiteOverclock
    FROM   ComponenteEspecificaciones;
END
GO

CREATE OR ALTER PROCEDURE sp_Especificaciones_ObtenerPorCodigo
    @Codigo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoComponente, Plataforma, Socket, TipoMemoria, FrecuenciaMemoriaMhz, CapacidadGb,
           SlotsMemoria, MemoriaMaximaGb, Formato, ConsumoW, PotenciaW, TdpSoportadoW,
           GraficosIntegrados, Desbloqueado, PermiteOverclock
    FROM   ComponenteEspecificaciones
    WHERE  CodigoComponente = @Codigo;
END
GO

CREATE OR ALTER PROCEDURE sp_Especificaciones_Guardar
    @Codigo               NVARCHAR(50),
    @Plataforma           NVARCHAR(10)  = NULL,
    @Socket               NVARCHAR(100) = NULL,
    @TipoMemoria          NVARCHAR(10)  = NULL,
    @FrecuenciaMemoriaMhz INT           = NULL,
    @CapacidadGb          INT           = NULL,
    @SlotsMemoria         INT           = NULL,
    @MemoriaMaximaGb      INT           = NULL,
    @Formato              NVARCHAR(20)  = NULL,
    @ConsumoW             INT           = NULL,
    @PotenciaW            INT           = NULL,
    @TdpSoportadoW        INT           = NULL,
    @GraficosIntegrados   BIT           = NULL,
    @Desbloqueado         BIT           = NULL,
    @PermiteOverclock     BIT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM Componentes WHERE Codigo = @Codigo)
        THROW 51010, 'El componente no existe.', 1;

    IF EXISTS (SELECT 1 FROM ComponenteEspecificaciones WHERE CodigoComponente = @Codigo)
        UPDATE ComponenteEspecificaciones
        SET    Plataforma = @Plataforma, Socket = @Socket, TipoMemoria = @TipoMemoria,
               FrecuenciaMemoriaMhz = @FrecuenciaMemoriaMhz, CapacidadGb = @CapacidadGb,
               SlotsMemoria = @SlotsMemoria, MemoriaMaximaGb = @MemoriaMaximaGb, Formato = @Formato,
               ConsumoW = @ConsumoW, PotenciaW = @PotenciaW, TdpSoportadoW = @TdpSoportadoW,
               GraficosIntegrados = @GraficosIntegrados, Desbloqueado = @Desbloqueado,
               PermiteOverclock = @PermiteOverclock
        WHERE  CodigoComponente = @Codigo;
    ELSE
        INSERT INTO ComponenteEspecificaciones
               (CodigoComponente, Plataforma, Socket, TipoMemoria, FrecuenciaMemoriaMhz, CapacidadGb,
                SlotsMemoria, MemoriaMaximaGb, Formato, ConsumoW, PotenciaW, TdpSoportadoW,
                GraficosIntegrados, Desbloqueado, PermiteOverclock)
        VALUES (@Codigo, @Plataforma, @Socket, @TipoMemoria, @FrecuenciaMemoriaMhz, @CapacidadGb,
                @SlotsMemoria, @MemoriaMaximaGb, @Formato, @ConsumoW, @PotenciaW, @TdpSoportadoW,
                @GraficosIntegrados, @Desbloqueado, @PermiteOverclock);
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_ObtenerComponentes
    @IdModelo INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CodigoComponente, Cantidad FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
END
GO

CREATE OR ALTER PROCEDURE sp_ModeloEstandar_AgregarComponente
    @IdModelo INT, @CodigoComponente NVARCHAR(50), @Cantidad INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF @Cantidad IS NULL OR @Cantidad < 1 SET @Cantidad = 1;
    IF EXISTS (SELECT 1 FROM ModeloEstandarComponentes
               WHERE IdModelo = @IdModelo AND CodigoComponente = @CodigoComponente)
        UPDATE ModeloEstandarComponentes SET Cantidad = @Cantidad
        WHERE  IdModelo = @IdModelo AND CodigoComponente = @CodigoComponente;
    ELSE
        INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente, Cantidad)
        VALUES (@IdModelo, @CodigoComponente, @Cantidad);
END
GO

-- ── 4) Especificaciones de los componentes de demostración ───
--  Solo se cargan si el componente existe y todavía no tiene especificación.
DECLARE @E TABLE (
    Codigo NVARCHAR(50) PRIMARY KEY, Plataforma NVARCHAR(10), Socket NVARCHAR(100), TipoMemoria NVARCHAR(10),
    Frecuencia INT, CapacidadGb INT, Slots INT, MaxGb INT, Formato NVARCHAR(20), ConsumoW INT, PotenciaW INT,
    TdpSoportado INT, Graficos BIT, Desbloqueado BIT, PermiteOc BIT);

INSERT INTO @E VALUES
 -- Procesadores                  plataforma socket       mem   MHz   GB  slots maxGB formato  W    fuente cooler iGPU  OC   placaOC
 (N'CPU-001',  N'Intel', N'LGA1700', NULL,  NULL, NULL, NULL, NULL, NULL,       65, NULL, NULL, 1, 0, NULL),  -- Core i5-12400
 (N'CPU-002',  N'AMD',   N'AM4',     NULL,  NULL, NULL, NULL, NULL, NULL,       65, NULL, NULL, 0, 1, NULL),  -- Ryzen 5 5600 (sin video)
 (N'CPU-003',  N'Intel', N'LGA1700', NULL,  NULL, NULL, NULL, NULL, NULL,       65, NULL, NULL, 1, 0, NULL),  -- Core i7-13700
 (N'CPU-004',  N'AMD',   N'AM5',     NULL,  NULL, NULL, NULL, NULL, NULL,       65, NULL, NULL, 1, 1, NULL),  -- Ryzen 7 7700
 (N'CPU-005',  N'Intel', N'LGA1700', NULL,  NULL, NULL, NULL, NULL, NULL,       60, NULL, NULL, 1, 0, NULL),  -- Core i3-12100
 -- Memorias
 (N'RAM-001',  NULL, NULL, N'DDR4', 3200, 16, NULL, NULL, NULL,  5, NULL, NULL, NULL, NULL, NULL),
 (N'RAM-002',  NULL, NULL, N'DDR4', 3600, 32, NULL, NULL, NULL,  5, NULL, NULL, NULL, NULL, NULL),
 (N'RAM-003',  NULL, NULL, N'DDR5', 6000, 32, NULL, NULL, NULL,  6, NULL, NULL, NULL, NULL, NULL),
 (N'RAM-004',  NULL, NULL, N'DDR4', 3200,  8, NULL, NULL, NULL,  4, NULL, NULL, NULL, NULL, NULL),
 -- Discos
 (N'SSD-001',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,  7, NULL, NULL, NULL, NULL, NULL),
 (N'SSD-002',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,  5, NULL, NULL, NULL, NULL, NULL),
 (N'SSD-003',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,  8, NULL, NULL, NULL, NULL, NULL),
 (N'HDD-001',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,  8, NULL, NULL, NULL, NULL, NULL),
 (N'HDD-002',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,  7, NULL, NULL, NULL, NULL, NULL),
 -- Placas madre
 (N'MB-001',   N'Intel', N'LGA1700', N'DDR4', 3200, NULL, 4, 128, N'ATX',      50, NULL, NULL, NULL, NULL, 0),  -- Prime B660
 (N'MB-002',   N'AMD',   N'AM4',     N'DDR4', 3200, NULL, 4, 128, N'ATX',      50, NULL, NULL, NULL, NULL, 1),  -- B550 Aorus
 (N'MB-003',   N'Intel', N'LGA1700', N'DDR5', 5600, NULL, 4, 192, N'ATX',      60, NULL, NULL, NULL, NULL, 1),  -- PRO Z790-P
 (N'MB-004',   N'AMD',   N'AM5',     N'DDR5', 5200, NULL, 4, 192, N'MicroATX', 50, NULL, NULL, NULL, NULL, 1),  -- B650M Pro RS
 (N'MB-005',   N'Intel', N'LGA1700', N'DDR4', 3200, NULL, 2,  64, N'MicroATX', 40, NULL, NULL, NULL, NULL, 0),  -- H610M S2H
 -- Fuentes
 (N'PSU-001',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 650, NULL, NULL, NULL, NULL),
 (N'PSU-002',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 850, NULL, NULL, NULL, NULL),
 (N'PSU-003',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 500, NULL, NULL, NULL, NULL),
 -- Gabinetes
 (N'GAB-001',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'ATX',      NULL, NULL, NULL, NULL, NULL, NULL),
 (N'GAB-002',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'ATX',      NULL, NULL, NULL, NULL, NULL, NULL),
 (N'GAB-003',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'MicroATX', NULL, NULL, NULL, NULL, NULL, NULL),
 -- Placas de video
 (N'GPU-001',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 170, NULL, NULL, NULL, NULL, NULL),
 (N'GPU-002',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 115, NULL, NULL, NULL, NULL, NULL),
 (N'GPU-003',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 220, NULL, NULL, NULL, NULL, NULL),
 (N'GPU-004',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 165, NULL, NULL, NULL, NULL, NULL),
 (N'GPU-005',  NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,  75, NULL, NULL, NULL, NULL, NULL),
 -- Refrigeración
 (N'COOL-001', NULL, N'AM4,AM5,LGA1700', NULL, NULL, NULL, NULL, NULL, NULL, 4, NULL, 150, NULL, NULL, NULL),
 (N'COOL-002', NULL, N'AM4,AM5,LGA1700', NULL, NULL, NULL, NULL, NULL, NULL, 8, NULL, 250, NULL, NULL, NULL),
 (N'COOL-003', NULL, N'AM4,AM5,LGA1700', NULL, NULL, NULL, NULL, NULL, NULL, 3, NULL, 220, NULL, NULL, NULL);

INSERT INTO ComponenteEspecificaciones
       (CodigoComponente, Plataforma, Socket, TipoMemoria, FrecuenciaMemoriaMhz, CapacidadGb, SlotsMemoria,
        MemoriaMaximaGb, Formato, ConsumoW, PotenciaW, TdpSoportadoW, GraficosIntegrados, Desbloqueado, PermiteOverclock)
SELECT e.Codigo, e.Plataforma, e.Socket, e.TipoMemoria, e.Frecuencia, e.CapacidadGb, e.Slots,
       e.MaxGb, e.Formato, e.ConsumoW, e.PotenciaW, e.TdpSoportado, e.Graficos, e.Desbloqueado, e.PermiteOc
FROM   @E e
WHERE  EXISTS     (SELECT 1 FROM Componentes c WHERE c.Codigo = e.Codigo)
  AND  NOT EXISTS (SELECT 1 FROM ComponenteEspecificaciones x WHERE x.CodigoComponente = e.Codigo);

PRINT '   Especificaciones de demostración cargadas: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
GO

-- ── 5) Modelos de demostración que mezclaban plataformas ─────
--  El script 30 armó "PC Oficina" y "PC Básica" con Core i5 (Intel) sobre una
--  placa B550 (AMD), y "PC Gamer" con Ryzen 5 sobre una B660 (Intel). Con la
--  validación nueva esos modelos no se podrían vender: se intercambian las
--  placas. "PC Gamer" además usa un Ryzen 5 5600, que no tiene video propio,
--  así que se le agrega la RTX 3060 si no tiene placa de video.
UPDATE mc SET CodigoComponente = N'MB-001'
FROM   ModeloEstandarComponentes mc
WHERE  mc.CodigoComponente = N'MB-002'
  AND  EXISTS (SELECT 1 FROM ModeloEstandarComponentes x WHERE x.IdModelo = mc.IdModelo AND x.CodigoComponente = N'CPU-001')
  AND  NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes x WHERE x.IdModelo = mc.IdModelo AND x.CodigoComponente = N'MB-001')
  AND  EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = N'MB-001');

UPDATE mc SET CodigoComponente = N'MB-002'
FROM   ModeloEstandarComponentes mc
WHERE  mc.CodigoComponente = N'MB-001'
  AND  EXISTS (SELECT 1 FROM ModeloEstandarComponentes x WHERE x.IdModelo = mc.IdModelo AND x.CodigoComponente = N'CPU-002')
  AND  NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes x WHERE x.IdModelo = mc.IdModelo AND x.CodigoComponente = N'MB-002')
  AND  EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = N'MB-002');

INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente, Cantidad)
SELECT mc.IdModelo, N'GPU-001', 1
FROM   ModeloEstandarComponentes mc
WHERE  mc.CodigoComponente = N'CPU-002'
  AND  EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = N'GPU-001' AND c.Bit_Lo_Bo = 0)
  AND  NOT EXISTS (SELECT 1 FROM ModeloEstandarComponentes x
                   INNER JOIN Componentes c ON c.Codigo = x.CodigoComponente
                   WHERE x.IdModelo = mc.IdModelo AND c.Tipo = 6);
GO

-- ── 6) Reglas de compatibilidad configurables ────────────────
--  Sistema = 1 → regla programada: sólo se puede activar/desactivar, cambiar
--                el nivel (1 advierte, 2 bloquea) y ajustar Valor.
--  Sistema = 0 → regla propia: compara un dato de la pieza A con un dato de
--                la pieza B (Igual, Distinto, MenorOIgual, MayorOIgual,
--                Contiene). Si no se cumple, se informa con su Nivel.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReglasCompatibilidad')
CREATE TABLE ReglasCompatibilidad (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReglasCompatibilidad PRIMARY KEY,
    Codigo     NVARCHAR(50)  NOT NULL CONSTRAINT UX_ReglasCompatibilidad_Codigo UNIQUE,
    Nombre     NVARCHAR(150) NOT NULL,
    Sistema    BIT           NOT NULL CONSTRAINT DF_ReglasCompat_Sistema DEFAULT (0),
    Activa     BIT           NOT NULL CONSTRAINT DF_ReglasCompat_Activa  DEFAULT (1),
    Nivel      INT           NOT NULL CONSTRAINT DF_ReglasCompat_Nivel   DEFAULT (2),
    TipoA      INT           NULL,
    AtributoA  NVARCHAR(50)  NULL,
    Operador   NVARCHAR(20)  NULL,
    TipoB      INT           NULL,
    AtributoB  NVARCHAR(50)  NULL,
    Valor      DECIMAL(10,2) NULL,
    Mensaje    NVARCHAR(300) NULL,
    CONSTRAINT CK_ReglasCompat_Nivel CHECK (Nivel IN (1, 2))
);
GO

CREATE OR ALTER PROCEDURE sp_ReglasCompat_ObtenerTodas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Codigo, Nombre, Sistema, Activa, Nivel, TipoA, AtributoA, Operador, TipoB, AtributoB, Valor, Mensaje
    FROM   ReglasCompatibilidad
    ORDER  BY Sistema DESC, Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ReglasCompat_Guardar
    @Id        INT,
    @Codigo    NVARCHAR(50)  = NULL,
    @Nombre    NVARCHAR(150),
    @Activa    BIT,
    @Nivel     INT,
    @TipoA     INT           = NULL,
    @AtributoA NVARCHAR(50)  = NULL,
    @Operador  NVARCHAR(20)  = NULL,
    @TipoB     INT           = NULL,
    @AtributoB NVARCHAR(50)  = NULL,
    @Valor     DECIMAL(10,2) = NULL,
    @Mensaje   NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id > 0 AND EXISTS (SELECT 1 FROM ReglasCompatibilidad WHERE Id = @Id AND Sistema = 1)
    BEGIN
        UPDATE ReglasCompatibilidad
        SET    Activa = @Activa, Nivel = @Nivel, Valor = @Valor
        WHERE  Id = @Id;
        SELECT @Id AS Id;
        RETURN;
    END

    IF @Id > 0
    BEGIN
        UPDATE ReglasCompatibilidad
        SET    Nombre = @Nombre, Activa = @Activa, Nivel = @Nivel, TipoA = @TipoA, AtributoA = @AtributoA,
               Operador = @Operador, TipoB = @TipoB, AtributoB = @AtributoB, Valor = @Valor, Mensaje = @Mensaje
        WHERE  Id = @Id AND Sistema = 0;
        SELECT @Id AS Id;
        RETURN;
    END

    INSERT INTO ReglasCompatibilidad (Codigo, Nombre, Sistema, Activa, Nivel, TipoA, AtributoA, Operador, TipoB, AtributoB, Valor, Mensaje)
    VALUES (ISNULL(@Codigo, CONVERT(NVARCHAR(50), NEWID())), @Nombre, 0, @Activa, @Nivel, @TipoA, @AtributoA,
            @Operador, @TipoB, @AtributoB, @Valor, @Mensaje);
    DECLARE @Nuevo INT = CAST(SCOPE_IDENTITY() AS INT);
    IF @Codigo IS NULL
        UPDATE ReglasCompatibilidad SET Codigo = N'REG-' + RIGHT(N'0000' + CAST(@Nuevo AS NVARCHAR(10)), 4) WHERE Id = @Nuevo;
    SELECT @Nuevo AS Id;
END
GO

CREATE OR ALTER PROCEDURE sp_ReglasCompat_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ReglasCompatibilidad WHERE Id = @Id AND Sistema = 1)
        THROW 51020, 'Las reglas del sistema no se pueden eliminar: se pueden desactivar.', 1;
    DELETE FROM ReglasCompatibilidad WHERE Id = @Id;
END
GO

DECLARE @R TABLE (Codigo NVARCHAR(50), Nombre NVARCHAR(150), Nivel INT, Valor DECIMAL(10,2));
INSERT INTO @R VALUES
 (N'plataforma',          N'Procesador y placa madre de la misma marca',            2, NULL),
 (N'socket',              N'Procesador y placa madre con el mismo socket',          2, NULL),
 (N'oc_cpu',              N'Procesador desbloqueado en placa sin overclock',        1, NULL),
 (N'ddr',                 N'Memoria del tipo que usa la placa (DDR4/DDR5)',         2, NULL),
 (N'ram_tipos',           N'No mezclar tipos de memoria',                           2, NULL),
 (N'ram_mezcla',          N'Módulos de memoria iguales',                            1, NULL),
 (N'ram_oc',              N'Memoria más rápida que la placa (requiere overclock)',  1, NULL),
 (N'ram_slots',           N'Módulos de memoria dentro de las ranuras de la placa',  2, 4),
 (N'ram_max',             N'Memoria total dentro del máximo de la placa',           2, NULL),
 (N'formato',             N'Placa madre que entra en el gabinete',                  2, NULL),
 (N'cooler_socket',       N'Refrigeración compatible con el socket',                2, NULL),
 (N'cooler_tdp',          N'Refrigeración que soporta el TDP del procesador',       2, NULL),
 (N'fuente_insuficiente', N'Fuente que cubre el consumo',                           2, NULL),
 (N'fuente_limite',       N'Fuente con margen de seguridad',                        1, 30),
 (N'sin_video',           N'Salida de video (integrada o placa de video)',          2, NULL);

INSERT INTO ReglasCompatibilidad (Codigo, Nombre, Sistema, Activa, Nivel, Valor)
SELECT r.Codigo, r.Nombre, 1, 1, r.Nivel, r.Valor
FROM   @R r
WHERE  NOT EXISTS (SELECT 1 FROM ReglasCompatibilidad x WHERE x.Codigo = r.Codigo);
GO

--  Patente de la pantalla, asignada al rol Administrador.
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'GestionarCompatibilidad')
    INSERT INTO Patentes (Id, Descripcion) VALUES ('GestionarCompatibilidad', 'Gestionar reglas de compatibilidad');

DECLARE @IdRolAdmin NVARCHAR(450);
SELECT @IdRolAdmin = Id FROM Roles WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';
IF @IdRolAdmin IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM RolPatentes WHERE IdRol = @IdRolAdmin AND IdPatente = 'GestionarCompatibilidad')
    INSERT INTO RolPatentes (IdRol, IdPatente) VALUES (@IdRolAdmin, 'GestionarCompatibilidad');
GO

-- ── 7) Dígito verificador ────────────────────────────────────
--  ModeloEstandarComponentes cambió de estructura; ComponenteEspecificaciones,
--  ReglasCompatibilidad, Patentes y RolPatentes cambiaron o pasan a ser tablas
--  protegidas: se vacía DV y el sistema recalcula la línea
--  base en el próximo ingreso.
IF OBJECT_ID('DV') IS NOT NULL
    DELETE FROM DV;
GO

PRINT '39_compatibilidad_componentes.sql aplicado.';
GO
