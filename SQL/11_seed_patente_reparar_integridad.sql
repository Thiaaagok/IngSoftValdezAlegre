-- ============================================================
--  11_seed_patente_reparar_integridad.sql
--
--  Crea la patente 'RepararIntegridad' y la asigna al rol
--  Administrador (a través de la familia 'Administracion'), para
--  que el administrador pueda acceder al GUI de Reparación del
--  Dígito Verificador cuando se detecta una inconsistencia.
--
--  El Id de la patente debe coincidir EXACTAMENTE con el valor del
--  enum SER.PatenteEnum06AV.RepararIntegridad ("RepararIntegridad").
--
--  Idempotente: si ya existen, no hace nada.
-- ============================================================

SET NOCOUNT ON;

-- 1) La patente en sí.
IF NOT EXISTS (SELECT 1 FROM Patentes WHERE Id = 'RepararIntegridad')
    INSERT INTO Patentes (Id, Descripcion)
    VALUES ('RepararIntegridad', 'Reparar integridad (Dígito Verificador)');

-- 2) Asignación a la familia Administracion (que ya tiene el rol Administrador).
--    Se asigna SOLO por familia (no también directo al rol) para no duplicar la
--    patente: el rol la hereda de la familia, y una asignación directa adicional
--    haría que el árbol del rol la tenga dos veces ("ya está contenida en este rol").
IF EXISTS (SELECT 1 FROM Familias WHERE Id = 'Administracion')
   AND NOT EXISTS (SELECT 1 FROM FamiliaPatentes
                   WHERE IdFamilia = 'Administracion' AND IdPatente = 'RepararIntegridad')
    INSERT INTO FamiliaPatentes (IdFamilia, IdPatente)
    VALUES ('Administracion', 'RepararIntegridad');

PRINT 'Patente RepararIntegridad creada y asignada al rol Administrador (via familia Administracion).';
GO
