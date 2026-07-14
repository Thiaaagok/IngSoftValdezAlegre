-- ============================================================
--  Patente 'RepararIntegridad': habilita el GUI de Reparación
--  del Dígito Verificador cuando la REVISIÓN del Login detecta
--  una inconsistencia de datos.
--
--  IMPORTANTE: el Id debe ser EXACTAMENTE 'RepararIntegridad'
--  porque coincide con PatenteEnum06AV.RepararIntegridad
--  (convención del sistema: Id de la patente == nombre del enum).
-- ============================================================

-- 1) Alta de la patente
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'RepararIntegridad')
    INSERT INTO Patentes (Id, Descripcion)
    VALUES ('RepararIntegridad', 'Reparar integridad de datos (DV)');

-- 2) Asignarla al/los rol(es) que deban poder reparar.
--    Por defecto: todos los roles cuya descripción sea 'Administrador'.
INSERT INTO RolPatentes (IdRol, IdPatente)
SELECT r.Id, 'RepararIntegridad'
FROM Roles r
WHERE r.Descripcion = 'Administrador'
  AND NOT EXISTS (
      SELECT 1 FROM RolPatentes rp
      WHERE rp.IdRol = r.Id AND rp.IdPatente = 'RepararIntegridad'
  );

-- --- Alternativa: asignar a un rol puntual por Id ---
-- INSERT INTO RolPatentes (IdRol, IdPatente)
-- SELECT '123456789012345678901234567890', 'RepararIntegridad'
-- WHERE NOT EXISTS (
--     SELECT 1 FROM RolPatentes
--     WHERE IdRol = '123456789012345678901234567890'
--       AND IdPatente = 'RepararIntegridad');

-- Verificación
SELECT r.Descripcion AS Rol, rp.IdPatente
FROM RolPatentes rp
JOIN Roles r ON r.Id = rp.IdRol
WHERE rp.IdPatente = 'RepararIntegridad';
