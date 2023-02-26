using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace Razorphyn
{
    public class ProgramUpdateFunctions
    {

        public void UpdateDataManipulation(decimal version)
        {
            if (version == 5)
            {
                string tempfile = ProgramParameters.exeFolderPath + ProgramParameters.db_file_path + version + ".pending";

                UserSettings UserSettings = new UserSettings();
                CalendarManager CalendarManager = new CalendarManager();

                if (!File.Exists(tempfile)) File.Create(tempfile).Close();

                Outlook.Application OlApp = new Outlook.Application();
                Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

                string commandText = @"SELECT data_ETA FROM " + ProgramParameters.schemadb + @"[ordini_elenco] ORDER BY data_ETA ASC LIMIT 1;";
                string startDate = null;

                using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        startDate = Convert.ToString(cmd.ExecuteScalar());
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante selezione info dal database. Codice: " + ex.Message);
                    }
                }


                if (!String.IsNullOrEmpty(startDate))
                {
                    Outlook.Items restrictedItems = CalendarManager.CalendarGetItems(personalCalendar, Convert.ToDateTime(startDate).AddDays(-1), Convert.ToDateTime(startDate).AddDays(+1));

                    Dictionary<int, DateTime> ordNum = new Dictionary<int, DateTime>();

                    string pattern = @"^.+##ManaOrdini([0-9]+)##$";
                    string query = "";
                    int i = 0;

                    foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                    {
                        foreach (Match match in Regex.Matches(apptItem.Subject, pattern, RegexOptions.IgnoreCase))
                        {
                            query += @"UPDATE OR IGNORE " + ProgramParameters.schemadb + @"[ordini_elenco]  SET data_calendar_event = @dataVal" + i + " WHERE codice_ordine = @codord" + i + " LIMIT 1;";
                            ordNum.Add(Convert.ToInt32(match.Groups[1].Value), new DateTime(apptItem.Start.Year, apptItem.Start.Month, apptItem.Start.Day, 0, 0, 0));
                            i++;
                        }
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(query, ProgramParameters.connection))
                    {
                        try
                        {
                            i = 0;
                            foreach (KeyValuePair<int, DateTime> entry in ordNum)
                            {
                                cmd.Parameters.AddWithValue("@dataVal" + i, entry.Value);
                                cmd.Parameters.AddWithValue("@codord" + i, entry.Key);

                                i++;
                            }
                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante aggiornamento date calendario al database. Codice: " + ex.Message);
                        }
                    }

                }
                try
                {
                    File.Delete(tempfile);
                }
                catch
                {
                    OnTopMessage.Error("errore durante eliminazione file. Per favore eliminare manualemnte il file: " + tempfile);

                }
            }
        }
        
        public void CheckUpdates()
        {
            //Check for updates
            AutoUpdater.InstalledVersion = new Version(Application.ProductVersion);
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.DownloadPath = Application.StartupPath;
            AutoUpdater.Start("https://github.com/Razorphyn/OrderManager/blob/main/mangaerordini/AutoUpdater.xml?raw=true");

            //Remove exec and log use in AutoUpdater
            string zipextractorfile = ProgramParameters.exeFolderPath + "\\ZipExtractor.exe";
            string zipextractorlog = ProgramParameters.exeFolderPath + "\\ZipExtractor.log";

            if (File.Exists(zipextractorfile))
            {
                File.Delete(zipextractorfile);
            }
            if (File.Exists(zipextractorlog))
            {
                File.Delete(zipextractorlog);
            }
        }
    }


}