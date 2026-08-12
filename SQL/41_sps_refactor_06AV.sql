-- ============================================================================
--  41_sps_refactor_06AV.sql
--  Procedimientos almacenados para el esquema refactorizado (correr DESPUÉS de
--  40_refactor_dominio_06AV.sql). Cubre Componentes (fusionado), Computadora,
--  Pago, Recibo, OrdenCompra + Cotizacion + FacturaCompra, OrdenProduccion +
--  FacturaVenta. Los Id string los genera la aplicación (GeneradorCodigo06AV);
--  los números de negocio (NumeroCompra/NumeroOrden) los asigna la secuencia.
-- ============================================================================
SET NOCOUNT ON;
GO

-- ══════════════════ COMPONENTES (fusiona Insumo) ══════════════════
CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerTodos AS
BEGIN SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Marca, Modelo, PrecioUnitario, Stock, StockMinimo, Tipo
    FROM Componentes ORDER BY Descripcion;
END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerPorCodigo @Codigo NVARCHAR(50) AS
BEGIN SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Marca, Modelo, PrecioUnitario, Stock, StockMinimo, Tipo
    FROM Componentes WHERE Codigo = @Codigo;
END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_ObtenerBajoStock AS
BEGIN SET NOCOUNT ON;
    SELECT Codigo, Descripcion, Marca, Modelo, PrecioUnitario, Stock, StockMinimo, Tipo
    FROM Componentes WHERE Stock <= StockMinimo ORDER BY Descripcion;
END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_Agregar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(18,2), @Stock INT, @StockMinimo INT, @Tipo INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO Componentes (Codigo, Descripcion, Marca, Modelo, PrecioUnitario, Stock, StockMinimo, Tipo)
    VALUES (@Codigo, @Descripcion, @Marca, @Modelo, @PrecioUnitario, @Stock, @StockMinimo, @Tipo);
END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_Modificar
    @Codigo NVARCHAR(50), @Descripcion NVARCHAR(200), @Marca NVARCHAR(100), @Modelo NVARCHAR(100),
    @PrecioUnitario DECIMAL(18,2), @Stock INT, @StockMinimo INT, @Tipo INT AS
BEGIN SET NOCOUNT ON;
    UPDATE Componentes SET Descripcion=@Descripcion, Marca=@Marca, Modelo=@Modelo,
        PrecioUnitario=@PrecioUnitario, Stock=@Stock, StockMinimo=@StockMinimo, Tipo=@Tipo
    WHERE Codigo=@Codigo;
END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_Eliminar @Codigo NVARCHAR(50) AS
BEGIN SET NOCOUNT ON; DELETE FROM Componentes WHERE Codigo=@Codigo; END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_DescontarStock @Codigo NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    UPDATE Componentes SET Stock = Stock - @Cantidad WHERE Codigo=@Codigo AND Stock >= @Cantidad;
    IF @@ROWCOUNT = 0 THROW 51000, 'Stock insuficiente o componente inexistente al descontar stock.', 1;
END
GO
CREATE OR ALTER PROCEDURE sp_Componentes_SumarStock @Codigo NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    UPDATE Componentes SET Stock = Stock + @Cantidad WHERE Codigo=@Codigo;
    IF @@ROWCOUNT = 0 THROW 51001, 'Componente inexistente al sumar stock.', 1;
END
GO

-- ══════════════════ COMPUTADORA + COMPONENTES ══════════════════
CREATE OR ALTER PROCEDURE sp_Computadoras_Agregar
    @Id NVARCHAR(30), @DniCliente NVARCHAR(20), @Nombre NVARCHAR(200),
    @TipoConfiguracion INT, @IdModeloOrigen INT, @PrecioTotal DECIMAL(18,2) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO Computadoras (Id, DniCliente, Nombre, TipoConfiguracion, IdModeloOrigen, PrecioTotal)
    VALUES (@Id, @DniCliente, @Nombre, @TipoConfiguracion, @IdModeloOrigen, @PrecioTotal);
END
GO
CREATE OR ALTER PROCEDURE sp_Computadora_AgregarComponente
    @IdComputadora NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Orden INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente, Orden)
    VALUES (@IdComputadora, @CodigoComponente, @Orden);
END
GO
CREATE OR ALTER PROCEDURE sp_Computadoras_ObtenerPorId @Id NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT Id, DniCliente, Nombre, TipoConfiguracion, IdModeloOrigen, PrecioTotal
    FROM Computadoras WHERE Id=@Id;
    SELECT cc.CodigoComponente, cc.Orden, c.Descripcion, c.Marca, c.Modelo, c.PrecioUnitario,
           c.Stock, c.StockMinimo, c.Tipo
    FROM ComputadoraComponentes cc JOIN Componentes c ON c.Codigo=cc.CodigoComponente
    WHERE cc.IdComputadora=@Id ORDER BY cc.Orden;
END
GO
CREATE OR ALTER PROCEDURE sp_Computadora_ExisteOrden @IdComputadora NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM OrdenesProduccion WHERE IdComputadora=@IdComputadora)
                THEN 1 ELSE 0 END AS BIT) AS Existe;
END
GO

-- ══════════════════ PAGOS + RECIBO ══════════════════
CREATE OR ALTER PROCEDURE sp_Pagos_Agregar
    @Id NVARCHAR(30), @IdComputadora NVARCHAR(30), @Tipo INT, @Monto DECIMAL(18,2), @Fecha DATETIME AS
BEGIN SET NOCOUNT ON;
    INSERT INTO Pagos (Id, IdComputadora, Tipo, Monto, Fecha)
    VALUES (@Id, @IdComputadora, @Tipo, @Monto, @Fecha);
END
GO
CREATE OR ALTER PROCEDURE sp_Pagos_ObtenerPorComputadora @IdComputadora NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT Id, IdComputadora, Tipo, Monto, Fecha FROM Pagos WHERE IdComputadora=@IdComputadora ORDER BY Fecha;
END
GO
CREATE OR ALTER PROCEDURE sp_Recibo_Agregar
    @Id NVARCHAR(30), @IdPago NVARCHAR(30), @FechaEmision DATETIME,
    @MontoAbonado DECIMAL(18,2), @SaldoPendiente DECIMAL(18,2), @FechaEntregaEstimada DATE AS
BEGIN SET NOCOUNT ON;
    INSERT INTO Recibo (Id, IdPago, FechaEmision, MontoAbonado, SaldoPendiente, FechaEntregaEstimada)
    VALUES (@Id, @IdPago, @FechaEmision, @MontoAbonado, @SaldoPendiente, @FechaEntregaEstimada);
END
GO
CREATE OR ALTER PROCEDURE sp_Recibo_ObtenerTodos AS
BEGIN SET NOCOUNT ON;
    SELECT r.Id, r.IdPago, r.FechaEmision, r.MontoAbonado, r.SaldoPendiente, r.FechaEntregaEstimada,
           p.IdComputadora, p.Monto, comp.Nombre AS NombrePC, comp.DniCliente
    FROM Recibo r JOIN Pagos p ON p.Id=r.IdPago JOIN Computadoras comp ON comp.Id=p.IdComputadora
    ORDER BY r.FechaEmision DESC;
END
GO

-- ══════════════════ ORDEN DE COMPRA + DETALLE ══════════════════
CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Agregar
    @Id NVARCHAR(30), @FechaLimite DATE, @DniRepositor NVARCHAR(20), @NumeroCompra INT OUTPUT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado)
    VALUES (@Id, @FechaLimite, @DniRepositor, 0);
    SELECT @NumeroCompra = NumeroCompra FROM OrdenesCompra WHERE Id=@Id;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenCompra_AgregarDetalle
    @IdOrdenCompra NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad)
    VALUES (@IdOrdenCompra, @CodigoComponente, @Cantidad);
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesCompra_CambiarEstado @Id NVARCHAR(30), @Estado INT AS
BEGIN SET NOCOUNT ON; UPDATE OrdenesCompra SET Estado=@Estado WHERE Id=@Id; END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Cerrar @Id NVARCHAR(30), @FechaCierre DATETIME AS
BEGIN SET NOCOUNT ON;
    UPDATE OrdenesCompra SET Estado=2 /*Finalizada*/, FechaCierre=@FechaCierre WHERE Id=@Id;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesCompra_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Id, NumeroCompra, FechaLimite, DniRepositor, Estado, FechaCierre
    FROM OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenCompra_ObtenerDetalle @IdOrdenCompra NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT d.CodigoComponente, d.Cantidad, c.Descripcion, c.Marca, c.Modelo, c.PrecioUnitario,
           c.Stock, c.StockMinimo, c.Tipo
    FROM OrdenCompraDetalle d JOIN Componentes c ON c.Codigo=d.CodigoComponente
    WHERE d.IdOrdenCompra=@IdOrdenCompra;
END
GO

-- ══════════════════ COTIZACION ══════════════════
CREATE OR ALTER PROCEDURE sp_Cotizacion_Agregar
    @Numero NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @IdProveedor INT,
    @Costo DECIMAL(18,2), @Condiciones NVARCHAR(500) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones)
    VALUES (@Numero, @IdOrdenCompra, @IdProveedor, 0 /*PorAprobar*/, @Costo, @Condiciones);
END
GO
CREATE OR ALTER PROCEDURE sp_Cotizacion_CambiarEstado
    @Numero NVARCHAR(30), @Estado INT, @DniGerenteAprobador NVARCHAR(20) AS
BEGIN SET NOCOUNT ON;
    UPDATE PedidosCotizacion SET Estado=@Estado, DniGerenteAprobador=@DniGerenteAprobador WHERE Numero=@Numero;
END
GO
CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerPorOrden @IdOrdenCompra NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador
    FROM PedidosCotizacion WHERE IdOrdenCompra=@IdOrdenCompra ORDER BY FechaEmision DESC;
END
GO
CREATE OR ALTER PROCEDURE sp_Cotizacion_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador
    FROM PedidosCotizacion ORDER BY FechaEmision DESC;
END
GO

-- ══════════════════ FACTURA DE COMPRA ══════════════════
CREATE OR ALTER PROCEDURE sp_FacturaCompra_Agregar
    @NumeroFactura NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @FechaEmision DATETIME,
    @FechaEntrega DATETIME, @Total DECIMAL(18,2), @Observaciones NVARCHAR(1000) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompra (NumeroFactura, IdOrdenCompra, FechaEmision, FechaEntrega, Total, Observaciones)
    VALUES (@NumeroFactura, @IdOrdenCompra, @FechaEmision, @FechaEntrega, @Total, @Observaciones);
END
GO
CREATE OR ALTER PROCEDURE sp_FacturaCompra_AgregarDetalle
    @NumeroFactura NVARCHAR(30), @CodigoComponente NVARCHAR(50), @Cantidad INT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompraDetalle (NumeroFactura, CodigoComponente, Cantidad)
    VALUES (@NumeroFactura, @CodigoComponente, @Cantidad);
END
GO

-- ══════════════════ ORDEN DE PRODUCCION + FACTURA DE VENTA ══════════════════
CREATE OR ALTER PROCEDURE sp_OrdenesProduccion_Agregar
    @Id NVARCHAR(30), @IdComputadora NVARCHAR(30), @FechaEntrega DATE, @NumeroOrden INT OUTPUT AS
BEGIN SET NOCOUNT ON;
    INSERT INTO OrdenesProduccion (Id, IdComputadora, FechaEntrega, Estado)
    VALUES (@Id, @IdComputadora, @FechaEntrega, 0 /*Pendiente*/);
    SELECT @NumeroOrden = NumeroOrden FROM OrdenesProduccion WHERE Id=@Id;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesProduccion_CambiarEstado @Id NVARCHAR(30), @Estado INT AS
BEGIN SET NOCOUNT ON; UPDATE OrdenesProduccion SET Estado=@Estado WHERE Id=@Id; END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesProduccion_Planificar
    @Id NVARCHAR(30), @IdLinea INT, @FechaInicio DATE, @Responsable NVARCHAR(150) AS
BEGIN SET NOCOUNT ON;
    UPDATE OrdenesProduccion SET IdLinea=@IdLinea, FechaInicioPrevista=@FechaInicio,
        ResponsableTecnico=@Responsable, Estado=1 /*Planificada*/ WHERE Id=@Id;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesProduccion_Cerrar @Id NVARCHAR(30), @FechaCierre DATETIME AS
BEGIN SET NOCOUNT ON;
    UPDATE OrdenesProduccion SET Estado=4 /*Entregada*/, FechaCierre=@FechaCierre WHERE Id=@Id;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesProduccion_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Id, NumeroOrden, IdComputadora, FechaEntrega, Estado, IdLinea,
           FechaInicioPrevista, ResponsableTecnico, FechaCierre
    FROM OrdenesProduccion ORDER BY NumeroOrden DESC;
END
GO
CREATE OR ALTER PROCEDURE sp_OrdenesProduccion_ObtenerPorId @Id NVARCHAR(30) AS
BEGIN SET NOCOUNT ON;
    SELECT Id, NumeroOrden, IdComputadora, FechaEntrega, Estado, IdLinea,
           FechaInicioPrevista, ResponsableTecnico, FechaCierre
    FROM OrdenesProduccion WHERE Id=@Id;
END
GO
CREATE OR ALTER PROCEDURE sp_FacturaVenta_Agregar
    @NumeroFactura NVARCHAR(30), @IdOrdenProduccion NVARCHAR(30), @FechaEmision DATETIME, @Total DECIMAL(18,2) AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaVenta (NumeroFactura, IdOrdenProduccion, FechaEmision, Total)
    VALUES (@NumeroFactura, @IdOrdenProduccion, @FechaEmision, @Total);
END
GO
CREATE OR ALTER PROCEDURE sp_FacturaVenta_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT NumeroFactura, IdOrdenProduccion, FechaEmision, Total FROM FacturaVenta ORDER BY FechaEmision DESC;
END
GO

PRINT 'Procedimientos almacenados del refactor creados.';
GO
