using ENTIDADES_HRS_480;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SERVICIOS_HRS_480
{
    /// <summary>
    /// Generación de reportes PDF con QuestPDF (licencia Community: gratuita para uso académico).
    /// Solo arma el documento con los datos que recibe; no accede a la base.
    /// </summary>
    public static class ReporteServicio
    {
        private const string HOTEL = "Hotelería Rydl";
        private const string COLOR_MARINO = "#142D50";
        private const string COLOR_DORADO = "#C9A227";
        private const string COLOR_GRIS = "#F3F5F9";
        private static readonly System.Globalization.CultureInfo AR = new System.Globalization.CultureInfo("es-AR");

        static ReporteServicio()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string Plata(decimal v) => "$ " + v.ToString("N2", AR);
        private static string Num(decimal v) => v.ToString("0.##", AR);

        // ─────────────────────────────────────────────────────────────
        // Piezas comunes
        // ─────────────────────────────────────────────────────────────
        private static void Encabezado(PageDescriptor page, string titulo, string subtitulo)
        {
            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(HOTEL).FontSize(20).Bold().FontColor(COLOR_MARINO);
                        c.Item().Text(subtitulo).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(220).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text(titulo).FontSize(15).Bold().FontColor(COLOR_MARINO);
                    });
                });
                col.Item().PaddingTop(6).LineHorizontal(2).LineColor(COLOR_DORADO);
            });
        }

        private static void Pie(PageDescriptor page, string generadoPor)
        {
            page.Footer().Row(row =>
            {
                row.RelativeItem().Text("Generado por " + generadoPor + " el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", AR))
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(120).AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        }

        private static IContainer CeldaTitulo(IContainer c)
            => c.Background(COLOR_MARINO).PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer CeldaDato(IContainer c)
            => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4).PaddingHorizontal(5);

        private static void Dato(ColumnDescriptor col, string etiqueta, string valor)
        {
            col.Item().PaddingBottom(4).Column(c =>
            {
                c.Item().Text(etiqueta).FontSize(8).FontColor(Colors.Grey.Darken1);
                c.Item().Text(string.IsNullOrWhiteSpace(valor) ? "-" : valor).FontSize(10).SemiBold();
            });
        }

        private static void Bloque(ColumnDescriptor col, string titulo, Action<ColumnDescriptor> contenido)
        {
            col.Item().Background(COLOR_GRIS).Padding(10).Column(c =>
            {
                c.Item().PaddingBottom(5).Text(titulo).FontSize(10).Bold().FontColor(COLOR_MARINO);
                contenido(c);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // FACTURA de estadía (PN1: Check-IN / Check-OUT)
        // ─────────────────────────────────────────────────────────────
        public static void FacturaPdf(FacturaReporteBE f, string ruta, string generadoPor)
        {
            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    Encabezado(page, "FACTURA " + f.NroFactura, "Gestión hotelera · Comprobante de estadía");
                    Pie(page, generadoPor);

                    page.Content().PaddingVertical(14).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c => Dato(c, "Fecha de emisión", f.FechaEmision.ToString("dd/MM/yyyy HH:mm", AR)));
                            row.RelativeItem().Column(c => Dato(c, "Estado", f.Estado));
                            row.RelativeItem().Column(c => Dato(c, "Reserva N°", f.IdReserva.ToString()));
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(6).Column(c => Bloque(c, "Huésped", b =>
                            {
                                Dato(b, "Apellido y nombre", f.Huesped);
                                Dato(b, "DNI", f.DNI);
                            }));
                            row.RelativeItem().PaddingLeft(6).Column(c => Bloque(c, "Alojamiento", b =>
                            {
                                Dato(b, "Habitación", "N° " + f.NumeroHabitacion + " — " + f.TipoHabitacion);
                                Dato(b, "Estadía", f.FechaIngreso.ToString("dd/MM/yyyy") + " al " + f.FechaSalida.ToString("dd/MM/yyyy")
                                                   + "  (" + f.Noches + (f.Noches == 1 ? " noche)" : " noches)"));
                            }));
                        });

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(4);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Element(CeldaTitulo).Text("Concepto").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Noches").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Precio por noche").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Importe").FontColor(Colors.White).Bold();
                            });
                            t.Cell().Element(CeldaDato).Text("Alojamiento — Habitación " + f.NumeroHabitacion + " (" + f.TipoHabitacion + ")");
                            t.Cell().Element(CeldaDato).AlignRight().Text(f.Noches.ToString());
                            t.Cell().Element(CeldaDato).AlignRight().Text(Plata(f.PrecioPorNoche));
                            t.Cell().Element(CeldaDato).AlignRight().Text(Plata(f.Subtotal));
                        });

                        col.Item().AlignRight().Width(240).Column(c =>
                        {
                            c.Spacing(3);
                            c.Item().Row(r => { r.RelativeItem().Text("Subtotal"); r.ConstantItem(100).AlignRight().Text(Plata(f.Subtotal)); });
                            c.Item().Row(r => { r.RelativeItem().Text("Impuestos"); r.ConstantItem(100).AlignRight().Text(Plata(f.Impuestos)); });
                            c.Item().PaddingTop(2).LineHorizontal(1).LineColor(COLOR_MARINO);
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL").Bold().FontSize(12).FontColor(COLOR_MARINO);
                                r.ConstantItem(100).AlignRight().Text(Plata(f.MontoTotal)).Bold().FontSize(12).FontColor(COLOR_MARINO);
                            });
                        });

                        string medio = string.IsNullOrWhiteSpace(f.MetodoPago) ? "Sin datos de pago"
                            : f.MetodoPago + (string.IsNullOrWhiteSpace(f.UltimosCuatroTarjeta) ? "" : " (tarjeta terminada en " + f.UltimosCuatroTarjeta + ")");
                        col.Item().Column(c => Bloque(c, "Pago", b =>
                        {
                            Dato(b, "Medio de pago", medio);
                            Dato(b, "Registrada por", f.EmitidaPor);
                        }));
                    });
                });
            }).GeneratePdf(ruta);
        }

        // ─────────────────────────────────────────────────────────────
        // LISTADO de facturas
        // ─────────────────────────────────────────────────────────────
        public static void ListadoFacturasPdf(IList<FacturaReporteBE> facturas, string periodo, string ruta, string generadoPor)
        {
            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    Encabezado(page, "Reporte de facturas", periodo);
                    Pie(page, generadoPor);

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(62);
                                cd.ConstantColumn(72);
                                cd.RelativeColumn(3);
                                cd.ConstantColumn(60);
                                cd.RelativeColumn(2);
                                cd.ConstantColumn(40);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.ConstantColumn(60);
                            });
                            t.Header(h =>
                            {
                                foreach (var s in new[] { "Factura", "Emisión", "Huésped", "Hab.", "Medio de pago", "Noches", "Subtotal", "Total", "Estado" })
                                    h.Cell().Element(CeldaTitulo).Text(s).FontColor(Colors.White).Bold();
                            });
                            foreach (var f in facturas)
                            {
                                t.Cell().Element(CeldaDato).Text(f.NroFactura);
                                t.Cell().Element(CeldaDato).Text(f.FechaEmision.ToString("dd/MM/yyyy", AR));
                                t.Cell().Element(CeldaDato).Text(f.Huesped);
                                t.Cell().Element(CeldaDato).Text(f.NumeroHabitacion);
                                t.Cell().Element(CeldaDato).Text(string.IsNullOrWhiteSpace(f.MetodoPago) ? "-" : f.MetodoPago);
                                t.Cell().Element(CeldaDato).AlignRight().Text(f.Noches.ToString());
                                t.Cell().Element(CeldaDato).AlignRight().Text(Plata(f.Subtotal));
                                t.Cell().Element(CeldaDato).AlignRight().Text(Plata(f.MontoTotal));
                                t.Cell().Element(CeldaDato).Text(f.Estado);
                            }
                        });

                        decimal total = facturas.Sum(x => x.MontoTotal);
                        col.Item().AlignRight().Text(facturas.Count + (facturas.Count == 1 ? " factura" : " facturas") + "  ·  Total facturado: " + Plata(total))
                            .Bold().FontSize(11).FontColor(COLOR_MARINO);

                        // Resumen por medio de pago
                        var resumen = facturas
                            .GroupBy(x => string.IsNullOrWhiteSpace(x.MetodoPago) ? "Sin datos" : x.MetodoPago)
                            .OrderByDescending(g => g.Sum(x => x.MontoTotal)).ToList();
                        col.Item().PaddingTop(6).Width(380).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.ConstantColumn(60); cd.RelativeColumn(2); });
                            t.Header(h =>
                            {
                                h.Cell().Element(CeldaTitulo).Text("Resumen por medio de pago").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Cant.").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Total").FontColor(Colors.White).Bold();
                            });
                            foreach (var g in resumen)
                            {
                                t.Cell().Element(CeldaDato).Text(g.Key);
                                t.Cell().Element(CeldaDato).AlignRight().Text(g.Count().ToString());
                                t.Cell().Element(CeldaDato).AlignRight().Text(Plata(g.Sum(x => x.MontoTotal)));
                            }
                        });
                    });
                });
            }).GeneratePdf(ruta);
        }

        // ─────────────────────────────────────────────────────────────
        // ORDEN DE COMPRA (PN2: Reposición de insumos)
        // ─────────────────────────────────────────────────────────────
        public static void OrdenCompraPdf(OrdenCompraBE o, ProveedorBE? p, string ruta, string generadoPor)
        {
            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    Encabezado(page, "ORDEN DE COMPRA " + o.NroOrden, "Reposición de insumos · Orden de compra");
                    Pie(page, generadoPor);

                    page.Content().PaddingVertical(14).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c => Dato(c, "Fecha de emisión", o.FechaEmision.ToString("dd/MM/yyyy HH:mm", AR)));
                            row.RelativeItem().Column(c => Dato(c, "Estado", o.Estado));
                            row.RelativeItem().Column(c => Dato(c, "Plazo de entrega", o.PlazoDias.HasValue ? o.PlazoDias + " días" : "-"));
                            row.RelativeItem().Column(c => Dato(c, "Enviada al proveedor",
                                o.FechaEnvio.HasValue ? o.FechaEnvio.Value.ToString("dd/MM/yyyy HH:mm", AR) : "No enviada"));
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(6).Column(c => Bloque(c, "Proveedor", b =>
                            {
                                Dato(b, "Razón social", o.NombreProveedor);
                                Dato(b, "Contacto", p?.Contacto ?? "");
                                Dato(b, "Correo", string.IsNullOrWhiteSpace(o.EmailProveedor) ? (p?.Email ?? "") : o.EmailProveedor);
                                Dato(b, "Teléfono", p?.Telefono ?? "");
                                Dato(b, "Condiciones de pago", p?.CondicionesPago ?? "");
                            }));
                            row.RelativeItem().PaddingLeft(6).Column(c => Bloque(c, "Entrega", b =>
                            {
                                Dato(b, "Dirección de entrega", o.DireccionEntrega);
                                if (!string.IsNullOrWhiteSpace(o.NroPreOrden))
                                    Dato(b, "Pre-orden de origen", o.NroPreOrden);
                                else if (o.IdSolicitud.HasValue)
                                    Dato(b, "Solicitud de origen", "N° " + o.IdSolicitud);
                                if (!string.IsNullOrWhiteSpace(o.NroOperacionPago))
                                    Dato(b, "Pago (transferencia)", o.NroOperacionPago +
                                        (o.FechaPago.HasValue ? " · " + o.FechaPago.Value.ToString("dd/MM/yyyy HH:mm", AR) : ""));
                            }));
                        });

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(4);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Element(CeldaTitulo).Text("Insumo").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Cantidad").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Precio unitario").FontColor(Colors.White).Bold();
                                h.Cell().Element(CeldaTitulo).AlignRight().Text("Importe").FontColor(Colors.White).Bold();
                            });
                            t.Cell().Element(CeldaDato).Text(o.NombreInsumo);
                            t.Cell().Element(CeldaDato).AlignRight().Text(Num(o.Cantidad));
                            t.Cell().Element(CeldaDato).AlignRight().Text(Plata(o.PrecioUnitario));
                            t.Cell().Element(CeldaDato).AlignRight().Text(Plata(o.PrecioTotal));
                        });

                        col.Item().AlignRight().Width(240).Column(c =>
                        {
                            c.Item().PaddingTop(2).LineHorizontal(1).LineColor(COLOR_MARINO);
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL DE LA ORDEN").Bold().FontSize(12).FontColor(COLOR_MARINO);
                                r.ConstantItem(100).AlignRight().Text(Plata(o.PrecioTotal)).Bold().FontSize(12).FontColor(COLOR_MARINO);
                            });
                        });
                    });
                });
            }).GeneratePdf(ruta);
        }

        // ─────────────────────────────────────────────────────────────
        // LISTADO de órdenes de compra
        // ─────────────────────────────────────────────────────────────
        public static void ListadoOrdenesPdf(IList<OrdenCompraBE> ordenes, string filtro, string ruta, string generadoPor)
        {
            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    Encabezado(page, "Reporte de órdenes de compra", filtro);
                    Pie(page, generadoPor);

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(66);
                                cd.ConstantColumn(72);
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(3);
                                cd.ConstantColumn(56);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.ConstantColumn(80);
                            });
                            t.Header(h =>
                            {
                                foreach (var s in new[] { "Orden", "Emisión", "Proveedor", "Insumo", "Cantidad", "Precio unit.", "Total", "Estado" })
                                    h.Cell().Element(CeldaTitulo).Text(s).FontColor(Colors.White).Bold();
                            });
                            foreach (var o in ordenes)
                            {
                                t.Cell().Element(CeldaDato).Text(o.NroOrden);
                                t.Cell().Element(CeldaDato).Text(o.FechaEmision.ToString("dd/MM/yyyy", AR));
                                t.Cell().Element(CeldaDato).Text(o.NombreProveedor);
                                t.Cell().Element(CeldaDato).Text(o.NombreInsumo);
                                t.Cell().Element(CeldaDato).AlignRight().Text(Num(o.Cantidad));
                                t.Cell().Element(CeldaDato).AlignRight().Text(Plata(o.PrecioUnitario));
                                t.Cell().Element(CeldaDato).AlignRight().Text(Plata(o.PrecioTotal));
                                t.Cell().Element(CeldaDato).Text(o.Estado);
                            }
                        });

                        decimal total = ordenes.Sum(x => x.PrecioTotal);
                        col.Item().AlignRight().Text(ordenes.Count + (ordenes.Count == 1 ? " orden" : " órdenes") + "  ·  Total comprado: " + Plata(total))
                            .Bold().FontSize(11).FontColor(COLOR_MARINO);
                    });
                });
            }).GeneratePdf(ruta);
        }
    }
}
