!INC Local Scripts.EAConstants-JScript

/*
 * HRS-480 · Tercera entrega — crea en Enterprise Architect 12 los diagramas de:
 *   T06 Bitácora de cambios: actividad del trigger TR_Insumos_RegistrarCambio y de Activar versión,
 *       DCU, DS, DC y DER de CUN14 Auditar Cambios de Insumos.
 *   A03 Serializar Clientes: DCU, DS, DC y DER de CUN13.
 *
 * USO: seleccioná un paquete en el Project Browser, Tools > Scripting, nuevo script JScript,
 *      pegá este archivo y Run. Crea el paquete "Tercera Entrega"; no borra ni modifica nada existente.
 */

var DATA = [{"nombre": "T06 Bit\u00e1cora de cambios", "diagramas": [{"tipo": "act", "titulo": "act Registro de cambios TR_Insumos_RegistrarCambio HRS-480", "data": {"lanes": [], "h": 0, "nodes": [{"k": "ini", "ty": "Initial", "n": "Inicio", "pos": [135, 60, 22, 22], "st": ""}, {"k": "op", "ty": "Decision", "n": "\u00bfOperaci\u00f3n sobre Insumos?", "pos": [111, 105, 70, 70], "st": ""}, {"k": "ins", "ty": "Action", "n": "INSERT o UPDATE en la tabla Insumos (Maestro de Insumos)", "pos": [51, 210, 190, 44], "st": ""}, {"k": "del", "ty": "Action", "n": "DELETE en la tabla Insumos (Eliminar)", "pos": [300, 118, 180, 44], "st": ""}, {"k": "bor", "ty": "Action", "n": "TR_Insumos_BorradoLogico (INSTEAD OF DELETE): UPDATE Activo = 0", "pos": [300, 198, 180, 52], "st": ""}, {"k": "tr", "ty": "Action", "n": "Se ejecuta TR_Insumos_RegistrarCambio (AFTER INSERT, UPDATE)", "pos": [51, 290, 190, 44], "st": ""}, {"k": "act", "ty": "Decision", "n": "\u00bfActivando versi\u00f3n? (sp)", "pos": [111, 365, 70, 70], "st": ""}, {"k": "cmp", "ty": "Action", "n": "Comparar valores nuevos (inserted) con los anteriores (deleted) con EXCEPT", "pos": [51, 470, 190, 52], "st": ""}, {"k": "cam", "ty": "Decision", "n": "\u00bfExiste cambio?", "pos": [111, 555, 70, 70], "st": ""}, {"k": "ide", "ty": "Action", "n": "Identificar insumos afectados y clasificar la Operaci\u00f3n: Alta / Modificaci\u00f3n / Baja / Reactivaci\u00f3n", "pos": [300, 552, 200, 64], "st": ""}, {"k": "ant", "ty": "Action", "n": "Marcar versi\u00f3n vigente anterior Act = 0 en Insumos_C", "pos": [300, 645, 200, 44], "st": ""}, {"k": "nue", "ty": "Action", "n": "Insertar nueva versi\u00f3n en Insumos_C (Fecha, Hora, datos, Operaci\u00f3n, IdUsuario)", "pos": [300, 720, 200, 52], "st": ""}, {"k": "vig", "ty": "Action", "n": "Registrar la nueva versi\u00f3n con Act = 1 (vigente)", "pos": [300, 805, 200, 44], "st": ""}, {"k": "fin", "ty": "Final", "n": "Fin", "pos": [136, 810, 26, 26], "st": ""}, {"k": "nota", "ty": "Note", "n": "IdUsuario lo toma de SESSION_CONTEXT('IdUsuario'). Si el UPDATE no cambi\u00f3 ning\u00fan dato, no se registra nada.", "pos": [530, 645, 170, 70], "st": ""}], "flows": [{"f": "ini", "t": "op", "g": "", "ty": "ControlFlow"}, {"f": "op", "t": "ins", "g": "INSERT / UPDATE", "ty": "ControlFlow"}, {"f": "op", "t": "del", "g": "DELETE", "ty": "ControlFlow"}, {"f": "del", "t": "bor", "g": "", "ty": "ControlFlow"}, {"f": "bor", "t": "tr", "g": "", "ty": "ControlFlow"}, {"f": "ins", "t": "tr", "g": "", "ty": "ControlFlow"}, {"f": "tr", "t": "act", "g": "", "ty": "ControlFlow"}, {"f": "act", "t": "fin", "g": "S\u00ed", "ty": "ControlFlow"}, {"f": "act", "t": "cmp", "g": "No", "ty": "ControlFlow"}, {"f": "cmp", "t": "cam", "g": "", "ty": "ControlFlow"}, {"f": "cam", "t": "ide", "g": "S\u00ed", "ty": "ControlFlow"}, {"f": "cam", "t": "fin", "g": "No", "ty": "ControlFlow"}, {"f": "ide", "t": "ant", "g": "", "ty": "ControlFlow"}, {"f": "ant", "t": "nue", "g": "", "ty": "ControlFlow"}, {"f": "nue", "t": "vig", "g": "", "ty": "ControlFlow"}, {"f": "vig", "t": "fin", "g": "", "ty": "ControlFlow"}, {"f": "nota", "t": "nue", "g": "", "ty": "NoteLink"}], "titulo": "act Registro de cambios TR_Insumos_RegistrarCambio HRS-480"}, "paquete": "T06.a Registro de cambios"}, {"tipo": "act", "titulo": "act Activar versi\u00f3n sp_ActivarVersionInsumo HRS-480", "data": {"lanes": [], "h": 0, "nodes": [{"k": "ini", "ty": "Initial", "n": "Inicio", "pos": [135, 60, 22, 22], "st": ""}, {"k": "sel", "ty": "Action", "n": "Seleccionar versi\u00f3n hist\u00f3rica en la bit\u00e1cora de cambios y presionar ACTIVAR", "pos": [51, 105, 190, 52], "st": ""}, {"k": "conf", "ty": "Action", "n": "Confirmar la activaci\u00f3n", "pos": [51, 185, 190, 44], "st": ""}, {"k": "val", "ty": "Action", "n": "Validar la versi\u00f3n seleccionada (InsumoCBLL.Activar)", "pos": [51, 260, 190, 44], "st": ""}, {"k": "ok", "ty": "Decision", "n": "\u00bfExiste y tiene Act = 0?", "pos": [111, 335, 70, 70], "st": ""}, {"k": "msg", "ty": "Action", "n": "Informar: no existe o ya es la vigente", "pos": [300, 348, 180, 44], "st": ""}, {"k": "dv", "ty": "Action", "n": "Controlar el DV de Insumos (Consistente)", "pos": [51, 440, 190, 44], "st": ""}, {"k": "sp", "ty": "Action", "n": "EXEC sp_ActivarVersionInsumo: BEGIN TRANSACTION y SESSION_CONTEXT ActivandoVersion = 1", "pos": [51, 515, 190, 64], "st": ""}, {"k": "upd", "ty": "Action", "n": "UPDATE Insumos con los datos de la versi\u00f3n elegida", "pos": [51, 610, 190, 44], "st": ""}, {"k": "a0", "ty": "Action", "n": "Desactivar la versi\u00f3n vigente: Act = 0", "pos": [51, 685, 190, 44], "st": ""}, {"k": "a1", "ty": "Action", "n": "Activar la versi\u00f3n elegida: Act = 1", "pos": [51, 760, 190, 44], "st": ""}, {"k": "com", "ty": "Action", "n": "ActivandoVersion = 0 y COMMIT", "pos": [51, 835, 190, 44], "st": ""}, {"k": "bit", "ty": "Action", "n": "Registrar en la bit\u00e1cora de eventos y recalcular el DV de Insumos", "pos": [51, 910, 190, 52], "st": ""}, {"k": "fin", "ty": "Final", "n": "Fin", "pos": [377, 920, 26, 26], "st": ""}, {"k": "nota", "ty": "Note", "n": "Con ActivandoVersion = 1, TR_Insumos_RegistrarCambio no inserta otra versi\u00f3n: la elegida vuelve a ser la vigente. TR_Insumos_C_SoloAct solo deja cambiar Act.", "pos": [300, 610, 190, 92], "st": ""}], "flows": [{"f": "ini", "t": "sel", "g": "", "ty": "ControlFlow"}, {"f": "sel", "t": "conf", "g": "", "ty": "ControlFlow"}, {"f": "conf", "t": "val", "g": "", "ty": "ControlFlow"}, {"f": "val", "t": "ok", "g": "", "ty": "ControlFlow"}, {"f": "ok", "t": "msg", "g": "No", "ty": "ControlFlow"}, {"f": "msg", "t": "fin", "g": "", "ty": "ControlFlow"}, {"f": "ok", "t": "dv", "g": "S\u00ed", "ty": "ControlFlow"}, {"f": "dv", "t": "sp", "g": "", "ty": "ControlFlow"}, {"f": "sp", "t": "upd", "g": "", "ty": "ControlFlow"}, {"f": "upd", "t": "a0", "g": "", "ty": "ControlFlow"}, {"f": "a0", "t": "a1", "g": "", "ty": "ControlFlow"}, {"f": "a1", "t": "com", "g": "", "ty": "ControlFlow"}, {"f": "com", "t": "bit", "g": "", "ty": "ControlFlow"}, {"f": "bit", "t": "fin", "g": "", "ty": "ControlFlow"}, {"f": "nota", "t": "upd", "g": "", "ty": "NoteLink"}], "titulo": "act Activar versi\u00f3n sp_ActivarVersionInsumo HRS-480"}, "paquete": "T06.a Registro de cambios"}, {"tipo": "uc", "titulo": "uc DCU Bit\u00e1cora de cambios HRS-480", "data": {"cus": [["CUN14", "CUN14 - Auditar Cambios de Insumos", 470, 160]], "actors": [["adm", "Administrador", 110, 140]], "assoc": [["adm", "CUN14"]], "ext": [], "limite": "Hotel Rydl", "box": [290, 50, 360, 230], "margen": 40, "titulo": "uc DCU Bit\u00e1cora de cambios HRS-480"}, "paquete": "T06.b Auditar Cambios"}, {"tipo": "seq", "titulo": "sd DS CUN14 Auditar Cambios de Insumos HRS-480", "data": {"lifes": [{"k": "a", "n": "Administrador", "ty": "Actor", "st": ""}, {"k": "gui", "n": "GUI", "ty": "Boundary", "st": ""}, {"k": "ibll", "n": "InsumoBLL", "ty": "Class", "st": ""}, {"k": "cbll", "n": "InsumoCBLL", "ty": "Class", "st": ""}, {"k": "dv", "n": "DigitoVerificador", "ty": "Class", "st": ""}, {"k": "bit", "n": "BitacoraModulo", "ty": "Class", "st": ""}, {"k": "idal", "n": "InsumoDAL", "ty": "Class", "st": ""}, {"k": "cdal", "n": "InsumoCDAL", "ty": "Class", "st": ""}, {"k": "bdal", "n": "Bitacora", "ty": "Class", "st": ""}, {"k": "dvs", "n": "DigitoVerificadorServicio", "ty": "Class", "st": ""}], "msgs": [{"f": "a", "t": "gui", "n": "BitacoraDeCambios()", "r": 0, "st": ""}, {"f": "gui", "t": "ibll", "n": "ObtenerInsumos(incluirBajas: true)", "r": 0, "st": ""}, {"f": "ibll", "t": "idal", "n": "ObtenerInsumos(true)", "r": 0, "st": ""}, {"f": "idal", "t": "ibll", "n": ":List<InsumoBE>", "r": 1, "st": ""}, {"f": "ibll", "t": "gui", "n": ":ResultadoOperacion<List<InsumoBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "cbll", "n": "ObtenerNombres()", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerNombres()", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": ":List<string>", "r": 1, "st": ""}, {"f": "cbll", "t": "gui", "n": ":ResultadoOperacion<List<string>>", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "Aplicar(c\u00f3digo, nombre, fechaInicial, fechaFinal)", "r": 0, "st": ""}, {"f": "gui", "t": "cbll", "n": "Consultar(FiltroCambiosBE)", "r": 0, "st": ""}, {"f": "cbll", "t": "cbll", "n": "ValidarFechas(fechaInicio, fechaFin)", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerCambios(FiltroCambiosBE)", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": ":List<InsumoCBE>", "r": 1, "st": ""}, {"f": "cbll", "t": "bit", "n": "Registrar(\"Maestros\", \"Consulta de bit\u00e1cora de cambios\", 5, detalle, \"EXITOSO\")", "r": 0, "st": ""}, {"f": "bit", "t": "bdal", "n": "Registrar(idUsuario, idModulo, idTipoEvento, criticidad, detalle, resultado)", "r": 0, "st": ""}, {"f": "bdal", "t": "bit", "n": "true", "r": 1, "st": ""}, {"f": "bit", "t": "cbll", "n": "true", "r": 1, "st": ""}, {"f": "cbll", "t": "gui", "n": ":ResultadoOperacion<List<InsumoCBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "MostrarHistorial() (la versi\u00f3n con Act = 1 resaltada)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "SeleccionarVersion(idCambio) y Activar()", "r": 0, "st": ""}, {"f": "gui", "t": "a", "n": "ConfirmarActivacion(fecha, nombre, stock, estado)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "Confirmar()", "r": 0, "st": ""}, {"f": "gui", "t": "cbll", "n": "Activar(idCambio)", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerCambio(idCambio)", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": ":InsumoCBE (Act = 0)", "r": 1, "st": ""}, {"f": "cbll", "t": "dv", "n": "Consistente(\"Insumos\")", "r": 0, "st": ""}, {"f": "dv", "t": "cbll", "n": "dvOk", "r": 1, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ActivarVersion(idCambio, idUsuario)", "r": 0, "st": ""}, {"f": "cdal", "t": "cdal", "n": "EXEC sp_ActivarVersionInsumo(idCambio, idUsuario)", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": "true (versi\u00f3n activada)", "r": 1, "st": ""}, {"f": "cbll", "t": "bit", "n": "Registrar(\"Maestros\", \"Versi\u00f3n de insumo activada\", 2, detalle, \"EXITOSO\")", "r": 0, "st": ""}, {"f": "bit", "t": "bdal", "n": "Registrar(idUsuario, idModulo, idTipoEvento, criticidad, detalle, resultado)", "r": 0, "st": ""}, {"f": "bdal", "t": "bit", "n": "true", "r": 1, "st": ""}, {"f": "bit", "t": "cbll", "n": "true", "r": 1, "st": ""}, {"f": "cbll", "t": "dv", "n": "RecalcularSi(dvOk, \"Insumos\")", "r": 0, "st": ""}, {"f": "dv", "t": "dvs", "n": "RecalcularTablas(\"Insumos\")", "r": 0, "st": ""}, {"f": "dvs", "t": "dv", "n": "DVH / DVV actualizados", "r": 1, "st": ""}, {"f": "dv", "t": "cbll", "n": "true", "r": 1, "st": ""}, {"f": "cbll", "t": "gui", "n": ":ResultadoOperacion<InsumoCBE>", "r": 1, "st": ""}, {"f": "gui", "t": "ibll", "n": "ObtenerInsumos(incluirBajas: true)", "r": 0, "st": ""}, {"f": "ibll", "t": "idal", "n": "ObtenerInsumos(true)", "r": 0, "st": ""}, {"f": "idal", "t": "ibll", "n": ":List<InsumoBE>", "r": 1, "st": ""}, {"f": "ibll", "t": "gui", "n": ":ResultadoOperacion<List<InsumoBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "VersionActivada() (Act = 1)", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "ACTIVAR deshabilitado: la versi\u00f3n ya es la vigente (Act = 1)", "r": 1, "st": ""}], "frags": [{"op": "loop", "n": "loop [por cada vez que se filtra la consulta]", "s": 9, "e": 19}, {"op": "alt", "n": "alt [versi\u00f3n seleccionada con Act = 0] / [sin selecci\u00f3n o versi\u00f3n vigente]", "s": 20, "e": 45}]}, "paquete": "T06.b Auditar Cambios"}, {"tipo": "cls", "titulo": "class DC CUN14 Auditar Cambios de Insumos HRS-480", "data": {"els": [{"k": "gui", "n": "FrmBitacoraCambios", "st": "", "a": [{"n": "_cambios", "t": "InsumoCBLL", "v": "Private"}, {"n": "_insumos", "t": "InsumoBLL", "v": "Private"}], "m": [{"n": "Aplicar", "p": [], "r": "void", "v": "Public"}, {"n": "Limpiar", "p": [], "r": "void", "v": "Public"}, {"n": "Activar", "p": [], "r": "void", "v": "Public"}, {"n": "CargarInsumos", "p": [], "r": "void", "v": "Public"}], "pos": [30, 60, 210, 112]}, {"k": "cbll", "n": "InsumoCBLL", "st": "", "a": [{"n": "_dal", "t": "InsumoCDAL", "v": "Private"}], "m": [{"n": "Consultar", "p": ["filtro: FiltroCambiosBE"], "r": "ResultadoOperacion<List<InsumoCBE>>", "v": "Public"}, {"n": "ObtenerNombres", "p": [], "r": "ResultadoOperacion<List<string>>", "v": "Public"}, {"n": "Activar", "p": ["idCambio: int"], "r": "ResultadoOperacion<InsumoCBE>", "v": "Public"}], "pos": [330, 60, 390, 86]}, {"k": "cdal", "n": "InsumoCDAL", "st": "", "a": [], "m": [{"n": "ObtenerCambios", "p": ["f: FiltroCambiosBE"], "r": "List<InsumoCBE>", "v": "Public"}, {"n": "ObtenerNombres", "p": [], "r": "List<string>", "v": "Public"}, {"n": "ObtenerCambio", "p": ["idCambio: int"], "r": "InsumoCBE", "v": "Public"}, {"n": "ActivarVersion", "p": ["idCambio: int", "idUsuario: int?"], "r": "void", "v": "Public"}], "pos": [800, 60, 330, 99]}, {"k": "ibll", "n": "InsumoBLL", "st": "", "a": [{"n": "_dal", "t": "InsumoDAL", "v": "Private"}], "m": [{"n": "ObtenerInsumos", "p": ["incluirBajas: bool"], "r": "ResultadoOperacion<List<InsumoBE>>", "v": "Public"}], "pos": [30, 250, 330, 60]}, {"k": "idal", "n": "InsumoDAL", "st": "", "a": [], "m": [{"n": "ObtenerInsumos", "p": ["incluirBajas: bool"], "r": "List<InsumoBE>", "v": "Public"}], "pos": [30, 400, 280, 60]}, {"k": "dv", "n": "DigitoVerificador", "st": "", "a": [], "m": [{"n": "Consistente", "p": ["tablas: string[]"], "r": "bool", "v": "Public"}, {"n": "RecalcularSi", "p": ["dvOk: bool", "tablas: string[]"], "r": "void", "v": "Public"}], "pos": [400, 270, 260, 73]}, {"k": "bit", "n": "BitacoraModulo", "st": "", "a": [], "m": [{"n": "Registrar", "p": ["modulo: string", "evento: string", "criticidad: int", "detalle: string", "resultado: string"], "r": "void", "v": "Public"}], "pos": [560, 420, 420, 60]}, {"k": "bdal", "n": "Bitacora", "st": "", "a": [], "m": [{"n": "Registrar", "p": ["idUsuario: int?", "idModulo: int?", "idTipoEvento: int?", "criticidad: int", "detalle: string", "resultado: string"], "r": "void", "v": "Public"}], "pos": [560, 550, 470, 60]}, {"k": "cbe", "n": "InsumoCBE", "st": "", "a": [{"n": "IdCambio", "t": "int", "v": "Public"}, {"n": "IdInsumo", "t": "int", "v": "Public"}, {"n": "Fecha", "t": "DateTime", "v": "Public"}, {"n": "Hora", "t": "TimeSpan", "v": "Public"}, {"n": "Nombre", "t": "string", "v": "Public"}, {"n": "Descripcion", "t": "string", "v": "Public"}, {"n": "Categoria", "t": "string", "v": "Public"}, {"n": "UnidadMedida", "t": "string", "v": "Public"}, {"n": "StockActual", "t": "decimal", "v": "Public"}, {"n": "StockMinimo", "t": "decimal", "v": "Public"}, {"n": "Activo", "t": "bool", "v": "Public"}, {"n": "Act", "t": "bool", "v": "Public"}, {"n": "Operacion", "t": "string", "v": "Public"}, {"n": "Usuario", "t": "string", "v": "Public"}], "m": [], "pos": [1000, 250, 180, 229]}, {"k": "fil", "n": "FiltroCambiosBE", "st": "", "a": [{"n": "IdInsumo", "t": "int?", "v": "Public"}, {"n": "Nombre", "t": "string", "v": "Public"}, {"n": "FechaInicio", "t": "DateTime?", "v": "Public"}, {"n": "FechaFin", "t": "DateTime?", "v": "Public"}], "m": [], "pos": [760, 250, 170, 99]}, {"k": "ibe", "n": "InsumoBE", "st": "", "a": [{"n": "IdInsumo", "t": "int", "v": "Public"}, {"n": "Nombre", "t": "string", "v": "Public"}, {"n": "Descripcion", "t": "string", "v": "Public"}, {"n": "Categoria", "t": "string", "v": "Public"}, {"n": "UnidadMedida", "t": "string", "v": "Public"}, {"n": "StockActual", "t": "decimal", "v": "Public"}, {"n": "StockMinimo", "t": "decimal", "v": "Public"}, {"n": "Activo", "t": "bool", "v": "Public"}], "m": [], "pos": [30, 500, 180, 151]}], "cons": [{"f": "gui", "t": "cbll", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "ibll", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cbll", "t": "cdal", "ty": "Aggregation", "st": "", "n": "", "cf": "", "ct": "1"}, {"f": "ibll", "t": "idal", "ty": "Aggregation", "st": "", "n": "", "cf": "", "ct": "1"}, {"f": "cbll", "t": "dv", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cbll", "t": "bit", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "bit", "t": "bdal", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cdal", "t": "cbe", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cbll", "t": "fil", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "idal", "t": "ibe", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}]}, "paquete": "T06.b Auditar Cambios"}, {"tipo": "der", "titulo": "dm DER Auditar Cambios HRS-480", "data": {"els": [{"n": "Insumos", "cols": [{"n": "IdInsumo", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "Nombre", "t": "nvarchar", "len": 150, "sc": 0, "pk": false, "calc": false}, {"n": "Descripcion", "t": "nvarchar", "len": 300, "sc": 0, "pk": false, "calc": false}, {"n": "Categoria", "t": "nvarchar", "len": 50, "sc": 0, "pk": false, "calc": false}, {"n": "UnidadMedida", "t": "nvarchar", "len": 30, "sc": 0, "pk": false, "calc": false}, {"n": "StockActual", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "StockMinimo", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "Activo", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Insumos", "pkcol": "IdInsumo", "pktipo": "int", "fks": [], "pos": [40, 80, 230, 0]}, {"n": "Insumos_C", "cols": [{"n": "IdCambio", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "IdInsumo", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Fecha", "t": "date", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Hora", "t": "time", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Nombre", "t": "nvarchar", "len": 150, "sc": 0, "pk": false, "calc": false}, {"n": "Descripcion", "t": "nvarchar", "len": 300, "sc": 0, "pk": false, "calc": false}, {"n": "Categoria", "t": "nvarchar", "len": 50, "sc": 0, "pk": false, "calc": false}, {"n": "UnidadMedida", "t": "nvarchar", "len": 30, "sc": 0, "pk": false, "calc": false}, {"n": "StockActual", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "StockMinimo", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "Activo", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Act", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Operacion", "t": "nvarchar", "len": 20, "sc": 0, "pk": false, "calc": false}, {"n": "IdUsuario", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Insumos_C", "pkcol": "IdCambio", "pktipo": "int", "fks": [{"n": "FK_Insumos_C_Insumos", "c": "IdInsumo"}], "pos": [380, 60, 240, 0]}, {"n": "Bitacora", "cols": [{"n": "IdBitacora", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "IdUsuario", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Fecha", "t": "date", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Hora", "t": "time", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "IdModulo", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "IdTipoEvento", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Detalle", "t": "nvarchar", "len": 500, "sc": 0, "pk": false, "calc": false}, {"n": "Criticidad", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Resultado", "t": "nvarchar", "len": 20, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Bitacora", "pkcol": "IdBitacora", "pktipo": "int", "fks": [], "pos": [40, 320, 230, 0]}, {"n": "DigitoVerificador", "cols": [{"n": "NombreTabla", "t": "nvarchar", "len": 50, "sc": 0, "pk": true, "calc": false}, {"n": "DVH", "t": "nvarchar", "len": 64, "sc": 0, "pk": false, "calc": false}, {"n": "DVV", "t": "nvarchar", "len": 64, "sc": 0, "pk": false, "calc": false}, {"n": "FechaCalculo", "t": "datetime", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Valido", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}], "pk": "PK_DigitoVerificador", "pkcol": "NombreTabla", "pktipo": "nvarchar", "fks": [], "pos": [380, 440, 230, 0]}], "cons": [{"f": "Insumos_C", "t": "Insumos", "n": "FK_Insumos_C_Insumos"}]}, "paquete": "T06.b Auditar Cambios"}]}, {"nombre": "A03 Serializar Clientes", "diagramas": [{"tipo": "uc", "titulo": "uc DCU Serializar Clientes HRS-480", "data": {"cus": [["CUN13", "CUN13 - Serializar Clientes", 470, 170]], "actors": [["rec", "Recepcionista", 110, 80], ["adm", "Administrador", 110, 210]], "assoc": [["rec", "CUN13"], ["adm", "CUN13"]], "ext": [], "limite": "Hotel Rydl", "box": [290, 50, 360, 250], "margen": 40, "titulo": "uc DCU Serializar Clientes HRS-480"}, "paquete": ""}, {"tipo": "seq", "titulo": "sd DS CUN13 Serializar Clientes HRS-480", "data": {"lifes": [{"k": "a", "n": "Recepcionista", "ty": "Actor", "st": ""}, {"k": "gui", "n": "GUI", "ty": "Boundary", "st": ""}, {"k": "cbll", "n": "ClienteBLL", "ty": "Class", "st": ""}, {"k": "ser", "n": "SerializarBLL", "ty": "Class", "st": ""}, {"k": "des", "n": "DesSerializarBLL", "ty": "Class", "st": ""}, {"k": "bit", "n": "BitacoraModulo", "ty": "Class", "st": ""}, {"k": "cdal", "n": "ClienteDAL", "ty": "Class", "st": ""}, {"k": "bdal", "n": "Bitacora", "ty": "Class", "st": ""}], "msgs": [{"f": "a", "t": "gui", "n": "MaestroDeClientes()", "r": 0, "st": ""}, {"f": "gui", "t": "cbll", "n": "ObtenerClientes()", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerClientes()", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": ":List<ClienteBE>", "r": 1, "st": ""}, {"f": "cbll", "t": "gui", "n": ":ResultadoOperacion<List<ClienteBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "MostrarClientes(DNI, apellidos, nombres, email, celular, direcci\u00f3n)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "ElegirUbicacion()", "r": 0, "st": ""}, {"f": "gui", "t": "gui", "n": "SaveFileDialog: carpeta y nombre Clientes_HotelRydl_aaaaMMdd_HHmm.xml", "r": 0, "st": ""}, {"f": "gui", "t": "a", "n": "MostrarRuta(rutaArchivo)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "Serializar()", "r": 0, "st": ""}, {"f": "gui", "t": "gui", "n": "FilasDeLaGrilla()", "r": 0, "st": ""}, {"f": "gui", "t": "ser", "n": "Serializar(List<ClienteBE>, rutaArchivo)", "r": 0, "st": ""}, {"f": "ser", "t": "ser", "n": "ValidarGrillaYCarpeta()", "r": 0, "st": ""}, {"f": "ser", "t": "ser", "n": "XmlSerializer.Serialize(archivo, clientes)", "r": 0, "st": ""}, {"f": "ser", "t": "bit", "n": "Registrar(\"Maestros\", \"Clientes serializados\", 4, detalle, \"EXITOSO\")", "r": 0, "st": ""}, {"f": "bit", "t": "bdal", "n": "Registrar(idUsuario, idModulo, idTipoEvento, criticidad, detalle, resultado)", "r": 0, "st": ""}, {"f": "bdal", "t": "bit", "n": "true", "r": 1, "st": ""}, {"f": "bit", "t": "ser", "n": "true", "r": 1, "st": ""}, {"f": "ser", "t": "gui", "n": ":ResultadoOperacion<string> (ruta del XML)", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "SerializacionCorrecta(cantidad de clientes)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "ElegirArchivo()", "r": 0, "st": ""}, {"f": "gui", "t": "gui", "n": "OpenFileDialog: archivo .xml", "r": 0, "st": ""}, {"f": "gui", "t": "a", "n": "MostrarRuta(rutaArchivo)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "DesSerializar()", "r": 0, "st": ""}, {"f": "gui", "t": "des", "n": "DesSerializar(rutaArchivo)", "r": 0, "st": ""}, {"f": "des", "t": "des", "n": "ValidarArchivo(existe y es .xml)", "r": 0, "st": ""}, {"f": "des", "t": "des", "n": "XmlSerializer.Deserialize(archivo)", "r": 0, "st": ""}, {"f": "des", "t": "des", "n": "ValidarCliente(c) por cada cliente (descarta inv\u00e1lidos y DNI repetidos)", "r": 0, "st": ""}, {"f": "des", "t": "bit", "n": "Registrar(\"Maestros\", \"Clientes des-serializados\", 4, detalle, \"EXITOSO\")", "r": 0, "st": ""}, {"f": "bit", "t": "bdal", "n": "Registrar(idUsuario, idModulo, idTipoEvento, criticidad, detalle, resultado)", "r": 0, "st": ""}, {"f": "bdal", "t": "bit", "n": "true", "r": 1, "st": ""}, {"f": "bit", "t": "des", "n": "true", "r": 1, "st": ""}, {"f": "des", "t": "gui", "n": ":ResultadoOperacion<List<ClienteBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "gui", "n": "CargarGrilla(clientes del XML): no modifica la base", "r": 0, "st": ""}, {"f": "gui", "t": "a", "n": "ClientesDesSerializados(cantidad de clientes)", "r": 1, "st": ""}], "frags": [{"op": "alt", "n": "alt [Serializar] / [Des-serializar]", "s": 6, "e": 34}]}, "paquete": ""}, {"tipo": "cls", "titulo": "class DC CUN13 Serializar Clientes HRS-480", "data": {"els": [{"k": "gui", "n": "FrmMaestroClientes", "st": "", "a": [{"n": "_clientes", "t": "ClienteBLL", "v": "Private"}, {"n": "_serializar", "t": "SerializarBLL", "v": "Private"}, {"n": "_desSerializar", "t": "DesSerializarBLL", "v": "Private"}], "m": [{"n": "ElegirUbicacion", "p": [], "r": "void", "v": "Public"}, {"n": "Serializar", "p": [], "r": "void", "v": "Public"}, {"n": "ElegirArchivo", "p": [], "r": "void", "v": "Public"}, {"n": "DesSerializar", "p": [], "r": "void", "v": "Public"}, {"n": "CargarGrilla", "p": ["clientes: IEnumerable<ClienteBE>"], "r": "void", "v": "Public"}, {"n": "FilasDeLaGrilla", "p": [], "r": "List<ClienteBE>", "v": "Public"}], "pos": [330, 60, 300, 151]}, {"k": "cbll", "n": "ClienteBLL", "st": "", "a": [{"n": "_dal", "t": "ClienteDAL", "v": "Private"}], "m": [{"n": "ObtenerClientes", "p": [], "r": "ResultadoOperacion<List<ClienteBE>>", "v": "Public"}], "pos": [30, 280, 300, 60]}, {"k": "cdal", "n": "ClienteDAL", "st": "", "a": [], "m": [{"n": "ObtenerClientes", "p": [], "r": "List<ClienteBE>", "v": "Public"}], "pos": [30, 440, 240, 60]}, {"k": "ser", "n": "SerializarBLL", "st": "", "a": [], "m": [{"n": "Serializar", "p": ["clientes: List<ClienteBE>", "rutaArchivo: string"], "r": "ResultadoOperacion<string>", "v": "Public"}, {"n": "Serializador", "p": [], "r": "XmlSerializer", "v": "Package"}], "pos": [370, 280, 420, 73]}, {"k": "des", "n": "DesSerializarBLL", "st": "", "a": [], "m": [{"n": "DesSerializar", "p": ["rutaArchivo: string"], "r": "ResultadoOperacion<List<ClienteBE>>", "v": "Public"}], "pos": [830, 280, 370, 60]}, {"k": "be", "n": "ClienteBE", "st": "", "a": [{"n": "DNI", "t": "string", "v": "Public"}, {"n": "Apellido", "t": "string", "v": "Public"}, {"n": "Nombre", "t": "string", "v": "Public"}, {"n": "Email", "t": "string", "v": "Public"}, {"n": "Celular", "t": "string", "v": "Public"}, {"n": "Direccion", "t": "string", "v": "Public"}, {"n": "DesdeXml", "t": "bool", "v": "Public"}], "m": [], "pos": [420, 420, 170, 138]}, {"k": "bit", "n": "BitacoraModulo", "st": "", "a": [], "m": [{"n": "Registrar", "p": ["modulo: string", "evento: string", "criticidad: int", "detalle: string", "resultado: string"], "r": "void", "v": "Public"}], "pos": [700, 440, 420, 60]}, {"k": "bdal", "n": "Bitacora", "st": "", "a": [], "m": [{"n": "Registrar", "p": ["idUsuario: int?", "idModulo: int?", "idTipoEvento: int?", "criticidad: int", "detalle: string", "resultado: string"], "r": "void", "v": "Public"}], "pos": [700, 560, 470, 60]}], "cons": [{"f": "gui", "t": "cbll", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "ser", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "des", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cbll", "t": "cdal", "ty": "Aggregation", "st": "", "n": "", "cf": "", "ct": "1"}, {"f": "ser", "t": "be", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "des", "t": "be", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cdal", "t": "be", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "ser", "t": "bit", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "des", "t": "bit", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "bit", "t": "bdal", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}]}, "paquete": ""}, {"tipo": "der", "titulo": "dm DER Serializar Clientes HRS-480", "data": {"els": [{"n": "Huespedes", "cols": [{"n": "IdHuesped", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "Nombre", "t": "nvarchar", "len": 100, "sc": 0, "pk": false, "calc": false}, {"n": "Apellido", "t": "nvarchar", "len": 100, "sc": 0, "pk": false, "calc": false}, {"n": "DNI", "t": "nvarchar", "len": 20, "sc": 0, "pk": false, "calc": false}, {"n": "Email", "t": "nvarchar", "len": 200, "sc": 0, "pk": false, "calc": false}, {"n": "Telefono", "t": "nvarchar", "len": 30, "sc": 0, "pk": false, "calc": false}, {"n": "Nacionalidad", "t": "nvarchar", "len": 50, "sc": 0, "pk": false, "calc": false}, {"n": "Direccion", "t": "nvarchar", "len": 200, "sc": 0, "pk": false, "calc": false}, {"n": "FechaRegistro", "t": "datetime", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Activo", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Huespedes", "pkcol": "IdHuesped", "pktipo": "int", "fks": [], "pos": [40, 80, 230, 0]}, {"n": "Bitacora", "cols": [{"n": "IdBitacora", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "IdUsuario", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Fecha", "t": "date", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Hora", "t": "time", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "IdModulo", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "IdTipoEvento", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Detalle", "t": "nvarchar", "len": 500, "sc": 0, "pk": false, "calc": false}, {"n": "Criticidad", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Resultado", "t": "nvarchar", "len": 20, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Bitacora", "pkcol": "IdBitacora", "pktipo": "int", "fks": [], "pos": [360, 80, 230, 0]}], "cons": []}, "paquete": ""}]}];

var ok = 0, avisos = 0;
function log(s) { Session.Output(s); }
function aviso(s) { avisos++; Session.Output("   AVISO: " + s); }

function nuevoPaquete(padre, nombre) {
    var p = padre.Packages.AddNew(nombre, "Package");
    p.Update();
    padre.Packages.Refresh();
    return p;
}

function nuevoElemento(pkg, nombre, tipo, estereotipo) {
    var e = pkg.Elements.AddNew(nombre, tipo);
    if (estereotipo) e.Stereotype = estereotipo;
    e.Update();
    pkg.Elements.Refresh();
    return e;
}

function nuevoDiagrama(pkg, nombre, tipo) {
    var d = pkg.Diagrams.AddNew(nombre, tipo);
    d.Update();
    pkg.Diagrams.Refresh();
    return d;
}

function ubicar(diag, el, x, y, w, h) {
    var o = diag.DiagramObjects.AddNew("l=" + x + ";r=" + (x + w) + ";t=" + y + ";b=" + (y + h) + ";", "");
    o.ElementID = el.ElementID;
    o.Update();
    return o;
}

function conectar(src, tgt, tipo, nombre, estereotipo) {
    var c = src.Connectors.AddNew(nombre || "", tipo);
    c.SupplierID = tgt.ElementID;
    if (estereotipo) c.Stereotype = estereotipo;
    c.Update();
    src.Connectors.Refresh();
    return c;
}

function agregarAtributo(el, nombre, tipo, vis, estereotipo) {
    var a = el.Attributes.AddNew(nombre, tipo);
    a.Visibility = vis || "Private";
    if (estereotipo) a.Stereotype = estereotipo;
    a.Update();
    return a;
}

function agregarMetodo(el, nombre, params, vis, estereotipo, retorno) {
    var m = el.Methods.AddNew(nombre, retorno || "");
    m.Visibility = vis || "Public";
    if (estereotipo) m.Stereotype = estereotipo;
    m.Update();
    for (var i = 0; i < params.length; i++) {
        var pn = params[i], pt = (estereotipo == "PK" || estereotipo == "FK") ? "int" : "";
        var k = pn.indexOf(":");
        if (k > 0) { pt = pn.substring(k + 1).replace(/^\s+|\s+$/g, ""); pn = pn.substring(0, k).replace(/^\s+|\s+$/g, ""); }
        if (estereotipo == "PK" || estereotipo == "FK") { if (params.tipos) pt = params.tipos[i]; }
        var p = m.Parameters.AddNew(pn, pt);
        p.Position = i;
        p.Update();
    }
    m.Parameters.Refresh();
    return m;
}

// ── DSS ─────────────────────────────────────────────────────────────────
var OPERADOR = { "alt": 0, "opt": 1, "loop": 4 };

function crearSecuencia(pkg, nombreDiag, seq) {
    var d = nuevoDiagrama(pkg, nombreDiag, "Sequence");
    var vida = {};
    for (var i = 0; i < seq.lifes.length; i++) {
        var L = seq.lifes[i];
        var el = nuevoElemento(pkg, L.n, L.ty, L.st);
        ubicar(d, el, 40 + i * 190, 40, 120, (L.ty == "Actor" ? 70 : 60));
        vida[L.k] = el;
    }
    for (var j = 0; j < seq.msgs.length; j++) {
        var m = seq.msgs[j];
        var c = vida[m.f].Connectors.AddNew(m.n, "Sequence");
        c.SupplierID = vida[m.t].ElementID;
        c.DiagramID = d.DiagramID;
        c.SequenceNo = j + 1;
        if (m.st) c.Stereotype = m.st;
        c.Update();
        if (m.r == 1) {
            // Mensaje de retorno (línea punteada)
            try { Repository.Execute("UPDATE t_connector SET PDATA4 = '1' WHERE Connector_ID = " + c.ConnectorID); }
            catch (e) { aviso("no se pudo marcar como retorno: " + m.n); }
        }
        vida[m.f].Connectors.Refresh();
    }
    // Fragmentos combinados (alt / opt / loop): se ubican aproximadamente; ajustá su alto si hace falta
    for (var k = 0; k < seq.frags.length; k++) {
        var f = seq.frags[k];
        var fr = pkg.Elements.AddNew(f.n, "InteractionFragment");
        fr.Subtype = OPERADOR[f.op] != null ? OPERADOR[f.op] : 0;
        fr.Update();
        pkg.Elements.Refresh();
        var y = 130 + f.s * 40, alto = (f.e - f.s + 1) * 40 + 30;
        ubicar(d, fr, 15, y, 40 + seq.lifes.length * 190, alto);
    }
    Repository.ReloadDiagram(d.DiagramID);
    ok++;
    return d;
}

// ── Diagramas de clases ─────────────────────────────────────────────────
function crearClases(pkg, nombreDiag, cls) {
    var d = nuevoDiagrama(pkg, nombreDiag, "Logical");
    var mapa = {};
    for (var i = 0; i < cls.els.length; i++) {
        var E = cls.els[i];
        var el = nuevoElemento(pkg, E.n, "Class", E.st);
        for (var a = 0; a < E.a.length; a++) agregarAtributo(el, E.a[a].n, E.a[a].t, E.a[a].v || "Private");
        el.Attributes.Refresh();
        for (var m = 0; m < E.m.length; m++) agregarMetodo(el, E.m[m].n, E.m[m].p, E.m[m].v || "Public", "", E.m[m].r);
        el.Methods.Refresh();
        ubicar(d, el, E.pos[0], E.pos[1], E.pos[2], E.pos[3]);
        mapa[E.k] = el;
    }
    for (var j = 0; j < cls.cons.length; j++) {
        var C = cls.cons[j];
        var agg = (C.ty == "Aggregation");
        var c = conectar(mapa[C.f], mapa[C.t], agg ? "Association" : C.ty, C.n, C.st);
        if (agg) { c.ClientEnd.Aggregation = 1; c.ClientEnd.Update(); c.Update(); }   // rombo del lado del "todo"
        if (C.cf || C.ct) {
            c.ClientEnd.Cardinality = C.cf || "";
            c.SupplierEnd.Cardinality = C.ct || "";
            c.ClientEnd.Update();
            c.SupplierEnd.Update();
            c.Update();
        }
    }
    Repository.ReloadDiagram(d.DiagramID);
    ok++;
    return d;
}

// ── DER ─────────────────────────────────────────────────────────────────
function crearDER(pkg, nombreDiag, der) {
    var d = nuevoDiagrama(pkg, nombreDiag, "Logical");
    var mapa = {};
    for (var i = 0; i < der.els.length; i++) {
        var T = der.els[i];
        var t = nuevoElemento(pkg, T.n, "Class", "table");
        t.Gentype = "SQL Server 2012";
        t.Update();
        for (var c = 0; c < T.cols.length; c++) {
            var C = T.cols[c];
            var a = t.Attributes.AddNew(C.n, C.t);
            a.Stereotype = "column";
            a.Visibility = "Public";
            if (C.len) { if (C.sc) { a.Precision = C.len; a.Scale = C.sc; } else { a.Length = C.len; } }
            if (C.pk) { a.IsOrdered = true; a.AllowDuplicates = true; }   // Primary Key + Not Null
            if (C.calc) a.Notes = "Columna calculada";
            a.Update();
        }
        t.Attributes.Refresh();
        var pkp = [T.pkcol]; if (T.pktipo) pkp.tipos = [T.pktipo];
        agregarMetodo(t, T.pk, pkp, "Public", "PK");
        for (var f = 0; f < T.fks.length; f++) agregarMetodo(t, T.fks[f].n, [T.fks[f].c], "Public", "FK");
        t.Methods.Refresh();
        ubicar(d, t, T.pos[0], T.pos[1], T.pos[2], T.pos[3]);
        mapa[T.n] = t;
    }
    for (var j = 0; j < der.cons.length; j++) {
        var R = der.cons[j];
        var k = conectar(mapa[R.f], mapa[R.t], "Association", R.n, "FK");
        k.StyleEx = "FKINFO=SRC=" + R.n + ":DST=PK_" + R.t + ":;";
        k.Update();
        k.ClientEnd.Role = R.n;
        k.ClientEnd.Cardinality = "0..*";
        k.ClientEnd.Update();
        k.SupplierEnd.Role = "PK_" + R.t;
        k.SupplierEnd.Cardinality = "1";
        k.SupplierEnd.Update();
        k.Update();
    }
    if (der.nota) {
        var nt = pkg.Elements.AddNew("", "Note"); nt.Notes = der.nota; nt.Update(); pkg.Elements.Refresh();
        ubicar(d, nt, 40, der.notaY, 700, 50);
    }
    Repository.ReloadDiagram(d.DiagramID);
    ok++;
    return d;
}

// ── Casos de uso ────────────────────────────────────────────────────────
function crearCasosDeUso(pkg, uc) {
    var d = nuevoDiagrama(pkg, uc.titulo, "Use Case");
    var lim = nuevoElemento(pkg, uc.limite, "Boundary", "");
    ubicar(d, lim, uc.box[0], uc.box[1], uc.box[2], uc.box[3]);
    var mapa = {};
    for (var i = 0; i < uc.cus.length; i++) {
        var U = uc.cus[i];
        var e = nuevoElemento(pkg, U[1], "UseCase", "");
        ubicar(d, e, U[2] - 140, U[3] - 45, 280, 90);
        mapa[U[0]] = e;
    }
    for (var a = 0; a < uc.actors.length; a++) {
        var A = uc.actors[a];
        var ac = nuevoElemento(pkg, A[1], "Actor", "");
        ubicar(d, ac, A[2] - 25, A[3], 50, 90);
        mapa[A[0]] = ac;
    }
    for (var s = 0; s < uc.assoc.length; s++) conectar(mapa[uc.assoc[s][0]], mapa[uc.assoc[s][1]], "Association", "", "");
    for (var x = 0; x < uc.ext.length; x++) {
        var ex = conectar(mapa[uc.ext[x][0]], mapa[uc.ext[x][1]], "Dependency", "", uc.ext[x][2]);
        if (uc.ext[x][3]) { ex.Notes = uc.ext[x][3]; ex.Update(); }
    }
    if (uc.notas) for (var q = 0; q < uc.notas.length; q++) {
        var nt = pkg.Elements.AddNew("", "Note"); nt.Notes = uc.notas[q][3]; nt.Update(); pkg.Elements.Refresh();
        ubicar(d, nt, uc.notas[q][0], uc.notas[q][1], uc.notas[q][2], 60);
    }
    Repository.ReloadDiagram(d.DiagramID);
    ok++;
}

// ── Actividad ───────────────────────────────────────────────────────────
function crearActividad(pkg, act) {
    var d = nuevoDiagrama(pkg, act.titulo, "Activity");
    for (var l = 0; l < act.lanes.length; l++) {
        var L = act.lanes[l];
        var p = nuevoElemento(pkg, L[0], "ActivityPartition", "");
        ubicar(d, p, L[1], 4, L[2] - L[1], act.h - 8);
    }
    var mapa = {};
    for (var i = 0; i < act.nodes.length; i++) {
        var N = act.nodes[i], e;
        if (N.ty == "Initial") { e = pkg.Elements.AddNew("Inicio", "StateNode"); e.Subtype = 100; e.Update(); pkg.Elements.Refresh(); }
        else if (N.ty == "Final") { e = pkg.Elements.AddNew("Fin", "StateNode"); e.Subtype = 101; e.Update(); pkg.Elements.Refresh(); }
        else if (N.ty == "Note") { e = pkg.Elements.AddNew("", "Note"); e.Notes = N.n; e.Update(); pkg.Elements.Refresh(); }
        else if (N.ty == "Store") e = nuevoElemento(pkg, N.n, "Object", "datastore");
        else e = nuevoElemento(pkg, N.n, N.ty, "");
        ubicar(d, e, N.pos[0], N.pos[1], N.pos[2], N.pos[3]);
        mapa[N.k] = e;
    }
    for (var j = 0; j < act.flows.length; j++) {
        var F = act.flows[j];
        var c = conectar(mapa[F.f], mapa[F.t], F.ty, "", "");
        if (F.g) { c.TransitionGuard = F.g; c.Update(); }
    }
    Repository.ReloadDiagram(d.DiagramID);
    ok++;
}

// ── Diagrama de roles ───────────────────────────────────────────────────
function crearRoles(pkg, titulo, R) {
    var d = nuevoDiagrama(pkg, titulo, "Use Case");
    var mapa = {};
    for (var i = 0; i < R.els.length; i++) {
        var E = R.els[i], e;
        if (E[2] == "Actor") { e = nuevoElemento(pkg, E[1], "Actor", ""); ubicar(d, e, E[3] - 25, E[4], 50, 90); }
        else { e = nuevoElemento(pkg, E[1], "Class", ""); ubicar(d, e, E[3] - 40, E[4], 80, 60); }
        mapa[E[0]] = e;
    }
    for (var j = 0; j < R.links.length; j++) conectar(mapa[R.links[j][0]], mapa[R.links[j][1]], "Association", R.links[j][2], "");
    if (R.nota) { var nt = pkg.Elements.AddNew("", "Note"); nt.Notes = R.nota; nt.Update(); pkg.Elements.Refresh(); ubicar(d, nt, 560, 400, 360, 50); }
    Repository.ReloadDiagram(d.DiagramID);
    ok++;
}

// ── Principal ───────────────────────────────────────────────────────────
function main() {
    Repository.EnsureOutputVisible("Script");
    Repository.ClearOutput("Script");
    var raiz = Repository.GetTreeSelectedPackage();
    if (raiz == null) { Session.Prompt("Seleccioná un paquete en el Project Browser y volvé a ejecutar el script.", 0); return; }
    log("Creando Correcciones Carpeta 2 dentro de: " + raiz.Name);
    var base = nuevoPaquete(raiz, "Tercera Entrega");
    for (var i = 0; i < DATA.length; i++) {
        var P = DATA[i];
        var pk = nuevoPaquete(base, P.nombre);
        for (var j = 0; j < P.diagramas.length; j++) {
            var D = P.diagramas[j];
            var sub = D.paquete ? nuevoPaquete(pk, D.paquete) : pk;
            try {
                if (D.tipo == "seq") crearSecuencia(sub, D.titulo, D.data);
                else if (D.tipo == "cls") crearClases(sub, D.titulo, D.data);
                else if (D.tipo == "uc") { D.data.titulo = D.titulo; crearCasosDeUso(sub, D.data); }
                else if (D.tipo == "act") { D.data.titulo = D.titulo; crearActividad(sub, D.data); }
                else if (D.tipo == "roles") crearRoles(sub, D.titulo, D.data);
                else if (D.tipo == "der") crearDER(sub, D.titulo, D.data);
                log("OK  " + D.titulo);
            } catch (e) { aviso(D.titulo + ": " + e.message); }
        }
    }
    Repository.RefreshModelView(raiz.PackageID);
    log("Listo: " + ok + " diagramas creados" + (avisos ? ", " + avisos + " avisos (ver arriba)" : "") + ".");
    log("Los fragmentos alt/opt/loop de los DS quedan ubicados aproximadamente: ajustá su alto para que encierren sus mensajes.");
}

main();
