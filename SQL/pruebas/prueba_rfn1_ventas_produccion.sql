-- prueba_rfn1_ventas_produccion.sql
-- Pruebas de integración del RFN1 y del reporte RF1 sobre la base (casos PI-xx y PR-xx
-- del documento de pruebas). Se corre a mano en SSMS con los scripts 00 a 36 aplicados;
-- está en SQL\pruebas\ porque el Instalador solo lee el primer nivel de SQL\.
--
-- Todo corre en una transacción que se deshace al final, así que no deja datos ni
-- altera el dígito verificador. Cada caso deja OK o FALLA en la tabla de resultados.
--
-- Los casos que esperan un error llaman al procedimiento con EXEC directo y vuelven a
-- un punto de guardado: si fallara un INSERT ... EXEC, la transacción quedaría
-- condenada y no se podría seguir.

USE IngSoftValdezAlegre;
GO
SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO

DECLARE @R TABLE (Orden INT IDENTITY(1,1), Caso VARCHAR(10), Descripcion NVARCHAR(200),
                  Resultado VARCHAR(5), Detalle NVARCHAR(400));

DECLARE @Hoy DATE = CAST(GETDATE() AS DATE);
DECLARE @n INT, @m INT, @txt NVARCHAR(400), @idPc INT, @idPc2 INT, @idPc3 INT,
        @venta INT, @venta2 INT, @venta3 INT, @orden INT, @idLinea INT, @idModelo INT;
DECLARE @Pago TABLE (Id INT, NumeroVenta INT, Tipo INT, NumeroRecibo NVARCHAR(30), Monto DECIMAL(12,2),
                     FormaPago INT, Referencia NVARCHAR(100), Fecha DATETIME, Usuario NVARCHAR(150));
DECLARE @Vta TABLE (NumeroVenta INT, DniCliente NVARCHAR(20), IdComputadora INT, FechaVenta DATETIME,
                    FechaEntregaEstimada DATE, Estado INT, UsuarioRegistro NVARCHAR(150), NumeroOrdenProduccion INT);
DECLARE @Rep TABLE (NumeroVenta INT, FechaVenta DATETIME, EstadoVenta INT, UsuarioRegistro NVARCHAR(150),
                    Dni NVARCHAR(20), Cliente NVARCHAR(210), Equipo NVARCHAR(150), TipoConfiguracion INT,
                    ModeloOrigen NVARCHAR(150), Total DECIMAL(12,2), Sena DECIMAL(12,2), SaldoCobrado DECIMAL(12,2),
                    FechaEntrega DATETIME, NumeroOrden INT, EstadoOrden INT, Linea NVARCHAR(100),
                    NumeroSerie NVARCHAR(50), FechaCierre DATETIME, FechaEntregaComprometida DATE);

BEGIN TRANSACTION;

INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion) VALUES
    (N'99000001', N'Marta',  N'Zeta',      N'11-0000-0001', N'Calle 1'),
    (N'99000002', N'Otro',   N'Omega_Test', N'11-0000-0002', N'Calle 2');

INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, Stock, StockMinimo) VALUES
    (N'ZZT-CPU', N'CPU de prueba',       0, N'T', N'C1', 100.00, 5, 1),
    (N'ZZT-RAM', N'RAM de prueba',       1, N'T', N'R1',  50.00, 4, 1),
    (N'ZZT-SSD', N'SSD de prueba',       2, N'T', N'S1',  40.00, 3, 1),
    (N'ZZT-MB',  N'Placa de prueba',     3, N'T', N'M1',  80.00, 2, 1),
    (N'ZZT-PSU', N'Fuente de prueba',    4, N'T', N'F1',  30.00, 2, 1),
    (N'ZZT-GAB', N'Gabinete de prueba',  5, N'T', N'G1',  20.00, 2, 1);

INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible) VALUES (N'ZZ Línea de prueba', N'Prueba', 1);
SET @idLinea = SCOPE_IDENTITY();

INSERT INTO ModelosEstandar (Nombre, Descripcion) VALUES (N'ZZ Modelo de prueba', N'Prueba');
SET @idModelo = SCOPE_IDENTITY();

-- Stock: la venta reserva (CU01), el cierre consume (CU06) y la anulación libera.
EXEC sp_Componentes_ReservarStock @Codigo = N'ZZT-CPU', @Cantidad = 2;
SELECT @n = StockReservado FROM Componentes WHERE Codigo = N'ZZT-CPU';
INSERT INTO @R SELECT 'PI-01', N'Reserva con stock libre suficiente (5 libres, reserva 2)',
    CASE WHEN @n = 2 THEN 'OK' ELSE 'FALLA' END, N'StockReservado = ' + CAST(@n AS NVARCHAR(10));

SAVE TRANSACTION sp_pi02;
BEGIN TRY
    EXEC sp_Componentes_ReservarStock @Codigo = N'ZZT-CPU', @Cantidad = 4;   -- libres: 3
    INSERT INTO @R VALUES ('PI-02', N'Reserva mayor al stock libre (3 libres, pide 4)', 'FALLA', N'No lanzó error');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi02;
    SELECT @n = StockReservado FROM Componentes WHERE Codigo = N'ZZT-CPU';
    INSERT INTO @R VALUES ('PI-02', N'Reserva mayor al stock libre (3 libres, pide 4)',
        CASE WHEN ERROR_NUMBER() = 51001 AND @n = 2 THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N'; reservado sigue en ' + CAST(@n AS NVARCHAR(10)));
END CATCH

EXEC sp_Componentes_ReservarStock @Codigo = N'ZZT-CPU', @Cantidad = 3;       -- libres exactos
SELECT @n = Stock - StockReservado FROM Componentes WHERE Codigo = N'ZZT-CPU';
INSERT INTO @R SELECT 'PI-03', N'Valor límite: reserva exactamente el stock libre (3 de 3)',
    CASE WHEN @n = 0 THEN 'OK' ELSE 'FALLA' END, N'Stock libre = ' + CAST(@n AS NVARCHAR(10));

EXEC sp_Componentes_LiberarReserva @Codigo = N'ZZT-CPU', @Cantidad = 99;
SELECT @n = StockReservado FROM Componentes WHERE Codigo = N'ZZT-CPU';
INSERT INTO @R SELECT 'PI-04', N'Liberar más de lo reservado no deja la reserva negativa',
    CASE WHEN @n = 0 THEN 'OK' ELSE 'FALLA' END, N'StockReservado = ' + CAST(@n AS NVARCHAR(10));

EXEC sp_Componentes_ReservarStock  @Codigo = N'ZZT-RAM', @Cantidad = 2;
EXEC sp_Componentes_ConsumirReserva @Codigo = N'ZZT-RAM', @Cantidad = 2;
SELECT @n = Stock, @m = StockReservado FROM Componentes WHERE Codigo = N'ZZT-RAM';
INSERT INTO @R SELECT 'PI-05', N'Consumir la reserva descuenta stock físico y reserva (4→2, 2→0)',
    CASE WHEN @n = 2 AND @m = 0 THEN 'OK' ELSE 'FALLA' END,
    N'Stock = ' + CAST(@n AS NVARCHAR(10)) + N', reservado = ' + CAST(@m AS NVARCHAR(10));

SAVE TRANSACTION sp_pi06;
BEGIN TRY
    EXEC sp_Componentes_ConsumirReserva @Codigo = N'ZZT-RAM', @Cantidad = 3;   -- hay 2
    INSERT INTO @R VALUES ('PI-06', N'Consumir más stock del que hay', 'FALLA', N'No lanzó error');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi06;
    INSERT INTO @R VALUES ('PI-06', N'Consumir más stock del que hay',
        CASE WHEN ERROR_NUMBER() = 51002 THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N': ' + ERROR_MESSAGE());
END CATCH

-- Computadora y venta (CU01).
CREATE TABLE #Id (Id INT);
INSERT INTO #Id EXEC sp_Computadoras_Agregar @Nombre = N'ZZ Modelo de prueba', @TipoConfiguracion = 0,
                                             @PrecioTotal = 320.00, @IdModeloOrigen = @idModelo;
SELECT @idPc = Id FROM #Id; DELETE FROM #Id;

EXEC sp_Computadoras_AgregarComponente @IdComputadora = @idPc, @CodigoComponente = N'ZZT-RAM', @Cantidad = 1;
EXEC sp_Computadoras_AgregarComponente @IdComputadora = @idPc, @CodigoComponente = N'ZZT-RAM', @Cantidad = 1;
SELECT @n = Cantidad FROM ComputadoraComponentes WHERE IdComputadora = @idPc AND CodigoComponente = N'ZZT-RAM';
INSERT INTO @R SELECT 'PI-07', N'Mismo componente dos veces acumula la cantidad (1+1)',
    CASE WHEN @n = 2 THEN 'OK' ELSE 'FALLA' END, N'Cantidad = ' + CAST(@n AS NVARCHAR(10));

INSERT INTO #Id EXEC sp_Ventas_Agregar @DniCliente = N'99000001', @IdComputadora = @idPc,
                                       @FechaEntregaEstimada = @Hoy, @UsuarioRegistro = N'prueba';
SELECT @venta = Id FROM #Id; DELETE FROM #Id;
SELECT @n = Estado FROM Ventas WHERE NumeroVenta = @venta;
INSERT INTO @R SELECT 'PI-08', N'La venta nace en estado Pendiente (0)',
    CASE WHEN @n = 0 THEN 'OK' ELSE 'FALLA' END, N'Venta #' + CAST(@venta AS NVARCHAR(10)) + N', estado ' + CAST(@n AS NVARCHAR(10));

SAVE TRANSACTION sp_pi09;
BEGIN TRY
    EXEC sp_Ventas_Agregar @DniCliente = N'00000000', @IdComputadora = @idPc,
                           @FechaEntregaEstimada = @Hoy, @UsuarioRegistro = N'prueba';
    INSERT INTO @R VALUES ('PI-09', N'Venta a un cliente inexistente', 'FALLA', N'La base la aceptó');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi09;
    INSERT INTO @R VALUES ('PI-09', N'Venta a un cliente inexistente',
        CASE WHEN ERROR_NUMBER() = 547 THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N' (FK_Venta_Cliente)');
END CATCH
DELETE FROM #Id;

-- Pagos: seña (CU03) y saldo final (CU07).
INSERT INTO @Pago EXEC sp_Pagos_Agregar @NumeroVenta = @venta, @Tipo = 0, @Monto = 160.00,
                                        @FormaPago = 1, @Referencia = N'TRF-1', @Usuario = N'prueba';
SELECT TOP 1 @txt = NumeroRecibo FROM @Pago;
INSERT INTO @R SELECT 'PI-10', N'La seña genera un recibo REC-######## correlativo',
    CASE WHEN @txt LIKE N'REC-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THEN 'OK' ELSE 'FALLA' END, @txt;
EXEC sp_Ventas_CambiarEstado @NumeroVenta = @venta, @Estado = 1;

SAVE TRANSACTION sp_pi11;
BEGIN TRY
    EXEC sp_Pagos_Agregar @NumeroVenta = @venta, @Tipo = 0, @Monto = 1.00;
    INSERT INTO @R VALUES ('PI-11', N'Segunda seña sobre la misma venta', 'FALLA', N'La base la aceptó');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi11;
    INSERT INTO @R VALUES ('PI-11', N'Segunda seña sobre la misma venta',
        CASE WHEN ERROR_NUMBER() IN (2601, 2627) THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N' (UX_Pago_SenaUnica)');
END CATCH

INSERT INTO @Vta EXEC sp_Ventas_ObtenerParaProduccion;
SELECT @n = COUNT(*) FROM @Vta WHERE NumeroVenta = @venta;
INSERT INTO @R SELECT 'PI-12', N'La venta señada aparece en sp_Ventas_ObtenerParaProduccion',
    CASE WHEN @n = 1 THEN 'OK' ELSE 'FALLA' END, N'Coincidencias: ' + CAST(@n AS NVARCHAR(10));

-- Orden de producción (CU04 a CU06).
INSERT INTO #Id EXEC sp_OP_Agregar @NumeroVenta = @venta, @FechaEntregaEstimada = @Hoy;
SELECT @orden = Id FROM #Id; DELETE FROM #Id;
SELECT @n = Estado FROM OrdenesProduccion WHERE NumeroOrden = @orden;
INSERT INTO @R SELECT 'PI-13', N'La orden nace en estado Pendiente (0)',
    CASE WHEN @n = 0 THEN 'OK' ELSE 'FALLA' END, N'OP #' + CAST(@orden AS NVARCHAR(10));

SAVE TRANSACTION sp_pi14;
BEGIN TRY
    EXEC sp_OP_Agregar @NumeroVenta = @venta, @FechaEntregaEstimada = @Hoy;
    INSERT INTO @R VALUES ('PI-14', N'Segunda orden para la misma venta', 'FALLA', N'La base la aceptó');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi14;
    INSERT INTO @R VALUES ('PI-14', N'Segunda orden para la misma venta',
        CASE WHEN ERROR_NUMBER() IN (2601, 2627) THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N' (UQ_OP_Venta)');
END CATCH
DELETE FROM #Id;

EXEC sp_OP_Planificar @NumeroOrden = @orden, @IdLinea = @idLinea, @FechaInicioPrevista = @Hoy,
                      @ResponsableTecnico = N'Técnico de prueba';
SELECT @n = Estado, @m = IdLinea FROM OrdenesProduccion WHERE NumeroOrden = @orden;
INSERT INTO @R SELECT 'PI-15', N'Planificar deja la orden Planificada (1) con su línea',
    CASE WHEN @n = 1 AND @m = @idLinea THEN 'OK' ELSE 'FALLA' END, N'Estado ' + CAST(@n AS NVARCHAR(10));

EXEC sp_OP_Desplanificar @NumeroOrden = @orden;
SELECT @n = Estado, @m = ISNULL(IdLinea, -1) FROM OrdenesProduccion WHERE NumeroOrden = @orden;
INSERT INTO @R SELECT 'PI-16', N'Desplanificar vuelve a Pendiente (0) y suelta la línea',
    CASE WHEN @n = 0 AND @m = -1 THEN 'OK' ELSE 'FALLA' END, N'Estado ' + CAST(@n AS NVARCHAR(10));

EXEC sp_OP_Planificar @NumeroOrden = @orden, @IdLinea = @idLinea, @FechaInicioPrevista = @Hoy,
                      @ResponsableTecnico = N'Técnico de prueba';
EXEC sp_OP_CambiarEstado @NumeroOrden = @orden, @Estado = 2;
EXEC sp_OP_RegistrarControlCalidad @NumeroOrden = @orden, @Encendido = 1, @Conexiones = 1,
                                   @SistemaOperativo = 1, @Drivers = 1, @Responsable = N'Técnico de prueba';
EXEC sp_OP_Cerrar @NumeroOrden = @orden, @NumeroSerie = N'SN-TEST-00001';
SELECT @n = NULL, @txt = NULL;
SELECT @n = Estado, @txt = NumeroSerie FROM OrdenesProduccion WHERE NumeroOrden = @orden AND FechaCierre IS NOT NULL AND CcFecha IS NOT NULL;
INSERT INTO @R SELECT 'PI-17', N'Cerrar con control aprobado: Finalizada (3), N° de serie y fechas',
    CASE WHEN @n = 3 AND @txt = N'SN-TEST-00001' THEN 'OK' ELSE 'FALLA' END, ISNULL(@txt, N'(sin serie)');

DELETE FROM @Pago;
INSERT INTO @Pago EXEC sp_Pagos_Agregar @NumeroVenta = @venta, @Tipo = 1, @Monto = 160.00, @FormaPago = 2;
SELECT TOP 1 @txt = NumeroRecibo FROM @Pago;
INSERT INTO @R SELECT 'PI-18', N'El cobro del saldo final genera la factura FAC-########',
    CASE WHEN @txt LIKE N'FAC-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THEN 'OK' ELSE 'FALLA' END, @txt;
EXEC sp_OP_CambiarEstado @NumeroOrden = @orden, @Estado = 4;
EXEC sp_Ventas_CambiarEstado @NumeroVenta = @venta, @Estado = 3;

-- Clientes (CU02).
SAVE TRANSACTION sp_pi19;
BEGIN TRY
    EXEC sp_Clientes_Agregar @Dni = N'99000001', @Nombre = N'X', @Apellido = N'Y', @Telefono = NULL, @Direccion = NULL;
    INSERT INTO @R VALUES ('PI-19', N'Alta de un cliente con DNI repetido', 'FALLA', N'La base la aceptó');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi19;
    INSERT INTO @R VALUES ('PI-19', N'Alta de un cliente con DNI repetido',
        CASE WHEN ERROR_NUMBER() IN (2601, 2627) THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N' (PK Clientes)');
END CATCH

SAVE TRANSACTION sp_pi20;
BEGIN TRY
    EXEC sp_Clientes_Eliminar @Dni = N'99000001';
    INSERT INTO @R VALUES ('PI-20', N'Borrar un cliente que tiene ventas', 'FALLA', N'La base lo borró');
END TRY
BEGIN CATCH
    IF XACT_STATE() = 1 ROLLBACK TRANSACTION sp_pi20;
    INSERT INTO @R VALUES ('PI-20', N'Borrar un cliente que tiene ventas',
        CASE WHEN ERROR_NUMBER() = 547 THEN 'OK' ELSE 'FALLA' END,
        N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + N' (FK_Venta_Cliente)');
END CATCH

-- Reporte RF1 (sp_Reporte_VentasProduccion).
-- Ventas con fecha controlada: una a medida el día "desde" a las 00:00 y otra un
-- segundo antes, para probar los dos bordes del período.
INSERT INTO #Id EXEC sp_Computadoras_Agregar @Nombre = N'PC a medida · T C1', @TipoConfiguracion = 1,
                                             @PrecioTotal = 250.00, @IdModeloOrigen = NULL;
SELECT @idPc2 = Id FROM #Id; DELETE FROM #Id;
INSERT INTO #Id EXEC sp_Computadoras_Agregar @Nombre = N'ZZ Modelo de prueba', @TipoConfiguracion = 0,
                                             @PrecioTotal = 320.00, @IdModeloOrigen = @idModelo;
SELECT @idPc3 = Id FROM #Id; DELETE FROM #Id;

DECLARE @Desde DATE = DATEADD(DAY, -10, @Hoy);
INSERT INTO Ventas (DniCliente, IdComputadora, FechaVenta, FechaEntregaEstimada, Estado, UsuarioRegistro)
VALUES (N'99000002', @idPc2, CAST(@Desde AS DATETIME), DATEADD(DAY, -2, @Hoy), 0, N'prueba');
SET @venta2 = SCOPE_IDENTITY();
INSERT INTO Ventas (DniCliente, IdComputadora, FechaVenta, FechaEntregaEstimada, Estado, UsuarioRegistro)
VALUES (N'99000001', @idPc3, DATEADD(SECOND, -1, CAST(@Desde AS DATETIME)), @Hoy, 4, N'prueba');
SET @venta3 = SCOPE_IDENTITY();

INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @Cliente = N'9900000';
SELECT @n = COUNT(*) FROM @Rep WHERE NumeroVenta IN (@venta, @venta2, @venta3);
INSERT INTO @R SELECT 'PR-01', N'Período inclusive: entran la venta de hoy y la del día "desde"; no la de un segundo antes',
    CASE WHEN @n = 2 AND NOT EXISTS (SELECT 1 FROM @Rep WHERE NumeroVenta = @venta3) THEN 'OK' ELSE 'FALLA' END,
    N'Filas de prueba devueltas: ' + CAST(@n AS NVARCHAR(10));

SELECT @n = COUNT(*) FROM @Rep WHERE NumeroVenta = @venta
    AND Sena = 160.00 AND SaldoCobrado = 160.00 AND Total = 320.00
    AND EstadoOrden = 4 AND Linea = N'ZZ Línea de prueba' AND NumeroSerie = N'SN-TEST-00001'
    AND ModeloOrigen = N'ZZ Modelo de prueba' AND FechaEntrega IS NOT NULL;
INSERT INTO @R SELECT 'PR-02', N'La fila trae cobros, orden, línea, serie, modelo y fecha de entrega correctos',
    CASE WHEN @n = 1 THEN 'OK' ELSE 'FALLA' END, N'Coincidencias: ' + CAST(@n AS NVARCHAR(10));

SELECT @n = COUNT(*) FROM @Rep WHERE NumeroVenta = @venta2 AND ModeloOrigen IS NULL AND NumeroOrden IS NULL
    AND FechaEntregaComprometida = DATEADD(DAY, -2, @Hoy);
INSERT INTO @R SELECT 'PR-03', N'Venta a medida sin orden: sin modelo y con la entrega estimada de la venta',
    CASE WHEN @n = 1 THEN 'OK' ELSE 'FALLA' END, N'Coincidencias: ' + CAST(@n AS NVARCHAR(10));

DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @Estado = 3, @Cliente = N'9900000';
SELECT @n = COUNT(*) FROM @Rep; SELECT @m = COUNT(*) FROM @Rep WHERE EstadoVenta <> 3;
INSERT INTO @R SELECT 'PR-04', N'Filtro por estado (Entregada): solo devuelve entregadas',
    CASE WHEN @n >= 1 AND @m = 0 THEN 'OK' ELSE 'FALLA' END, N'Filas: ' + CAST(@n AS NVARCHAR(10));

DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @TipoConfiguracion = 1, @Cliente = N'9900000';
SELECT @n = COUNT(*) FROM @Rep; SELECT @m = COUNT(*) FROM @Rep WHERE TipoConfiguracion <> 1;
INSERT INTO @R SELECT 'PR-05', N'Filtro por tipo (a medida): solo devuelve equipos a medida',
    CASE WHEN @n = 1 AND @m = 0 THEN 'OK' ELSE 'FALLA' END, N'Filas: ' + CAST(@n AS NVARCHAR(10));

DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @Cliente = N'zet';
SELECT @n = COUNT(*) FROM @Rep WHERE Dni = N'99000001'; SELECT @m = COUNT(*) FROM @Rep WHERE Dni = N'99000002';
INSERT INTO @R SELECT 'PR-06', N'Búsqueda parcial por apellido, sin distinguir mayúsculas ("zet" → Zeta)',
    CASE WHEN @n >= 1 AND @m = 0 THEN 'OK' ELSE 'FALLA' END, N'Zeta: ' + CAST(@n AS NVARCHAR(10)) + N', Omega: ' + CAST(@m AS NVARCHAR(10));

DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @Cliente = N'a_T';
SELECT @n = COUNT(*) FROM @Rep WHERE Dni = N'99000002'; SELECT @m = COUNT(*) FROM @Rep WHERE Dni <> N'99000002';
INSERT INTO @R SELECT 'PR-07', N'El "_" se busca literal (a_T encuentra Omega_Test y no funciona como comodín)',
    CASE WHEN @n = 1 AND @m = 0 THEN 'OK' ELSE 'FALLA' END, N'Omega: ' + CAST(@n AS NVARCHAR(10)) + N', otros: ' + CAST(@m AS NVARCHAR(10));

DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @Cliente = N'%';
SELECT @n = COUNT(*) FROM @Rep;
INSERT INTO @R SELECT 'PR-08', N'El "%" no trae todo: ningún cliente tiene "%" en su nombre',
    CASE WHEN @n = 0 THEN 'OK' ELSE 'FALLA' END, N'Filas: ' + CAST(@n AS NVARCHAR(10));

UPDATE Componentes SET PrecioUnitario = PrecioUnitario * 10 WHERE Codigo = N'ZZT-RAM';
DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Desde, @Hasta = @Hoy, @Cliente = N'99000001';
SELECT @n = COUNT(*) FROM @Rep WHERE NumeroVenta = @venta AND Total = 320.00;
INSERT INTO @R SELECT 'PR-09', N'El total es el precio congelado al vender, aunque después suba un componente',
    CASE WHEN @n = 1 THEN 'OK' ELSE 'FALLA' END, N'Coincidencias: ' + CAST(@n AS NVARCHAR(10));

DELETE FROM @Rep;
INSERT INTO @Rep EXEC sp_Reporte_VentasProduccion @Desde = @Hoy, @Hasta = @Desde;
SELECT @n = COUNT(*) FROM @Rep;
INSERT INTO @R SELECT 'PR-10', N'Período invertido en la base: no devuelve filas (la BLL lo rechaza antes)',
    CASE WHEN @n = 0 THEN 'OK' ELSE 'FALLA' END, N'Filas: ' + CAST(@n AS NVARCHAR(10));

DROP TABLE #Id;

IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

SELECT Caso, Resultado, Descripcion, Detalle FROM @R ORDER BY Orden;

DECLARE @ok INT = (SELECT COUNT(*) FROM @R WHERE Resultado = 'OK'),
        @tot INT = (SELECT COUNT(*) FROM @R);
PRINT '';
PRINT 'Resultado: ' + CAST(@ok AS VARCHAR(10)) + ' de ' + CAST(@tot AS VARCHAR(10)) + ' casos OK.';
PRINT 'La transacción se deshizo: la base quedó como estaba.';
GO
