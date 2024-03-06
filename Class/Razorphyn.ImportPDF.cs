using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static OrderManager.Class.SupportClasses;

namespace OrderManager.Class
{
    internal class ImportPDF
    {
        internal static readonly string patterCode = @"^[0-9]+[ ]([a-zA-Z]{1,}\d{1,}[-]\d{1,})\s*([0-9,]*).*[PZ|SZ|PCE|M](\s+([0-9.,]+[ ]?[0-9]+)\s+([0-9,.]+[ ]?[0-9]+))?$";
        internal static readonly string patterPricsDisc = @"^.+[PZ|SZ|PCE]\s+([0-9,.]+)\s+([0-9,.]+)$";
        internal static readonly string patterNumberDate = @"([0-9]+) ?\/ ?([0-9\/.]+)$";
        internal static readonly string patterDate = @"([0-9\/.]+)";

        internal class Offerte
        {

            internal static string IdentifyLanguage(List<Word> words)
            {
                string Offerlang = "";
                int WordsCount = words.Count;

                Dictionary<string, string> findStrLang = new()
                                {
                                    { "offerta", "ita" },
                                    { "quotation", "eng" }
                                };

                for (int i = 0; i < WordsCount; i++)
                {
                    if (findStrLang.ContainsKey(words[i].Value))
                    {
                        Offerlang = findStrLang[words[i].Value];
                        break;
                    }
                }

                return Offerlang;
            }

            internal static Dictionary<string, string> ExtractHeader(List<Word> words, string[] lines, string Language, bool isImage)
            {
                Dictionary<string, string> offerInfo = new()
                            {
                                { "numero", "" },
                                { "cliente", "" },
                                { "data", "" }
                            };

                Dictionary<string, Dictionary<string, string>> findStrField = Helper.GetDictionarySDSS("offerta");

                words = words.OrderBy(a => a.X).ThenBy(a => a.Y).ToList();
                int WordsCount = words.Count;
                int pos;
                int c = lines.Length;

                if (!isImage)
                {
                    for (int i = 0; i < WordsCount; i++)
                    {
                        string line = Helper.BuildStringH(words, words[i].X, words[i].Y);
                        pos = line.IndexOf(findStrField[Language]["numero"]);

                        if (pos == 0)
                        {
                            offerInfo["numero"] = words[i - 1].Value;
                            offerInfo["data"] = Helper.RemoveNotIntLeft(Helper.BuildStringH(words, words[i - 1].X, words[i - 1].Y)).Split('/')[1];
                        }

                        pos = Helper.BuildStringH(words, words[i].X, words[i].Y).IndexOf(findStrField[Language]["cliente"]);
                        if (pos == 0)
                        {
                            offerInfo["cliente"] = words[i - 1].Value;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < WordsCount; i++)
                    {
                        string line = Helper.BuildStringH(words, words[i].X, words[i].Y);
                        pos = line.IndexOf(findStrField[Language]["numero"]);

                        if (pos == 0)
                        {
                            Word temp = OCR.FindClosestFollowingWord(words, words[i]);
                            offerInfo["numero"] = temp.Value;
                        }

                        pos = Helper.BuildStringH(words, words[i].X, words[i].Y).IndexOf(findStrField[Language]["cliente"]);
                        if (pos == 0)
                        {
                            offerInfo["cliente"] = OCR.FindClosestFollowingWord(words, words[i]).Value;
                        }
                    }

                    for (int i = 0; i < c; i++)
                    {
                        Match date = Regex.Match(lines[i], patterNumberDate, RegexOptions.IgnoreCase);
                        if (date.Success)
                        {
                            if (date.Groups[1].ToString() == offerInfo["numero"])
                                offerInfo["data"] = date.Groups[2].ToString();
                        }

                        pos = lines[i].IndexOf(offerInfo["numero"]);
                        if (pos > -1)
                        {
                            offerInfo["data"] = Helper.RemoveNotIntLeft(lines[i].Split('/')[1]);
                        }

                        if (string.IsNullOrEmpty(offerInfo["cliente"]))
                        {
                            pos = lines[i].Replace(" ", "").IndexOf(findStrField[Language]["cliente"], StringComparison.OrdinalIgnoreCase);
                            if (pos > -1)
                            {
                                offerInfo["cliente"] = lines[i + 1].Trim();
                            }
                        }
                    }

                }

                return offerInfo;
            }
        }

        internal class Order
        {
            internal static string IdentifyLanguage(List<Word> words)
            {
                string Offerlang = "";
                int WordsCount = words.Count;

                Dictionary<string, string> findStrLang = new()
                            {
                                { "d'ordine", "ita" },
                                { "order", "eng" },
                                { "order confirmation", "eng" }

                            };

                for (int i = 0; i < WordsCount; i++)
                {
                    if (findStrLang.ContainsKey(words[i].Value))
                    {
                        Offerlang = findStrLang[words[i].Value];
                        break;
                    }
                }

                return Offerlang;
            }

            internal static Dictionary<string, string> ExtractHeader(List<Word> words, string[] lines, string Language, bool isImage)
            {
                Dictionary<string, string> orderInfo = new()
                        {
                            { "numero", "" },
                            { "cliente", "" },
                            { "numeroOff", "" },
                            { "data", "" },
                            { "ETA", "" }
                        };

                Dictionary<string, Dictionary<string, string>> findStrField = ImportPDF.Helper.GetDictionarySDSS("Ordine");

                words = words.OrderBy(a => a.X).ThenBy(a => a.Y).ToList();
                int WordsCount = words.Count;
                int pos;
                int c = lines.Length;

                if (!isImage)
                {
                    for (int i = 0; i < WordsCount; i++)
                    {

                        foreach (KeyValuePair<string, string> searchstr in findStrField[Language])
                        {
                            if (orderInfo[searchstr.Key] != "")
                            {
                                continue;
                            }

                            string retruned = ImportPDF.Helper.BuildStringH(words, words[i].X, words[i].Y);
                            pos = retruned.IndexOf(searchstr.Value);

                            if (pos == 0)
                            {
                                int posSlash = searchstr.Value.ToString().IndexOf("/") + 1;
                                if (posSlash > 0)
                                {
                                    if (posSlash == searchstr.Value.Length)
                                    {
                                        orderInfo[searchstr.Key] = words[i - 1].Value;
                                    }
                                    else
                                    {
                                        orderInfo[searchstr.Key] = ImportPDF.Helper.BuildStringH(words, words[i - 1].X, words[i - 1].Y).Split('/')[1];
                                    }
                                }
                                else
                                {
                                    if (searchstr.Key == "ETA")
                                        orderInfo[searchstr.Key] = ImportPDF.Helper.BuildStringH(words, words[i - 1].X, words[i - 1].Y);
                                    else
                                        orderInfo[searchstr.Key] = words[i - 1].Value;
                                }
                            }
                        }

                    }

                    orderInfo["ETA"] = Helper.ReturnEtaImportOrder(orderInfo["ETA"]);
                    orderInfo["data"] = ImportPDF.Helper.RemoveNotIntLeft(ImportPDF.Helper.RemoveNotIntRight(orderInfo["data"]));
                }
                else //image
                {
                    for (int i = 0; i < WordsCount; i++)
                    {
                        string line = ImportPDF.Helper.BuildStringH(words, words[i].X, words[i].Y);
                        pos = line.IndexOf(findStrField[Language]["numero"]);

                        if (pos == 0)
                        {
                            Word temp = OCR.FindClosestFollowingWord(words, words[i]);
                            orderInfo["numero"] = temp.Value;
                        }

                        pos = ImportPDF.Helper.BuildStringH(words, words[i].X, words[i].Y).IndexOf(findStrField[Language]["cliente"]);
                        if (pos == 0)
                        {
                            orderInfo["cliente"] = OCR.FindClosestFollowingWord(words, words[i]).Value;
                        }
                    }

                    for (int i = 0; i < c; i++)
                    {
                        Match date = Regex.Match(lines[i], patterNumberDate, RegexOptions.IgnoreCase);
                        if (date.Success)
                        {
                            if (date.Groups[1].ToString() == orderInfo["numero"])
                                orderInfo["data"] = date.Groups[2].ToString();
                        }

                        if (string.IsNullOrEmpty(orderInfo["numeroOff"]))
                        {
                            pos = lines[i].Replace(" ", "").IndexOf(findStrField[Language]["numeroOff"], StringComparison.OrdinalIgnoreCase);
                            if (pos > -1)
                            {
                                string temp = lines[i + 1].Trim();
                                pos = temp.IndexOf("/");
                                if (pos > -1)
                                {
                                    orderInfo["numeroOff"] = temp.Split("/")[0].Trim();
                                }
                                else
                                {
                                    orderInfo["numeroOff"] = temp;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(orderInfo["ETA"]))
                        {
                            pos = lines[i].Replace(" ", "").IndexOf(findStrField[Language]["ETA"], StringComparison.OrdinalIgnoreCase);
                            if (pos > -1)
                            {
                                orderInfo["ETA"] = lines[i + 1].Trim();
                            }
                        }

                        if (string.IsNullOrEmpty(orderInfo["cliente"]))
                        {
                            pos = lines[i].Replace(" ", "").IndexOf(findStrField[Language]["cliente"], StringComparison.OrdinalIgnoreCase);
                            if (pos > -1)
                            {
                                orderInfo["cliente"] = lines[i + 1].Trim();
                            }
                        }
                    }

                }

                return orderInfo;
            }

            internal class Helper
            {

                internal static string ReturnEtaImportOrder(string line)
                {
                    string eta = "";

                    if (line.Contains("setti.") || line.Contains("week"))
                    {
                        string pat = @"\d{1,2}.\d{1,4}$";
                        Regex r = new Regex(pat, RegexOptions.IgnoreCase);
                        Match m = r.Match(line);

                        bool foundmatch = false;

                        if (m.Groups.Count > 0)
                        {
                            line = m.Groups[0].Value;
                            foundmatch = true;
                        }

                        if (foundmatch)
                        {
                            string[] date = line.Split('.');
                            eta = Convert.ToString(Utility.FirstDateOfWeekISO8601(Convert.ToInt32(date[1]), Convert.ToInt32(date[0])));
                        }
                        else
                        {
                            eta = "";
                        }
                    }

                    return eta;

                }
            }
        }

        internal class Helper
        {
            internal static string BuildStringH(List<Word> words, int x, int y)
            {
                string builder = "";

                foreach (Word w in words)
                {
                    if (w.Y == y && w.X >= x)
                    {
                        builder += w.Value;
                    }
                }

                return builder;
            }

            internal static string RemoveNotIntLeft(string builder)
            {
                bool isInt = false;

                while (!isInt && builder.Length > 0)
                {
                    if (!int.TryParse(builder.AsSpan(0, 1), out _))
                    {
                        builder = builder.Remove(0, 1);

                    }
                    else
                    {
                        isInt = true;
                    }
                }

                return builder;
            }

            internal static string RemoveNotIntRight(string builder)
            {
                bool isInt = false;

                while (!isInt && builder.Length > 0)
                {
                    if (!int.TryParse(builder.AsSpan(builder.Length - 1, 1), out _))
                    {
                        builder = builder.Remove(builder.Length - 1, 1);

                    }
                    else
                    {
                        isInt = true;
                    }
                }

                return builder;
            }

            internal static Dictionary<string, Dictionary<string, string>> GetDictionarySDSS(string DictCase)
            {

                Dictionary<string, Dictionary<string, string>> findStrField = new()
                            {
                                { "ita",    new  Dictionary<string, string>() },
                                { "eng",    new  Dictionary<string, string>() }
                            };

                switch (DictCase)
                {
                    case "offerta":

                        findStrField["ita"].Add("numero", "ordineno./data/");
                        findStrField["eng"].Add("numero", "number/date");

                        findStrField["ita"].Add("cliente", "no.cliente");
                        findStrField["eng"].Add("cliente", "cust.no.");

                        findStrField["ita"].Add("data", "/data");
                        findStrField["eng"].Add("data", "/date");
                        break;
                    case "OffertaItem":

                        findStrField["ita"].Add("prezzo_uni_scontato", "Pos. net.");
                        findStrField["eng"].Add("prezzo_uni_scontato", "Pos. net.");
                        break;

                    case "Ordine":

                        findStrField["ita"].Add("numero", "ordineno./");
                        findStrField["eng"].Add("numero", "number/");

                        findStrField["ita"].Add("data", "ordineno./data");
                        findStrField["eng"].Add("data", "number/date");

                        findStrField["ita"].Add("numeroOff", "offertano./");
                        findStrField["eng"].Add("numeroOff", "quotationno./");

                        findStrField["ita"].Add("cliente", "no.cliente");
                        findStrField["eng"].Add("cliente", "cust.no.");

                        findStrField["ita"].Add("ETA", "terminedat.");
                        findStrField["eng"].Add("ETA", "shipmentdate");
                        break;
                    case "OrdineItem":

                        findStrField["ita"].Add("prezzo_uni_scontato", "Pos. net.");
                        findStrField["eng"].Add("prezzo_uni_scontato", "Pos. net.");
                        break;
                    default:
                        OnTopMessage.Error("Errore selezione dizionario");
                        break;

                }
                return findStrField;
            }

            internal static string ExtractBodyPDF(string filePath)
            {
                string text = "";
                using (PdfDocument pdfDoc = new(new PdfReader(filePath)))
                {
                    int c = pdfDoc.GetNumberOfPages();

                    for (int i = 1; i < c; i++)
                    {
                        LocationTextExtractionStrategy strategy = new();

                        PdfCanvasProcessor parser = new(strategy);
                        parser.ProcessPageContent(pdfDoc.GetPage(i));

                        text += strategy.GetResultantText() + Environment.NewLine;

                        parser.Reset();
                    }
                    pdfDoc.Close();
                }

                return text;
            }

            internal static string GetImageLanguage()
            {
                string lang = "";
                while (lang != "1" && lang != "2")
                {
                    lang = OnTopMessage.InputBox("Inserire il numero corrispondente alla lingua del documento:" + Environment.NewLine + "1 - English" + Environment.NewLine + "2 - Italiano").Trim();
                }

                switch (lang)
                {
                    case "1":
                        lang = "eng";
                        break;
                    case "2":
                        lang = "ita";
                        break;
                }

                return lang;
            }
        }
    }
}
