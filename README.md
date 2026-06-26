# Abrir Windows Terminal desde la barra de direcciones del Explorador de Windows 10

> Guía para que escribir `wt` en la barra de direcciones del Explorador de Windows 10 abra Windows Terminal directamente en la carpeta actual.

---

## Índice

1. [Qué conseguimos](#qué-conseguimos)
2. [Por qué no funciona por defecto](#por-qué-no-funciona-por-defecto)
3. [Requisitos](#requisitos)
4. [Solución paso a paso](#solución-paso-a-paso)
   1. [Localizar el ejecutable real de Windows Terminal](#1-localizar-el-ejecutable-real-de-windows-terminal)
   2. [Crear el envoltorio `wt.cs`](#2-crear-el-envoltorio-wtcs)
   3. [Compilar el envoltorio a `wt.exe`](#3-compilar-el-envoltorio-a-wtexe)
   4. [Añadir `wt.exe` al registro `App Paths`](#4-añadir-wtexe-al-registro-app-paths)
5. [Verificación](#verificación)
6. [Solución de problemas](#solución-de-problemas)
7. [Cómo deshacer los cambios](#cómo-deshacer-los-cambios)
8. [Notas técnicas](#notas-técnicas)
9. [Licencia](#licencia)

---

## Qué conseguimos

Tras seguir esta guía, podrás abrir el Explorador de Windows en cualquier carpeta, escribir `wt` en la barra de direcciones y pulsar `Enter`. Windows Terminal se abrirá con una pestaña nueva ya situada en esa carpeta.

```
Explorador de Windows
┌─────────────────────────────────────────────┐
│ ▶  Esta PC ▶ Documentos ▶ MiProyecto          │
│                                               │
│ ┌─────────────────────────────────────────┐   │
│ │ wt                                      │   │
│ └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
                 ↓
        ┌──────────────────┐
        │ Windows Terminal │
        │ ~/Documentos/MiProyecto $  │
        └──────────────────┘
```

---

## Por qué no funciona por defecto

Windows Terminal ya se puede lanzar con `wt.exe -d <carpeta>`, pero la barra de direcciones del Explorador no pasa la carpeta actual como argumento `-d`. Simplemente ejecuta el programa registrado en `App Paths\wt.exe` con el directorio de trabajo apuntando a la carpeta activa.

Además, si intentamos crear un envoltorio `wt.cmd` o `wt.exe` que llame al `wt.exe` real sin usar su ruta completa, puede producirse una **llamada recursiva infinita** y el Explorador se quedará "pensando" sin abrir nada.

Por eso la solución segura es crear un pequeño ejecutable envoltorio que:

1. Sepa exactamente dónde está el `wt.exe` real.
2. Lance `wt.exe -d .` cuando no se le pase otra carpeta.
3. Esté registrado en `HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe`, que es lo que la barra de direcciones del Explorador consulta.

---

## Requisitos

- Windows 10 (versión actual, actualizada).
- Windows Terminal instalado (desde Microsoft Store o manualmente).
- Cuenta de usuario con permisos para editar variables de entorno y el registro de `HKCU`.
- Compilador de C# (`csc.exe`) incluido en el .NET Framework. Suele estar en:
  ```
  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
  ```

---

## Solución paso a paso

### 1. Localizar el ejecutable real de Windows Terminal

Windows expone un acceso directo en:

```
%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe
```

pero ese archivo es un punto de análisis (reparse point), no el ejecutable físico. Necesitamos la ruta real, que suele estar en:

```
C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_<versión>__8wekyb3d8bbwe\wt.exe
```

Para averiguar la ruta exacta en tu equipo, abre PowerShell y ejecuta:

```powershell
fsutil reparsepoint query "$env:LOCALAPPDATA\Microsoft\WindowsApps\wt.exe"
```

Anota la ruta del ejecutable físico. En nuestro caso fue:

```
C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.24.11321.0_x64__8wekyb3d8bbwe\wt.exe
```

> **Importante:** Usa tu versión exacta. La ruta puede cambiar cuando Windows Terminal se actualice.

---

### 2. Crear el envoltorio `wt.cs`

Crea una carpeta para herramientas personales. Nosotros usaremos:

```
C:\Users\<tuUsuario>\AppData\Local\MyTools
```

Dentro de esa carpeta crea un archivo llamado `wt.cs` con este contenido:

```csharp
using System;
using System.Diagnostics;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        // Ruta absoluta al ejecutable REAL de Windows Terminal.
        // ¡Cámbiala por la ruta que obtuviste en el paso anterior!
        string realWt = @"C:\Program Files\WindowsApps\"
            + @"Microsoft.WindowsTerminal_1.24.11321.0_x64__8wekyb3d8bbwe\"
            + "wt.exe";

        var sb = new StringBuilder();
        bool hasDir = false;

        foreach (var a in args)
        {
            if (sb.Length > 0)
                sb.Append(' ');

            if (a.Contains(" ") || a.Contains("\""))
                sb.Append("\"").Append(a.Replace("\"", "\\\"")).Append("\"");
            else
                sb.Append(a);

            if (a == "-d" || a == "--directory")
                hasDir = true;
        }

        if (!hasDir)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append("-d .");
        }

        var psi = new ProcessStartInfo(realWt, sb.ToString())
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory
        };

        Process.Start(psi);
    }
}
```

---

### 3. Compilar el envoltorio a `wt.exe`

Abre PowerShell en la carpeta `MyTools` y ejecuta:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
    /target:winexe `
    /out:"C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.exe" `
    "C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.cs"
```

> Recuerda sustituir `<tuUsuario>` por tu nombre de usuario.

Si la compilación es correcta, verás un archivo `wt.exe` de unos 5 KB en `MyTools`.

---

### 4. Añadir `wt.exe` al registro `App Paths`

La barra de direcciones del Explorador consulta la clave:

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe
```

Para registrar nuestro envoltorio, abre PowerShell como administrador **no es necesario**, porque sólo vamos a tocar `HKEY_CURRENT_USER`. Ejecuta:

```powershell
reg.exe add "HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe" `
    /ve /d "C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.exe" /f

reg.exe add "HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe" `
    /v Path /d "C:\Users\<tuUsuario>\AppData\Local\MyTools" /f
```

Sustituye `<tuUsuario>` por tu nombre de usuario.

---

## Verificación

1. Abre el Explorador de Windows en cualquier carpeta, por ejemplo:
   ```
   C:\Users\<tuUsuario>\Documentos
   ```

2. Haz clic en la barra de direcciones.

3. Escribe `wt` y pulsa `Enter`.

4. Debería abrirse Windows Terminal con una pestaña en la carpeta actual.

Si quieres verificar desde PowerShell que la clave de registro está bien, ejecuta:

```powershell
reg.exe query "HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe"
```

Debería mostrar algo como:

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe
    (Predeterminado)    REG_SZ    C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.exe
    Path                REG_SZ    C:\Users\<tuUsuario>\AppData\Local\MyTools
```

---

## Solución de problemas

### El Explorador se queda "pensando" y no abre nada

Probablemente tienes un `wt.exe` o `wt.cmd` envoltorio que llama a `wt` sin ruta absoluta, causando recursión infinita.

Revisa si existe:

```
C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.exe
C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.cmd
```

y asegúrate de que apunten al ejecutable físico de Windows Terminal, no a un `wt` genérico.

También revisa la variable de entorno `PATH` del usuario. Si `MyTools` contiene un `wt.exe` defectuoso y está antes que `%LOCALAPPDATA%\Microsoft\WindowsApps`, cualquier consola o menú `Win + R` también se verá afectado.

### Windows Terminal se abre pero en la carpeta del perfil

Esto significa que `wt.exe` no recibe el argumento `-d`. Revisa que tu `wt.cs` añada `-d .` cuando no reciba otra carpeta.

### Windows Terminal se abre pero en una carpeta incorrecta

El Explorador pasa el directorio de trabajo correctamente, pero tu envoltorio no lo usa. Asegúrate de que en `wt.cs` tengas:

```csharp
WorkingDirectory = Environment.CurrentDirectory
```

### La ruta de Windows Terminal cambió tras una actualización

Si Windows Terminal se actualiza, la carpeta de versión puede cambiar. Vuelve a ejecutar:

```powershell
fsutil reparsepoint query "$env:LOCALAPPDATA\Microsoft\WindowsApps\wt.exe"
```

Actualiza la ruta en `wt.cs`, recompila y reinicia el Explorador.

---

## Cómo deshacer los cambios

Si quieres volver al comportamiento original:

1. Elimina el envoltorio:
   ```powershell
   Remove-Item -Path "C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.exe" -Force
   Remove-Item -Path "C:\Users\<tuUsuario>\AppData\Local\MyTools\wt.cs" -Force
   ```

2. Elimina la clave de registro:
   ```powershell
   reg.exe delete "HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe" /f
   ```

3. Reinicia el Explorador de Windows o cierra sesión.

---

## Notas técnicas

- La barra de direcciones del Explorador no usa la variable `PATH` del usuario para resolver `wt`. Usa `App Paths`.
- El acceso directo `%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe` es un reparse point, no un ejecutable real. Llamarlo directamente desde un envoltorio puede funcionar, pero usar la ruta física es más fiable.
- `UseShellExecute = false` en el `ProcessStartInfo` evita problemas con la resolución de ejecutables de la Windows Store.
- Si prefieres que el envoltorio no abra una ventana de consola propia, compílalo con `/target:winexe`.

---

## Licencia

Este documento y el código de ejemplo se proporcionan tal cual, sin garantía de ningún tipo. Puedes usarlo, modificarlo y compartirlo libremente.

