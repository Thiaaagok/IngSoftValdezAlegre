using DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public class UsuariosDAL06AV
{
    #region Obtener

    public DataTable ObtenerTodos(Dictionary<string, object> parametros)
    {
        string query = "SELECT * FROM Usuarios";

        SqlConnection conn = Conexion.Instancia.ObtenerConexion();

        SqlCommand cmd = new SqlCommand(query, conn);

        DataTable tabla = new DataTable();

        conn.Open();
        SqlDataAdapter da = new SqlDataAdapter(cmd);
        da.Fill(tabla);
        conn.Close();

        return tabla;
    }

    public DataTable ObtenerPorDni(Dictionary<string, object> parametros)
    {
        string query = "SELECT * FROM Usuarios WHERE Dni = @dni";

        SqlConnection conn = Conexion.Instancia.ObtenerConexion();

        SqlCommand cmd = new SqlCommand(query, conn);

        foreach (var p in parametros)
        {
            cmd.Parameters.AddWithValue( p.Key, p.Value );
        }

        DataTable tabla = new DataTable();

        conn.Open();
        SqlDataAdapter da = new SqlDataAdapter(cmd);
        da.Fill(tabla);
        conn.Close();

        return tabla;
    }

    public DataTable ObtenerPorLogin(Dictionary<string, object> parametros)
    {
        string query = "SELECT * FROM Usuarios WHERE Login = @login";

        SqlConnection conn = Conexion.Instancia.ObtenerConexion();

        SqlCommand cmd = new SqlCommand(query, conn);

        foreach (var p in parametros)
        {
            cmd.Parameters.AddWithValue(p.Key, p.Value);
        }

        DataTable tabla = new DataTable();

        conn.Open();
        SqlDataAdapter da = new SqlDataAdapter(cmd);
        da.Fill(tabla);
        conn.Close();

        return tabla;
    }

    #endregion

    #region Crear

    public bool CrearUsuario(Dictionary<string, object> parametros)
    {
        string query = @"INSERT INTO Usuarios 
        (Dni, Nombre, Apellido, Email, IdRol, Activo, Bloqueado, Login, Contrasenia, DebeCambiarContrasenia)
        VALUES
        (@dni, @nombre, @apellido, @email, @IdRol,
         @activo, @bloqueado, @login, @contrasenia, @debeCambiarContrasenia)";

        return Ejecutar(query, parametros);
    }

    #endregion

    #region Modificar

    public bool EditarUsuario(Dictionary<string, object> parametros)
    {
        string query = @"UPDATE Usuarios SET Email = @email, IdRol = @rol WHERE Dni = @dni";

        return Ejecutar(query, parametros);
    }

    public bool ReactivarUsuario(Dictionary<string, object> parametros)
    {
        string query = "UPDATE Usuarios SET Activo = 1 WHERE Dni = @dni";

        return Ejecutar(query, parametros);
    }

    public bool DesactivarUsuario(Dictionary<string, object> parametros)
    {
        string query = "UPDATE Usuarios SET Activo = 0 WHERE Dni = @dni";

        return Ejecutar(query, parametros);
    }

    public bool BloquearUsuario(Dictionary<string, object> parametros)
    {
        string query = "UPDATE Usuarios SET Bloqueado = 1 WHERE Dni = @dni";

        return Ejecutar(query, parametros);
    }

    public bool DesbloquearUsuario(Dictionary<string, object> parametros)
    {
        string query = @"UPDATE Usuarios 
                     SET Bloqueado = 0, DebeCambiarContrasenia = 1, 
                     Contrasenia = @contrasenia
                     WHERE Dni = @dni";
        return Ejecutar(query, parametros);
    }

    public bool CambiarContraseña(Dictionary<string, object> parametros)
    {
        string query = @"UPDATE Usuarios 
                     SET Contrasenia = @nueva, DebeCambiarContrasenia = 0 
                     WHERE Dni = @dni AND Contrasenia = @actual";
        return Ejecutar(query, parametros);
    }

    #endregion

    #region Eliminar

    public bool EliminarUsuario(Dictionary<string, object> parametros)
    {
        string query = "DELETE FROM Usuarios WHERE Dni = @dni";

        return Ejecutar(query, parametros);
    }

    #endregion

    #region Ejecutar

    private bool Ejecutar( string query, Dictionary<string, object> parametros)
    {
        SqlConnection conn = Conexion.Instancia.ObtenerConexion();

        SqlCommand cmd = new SqlCommand(query, conn);

        foreach (var p in parametros)
        {
            cmd.Parameters.AddWithValue( p.Key, p.Value ?? DBNull.Value );
        }

        conn.Open();
        int filas = cmd.ExecuteNonQuery();
        conn.Close();

        return filas > 0;
    }

    #endregion

    #region Intentos Login

    public bool RegistrarIntentoSesion(Dictionary<string, object> parametros)
    {
        string query = @"
            INSERT INTO IntentosLogin (Id, UsuarioDni, Fecha, Exitoso)
            VALUES (@id, @dni, GETDATE(), @exitoso)";

        SqlConnection conn = Conexion.Instancia.ObtenerConexion();
        SqlCommand cmd = new SqlCommand(query, conn);

        foreach (var p in parametros)
        {
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        }

        conn.Open();
        int filas = cmd.ExecuteNonQuery();
        conn.Close();

        return filas > 0;
    }

    public int ObtenerIntentosFallidosRecientes(Dictionary<string, object> parametros)
    {
        string query = @"
            SELECT COUNT(*) 
            FROM IntentosLogin
            WHERE UsuarioDni = @dni
              AND Exitoso = 0
              AND Fecha >= @desde
              AND Fecha > ISNULL(
                  (SELECT MAX(Fecha) FROM IntentosLogin 
                   WHERE UsuarioDni = @dni AND Exitoso = 1),
                  '1900-01-01'
              )";

        SqlConnection conn = Conexion.Instancia.ObtenerConexion();
        SqlCommand cmd = new SqlCommand(query, conn);

        foreach (var p in parametros)
        {
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        }

        conn.Open();
        object resultado = cmd.ExecuteScalar();
        conn.Close();

        return resultado != null ? Convert.ToInt32(resultado) : 0;
    }

    public bool LimpiarIntentosFallidos(Dictionary<string, object> parametros)
    {
        string query = "DELETE FROM IntentosLogin WHERE UsuarioDni = @dni AND Exitoso = 0";

        SqlConnection conn = Conexion.Instancia.ObtenerConexion();
        SqlCommand cmd = new SqlCommand(query, conn);
        foreach (var p in parametros)
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);

        conn.Open();
        cmd.ExecuteNonQuery();
        conn.Close();
        return true;
    }

    #endregion
}