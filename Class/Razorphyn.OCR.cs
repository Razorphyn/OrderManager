using PDFtoImage;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using Tesseract;
using static OrderManager.Class.SupportClasses;

namespace OrderManager.Class
{
    internal class OCR
    {
        internal static void ExtarctImagesFromPDF(string PDF, string ImageSavePath)
        {
            byte[] fileBytes = File.ReadAllBytes(PDF);
            var map = Conversion.ToImages(fileBytes);
            int i = 0;

            foreach (SKBitmap mapbyte in map)
            {
                using (var image = SKImage.FromBitmap(mapbyte))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 250))
                {
                    // save the data to a stream
                    using (var stream = File.OpenWrite(Path.Combine(ImageSavePath, i + 1 + ".png")))
                    {
                        data.SaveTo(stream);
                    }
                }
                i++;
            }
        }

        internal static List<Word> ExtractWordFromImage(string Folder, string language, int page = 0)
        {
            List<Word> words = new List<Word>();

            DirectoryInfo d = new(Folder);
            FileInfo[] Files = d.GetFiles("*.png"); //Getting sql files

            Array.Sort(Files, delegate (FileInfo x, FileInfo y) { return Convert.ToInt32(Path.GetFileNameWithoutExtension(x.Name)).CompareTo(Convert.ToInt32(Path.GetFileNameWithoutExtension(y.Name))); });

            int i = 1;
            foreach (FileInfo File in Files)
            {
                if (page > 0 && i != page)
                    continue;

                try
                {
                    using (var engine = new TesseractEngine(@"./tessdata", language, EngineMode.Default))
                    {
                        using (var img = Pix.LoadFromFile(File.FullName))
                        {
                            using (var CurPage = engine.Process(img, PageSegMode.AutoOsd))
                            {
                                using (var iter = CurPage.GetIterator())
                                {
                                    iter.Begin();
                                    do
                                    {
                                        if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                                        {
                                            string text = iter.GetText(PageIteratorLevel.TextLine).Trim();
                                            if (!string.IsNullOrEmpty(text))
                                            {
                                                words.Add(new Word()
                                                {
                                                    Value = iter.GetText(PageIteratorLevel.Word).ToLower(),
                                                    X = rect.X1,
                                                    Y = rect.Y1,
                                                    X2 = rect.X2,
                                                    Y2 = rect.Y2
                                                });
                                            }
                                        }
                                    } while (iter.Next(PageIteratorLevel.Word));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    OnTopMessage.Error("Errore durante lettura testo da immagine. Codice: " + e.Message);
                }
                i++;
            }

            return words;
        }

        internal static string[] ExtractLineFromImage(string Folder, string language, int page = 0)
        {
            List<string> lines = new List<string>();

            DirectoryInfo d = new(Folder);
            FileInfo[] Files = d.GetFiles("*.png"); //Getting sql files

            Array.Sort(Files, delegate (FileInfo x, FileInfo y) { return Convert.ToInt32(Path.GetFileNameWithoutExtension(x.Name)).CompareTo(Convert.ToInt32(Path.GetFileNameWithoutExtension(y.Name))); });

            int i = 1;

            foreach (FileInfo File in Files)
            {
                if (page > 0 && i != page)
                    continue;

                try
                {
                    using (var engine = new TesseractEngine(@"./tessdata", language, EngineMode.Default))
                    {
                        using (var img = Pix.LoadFromFile(File.FullName))
                        {
                            using (var CurPage = engine.Process(img, PageSegMode.AutoOsd))
                            {
                                using (var iter = CurPage.GetIterator())
                                {
                                    iter.Begin();
                                    do
                                    {
                                        if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
                                        {
                                            string text = iter.GetText(PageIteratorLevel.TextLine).Trim();
                                            if (!string.IsNullOrEmpty(text))
                                            {
                                                lines.Add(text);
                                            }
                                        }
                                    } while (iter.Next(PageIteratorLevel.TextLine));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    OnTopMessage.Error("Errore durante lettura testo da immagine. Codice: " + e.Message);
                }
                i++;
            }

            return lines.ToArray();
        }

        internal static Word FindClosestFollowingWord(List<Word> words, Word center)
        {
            Word target = new Word();
            long distance = long.MaxValue;
            long computedDistance;

            foreach (Word word in words)
            {
                if (word.Y2 > center.Y && word.Value != center.Value)
                {
                    computedDistance = Convert.ToInt64(Math.Pow(word.Y - center.Y, 2) + Math.Pow(word.X - center.X, 2));
                    if (computedDistance < distance)
                    {
                        distance = computedDistance;
                        target = word;
                    }
                }
            }

            return target;
        }
    }
}