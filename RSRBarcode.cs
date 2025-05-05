using PdfiumViewer;
using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;
using ZXing.Windows.Compatibility;

namespace RSRBarcode
{
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
        */
        #endregion
        public string ReadBarcodePDF(string inputFile)
        {
            string imageName = PdfToBmp(inputFile, Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile) + ".bmp"), 1, 1);

            string result = ReadBarcode(imageName);

            File.Delete(imageName);

            return result;

        }
        public string ReadBarcode(string imageName)
        {
            try
            {
                BarcodeReader reader;
                List<BarcodeFormat> formatList = new List<BarcodeFormat>();
                formatList.Add(BarcodeFormat.CODE_39);
                formatList.Add(BarcodeFormat.QR_CODE);

                reader = new BarcodeReader { AutoRotate = true };
                reader.Options.PossibleFormats = formatList;
                reader.Options.TryHarder = true;
                reader.Options.TryInverted = true;

                Bitmap Image = (Bitmap)Bitmap.FromFile(imageName);
                Result result = reader.Decode(Image);
                Image.Dispose();

                if (result == null)
                {
                    return "";
                }
                return result.Text.ToString();

            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        public void BarcodeGenerate(string source, string outputFile)
        {
            BarcodeWriter writer = new BarcodeWriter();

            writer.Format = BarcodeFormat.CODE_39;
            writer.Options = new EncodingOptions
            {
                Height = 41,
                Width = 423,
                PureBarcode = true,
                Margin = 0,
            };
            var bitmap = writer.Write(source);
            bitmap.Save(outputFile);
            bitmap.Dispose();
            return;
        }
        public void QrGenerate(string source, string outputFile)
        {
            BarcodeWriter writer = new BarcodeWriter();

            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new EncodingOptions
            {
                Height = 230,
                Width = 230,
                PureBarcode = true,
                Margin = 2,
            };
            var bitmap = writer.Write(source);
            bitmap.Save(outputFile);
            bitmap.Dispose();
            return;
        }

        public string PdfToBmp(string source, string outputFile, int pageFrom, int pageTo)
        {

            try
            {
                var document = PdfDocument.Load(source);

                for (int i = pageFrom - 1; i < pageTo; i++)
                {
                    var dpi = 300;

                    using (var image = document.Render(i, dpi, dpi, PdfRenderFlags.CorrectFromDpi))
                    {
                        var encoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Bmp.Guid);
                        var encParams = new EncoderParameters(1);
                        encParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

                        if (pageTo > pageFrom)
                        {
                            outputFile = Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile) + "_" + i + ".bmp");
                        }

                        image.Save(outputFile, encoder, encParams);
                    }
                }
                document.Dispose();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return outputFile;
        }
    }
}
