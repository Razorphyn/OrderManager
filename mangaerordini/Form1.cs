using AutoUpdaterDotNET;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace mangaerordini
{
    public partial class Form1 : Form
    {
        static readonly string exeFolderPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\";
        static readonly string db_file_path = @"db\";
        static readonly string db_file_name = @"ManagerOrdini.db";
        static readonly string settingFile = exeFolderPath + @"\" + "ManagerOrdiniSettings.txt";
        static readonly int recordsPerPage = 8;
        static readonly string schemadb = "";
        //readonly string connectionString = @"Data Source = " + exeFolderPath + db_file_path + db_file_name + @";cache=shared; synchronous  = NORMAL ;  foreign_keys  = 1;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ";

        Dictionary<string, Dictionary<string, string>> settings;

        int datiGridViewFornitoriCurPage = 1;
        int datiGridViewClientiCurPage = 1;
        int datiGridViewPrefCurPage = 1;
        int datiGridViewMacchineCurPage = 1;
        int datiGridViewRicambiCurPage = 1;

        int offerteCreaCurPage = 1;
        int OrdiniCurPage = 1;
        int OrdiniViewCurPage = 1;

        string AddOffCreaOggettoPezzoFiltro_Text = "";
        string FieldOrdOggPezzoFiltro_Text = "";

        readonly CultureInfo provider = CultureInfo.InvariantCulture;
        readonly NumberStyles style = NumberStyles.AllowDecimalPoint;
        readonly CultureInfo culture = CultureInfo.CreateSpecificCulture("it-IT");
        readonly NumberFormatInfo nfi = CultureInfo.GetCultureInfo("it-IT").NumberFormat;
        readonly string dateFormat = "dd/MM/yyyy";

        readonly SQLiteConnection connection = new SQLiteConnection(@"Data Source = " + exeFolderPath + db_file_path + db_file_name + @";cache=shared; synchronous  = NORMAL ;  foreign_keys  = 1;  journal_mode=WAL; temp_store = memory;  mmap_size = 30000000000; ");

        public Form1()
        {
            InitializeComponent();

            Timer_RunSQLiteOptimize.Interval = 60 * 1000;

            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);

            connection.Open();
            connection.SetExtendedResultCodes(true);

            RunSQLiteOptimize(500);

            Timer_RunSQLiteOptimize.Interval = 180 * 1000;
            Timer_RunSQLiteOptimize.Enabled = true;
            Timer_RunSQLiteOptimize.Start();

            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };

            this.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint,
              true);
            this.UpdateStyles();

            var TabPagelist = this.Controls.OfType<TabPage>();

            foreach (TabPage ele in TabPagelist)
            {
                typeof(TabPage).InvokeMember(
                   "DoubleBuffered",
                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                   null,
                   ele,
                   new object[] { true }
                );
            }

            var GridViewlist = this.Controls.OfType<DataGridView>();

            foreach (DataGridView ele in GridViewlist)
            {
                typeof(DataGridView).InvokeMember(
                   "DoubleBuffered",
                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                   null,
                   ele,
                   new object[] { true }
                );

                ele.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.EnableResizing;
            }

            var comboBoxes = this.Controls.OfType<ComboBox>();

            foreach (ComboBox ctrl in comboBoxes)
            {
                ctrl.BindingContext = new BindingContext();
                ctrl.DisplayMember = "Name";
                ctrl.ValueMember = "Value";
            }

            ReadSettingApp();

            Populate_combobox_dummy(ComboBoxOrdCliente);
            Populate_combobox_dummy(ComboBoxOrdOfferta);
            Populate_combobox_dummy(ComboSelOrd);
            Populate_combobox_dummy(FieldOrdStato);

            Populate_combobox_dummy(FieldOrdOggMach);
            Populate_combobox_dummy(FieldOrdOggPezzo);

            Populate_combobox_FieldOrdSpedGestione(FieldOrdSpedGestione);
            Populate_combobox_FieldOrdSpedGestione(AddOffCreaSpedizioneGest);
            Populate_combobox_statoOrdini(new ComboBox[] { FieldOrdStato });

            UpdateFixedComboValue();
            UpdateOrdiniStato();
            UpdateSetting();
            UpdateCountryList();
            UpdateClienti();
            UpdateFornitori();
            UpdateMacchine();
            UpdatePRef();
            UpdateRicambi();
            UpdateOfferteCrea();

            UpdateOrdini();

            UpdateFields("C", "E", false);
            UpdateFields("F", "E", false);
            UpdateFields("P", "E", false);
            UpdateFields("M", "E", false);
            UpdateFields("R", "E", false);
            UpdateFields("OAO", "A", false);
            UpdateFields("OC", "E", false);
            UpdateFields("OC", "A", true);

            UpdateFields("OCR", "A", false);
            UpdateFields("OCR", "E", false);

            UpdateFields("OCR", "E2", false);
            BtCreaOrdineOgg.Enabled = false;
            UpdateFields("OCR", "FE", false);

            UpdateFields("VS", "CA", false);
            UpdateFields("OCR", "E", false);

            DateTime today = DateTime.Today;
            AddOffCreaData.Text = today.ToString("dd/MM/yyyy");

            AddOffCreaOggettoPezzoFiltro.PlaceholderText = "Filtra per Id,Nome o Codice";
            FieldOrdOggPezzoFiltro.PlaceholderText = "Filtra per Id,Nome o Codice";

            buildVersionValue.Text = Convert.ToString(Application.ProductVersion);
        }

        //ALTRO
        private void BtDbBackup_Click(object sender, EventArgs e)
        {
            UpdateFields("DB", "E", false);

            string db_name = (exeFolderPath + db_file_path + db_file_name).ToUpper().ToString();

            using (FolderBrowserDialog db_backup_path = new FolderBrowserDialog())
            {
                db_backup_path.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                db_backup_path.SelectedPath = exeFolderPath;
                string bk_fileName;

                if (db_backup_path.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = db_backup_path.SelectedPath;

                    string iden = DateTime.Now.ToString();
                    iden = iden.Replace(":", "").Replace(" ", "").Replace(@"/", "");

                    bk_fileName = folderPath + "/db_managerordini_" + iden + ".sqlitebak";

                    using (var source = new SQLiteConnection("Data Source=" + exeFolderPath + db_file_path + db_file_name))
                    using (var destination = new SQLiteConnection("Data Source=" + bk_fileName))
                    {
                        try
                        {
                            source.Open();
                            destination.Open();
                            source.BackupDatabase(destination, "main", "main", -1, null, 0);
                            MessageBox.Show("Backup eseguito");
                            Process.Start(folderPath);
                        }
                        catch
                        {
                            MessageBox.Show("Errore");
                        }
                    }

                }
            }
            UpdateFields("DB", "E", true);
            return;
        }

        private void BtDbRestore_Click(object sender, EventArgs e)
        {
            UpdateFields("DB", "E", false);


            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string filePath = null;

                openFileDialog.InitialDirectory = exeFolderPath;
                openFileDialog.Filter = "Database (.sqlitebak)|*.sqlitebak";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }

                if (!String.IsNullOrEmpty(filePath))
                {

                    DialogResult dialogResult = MessageBox.Show("Procedere con il ripristino del database?", "Ripristino Database", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {

                        using (var source = new SQLiteConnection("Data Source=" + filePath))
                        using (var destination = new SQLiteConnection("Data Source=" + exeFolderPath + db_file_path + db_file_name))
                        {
                            source.Open();
                            destination.Open();
                            source.BackupDatabase(destination, "main", "main", -1, null, 0);
                            MessageBox.Show("L'applicazione verrà riavviata.");

                            Application.Restart();
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Il database non esiste.");
                    }
                }
            }
            UpdateFields("DB", "E", true);
            return;
        }

        private void RunSqlScriptFile(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = exeFolderPath;
                openFileDialog.Filter = "SQL (.sql)|*.sql";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string pathStoreProceduresFile = openFileDialog.FileName;
                        string script = File.ReadAllText(pathStoreProceduresFile);

                        // split script on GO command
                        System.Collections.Generic.IEnumerable<string> commandStrings = Regex.Split(script, @"^\s*GO\s*$",
                                                 RegexOptions.Multiline | RegexOptions.IgnoreCase);

                        {

                            foreach (string commandString in commandStrings)
                            {
                                if (commandString.Trim() != "")
                                {
                                    using (var command = new SQLiteCommand(commandString, connection))
                                    {
                                        try
                                        {
                                            command.ExecuteNonQuery();
                                        }
                                        catch (SQLiteException ex)
                                        {
                                            string spError = commandString.Length > 100 ? commandString.Substring(0, 100) + " ...\n..." : commandString;
                                            MessageBox.Show(string.Format("Please check the SqlServer script.\nFile: {0} \nLine: {1} \nError: {2} \nSQL Command: \n{3}", pathStoreProceduresFile, "", ex.Message, spError), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        }
                                    }
                                }
                            }


                        }
                        MessageBox.Show("Script Eseguito. L'applicazione verrà riavviata.");

                        Application.Restart();
                        Environment.Exit(0);

                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Warning");
                    }
                }
            }
            return;
        }

        private void SettingDbOptimize_Click(object sender, EventArgs e)
        {
            UpdateFields("DB", "E", false);
            var TabPagelist = this.Controls.OfType<TabPage>();
            foreach (TabPage c in TabPagelist)
            {
                c.Enabled = false;
            }

            string commandText = "PRAGMA vacuum;PRAGMA optimize;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;


                    cmd.ExecuteNonQuery();



                    MessageBox.Show("Ottimizzzazione Eseguita");

                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante Ottimizzzazione. Errore: " + ReturnErorrCode(ex));
                }
            }

            foreach (TabPage c in TabPagelist)
            {
                c.Enabled = true;
            }
            UpdateFields("DB", "E", true);
            return;
        }

        private void RunSQLiteOptimize(int entry = 150)
        {

            string commandText = "PRAGMA analysis_limit  = " + entry + ";  PRAGMA optimize;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {


                    cmd.CommandText = commandText;
                    cmd.ExecuteNonQuery();

                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante Ottimizzzazione. Errore: " + ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void ExportToCSV_Click(object sender, EventArgs e)
        {
            bool exportOfferte = false;
            bool exportOrdini = false;

            string start = ExportToCSVInfoStart.Value.ToString("yyyy-MM-dd");
            string end = ExportToCSVInfoEnd.Value.ToString("yyyy-MM-dd");

            if (ExportToCSVInfo.CheckedItems.Count != 0)
            {
                for (int x = 0; x < ExportToCSVInfo.CheckedItems.Count; x++)
                {
                    string temp = ExportToCSVInfo.CheckedItems[x].ToString();
                    if (temp == "Offerte")
                        exportOfferte = true;
                    else if (temp == "Ordini")
                        exportOrdini = true;
                }
            }
            else
            {
                MessageBox.Show("Selezione almeno una informazione da esportare");
                return;
            }

            using (FolderBrowserDialog csv_path = new FolderBrowserDialog())
            {
                csv_path.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                csv_path.SelectedPath = exeFolderPath;


                if (csv_path.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = csv_path.SelectedPath;
                    string iden = DateTime.Now.ToString("yyMMddHHmmss");
                    iden = iden.Replace(":", "").Replace(" ", "").Replace(@"/", "");
                    string commandText = "";

                    if (exportOfferte)
                    {
                        commandText = @"SELECT  
                                    OE.codice_offerta AS NumOfferta,
									CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')' AS Cliente,
									strftime('%d/%m/%Y',OE.data_offerta) AS DataOfferta,
                                    REPLACE( printf('%.2f',OE.tot_offerta ),'.',',') AS TotOfferta,
                                    CASE OE.stato WHEN 0 THEN 'APERTA'  WHEN 1 THEN 'ORDINATA' WHEN 2 THEN 'ANNULLATA' END AS StatoOfferta,
                                    CASE OE.trasformato_ordine WHEN 0 THEN 'No'  WHEN 1 THEN 'Sì' END AS ConvOfferta,
                                    PR.nome as PezzoOfferta,
                                    PR.codice AS CodicePezzo,
                                    CM.modello  || ' (' ||  CM.codice || ' - ' || CM.seriale || ')' AS MacchinaOfferta,
                                    OP.pezzi AS QtaOfferta,
                                    REPLACE( printf('%.2f',OP.prezzo_unitario_originale),'.',',')  AS PrezOrOfferta,
                                    REPLACE( printf('%.2f',OP.prezzo_unitario_sconto),'.',',')  AS PrezzoOfferta,
                                    CASE OP.aggiunto WHEN 0 THEN 'No'  WHEN 1 THEN 'Sì' END AS PzzAggOfferta

								   FROM " + schemadb + @"[offerte_elenco] AS OE
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OE.ID_cliente 
								   LEFT JOIN " + schemadb + @"[offerte_pezzi] AS OP
										ON OP.ID_offerta = OE.ID
                                    LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id = OP.ID_ricambio
                                    LEFT JOIN " + schemadb + @"[clienti_macchine] AS CM
										ON CM.Id = PR.ID_macchina

                                   WHERE OE.data_offerta BETWEEN @startdate AND @enddate
								   ORDER BY OE.data_offerta ASC";


                        using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
                        {
                            try
                            {
                                DataTable ds = new DataTable();
                                cmd.SelectCommand.Parameters.AddWithValue("@startdate", start);
                                cmd.SelectCommand.Parameters.AddWithValue("@enddate", end);


                                cmd.Fill(ds);


                                using (var writer = new StreamWriter(folderPath + @"\" + "OFFERTE_" + iden + ".csv", true, Encoding.UTF8))
                                using (var csv = new CsvWriter(writer, provider))
                                {
                                    csv.WriteHeader<Offerte>();
                                    csv.NextRecord();

                                    foreach (DataRow row in ds.Rows)
                                    {
                                        foreach (DataColumn column in ds.Columns)
                                        {
                                            csv.WriteField(row[column]);
                                        }
                                        csv.NextRecord();
                                    }

                                    MessageBox.Show("Offerte Esportate");
                                }
                            }
                            catch (SQLiteException ex)
                            {
                                MessageBox.Show("Errore durante lettura dati Offerte esportazione in csv. Codice: " + ReturnErorrCode(ex));


                                return;
                            }
                        }


                    }

                    if (exportOrdini)
                    {
                        commandText = @"SELECT  
									OE.codice_ordine AS codOrd,
									OFE.codice_offerta AS IDoff,
									CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')' AS Cliente,
									strftime('%d/%m/%Y',OE.data_ordine) AS datOr,
									strftime('%d/%m/%Y',OE.data_ETA) AS datEta,
									REPLACE((printf('%.2f',OE.totale_ordine)),'.',',')  AS totord,
                                    REPLACE((printf('%.2f',OE.prezzo_finale )),'.',',')  AS prezfinale,
                                    REPLACE((printf('%.2f',OE.sconto ) || '%'),'.',',')  AS Sconto,
									CASE OE.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END AS Stato,
                                    PR.nome AS nome,
									PR.codice AS code,
									REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS por,
									REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pos,
									OP.pezzi AS qta,
									strftime('%d/%m/%Y', OP.ETA) AS ETA								   

								   FROM " + schemadb + @"[ordini_elenco] AS OE 
								   LEFT JOIN " + schemadb + @"[offerte_elenco] OFE 
										ON OFE.Id = OE.ID_offerta 
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = OFE.ID_cliente 
								   LEFT JOIN " + schemadb + @"[ordine_pezzi] AS OP
									    ON OP.ID_ordine = OE.Id
                                   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
									    ON PR.Id = OP.ID_ricambio
                                    WHERE OE.ID_offerta IS NOT NULL AND OE.data_ordine BETWEEN @startdate AND @enddate 

                                    UNION ALL
                                    
                                    SELECT OE.codice_ordine AS codOrd, 
									'' AS IDoff,
									CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')' AS Cliente,
									strftime('%d/%m/%Y',OE.data_ordine) AS datOr,
									strftime('%d/%m/%Y',OE.data_ETA) AS datEta,
									REPLACE((printf('%.2f',OE.totale_ordine)),'.',',')  AS totord,
                                    REPLACE((printf('%.2f',OE.prezzo_finale )),'.',',')  AS prezfinale,
                                    REPLACE((printf('%.2f',OE.sconto ) || '%'),'.',',')  AS Sconto,
									CASE OE.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END AS Stato,
                                    PR.nome AS nome,
									PR.codice AS code,
									REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS por,
									REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pos,
									OP.pezzi AS qta,
									strftime('%d/%m/%Y', OP.ETA) AS ETA								   

								   FROM " + schemadb + @"[ordini_elenco] AS OE 
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = OE.ID_cliente 
								   LEFT JOIN " + schemadb + @"[ordine_pezzi] AS OP
									    ON OP.ID_ordine = OE.Id
                                   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
									    ON PR.Id = OP.ID_ricambio
                                    WHERE OE.ID_offerta IS NULL AND OE.data_ordine BETWEEN @startdate AND @enddate

								   ORDER BY datOr ASC";


                        using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
                        {
                            try
                            {
                                DataTable ds = new DataTable();
                                cmd.SelectCommand.Parameters.AddWithValue("@startdate", start);
                                cmd.SelectCommand.Parameters.AddWithValue("@enddate", end);


                                cmd.Fill(ds);


                                using (var writer = new StreamWriter(folderPath + @"\" + "ORDINI_" + iden + ".csv", true, Encoding.UTF8))
                                using (var csv = new CsvWriter(writer, provider))
                                {
                                    csv.WriteHeader<Ordini>();
                                    csv.NextRecord();

                                    foreach (DataRow row in ds.Rows)
                                    {
                                        foreach (DataColumn column in ds.Columns)
                                        {
                                            csv.WriteField(row[column]);
                                        }
                                        csv.NextRecord();
                                    }

                                    MessageBox.Show("Ordini Esportati");
                                }
                            }
                            catch (SQLiteException ex)
                            {
                                MessageBox.Show("Errore durante lettura dati Ordini esportazione in csv. Codice: " + ReturnErorrCode(ex));


                                return;
                            }
                        }
                    }

                }
            }
        }

        private void ButtonCheckUpdate_Click(object sender, EventArgs e)
        {
            ButtonCheckUpdate.Enabled = false;

            var currentDirectory = new DirectoryInfo(Application.StartupPath);
            AutoUpdater.InstalledVersion = new Version(Application.ProductVersion);
            AutoUpdater.Synchronous = true;
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.DownloadPath = Application.StartupPath;
            AutoUpdater.Start("https://github.com/Razorphyn/OrderManager/blob/main/mangaerordini/AutoUpdater.xml?raw=true");

            ButtonCheckUpdate.Enabled = true;
            return;
        }

        //TAB RICAMBI
        private void BtAddComponent_Click(object sender, EventArgs e)
        {
            UpdateFields("R", "A", false);

            string nome = AddDatiCompNome.Text.Trim();
            string codice = AddDatiCompCode.Text.Trim();
            string descrizione = AddDatiCompDesc.Text.Trim();
            string prezzo = AddDatiCompPrice.Text.Trim();
            int fornitoreId = Convert.ToInt32(AddDatiCompSupplier.SelectedValue.GetHashCode());
            int macchinaId = Convert.ToInt32(AddDatiCompMachine.SelectedValue.GetHashCode());

            string er_list = "";

            er_list += ValidateComponenteNome(nome);

            er_list += ValidateCodiceRicambio(codice);

            ValidationResult answer = ValidatePrezzo(prezzo);
            er_list += answer.Error;

            if (fornitoreId < 1)
            {
                er_list += "ID Fornitore non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("R", "A", true);
                return;
            }

            string commandText = "INSERT INTO " + schemadb + @"[pezzi_ricambi](nome, codice, descrizione, prezzo,ID_fornitore,ID_macchina) VALUES (@nome,@codice,@desc,@prezzo,@idif,@idma);";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@codice", codice);
                    cmd.Parameters.AddWithValue("@desc", descrizione);
                    cmd.Parameters.AddWithValue("@prezzo", answer.DecimalValue);
                    cmd.Parameters.AddWithValue("@idif", fornitoreId);
                    if (macchinaId < 1)
                        cmd.Parameters.AddWithValue("@idma", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@idma", macchinaId);

                    cmd.ExecuteNonQuery();

                    UpdateFields("R", "CA", true);
                    UpdateFields("R", "A", true);
                    UpdateRicambi();

                    MessageBox.Show("Componente aggiunto al database");

                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Errore: " + ReturnErorrCode(ex));
                    UpdateFields("R", "A", true);
                }
            }
            return;
        }

        private void BtSaveChangesComp_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("R", "E", false);

            string nome = ChangeDatiCompNome.Text.Trim();
            string codice = ChangeDatiCompCode.Text.Trim();
            string descrizione = ChangeDatiCompDesc.Text.Trim();
            string prezzo = ChangeDatiCompPrice.Text.Trim();
            int fornitoreId = Convert.ToInt32(ChangeDatiCompSupplier.SelectedItem.GetHashCode());
            int macchinaId = Convert.ToInt32(ChangeDatiCompMachine.SelectedItem.GetHashCode());
            string idF = ChangeDatiCompID.Text;

            ValidationResult answer = new ValidationResult();
            string er_list = "";

            string commandText;

            er_list += ValidateComponenteNome(nome);

            answer = ValidateMacchina(macchinaId);

            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            answer = ValidateFornitore(fornitoreId);
            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            er_list += ValidateCodiceRicambio(codice);

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            ValidationResult prezzod = ValidatePrezzo(prezzo);
            er_list += prezzod.Error;

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi salvare le modifiche?", "Salvare Cambiamenti Ricambio", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);
                return;
            }

            commandText = "UPDATE " + schemadb + @"[pezzi_ricambi] SET nome=@nome,codice=@codice,descrizione=@descrizione,prezzo=@prezzod,ID_fornitore=@idif,ID_macchina=@idma WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@codice", codice);
                    cmd.Parameters.AddWithValue("@descrizione", descrizione);
                    cmd.Parameters.AddWithValue("@prezzod", prezzod.DecimalValue);
                    cmd.Parameters.AddWithValue("@idif", fornitoreId);
                    cmd.Parameters.AddWithValue("@idq", idQ);
                    if (macchinaId < 1)
                        cmd.Parameters.AddWithValue("@idma", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@idma", macchinaId);


                    cmd.ExecuteNonQuery();

                    string IdAddOffCreaOggettoId = AddOffCreaOggettoId.Text.Trim();
                    if (!String.IsNullOrEmpty(IdAddOffCreaOggettoId) && int.TryParse(IdAddOffCreaOggettoId, out int tempid))
                    {
                        if (tempid == idQ)
                        {
                            UpdateFields("OAO", "CA", false);
                        }
                    }

                    SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);

                    UpdateFields("R", "CE", false);
                    UpdateFields("R", "E", false);
                    UpdateRicambi();

                    MessageBox.Show("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento del ricambio. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("R", "E", true);
                }
            }
            return;
        }

        private void BtDelComp_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("R", "E", false);

            string nome = ChangeDatiCompNome.Text.Trim();
            string idF = ChangeDatiCompID.Text;

            string er_list = "";

            er_list += ValidateComponenteNome(nome);

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare il Pezzo di Ricambio?", "Eliminare Pezzo di Ricambio", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);
                return;
            }

            string commandText = "DELETE FROM " + schemadb + @"[pezzi_ricambi] WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);


                    cmd.ExecuteNonQuery();

                    UpdateFields("R", "CE", false);
                    UpdateFields("R", "E", false);
                    UpdateRicambi();

                    MessageBox.Show("Pezzo di ricambio (" + nome + ") eliminato.");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante eliminazione pezzo di ricambio. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("R", "E", true);
                }
            }
            return;
        }

        private void BtCloseChangesComp_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("R", "CE", false);
            UpdateFields("R", "E", false);
        }

        private void DataGridViewComp_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string macchina = row.Cells[1].Value.ToString();
                    string fornitore = row.Cells[2].Value.ToString();
                    string nome = row.Cells[3].Value.ToString();
                    string code = row.Cells[4].Value.ToString();
                    string prezzo = row.Cells[5].Value.ToString();
                    string descrizione = "";
                    int idcl = 0;

                    ChangeDatiCompCliente.SelectedIndex = 0;
                    ChangeDatiCompSupplier.SelectedIndex = ChangeDatiCompSupplier.FindString(fornitore);

                    string commandText = @"SELECT 
												" + schemadb + @"[pezzi_ricambi].descrizione AS descrizione, 
												" + schemadb + @"[clienti_macchine].ID_cliente AS Cliente 
											FROM " + schemadb + @"[pezzi_ricambi] 
											LEFT JOIN " + schemadb + @"[clienti_macchine] 
												ON " + schemadb + @"[clienti_macchine].Id = " + schemadb + @"[pezzi_ricambi].ID_macchina 
											WHERE " + schemadb + @"[pezzi_ricambi].Id=@ID LIMIT 1;";

                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {

                        try
                        {

                            cmd.Parameters.AddWithValue("@ID", id);
                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                descrizione = Convert.ToString(reader["descrizione"]);
                                if (!string.IsNullOrEmpty(Convert.ToString(reader["Cliente"])))
                                    idcl = Convert.ToInt32(reader["Cliente"]);
                            }
                            reader.Close();
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante popolamento Macchine e Clienti. Codice: " + ReturnErorrCode(ex));


                            return;
                        }
                    }

                    ChangeDatiCompID.Text = id;
                    ChangeDatiCompNome.Text = nome;
                    ChangeDatiCompCode.Text = code;
                    ChangeDatiCompPrice.Text = prezzo;
                    ChangeDatiCompDesc.Text = descrizione;

                    int indexCombo = 0;
                    for (int i = 0; i < ChangeDatiCompCliente.Items.Count; i++)
                    {
                        if (ChangeDatiCompCliente.Items[i].GetHashCode() == idcl)
                        {
                            indexCombo = i;
                        }
                    }

                    ChangeDatiCompCliente.SelectedIndex = indexCombo;

                    indexCombo = ChangeDatiCompMachine.FindString(macchina);
                    indexCombo = indexCombo > 0 ? indexCombo : 0;
                    ChangeDatiCompMachine.SelectedIndex = indexCombo;

                    UpdateFields("R", "E", true);

                    ChangeDatiCompMachine.Enabled = false;
                    ChangeDatiCompCliente.Enabled = false;
                }
            }
        }

        private void AddDatiCompCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            ComboBox ctr = AddDatiCompMachine;

            if (cmb.DataSource == null || ctr.DataSource == null)
            {
                return;
            }

            int curItemValue = cmb.SelectedItem.GetHashCode();

            if (curItemValue > 0)
            {
                Populate_combobox_machine(new ComboBox[] { ctr }, curItemValue);
                ctr.Enabled = true;
            }
            else
            {
                ctr.Enabled = false;
                Populate_combobox_dummy(ctr);
                ctr.SelectedIndex = 0;
            }
            return;
        }

        private void ChangeDatiCompCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            ComboBox ctr = ChangeDatiCompMachine;

            if (ctr.DataSource == null || cmb.DataSource == null)
            {
                return;
            }


            int curItemValue = cmb.SelectedItem.GetHashCode();

            if (curItemValue > 0)
            {
                Populate_combobox_machine(new ComboBox[] { ctr }, curItemValue);
                ctr.Enabled = true;
            }
            else
            {
                ctr.Enabled = false;
                Populate_combobox_dummy(ctr);
                ctr.SelectedIndex = 0;
            }
            return;
        }

        private void LoadCompTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewComp;

            int count = 1;
            string codiceRicambioFilter = dataGridViewComp_Filtro_Codice.Text.Trim();

            string addInfo = "";
            List<string> paramsQuery = new List<string>();

            if (codiceRicambioFilter != dataGridViewComp_Filtro_Codice.PlaceholderText && String.IsNullOrEmpty(codiceRicambioFilter) == false)
                paramsQuery.Add(" [pezzi_ricambi].codice LIKE @codiceRicambioFilter");

            if (paramsQuery.Count > 0)
                addInfo = " WHERE " + String.Join(" AND ", paramsQuery) + " ";

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[pezzi_ricambi] " + addInfo;


            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {

                cmdCount.Parameters.AddWithValue("@codiceRicambioFilter", "%" + codiceRicambioFilter + "%");
                count = Convert.ToInt32(cmdCount.ExecuteScalar());
                count = (count - 1) / recordsPerPage + 1;
                MaxPageDataComp.Text = Convert.ToString((count > 1) ? count : 1);
                if (count > 1)
                {
                    DatiCompNxtPage.Enabled = true;
                    DatiCompPrvPage.Enabled = true;
                    DataCompCurPage.Enabled = true;
                }
                else
                {
                    DatiCompNxtPage.Enabled = false;
                    DatiCompPrvPage.Enabled = false;
                    DataCompCurPage.Enabled = false;
                }
                page = (page > count) ? count : page;
                datiGridViewRicambiCurPage = page;
                DataCompCurPage.Text = Convert.ToString(page);
            }


            commandText = @"SELECT 
									" + schemadb + @"[pezzi_ricambi].Id AS ID,
									IIF(" + schemadb + @"[clienti_macchine].Id IS NULL, 
                                        '',
										(" + schemadb + @"[clienti_macchine].Id || ' - ' || " + schemadb + @"[clienti_macchine].modello  || ' (' ||  " + schemadb + @"[clienti_macchine].seriale || ')')
										) AS Macchina,
									IIF(" + schemadb + @"[fornitori].Id IS NULL,
                                        '',
										(" + schemadb + @"[fornitori].Id || ' - ' || " + schemadb + @"[fornitori].nome)
										) AS Fornitore,
									" + schemadb + @"[pezzi_ricambi].nome AS Nome,
									" + schemadb + @"[pezzi_ricambi].codice AS Codice,
									REPLACE(printf('%.2f'," + schemadb + @"[pezzi_ricambi].prezzo),'.',',')  AS Prezzo
								   FROM " + schemadb + @"[pezzi_ricambi]
								   LEFT JOIN " + schemadb + @"[clienti_macchine]
									ON " + schemadb + @"[clienti_macchine].Id = " + schemadb + @"[pezzi_ricambi].ID_macchina
								   LEFT JOIN " + schemadb + @"[fornitori]
									ON " + schemadb + @"[fornitori].Id = " + schemadb + @"[pezzi_ricambi].ID_fornitore " + addInfo +
                                   @" ORDER BY " + schemadb + @"[pezzi_ricambi].Id ASC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;


            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {

                    DataSet ds = new DataSet();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@codiceRicambioFilter", "%" + codiceRicambioFilter + "%");

                    cmd.Fill(ds, "Ricambi");
                    data_grid.DataSource = ds.Tables["Ricambi"].DefaultView;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "Nome", "Nome" },
                        { "Fornitore", "Fornitore" },
                        { "Macchina", "Macchina" },
                        { "Codice", "Codice" },
                        { "Prezzo", "Prezzo" }
                    };
                    int colCount = data_grid.ColumnCount;
                    for (int i = 0; i < colCount; i++)
                    {
                        if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                            data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                        int colw = data_grid.Columns[i].Width;
                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        data_grid.Columns[i].Width = colw;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella Componenti. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }
            return;
        }

        private void TimerdataGridViewCompFilter_Tick(object sender, EventArgs e)
        {
            TimerdataGridViewCompFilter.Stop();
            LoadCompTable(datiGridViewRicambiCurPage);
        }

        private void DataGridViewComp_Filtro_Codice_TextChanged(object sender, EventArgs e)
        {
            TimerdataGridViewCompFilter.Stop();
            TimerdataGridViewCompFilter.Start();
        }

        //TAB CLIENTI
        private void BtAddCliente_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("C", "A", false);

            string nome = AddDatiClienteNome.Text.Trim();
            string stato = AddDatiClienteStato.SelectedItem.ToString().Trim();
            string citta = AddDatiClienteCitta.Text.Trim();
            string prov = AddDatiClienteProv.Text.Trim();

            string er_list = "";

            er_list += ValidateComponenteNome(nome);

            if (string.IsNullOrEmpty(stato))
            {
                er_list += "Stato non valido o vuoto" + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(prov))
            {
                er_list += "Provincia non valida o vuota" + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(citta))
            {
                er_list += "Città non valida o vuota" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "A", true);
                return;
            }

            string commandText = "INSERT INTO " + schemadb + @"[clienti_elenco](nome, stato, citta, provincia) VALUES (@nome,@stato,@citta,@prov);";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    cmd.Parameters.AddWithValue("@citta", citta);
                    cmd.Parameters.AddWithValue("@prov", prov);


                    cmd.ExecuteNonQuery();

                    UpdateFields("C", "CA", true);
                    UpdateFields("C", "A", true);
                    UpdateClienti();

                    MessageBox.Show("Cliente aggiunto al database");
                }
                catch (SQLiteException ex)
                {
                    UpdateFields("C", "A", true);
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void BtSaveChangesClienti_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("C", "E", false);

            string nome = ChangeDatiClientiNome.Text.Trim();
            string stato = ChangeDatiClientiStato.SelectedItem.ToString().Trim();
            string citta = ChangeDatiClientiCitta.Text.Trim();
            string prov = ChangeDatiClientiProvincia.Text.Trim();
            string idF = ChangeDatiClientiID.Text;

            string er_list = "";

            er_list += ValidateComponenteNome(nome);

            if (string.IsNullOrEmpty(stato))
            {
                er_list += "Stato non valido o vuoto" + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(prov))
            {
                er_list += "Provincia non valida o vuota" + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(citta))
            {
                er_list += "Città non valida o vuota" + Environment.NewLine;
            }

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi salvare le modifiche?", "Salvare Cambiamenti Cliente", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);
                return;
            }

            string commandText = "UPDATE " + schemadb + @"[clienti_elenco] SET nome=@nome,stato=@stato,citta=@citta,provincia=@provincia WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    cmd.Parameters.AddWithValue("@citta", citta);
                    cmd.Parameters.AddWithValue("@provincia", prov);
                    cmd.Parameters.AddWithValue("@idq", idQ);

                    cmd.ExecuteNonQuery();

                    UpdateClienti();

                    LoaVisOrdOggTable(OrdiniViewCurPage);
                    LoadOrdiniTable(OrdiniCurPage);
                    LoadOfferteCreaTable(offerteCreaCurPage);
                    LoadMacchinaTable(datiGridViewMacchineCurPage);
                    LoadPrefTable(datiGridViewPrefCurPage);

                    ChangeDatiClientiNome.Text = "";
                    ChangeDatiClientiCitta.Text = "";
                    ChangeDatiClientiProvincia.Text = "";
                    ChangeDatiClientiID.Text = "";

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("C", "E", false);

                    MessageBox.Show("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento del cliente. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "E", true);
                }
            }
            return;
        }

        private void BtDelClienti_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("C", "E", false);

            string nome = ChangeDatiClientiNome.Text.Trim();
            string idF = ChangeDatiClientiID.Text;

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare il cliente?", "Eliminare Cliente", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);
                return;
            }

            string commandText = "DELETE FROM " + schemadb + @"[clienti_elenco] WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);


                    cmd.ExecuteNonQuery();

                    UpdateClienti();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("C", "CE", false);
                    UpdateFields("C", "E", false);

                    MessageBox.Show("Cliente (" + nome + ") eliminato.");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante eliminazione del cliente. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "E", true);
                }
            }
            return;
        }

        private void BtCloseChangesClienti_Click(object sender, EventArgs e)
        {

            UpdateFields("C", "CE", false);
            UpdateFields("C", "E", false);
        }

        private void DataGridViewClienti_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string nome = row.Cells[1].Value.ToString();
                    string stato = row.Cells[2].Value.ToString();
                    string provincia = row.Cells[3].Value.ToString();
                    string citta = row.Cells[4].Value.ToString();

                    ChangeDatiClientiID.Text = id;
                    ChangeDatiClientiNome.Text = nome;
                    ChangeDatiClientiProvincia.Text = provincia;
                    ChangeDatiClientiCitta.Text = citta;
                    ChangeDatiClientiStato.SelectedItem = stato;

                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "E", true);
                }
            }
        }

        private void LoadClientiTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewClienti;

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_elenco]";
            int count = 1;



            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {

                count = Convert.ToInt32(cmdCount.ExecuteScalar());
                count = (count - 1) / recordsPerPage + 1;
                MaxPageDataClienti.Text = Convert.ToString((count > 1) ? count : 1);
                if (count > 1)
                {
                    DatiClientiNxtPage.Enabled = true;
                    DatiClientiPrvPage.Enabled = true;
                    DataClientiCurPage.Enabled = true;
                }
                else
                {
                    DatiClientiNxtPage.Enabled = false;
                    DatiClientiPrvPage.Enabled = false;
                    DataClientiCurPage.Enabled = false;
                }
                page = (page > count) ? count : page;
                datiGridViewClientiCurPage = page;
                DataClientiCurPage.Text = "" + page;
            }

            commandText = @"SELECT Id,nome,stato,provincia,citta FROM " + schemadb + @"[clienti_elenco] ORDER BY Id ASC LIMIT @recordperpage OFFSET @startingrecord;";
            page--;


            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {
                    DataTable ds = new DataTable();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);
                    data_grid.DataSource = null;
                    data_grid.Rows.Clear();
                    if (data_grid.InvokeRequired)
                        data_grid.Invoke(new MethodInvoker(() => data_grid.DataSource = ds));
                    else
                        data_grid.DataSource = ds;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "Id", "ID" },
                        { "nome", "Nome" },
                        { "stato", "Stato" },
                        { "citta", "Città" },
                        { "provincia", "Provincia" }
                    };
                    int colCount = data_grid.ColumnCount;
                    for (int i = 0; i < colCount; i++)
                    {
                        if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                            data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                        int colw = data_grid.Columns[i].Width;
                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        data_grid.Columns[i].Width = colw;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella Clienti. Codice: " + ReturnErorrCode(ex));
                }
            }
            return;
        }


        //TAB PERS RIFERIMENTI

        private void BtAddPersonaRef_Click(object sender, EventArgs e)
        {
            UpdateFields("P", "A", false);

            string nome = AddDatiPRefNome.Text.Trim();
            int idcl = AddDatiPRefCliente.SelectedItem.GetHashCode();
            string tel = AddDatiPRefTel.Text.Trim();
            string mail = AddDatiPRefMail.Text.Trim();

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }
            //add check if ID exist databse

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_elenco] WHERE Id = @user LIMIT 1;";
            int UserExist = 0;

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", idcl);

                    UserExist = Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante verifica ID Cliente. Codice: " + ReturnErorrCode(ex));
                    return;
                }
            }

            if (UserExist < 1)
            {
                er_list += "Cliente non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("P", "A", true);
                return;
            }

            commandText = "INSERT INTO " + schemadb + @"[clienti_riferimenti](nome,ID_clienti, mail, telefono) VALUES (@nome,@idcl,@mail,@tel);";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@mail", mail);
                    cmd.Parameters.AddWithValue("@tel", tel);


                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Persona di riferimento aggiunta al database");

                    UpdateFields("P", "CA", true);
                    UpdateFields("P", "A", true);


                    UpdatePRef();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("P", "A", true);
                }
            }
            return;
        }

        private void BtSaveChangesPref_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("P", "E", false);

            string nome = ChangeDatiPRefNome.Text.Trim();
            int cliente = Convert.ToInt32(ChangeDatiPRefClienti.SelectedItem.GetHashCode());
            string tel = ChangeDatiPRefTelefono.Text.Trim();
            string mail = ChangeDatiPRefMail.Text.Trim();
            string idF = ChangeDatiPRefID.Text;

            ValidationResult answer;
            string commandText = "";

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome Persona di Riferimento non valido o vuoto" + Environment.NewLine;
            }

            answer = ValidateCliente(cliente);
            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi salvare le modifiche?", "Salvare Cambiamenti Persona di Riferimento", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);
                return;
            }

            commandText = "UPDATE " + schemadb + @"[clienti_riferimenti] SET nome=@nome,ID_clienti=@cliente,mail=@mail,telefono=@telefono WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@cliente", cliente);
                    cmd.Parameters.AddWithValue("@mail", mail);
                    cmd.Parameters.AddWithValue("@telefono", tel);
                    cmd.Parameters.AddWithValue("@idq", idQ);

                    cmd.ExecuteNonQuery();

                    UpdatePRef();
                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("P", "CE", false);
                    UpdateFields("P", "E", false);

                    MessageBox.Show("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento del cliente. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("P", "E", true);
                }
            }
            return;
        }

        private void BtDelPref_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("P", "E", false);

            string nome = ChangeDatiPRefNome.Text.Trim();
            string idF = ChangeDatiPRefID.Text;

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare la Persona di Riferimento?", "Eliminare Persona di Riferimento", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);
                return;
            }


            string commandText = "DELETE FROM " + schemadb + @"[clienti_riferimenti] WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);


                    cmd.ExecuteNonQuery();

                    UpdatePRef();
                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("P", "CE", false);
                    UpdateFields("P", "E", false);

                    MessageBox.Show("Persona di Riferimento (" + nome + ") eliminata.");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante eliminazione Persona di Riferimento. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("P", "E", true);
                }
            }
            return;
        }

        private void BtCloseChangesPref_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("P", "CE", false);
            UpdateFields("P", "E", false);
        }

        private void DataGridViewPref_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string cliente = row.Cells[1].Value.ToString();
                    string nome = row.Cells[2].Value.ToString();
                    string mail = row.Cells[3].Value.ToString();
                    string tel = row.Cells[4].Value.ToString();

                    int index = ChangeDatiPRefClienti.FindString(cliente);
                    ChangeDatiPRefClienti.SelectedIndex = index;

                    ChangeDatiPRefID.Text = id;
                    ChangeDatiPRefNome.Text = nome;
                    ChangeDatiPRefTelefono.Text = tel;
                    ChangeDatiPRefMail.Text = mail;

                    UpdateFields("P", "E", true);

                    ChangeDatiPRefClienti.Enabled = false;
                }
            }
        }

        private void LoadPrefTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewPRef;

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_riferimenti]";
            int count = 1;

            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {

                count = Convert.ToInt32(cmdCount.ExecuteScalar());
                count = (count - 1) / recordsPerPage + 1;
                MaxPageDataPRef.Text = Convert.ToString((count > 1) ? count : 1);
                if (count > 1)
                {
                    DatiPRefNxtPage.Enabled = true;
                    DatiPRefPrvPage.Enabled = true;
                    DataPRefCurPage.Enabled = true;
                }
                else
                {
                    DatiPRefNxtPage.Enabled = false;
                    DatiPRefPrvPage.Enabled = false;
                    DataPRefCurPage.Enabled = false;
                }
                page = (page > count) ? count : page;
                datiGridViewPrefCurPage = page;
                DataPRefCurPage.Text = Convert.ToString(page);
            }

            commandText = @"SELECT
									CR.Id AS ID,
									CE.Id || ' - ' || CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')'  AS Cliente,
									CR.nome AS Nome,
									CR.mail AS Mail,
									CR.telefono AS Telefono
								   FROM " + schemadb + @"[clienti_riferimenti] AS CR
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
									ON CE.Id = CR.ID_clienti 
								    ORDER BY CR.Id ASC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {
                    data_grid.RowHeadersVisible = false;
                    DataSet ds = new DataSet();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds, "Riferimenti");
                    data_grid.DataSource = ds.Tables["Riferimenti"].DefaultView;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "Nome", "Nome" },
                        { "Mail", "Mail" },
                        { "Telefono", "Telefono" },
                        { "Cliente", "Cliente" }
                    };
                    int colCount = data_grid.ColumnCount;

                    for (int i = 0; i < colCount; i++)
                    {

                        if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                            data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                        int colw = data_grid.Columns[i].Width;
                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        data_grid.Columns[i].Width = colw;
                    }
                    data_grid.RowHeadersVisible = true;
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento Riferimenti. Codice: " + ReturnErorrCode(ex));
                }
            }
            return;
        }

        //TAB FORNITORI 
        private void BtAddFornitore_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("F", "A", false);

            string nome = AddDatiFornitoreNome.Text.Trim();

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "A", true);

                return;
            }


            string commandText = "INSERT INTO " + schemadb + @"[fornitori](nome) VALUES (@nome);";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);


                    cmd.ExecuteNonQuery();

                    UpdateFields("F", "CA", true);
                    UpdateFields("F", "A", true);
                    UpdateFornitori();

                    MessageBox.Show("Fornitore aggiunto al database");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("F", "A", true);
                }
            }
            return;
        }

        private void BtSaveChangesFornitore_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("F", "E", false);

            string nome = ChangeDatiFornitoreNome.Text.Trim();
            string idF = ChangeDatiFornitoreID.Text;

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi salvare le modifiche?", "Salvare Cambiamenti Fornitore", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);
                return;
            }

            string commandText = "UPDATE " + schemadb + @"[fornitori] SET nome=@nome WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@idq", idQ);


                    cmd.ExecuteNonQuery();

                    UpdateFornitori();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("F", "CE", false);
                    UpdateFields("F", "E", false);

                    MessageBox.Show("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento del fornitore. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("F", "E", true);
                }
            }
            return;
        }

        private void BtDelFornitore_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("F", "E", false);

            string nome = ChangeDatiFornitoreNome.Text.Trim();
            string idF = ChangeDatiFornitoreID.Text;

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare il fornitore(" + nome + "))?", "Eliminare Fornitore", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);
                return;
            }

            string commandText = "DELETE FROM " + schemadb + @"[fornitori] WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);

                    cmd.ExecuteNonQuery();

                    UpdateFornitori();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("F", "CE", false);
                    UpdateFields("F", "E", false);

                    MessageBox.Show("Fornitore " + nome + " eliminato.");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante eliminazione del fornitore. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("F", "E", true);
                }
            }
            return;
        }

        private void BtCloseChangesFornitore_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("F", "CE", false);
            UpdateFields("F", "E", false);
        }

        private void DataGridViewFornitori_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string nome = row.Cells[1].Value.ToString();
                    ChangeDatiFornitoreID.Text = id;
                    ChangeDatiFornitoreNome.Text = nome;

                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("F", "E", true);
                }
            }
        }

        private void LoadFornitoriTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewFornitori;

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[fornitori]";
            int count = 1;

            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {

                count = Convert.ToInt32(cmdCount.ExecuteScalar());
                count = (count - 1) / recordsPerPage + 1;
                MaxPageDataFornitori.Text = Convert.ToString((count > 1) ? count : 1);
                if (count > 1)
                {
                    DatiFornitoriNxtPage.Enabled = true;
                    DatiFornitoriPrvPage.Enabled = true;
                    DataFornitoriCurPage.Enabled = true;
                }
                else
                {
                    DatiFornitoriNxtPage.Enabled = false;
                    DatiFornitoriPrvPage.Enabled = false;
                    DataFornitoriCurPage.Enabled = false;
                }
                page = (page > count) ? count : page;
                datiGridViewFornitoriCurPage = page;
                DataFornitoriCurPage.Text = Convert.ToString(page);
            }

            commandText = @"SELECT Id,nome FROM " + schemadb + @"[fornitori] ORDER BY Id ASC LIMIT " + recordsPerPage;
            page--;

            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {
                    DataTable ds = new DataTable();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);
                    data_grid.DataSource = ds;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "Id", "ID" },
                        { "nome", "Nome" }
                    };
                    int colCount = data_grid.ColumnCount;
                    for (int i = 0; i < colCount; i++)
                    {
                        if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                            data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                        int colw = data_grid.Columns[i].Width;
                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        data_grid.Columns[i].Width = colw;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella Fornitori. Codice: " + ReturnErorrCode(ex));
                }
            }
            return;
        }

        //TAB MACCHINE

        private void BtAddMacchina_Click(object sender, EventArgs e)
        {
            UpdateFields("M", "A", false);

            string nome = AddDatiMacchinaNome.Text.Trim();
            int idcl = Convert.ToInt32(AddDatiMacchinaCliente.SelectedValue.GetHashCode());
            string seriale = AddDatiMacchinaSeriale.Text.Trim();
            string codice = AddDatiMacchinaCodice.Text.Trim();

            ValidationResult answer = new ValidationResult();
            string commandText = "";

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            answer = ValidateCliente(idcl);
            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("M", "A", true);
                return;
            }

            commandText = "INSERT INTO " + schemadb + @"[clienti_macchine](modello, ID_cliente, seriale, codice) VALUES (@modello, @idcl, @seriale, @code);";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@modello", nome);
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@seriale", seriale);
                    cmd.Parameters.AddWithValue("@code", codice);

                    cmd.ExecuteNonQuery();

                    UpdateFields("M", "CA", true);
                    UpdateFields("M", "A", true);

                    UpdateMacchine();

                    MessageBox.Show("Macchina aggiunta al database");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("M", "A", true);
                }
            }
            return;
        }

        private void BtSaveChangesMacchina_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("M", "E", false);

            string nome = ChangeDatiMacchinaNome.Text.Trim();
            int cliente = Convert.ToInt32(ChangeDatiMacchinaCliente.SelectedItem.GetHashCode());
            string seriale = ChangeDatiMacchinaSeriale.Text.Trim();
            string codice = ChangeDatiMacchinaCodice.Text.Trim();
            string idF = ChangeDatiMacchinaID.Text;

            ValidationResult answer;
            string commandText;

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            answer = ValidateCliente(cliente);
            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("M", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi salvare le modifiche?", "Salvare Cambiamenti Macchina", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("M", "E", true);
                return;
            }

            commandText = "UPDATE " + schemadb + @"[clienti_macchine] SET modello=@nome,ID_cliente=@cliente,seriale=@seriale, codice=@code WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@cliente", cliente);
                    cmd.Parameters.AddWithValue("@seriale", seriale);
                    cmd.Parameters.AddWithValue("@code", codice);
                    cmd.Parameters.AddWithValue("@idq", idQ);

                    cmd.ExecuteNonQuery();

                    UpdateMacchine();
                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("M", "CE", false);
                    UpdateFields("M", "E", false);

                    MessageBox.Show("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento della macchina. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("M", "E", true);
                }
            }
            return;
        }

        private void BtDelMacchina_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("M", "E", false);

            string nome = ChangeDatiMacchinaNome.Text.Trim();
            string idF = ChangeDatiMacchinaID.Text;

            string er_list = "";

            if (string.IsNullOrEmpty(nome))
            {
                er_list += "Nome non valido o vuoto" + Environment.NewLine;
            }

            if (!int.TryParse(idF, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("M", "E", true);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare la macchina?", "Eliminare Macchina", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("M", "E", true);
                return;
            }


            string commandText = "DELETE FROM " + schemadb + @"[clienti_macchine] WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);


                    cmd.ExecuteNonQuery();

                    UpdateMacchine();

                    UpdateFields("M", "CE", false);
                    UpdateFields("M", "E", false);

                    MessageBox.Show("Macchina (" + nome + ") eliminata.");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante eliminazione macchina. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("M", "E", true);
                }
            }
            return;
        }

        private void BtCloseChangesMacchina_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("M", "CE", false);
            UpdateFields("M", "E", false);
        }

        private void DataGridViewMacchina_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string cliente = row.Cells[1].Value.ToString();
                    string nome = row.Cells[2].Value.ToString();
                    string seriale = row.Cells[3].Value.ToString();
                    string codice = row.Cells[4].Value.ToString();

                    int index = ChangeDatiMacchinaCliente.FindString(cliente);
                    ChangeDatiMacchinaCliente.SelectedIndex = index;

                    ChangeDatiMacchinaID.Text = id;
                    ChangeDatiMacchinaNome.Text = nome;
                    ChangeDatiMacchinaSeriale.Text = seriale;
                    ChangeDatiMacchinaCodice.Text = codice;

                    UpdateFields("M", "E", true);

                    ChangeDatiMacchinaCliente.Enabled = false;
                }
            }
        }

        private void LoadMacchinaTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewMacchina;

            if (dataGridViewMacchina_Filtro_Cliente.DataSource == null)
            {
                return;
            }
            int idcl = Convert.ToInt32(dataGridViewMacchina_Filtro_Cliente.SelectedValue.GetHashCode());

            string addInfo = "";
            List<string> paramsQuery = new List<string>();

            if (idcl > 0)
                paramsQuery.Add(" CM.ID_cliente = @idcl ");

            if (paramsQuery.Count > 0)
                addInfo = " WHERE " + String.Join(" AND ", paramsQuery);

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_macchine] AS CM " + addInfo + ";";
            int count = 1;

            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    cmdCount.Parameters.AddWithValue("@idcl", idcl);
                    count = Convert.ToInt32(cmdCount.ExecuteScalar());
                    count = (count - 1) / recordsPerPage + 1;
                    MaxPageDataMacchina.Text = Convert.ToString((count > 1) ? count : 1);
                    if (count > 1)
                    {
                        DatiMacchinaNxtPage.Enabled = true;
                        DatiMacchinaPrvPage.Enabled = true;
                        DataMacchinaCurPage.Enabled = true;
                    }
                    else
                    {
                        DatiMacchinaNxtPage.Enabled = false;
                        DatiMacchinaPrvPage.Enabled = false;
                        DataMacchinaCurPage.Enabled = false;
                    }
                    page = (page > count) ? count : page;
                    datiGridViewMacchineCurPage = page;
                    DataMacchinaCurPage.Text = Convert.ToString(page);
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante verifica ID Cliente. Codice: " + ReturnErorrCode(ex));
                    return;
                }
            }

            commandText = @"SELECT 
									CM.Id AS ID,
									(CE.Id || ' - ' || CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')') AS Cliente,
									CM.modello        AS Modello,
									CM.seriale AS Seriale,
									CM.codice AS code 
								   FROM " + schemadb + @"[clienti_macchine] AS CM
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
									ON CE.Id = CM.ID_cliente " + addInfo +
                                   @"ORDER BY CM.Id ASC LIMIT @recordperpage OFFSET @startingrecord ";

            page--;

            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {

                    DataSet ds = new DataSet();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);

                    cmd.Fill(ds, "Macchine");
                    data_grid.DataSource = ds.Tables["Macchine"].DefaultView;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "Modello", "Modello" },
                        { "Cliente", "Cliente" },
                        { "Seriale", "Seriale" },
                        { "code", "Codice" }
                    };
                    int colCount = data_grid.ColumnCount;
                    for (int i = 0; i < colCount; i++)
                    {
                        if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                            data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                        int colw = data_grid.Columns[i].Width;
                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        data_grid.Columns[i].Width = colw;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento Macchine. Codice: " + ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void DataGridViewMacchina_Filtro_Cliente_SelectedValueChanged(object sender, EventArgs e)
        {
            LoadMacchinaTable();
        }

        //OFFERTE CREA
        private void AddOffCreaCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaCliente.DataSource == null)
            {
                return;
            }

            int curItemValue = AddOffCreaCliente.SelectedItem.GetHashCode();
            if (curItemValue > 0)
            {
                Populate_combobox_pref(AddOffCreaPRef, curItemValue);
                AddOffCreaPRef.Enabled = true;
            }
            else
            {
                Populate_combobox_dummy(AddOffCreaPRef);
                AddOffCreaPRef.SelectedIndex = 0;
                AddOffCreaPRef.Enabled = false;
            }
        }

        private void BtCreaOfferta_Click(object sender, EventArgs e)
        {

            UpdateFields("OC", "A", false);

            string numeroOff = AddOffCreaNOff.Text.Trim();
            string spedizioni = AddOffCreaSpedizione.Text.Trim();
            string dataoffString = AddOffCreaData.Text.Trim();

            int gestSP = AddOffCreaSpedizioneGest.SelectedItem.GetHashCode();

            int idcl = Convert.ToInt32(AddOffCreaCliente.SelectedValue.GetHashCode());
            int idpref = Convert.ToInt32(AddOffCreaPRef.SelectedValue.GetHashCode());
            int stato = Convert.ToInt32(AddOffCreaStato.SelectedValue.GetHashCode());

            ValidationResult dataoffValue = new ValidationResult();

            stato = (stato < 0) ? 0 : stato;

            string commandText = "";

            ValidationResult answer = new ValidationResult();
            ValidationResult prezzoSpedizione = new ValidationResult();

            string er_list = "";
            if (string.IsNullOrEmpty(numeroOff) || !Regex.IsMatch(numeroOff, @"^\d+$"))
            {
                er_list += "Numero Offerta non valido o vuoto" + Environment.NewLine;
            }

            dataoffValue = ValidateDate(dataoffString);
            er_list += dataoffValue.Error;

            answer = ValidateCliente(idcl);
            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            if (idpref > 0)
            {
                answer = ValidatePRef(idpref);
                if (!answer.Success)
                {
                    MessageBox.Show(answer.Error);
                    return;
                }
                er_list += answer.Error;
            }

            if (!string.IsNullOrEmpty(spedizioni))
            {
                prezzoSpedizione = ValidateSpedizione(spedizioni, gestSP);
                er_list += prezzoSpedizione.Error;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("OC", "A", true);
                return;
            }

            commandText = @"INSERT INTO " + schemadb + @"[offerte_elenco]
                                (data_offerta, codice_offerta, ID_cliente, ID_riferimento,stato, costo_spedizione, gestione_spedizione) 
                            VALUES 
                                (@data,@code,@idcl,@idref,@stato, @cossp, @gestsp);";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@data", dataoffValue.DateValue);
                    cmd.Parameters.AddWithValue("@code", numeroOff);
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    if (idpref > 0)
                        cmd.Parameters.AddWithValue("@idref", idpref);
                    else
                        cmd.Parameters.AddWithValue("@idref", DBNull.Value);

                    if (prezzoSpedizione.DecimalValue.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@cossp", prezzoSpedizione.DecimalValue);
                        cmd.Parameters.AddWithValue("@gestsp", gestSP);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@cossp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@gestsp", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();

                    int temp_FieldOrdOfferta = ComboBoxOrdOfferta.SelectedItem.GetHashCode();
                    int temp_FieldOrdCliente = ComboBoxOrdCliente.SelectedIndex;

                    UpdateFields("OC", "CA", true);
                    UpdateFields("OC", "A", true);


                    UpdateOfferteCrea();

                    string temp_info = "";
                    if (stato == 1)
                        temp_info = Environment.NewLine + "Nel caso, è necessario creare l'ordine associato all'oferta.";

                    if (ComboBoxOrdCliente.SelectedItem.GetHashCode() == idcl)
                    {
                        ComboBoxOrdCliente.SelectedIndex = temp_FieldOrdCliente;
                        if (temp_FieldOrdOfferta > 0) ComboBoxOrdOfferta.SelectedIndex = FindIndexFromValue(ComboBoxOrdOfferta, temp_FieldOrdOfferta);
                    }

                    MessageBox.Show("Offerta Creata." + temp_info);
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("OC", "A", true);
                }
                finally
                {
                    UpdateFields("OC", "A", true);
                }
            }
            return;
        }

        private void LoadOfferteCreaTable(int page = 1)
        {
            DataGridView[] data_grid = new DataGridView[] { DataGridViewOffCrea };

            int idcl = Convert.ToInt32(OffCreaFiltroCliente.SelectedValue.GetHashCode());
            int stato = Convert.ToInt32(OffCreaFiltroStato.SelectedValue.GetHashCode());

            string addInfo = "";
            List<string> paramsQuery = new List<string>();

            if (idcl > 0)
                paramsQuery.Add(" OE.ID_cliente = @idcl ");
            if (stato >= 0)
                paramsQuery.Add(" OE.stato = @stato ");

            if (paramsQuery.Count > 0)
                addInfo = " WHERE " + String.Join(" AND ", paramsQuery);

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[offerte_elenco] AS OE " + addInfo;
            int count = 1;


            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmdCount.Parameters.AddWithValue("@idcl", idcl);
                    cmdCount.Parameters.AddWithValue("@stato", stato);
                    count = Convert.ToInt32(cmdCount.ExecuteScalar());
                    count = (count - 1) / recordsPerPage + 1;
                    MaxPageOffCrea.Text = Convert.ToString((count > 1) ? count : 1);
                    if (count > 1)
                    {
                        OffCreaNxtPage.Enabled = true;
                        OffCreaPrvPage.Enabled = true;
                        OffCreaCurPage.Enabled = true;
                    }
                    else
                    {
                        OffCreaNxtPage.Enabled = false;
                        OffCreaPrvPage.Enabled = false;
                        OffCreaCurPage.Enabled = false;
                    }
                    page = (page > count) ? count : page;
                    offerteCreaCurPage = page;
                    OffCreaCurPage.Text = Convert.ToString(page);
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante verifica records in elenco offerte. Codice: " + ReturnErorrCode(ex));
                    return;
                }
            }


            commandText = @"SELECT  
									OE.Id AS ID,
									CE.Id || ' - ' || CE.nome  || ' (' ||  CE.stato || ')' AS Cliente,
									IIF(OE.ID_riferimento>0, CR.Id  || ' - ' || CR.nome,'') AS Pref,
									OE.codice_offerta        AS cod,
									strftime('%d/%m/%Y',OE.data_offerta) AS dat,
									REPLACE( printf('%.2f',OE.tot_offerta ),'.',',') AS totoff,
									IIF(OE.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OE.costo_spedizione ),'.',','), NULL) AS csped,
									(CASE OE.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,

									CASE OE.stato WHEN 0 THEN 'APERTA'  WHEN 1 THEN 'ORDINATA' WHEN 2 THEN 'ANNULLATA' END AS Stato,
									CASE OE.trasformato_ordine WHEN 0 THEN 'No'  WHEN 1 THEN 'Sì' END AS conv
								   FROM " + schemadb + @"[offerte_elenco] AS OE
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OE.ID_cliente 
								   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OE.ID_riferimento " + addInfo +

                                   @" ORDER BY OE.Id DESC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {
                    DataSet ds = new DataSet();
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);
                    cmd.SelectCommand.Parameters.AddWithValue("@stato", stato);
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds, "OfferteCrea");
                    for (int i = 0; i < data_grid.Length; i++)
                    {
                        data_grid[i].DataSource = ds.Tables["OfferteCrea"].DefaultView;

                        Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "Cliente", "Cliente" },
                        { "Pref", "Contatto" },
                        { "cod", "N.Offerta" },
                        { "dat", "Data" },
                        { "totoff", "Totale Offerta"+Environment.NewLine+"(Excl. Spedizioni)"},
                        { "Stato", "Stato" },
                        { "csped", "Costo Spedizione"+Environment.NewLine+"(Excl. Sconti)" },
                        { "spedg", "Gestione Costo Spedizione" },
                        { "conv", "Ordine Creato" }
                    };
                        int colCount = data_grid[i].ColumnCount;
                        for (int j = 0; j < colCount; j++)
                        {
                            if (columnNames.ContainsKey(data_grid[i].Columns[j].HeaderText))
                                data_grid[i].Columns[j].HeaderText = columnNames[data_grid[i].Columns[j].HeaderText];

                            data_grid[i].Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                            int colw = data_grid[i].Columns[j].Width;
                            data_grid[i].Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                            data_grid[i].Columns[j].Width = colw;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella crea offerta. Codice: " + ReturnErorrCode(ex));

                    return;
                }
            }
            return;
        }

        private void LoadOfferteOggettiCreaTable(int idof)
        {
            DataGridView data_grid = dataGridViewOffCreaOggetti;

            if (idof > 0)
            {

                string commandText = @"SELECT 
										OP.Id AS ID,
										PR.Id || ' - ' || PR.nome  || ' (' ||  PR.codice || ')' AS pezzo,
										REPLACE( printf('%.2f',OP.prezzo_unitario_originale),'.',',')  AS porig,
										REPLACE( printf('%.2f',OP.prezzo_unitario_sconto),'.',',')  AS pscont,
										OP.pezzi AS numpezzi,
                                        REPLACE( printf('%.2f',OP.prezzo_unitario_sconto * OP.pezzi),'.',',')  AS totparz
									   FROM " + schemadb + @"[offerte_pezzi] AS OP
									   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
											ON PR.Id = OP.ID_ricambio
									   WHERE OP.ID_offerta=@idofferta
									   ORDER BY OP.Id ASC;";


                using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
                {
                    try
                    {

                        DataSet ds = new DataSet();
                        cmd.SelectCommand.Parameters.AddWithValue("@idofferta", idof);

                        cmd.Fill(ds, "OfferteCreaOgg");
                        data_grid.DataSource = ds.Tables["OfferteCreaOgg"].DefaultView;

                        Dictionary<string, string> columnNames = new Dictionary<string, string>
                        {
                            { "ID", "ID" },
                            { "pezzo", "Ricambio" },
                            { "porig", "Prezzo Nell'Offerta" },
                            { "pscont", "Prezzo Scontato" },
                            { "numpezzi", "N. Pezzi" },
                            { "totparz", "Totale Parziale" }
                        };
                        int colCount = data_grid.ColumnCount;
                        for (int i = 0; i < colCount; i++)
                        {
                            if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                                data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                            data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                            int colw = data_grid.Columns[i].Width;
                            data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                            data_grid.Columns[i].Width = colw;
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        MessageBox.Show("Errore durante popolamento tabella pezzi dell'offerta. Codice: " + ReturnErorrCode(ex));


                        return;
                    }
                }
            }
            return;
        }

        private void SelOffCreaCl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelOffCreaCl.DataSource == null)
            {
                return;
            }

            int idcl = (String.IsNullOrEmpty(SelOffCreaCl.Text.Trim())) ? 0 : Convert.ToInt32(SelOffCreaCl.Text.Split('-')[0]);
            Populate_combobox_offerte_crea(new ComboBox[] { SelOffCrea }, idcl);
        }

        private void SelOffCrea_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (SelOffCrea.DataSource == null)
            {
                return;
            }

            int curItemValue = SelOffCrea.SelectedItem.GetHashCode();

            if (curItemValue > 0)
            {

                LoadOfferteOggettiCreaTable(curItemValue);

                string commandText = @"SELECT  ID_cliente as Cliente FROM " + schemadb + @"[offerte_elenco] WHERE id=@idofferta LIMIT " + recordsPerPage;

                using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@idofferta", curItemValue);
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            AddOffCreaOggettoClieID.Text = reader["Cliente"].ToString();
                        }
                        reader.Close();

                        Populate_combobox_machine(new ComboBox[] { AddOffCreaOggettoMach }, Convert.ToInt32(AddOffCreaOggettoClieID.Text));
                        Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, 0, true);

                        AddOffCreaOggettoMach.Enabled = true;
                        AddOffCreaOggettoRica.Enabled = true;
                        AddOffCreaOggettoPezzoFiltro.Enabled = true;

                    }
                    catch (SQLiteException ex)
                    {
                        MessageBox.Show("Errore durante selezione cliente. Codice: " + ReturnErorrCode(ex));
                        return;
                    }
                }

                return;
            }
            else
            {
                AddOffCreaOggettoMach.Enabled = false;
                AddOffCreaOggettoRica.Enabled = false;
                AddOffCreaOggettoPezzoFiltro.Enabled = false;

                Populate_combobox_dummy(AddOffCreaOggettoMach);
                Populate_combobox_dummy(AddOffCreaOggettoRica);
                AddOffCreaOggettoRica.SelectedIndex = 0;
                AddOffCreaOggettoMach.SelectedIndex = 0;

                AddOffCreaOggettoPori.Text = "";
                AddOffCreaOggettoPoriRic.Text = "";
                AddOffCreaOggettoPsco.Text = "";
                AddOffCreaOggettoPezzi.Text = "";
                AddOffCreaOggettoDesc.Text = "";
                AddOffCreaOggettoClieID.Text = "";

                AddOffCreaOggettoPori.Enabled = false;
                AddOffCreaOggettoPsco.Enabled = false;
                AddOffCreaOggettoPezzi.Enabled = false;

                ClearDataGridView(dataGridViewOffCreaOggetti);

            }
        }

        private void AddOffCreaOggettoMach_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaOggettoMach.DataSource == null)
            {
                return;
            }

            int curItem = AddOffCreaOggettoMach.SelectedItem.GetHashCode();

            if (curItem > 0)
            {
                Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, curItem, true);
            }

            else
            {
                Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, 0, true);
                AddOffCreaOggettoPori.Text = "";
                AddOffCreaOggettoPoriRic.Text = "";
                AddOffCreaOggettoPsco.Text = "";
                AddOffCreaOggettoDesc.Text = "";
                AddOffCreaOggettoPezzi.Text = "";
            }

        }

        private void AddOffCreaOggettoRica_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaOggettoRica.DataSource == null)
            {
                return;
            }

            int curItemValue = AddOffCreaOggettoRica.SelectedItem.GetHashCode();

            if (curItemValue > 0)
            {

                string commandText = @"SELECT 
										REPLACE(printf('%.2f',prezzo) ,'.',',') AS prezzo,
										descrizione
									   FROM " + schemadb + @"[pezzi_ricambi]
									   WHERE Id=@idpezzo
									   ORDER BY Id ASC LIMIT 1;";

                using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                {
                    try
                    {

                        cmd.Parameters.AddWithValue("@idpezzo", curItemValue);
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            AddOffCreaOggettoPoriRic.Text = reader["prezzo"].ToString();
                            AddOffCreaOggettoPori.Text = reader["prezzo"].ToString();
                            AddOffCreaOggettoPsco.Text = reader["prezzo"].ToString();
                            AddOffCreaOggettoDesc.Text = reader["descrizione"].ToString();
                        }
                        reader.Close();
                        UpdateFields("OAO", "A", true);
                        BtCancChangesOffOgg.Enabled = true;
                    }
                    catch (SQLiteException ex)
                    {
                        MessageBox.Show("Errore durante selezione cliente. Codice: " + ReturnErorrCode(ex));
                        return;
                    }
                }
            }
            else
            {
                AddOffCreaOggettoPoriRic.Text = "";
                AddOffCreaOggettoPori.Text = "";
                AddOffCreaOggettoPsco.Text = "";
                AddOffCreaOggettoDesc.Text = "";
                AddOffCreaOggettoPezzi.Text = "";
                BtCancChangesOffOgg.Enabled = false;
            }
            return;
        }

        private void BtAddRicToOff_Click(object sender, EventArgs e)
        {

            UpdateFields("OAO", "A", false);

            string prezzoOr = AddOffCreaOggettoPori.Text.Trim();
            string prezzoSc = AddOffCreaOggettoPsco.Text.Trim();
            string qta = AddOffCreaOggettoPezzi.Text.Trim();

            int idof = Convert.ToInt32(SelOffCrea.SelectedItem.GetHashCode());
            int idir = Convert.ToInt32(AddOffCreaOggettoRica.SelectedItem.GetHashCode());

            string er_list = "";


            if (!Decimal.TryParse(prezzoOr, style, culture, out decimal prezzoOrV))
            {
                er_list += "Prezzo non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzoOrV < 0)
                {
                    er_list += "Il prezzo deve essere positivo" + Environment.NewLine;
                }
            }

            if (!Decimal.TryParse(prezzoSc, style, culture, out decimal prezzoScV))
            {
                er_list += "Prezzo non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzoScV < 0)
                {
                    er_list += "Il prezzo deve essere positivo" + Environment.NewLine;
                }
            }

            if (!int.TryParse(qta, out int qtaV))
            {
                er_list += "Quantità non aalido o vuota" + Environment.NewLine;
            }
            else
            {
                if (qtaV < 1)
                    er_list += "La quanità deve essere positiva, intera e maggiore di 0" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                UpdateFields("OAO", "A", true);
                return;
            }


            string commandText = @" BEGIN TRANSACTION;
                                    INSERT OR ROLLBACK INTO " + schemadb + @"[offerte_pezzi]
                                        (ID_offerta, ID_ricambio, prezzo_unitario_originale, prezzo_unitario_sconto,pezzi) 
                                        VALUES (@idof,@idri,@por,@pos,@pezzi);
                                    UPDATE OR ROLLBACK " + schemadb + @"[offerte_elenco]
									    SET tot_offerta = ifnull( (SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[offerte_pezzi] AS OP WHERE OP.ID_offerta=@idof) , 0) 
									    WHERE Id=@idof LIMIT 1;
                                    COMMIT;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idof", idof);
                    cmd.Parameters.AddWithValue("@idri", idir);
                    cmd.Parameters.AddWithValue("@por", prezzoOrV);
                    cmd.Parameters.AddWithValue("@pos", prezzoScV);
                    cmd.Parameters.AddWithValue("@pezzi", qtaV);
                    cmd.ExecuteNonQuery();

                    LoadOfferteCreaTable();
                    LoadOfferteOggettiCreaTable(idof);

                    UpdateFields("OAO", "A", false);
                    UpdateFields("OAO", "CA", false);

                    ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);
                    SelOffCrea_SelectedIndexChanged(this, System.EventArgs.Empty);

                    AddOffCreaOggettoRica.Enabled = true;

                    MessageBox.Show("Oggetto aggiunta all'offerta");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("OAO", "A", true);
                }
            }
            return;
        }

        private void DataGridViewOffCrea_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string cliente = row.Cells[1].Value.ToString();
                    string pref = row.Cells[2].Value.ToString();
                    string nord = row.Cells[3].Value.ToString();
                    string dataoffString = row.Cells[4].Value.ToString();
                    string totOf = row.Cells[5].Value.ToString();
                    string spedizione = row.Cells[6].Value.ToString();
                    string gestsp = row.Cells[7].Value.ToString();
                    string stato = row.Cells[8].Value.ToString();

                    int id_cliente = Convert.ToInt32(cliente.Split('-')[0]);
                    int index;


                    AddOffCreaId.Text = id;
                    AddOffCreaSpedizione.Text = spedizione;

                    index = FindIndexFromValue(AddOffCreaCliente, id_cliente);
                    AddOffCreaCliente.SelectedIndex = index;

                    Populate_combobox_pref(AddOffCreaPRef, AddOffCreaCliente.SelectedValue.GetHashCode());

                    index = AddOffCreaPRef.FindString(pref);
                    AddOffCreaPRef.SelectedIndex = index;

                    AddOffCreaNOff.Text = nord;
                    AddOffCreaData.Text = dataoffString;

                    index = AddOffCreaStato.FindString(stato);
                    AddOffCreaStato.SelectedIndex = index;

                    AddOffCreaSpedizioneGest.SelectedIndex = AddOffCreaSpedizioneGest.FindString(gestsp);

                    UpdateFields("OC", "E", true);

                    AddOffCreaCliente.Enabled = false;
                }
            }
        }

        private void BtCancChangesOff_Click(object sender, EventArgs e)
        {
            UpdateFields("OC", "CA", false);
            UpdateFields("OC", "E", false);
            UpdateFields("OC", "A", true);
        }

        private void BtSaveChangesOff_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("OC", "E", false);
            UpdateFields("OC", "A", false);

            int idOf = Convert.ToInt32(AddOffCreaId.Text.Trim());
            string numeroOff = AddOffCreaNOff.Text.Trim();
            string dataoffString = AddOffCreaData.Text.Trim();

            string spedizioni = AddOffCreaSpedizione.Text.Trim();
            int gestSP = AddOffCreaSpedizioneGest.SelectedItem.GetHashCode();

            int cliente = Convert.ToInt32(AddOffCreaCliente.SelectedItem.GetHashCode());
            int pref = Convert.ToInt32(AddOffCreaPRef.SelectedItem.GetHashCode());
            int stato = Convert.ToInt32(AddOffCreaStato.SelectedItem.GetHashCode());

            ValidationResult answer;
            ValidationResult prezzoSpedizione = new ValidationResult();
            ValidationResult dataoffValue = new ValidationResult();

            string commandText;

            string er_list = "";

            if (string.IsNullOrEmpty(numeroOff) || !Regex.IsMatch(numeroOff, @"^\d+$"))
            {
                er_list += "Numero Offerta non valido o vuoto" + Environment.NewLine;
            }

            dataoffValue = ValidateDate(dataoffString);
            er_list += dataoffValue.Error;

            answer = ValidateCliente(cliente);
            if (!answer.Success)
            {
                MessageBox.Show(answer.Error);
                return;
            }
            er_list += answer.Error;

            if (pref > 0)
            {
                answer = ValidatePRef(pref);
                if (!answer.Success)
                {
                    MessageBox.Show(answer.Error);
                    return;
                }
                er_list += answer.Error;
            }

            if (!string.IsNullOrEmpty(spedizioni))
            {
                if (!string.IsNullOrEmpty(spedizioni))
                {
                    prezzoSpedizione = ValidateSpedizione(spedizioni, gestSP);
                    er_list += prezzoSpedizione.Error;
                }
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi salvare le modifiche?", "Salvare Cambiamenti Offerta", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);
                return;
            }

            commandText = @"UPDATE " + schemadb + @"[offerte_elenco] 
                            SET data_offerta=@date, codice_offerta=@noff, ID_cliente=@idcl, ID_riferimento=@idref,stato=@stato, costo_spedizione=@cossp , gestione_spedizione=@gestsp 
                            WHERE Id=@idof LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@date", dataoffValue.DateValue);
                    cmd.Parameters.AddWithValue("@noff", numeroOff);
                    cmd.Parameters.AddWithValue("@idcl", cliente);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    cmd.Parameters.AddWithValue("@idof", idOf);
                    if (pref > 0)
                        cmd.Parameters.AddWithValue("@idref", pref);
                    else
                        cmd.Parameters.AddWithValue("@idref", DBNull.Value);

                    if (prezzoSpedizione.DecimalValue.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@cossp", prezzoSpedizione.DecimalValue);
                        cmd.Parameters.AddWithValue("@gestsp", gestSP);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@cossp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@gestsp", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();

                    int temp_SelOffCrea = SelOffCrea.SelectedItem.GetHashCode();

                    int temp_FieldOrdOfferta = ComboBoxOrdOfferta.SelectedIndex;
                    int temp_FieldOrdCliente = ComboBoxOrdCliente.SelectedIndex;

                    UpdateOfferteCrea();
                    UpdateOrdini(OrdiniViewCurPage);

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "CA", false);
                    UpdateFields("OC", "E", false);
                    UpdateFields("OC", "A", true);

                    if (SelOffCreaCl.SelectedItem.GetHashCode() > 0)
                        SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);

                    if (temp_SelOffCrea > 0)
                        SelOffCrea.SelectedIndex = FindIndexFromValue(SelOffCrea, temp_SelOffCrea);

                    if (ComboSelOrdCl.SelectedItem.GetHashCode() > 0)
                        ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    if (temp_FieldOrdCliente == cliente)
                    {
                        ComboBoxOrdCliente.SelectedIndex = temp_FieldOrdCliente;
                        if (temp_FieldOrdOfferta > 0) ComboBoxOrdOfferta.SelectedIndex = temp_FieldOrdOfferta;
                    }

                    string temp_info = "";
                    if (stato == 1)
                        temp_info = Environment.NewLine + "Nel caso, è necessario creare l'ordine associato all'oferta.";

                    MessageBox.Show("Cambiamenti salvati." + temp_info);
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento dell'OFFERTA. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "A", true);
                    UpdateFields("OC", "E", true);
                }
            }
            return;
        }

        private void BtDelChangesOff_Click(object sender, EventArgs e)
        {

            //DISABILITA CAMPI
            UpdateFields("OC", "E", false);
            UpdateFields("OC", "A", false);

            string idOf = AddOffCreaId.Text.Trim();

            string er_list = "";

            if (!int.TryParse(idOf, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare l'offerta? Tutti i dati relativi all'offerta verrano eliminati", "Eliminare Offerta", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);
                return;
            }

            string commandText = @" DELETE FROM " + schemadb + @"[offerte_pezzi] WHERE ID_offerta=@idq; 
                                    DELETE FROM " + schemadb + @"[offerte_elenco] WHERE Id=@idq LIMIT 1;";



            using (var transaction = connection.BeginTransaction())
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection, transaction))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    int temp = SelOffCrea.SelectedItem.GetHashCode();
                    MessageBox.Show("" + temp);

                    UpdateOfferteCrea();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "CA", true);
                    UpdateFields("OC", "E", false);
                    UpdateFields("OC", "A", true);

                    if (SelOffCreaCl.SelectedItem.GetHashCode() > 0)
                        SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);
                    if (temp > 0)
                        SelOffCrea.SelectedIndex = FindIndexFromValue(SelOffCrea, temp);

                    MessageBox.Show("Offerta eliminata.");
                }
                catch (SQLiteException ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Errore durante eliminazione dell'offferta. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "A", true);
                    UpdateFields("OC", "E", true);
                }
            }
            return;
        }

        private void DataGridViewOffCreaOggetti_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                int id_offerta = SelOffCrea.SelectedItem.GetHashCode();
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    string id = row.Cells[0].Value.ToString();
                    string pezzo = row.Cells[1].Value.ToString();
                    string porig = row.Cells[2].Value.ToString();
                    string pscont = row.Cells[3].Value.ToString();
                    string numpezzi = row.Cells[4].Value.ToString();
                    string descrizione = "";
                    int id_macchina = 0;
                    int id_cliente = 0;
                    string string_macchina = "";
                    string string_pezzo = "";
                    string temp = pezzo.Split('-')[0].Trim();
                    int idogg = 0;
                    if (!String.IsNullOrEmpty(temp))
                    {
                        idogg = Convert.ToInt32(temp);
                    }

                    string commandText = @"SELECT 
												IIF(PR.ID_macchina IS NOT NULL, (CM.Id || ' - ' || CM.modello  || ' (' ||  CM.seriale || ')'), '') AS macchina,
												IIF(PR.ID_macchina IS NOT NULL, CM.Id, 0) AS id,
												IIF(PR.ID_macchina IS NOT NULL, CM.ID_cliente, 0) AS id_cliente,
												REPLACE( printf('%.2f',PR.prezzo), '.', ',')  AS prezzo,
												PR.descrizione AS descrizione
											FROM " + schemadb + @"[pezzi_ricambi] AS PR
											LEFT JOIN " + schemadb + @"[clienti_macchine] AS CM
												ON CM.Id = PR.ID_macchina
											WHERE PR.Id=@idogg LIMIT " + recordsPerPage;


                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@idogg", idogg);

                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                string_macchina = (reader["macchina"] == DBNull.Value) ? "" : Convert.ToString(reader["macchina"]);
                                id_macchina = (reader["id"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["id"]);
                                descrizione = (reader["descrizione"] == DBNull.Value) ? "" : Convert.ToString(reader["descrizione"]);
                                id_cliente = Convert.ToInt32(reader["id_cliente"]);
                                AddOffCreaOggettoPoriRic.Text = Convert.ToString(reader["prezzo"]);
                            }
                            reader.Close();
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante recupero infooggetti offerte. Codice: " + ReturnErorrCode(ex));
                            //ABILITA CAMPI & BOTTONI
                            UpdateFields("OAO", "A", true);
                            UpdateFields("OAO", "E", true);
                        }
                    }

                    int curItem = AddOffCreaOggettoMach.SelectedItem.GetHashCode();

                    AddOffCreaOggettoPori.Text = porig;
                    AddOffCreaOggettoPsco.Text = pscont;

                    AddOffCreaOggettoMach.SelectedIndexChanged -= AddOffCreaOggettoMach_SelectedIndexChanged;
                    Populate_combobox_machine(new ComboBox[] { AddOffCreaOggettoMach }, id_cliente);
                    AddOffCreaOggettoMach.SelectedIndex = AddOffCreaOggettoMach.FindString(string_macchina);
                    AddOffCreaOggettoMach.SelectedIndexChanged += AddOffCreaOggettoMach_SelectedIndexChanged;

                    AddOffCreaOggettoRica.SelectedIndexChanged -= AddOffCreaOggettoRica_SelectedIndexChanged;
                    Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, id_macchina);
                    AddOffCreaOggettoRica.SelectedIndex = FindIndexFromValue(AddOffCreaOggettoRica, idogg);
                    AddOffCreaOggettoRica.SelectedIndexChanged += AddOffCreaOggettoRica_SelectedIndexChanged;

                    commandText = @"SELECT  
										PR.Id,
										PR.nome,
										PR.codice
									FROM " + schemadb + @"[offerte_pezzi] AS OP
									JOIN " + schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id=OP.ID_ricambio
									WHERE OP.id=@idoff LIMIT " + recordsPerPage;


                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {
                        try
                        {

                            cmd.Parameters.AddWithValue("idoff", id);
                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                string_pezzo = String.Format("{0} - {1} ({2})", reader["Id"], reader["nome"], reader["codice"]);
                            }
                            reader.Close();
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante recupero infooggetti offerte. Codice: " + ReturnErorrCode(ex));
                            //ABILITA CAMPI & BOTTONI
                            UpdateFields("OAO", "A", true);
                            UpdateFields("OAO", "E", true);
                        }
                    }

                    AddOffCreaOggettoDesc.Text = descrizione;
                    AddOffCreaOggettoId.Text = id;

                    UpdateFields("OAO", "A", true);
                    UpdateFields("OAO", "E", true);

                    AddOffCreaOggettoMach.Enabled = false;
                    AddOffCreaOggettoRica.Enabled = false;
                    AddOffCreaOggettoPezzoFiltro.Enabled = false;
                    AddOffCreaOggettoPezzi.Text = numpezzi;
                }
            }
        }

        private void BtCancChangesOffOgg_Click(object sender, EventArgs e)
        {
            UpdateFields("OAO", "CA", true);
            UpdateFields("OAO", "E", false);
            UpdateFields("OAO", "A", true);

        }

        private void BtDelChangesOffOgg_Click(object sender, EventArgs e)
        {

            //DISABILITA CAMPI
            UpdateFields("OAO", "E", false);
            UpdateFields("OAO", "A", false);

            string IdOgOfOff = AddOffCreaOggettoId.Text.Trim();
            int idof = Convert.ToInt32(SelOffCrea.SelectedItem.GetHashCode());

            int selClIndex = SelOffCreaCl.SelectedIndex;
            int selOfIndex = SelOffCrea.SelectedIndex;

            string er_list = "";

            if (!int.TryParse(IdOgOfOff, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OAO", "A", true);
                UpdateFields("OAO", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare questo oggetto dall'offerta?", "Eliminare Oggetto dall'offerta", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OAO", "A", true);
                UpdateFields("OAO", "E", true);
                return;
            }


            string commandText = @"DELETE FROM " + schemadb + @"[offerte_pezzi] WHERE Id=@idq LIMIT 1;
                                   UPDATE " + schemadb + @"[offerte_elenco]
                                        SET tot_offerta = ifnull((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[offerte_pezzi] AS OP WHERE OP.ID_offerta=@idof),0)
                                        WHERE Id=@idof LIMIT 1;";


            using (var transaction = connection.BeginTransaction())
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection, transaction))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ);
                    cmd.Parameters.AddWithValue("@idof", idof);

                    cmd.ExecuteNonQuery();

                    transaction.Commit();

                    LoadOfferteCreaTable();

                    UpdateOfferteCrea(0, false);
                    LoadOfferteOggettiCreaTable(idof);

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("OAO", "CA", true);
                    UpdateFields("OAO", "E", false);
                    UpdateFields("OAO", "A", true);

                    ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);
                    SelOffCrea_SelectedIndexChanged(this, System.EventArgs.Empty);

                    if (selClIndex > 0)
                    {
                        SelOffCreaCl.SelectedIndex = selClIndex;
                    }
                    SelOffCrea.SelectedIndex = selOfIndex;

                    MessageBox.Show("Oggetto rimosso.");
                }
                catch (SQLiteException ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Errore durante eliminazione dell'ogetto. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("OAO", "A", true);
                    UpdateFields("OAO", "E", true);
                }
            }
            return;
        }

        private void BtSaveChangesOffOgg_Click(object sender, EventArgs e)
        {
            UpdateFields("OAO", "A", false);

            string prezzoOr = AddOffCreaOggettoPori.Text.Trim();
            string prezzoSc = AddOffCreaOggettoPsco.Text.Trim();
            string qta = AddOffCreaOggettoPezzi.Text.Trim();

            int idof = Convert.ToInt32(SelOffCrea.SelectedItem.GetHashCode());
            int idOggToOff = Convert.ToInt32(AddOffCreaOggettoId.Text.Trim());

            string er_list = "";

            if (!Decimal.TryParse(prezzoOr, style, culture, out decimal prezzoOrV))
            {
                er_list += "Prezzo nell'offerta non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzoOrV < 0)
                {
                    er_list += "Il prezzo deve essere positivo" + Environment.NewLine;
                }
            }

            if (!Decimal.TryParse(prezzoSc, style, culture, out decimal prezzoScV))
            {
                er_list += "Prezzo Finale non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzoScV < 0)
                {
                    er_list += "Il prezzo deve essere positivo" + Environment.NewLine;
                }
            }

            if (!int.TryParse(qta, out int qtaV))
            {
                er_list += "Quantità non aalido o vuota" + Environment.NewLine;
            }
            else
            {
                if (qtaV < 1)
                    er_list += "La quanità deve essere positiva, intera e maggiore di 0" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                UpdateFields("OAO", "A", true);
                BtAddRicToOff.Enabled = false;
                return;
            }


            string commandText = @" BEGIN TRANSACTION;
                                    UPDATE OR ROLLBACK " + schemadb + @"[offerte_pezzi] 
                                        SET prezzo_unitario_originale=@por, prezzo_unitario_sconto=@pos,pezzi=@pezzi 
                                        WHERE Id=@idOggToOff LIMIT 1;
                                    UPDATE OR ROLLBACK " + schemadb + @"[offerte_elenco] 
									    SET tot_offerta = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[offerte_pezzi] AS OP WHERE OP.ID_offerta=@idof),0)
									    WHERE Id = @idof LIMIT 1;
                                    COMMIT;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@por", prezzoOrV);
                    cmd.Parameters.AddWithValue("@pos", prezzoScV);
                    cmd.Parameters.AddWithValue("@pezzi", qtaV);
                    cmd.Parameters.AddWithValue("@idOggToOff", idOggToOff);
                    cmd.Parameters.AddWithValue("@idof", idof);
                    cmd.ExecuteNonQuery();

                    LoadOfferteCreaTable();

                    LoadOfferteOggettiCreaTable(idof);
                    ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);

                    UpdateFields("OAO", "CA", false);
                    UpdateFields("OAO", "A", false);

                    BtCancChangesOffOgg_Click(this, EventArgs.Empty);

                    AddOffCreaOggettoRica.Enabled = true;

                    MessageBox.Show("Modfiche salvate");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("OAO", "A", true);
                    AddOffCreaOggettoRica.Enabled = false;
                }
            }
            return;
        }

        private void AddOffCreaOggettoPezzoFiltro_TextChanged(object sender, EventArgs e)
        {
            TimerAddOffCreaOggettoPezzoFiltro.Stop();
            TimerAddOffCreaOggettoPezzoFiltro.Start();
        }

        private void TimerAddOffCreaOggettoPezzoFiltro_Tick(object sender, EventArgs e)
        {
            TimerAddOffCreaOggettoPezzoFiltro.Stop();
            string newAddOffCreaOggettoPezzoFiltro_Text = AddOffCreaOggettoPezzoFiltro.Text.Trim();

            if (newAddOffCreaOggettoPezzoFiltro_Text != AddOffCreaOggettoPezzoFiltro_Text && newAddOffCreaOggettoPezzoFiltro_Text != AddOffCreaOggettoPezzoFiltro.PlaceholderText)
            {
                AddOffCreaOggettoPezzoFiltro_Text = newAddOffCreaOggettoPezzoFiltro_Text;

                int curItem = FieldOrdOggMach.SelectedItem.GetHashCode();
                Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, curItem > 0 ? curItem : 0, true);
                AddOffCreaOggettoPori.Text = "";
                AddOffCreaOggettoPoriRic.Text = "";
                AddOffCreaOggettoPsco.Text = "";
                AddOffCreaOggettoDesc.Text = "";
                AddOffCreaOggettoPezzi.Text = "";
            }
        }

        private void TimerOffCreaFiltro_Tick(object sender, EventArgs e)
        {
            TimerOffCreaFiltro.Stop();
            UpdateOfferteCrea(offerteCreaCurPage);
        }

        private void OffCreaFiltroCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            TimerOffCreaFiltro.Stop();
            TimerOffCreaFiltro.Start();
        }

        private void OffCreaFiltroStato_SelectedIndexChanged(object sender, EventArgs e)
        {
            TimerOffCreaFiltro.Stop();
            TimerOffCreaFiltro.Start();
        }

        //CREA ORDINI

        private void ComboBoxOrdCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboBoxOrdCliente.DataSource == null)
            {
                return;
            }

            int curItemValue = ComboBoxOrdCliente.SelectedItem.GetHashCode();
            int index = ComboBoxOrdCliente.SelectedIndex;


            if (curItemValue > 0)
            {
                Populate_combobox_ordini_crea_offerta(ComboBoxOrdOfferta, curItemValue);
                Populate_combobox_pref(ComboBoxOrdContatto, curItemValue);

                ComboBoxOrdOfferta.Enabled = true;
                CheckBoxOrdOffertaNonPresente.Enabled = true;
                return;
            }
            else
            {
                ComboBoxOrdOfferta.Enabled = false;
                CheckBoxOrdOffertaNonPresente.Enabled = false;

                Populate_combobox_dummy(ComboBoxOrdOfferta);
                Populate_combobox_dummy(ComboBoxOrdContatto);

                FieldOrdStato.SelectedIndex = 0;

                CheckBoxOrdOffertaNonPresente.Enabled = false;
                CheckBoxOrdOffertaNonPresente.CheckedChanged -= CheckBoxOrdOffertaNonPresente_CheckedChanged;
                CheckBoxOrdOffertaNonPresente.Checked = false;
                CheckBoxOrdOffertaNonPresente.CheckedChanged += CheckBoxOrdOffertaNonPresente_CheckedChanged;

                ComboBoxOrdOfferta.Enabled = false;

                UpdateFields("OCR", "CA", false);
            }
            return;
        }

        private void ComboBoxOrdOfferta_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboBoxOrdOfferta.DataSource == null)
            {
                return;
            }

            int? curItemValue = null;

            if (ComboBoxOrdOfferta.SelectedItem == null)
                curItemValue = -1;
            else
                curItemValue = ComboBoxOrdOfferta.SelectedItem.GetHashCode();

            curItemValue = (curItemValue == null) ? 0 : curItemValue;

            if (CheckBoxOrdOffertaNonPresente.Checked)
            {
                UpdateFields("OCR", "A", true);
            }
            else if (curItemValue > 0)
            {
                CheckBoxOrdOffertaNonPresente.Enabled = false;

                string commandText = @"SELECT 
										codice_offerta,
										data_offerta,
										REPLACE( printf('%.2f',tot_offerta), '.', ',') AS tot_offerta,
										IIF(gestione_spedizione IS NULL, '', REPLACE( printf('%.2f',costo_spedizione), '.', ',')) AS costosp,
										IIF(gestione_spedizione IS NULL, -1, gestione_spedizione) AS gestsp
									   FROM " + schemadb + @"[offerte_elenco]
									   WHERE Id=@idoff
									   ORDER BY Id DESC";


                using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@idoff", curItemValue);
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            FieldOrdTot.Text = reader["tot_offerta"].ToString();
                            FieldOrdPrezF.Text = reader["tot_offerta"].ToString();
                            FieldOrdSped.Text = "0";
                            FieldOrdSpedGestione.SelectedIndex = Convert.ToInt32(reader["gestsp"]) + 1;
                            FieldOrdSped.Text = reader["costosp"].ToString();
                        }
                        reader.Close();
                        UpdateFields("OCR", "A", true);
                    }
                    catch (SQLiteException ex)
                    {
                        MessageBox.Show("Errore durante selezione Offerta. Codice: " + ReturnErorrCode(ex));
                        return;
                    }
                }
            }
            else
            {
                UpdateFields("OCR", "CA", false, false);
                UpdateFields("OCR", "A", false);
                Populate_combobox_dummy(ComboBoxOrdOfferta);
                ComboBoxOrdOfferta.Enabled = false;
            }
            return;
        }

        private void FieldOrdSconto_Leave(object sender, System.EventArgs e)
        {
            string sconto = FieldOrdSconto.Text.Trim();
            string prezzoIS = FieldOrdTot.Text.Trim();
            decimal prezzoI;
            ValidationResult scontoV = new ValidationResult();

            if (!string.IsNullOrEmpty(prezzoIS))
                prezzoI = Convert.ToDecimal(prezzoIS);
            else
                prezzoI = 0;
            string er_list = "";

            if (string.IsNullOrEmpty(sconto))
            {
                FieldOrdSconto.Text = "0";
                return;
            }

            scontoV = ValidateSconto(sconto);
            er_list += scontoV.Error;

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                return;
            }
            FieldOrdPrezF.Text = (prezzoI * (1 - scontoV.DecimalValue / 100)).Value.ToString("N2", nfi).Replace(".", "");
        }

        private void ApplySconto(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FieldOrdSconto_Leave(this, EventArgs.Empty);
            }
        }

        private void FieldOrdPrezF_Leave(object sender, System.EventArgs e)
        {
            string prezzoF = FieldOrdPrezF.Text.Trim();
            decimal prezzoI = (String.IsNullOrEmpty(FieldOrdTot.Text.Trim())) ? 0 : Convert.ToDecimal(FieldOrdTot.Text.Trim());
            string er_list = "";

            if (string.IsNullOrEmpty(prezzoF))
            {
                FieldOrdSconto.Text = FieldOrdTot.Text;
                return;
            }

            if (!Decimal.TryParse(prezzoF, style, culture, out decimal prezzoFV))
            {
                er_list += "Prezzo finale non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzoFV < 0)
                {
                    er_list += "Il prezzo finale deve essere positivo" + Environment.NewLine;
                }
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("OCR", "A", true);
                return;
            }

            if (prezzoI != 0)
                FieldOrdSconto.Text = (-(prezzoFV - prezzoI) / prezzoI * 100).ToString("N2", nfi);
            return;
        }

        private void CalcSconto(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FieldOrdPrezF_Leave(this, EventArgs.Empty);
            }
            return;
        }

        private void LoadOrdiniTable(int page = 1)
        {
            DataGridView[] data_grid = new DataGridView[] { DataGridViewOrd };

            if (DataGridViewOrdStato.DataSource == null || DataGridViewFilterCliente.DataSource == null)
            {
                return;
            }

            int stato = (DataGridViewOrdStato.DataSource == null) ? -1 : DataGridViewOrdStato.SelectedItem.GetHashCode();
            int idcl = Convert.ToInt32(DataGridViewFilterCliente.SelectedValue.GetHashCode());
            string numOrdineFilter = DataGridViewFilterNumOrdine.Text.Trim();

            string addInfo = "";
            List<string> paramsQuery = new List<string>();

            if (stato >= 0)
                paramsQuery.Add("OE.stato = @stato");

            if (idcl > 0)
                paramsQuery.Add("CE.Id = @idcl ");

            if (Regex.IsMatch(numOrdineFilter, @"^\d+$"))
                paramsQuery.Add(" OE.codice_ordine LIKE @numOrdineFilter");

            if (paramsQuery.Count > 0)
                addInfo = " WHERE " + String.Join(" AND ", paramsQuery);

            string commandText = @"SELECT COUNT(OE.Id) 
                                    FROM " + schemadb + @"[ordini_elenco] AS OE
                                    LEFT JOIN " + schemadb + @"[offerte_elenco] OFE 
                                        ON OFE.Id = OE.ID_offerta
                                    LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
                                        ON CE.Id = OFE.ID_cliente "
                                    + addInfo;
            int count = 1;


            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmdCount.Parameters.AddWithValue("@stato", stato);
                    cmdCount.Parameters.AddWithValue("@idcl", idcl);
                    cmdCount.Parameters.AddWithValue("@numOrdineFilter", "%" + numOrdineFilter + "%");

                    count = Convert.ToInt32(cmdCount.ExecuteScalar());
                    count = (count - 1) / recordsPerPage + 1;
                    MaxPageOrd.Text = Convert.ToString((count > 1) ? count : 1);
                    if (count > 1)
                    {
                        OrdNxtPage.Enabled = true;
                        OrdPrvPage.Enabled = true;
                        OrdCurPage.Enabled = true;
                    }
                    else
                    {
                        OrdNxtPage.Enabled = false;
                        OrdPrvPage.Enabled = false;
                        OrdCurPage.Enabled = false;
                    }
                    page = (page > count) ? count : page;
                    OrdiniCurPage = page;
                    OrdCurPage.Text = Convert.ToString(page);
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante verifica records in elenco ordini. Codice: " + ReturnErorrCode(ex));
                    return;
                }
            }

            commandText = @"SELECT  
									OE.Id AS ID,
									OE.codice_ordine AS codOrd,
									OFE.Id || ' - ' || OFE.codice_offerta AS IDoff,
                                    CE.Id || ' - ' || CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')' AS Cliente,
                                    IIF(OFE.ID_riferimento>0 OR OE.ID_riferimento IS NOT NULL,   (CR.Id || ' - ' || CR.nome),'') AS Pref,
									strftime('%d/%m/%Y',OE.data_ordine) AS datOr,
									strftime('%d/%m/%Y',OE.data_ETA) AS datEta,
									REPLACE( printf('%.2f',OE.totale_ordine),'.',',')  AS totord,
                                    REPLACE(  (printf('%.2f',OE.prezzo_finale ) || ' (' || printf('%.2f',OE.sconto ) || '%)'),'.',',')  AS prezfinale,
									IIF(OE.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OE.costo_spedizione ),'.',','), NULL) AS csped,
									CASE OE.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END AS spedg,

									
									CASE OE.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END AS Stato

								   FROM " + schemadb + @"[ordini_elenco] AS OE 
								   LEFT JOIN " + schemadb + @"[offerte_elenco] OFE 
										ON OFE.Id = IIF(OE.ID_offerta IS NOT NULL ,OE.ID_offerta,0)
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = IIF(OE.ID_offerta IS NOT NULL , OFE.ID_cliente, OE.ID_cliente)  
								   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR 
										ON CR.Id = IIF(OE.ID_offerta IS NOT NULL , OFE.ID_riferimento,  OE.ID_riferimento)  "
                                    + addInfo + @" 
								   ORDER BY OE.Id DESC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {
                    DataTable ds = new DataTable();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@stato", stato);
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);
                    cmd.SelectCommand.Parameters.AddWithValue("@numOrdineFilter", "%" + numOrdineFilter + "%");

                    cmd.Fill(ds);

                    for (int i = 0; i < data_grid.Length; i++)
                    {
                        data_grid[i].DataSource = ds;

                        Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "codOrd", "Codice Ordine" },
                        { "IDoff", "ID - #Offerta" },
                        { "Cliente", "Cliente" },
                        { "Pref", "Contatto" },
                        { "datOr", "Data Ordine" },
                        { "datEta", "Data Arrivo" },
                        { "totord", "Tot. Ordine"+Environment.NewLine+"(Excl. Spedizioni)" },
                        { "csped", "Costo Spedizione"+Environment.NewLine+"(Excl. Sconti)" },
                        { "spedg", "Gestione Costo Spedizione" },
                        { "prezfinale", "Prezzo Finale" },
                        { "Stato", "Stato" }
                    };
                        int colCount = data_grid[i].ColumnCount;
                        for (int j = 0; j < colCount; j++)
                        {
                            if (columnNames.ContainsKey(data_grid[i].Columns[j].HeaderText))
                                data_grid[i].Columns[j].HeaderText = columnNames[data_grid[i].Columns[j].HeaderText];

                            data_grid[i].Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                            int colw = data_grid[i].Columns[j].Width;
                            data_grid[i].Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                            data_grid[i].Columns[j].Width = colw;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella Ordini. Codice: " + ReturnErorrCode(ex));
                    return;
                }
            }
            return;
        }

        private void DataGridViewOrdStato_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (DataGridViewOrd.DataSource != null)
                LoadOrdiniTable(OrdiniCurPage);
            return;
        }

        private void CheckBoxCopiaOffertainOrdine_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox controllo = (CheckBox)sender;

            if (controllo.Checked)
            {
                labelOffoOrd.Visible = false;
                LabelPtotFOff.Visible = false;
            }
            else
            {
                labelOffoOrd.Visible = true;
                LabelPtotFOff.Visible = true;
            }
            return;
        }

        private void BtCreaOrdine_Click(object sender, EventArgs e)
        {
            UpdateFields("OCR", "A", false);

            string commandText = "";

            int? id_offerta = (CheckBoxOrdOffertaNonPresente.Checked == false) ? (int?)ComboBoxOrdOfferta.SelectedItem.GetHashCode() : null;

            int? id_cl = (CheckBoxOrdOffertaNonPresente.Checked == true) ? (int?)ComboBoxOrdCliente.SelectedItem.GetHashCode() : null;
            int? id_contatto = (CheckBoxOrdOffertaNonPresente.Checked == true && ComboBoxOrdContatto.SelectedItem.GetHashCode() > 0) ? (int?)ComboBoxOrdContatto.SelectedItem.GetHashCode() : null;


            string n_ordine = FieldOrdNOrdine.Text.Trim();

            string dataOrdString = FieldOrdData.Text.Trim();
            string dataETAString = FieldOrdETA.Text.Trim();

            string sconto = FieldOrdSconto.Text.Trim();

            string spedizioni = FieldOrdSped.Text.Trim();
            int gestSP = FieldOrdSpedGestione.SelectedItem.GetHashCode();

            string prezzo_finale = FieldOrdPrezF.Text.Trim();
            string tot_ordine = FieldOrdTot.Text.Trim();

            int stato_ordine = FieldOrdStato.SelectedItem.GetHashCode();
            stato_ordine = (stato_ordine < 0) ? 0 : stato_ordine;

            ValidationResult answer;
            ValidationResult prezzoSpedizione = new ValidationResult();
            ValidationResult dataOrdValue = new ValidationResult();
            ValidationResult dataETAOrdValue = new ValidationResult();
            ValidationResult tot_ordineV = new ValidationResult();
            ValidationResult prezzo_finaleV = new ValidationResult();
            ValidationResult scontoV = new ValidationResult();

            string er_list = "";

            if (CheckBoxOrdOffertaNonPresente.Checked)
            {
                answer = ValidateCliente((int)id_cl);
                if (!answer.Success)
                {
                    MessageBox.Show(answer.Error);
                    return;
                }
                er_list += answer.Error;
            }

            if (string.IsNullOrEmpty(n_ordine) || !Regex.IsMatch(n_ordine, @"^\d+$"))
            {
                er_list += "Numero Ordine non valido o vuoto" + Environment.NewLine;
            }

            dataOrdValue = ValidateDate(dataOrdString);
            er_list += dataOrdValue.Error;

            dataETAOrdValue = ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            if (DateTime.Compare(dataOrdValue.DateValue, dataETAOrdValue.DateValue) > 0)
            {
                er_list += "Data di Arrivo(ETA) antecedente a quella di creazione dell'ordine" + Environment.NewLine;
            }

            if (!string.IsNullOrEmpty(spedizioni))
            {
                if (!string.IsNullOrEmpty(spedizioni))
                {
                    prezzoSpedizione = ValidateSpedizione(spedizioni, gestSP);
                    er_list += prezzoSpedizione.Error;
                }
            }

            if (CheckBoxCopiaOffertainOrdine.Checked == false)
            {
                tot_ordineV.DecimalValue = 0;
                prezzo_finaleV.DecimalValue = 0;
            }
            else
            {
                tot_ordineV = ValidatePrezzo(tot_ordine);
                er_list += tot_ordineV.Error;

                prezzo_finaleV = ValidatePrezzo(prezzo_finale);
                er_list += prezzo_finaleV.Error;

                prezzo_finaleV = ValidateSconto(prezzo_finale);
                er_list += prezzo_finaleV.Error;
            }

            scontoV = ValidateSconto(sconto);
            er_list += scontoV.Error;


            if (CheckBoxOrdOffertaNonPresente.Checked == false)
            {
                commandText = "SELECT COUNT(*) FROM " + schemadb + @"[offerte_elenco] WHERE ([Id] = @id_offerta) LIMIT 1;";
                int UserExist = 0;

                using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@id_offerta", id_offerta);

                        UserExist = Convert.ToInt32(cmd.ExecuteScalar());
                        if (UserExist < 1)
                        {
                            er_list += "Offerta non valida" + Environment.NewLine;
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        MessageBox.Show("Errore durante verifica ID Offerta. Codice: " + ReturnErorrCode(ex));
                        return;
                    }
                }
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("OCR", "A", true);
                return;
            }

            commandText = @"INSERT INTO " + schemadb + @"[ordini_elenco]
                            (codice_ordine, ID_offerta, ID_cliente, ID_riferimento, data_ordine, data_ETA, totale_ordine,sconto,prezzo_finale,stato,costo_spedizione,gestione_spedizione)
						   VALUES (@codo, @idoof, @idlc, @idcont, @dataord, @dataeta, @totord, @sconto, @prezzoF, @stato, @cossp, @gestsp);
						   SELECT last_insert_rowid();";

            int lastinsertedid = 0;

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@codo", n_ordine);
                    cmd.Parameters.AddWithValue("@idoof", id_offerta);
                    cmd.Parameters.AddWithValue("@idlc", id_cl);
                    cmd.Parameters.AddWithValue("@idcont", id_contatto);
                    cmd.Parameters.AddWithValue("@dataord", dataOrdValue.DateValue);
                    cmd.Parameters.AddWithValue("@dataeta", dataETAOrdValue.DateValue);
                    cmd.Parameters.AddWithValue("@totord", tot_ordineV.DecimalValue);
                    cmd.Parameters.AddWithValue("@sconto", scontoV.DecimalValue);
                    cmd.Parameters.AddWithValue("@prezzoF", prezzo_finaleV.DecimalValue);
                    cmd.Parameters.AddWithValue("@stato", stato_ordine);
                    if (prezzoSpedizione.DecimalValue.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@cossp", prezzoSpedizione.DecimalValue);
                        cmd.Parameters.AddWithValue("@gestsp", gestSP);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@cossp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@gestsp", DBNull.Value);
                    }

                    lastinsertedid = Convert.ToInt32(cmd.ExecuteScalar());

                    if (CheckBoxOrdOffertaNonPresente.Checked == false)
                    {
                        commandText = "UPDATE " + schemadb + @"[offerte_elenco] SET trasformato_ordine=1 WHERE Id=@idoff LIMIT 1;";
                        using (SQLiteCommand cmd2 = new SQLiteCommand(commandText, connection))
                        {
                            try
                            {
                                cmd2.CommandText = commandText;
                                cmd2.Parameters.AddWithValue("@idoff", id_offerta);
                                cmd2.ExecuteScalar();
                            }
                            catch (SQLiteException ex)
                            {
                                MessageBox.Show("Errore durante aggiornamento offerta(convertito ordine update). Codice: " + ReturnErorrCode(ex));
                            }
                        }

                        if (CheckBoxCopiaOffertainOrdine.Checked)
                        {
                            if (lastinsertedid > 0)
                            {

                                commandText = @"SELECT * FROM " + schemadb + @"[offerte_pezzi] WHERE ID_offerta=@idof;";
                                using (SQLiteCommand cmd2 = new SQLiteCommand(commandText, connection))

                                {
                                    try
                                    {
                                        cmd2.CommandText = commandText;
                                        cmd2.Parameters.AddWithValue("@idof", id_offerta);


                                        SQLiteDataReader reader = cmd2.ExecuteReader();
                                        string query;
                                        bool error_copi = false;
                                        while (reader.Read())
                                        {
                                            query = @" BEGIN TRANSACTION;
                                                    INSERT OR ROLLBACK INTO " + schemadb + @"[ordine_pezzi](ID_ordine,ID_ricambio,prezzo_unitario_originale,prezzo_unitario_sconto,pezzi,ETA) 
													    VALUES (@idord,@idogg,@prezor,@prezsco,@qta,@dataeta);
                                                    UPDATE OR ROLLBACK " + schemadb + @"[offerte_pezzi] SET aggiunto=1 WHERE Id=@idoffogg LIMIT 1;
                                                    COMMIT;
                                                    ";

                                            using (SQLiteCommand cmd3 = new SQLiteCommand(query, connection))
                                            {
                                                try
                                                {
                                                    cmd3.CommandText = query;
                                                    cmd3.Parameters.AddWithValue("@idord", lastinsertedid);
                                                    cmd3.Parameters.AddWithValue("@idogg", reader["ID_ricambio"]);
                                                    cmd3.Parameters.AddWithValue("@prezor", reader["prezzo_unitario_originale"]);
                                                    cmd3.Parameters.AddWithValue("@prezsco", reader["prezzo_unitario_sconto"]);
                                                    cmd3.Parameters.AddWithValue("@qta", reader["pezzi"]);
                                                    cmd3.Parameters.AddWithValue("@dataeta", dataETAOrdValue);
                                                    cmd3.Parameters.AddWithValue("@idoffogg", reader["Id"]);

                                                    cmd3.ExecuteNonQuery();
                                                }
                                                catch (SQLiteException ex)
                                                {
                                                    MessageBox.Show("Errore durante copia pezzi offerta in ordine(pt2). COntrollare manualmente l'ordine. Codice: " + ReturnErorrCode(ex));
                                                    error_copi = true;
                                                }
                                            }
                                        }
                                        reader.Close();
                                        if (error_copi == false)
                                        {
                                            MessageBox.Show("Oggetti copiati nell'ordine");
                                        }
                                    }
                                    catch (SQLiteException ex)
                                    {
                                        MessageBox.Show("Errore durante copia pezzi offerta in ordine(pt1). Codice: " + ReturnErorrCode(ex));
                                    }
                                }
                            }
                        }
                    }

                    UpdateOrdini(OrdiniCurPage);
                    UpdateFields("OCR", "CA", true);
                    UpdateFields("OCR", "A", false);
                    UpdateFields("VS", "CA", true);
                    UpdateFields("VS", "E", false);
                    BtChiudiOrd_Click(this, System.EventArgs.Empty);

                    ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);
                    UpdateOfferteCrea(offerteCreaCurPage);

                    CheckBoxOrdOffertaNonPresente.Checked = false;

                    ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    MessageBox.Show("Ordine Creato.");

                    DateTime today = DateTime.Today;
                    FieldOrdData.Text = today.ToString("dd/MM/yyyy");
                    FieldOrdETA.Text = today.ToString("dd/MM/yyyy");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("OCR", "A", true);
                }
            }
            return;
        }

        private void ComboSelOrd_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboSelOrd.DataSource == null || ComboSelOrdCl.DataSource == null)
            {
                return;
            }

            if ((int)ComboSelOrdCl.SelectedItem.GetHashCode() < 1)
            {
                ComboSelOrd.Enabled = false;
            }

            int id_ordine = ComboSelOrd.SelectedItem.GetHashCode();

            if (id_ordine > 0)
            {
                UpdateOrdiniOggettiOfferta(id_ordine);
                UpdateOrdiniOggetti(id_ordine);
                CheckBoxOrdOggCheckAddNotOffer.Enabled = true;

                Populate_combobox_machine(new ComboBox[] { FieldOrdOggMach }, Convert.ToInt32(ComboSelOrdCl.Text.Split('-')[0]));

                CheckBoxOrdOggCheckAddNotOffer.Enabled = true;
                return;
            }
            else
            {
                ClearDataGridView(DataGridViewOrdOffOgg);
                ClearDataGridView(DataGridViewOrdOgg);

                CheckBoxOrdOggCheckAddNotOffer.Checked = false;

                FieldOrdOggMach.Text = "";
                FieldOrdOggId.Text = "";
                FieldOrdOggPezzo.Text = "";
                FieldOrdOggPOr.Text = "";
                FieldOrdOggPsc.Text = "";
                FieldOrdOggQta.Text = "";
                FieldOrdOggETA.Text = "";
                FieldOrdOggDesc.Text = "";

                CheckBoxOrdOggCheckAddNotOffer.Enabled = false;

                return;
            }
        }

        private void UpdateOrdiniOggettiOfferta(int id_ordine)
        {

            string commandText = @"SELECT 

										OFE.Id AS id,
                                        
										IFNULL(PR.nome,'Rimosso da Database') AS nome_pezzo,
										PR.codice AS code_pezzo,

										REPLACE( printf('%.2f',OFE.prezzo_unitario_originale ),'.',',')  AS puo,
										REPLACE( printf('%.2f',OFE.prezzo_unitario_sconto ),'.',',')  AS pus,

										OFE.pezzi AS qta,
										PR.descrizione AS descrizione 
									   
										FROM " + schemadb + @"[ordini_elenco] AS OE 

										LEFT JOIN " + schemadb + @"[offerte_pezzi] AS OFE 
											ON OFE.ID_offerta=OE.ID_offerta 

										LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR 
											ON PR.Id=OFE.ID_ricambio 

									   WHERE OE.id=@idofferta AND OFE.aggiunto=0;";


            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {

                    cmd.SelectCommand.Parameters.AddWithValue("@idofferta", id_ordine);
                    DataTable ds = new DataTable();
                    cmd.Fill(ds);
                    DataGridViewOrdOffOgg.DataSource = ds;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "id", "ID" },
                        { "idpez,", "ID Ricambio" },
                        { "puo", "Prezzo Originale" },
                        { "pus", "Prezzo Finale" },
                        { "qta", "Quantità" },
                        { "nome_pezzo", "Nome Pezzo" },
                        { "code_pezzo", "Codice Pezzo" },
                        { "descrizione", "Descrizione" }
                    };
                    int colCount = DataGridViewOrdOffOgg.ColumnCount;
                    for (int j = 0; j < colCount; j++)
                    {
                        if (columnNames.ContainsKey(DataGridViewOrdOffOgg.Columns[j].HeaderText))
                            DataGridViewOrdOffOgg.Columns[j].HeaderText = columnNames[DataGridViewOrdOffOgg.Columns[j].HeaderText];

                        DataGridViewOrdOffOgg.Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;


                        int colw = DataGridViewOrdOffOgg.Columns[j].Width;
                        DataGridViewOrdOffOgg.Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        DataGridViewOrdOffOgg.Columns[j].Width = colw;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante riempimento oggetti offerte. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }
            return;

        }

        private void UpdateOrdiniOggetti(int id_ordine)
        {

            string commandText = @"SELECT 
										OP.Id AS id,
										IFNULL(PR.nome,'Rimosso da Database') AS nome_pezzo,
										PR.codice AS code_pezzo,
										REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS puo,
										REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pus,
										OP.pezzi AS qta,
										PR.descrizione AS descrizione,
										op.ETA as ETA
									   FROM " + schemadb + @"[ordine_pezzi] AS OP
										LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
											ON PR.Id=OP.ID_ricambio
									   WHERE OP.ID_ordine=@idofferta;";


            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {

                    cmd.SelectCommand.Parameters.AddWithValue("@idofferta", id_ordine);
                    DataTable ds = new DataTable();
                    cmd.Fill(ds);
                    DataGridViewOrdOgg.DataSource = ds;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "id", "ID" },
                        { "idpez", "ID Ricambio" },
                        { "puo", "Prezzo Originale" },
                        { "pus", "Prezzo Finale" },
                        { "qta", "Quantità" },
                        { "nome_pezzo", "Nome Pezzo" },
                        { "code_pezzo", "Codice Pezzo" },
                        { "descrizione", "Descrizione" },
                        { "ETA", "Data Arrivo" }
                    };
                    int colCount = DataGridViewOrdOgg.ColumnCount;
                    for (int j = 0; j < colCount; j++)
                    {
                        if (columnNames.ContainsKey(DataGridViewOrdOgg.Columns[j].HeaderText))
                            DataGridViewOrdOgg.Columns[j].HeaderText = columnNames[DataGridViewOrdOgg.Columns[j].HeaderText];

                        DataGridViewOrdOgg.Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                        int colw = DataGridViewOrdOgg.Columns[j].Width;
                        DataGridViewOrdOgg.Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        DataGridViewOrdOgg.Columns[j].Width = colw;
                    }


                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante oggetti ordini. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }
        }

        private void DataGridViewOrd_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }

            if (dgv.SelectedRows.Count == 1)
            {
                CheckBoxOrdOffertaNonPresente.Checked = false;
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {

                    string id = row.Cells[0].Value.ToString();
                    string codOrd = row.Cells[1].Value.ToString();
                    string offerta = Convert.ToString(row.Cells[2].Value.ToString().Trim());
                    string cliente = row.Cells[3].Value.ToString();
                    string contatto = row.Cells[4].Value.ToString();
                    string datOrd = row.Cells[5].Value.ToString();
                    string datETA = row.Cells[6].Value.ToString();
                    string totOrd = row.Cells[7].Value.ToString();
                    string[] subs = row.Cells[8].Value.ToString().Split(' ');

                    string spedizione = row.Cells[9].Value.ToString();
                    string gestSp = row.Cells[10].Value.ToString();
                    string stato = row.Cells[11].Value.ToString();

                    string pfinale = subs[0].Trim();
                    string sconto = Regex.Replace(subs[1], @"[^,.\d]", "").Trim();

                    ComboBoxOrdCliente.SelectedIndex = ComboBoxOrdCliente.FindString(cliente);

                    ComboBoxOrdContatto.SelectedIndex = ComboBoxOrdContatto.FindString(contatto);

                    string ID_offerta_str = offerta.Split('-')[0].Trim();
                    if (int.TryParse(ID_offerta_str, out int ID_offerta))
                    {
                        Populate_combobox_ordini_crea_offerta(ComboBoxOrdOfferta, ComboBoxOrdCliente.SelectedItem.GetHashCode(), false, ID_offerta);
                    }

                    ComboBoxOrdOfferta.SelectedIndex = ComboBoxOrdOfferta.FindString(offerta);


                    FieldOrdId.Text = id;
                    FieldOrdNOrdine.Text = codOrd;
                    FieldOrdData.Text = datOrd;
                    FieldOrdETA.Text = datETA;
                    FieldOrdTot.Text = totOrd;
                    FieldOrdSconto.Text = sconto;
                    FieldOrdPrezF.Text = pfinale;
                    FieldOrdSped.Text = spedizione;

                    FieldOrdSpedGestione.SelectedIndex = FieldOrdSpedGestione.FindString(gestSp);
                    FieldOrdStato.SelectedIndex = FieldOrdStato.FindString(stato);

                    UpdateFields("OCR", "A", true);

                    BtCreaOrdine.Enabled = false;
                    CheckBoxCopiaOffertainOrdine.Enabled = false;
                    BtSaveModOrd.Enabled = true;
                    BtDelOrd.Enabled = true;
                    BtChiudiOrd.Enabled = true;

                    ComboBoxOrdCliente.Enabled = false;
                    ComboBoxOrdOfferta.Enabled = false;

                    CheckBoxOrdOffertaNonPresente.CheckedChanged -= CheckBoxOrdOffertaNonPresente_CheckedChanged;
                    if (String.IsNullOrEmpty(offerta))
                    {
                        CheckBoxOrdOffertaNonPresente.Enabled = false;
                        CheckBoxOrdOffertaNonPresente.Checked = true;
                        ComboBoxOrdContatto.Enabled = true;
                    }
                    else
                    {
                        CheckBoxOrdOffertaNonPresente.Enabled = false;
                        CheckBoxOrdOffertaNonPresente.Checked = false;
                        ComboBoxOrdContatto.Enabled = false;
                    }
                    CheckBoxOrdOffertaNonPresente.CheckedChanged += CheckBoxOrdOffertaNonPresente_CheckedChanged;
                }
            }
        }

        private void BtChiudiOrd_Click(object sender, EventArgs e)
        {
            UpdateFields("OCR", "CA", true);
            UpdateFields("OCR", "E", false);
            UpdateFields("OCR", "A", false);

            BtCreaOrdine.Enabled = false;
            CheckBoxCopiaOffertainOrdine.Enabled = false;
            BtSaveModOrd.Enabled = false;
            BtDelOrd.Enabled = false;
            BtChiudiOrd.Enabled = false;

            ComboBoxOrdCliente.Enabled = true;
            ComboBoxOrdContatto.Enabled = false;

            CheckBoxOrdOffertaNonPresente.Checked = false;
        }

        private void DataGridViewOrdOffOgg_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int idOrdine = ComboSelOrd.SelectedItem.GetHashCode();

                    string idpez = row.Cells[0].Value.ToString();
                    string nome = row.Cells[1].Value.ToString();
                    string codice = row.Cells[2].Value.ToString();
                    string puo = row.Cells[3].Value.ToString();
                    string pus = row.Cells[4].Value.ToString();
                    string qta = row.Cells[5].Value.ToString();
                    string desc = row.Cells[6].Value.ToString();
                    string ETA = "";
                    string mach = "";
                    int idricambio = 0;

                    int index = 0;

                    string commandText = @"SELECT 
											OP.data_ETA AS ETA,
											IIF(PR.ID_macchina IS NOT NULL, CM.Id  , 0) AS ID,
											IIF(PR.ID_macchina IS NOT NULL,   (CM.Id || ' - ' || CM.modello  || ' (' ||  CM.seriale || ')'), '') AS macchina,
                                            PR.Id as pezzo,
											PR.Id AS ID_ricambio

									   FROM " + schemadb + @"[ordini_elenco] AS OP, " + schemadb + @"[offerte_pezzi] AS OFP

									   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id = OFP.ID_ricambio
									   LEFT JOIN " + schemadb + @"[clienti_macchine] AS CM
										ON CM.Id=PR.ID_macchina

									   WHERE OP.id=@idOrdine AND OFP.Id=@idpez LIMIT " + recordsPerPage;


                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@idOrdine", idOrdine);
                            cmd.Parameters.AddWithValue("@idpez", idpez);

                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                ETA = Convert.ToString(reader["ETA"]);
                                mach = (reader["macchina"] == DBNull.Value) ? "" : Convert.ToString(reader["macchina"]);
                                idricambio = Convert.ToInt32(reader["ID_ricambio"]);
                                index = Convert.ToInt32(reader["pezzo"]);
                            }
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante oggetti ordini. Codice: " + ReturnErorrCode(ex));


                            return;
                        }
                    }
                    CheckBoxOrdOggCheckAddNotOffer.Enabled = false;
                    CheckBoxOrdOggCheckAddNotOffer.Checked = false;

                    FieldOrdOggIdRic.Text = Convert.ToString(idricambio);
                    FieldOrdOggId.Text = idpez;

                    FieldOrdOggPOr.Text = puo;
                    FieldOrdOggPsc.Text = pus;
                    FieldOrdOggQta.Text = qta;
                    FieldOrdOggETA.Text = ETA;

                    FieldOrdOggMach.SelectedIndex = FieldOrdOggMach.FindString(mach);
                    FieldOrdOggPezzo.SelectedIndex = FindIndexFromValue(FieldOrdOggPezzo, index);

                    FieldOrdOggDesc.Text = desc;

                    old_dataETAOrdValue.Text = ETA;
                    old_prezzo_scontatoV.Text = pus;
                    old_pezziV.Text = qta;



                    UpdateFields("OCR", "E", false);
                    UpdateFields("OCR", "FE", true);

                    BtChiudiOrdOgg.Enabled = true;
                    BtCreaOrdineOgg.Enabled = true;
                }
            }
        }

        private void DataGridViewOrdOgg_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int idOrdine = ComboSelOrd.SelectedItem.GetHashCode();

                    string idpez = row.Cells[0].Value.ToString();
                    string nome = row.Cells[1].Value.ToString();
                    string codice = row.Cells[2].Value.ToString();
                    string puo = row.Cells[3].Value.ToString();
                    string pus = row.Cells[4].Value.ToString();
                    string qta = row.Cells[5].Value.ToString();
                    string desc = row.Cells[6].Value.ToString();
                    string ETA = row.Cells[7].Value.ToString();
                    int mach = 0;
                    int index = 0;
                    bool isnotoffer = false;

                    string commandText = @"SELECT 

											PR.ID_macchina AS macchina,
                                            ORP.ID_ricambio as pezzo,
                                            ORP.Outside_Offer as isnotoffer

									   FROM " + schemadb + @"[ordini_elenco] AS OP, " + schemadb + @"[ordine_pezzi] AS ORP

									   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id=ORP.ID_ricambio
									   LEFT JOIN " + schemadb + @"[clienti_macchine] AS CM
										ON CM.Id=PR.ID_macchina

									   WHERE OP.id=@idOrdine AND ORP.Id=@idpez LIMIT 1;";


                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@idOrdine", idOrdine);
                            cmd.Parameters.AddWithValue("@idpez", idpez);

                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                mach = (reader["macchina"] == DBNull.Value) ? -1 : (int)reader["macchina"];
                                index = Convert.ToInt32(reader["pezzo"]);
                                isnotoffer = Convert.ToBoolean(reader["isnotoffer"]);
                            }
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante oggetti ordini. Codice: " + ReturnErorrCode(ex));


                            return;
                        }
                    }

                    CheckBoxOrdOggCheckAddNotOffer.Checked = isnotoffer;
                    CheckBoxOrdOggCheckAddNotOffer.Enabled = false;



                    FieldOrdOggMach.SelectedIndex = FindIndexFromValue(FieldOrdOggPezzo, mach);
                    Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, mach);
                    FieldOrdOggPezzo.SelectedIndex = FindIndexFromValue(FieldOrdOggPezzo, index);

                    FieldOrdOggId.Text = idpez;
                    FieldOrdOggPOr.Text = puo;
                    FieldOrdOggPsc.Text = pus;
                    FieldOrdOggQta.Text = qta;
                    FieldOrdOggETA.Text = ETA;
                    FieldOrdOggDesc.Text = desc;

                    old_prezzo_scontatoV.Text = pus;
                    old_pezziV.Text = qta;
                    old_dataETAOrdValue.Text = ETA;

                    FieldOrdOggPOr.Enabled = true;
                    FieldOrdOggPsc.Enabled = true;
                    FieldOrdOggQta.Enabled = true;
                    FieldOrdOggETA.Enabled = true;

                    CheckBoxOrdOggSconto.Enabled = true;

                    BtChiudiOrdOgg.Enabled = true;
                    BtDelOrdOgg.Enabled = true;
                    BtSaveModOrdOgg.Enabled = true;

                    BtCreaOrdineOgg.Enabled = false;
                }
            }
        }

        private void BtCreaOrdineOgg_Click(object sender, EventArgs e)
        {
            BtCreaOrdineOgg.Enabled = false;
            UpdateFields("OCR", "FE", false);

            int idoggOff = (String.IsNullOrEmpty(FieldOrdOggId.Text.Trim())) ? 0 : Convert.ToInt32(FieldOrdOggId.Text.Trim());
            int idordine = ComboSelOrd.SelectedItem.GetHashCode();

            string dataETAString = FieldOrdOggETA.Text.Trim();
            string prezzo_originale = FieldOrdOggPOr.Text.Trim();
            string prezzo_scontato = FieldOrdOggPsc.Text.Trim();
            string pezzi = FieldOrdOggQta.Text.Trim();
            int idiri = 0;

            ValidationResult dataETAOrdValue;
            ValidationResult prezzo_originaleV;
            ValidationResult prezzo_scontatoV;

            string er_list = "";

            if (CheckBoxOrdOggCheckAddNotOffer.Checked == true)
            {
                idiri = Convert.ToInt32(FieldOrdOggPezzo.SelectedItem.GetHashCode());
            }
            else
            {
                idiri = Convert.ToInt32(FieldOrdOggIdRic.Text);
            }

            if (idiri < 1)
            {
                er_list += "Selezionare un ricambio dal menù a tendina." + Environment.NewLine;
            }

            dataETAOrdValue = ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            prezzo_originaleV = ValidatePrezzo(prezzo_originale);
            er_list += prezzo_originaleV.Error;

            prezzo_scontatoV = ValidatePrezzo(prezzo_scontato);
            er_list += prezzo_originaleV.Error;

            if (!Int32.TryParse(pezzi, style, culture, out int pezziV))
            {
                er_list += "Numero pezzi non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (pezziV < 0)
                {
                    er_list += "Il numero di pezzi deve essere positivo" + Environment.NewLine;
                }
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                bool ischk = CheckBoxOrdOggCheckAddNotOffer.Checked;

                FieldOrdOggPOr.Enabled = true;
                FieldOrdOggPsc.Enabled = true;
                FieldOrdOggQta.Enabled = true;
                FieldOrdOggETA.Enabled = true;
                BtChiudiOrdOgg.Enabled = true;
                CheckBoxOrdOggSconto.Enabled = true;
                CheckBoxOrdOggCheckAddNotOffer.Checked = ischk;
                if (ischk)
                {
                    FieldOrdOggMach.Enabled = true;
                    FieldOrdOggPezzo.Enabled = true;
                    FieldOrdOggPezzoFiltro.Enabled = true;

                    CheckBoxOrdOggCheckAddNotOffer.Enabled = true;
                }
                BtCreaOrdineOgg.Enabled = true;
                return;
            }

            string commandText = @" BEGIN TRANSACTION;
                                        INSERT OR ROLLBACK INTO " + schemadb + @"[ordine_pezzi]
										(ID_ordine, ID_ricambio, prezzo_unitario_originale, prezzo_unitario_sconto,pezzi, ETA, Outside_Offer) 
										VALUES (@idord,@idri,@por,@pos,@pezzi,@eta,@Outside_Offer); 

									UPDATE OR ROLLBACK " + schemadb + @"[ordini_elenco]
										SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine=@idord),0)
										WHERE Id=@idord LIMIT 1;
										
									UPDATE OR ROLLBACK " + schemadb + @"[ordini_elenco] 
										SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine = @idord),0)
										WHERE Id = @idord LIMIT 1;
							";

            if (!CheckBoxOrdOggCheckAddNotOffer.Checked)
            {
                commandText += @" UPDATE OR ROLLBACK " + schemadb + @"[offerte_pezzi] SET aggiunto=1 WHERE Id=@idoggoff LIMIT 1;";
            }

            if (CheckBoxOrdOggSconto.Checked)
            {
                commandText += @" UPDATE OR ROLLBACK " + schemadb + @"[ordini_elenco] 
									SET prezzo_finale = IFNULL(totale_ordine*(1-sconto/100),0) 
									WHERE Id=@idord LIMIT 1;";
            }
            commandText += "COMMIT;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idord", idordine);
                    cmd.Parameters.AddWithValue("@idri", idiri);
                    cmd.Parameters.AddWithValue("@por", prezzo_originaleV.DecimalValue);
                    cmd.Parameters.AddWithValue("@pos", prezzo_scontatoV.DecimalValue);
                    cmd.Parameters.AddWithValue("@pezzi", pezziV);
                    cmd.Parameters.AddWithValue("@eta", dataETAOrdValue.DateValue);
                    cmd.Parameters.AddWithValue("@Outside_Offer", (CheckBoxOrdOggCheckAddNotOffer.Checked == true) ? 1 : 0);
                    cmd.Parameters.AddWithValue("@idoggoff", idoggOff);


                    cmd.ExecuteNonQuery();

                    if (Boolean.Parse(settings["calendario"]["aggiornaCalendario"]) == true)
                    {
                        string ordinecode = "";
                        DateTime eta = DateTime.MinValue;

                        commandText = @"SELECT 
                                            codice_ordine,
                                            data_ETA
                                        FROM " + schemadb + @"[ordini_elenco] 
                                        WHERE Id=@idord LIMIT 1;";

                        using (SQLiteCommand cmd2 = new SQLiteCommand(commandText, connection))
                        {
                            try
                            {
                                cmd2.CommandText = commandText;
                                cmd2.Parameters.AddWithValue("@idord", idordine);

                                SQLiteDataReader reader = cmd2.ExecuteReader();
                                while (reader.Read())
                                {
                                    ordinecode = (string)reader["codice_ordine"];
                                    eta = (DateTime)reader["data_ETA"];
                                }
                                eta = eta.Date;
                            }
                            catch (SQLiteException ex)
                            {
                                MessageBox.Show("Errore durante lettura dati ordine(dati calendario). Codice: " + ReturnErorrCode(ex));
                            }
                        }

                        if (!String.IsNullOrEmpty(ordinecode) && FindAppointment(ordinecode, eta) == true)
                        {
                            DialogResult dialogResult = MessageBox.Show("Vuoi aggiornare l'evento sul calendario con le nuove informazioni?", "Aggiornare Evento Ordine Calendario", MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.Yes)
                            {
                                UpdateCalendar(ordinecode, ordinecode, idordine, eta, false);
                            }
                        }
                    }

                    int currentOrd = ComboSelOrd.SelectedIndex;

                    UpdateFields("OCR", "CE", false);
                    UpdateFields("OCR", "E", false);
                    UpdateFields("OCR", "FE", false);

                    ComboBoxOrdOfferta_SelectedIndexChanged(this, System.EventArgs.Empty);

                    UpdateOrdini(OrdiniCurPage);
                    ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    UpdateFields("OCR", "CA", false);
                    UpdateFields("OCR", "A", false);

                    ComboSelOrd.SelectedIndex = currentOrd;

                    int i = 0;
                    foreach (ComboBoxList item in ComboSelOrd.Items)
                    {
                        if (item.Value == idordine)
                        {
                            ComboSelOrd.SelectedIndex = i;
                        }
                        i++;
                    }

                    MessageBox.Show("Oggetto aggiunto all'ordine");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiunta al database. Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    bool ischk = CheckBoxOrdOggCheckAddNotOffer.Checked;

                    FieldOrdOggPOr.Enabled = true;
                    FieldOrdOggPsc.Enabled = true;
                    FieldOrdOggQta.Enabled = true;
                    FieldOrdOggETA.Enabled = true;
                    BtChiudiOrdOgg.Enabled = true;
                    CheckBoxOrdOggSconto.Enabled = true;
                    CheckBoxOrdOggCheckAddNotOffer.Checked = ischk;
                    if (ischk)
                    {
                        FieldOrdOggMach.Enabled = true;
                        FieldOrdOggPezzo.Enabled = true;
                        FieldOrdOggPezzoFiltro.Enabled = true;

                        CheckBoxOrdOggCheckAddNotOffer.Enabled = true;
                    }
                    BtCreaOrdineOgg.Enabled = true;
                    return;
                }
            }
            return;
        }

        private void BtDelOrd_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("OCR", "E", false);
            UpdateFields("OCR", "A", false);

            string idOr = FieldOrdId.Text.Trim();

            string er_list = "";

            if (!int.TryParse(idOr, out int idQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "A", true);
                UpdateFields("OCR", "E", true);

                BtCreaOrdine.Enabled = false;
                CheckBoxCopiaOffertainOrdine.Enabled = false;
                BtSaveModOrd.Enabled = true;
                BtDelOrd.Enabled = true;
                BtChiudiOrd.Enabled = true;

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi veramente eliminare l'ordine? Tutti i dati relativi all'ordine verrano eliminati", "Eliminare Ordine", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "A", true);
                UpdateFields("OCR", "E", true);

                BtCreaOrdine.Enabled = false;
                CheckBoxCopiaOffertainOrdine.Enabled = false;
                BtSaveModOrd.Enabled = true;
                BtDelOrd.Enabled = true;
                BtChiudiOrd.Enabled = true;

                return;
            }


            string commandText = "SELECT  ID_offerta FROM " + schemadb + @"[ordini_elenco] WHERE Id=@idord LIMIT 1;";


            using (SQLiteCommand cmd2 = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd2.CommandText = commandText;
                    cmd2.Parameters.AddWithValue("@idord", idQ);

                    var Id_offerta_result = cmd2.ExecuteScalar();

                    int? id_offerta = (Id_offerta_result == DBNull.Value) ? null : (int?)Convert.ToInt32(cmd2.ExecuteScalar());

                    commandText = "";

                    if (id_offerta > 0)
                    {
                        commandText = @"UPDATE " + schemadb + @"[offerte_pezzi]
									        SET
										        aggiunto = 0 
									        WHERE 
										        ID_offerta=@idoff;
                                        UPDATE " + schemadb + @"[offerte_elenco] SET trasformato_ordine=0 WHERE Id=@idoff LIMIT 1;
                                    ";
                    }

                    commandText += @"   DELETE FROM " + schemadb + @"[ordine_pezzi] WHERE ID_ordine=@idq;
                                        DELETE FROM " + schemadb + @"[ordini_elenco] WHERE Id=@idq LIMIT 1;";

                    using (var transaction = connection.BeginTransaction())
                    using (SQLiteCommand cmd_up = new SQLiteCommand(commandText, connection, transaction))
                    {
                        try
                        {
                            cmd_up.CommandText = commandText;
                            cmd_up.Parameters.AddWithValue("@idoff", id_offerta);
                            cmd_up.Parameters.AddWithValue("@idq", idQ);

                            cmd_up.ExecuteNonQuery();
                            transaction.Commit();

                            if (Boolean.Parse(settings["calendario"]["aggiornaCalendario"]) == true)
                            {
                                string ordinecode = "";
                                DateTime eta = DateTime.MinValue;

                                commandText = @"SELECT 
                                                codice_ordine,
                                                data_ETA
                                            FROM " + schemadb + @"[ordini_elenco] 
                                            WHERE Id=@idord LIMIT 1;";

                                using (SQLiteCommand cmd3 = new SQLiteCommand(commandText, connection))
                                {
                                    try
                                    {
                                        cmd3.CommandText = commandText;
                                        cmd3.Parameters.AddWithValue("@idord", idQ);

                                        SQLiteDataReader reader = cmd3.ExecuteReader();
                                        while (reader.Read())
                                        {
                                            ordinecode = (string)reader["codice_ordine"];
                                            eta = (DateTime)reader["data_ETA"];
                                        }
                                        eta = eta.Date;
                                    }
                                    catch (SQLiteException ex)
                                    {
                                        MessageBox.Show("Errore durante lettura dati ordine(dati calendario). Codice: " + ReturnErorrCode(ex));
                                    }
                                }

                                if (!String.IsNullOrEmpty(ordinecode) && FindAppointment(ordinecode, eta) == true)
                                {
                                    dialogResult = MessageBox.Show("Vuoi eliminare l'evento associato all'ordine?", "Eliminazione Evento Ordine Calendario", MessageBoxButtons.YesNo);
                                    if (dialogResult == DialogResult.Yes)
                                    {
                                        RemoveAppointment(ordinecode, eta);
                                    }
                                }
                            }


                            int temp = ComboSelOrd.SelectedItem.GetHashCode();

                            UpdateOrdini();

                            //DISABILITA CAMPI & BOTTONI
                            UpdateFields("OCR", "CA", true);
                            UpdateFields("OCR", "E", false);
                            UpdateFields("OCR", "A", true);
                            UpdateFields("VS", "E", false);
                            UpdateFields("VS", "CA", true);

                            BtChiudiOrd_Click(this, EventArgs.Empty);

                            ComboBoxOrdCliente.Enabled = true;

                            UpdateOfferteCrea(offerteCreaCurPage);

                            if (ComboSelOrdCl.SelectedItem.GetHashCode() > 0)
                                ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                            if (temp > 0 && idQ != temp)
                                ComboSelOrd.SelectedIndex = FindIndexFromValue(ComboSelOrd, temp);

                            MessageBox.Show("Ordine eliminato.");

                        }
                        catch (SQLiteException ex)
                        {

                            transaction.Rollback();

                            MessageBox.Show("Errore durante eliminazione ordine. Codice: " + ReturnErorrCode(ex));

                            //ABILITA CAMPI & BOTTONI
                            UpdateFields("OCR", "A", true);
                            UpdateFields("OCR", "E", true);

                            BtCreaOrdine.Enabled = false;
                            CheckBoxCopiaOffertainOrdine.Enabled = false;
                            BtSaveModOrd.Enabled = false;
                            BtDelOrd.Enabled = false;
                            BtChiudiOrd.Enabled = false;



                            return;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento offerta(select offerta). Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("OCR", "A", true);
                    UpdateFields("OCR", "E", true);

                    BtCreaOrdine.Enabled = false;
                    CheckBoxCopiaOffertainOrdine.Enabled = false;
                    BtSaveModOrd.Enabled = false;
                    BtDelOrd.Enabled = false;
                    BtChiudiOrd.Enabled = false;
                }
            }
            return;
        }

        private void BtChiudiOrdOgg_Click(object sender, EventArgs e)
        {
            UpdateFields("OCR", "CE", false);
            UpdateFields("OCR", "E2", false);
        }

        private void BtDelOrdOgg_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("OCR", "E", false);
            UpdateFields("OCR", "FE", false);
            BtCreaOrdineOgg.Enabled = false;

            string idOf = FieldOrdOggId.Text.Trim();

            string er_list = "";

            if (!int.TryParse(idOf, out int idOfQ))
            {
                er_list += "ID non valido o vuoto" + Environment.NewLine;
            }


            if (er_list != "")
            {
                MessageBox.Show(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "FE", true);
                UpdateFields("OCR", "E", true);

                return;
            }

            DialogResult dialogResult = MessageBox.Show("Vuoi rimuovere il pezzo dall'ordine?", "Eliminare Pezzo da Ordine", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "FE", true);
                UpdateFields("OCR", "E", true);
                return;
            }

            bool updateFprice = false;
            bool updateFpriceSconto = false;

            dialogResult = MessageBox.Show("Vuoi aggiornare il prezzo finale?", "Eliminare Pezzo da Ordine", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                updateFprice = true;
                dialogResult = MessageBox.Show("Applicare lo sconto al prezzo finale?", "Eliminare Pezzo da Ordine", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    updateFpriceSconto = true;
                }
            }

            int idordine = ComboSelOrd.SelectedItem.GetHashCode();


            string commandText = @"
									UPDATE " + schemadb + @"[offerte_pezzi]
                                        SET aggiunto = 0 
                                        WHERE 
	                                        Id IN (
						                            SELECT 
                                                        OFP.Id
						                            FROM " + schemadb + @"[ordine_pezzi] AS OP 
                                                    INNER JOIN " + schemadb + @"[ordini_elenco] AS OE 
											            ON OE.Id = OP.ID_ordine 
										            INNER JOIN " + schemadb + @" [offerte_pezzi] AS OFP 
											            ON OFP.ID_ricambio = OP.ID_ricambio AND OFP.ID_offerta=OE.ID_offerta
                                                    WHERE
                                                        OP.Id=@idoff
                                                    LIMIT 1
					                            )
                                        LIMIT 1;

                                    DELETE FROM " + schemadb + @"[ordine_pezzi] WHERE Id=@idoff LIMIT 1;

                                    UPDATE " + schemadb + @"[ordini_elenco]
                                        SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine = @idord),0)
                                        WHERE Id = @idord 
                                        LIMIT 1; 
                                    ";
            if (updateFprice)
            {
                commandText += @"UPDATE " + schemadb + @"[ordini_elenco]
												SET 
                                                    prezzo_finale = " + ((updateFpriceSconto) ? " (totale_ordine*(1-sconto/100)) " : " totale_ordine ") + @"
												WHERE Id = @idord
                                                LIMIT 1;";
            }


            using (var transaction = connection.BeginTransaction())
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection, transaction))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idoff", idOfQ);
                    cmd.Parameters.AddWithValue("@idord", idordine);
                    cmd.ExecuteNonQuery();

                    transaction.Commit();

                    if (Boolean.Parse(settings["calendario"]["aggiornaCalendario"]) == true)
                    {
                        string ordinecode = "";
                        DateTime eta = DateTime.MinValue;

                        commandText = @"SELECT 
                                            codice_ordine,
                                            data_ETA
                                        FROM " + schemadb + @"[ordini_elenco] 
                                        WHERE Id=@idord LIMIT 1;";

                        using (SQLiteCommand cmd3 = new SQLiteCommand(commandText, connection))
                        {
                            try
                            {
                                cmd3.CommandText = commandText;
                                cmd3.Parameters.AddWithValue("@idord", idordine);

                                SQLiteDataReader reader = cmd3.ExecuteReader();
                                while (reader.Read())
                                {
                                    ordinecode = (string)reader["codice_ordine"];
                                    eta = (DateTime)reader["data_ETA"];
                                }
                                eta = eta.Date;
                            }
                            catch (SQLiteException ex)
                            {
                                MessageBox.Show("Errore durante lettura dati ordine(dati calendario). Codice: " + ReturnErorrCode(ex));
                            }
                        }

                        if (!String.IsNullOrEmpty(ordinecode) && FindAppointment(ordinecode, eta) == true)
                        {
                            dialogResult = MessageBox.Show("Vuoi aggiornare l'evento sul calendario con le nuove informazioni?", "Aggiornare Evento Ordine Calendario", MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.Yes)
                            {
                                UpdateCalendar(ordinecode, ordinecode, idordine, eta, false);
                            }
                        }
                    }

                    UpdateOrdini();
                    UpdateOrdiniOggettiOfferta(idordine);
                    UpdateOrdiniOggetti(idordine);

                    ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    UpdateFields("OCR", "CA", false);
                    UpdateFields("OCR", "A", false);

                    int i = 0;
                    foreach (ComboBoxList item in ComboSelOrd.Items)
                    {
                        if (item.Value == idordine)
                        {
                            ComboSelOrd.SelectedIndex = i;
                        }
                        i++;
                    }

                    ComboBoxOrdOfferta_SelectedIndexChanged(this, System.EventArgs.Empty);

                    BtChiudiOrdOgg_Click(this, EventArgs.Empty);

                    MessageBox.Show("Oggetti eliminato dall'ordine.");

                }
                catch (SQLiteException ex)
                {
                    transaction.Rollback();

                    MessageBox.Show("Errore durante upate tot ordine (aggiornamento stato oggetto offerta). Codice: " + ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("OCR", "FE", true);
                    UpdateFields("OCR", "E", true);
                    return;
                }
            }
            return;
        }

        private void BtSaveModOrd_Click(object sender, EventArgs e)
        {
            int id_ordine = Convert.ToInt32(FieldOrdId.Text.Trim());

            string n_ordine = FieldOrdNOrdine.Text.Trim();

            string dataOrdString = FieldOrdData.Text.Trim();
            string dataETAString = FieldOrdETA.Text.Trim();

            string sconto = FieldOrdSconto.Text.Trim();

            string spedizioni = FieldOrdSped.Text.Trim();
            int gestSP = FieldOrdSpedGestione.SelectedItem.GetHashCode();

            string prezzo_finale = FieldOrdPrezF.Text.Trim();
            string tot_ordine = FieldOrdTot.Text.Trim();

            int stato_ordine = FieldOrdStato.SelectedItem.GetHashCode();
            stato_ordine = (stato_ordine < 0) ? 0 : stato_ordine;

            ValidationResult dataOrdValue;
            ValidationResult dataETAOrdValue;

            ValidationResult prezzoSpedizione = new ValidationResult();
            ValidationResult scontoV = new ValidationResult();
            ValidationResult tot_ordineV = new ValidationResult();
            ValidationResult prezzo_finaleV = new ValidationResult();

            string er_list = "";
            if (string.IsNullOrEmpty(n_ordine) || !Regex.IsMatch(n_ordine, @"^\d+$"))
            {
                er_list += "Numero Ordine non valido o vuoto" + Environment.NewLine;
            }

            dataOrdValue = ValidateDate(dataOrdString);
            er_list += dataOrdValue.Error;

            dataETAOrdValue = ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            if (DateTime.Compare(dataOrdValue.DateValue, dataETAOrdValue.DateValue) > 0)
            {
                er_list += "Data di Arrivo(ETA) antecedente a quella di creazione dell'ordine" + Environment.NewLine;
            }

            scontoV = ValidateSconto(sconto);
            er_list += scontoV.Error;

            tot_ordineV = ValidatePrezzo(tot_ordine);
            er_list += tot_ordineV.Error;

            prezzo_finaleV = ValidatePrezzo(prezzo_finale);
            er_list += prezzo_finaleV.Error;

            if (!string.IsNullOrEmpty(spedizioni))
            {
                if (!string.IsNullOrEmpty(spedizioni))
                {
                    prezzoSpedizione = ValidateSpedizione(spedizioni, gestSP);
                    er_list += prezzoSpedizione.Error;
                }
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);
                UpdateFields("OCR", "A", true);
                return;
            }

            DialogResult res = MessageBox.Show("Vuoi salvare le modifiche all'ordine?", "Conferma Salvataggio Modifiche Ordine", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (res != DialogResult.OK)
            {
                UpdateFields("OCR", "A", true);
                return;
            }



            string oldRef = "";
            DateTime oldETA = DateTime.MinValue;
            decimal oldPrezF = 0;
            int oldStato = -1;

            string commandText = commandText = @"SELECT 
                                                    codice_ordine,
                                                    data_ETA,
                                                    prezzo_finale,
                                                    stato
                                                FROM " + schemadb + @"[ordini_elenco] WHERE Id=@idord LIMIT " + recordsPerPage;


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idord", id_ordine);

                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        oldRef = Convert.ToString(reader["codice_ordine"]);
                        oldETA = (DateTime)reader["data_ETA"];
                        oldPrezF = (decimal)(reader["prezzo_finale"]);

                        oldStato = Convert.ToInt32(reader["stato"]);
                    }
                    oldETA = oldETA.Date;

                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante eliminazione ordine (aggiornamento toast). Codice: " + ReturnErorrCode(ex));
                }
            }

            commandText = @"UPDATE " + schemadb + @"[ordini_elenco] SET 
									codice_ordine= @codo, 
                                    data_ordine=@dataord, 
									data_ETA=@dataeta, 
									totale_ordine=@totord,
									sconto=@sconto,
									prezzo_finale=@prezzoF,
									stato=@stato, 
									costo_spedizione=@cossp, 
									gestione_spedizione=@gestsp
						   WHERE Id=@idord 
                           LIMIT 1";



            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@codo", n_ordine);
                    cmd.Parameters.AddWithValue("@dataord", dataOrdValue);
                    cmd.Parameters.AddWithValue("@dataeta", dataETAOrdValue);
                    cmd.Parameters.AddWithValue("@totord", tot_ordineV.DecimalValue);
                    cmd.Parameters.AddWithValue("@sconto", scontoV.DecimalValue);
                    cmd.Parameters.AddWithValue("@prezzoF", prezzo_finaleV.DecimalValue);
                    cmd.Parameters.AddWithValue("@stato", stato_ordine);
                    cmd.Parameters.AddWithValue("@idord", id_ordine);
                    if (prezzoSpedizione.DecimalValue.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@cossp", prezzoSpedizione.DecimalValue);
                        cmd.Parameters.AddWithValue("@gestsp", gestSP);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@cossp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@gestsp", DBNull.Value);
                    }

                    cmd.ExecuteScalar();

                    int temp = ComboSelOrd.SelectedItem.GetHashCode();

                    UpdateOrdini(OrdiniCurPage);
                    UpdateFields("OCR", "CA", true);
                    UpdateFields("OCR", "A", false);
                    UpdateFields("VS", "E", false);

                    BtChiudiOrd_Click(this, System.EventArgs.Empty);

                    if (ComboSelOrdCl.SelectedItem.GetHashCode() > 0)
                        ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    if (temp > 0)
                        ComboSelOrd.SelectedIndex = FindIndexFromValue(ComboSelOrd, temp);

                    UpdateOfferteCrea(offerteCreaCurPage);


                    if (Boolean.Parse(settings["calendario"]["aggiornaCalendario"]) == true)
                    {
                        if (FindAppointment(oldRef, oldETA) == true)
                        {
                            bool removed = false;
                            if (oldStato != stato_ordine && stato_ordine == 1)
                            {
                                res = MessageBox.Show("L'ordine è stato chiuso, vuoi rimuoverlo dal calendario?", "Conferma Rimozione Ordine da Calendario", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                                if (res != DialogResult.OK)
                                {
                                    RemoveAppointment(oldRef, oldETA);
                                    removed = true;
                                }
                            }
                            if (removed == false)
                            {
                                if (DateTime.Compare(oldETA, dataETAOrdValue.DateValue) == 0 && (oldPrezF != prezzo_finaleV.DecimalValue || oldRef != n_ordine))
                                {
                                    res = MessageBox.Show("Vuoi aggiornare l'evento del calendario relativo alll'ordine con le nuove informazioni?", "Conferma Aggiornamento Ordine Calendario", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                                    if (res != DialogResult.Yes)
                                    {
                                        UpdateCalendar(oldRef, n_ordine, id_ordine, dataETAOrdValue.DateValue, false);
                                    }
                                }
                                else if (DateTime.Compare(oldETA, dataETAOrdValue.DateValue) != 0)
                                {
                                    res = MessageBox.Show("Vuoi aggiornare l'evento del calendario relativo alll'ordine con le nuove informazioni?" + Environment.NewLine + "L'evento verrà cancellato per poi essere inserito nuovamente.", "Conferma Aggiornamento Ordine Calendario", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                                    if (res != DialogResult.Yes)
                                    {
                                        UpdateCalendar(oldRef, n_ordine, id_ordine, dataETAOrdValue.DateValue);
                                    }
                                }
                            }
                        }
                    }

                    MessageBox.Show("Ordine Aggiornato.");

                    DateTime today = DateTime.Today;
                    FieldOrdData.Text = today.ToString("dd/MM/yyyy");
                    FieldOrdETA.Text = today.ToString("dd/MM/yyyy");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento ordine. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("OCR", "A", true);
                }
            }
            return;
        }

        private void BtSaveModOrdOgg_Click(object sender, EventArgs e)
        {
            int idoggOff = Convert.ToInt32(FieldOrdOggId.Text.Trim());
            int idordine = Convert.ToInt32(ComboSelOrd.SelectedItem.GetHashCode());

            string dataETAString = FieldOrdOggETA.Text.Trim();
            string prezzo_originale = FieldOrdOggPOr.Text.Trim();
            string prezzo_scontato = FieldOrdOggPsc.Text.Trim();
            string pezzi = FieldOrdOggQta.Text.Trim();


            string er_list = "";

            if (!DateTime.TryParseExact(dataETAString, dateFormat, provider, DateTimeStyles.None, out DateTime dataETAOrdValue))
            {
                er_list += "Data non valida o vuota" + Environment.NewLine;
            }

            if (!Decimal.TryParse(prezzo_originale, style, culture, out decimal prezzo_originaleV))
            {
                er_list += "Prezzo non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzo_originaleV < 0)
                {
                    er_list += "Il prezzo deve essere positivo" + Environment.NewLine;
                }
            }

            if (!Decimal.TryParse(prezzo_scontato, style, culture, out decimal prezzo_scontatoV))
            {
                er_list += "Prezzo finale non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzo_scontatoV < 0)
                {
                    er_list += "Il prezzo finale deve essere positivo" + Environment.NewLine;
                }
            }

            if (!Int32.TryParse(pezzi, style, culture, out int pezziV))
            {
                er_list += "Numero pezzi non valido o vuoto" + Environment.NewLine;
            }
            else
            {
                if (pezziV < 0)
                {
                    er_list += "Il numero di pezzi deve essere positivo" + Environment.NewLine;
                }
            }

            if (er_list != "")
            {
                MessageBox.Show(er_list);

                FieldOrdOggPOr.Enabled = true;
                FieldOrdOggPsc.Enabled = true;
                FieldOrdOggQta.Enabled = true;
                FieldOrdOggETA.Enabled = true;

                BtChiudiOrdOgg.Enabled = true;
                BtDelOrdOgg.Enabled = true;
                BtSaveModOrdOgg.Enabled = true;

                return;
            }

            DialogResult res = MessageBox.Show("Vuoi salvare le modifiche all'oggetto?", "Conferma Salvataggio Modifiche Oggetto Ordine", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (res != DialogResult.OK)
            {

                FieldOrdOggPOr.Enabled = true;
                FieldOrdOggPsc.Enabled = true;
                FieldOrdOggQta.Enabled = true;
                FieldOrdOggETA.Enabled = true;

                BtChiudiOrdOgg.Enabled = true;
                BtDelOrdOgg.Enabled = true;
                BtSaveModOrdOgg.Enabled = true;

                return;
            }


            string commandText = @" BEGIN TRANSACTION;
                                    UPDATE OR ROLLBACK " + schemadb + @"[ordine_pezzi]
									    SET
										    prezzo_unitario_originale=@por, prezzo_unitario_sconto=@pos,pezzi=@pezzi, ETA=@eta
									    WHERE
										    Id=@idoggoff
									    LIMIT 1;

                                    UPDATE OR ROLLBACK " + schemadb + @"[ordini_elenco]
									    SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine = @idord),0)
									    WHERE Id = @idord LIMIT 1;
                                    ";
            if (CheckBoxOrdOggSconto.Checked)
            {
                commandText += @"UPDATE OR ROLLBACK " + schemadb + @"[ordini_elenco] 
									SET prezzo_finale = IFNULL(totale_ordine*(1-sconto/100),0)
									WHERE Id=@idord LIMIT 1;";
            }
            commandText += "COMMIT;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@por", prezzo_originaleV);
                    cmd.Parameters.AddWithValue("@pos", prezzo_scontatoV);
                    cmd.Parameters.AddWithValue("@pezzi", pezziV);
                    cmd.Parameters.AddWithValue("@eta", dataETAOrdValue);
                    cmd.Parameters.AddWithValue("@idoggoff", idoggOff);

                    cmd.Parameters.AddWithValue("@idord", idordine);


                    cmd.ExecuteNonQuery();

                    if (Boolean.Parse(settings["calendario"]["aggiornaCalendario"]) == true)
                    {
                        if (Convert.ToDecimal(old_prezzo_scontatoV.Text) != prezzo_scontatoV || Convert.ToInt32(old_pezziV.Text) != pezziV || DateTime.Compare(Convert.ToDateTime(old_dataETAOrdValue.Text).Date, dataETAOrdValue) != 0)
                        {
                            string ordinecode = "";
                            DateTime eta = DateTime.MinValue;

                            commandText = @"SELECT 
                                                codice_ordine,
                                                data_ETA
                                            FROM " + schemadb + @"[ordini_elenco] 
                                            WHERE Id=@idord LIMIT 1;";

                            using (SQLiteCommand cmd3 = new SQLiteCommand(commandText, connection))
                            {
                                try
                                {
                                    cmd3.CommandText = commandText;
                                    cmd3.Parameters.AddWithValue("@idord", idordine);

                                    SQLiteDataReader reader = cmd3.ExecuteReader();
                                    while (reader.Read())
                                    {
                                        ordinecode = (string)reader["codice_ordine"];
                                        eta = (DateTime)reader["data_ETA"];
                                    }
                                    eta = eta.Date;
                                }
                                catch (SQLiteException ex)
                                {
                                    MessageBox.Show("Errore durante lettura dati ordine(dati calendario). Codice: " + ReturnErorrCode(ex));
                                }
                            }

                            if (!String.IsNullOrEmpty(ordinecode) && FindAppointment(ordinecode, eta) == true)
                            {
                                DialogResult dialogResult = MessageBox.Show("Vuoi aggiornare l'evento sul calendario con le nuove informazioni?", "Aggiornare Evento Ordine Calendario", MessageBoxButtons.YesNo);
                                if (dialogResult == DialogResult.Yes)
                                {
                                    UpdateCalendar(ordinecode, ordinecode, idordine, eta, false);
                                }
                            }
                        }
                    }

                    UpdateOrdini(OrdiniCurPage);
                    ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    if (FieldOrdId.Text.Trim() == Convert.ToString(idordine))
                    {
                        UpdateFields("OCR", "CA", false);
                        UpdateFields("OCR", "A", false);
                    }

                    UpdateFields("OCR", "CE", false);
                    UpdateFields("OCR", "E2", false);
                    UpdateFields("OCR", "E", false);
                    UpdateFields("OCR", "FE", false);

                    int i = 0;
                    foreach (ComboBoxList item in ComboSelOrd.Items)
                    {
                        if (item.Value == idordine)
                        {
                            ComboSelOrd.SelectedIndex = i;
                        }
                        i++;
                    }


                    ComboBoxOrdOfferta_SelectedIndexChanged(this, System.EventArgs.Empty);
                    ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);

                    CheckBoxOrdOggCheckAddNotOffer.Enabled = true;

                    MessageBox.Show("Oggetto aggiornato.");
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante aggiornamento oggetto. Codice: " + ReturnErorrCode(ex));
                    UpdateFields("OAO", "E", false);
                    UpdateFields("OCR", "FE", true);
                }
            }
            return;
        }

        private void ComboSelOrdCl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboSelOrdCl.DataSource == null)
            {
                return;
            }

            int idcl = (String.IsNullOrEmpty(ComboSelOrdCl.Text.Trim())) ? 0 : Convert.ToInt32(ComboSelOrdCl.Text.Split('-')[0]);

            if (idcl > 0)
            {
                Populate_combobox_ordini(ComboSelOrd, idcl);
                ComboSelOrd.Enabled = true;
            }
            else
            {
                ComboSelOrd.Enabled = false;
                Populate_combobox_dummy(ComboSelOrd);
                ComboSelOrd.SelectedIndex = 0;
            }
        }

        private void DataGridViewFilterCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            TimerDataGridViewFilter.Stop();
            TimerDataGridViewFilter.Start();
        }

        private void DataGridViewFilterNumOrdine_TextChanged(object sender, EventArgs e)
        {
            FilterTextBox inputBx = (FilterTextBox)sender;

            string temp_text = inputBx.Text.Trim();

            if (!String.IsNullOrEmpty(temp_text) && !Regex.IsMatch(temp_text, @"^\d+$"))
                return;


            TimerDataGridViewFilter.Stop();
            TimerDataGridViewFilter.Start();
        }

        private void TimerDataGridViewFilter_Tick(object sender, EventArgs e)
        {
            TimerDataGridViewFilter.Stop();
            LoadOrdiniTable();
        }

        private void CheckBoxOrdOffertaNonPresente_CheckedChanged(object sender, EventArgs e)
        {
            int idcl = ComboBoxOrdCliente.SelectedItem.GetHashCode();
            int idcl_index = ComboBoxOrdCliente.SelectedIndex;
            int idcont = ComboBoxOrdContatto.SelectedItem.GetHashCode();
            bool origanl_state = CheckBoxOrdOffertaNonPresente.Checked;

            if (idcl > 0)
            {
                idcl = FindIndexFromValue(ComboBoxOrdCliente, idcl);

                UpdateFields("OCR", "CA", false);
                UpdateFields("OCR", "E", false);
                UpdateFields("OCR", "A", false);
                UpdateFields("OCR", "AE", false);
                ComboBoxOrdCliente.Enabled = true;
                ComboBoxOrdContatto.Enabled = false;

                ComboBoxOrdCliente.SelectedIndex = idcl_index;
                ComboBoxOrdCliente_SelectedIndexChanged(this, System.EventArgs.Empty);

                if (idcont > 0)
                {
                    idcont = FindIndexFromValue(ComboBoxOrdContatto, idcont);
                    ComboBoxOrdContatto.SelectedIndex = idcont;
                }
            }

            ComboBoxOrdCliente.SelectedIndex = idcl_index;

            if (CheckBoxOrdOffertaNonPresente.Checked)
            {
                ComboBoxOrdOfferta.Enabled = false;
                ComboBoxOrdContatto.Enabled = true;
                FieldOrdTot.Text = "0";
                FieldOrdPrezF.Text = "0";

                CheckBoxCopiaOffertainOrdine.Enabled = false;
                CheckBoxCopiaOffertainOrdine.Checked = false;

            }
            else
            {
                ComboBoxOrdOfferta.Enabled = true;
                ComboBoxOrdContatto.Enabled = false;
                ComboBoxOrdContatto.SelectedIndex = 0;

                CheckBoxCopiaOffertainOrdine.Enabled = true;
                CheckBoxCopiaOffertainOrdine.Checked = true;

            }


        }

        private void FieldOrdOggCheckAddNotOffer_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBoxOrdOggCheckAddNotOffer.Checked)
            {
                FieldOrdOggMach.SelectedIndex = 0;

                FieldOrdOggMach.Enabled = true;
                FieldOrdOggPezzo.Enabled = true;
                FieldOrdOggPezzoFiltro.Enabled = true;

                BtChiudiOrdOgg.Enabled = true;
                BtCreaOrdineOgg.Enabled = true;
                BtSaveModOrdOgg.Enabled = false;

                ComboSelOrd.Enabled = false;

                FieldOrdOggPsc.Enabled = true;
                FieldOrdOggQta.Enabled = true;
                FieldOrdOggETA.Enabled = true;
                FieldOrdOggPOr.Enabled = true;
                CheckBoxOrdOggSconto.Enabled = true;
            }
            else
            {
                UpdateFields("OCR", "CE", false);

                FieldOrdOggMach.Enabled = false;
                FieldOrdOggMach.Enabled = false;
                FieldOrdOggPezzoFiltro.Enabled = false;

                BtChiudiOrdOgg.Enabled = false;
                BtCreaOrdineOgg.Enabled = false;

                FieldOrdOggPsc.Enabled = false;
                FieldOrdOggQta.Enabled = false;
                FieldOrdOggETA.Enabled = false;
                FieldOrdOggPOr.Enabled = false;

                CheckBoxOrdOggSconto.Enabled = false;

                int curItem = AddOffCreaOggettoMach.SelectedItem.GetHashCode();
                Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, curItem > 0 ? curItem : 0);

                if ((int)ComboSelOrdCl.SelectedItem.GetHashCode() > 0)
                {
                    ComboSelOrd.Enabled = true;
                }
            }
        }

        private void TimerFieldOrdOggPezzoFiltro_Tick(object sender, EventArgs e)
        {
            TimerFieldOrdOggPezzoFiltro.Stop();

            string newFieldOrdOggPezzoFiltro_Text = FieldOrdOggPezzoFiltro.Text.Trim();

            if (CheckBoxOrdOggCheckAddNotOffer.Checked)
            {
                if (newFieldOrdOggPezzoFiltro_Text != FieldOrdOggPezzoFiltro_Text && newFieldOrdOggPezzoFiltro_Text != FieldOrdOggPezzoFiltro.PlaceholderText)
                {

                    FieldOrdOggPezzoFiltro_Text = newFieldOrdOggPezzoFiltro_Text;

                    int curItem = AddOffCreaOggettoMach.SelectedItem.GetHashCode();
                    Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, curItem > 0 ? curItem : 0, true);

                    FieldOrdOggPOr.Text = "";
                    FieldOrdOggPsc.Text = "";
                    FieldOrdOggQta.Text = "";

                    FieldOrdOggETA.Text = DateTime.Today.ToString("dd/MM/yyyy");
                    FieldOrdOggDesc.Text = "";
                }
            }
        }

        private void FieldOrdOggPezzoFiltro_TextChanged(object sender, EventArgs e)
        {
            TimerFieldOrdOggPezzoFiltro.Stop();
            TimerFieldOrdOggPezzoFiltro.Start();
        }

        private void FieldOrdOggMach_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FieldOrdOggMach.DataSource == null)
            {
                return;
            }

            int id_mach = Convert.ToInt32(FieldOrdOggMach.SelectedItem.GetHashCode());

            id_mach = (id_mach > 0) ? id_mach : 0;
            Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, id_mach, true);
        }

        private void FieldOrdOggPezzo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FieldOrdOggPezzo.DataSource == null)
            {
                return;
            }

            if (CheckBoxOrdOggCheckAddNotOffer.Checked)
            {
                int id_ricambio = FieldOrdOggPezzo.SelectedItem.GetHashCode();
                if (id_ricambio > 0)
                {
                    string commandText = @"SELECT 
										prezzo
									   FROM " + schemadb + @"[pezzi_ricambi]
									   WHERE Id=@id_ricambio 
                                        LIMIT 1;";


                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {
                        try
                        {

                            cmd.Parameters.AddWithValue("@id_ricambio", id_ricambio);
                            FieldOrdOggPOr.Text = Convert.ToString(cmd.ExecuteScalar());
                            FieldOrdOggPsc.Text = FieldOrdOggPOr.Text;
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante recupero prezzo ricambio. Codice: " + ReturnErorrCode(ex));

                            return;
                        }
                    }
                }
                else
                {
                    FieldOrdOggPOr.Text = "";
                    FieldOrdOggPsc.Text = "";
                    FieldOrdOggQta.Text = "";

                    FieldOrdOggETA.Text = DateTime.Today.ToString("dd/MM/yyyy");
                    FieldOrdOggDesc.Text = "";
                }
            }
        }

        //VISUALIZZA ORDINI
        private void LoadVisualizzaOrdiniTable(int page = 1)
        {
            DataGridView[] data_grid = new DataGridView[] { DataGridViewVisualizzaOrdini };


            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[ordini_elenco];";
            int count = 1;


            using (SQLiteCommand cmdCount = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    count = Convert.ToInt32(cmdCount.ExecuteScalar());
                    count = (count - 1) / recordsPerPage + 1;
                    MaxPageOrdView.Text = Convert.ToString((count > 1) ? count : 1);
                    if (count > 1)
                    {
                        OrdViewNxtPage.Enabled = true;
                        OrdViewPrvPage.Enabled = true;
                        OrdViewCurPage.Enabled = true;
                    }
                    else
                    {
                        OrdViewNxtPage.Enabled = false;
                        OrdViewPrvPage.Enabled = false;
                        OrdViewCurPage.Enabled = false;
                    }
                    page = (page > count) ? count : page;
                    OrdiniViewCurPage = page;
                    OrdViewCurPage.Text = Convert.ToString(page);
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante verifica records in elenco ordini. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            int stato = 0;
            string addInfo = "";
            if (stato > 0)
                addInfo = " AND OE.stato= " + stato + " ";

            commandText = @"SELECT  
									OE.Id AS ID,
									OE.codice_ordine AS codOrd,
									(OFE.Id || ' - ' || OFE.codice_offerta) AS IDoff,
									(CE.Id || ' - ' || CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')') AS Cliente,
									IIF(OFE.ID_riferimento>0,   (CR.Id || ' - ' || CR.nome),'') AS Pref,
									strftime('%d/%m/%Y',OE.data_ordine) AS datOr,
									strftime('%d/%m/%Y',OE.data_ETA) AS datEta,
									REPLACE( printf('%.2f',OE.totale_ordine ),'.',',')  AS totord,
									REPLACE( (printf('%.2f',OE.prezzo_finale + (CASE OE.gestione_spedizione  
                                                         WHEN 1 THEN   OE.costo_spedizione
                                                         WHEN 2 THEN   OE.costo_spedizione*(1-OE.sconto/100) 
                                                         ELSE 0  
                                                      END) ) || ' (' || printf('%.2f',OE.sconto ) || '%)'),'.',',')  AS prezfinale,
                                    IIF(OE.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OE.costo_spedizione ),'.',','), NULL)  AS spesesped,
                                    (CASE OE.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,
									(CASE OE.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS Stato

								   FROM " + schemadb + @"[ordini_elenco] AS OE 
								   LEFT JOIN " + schemadb + @"[offerte_elenco] OFE 
										ON OFE.Id = OE.ID_offerta 
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = OFE.ID_cliente 
								   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR 
										ON CR.Id = OFE.ID_riferimento 
                                    WHERE OE.ID_offerta IS NOT NULL " + addInfo + @" 

                                    UNION ALL

                                    SELECT  
									OE.Id AS ID,
									OE.codice_ordine AS codOrd,
									'' AS IDoff,
									(CE.Id || ' - ' || CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')') AS Cliente,
									IIF(OE.ID_riferimento>0,   (CR.Id || ' - ' || CR.nome),'') AS Pref,
									strftime('%d/%m/%Y',OE.data_ordine) AS datOr,
									strftime('%d/%m/%Y',OE.data_ETA) AS datEta,
									REPLACE( printf('%.2f',OE.totale_ordine ),'.',',')  AS totord,
									REPLACE( (printf('%.2f',OE.prezzo_finale + (CASE OE.gestione_spedizione  
                                                         WHEN 1 THEN   OE.costo_spedizione
                                                         WHEN 2 THEN   OE.costo_spedizione*(1-OE.sconto/100) 
                                                         ELSE 0  
                                                      END) ) || ' (' || printf('%.2f',OE.sconto ) || '%)'),'.',',')  AS prezfinale,
                                    IIF(OE.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OE.costo_spedizione ),'.',','), NULL)  AS spesesped,
                                    (CASE OE.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,
									(CASE OE.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS Stato

								   FROM " + schemadb + @"[ordini_elenco] AS OE
								   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = OE.ID_cliente 
								   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR 
										ON CR.Id = OE.ID_riferimento 
                                    WHERE OE.ID_offerta IS NULL " + addInfo + @" 

								   ORDER BY OE.Id DESC LIMIT " + recordsPerPage + " OFFSET @startingrecord;";

            page--;


            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {

                    DataTable ds = new DataTable();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);
                    for (int i = 0; i < data_grid.Length; i++)
                    {
                        data_grid[i].DataSource = ds;

                        Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "codOrd", "Codice Ordine" },
                        { "IDoff", "ID - #Offerta" },
                        { "Cliente", "Cliente" },
                        { "Pref", "Contatto" },
                        { "datOr", "Data Ordine" },
                        { "datEta", "Data Arrivo" },
                        { "totord", "Tot. Ordine"+Environment.NewLine+"(Exl. Sconti e Sped." },
                        { "prezfinale", "Prezzo Finale"+Environment.NewLine+"(Sconti e Spedizione)" },
                        { "spesesped", "Costo Spedizione"+Environment.NewLine+"(Excl. Sconti)" },
                        { "spedg", "Gestione Costo Spedizione" },
                        { "Stato", "Stato" }
                    };
                        int colCount = data_grid[i].ColumnCount;
                        for (int j = 0; j < colCount; j++)
                        {
                            if (columnNames.ContainsKey(data_grid[i].Columns[j].HeaderText))
                                data_grid[i].Columns[j].HeaderText = columnNames[data_grid[i].Columns[j].HeaderText];

                            data_grid[i].Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                            int colw = data_grid[i].Columns[j].Width;
                            data_grid[i].Columns[j].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                            data_grid[i].Columns[j].Width = colw;
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella Visualizzazione Ordini. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }
            return;
        }

        private void LoaVisOrdOggTable(int id_ordine = 0)
        {
            DataGridView data_grid = dataGridViewVisOrdOggetti;



            string commandText = @"SELECT
									OP.Id as ID,
									PR.nome AS nome,
									PR.codice AS code,
									REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS por,
									REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pos,
									OP.pezzi AS qta,
									REPLACE( printf('%.2f',SUM(OP.prezzo_unitario_sconto * OP.pezzi) ),'.',',')  AS totale,
									strftime('%d/%m/%Y', OP.ETA) AS ETA,
								    PR.descrizione AS descrizione

									FROM " + schemadb + @"[ordine_pezzi] AS OP
								   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
									ON PR.Id = OP.ID_ricambio
								   
									WHERE OP.ID_ordine=@idord 
									GROUP BY OP.Id, PR.nome, PR.codice, OP.prezzo_unitario_originale, OP.prezzo_unitario_sconto, OP.pezzi, OP.ETA, PR.descrizione
									ORDER BY OP.Id;";

            using (SQLiteDataAdapter cmd = new SQLiteDataAdapter(commandText, connection))
            {
                try
                {

                    DataTable ds = new DataTable();
                    cmd.SelectCommand.Parameters.AddWithValue("@idord", id_ordine);

                    cmd.Fill(ds);
                    data_grid.DataSource = ds;

                    Dictionary<string, string> columnNames = new Dictionary<string, string>
                    {
                        { "ID", "ID" },
                        { "nome", "Nome Pezzo" },
                        { "code", "Codice Pezzo" },
                        { "por", "Prezzo Originale" },
                        { "pos", "Prezzo Finale" },
                        { "qta", "Quantità" },
                        { "totale", "Prezzo Totale" },
                        { "ETA", "Data Arrivo" },
                        { "descrizione", "Descrizione" },
                    };
                    int colCount = data_grid.ColumnCount;
                    for (int i = 0; i < colCount; i++)
                    {
                        if (columnNames.ContainsKey(data_grid.Columns[i].HeaderText))
                            data_grid.Columns[i].HeaderText = columnNames[data_grid.Columns[i].HeaderText];

                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                        int colw = data_grid.Columns[i].Width;
                        data_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        data_grid.Columns[i].Width = colw;
                    }

                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante popolamento tabella oggetti ordini. Codice: " + ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void DataGridViewVisualizzaOrdini_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(sender is DataGridView dgv))
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int idOrdine = Convert.ToInt32(row.Cells[0].Value.ToString());

                    UpdateFields("VS", "CA", true);

                    string commandText = @"SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,

												IIF(OP.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OP.costo_spedizione ),'.',','), NULL) AS costo_sped,
												(CASE OP.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,

												CE.nome as clnome,
												CE.stato as clstato,
												CE.provincia as clprov,
												CE.citta as clcitt,

												CR.nome as crnome,
												CR.telefono as crtel,
												CR.mail as crmail,
												strftime('%d/%m/%Y', OP.data_ordine) AS opdo,
												strftime('%d/%m/%Y', OP.data_ETA) AS opde,
												REPLACE( printf('%.2f',OP.totale_ordine ),'.',',')  AS optot,
												REPLACE( printf('%.2f',OP.prezzo_finale+ (CASE OP.gestione_spedizione  
                                                         WHEN 1 THEN   OP.costo_spedizione
                                                         WHEN 2 THEN   OP.costo_spedizione*(1-OP.sconto/100) 
                                                         ELSE 0  
                                                      END) ),'.',',')  AS optotf

									   FROM " + schemadb + @"[ordini_elenco] AS OP
									   LEFT JOIN " + schemadb + @"[offerte_elenco] AS OE
										ON OE.Id = OP.ID_offerta
									   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OE.ID_cliente
									   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OE.ID_riferimento
                                         WHERE OP.ID_offerta IS NOT NULL AND OP.id=@idOrdine

                                        UNION ALL 

                                        SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,

												IIF(OP.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OP.costo_spedizione ),'.',','), NULL) AS costo_sped,
												(CASE OP.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,

												CE.nome as clnome,
												CE.stato as clstato,
												CE.provincia as clprov,
												CE.citta as clcitt,

												CR.nome as crnome,
												CR.telefono as crtel,
												CR.mail as crmail,
												strftime('%d/%m/%Y', OP.data_ordine) AS opdo,
												strftime('%d/%m/%Y', OP.data_ETA) AS opde,
												REPLACE( printf('%.2f',OP.totale_ordine ),'.',',')  AS optot,
												REPLACE( printf('%.2f',OP.prezzo_finale+ (CASE OP.gestione_spedizione  
                                                         WHEN 1 THEN   OP.costo_spedizione
                                                         WHEN 2 THEN   OP.costo_spedizione*(1-OP.sconto/100) 
                                                         ELSE 0  
                                                      END) ),'.',',')  AS optotf

									   FROM " + schemadb + @"[ordini_elenco] AS OP
									   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OP.ID_cliente
									   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OP.ID_riferimento

									   WHERE OP.ID_offerta IS NULL AND OP.id = @idOrdine LIMIT 1;";


                    using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@idOrdine", idOrdine);

                            SQLiteDataReader reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                VisOrdId.Text = Convert.ToString(reader["idord"]);
                                VisOrdStato.Text = Convert.ToString(reader["ordstat"]);
                                VisOrdNumero.Text = Convert.ToString(reader["codice_ordine"]);
                                VisOrdSoc.Text = Convert.ToString(reader["clnome"]);
                                VisOrdSoStato.Text = Convert.ToString(reader["clstato"]);
                                VisOrdSoPro.Text = Convert.ToString(reader["clprov"]);
                                VisOrdSoCitta.Text = Convert.ToString(reader["clcitt"]);
                                VisOrdCont.Text = Convert.ToString(reader["crnome"]);
                                VisOrdContTel.Text = Convert.ToString(reader["crtel"]);
                                VisOrdContMail.Text = Convert.ToString(reader["crmail"]);

                                VisOrdData.Text = Convert.ToString(reader["opdo"]);
                                VisOrdETA.Text = Convert.ToString(reader["opde"]);
                                VisOrdTot.Text = Convert.ToString(reader["optot"]);
                                VisOrdTotFi.Text = Convert.ToString(reader["optotf"]);

                                VisOrdSped.Text = Convert.ToString(reader["costo_sped"]);
                                VisOrdSpedGest.Text = Convert.ToString(reader["spedg"]);
                            }

                            UpdateFields("VS", "E", true);
                            string nordine = VisOrdNumero.Text.Trim();

                            /*ToastNotifierCompat notifier = ToastNotificationManagerCompat.CreateToastNotifier();
                            IReadOnlyList<ScheduledToastNotification> scheduledToasts = notifier.GetScheduledToastNotifications();
                            var toRemove = scheduledToasts.FirstOrDefault(i => i.Group == "ManagerOrdiniLuca" && i.Tag == nordine);
                            if (toRemove != null)
                            {
                                BtStartToastNotification.Enabled = false;
                            }
                            else
                            {
                                BtStartToastNotification.Enabled = true;
                            }*/

                            LoaVisOrdOggTable(idOrdine);
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Errore durante recupero info visualizzaaione ordine. Codice: " + ReturnErorrCode(ex));

                            return;
                        }
                    }
                }
            }
        }

        private void VisOrdChiudi_Click(object sender, EventArgs e)
        {
            UpdateFields("VS", "CA", true);
            UpdateFields("VS", "E", false);
        }

        //APPUNTAMNETI

        private void AddAppointment(string ordRef, string body, DateTime estDate, DateTime orderEstDate)
        {


            if (FindAppointment(ordRef, orderEstDate) == true)
            {
                MessageBox.Show("Evento già presente. Rimuoverlo o aggiornarlo se necessario");
                return;
            }


            try
            {
                Outlook.Application outlookApp = new Outlook.Application();

                Outlook.Folder calendar = outlookApp.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;

                Outlook.Folder personalCalendar = calendar;

                string newCalendarName = settings["calendario"]["nomeCalendario"];

                if (!String.IsNullOrEmpty(newCalendarName) && calendar.Name != newCalendarName)
                {
                    foreach (Outlook.Folder personalCalendarLoop in calendar.Folders)
                    {
                        if (personalCalendarLoop.Name == newCalendarName)
                        {
                            personalCalendar = personalCalendarLoop;
                        }
                    }
                }

                if (CreateCustomCalendar(newCalendarName) == null)
                {
                    return;
                }

                Outlook.AppointmentItem newAppointment = personalCalendar.Items.Add(Outlook.OlItemType.olAppointmentItem) as Outlook.AppointmentItem;
                newAppointment.AllDayEvent = true;
                newAppointment.Start = estDate + TimeSpan.Parse("8:00");
                estDate += TimeSpan.Parse("17:00");
                newAppointment.End = estDate;

                newAppointment.Location = "";
                newAppointment.Body = body;
                newAppointment.Subject = "Reminder Ordine Numero:" + ordRef + "\t" + "##ManaOrdini" + ordRef + "##";

                newAppointment.Display(true);

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Si è verificato un errore durante la creazione dell'appuntamento. Errore: " + ex.Message);
            }
        }

        private bool RemoveAppointment(string ordRef, DateTime ETA)
        {
            if (FindAppointment(ordRef, ETA) == false)
            {
                MessageBox.Show("Evento non presente." + Environment.NewLine + Environment.NewLine + "NOTA: il proramma ricerca solo gli eventi tra la data di creazione ordine e la data di consegna." + Environment.NewLine + " Se l'evento è stato modfiicato a mano oltre queste date, il porgramma non lo troverà.");
                return false;
            }

            string AppCode = "##ManaOrdini" + ordRef + "##";
            Microsoft.Office.Interop.Outlook.Application OlApp = new Microsoft.Office.Interop.Outlook.Application();

            Outlook.Folder AppointmentFolder =
                OlApp.Session.GetDefaultFolder(
                Outlook.OlDefaultFolders.olFolderCalendar)
                as Outlook.Folder;

            Outlook.Folder personalCalendar = AppointmentFolder;

            string newCalendarName = settings["calendario"]["nomeCalendario"];

            if (!String.IsNullOrEmpty(newCalendarName) && AppointmentFolder.Name != newCalendarName)
            {
                foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                {
                    if (personalCalendarLoop.Name == newCalendarName)
                    {
                        personalCalendar = personalCalendarLoop;
                    }
                }
            }

            DateTime start = DateTime.Now.AddDays(-1);

            string filter1 = "[Start] >= '" + start.ToString("g") + "' AND [End] <= '" + ETA.AddDays(1).ToString("g") + "'"; ;

            Outlook.Items calendarItems = personalCalendar.Items.Restrict(filter1);
            calendarItems.IncludeRecurrences = true;
            calendarItems.Sort("[Start]", Type.Missing);

            string filter3 = "@SQL=" + "\"" + "urn:schemas:httpmail:subject" + "\"" + " LIKE '%" + AppCode + "%'";

            Outlook.Items restrictedItems = calendarItems.Restrict(filter3);

            bool found = false;
            int c = 0;
            List<Outlook.AppointmentItem> listaApp = new List<Outlook.AppointmentItem>();

            foreach (Outlook.AppointmentItem apptItem in restrictedItems)
            {
                if (Regex.IsMatch(apptItem.Subject, @"^.*##ManaOrdini\d{1,}##.*$"))
                {
                    listaApp.Add(apptItem);
                    c++;
                }
            }

            MessageBox.Show("Sono stati trovati " + c + " con l'identificativo dell'evento. Verrà chiesta conferma prima dell'eliminazione di ciascun evento.");

            for (int i = 0; i < c; i++)
            {
                DialogResult dialogResult = MessageBox.Show("Cancellare l'appuntamento col nome: '" + listaApp[i].Subject + "' fissato in data: " + (listaApp[i].Start) + "?", "Eliminazione Evento da Calendario", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    listaApp[i].Delete();
                    found = true;
                }
            }

            if (found == true)
            {
                MessageBox.Show("Evento calendario rimosso.");
                return true;
            }

            return false;

        }

        private bool? MoveAppointment(string oldCalendar, string newCalendar)
        {

            if (CreateCustomCalendar(newCalendar) == null)
            {
                return null;
            }

            string AppCode = "##ManaOrdini";

            Microsoft.Office.Interop.Outlook.Application OlApp = new Microsoft.Office.Interop.Outlook.Application();

            Outlook.Folder AppointmentFolder =
                OlApp.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar)
                as Outlook.Folder;

            Outlook.Folder personalCalendar = AppointmentFolder;
            Outlook.Folder newCalendarFolder = AppointmentFolder;

            if (!String.IsNullOrEmpty(oldCalendar))
            {
                foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                {
                    if (personalCalendarLoop.Name == oldCalendar)
                    {
                        personalCalendar = personalCalendarLoop;
                    }
                }
            }

            if (!String.IsNullOrEmpty(newCalendar))
            {
                foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                {
                    if (personalCalendarLoop.Name == newCalendar)
                    {
                        newCalendarFolder = personalCalendarLoop;
                    }
                }
            }

            DateTime start = DateTime.Now.AddDays(-2);

            string filter1 = "[Start] >= '" + start.ToString("g") + "' AND [End] <= '" + DateTime.MaxValue.ToString("g") + "'";

            Outlook.Items calendarItems = personalCalendar.Items.Restrict(filter1);
            calendarItems.IncludeRecurrences = true;
            calendarItems.Sort("[Start]", Type.Missing);

            string filter3 = "@SQL=" + "\"" + "urn:schemas:httpmail:subject" + "\"" + " LIKE '%" + AppCode + "%'";

            Outlook.Items restrictedItems = calendarItems.Restrict(filter3);

            bool error_free = true;
            int c = 0;
            List<Outlook.AppointmentItem> listaApp = new List<Outlook.AppointmentItem>();

            foreach (Outlook.AppointmentItem apptItem in restrictedItems)
            {
                if (Regex.IsMatch(apptItem.Subject, @"^.*##ManaOrdini\d{1,}##.*$"))
                {
                    listaApp.Add(apptItem);
                    c++;
                }
            }

            for (int i = 0; i < c; i++)
            {
                try
                {
                    listaApp[i].Move(newCalendarFolder);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Si è verificato un errore durante la creazione dell'appuntamento. Errore: " + ex.Message);
                    error_free = false;
                }
            }

            return error_free;
        }

        private bool UpdateBodyCalendar(string ordRef, string body, DateTime ETA)
        {
            string AppCode = "##ManaOrdini" + ordRef + "##";
            Microsoft.Office.Interop.Outlook.Application OlApp = new Microsoft.Office.Interop.Outlook.Application();
            bool found = false;

            Outlook.Folder AppointmentFolder =
                OlApp.Session.GetDefaultFolder(
                Outlook.OlDefaultFolders.olFolderCalendar)
                as Outlook.Folder;

            Outlook.Folder personalCalendar = AppointmentFolder;

            string newCalendarName = settings["calendario"]["nomeCalendario"];
            if (!String.IsNullOrEmpty(newCalendarName) && AppointmentFolder.Name != newCalendarName)
            {
                foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                {
                    if (personalCalendarLoop.Name == newCalendarName)
                    {
                        personalCalendar = personalCalendarLoop;
                    }
                }
            }

            DateTime start = DateTime.Now.AddDays(-1);

            string filter1 = "[Start] >= '" + start.ToString("g") + "' AND [End] <= '" + ETA.AddDays(1).ToString("g") + "'"; ;

            Outlook.Items calendarItems = personalCalendar.Items.Restrict(filter1);
            calendarItems.IncludeRecurrences = true;
            calendarItems.Sort("[Start]", Type.Missing);

            string filter3 = "@SQL=" + "\"" + "urn:schemas:httpmail:subject" + "\"" + " LIKE '%" + AppCode + "%'";

            Outlook.Items restrictedItems = calendarItems.Restrict(filter3);

            foreach (Outlook.AppointmentItem apptItem in restrictedItems)
            {
                apptItem.Body = body;
                apptItem.Save();
                found = true;
            }

            return found;
        }

        private void CreaEventoCalendario_Click(object sender, EventArgs e)
        {
            UpdateFields("VS", "E", false);

            string nordine = VisOrdNumero.Text;
            string opde = VisOrdETA.Text;

            DateTime dateAppoint = DateTime.MinValue;

            if (!DateTime.TryParseExact(opde, dateFormat, provider, DateTimeStyles.None, out DateTime dataETAOrdValue))
            {
                MessageBox.Show("Data non valida o vuota");
            }

            if (FindAppointment(nordine, dataETAOrdValue) == false)
            {
                DialogResult dialogResult = MessageBox.Show("Creare l'appuntamento? Una volta creato, sarà necessario salvarlo." + Environment.NewLine + Environment.NewLine
                                                            + "ATTENZIONE: NON rimuovere la stringa finale ##ManaOrdini[numero_ordine]## dal titolo dell'appunatmento. Serve per riconoscere l'evento.", "Creazione Appuntamento Calendario", MessageBoxButtons.YesNo);
                if (dialogResult != DialogResult.Yes)
                {
                    UpdateFields("VS", "E", true);
                    return;
                }

                while (dateAppoint == DateTime.MinValue)
                {
                    string input = Interaction.InputBox("Inserire data in cui ricevere la notifica relativa all'ordine", "Data Notifica Ordine", (dataETAOrdValue).ToString(dateFormat));
                    if (String.ReferenceEquals(input, String.Empty))
                    {
                        MessageBox.Show("Azione Cancellata");
                        UpdateFields("VS", "E", true);
                        return;
                    }

                    if (!DateTime.TryParse(input, out dateAppoint))
                    {
                        MessageBox.Show("Controllare formato data. Impossibile convertire in formato data crretto.");
                        dateAppoint = DateTime.MinValue;
                        continue;
                    }

                    if (DateTime.Compare(dateAppoint, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint, dataETAOrdValue) > 0)
                    {
                        DialogResult confDataLaterOrder = MessageBox.Show("La data scelta va oltre alla data di consegna dell'ordine, continuare?", "Creazione Appuntamento Calendario", MessageBoxButtons.YesNo);
                        if (confDataLaterOrder == DialogResult.No)
                        {
                            dateAppoint = DateTime.MinValue;
                        }
                    }

                    if (DateTime.Compare(dateAppoint, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint, DateTime.Now.Date) < 0)
                    {
                        DialogResult confDataLaterOrder = MessageBox.Show("La data scelta è antecedente alla dato odierna, continuare?", "Creazione Appuntamento Calendario", MessageBoxButtons.YesNo);
                        if (confDataLaterOrder == DialogResult.No)
                        {
                            dateAppoint = DateTime.MinValue;
                        }
                    }
                }

                string body = CreateAppointmentBody(Convert.ToInt32(VisOrdId.Text.Trim()));

                AddAppointment(nordine, body, dateAppoint, dataETAOrdValue);
            }
            else
            {
                MessageBox.Show("Evento già presente. Rimuoverlo o aggiornarlo se necessario");
            }
            UpdateFields("VS", "E", true);
            return;
        }

        private void RimuoviEventoCalendario_Click(object sender, EventArgs e)
        {
            string nordine = VisOrdNumero.Text;
            string ETA = VisOrdETA.Text;

            if (!DateTime.TryParseExact(ETA, dateFormat, provider, DateTimeStyles.None, out DateTime dataETAOrdValue))
            {
                MessageBox.Show("Data non valida o vuota");
                return;
            }
            else
            {
                dataETAOrdValue = dataETAOrdValue.AddDays(1);
            }

            if (FindAppointment(nordine, dataETAOrdValue) == true)
            {
                RemoveAppointment(nordine, dataETAOrdValue);
            }
            else
            {
                MessageBox.Show("Evento non presente." + Environment.NewLine + Environment.NewLine + "NOTA: il proramma ricerca solo gli eventi tra la data di creazione ordine e la data di consegna." + Environment.NewLine + " Se l'evento è stato modfiicato a mano oltre queste date, il porgramma non lo troverà.");
            }
        }

        private void UpdateCalendar(string oldRef, string newRef, int id_ordine, DateTime estDate, bool delete = true)
        {
            bool check = false;
            if (delete == true)
                check = RemoveAppointment(oldRef, estDate.AddDays(1));

            if (check == true || delete == false)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

                DateTime dateAppoint = DateTime.MinValue;

                if (delete == true)
                {
                    while (dateAppoint == DateTime.MinValue)
                    {
                        string input = Interaction.InputBox("Inserire la data per l'appunatmento sul calendario? Una volta creato, sarà necessario salvarlo." + Environment.NewLine + Environment.NewLine
                                                            + "ATTENZIONE: NON rimuovere la stringa finale ##ManaOrdini[numero_ordine]## dal titolo dell'appunatmento. Serve per riconoscere l'evento.", "Modifica Appuntamento Calendario", (estDate).ToString(dateFormat));
                        if (String.ReferenceEquals(input, String.Empty))
                        {
                            MessageBox.Show("Azione Cancellata");
                            UpdateFields("VS", "E", true);
                            return;
                        }

                        if (DateTime.TryParse(input, out dateAppoint))
                        {
                            MessageBox.Show("Controllare formato data. Impossibile convertire in formato data crretto.");
                            dateAppoint = DateTime.MinValue;
                            continue;
                        }

                        if (DateTime.Compare(dateAppoint, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint, estDate) > 0)
                        {
                            DialogResult confDataLaterOrder = MessageBox.Show("La data scelta va oltre alla data di consegna dell'ordine, continuare?", "Creazione Appuntamento Calendario", MessageBoxButtons.YesNo);
                            if (confDataLaterOrder == DialogResult.No)
                            {
                                dateAppoint = DateTime.MinValue;
                            }
                        }

                        if (DateTime.Compare(dateAppoint, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint, DateTime.Now.Date) < 0)
                        {
                            DialogResult confDataLaterOrder = MessageBox.Show("La data scelta è antecedente alla dato odierna, continuare?", "Creazione Appuntamento Calendario", MessageBoxButtons.YesNo);
                            if (confDataLaterOrder == DialogResult.No)
                            {
                                dateAppoint = DateTime.MinValue;
                            }
                        }
                    }
                }

                string body = CreateAppointmentBody(id_ordine);

                if (delete == true)
                    AddAppointment(newRef, body, dateAppoint, estDate);
                else
                    UpdateBodyCalendar(newRef, body, estDate);
                MessageBox.Show("Appuntamento calendario aggiornato");
            }
        }

        private string CreateAppointmentBody(int id_ordine)
        {
            string clnome = "";
            string clstato = "";
            string clprov = "";
            string clcitt = "";
            string crnome = "";
            string crtel = "";
            string crmail = "";
            string optot = "";
            string opde = "";
            string optotf = "";
            string prezzofinaleIclSped = "";


            string commandText = @"SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,
												CE.nome as clnome,
												CE.stato as clstato,
												CE.provincia as clprov,
												CE.citta as clcitt,

												CR.nome as crnome,
												CR.telefono as crtel,
												CR.mail as crmail,
												strftime('%d/%m/%Y', OP.data_ordine) AS opdo,
												strftime('%d/%m/%Y', OP.data_ETA) AS opde,
												REPLACE( printf('%.2f',OP.totale_ordine ),'.',',')  AS optot,
                                                REPLACE(  (
                                                        printf('%.2f',OP.prezzo_finale  ) || 
                                                        ' (' ||    
                                                        printf('%.2f',OP.sconto ) || '%)'),'.',',')  AS optotf,

                                                REPLACE(printf('%.2f',OP.prezzo_finale + (CASE OP.gestione_spedizione  
                                                                                         WHEN 1 THEN   OP.costo_spedizione
                                                                                         WHEN 2 THEN   OP.costo_spedizione*(1-OP.sconto/100) 
                                                                                         ELSE 0  
                                                                                      END) ),'.',',') AS prezzofinaleIclSped
												

									   FROM " + schemadb + @"[ordini_elenco] AS OP
									   LEFT JOIN " + schemadb + @"[offerte_elenco] AS OE
										ON OE.Id = OP.ID_offerta
									   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OE.ID_cliente
									   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OE.ID_riferimento

									   WHERE OP.ID_offerta IS NOT NULL AND OP.id=@idOrdine

                                        UNION ALL
                                        SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,
												CE.nome as clnome,
												CE.stato as clstato,
												CE.provincia as clprov,
												CE.citta as clcitt,

												CR.nome as crnome,
												CR.telefono as crtel,
												CR.mail as crmail,
												strftime('%d/%m/%Y', OP.data_ordine) AS opdo,
												strftime('%d/%m/%Y', OP.data_ETA) AS opde,
												REPLACE( printf('%.2f',OP.totale_ordine ),'.',',')  AS optot,
                                                REPLACE(  (
                                                        printf('%.2f',OP.prezzo_finale  ) || 
                                                        ' (' ||    
                                                        printf('%.2f',OP.sconto ) || '%)'),'.',',')  AS optotf,

                                                REPLACE(printf('%.2f',OP.prezzo_finale + (CASE OP.gestione_spedizione  
                                                                                         WHEN 1 THEN   OP.costo_spedizione
                                                                                         WHEN 2 THEN   OP.costo_spedizione*(1-OP.sconto/100) 
                                                                                         ELSE 0  
                                                                                      END) ),'.',',') AS prezzofinaleIclSped
												

									   FROM " + schemadb + @"[ordini_elenco] AS OP
									   LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OP.ID_cliente
									   LEFT JOIN " + schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OP.ID_riferimento

									   WHERE OP.ID_offerta IS NULL AND OP.id=@idOrdine  

                                        LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idOrdine", id_ordine);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        clnome = Convert.ToString(reader["clnome"]);
                        clstato = Convert.ToString(reader["clstato"]);
                        clprov = Convert.ToString(reader["clprov"]);
                        clcitt = Convert.ToString(reader["clcitt"]);
                        crnome = Convert.ToString(reader["crnome"]);
                        crtel = Convert.ToString(reader["crtel"]);
                        crmail = Convert.ToString(reader["crmail"]);
                        optot = Convert.ToString(reader["optot"]);
                        opde = Convert.ToString(reader["opde"]);
                        optotf = Convert.ToString(reader["optotf"]);
                        prezzofinaleIclSped = Convert.ToString(reader["prezzofinaleIclSped"]);
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante recupero info ordine(appuntamento). Codice: " + ReturnErorrCode(ex));

                    return "";
                }
            }

            string body = "";

            body += clnome + Environment.NewLine;
            body += clstato + " " + clprov + " " + clcitt + Environment.NewLine;
            body += Environment.NewLine;
            body += "Contatto: " + Environment.NewLine + crnome + "\t" + crtel + "\t" + crmail + Environment.NewLine;
            body += Environment.NewLine;
            body += "Data Consegna: " + Environment.NewLine + opde + Environment.NewLine;
            body += Environment.NewLine;
            body += "Totale Finale (Excl Sconti): " + "\t" + optot + Environment.NewLine;
            body += Environment.NewLine;
            body += "Totale Finale (Incl Sconti): " + "\t" + optotf + Environment.NewLine;
            body += Environment.NewLine;
            body += "Totale Finale (Incl. Spedizioni e sconti): " + "\t" + prezzofinaleIclSped + Environment.NewLine;
            body += Environment.NewLine;
            body += Environment.NewLine;
            body += "Elenco Oggetti Ordine";
            body += Environment.NewLine;

            commandText = @"SELECT
									OP.Id as ID,
									PR.nome AS nome,
									PR.codice AS code,
									REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS por,
									REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pos,
									OP.pezzi AS qta,
									REPLACE( printf('%.2f',SUM(OP.prezzo_unitario_sconto * OP.pezzi) ),'.',',')  AS totale,
									strftime('%d/%m/%Y', OP.ETA) AS ETA,
								    PR.descrizione AS descrizione

									FROM " + schemadb + @"[ordine_pezzi] AS OP
								   LEFT JOIN " + schemadb + @"[pezzi_ricambi] AS PR
									ON PR.Id = OP.ID_ricambio
								   
									WHERE OP.ID_ordine=@idord 
									GROUP BY OP.Id, PR.nome, PR.codice, OP.prezzo_unitario_originale, OP.prezzo_unitario_sconto, OP.pezzi, PR.descrizione, PR.descrizione, OP.ETA
									ORDER BY OP.Id;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idord", id_ordine);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        body += "\t" + reader["code"] + "\tPrezzo: " + reader["pos"] + "€\tQuantità: " + reader["qta"] + "\tTotale: " + reader["totale"] + "€";
                        body += Environment.NewLine + "\t" + Convert.ToString(reader["descrizione"]);
                        body += Environment.NewLine + "\t" + "Data Consegna Pezzo:" + "\t" + Convert.ToString(reader["ETA"]);
                        body += Environment.NewLine;
                        body += Environment.NewLine;
                    }
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore durante recupero oggetti ordine(appuntamento). Codice: " + ReturnErorrCode(ex));
                    return "";
                }
            }

            return body;
        }

        private void AggiornaEventoCalendario_Click(object sender, EventArgs e)
        {
            string oldRef = VisOrdNumero.Text;
            string newRef = VisOrdNumero.Text;
            int id_ordine = Convert.ToInt32(VisOrdId.Text);
            DateTime estDate = Convert.ToDateTime(VisOrdETA.Text);

            if (FindAppointment(oldRef, estDate.AddDays(1)) == true)
            {
                UpdateCalendar(oldRef, newRef, id_ordine, estDate, false);
            }
            else
            {
                MessageBox.Show("Evento non presente." + Environment.NewLine + Environment.NewLine + "NOTA: il proramma ricerca solo gli eventi tra la data di creazione ordine e la data di consegna." + Environment.NewLine + " Se l'evento è stato modfiicato a mano oltre queste date, il porgramma non lo troverà.");
            }

        }

        private bool? CreateCustomCalendar(string calName)
        {
            bool needFolder = true;

            if (String.IsNullOrEmpty(calName))
            {
                return false;
            }

            try
            {
                Microsoft.Office.Interop.Outlook.Application OlApp = new Microsoft.Office.Interop.Outlook.Application();
                Outlook.Folder primaryCalendar = OlApp.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;

                foreach (Outlook.Folder personalCalendar in primaryCalendar.Folders)
                {
                    if (personalCalendar.Name == calName)
                    {
                        needFolder = false;
                        return needFolder;
                    }
                }
                if (needFolder)
                {
                    Outlook.Folder personalCalendar = primaryCalendar
                        .Folders.Add(calName,
                            Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;
                }
            }
            catch
            {
                MessageBox.Show("Errore durante verifica necessità cartella OutLook. Impossibile aggiornare informazioni." + Environment.NewLine + "Incrociare dia per evitare danni ai dati");
                return null;
            }

            return needFolder;
        }

        private bool? FindAppointment(string ordRef, DateTime ETA)
        {
            try
            {
                bool found = false;

                string AppCode = "##ManaOrdini" + ordRef + "##";
                Microsoft.Office.Interop.Outlook.Application OlApp = new Microsoft.Office.Interop.Outlook.Application();

                Outlook.Folder AppointmentFolder =
                    OlApp.Session.GetDefaultFolder(
                    Outlook.OlDefaultFolders.olFolderCalendar)
                    as Outlook.Folder;

                Outlook.Folder personalCalendar = AppointmentFolder;

                string newCalendarName = settings["calendario"]["nomeCalendario"];

                if (CreateCustomCalendar(newCalendarName) == null)
                {
                    return null;
                };

                if (!String.IsNullOrEmpty(newCalendarName) && AppointmentFolder.Name != newCalendarName)
                {
                    foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                    {
                        if (personalCalendarLoop.Name == newCalendarName)
                        {
                            personalCalendar = personalCalendarLoop;
                        }
                    }
                }



                DateTime start = DateTime.Now.AddDays(-1);

                string filter1 = "[Start] >= '" + start.ToString("g") + "' AND [End] <= '" + ETA.AddDays(1).ToString("g") + "'"; ;

                Outlook.Items calendarItems = personalCalendar.Items.Restrict(filter1);
                calendarItems.IncludeRecurrences = true;
                calendarItems.Sort("[Start]", Type.Missing);

                string filter3 = "@SQL=" + "\"" + "urn:schemas:httpmail:subject" + "\"" + " LIKE '%" + AppCode + "%'";

                Outlook.Items restrictedItems = calendarItems.Restrict(filter3);

                foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                {
                    found = true;
                    break;
                }

                return found;
            }
            catch
            {
                MessageBox.Show("Errore durante verifica necessità cartella OutLook. Impossibile aggiornare informazioni." + Environment.NewLine + "Incrociare dia per evitare danni ai dati");
                return null;
            }
        }

        //SETTING
        private void SettingSalva_Click(object sender, EventArgs e)
        {
            string nomeCal = Convert.ToString(settingCalendarioNome.Text.Trim());
            bool upCalendar = (bool)settingCalendarioUpdate.Checked;
            string destinatari = Regex.Replace(settingCalendarioDestinatari.Text, @"\s+", "").Trim();

            if (!String.IsNullOrEmpty(destinatari))
            {
                string[] destinatariSubs = destinatari.Split(';');
                var email = new EmailAddressAttribute();

                foreach (string element in destinatariSubs)
                {
                    if (!email.IsValid(element))
                    {
                        MessageBox.Show(element + " non è una email valida.");
                        return;
                    }
                }
            }


            if (settings["calendario"]["nomeCalendario"] != nomeCal)
            {
                DialogResult dialogResult = MessageBox.Show("Stai per cambiare nome al calendario, il software proverà a spostare gli eventi pianificati da oggi in avanti nel nuovo calendario. In caso di errori, gli eventi rimanenti dovranno essere modificati manualmente. Continuare?", "Cambio Nome CAlendario - Aggiornamento Eventi Calendario", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (MoveAppointment(settings["calendario"]["nomeCalendario"], nomeCal) == false)
                    {
                        MessageBox.Show("Errore: Il nome è stato aggiornato, ma non è stato possibile spostare alcuni eventi. Controllare manualemnte");
                    }
                    settings["calendario"]["nomeCalendario"] = nomeCal;
                }
                else
                {
                    settingCalendarioNome.Text = settings["calendario"]["nomeCalendario"];
                }
            }

            settings["calendario"]["aggiornaCalendario"] = Convert.ToString(upCalendar);
            settings["calendario"]["destinatari"] = destinatari;

            UpdateSettingApp();

            MessageBox.Show("Impostazioni Salvate");
        }

        private void UpdateSettingApp()
        {
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(settingFile, json);
        }

        private void ReadSettingApp()
        {
            string json = File.ReadAllText(settingFile);
            settings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

            if (!settings["calendario"].ContainsKey("destinatari"))
            {
                settings["calendario"]["destinatari"] = "";
                UpdateSettingApp();
            }
        }

        //PAGE NAVIGATION
        private void GoToPageGridView(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

                TextBox pageBox = (TextBox)sender;
                TextBox txtboxCurPage;
                Label maxpageLabel;
                int selCurValue = 1;

                switch (Convert.ToString(pageBox.Name))
                {
                    case "DataFornitoriCurPage":
                        maxpageLabel = MaxPageDataFornitori;
                        selCurValue = datiGridViewFornitoriCurPage;
                        txtboxCurPage = DataFornitoriCurPage;
                        break;
                    case "DataClientiCurPage":
                        maxpageLabel = MaxPageDataClienti;
                        selCurValue = datiGridViewClientiCurPage;
                        txtboxCurPage = DataClientiCurPage;
                        break;
                    case "DataPRefCurPage":
                        maxpageLabel = MaxPageDataPRef;
                        selCurValue = datiGridViewPrefCurPage;
                        txtboxCurPage = DataPRefCurPage;
                        break;
                    case "DataMacchinaCurPage":
                        maxpageLabel = MaxPageDataMacchina;
                        selCurValue = datiGridViewMacchineCurPage;
                        txtboxCurPage = DataMacchinaCurPage;
                        break;
                    case "DataCompCurPage":
                        maxpageLabel = MaxPageDataComp;
                        selCurValue = datiGridViewRicambiCurPage;
                        txtboxCurPage = DataCompCurPage;
                        break;
                    case "OffCreaCurPage":
                        maxpageLabel = MaxPageOffCrea;
                        selCurValue = offerteCreaCurPage;
                        txtboxCurPage = OffCreaCurPage;
                        break;
                    case "OrdCurPage":
                        maxpageLabel = MaxPageOrd;
                        selCurValue = OrdiniCurPage;
                        txtboxCurPage = OrdCurPage;
                        break;
                    case "OrdViewCurPage":
                        maxpageLabel = MaxPageOrdView;
                        selCurValue = OrdiniViewCurPage;
                        txtboxCurPage = OrdViewCurPage;
                        break;
                    default:
                        Console.WriteLine("Nome non valido: " + Convert.ToString(pageBox.Name));
                        return;
                }


                int Maxpage = Convert.ToInt32(maxpageLabel.Text);
                string page = pageBox.Text;

                if (!int.TryParse(page, out int value))
                {
                    MessageBox.Show("Numero pagina non valido");
                    txtboxCurPage.Text = Convert.ToString(selCurValue);
                    return;
                }

                int pagev = Convert.ToInt32(pageBox.Text);
                if (pagev < 1)
                {
                    pagev = 1;
                }
                else if (pagev > Maxpage)
                {
                    pagev = Maxpage;
                }
                else if (selCurValue == pagev)
                {
                    return;
                }

                txtboxCurPage.Text = Convert.ToString(pagev);

                switch (Convert.ToString(pageBox.Name))
                {
                    case "DataFornitoriCurPage":
                        datiGridViewFornitoriCurPage = pagev;
                        LoadFornitoriTable(pagev);
                        break;
                    case "DataClientiCurPage":
                        datiGridViewClientiCurPage = pagev;
                        LoadClientiTable(pagev);
                        break;
                    case "DataPRefCurPage":
                        datiGridViewPrefCurPage = pagev;
                        LoadPrefTable(pagev);
                        break;
                    case "DataMacchinaCurPage":
                        datiGridViewMacchineCurPage = pagev;
                        LoadMacchinaTable(pagev);
                        break;
                    case "DataCompCurPage":
                        datiGridViewRicambiCurPage = pagev;
                        LoadCompTable(pagev);
                        break;
                    case "OffCreaCurPage":
                        offerteCreaCurPage = pagev;
                        LoadOfferteCreaTable(pagev);
                        break;
                    case "OrdCurPage":
                        OrdiniCurPage = pagev;
                        LoadOrdiniTable(pagev);
                        break;
                    case "OrdViewCurPage":
                        OrdiniViewCurPage = pagev;
                        LoadVisualizzaOrdiniTable(pagev);
                        break;
                }
            }
        }

        private void GoToPrevPageGridView(object sender, EventArgs e)
        {
            Button buttonP = (Button)sender;
            TextBox txtboxCurPage;
            int selCurValue;

            switch (Convert.ToString(buttonP.Name))
            {
                case "DatiFornitoriPrvPage":
                    selCurValue = datiGridViewFornitoriCurPage;
                    txtboxCurPage = DataFornitoriCurPage;
                    break;
                case "DatiClientiPrvPage":
                    selCurValue = datiGridViewClientiCurPage;
                    txtboxCurPage = DataClientiCurPage;
                    break;
                case "DatiPRefPrvPage":
                    selCurValue = datiGridViewPrefCurPage;
                    txtboxCurPage = DataPRefCurPage;
                    break;
                case "DatiMacchinaPrvPage":
                    selCurValue = datiGridViewMacchineCurPage;
                    txtboxCurPage = DataMacchinaCurPage;
                    break;
                case "DatiCompPrvPage":
                    selCurValue = datiGridViewRicambiCurPage;
                    txtboxCurPage = DataCompCurPage;
                    break;
                case "OffCreaPrvPage":
                    selCurValue = offerteCreaCurPage;
                    txtboxCurPage = OffCreaCurPage;
                    break;
                case "OrdPrvPage":
                    selCurValue = OrdiniCurPage;
                    txtboxCurPage = OrdCurPage;
                    break;
                case "OrdViewPrvPage":
                    selCurValue = OrdiniViewCurPage;
                    txtboxCurPage = OrdViewCurPage;
                    break;
                default:
                    Console.WriteLine("Nome non valido: " + Convert.ToString(buttonP.Name));
                    return;
            }

            if (selCurValue > 1)
            {
                selCurValue -= 1;
            }
            else
            {
                return;
            }

            txtboxCurPage.Text = Convert.ToString(selCurValue);

            switch (Convert.ToString(buttonP.Name))
            {
                case "DatiFornitoriPrvPage":
                    datiGridViewFornitoriCurPage = selCurValue;
                    LoadFornitoriTable(selCurValue);
                    break;
                case "DatiClientiPrvPage":
                    datiGridViewClientiCurPage = selCurValue;
                    LoadClientiTable(selCurValue);
                    break;
                case "DatiPRefPrvPage":
                    datiGridViewPrefCurPage = selCurValue;
                    LoadPrefTable(selCurValue);
                    break;
                case "DatiMacchinaPrvPage":
                    datiGridViewMacchineCurPage = selCurValue;
                    LoadMacchinaTable(selCurValue);
                    break;
                case "DatiCompPrvPage":
                    datiGridViewRicambiCurPage = selCurValue;
                    LoadCompTable(selCurValue);
                    break;
                case "OffCreaPrvPage":
                    offerteCreaCurPage = selCurValue;
                    LoadOfferteCreaTable(selCurValue);
                    break;
                case "OrdPrvPage":
                    OrdiniCurPage = selCurValue;
                    LoadOrdiniTable(selCurValue);
                    break;
                case "OrdViewPrvPage":
                    OrdiniViewCurPage = selCurValue;
                    LoadVisualizzaOrdiniTable(selCurValue);
                    break;
            }
        }

        private void GoToNextPageGridView(object sender, EventArgs e)
        {
            Button buttonP = (Button)sender;
            TextBox txtboxCurPage;
            Label maxpageLabel;
            int selCurValue;

            switch (buttonP.Name)
            {
                case "DatiFornitoriNxtPage":
                    maxpageLabel = MaxPageDataFornitori;
                    selCurValue = datiGridViewFornitoriCurPage;
                    txtboxCurPage = DataFornitoriCurPage;
                    break;
                case "DatiClientiNxtPage":
                    maxpageLabel = MaxPageDataClienti;
                    selCurValue = datiGridViewClientiCurPage;
                    txtboxCurPage = DataClientiCurPage;
                    break;
                case "DatiPRefNxtPage":
                    maxpageLabel = MaxPageDataPRef;
                    selCurValue = datiGridViewPrefCurPage;
                    txtboxCurPage = DataPRefCurPage;
                    break;
                case "DatiMacchinaNxtPage":
                    maxpageLabel = MaxPageDataMacchina;
                    selCurValue = datiGridViewMacchineCurPage;
                    txtboxCurPage = DataMacchinaCurPage;
                    break;
                case "DatiCompNxtPage":
                    maxpageLabel = MaxPageDataComp;
                    selCurValue = datiGridViewRicambiCurPage;
                    txtboxCurPage = DataCompCurPage;
                    break;
                case "OffCreaNxtPage":
                    maxpageLabel = MaxPageOffCrea;
                    selCurValue = offerteCreaCurPage;
                    txtboxCurPage = OffCreaCurPage;
                    break;
                case "OrdNxtPage":
                    maxpageLabel = MaxPageOrd;
                    selCurValue = OrdiniCurPage;
                    txtboxCurPage = OrdCurPage;
                    break;
                case "OrdViewNxtPage":
                    maxpageLabel = MaxPageOrdView;
                    selCurValue = OrdiniViewCurPage;
                    txtboxCurPage = OrdViewCurPage;
                    break;
                default:
                    Console.WriteLine("Nome non valido: " + Convert.ToString(buttonP.Name));
                    return;
            }


            int maxPage = Convert.ToInt32(maxpageLabel.Text);
            if (selCurValue < maxPage)
            {
                selCurValue += 1;
            }
            else
            {
                return;
            }

            txtboxCurPage.Text = Convert.ToString(selCurValue);

            switch (Convert.ToString(buttonP.Name))
            {
                case "DatiFornitoriNxtPage":
                    datiGridViewFornitoriCurPage = selCurValue;
                    LoadFornitoriTable(selCurValue);
                    break;
                case "DatiClientiNxtPage":
                    datiGridViewClientiCurPage = selCurValue;
                    LoadClientiTable(selCurValue);
                    break;
                case "DatiPRefNxtPage":
                    datiGridViewPrefCurPage = selCurValue;
                    LoadPrefTable(selCurValue);
                    break;
                case "DatiMacchinaNxtPage":
                    datiGridViewMacchineCurPage = selCurValue;
                    LoadMacchinaTable(selCurValue);
                    break;
                case "DatiCompNxtPage":
                    datiGridViewRicambiCurPage = selCurValue;
                    LoadCompTable(selCurValue);
                    break;
                case "OffCreaNxtPage":
                    offerteCreaCurPage = selCurValue;
                    LoadOfferteCreaTable(selCurValue);
                    break;
                case "OrdNxtPage":
                    OrdiniCurPage = selCurValue;
                    LoadOrdiniTable(selCurValue);
                    break;
                case "OrdViewNxtPage":
                    OrdiniViewCurPage = selCurValue;
                    LoadVisualizzaOrdiniTable(selCurValue);
                    break;
            }
        }

        //POPULTAE FUNCTIONS
        private void Populate_combobox_machine(ComboBox[] nome_ctr, int idcl = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string commandText = "SELECT Id,modello,seriale FROM " + schemadb + @"[clienti_macchine] ORDER BY Id ASC;";

            if (idcl > 0)
                commandText = "SELECT Id,modello,seriale FROM " + schemadb + @"[clienti_macchine] WHERE ID_cliente=@idcl ORDER BY Id ASC;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {

                try
                {

                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} ({2})", reader["Id"], reader["modello"], reader["seriale"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_machine. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Populate_combobox_ricambi(ComboBox[] nome_ctr, int idmc = 0, bool offpezziSel = false)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };
            string extenQuery = "";
            int idoff = 0;
            string filter = "";

            if (offpezziSel == true)
            {
                idoff = SelOffCrea.SelectedItem.GetHashCode();
                extenQuery += @" AND Id NOT IN (
                                                    SELECT ID_ricambio FROM " + schemadb + @"[offerte_pezzi] WHERE ID_offerta=@idoff 

                                                    UNION 

                                                    SELECT OP.ID_ricambio 
                                                        FROM " + schemadb + @"[ordine_pezzi] AS OP 
                                                        INNER JOIN " + schemadb + @"[ordini_elenco] AS OE 
                                                            ON OE.Id =  OP.ID_ordine
                                                    WHERE OP.Outside_Offer=true AND OE.ID_offerta 
                                                ) 
                                ";
            }

            if (!String.IsNullOrEmpty(AddOffCreaOggettoPezzoFiltro_Text) || !String.IsNullOrEmpty(FieldOrdOggPezzoFiltro_Text))
            {
                filter = "%" + AddOffCreaOggettoPezzoFiltro_Text + "%";
                extenQuery += " AND ( Id LIKE @filterstr OR nome LIKE @filterstr OR codice LIKE @filterstr)  ";
            }

            string commandText = @"SELECT Id,nome,codice FROM " + schemadb + @"[pezzi_ricambi] WHERE ID_macchina IS NULL " + extenQuery + " ORDER BY Id ASC;";

            if (idmc > 0)
                commandText = "SELECT Id,nome,codice FROM " + schemadb + @"[pezzi_ricambi] WHERE (ID_macchina=@idmc)  " + extenQuery + " ORDER BY Id ASC;";



            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idmc", idmc);
                    cmd.Parameters.AddWithValue("@idoff", idoff);
                    cmd.Parameters.AddWithValue("@filterstr", filter);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} ({2})", reader["Id"], reader["codice"], reader["nome"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_ricambi. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            //Setup data binding
            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }

        }

        private void Populate_combobox_ricambi_ordine(ComboBox[] nome_ctr, int idmc = 0, bool offpezziSel = false)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };
            string extenQuery = "";
            int idOrd = 0;
            string filter = "";

            if (offpezziSel == true)
            {
                idOrd = ComboSelOrd.SelectedItem.GetHashCode();
                extenQuery += " AND Id NOT IN (SELECT ID_ricambio FROM " + schemadb + @"[ordine_pezzi] WHERE ID_ordine=@idoff) ";
            }

            if (!String.IsNullOrEmpty(FieldOrdOggPezzoFiltro_Text))
            {
                filter = "%" + FieldOrdOggPezzoFiltro_Text + "%";
                extenQuery += " AND ( Id LIKE @filterstr OR nome LIKE @filterstr OR codice LIKE @filterstr)  ";
            }

            string commandText = @"SELECT Id,nome,codice FROM " + schemadb + @"[pezzi_ricambi] WHERE ID_macchina IS NULL " + extenQuery + " ORDER BY Id ASC;";

            if (idmc > 0)
                commandText = "SELECT Id,nome,codice FROM " + schemadb + @"[pezzi_ricambi] WHERE (ID_macchina=@idmc)  " + extenQuery + " ORDER BY Id ASC;";



            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    cmd.Parameters.AddWithValue("@idmc", idmc);
                    cmd.Parameters.AddWithValue("@idoff", idOrd);
                    cmd.Parameters.AddWithValue("@filterstr", filter);
                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} ({2})", reader["Id"], reader["codice"], reader["nome"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_ricambi. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            //Setup data binding
            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }

        }

        private void Populate_combobox_clienti(ComboBox[] nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };


            string commandText = "SELECT Id,nome,stato, provincia, citta FROM " + schemadb + @"[clienti_elenco] ORDER BY Id ASC;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {

                try
                {

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} ({2} - {3} - {4})", reader["Id"], reader["nome"], reader["stato"], reader["provincia"], reader["citta"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_clienti. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Populate_combobox_fornitore(ComboBox[] nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string commandText = "SELECT Id,nome FROM " + schemadb + @"[fornitori] ORDER BY Id ASC;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {

                try
                {

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1}", reader["Id"], reader["nome"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_fornitore. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }


        }

        private void Populate_combobox_pref(ComboBox nome_ctr, int ID_cliente = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string commandText = "SELECT Id,nome FROM " + schemadb + @"[clienti_riferimenti] ORDER BY Id ASC;";

            if (ID_cliente > 0)
            {
                commandText = "SELECT Id,nome FROM " + schemadb + @"[clienti_riferimenti] WHERE ID_clienti=@idcl ORDER BY Id ASC;";
            }

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    cmd.Parameters.AddWithValue("@idcl", ID_cliente);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1}", reader["Id"], reader["nome"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_pref. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            //Setup data binding
            nome_ctr.DataSource = null;
            nome_ctr.BindingContext = new BindingContext();
            nome_ctr.DataSource = dataSource;
            nome_ctr.Refresh();
            nome_ctr.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void Populate_combobox_offerte_crea(ComboBox[] nome_ctr, int idcl = 0)
        {
            string queryExtra = "";
            if (idcl > 0)
            {
                queryExtra = " AND ID_cliente=@idcl ";
            }
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string commandText = @"SELECT 

									OE.Id AS id,
									OE.codice_offerta AS noff,
									  (CE.nome  || ' (' ||  CE.stato || ' - ' || CE.provincia || ' - ' || CE.citta || ')') AS cliente

									FROM " + schemadb + @"[offerte_elenco] AS OE
									LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE
										ON CE.Id=OE.[ID_cliente]
									WHERE OE.stato=0 " + queryExtra + @" 
                                    ORDER BY OE.Id DESC;";
            bool presres = false;

            int countResIDCL = 0;


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    SQLiteDataReader reader = cmd.ExecuteReader();


                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} [{2}]", reader["id"], reader["noff"], reader["cliente"]), Value = Convert.ToInt32(reader["Id"]) });
                        presres = true;
                        countResIDCL++;
                    }

                    reader.Close();

                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_offerte_crea. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();

            for (int i = 0; i < count; i++)
            {
                if (presres == true)
                    nome_ctr[i].Enabled = true;

                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Populate_combobox_dummy(ComboBox nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            nome_ctr.DataSource = null;
            nome_ctr.BindingContext = new BindingContext();
            nome_ctr.DataSource = dataSource;
            nome_ctr.Refresh();
            nome_ctr.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void Populate_combobox_statoOfferte(ComboBox[] nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 },
                new ComboBoxList() { Name = "APERTA", Value = 0 },
                new ComboBoxList() { Name = "ORDINATA", Value = 1 },
                new ComboBoxList() { Name = "ANNULLATA", Value = 2 }
            };

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Populate_combobox_statoOrdini(ComboBox[] nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 },
                new ComboBoxList() { Name = "APERTO", Value = 0 },
                new ComboBoxList() { Name = "CHIUSO", Value = 1 }
            };

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                nome_ctr[i].DataSource = null;
                nome_ctr[i].BindingContext = new BindingContext();
                nome_ctr[i].DataSource = dataSource;
                nome_ctr[i].Refresh();
                nome_ctr[i].DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Populate_combobox_ordini_crea_offerta(ComboBox nome_ctr, int idcl = 0, bool transformed = true, int codice = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };
            string commandText;


            if (transformed)
                commandText = @"SELECT Id AS id, codice_offerta AS codice FROM " + schemadb + @"[offerte_elenco] WHERE ID_cliente=@idcl AND trasformato_ordine=0 AND stato=1;";
            else
                commandText = @"SELECT Id AS id, codice_offerta AS codice FROM " + schemadb + @"[offerte_elenco] WHERE Id=@idof;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@idof", codice);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    bool presres = false;
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1}", reader["id"], reader["codice"]), Value = Convert.ToInt32(reader["Id"]) });
                        presres = true;
                    }

                    reader.Close();
                    if (presres == true)
                        nome_ctr.Enabled = true;
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_ordini_crea. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            nome_ctr.DataSource = null;
            nome_ctr.BindingContext = new BindingContext();
            nome_ctr.DataSource = dataSource;
            nome_ctr.Refresh();
            nome_ctr.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void Populate_combobox_ordini(ComboBox nome_ctr, int idcl = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string queryExtra = "";
            if (idcl > 0)
            {
                queryExtra = " AND OFE.ID_cliente=@idcl ";
            }


            string commandText = @"SELECT 
										OE.Id AS id,
										OE.codice_ordine AS noff,
										 (CE.nome || ' (' || CE.stato || '-' || CE.provincia || '-' || CE.citta || ')') AS Cliente
									FROM " + schemadb + @"[ordini_elenco] AS OE 
									LEFT JOIN " + schemadb + @"[offerte_elenco] AS OFE 
										ON OFE.Id = OE.[ID_offerta] 
									LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = OFE.[ID_cliente]
									WHERE OE.ID_offerta IS NOT NULL AND OE.stato=0 " + queryExtra + @" 

                                    UNION ALL 

                                    SELECT 
										OE.Id AS id,
										OE.codice_ordine AS noff,
										 (CE.nome || ' (' || CE.stato || '-' || CE.provincia || '-' || CE.citta || ')') AS Cliente
									FROM " + schemadb + @"[ordini_elenco] AS OE
									LEFT JOIN " + schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = OE.ID_cliente
									WHERE OE.ID_offerta IS NULL AND OE.stato=0 AND OE.ID_cliente=@idcl
                                    ORDER BY OE.Id DESC;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {

                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    bool presres = false;
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} [{2}]", reader["id"], reader["noff"], reader["Cliente"]), Value = Convert.ToInt32(reader["Id"]) });
                        presres = true;
                    }

                    reader.Close();
                    if (presres == true)
                        nome_ctr.Enabled = true;
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show("Errore populate_combobox_ordini. Codice: " + ReturnErorrCode(ex));


                    return;
                }
            }

            //Setup data binding
            nome_ctr.DataSource = null;
            nome_ctr.BindingContext = new BindingContext();
            nome_ctr.DataSource = dataSource;
            nome_ctr.Refresh();
            nome_ctr.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void Populate_combobox_FieldOrdSpedGestione(ComboBox nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 },
                new ComboBoxList() { Name = "Exlude from Tot.", Value = 0 },
                new ComboBoxList() { Name = "Add total & No Discount", Value = 1 },
                new ComboBoxList() { Name = "Add Tot with Discount", Value = 2 }
            };

            //Setup data binding
            nome_ctr.DataSource = null;
            nome_ctr.BindingContext = new BindingContext();
            nome_ctr.DataSource = dataSource;
            nome_ctr.Refresh();
            nome_ctr.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void ClearDataGridView(DataGridView nome_ctr)
        {
            nome_ctr.DataSource = null;
            nome_ctr.Rows.Clear();
        }

        public object ReturnStato(object stat)
        {
            Dictionary<string, int> stati = new Dictionary<string, int>
            {
                { "APERTA", 0 },
                { "ORDINATA", 1 },
                { "ANNULLATA", 2 },
                { "APERTO", 0 },
                { "CHIUSO", 1 }
            };

            if (stat.GetType() == typeof(string))
            {
                string key = (string)stat;
                return stati[key];
            }
            else if (stat.GetType() == typeof(int))
            {
                foreach (KeyValuePair<string, int> entry in stati)
                {
                    if (entry.Value == (int)stat)
                        return entry.Key;
                }
            }

            return 0;
        }

        public static List<string> CountryList()
        {
            List<string> CultureList = new List<string>();
            CultureInfo[] getCultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            CultureList.Add("");
            foreach (CultureInfo getCulture in getCultureInfo)
            {
                RegionInfo GetRegionInfo = new RegionInfo(getCulture.LCID);
                if (!(CultureList.Contains(GetRegionInfo.EnglishName)))
                {
                    CultureList.Add(GetRegionInfo.EnglishName);
                }
            }
            CultureList.Sort();
            return CultureList;
        }

        //UPDATE FUNCTIONS

        private void UpdateFornitori(int page = 1)
        {
            ComboBox[] nomi_ctr = { AddDatiCompSupplier, ChangeDatiCompSupplier };

            Populate_combobox_fornitore(nomi_ctr);

            string curPage = DataFornitoriCurPage.Text.Trim();
            if (!int.TryParse(curPage, out page))
                page = 1;


            LoadFornitoriTable(page);
        }

        private void UpdateMacchine(int page = 1)
        {

            ComboBox[] nomi_ctr = { AddDatiCompMachine, ChangeDatiCompMachine, FieldOrdOggMach };

            string curPage = DataMacchinaCurPage.Text.Trim();
            if (!int.TryParse(curPage, out page))
                page = 1;

            Populate_combobox_machine(nomi_ctr);
            LoadMacchinaTable(page);
        }

        private void UpdateClienti(int page = 1)
        {

            ComboBox[] nomi_ctr = {
                    AddDatiMacchinaCliente,
                    AddDatiPRefCliente,
                    ChangeDatiPRefClienti,
                    ChangeDatiMacchinaCliente,
                    AddDatiCompCliente,
                    ChangeDatiCompCliente,
                    AddOffCreaCliente,
                    ComboBoxOrdCliente,
                    SelOffCreaCl,
                    ComboSelOrdCl,
                    OffCreaFiltroCliente,
                    DataGridViewFilterCliente,
                    dataGridViewMacchina_Filtro_Cliente
            };

            string curPage = DataClientiCurPage.Text.Trim();
            if (!int.TryParse(curPage, out page))
                page = 1;

            Populate_combobox_clienti(nomi_ctr);

            LoadClientiTable(page);
        }

        private void UpdateCountryList()
        {
            AddDatiClienteStato.DataSource = CountryList();
            AddDatiClienteStato.SelectedItem = "Italy";
            AddDatiClienteStato.DropDownStyle = ComboBoxStyle.DropDownList;
            ChangeDatiClientiStato.DataSource = CountryList();
            ChangeDatiClientiStato.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void UpdatePRef(int page = 1)
        {

            ComboBox[] nome_ctr ={
                AddOffCreaPRef,
                    ComboBoxOrdContatto
            };

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                if (nome_ctr[i].DataSource != null)
                {
                    int curItemValue = nome_ctr[i].SelectedItem.GetHashCode();
                    if (curItemValue > 0)
                    {
                        Populate_combobox_pref(nome_ctr[i], curItemValue);
                    }
                }
                else
                {
                    Populate_combobox_dummy(nome_ctr[i]);
                }
            }

            string curPage = DataPRefCurPage.Text.Trim();
            if (!int.TryParse(curPage, out page))
                page = 1;

            LoadPrefTable(page);
        }

        private void UpdateRicambi(int page = 1)
        {
            string curPage = DataCompCurPage.Text.Trim();
            if (!int.TryParse(curPage, out page))
                page = 1;

            LoadCompTable(page);
            if (AddOffCreaOggettoRica.Enabled == true && AddOffCreaOggettoRica.SelectedIndex > -1)
            {
                int idmacchina = AddOffCreaOggettoMach.SelectedItem.GetHashCode();
                Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, idmacchina);
                Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, idmacchina);
            }
        }

        private void UpdateOfferteCrea(int page = 0, bool EditedList = true)
        {
            if (EditedList == true)
            {
                ComboBox[] nomi_ctr = {
                SelOffCrea,
                ComboBoxOrdOfferta
                };

                Populate_combobox_offerte_crea(nomi_ctr);
            }

            if (page == 0)
            {
                string curPage = OffCreaCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            LoadOfferteCreaTable(page);

            ClearDataGridView(dataGridViewOffCreaOggetti);

            ComboBoxOrdCliente_SelectedIndexChanged(this, EventArgs.Empty);
            SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private void UpdateFixedComboValue()
        {

            ComboBox[] nomi_ctr = new ComboBox[] {
                AddOffCreaStato,
                OffCreaFiltroStato
                };

            Populate_combobox_statoOfferte(nomi_ctr);
        }

        private void UpdateOrdiniStato()
        {

            ComboBox[] nomi_ctr = {
                DataGridViewOrdStato,
                FieldOrdStato
                };

            Populate_combobox_statoOrdini(nomi_ctr);
        }

        private void UpdateOrdini(int page = 0)
        {
            if (page == 0)
            {
                string curPage = OrdCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            LoadOrdiniTable(page);
            LoadVisualizzaOrdiniTable(OrdiniViewCurPage);
            Populate_combobox_ordini(ComboSelOrd);

            UpdateFields("VS", "CA", true);
            UpdateFields("VS", "E", false);

            UpdateFields("OCR", "CA", true);
            UpdateFields("OCR", "E", true);

            ComboBoxOrdCliente.SelectedIndex = 0;
            ComboBoxOrdCliente.Enabled = true;
        }

        private void UpdateSetting()
        {
            settingCalendarioNome.Text = settings["calendario"]["nomeCalendario"];
            settingCalendarioDestinatari.Text = settings["calendario"]["destinatari"];
            settingCalendarioUpdate.Checked = Boolean.Parse(settings["calendario"]["aggiornaCalendario"]);
        }

        private void UpdateFields(string tabC, string action, bool stat, bool clean = true)
        {
            DateTime today = DateTime.Today;
            string t = today.ToString("dd/MM/yyyy");
            switch (tabC)
            {
                case "P":
                    switch (action)
                    {
                        case "E":
                            ChangeDatiPRefNome.Enabled = stat;
                            ChangeDatiPRefClienti.Enabled = stat;
                            ChangeDatiPRefTelefono.Enabled = stat;
                            ChangeDatiPRefMail.Enabled = stat;

                            BtCancChangesPRef.Enabled = stat;
                            BtSaveChangesPRef.Enabled = stat;
                            BtDelChangesPRef.Enabled = stat;
                            return;
                        case "A":
                            AddDatiPRefNome.Enabled = stat;
                            AddDatiPRefCliente.Enabled = stat;
                            AddDatiPRefTel.Enabled = stat;
                            AddDatiPRefMail.Enabled = stat;

                            BtAddPersonaRef.Enabled = stat;
                            return;
                        case "CA":
                            AddDatiPRefNome.Text = "";
                            AddDatiPRefCliente.SelectedIndex = 0;
                            AddDatiPRefTel.Text = "";
                            AddDatiPRefMail.Text = "";
                            return;
                        case "CE":
                            ChangeDatiPRefNome.Text = "";
                            ChangeDatiPRefClienti.SelectedIndex = 0;
                            ChangeDatiPRefTelefono.Text = "";
                            ChangeDatiPRefMail.Text = "";
                            ChangeDatiPRefID.Text = "";
                            return;
                    }
                    return;
                case "F":
                    switch (action)
                    {
                        case "E":
                            ChangeDatiFornitoreNome.Enabled = stat;

                            BtSaveChangesFornitore.Enabled = stat;
                            BtCancChangesFornitore.Enabled = stat;
                            BtDelChangesFornitore.Enabled = stat;

                            return;
                        case "A":
                            AddDatiFornitoreNome.Enabled = stat;

                            BtAddFornitore.Enabled = stat;
                            return;
                        case "CA":
                            ChangeDatiFornitoreNome.Text = "";
                            return;
                        case "CE":
                            ChangeDatiFornitoreNome.Text = "";
                            ChangeDatiFornitoreID.Text = "";
                            return;
                    }
                    return;
                case "C":
                    switch (action)
                    {
                        case "E":
                            ChangeDatiClientiNome.Enabled = stat;
                            ChangeDatiClientiStato.Enabled = stat;
                            ChangeDatiClientiCitta.Enabled = stat;
                            ChangeDatiClientiProvincia.Enabled = stat;

                            BtCancChangesClienti.Enabled = stat;
                            BtSaveChangesClienti.Enabled = stat;
                            BtDelChangesClienti.Enabled = stat;
                            return;
                        case "A":
                            AddDatiClienteNome.Enabled = stat;
                            AddDatiClienteStato.Enabled = stat;
                            AddDatiClienteCitta.Enabled = stat;
                            AddDatiClienteProv.Enabled = stat;

                            BtAddCliente.Enabled = stat;
                            return;
                        case "CA":
                            AddDatiClienteNome.Text = "";
                            AddDatiClienteStato.SelectedIndex = ChangeDatiClientiStato.FindString("Italy");
                            AddDatiClienteCitta.Text = "";
                            AddDatiClienteProv.Text = "";
                            return;
                        case "CE":
                            ChangeDatiClientiNome.Text = "";
                            ChangeDatiClientiStato.SelectedIndex = 0;
                            ChangeDatiClientiCitta.Text = "";
                            ChangeDatiClientiProvincia.Text = "";
                            ChangeDatiClientiID.Text = "";
                            return;
                    }
                    return;
                case "M":
                    switch (action)
                    {
                        case "E":
                            ChangeDatiMacchinaNome.Enabled = stat;
                            ChangeDatiMacchinaSeriale.Enabled = stat;
                            ChangeDatiMacchinaCodice.Enabled = stat;
                            ChangeDatiMacchinaCliente.Enabled = stat;

                            BtCancChangesMacchina.Enabled = stat;
                            BtSaveChangesMacchina.Enabled = stat;
                            BtDelChangesMacchina.Enabled = stat;
                            return;
                        case "A":
                            AddDatiMacchinaNome.Enabled = stat;
                            AddDatiMacchinaSeriale.Enabled = stat;
                            AddDatiMacchinaCodice.Enabled = stat;
                            AddDatiMacchinaCliente.Enabled = stat;

                            BtAddMachine.Enabled = stat;
                            return;
                        case "CA":
                            AddDatiMacchinaNome.Text = "";
                            AddDatiMacchinaSeriale.Text = "";
                            AddDatiMacchinaCodice.Text = "";
                            AddDatiMacchinaCliente.SelectedIndex = 0;
                            return;
                        case "CE":
                            ChangeDatiMacchinaNome.Text = "";
                            ChangeDatiMacchinaSeriale.Text = "";
                            ChangeDatiMacchinaCodice.Text = "";
                            ChangeDatiMacchinaID.Text = "";
                            ChangeDatiMacchinaCliente.SelectedIndex = 0;
                            return;
                    }
                    return;
                case "R":
                    switch (action)
                    {
                        case "E":
                            ChangeDatiCompNome.Enabled = stat;
                            ChangeDatiCompCode.Enabled = stat;
                            ChangeDatiCompPrice.Enabled = stat;
                            ChangeDatiCompCliente.Enabled = stat;
                            ChangeDatiCompSupplier.Enabled = stat;
                            ChangeDatiCompMachine.Enabled = stat;
                            ChangeDatiCompDesc.Enabled = stat;

                            BtCancChangesComp.Enabled = stat;
                            BtSaveChangesComp.Enabled = stat;
                            BtDelChangesComp.Enabled = stat;
                            return;
                        case "A":
                            AddDatiCompNome.Enabled = stat;
                            AddDatiCompCode.Enabled = stat;
                            AddDatiCompPrice.Enabled = stat;
                            AddDatiCompCliente.Enabled = stat;
                            AddDatiCompSupplier.Enabled = stat;
                            AddDatiCompMachine.Enabled = stat;
                            AddDatiCompDesc.Enabled = stat;

                            BtAddMachine.Enabled = stat;
                            return;
                        case "CA":
                            AddDatiCompNome.Text = "";
                            AddDatiCompCode.Text = "";
                            AddDatiCompPrice.Text = "";
                            AddDatiCompSupplier.Text = "";
                            AddDatiCompDesc.Text = "";
                            AddDatiCompCliente.SelectedIndex = 0;
                            AddDatiCompMachine.SelectedIndex = 0;
                            return;
                        case "CE":
                            ChangeDatiCompNome.Text = "";
                            ChangeDatiCompCode.Text = "";
                            ChangeDatiCompPrice.Text = "";
                            ChangeDatiCompSupplier.Text = "";
                            ChangeDatiCompDesc.Text = "";
                            ChangeDatiCompCliente.SelectedIndex = 0;
                            ChangeDatiCompMachine.SelectedIndex = 0;
                            return;
                    }
                    return;
                case "OC":
                    switch (action)
                    {
                        case "E":

                            BtCancChangesOff.Enabled = stat;
                            BtSaveChangesOff.Enabled = stat;
                            BtDelChangesOff.Enabled = stat;

                            BtCreaOfferta.Enabled = stat != true;

                            return;

                        case "A":

                            AddOffCreaNOff.Enabled = stat;
                            AddOffCreaData.Enabled = stat;
                            AddOffCreaCliente.Enabled = stat;

                            if (AddOffCreaCliente.SelectedItem.GetHashCode() > 0)
                            {
                                AddOffCreaPRef.Enabled = stat;
                            }

                            AddOffCreaStato.Enabled = stat;

                            BtCreaOfferta.Enabled = stat;
                            return;
                        case "CA":
                            AddOffCreaNOff.Text = "";
                            AddOffCreaData.Text = t;
                            AddOffCreaCliente.SelectedIndex = 0;
                            AddOffCreaStato.SelectedIndex = 0;
                            AddOffCreaSpedizioneGest.SelectedIndex = 0;
                            AddOffCreaId.Text = "";
                            AddOffCreaSpedizione.Text = "";
                            return;
                        case "CE":

                            return;
                    }
                    return;
                case "OAO":
                    switch (action)
                    {
                        case "E":

                            BtCancChangesOffOgg.Enabled = stat;
                            BtSaveChangesOffOgg.Enabled = stat;
                            BtDelChangesOffOgg.Enabled = stat;
                            AddOffCreaOggettoRica.Enabled = stat;

                            AddOffCreaOggettoPezzoFiltro.Enabled = stat != true;
                            BtAddRicToOff.Enabled = stat != true;
                            SelOffCrea.Enabled = stat != true;

                            return;
                        case "A":
                            AddOffCreaOggettoPori.Enabled = stat;
                            AddOffCreaOggettoPsco.Enabled = stat;
                            AddOffCreaOggettoPezzi.Enabled = stat;
                            AddOffCreaOggettoPezzoFiltro.Enabled = stat;

                            SelOffCrea.Enabled = stat;
                            AddOffCreaOggettoMach.Enabled = stat;
                            AddOffCreaOggettoRica.Enabled = stat;


                            BtAddRicToOff.Enabled = stat;
                            return;
                        case "CA":
                            AddOffCreaOggettoPori.Text = "";
                            AddOffCreaOggettoPoriRic.Text = "";
                            AddOffCreaOggettoPsco.Text = "";
                            AddOffCreaOggettoPezzi.Text = "";
                            AddOffCreaOggettoDesc.Text = "";

                            SelOffCrea.Enabled = true;
                            AddOffCreaOggettoMach.Enabled = true;
                            AddOffCreaOggettoMach_SelectedIndexChanged(this, EventArgs.Empty);
                            return;
                        case "CE":

                            return;
                    }
                    return;
                case "OCR":
                    switch (action)
                    {
                        case "E":

                            //Crea ordine
                            BtDelOrd.Enabled = stat;
                            BtChiudiOrd.Enabled = stat;
                            BtSaveModOrd.Enabled = stat;
                            BtCreaOrdine.Enabled = stat;
                            return;
                        case "E2":

                            //Oggetti ordine
                            BtCreaOrdineOgg.Enabled = stat;
                            BtChiudiOrdOgg.Enabled = stat;
                            BtDelOrdOgg.Enabled = stat;
                            BtSaveModOrdOgg.Enabled = stat;

                            return;
                        case "FE":

                            FieldOrdOggIdRic.Enabled = stat;
                            FieldOrdOggId.Enabled = stat;
                            FieldOrdOggPOr.Enabled = stat;
                            FieldOrdOggPsc.Enabled = stat;
                            FieldOrdOggQta.Enabled = stat;
                            FieldOrdOggETA.Enabled = stat;
                            CheckBoxOrdOggSconto.Enabled = stat;
                            CheckBoxOrdOggSconto.Checked = true;


                            CheckBoxOrdOggCheckAddNotOffer.Enabled = false;
                            FieldOrdOggMach.Enabled = false;
                            FieldOrdOggPezzo.Enabled = false;
                            FieldOrdOggPezzoFiltro.Enabled = false;

                            return;
                        case "A":
                            if (ComboBoxOrdCliente.SelectedItem.GetHashCode() > 0)
                            {
                                CheckBoxOrdOffertaNonPresente.Enabled = true;
                            }
                            else
                            {
                                CheckBoxOrdOffertaNonPresente.Enabled = stat;
                                ComboBoxOrdOfferta.Enabled = stat;
                            }

                            FieldOrdNOrdine.Enabled = stat;
                            FieldOrdData.Enabled = stat;
                            FieldOrdETA.Enabled = stat;
                            FieldOrdSconto.Enabled = stat;
                            FieldOrdPrezF.Enabled = stat;
                            FieldOrdStato.Enabled = stat;
                            FieldOrdSped.Enabled = stat;
                            FieldOrdSpedGestione.Enabled = stat;

                            CheckBoxCopiaOffertainOrdine.Enabled = stat;

                            BtCreaOrdine.Enabled = stat;
                            return;
                        case "AE":

                            BtCreaOrdine.Enabled = stat != true;
                            CheckBoxCopiaOffertainOrdine.Enabled = stat != true;
                            BtSaveModOrd.Enabled = stat;
                            BtDelOrd.Enabled = stat;
                            BtChiudiOrd.Enabled = stat;
                            return;
                        case "CA":
                            FieldOrdId.Text = "";
                            FieldOrdNOrdine.Text = "";
                            FieldOrdData.Text = t;
                            FieldOrdETA.Text = t;
                            FieldOrdSconto.Text = "0";
                            FieldOrdPrezF.Text = "";
                            FieldOrdTot.Text = "";
                            FieldOrdSped.Text = "";
                            CheckBoxCopiaOffertainOrdine.Checked = true;
                            ComboBoxOrdContatto.Enabled = false;

                            if (clean == true)
                            {
                                ComboBoxOrdOfferta.SelectedIndexChanged -= ComboBoxOrdOfferta_SelectedIndexChanged;
                                Populate_combobox_dummy(ComboBoxOrdOfferta);
                                ComboBoxOrdOfferta.SelectedIndexChanged += ComboBoxOrdOfferta_SelectedIndexChanged;
                            }

                            if (stat == true)
                                ComboBoxOrdCliente.SelectedIndex = 0;

                            if (FieldOrdStato.DataSource != null)
                                FieldOrdStato.SelectedIndex = 0;
                            if (FieldOrdSpedGestione.DataSource != null)
                                FieldOrdSpedGestione.SelectedIndex = 0;
                            return;

                        case "CE":
                            CheckBoxOrdOggSconto.Checked = true;
                            CheckBoxOrdOggSconto.Enabled = false;
                            CheckBoxOrdOggCheckAddNotOffer.Checked = false;
                            CheckBoxOrdOggCheckAddNotOffer.Enabled = true;

                            ComboBoxOrdContatto.Enabled = false;
                            FieldOrdOggPezzoFiltro.Enabled = false;
                            FieldOrdOggPezzo.Enabled = false;
                            FieldOrdOggMach.Enabled = false;

                            FieldOrdOggPOr.Enabled = false;
                            FieldOrdOggPsc.Enabled = false;
                            FieldOrdOggQta.Enabled = false;
                            FieldOrdOggETA.Enabled = false;

                            FieldOrdOggIdRic.Text = "";
                            FieldOrdOggId.Text = "";

                            FieldOrdOggPOr.Text = "";
                            FieldOrdOggPsc.Text = "";
                            FieldOrdOggQta.Text = "";
                            FieldOrdOggETA.Text = t;
                            FieldOrdOggDesc.Text = "";
                            FieldOrdOggPezzoFiltro.Text = "";
                            FieldOrdOggPezzoFiltro_Text = "";

                            old_prezzo_scontatoV.Text = "";
                            old_pezziV.Text = "";
                            old_dataETAOrdValue.Text = "";

                            FieldOrdOggMach.SelectedIndex = 0;
                            return;
                    }
                    return;
                case "VS":
                    switch (action)
                    {
                        case "E":
                            creaEventoCalendario.Enabled = stat;
                            RimuoviEventoCalendario.Enabled = stat;
                            AggiornaEventoCalendario.Enabled = stat;

                            VisOrdChiudi.Enabled = stat;

                            AggiornaEventoCalendario.Enabled = stat;
                            return;
                        case "FE":

                            return;
                        case "A":

                            return;
                        case "AE":

                        case "CA":
                            VisOrdSoc.Text = "";
                            VisOrdSoStato.Text = "";
                            VisOrdSoPro.Text = "";
                            VisOrdSoCitta.Text = "";
                            VisOrdCont.Text = "";
                            VisOrdContTel.Text = "";
                            VisOrdContMail.Text = "";
                            VisOrdData.Text = "";
                            VisOrdETA.Text = "";
                            VisOrdTot.Text = "";
                            VisOrdTotFi.Text = "";
                            VisOrdStato.Text = "";
                            VisOrdSped.Text = "";
                            VisOrdSpedGest.Text = "";
                            VisOrdNumero.Text = "";

                            ClearDataGridView(dataGridViewVisOrdOggetti);

                            return;

                        case "CE":

                            return;
                    }
                    return;
                case "DB":
                    switch (action)
                    {
                        case "E":
                            BtDbBackup.Enabled = stat;
                            BtDbRestore.Enabled = stat;
                            SettingDbOptimize.Enabled = stat;
                            return;
                        case "FE":

                            return;
                        case "A":

                            return;
                        case "AE":

                        case "CA":

                            return;

                        case "CE":

                            return;
                    }
                    return;
                default:
                    return;
            }
        }

        //COMBOBOX
        private int FindIndexFromValue(ComboBox nome_ctr, int value)
        {
            int i = 0;
            bool indexfound = false;
            foreach (ComboBoxList item in nome_ctr.Items)
            {
                if (item.Value == value)
                {
                    indexfound = true;
                    break;
                }
                i++;
            }
            if (indexfound == true)
                return i;
            else
                return -1;
        }

        //DATABASE

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (connection != null && connection.State == ConnectionState.Open)
            {
                RunSQLiteOptimize(1000);
                connection.Close();
            }
        }

        public string ReturnErorrCode(SQLiteException ex)
        {
            Dictionary<int, string> er = new Dictionary<int, string>
            {
                { 787, Environment.NewLine + "L'informazione che si sta provando ad eliminare è associata ad un elemento, eliminare prima l'elemento e poi riprovare." + Environment.NewLine + Environment.NewLine + "Esempio: se si sta provando ad eliminare un'offerta per la quale è stato creato un ordine, eliminare prima l'ordine e poi l'offerta." },
                { 2067, Environment.NewLine + "Esiste già un elemento nel database con le stesse uniche informazioni." }
            };


            if (er.ContainsKey(ex.ErrorCode))
                return er[ex.ErrorCode];
            else
                return ex.Message;
        }

        private void Timer_RunSQLiteOptimize_Tick(object sender, EventArgs e)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                RunSQLiteOptimize();
            }
        }

        //ALTRO
        private void DataGridViewOrd_ColumnSortModeChanged(object sender, DataGridViewColumnEventArgs e)
        {
            MessageBox.Show(Convert.ToString(e.Column));
            MessageBox.Show("w");
        }


        //Validate functions

        public class ValidationResult
        {
            public bool Success { get; set; } = false;
            public bool BoolValue { get; set; } = false;
            public decimal? DecimalValue { get; set; } = null;
            public int? IntValue { get; set; } = null;
            public string Error { get; set; } = "";
            public DateTime DateValue { get; set; } = DateTime.MinValue;
        }

        public ValidationResult ValidateCliente(int idcl)
        {
            ValidationResult answer = new ValidationResult();

            if (idcl < 0)
            {
                answer.Success = true;
                answer.BoolValue = false;
                answer.Error = "Selezionare cliente dalla lista." + Environment.NewLine;

                return answer;
            }

            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_elenco] WHERE ([Id] = @user) LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", idcl);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Cliente. Codice: " + ReturnErorrCode(ex);
                    return answer;
                }

                if (answer.IntValue < 1)
                {
                    answer.BoolValue = false;
                    answer.Error = "Cliente non valido o vuoto" + Environment.NewLine;
                }
                else
                {
                    answer.BoolValue = true;
                }

                return answer;
            }
        }

        public string ValidateCodiceRicambio(string codice)
        {
            Regex rgx = new Regex(@"^[a-zA-Z]{1}\d{1,}[-]\d{1,}$");

            if (string.IsNullOrEmpty(codice) || !rgx.IsMatch(codice))
            {
                return "Codice non valido o vuoto" + Environment.NewLine;
            }

            return "";
        }

        public ValidationResult ValidatePrezzo(string prezzo)
        {
            ValidationResult answer = new ValidationResult
            {
                Success = Decimal.TryParse(prezzo, style, culture, out decimal prezzoD)
            };

            if (!answer.Success)
            {
                answer.Error = "Prezzo non valido(##,##) o vuoto" + Environment.NewLine;
                return answer;
            }
            if (prezzoD < 0)
            {
                answer.Error = "Il prezzo deve essere positivo" + Environment.NewLine;
                return answer;
            }

            answer.DecimalValue = prezzoD;
            return answer;
        }

        public ValidationResult ValidateSconto(string sconto)
        {
            ValidationResult answer = new ValidationResult
            {
                Success = true
            };

            if (!Decimal.TryParse(sconto, style, culture, out decimal scontoV) || !Regex.IsMatch(sconto, @"^[\d,.]+$"))
            {
                answer.Success = false;
            }

            if (!answer.Success)
            {
                answer.Error = "Sconto non valido(##,##) o vuoto" + Environment.NewLine;
                return answer;
            }
            else if (scontoV < 0 || scontoV > 100)
            {
                answer.Error = "Lo socnto deve essere compreso tra 0 e 100. " + Environment.NewLine;
                return answer;
            }
            else
            {
                answer.DecimalValue = scontoV;
            }

            return answer;
        }

        public ValidationResult ValidateFornitore(int id)
        {
            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[fornitori] WHERE ([Id] = @user) LIMIT 1";

            ValidationResult answer = new ValidationResult();

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", id);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Fornitore. Codice: " + ReturnErorrCode(ex);
                    return answer;
                }
            }

            if (answer.IntValue < 1)
            {
                answer.BoolValue = false;
                answer.Error = "Fornitore non presente nel database" + Environment.NewLine;
            }
            else
            {
                answer.BoolValue = true;
            }

            return answer;
        }

        public ValidationResult ValidateMacchina(int id)
        {
            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_macchine] WHERE ([Id] = @user) LIMIT 1;";
            ValidationResult answer = new ValidationResult();

            if (id > 0)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@user", id);

                        answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                        answer.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        answer.Error = "Errore durante verifica ID Macchina. Codice: " + ReturnErorrCode(ex);
                        answer.Success = false;

                        return answer;
                    }
                }
                if (answer.IntValue < 1)
                {
                    answer.BoolValue = false;
                    answer.Error = "Macchina non presente nel database" + Environment.NewLine;
                }
                else
                {
                    answer.BoolValue = true;
                }
                return answer;
            }
            answer.BoolValue = false;
            answer.Error = "Selezionare Macchina." + Environment.NewLine;

            return answer;
        }

        public ValidationResult ValidatePRef(int id)
        {
            string commandText = "SELECT COUNT(*) FROM " + schemadb + @"[clienti_riferimenti] WHERE ([Id] = @user) LIMIT 1;";

            ValidationResult answer = new ValidationResult();

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", id);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;

                    if (answer.IntValue < 1)
                    {
                        answer.BoolValue = false;
                        answer.Error = "Persona di riferimento non valida o vuota." + Environment.NewLine;
                    }
                    else
                    {
                        answer.BoolValue = true;
                    }
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Persona Riferiemnto. Codice: " + ReturnErorrCode(ex);
                }
            }

            return answer;
        }

        public string ValidateComponenteNome(string nome)
        {
            if (string.IsNullOrEmpty(nome))
            {
                return "Nome Componenete non valido o vuoto" + Environment.NewLine;
            }
            return "";
        }

        public ValidationResult ValidateSpedizione(string spedizioni, int gestSP)
        {
            ValidationResult answer = new ValidationResult();

            if (!Decimal.TryParse(spedizioni, style, culture, out decimal prezzo))
            {
                answer.Error += "Prezzo spedizione non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzo < 0)
                {
                    answer.Error += "Il prezzo spedizione deve essere positivo" + Environment.NewLine;
                }
                else
                {
                    answer.DecimalValue = prezzo;
                }
            }

            if (gestSP < 0)
            {
                answer.Error += "Selezionare opzione per la gestione del costo della spedizione" + Environment.NewLine;
            }

            return answer;
        }

        public ValidationResult ValidateDate(string stringDate)
        {
            ValidationResult answer = new ValidationResult();

            if (!DateTime.TryParseExact(stringDate, dateFormat, provider, DateTimeStyles.None, out DateTime dataOrdValue))
            {
                answer.Error += "Valore: " + stringDate + ". Data non valida o vuota" + Environment.NewLine;
            }
            else
            {
                answer.DateValue = dataOrdValue;
            }

            return answer;
        }

        //CREDITI

        private void Csvhelper_github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://joshclose.github.io/CsvHelper/");

        }

        private void Autoupdaternet_github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/ravibpatel/AutoUpdater.NET");
        }

        private void Fody_github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Fody/Fody");
        }

        private void CosturaFody_github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Fody/Costura");
        }


    }

    public class ComboBoxList
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public override string ToString()
        {

            return Name;
        }

        public override int GetHashCode()
        {
            int? check = Value;
            if (check != null)
                return Value;
            else
                return -1;
        }
    }


    public class FilterTextBox : TextBox
    {

        private string placeholdertext;

        public string PlaceholderText { get { return placeholdertext; } set { placeholdertext = value; if (String.IsNullOrEmpty(this.Text.Trim())) this.Text = value; } }

        public FilterTextBox()
        {
            Initialize();
        }

        private void Initialize()
        {
            this.Enter += new EventHandler(ThisHasFocus);
            this.Leave += new EventHandler(ThisWasLeaved);
        }

        private void ThisHasFocus(object sender, EventArgs e)
        {
            if (this.Text == this.PlaceholderText)
            {
                this.Text = "";
            }
        }

        private void ThisWasLeaved(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(this.Text.Trim()))
            {
                this.Text = this.PlaceholderText;
            }
        }

    }

    public class Offerte
    {
        [Index(0)]
        [Name("Numero Offerta")]
        public string NumOfferta { get; set; }

        [Index(1)]
        [Name("Cliente")]
        public string Cliente { get; set; }

        [Index(2)]
        [Name("Data")]
        public string DataOfferta { get; set; }

        [Index(3)]
        [Name("Totale Offerta")]
        public string TotOfferta { get; set; }

        [Index(4)]
        [Name("Stato")]
        public string StatoOfferta { get; set; }

        [Index(5)]
        [Name("Converito in Ordine")]
        public string ConvOfferta { get; set; }

        [Index(6)]
        [Name("Ricambio")]
        public string PezzoOfferta { get; set; }

        [Index(7)]
        [Name("Codice Ricambio")]
        public string CodicePezzo { get; set; }

        [Index(8)]
        [Name("Macchina")]
        public string MacchinaOfferta { get; set; }

        [Index(9)]
        [Name("Quantità")]
        public string QtaOfferta { get; set; }

        [Index(10)]
        [Name("Prezzo Nell'Offerta")]
        public string PrezzoOfferta { get; set; }

        [Index(11)]
        [Name("Prezzo Finale")]
        public string PrezzoFinOfferta { get; set; }

        [Index(12)]
        [Name("Aggiunto ad Offerta")]
        public string PzzAggOfferta { get; set; }
    }

    public class Ordini
    {
        [Index(0)]
        [Name("Numero Ordine")]
        public string NumOrdine { get; set; }

        [Index(1)]
        [Name("Numero Offerta")]
        public string NumOfferta { get; set; }

        [Index(2)]
        [Name("Cliente")]
        public string Cliente { get; set; }

        [Index(3)]
        [Name("Data Ordine")]
        public string DataOrdine { get; set; }

        [Index(4)]
        [Name("ETA Ordine")]
        public string ETAOrdine { get; set; }

        [Index(5)]
        [Name("Totale Ordine")]
        public string TotOrdine { get; set; }

        [Index(6)]
        [Name("Prezzo Finale Ordine")]
        public string TotFinOrdine { get; set; }

        [Index(7)]
        [Name("Sconto")]
        public string Sconto { get; set; }

        [Index(8)]
        [Name("Stato")]
        public string Stato { get; set; }

        [Index(9)]
        [Name("Ricambio")]
        public string Ricambio { get; set; }

        [Index(10)]
        [Name("Codice Ricambio")]
        public string CodRicambio { get; set; }

        [Index(11)]
        [Name("Prezzo Nell'Offerta")]
        public string PrezzoRicOrdine { get; set; }

        [Index(12)]
        [Name("Prezzo Finale")]
        public string PrezzoRicFinOrdine { get; set; }

        [Index(13)]
        [Name("Quantità")]
        public string QtaRicOrdine { get; set; }

        [Index(14)]
        [Name("ETA Ricambio")]
        public string ETARicambio { get; set; }
    }

}