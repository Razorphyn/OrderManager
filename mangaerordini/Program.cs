//using log4net;
using AutoUpdaterDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        static readonly string nameTempDbRetsore = "temp_updateDB_then_delete_do_not_use_this_name_pls";

        [STAThread]
        private static void Main()
        {

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


            var currentDirectory = new DirectoryInfo(Application.StartupPath);
            AutoUpdater.InstalledVersion = new Version(Application.ProductVersion);
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.DownloadPath = Application.StartupPath;
            AutoUpdater.InstallationPath = currentDirectory.Parent.FullName;
            AutoUpdater.Start("https://github.com/Razorphyn/OrderManager/blob/main/mangaerordini/AutoUpdater.xml?raw=true");

            decimal version = 0;

            string connectionString = @"Data Source = " + exeFolderPath + db_path + db_name + @";cache=shared; synchronous  = NORMAL ;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ";
            string schemadb = "";

            SQLiteConnection connection = new SQLiteConnection(connectionString);

            if (File.Exists(db_check_file) == false)
            {
                DialogResult dialogResult = MessageBox.Show("Il file del database non è stato trovato. Generare un nuovo file?" + Environment.NewLine + "Premere No per altre opzioni." + Environment.NewLine + Environment.NewLine + "Altriemnti chiudere il programma e copiare e incollare il file '" + db_name + "'  dalla cartella precedente nella cartella 'db' che si trova nel percorso corrente dell'eseguibile e riavviare il software.", "Errore - File Databse non trovato", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    RunSqlScriptFile(exeFolderPath + @"\db\tables\tables.sql", connectionString);
                }
                else if (dialogResult == DialogResult.No)
                {
                    dialogResult = MessageBox.Show("Vuoi selezionare un file da copiare e incollare nella destinazione? Altriemnti premere No ed uscire dal programma", "Errore - File Databse non trovato", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        var fileContent = string.Empty;
                        var filePath = string.Empty;

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
                    if (System.Windows.Forms.Application.MessageLoop)
                    {
                        // WinForms app
                        System.Windows.Forms.Application.Exit();
                    }
                    else
                    {
                        // Console app
                        System.Environment.Exit(1);
                    }
                }
            }

            string commandText = "SELECT versione FROM " + schemadb + @"[informazioni] WHERE Id=1 LIMIT 1;";
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
            {
                try
                {
                    cmd.CommandText = commandText;

                    conn.Open();
                    version = Convert.ToDecimal(cmd.ExecuteScalar());
                    if (version == 0)
                    {
                        commandText = "INSERT INTO " + schemadb + @"[informazioni](Id,versione) VALUES (1,1);";
                        using (SQLiteCommand cmd2 = new SQLiteCommand(commandText, conn))
                        {
                            try
                            {
                                cmd.CommandText = commandText;
                                cmd.ExecuteNonQuery();
                                version = 1;
                            }
                            catch (SQLiteException ex)
                            {
                                MessageBox.Show("Errore durante aggiunta versione al database informazioni al database. Codice: " + ex.Message);
                            }
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante selezione versione database al database. Codice: " + ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }

            //DB UPDATE
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
                        if (version < Convert.ToDecimal(fnames_ver[index_str]))
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

                            BkBackup(true);

                            bool success = RunSqlScriptFile(exeFolderPath + db__query_folder + @"\" + file.Name, connectionString);

                            if (success)
                            {
                                /*commandText = "UPDATE " + schemadb + @"[informazioni] SET versione=@ver WHERE id=1;";
                                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                                using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn))
                                {
                                    try
                                    {
                                        cmd.CommandText = commandText;
                                        cmd.Parameters.AddWithValue("@ver", dec);
                                        conn.Open();
                                        cmd.ExecuteNonQuery();
                                        MessageBox.Show("Database aggiornato alla versione: " + fnames_ver[index_str]);
                                        version = Convert.ToDecimal(fnames_ver[index_str]);
                                    }
                                    catch (SQLiteException ex)
                                    {
                                        MessageBox.Show("Errore durante aggiornamento tabella informazioni alla versione " + file.Name + " . Codice: " + ex.Message);
                                    }
                                    finally
                                    {
                                        conn.Close();
                                    }
                                }*/
                                DelTempFileBkDb();
                            }
                            else
                            {
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

            if (!File.Exists(settingFile))
            {
                string input = null;

                while (input == null)
                    input = Interaction.InputBox("Impostare un nome per il calendario in cui verranno aggiunti i rememnder per gli ordini." + Environment.NewLine + Environment.NewLine + "Se lasciato vuoto, verrà usato il calendario di default di Outlook", "Nome Calendario Eventi", "ManagerOrdini");

                if (String.IsNullOrEmpty(input))
                {
                    Microsoft.Office.Interop.Outlook.Application OlApp = new Microsoft.Office.Interop.Outlook.Application();
                    Outlook.Folder primaryCalendar = OlApp.Session.GetDefaultFolder(
                        Outlook.OlDefaultFolders.olFolderCalendar)
                        as Outlook.Folder;

                    //input = primaryCalendar.Name;
                }

                Dictionary<string, Dictionary<string, string>> settings = new Dictionary<string, Dictionary<string, string>>();
                settings["calendario"] = new Dictionary<string, string>();

                settings["calendario"].Add("nomeCalendario", input);
                settings["calendario"].Add("destinatari", "");

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

            Application.Run(new Form1());
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

        static void ExitProgram()
        {
            if (System.Windows.Forms.Application.MessageLoop)
            {
                // WinForms app
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                // Console app
                System.Environment.Exit(1);
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string createText = e.Exception.Message + Environment.NewLine;
            string path = Path.GetDirectoryName(Application.ExecutablePath) + @"\error.log";
            File.WriteAllText(path, createText);

            // Log the exception, display it, etc
            //Debug.WriteLine(e.Exception.Message);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string createText = (e.ExceptionObject as Exception).Message + Environment.NewLine;
            string path = Path.GetDirectoryName(Application.ExecutablePath) + @"\error.log";

            File.AppendAllText(path, createText);
            // Log the exception, display it, etc
            //Debug.WriteLine((e.ExceptionObject as Exception).Message);
        }
        static void FirstChanceHandler(object source, FirstChanceExceptionEventArgs e)
        {
            string createText = String.Format("FirstChanceException event raised in {0}: {1}", AppDomain.CurrentDomain.FriendlyName, e.Exception.Message) + Environment.NewLine;
            string path = Path.GetDirectoryName(Application.ExecutablePath) + @"\error.log";

            File.AppendAllText(path, createText);

        }

        public static IEnumerable<string> CustomSort(this IEnumerable<string> list)
        {
            int maxLen = list.Select(s => s.Length).Max();

            return list.Select(s => new
            {
                OrgStr = s,
                SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
            })
            .OrderBy(x => x.SortStr)
            .Select(x => x.OrgStr);
        }

    }
}
