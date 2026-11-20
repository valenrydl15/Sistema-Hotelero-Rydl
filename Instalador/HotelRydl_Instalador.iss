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
; Al iniciar pregunta el idioma del instalador (el programa siempre abre en español; se cambia desde Cambiar idioma)
ShowLanguageDialog=yes
LanguageDetectionMethod=none

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"

[Messages]
spanish.WelcomeLabel2=Este asistente instala o reinstala [name/ver] en el equipo.%n%nEn el próximo paso vas a elegir:%n%n•  INSTALAR: programa + base de datos (instalación completa).%n•  REINSTALAR: solo el programa, conservando la base de datos.%n%nAntes de continuar cerrá Hotel Rydl si está abierto. Necesitás SQL Server (2016 o superior) instalado en este equipo.
english.WelcomeLabel2=This wizard installs or reinstalls [name/ver] on this computer.%n%nIn the next step you will choose:%n%n•  INSTALL: program + database (full installation).%n•  REINSTALL: program only, keeping the database.%n%nClose Hotel Rydl before continuing if it is open. SQL Server (2016 or later) must be installed on this computer.
german.WelcomeLabel2=Dieser Assistent installiert [name/ver] auf diesem Computer oder installiert es neu.%n%nIm nächsten Schritt wählen Sie:%n%n•  INSTALLIEREN: Programm + Datenbank (vollständige Installation).%n•  NEU INSTALLIEREN: nur das Programm, die Datenbank bleibt erhalten.%n%nSchließen Sie Hotel Rydl, falls es geöffnet ist. SQL Server (2016 oder neuer) muss auf diesem Computer installiert sein.
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
; ── textos de las pantallas propias del instalador ([Code]); %n = salto de línea, %1/%2 = datos ──
spanish.ModoReinst=Reinstalación
spanish.ModoInst=Instalación completa
spanish.PagModoTit=¿Qué querés hacer?
spanish.PagModoSub=Elegí si vas a instalar el sistema completo o reinstalar solo el programa.
spanish.PagModoTxt=INSTALAR copia el programa y crea la base de datos HotelRydlDB (si ya existe, se reemplaza). REINSTALAR vuelve a copiar el programa en la carpeta donde ya está instalado y CONSERVA la base de datos.
spanish.OpcInst=Instalar  (programa + base de datos)
spanish.OpcReinst=Reinstalar  (solo el programa, conserva la base de datos)
spanish.EncontradoEn=Se encontró Hotel Rydl instalado en: %1
spanish.PagReinstTit=Reinstalación
spanish.PagReinstSub=Se va a reinstalar el programa en la carpeta donde ya está instalado.
spanish.PagBDTit=La base de datos ya existe
spanish.PagBDSub=Se encontró la base HotelRydlDB en este equipo.
spanish.PagBDTxt=La instalación completa REEMPLAZA la base de datos por la base inicial del sistema (sin datos cargados): se pierden las reservas, huéspedes, insumos, usuarios y demás datos cargados.%n%nSi solo querés actualizar el programa, volvé atrás y elegí REINSTALAR.
spanish.OpcBackup=Hacer un backup de la base actual antes de reemplazarla (recomendado)
spanish.ReinstDetalle=Carpeta de instalación encontrada:%n    %1%n%nQué se hace:%n    •  Se reemplazan los archivos del programa por esta versión ({#MiVersion}).%n    •  Se actualizan los scripts SQL y los accesos directos.%n%nQué NO se toca:%n    •  La base de datos HotelRydlDB (todos tus datos se conservan).%n    •  Tus backups y los archivos conexion.txt y correo.txt.%n%nBase de datos: %2 / HotelRydlDB
spanish.FinReinst=Hotel Rydl se reinstaló correctamente en:%n%1%n%nLa base de datos HotelRydlDB se conservó sin cambios. Ingresá con tu usuario y contraseña de siempre.
spanish.FinInst=Hotel Rydl se instaló en:%n%1
spanish.NoEncontrado=No se encontró Hotel Rydl instalado en este equipo.%n%n¿Querés indicar a mano la carpeta donde está instalado?
spanish.UsarInstalar=Para instalar el sistema por primera vez elegí la opción INSTALAR.
spanish.ElegirCarpeta=Elegí la carpeta donde está instalado Hotel Rydl (la que contiene {#MiExe}):
spanish.CarpetaMal=En esa carpeta no está el programa ({#MiExe}).%nElegí la carpeta correcta o usá la opción INSTALAR.
spanish.ReinstSinSQL=No se pudo conectar con SQL Server.%n%nEl programa se puede reinstalar igual, pero no va a funcionar hasta que SQL Server esté disponible.%n%n¿Estás seguro de que querés continuar?
spanish.ReinstSinBD=No se encontró la base de datos HotelRydlDB en %1.%n%nLa reinstalación no crea la base: sin ella el programa no va a poder iniciar.%nSi querés instalar todo, volvé y elegí INSTALAR.%n%n¿Estás seguro de que querés continuar con la reinstalación?
spanish.ConfReinst=¿Estás seguro de que querés REINSTALAR Hotel Rydl en esta carpeta?%n%n    %1%n%nSe reemplazan los archivos del programa. La base de datos se conserva.
spanish.YaInstalado=Hotel Rydl ya está instalado en:%n    %1%n%nINSTALAR hace una instalación COMPLETA: también instala la base de datos y, si ya existe, la REEMPLAZA.%nSi solo querés actualizar el programa, elegí REINSTALAR.%n%n¿Estás seguro de que querés hacer una instalación completa?
spanish.SinSQL=No se encontró SQL Server en este equipo, o tu usuario de Windows no tiene permiso para conectarse.%n%nInstalá SQL Server 2016 o superior (por ejemplo SQL Server Express) y volvé a ejecutar el instalador.%nDetalle en: %1
spanish.ConfCarpeta=¿Estás seguro de que querés instalar Hotel Rydl en esta carpeta?%n%n    %1
spanish.ConfBorrar=ATENCIÓN: se van a BORRAR todos los datos actuales de HotelRydlDB%n(reservas, huéspedes, pagos, insumos, órdenes, usuarios y bitácora)%ny se reemplazan por la base inicial del sistema.%n%n¿Estás seguro?
spanish.ConfSinBackup=Elegiste NO hacer un backup antes de reemplazar la base.%nSi continuás, los datos actuales no se van a poder recuperar.%n%n¿Estás seguro de continuar sin backup?
spanish.ConfUltima=Última confirmación: ¿reemplazar la base de datos HotelRydlDB?%nEsta acción no se puede deshacer.
spanish.ComenzarReinst=Se va a reinstalar el programa en %1.%n%n¿Comenzar?
spanish.ComenzarInst=Se va a instalar el programa en %1 y la base de datos HotelRydlDB.%n%n¿Comenzar?
spanish.MemoOperacion=Operación:
spanish.MemoBD=Base de datos:
spanish.MemoConserva=Se conserva sin cambios (%1)
spanish.MemoServidor=Servidor: %1
spanish.MemoExiste=HotelRydlDB YA EXISTE: se reemplaza por la base inicial
spanish.MemoBackup=Antes se hace un backup de la base actual
spanish.MemoSinBackup=SIN backup previo
spanish.MemoCrea=Se crea HotelRydlDB en estado inicial (sin datos cargados)
spanish.MemoAdmin=Se crea el usuario administrador para el primer ingreso
spanish.EstadoBD=Instalando la base de datos HotelRydlDB (puede tardar unos minutos)...
spanish.BDInstalada=Base de datos HotelRydlDB instalada en %1.
spanish.BDRespaldo=Backup de la base anterior: %1
spanish.PrimerIngreso=Primer ingreso (anotalo antes de cerrar):%n    Usuario: %1
spanish.ClaveInicialFin=    Contraseña inicial: %1  (DNI + Apellido)%nEl sistema te va a pedir que la cambies en el primer ingreso.
spanish.CartelClave=Datos para el primer ingreso a Hotel Rydl:%n%nUsuario: %1%nContraseña inicial: %2   (DNI + Apellido)%n%nAnotalos antes de cerrar este mensaje. En el primer ingreso el sistema te va a pedir cambiar la contraseña.
spanish.ClaveExistenteFin=    Contraseña: la del administrador que ya tenía la base inicial.
spanish.CartelClaveExistente=Datos para el primer ingreso a Hotel Rydl:%n%nUsuario: %1%nContraseña: la del administrador guardada en la base inicial.%n%nAnotá el usuario antes de cerrar este mensaje.
spanish.BDErrorFin=ATENCIÓN: no se pudo instalar la base de datos (%1).%nVolvé a ejecutar el instalador en modo INSTALAR. Detalle en %2
spanish.BDErrorCartel=No se pudo instalar la base de datos HotelRydlDB.%n%n%1%n%nEl programa quedó instalado, pero sin base de datos. Revisá que SQL Server esté iniciado y volvé a ejecutar el instalador en modo INSTALAR.%nDetalle en: %2
spanish.ConfDesinstalar=¿Estás seguro de que querés desinstalar Hotel Rydl?%n%nSe borra el programa. La base de datos HotelRydlDB y la carpeta Backups NO se borran.
spanish.IconoManual=Manual de instalación
spanish.IconoDesinstalar=Desinstalar Hotel Rydl
english.ModoReinst=Reinstallation
english.ModoInst=Full installation
english.PagModoTit=What do you want to do?
english.PagModoSub=Choose whether to install the complete system or reinstall only the program.
english.PagModoTxt=INSTALL copies the program and creates the HotelRydlDB database (if it already exists, it is replaced). REINSTALL copies the program again into the folder where it is already installed and KEEPS the database.
english.OpcInst=Install  (program + database)
english.OpcReinst=Reinstall  (program only, keeps the database)
english.EncontradoEn=Hotel Rydl was found installed in: %1
english.PagReinstTit=Reinstallation
english.PagReinstSub=The program will be reinstalled in the folder where it is already installed.
english.PagBDTit=The database already exists
english.PagBDSub=The HotelRydlDB database was found on this computer.
english.PagBDTxt=The full installation REPLACES the database with the initial system database (no data loaded): bookings, guests, supplies, users and all other loaded data are lost.%n%nIf you only want to update the program, go back and choose REINSTALL.
english.OpcBackup=Back up the current database before replacing it (recommended)
english.ReinstDetalle=Installation folder found:%n    %1%n%nWhat is done:%n    •  The program files are replaced by this version ({#MiVersion}).%n    •  The SQL scripts and shortcuts are updated.%n%nWhat is NOT touched:%n    •  The HotelRydlDB database (all your data is kept).%n    •  Your backups and the conexion.txt and correo.txt files.%n%nDatabase: %2 / HotelRydlDB
english.FinReinst=Hotel Rydl was successfully reinstalled in:%n%1%n%nThe HotelRydlDB database was kept unchanged. Log in with your usual user and password.
english.FinInst=Hotel Rydl was installed in:%n%1
english.NoEncontrado=Hotel Rydl was not found on this computer.%n%nDo you want to select the installation folder manually?
english.UsarInstalar=To install the system for the first time, choose INSTALL.
english.ElegirCarpeta=Choose the folder where Hotel Rydl is installed (the one containing {#MiExe}):
english.CarpetaMal=The program ({#MiExe}) is not in that folder.%nChoose the correct folder or use INSTALL.
english.ReinstSinSQL=Could not connect to SQL Server.%n%nThe program can still be reinstalled, but it will not work until SQL Server is available.%n%nAre you sure you want to continue?
english.ReinstSinBD=The HotelRydlDB database was not found on %1.%n%nThe reinstallation does not create the database: without it the program cannot start.%nIf you want to install everything, go back and choose INSTALL.%n%nAre you sure you want to continue with the reinstallation?
english.ConfReinst=Are you sure you want to REINSTALL Hotel Rydl in this folder?%n%n    %1%n%nThe program files are replaced. The database is kept.
english.YaInstalado=Hotel Rydl is already installed in:%n    %1%n%nINSTALL performs a FULL installation: it also installs the database and, if it already exists, REPLACES it.%nIf you only want to update the program, choose REINSTALL.%n%nAre you sure you want to perform a full installation?
english.SinSQL=SQL Server was not found on this computer, or your Windows user does not have permission to connect.%n%nInstall SQL Server 2016 or later (for example SQL Server Express) and run the installer again.%nDetails in: %1
english.ConfCarpeta=Are you sure you want to install Hotel Rydl in this folder?%n%n    %1
english.ConfBorrar=WARNING: ALL current data in HotelRydlDB will be DELETED%n(bookings, guests, payments, supplies, orders, users and log)%nand replaced by the initial system database.%n%nAre you sure?
english.ConfSinBackup=You chose NOT to back up before replacing the database.%nIf you continue, the current data cannot be recovered.%n%nAre you sure you want to continue without a backup?
english.ConfUltima=Final confirmation: replace the HotelRydlDB database?%nThis action cannot be undone.
english.ComenzarReinst=The program will be reinstalled in %1.%n%nStart?
english.ComenzarInst=The program will be installed in %1 together with the HotelRydlDB database.%n%nStart?
english.MemoOperacion=Operation:
english.MemoBD=Database:
english.MemoConserva=Kept unchanged (%1)
english.MemoServidor=Server: %1
english.MemoExiste=HotelRydlDB ALREADY EXISTS: it is replaced by the initial database
english.MemoBackup=A backup of the current database is made first
english.MemoSinBackup=WITHOUT a previous backup
english.MemoCrea=HotelRydlDB is created in its initial state (no data loaded)
english.MemoAdmin=The administrator user for the first login is created
english.EstadoBD=Installing the HotelRydlDB database (this may take a few minutes)...
english.BDInstalada=HotelRydlDB database installed on %1.
english.BDRespaldo=Backup of the previous database: %1
english.PrimerIngreso=First login (write it down before closing):%n    User: %1
english.ClaveInicialFin=    Initial password: %1  (ID + Surname)%nThe system will ask you to change it at the first login.
english.CartelClave=Details for the first login to Hotel Rydl:%n%nUser: %1%nInitial password: %2   (ID + Surname)%n%nWrite them down before closing this message. At the first login the system will ask you to change the password.
english.ClaveExistenteFin=    Password: the one of the administrator already stored in the initial database.
english.CartelClaveExistente=Details for the first login to Hotel Rydl:%n%nUser: %1%nPassword: the administrator password stored in the initial database.%n%nWrite down the user before closing this message.
english.BDErrorFin=WARNING: the database could not be installed (%1).%nRun the installer again in INSTALL mode. Details in %2
english.BDErrorCartel=The HotelRydlDB database could not be installed.%n%n%1%n%nThe program was installed, but without a database. Check that SQL Server is running and run the installer again in INSTALL mode.%nDetails in: %2
english.ConfDesinstalar=Are you sure you want to uninstall Hotel Rydl?%n%nThe program is removed. The HotelRydlDB database and the Backups folder are NOT deleted.
english.IconoManual=Installation manual
english.IconoDesinstalar=Uninstall Hotel Rydl
german.ModoReinst=Neuinstallation
german.ModoInst=Vollständige Installation
german.PagModoTit=Was möchten Sie tun?
german.PagModoSub=Wählen Sie, ob Sie das komplette System installieren oder nur das Programm neu installieren möchten.
german.PagModoTxt=INSTALLIEREN kopiert das Programm und erstellt die Datenbank HotelRydlDB (falls sie existiert, wird sie ersetzt). NEU INSTALLIEREN kopiert das Programm erneut in den Ordner, in dem es bereits installiert ist, und BEHÄLT die Datenbank.
german.OpcInst=Installieren  (Programm + Datenbank)
german.OpcReinst=Neu installieren  (nur das Programm, Datenbank bleibt erhalten)
german.EncontradoEn=Hotel Rydl wurde gefunden in: %1
german.PagReinstTit=Neuinstallation
german.PagReinstSub=Das Programm wird in dem Ordner neu installiert, in dem es bereits installiert ist.
german.PagBDTit=Die Datenbank existiert bereits
german.PagBDSub=Die Datenbank HotelRydlDB wurde auf diesem Computer gefunden.
german.PagBDTxt=Die vollständige Installation ERSETZT die Datenbank durch die Anfangsdatenbank des Systems (ohne Daten): Buchungen, Gäste, Artikel, Benutzer und alle anderen Daten gehen verloren.%n%nWenn Sie nur das Programm aktualisieren möchten, gehen Sie zurück und wählen Sie NEU INSTALLIEREN.
german.OpcBackup=Vor dem Ersetzen eine Sicherung der aktuellen Datenbank erstellen (empfohlen)
german.ReinstDetalle=Gefundener Installationsordner:%n    %1%n%nWas gemacht wird:%n    •  Die Programmdateien werden durch diese Version ersetzt ({#MiVersion}).%n    •  Die SQL-Skripte und Verknüpfungen werden aktualisiert.%n%nWas NICHT verändert wird:%n    •  Die Datenbank HotelRydlDB (alle Ihre Daten bleiben erhalten).%n    •  Ihre Sicherungen und die Dateien conexion.txt und correo.txt.%n%nDatenbank: %2 / HotelRydlDB
german.FinReinst=Hotel Rydl wurde erfolgreich neu installiert in:%n%1%n%nDie Datenbank HotelRydlDB blieb unverändert. Melden Sie sich mit Ihrem gewohnten Benutzer und Passwort an.
german.FinInst=Hotel Rydl wurde installiert in:%n%1
german.NoEncontrado=Hotel Rydl wurde auf diesem Computer nicht gefunden.%n%nMöchten Sie den Installationsordner manuell auswählen?
german.UsarInstalar=Um das System zum ersten Mal zu installieren, wählen Sie INSTALLIEREN.
german.ElegirCarpeta=Wählen Sie den Ordner, in dem Hotel Rydl installiert ist (der {#MiExe} enthält):
german.CarpetaMal=Das Programm ({#MiExe}) befindet sich nicht in diesem Ordner.%nWählen Sie den richtigen Ordner oder verwenden Sie INSTALLIEREN.
german.ReinstSinSQL=Keine Verbindung zu SQL Server möglich.%n%nDas Programm kann trotzdem neu installiert werden, funktioniert aber erst, wenn SQL Server verfügbar ist.%n%nMöchten Sie wirklich fortfahren?
german.ReinstSinBD=Die Datenbank HotelRydlDB wurde auf %1 nicht gefunden.%n%nDie Neuinstallation erstellt keine Datenbank: ohne sie kann das Programm nicht starten.%nWenn Sie alles installieren möchten, gehen Sie zurück und wählen Sie INSTALLIEREN.%n%nMöchten Sie die Neuinstallation wirklich fortsetzen?
german.ConfReinst=Möchten Sie Hotel Rydl wirklich in diesem Ordner NEU INSTALLIEREN?%n%n    %1%n%nDie Programmdateien werden ersetzt. Die Datenbank bleibt erhalten.
german.YaInstalado=Hotel Rydl ist bereits installiert in:%n    %1%n%nINSTALLIEREN führt eine VOLLSTÄNDIGE Installation durch: es installiert auch die Datenbank und ERSETZT sie, falls sie existiert.%nWenn Sie nur das Programm aktualisieren möchten, wählen Sie NEU INSTALLIEREN.%n%nMöchten Sie wirklich eine vollständige Installation durchführen?
german.SinSQL=SQL Server wurde auf diesem Computer nicht gefunden, oder Ihr Windows-Benutzer hat keine Berechtigung zur Verbindung.%n%nInstallieren Sie SQL Server 2016 oder neuer (z. B. SQL Server Express) und führen Sie das Installationsprogramm erneut aus.%nDetails in: %1
german.ConfCarpeta=Möchten Sie Hotel Rydl wirklich in diesem Ordner installieren?%n%n    %1
german.ConfBorrar=ACHTUNG: ALLE aktuellen Daten in HotelRydlDB werden GELÖSCHT%n(Buchungen, Gäste, Zahlungen, Artikel, Bestellungen, Benutzer und Protokoll)%nund durch die Anfangsdatenbank des Systems ersetzt.%n%nSind Sie sicher?
german.ConfSinBackup=Sie haben gewählt, KEINE Sicherung vor dem Ersetzen der Datenbank zu erstellen.%nWenn Sie fortfahren, können die aktuellen Daten nicht wiederhergestellt werden.%n%nMöchten Sie wirklich ohne Sicherung fortfahren?
german.ConfUltima=Letzte Bestätigung: die Datenbank HotelRydlDB ersetzen?%nDiese Aktion kann nicht rückgängig gemacht werden.
german.ComenzarReinst=Das Programm wird in %1 neu installiert.%n%nBeginnen?
german.ComenzarInst=Das Programm wird in %1 zusammen mit der Datenbank HotelRydlDB installiert.%n%nBeginnen?
german.MemoOperacion=Vorgang:
german.MemoBD=Datenbank:
german.MemoConserva=Bleibt unverändert (%1)
german.MemoServidor=Server: %1
german.MemoExiste=HotelRydlDB EXISTIERT BEREITS: sie wird durch die Anfangsdatenbank ersetzt
german.MemoBackup=Zuerst wird eine Sicherung der aktuellen Datenbank erstellt
german.MemoSinBackup=OHNE vorherige Sicherung
german.MemoCrea=HotelRydlDB wird im Anfangszustand erstellt (ohne Daten)
german.MemoAdmin=Der Administrator-Benutzer für die erste Anmeldung wird erstellt
german.EstadoBD=Die Datenbank HotelRydlDB wird installiert (dies kann einige Minuten dauern)...
german.BDInstalada=Datenbank HotelRydlDB installiert auf %1.
german.BDRespaldo=Sicherung der vorherigen Datenbank: %1
german.PrimerIngreso=Erste Anmeldung (notieren Sie es vor dem Schließen):%n    Benutzer: %1
german.ClaveInicialFin=    Anfangspasswort: %1  (Ausweis + Nachname)%nDas System fordert Sie bei der ersten Anmeldung auf, es zu ändern.
german.CartelClave=Daten für die erste Anmeldung bei Hotel Rydl:%n%nBenutzer: %1%nAnfangspasswort: %2   (Ausweis + Nachname)%n%nNotieren Sie sie, bevor Sie diese Meldung schließen. Bei der ersten Anmeldung fordert das System Sie auf, das Passwort zu ändern.
german.ClaveExistenteFin=    Passwort: das des Administrators, das bereits in der Anfangsdatenbank gespeichert ist.
german.CartelClaveExistente=Daten für die erste Anmeldung bei Hotel Rydl:%n%nBenutzer: %1%nPasswort: das in der Anfangsdatenbank gespeicherte Administratorpasswort.%n%nNotieren Sie den Benutzer, bevor Sie diese Meldung schließen.
german.BDErrorFin=ACHTUNG: die Datenbank konnte nicht installiert werden (%1).%nFühren Sie das Installationsprogramm erneut im Modus INSTALLIEREN aus. Details in %2
german.BDErrorCartel=Die Datenbank HotelRydlDB konnte nicht installiert werden.%n%n%1%n%nDas Programm wurde installiert, aber ohne Datenbank. Prüfen Sie, ob SQL Server läuft, und führen Sie das Installationsprogramm erneut im Modus INSTALLIEREN aus.%nDetails in: %2
german.ConfDesinstalar=Möchten Sie Hotel Rydl wirklich deinstallieren?%n%nDas Programm wird entfernt. Die Datenbank HotelRydlDB und der Ordner Backups werden NICHT gelöscht.
german.IconoManual=Installationshandbuch
german.IconoDesinstalar=Hotel Rydl deinstallieren

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

[Dirs]
Name: "{app}\Backups"; Flags: uninsneveruninstall
Name: "{app}\Logs"

[Icons]
Name: "{group}\Hotel Rydl"; Filename: "{app}\{#MiExe}"; WorkingDir: "{app}"
Name: "{group}\{cm:IconoManual}"; Filename: "{app}\LEEME_Instalador.txt"
Name: "{group}\{cm:IconoDesinstalar}"; Filename: "{uninstallexe}"
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

{ Texto en el idioma elegido al iniciar el instalador ([CustomMessages]) }
function M(const Clave: String): String;
begin
  Result := CustomMessage(Clave);
  StringChangeEx(Result, '%n', #13#10, True);
end;

{ Igual que M, reemplazando %1 y %2 por los datos }
function M1(const Clave, A: String): String;
begin
  Result := M(Clave);
  StringChangeEx(Result, '%1', A, True);
end;

function M2(const Clave, A, B: String): String;
begin
  Result := M1(Clave, A);
  StringChangeEx(Result, '%2', B, True);
end;

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
  if EsReinstalar then Result := M('ModoReinst') else Result := M('ModoInst');
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
    M('PagModoTit'), M('PagModoSub'), M('PagModoTxt'), True, False);
  PaginaModo.Add(M('OpcInst'));
  PaginaModo.Add(M('OpcReinst'));
  if HayInstalacion then
  begin
    PaginaModo.Values[1] := True;     // si ya está instalado, lo más probable es reinstalar
    PaginaModo.SubCaptionLabel.Caption := PaginaModo.SubCaptionLabel.Caption + #13#10#13#10 +
      M1('EncontradoEn', CarpetaPrevia);
  end
  else
    PaginaModo.Values[0] := True;

  PaginaReinst := CreateOutputMsgPage(PaginaModo.ID,
    M('PagReinstTit'), M('PagReinstSub'), '');

  PaginaBD := CreateInputOptionPage(wpSelectDir,
    M('PagBDTit'), M('PagBDSub'), M('PagBDTxt'), False, False);
  PaginaBD.Add(M('OpcBackup'));
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
    PaginaReinst.MsgLabel.Caption := M2('ReinstDetalle', WizardForm.DirEdit.Text, Servidor);

  if CurPageID = wpFinished then
  begin
    if EsReinstalar then
      WizardForm.FinishedLabel.Caption := M1('FinReinst', WizardForm.DirEdit.Text)
    else
      WizardForm.FinishedLabel.Caption := M1('FinInst', WizardForm.DirEdit.Text) + #13#10#13#10 + ResultadoBD;
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
        if MsgBox(M('NoEncontrado'), mbConfirmation, MB_YESNO) = IDNO then
        begin
          MsgBox(M('UsarInstalar'), mbInformation, MB_OK);
          Result := False; Exit;
        end;
        Dir := ExpandConstant('{autopf}');
        if not BrowseForFolder(M('ElegirCarpeta'), Dir, False) then
        begin
          Result := False; Exit;
        end;
        if not ExeEn(Dir) then
        begin
          MsgBox(M('CarpetaMal'), mbError, MB_OK);
          Result := False; Exit;
        end;
        CarpetaPrevia := Dir;
        HayInstalacion := True;
      end;
      WizardForm.DirEdit.Text := CarpetaPrevia;

      Codigo := VerificarSQL;
      if Codigo = SIN_SQL then
      begin
        if not Confirmar(M('ReinstSinSQL')) then
        begin
          Result := False; Exit;
        end;
      end
      else if Codigo = BD_NO_EXISTE then
      begin
        if not Confirmar(M1('ReinstSinBD', Servidor)) then
        begin
          Result := False; Exit;
        end;
      end;

      Result := Confirmar(M1('ConfReinst', CarpetaPrevia));
    end
    else
    begin
      if HayInstalacion then
        if not Confirmar(M1('YaInstalado', CarpetaPrevia)) then
        begin
          Result := False; Exit;
        end;

      Codigo := VerificarSQL;
      if (Codigo = SIN_SQL) or (Codigo = ERROR_BD) then
      begin
        MsgBox(M1('SinSQL', ExpandConstant('{tmp}\basedatos.log')), mbCriticalError, MB_OK);
        Result := False; Exit;
      end;
    end;
  end;

  { ── paso 2 (instalar): carpeta elegida ── }
  if (CurPageID = wpSelectDir) and EsInstalar then
    Result := Confirmar(M1('ConfCarpeta', WizardForm.DirEdit.Text));

  { ── paso 3 (instalar con base existente): doble confirmación ── }
  if CurPageID = PaginaBD.ID then
  begin
    if not Confirmar(M('ConfBorrar')) then
    begin
      Result := False; Exit;
    end;
    if not PaginaBD.Values[0] then
      if not Confirmar(M('ConfSinBackup')) then
      begin
        Result := False; Exit;
      end;
    Result := Confirmar(M('ConfUltima'));
  end;

  { ── listo para instalar ── }
  if CurPageID = wpReady then
    if EsReinstalar then
      Result := Confirmar(M1('ComenzarReinst', WizardForm.DirEdit.Text))
    else
      Result := Confirmar(M1('ComenzarInst', WizardForm.DirEdit.Text));
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo,
  MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := M('MemoOperacion') + NewLine + Space + NombreModo('') + NewLine + NewLine +
            MemoDirInfo + NewLine + NewLine;
  if EsReinstalar then
    Result := Result + M('MemoBD') + NewLine + Space + M1('MemoConserva', Servidor) + NewLine + NewLine
  else
  begin
    Result := Result + M('MemoBD') + NewLine + Space + M1('MemoServidor', Servidor) + NewLine;
    if BaseExiste then
    begin
      Result := Result + Space + M('MemoExiste') + NewLine;
      if PaginaBD.Values[0] then
        Result := Result + Space + M('MemoBackup') + NewLine
      else
        Result := Result + Space + M('MemoSinBackup') + NewLine;
    end
    else
      Result := Result + Space + M('MemoCrea') + NewLine;
    Result := Result + Space + M('MemoAdmin') + NewLine;
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

  WizardForm.StatusLabel.Caption := M('EstadoBD');
  Codigo := EjecutarBD('Instalar', Extra, Salida, Log);

  if Codigo = 0 then
  begin
    ResultadoBD := M1('BDInstalada', Servidor);
    Respaldo := LeerValor(Salida, 'respaldo');
    if Respaldo <> '' then
      ResultadoBD := ResultadoBD + #13#10 + M1('BDRespaldo', Respaldo);

    Salida := ExpandConstant('{tmp}\credenciales.txt');
    if EjecutarBD('Credenciales', '-CarpetaApp "' + ExpandConstant('{app}') + '"', Salida, Log) = 0 then
    begin
      UsuarioAdmin := LeerValor(Salida, 'usuario');
      ClaveAdmin := LeerValor(Salida, 'clave_inicial');
      ClavePendiente := LeerValor(Salida, 'clave_pendiente') = '1';
      if UsuarioAdmin <> '' then
      begin
        ResultadoBD := ResultadoBD + #13#10#13#10 + M1('PrimerIngreso', UsuarioAdmin) + #13#10;
        if ClavePendiente then
        begin
          ResultadoBD := ResultadoBD + M1('ClaveInicialFin', ClaveAdmin);
          MsgBox(M2('CartelClave', UsuarioAdmin, ClaveAdmin), mbInformation, MB_OK);
        end
        else
        begin
          ResultadoBD := ResultadoBD + M('ClaveExistenteFin');
          MsgBox(M1('CartelClaveExistente', UsuarioAdmin), mbInformation, MB_OK);
        end;
      end;
    end;
  end
  else
  begin
    ResultadoBD := M2('BDErrorFin', LeerValor(Salida, 'error'), Log);
    MsgBox(M2('BDErrorCartel', LeerValor(Salida, 'error'), Log), mbError, MB_OK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and EsInstalar then
    InstalarBase;
end;

{ ───────────────────────── desinstalación ───────────────────────── }

function InitializeUninstall: Boolean;
begin
  Result := MsgBox(M('ConfDesinstalar'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
end;
