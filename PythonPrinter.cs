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
using CG.Web.MegaApiClient;
using System.Collections.Generic;
using System.Collections;
using UglyToad.PdfPig.Content;
using System.Xml.Linq;

namespace PrintingPdfForPython
{
    public class PythonPrinter
    {
        public static void PythonPrint(String filepath, String printerName, String jobName, int duplexMode)
        {
            PdfDocument document = new PdfDocument();
            document.PrintSettings.PrinterName = printerName;
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
                default:
                    document.PrintSettings.Duplex = Duplex.Default;
                    break;
            }
            document.LoadFromFile(filepath);
            document.PrintSettings.PrintController = new StandardPrintController();
            try
            {
                document.Print();
            }
            catch (Exception e)
            {
                document.Close();
                Console.Write(e);
            }


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
            PdfAppearance apprearance = new PdfAppearance(stamp)
            {
                Normal = template
            };
            stamp.Appearance = apprearance;
            page.AnnotationsWidget.Add(stamp);
            string output = filename;
            document.SaveToFile(output);
            document.Close();
            return output;
        }
    }
    public class UpdateFromMegaNz
    {
        public static Dictionary<String, INode> nameNode;
        public static MegaApiClient client = new MegaApiClient();
        public static IEnumerable<INode> nodes;

        public void UpdateFromMega()
        {
            UpdateFromMegaNz.nameNode = new Dictionary<string, INode>();
            UpdateFromMegaNz.client.Login("microrazrab@mail.ru", "645186885");
            UpdateFromMegaNz.nodes = client.GetNodes();
            INode parent = UpdateFromMegaNz.nodes.Single(n => n.Type == NodeType.Root);
            DisplayNodesRecursive(UpdateFromMegaNz.nodes, parent);
        }

        public void DisplayNodesRecursive(IEnumerable<INode> nodes, INode parent)
        {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);
            foreach (INode child in children)
            {
                UpdateFromMegaNz.nameNode[child.Name] = child;
                if (child.Type == NodeType.Directory)
                {
                    DisplayNodesRecursive(nodes, child);
                }
            }
        }
        void DownloadChildrensRecursive(IEnumerable<INode> nodes, INode parent, string rootDirSaveTo)
            {
            IEnumerable<INode> children = nodes.Where(x => x.ParentId == parent.Id);

            foreach (INode child in children)
            { 
                if (child.Type == NodeType.Directory)
                {
                    Directory.CreateDirectory(Path.Combine(rootDirSaveTo, child.Name));
                    DownloadChildrensRecursive(nodes, child, Path.Combine(rootDirSaveTo, child.Name));
                }
                else
                {
                    Console.WriteLine($"Saving to { Path.Combine(rootDirSaveTo, child.Name)}");
                    UpdateFromMegaNz.client.DownloadFile(child, Path.Combine(rootDirSaveTo, child.Name));
                }
            }
        }

   
        public void DownloadFilesFromMegaNz(string[] filenames, string[] filepathsToDownload)
        {
            var fnfp = filenames.Zip(filepathsToDownload, (n, w) => new { fn = n, fp = w });
            foreach (var ff in fnfp)
            {
            if (UpdateFromMegaNz.nameNode.ContainsKey(ff.fn))
                {
                    if (UpdateFromMegaNz.nameNode[ff.fn].Type == NodeType.File)
                    {
                        Console.WriteLine($"Downloading {ff.fn}");
                        UpdateFromMegaNz.client.DownloadFile(UpdateFromMegaNz.nameNode[ff.fn], ff.fp);
                        Console.WriteLine($"Downloaded {ff.fn}");
                    }
                    else
                    {
                        DownloadChildrensRecursive(UpdateFromMegaNz.nodes, UpdateFromMegaNz.nameNode[ff.fn], ff.fp);
                    }



                }

            }
        }
    }

}
