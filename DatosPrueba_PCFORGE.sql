/* =====================================================================
   PCFORGE — Script de datos de prueba (carga adicional)
   Base: IngSoftValdezAlegre

   QUÉ HACE
   - SOLO INSERTA. No modifica ni borra ninguna fila que ya exista en tus
     tablas de negocio (clientes, componentes, ventas, órdenes, compras,
     usuarios, roles, etc.). Los únicos UPDATE son sobre filas que el propio
     script acaba de insertar (número de recibo y número de serie, que se
     arman a partir del Id autonumérico).
   - Las ventas "activas" nuevas (pendientes, señadas o en producción) usan
     SOLO componentes nuevos, así el StockReservado de tus componentes
     actuales no cambia. Las ventas históricas (entregadas / anuladas) sí
     pueden usar los componentes y modelos que ya tenías, porque no afectan
     el stock actual.
   - Todo corre en UNA transacción: si algo falla, no queda nada a medias.
   - Si detecta que ya se ejecutó (o que algún código/DNI choca con uno
     existente), no hace nada y avisa.

   DÍGITO VERIFICADOR
   - Al insertar en tablas protegidas, el DV guardado deja de coincidir.
     Por eso, al final se vacía la tabla DV (es un dato derivado, no un dato
     de negocio). En el próximo inicio de sesión el sistema detecta que no
     hay línea base y la recalcula sola (FRMLogin → SinLineaBase → Recalcular).

   USUARIOS NUEVOS (roles de la tabla G04 del TD) — contraseña: Pcforge2026!
     tpereyra  Recepcionista         (CU01, CU02, CU03, CU07)
     tortiz    Gerente de producción (CU04, CU05)
     bherrera  Responsable técnico   (CU06)
     jacosta   Repositor             (CU08, CU09, CU10, CU12)
     lalegre   Gerente de compras    (CU11)
     smedina   Administrador
   ===================================================================== */

USE [IngSoftValdezAlegre];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* ---------------------------------------------------------------------
   0. Datos a cargar (tablas temporales)
   --------------------------------------------------------------------- */

-- Clientes -------------------------------------------------------------
CREATE TABLE #Cliente (Dni NVARCHAR(20) PRIMARY KEY, Nombre NVARCHAR(100), Apellido NVARCHAR(100),
                       Telefono NVARCHAR(50), Direccion NVARCHAR(200));
INSERT INTO #Cliente VALUES
 (N'20345678', N'Martín',    N'Rodríguez', N'11-5002-0011',  N'Av. Corrientes 1850, CABA'),
 (N'22456123', N'Valeria',   N'Suárez',    N'11-5002-0012',  N'Av. Cabildo 2300, CABA'),
 (N'23567890', N'Diego',     N'Romero',    N'221-455-0013',  N'Calle 7 N° 845, La Plata'),
 (N'24678901', N'Florencia', N'Díaz',      N'351-500-0014',  N'Av. Colón 1500, Córdoba'),
 (N'25789012', N'Gustavo',   N'Morales',   N'341-600-0015',  N'Bv. Oroño 900, Rosario'),
 (N'26890123', N'Carolina',  N'Castro',    N'11-5002-0016',  N'Av. Mitre 650, Avellaneda'),
 (N'29123456', N'Nicolás',   N'Ríos',      N'11-5002-0017',  N'Hipólito Yrigoyen 8800, Lomas de Zamora'),
 (N'32234567', N'Micaela',   N'Ortega',    N'11-5002-0018',  N'Av. Rivadavia 14200, Ramos Mejía'),
 (N'34345678', N'Federico',  N'Acuña',     N'261-420-0019',  N'San Martín 1100, Mendoza'),
 (N'35456789', N'Agustina',  N'Molina',    N'11-5002-0020',  N'Av. Santa Fe 3100, CABA'),
 (N'36567890', N'Emiliano',  N'Vera',      N'11-5002-0021',  N'Laprida 450, Lomas de Zamora'),
 (N'38678901', N'Camila',    N'Paz',       N'11-5002-0022',  N'Av. Belgrano 2100, Quilmes'),
 (N'39789012', N'Joaquín',   N'Ibarra',    N'223-495-0023',  N'Av. Colón 2500, Mar del Plata'),
 (N'40890123', N'Milagros',  N'Quiroga',   N'11-5002-0024',  N'Alsina 300, Banfield'),
 (N'41901234', N'Santiago',  N'Giménez',   N'11-5002-0025',  N'Av. Pavón 2900, Lanús'),
 (N'42012345', N'Rocío',     N'Navarro',   N'11-5002-0026',  N'Pueyrredón 1200, Temperley');   -- registrada, sin compras

-- Componentes nuevos -------------------------------------------------------
-- Tipo: 0 Procesador, 1 RAM, 2 Disco, 3 Placa madre, 4 Fuente, 5 Gabinete, 6 Placa de video, 7 Refrigeración
-- Stock = stock físico ACTUAL. El StockReservado se calcula más abajo a partir de las ventas activas.
CREATE TABLE #Comp (Codigo NVARCHAR(50) PRIMARY KEY, Descripcion NVARCHAR(200), Tipo INT, Marca NVARCHAR(100),
                    Modelo NVARCHAR(100), Precio DECIMAL(12,2), Stock INT, StockMinimo INT, Baja BIT);
INSERT INTO #Comp VALUES
 (N'CPU-003',  N'Procesador 16 núcleos',             0, N'Intel',        N'Core i7-13700',        380.00, 8,  4, 0),
 (N'CPU-004',  N'Procesador 8 núcleos',              0, N'AMD',          N'Ryzen 7 7700',         330.00, 6,  4, 0),
 (N'CPU-005',  N'Procesador 4 núcleos',              0, N'Intel',        N'Core i3-12100',        110.00, 12, 5, 0),
 (N'RAM-003',  N'Memoria RAM 32GB DDR5',             1, N'Kingston',     N'Fury Beast 6000',      140.00, 14, 6, 0),
 (N'RAM-004',  N'Memoria RAM 8GB DDR4',              1, N'Crucial',      N'CT8G4DFRA32A',          28.00, 25, 10, 0),
 (N'SSD-002',  N'Disco SSD 500GB NVMe',              2, N'Western Digital', N'Blue SN580',         45.00, 20, 8, 0),
 (N'SSD-003',  N'Disco SSD 2TB NVMe',                2, N'Kingston',     N'KC3000',               150.00, 4,  5, 0),
 (N'HDD-002',  N'Disco HDD 1TB',                     2, N'Western Digital', N'Blue 7200',          45.00, 6,  6, 0),
 (N'MB-003',   N'Placa madre Z790',                  3, N'MSI',          N'PRO Z790-P',           240.00, 6,  3, 0),
 (N'MB-004',   N'Placa madre B650',                  3, N'ASRock',       N'B650M Pro RS',         170.00, 5,  4, 0),
 (N'MB-005',   N'Placa madre H610',                  3, N'Gigabyte',     N'H610M S2H',             85.00, 9,  5, 0),
 (N'PSU-002',  N'Fuente 850W 80+ Gold',              4, N'Corsair',      N'RM850e',               125.00, 10, 5, 0),
 (N'PSU-003',  N'Fuente 500W 80+ White',             4, N'Thermaltake',  N'Smart 500W',            45.00, 4,  6, 0),
 (N'GAB-002',  N'Gabinete Mid Tower airflow',        5, N'Lian Li',      N'Lancool 216',           95.00, 11, 5, 0),
 (N'GAB-003',  N'Gabinete Micro ATX compacto',       5, N'Cooler Master', N'Q300L',                50.00, 4,  5, 0),
 (N'GPU-002',  N'Placa de video RTX 4060',           6, N'Gigabyte',     N'Windforce OC 8G',      320.00, 7,  3, 0),
 (N'GPU-003',  N'Placa de video RTX 4070 Super',     6, N'ASUS',         N'Dual 12G',             620.00, 3,  2, 0),
 (N'GPU-004',  N'Placa de video RX 7600',            6, N'Sapphire',     N'Pulse 8G',             280.00, 1,  3, 0),
 (N'GPU-005',  N'Placa de video GTX 1650 (discontinuada)', 6, N'Zotac', N'Gaming 4G',            150.00, 0,  0, 1),  -- baja lógica
 (N'COOL-002', N'Refrigeración líquida 240mm',       7, N'NZXT',         N'Kraken 240',           130.00, 5,  3, 0),
 (N'COOL-003', N'Cooler para CPU torre',             7, N'DeepCool',     N'AK400',                 40.00, 10, 6, 0);

-- Modelos estándar nuevos ----------------------------------------------------
CREATE TABLE #Modelo (Nombre NVARCHAR(150) PRIMARY KEY, Descripcion NVARCHAR(300));
INSERT INTO #Modelo VALUES
 (N'PC Gamer Pro',        N'Equipo gamer de alta gama: RTX 4070 Super, 32GB DDR5 y refrigeración líquida'),
 (N'PC Gamer Entry',      N'Equipo gamer de entrada: Ryzen 7 con RTX 4060'),
 (N'PC Hogar',            N'Equipo compacto y económico para estudio y uso hogareño'),
 (N'Workstation Diseño',  N'Estación de trabajo para diseño y edición: i7, 32GB DDR5, SSD 2TB + HDD');

CREATE TABLE #ModeloComp (Modelo NVARCHAR(150), Codigo NVARCHAR(50), PRIMARY KEY (Modelo, Codigo));
INSERT INTO #ModeloComp VALUES
 (N'PC Gamer Pro', N'CPU-003'), (N'PC Gamer Pro', N'MB-003'), (N'PC Gamer Pro', N'RAM-003'), (N'PC Gamer Pro', N'SSD-003'),
 (N'PC Gamer Pro', N'GPU-003'), (N'PC Gamer Pro', N'PSU-002'), (N'PC Gamer Pro', N'GAB-002'), (N'PC Gamer Pro', N'COOL-002'),
 (N'PC Gamer Entry', N'CPU-004'), (N'PC Gamer Entry', N'MB-004'), (N'PC Gamer Entry', N'RAM-003'), (N'PC Gamer Entry', N'SSD-002'),
 (N'PC Gamer Entry', N'GPU-002'), (N'PC Gamer Entry', N'PSU-002'), (N'PC Gamer Entry', N'GAB-002'), (N'PC Gamer Entry', N'COOL-003'),
 (N'PC Hogar', N'CPU-005'), (N'PC Hogar', N'MB-005'), (N'PC Hogar', N'RAM-004'), (N'PC Hogar', N'SSD-002'),
 (N'PC Hogar', N'PSU-003'), (N'PC Hogar', N'GAB-003'),
 (N'Workstation Diseño', N'CPU-003'), (N'Workstation Diseño', N'MB-003'), (N'Workstation Diseño', N'RAM-003'),
 (N'Workstation Diseño', N'SSD-003'), (N'Workstation Diseño', N'HDD-002'), (N'Workstation Diseño', N'GPU-002'),
 (N'Workstation Diseño', N'PSU-002'), (N'Workstation Diseño', N'GAB-002'), (N'Workstation Diseño', N'COOL-002');

-- Ventas ---------------------------------------------------------------------
-- EstadoVenta: 0 Pendiente, 1 Señada, 2 EnProducción, 3 Entregada, 4 Anulada
-- OpEstado:    0 Pendiente, 1 Planificada, 2 EnEnsamblaje, 3 Finalizada, 4 Entregada, 5 EnRevisión
-- FormaPago:   0 Efectivo, 1 Transferencia, 2 Tarjeta
-- Modelo NULL = computadora configurable (componentes en #VentaComp).
CREATE TABLE #Venta (
    Clave NVARCHAR(10) PRIMARY KEY, Dni NVARCHAR(20), Modelo NVARCHAR(150) NULL,
    FechaVenta DATETIME, EstadoVenta INT,
    SenaFecha DATETIME NULL, SenaForma INT NULL, SenaRef NVARCHAR(100) NULL,
    OpFecha DATETIME NULL, OpEstado INT NULL, Linea NVARCHAR(100) NULL, InicioPrevisto DATE NULL,
    CcFecha DATETIME NULL, CcDrivers BIT NULL, CcObs NVARCHAR(500) NULL,
    SaldoFecha DATETIME NULL, SaldoForma INT NULL, SaldoRef NVARCHAR(100) NULL);

INSERT INTO #Venta VALUES
 -- Entregadas (CU07 completo: seña + orden cerrada + saldo y factura)
 (N'V01', N'20345678', N'PC Oficina',   '2026-09-15T09:40:00', 3, '2026-09-15T09:52:00', 0, NULL,
         '2026-09-15T16:10:00', 4, N'Línea C', '2026-09-16', '2026-09-19T11:30:00', 1, NULL, '2026-09-22T10:15:00', 1, N'TRF-0922-5512'),
 (N'V02', N'22456123', N'PC Gamer Pro', '2026-09-15T11:15:00', 3, '2026-09-15T11:30:00', 2, N'VISA ****4821',
         '2026-09-15T17:00:00', 4, N'Línea B', '2026-09-17', '2026-09-24T15:40:00', 1, NULL, '2026-09-26T12:30:00', 2, N'VISA ****4821'),
 (N'V03', N'23567890', NULL,            '2026-09-16T10:05:00', 3, '2026-09-16T10:20:00', 1, N'TRF-0916-0381',
         '2026-09-16T12:00:00', 4, N'Línea A', '2026-09-17', '2026-09-22T17:05:00', 1, NULL, '2026-09-24T09:45:00', 0, NULL),
 (N'V04', N'24678901', N'PC Hogar',     '2026-09-16T17:30:00', 3, '2026-09-16T17:41:00', 0, NULL,
         '2026-09-17T09:15:00', 4, N'Línea C', '2026-09-20', '2026-09-23T16:20:00', 1, NULL, '2026-09-25T18:00:00', 0, NULL),
 (N'V05', N'25789012', N'PC Básica',    '2026-09-17T12:00:00', 3, '2026-09-17T12:08:00', 1, N'TRF-0917-7720',
         '2026-09-17T15:30:00', 4, N'Línea C', '2026-09-24', '2026-09-26T13:10:00', 1, NULL, '2026-09-29T11:20:00', 1, N'TRF-0929-1044'),
 -- Finalizadas: listas para retirar (entrada del CU07)
 (N'V06', N'26890123', N'PC Gamer Entry', '2026-09-18T10:20:00', 2, '2026-09-18T10:35:00', 2, N'MASTER ****1190',
         '2026-09-18T14:00:00', 3, N'Línea A', '2026-09-23', '2026-09-29T12:15:00', 1, NULL, NULL, NULL, NULL),
 (N'V07', N'29123456', N'Workstation Diseño', '2026-09-19T15:45:00', 2, '2026-09-19T16:00:00', 1, N'TRF-0919-6630',
         '2026-09-20T09:30:00', 3, N'Línea B', '2026-09-25', '2026-09-29T17:40:00', 1, NULL, NULL, NULL, NULL),
 -- Anulada antes de señar
 (N'V18', N'32234567', N'PC Hogar',     '2026-09-20T11:00:00', 4, NULL, NULL, NULL,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 -- En revisión: el control de calidad no pasó (entrada del CU06)
 (N'V08', N'34345678', N'PC Gamer Pro', '2026-09-21T09:30:00', 2, '2026-09-21T09:45:00', 2, N'VISA ****3307',
         '2026-09-21T15:00:00', 5, N'Línea D', '2026-09-22', '2026-09-29T17:10:00', 0,
         N'La placa de video no toma los drivers: reinstalar y repetir el control.', NULL, NULL, NULL),
 -- Anulada (cliente existente)
 (N'V19', N'30111222', N'PC Oficina',   '2026-09-22T16:00:00', 4, NULL, NULL, NULL,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 -- En ensamblaje
 (N'V09', N'35456789', N'PC Hogar',     '2026-09-22T18:10:00', 2, '2026-09-22T18:20:00', 0, NULL,
         '2026-09-23T10:00:00', 2, N'Línea E', '2026-09-26', NULL, NULL, NULL, NULL, NULL, NULL),
 -- Planificada (CU05 hecho, falta iniciar ensamblaje)
 (N'V11', N'36567890', N'PC Gamer Entry', '2026-09-23T10:00:00', 2, '2026-09-23T10:12:00', 1, N'TRF-0923-2291',
         '2026-09-24T11:00:00', 1, N'Línea F', '2026-10-01', NULL, NULL, NULL, NULL, NULL, NULL),
 -- Orden generada, sin línea (entrada del CU05)
 (N'V10', N'38678901', NULL,            '2026-09-24T12:30:00', 2, '2026-09-24T12:50:00', 2, N'VISA ****7715',
         '2026-09-25T09:20:00', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 (N'V12', N'20345678', N'PC Hogar',     '2026-09-25T14:00:00', 2, '2026-09-25T14:05:00', 0, NULL,
         '2026-09-26T10:30:00', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 (N'V13', N'39789012', N'Workstation Diseño', '2026-09-26T11:10:00', 2, '2026-09-26T11:25:00', 1, N'TRF-0926-8804',
         '2026-09-28T09:45:00', 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 -- Señadas, sin orden (entrada del CU04)
 (N'V14', N'28999888', N'PC Gamer Pro', '2026-09-27T10:45:00', 1, '2026-09-27T11:00:00', 2, N'MASTER ****5502',
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 (N'V15', N'40890123', NULL,            '2026-09-28T16:20:00', 1, '2026-09-28T16:35:00', 0, NULL,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 -- Pendientes, sin seña (entrada del CU03)
 (N'V16', N'32234567', N'PC Hogar',     '2026-09-29T09:50:00', 0, NULL, NULL, NULL,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
 (N'V17', N'41901234', N'PC Gamer Entry', '2026-09-29T18:30:00', 0, NULL, NULL, NULL,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

-- Componentes de las ventas configurables
CREATE TABLE #VentaComp (Clave NVARCHAR(10), Codigo NVARCHAR(50), PRIMARY KEY (Clave, Codigo));
INSERT INTO #VentaComp VALUES
 -- V03 (entregada): armada con componentes que ya tenías
 (N'V03', N'CPU-002'), (N'V03', N'MB-002'), (N'V03', N'RAM-002'), (N'V03', N'SSD-001'),
 (N'V03', N'GPU-001'), (N'V03', N'PSU-001'), (N'V03', N'GAB-001'), (N'V03', N'COOL-001'),
 -- V10 (activa)
 (N'V10', N'CPU-004'), (N'V10', N'MB-004'), (N'V10', N'RAM-003'), (N'V10', N'SSD-002'),
 (N'V10', N'GPU-004'), (N'V10', N'PSU-002'), (N'V10', N'GAB-002'), (N'V10', N'COOL-003'),
 -- V15 (activa)
 (N'V15', N'CPU-005'), (N'V15', N'MB-005'), (N'V15', N'RAM-004'), (N'V15', N'SSD-002'),
 (N'V15', N'GPU-002'), (N'V15', N'PSU-002'), (N'V15', N'GAB-002');

/* ---------------------------------------------------------------------
   1. Controles previos: no pisar nada existente
   --------------------------------------------------------------------- */
DECLARE @Choques NVARCHAR(2000) = N'';

SELECT @Choques += N' Cliente ' + c.Dni + N';' FROM #Cliente c WHERE EXISTS (SELECT 1 FROM Clientes x WHERE x.Dni = c.Dni);
SELECT @Choques += N' Componente ' + c.Codigo + N';' FROM #Comp c WHERE EXISTS (SELECT 1 FROM Componentes x WHERE x.Codigo = c.Codigo);
SELECT @Choques += N' Modelo ' + m.Nombre + N';' FROM #Modelo m WHERE EXISTS (SELECT 1 FROM ModelosEstandar x WHERE x.Nombre = m.Nombre);
SELECT @Choques += N' Rol ' + r.Id + N';' FROM (VALUES (N'Recepcionista'), (N'GerenteProduccion'), (N'ResponsableTecnico'), (N'Repositor'), (N'GerenteCompras')) r(Id)
       WHERE EXISTS (SELECT 1 FROM Roles x WHERE x.Id = r.Id);
SELECT @Choques += N' Usuario ' + u.Dni + N';' FROM (VALUES (N'31555111'), (N'32555222'), (N'33555333'), (N'34555444'), (N'29555555'), (N'28555666')) u(Dni)
       WHERE EXISTS (SELECT 1 FROM Usuarios x WHERE x.Dni = u.Dni);
SELECT @Choques += N' Login ' + u.L + N';' FROM (VALUES (N'tpereyra'), (N'tortiz'), (N'bherrera'), (N'jacosta'), (N'lalegre'), (N'smedina')) u(L)
       WHERE EXISTS (SELECT 1 FROM Usuarios x WHERE x.Login = u.L);
SELECT @Choques += N' Proveedor ' + p.Cuit + N';' FROM (VALUES (N'30-55555555-5'), (N'30-66666666-6'), (N'30-77777777-7')) p(Cuit)
       WHERE EXISTS (SELECT 1 FROM Proveedores x WHERE x.Cuit = p.Cuit);
SELECT @Choques += N' Línea ' + l.N + N';' FROM (VALUES (N'Línea D'), (N'Línea E'), (N'Línea F')) l(N)
       WHERE EXISTS (SELECT 1 FROM LineasEnsamblaje x WHERE x.Nombre = l.N);

IF @Choques <> N''
BEGIN
    RAISERROR(N'No se cargó nada: el script ya se ejecutó o hay datos que chocan con los existentes:%s', 16, 1, @Choques);
    RETURN;
END

-- Lo que el script necesita que YA exista (datos actuales, solo se leen)
DECLARE @Faltan NVARCHAR(2000) = N'';
SELECT @Faltan += N' Cliente ' + v.Dni + N';' FROM (SELECT DISTINCT Dni FROM #Venta) v
       WHERE NOT EXISTS (SELECT 1 FROM #Cliente c WHERE c.Dni = v.Dni) AND NOT EXISTS (SELECT 1 FROM Clientes c WHERE c.Dni = v.Dni);
SELECT @Faltan += N' Modelo ' + v.Modelo + N';' FROM (SELECT DISTINCT Modelo FROM #Venta WHERE Modelo IS NOT NULL) v
       WHERE NOT EXISTS (SELECT 1 FROM #Modelo m WHERE m.Nombre = v.Modelo) AND NOT EXISTS (SELECT 1 FROM ModelosEstandar m WHERE m.Nombre = v.Modelo);
SELECT @Faltan += N' Componente ' + vc.Codigo + N';' FROM (SELECT DISTINCT Codigo FROM #VentaComp) vc
       WHERE NOT EXISTS (SELECT 1 FROM #Comp c WHERE c.Codigo = vc.Codigo) AND NOT EXISTS (SELECT 1 FROM Componentes c WHERE c.Codigo = vc.Codigo);
SELECT @Faltan += N' Línea ' + l.N + N';' FROM (VALUES (N'Línea A'), (N'Línea B'), (N'Línea C')) l(N)
       WHERE NOT EXISTS (SELECT 1 FROM LineasEnsamblaje x WHERE x.Nombre = l.N);
SELECT @Faltan += N' Proveedor ' + p.Cuit + N';' FROM (VALUES (N'30-11111111-1'), (N'30-22222222-2'), (N'30-33333333-3'), (N'30-44444444-4')) p(Cuit)
       WHERE NOT EXISTS (SELECT 1 FROM Proveedores x WHERE x.Cuit = p.Cuit);
SELECT @Faltan += N' Patente ' + p.Id + N';' FROM (VALUES (N'GestionarClientes'), (N'GestionarVentas'), (N'GestionarEntregas'), (N'GestionarProduccion'),
       (N'GestionarLineasEnsamblaje'), (N'GestionarModelosEstandar'), (N'VerReporteVentas'), (N'GestionarComponentes'), (N'GestionarInsumos'),
       (N'RegistrarOrdenCompra'), (N'GestionarCompras'), (N'GestionarProveedores'), (N'GestionarCotizaciones'), (N'AprobarCotizacion')) p(Id)
       WHERE NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = N'Administrador') SET @Faltan += N' Rol Administrador;';

IF @Faltan <> N''
BEGIN
    RAISERROR(N'No se cargó nada: faltan datos base que el script espera encontrar:%s', 16, 1, @Faltan);
    RETURN;
END

/* ---------------------------------------------------------------------
   2. Reservas: las ventas activas deben usar SOLO componentes nuevos
   --------------------------------------------------------------------- */
-- Activa = sin seña, señada, o en producción con la orden todavía abierta
-- (Pendiente, Planificada, EnEnsamblaje, EnRevisión). Al cerrar la orden (CU06)
-- la reserva se consume; al anular la venta, se libera.
CREATE TABLE #Reserva (Codigo NVARCHAR(50) PRIMARY KEY, Cantidad INT);

;WITH Activas AS (
    SELECT Clave, Modelo FROM #Venta
    WHERE EstadoVenta IN (0, 1) OR (EstadoVenta = 2 AND OpEstado IN (0, 1, 2, 5))
), CompActivos AS (
    SELECT mc.Codigo FROM Activas a JOIN #ModeloComp mc ON mc.Modelo = a.Modelo
    UNION ALL
    SELECT vc.Codigo FROM Activas a JOIN #VentaComp vc ON vc.Clave = a.Clave WHERE a.Modelo IS NULL
)
INSERT INTO #Reserva (Codigo, Cantidad)
SELECT Codigo, COUNT(*) FROM CompActivos GROUP BY Codigo;

IF EXISTS (SELECT 1 FROM #Venta
           WHERE (EstadoVenta IN (0, 1) OR (EstadoVenta = 2 AND OpEstado IN (0, 1, 2, 5)))
             AND Modelo IS NOT NULL AND Modelo NOT IN (SELECT Nombre FROM #Modelo))
   OR EXISTS (SELECT 1 FROM #Reserva r WHERE NOT EXISTS (SELECT 1 FROM #Comp c WHERE c.Codigo = r.Codigo))
BEGIN
    RAISERROR(N'Una venta activa usa componentes existentes: eso obligaría a modificar su StockReservado. No se cargó nada.', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM #Reserva r JOIN #Comp c ON c.Codigo = r.Codigo WHERE r.Cantidad > c.Stock OR c.Baja = 1)
BEGIN
    RAISERROR(N'Hay reservas que superan el stock de algún componente nuevo. No se cargó nada.', 16, 1);
    RETURN;
END

/* ---------------------------------------------------------------------
   3. Carga
   --------------------------------------------------------------------- */
BEGIN TRY
BEGIN TRANSACTION;

DECLARE @Clave_Hash NVARCHAR(255) = N'5KxDVgaO51rzm4XHlkufZS1Xz1qyhhiCSiCZP83tf68=';   -- SHA-256 (Base64) de "Pcforge2026!"

-- 3.1 Roles y patentes (actores del TD) ---------------------------------------
INSERT INTO Roles (Id, Descripcion, Codigo) VALUES
 (N'Recepcionista',      N'Recepcionista',          N'REC'),
 (N'GerenteProduccion',  N'Gerente de producción',  N'GPR'),
 (N'ResponsableTecnico', N'Responsable técnico',    N'TEC'),
 (N'Repositor',          N'Repositor',              N'REP'),
 (N'GerenteCompras',     N'Gerente de compras',     N'GCO');

INSERT INTO RolPatentes (IdRol, IdPatente) VALUES
 (N'Recepcionista', N'GestionarClientes'), (N'Recepcionista', N'GestionarVentas'), (N'Recepcionista', N'GestionarEntregas'),
 (N'GerenteProduccion', N'GestionarProduccion'), (N'GerenteProduccion', N'GestionarLineasEnsamblaje'),
 (N'GerenteProduccion', N'GestionarModelosEstandar'), (N'GerenteProduccion', N'VerReporteVentas'),
 (N'ResponsableTecnico', N'GestionarProduccion'),
 (N'Repositor', N'GestionarComponentes'), (N'Repositor', N'GestionarInsumos'), (N'Repositor', N'RegistrarOrdenCompra'),
 (N'Repositor', N'GestionarCompras'), (N'Repositor', N'GestionarProveedores'), (N'Repositor', N'GestionarCotizaciones'),
 (N'GerenteCompras', N'GestionarCompras'), (N'GerenteCompras', N'GestionarCotizaciones'),
 (N'GerenteCompras', N'AprobarCotizacion'), (N'GerenteCompras', N'GestionarProveedores');

-- 3.2 Usuarios (contraseña Pcforge2026!) --------------------------------------
-- El Email va cifrado igual que lo guarda la app (AES de EncriptacionSER06AV):
-- <login>@pcfactory.com.ar
INSERT INTO Usuarios (Dni, Nombre, Apellido, Email, IdRol, Activo, Bloqueado, Login, Contrasenia, DebeCambiarContrasenia, Idioma) VALUES
 (N'31555111', N'Tomás',   N'Pereyra', N'W1xL40BGdxBGjxmQvryN2SNZWcOLUERHYkwKN2QlcmQ=', N'Recepcionista',      1, 0, N'tpereyra', @Clave_Hash, 0, N'es'),
 (N'32555222', N'Thiago',  N'Ortiz',   N'jOq3SAR4PzFiuGpPn2TBArTafmkPf2I3Kf9qUdHj0jc=',   N'GerenteProduccion',  1, 0, N'tortiz',   @Clave_Hash, 0, N'es'),
 (N'33555333', N'Bruno',   N'Herrera', N'3CArCkkz9qBeDMifLQw3xe9XZx7nN8ulrYlZjlitIG0=', N'ResponsableTecnico', 1, 0, N'bherrera', @Clave_Hash, 0, N'es'),
 (N'34555444', N'Julieta', N'Acosta',  N'ON3gq94rTgmlCsx4O/VnDzwNahmjMMKhVKxEAdStj2s=',  N'Repositor',          1, 0, N'jacosta',  @Clave_Hash, 0, N'es'),
 (N'29555555', N'Lautaro', N'Alegre',  N'X+wjnpHjR1EzUQEwB48JgsHDUc6X07GrUdL2bnb5vjY=',  N'GerenteCompras',     1, 0, N'lalegre',  @Clave_Hash, 0, N'es'),
 (N'28555666', N'Sofía',   N'Medina',  N'LoGyYEGwQffczcYiF3bIM96vWc+htIvTQzghLuiOLAU=',  N'Administrador',      1, 0, N'smedina',  @Clave_Hash, 0, N'es');

DECLARE @Recepcionista NVARCHAR(150) = N'Tomás Pereyra',
        @Tecnico       NVARCHAR(150) = N'Bruno Herrera',
        @DniRepositor  NVARCHAR(20)  = N'34555444',
        @DniGerenteCom NVARCHAR(20)  = N'29555555';

-- 3.3 Proveedores ----------------------------------------------------------------
INSERT INTO Proveedores (Nombre, Cuit, Email, Telefono, Direccion) VALUES
 (N'Mayorista Andes SA',     N'30-55555555-5', N'ventas@mayoristaandes.com.ar', N'0261-420-5500', N'Av. Las Heras 480, Mendoza'),
 (N'Hardware Patagonia SRL', N'30-66666666-6', N'pedidos@hwpatagonia.com.ar',   N'0299-448-6600', N'Av. Argentina 950, Neuquén'),
 (N'Nexo Informática SA',    N'30-77777777-7', N'comercial@nexoinfo.com.ar',    N'011-4300-7700', N'Av. Belgrano 1650, CABA');

DECLARE @PInsumosSur INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-11111111-1'),
        @PTecnoParts INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-22222222-2'),
        @PDistNorte  INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-33333333-3'),
        @PCompYA     INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-44444444-4'),
        @PAndes      INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-55555555-5'),
        @PPatagonia  INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-66666666-6'),
        @PNexo       INT = (SELECT Id FROM Proveedores WHERE Cuit = N'30-77777777-7');

-- 3.4 Líneas de ensamblaje (ocupadas por órdenes en curso: Disponible = 0) -------
INSERT INTO LineasEnsamblaje (Nombre, Descripcion, Disponible) VALUES
 (N'Línea D', N'Equipos gamer de alta gama',   0),
 (N'Línea E', N'Equipos hogar y estudio',      0),
 (N'Línea F', N'Armado express',               0);

-- 3.5 Clientes -------------------------------------------------------------------
INSERT INTO Clientes (Dni, Nombre, Apellido, Telefono, Direccion)
SELECT Dni, Nombre, Apellido, Telefono, Direccion FROM #Cliente;

-- 3.6 Componentes (con la reserva de las ventas activas) --------------------------
INSERT INTO Componentes (Codigo, Descripcion, Tipo, Marca, Modelo, PrecioUnitario, Stock, StockMinimo, StockReservado, Bit_Lo_Bo)
SELECT c.Codigo, c.Descripcion, c.Tipo, c.Marca, c.Modelo, c.Precio, c.Stock, c.StockMinimo,
       ISNULL(r.Cantidad, 0), c.Baja
FROM   #Comp c LEFT JOIN #Reserva r ON r.Codigo = c.Codigo;

-- Historial (Componentes_C): si el trigger de la base ya asentó la versión, no se
-- duplica; si no hay trigger, se deja la versión vigente (Act = 1) igual.
INSERT INTO Componentes_C (CodigoComponente, Fecha, Hora, Descripcion, Tipo, Marca, Modelo,
                           PrecioUnitario, Stock, StockMinimo, Bit_Lo_Bo, Act)
SELECT c.Codigo, '2026-09-15', '09:00', c.Descripcion, c.Tipo, c.Marca, c.Modelo,
       c.PrecioUnitario, c.Stock, c.StockMinimo, c.Bit_Lo_Bo, 1
FROM   Componentes c
WHERE  c.Codigo IN (SELECT Codigo FROM #Comp)
  AND  NOT EXISTS (SELECT 1 FROM Componentes_C h WHERE h.CodigoComponente = c.Codigo);

-- 3.7 Modelos estándar -------------------------------------------------------------
INSERT INTO ModelosEstandar (Nombre, Descripcion)
SELECT Nombre, Descripcion FROM #Modelo;

INSERT INTO ModeloEstandarComponentes (IdModelo, CodigoComponente)
SELECT m.Id, mc.Codigo
FROM   #ModeloComp mc JOIN ModelosEstandar m ON m.Nombre = mc.Modelo;

-- 3.8 Ventas, computadoras, pagos y órdenes de producción ---------------------------
DECLARE @Lista TABLE (Codigo NVARCHAR(50) PRIMARY KEY);
DECLARE @Clave NVARCHAR(10), @Dni NVARCHAR(20), @Modelo NVARCHAR(150), @FechaVenta DATETIME, @EstadoVenta INT,
        @SenaFecha DATETIME, @SenaForma INT, @SenaRef NVARCHAR(100),
        @OpFecha DATETIME, @OpEstado INT, @Linea NVARCHAR(100), @Inicio DATE,
        @CcFecha DATETIME, @CcDrivers BIT, @CcObs NVARCHAR(500),
        @SaldoFecha DATETIME, @SaldoForma INT, @SaldoRef NVARCHAR(100),
        @IdModelo INT, @Total DECIMAL(12,2), @Sena DECIMAL(12,2), @IdPc INT, @NumVenta INT,
        @NumOrden INT, @IdLinea INT, @IdPago INT, @Entrega DATE, @Cierre DATETIME;

DECLARE cVentas CURSOR LOCAL FAST_FORWARD FOR
    SELECT Clave FROM #Venta ORDER BY FechaVenta;
OPEN cVentas;
FETCH NEXT FROM cVentas INTO @Clave;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @Dni = Dni, @Modelo = Modelo, @FechaVenta = FechaVenta, @EstadoVenta = EstadoVenta,
           @SenaFecha = SenaFecha, @SenaForma = SenaForma, @SenaRef = SenaRef,
           @OpFecha = OpFecha, @OpEstado = OpEstado, @Linea = Linea, @Inicio = InicioPrevisto,
           @CcFecha = CcFecha, @CcDrivers = CcDrivers, @CcObs = CcObs,
           @SaldoFecha = SaldoFecha, @SaldoForma = SaldoForma, @SaldoRef = SaldoRef
    FROM   #Venta WHERE Clave = @Clave;

    -- Componentes de la computadora (modelo estándar o configurable)
    DELETE FROM @Lista;
    SET @IdModelo = NULL;
    IF @Modelo IS NOT NULL
    BEGIN
        SELECT @IdModelo = MIN(Id) FROM ModelosEstandar WHERE Nombre = @Modelo;
        INSERT INTO @Lista (Codigo) SELECT CodigoComponente FROM ModeloEstandarComponentes WHERE IdModelo = @IdModelo;
    END
    ELSE
        INSERT INTO @Lista (Codigo) SELECT Codigo FROM #VentaComp WHERE Clave = @Clave;

    SELECT @Total = SUM(c.PrecioUnitario) FROM @Lista l JOIN Componentes c ON c.Codigo = l.Codigo;
    SET @Sena    = ROUND(@Total * 0.5, 2);
    SET @Entrega = CAST(DATEADD(DAY, 15, @FechaVenta) AS DATE);

    INSERT INTO Computadoras (Nombre, TipoConfiguracion, PrecioTotal, IdModeloOrigen)
    VALUES (ISNULL(@Modelo, N'Configurable'), CASE WHEN @Modelo IS NULL THEN 1 ELSE 0 END, @Total, @IdModelo);
    SET @IdPc = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO ComputadoraComponentes (IdComputadora, CodigoComponente, Cantidad)
    SELECT @IdPc, Codigo, 1 FROM @Lista;

    INSERT INTO Ventas (DniCliente, IdComputadora, FechaVenta, FechaEntregaEstimada, Estado, UsuarioRegistro)
    VALUES (@Dni, @IdPc, @FechaVenta, @Entrega, @EstadoVenta, @Recepcionista);
    SET @NumVenta = CAST(SCOPE_IDENTITY() AS INT);

    -- Seña (CU "Registrar seña y emitir recibo")
    IF @SenaFecha IS NOT NULL
    BEGIN
        INSERT INTO Pagos (NumeroVenta, Tipo, NumeroRecibo, Monto, FormaPago, Referencia, Fecha, Usuario)
        VALUES (@NumVenta, 0, NULL, @Sena, @SenaForma, ISNULL(@SenaRef, N''), @SenaFecha, @Recepcionista);
        SET @IdPago = CAST(SCOPE_IDENTITY() AS INT);
        UPDATE Pagos SET NumeroRecibo = N'REC-' + RIGHT(N'00000000' + CAST(@IdPago AS NVARCHAR(10)), 8) WHERE Id = @IdPago;
    END

    -- Orden de producción
    IF @OpFecha IS NOT NULL
    BEGIN
        SET @IdLinea = (SELECT MIN(Id) FROM LineasEnsamblaje WHERE Nombre = @Linea);
        SET @Cierre  = CASE WHEN @OpEstado IN (3, 4) THEN DATEADD(MINUTE, 2, @CcFecha) END;

        INSERT INTO OrdenesProduccion (NumeroVenta, FechaRegistro, FechaEntregaEstimada, Estado, IdLinea, FechaInicioPrevista,
                                       ResponsableTecnico, NumeroSerie, FechaCierre,
                                       CcEncendido, CcConexiones, CcSistemaOperativo, CcDrivers,
                                       CcObservaciones, CcFecha, CcResponsable)
        VALUES (@NumVenta, @OpFecha, @Entrega, @OpEstado, @IdLinea, @Inicio,
                CASE WHEN @IdLinea IS NULL THEN NULL ELSE @Tecnico END, NULL, @Cierre,
                CASE WHEN @CcFecha IS NULL THEN 0 ELSE 1 END,
                CASE WHEN @CcFecha IS NULL THEN 0 ELSE 1 END,
                CASE WHEN @CcFecha IS NULL THEN 0 ELSE 1 END,
                CASE WHEN @CcFecha IS NULL THEN 0 ELSE ISNULL(@CcDrivers, 1) END,
                CASE WHEN @CcFecha IS NULL THEN NULL ELSE ISNULL(@CcObs, N'') END,
                @CcFecha,
                CASE WHEN @CcFecha IS NULL THEN NULL ELSE @Tecnico END);
        SET @NumOrden = CAST(SCOPE_IDENTITY() AS INT);

        IF @OpEstado IN (3, 4)
            UPDATE OrdenesProduccion
            SET    NumeroSerie = N'SN-' + CAST(YEAR(@Cierre) AS NVARCHAR(4)) + N'-' + RIGHT(N'00000' + CAST(@NumOrden AS NVARCHAR(10)), 5)
            WHERE  NumeroOrden = @NumOrden;
    END

    -- Saldo final y factura (CU "Entregar computadora")
    IF @SaldoFecha IS NOT NULL
    BEGIN
        INSERT INTO Pagos (NumeroVenta, Tipo, NumeroRecibo, Monto, FormaPago, Referencia, Fecha, Usuario)
        VALUES (@NumVenta, 1, NULL, @Total - @Sena, @SaldoForma, ISNULL(@SaldoRef, N''), @SaldoFecha, @Recepcionista);
        SET @IdPago = CAST(SCOPE_IDENTITY() AS INT);
        UPDATE Pagos SET NumeroRecibo = N'FAC-' + RIGHT(N'00000000' + CAST(@IdPago AS NVARCHAR(10)), 8) WHERE Id = @IdPago;
    END

    FETCH NEXT FROM cVentas INTO @Clave;
END

CLOSE cVentas;
DEALLOCATE cVentas;

-- 3.9 Compras de insumos (RFN2) ------------------------------------------------------
-- EstadoOrdenCompra: 0 Pendiente, 1 Enviada, 2 Finalizada, 3 RecibidaParcial
-- EstadoCotizacion:  0 PorAprobar, 1 Aprobado, 2 Desaprobada
-- Un componente no está en dos órdenes abiertas a la vez; un proveedor, una oferta por orden.
DECLARE @OC NVARCHAR(30), @CO NVARCHAR(30), @FC NVARCHAR(30);

-- OC histórica, recibida completa (16/09 → 19/09)
SET @OC = N'OC-20260916090012347';
INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado, FechaCierre)
VALUES (@OC, '2026-09-20', @DniRepositor, 2, '2026-09-19T14:20:00');
INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad) VALUES (@OC, N'RAM-003', 10), (@OC, N'SSD-002', 15);

SET @CO = N'CO-20260916103015220';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PAndes, 1, 1880.00, N'Entrega en 72 h. Pago a 30 días.', '2026-09-16T10:30:15', @DniGerenteCom);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'RAM-003', 10, 128.00), (@CO, N'SSD-002', 15, 40.00);

SET @CO = N'CO-20260916110508731';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PTecnoParts, 2, 1980.00, N'Entrega en 5 días hábiles. Pago contado.', '2026-09-16T11:05:08', @DniGerenteCom);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'RAM-003', 10, 135.00), (@CO, N'SSD-002', 15, 42.00);

SET @FC = N'FC-20260919142004118';
INSERT INTO FacturaCompra (NumeroFactura, IdOrdenCompra, FechaEmision, FechaEntrega, Total, Observaciones)
VALUES (@FC, @OC, '2026-09-19T14:20:04', '2026-09-19T00:00:00', 1880.00, N'Entrega completa.');
INSERT INTO FacturaCompraDetalle (NumeroFactura, CodigoComponente, Cantidad) VALUES (@FC, N'RAM-003', 10), (@FC, N'SSD-002', 15);

-- OC recibida en parte (llegaron los coolers, faltan las placas de video)
SET @OC = N'OC-20260922100044512';
INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado, FechaCierre)
VALUES (@OC, '2026-10-02', @DniRepositor, 3, NULL);
INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad) VALUES (@OC, N'COOL-002', 6), (@OC, N'GPU-003', 4);

SET @CO = N'CO-20260922120031906';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PCompYA, 1, 3048.00, N'Las placas de video se entregan en una segunda tanda.', '2026-09-22T12:00:31', @DniGerenteCom);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'COOL-002', 6, 118.00), (@CO, N'GPU-003', 4, 585.00);

SET @FC = N'FC-20260927164512077';
INSERT INTO FacturaCompra (NumeroFactura, IdOrdenCompra, FechaEmision, FechaEntrega, Total, Observaciones)
VALUES (@FC, @OC, '2026-09-27T16:45:12', '2026-09-27T00:00:00', 708.00, N'Entrega parcial: las RTX 4070 Super llegan la semana próxima.');
INSERT INTO FacturaCompraDetalle (NumeroFactura, CodigoComponente, Cantidad) VALUES (@FC, N'COOL-002', 6);

-- OC adjudicada y enviada al proveedor (espera recepción: entrada del CU12)
SET @OC = N'OC-20260925091533640';
INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado, FechaCierre)
VALUES (@OC, '2026-10-05', @DniRepositor, 1, NULL);
INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad) VALUES (@OC, N'PSU-003', 12), (@OC, N'GAB-003', 10);

SET @CO = N'CO-20260925110002415';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PInsumosSur, 1, 920.00, N'Entrega en 4 días hábiles. Pago a 15 días.', '2026-09-25T11:00:02', @DniGerenteCom);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'PSU-003', 12, 40.00), (@CO, N'GAB-003', 10, 44.00);

SET @CO = N'CO-20260925114027803';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PDistNorte, 2, 982.00, N'Entrega en 7 días. Flete a cargo del comprador.', '2026-09-25T11:40:27', @DniGerenteCom);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'PSU-003', 12, 43.50), (@CO, N'GAB-003', 10, 46.00);

-- OC pendiente con dos ofertas por aprobar (entrada del CU11)
SET @OC = N'OC-20260929084510933';
INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado, FechaCierre)
VALUES (@OC, '2026-10-10', @DniRepositor, 0, NULL);
INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad) VALUES (@OC, N'SSD-003', 10), (@OC, N'GPU-004', 6);

SET @CO = N'CO-20260929121044150';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PPatagonia, 0, 2952.00, N'Entrega en 5 días hábiles. Pago a 30 días.', '2026-09-29T12:10:44', NULL);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'SSD-003', 10, 138.00), (@CO, N'GPU-004', 6, 262.00);

SET @CO = N'CO-20260929153009281';
INSERT INTO PedidosCotizacion (Numero, IdOrdenCompra, IdProveedor, Estado, Costo, Condiciones, FechaEmision, DniGerenteAprobador)
VALUES (@CO, @OC, @PNexo, 0, 2958.00, N'Entrega en 48 h en CABA y GBA. Pago contado con 3% de descuento.', '2026-09-29T15:30:09', NULL);
INSERT INTO PedidoCotizacionDetalle (NumeroCotizacion, CodigoComponente, Cantidad, PrecioUnitario)
VALUES (@CO, N'SSD-003', 10, 135.00), (@CO, N'GPU-004', 6, 268.00);

-- OC recién registrada, todavía sin cotizaciones (entrada del CU10)
SET @OC = N'OC-20260929180521764';
INSERT INTO OrdenesCompra (Id, FechaLimite, DniRepositor, Estado, FechaCierre)
VALUES (@OC, '2026-10-08', @DniRepositor, 0, NULL);
INSERT INTO OrdenCompraDetalle (IdOrdenCompra, CodigoComponente, Cantidad) VALUES (@OC, N'HDD-002', 10);

-- 3.10 Dígito verificador: se vacía para que el sistema recalcule la línea base -----
DELETE FROM DV;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE(), @Linea_Err INT = ERROR_LINE();
    RAISERROR(N'Se deshizo toda la carga. Error en la línea %d: %s', 16, 1, @Linea_Err, @Err);
    RETURN;
END CATCH

/* ---------------------------------------------------------------------
   4. Resumen
   --------------------------------------------------------------------- */
PRINT N'Datos de prueba cargados. Iniciá sesión en PCFORGE para que se recalcule el dígito verificador.';

SELECT v.NumeroVenta, v.FechaVenta, c.Apellido + N', ' + c.Nombre AS Cliente, pc.Nombre AS Equipo, pc.PrecioTotal,
       CASE v.Estado WHEN 0 THEN N'Pendiente' WHEN 1 THEN N'Señada' WHEN 2 THEN N'En producción'
                     WHEN 3 THEN N'Entregada' ELSE N'Anulada' END AS EstadoVenta,
       op.NumeroOrden,
       CASE op.Estado WHEN 0 THEN N'Pendiente' WHEN 1 THEN N'Planificada' WHEN 2 THEN N'En ensamblaje'
                      WHEN 3 THEN N'Finalizada' WHEN 4 THEN N'Entregada' WHEN 5 THEN N'En revisión' END AS EstadoOrden,
       l.Nombre AS Linea, op.NumeroSerie
FROM   Ventas v
       JOIN Clientes c ON c.Dni = v.DniCliente
       JOIN Computadoras pc ON pc.Id = v.IdComputadora
       LEFT JOIN OrdenesProduccion op ON op.NumeroVenta = v.NumeroVenta
       LEFT JOIN LineasEnsamblaje l ON l.Id = op.IdLinea
ORDER  BY v.NumeroVenta;

SELECT Codigo, Descripcion, Stock, StockMinimo, StockReservado, Stock - StockReservado AS Libre,
       CASE WHEN Bit_Lo_Bo = 1 THEN N'Baja' WHEN Stock <= StockMinimo THEN N'Reponer' ELSE N'' END AS Aviso
FROM   Componentes ORDER BY Tipo, Codigo;

SELECT oc.NumeroCompra, oc.Id,
       CASE oc.Estado WHEN 0 THEN N'Pendiente' WHEN 1 THEN N'Enviada' WHEN 2 THEN N'Finalizada' ELSE N'Recibida parcial' END AS Estado,
       (SELECT COUNT(*) FROM PedidosCotizacion p WHERE p.IdOrdenCompra = oc.Id) AS Cotizaciones
FROM   OrdenesCompra oc ORDER BY oc.NumeroCompra;

SELECT Login, Nombre + N' ' + Apellido AS Usuario, IdRol FROM Usuarios ORDER BY IdRol, Login;
