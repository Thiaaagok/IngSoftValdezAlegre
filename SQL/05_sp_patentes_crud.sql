-- ============================================================
--  ABM de Patentes: altas, modificaciones y bajas.
--
--  Hasta ahora Patentes solo se consumía en modo lectura
--  (sp_Patentes_ObtenerTodos / sp_Patentes_ObtenerPorId). Esta
--  pantalla nueva permite gestionarlas directamente, así que se
--  agregan los 3 procedimientos de escritura que faltaban.
--
--  El Id de una patente debe coincidir EXACTAMENTE con el nombre
--  de un valor del enum PatenteEnum06AV (ver SER/Enums/PatenteEnum06AV.cs),
--  por eso se ingresa a mano en la pantalla (no se genera random como
--  en Roles/Familias) y queda bloqueado para edición una vez creada.
-- ============================================================

CREATE OR ALTER PROCEDURE sp_Patentes_Agregar
    @Id NVARCHAR(100),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Patentes (Id, Descripcion) VALUES (@Id, @Descripcion);
END
GO

CREATE OR ALTER PROCEDURE sp_Patentes_Modificar
    @Id NVARCHAR(100),
    @Descripcion NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Patentes SET Descripcion = @Descripcion WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE sp_Patentes_Eliminar
    @Id NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Patentes WHERE Id = @Id;
END
GO
