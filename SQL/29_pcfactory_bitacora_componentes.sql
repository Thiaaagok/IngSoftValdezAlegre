-- ============================================================
--  29_pcfactory_bitacora_componentes.sql
--  BITÁCORA DE CAMBIOS de Componentes (histórico por TRIGGERS).
--
--  Toda la auditoría vive en la base: la aplicación solo hace INSERT/UPDATE
--  sobre Componentes a través de los SP de 21_pcfactory_componentes.sql y
--  NUNCA escribe en Componentes_C.
--
--  · Componentes_C guarda una fila por cada versión del componente.
--    Act = 1 marca la versión VIGENTE; hay a lo sumo una por código
--    (índice único filtrado UX_ComponentesC_UnicoActivo).
--  · TR_Componentes_Insert         → alta: primera versión, Act = 1.
--  · TR_Componentes_Update         → modificación o baja lógica: apaga la
--    versión anterior (Act = 0) y agrega la nueva (Act = 1). El orden
--    UPDATE → INSERT es obligatorio: al revés el índice único se rompe.
--  · TR_Componentes_BloquearDelete → prohíbe el borrado físico.
--
--  RUIDO DE STOCK: reservar / liberar reserva NO genera versión nueva
--  (solo tocan StockReservado, que no se audita). Sí la generan los cambios
--  de Stock, precio, descripción, tipo, marca, modelo, mínimo y baja lógica.
--
--  Requiere que 21_pcfactory_componentes.sql se haya ejecutado antes.
-- ============================================================

SET NOCOUNT ON;
GO

-- ── Tabla histórica ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Componentes_C')
CREATE TABLE Componentes_C (
    IdHistorico      INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Componentes_C PRIMARY KEY,
    CodigoComponente NVARCHAR(50)   NOT NULL,
    Fecha            DATE           NOT NULL,
    Hora             TIME(0)        NOT NULL,
    Descripcion      NVARCHAR(200)  NOT NULL,
    Tipo             INT            NOT NULL,
    Marca            NVARCHAR(100)  NULL,
    Modelo           NVARCHAR(100)  NULL,
    PrecioUnitario   DECIMAL(12,2)  NOT NULL,
    Stock            INT            NOT NULL,
    StockMinimo      INT            NOT NULL,
    Bit_Lo_Bo        BIT            NOT NULL CONSTRAINT DF_CompC_BitLoBo DEFAULT (0),
    Act              BIT            NOT NULL CONSTRAINT DF_CompC_Act     DEFAULT (0),
    CONSTRAINT FK_Componentes_C_Componentes
        FOREIGN KEY (CodigoComponente) REFERENCES Componentes(Codigo)
);
GO

-- Un único registro vigente por componente.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ComponentesC_UnicoActivo')
    CREATE UNIQUE INDEX UX_ComponentesC_UnicoActivo
        ON Componentes_C (CodigoComponente)
        WHERE Act = 1;
GO

-- Búsquedas de la pantalla de bitácora (filtro por código y rango de fechas).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ComponentesC_CodigoFecha')
    CREATE INDEX IX_ComponentesC_CodigoFecha
        ON Componentes_C (CodigoComponente, Fecha, Hora);
GO

-- ── Trigger de ALTA ──────────────────────────────────────────
CREATE OR ALTER TRIGGER TR_Componentes_Insert
ON Componentes
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Componentes_C (CodigoComponente, Fecha, Hora, Descripcion, Tipo,
                               Marca, Modelo, PrecioUnitario, Stock, StockMinimo,
                               Bit_Lo_Bo, Act)
    SELECT i.Codigo,
           CAST(GETDATE() AS DATE),
           CAST(GETDATE() AS TIME(0)),
           i.Descripcion, i.Tipo, i.Marca, i.Modelo, i.PrecioUnitario,
           i.Stock, i.StockMinimo, i.Bit_Lo_Bo, 1
    FROM   INSERTED i;
END
GO

-- ── Trigger de MODIFICACIÓN / BAJA LÓGICA ────────────────────
CREATE OR ALTER TRIGGER TR_Componentes_Update
ON Componentes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Evita versionar dos veces si el UPDATE viene disparado por otro trigger.
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- Movimientos que solo tocan StockReservado (reserva de venta, liberación)
    -- no son un cambio del componente: no generan versión.
    IF  NOT UPDATE(Descripcion)    AND NOT UPDATE(Tipo)  AND NOT UPDATE(Marca)
    AND NOT UPDATE(Modelo)         AND NOT UPDATE(Stock) AND NOT UPDATE(StockMinimo)
    AND NOT UPDATE(PrecioUnitario) AND NOT UPDATE(Bit_Lo_Bo)
        RETURN;

    -- Solo las filas cuyo contenido auditado cambió realmente.
    DECLARE @Cambiados TABLE (Codigo NVARCHAR(50) NOT NULL PRIMARY KEY);

    INSERT INTO @Cambiados (Codigo)
    SELECT i.Codigo
    FROM   INSERTED i
    INNER JOIN DELETED d ON d.Codigo = i.Codigo
    WHERE  ISNULL(i.Descripcion, N'') <> ISNULL(d.Descripcion, N'')
        OR i.Tipo                     <> d.Tipo
        OR ISNULL(i.Marca,  N'')      <> ISNULL(d.Marca,  N'')
        OR ISNULL(i.Modelo, N'')      <> ISNULL(d.Modelo, N'')
        OR i.PrecioUnitario           <> d.PrecioUnitario
        OR i.Stock                    <> d.Stock
        OR i.StockMinimo              <> d.StockMinimo
        OR i.Bit_Lo_Bo                <> d.Bit_Lo_Bo;

    IF NOT EXISTS (SELECT 1 FROM @Cambiados) RETURN;

    -- 1) Se apaga la versión vigente...
    UPDATE C
    SET    C.Act = 0
    FROM   Componentes_C C
    INNER JOIN @Cambiados X ON X.Codigo = C.CodigoComponente
    WHERE  C.Act = 1;

    -- 2) ...y recién ahí se inserta la nueva. Invertir el orden viola
    --     el índice único de "un solo vigente por componente".
    INSERT INTO Componentes_C (CodigoComponente, Fecha, Hora, Descripcion, Tipo,
                               Marca, Modelo, PrecioUnitario, Stock, StockMinimo,
                               Bit_Lo_Bo, Act)
    SELECT i.Codigo,
           CAST(GETDATE() AS DATE),
           CAST(GETDATE() AS TIME(0)),
           i.Descripcion, i.Tipo, i.Marca, i.Modelo, i.PrecioUnitario,
           i.Stock, i.StockMinimo, i.Bit_Lo_Bo, 1
    FROM   INSERTED i
    INNER JOIN @Cambiados X ON X.Codigo = i.Codigo;
END
GO

-- ── Trigger que BLOQUEA EL BORRADO FÍSICO ────────────────────
CREATE OR ALTER TRIGGER TR_Componentes_BloquearDelete
ON Componentes
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    THROW 51010, 'No se permite el borrado físico de componentes. Usá la baja lógica (Bit_Lo_Bo = 1).', 1;
END
GO

-- ── Bitácora: consulta con filtros ───────────────────────────
CREATE OR ALTER PROCEDURE sp_ComponentesC_Listar
    @Codigo      NVARCHAR(50)  = NULL,
    @Descripcion NVARCHAR(200) = NULL,
    @FechaIni    DATE          = NULL,
    @FechaFin    DATE          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT C.IdHistorico, C.CodigoComponente, C.Fecha, C.Hora, C.Descripcion,
           C.Tipo, C.Marca, C.Modelo, C.PrecioUnitario, C.Stock, C.StockMinimo,
           C.Bit_Lo_Bo, C.Act
    FROM   Componentes_C C
    WHERE  (@Codigo      IS NULL OR C.CodigoComponente LIKE '%' + @Codigo + '%')
      AND  (@Descripcion IS NULL OR C.Descripcion      LIKE '%' + @Descripcion + '%')
      AND  (@FechaIni    IS NULL OR C.Fecha >= @FechaIni)
      AND  (@FechaFin    IS NULL OR C.Fecha <= @FechaFin)
    ORDER BY C.CodigoComponente, C.Fecha DESC, C.Hora DESC, C.IdHistorico DESC;
END
GO

-- ── Bitácora: restaurar una versión histórica ────────────────
--  Escribe sobre Componentes (nunca sobre Componentes_C): es el trigger de
--  UPDATE el que apaga la versión vigente y asienta la restauración como una
--  versión nueva. Así la bitácora nunca pierde el historial.
CREATE OR ALTER PROCEDURE sp_ComponentesC_Activar
    @IdHistorico INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Codigo      NVARCHAR(50),  @Descripcion NVARCHAR(200),
            @Tipo        INT,           @Marca       NVARCHAR(100),
            @Modelo      NVARCHAR(100), @Precio      DECIMAL(12,2),
            @Stock       INT,           @StockMinimo INT,
            @BitLoBo     BIT,           @Act         BIT;

    SELECT @Codigo = CodigoComponente, @Descripcion = Descripcion, @Tipo = Tipo,
           @Marca = Marca, @Modelo = Modelo, @Precio = PrecioUnitario,
           @Stock = Stock, @StockMinimo = StockMinimo, @BitLoBo = Bit_Lo_Bo,
           @Act = Act
    FROM   Componentes_C
    WHERE  IdHistorico = @IdHistorico;

    IF @Codigo IS NULL
        THROW 51011, 'No existe el registro histórico indicado.', 1;

    IF @Act = 1
        THROW 51012, 'Esa versión ya es la vigente del componente.', 1;

    IF NOT EXISTS (SELECT 1 FROM Componentes WHERE Codigo = @Codigo)
        THROW 51013, 'El componente de esa versión histórica ya no existe.', 1;

    UPDATE Componentes
    SET    Descripcion    = @Descripcion,
           Tipo           = @Tipo,
           Marca          = @Marca,
           Modelo         = @Modelo,
           PrecioUnitario = @Precio,
           Stock          = @Stock,
           StockMinimo    = @StockMinimo,
           Bit_Lo_Bo      = @BitLoBo
    WHERE  Codigo = @Codigo;
END
GO

-- ── Línea base: una versión vigente por componente ya existente ──
--  Para bases que ya tenían componentes cargados antes de esta bitácora.
INSERT INTO Componentes_C (CodigoComponente, Fecha, Hora, Descripcion, Tipo,
                           Marca, Modelo, PrecioUnitario, Stock, StockMinimo,
                           Bit_Lo_Bo, Act)
SELECT c.Codigo,
       CAST(GETDATE() AS DATE),
       CAST(GETDATE() AS TIME(0)),
       c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.PrecioUnitario,
       c.Stock, c.StockMinimo, c.Bit_Lo_Bo, 1
FROM   Componentes c
WHERE  NOT EXISTS (SELECT 1 FROM Componentes_C h
                   WHERE h.CodigoComponente = c.Codigo AND h.Act = 1);
GO

PRINT 'Bitácora de cambios de Componentes (Componentes_C + triggers) creada/actualizada.';
GO
