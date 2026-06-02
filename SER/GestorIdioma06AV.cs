using System.Collections.Generic;

namespace SER
{
    /// <summary>
    /// Singleton que gestiona el idioma activo de la sesión.
    /// Se inicializa al hacer Login con el idioma guardado del usuario.
    /// El usuario puede cambiarlo en cualquier momento (persiste via BLL).
    /// </summary>
    public sealed class GestorIdioma06AV
    {
        #region Singleton

        private static GestorIdioma06AV _instancia;
        private static readonly object _lock = new object();

        private GestorIdioma06AV()
        {
            _idiomaActual = IdiomaDefecto;
        }

        public static GestorIdioma06AV Instancia
        {
            get
            {
                if (_instancia == null)
                    lock (_lock)
                        if (_instancia == null)
                            _instancia = new GestorIdioma06AV();
                return _instancia;
            }
        }

        #endregion

        #region Constantes

        public const string ES = "ES";
        public const string EN = "EN";
        public const string IdiomaDefecto = ES;

        public static readonly string[] IdiomasDisponibles = { ES, EN };

        #endregion

        #region Estado

        private string _idiomaActual;

        /// <summary>Idioma actualmente activo en la sesión.</summary>
        public string IdiomaActual => _idiomaActual;

        /// <summary>
        /// Carga el idioma del usuario al iniciar sesión.
        /// Si el valor es nulo o no reconocido, usa el idioma por defecto.
        /// </summary>
        public void Cargar(string idioma)
        {
            _idiomaActual = EsValido(idioma) ? idioma : IdiomaDefecto;
        }

        /// <summary>
        /// Cambia el idioma activo en memoria.
        /// Para persistirlo a la base de datos llamar a UsuariosBLL06AV.CambiarIdioma().
        /// </summary>
        public void CambiarIdioma(string idioma)
        {
            if (!EsValido(idioma))
                throw new System.ArgumentException($"Idioma '{idioma}' no está disponible. Opciones: {string.Join(", ", IdiomasDisponibles)}");
            _idiomaActual = idioma;
        }

        public bool EsValido(string idioma)
        {
            if (string.IsNullOrWhiteSpace(idioma)) return false;
            foreach (var i in IdiomasDisponibles)
                if (i == idioma.ToUpper()) return true;
            return false;
        }

        #endregion

        #region Traducciones

        /// <summary>
        /// Retorna el texto traducido para la clave dada en el idioma activo.
        /// Si la clave no existe, retorna la clave misma como fallback.
        /// </summary>
        public string Obtener(string clave)
        {
            if (_traducciones.TryGetValue(_idiomaActual, out var dicc))
                if (dicc.TryGetValue(clave, out var texto))
                    return texto;
            return clave; // fallback: muestra la clave
        }

        // Diccionario principal: idioma → (clave → texto)
        private static readonly Dictionary<string, Dictionary<string, string>> _traducciones =
            new Dictionary<string, Dictionary<string, string>>
            {
                [ES] = new Dictionary<string, string>
                {
                    // General
                    ["aceptar"]               = "Aceptar",
                    ["cancelar"]              = "Cancelar",
                    ["guardar"]               = "Guardar",
                    ["eliminar"]              = "Eliminar",
                    ["editar"]                = "Editar",
                    ["agregar"]               = "Agregar",
                    ["buscar"]                = "Buscar",
                    ["cerrar"]                = "Cerrar",
                    ["confirmar"]             = "Confirmar",
                    ["limpiar"]               = "Limpiar",
                    ["exportar"]              = "Exportar",
                    ["error"]                 = "Error",
                    ["exito"]                 = "Éxito",
                    ["advertencia"]           = "Advertencia",
                    // Sesión
                    ["login"]                 = "Iniciar sesión",
                    ["iniciar_sesion"]        = "INICIAR SESION",
                    ["logout"]                = "Cerrar sesión",
                    ["cerrar_sesion"]         = "Cerrar Sesión",
                    ["usuario"]               = "Usuario",
                    ["usuario_label"]         = "USUARIO",
                    ["contrasenia"]           = "Contraseña",
                    ["contrasenia_label"]     = "CONTRASEÑA",
                    ["cambiar_contrasenia"]   = "Cambiar Contraseña",
                    ["nueva_contrasenia"]     = "Nueva contraseña",
                    ["contrasenia_actual"]    = "Contraseña actual",
                    ["repetir_contrasenia"]   = "Repetir nueva contraseña",
                    // Usuarios
                    ["usuarios"]              = "Usuarios",
                    ["nombre"]                = "Nombre",
                    ["apellido"]              = "Apellido",
                    ["email"]                 = "Email",
                    ["dni"]                   = "DNI",
                    ["campo_login"]           = "Login",
                    ["rol"]                   = "Rol",
                    ["activo"]                = "Activo",
                    ["activos"]               = "Activos",
                    ["todos"]                 = "Todos",
                    ["bloqueado"]             = "Bloqueado",
                    ["bloquear"]              = "Bloquear",
                    ["desbloquear"]           = "Desbloquear",
                    ["activar"]               = "Activar",
                    ["desactivar"]            = "Desactivar",
                    ["crear"]                 = "Crear",
                    ["modificar"]             = "Modificar",
                    ["act_desact"]            = "Act. / Desact.",
                    ["aplicar"]               = "Aplicar",
                    ["datos_usuario"]         = "Datos del usuario",
                    ["mensaje"]               = "Mensaje",
                    ["numero_usuarios"]       = "Usuarios:",
                    // Bitácora
                    ["bitacora"]              = "Bitácora",
                    ["bitacora_titulo"]       = "Bitácora de eventos",
                    ["filtrar"]               = "Filtrar",
                    ["imprimir_pdf"]          = "Imprimir PDF",
                    ["imprimir_excel"]        = "Imprimir EXCEL",
                    ["fecha_desde"]           = "Fecha Desde",
                    ["fecha_hasta"]           = "Fecha Hasta",
                    ["criticidad"]            = "Criticidad",
                    ["evento"]                = "Evento",
                    ["modulo"]                = "Modulo",
                    ["numero_eventos"]        = "Eventos:",
                    // Permisos
                    ["patentes"]              = "Patentes",
                    ["familias"]              = "Familias",
                    ["roles"]                 = "Roles",
                    ["permisos"]              = "Permisos",
                    ["agregar_patente"]       = "Agregar patente",
                    ["agregar_familia"]       = "Agregar familia",
                    ["quitar_patente"]        = "Quitar patente",
                    ["quitar_familia"]        = "Quitar familia",
                    // Idioma
                    ["idioma"]                = "Idioma",
                    ["cambiar_idioma"]        = "Cambiar idioma",
                    ["idioma_guardado"]       = "Idioma guardado correctamente.",
                    // Mensajes
                    ["confirmar_eliminacion"] = "¿Está seguro que desea eliminar este registro?",
                    ["operacion_exitosa"]     = "Operación realizada con éxito.",
                    ["operacion_fallida"]     = "La operación no pudo completarse.",
                    ["campo_requerido"]       = "Este campo es obligatorio.",
                    ["sin_resultados"]        = "No se encontraron resultados.",
                },

                [EN] = new Dictionary<string, string>
                {
                    // General
                    ["aceptar"]               = "Accept",
                    ["cancelar"]              = "Cancel",
                    ["guardar"]               = "Save",
                    ["eliminar"]              = "Delete",
                    ["editar"]                = "Edit",
                    ["agregar"]               = "Add",
                    ["buscar"]                = "Search",
                    ["cerrar"]                = "Close",
                    ["confirmar"]             = "Confirm",
                    ["limpiar"]               = "Clear",
                    ["exportar"]              = "Export",
                    ["error"]                 = "Error",
                    ["exito"]                 = "Success",
                    ["advertencia"]           = "Warning",
                    // Session
                    ["login"]                 = "Sign in",
                    ["iniciar_sesion"]        = "SIGN IN",
                    ["logout"]                = "Sign out",
                    ["cerrar_sesion"]         = "Sign Out",
                    ["usuario"]               = "User",
                    ["usuario_label"]         = "USERNAME",
                    ["contrasenia"]           = "Password",
                    ["contrasenia_label"]     = "PASSWORD",
                    ["cambiar_contrasenia"]   = "Change Password",
                    ["nueva_contrasenia"]     = "New password",
                    ["contrasenia_actual"]    = "Current password",
                    ["repetir_contrasenia"]   = "Repeat new password",
                    // Users
                    ["usuarios"]              = "Users",
                    ["nombre"]                = "First name",
                    ["apellido"]              = "Last name",
                    ["email"]                 = "Email",
                    ["dni"]                   = "ID number",
                    ["campo_login"]           = "Username",
                    ["rol"]                   = "Role",
                    ["activo"]                = "Active",
                    ["activos"]               = "Active only",
                    ["todos"]                 = "All",
                    ["bloqueado"]             = "Locked",
                    ["bloquear"]              = "Lock",
                    ["desbloquear"]           = "Unlock",
                    ["activar"]               = "Activate",
                    ["desactivar"]            = "Deactivate",
                    ["crear"]                 = "Create",
                    ["modificar"]             = "Edit",
                    ["act_desact"]            = "Enable / Disable",
                    ["aplicar"]               = "Apply",
                    ["datos_usuario"]         = "User details",
                    ["mensaje"]               = "Message",
                    ["numero_usuarios"]       = "Users:",
                    // Audit log
                    ["bitacora"]              = "Audit Log",
                    ["bitacora_titulo"]       = "Event log",
                    ["filtrar"]               = "Filter",
                    ["imprimir_pdf"]          = "Print PDF",
                    ["imprimir_excel"]        = "Print Excel",
                    ["fecha_desde"]           = "From",
                    ["fecha_hasta"]           = "To",
                    ["criticidad"]            = "Severity",
                    ["evento"]                = "Event",
                    ["modulo"]                = "Module",
                    ["numero_eventos"]        = "Events:",
                    // Permissions
                    ["patentes"]              = "Permissions",
                    ["familias"]              = "Groups",
                    ["roles"]                 = "Roles",
                    ["permisos"]              = "Permissions",
                    ["agregar_patente"]       = "Add permission",
                    ["agregar_familia"]       = "Add group",
                    ["quitar_patente"]        = "Remove permission",
                    ["quitar_familia"]        = "Remove group",
                    // Language
                    ["idioma"]                = "Language",
                    ["cambiar_idioma"]        = "Change language",
                    ["idioma_guardado"]       = "Language saved successfully.",
                    // Messages
                    ["confirmar_eliminacion"] = "Are you sure you want to delete this record?",
                    ["operacion_exitosa"]     = "Operation completed successfully.",
                    ["operacion_fallida"]     = "The operation could not be completed.",
                    ["campo_requerido"]       = "This field is required.",
                    ["sin_resultados"]        = "No results found.",
                }
            };

        #endregion
    }
}
