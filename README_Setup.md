# Generar el instalador distribuible (SetUp.exe)

Con esto obtenés **un único archivo** `IngSoftValdezAlegre_Setup.exe` que podés mandarle a
cualquier persona. Esa persona **no necesita el proyecto ni Visual Studio**: solo ejecuta
el setup y queda todo instalado y configurado.

## Pasos para generarlo (una sola vez, en tu PC)

1. **Compilá la solución en modo Release.**
   En Visual Studio, cambiá la configuración de `Debug` a `Release` (barra superior) y hacé
   *Compilar → Recompilar solución*. Esto genera:
   - `IngSoftValdezAlegre\bin\Release\` (la app + DLLs + `Resources\Idiomas`)
   - `Instalador\bin\Release\` (el configurador + carpeta `Scripts`)

2. **Instalá Inno Setup 6** (gratis): https://jrsoftware.org/isinfo.php

3. **Compilá el instalador.**
   Abrí `IngSoftValdezAlegre_Setup.iss` con *Inno Setup Compiler* y presioná **Compile**
   (o desde consola: `ISCC.exe IngSoftValdezAlegre_Setup.iss`).

4. El resultado queda en `Setup_Output\IngSoftValdezAlegre_Setup.exe`. **Ese es el archivo
   que compartís.**

## Qué hace el setup en la PC del usuario final

1. Verifica que exista **.NET Framework 4.8** (avisa si falta).
2. Copia la aplicación y el configurador a `Archivos de programa\IngSoftValdezAlegre`.
3. Crea accesos directos en el menú Inicio (sistema, configurador y manual).
4. Abre automáticamente el **asistente de base de datos**: elegir instancia (incluida
   LocalDB), crear la base, dejar la app conectada y crear el **acceso directo en el
   Escritorio**.

## Requisitos en la PC del usuario final

- Windows 10 / 11.
- **.NET Framework 4.8** o superior (viene en Windows actualizado).
- **SQL Server o LocalDB** instalado (el motor de base de datos no se puede empaquetar
  dentro del setup; si el usuario no lo tiene, hay que instalar SQL Server Express o
  LocalDB por separado).
