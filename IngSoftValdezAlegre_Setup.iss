; ============================================================================
;  Inno Setup - Instalador distribuible del Sistema IngSoftValdezAlegre
; ----------------------------------------------------------------------------
;  Genera UN unico archivo "IngSoftValdezAlegre_Setup.exe" que empaqueta la
;  aplicacion, sus DLLs, los recursos (idiomas) y el configurador de base de
;  datos (Instalador.exe con sus scripts .sql). El usuario final solo recibe
;  ese unico .exe: no necesita el proyecto ni Visual Studio.
;
;  COMO GENERAR EL SETUP:
;    1. Compilar la solucion en modo *Release* (ambos proyectos).
;    2. Abrir este archivo con Inno Setup Compiler.
;    3. Build -> Compile (o F9).
;    4. El instalador queda en la subcarpeta  Setup_Output\
; ============================================================================

#define MyAppName "Sistema IngSoftValdezAlegre"
#define MyAppVersion "1.1"
#define MyAppPublisher "Grupo 06 - Valdez / Alegre"
#define MyAppExeName "IngSoftValdezAlegre.exe"
#define ConfiguradorExe "Instalador.exe"

[Setup]
AppId={{7A6C1E90-06A5-4F3B-9C2D-1B2C3D4E5F60}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\IngSoftValdezAlegre
DefaultGroupName=IngSoftValdezAlegre
DisableProgramGroupPage=yes
OutputDir=Setup_Output
OutputBaseFilename=IngSoftValdezAlegre_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
; --- Aplicacion principal (exe + DLLs + config + Resources\Idiomas) ---
Source: "IngSoftValdezAlegre\bin\Release\*"; DestDir: "{app}"; \
    Flags: recursesubdirs createallsubdirs ignoreversion
; --- Configurador de base de datos (Instalador.exe + carpeta Scripts) ---
Source: "Instalador\bin\Release\*"; DestDir: "{app}"; \
    Flags: recursesubdirs createallsubdirs ignoreversion
; --- Manual (se incluye si existe) ---
Source: "Manual_Instalador_IngSoftValdezAlegre_v1.1.pdf"; DestDir: "{app}"; \
    DestName: "Manual_Instalador.pdf"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\Sistema IngSoftValdezAlegre"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Configurar base de datos"; Filename: "{app}\{#ConfiguradorExe}"
Name: "{group}\Manual del Instalador"; Filename: "{app}\Manual_Instalador.pdf"
Name: "{group}\Desinstalar IngSoftValdezAlegre"; Filename: "{uninstallexe}"
; El acceso directo en el Escritorio lo crea automaticamente el configurador
; (Instalador.exe) al finalizar la configuracion de la base.

[Run]
Filename: "{app}\{#ConfiguradorExe}"; \
    Description: "Configurar la base de datos ahora"; \
    Flags: postinstall skipifsilent nowait

[Code]
// Verifica que exista .NET Framework 4.8 (Release >= 528040). Si no, avisa.
function InitializeSetup(): Boolean;
var
  release: Cardinal;
begin
  Result := True;
  if RegQueryDWordValue(HKLM,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) then
  begin
    if release < 528040 then
    begin
      if MsgBox('Este sistema requiere Microsoft .NET Framework 4.8 o superior, '
              + 'que no parece estar instalado.' + #13#10#13#10
              + 'Desea continuar de todas formas?',
              mbConfirmation, MB_YESNO) = IDNO then
        Result := False;
    end;
  end
  else
  begin
    if MsgBox('No se pudo verificar la version de .NET Framework. '
            + 'Desea continuar de todas formas?',
            mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;
