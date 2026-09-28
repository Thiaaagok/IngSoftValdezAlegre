-- 36_reporte_ventas_produccion.sql
-- Reporte RF1 (ventas y producción): índice por fecha de venta,
-- sp_Reporte_VentasProduccion y la patente VerReporteVentas para el Administrador.
--
-- Requiere los scripts 20, 24, 25, 28 y 35 (usa Computadoras.IdModeloOrigen).
-- Idempotente.
--
-- Agrega filas en Patentes y RolPatentes, que tienen dígito verificador: después
-- de correrlo hay que recalcularlo, y cerrar sesión para tomar la patente nueva.

-- El reporte filtra por período; sin este índice recorre toda la tabla Ventas.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Ventas_FechaVenta' AND object_id = OBJECT_ID('Ventas'))
    CREATE NONCLUSTERED INDEX IX_Ventas_FechaVenta
        ON Ventas (FechaVenta)
        INCLUDE (DniCliente, IdComputadora, FechaEntregaEstimada, Estado, UsuarioRegistro);
GO

-- Una fila por venta. @Desde y @Hasta son inclusivos; los demás filtros en NULL no filtran.
-- Total es Computadoras.PrecioTotal, el precio congelado al vender.
-- FechaEntrega es la del cobro del saldo final: la entrega no guarda fecha propia.
CREATE OR ALTER PROCEDURE sp_Reporte_VentasProduccion
    @Desde             DATE,
    @Hasta             DATE,
    @Estado            INT           = NULL,
    @TipoConfiguracion INT           = NULL,
    @Cliente           NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- FechaVenta tiene hora: "< día siguiente" incluye todo el último día y deja usar el índice.
    DECLARE @HastaExclusivo DATETIME = DATEADD(DAY, 1, CAST(@Hasta AS DATETIME));
    -- Se escapan %, _ y [ para buscar el texto literal. El [ va primero para no
    -- volver a escapar los corchetes que agregan los otros dos reemplazos.
    DECLARE @Texto NVARCHAR(300) = LTRIM(RTRIM(ISNULL(@Cliente, N'')));
    SET @Texto = REPLACE(REPLACE(REPLACE(@Texto, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]');
    DECLARE @Patron NVARCHAR(310) = CASE WHEN @Texto = N'' THEN NULL ELSE N'%' + @Texto + N'%' END;

    SELECT  v.NumeroVenta,
            v.FechaVenta,
            v.Estado                                   AS EstadoVenta,
            v.UsuarioRegistro,
            c.Dni,
            c.Apellido + N', ' + c.Nombre              AS Cliente,
            pc.Nombre                                  AS Equipo,
            pc.TipoConfiguracion,
            m.Nombre                                   AS ModeloOrigen,
            pc.PrecioTotal                             AS Total,
            ISNULL(pg.Sena, 0)                         AS Sena,
            ISNULL(pg.SaldoCobrado, 0)                 AS SaldoCobrado,
            pg.FechaEntrega,
            op.NumeroOrden,
            op.Estado                                  AS EstadoOrden,
            l.Nombre                                   AS Linea,
            op.NumeroSerie,
            op.FechaCierre,
            COALESCE(op.FechaEntregaEstimada, v.FechaEntregaEstimada) AS FechaEntregaComprometida
    FROM    Ventas v
            INNER JOIN Clientes     c  ON c.Dni = v.DniCliente
            INNER JOIN Computadoras pc ON pc.Id = v.IdComputadora
            LEFT  JOIN ModelosEstandar   m  ON m.Id = pc.IdModeloOrigen
            LEFT  JOIN OrdenesProduccion op ON op.NumeroVenta = v.NumeroVenta
            LEFT  JOIN LineasEnsamblaje  l  ON l.Id = op.IdLinea
            OUTER APPLY (
                SELECT  SUM(CASE WHEN p.Tipo = 0 THEN p.Monto ELSE 0 END) AS Sena,
                        SUM(CASE WHEN p.Tipo = 1 THEN p.Monto ELSE 0 END) AS SaldoCobrado,
                        MAX(CASE WHEN p.Tipo = 1 THEN p.Fecha END)       AS FechaEntrega
                FROM    Pagos p
                WHERE   p.NumeroVenta = v.NumeroVenta
            ) pg
    WHERE   v.FechaVenta >= @Desde
      AND   v.FechaVenta <  @HastaExclusivo
      AND  (@Estado IS NULL            OR v.Estado = @Estado)
      AND  (@TipoConfiguracion IS NULL OR pc.TipoConfiguracion = @TipoConfiguracion)
      AND  (@Patron IS NULL
            OR c.Dni      LIKE @Patron
            OR c.Apellido LIKE @Patron
            OR c.Nombre   LIKE @Patron)
    ORDER BY v.FechaVenta DESC, v.NumeroVenta DESC;
END
GO

-- GestionarCotizaciones está en PatenteEnum06AV pero ningún script la sembraba:
-- sin ella el módulo de cotizaciones no aparece en el menú.
BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Patentes TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
    INSERT INTO @Patentes (Id, Descripcion) VALUES
        ('VerReporteVentas',      'Ver el reporte de ventas y producción'),
        ('GestionarCotizaciones', 'Gestionar la mesa de cotizaciones');

    INSERT INTO Patentes (Id, Descripcion)
    SELECT p.Id, p.Descripcion
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);

    DECLARE @IdRolAdmin NVARCHAR(450);
    SELECT @IdRolAdmin = Id FROM Roles WHERE Descripcion = 'Administrador' OR Codigo = 'ADM';

    IF @IdRolAdmin IS NULL
        RAISERROR('No se encontró el rol Administrador (por Descripcion o Codigo=ADM).', 16, 1);

    INSERT INTO RolPatentes (IdRol, IdPatente)
    SELECT @IdRolAdmin, p.Id
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM RolPatentes rp
                       WHERE rp.IdRol = @IdRolAdmin AND rp.IdPatente = p.Id);

    COMMIT TRANSACTION;
    PRINT '   Patentes VerReporteVentas y GestionarCotizaciones asignadas al Administrador.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'Error sembrando las patentes del reporte: ' + ERROR_MESSAGE();
END CATCH
GO

-- Verificación: el reporte del último año.
DECLARE @d DATE = DATEADD(YEAR, -1, CAST(GETDATE() AS DATE)),
        @h DATE = CAST(GETDATE() AS DATE);
EXEC sp_Reporte_VentasProduccion @Desde = @d, @Hasta = @h;
GO

PRINT '>>> 36: Reporte RF1 (ventas y producción) listo.';
PRINT '    Recordá: cerrar sesión, volver a entrar y recalcular el dígito verificador.';
GO
