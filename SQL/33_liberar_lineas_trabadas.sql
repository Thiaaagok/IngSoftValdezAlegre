-- ============================================================
--  33_liberar_lineas_trabadas.sql
--  CORRECCIÓN DE DATOS: líneas de ensamblaje ocupadas para siempre.
--
--  Qué pasaba: la línea se marcaba Disponible = 0 al planificar una orden,
--  pero sólo se volvía a marcar Disponible = 1 si la orden se desplanificaba.
--  Al finalizar una orden la línea quedaba tomada, y con todas las líneas
--  tomadas la pantalla de "Asignar línea" no ofrecía ninguna: era imposible
--  planificar una orden nueva.
--
--  El código ya se corrigió (OrdenProduccionBLL06AV.LiberarLinea se llama al
--  cerrar la orden). Este script arregla los datos que ya quedaron mal.
--
--  Criterio: una línea está realmente ocupada sólo si tiene alguna orden en
--  estado Planificada (1), En ensamblaje (2) o En revisión (5). Cualquier otra
--  línea vuelve al pool de disponibles.
--
--  Es idempotente: se puede correr todas las veces que haga falta.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Antes INT =
        (SELECT COUNT(*) FROM LineasEnsamblaje WHERE Disponible = 0);

    UPDATE l
    SET    l.Disponible = 1
    FROM   LineasEnsamblaje l
    WHERE  l.Disponible = 0
      AND  NOT EXISTS (
               SELECT 1
               FROM   OrdenesProduccion op
               WHERE  op.IdLinea = l.Id
                 AND  op.Estado IN (1, 2, 5)   -- Planificada, EnEnsamblaje, EnRevision
           );

    DECLARE @Liberadas INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT 'Liberación de líneas completada.';
    PRINT '  Estaban ocupadas: ' + CAST(@Antes AS VARCHAR(10));
    PRINT '  Liberadas:        ' + CAST(@Liberadas AS VARCHAR(10));

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error liberando líneas: ' + ERROR_MESSAGE();
END CATCH
GO

-- ── Verificación: estado real de cada línea ─────────────────
SELECT l.Id,
       l.Nombre,
       CASE WHEN l.Disponible = 1 THEN 'Disponible' ELSE 'Ocupada' END AS Estado,
       (SELECT COUNT(*) FROM OrdenesProduccion op
        WHERE op.IdLinea = l.Id AND op.Estado IN (1, 2, 5))            AS OrdenesActivas
FROM   LineasEnsamblaje l
ORDER  BY l.Id;
GO
