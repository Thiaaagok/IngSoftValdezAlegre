-- ============================================================
--  Tabla DV: almacena el Dígito Verificador Horizontal (DVH) y
--  Vertical (DVV) de cada tabla protegida de la base.
--
--  El DVH/DVV de la BASE DE DATOS es la suma de los dígitos de
--  todas las filas de esta tabla (se calcula en memoria, no se
--  guarda una fila aparte).
--
--  La aplicación también crea esta tabla automáticamente si no
--  existe (IntegridadDAL06AV.AsegurarEstructura), este script es
--  para poder inicializarla manualmente.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DV')
BEGIN
    CREATE TABLE DV (
        Tabla NVARCHAR(128) NOT NULL PRIMARY KEY,
        DVH   NVARCHAR(64)  NOT NULL,
        DVV   NVARCHAR(64)  NOT NULL
    );
END
GO
