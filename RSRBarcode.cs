using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using PDFtoImage;

namespace RSRBarcode
{
    /// <summary>
    /// Clase para leer y generar códigos de barras (CODE_39) y códigos QR desde PowerBuilder,
    /// usando el .NET DLL Importer.
    /// <para>
    /// Esta es la versión <b>migrada a .NET 10</b>. ¿Qué cambió y por qué? Os lo cuento, que tiene
    /// miga:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>PdfiumViewer → PDFtoImage:</b> PdfiumViewer está abandonado; PDFtoImage (MIT)
    ///   rasteriza PDF apoyándose en PDFium + SkiaSharp y se mantiene al día con .NET.</item>
    ///   <item><b>ZXing.Windows.Compatibility → ZXing.Net.Bindings.SkiaSharp:</b> la lectura y
    ///   generación de códigos ya no dependen de System.Drawing (GDI+/Windows), sino del binding
    ///   de SkiaSharp, multiplataforma.</item>
    ///   <item><b>System.Drawing solo para re-codificar a BMP:</b> SkiaSharp no sabe exportar BMP,
    ///   así que ese único paso lo seguimos haciendo con System.Drawing (ver <see cref="PdfToBmp"/>).</item>
    /// </list>
    /// </summary>
    public class RSRbarcode
    {
        #region Copyright
        /*
        Class				: RSRbarcode
        Author				: Ramón San Félix Ramón
        E-Mail              : rsrsystem.soft@gmail.com
        Scope  				: Public

        Description			: Class to read and write barcodes in PowerBuilder
        Behaviour			: Ready for use in new versions of PowerBUilder using .Net Dll Importer


        --------------------------------------------  CopyRight -----------------------------------------------------
        Copyright © 2023 by Ramón San Félix Ramón. All rights reserved.
        Any distribution of this application or its source code by persons other than Ramón San Félix without their
        express consent is prohibited.
        To be aware of what I publish visit my blog: https://rsrsystem.blogspot.com/
        -------------------------------------------  Revisions -------------------------------------------------------
        1.0 		Inital Version																		-	2023-03-06
        1.1         Change GhostScript reference to PdfiumViewer                                        -   2023-03-08
        1.2         Migracion a .NET 10: PdfiumViewer -> PDFtoImage y ZXing SkiaSharp                   -   2026-06-27
        */
        #endregion

        #region Resolución de DLLs nativas (clave al hostear desde PowerBuilder)
        /*
         * ¿Por qué esto? SkiaSharp (libSkiaSharp) y PDFium (pdfium) son librerías NATIVAS que se
         * entregan bajo 'runtimes\win-<arch>\native\'. En una app .NET normal, el host resuelve esa
         * ruta leyendo RSRBarcode.deps.json. Pero cuando nos hostea un proceso ajeno (PowerBuilder,
         * vía .NET DLL Importer), el host es PB y usa SU propio deps.json: la carpeta 'runtimes\' de
         * RSRBarcode NUNCA entra en el search path de nativas. Resultado: o no encuentra la DLL, o
         * carga una de bitness equivocado -> BadImageFormatException (0x8007000B) al primer uso.
         *
         * Solución: registramos un DllImportResolver propio que carga libSkiaSharp/pdfium desde la
         * subcarpeta 'runtimes\win-<arch>\native\' que cuelga de ESTA DLL, eligiendo arquitectura
         * según el bitness del proceso. Así funciona igual bajo PB (x86 o x64) que como app .NET.
         */
        static RSRbarcode()
        {
            TryRegister(typeof(SKBitmap).Assembly);        // libSkiaSharp (SkiaSharp)
            TryRegister(typeof(Conversion).Assembly);      // pdfium (PDFtoImage)
        }

        private static void TryRegister(Assembly assembly)
        {
            // SetDllImportResolver lanza si el assembly ya tiene uno: lo ignoramos (mejor el suyo).
            try { NativeLibrary.SetDllImportResolver(assembly, ResolveNative); } catch { }
        }

        private static IntPtr ResolveNative(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            string baseName = Path.GetFileNameWithoutExtension(libraryName);
            if (!baseName.Equals("libSkiaSharp", StringComparison.OrdinalIgnoreCase) &&
                !baseName.Equals("pdfium", StringComparison.OrdinalIgnoreCase))
            {
                return IntPtr.Zero; // no es de las nuestras -> resolución por defecto
            }

            string rid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => "win-x64",
            };

            string baseDir = Path.GetDirectoryName(typeof(RSRbarcode).Assembly.Location)!;
            string candidate = Path.Combine(baseDir, "runtimes", rid, "native", baseName + ".dll");

            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle))
            {
                return handle;
            }
            return IntPtr.Zero; // fallback a la resolución por defecto
        }
        #endregion

        /// <summary>
        /// Lee el código de barras contenido en la <b>primera página de un PDF</b>.
        /// </summary>
        /// <param name="inputFile">Ruta completa del PDF de entrada.</param>
        /// <returns>El texto decodificado; cadena vacía si no se reconoce nada.</returns>
        /// <remarks>
        /// Es un método "todo en uno": primero rasteriza la página a un BMP temporal (junto al PDF),
        /// lo lee con <see cref="ReadBarcode"/> y borra el temporal. Cómodo de llamar desde PowerBuilder.
        /// </remarks>
        public string ReadBarcodePDF(string inputFile)
        {
            // Generamos un BMP temporal con el mismo nombre que el PDF, en su misma carpeta.
            // El '!' (null-forgiving) le dice al compilador que GetDirectoryName no será null aquí,
            // ya que 'inputFile' es una ruta de fichero válida.
            string imageName = PdfToBmp(inputFile, Path.Combine(Path.GetDirectoryName(inputFile)!, Path.GetFileNameWithoutExtension(inputFile) + ".bmp"), 1, 1);

            string result = ReadBarcode(imageName);

            File.Delete(imageName); // limpiamos el temporal: no dejamos basura en disco

            return result;

        }
        /// <summary>
        /// Lee un código de barras (CODE_39) o un código QR desde un fichero de imagen.
        /// </summary>
        /// <param name="imageName">Ruta completa del fichero de imagen (PNG, BMP...).</param>
        /// <returns>El texto decodificado; cadena vacía si no hay código; o el mensaje de error.</returns>
        public string ReadBarcode(string imageName)
        {
            try
            {
                // Acotamos a los dos formatos que de verdad usamos: menos formatos = lectura
                // más rápida y con menos falsos positivos. Expresión de colección [...] de C# 12.
                List<BarcodeFormat> formatList = [BarcodeFormat.CODE_39, BarcodeFormat.QR_CODE];

                // BarcodeReader del binding SkiaSharp de ZXing (antes ZXing.Windows.Compatibility).
                // Fijaos: ahora trabaja con SKBitmap, no con System.Drawing.Bitmap.
                BarcodeReader reader = new() { AutoRotate = true };
                reader.Options.PossibleFormats = formatList;
                reader.Options.TryHarder = true;    // se esfuerza más a costa de algo de CPU
                reader.Options.TryInverted = true;  // prueba también con colores invertidos

                // SKBitmap es un recurso nativo de SkiaSharp: 'using' lo libera al salir.
                using SKBitmap image = SKBitmap.Decode(imageName);
                Result result = reader.Decode(image);

                if (result == null)
                {
                    return "";
                }
                return result.Text;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        /// <summary>
        /// Genera un código de barras <b>CODE_39</b> a partir de un texto y lo guarda como PNG.
        /// </summary>
        /// <param name="source">Texto a codificar.</param>
        /// <param name="outputFile">Ruta del PNG de salida.</param>
        public void BarcodeGenerate(string source, string outputFile)
        {
            // Tamaño/margen fijos pensados para una etiqueta CODE_39 típica.
            BarcodeWriter writer = new()
            {
                Format = BarcodeFormat.CODE_39,
                Options = new EncodingOptions
                {
                    Height = 41,
                    Width = 423,
                    PureBarcode = true, // solo las barras, sin el texto debajo
                    Margin = 0,
                },
            };

            // Write() devuelve un SKBitmap (binding SkiaSharp); lo guardamos como PNG nativo.
            using SKBitmap bitmap = writer.Write(source);
            SaveAsPng(bitmap, outputFile);
        }

        /// <summary>
        /// Genera un <b>código QR</b> a partir de un texto y lo guarda como PNG.
        /// </summary>
        /// <param name="source">Texto a codificar en el QR.</param>
        /// <param name="outputFile">Ruta del PNG de salida.</param>
        public void QrGenerate(string source, string outputFile)
        {
            // Un QR es cuadrado, de ahí el alto = ancho. Margin = 2 deja la "quiet zone" mínima.
            BarcodeWriter writer = new()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 230,
                    Width = 230,
                    PureBarcode = true,
                    Margin = 2,
                },
            };

            using SKBitmap bitmap = writer.Write(source);
            SaveAsPng(bitmap, outputFile);
        }

        /// <summary>
        /// Rasteriza una o varias páginas de un PDF a fichero(s) BMP.
        /// </summary>
        /// <param name="source">Ruta del PDF de entrada.</param>
        /// <param name="outputFile">Ruta del BMP de salida (si hay varias páginas se le añade "_n").</param>
        /// <param name="pageFrom">Primera página a convertir (base 1).</param>
        /// <param name="pageTo">Última página a convertir (base 1, inclusive).</param>
        /// <returns>La ruta del último BMP generado; o el mensaje de error si algo falla.</returns>
        public string PdfToBmp(string source, string outputFile, int pageFrom, int pageTo)
        {
            try
            {
                // PDFtoImage (PDFium + SkiaSharp) sustituye a PdfiumViewer.
                byte[] pdfBytes = File.ReadAllBytes(source);

                // Ojo: PDFtoImage numera las páginas en base 0, por eso 'pageFrom - 1'.
                for (int i = pageFrom - 1; i < pageTo; i++)
                {
                    string pageFile = (pageTo > pageFrom)
                        ? Path.Combine(Path.GetDirectoryName(outputFile)!, Path.GetFileNameWithoutExtension(outputFile) + "_" + i + ".bmp")
                        : outputFile;

                    // 300 DPI: resolución generosa para que ZXing lea bien barras finas.
                    using SKBitmap skBitmap = Conversion.ToImage(pdfBytes, page: i, options: new RenderOptions(Dpi: 300));
                    // SkiaSharp no exporta BMP: pasamos a PNG en memoria y re-codificamos a BMP
                    // con System.Drawing (único punto donde seguimos dependiendo de GDI+/Windows).
                    using SKData data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
                    using var ms = new MemoryStream();
                    data.SaveTo(ms);
                    ms.Position = 0;
                    using var bmp = new Bitmap(ms);
                    bmp.Save(pageFile, ImageFormat.Bmp);
                    outputFile = pageFile;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return outputFile;
        }

        // Guarda un SKBitmap como PNG usando SkiaSharp nativo (sin pasar por System.Drawing).
        // Helper privado reutilizado por BarcodeGenerate y QrGenerate.
        private static void SaveAsPng(SKBitmap bitmap, string outputFile)
        {
            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var fs = File.OpenWrite(outputFile);
            data.SaveTo(fs);
        }
    }
}
