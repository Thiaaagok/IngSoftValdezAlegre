-- ============================================================
--  40_compras_pn2.sql
--  COMPRA DE INSUMOS (PN2): lo que faltaba del RFN2.
--
--    1. Orden de compra anulada o cerrada con faltante.
--         Estado 4 = Anulada            (no se adjudicó / el proveedor no la cumplió)
--         Estado 5 = CerradaConFaltante (llegó una parte y el resto no va a llegar)
--       Las dos liberan los componentes para volver a pedirlos y guardan el
--       motivo en OrdenesCompra.MotivoCierre.
--    2. Factura del proveedor en cada recepción (RFN2 paso 9): número y total
--       facturado, además del total esperado según la cotización.
--    3. Pagos al proveedor (RFN2 paso 10): tabla PagosProveedor.
--    4. Proveedores con estado Activo/Inactivo (baja lógica).
--    5. Reporte de Lista de Proveedores.
--    6. Patentes PagarProveedores y VerReporteProveedores.
--    7. Vacía la tabla DV (se recalcula sola en el próximo ingreso).
--
--  Es idempotente: se puede correr varias veces.
-- ============================================================

SET NOCOUNT ON;
GO

-- ── 1) Orden de compra: motivo de anulación / cierre ─────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('OrdenesCompra') AND name = 'MotivoCierre')
    ALTER TABLE OrdenesCompra ADD MotivoCierre NVARCHAR(300) NULL;
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_ObtenerTodas AS
BEGIN SET NOCOUNT ON;
    SELECT Id, NumeroCompra, FechaLimite, DniRepositor, Estado, FechaCierre, MotivoCierre
    FROM OrdenesCompra ORDER BY NumeroCompra DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_OrdenesCompra_Terminar
    @Id NVARCHAR(30), @Estado INT, @FechaCierre DATETIME, @Motivo NVARCHAR(300)
AS
BEGIN SET NOCOUNT ON;
    IF @Estado NOT IN (4, 5)
        THROW 51030, 'Estado de cierre inválido.', 1;
    UPDATE OrdenesCompra
    SET    Estado = @Estado, FechaCierre = @FechaCierre, MotivoCierre = @Motivo
    WHERE  Id = @Id;
END
GO

-- ── 2) Factura del proveedor en cada recepción ───────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FacturaCompra') AND name = 'NumeroFacturaProveedor')
    ALTER TABLE FacturaCompra ADD NumeroFacturaProveedor NVARCHAR(50) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FacturaCompra') AND name = 'TotalFacturado')
    ALTER TABLE FacturaCompra ADD TotalFacturado DECIMAL(18,2) NULL;
GO

CREATE OR ALTER PROCEDURE sp_FacturaCompra_Agregar
    @NumeroFactura NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @FechaEmision DATETIME,
    @FechaEntrega DATETIME, @Total DECIMAL(18,2), @Observaciones NVARCHAR(1000),
    @NumeroFacturaProveedor NVARCHAR(50) = NULL, @TotalFacturado DECIMAL(18,2) = NULL
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO FacturaCompra (NumeroFactura, IdOrdenCompra, FechaEmision, FechaEntrega, Total, Observaciones,
                               NumeroFacturaProveedor, TotalFacturado)
    VALUES (@NumeroFactura, @IdOrdenCompra, @FechaEmision, @FechaEntrega, @Total, @Observaciones,
            @NumeroFacturaProveedor, @TotalFacturado);
END
GO

CREATE OR ALTER PROCEDURE sp_FacturaCompra_ObtenerPorOrden
    @IdOrdenCompra NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  f.NumeroFactura, f.IdOrdenCompra, f.FechaEmision, f.FechaEntrega, f.Total, f.Observaciones,
            f.NumeroFacturaProveedor, f.TotalFacturado
    FROM    FacturaCompra f
    WHERE   f.IdOrdenCompra = @IdOrdenCompra
    ORDER BY f.FechaEmision;
END
GO

-- ── 3) Pagos al proveedor ────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PagosProveedor')
CREATE TABLE PagosProveedor (
    Numero        NVARCHAR(30)  NOT NULL CONSTRAINT PK_PagosProveedor PRIMARY KEY,   -- "PP-2026-0001"
    IdOrdenCompra NVARCHAR(30)  NOT NULL,
    Monto         DECIMAL(18,2) NOT NULL,
    FechaPago     DATE          NOT NULL,
    FormaPago     INT           NOT NULL CONSTRAINT DF_PP_Forma DEFAULT (1),   -- FormaPago06AV
    Referencia    NVARCHAR(100) NULL,
    DniUsuario    NVARCHAR(20)  NULL,
    FechaRegistro DATETIME      NOT NULL CONSTRAINT DF_PP_Registro DEFAULT (GETDATE()),
    CONSTRAINT FK_PP_OC      FOREIGN KEY (IdOrdenCompra) REFERENCES OrdenesCompra(Id),
    CONSTRAINT FK_PP_Usuario FOREIGN KEY (DniUsuario)    REFERENCES Usuarios(Dni),
    CONSTRAINT CK_PP_Monto   CHECK (Monto > 0)
);
GO

CREATE OR ALTER PROCEDURE sp_PagosProveedor_Agregar
    @Numero NVARCHAR(30), @IdOrdenCompra NVARCHAR(30), @Monto DECIMAL(18,2), @FechaPago DATE,
    @FormaPago INT, @Referencia NVARCHAR(100), @DniUsuario NVARCHAR(20)
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO PagosProveedor (Numero, IdOrdenCompra, Monto, FechaPago, FormaPago, Referencia, DniUsuario)
    VALUES (@Numero, @IdOrdenCompra, @Monto, @FechaPago, @FormaPago, NULLIF(@Referencia, N''), @DniUsuario);
END
GO

CREATE OR ALTER PROCEDURE sp_PagosProveedor_ObtenerTodos
AS
BEGIN SET NOCOUNT ON;
    SELECT Numero, IdOrdenCompra, Monto, FechaPago, FormaPago, Referencia, DniUsuario, FechaRegistro
    FROM   PagosProveedor
    ORDER  BY FechaPago, FechaRegistro;
END
GO

-- ── 4) Proveedores activos / inactivos ───────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Proveedores') AND name = 'Activo')
    ALTER TABLE Proveedores ADD Activo BIT NOT NULL CONSTRAINT DF_Proveedores_Activo DEFAULT (1);
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion, Activo FROM Proveedores ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion, Activo FROM Proveedores WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_ObtenerPorCuit
    @Cuit NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Cuit, Email, Telefono, Direccion, Activo FROM Proveedores WHERE Cuit = @Cuit;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Agregar
    @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200), @Activo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion, Activo)
    VALUES (@Nombre, @Cuit, @Email, @Telefono, @Direccion, ISNULL(@Activo, 1));
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NuevoId;
END
GO

CREATE OR ALTER PROCEDURE sp_Proveedores_Modificar
    @Id INT, @Nombre NVARCHAR(150), @Cuit NVARCHAR(20), @Email NVARCHAR(150),
    @Telefono NVARCHAR(50), @Direccion NVARCHAR(200), @Activo BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Proveedores
    SET Nombre = @Nombre, Cuit = @Cuit, Email = @Email,
        Telefono = @Telefono, Direccion = @Direccion, Activo = ISNULL(@Activo, 1)
    WHERE Id = @Id;
END
GO

--  Un proveedor que ya cotizó no se borra: queda inactivo (baja lógica) para
--  no perder el historial de compras. Devuelve 'E' si se eliminó o 'B' si
--  quedó dado de baja.
CREATE OR ALTER PROCEDURE sp_Proveedores_Eliminar
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM PedidosCotizacion WHERE IdProveedor = @Id)
    BEGIN
        UPDATE Proveedores SET Activo = 0 WHERE Id = @Id;
        SELECT 'B' AS Resultado;
        RETURN;
    END
    DELETE FROM Proveedores WHERE Id = @Id;
    SELECT 'E' AS Resultado;
END
GO

-- ── 5) Reporte de Lista de Proveedores ───────────────────────
CREATE OR ALTER PROCEDURE sp_ReporteProveedores
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Nombre, p.Cuit, p.Email, p.Telefono, p.Direccion, p.Activo,
           (SELECT COUNT(*) FROM PedidosCotizacion c WHERE c.IdProveedor = p.Id)                       AS Cotizaciones,
           (SELECT COUNT(DISTINCT c.IdOrdenCompra) FROM PedidosCotizacion c
             WHERE c.IdProveedor = p.Id AND c.Estado = 1)                                               AS OrdenesCompra,
           (SELECT ISNULL(SUM(ISNULL(f.TotalFacturado, f.Total)), 0)
              FROM FacturaCompra f
             WHERE f.IdOrdenCompra IN (SELECT c.IdOrdenCompra FROM PedidosCotizacion c
                                        WHERE c.IdProveedor = p.Id AND c.Estado = 1))                   AS TotalComprado,
           (SELECT MAX(c.FechaEmision) FROM PedidosCotizacion c WHERE c.IdProveedor = p.Id AND c.Estado = 1) AS UltimaCompra
    FROM   Proveedores p
    ORDER  BY p.Nombre;
END
GO

--  Tipos de componente que cotizó cada proveedor (para "Tipo de componentes que provee").
CREATE OR ALTER PROCEDURE sp_ReporteProveedores_Tipos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT c.IdProveedor, comp.Tipo
    FROM   PedidosCotizacion c
           JOIN OrdenCompraDetalle d ON d.IdOrdenCompra = c.IdOrdenCompra
           JOIN Componentes comp     ON comp.Codigo     = d.CodigoComponente;
END
GO

-- ── 6) Patentes nuevas ───────────────────────────────────────
DECLARE @P TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
INSERT INTO @P VALUES
    ('PagarProveedores',      'Registrar pagos a proveedores'),
    ('VerReporteProveedores', 'Ver el reporte de lista de proveedores');

INSERT INTO Patentes (Id, Descripcion)
SELECT p.Id, p.Descripcion FROM @P p WHERE NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);

DECLARE @Asignar TABLE (IdRol NVARCHAR(450), IdPatente NVARCHAR(450));
INSERT INTO @Asignar (IdRol, IdPatente)
SELECT r.Id, p.Id FROM Roles r CROSS JOIN @P p WHERE r.Descripcion = 'Administrador' OR r.Codigo = 'ADM';
INSERT INTO @Asignar (IdRol, IdPatente)
SELECT r.Id, v.IdPatente
FROM   Roles r
       JOIN (VALUES (N'Repositor', N'PagarProveedores'), (N'Repositor', N'VerReporteProveedores'),
                    (N'GerenteCompras', N'VerReporteProveedores')) v(IdRol, IdPatente) ON v.IdRol = r.Id;

INSERT INTO RolPatentes (IdRol, IdPatente)
SELECT DISTINCT a.IdRol, a.IdPatente
FROM   @Asignar a
WHERE  NOT EXISTS (SELECT 1 FROM RolPatentes rp WHERE rp.IdRol = a.IdRol AND rp.IdPatente = a.IdPatente);
GO

-- ── 7) Dígito verificador ────────────────────────────────────
--  OrdenesCompra y Proveedores cambiaron de estructura, PagosProveedor pasa a
--  ser tabla protegida y se agregaron patentes: se vacía DV y el sistema
--  recalcula la línea base en el próximo ingreso.
IF OBJECT_ID('DV') IS NOT NULL
    DELETE FROM DV;
GO

PRINT '40_compras_pn2.sql aplicado. Cerrá sesión y volvé a entrar para tomar las patentes nuevas.';
GO
