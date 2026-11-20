; ════════════════════════════════════════════════════════════════════════
;  Hotel Rydl — SIH (Sistema Integrado de Hotelería) — HRS-480
;  Instalador y REINSTALADOR (Inno Setup 6)
;
;  Al iniciar pregunta qué se quiere hacer:
;    • INSTALAR   → instala TODO: programa + base de datos HotelRydlDB en estado
;                   de fábrica (sin datos cargados; con roles, permisos, configuración
;                   y el usuario administrador del primer ingreso).
;                   Pide la carpeta de instalación. Si la base ya existe,
;                   ofrece respaldarla y pide doble confirmación antes de reemplazarla.
;    • REINSTALAR → reinstala SOLO el programa en la carpeta donde ya está instalado
;                   (la busca solo). La base de datos y los backups NO se tocan.
;
;  Antes de compilar: ejecutar publicar.bat (genera la carpeta "publicar").
;  Compilar con ISCC.exe o abriendo este archivo en Inno Setup Compiler (F9).
; ════════════════════════════════════════════════════════════════════════

#define MiApp        "Hotel Rydl - SIH"
#define MiVersion    "3.0"
#define MiEmpresa    "Santiago Valentín Rydl - UAI"
#define MiExe        "GUI HRS-480.exe"
#define MiBak        "HotelRydl011026_04.bak"
; El AppId identifica la instalación en Windows: NO cambiarlo entre versiones
#define MiAppId      "6E1B9F3A-2C47-4E8B-A5D2-7F04C1B9E480"
#define ClaveReg     "Software\HotelRydl\SIH"

[Setup]
AppId={{{#MiAppId}}
AppName={#MiApp}
AppVersion={#MiVersion}
AppVerName={#MiApp} {#MiVersion}
AppPublisher={#MiEmpresa}
DefaultDirName={autopf}\Hotel Rydl
DefaultGroupName=Hotel Rydl
UsePreviousAppDir=yes
DisableDirPage=no
DisableProgramGroupPage=no
DisableWelcomePage=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=salida
OutputBaseFilename=Instalador_HotelRydl_v{#MiVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MiApp}
UninstallDisplayIcon={app}\{#MiExe}
CloseApplications=yes
RestartApplications=no
; Al iniciar pregunta el idioma: el del instalador y el que usa el programa al abrirse (idioma.txt)
ShowLanguageDialog=yes
LanguageDetectionMethod=none

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"

[Messages]
spanish.WelcomeLabel2=Este asistente instala o reinstala [name/ver] en el equipo.%n%nEn el próximo paso vas a elegir:%n%n•  INSTALAR: programa + base de datos (instalación completa).%n•  REINSTALAR: solo el programa, conservando la base de datos.%n%nAntes de continuar cerrá Hotel Rydl si está abierto. Necesitás SQL Server (2016 o superior) instalado en este equipo.%n%nEl programa se va a abrir en español.
english.WelcomeLabel2=This wizard installs or reinstalls [name/ver] on this computer.%n%nIn the next step you will choose:%n%n•  INSTALL: program + database (full installation).%n•  REINSTALL: program only, keeping the database.%n%nClose Hotel Rydl before continuing if it is open. SQL Server (2016 or later) must be installed on this computer.%n%nThe program will open in English.
german.WelcomeLabel2=Dieser Assistent installiert [name/ver] auf diesem Computer oder installiert es neu.%n%nIm nächsten Schritt wählen Sie:%n%n•  INSTALLIEREN: Programm + Datenbank (vollständige Installation).%n•  NEU INSTALLIEREN: nur das Programm, die Datenbank bleibt erhalten.%n%nSchließen Sie Hotel Rydl, falls es geöffnet ist. SQL Server (2016 oder neuer) muss auf diesem Computer installiert sein.%n%nDas Programm wird auf Deutsch geöffnet.
english.ExitSetupMessage=The installation is not finished. If you exit now, Hotel Rydl will not be installed.%n%nAre you sure you want to exit?
german.ExitSetupMessage=Die Installation ist nicht abgeschlossen. Wenn Sie jetzt beenden, wird Hotel Rydl nicht installiert.%n%nMöchten Sie wirklich beenden?
spanish.ExitSetupMessage=La instalación no terminó. Si salís ahora, Hotel Rydl no queda instalado.%n%n¿Estás seguro de que querés salir?

[CustomMessages]
spanish.TareaEscritorio=Crear un acceso directo en el escritorio
english.TareaEscritorio=Create a desktop shortcut
german.TareaEscritorio=Verknüpfung auf dem Desktop erstellen
spanish.GrupoAccesos=Accesos directos:
english.GrupoAccesos=Shortcuts:
german.GrupoAccesos=Verknüpfungen:
spanish.AbrirAhora=Abrir Hotel Rydl ahora
english.AbrirAhora=Open Hotel Rydl now
german.AbrirAhora=Hotel Rydl jetzt öffnen

[Tasks]
Name: "escritorio"; Description: "{cm:TareaEscritorio}"; GroupDescription: "{cm:GrupoAccesos}"

[InstallDelete]
; REINSTALAR: se borran los binarios viejos del programa (no los datos, backups ni configuración)
Type: files;          Name: "{app}\*.dll";            Check: EsReinstalar
Type: files;          Name: "{app}\*.pdb";            Check: EsReinstalar
Type: files;          Name: "{app}\GUI HRS-480.*";    Check: EsReinstalar
Type: filesandordirs; Name: "{app}\runtimes";         Check: EsReinstalar

[Files]
; Programa publicado (self-contained, no requiere instalar .NET)
Source: "publicar\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "conexion.txt,correo.txt,*.pdb,Backups\*"
; Base inicial: el .bak aporta SOLO la estructura (tablas, triggers, procedimientos y catálogos con los Id
; que espera el código). BaseInicial.sql la deja de fábrica. Ninguno de los dos queda en la carpeta del programa.
Source: "..\Backups\{#MiBak}"; DestDir: "{tmp}"; Flags: dontcopy
Source: "BaseInicial.sql"; DestDir: "{tmp}"; Flags: dontcopy
; Scripts SQL (el programa los aplica solo al iniciar; quedan para ejecutarlos a mano si hace falta)
Source: "..\Scripts\*.sql"; DestDir: "{app}\Scripts"; Flags: ignoreversion
; Herramienta de base de datos y manual
Source: "BaseDatos.ps1"; DestDir: "{app}\Instalador"; Flags: ignoreversion
Source: "LEEME_Instalador.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme

[UninstallDelete]
Type: files; Name: "{app}\idioma.txt"

[Dirs]
Name: "{app}\Backups"; Flags: uninsneveruninstall
Name: "{app}\Logs"

[Icons]
Name: "{group}\Hotel Rydl"; Filename: "{app}\{#MiExe}"; WorkingDir: "{app}"
Name: "{group}\Manual de instalación"; Filename: "{app}\LEEME_Instalador.txt"
Name: "{group}\Desinstalar Hotel Rydl"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Hotel Rydl"; Filename: "{app}\{#MiExe}"; WorkingDir: "{app}"; Tasks: escritorio

[Registry]
; Lo usa el REINSTALADOR para encontrar la carpeta de instalación
Root: HKLM; Subkey: "{#ClaveReg}"; ValueType: string; ValueName: "InstallDir"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "{#ClaveReg}"; ValueType: string; ValueName: "Version"; ValueData: "{#MiVersion}"
Root: HKLM; Subkey: "{#ClaveReg}"; ValueType: string; ValueName: "UltimaOperacion"; ValueData: "{code:NombreModo}"

[Run]
Filename: "{app}\{#MiExe}"; Description: "{cm:AbrirAhora}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Logs"

[Code]
const
  BD_NO_EXISTE = 0;
  BD_EXISTE    = 1;
  SIN_SQL      = 2;
  ERROR_BD     = 3;

var
  PaginaModo: TInputOptionWizardPage;      // Instalar / Reinstalar
  PaginaReinst: TOutputMsgWizardPage;      // dónde se encontró la instalación
  PaginaBD: TInputOptionWizardPage;        // base existente: respaldar antes de reemplazar
  HayInstalacion: Boolean;
  CarpetaPrevia: String;
  Servidor: String;
  BaseExiste: Boolean;
  ResultadoBD: String;                     // texto para la página final
  UsuarioAdmin, ClaveAdmin: String;
  ClavePendiente: Boolean;

{ ───────────────────────── utilidades ───────────────────────── }

function EsReinstalar: Boolean;
begin
  if PaginaModo = nil then
    Result := False
  else
    Result := PaginaModo.Values[1];
end;

function EsInstalar: Boolean;
begin
  Result := not EsReinstalar;
end;

function NombreModo(Param: String): String;
begin
  if EsReinstalar then Result := 'Reinstalación' else Result := 'Instalación completa';
end;

function Confirmar(const Texto: String): Boolean;
begin
  // botón "No" por defecto: hay que elegir "Sí" a conciencia
  Result := MsgBox(Texto, mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
end;

function ExeEn(const Dir: String): Boolean;
begin
  Result := (Dir <> '') and FileExists(AddBackslash(Dir) + '{#MiExe}');
end;

function LeerValor(const Archivo, Clave: String): String;
var
  Lineas: TArrayOfString;
  I, P: Integer;
begin
  Result := '';
  if not LoadStringsFromFile(Archivo, Lineas) then Exit;
  for I := 0 to GetArrayLength(Lineas) - 1 do
  begin
    P := Pos('=', Lineas[I]);
    // tolera un BOM al comienzo de la primera línea
    if (P > 0) and (Pos(Clave + '=', Lineas[I]) > 0) and (Pos(Clave + '=', Lineas[I]) <= 4) then
    begin
      Result := Copy(Lineas[I], P + 1, Length(Lineas[I]));
      Exit;
    end;
  end;
end;

{ Ejecuta BaseDatos.ps1 y devuelve su código de salida }
function EjecutarBD(const Accion, Extra, Salida, Log: String): Integer;
var
  Ps, Params: String;
  Codigo: Integer;
begin
  ExtractTemporaryFile('BaseDatos.ps1');
  Ps := ExpandConstant('{tmp}\BaseDatos.ps1');
  Params := '-NoProfile -ExecutionPolicy Bypass -File "' + Ps + '" -Accion ' + Accion +
            ' -Salida "' + Salida + '" -Log "' + Log + '" ' + Extra;
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Params, '',
              SW_HIDE, ewWaitUntilTerminated, Codigo) then
    Codigo := ERROR_BD;
  Result := Codigo;
end;

{ ¿Hay SQL Server? ¿Existe HotelRydlDB? }
function VerificarSQL: Integer;
var
  Salida, CarpetaApp: String;
begin
  Salida := ExpandConstant('{tmp}\verificar.txt');
  DeleteFile(Salida);
  CarpetaApp := WizardForm.DirEdit.Text;
  WizardForm.NextButton.Enabled := False;
  try
    Result := EjecutarBD('Verificar', '-CarpetaApp "' + CarpetaApp + '"', Salida, ExpandConstant('{tmp}\basedatos.log'));
  finally
    WizardForm.NextButton.Enabled := True;
  end;
  Servidor := LeerValor(Salida, 'servidor');
  BaseExiste := (Result = BD_EXISTE);
end;

{ Busca la instalación existente: registro propio, registro de Inno Setup y carpetas habituales }
function BuscarInstalacion(var Dir: String): Boolean;
var
  Clave: String;
begin
  Result := False;
  Clave := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{' + '{#MiAppId}' + '}_is1';
  if RegQueryStringValue(HKLM64, '{#ClaveReg}', 'InstallDir', Dir) and ExeEn(Dir) then begin Result := True; Exit; end;
  if RegQueryStringValue(HKLM32, '{#ClaveReg}', 'InstallDir', Dir) and ExeEn(Dir) then begin Result := True; Exit; end;
  if RegQueryStringValue(HKLM64, Clave, 'Inno Setup: App Path', Dir) and ExeEn(Dir) then begin Result := True; Exit; end;
  if RegQueryStringValue(HKLM32, Clave, 'Inno Setup: App Path', Dir) and ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := ExpandConstant('{autopf}\Hotel Rydl');            if ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := ExpandConstant('{autopf32}\Hotel Rydl');          if ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := ExpandConstant('{autopf}\Hoteleria Rydl');        if ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := ExpandConstant('{autopf32}\Hoteleria Rydl');      if ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := 'C:\HotelRydl';                                   if ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := ExpandConstant('{localappdata}\Programs\Hotel Rydl'); if ExeEn(Dir) then begin Result := True; Exit; end;
  Dir := '';
end;

{ ───────────────────────── páginas ───────────────────────── }

procedure InitializeWizard;
begin
  HayInstalacion := BuscarInstalacion(CarpetaPrevia);

  PaginaModo := CreateInputOptionPage(wpWelcome,
    '¿Qué querés hacer?',
    'Elegí si vas a instalar el sistema completo o reinstalar solo el programa.',
    'INSTALAR copia el programa y crea la base de datos HotelRydlDB (si ya existe, se reemplaza). ' +
    'REINSTALAR vuelve a copiar el programa en la carpeta donde ya está instalado y CONSERVA la base de datos.',
    True, False);
  PaginaModo.Add('Instalar  (programa + base de datos)');
  PaginaModo.Add('Reinstalar  (solo el programa, conserva la base de datos)');
  if HayInstalacion then
  begin
    PaginaModo.Values[1] := True;     // si ya está instalado, lo más probable es reinstalar
    PaginaModo.SubCaptionLabel.Caption := PaginaModo.SubCaptionLabel.Caption + #13#10#13#10 +
      'Se encontró Hotel Rydl instalado en: ' + CarpetaPrevia;
  end
  else
    PaginaModo.Values[0] := True;

  PaginaReinst := CreateOutputMsgPage(PaginaModo.ID,
    'Reinstalación',
    'Se va a reinstalar el programa en la carpeta donde ya está instalado.',
    '');

  PaginaBD := CreateInputOptionPage(wpSelectDir,
    'La base de datos ya existe',
    'Se encontró la base HotelRydlDB en este equipo.',
    'La instalación completa REEMPLAZA la base de datos por la base inicial del sistema (sin datos cargados): ' +
    'se pierden las reservas, huéspedes, insumos, usuarios y demás datos cargados.' + #13#10#13#10 +
    'Si solo querés actualizar el programa, volvé atrás y elegí REINSTALAR.',
    False, False);
  PaginaBD.Add('Hacer un backup de la base actual antes de reemplazarla (recomendado)');
  PaginaBD.Values[0] := True;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if EsReinstalar then
    Result := (PageID = wpSelectDir) or (PageID = wpSelectProgramGroup) or (PageID = PaginaBD.ID)
  else
    Result := (PageID = PaginaReinst.ID) or ((PageID = PaginaBD.ID) and not BaseExiste);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = PaginaReinst.ID then
    PaginaReinst.MsgLabel.Caption :=
      'Carpeta de instalación encontrada:' + #13#10 + '    ' + WizardForm.DirEdit.Text + #13#10#13#10 +
      'Qué se hace:' + #13#10 +
      '    •  Se reemplazan los archivos del programa por esta versión ({#MiVersion}).' + #13#10 +
      '    •  Se actualizan los scripts SQL y los accesos directos.' + #13#10#13#10 +
      'Qué NO se toca:' + #13#10 +
      '    •  La base de datos HotelRydlDB (todos tus datos se conservan).' + #13#10 +
      '    •  Tus backups y los archivos conexion.txt y correo.txt.' + #13#10#13#10 +
      'Base de datos: ' + Servidor + ' / HotelRydlDB';

  if CurPageID = wpFinished then
  begin
    if EsReinstalar then
      WizardForm.FinishedLabel.Caption :=
        'Hotel Rydl se reinstaló correctamente en:' + #13#10 + WizardForm.DirEdit.Text + #13#10#13#10 +
        'La base de datos HotelRydlDB se conservó sin cambios. Ingresá con tu usuario y contraseña de siempre.'
    else
      WizardForm.FinishedLabel.Caption :=
        'Hotel Rydl se instaló en:' + #13#10 + WizardForm.DirEdit.Text + #13#10#13#10 + ResultadoBD;
    // el texto es más largo que el original: se acomodan el rótulo y la lista de opciones
    WizardForm.AdjustLabelHeight(WizardForm.FinishedLabel);
    WizardForm.RunList.Top := WizardForm.FinishedLabel.Top + WizardForm.FinishedLabel.Height + ScaleY(12);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Dir: String;
  Codigo: Integer;
begin
  Result := True;

  { ── paso 1: modo elegido ── }
  if CurPageID = PaginaModo.ID then
  begin
    if EsReinstalar then
    begin
      if not HayInstalacion then
      begin
        if MsgBox('No se encontró Hotel Rydl instalado en este equipo.' + #13#10#13#10 +
                  '¿Querés indicar a mano la carpeta donde está instalado?',
                  mbConfirmation, MB_YESNO) = IDNO then
        begin
          MsgBox('Para instalar el sistema por primera vez elegí la opción INSTALAR.', mbInformation, MB_OK);
          Result := False; Exit;
        end;
        Dir := ExpandConstant('{autopf}');
        if not BrowseForFolder('Elegí la carpeta donde está instalado Hotel Rydl (la que contiene {#MiExe}):', Dir, False) then
        begin
          Result := False; Exit;
        end;
        if not ExeEn(Dir) then
        begin
          MsgBox('En esa carpeta no está el programa ({#MiExe}).' + #13#10 + 'Elegí la carpeta correcta o usá la opción INSTALAR.', mbError, MB_OK);
          Result := False; Exit;
        end;
        CarpetaPrevia := Dir;
        HayInstalacion := True;
      end;
      WizardForm.DirEdit.Text := CarpetaPrevia;

      Codigo := VerificarSQL;
      if Codigo = SIN_SQL then
      begin
        if not Confirmar('No se pudo conectar con SQL Server.' + #13#10#13#10 +
                         'El programa se puede reinstalar igual, pero no va a funcionar hasta que SQL Server esté disponible.' + #13#10#13#10 +
                         '¿Estás seguro de que querés continuar?') then
        begin
          Result := False; Exit;
        end;
      end
      else if Codigo = BD_NO_EXISTE then
      begin
        if not Confirmar('No se encontró la base de datos HotelRydlDB en ' + Servidor + '.' + #13#10#13#10 +
                         'La reinstalación no crea la base: sin ella el programa no va a poder iniciar.' + #13#10 +
                         'Si querés instalar todo, volvé y elegí INSTALAR.' + #13#10#13#10 +
                         '¿Estás seguro de que querés continuar con la reinstalación?') then
        begin
          Result := False; Exit;
        end;
      end;

      Result := Confirmar('¿Estás seguro de que querés REINSTALAR Hotel Rydl en esta carpeta?' + #13#10#13#10 +
                          '    ' + CarpetaPrevia + #13#10#13#10 +
                          'Se reemplazan los archivos del programa. La base de datos se conserva.');
    end
    else
    begin
      if HayInstalacion then
        if not Confirmar('Hotel Rydl ya está instalado en:' + #13#10 + '    ' + CarpetaPrevia + #13#10#13#10 +
                         'INSTALAR hace una instalación COMPLETA: también instala la base de datos y, si ya existe, la REEMPLAZA.' + #13#10 +
                         'Si solo querés actualizar el programa, elegí REINSTALAR.' + #13#10#13#10 +
                         '¿Estás seguro de que querés hacer una instalación completa?') then
        begin
          Result := False; Exit;
        end;

      Codigo := VerificarSQL;
      if (Codigo = SIN_SQL) or (Codigo = ERROR_BD) then
      begin
        MsgBox('No se encontró SQL Server en este equipo, o tu usuario de Windows no tiene permiso para conectarse.' + #13#10#13#10 +
               'Instalá SQL Server 2016 o superior (por ejemplo SQL Server Express) y volvé a ejecutar el instalador.' + #13#10 +
               'Detalle en: ' + ExpandConstant('{tmp}\basedatos.log'),
               mbCriticalError, MB_OK);
        Result := False; Exit;
      end;
    end;
  end;

  { ── paso 2 (instalar): carpeta elegida ── }
  if (CurPageID = wpSelectDir) and EsInstalar then
    Result := Confirmar('¿Estás seguro de que querés instalar Hotel Rydl en esta carpeta?' + #13#10#13#10 +
                        '    ' + WizardForm.DirEdit.Text);

  { ── paso 3 (instalar con base existente): doble confirmación ── }
  if CurPageID = PaginaBD.ID then
  begin
    if not Confirmar('ATENCIÓN: se van a BORRAR todos los datos actuales de HotelRydlDB' + #13#10 +
                     '(reservas, huéspedes, pagos, insumos, órdenes, usuarios y bitácora)' + #13#10 +
                     'y se reemplazan por la base inicial del sistema.' + #13#10#13#10 +
                     '¿Estás seguro?') then
    begin
      Result := False; Exit;
    end;
    if not PaginaBD.Values[0] then
      if not Confirmar('Elegiste NO hacer un backup antes de reemplazar la base.' + #13#10 +
                       'Si continuás, los datos actuales no se van a poder recuperar.' + #13#10#13#10 +
                       '¿Estás seguro de continuar sin backup?') then
      begin
        Result := False; Exit;
      end;
    Result := Confirmar('Última confirmación: ¿reemplazar la base de datos HotelRydlDB?' + #13#10 +
                        'Esta acción no se puede deshacer.');
  end;

  { ── listo para instalar ── }
  if CurPageID = wpReady then
    if EsReinstalar then
      Result := Confirmar('Se va a reinstalar el programa en ' + WizardForm.DirEdit.Text + '.' + #13#10#13#10 + '¿Comenzar?')
    else
      Result := Confirmar('Se va a instalar el programa en ' + WizardForm.DirEdit.Text + ' y la base de datos HotelRydlDB.' + #13#10#13#10 + '¿Comenzar?');
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := 'Operación:' + NewLine + Space + NombreModo('') + NewLine + NewLine +
            MemoDirInfo + NewLine + NewLine;
  if EsReinstalar then
    Result := Result + 'Base de datos:' + NewLine + Space + 'Se conserva sin cambios (' + Servidor + ')' + NewLine + NewLine
  else
  begin
    Result := Result + 'Base de datos:' + NewLine + Space + 'Servidor: ' + Servidor + NewLine;
    if BaseExiste then
    begin
      Result := Result + Space + 'HotelRydlDB YA EXISTE: se reemplaza por la base inicial' + NewLine;
      if PaginaBD.Values[0] then
        Result := Result + Space + 'Antes se hace un backup de la base actual' + NewLine
      else
        Result := Result + Space + 'SIN backup previo' + NewLine;
    end
    else
      Result := Result + Space + 'Se crea HotelRydlDB en estado inicial (sin datos cargados)' + NewLine;
    Result := Result + Space + 'Se crea el usuario administrador para el primer ingreso' + NewLine;
    Result := Result + NewLine;
    if MemoGroupInfo <> '' then Result := Result + MemoGroupInfo + NewLine + NewLine;
  end;
  if MemoTasksInfo <> '' then Result := Result + MemoTasksInfo + NewLine;
end;

{ ───────────────────────── instalación de la base ───────────────────────── }

procedure InstalarBase;
var
  Salida, Extra, Respaldo, Log: String;
  Codigo: Integer;
begin
  Log := ExpandConstant('{app}\Logs\instalacion_bd.log');
  Salida := ExpandConstant('{tmp}\instalar.txt');
  ExtractTemporaryFile('{#MiBak}');
  ExtractTemporaryFile('BaseInicial.sql');
  Extra := '-CarpetaApp "' + ExpandConstant('{app}') + '" -Bak "' + ExpandConstant('{tmp}\{#MiBak}') + '"' +
           ' -Inicial "' + ExpandConstant('{tmp}\BaseInicial.sql') + '"';
  if BaseExiste and PaginaBD.Values[0] then Extra := Extra + ' -Respaldo';

  WizardForm.StatusLabel.Caption := 'Instalando la base de datos HotelRydlDB (puede tardar unos minutos)...';
  Codigo := EjecutarBD('Instalar', Extra, Salida, Log);

  if Codigo = 0 then
  begin
    ResultadoBD := 'Base de datos HotelRydlDB instalada en ' + Servidor + '.';
    Respaldo := LeerValor(Salida, 'respaldo');
    if Respaldo <> '' then
      ResultadoBD := ResultadoBD + #13#10 + 'Backup de la base anterior: ' + Respaldo;

    Salida := ExpandConstant('{tmp}\credenciales.txt');
    if EjecutarBD('Credenciales', '-CarpetaApp "' + ExpandConstant('{app}') + '"', Salida, Log) = 0 then
    begin
      UsuarioAdmin := LeerValor(Salida, 'usuario');
      ClaveAdmin := LeerValor(Salida, 'clave_inicial');
      ClavePendiente := LeerValor(Salida, 'clave_pendiente') = '1';
      if UsuarioAdmin <> '' then
      begin
        ResultadoBD := ResultadoBD + #13#10#13#10 + 'Primer ingreso (anotalo antes de cerrar):' + #13#10 +
                       '    Usuario: ' + UsuarioAdmin + #13#10;
        if ClavePendiente then
        begin
          ResultadoBD := ResultadoBD + '    Contraseña inicial: ' + ClaveAdmin + '  (DNI + Apellido)' + #13#10 +
                         'El sistema te va a pedir que la cambies en el primer ingreso.';
          MsgBox('Datos para el primer ingreso a Hotel Rydl:' + #13#10#13#10 +
                 'Usuario: ' + UsuarioAdmin + #13#10 +
                 'Contraseña inicial: ' + ClaveAdmin + '   (DNI + Apellido)' + #13#10#13#10 +
                 'Anotalos antes de cerrar este mensaje. En el primer ingreso el sistema te va a pedir cambiar la contraseña.',
                 mbInformation, MB_OK);
        end
        else
        begin
          ResultadoBD := ResultadoBD + '    Contraseña: la del administrador que ya tenía la base inicial.';
          MsgBox('Datos para el primer ingreso a Hotel Rydl:' + #13#10#13#10 +
                 'Usuario: ' + UsuarioAdmin + #13#10 +
                 'Contraseña: la del administrador guardada en la base inicial.' + #13#10#13#10 +
                 'Anotá el usuario antes de cerrar este mensaje.', mbInformation, MB_OK);
        end;
      end;
    end;
  end
  else
  begin
    ResultadoBD := 'ATENCIÓN: no se pudo instalar la base de datos (' + LeerValor(Salida, 'error') + ').' + #13#10 +
                   'Volvé a ejecutar el instalador en modo INSTALAR. Detalle en ' + Log;
    MsgBox('No se pudo instalar la base de datos HotelRydlDB.' + #13#10#13#10 +
           LeerValor(Salida, 'error') + #13#10#13#10 +
           'El programa quedó instalado, pero sin base de datos. Revisá que SQL Server esté iniciado y volvé a ejecutar el instalador en modo INSTALAR.' + #13#10 +
           'Detalle en: ' + Log, mbError, MB_OK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  IdiomaPrograma: String;
begin
  if CurStep = ssPostInstall then
  begin
    { idioma con el que abre el programa: el elegido al iniciar el instalador }
    if ActiveLanguage = 'english' then IdiomaPrograma := 'ingles'
    else if ActiveLanguage = 'german' then IdiomaPrograma := 'aleman'
    else IdiomaPrograma := 'espanol';
    SaveStringToFile(ExpandConstant('{app}\idioma.txt'), IdiomaPrograma, False);
  end;
  if (CurStep = ssPostInstall) and EsInstalar then
    InstalarBase;
end;

{ ───────────────────────── desinstalación ───────────────────────── }

function InitializeUninstall: Boolean;
begin
  Result := MsgBox('¿Estás seguro de que querés desinstalar Hotel Rydl?' + #13#10#13#10 +
                   'Se borra el programa. La base de datos HotelRydlDB y la carpeta Backups NO se borran.',
                   mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
end;
