using Spire.Pdf;
using Spire.Pdf.Annotations;
using Spire.Pdf.Annotations.Appearance;
using Spire.Pdf.Graphics;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using Tesseract;
using UglyToad.PdfPig.Writer;

namespace PrintingPdfForPython
{
    public class PythonPrinter
    {
        public static void PythonPrint(String filepath, String printerName, String jobName, int duplexMode)
        {
            PdfDocument document = new PdfDocument();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.Contains(printerName))
                {
                    document.PrintSettings.PrinterName = printer;
                    break;
                }
            }
            document.PrintSettings.DocumentName = jobName;
            switch (duplexMode)
            {
                case 0:
                    document.PrintSettings.Duplex = Duplex.Simplex;
                    break;
                case 1:
                    document.PrintSettings.Duplex = Duplex.Vertical;
                    break;
                case 2:
                    document.PrintSettings.Duplex = Duplex.Horizontal;
                    break;
                default: break;
            }
            document.LoadFromFile(filepath);
            document.PrintSettings.PrintController = new StandardPrintController();
            document.Print();
            document.Close();
        }

        public static String GeneratePdfFromImage(string filename)
        {
            // Create a pdf document with a section and page added.
            PdfDocument pdf = new PdfDocument();
            PdfSection section = pdf.Sections.Add();
            PdfPageBase page = pdf.Pages.Add();

            //Load a tiff image from system
            PdfImage image = PdfImage.FromFile(filename);
            //Set image display location and size in PDF
            //Calculate rate
            float widthFitRate = image.PhysicalDimension.Width / page.Canvas.ClientSize.Width;
            float heightFitRate = image.PhysicalDimension.Height / page.Canvas.ClientSize.Height;
            float fitRate = Math.Max(widthFitRate, heightFitRate);
            //Calculate the size of image 
            float fitWidth = image.PhysicalDimension.Width / fitRate;
            float fitHeight = image.PhysicalDimension.Height / fitRate;
            //Draw image
            page.Canvas.DrawImage(image, 0, 30, fitWidth, fitHeight);

            string output = filename + ".pdf";

            pdf.SaveToFile(output);
            pdf.Close();
            return output;
        }

        public static String ConcatenatePDF(String[] filenames, bool isDel)
        {
            var fileBytes = filenames.Select(File.ReadAllBytes).ToList();
            var resultFileBytes = PdfMerger.Merge(fileBytes);
            File.WriteAllBytes(filenames[0], resultFileBytes);

            if (isDel)
            {
                for (int i = 1; i < filenames.Length; i++)
                {
                    File.Delete(filenames[i]);
                }
            }
            return filenames[0];
        }

        public static String ExtractTextFromPdf(String filename)
        {
            PdfDocument document = new PdfDocument();
            document.LoadFromFile(filename);
            PdfDocument pdf = new PdfDocument();
            pdf.LoadFromFile(filename);
            int pageIndex = 0;
            var tempImg = Path.GetTempFileName();
            using (Image image = pdf.SaveAsImage(pageIndex, 300, 300))
            {
                image.Save(tempImg, System.Drawing.Imaging.ImageFormat.Png);
            }
            String extractedText = "";
            using (var engine = new TesseractEngine(@".", "rus", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(tempImg))
                {
                    using (var page = engine.Process(img))
                    {
                        extractedText = page.GetText();
                    }
                }
            }
            File.Delete(tempImg);
            pdf.Close();
            document.Close();
            return extractedText;
        }



        public static String AddStamp(String filename, String numAppeal, String numDoc)
        {
            PdfDocument document = new PdfDocument();
            document.LoadFromFile(filename);
            PdfPageBase page = document.Pages[0];
            PdfTemplate template = new PdfTemplate(600, 70);
            PdfTrueTypeFont font1 = new PdfTrueTypeFont(new Font("Times New Roman", 8f, FontStyle.Bold), true);
            PdfSolidBrush brush = new PdfSolidBrush(new PdfRGBColor(80, 80, 80));
            RectangleF rectangle = new RectangleF(new PointF(5, 5), template.Size);
            template.Graphics.DrawString(numAppeal, font1, brush, new PointF(480, 10));
            template.Graphics.DrawString(numDoc, font1, brush, new PointF(480, 20));
            if ("Квитанция".Equals(numDoc))
            {
                PdfTrueTypeFont font2 = new PdfTrueTypeFont(new Font("Times New Roman", 12f, FontStyle.Bold), true);
                template.Graphics.DrawString("         ПОСТУПИЛ В \nЭЛЕКТРОННОМ ВИДЕ", font2, brush, new PointF(410, 35));
            }
            PdfRubberStampAnnotation stamp = new PdfRubberStampAnnotation(rectangle);
            PdfAppearance apprearance = new PdfAppearance(stamp);
            apprearance.Normal = template;
            stamp.Appearance = apprearance;
            page.AnnotationsWidget.Add(stamp);
            string output = filename;
            document.SaveToFile(output);
            document.Close();
            return output;
        }
    }
}
