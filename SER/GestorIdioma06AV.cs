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

        /// <summary>
        /// Retorna el texto traducido con formato aplicado (string.Format).
        /// Útil para mensajes con parámetros como nombres, DNIs, etc.
        /// </summary>
        public string Obtener(string clave, params object[] args)
        {
            string plantilla = Obtener(clave);
            if (args == null || args.Length == 0) return plantilla;
            try { return string.Format(plantilla, args); }
            catch { return plantilla; }
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
                    // Títulos genéricos
                    ["listo"]                 = "Listo",
                    ["aviso"]                 = "Aviso",
                    ["confirmar_titulo"]      = "Confirmar",
                    ["validacion"]            = "Validación",
                    ["acceso_denegado"]       = "Acceso denegado",
                    ["permiso_denegado"]      = "Permiso denegado",
                    ["pass_actualizada_titulo"] = "Contraseña actualizada",
                    ["cambio_pass_requerido_titulo"] = "Cambio de contraseña requerido",
                    // Excepciones - Usuario
                    ["exc_usr_no_encontrado"]    = "No se encontró ningún usuario con DNI '{0}'.",
                    ["exc_usr_duplicado"]        = "Ya existe un usuario con DNI '{0}'.",
                    ["exc_usr_estado_invalido"]  = "No se puede ejecutar '{2}' sobre el usuario '{0}': estado actual es '{1}'.",
                    ["exc_contrasenia_invalida"] = "La contraseña actual ingresada para el usuario '{0}' es incorrecta.",
                    ["exc_acceso_datos"]         = "Error de acceso a datos durante '{0}'.",
                    // Excepciones - Bitácora
                    ["exc_bit_no_encontrado"]    = "No se encontró ningún evento con identificador '{0}'.",
                    ["exc_bit_fechas_invalidas"] = "Rango inválido: 'desde' ({0}) no puede ser posterior a 'hasta' ({1}).",
                    // Validaciones BLL
                    ["val_login_vacio"]              = "El login no puede estar vacío.",
                    ["val_contrasenia_vacia"]        = "La contraseña no puede estar vacía.",
                    ["val_sesion_inactiva"]          = "No hay una sesión activa.",
                    ["val_dni_vacio"]                = "El DNI no puede estar vacío.",
                    ["val_dni_solo_digitos"]         = "El DNI '{0}' solo debe contener dígitos.",
                    ["val_dni_longitud"]             = "El DNI '{0}' debe tener entre 7 y 8 dígitos.",
                    ["val_campo_vacio"]              = "El campo '{0}' no puede estar vacío.",
                    ["val_email_vacio"]              = "El email no puede estar vacío.",
                    ["val_email_formato"]            = "El email '{0}' no tiene un formato válido.",
                    ["val_usuario_nulo"]             = "El objeto usuario no puede ser nulo.",
                    ["val_idioma_invalido"]          = "El idioma '{0}' no es válido. Opciones: {1}",
                    ["val_contrasenia_actual_vacia"] = "La contraseña actual no puede estar vacía.",
                    ["val_contrasenia_nueva_vacia"]  = "La contraseña nueva no puede estar vacía.",
                    ["val_contrasenias_iguales"]     = "La nueva contraseña debe ser distinta a la actual.",
                    // Mensajes UI - Modos UsuariosControl
                    ["modo_consulta_titulo"]    = "Modo Consulta",
                    ["modo_consulta_detalle"]   = "Únicamente para ver los usuarios.",
                    ["modo_anadir_titulo"]      = "Modo Añadir",
                    ["modo_anadir_detalle"]     = "Completá DNI, Apellido, Nombre, Email y Rol. El sistema genera el Login y una contraseña inicial. Presioná Aplicar para crear.",
                    ["modo_modificar_titulo"]   = "Modo Modificar",
                    ["modo_modificar_detalle"]  = "Solo se puede modificar el Email y el Rol del usuario.",
                    ["modo_actdesact_titulo"]   = "Modo Activar/Desactivar",
                    ["modo_actdesact_detalle"]  = "Vas a alternar el estado Activo/Inactivo del usuario seleccionado. Presioná Aplicar para confirmar.",
                    ["modo_desbloquear_titulo"] = "Modo Desbloquear",
                    ["modo_desbloquear_detalle"]= "Seleccioná un usuario bloqueado en la grilla y presioná Aplicar para desbloquearlo.",
                    // Mensajes UI - UsuariosControl - Acciones
                    ["no_admin_crear"]          = "No sos administrador, no podés crear usuarios.",
                    ["no_admin_actdesact"]      = "No sos administrador, no podés activar o desactivar usuarios.",
                    ["no_admin_desbloquear"]    = "No sos administrador, no podés desbloquear usuarios.",
                    ["seleccionar_usuario"]     = "Seleccioná un usuario en la grilla.",
                    ["no_permisos_modificar"]   = "No tenés permisos para modificar a otros usuarios.\nSolo podés modificar tu propio email.",
                    ["usuario_no_bloqueado"]    = "El usuario seleccionado no está bloqueado.",
                    ["no_desactivar_mismo"]     = "No podés desactivar tu propia cuenta.",
                    ["error_cargar_roles"]      = "Error al cargar roles",
                    ["error_cargar_usuarios"]   = "No se pudo cargar la lista de usuarios.",
                    ["usuario_creado"]          = "Usuario creado correctamente.",
                    ["error_crear_usuario"]     = "No se pudo crear el usuario.",
                    ["usuario_modificado"]      = "Usuario modificado correctamente.",
                    ["error_modificar_usuario"] = "No se pudo modificar el usuario.",
                    ["confirmar_toggle_activo"] = "¿Confirmás {0} al usuario {1}, {2}?",
                    ["accion_desactivar"]       = "desactivar",
                    ["accion_reactivar"]        = "reactivar",
                    ["si_desactivar"]           = "Sí, desactivar",
                    ["si_reactivar"]            = "Sí, reactivar",
                    ["confirmar_desbloqueo"]    = "¿Desbloquear al usuario {0}, {1}?",
                    ["confirmar_desbloqueo_titulo"] = "Confirmar desbloqueo",
                    ["si_desbloquear"]          = "Sí, desbloquear",
                    ["usuario_desbloqueado"]    = "Usuario desbloqueado correctamente.",
                    // Mensajes UI - Login
                    ["login_cambiar_pass_requerido"] = "Por seguridad, debés cambiar tu contraseña antes de continuar.",
                    ["login_pass_obligatoria"]       = "Debés cambiar tu contraseña para poder ingresar al sistema.",
                    ["login_pass_actualizada"]       = "Tu contraseña fue actualizada. Iniciá sesión nuevamente con tu nueva contraseña.",
                    ["login_o_pass_incorrectos"]     = "Login o contraseña incorrectos.",
                    ["usuario_bloqueado_contacte"]   = "El usuario está bloqueado. Contacte a un administrador.",
                    ["usuario_inactivo_contacte"]    = "El usuario está inactivo. Contacte a un administrador.",
                    ["error_conexion_tarde"]         = "Error de conexión. Intentá más tarde.",
                    // Mensajes UI - CambiarContrasenia
                    ["cambiar_pass_obligatorio_msg"]  = "Por seguridad, debés cambiar tu contraseña antes de continuar.",
                    ["cambiar_contrasenia_obligatorio_sufijo"] = " (obligatorio)",
                    ["completar_tres_campos"]         = "Completá los tres campos.",
                    ["pass_no_coincide"]              = "La nueva contraseña y la repetición no coinciden.",
                    ["pass_actual_incorrecta"]        = "La contraseña actual es incorrecta.",
                    ["error_actualizar_pass"]         = "No se pudo actualizar la contraseña.",
                    ["pass_actualizada_obligatorio"]  = "Contraseña actualizada correctamente.\nYa podés ingresar al sistema.",
                    ["pass_actualizada_voluntario"]   = "Contraseña actualizada correctamente.\nDebés iniciar sesión nuevamente.",
                    ["error_conexion_reintentar"]     = "Error de conexión. Intentá de nuevo.",
                    // Mensajes UI - BitacoraControl
                    ["fecha_inicial_mayor"]   = "La fecha inicial no puede ser mayor a la final.",
                    ["error_filtro"]          = "No se pudo aplicar el filtro:",
                    ["pdf_generado"]          = "PDF generado correctamente.",
                    ["archivo_generado"]      = "Archivo generado correctamente.",
                    // Mensajes UI - FRMMain
                    ["confirmar_cerrar_sesion"] = "¿Estás seguro que querés cerrar sesión?",
                    ["si_cerrar"]               = "Sí, cerrar",
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
                    // Generic titles
                    ["listo"]                 = "Done",
                    ["aviso"]                 = "Notice",
                    ["confirmar_titulo"]      = "Confirm",
                    ["validacion"]            = "Validation",
                    ["acceso_denegado"]       = "Access Denied",
                    ["permiso_denegado"]      = "Permission Denied",
                    ["pass_actualizada_titulo"] = "Password Updated",
                    ["cambio_pass_requerido_titulo"] = "Password Change Required",
                    // Exceptions - User
                    ["exc_usr_no_encontrado"]    = "No user found with ID '{0}'.",
                    ["exc_usr_duplicado"]        = "A user with ID '{0}' already exists.",
                    ["exc_usr_estado_invalido"]  = "Cannot execute '{2}' on user '{0}': current state is '{1}'.",
                    ["exc_contrasenia_invalida"] = "The current password entered for user '{0}' is incorrect.",
                    ["exc_acceso_datos"]         = "Data access error during '{0}'.",
                    // Exceptions - Audit log
                    ["exc_bit_no_encontrado"]    = "No event found with identifier '{0}'.",
                    ["exc_bit_fechas_invalidas"] = "Invalid range: 'from' ({0}) cannot be after 'to' ({1}).",
                    // BLL Validations
                    ["val_login_vacio"]              = "Login cannot be empty.",
                    ["val_contrasenia_vacia"]        = "Password cannot be empty.",
                    ["val_sesion_inactiva"]          = "No active session found.",
                    ["val_dni_vacio"]                = "ID cannot be empty.",
                    ["val_dni_solo_digitos"]         = "ID '{0}' must contain only digits.",
                    ["val_dni_longitud"]             = "ID '{0}' must have 7 to 8 digits.",
                    ["val_campo_vacio"]              = "The field '{0}' cannot be empty.",
                    ["val_email_vacio"]              = "Email cannot be empty.",
                    ["val_email_formato"]            = "Email '{0}' has an invalid format.",
                    ["val_usuario_nulo"]             = "The user object cannot be null.",
                    ["val_idioma_invalido"]          = "Language '{0}' is not valid. Options: {1}",
                    ["val_contrasenia_actual_vacia"] = "Current password cannot be empty.",
                    ["val_contrasenia_nueva_vacia"]  = "New password cannot be empty.",
                    ["val_contrasenias_iguales"]     = "The new password must be different from the current one.",
                    // UI Messages - Modes UsuariosControl
                    ["modo_consulta_titulo"]    = "View Mode",
                    ["modo_consulta_detalle"]   = "Read-only view of all users.",
                    ["modo_anadir_titulo"]      = "Add Mode",
                    ["modo_anadir_detalle"]     = "Fill in ID, Last name, First name, Email and Role. The system generates the Username and an initial password. Press Apply to create.",
                    ["modo_modificar_titulo"]   = "Edit Mode",
                    ["modo_modificar_detalle"]  = "Only Email and Role can be modified.",
                    ["modo_actdesact_titulo"]   = "Enable/Disable Mode",
                    ["modo_actdesact_detalle"]  = "You will toggle the Active/Inactive status of the selected user. Press Apply to confirm.",
                    ["modo_desbloquear_titulo"] = "Unlock Mode",
                    ["modo_desbloquear_detalle"]= "Select a locked user in the grid and press Apply to unlock.",
                    // UI Messages - UsuariosControl - Actions
                    ["no_admin_crear"]          = "You need administrator permissions to create users.",
                    ["no_admin_actdesact"]      = "You need administrator permissions to enable or disable users.",
                    ["no_admin_desbloquear"]    = "You need administrator permissions to unlock users.",
                    ["seleccionar_usuario"]     = "Select a user from the grid.",
                    ["no_permisos_modificar"]   = "You don't have permission to modify other users.\nYou can only edit your own email.",
                    ["usuario_no_bloqueado"]    = "The selected user is not locked.",
                    ["no_desactivar_mismo"]     = "You cannot deactivate your own account.",
                    ["error_cargar_roles"]      = "Error loading roles",
                    ["error_cargar_usuarios"]   = "Could not load the user list.",
                    ["usuario_creado"]          = "User created successfully.",
                    ["error_crear_usuario"]     = "Could not create the user.",
                    ["usuario_modificado"]      = "User modified successfully.",
                    ["error_modificar_usuario"] = "Could not modify the user.",
                    ["confirmar_toggle_activo"] = "Confirm {0} for user {1}, {2}?",
                    ["accion_desactivar"]       = "disable",
                    ["accion_reactivar"]        = "enable",
                    ["si_desactivar"]           = "Yes, disable",
                    ["si_reactivar"]            = "Yes, enable",
                    ["confirmar_desbloqueo"]    = "Unlock user {0}, {1}?",
                    ["confirmar_desbloqueo_titulo"] = "Confirm unlock",
                    ["si_desbloquear"]          = "Yes, unlock",
                    ["usuario_desbloqueado"]    = "User unlocked successfully.",
                    // UI Messages - Login
                    ["login_cambiar_pass_requerido"] = "For security, you must change your password before continuing.",
                    ["login_pass_obligatoria"]       = "You must change your password to access the system.",
                    ["login_pass_actualizada"]       = "Your password has been updated. Please sign in again with your new password.",
                    ["login_o_pass_incorrectos"]     = "Invalid username or password.",
                    ["usuario_bloqueado_contacte"]   = "The account is locked. Please contact an administrator.",
                    ["usuario_inactivo_contacte"]    = "The account is inactive. Please contact an administrator.",
                    ["error_conexion_tarde"]         = "Connection error. Please try again later.",
                    // UI Messages - ChangePassword
                    ["cambiar_pass_obligatorio_msg"]  = "For security, you must change your password before continuing.",
                    ["cambiar_contrasenia_obligatorio_sufijo"] = " (required)",
                    ["completar_tres_campos"]         = "Please fill in all three fields.",
                    ["pass_no_coincide"]              = "The new password and confirmation do not match.",
                    ["pass_actual_incorrecta"]        = "The current password is incorrect.",
                    ["error_actualizar_pass"]         = "Could not update the password.",
                    ["pass_actualizada_obligatorio"]  = "Password updated successfully.\nYou can now sign in.",
                    ["pass_actualizada_voluntario"]   = "Password updated successfully.\nPlease sign in again.",
                    ["error_conexion_reintentar"]     = "Connection error. Please try again.",
                    // UI Messages - AuditLog
                    ["fecha_inicial_mayor"]   = "The start date cannot be after the end date.",
                    ["error_filtro"]          = "Could not apply the filter:",
                    ["pdf_generado"]          = "PDF generated successfully.",
                    ["archivo_generado"]      = "File generated successfully.",
                    // UI Messages - FRMMain
                    ["confirmar_cerrar_sesion"] = "Are you sure you want to sign out?",
                    ["si_cerrar"]               = "Sign out",
                }
            };

        #endregion
    }
}
