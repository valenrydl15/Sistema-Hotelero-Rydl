!INC Local Scripts.EAConstants-JScript

/*
 * HRS-480 · Entrega 3 / Final — Menú MAESTROS — crea los diagramas en Enterprise Architect 12.
 *
 * USO:
 *   1. En el Project Browser seleccioná el paquete donde querés que se creen los diagramas.
 *   2. Tools > Scripting > en el grupo "Normal" (o uno nuevo) crear un script JScript, pegar este archivo y guardar.
 *   3. Botón Run (o clic derecho > Run Script). El avance se ve en la ventana Output, pestaña "Script".
 *
 * Crea un paquete "Entrega 3 - Maestros" con:
 *   - Casos de uso: diagrama de casos de uso (CUN13 y CUN14)
 *   - Registro de cambios (back-end): diagrama de actividad de los triggers (añadir / modificar / eliminar insumos)
 *   - CUN13 y CUN14: DS, diagrama de clases y DER de cada CU (con PK y FK)
 * No borra ni modifica nada existente.
 */

var DATA = {"uc": {"cus": [["CUN13", "CUN13 Gestionar maestro de clientes (serializar / des-serializar XML)", 520, 190], ["CUN14", "CUN14 Auditar cambios de insumos", 520, 400]], "actors": [["rec", "Recepcionista", 110, 120], ["adm", "Administrador", 110, 330]], "assoc": [["rec", "CUN13"], ["adm", "CUN13"], ["adm", "CUN14"]], "ext": [], "limite": "HRS-480 \u00b7 Men\u00fa Maestros", "box": [260, 60, 520, 440], "titulo": "uc Diagrama de casos de uso Men\u00fa Maestros (Entrega 3) HRS-480"}, "act": {"lanes": [["Usuario (Maestro de Insumos)", 6, 300], ["Sistema (GUI + InsumoBLL + InsumoDAL)", 300, 640], ["Tabla Insumos + triggers", 640, 990], ["Tabla Insumos_C", 990, 1250]], "h": 1120, "nodes": [{"k": "ini", "ty": "Initial", "n": "", "pos": [143, 66, 20, 20], "st": ""}, {"k": "u1", "ty": "Action", "n": "Elegir operaci\u00f3n", "pos": [83, 106, 140, 40], "st": ""}, {"k": "d1", "ty": "Decision", "n": "\u00bfOperaci\u00f3n?", "pos": [127, 176, 52, 52], "st": ""}, {"k": "u2", "ty": "Action", "n": "Cargar datos del insumo", "pos": [30, 266, 150, 44], "st": ""}, {"k": "u3", "ty": "Action", "n": "Confirmar la baja", "pos": [170, 356, 120, 44], "st": ""}, {"k": "s1", "ty": "Action", "n": "Validar rol, datos y DV", "pos": [395, 266, 150, 44], "st": ""}, {"k": "d2", "ty": "Decision", "n": "\u00bfV\u00e1lido?", "pos": [444, 341, 52, 52], "st": ""}, {"k": "s2", "ty": "Action", "n": "Mostrar el motivo", "pos": [330, 426, 130, 40], "st": ""}, {"k": "fr", "ty": "Final", "n": "", "pos": [382, 496, 26, 26], "st": ""}, {"k": "s3", "ty": "Action", "n": "INSERT (alta) / UPDATE (modificaci\u00f3n) en Insumos", "pos": [490, 426, 140, 58], "st": ""}, {"k": "s4", "ty": "Action", "n": "DELETE FROM Insumos (baja)", "pos": [390, 566, 160, 44], "st": ""}, {"k": "t1", "ty": "Action", "n": "TR_Insumos_BorradoLogico (INSTEAD OF DELETE): UPDATE Activo = 0", "pos": [680, 556, 270, 60], "st": ""}, {"k": "t2", "ty": "Action", "n": "TR_Insumos_RegistrarCambio (AFTER INSERT, UPDATE)", "pos": [690, 676, 250, 50], "st": ""}, {"k": "d3", "ty": "Decision", "n": "\u00bfCambi\u00f3 alg\u00fan dato?", "pos": [789, 766, 52, 52], "st": ""}, {"k": "fs", "ty": "Final", "n": "", "pos": [902, 779, 26, 26], "st": ""}, {"k": "c1", "ty": "Action", "n": "UPDATE Insumos_C SET Act = 0 (versi\u00f3n vigente)", "pos": [1010, 846, 220, 50], "st": ""}, {"k": "c2", "ty": "Action", "n": "INSERT Insumos_C (datos + Fecha, Hora, Operaci\u00f3n, Usuario, Act = 1)", "pos": [1010, 921, 220, 60], "st": ""}, {"k": "s5", "ty": "Action", "n": "Recalcular DV de Insumos y registrar en la bit\u00e1cora", "pos": [395, 1026, 160, 58], "st": ""}, {"k": "u4", "ty": "Action", "n": "Ver el resultado en la grilla", "pos": [83, 1033, 140, 44], "st": ""}, {"k": "fin", "ty": "Final", "n": "", "pos": [140, 1104, 26, 26], "st": ""}, {"k": "n1", "ty": "Note", "n": "INSERT \u2192 Operaci\u00f3n \"Alta\" \u00b7 UPDATE \u2192 \"Modificaci\u00f3n\" \u00b7 Activo 1\u21920 \u2192 \"Baja\" \u00b7 Activo 0\u21921 \u2192 \"Reactivaci\u00f3n\". Insumos_C no admite DELETE (TR_Insumos_C_SinBorrado) y solo se puede cambiar Act (TR_Insumos_C_SoloAct).", "pos": [1000, 1026, 240, 110], "st": ""}], "flows": [{"f": "ini", "t": "u1", "g": "", "ty": "ControlFlow"}, {"f": "u1", "t": "d1", "g": "", "ty": "ControlFlow"}, {"f": "d1", "t": "u2", "g": "[A\u00f1adir / Modificar]", "ty": "ControlFlow"}, {"f": "d1", "t": "u3", "g": "[Eliminar]", "ty": "ControlFlow"}, {"f": "u2", "t": "s1", "g": "", "ty": "ControlFlow"}, {"f": "u3", "t": "s1", "g": "", "ty": "ControlFlow"}, {"f": "s1", "t": "d2", "g": "", "ty": "ControlFlow"}, {"f": "d2", "t": "s2", "g": "[no]", "ty": "ControlFlow"}, {"f": "s2", "t": "fr", "g": "", "ty": "ControlFlow"}, {"f": "d2", "t": "s3", "g": "[s\u00ed: alta / modif.]", "ty": "ControlFlow"}, {"f": "d2", "t": "s4", "g": "[s\u00ed: baja]", "ty": "ControlFlow"}, {"f": "s3", "t": "t2", "g": "", "ty": "ControlFlow"}, {"f": "s4", "t": "t1", "g": "", "ty": "ControlFlow"}, {"f": "t1", "t": "t2", "g": "", "ty": "ControlFlow"}, {"f": "t2", "t": "d3", "g": "", "ty": "ControlFlow"}, {"f": "d3", "t": "fs", "g": "[no]", "ty": "ControlFlow"}, {"f": "d3", "t": "c1", "g": "[s\u00ed]", "ty": "ControlFlow"}, {"f": "c1", "t": "c2", "g": "", "ty": "ControlFlow"}, {"f": "c2", "t": "s5", "g": "", "ty": "ControlFlow"}, {"f": "s5", "t": "u4", "g": "", "ty": "ControlFlow"}, {"f": "u4", "t": "fin", "g": "", "ty": "ControlFlow"}, {"f": "n1", "t": "c2", "g": "", "ty": "NoteLink"}], "titulo": "Diagrama de actividad \u00b7 Registro de cambios de Insumos con triggers HRS-480"}, "cus": [{"id": "CUN13", "nombre": "Gestionar maestro de clientes", "seq": {"lifes": [{"k": "a", "n": "Recepcionista", "ty": "Actor", "st": ""}, {"k": "gui", "n": "FrmMaestroClientes (GUI)", "ty": "Boundary", "st": ""}, {"k": "be", "n": "ClienteBE", "ty": "Class", "st": ""}, {"k": "bll", "n": "ClienteBLL", "ty": "Class", "st": ""}, {"k": "ser", "n": "SerializarBLL", "ty": "Class", "st": ""}, {"k": "des", "n": "DesSerializarBLL", "ty": "Class", "st": ""}, {"k": "dal", "n": "ClienteDAL", "ty": "Class", "st": ""}, {"k": "bit", "n": "BitacoraModulo", "ty": "Class", "st": ""}], "msgs": [{"f": "a", "t": "gui", "n": "AbrirMaestroDeClientes()", "r": 0, "st": ""}, {"f": "gui", "t": "bll", "n": "ObtenerClientes()", "r": 0, "st": ""}, {"f": "bll", "t": "dal", "n": "ObtenerClientes()", "r": 0, "st": ""}, {"f": "dal", "t": "be", "n": "new ClienteBE() por cada hu\u00e9sped activo", "r": 0, "st": ""}, {"f": "dal", "t": "bll", "n": "List<ClienteBE>", "r": 1, "st": ""}, {"f": "bll", "t": "gui", "n": "ResultadoOperacion<List<ClienteBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Grilla con los clientes (DNI, apellidos, nombres, email, celular, direcci\u00f3n)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "SeleccionarUbicacion(carpeta, nombreArchivo)", "r": 0, "st": ""}, {"f": "a", "t": "gui", "n": "Serializar()", "r": 0, "st": ""}, {"f": "gui", "t": "be", "n": "ObtenerClientesDeLaGrilla()", "r": 0, "st": ""}, {"f": "gui", "t": "ser", "n": "Serializar(List<ClienteBE>, rutaArchivo)", "r": 0, "st": ""}, {"f": "ser", "t": "ser", "n": "ValidarGrillaYUbicacion()", "r": 0, "st": ""}, {"f": "ser", "t": "ser", "n": "XmlSerializer.Serialize(archivo, clientes)", "r": 0, "st": ""}, {"f": "ser", "t": "bit", "n": "Registrar(\"Clientes serializados a XML\")", "r": 0, "st": ""}, {"f": "ser", "t": "gui", "n": "ResultadoOperacion<string> (ruta del XML)", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Mensaje: se serializaron N clientes en el archivo", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "SeleccionarArchivoXML(ruta)", "r": 0, "st": ""}, {"f": "a", "t": "gui", "n": "DesSerializar()", "r": 0, "st": ""}, {"f": "gui", "t": "des", "n": "DesSerializar(rutaArchivo)", "r": 0, "st": ""}, {"f": "des", "t": "des", "n": "XmlSerializer.Deserialize(archivo)", "r": 0, "st": ""}, {"f": "des", "t": "be", "n": "ValidarCliente(ClienteBE) por cada cliente", "r": 0, "st": ""}, {"f": "des", "t": "bit", "n": "Registrar(\"Clientes des-serializados desde XML\")", "r": 0, "st": ""}, {"f": "des", "t": "gui", "n": "ResultadoOperacion<List<ClienteBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Grilla con los clientes del XML", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Mensaje: el archivo no es un XML de clientes v\u00e1lido", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "Limpiar()", "r": 0, "st": ""}, {"f": "gui", "t": "gui", "n": "VaciarGrilla()", "r": 0, "st": ""}], "frags": [{"op": "opt", "n": "opt [SERIALIZAR]", "s": 7, "e": 15}, {"op": "alt", "n": "alt [archivo XML v\u00e1lido] / [archivo inv\u00e1lido]", "s": 19, "e": 24}, {"op": "opt", "n": "opt [DES-SERIALIZAR]", "s": 16, "e": 24}, {"op": "opt", "n": "opt [LIMPIAR]", "s": 25, "e": 26}]}, "cls": {"els": [{"k": "gui", "n": "FrmMaestroClientes", "st": "GUI", "a": [{"n": "dgvClientes", "t": "DataGridView"}, {"n": "txtRutaSerializar", "t": "TextBox"}, {"n": "txtRutaDesSerializar", "t": "TextBox"}, {"n": "_modo", "t": "Modo"}], "m": [{"n": "Actualizar", "p": []}, {"n": "Aplicar", "p": []}, {"n": "Eliminar", "p": []}, {"n": "ElegirUbicacion", "p": []}, {"n": "Serializar", "p": []}, {"n": "ElegirArchivo", "p": []}, {"n": "DesSerializar", "p": []}, {"n": "Limpiar", "p": []}], "pos": [380, 40, 280, 228]}, {"k": "bll", "n": "ClienteBLL", "st": "", "a": [{"n": "_dal", "t": "ClienteDAL"}], "m": [{"n": "ObtenerClientes", "p": []}, {"n": "Alta", "p": ["ClienteBE"]}, {"n": "Modificar", "p": ["ClienteBE"]}, {"n": "Eliminar", "p": ["string"]}], "pos": [20, 320, 260, 115]}, {"k": "ser", "n": "SerializarBLL", "st": "", "a": [], "m": [{"n": "Serializar", "p": ["List<ClienteBE>", "string"]}, {"n": "Serializador", "p": []}], "pos": [380, 320, 280, 85]}, {"k": "des", "n": "DesSerializarBLL", "st": "", "a": [], "m": [{"n": "DesSerializar", "p": ["string"]}], "pos": [760, 320, 260, 70]}, {"k": "be", "n": "ClienteBE", "st": "", "a": [{"n": "IdHuesped", "t": "int"}, {"n": "DNI", "t": "string"}, {"n": "Apellido", "t": "string"}, {"n": "Nombre", "t": "string"}, {"n": "Email", "t": "string"}, {"n": "Celular", "t": "string"}, {"n": "Direccion", "t": "string"}, {"n": "DesdeXml", "t": "bool"}], "m": [], "pos": [760, 40, 240, 175]}, {"k": "dal", "n": "ClienteDAL", "st": "", "a": [], "m": [{"n": "ObtenerClientes", "p": []}, {"n": "ObtenerPorDNI", "p": ["string"]}, {"n": "Insertar", "p": ["ClienteBE"]}, {"n": "Modificar", "p": ["ClienteBE", "bool"]}, {"n": "BajaLogica", "p": ["int"]}], "pos": [20, 520, 260, 130]}, {"k": "bit", "n": "BitacoraModulo", "st": "", "a": [], "m": [{"n": "Registrar", "p": ["string", "string", "int", "string", "string"]}], "pos": [500, 540, 300, 70]}, {"k": "res", "n": "ResultadoOperacion<T>", "st": "", "a": [{"n": "Exito", "t": "bool"}, {"n": "Mensaje", "t": "string"}, {"n": "Dato", "t": "T"}], "m": [{"n": "Ok", "p": ["T", "string"]}, {"n": "Fallo", "p": ["string"]}], "pos": [20, 40, 240, 115]}], "cons": [{"f": "gui", "t": "bll", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "ser", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "des", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "be", "ty": "Association", "st": "", "n": "muestra", "cf": "1", "ct": "0..*"}, {"f": "bll", "t": "dal", "ty": "Association", "st": "", "n": "", "cf": "1", "ct": "1"}, {"f": "ser", "t": "be", "ty": "Dependency", "st": "use", "n": "serializa", "cf": "", "ct": ""}, {"f": "des", "t": "be", "ty": "Dependency", "st": "use", "n": "crea", "cf": "", "ct": ""}, {"f": "ser", "t": "bit", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "des", "t": "bit", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "bll", "t": "res", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}]}, "der": {"els": [{"n": "Huespedes", "cols": [{"n": "IdHuesped", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "Nombre", "t": "nvarchar", "len": 100, "sc": 0, "pk": false, "calc": false}, {"n": "Apellido", "t": "nvarchar", "len": 100, "sc": 0, "pk": false, "calc": false}, {"n": "DNI", "t": "nvarchar", "len": 20, "sc": 0, "pk": false, "calc": false}, {"n": "Email", "t": "nvarchar", "len": 200, "sc": 0, "pk": false, "calc": false}, {"n": "Telefono", "t": "nvarchar", "len": 30, "sc": 0, "pk": false, "calc": false}, {"n": "Nacionalidad", "t": "nvarchar", "len": 50, "sc": 0, "pk": false, "calc": false}, {"n": "Direccion", "t": "nvarchar", "len": 200, "sc": 0, "pk": false, "calc": false}, {"n": "FechaRegistro", "t": "datetime", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Activo", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Huespedes", "pkcol": "IdHuesped", "fks": [], "pos": [60, 40, 300, 223]}], "cons": [], "nota": "CLIENTES = tabla Huespedes. Celular = Telefono. La grilla se serializa/des-serializa a XML (no se guarda en la base).", "notaY": 293}}, {"id": "CUN14", "nombre": "Auditar cambios de insumos", "seq": {"lifes": [{"k": "a", "n": "Administrador", "ty": "Actor", "st": ""}, {"k": "gui", "n": "FrmBitacoraCambios (GUI)", "ty": "Boundary", "st": ""}, {"k": "cbll", "n": "InsumoCBLL", "ty": "Class", "st": ""}, {"k": "cbe", "n": "InsumoCBE", "ty": "Class", "st": ""}, {"k": "cdal", "n": "InsumoCDAL", "ty": "Class", "st": ""}, {"k": "be", "n": "InsumoBE", "ty": "Class", "st": ""}, {"k": "bll", "n": "InsumoBLL", "ty": "Class", "st": ""}, {"k": "dal", "n": "InsumoDAL", "ty": "Class", "st": ""}, {"k": "bit", "n": "BitacoraModulo", "ty": "Class", "st": ""}], "msgs": [{"f": "a", "t": "gui", "n": "AbrirBitacoraDeCambios()", "r": 0, "st": ""}, {"f": "gui", "t": "bll", "n": "ObtenerInsumos(incluirBajas)", "r": 0, "st": ""}, {"f": "bll", "t": "dal", "n": "ObtenerInsumos(incluirBajas)", "r": 0, "st": ""}, {"f": "dal", "t": "be", "n": "new InsumoBE() por cada insumo", "r": 0, "st": ""}, {"f": "dal", "t": "bll", "n": "List<InsumoBE>", "r": 1, "st": ""}, {"f": "bll", "t": "gui", "n": "List<InsumoBE> (tabla Insumos y filtro C\u00f3digo)", "r": 1, "st": ""}, {"f": "gui", "t": "cbll", "n": "ObtenerNombres()", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerNombres()", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": "List<string>", "r": 1, "st": ""}, {"f": "cbll", "t": "gui", "n": "List<string> (filtro Nombre)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "Filtrar(c\u00f3digo, nombre, fechaIni, fechaFin) y APLICAR", "r": 0, "st": ""}, {"f": "gui", "t": "cbll", "n": "Consultar(FiltroCambiosBE)", "r": 0, "st": ""}, {"f": "cbll", "t": "cbll", "n": "ValidarFechas(fechaIni, fechaFin)", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerCambios(FiltroCambiosBE)", "r": 0, "st": ""}, {"f": "cdal", "t": "cbe", "n": "new InsumoCBE() por cada registro de Insumos_C", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": "List<InsumoCBE>", "r": 1, "st": ""}, {"f": "cbll", "t": "bit", "n": "Registrar(\"Consulta de bit\u00e1cora de cambios\")", "r": 0, "st": ""}, {"f": "cbll", "t": "gui", "n": "ResultadoOperacion<List<InsumoCBE>>", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Historial de cambios (la versi\u00f3n con Act = 1 resaltada)", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "SeleccionarVersion(idCambio) y ACTIVAR", "r": 0, "st": ""}, {"f": "gui", "t": "cbll", "n": "Activar(idCambio)", "r": 0, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ObtenerCambio(idCambio)", "r": 0, "st": ""}, {"f": "cdal", "t": "cbll", "n": "InsumoCBE (Act = 0)", "r": 1, "st": ""}, {"f": "cbll", "t": "cdal", "n": "ActivarVersion(idCambio, idUsuario)", "r": 0, "st": ""}, {"f": "cdal", "t": "cdal", "n": "EXEC sp_ActivarVersionInsumo(idCambio)", "r": 0, "st": ""}, {"f": "cbll", "t": "cbll", "n": "RecalcularDV(\"Insumos\")", "r": 0, "st": ""}, {"f": "cbll", "t": "bit", "n": "Registrar(\"Versi\u00f3n de insumo activada\")", "r": 0, "st": ""}, {"f": "cbll", "t": "gui", "n": "ResultadoOperacion<InsumoCBE>", "r": 1, "st": ""}, {"f": "gui", "t": "bll", "n": "ObtenerInsumos(incluirBajas)", "r": 0, "st": ""}, {"f": "bll", "t": "gui", "n": "List<InsumoBE> actualizada", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Mensaje: versi\u00f3n activada (Act = 1)", "r": 1, "st": ""}, {"f": "gui", "t": "a", "n": "Mensaje: esa versi\u00f3n ya es la vigente", "r": 1, "st": ""}, {"f": "a", "t": "gui", "n": "LIMPIAR()", "r": 0, "st": ""}, {"f": "gui", "t": "gui", "n": "BorrarFiltrosYAplicar()", "r": 0, "st": ""}], "frags": [{"op": "opt", "n": "opt [APLICAR]", "s": 10, "e": 18}, {"op": "alt", "n": "alt [la versi\u00f3n elegida tiene Act = 0] / [ya es la vigente]", "s": 21, "e": 31}, {"op": "opt", "n": "opt [ACTIVAR]", "s": 19, "e": 31}, {"op": "opt", "n": "opt [LIMPIAR]", "s": 32, "e": 33}]}, "cls": {"els": [{"k": "gui", "n": "FrmBitacoraCambios", "st": "GUI", "a": [{"n": "dgvCambios", "t": "DataGridView"}, {"n": "dgvInsumos", "t": "DataGridView"}, {"n": "cmbCodigo", "t": "ComboBox"}, {"n": "cmbNombre", "t": "ComboBox"}, {"n": "dtpInicio", "t": "DateTimePicker"}, {"n": "dtpFin", "t": "DateTimePicker"}], "m": [{"n": "Aplicar", "p": []}, {"n": "Limpiar", "p": []}, {"n": "Activar", "p": []}, {"n": "CargarInsumos", "p": []}], "pos": [400, 40, 280, 198]}, {"k": "cbll", "n": "InsumoCBLL", "st": "", "a": [{"n": "_dal", "t": "InsumoCDAL"}], "m": [{"n": "Consultar", "p": ["FiltroCambiosBE"]}, {"n": "ObtenerNombres", "p": []}, {"n": "Activar", "p": ["int"]}], "pos": [60, 340, 260, 100]}, {"k": "cdal", "n": "InsumoCDAL", "st": "", "a": [], "m": [{"n": "ObtenerCambios", "p": ["FiltroCambiosBE"]}, {"n": "ObtenerNombres", "p": []}, {"n": "ObtenerCambio", "p": ["int"]}, {"n": "ActivarVersion", "p": ["int", "int?"]}], "pos": [60, 560, 260, 115]}, {"k": "cbe", "n": "InsumoCBE", "st": "", "a": [{"n": "IdCambio", "t": "int"}, {"n": "IdInsumo", "t": "int"}, {"n": "Fecha", "t": "DateTime"}, {"n": "Hora", "t": "TimeSpan"}, {"n": "Nombre", "t": "string"}, {"n": "Descripcion", "t": "string"}, {"n": "StockActual", "t": "decimal"}, {"n": "StockMinimo", "t": "decimal"}, {"n": "Activo", "t": "bool"}, {"n": "Act", "t": "bool"}, {"n": "Operacion", "t": "string"}, {"n": "Usuario", "t": "string"}], "m": [], "pos": [400, 310, 260, 235]}, {"k": "bll", "n": "InsumoBLL", "st": "", "a": [{"n": "_dal", "t": "InsumoDAL"}], "m": [{"n": "ObtenerInsumos", "p": ["bool"]}, {"n": "Alta", "p": ["InsumoBE"]}, {"n": "Modificar", "p": ["InsumoBE"]}, {"n": "Eliminar", "p": ["int"]}, {"n": "Reactivar", "p": ["int"]}], "pos": [760, 340, 260, 130]}, {"k": "dal", "n": "InsumoDAL", "st": "", "a": [], "m": [{"n": "ObtenerInsumos", "p": ["bool"]}, {"n": "Insertar", "p": ["InsumoBE"]}, {"n": "Modificar", "p": ["InsumoBE"]}, {"n": "Eliminar", "p": ["int"]}, {"n": "Reactivar", "p": ["int"]}], "pos": [760, 580, 260, 130]}, {"k": "be", "n": "InsumoBE", "st": "", "a": [{"n": "IdInsumo", "t": "int"}, {"n": "Codigo", "t": "string"}, {"n": "Nombre", "t": "string"}, {"n": "Descripcion", "t": "string"}, {"n": "Categoria", "t": "string"}, {"n": "UnidadMedida", "t": "string"}, {"n": "StockActual", "t": "decimal"}, {"n": "StockMinimo", "t": "decimal"}, {"n": "Activo", "t": "bool"}], "m": [], "pos": [760, 40, 240, 190]}, {"k": "fil", "n": "FiltroCambiosBE", "st": "", "a": [{"n": "IdInsumo", "t": "int?"}, {"n": "Nombre", "t": "string"}, {"n": "FechaInicio", "t": "DateTime?"}, {"n": "FechaFin", "t": "DateTime?"}], "m": [], "pos": [60, 40, 240, 115]}, {"k": "bit", "n": "BitacoraModulo", "st": "", "a": [], "m": [{"n": "Registrar", "p": ["string", "string", "int", "string", "string"]}], "pos": [400, 740, 280, 70]}], "cons": [{"f": "gui", "t": "cbll", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "gui", "t": "bll", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cbll", "t": "cdal", "ty": "Association", "st": "", "n": "", "cf": "1", "ct": "1"}, {"f": "bll", "t": "dal", "ty": "Association", "st": "", "n": "", "cf": "1", "ct": "1"}, {"f": "cdal", "t": "cbe", "ty": "Dependency", "st": "use", "n": "crea", "cf": "", "ct": ""}, {"f": "dal", "t": "be", "ty": "Dependency", "st": "use", "n": "crea", "cf": "", "ct": ""}, {"f": "cbe", "t": "be", "ty": "Association", "st": "", "n": "versi\u00f3n de", "cf": "0..*", "ct": "1"}, {"f": "cbll", "t": "fil", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}, {"f": "cbll", "t": "bit", "ty": "Dependency", "st": "use", "n": "", "cf": "", "ct": ""}]}, "der": {"els": [{"n": "Insumos_C", "cols": [{"n": "IdCambio", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "IdInsumo", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Fecha", "t": "date", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Hora", "t": "time", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Nombre", "t": "nvarchar", "len": 150, "sc": 0, "pk": false, "calc": false}, {"n": "Descripcion", "t": "nvarchar", "len": 300, "sc": 0, "pk": false, "calc": false}, {"n": "Categoria", "t": "nvarchar", "len": 50, "sc": 0, "pk": false, "calc": false}, {"n": "UnidadMedida", "t": "nvarchar", "len": 30, "sc": 0, "pk": false, "calc": false}, {"n": "StockActual", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "StockMinimo", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "Activo", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Act", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Operacion", "t": "nvarchar", "len": 20, "sc": 0, "pk": false, "calc": false}, {"n": "IdUsuario", "t": "int", "len": 0, "sc": 0, "pk": false, "calc": false}], "pk": "PK_Insumos_C", "pkcol": "IdCambio", "fks": [{"n": "FK_Insumos_C_Insumos", "c": "IdInsumo"}], "pos": [40, 40, 300, 302]}, {"n": "Insumos", "cols": [{"n": "IdInsumo", "t": "int", "len": 0, "sc": 0, "pk": true, "calc": false}, {"n": "Nombre", "t": "nvarchar", "len": 150, "sc": 0, "pk": false, "calc": false}, {"n": "Descripcion", "t": "nvarchar", "len": 300, "sc": 0, "pk": false, "calc": false}, {"n": "Categoria", "t": "nvarchar", "len": 50, "sc": 0, "pk": false, "calc": false}, {"n": "UnidadMedida", "t": "nvarchar", "len": 30, "sc": 0, "pk": false, "calc": false}, {"n": "StockActual", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "StockMinimo", "t": "decimal", "len": 10, "sc": 2, "pk": false, "calc": false}, {"n": "Activo", "t": "bit", "len": 0, "sc": 0, "pk": false, "calc": false}, {"n": "Estado", "t": "varchar", "len": 20, "sc": 0, "pk": false, "calc": true}], "pk": "PK_Insumos", "pkcol": "IdInsumo", "fks": [], "pos": [480, 40, 300, 207]}], "cons": [{"f": "Insumos_C", "t": "Insumos", "n": "FK_Insumos_C_Insumos"}], "nota": "Insumos_C = PRODUCTOS_C \u00b7 Insumos = PRODUCTOS (Activo = Bit_Lo_Bo, borrado l\u00f3gico). Act = 1: versi\u00f3n vigente (una por insumo). IdUsuario sin FK: lo deja el trigger desde SESSION_CONTEXT.", "notaY": 372}}]};

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

function agregarMetodo(el, nombre, params, vis, estereotipo) {
    var m = el.Methods.AddNew(nombre, "");
    m.Visibility = vis || "Public";
    if (estereotipo) m.Stereotype = estereotipo;
    m.Update();
    for (var i = 0; i < params.length; i++) {
        var p = m.Parameters.AddNew(params[i], (estereotipo == "PK" || estereotipo == "FK") ? "int" : "");   // se muestra como Metodo(tipo, tipo)
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
        for (var a = 0; a < E.a.length; a++) agregarAtributo(el, E.a[a].n, E.a[a].t, "Private");
        el.Attributes.Refresh();
        for (var m = 0; m < E.m.length; m++) agregarMetodo(el, E.m[m].n, E.m[m].p, "Public");
        el.Methods.Refresh();
        ubicar(d, el, E.pos[0], E.pos[1], E.pos[2], E.pos[3]);
        mapa[E.k] = el;
    }
    for (var j = 0; j < cls.cons.length; j++) {
        var C = cls.cons[j];
        var c = conectar(mapa[C.f], mapa[C.t], C.ty, C.n, C.st);
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
        agregarMetodo(t, T.pk, [T.pkcol], "Public", "PK");
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
        else e = nuevoElemento(pkg, N.n, N.ty, N.st);
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

// ── Principal ───────────────────────────────────────────────────────────
function main() {
    Repository.EnsureOutputVisible("Script");
    Repository.ClearOutput("Script");
    var raiz = Repository.GetTreeSelectedPackage();
    if (raiz == null) { Session.Prompt("Seleccioná un paquete en el Project Browser y volvé a ejecutar el script.", 0); return; }

    log("Creando Entrega 3 - Maestros dentro de: " + raiz.Name);
    var e3 = nuevoPaquete(raiz, "Entrega 3 - Maestros");

    var cu = nuevoPaquete(e3, "Casos de uso");
    try { crearCasosDeUso(cu, DATA.uc); log("OK  Diagrama de casos de uso"); } catch (e) { aviso("Casos de uso: " + e.message); }

    var reg = nuevoPaquete(e3, "Registro de cambios (back-end)");
    try { crearActividad(reg, DATA.act); log("OK  Diagrama de actividad de los triggers"); } catch (e) { aviso("Actividad: " + e.message); }

    for (var i = 0; i < DATA.cus.length; i++) {
        var C = DATA.cus[i];
        var titulo = C.id + " " + C.nombre;
        var p = nuevoPaquete(e3, titulo);
        try { crearSecuencia(p, "sd DS " + titulo + " HRS-480", C.seq); log("OK  DS " + C.id); } catch (e) { aviso("DS " + C.id + ": " + e.message); }
        try { crearClases(p, "class Diagrama de clases " + titulo + " HRS-480", C.cls); log("OK  Clases " + C.id); } catch (e) { aviso("Clases " + C.id + ": " + e.message); }
        try { crearDER(p, "class DER " + C.nombre + " " + C.id + " HRS-480", C.der); log("OK  DER " + C.id); } catch (e) { aviso("DER " + C.id + ": " + e.message); }
    }

    Repository.RefreshModelView(raiz.PackageID);
    log("Listo: " + ok + " diagramas creados" + (avisos ? ", " + avisos + " avisos (ver arriba)" : "") + ".");
    log("Los fragmentos alt/opt de los DS quedan ubicados aproximadamente: ajustá su alto para que encierren sus mensajes.");
}

main();
