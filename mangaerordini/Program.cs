//using log4net;
using AutoUpdaterDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Windows.Management;
using static mangaerordini.Form1;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace mangaerordini
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        /// 

        static readonly string exeFolderPath = Path.GetDirectoryName(Application.ExecutablePath);
        static readonly string db_path = @"\db\";
        static readonly string db_name = @"ManagerOrdini.db";
        static readonly string db__query_folder = @"\db\updates\";
        static readonly string db_check_file = exeFolderPath + db_path + db_name;
        static readonly string settingFile = exeFolderPath + @"\" + "ManagerOrdiniSettings.txt";

        static readonly Dictionary<string, Dictionary<string, string>> settings = new Dictionary<string, Dictionary<string, string>>();

        static readonly string nameTempDbRetsore = "temp_updateDB_then_delete_do_not_use_this_name_pls";
        static readonly string connectionString = @"Data Source = " + exeFolderPath + db_path + db_name + @";cache=shared; synchronous  = NORMAL ;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ";
        static readonly string schemadb = "";
        static readonly SQLiteConnection connection = new SQLiteConnection(connectionString);

        [STAThread]
        private static void Main()
        {
            //Mutex based on GuidAttribute to prevent multiple program execution. Avoid accessing to DB on multiple instances.
            //Not all information are collected everytime from DB

            string appGuid =
                           ((GuidAttribute)Assembly.GetExecutingAssembly().
                               GetCustomAttributes(typeof(GuidAttribute), false).
                                   GetValue(0)).Value.ToString();

            string mutexId = string.Format("Global\\{{{0}}}", appGuid);

            Mutex mutex = new System.Threading.Mutex(false, mutexId, out bool created);
            mutex.WaitOne(TimeSpan.Zero, true);
            try
            {
                if (!created)
                {
                    MessageBox.Show("L'applicazione è già in esecuzione.");
                    ExitProgram();
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CheckUpdates();

            //Check if DB file exists otherwise create/copy one
            ValidateDB();

            DbCallResult versione = ReturnVersione();
            if (versione.Success != true) return;

            CheckPendingdataUpdate();

            CheckDbUpdate(versione);

            CheckSetting();

            Application.Run(new Form1());
        }

        public class DbCallResult
        {
            public bool Success { get; set; } = false;
            public int? IntValue { get; set; } = 0;
            public decimal? DecimalValue { get; set; } = 0;
        }

        private static void CheckUpdates()
        {
            //Check for updates
            AutoUpdater.InstalledVersion = new Version(Application.ProductVersion);
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.DownloadPath = Application.StartupPath;
            AutoUpdater.Start("https://github.com/Razorphyn/OrderManager/blob/main/mangaerordini/AutoUpdater.xml?raw=true");

            //Remove exec and log use in AutoUpdater
            string zipextractorfile = exeFolderPath + "\\ZipExtractor.exe";
            string zipextractorlog = exeFolderPath + "\\ZipExtractor.log";

            if (File.Exists(zipextractorfile))
            {
                File.Delete(zipextractorfile);
            }
            if (File.Exists(zipextractorlog))
            {
                File.Delete(zipextractorlog);
            }
        }

        private static void ValidateDB()
        {
            if (File.Exists(db_check_file) == false)
            {
                DialogResult dialogResult = MessageBox.Show("Il file del database non è stato trovato. Generare un nuovo file?" + Environment.NewLine + "Premere No per altre opzioni." + Environment.NewLine + Environment.NewLine + "Altriemnti chiudere il programma e copiare e incollare il file '" + db_name + "'  dalla cartella precedente nella cartella 'db' che si trova nel percorso corrente dell'eseguibile e riavviare il software.", "Errore - File Databse non trovato", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    RunSqlScriptFile(exeFolderPath + @"\db\tables\tables.sql", connectionString);
                }
                else if (dialogResult == DialogResult.No)
                {
                    dialogResult = MessageBox.Show("Vuoi selezionare un file da copiare nella destinazione? Altriemnti premere No per uscire dal programma", "Errore - File Databse non trovato", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        using (OpenFileDialog openFileDialog = new OpenFileDialog())
                        {
                            openFileDialog.InitialDirectory = exeFolderPath;
                            openFileDialog.Filter = "SQLite Database (*.db)|*.db";
                            openFileDialog.FilterIndex = 2;
                            openFileDialog.RestoreDirectory = true;

                            if (openFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                File.Copy(openFileDialog.FileName, exeFolderPath + db_path + db_name);
                                if (File.Exists(db_check_file) == true)
                                {
                                    dialogResult = MessageBox.Show("File copiato, vuoi eliminare l'originale?", "Errore - File Databse non trovato", MessageBoxButtons.YesNo);
                                    if (dialogResult == DialogResult.Yes)
                                    {
                                        File.Delete(openFileDialog.FileName);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("Il Programma verrà chiuso");
                                ExitProgram();
                            }
                        }
                    }
                    else
                    {
                        ExitProgram();
                    }
                }
                else
                {
                    ExitProgram();
                }
            }
        }

        private static DbCallResult ReturnVersione()
        {
            DbCallResult answer = new DbCallResult();

            //Retrieve database version, if not exist add default
            string commandText = "SELECT versione FROM " + schemadb + @"[informazioni] WHERE Id=1 LIMIT 1;";
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;

                    connection.Open();
                    answer.Success = true;
                    answer.DecimalValue = Convert.ToDecimal(cmd.ExecuteScalar());
                    if (answer.DecimalValue == 0)
                    {
                        commandText = "INSERT INTO " + schemadb + @"[informazioni](Id,versione) VALUES (1,1);";
                        using (SQLiteCommand cmd2 = new SQLiteCommand(commandText, connection))
                        {
                            try
                            {
                                cmd.CommandText = commandText;
                                cmd.ExecuteNonQuery();
                                answer.DecimalValue = 1;
                            }
                            catch (SQLiteException ex)
                            {
                                answer.Success = false;
                                MessageBox.Show("Errore durante aggiunta versione al database. Codice: " + ex.Message);
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    MessageBox.Show("Errore durante selezione versione database. Codice: " + ex.Message);
                }

                return answer;
            }
        }

        private static void CheckSetting()
        {
            // check if setting file exists, otherwise create it
            if (!File.Exists(settingFile))
            {
                string calendarName = GetCalendarName();

                Dictionary<string, Dictionary<string, string>> settings = new Dictionary<string, Dictionary<string, string>>
                {
                    ["calendario"] = new Dictionary<string, string>
                                        {
                                            { "nomeCalendario", calendarName },
                                            { "destinatari", "" }
                                        }
                };

                DialogResult dialogResult = MessageBox.Show("Vuoi che il software identifichi se necessario e aggiornare un evento di calendario? Prima di procedere chiede conferma. " + Environment.NewLine + "Se disabilitato, il tutto dovrà essere fatto manualemnte", "Aggiornamento Automatico Eventi Calendario", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    settings["calendario"].Add("aggiornaCalendario", "true");
                }
                else
                {
                    settings["calendario"].Add("aggiornaCalendario", "false");
                }

                string json = JsonConvert.SerializeObject(settings);
                File.WriteAllText(settingFile, json);
            }
        }

        private static string GetCalendarName()
        {
            string input = null;

            while (input == null)
                input = Interaction.InputBox("Impostare un nome per il calendario in cui verranno aggiunti i rememnder per gli ordini." + Environment.NewLine + Environment.NewLine + "Se lasciato vuoto, verrà usato il calendario di default di Outlook", "Nome Calendario Eventi", "ManagerOrdini")
                    .Trim();

            /*if (String.IsNullOrEmpty(input))
            {
                Outlook.Application OlApp = new Outlook.Application();
                Outlook.Folder primaryCalendar = OlApp.Session.GetDefaultFolder(
                    Outlook.OlDefaultFolders.olFolderCalendar)
                    as Outlook.Folder;
                input = primaryCalendar.Name;
            }*/

            return input;
        }

        private static void CheckDbUpdate(DbCallResult versione)
        {
            //Search for files with version lower than retrieved database
            // Add hash check?
            if (Directory.Exists(exeFolderPath + db__query_folder))
            {
                DirectoryInfo d = new DirectoryInfo(exeFolderPath + db__query_folder);

                FileInfo[] Files = d.GetFiles("*.sql"); //Getting sql files
                string str = "";

                bool bkAsked = false;
                Array.Sort(Files, delegate (FileInfo x, FileInfo y) { return Decimal.Compare(Convert.ToDecimal(Path.GetFileNameWithoutExtension(x.Name)), Convert.ToDecimal(Path.GetFileNameWithoutExtension(y.Name))); });

                foreach (FileInfo file in Files)
                {
                    str = Path.GetFileNameWithoutExtension(file.Name);
                    string[] fnames_ver = str.Split('-');
                    int index_str = (fnames_ver.Length > 1) ? 1 : 0;

                    if (Decimal.TryParse(fnames_ver[index_str], out decimal dec))
                    {
                        if (versione.DecimalValue < dec)
                        {
                            if (bkAsked == false)
                            {
                                DialogResult dialogResult = MessageBox.Show("Aggiornamenti database trovati. Eseguire backup database prima di effettuare l'aggiornamento(consigliato)?", "Backup Database", MessageBoxButtons.YesNo);
                                if (dialogResult == DialogResult.Yes)
                                {
                                    BkBackup();
                                }
                                bkAsked = true;
                            }

                            //do backup anyway to rollback in case of errors
                            BkBackup(true);

                            bool success = RunSqlScriptFile(exeFolderPath + db__query_folder + @"\" + file.Name, connectionString);

                            if (success)
                            {
                                //delete automatic backup
                                DelTempFileBkDb();

                                //delete update file
                                //File.Delete(exeFolderPath + db__query_folder + @"\" + file.Name);

                                UpdateDataManipulation(Convert.ToDecimal(fnames_ver[index_str]));
                            }
                            else
                            {
                                //if error then restore db and delete temp backup
                                BtDbRestore();
                                DelTempFileBkDb();

                                MessageBox.Show("Errore durante aggiornamento database. Il programma non può esssere avviato." + Environment.NewLine + "Contatta uno sviluppatore competente");
                                ExitProgram();
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Errore nel parse dei file" + str + ", controllare i file nella cartella db/update e riavviare il programma");
                        ExitProgram();
                    }
                }
            }
        }

        private static void BkBackup(bool automata = false)
        {
            using (FolderBrowserDialog db_backup_path = new FolderBrowserDialog())
            {
                var dialogreturn = DialogResult.No;

                if (!automata)
                {
                    db_backup_path.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    db_backup_path.SelectedPath = exeFolderPath;
                    dialogreturn = db_backup_path.ShowDialog();
                }

                if (automata == true || dialogreturn == DialogResult.OK)
                {
                    string bk_fileName = "";
                    string folderPath = "";

                    if (!automata)
                    {
                        folderPath = db_backup_path.SelectedPath;
                        string iden = DateTime.Now.ToString().Replace(":", "").Replace(" ", "").Replace(@"/", "");

                        bk_fileName = folderPath + "/db_managerordini_" + iden + ".sqlitebak";
                    }
                    else
                    {
                        bk_fileName = exeFolderPath + db_path + nameTempDbRetsore;
                    }

                    using (var source = new SQLiteConnection("Data Source=" + exeFolderPath + db_path + db_name))
                    using (var destination = new SQLiteConnection("Data Source=" + bk_fileName))
                    {
                        try
                        {
                            source.Open();
                            destination.Open();
                            source.BackupDatabase(destination, "main", "main", -1, null, 0);
                            if (!automata)
                            {
                                MessageBox.Show("Backup eseguito");
                                Process.Start(folderPath);
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Errore durante backup");
                        }
                    }

                }
            }
            return;
        }

        private static void BtDbRestore()
        {
            string filePath = exeFolderPath + db_path + nameTempDbRetsore;

            if (!String.IsNullOrEmpty(filePath))
            {
                using (var source = new SQLiteConnection("Data Source=" + filePath))
                using (var destination = new SQLiteConnection("Data Source=" + exeFolderPath + db_path + db_name))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination, "main", "main", -1, null, 0);
                }
            }
            return;
        }

        private static void DelTempFileBkDb()
        {
            string filePath = exeFolderPath + db_path + nameTempDbRetsore;
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException copyError)
                {
                    MessageBox.Show(copyError.Message);
                }
            }
        }

        private static bool RunSqlScriptFile(string pathStoreProceduresFile, string connectionString)
        {
            try
            {
                string script = File.ReadAllText(pathStoreProceduresFile);

                // split script on GO command
                System.Collections.Generic.IEnumerable<string> commandStrings = Regex.Split(script, @"^\s*GO\s*$",
                                         RegexOptions.Multiline | RegexOptions.IgnoreCase);
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    foreach (string commandString in commandStrings)
                    {
                        if (commandString.Trim() != "")
                        {
                            using (var command = new SQLiteCommand(commandString, connection))
                            {
                                try
                                {
                                    SQLiteDataReader reader = command.ExecuteReader();
                                    string message = "";

                                    while (reader.Read())
                                    {
                                        //If message is needed--> return as "retentry" from query
                                        if (!string.IsNullOrEmpty(Convert.ToString(reader["retentry"])))
                                            message = message + Environment.NewLine + Convert.ToString(reader["retentry"]);
                                    }
                                    reader.Close();
                                    message = message.Trim();
                                    if (!string.IsNullOrEmpty(message))
                                        MessageBox.Show(message);
                                }
                                catch (SQLiteException ex)
                                {
                                    string spError = commandString.Length > 100 ? commandString.Substring(0, 100) + " ...\n..." : commandString;
                                    MessageBox.Show(string.Format("Please check the SqlServer script.\nFile: {0} \nLine: {1} \nError: {2} \nSQL Command: \n{3}", pathStoreProceduresFile, "", ex.Message, spError), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return false;
                                }
                            }
                        }
                    }
                    connection.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        private static void CheckPendingdataUpdate()
        {
            DirectoryInfo d = new DirectoryInfo(exeFolderPath + db_path);

            FileInfo[] Files = d.GetFiles("*.pending"); //Getting sql files

            Array.Sort(Files, delegate (FileInfo x, FileInfo y) { return Decimal.Compare(Convert.ToDecimal(Path.GetFileNameWithoutExtension(x.Name)), Convert.ToDecimal(Path.GetFileNameWithoutExtension(y.Name))); });

            foreach (FileInfo file in Files)
            {
                if (Decimal.TryParse(Path.GetFileNameWithoutExtension(file.Name), out decimal dec))
                {
                    UpdateDataManipulation(dec);
                }
            }
        }

        private static void UpdateDataManipulation(decimal version)
        {
            if (version == 5)
            {
                string tempfile = exeFolderPath + db_path + version + ".pending";
                if (!File.Exists(tempfile)) File.Create(tempfile);

                ReadSettingApp();
                Outlook.Folder cal = FindCalendar(settings["calendario"]["nomeCalendario"]);

                string commandText = @"SELECT  data_ETA FROM " + schemadb + @"[ordini_elenco] ORDER BY data_ETA ASC LIMIT 1;";
                string parseRes = null;

                using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                {
                    try
                    {
                        parseRes = Convert.ToString(cmd.ExecuteScalar());
                    }
                    catch (SQLiteException ex)
                    {
                        MessageBox.Show("Errore durante selezione info dal database. Codice: " + ex.Message);
                    }
                }


                if (!String.IsNullOrEmpty(parseRes))
                {
                    Outlook.Items restrictedItems = CalendarGetItems(cal, Convert.ToDateTime(parseRes));

                    Dictionary<int, DateTime> ordNum = new Dictionary<int, DateTime>();

                    string pattern = @"^.+##ManaOrdini([0-9]+)##$";
                    string query = "";
                    int i = 0;

                    foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                    {
                        foreach (Match match in Regex.Matches(apptItem.Subject, pattern, RegexOptions.IgnoreCase))
                        {
                            query += @"UPDATE OR IGNORE " + schemadb + @"[ordini_elenco]  SET data_calendar_event = @dataVal" + i + " WHERE codice_ordine = @codord" + i + " LIMIT 1;";
                            ordNum.Add(Convert.ToInt32(match.Groups[1].Value), new DateTime(apptItem.Start.Year, apptItem.Start.Month, apptItem.Start.Day, 0, 0, 0));
                            i++;
                        }
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
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
                            MessageBox.Show("Errore durante aggiornamento date calendario al database. Codice: " + ex.Message);
                        }
                    }

                }
                File.Delete(tempfile);
            }
        }

        private static Outlook.Items CalendarGetItems(Outlook.Folder personalCalendar, DateTime startDate)
        {
            string AppCode = "##ManaOrdini";
            string filterDate = "[Start] >= '" + startDate.ToString("g") + "' AND [End] <= '" + DateTime.MaxValue.ToString("g") + "'";
            string filterSubject = "@SQL=" + "\"" + "urn:schemas:httpmail:subject" + "\"" + " LIKE '%" + AppCode + "%'";

            Outlook.Items calendarItems = personalCalendar.Items.Restrict(filterDate);
            calendarItems.IncludeRecurrences = true;
            calendarItems.Sort("[Start]", Type.Missing);

            Outlook.Items restrictedItems = calendarItems.Restrict(filterSubject);

            return restrictedItems;
        }

        private static Outlook.Folder FindCalendar(string calendarName)
        {
            Outlook.Application OlApp = new Outlook.Application();

            Outlook.Folder AppointmentFolder =
                OlApp.Session.GetDefaultFolder(
                Outlook.OlDefaultFolders.olFolderCalendar)
                as Outlook.Folder;

            Outlook.Folder personalCalendar = AppointmentFolder;

            if (!String.IsNullOrEmpty(calendarName) && AppointmentFolder.Name != calendarName)
            {
                foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                {
                    if (personalCalendarLoop.Name == calendarName)
                    {
                        return personalCalendarLoop;
                    }
                }

                CalendarResult re = CreateCustomCalendar(calendarName);

                if (re.Success && !re.Found)
                    personalCalendar = re.CalendarFolder;
                else if (!re.Success)
                    return null;
            }

            return personalCalendar;
        }

        private static CalendarResult CreateCustomCalendar(string calName)
        {
            CalendarResult answer = new CalendarResult
            {
                Success = true
            };

            if (String.IsNullOrEmpty(calName))
            {
                answer.Found = true;
            }
            else
            {
                try
                {
                    Outlook.Application OlApp = new Outlook.Application();
                    Outlook.Folder primaryCalendar = OlApp.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;

                    foreach (Outlook.Folder Calendar in primaryCalendar.Folders)
                    {
                        if (Calendar.Name == calName)
                        {
                            answer.Found = true;
                            break;
                        }
                    }

                    if (!answer.Found)
                    {
                        answer.CalendarFolder = primaryCalendar.Folders.Add(calName, Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;
                    }
                }
                catch
                {
                    MessageBox.Show("Errore durante verifica necessità cartella OutLook. Impossibile aggiornare informazioni." + Environment.NewLine + "Incrociare dia per evitare danni ai dati");
                    answer.Success = false;
                }
            }

            return answer;
        }

        private static void ReadSettingApp()
        {
            settings.Add("calendario", new Dictionary<string, string>());
            settings["calendario"].Add("aggiornaCalendario", "true");
            settings["calendario"].Add("destinatari", "");
            settings["calendario"].Add("nomeCalendario", "");

            string json = File.ReadAllText(settingFile);
            Dictionary<string, Dictionary<string, string>> read_settings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

            CopyDict(read_settings);
        }

        public static void CopyDict(Dictionary<string, Dictionary<string, string>> dict)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> rootKv in dict)
            {
                foreach (KeyValuePair<string, string> childKv in rootKv.Value)
                {
                    settings[rootKv.Key][childKv.Key] = dict[rootKv.Key][childKv.Key];
                }
            }
        }

        static void ExitProgram()
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                System.Environment.Exit(1);
            }
        }
    }

}
