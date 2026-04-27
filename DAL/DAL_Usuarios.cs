using DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public class DAL_Usuarios
{
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
            cmd.Parameters.AddWithValue(p.Key, p.Value);
        }

        DataTable tabla = new DataTable();

        conn.Open();
        SqlDataAdapter da = new SqlDataAdapter(cmd);
        da.Fill(tabla);
        conn.Close();

        return tabla;
    }

    public bool CrearUsuario(Dictionary<string, object> parametros)
    {
        string query = @"INSERT INTO Usuarios
                        (Dni, Nombre, Apellido, Email, Rol, Activo, Bloqueado, Login, Contrasenia)
                        VALUES
                        (@dni, @nombre, @apellido, @email, @rol, @activo, @bloqueado, @login, @contrasenia)";

        return Ejecutar(query, parametros);
    }

    public bool EditarUsuario(Dictionary<string, object> parametros)
    {
        string query = @"UPDATE Usuarios SET
                        Nombre = @nombre,
                        Apellido = @apellido,
                        Email = @email,
                        Rol = @rol
                        WHERE Dni = @dni";

        return Ejecutar(query, parametros);
    }

    public bool EliminarUsuario(Dictionary<string, object> parametros)
    {
        string query = "DELETE FROM Usuarios WHERE Dni = @dni";
        return Ejecutar(query, parametros);
    }

    public bool ReactivarUsuario(Dictionary<string, object> parametros)
    {
        string query = "UPDATE Usuarios SET Activo = 1 WHERE Dni = @dni";
        return Ejecutar(query, parametros);
    }

    public bool BloquearUsuario(Dictionary<string, object> parametros)
    {
        string query = "UPDATE Usuarios SET Bloqueado = 1 WHERE Dni = @dni";
        return Ejecutar(query, parametros);
    }

    public bool DesbloquearUsuario(Dictionary<string, object> parametros)
    {
        string query = "UPDATE Usuarios SET Bloqueado = 0 WHERE Dni = @dni";
        return Ejecutar(query, parametros);
    }

    public bool CambiarContraseña(Dictionary<string, object> parametros)
    {
        string query = @"UPDATE Usuarios 
                        SET Contrasenia = @nueva 
                        WHERE Dni = @dni AND Contrasenia = @actual";

        return Ejecutar(query, parametros);
    }
    public DataTable ObtenerContraseniaPorLogin(Dictionary<string, object> parametros)
    {
        string query = "SELECT Contrasenia FROM Usuarios WHERE Login = @login";

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

    //public DataTable ObtenerUsuarioPorLogin(Dictionary<string, object> parametros)
    //{
    //    string query = "SELECT * FROM Usuarios WHERE Login = @login";

    //    SqlConnection conn = 

    //}



    private bool Ejecutar(string query, Dictionary<string, object> parametros)
    {
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


}