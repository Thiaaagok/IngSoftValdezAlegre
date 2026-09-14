-- ============================================================
--  32_sincronizar_patentes.sql
--  SINCRONIZACIÓN COMPLETA DE PATENTES.
--
--  Este script es la red de seguridad del sistema de permisos: contiene
--  TODAS las patentes declaradas en el enum PatenteEnum06AV y se las
--  asigna al rol Administrador.
--
--  Por qué existe: cada vez que se agrega un valor al enum hay que sembrarlo
--  en la base, y si alguien se olvida la pantalla se abre pero la acción
--  falla con "el usuario no tiene el permiso X". Correr este script deja la
--  tabla Patentes alineada con el código, sin importar qué scripts previos
--  se hayan corrido.
--
--  Es idempotente: se puede ejecutar todas las veces que haga falta.
--  No borra ni revoca nada: sólo agrega lo que falta.
--
--  IMPORTANTE: las patentes se cargan en la sesión al iniciar sesión.
--  Después de correr este script hay que CERRAR SESIÓN Y VOLVER A ENTRAR
--  para que el usuario tome los permisos nuevos.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Patentes TABLE (Id NVARCHAR(450), Descripcion NVARCHAR(200));
    INSERT INTO @Patentes (Id, Descripcion) VALUES
        -- Usuarios
        ('VerUsuarios',               'Ver usuarios'),
        ('CrearUsuarios',             'Crear usuarios'),
        ('EditarUsuarios',            'Editar usuarios'),
        ('ActDesactivarUsuarios',     'Activar / desactivar usuarios'),
        ('DesbloquearUsuarios',       'Desbloquear usuarios'),
        -- Bitácora
        ('VerBitacora',               'Ver bitácora'),
        ('ExportarBitacora',          'Exportar bitácora'),
        -- Permisos
        ('GestionarRoles',            'Gestionar roles'),
        ('GestionarFamilias',         'Gestionar familias'),
        ('GestionarPatentes',         'Gestionar patentes'),
        -- Integridad
        ('RepararIntegridad',         'Reparar dígito verificador'),
        -- Dominio PC Factory
        ('GestionarClientes',         'Gestionar clientes'),
        ('GestionarComponentes',      'Gestionar componentes'),
        ('GestionarInsumos',          'Gestionar insumos'),
        ('GestionarProveedores',      'Gestionar proveedores'),
        ('GestionarLineasEnsamblaje', 'Gestionar líneas de ensamblaje'),
        ('GestionarVentas',           'Gestionar ventas'),
        ('GestionarEntregas',         'Entregar computadoras'),
        ('GestionarProduccion',       'Gestionar producción'),
        ('GestionarCompras',          'Gestionar compras'),
        ('GestionarModelosEstandar',  'Gestionar modelos estándar'),
        -- Acciones sensibles del circuito de compras (RFN2)
        ('RegistrarOrdenCompra',      'Registrar una orden de compra'),
        ('AprobarCotizacion',         'Aprobar o desaprobar una cotización');

    -- ── 1. Alta de las patentes que falten ───────────────────
    INSERT INTO Patentes (Id, Descripcion)
    SELECT p.Id, p.Descripcion
    FROM   @Patentes p
    WHERE  NOT EXISTS (SELECT 1 FROM Patentes x WHERE x.Id = p.Id);

    DECLARE @NuevasPatentes INT = @@ROWCOUNT;

    -- ── 2. Asignación directa al rol Administrador ───────────
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

    DECLARE @NuevasAsignaciones INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT 'Sincronización de patentes completada.';
    PRINT '  Patentes creadas:       ' + CAST(@NuevasPatentes AS VARCHAR(10));
    PRINT '  Asignadas al Admin:     ' + CAST(@NuevasAsignaciones AS VARCHAR(10));
    PRINT '  Recordá cerrar sesión y volver a entrar para tomar los permisos nuevos.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error sincronizando patentes: ' + ERROR_MESSAGE();
END CATCH
GO

-- ── Verificación: qué tiene hoy el rol Administrador ────────
SELECT p.Id AS Patente, p.Descripcion,
       CASE WHEN rp.IdPatente IS NULL THEN 'NO' ELSE 'SI' END AS TieneAdmin
FROM   Patentes p 
       LEFT JOIN RolPatentes rp
              ON rp.IdPatente = p.Id
             AND rp.IdRol = (SELECT TOP 1 Id FROM Roles
                             WHERE Descripcion = 'Administrador' OR Codigo = 'ADM')
ORDER BY TieneAdmin, p.Id;
GO
