-- ============================================================
--  Fix de patentes DIRECTAS duplicadas en un rol.
--
--  Problema: se agregaron patentes directas (RolPatentes) que el
--  rol YA aporta a través de sus familias. El modelo no permite
--  la misma patente dos veces en un rol, así que al cargar el rol
--  la app lanza excepción.
--
--  Este script borra SOLO las patentes directas que ya están
--  cubiertas por las familias del rol (las verdaderas duplicadas),
--  conservando cualquier patente directa legítima.
-- ============================================================

DECLARE @IdRol NVARCHAR(50) = '123456789012345678901234567890';

-- 1) DIAGNÓSTICO: ver qué patentes directas están duplicadas vía familias
;WITH FamiliasDelRol AS (
    SELECT rf.IdFamilia
    FROM RolFamilias rf
    WHERE rf.IdRol = @IdRol
    UNION ALL
    SELECT ff.IdHijo
    FROM FamiliaFamilias ff
    INNER JOIN FamiliasDelRol f ON f.IdFamilia = ff.IdPadre
)
SELECT DISTINCT rp.IdPatente AS PatenteDirectaDuplicada
FROM RolPatentes rp
INNER JOIN FamiliaPatentes fp ON fp.IdPatente = rp.IdPatente
INNER JOIN FamiliasDelRol fr  ON fr.IdFamilia = fp.IdFamilia
WHERE rp.IdRol = @IdRol
OPTION (MAXRECURSION 1000);

-- 2) FIX: borrar esas duplicadas
;WITH FamiliasDelRol AS (
    SELECT rf.IdFamilia
    FROM RolFamilias rf
    WHERE rf.IdRol = @IdRol
    UNION ALL
    SELECT ff.IdHijo
    FROM FamiliaFamilias ff
    INNER JOIN FamiliasDelRol f ON f.IdFamilia = ff.IdPadre
)
DELETE rp
FROM RolPatentes rp
INNER JOIN FamiliaPatentes fp ON fp.IdPatente = rp.IdPatente
INNER JOIN FamiliasDelRol fr  ON fr.IdFamilia = fp.IdFamilia
WHERE rp.IdRol = @IdRol
OPTION (MAXRECURSION 1000);

-- --- Alternativa DRÁSTICA: borrar TODAS las patentes directas del rol ---
-- (útil si el rol recibe todo por familias y no querés patentes directas)
-- DELETE FROM RolPatentes WHERE IdRol = '123456789012345678901234567890';
