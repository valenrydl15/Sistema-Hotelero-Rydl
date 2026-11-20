<#
  Hotel Rydl (SIH - HRS-480) — operaciones de base de datos del instalador.
  Lo ejecuta el instalador de Inno Setup (HotelRydl_Instalador.iss); también se puede correr a mano:

    powershell -ExecutionPolicy Bypass -File BaseDatos.ps1 -Accion Verificar   -Salida res.txt
    powershell -ExecutionPolicy Bypass -File BaseDatos.ps1 -Accion Instalar    -Bak "C:\...\HotelRydl.bak" -Inicial BaseInicial.sql -Respaldo -Salida res.txt
    powershell -ExecutionPolicy Bypass -File BaseDatos.ps1 -Accion Credenciales -Salida res.txt

  Acciones
    Verificar     Busca SQL Server en el equipo y si existe la base HotelRydlDB.
                  Código de salida: 0 = SQL Server OK y la base NO existe · 1 = la base YA existe
                                    2 = no se encontró SQL Server      · 3 = error
    Instalar      (Opcional) respalda la base actual y crea la base nueva: restaura la estructura (.bak)
                  y con -Inicial la deja en estado de fábrica (BaseInicial.sql: sin datos cargados,
                  con roles, permisos y el usuario administrador). La copia del .bak se borra al terminar.
                  Código de salida: 0 = OK · 2 = sin SQL Server · 3 = error
    Credenciales  Lee el usuario administrador (IdUsuario = 1) para mostrarlo al terminar.

  -Salida: archivo de texto donde se escriben los resultados (clave=valor) para que los lea el instalador.
  -CarpetaApp: carpeta del programa; si tiene conexion.txt se usa ese servidor (igual que el programa).
#>
param(
    [ValidateSet('Verificar', 'Instalar', 'Credenciales')]
    [string]$Accion = 'Verificar',
    [string]$Bak = '',
    [string]$Inicial = '',
    [switch]$Respaldo,
    [string]$CarpetaApp = '',
    [string]$Salida = '',
    [string]$Log = ''
)

$ErrorActionPreference = 'Stop'
$NombreBD = 'HotelRydlDB'
$resultado = [ordered]@{}

function Escribir-Log([string]$texto) {
    $linea = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $texto
    Write-Host $linea   # Write-Host: no se mezcla con lo que devuelven las funciones
    if ($Log) { try { Add-Content -Path $Log -Value $linea -Encoding UTF8 } catch { } }
}

function Guardar-Salida {
    if (-not $Salida) { return }
    $lineas = foreach ($k in $resultado.Keys) { "$k=$($resultado[$k])" }
    Set-Content -Path $Salida -Value $lineas -Encoding Default   # ANSI: lo lee Inno Setup
}

function Cadena([string]$servidor, [string]$base = 'master') {
    if ($servidor -match '=') {          # conexion.txt con una cadena completa
        $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($servidor)
        $b['Initial Catalog'] = $base
        return $b.ConnectionString
    }
    return "Server=$servidor;Database=$base;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=6"
}

function Ejecutar([string]$servidor, [string]$sql, [int]$timeout = 600) {
    $con = New-Object System.Data.SqlClient.SqlConnection (Cadena $servidor)
    $con.Open()
    try {
        $cmd = $con.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = $timeout
        [void]$cmd.ExecuteNonQuery()
    } finally { $con.Close() }
}

function Escalar([string]$servidor, [string]$sql, [string]$base = 'master') {
    $con = New-Object System.Data.SqlClient.SqlConnection (Cadena $servidor $base)
    $con.Open()
    try {
        $cmd = $con.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 60
        $r = $cmd.ExecuteScalar()
        if ($r -is [System.DBNull]) { return $null }
        return $r
    } finally { $con.Close() }
}

function Candidatos {
    $lista = New-Object System.Collections.Generic.List[string]
    if ($CarpetaApp) {
        $txt = Join-Path $CarpetaApp 'conexion.txt'
        if (Test-Path $txt) {
            $v = (Get-Content $txt -Raw).Trim()
            if ($v) { $lista.Add($v) }
        }
    }
    foreach ($ruta in 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL',
                      'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL') {
        if (Test-Path $ruta) {
            $props = Get-ItemProperty $ruta
            foreach ($p in $props.PSObject.Properties) {
                if ($p.Name -like 'PS*') { continue }
                if ($p.Name -eq 'MSSQLSERVER') { $n = 'localhost' } else { $n = "$env:COMPUTERNAME\$($p.Name)" }
                if (-not $lista.Contains($n)) { $lista.Add($n) }
            }
        }
    }
    foreach ($n in "$env:COMPUTERNAME\SQLEXPRESS", 'localhost', '(localdb)\MSSQLLocalDB') {
        if (-not $lista.Contains($n)) { $lista.Add($n) }
    }
    return $lista
}

# Elige la instancia: la que ya tiene HotelRydlDB; si ninguna, la primera que responde.
function Resolver-Servidor {
    $primera = $null
    foreach ($s in Candidatos) {
        try {
            $id = Escalar $s "SELECT DB_ID(N'$NombreBD')"
            Escribir-Log "Instancia disponible: $s (base $NombreBD $(if ($id) {'encontrada'} else {'no existe'}))"
            if ($id) { return @{ Servidor = $s; Existe = $true } }
            if (-not $primera) { $primera = $s }
        } catch {
            Escribir-Log "Sin conexión con $s"
        }
    }
    if ($primera) { return @{ Servidor = $primera; Existe = $false } }
    return $null
}

try {
    Escribir-Log "=== Acción: $Accion ==="
    $r = Resolver-Servidor
    if (-not $r) {
        $resultado['sqlserver'] = '0'
        $resultado['mensaje'] = 'No se encontró SQL Server en este equipo (o el usuario de Windows no tiene permiso para conectarse).'
        Escribir-Log $resultado['mensaje']
        Guardar-Salida; exit 2
    }
    $servidor = $r.Servidor
    $resultado['sqlserver'] = '1'
    $resultado['servidor'] = $servidor
    $resultado['existe'] = $(if ($r.Existe) { '1' } else { '0' })

    switch ($Accion) {
        'Verificar' {
            Guardar-Salida
            if ($r.Existe) { exit 1 } else { exit 0 }
        }

        'Instalar' {
            if (-not (Test-Path $Bak)) { throw "No se encontró el backup inicial: $Bak" }

            $carpetaBak = Escalar $servidor "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(500))"
            if (-not $carpetaBak) { $carpetaBak = 'C:\HotelRydl\Backups' }
            if (-not (Test-Path $carpetaBak)) { New-Item -ItemType Directory -Path $carpetaBak -Force | Out-Null }

            # 1) Respaldo de la base actual (si existe y se pidió)
            if ($r.Existe -and $Respaldo) {
                $archivo = Join-Path $carpetaBak ("{0}_antes_de_instalar_{1:yyyyMMdd_HHmmss}.bak" -f $NombreBD, (Get-Date))
                Escribir-Log "Respaldando la base actual en $archivo ..."
                Ejecutar $servidor "BACKUP DATABASE [$NombreBD] TO DISK = N'$($archivo.Replace("'", "''"))' WITH FORMAT, INIT, NAME = N'$NombreBD - antes de instalar'"
                $resultado['respaldo'] = $archivo
                Escribir-Log 'Respaldo terminado.'
            }

            # 2) El servicio de SQL Server tiene que poder leer el .bak: se copia a su carpeta de backups
            $bakSql = Join-Path $carpetaBak ([IO.Path]::GetFileName($Bak))
            if ($bakSql -ne $Bak) { Copy-Item $Bak $bakSql -Force }
            Escribir-Log "Backup inicial: $bakSql"

            # 3) Rutas de datos y log de la instancia
            $dataPath = Escalar $servidor "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(500))"
            $logPath = Escalar $servidor "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(500))"
            if (-not $dataPath) {
                $mdf = Escalar $servidor "SELECT TOP 1 physical_name FROM master.sys.master_files WHERE database_id = 1 AND type = 0"
                $dataPath = [IO.Path]::GetDirectoryName($mdf)
            }
            if (-not $logPath) { $logPath = $dataPath }

            $con = New-Object System.Data.SqlClient.SqlConnection (Cadena $servidor)
            $con.Open()
            $movs = @()
            try {
                $cmd = $con.CreateCommand()
                $cmd.CommandText = "RESTORE FILELISTONLY FROM DISK = N'$($bakSql.Replace("'", "''"))'"
                $rd = $cmd.ExecuteReader()
                while ($rd.Read()) {
                    $logico = $rd.GetString(0); $tipo = $rd.GetString(2)
                    if ($tipo -eq 'L') { $dir = $logPath; $ext = '.ldf' } else { $dir = $dataPath; $ext = '.mdf' }
                    $destino = Join-Path $dir ("{0}_{1}{2}" -f $NombreBD, $logico, $ext)
                    $movs += "MOVE N'$($logico.Replace("'", "''"))' TO N'$($destino.Replace("'", "''"))'"
                }
                $rd.Close()
            } finally { $con.Close() }

            # 4) Reemplazo de la base (se cierran las conexiones abiertas)
            if ($r.Existe) {
                Escribir-Log 'Cerrando conexiones a la base actual...'
                Ejecutar $servidor "ALTER DATABASE [$NombreBD] SET SINGLE_USER WITH ROLLBACK IMMEDIATE"
            }
            Escribir-Log 'Restaurando la base inicial...'
            Ejecutar $servidor ("RESTORE DATABASE [$NombreBD] FROM DISK = N'$($bakSql.Replace("'", "''"))' WITH " + ($movs -join ', ') + ', REPLACE, RECOVERY')
            Ejecutar $servidor "ALTER DATABASE [$NombreBD] SET MULTI_USER"

            # 5) Estado de fábrica: se vacían los datos cargados (queda solo lo indispensable + admin)
            if ($Inicial) {
                try {
                    if (-not (Test-Path $Inicial)) { throw "No se encontró el script de base inicial: $Inicial" }
                    Escribir-Log 'Dejando la base en estado inicial (sin datos cargados)...'
                    $sqlIni = [IO.File]::ReadAllText($Inicial, [Text.Encoding]::UTF8)
                    $con = New-Object System.Data.SqlClient.SqlConnection (Cadena $servidor $NombreBD)
                    $con.Open()
                    try {
                        $cmd = $con.CreateCommand(); $cmd.CommandText = $sqlIni; $cmd.CommandTimeout = 600
                        [void]$cmd.ExecuteNonQuery()
                    } finally { $con.Close() }
                    $resultado['inicial'] = '1'
                    Escribir-Log 'Base inicial lista: configuración del sistema + usuario administrador.'
                } catch {
                    # No se deja una base con datos ajenos: se elimina y se informa el error
                    $msg = $_.Exception.Message
                    Escribir-Log "No se pudo dejar la base en estado inicial: $msg"
                    try {
                        [System.Data.SqlClient.SqlConnection]::ClearAllPools()
                        Ejecutar $servidor "ALTER DATABASE [$NombreBD] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$NombreBD]"
                    } catch { }
                    throw "No se pudo preparar la base inicial: $msg"
                }
            }

            # 6) La copia del .bak en la carpeta de SQL Server ya no hace falta
            if ($bakSql -ne $Bak) { Remove-Item $bakSql -Force -ErrorAction SilentlyContinue }
            $resultado['instalada'] = '1'
            Escribir-Log "Base $NombreBD instalada en $servidor."
            Guardar-Salida; exit 0
        }

        'Credenciales' {
            if (-not $r.Existe) { throw "La base $NombreBD no existe." }
            $con = New-Object System.Data.SqlClient.SqlConnection (Cadena $servidor $NombreBD)
            $con.Open()
            try {
                $cmd = $con.CreateCommand()
                $cmd.CommandText = 'SELECT NombreUsuario, DNI, Apellido, ContrasenaHash, DebeChangiarClave FROM Usuarios WHERE IdUsuario = 1'
                $rd = $cmd.ExecuteReader()
                if ($rd.Read()) {
                    $resultado['usuario'] = $rd.GetString(0)
                    $resultado['clave_inicial'] = '{0}{1}' -f $rd.GetString(1), $rd.GetString(2)
                    $v = $rd.GetValue(4)
                    $pend = ($rd.GetString(3) -eq '##PENDING_HASH##') -or (($v -isnot [System.DBNull]) -and [bool]$v)
                    $resultado['clave_pendiente'] = $(if ($pend) { '1' } else { '0' })
                }
                $rd.Close()
            } finally { $con.Close() }
            Guardar-Salida; exit 0
        }
    }
}
catch {
    $resultado['error'] = ($_.Exception.Message -replace "[\r\n]+", ' ')
    Escribir-Log "ERROR: $($resultado['error'])"
    try { if ($servidor) { Ejecutar $servidor "IF DB_ID(N'$NombreBD') IS NOT NULL ALTER DATABASE [$NombreBD] SET MULTI_USER" } } catch { }
    Guardar-Salida
    exit 3
}
