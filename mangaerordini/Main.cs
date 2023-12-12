using CsvHelper;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.StyledXmlParser.Jsoup.Nodes;
using ManagerOrdini;
using ManagerOrdini.Forms;
using Microsoft.VisualBasic;
using Razorphyn;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Razorphyn.Populate;
using static Razorphyn.SupportClasses;
using Application = System.Windows.Forms.Application;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace mangaerordini
{
    internal partial class Main : Form
    {
        static readonly int recordsPerPage = 8;

        int datiGridViewFornitoriCurPage = 1;
        int datiGridViewClientiCurPage = 1;
        int datiGridViewPrefCurPage = 1;
        int datiGridViewMacchineCurPage = 1;
        int datiGridViewRicambiCurPage = 1;
        int datiGridViewClientiSediCurPage = 1;

        int offerteCreaCurPage = 1;
        int OrdiniCurPage = 1;
        int OrdiniViewCurPage = 1;

        string AddOffCreaOggettoPezzoFiltro_Text = "";
        string FieldOrdOggPezzoFiltro_Text = "";

        List<long> IdsInOfferOrder;

        [DllImport("User32.dll", SetLastError = true)]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        UserSettings UserSettings = new();

        internal Main()
        {
            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);

            ProgramParameters.connection.Open();
            ProgramParameters.connection.SetExtendedResultCodes(true);

            RunSQLiteOptimize(500);

            Timer_RunSQLiteOptimize.Interval = 180 * 1000;
            Timer_RunSQLiteOptimize.Enabled = true;
            Timer_RunSQLiteOptimize.Start();

            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };

            this.Text = Assembly.GetExecutingAssembly().GetName().Name + " - v" + Application.ProductVersion;

            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            FixBuffer(this);

            var comboBoxes = Utility.GetAllNestedControls(this).OfType<ComboBox>().ToList();

            foreach (ComboBox ctrl in comboBoxes)
            {
                Populate_combobox_dummy(ctrl);
            }

            Populate_combobox_dummy(ComboBoxOrdSede);
            Populate_combobox_dummy(FieldOrdOggSede);
            Populate_combobox_dummy(ComboBoxOrdCliente);
            Populate_combobox_dummy(ComboBoxOrdOfferta);
            Populate_combobox_dummy(ComboSelOrd);
            Populate_combobox_dummy(FieldOrdStato);
            Populate_combobox_dummy(ComboSelOrdSede);

            Populate_combobox_dummy(AddOffCreaPRef);
            Populate_combobox_dummy(AddOffCreaOggettoMach);
            Populate_combobox_dummy(ComboBoxOrdContatto);

            Populate_combobox_dummy(FieldOrdOggMach);
            Populate_combobox_dummy(FieldOrdOggPezzo);

            Populate_combobox_dummy(dataGridViewMacchina_Filtro_Cliente);

            Populate_combobox_FieldOrdSpedGestione(FieldOrdSpedGestione);
            Populate_combobox_FieldOrdSpedGestione(AddOffCreaSpedizioneGest);
            Populate_combobox_statoOrdini(new ComboBox[] { FieldOrdStato });

            UpdateFixedComboValue();
            UpdateOrdiniStato();
            UpdateSetting();
            UpdateCountryList();
            UpdateClienti();
            UpdateClientiSedi();
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

            SwitchToThisWindow(this.Handle, true);

        }

        //ALTRO
        private void BtDbBackup_Click(object sender, EventArgs e)
        {
            UpdateFields("DB", "E", false);

            string db_name = (ProgramParameters.exeFolderPath + ProgramParameters.db_file_path + ProgramParameters.db_file_name).ToUpper().ToString();

            using (FolderBrowserDialog db_backup_path = new())
            {
                db_backup_path.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                db_backup_path.SelectedPath = ProgramParameters.exeFolderPath;
                string bk_fileName;

                if (OnTopMessage.ShowFolderBrowserDialog(db_backup_path) == DialogResult.OK)
                {
                    string folderPath = db_backup_path.SelectedPath;

                    string iden = DateTime.Now.ToString("yyyyMMddHHmmss");
                    iden = iden.Replace(":", "").Replace(" ", "").Replace(@"/", "");

                    bk_fileName = folderPath + "/db_managerordini_" + iden + ".sqlitebak";

                    using (var source = new SQLiteConnection("Data Source=" + ProgramParameters.exeFolderPath + ProgramParameters.db_file_path + ProgramParameters.db_file_name))
                    using (var destination = new SQLiteConnection("Data Source=" + bk_fileName))
                    {
                        try
                        {
                            source.Open();
                            destination.Open();
                            source.BackupDatabase(destination, "main", "main", -1, null, 0);
                            OnTopMessage.Information("Backup eseguito correttamente", "Backup Eseguito");
                            Process.Start(folderPath);
                        }
                        catch
                        {
                            OnTopMessage.Error("Backup fallito", "Errore Generico");
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

            using (OpenFileDialog openFileDialog = new())
            {
                string filePath = null;

                openFileDialog.InitialDirectory = ProgramParameters.exeFolderPath;
                openFileDialog.Filter = "Database (.sqlitebak)|*.sqlitebak";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (OnTopMessage.ShowOpenFileDialog(openFileDialog) == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }

                if (!String.IsNullOrEmpty(filePath))
                {

                    DialogResult dialogResult = OnTopMessage.Question("Procedere con il ripristino del database?", "Ripristino Database");
                    if (dialogResult == DialogResult.Yes)
                    {

                        using (var source = new SQLiteConnection("Data Source=" + filePath))
                        using (var destination = new SQLiteConnection("Data Source=" + ProgramParameters.exeFolderPath + ProgramParameters.db_file_path + ProgramParameters.db_file_name))
                        {
                            source.Open();
                            destination.Open();
                            source.BackupDatabase(destination, "main", "main", -1, null, 0);
                            OnTopMessage.Information("L'applicazione verrà riavviata.");

                            Application.Restart();
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        OnTopMessage.Alert("Il database non esiste.");
                    }
                }
            }
            UpdateFields("DB", "E", true);
            return;
        }

        private void RunSqlScriptFile(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new())
            {
                openFileDialog.InitialDirectory = ProgramParameters.exeFolderPath;
                openFileDialog.Filter = "SQL (.sql)|*.sql";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (OnTopMessage.ShowOpenFileDialog(openFileDialog) == DialogResult.OK)
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
                                    using (var command = new SQLiteCommand(commandString, ProgramParameters.connection))
                                    {
                                        try
                                        {
                                            command.ExecuteNonQuery();
                                        }
                                        catch (SQLiteException ex)
                                        {
                                            string spError = commandString.Length > 100 ? commandString.Substring(0, 100) + " ...\n..." : commandString;
                                            OnTopMessage.Error(string.Format("Please check the SqlServer script.\nFile: {0} \nLine: {1} \nError: {2} \nSQL Command: \n{3}", pathStoreProceduresFile, "", ex.Message, spError), "Errore");
                                        }
                                    }
                                }
                            }


                        }
                        OnTopMessage.Information("Script Eseguito. L'applicazione verrà riavviata.");

                        Application.Restart();
                        Environment.Exit(0);

                    }
                    catch (System.Exception ex)
                    {
                        OnTopMessage.Error(ex.Message, "Errore");
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


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.ExecuteNonQuery();

                    OnTopMessage.Information("Ottimizzzazione Eseguita");

                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante Ottimizzzazione. Errore: " + DbTools.ReturnErorrCode(ex));
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

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.ExecuteNonQuery();

                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante Ottimizzzazione. Errore: " + DbTools.ReturnErorrCode(ex));
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
                OnTopMessage.Alert("Selezione almeno una informazione da esportare");
                return;
            }

            using (FolderBrowserDialog csv_path = new())
            {
                csv_path.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                csv_path.SelectedPath = ProgramParameters.exeFolderPath;

                if (OnTopMessage.ShowFolderBrowserDialog(csv_path) == DialogResult.OK)
                {
                    string folderPath = csv_path.SelectedPath;
                    string iden = DateTime.Now.ToString("yyMMddHHmmss");
                    iden = iden.Replace(":", "").Replace(" ", "").Replace(@"/", "");
                    string commandText = "";

                    if (exportOfferte)
                    {
                        commandText = @"SELECT  
                                    OE.codice_offerta AS NumOfferta,
									CE.nome  AS Cliente,
									CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta AS Sede,
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
                                    IIF( OP.pezzi_aggiunti = 0 , 'No' , 'Sì' ) AS PzzAggOfferta

								   FROM " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
								   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
										ON CS.Id = OE.ID_sede
								   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = CS.ID_cliente
								   LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP
										ON OP.ID_offerta = OE.ID
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id = OP.ID_ricambio
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM
										ON CM.Id = PR.ID_macchina

                                   WHERE OE.data_offerta BETWEEN @startdate AND @enddate
								   ORDER BY OE.data_offerta ASC";


                        using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
                        {
                            try
                            {
                                DataTable ds = new();
                                cmd.SelectCommand.Parameters.AddWithValue("@startdate", start);
                                cmd.SelectCommand.Parameters.AddWithValue("@enddate", end);

                                cmd.Fill(ds);

                                using (var writer = new StreamWriter(folderPath + @"\" + "OFFERTE_" + iden + ".csv", true, Encoding.UTF8))
                                using (var csv = new CsvWriter(writer, ProgramParameters.provider))
                                {
                                    csv.WriteHeader<SupportClasses.OfferteCSV>();
                                    csv.NextRecord();

                                    foreach (DataRow row in ds.Rows)
                                    {
                                        foreach (DataColumn column in ds.Columns)
                                        {
                                            csv.WriteField(row[column]);
                                        }
                                        csv.NextRecord();
                                    }

                                    OnTopMessage.Information("Offerte Esportate");
                                }
                            }
                            catch (SQLiteException ex)
                            {
                                OnTopMessage.Error("Errore durante lettura dati Offerte esportazione in csv. Codice: " + DbTools.ReturnErorrCode(ex));
                                return;
                            }
                        }
                    }

                    if (exportOrdini)
                    {
                        commandText = @"SELECT  
									OE.codice_ordine AS codOrd,
									OFE.codice_offerta AS IDoff,
									CE.nome AS Cliente,
									CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta AS Sede,
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

								   FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
								   LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] OFE 
										ON OFE.Id = OE.ID_offerta 
                                   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
										ON CS.Id = OFE.ID_sede
								   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = CS.ID_cliente
								   LEFT JOIN " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP
									    ON OP.ID_ordine = OE.Id
                                   LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
									    ON PR.Id = OP.ID_ricambio
                                    WHERE OE.ID_offerta IS NOT NULL AND OE.data_ordine BETWEEN @startdate AND @enddate 

                                    UNION ALL
                                    
                                    SELECT 
                                        OE.codice_ordine AS codOrd, 
									    '' AS IDoff,
									    CE.nome AS Cliente,
									    CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta AS Sede,
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

								   FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
                                   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
										ON CS.Id = OE.ID_sede
								   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = CS.ID_cliente 
								   LEFT JOIN " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP
									    ON OP.ID_ordine = OE.Id
                                   LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
									    ON PR.Id = OP.ID_ricambio
                                    WHERE OE.ID_offerta IS NULL AND OE.data_ordine BETWEEN @startdate AND @enddate

								   ORDER BY datOr ASC";


                        using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
                        {
                            try
                            {
                                DataTable ds = new();
                                cmd.SelectCommand.Parameters.AddWithValue("@startdate", start);
                                cmd.SelectCommand.Parameters.AddWithValue("@enddate", end);

                                cmd.Fill(ds);

                                using (var writer = new StreamWriter(folderPath + @"\" + "ORDINI_" + iden + ".csv", true, Encoding.UTF8))
                                using (var csv = new CsvWriter(writer, ProgramParameters.provider))
                                {
                                    csv.WriteHeader<SupportClasses.OrdiniCSV>();
                                    csv.NextRecord();

                                    foreach (DataRow row in ds.Rows)
                                    {
                                        foreach (DataColumn column in ds.Columns)
                                        {
                                            csv.WriteField(row[column]);
                                        }
                                        csv.NextRecord();
                                    }

                                    OnTopMessage.Information("Ordini Esportati");
                                }
                            }
                            catch (SQLiteException ex)
                            {
                                OnTopMessage.Error("Errore durante lettura dati Ordini esportazione in csv. Codice: " + DbTools.ReturnErorrCode(ex));
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

            new ProgramUpdateFunctions().CheckUpdates();

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
            long fornitoreId = Convert.ToInt64(AddDatiCompSupplier.SelectedValue.ToString());
            long macchinaId = Convert.ToInt64(AddDatiCompMachine.SelectedValue.ToString());

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Componente").Error;

            er_list += DataValidation.ValidateCodiceRicambio(codice);

            DataValidation.ValidationResult answerMacchina = DataValidation.ValidateMacchina(macchinaId);

            if (!answerMacchina.Success)
            {
                OnTopMessage.Error(answerMacchina.Error);
                return;
            }
            er_list += answerMacchina.Error;

            DataValidation.ValidationResult answer = DataValidation.ValidatePrezzo(prezzo);
            er_list += answer.Error;

            if (fornitoreId < 1)
            {
                er_list += "ID Fornitore non valido o vuoto" + Environment.NewLine;
            }

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("R", "A", true);
                return;
            }

            string commandText = "INSERT INTO " + ProgramParameters.schemadb + @"[pezzi_ricambi](nome, codice, descrizione, prezzo, ID_fornitore, ID_macchina, active) VALUES (@nome, @codice, @desc, @prezzo, @idif, @idma , 1);";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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

                    OnTopMessage.Information("Componente aggiunto al database");

                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiunta al database. Errore: " + DbTools.ReturnErorrCode(ex));
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
            long fornitoreId = Convert.ToInt64(ChangeDatiCompSupplier.SelectedValue.ToString());
            long macchinaId = Convert.ToInt64(ChangeDatiCompMachine.SelectedValue.ToString());
            string idF = ChangeDatiCompID.Text;

            string er_list = "";

            string commandText;

            er_list += DataValidation.ValidateName(nome, "Componente").Error;

            DataValidation.ValidationResult answer = DataValidation.ValidateMacchina(macchinaId);
            er_list += answer.Error;

            answer = DataValidation.ValidateFornitore(fornitoreId);
            er_list += answer.Error;

            er_list += DataValidation.ValidateCodiceRicambio(codice);

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            DataValidation.ValidationResult prezzod = DataValidation.ValidatePrezzo(prezzo);
            er_list += prezzod.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Ricambio");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);
                return;
            }

            commandText = "UPDATE " + ProgramParameters.schemadb + @"[pezzi_ricambi] SET nome=@nome,codice=@codice,descrizione=@descrizione,prezzo=@prezzod,ID_fornitore=@idif,ID_macchina=@idma WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);
                    if (macchinaId < 1)
                        cmd.Parameters.AddWithValue("@idma", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@idma", macchinaId);

                    cmd.ExecuteNonQuery();

                    string IdAddOffCreaOggettoId = AddOffCreaOggettoId.Text.Trim();
                    if (!String.IsNullOrEmpty(IdAddOffCreaOggettoId) && int.TryParse(IdAddOffCreaOggettoId, out int tempid))
                    {
                        if (tempid == idQ.IntValue)
                        {
                            UpdateFields("OAO", "CA", false);
                        }
                    }

                    SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);

                    UpdateFields("R", "CE", false);
                    UpdateFields("R", "E", false);
                    UpdateRicambi();

                    OnTopMessage.Information("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento del ricambio. Codice: " + DbTools.ReturnErorrCode(ex));
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

            er_list += DataValidation.ValidateName(nome, "Componente").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);
                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare il Pezzo di Ricambio?", "Eliminare Pezzo di Ricambio");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("R", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[pezzi_ricambi] SET active = null , deleted = 1 WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateFields("R", "CE", false);
                    UpdateFields("R", "E", false);
                    UpdateRicambi();

                    OnTopMessage.Information("Pezzo di ricambio (" + nome + ") eliminato.");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante eliminazione pezzo di ricambio. Codice: " + DbTools.ReturnErorrCode(ex));
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
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string macchina = row.Cells[i].Value.ToString(); i++;
                    string fornitore = row.Cells[i].Value.ToString(); i++;
                    string nome = row.Cells[i].Value.ToString(); i++;
                    string code = row.Cells[i].Value.ToString(); i++;
                    string prezzo = row.Cells[i].Value.ToString(); i++;

                    string descrizione = "";
                    long idcl = 0;
                    long idsd = 0;
                    long idmc = 0;

                    ChangeDatiCompCliente.SelectedIndex = 0;
                    ChangeDatiCompSupplier.SelectedIndex = ChangeDatiCompSupplier.FindString(fornitore);

                    string commandText = @"SELECT 
												PR.descrizione AS descrizione, 
												CM.Id AS Id,
												CM.ID_cliente AS ID_cliente,
												CM.ID_sede AS ID_sede 
											FROM [pezzi_ricambi] AS PR
											LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine]  AS CM
												ON CM.Id = PR.ID_macchina 
											WHERE PR.Id=@ID LIMIT 1;";

                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                descrizione = Convert.ToString(reader["descrizione"]);

                                if (!string.IsNullOrEmpty(Convert.ToString(reader["ID_cliente"])))
                                    idcl = Convert.ToInt64(reader["ID_cliente"]);

                                if (!string.IsNullOrEmpty(Convert.ToString(reader["ID_sede"])))
                                    idsd = Convert.ToInt64(reader["ID_sede"]);

                                if (!string.IsNullOrEmpty(Convert.ToString(reader["Id"])))
                                    idmc = Convert.ToInt64(reader["Id"]);
                            }
                            reader.Close();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante popolamento Macchine e Clienti. Codice: " + DbTools.ReturnErorrCode(ex));
                            return;
                        }
                    }

                    ChangeDatiCompID.Text = id;
                    ChangeDatiCompNome.Text = nome;
                    ChangeDatiCompCode.Text = code;
                    ChangeDatiCompPrice.Text = prezzo;
                    ChangeDatiCompDesc.Text = descrizione;
                    ChangeDatiCompIdMachine.Text = Convert.ToString(idmc);

                    ChangeDatiCompCliente.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiCompCliente, idcl);
                    ChangeDatiCompSede.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiCompSede, idsd);
                    ChangeDatiCompMachine.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiCompMachine, idmc);

                    UpdateFields("R", "E", true);
                }
            }
        }

        private void AddDatiCompCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cmb = sender as ComboBox;

            if (cmb.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(cmb.SelectedValue.ToString());

            if (curItemValue > 0)
            {
                Populate_combobox_machine(new ComboBox[] { AddDatiCompMachine }, curItemValue);
                Populate_combobox_sedi(new ComboBox[] { AddDatiCompSede }, curItemValue);

                AddDatiCompMachine.Enabled = true;
                AddDatiCompSede.Enabled = true;
            }
            else
            {
                AddDatiCompMachine.Enabled = false;
                AddDatiCompSede.Enabled = false;
                Populate_combobox_dummy(AddDatiCompMachine);
                Populate_combobox_dummy(AddDatiCompSede);
                AddDatiCompMachine.SelectedIndex = 0;
                AddDatiCompSede.SelectedIndex = 0;
            }
            return;
        }

        private void AddDatiCompSede_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cmb = AddDatiCompCliente;
            ComboBox ctr = AddDatiCompSede;

            if (cmb.DataSource == null || ctr.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(cmb.SelectedValue.ToString());
            long idsede = Convert.ToInt64(ctr.SelectedValue.ToString());

            if (idsede > 0)
            {
                Populate_combobox_machine(new ComboBox[] { AddDatiCompMachine }, idcl, idsede);

                ctr.Enabled = true;
            }
            return;
        }

        private void ChangeDatiCompCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cmb = sender as ComboBox;

            if (cmb.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(cmb.SelectedValue.ToString());

            if (idcl > 0)
            {
                Populate_combobox_machine(new ComboBox[] { ChangeDatiCompMachine }, idcl);
                Populate_combobox_sedi(new ComboBox[] { ChangeDatiCompSede }, idcl);

                long idmc = (String.IsNullOrEmpty(ChangeDatiCompIdMachine.Text.ToString())) ? 0 : Convert.ToInt64(ChangeDatiCompIdMachine.Text);
                ChangeDatiCompMachine.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiCompMachine, idmc);

                ChangeDatiCompMachine.Enabled = true;
                ChangeDatiCompSede.Enabled = true;
            }
            else
            {
                ChangeDatiCompMachine.Enabled = false;
                ChangeDatiCompSede.Enabled = false;
                Populate_combobox_dummy(ChangeDatiCompMachine);
                Populate_combobox_dummy(ChangeDatiCompSede);
                ChangeDatiCompMachine.SelectedIndex = 0;
                ChangeDatiCompSede.SelectedIndex = 0;
            }
            return;
        }

        private void ChangeDatiCompSede_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ChangeDatiCompMachine.DataSource == null || ChangeDatiCompSede.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(ChangeDatiCompCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(ChangeDatiCompSede.SelectedValue.ToString());
            long idmc = Convert.ToInt64(ChangeDatiCompMachine.SelectedValue.ToString());

            if (idsd > 0)
            {
                Populate_combobox_machine(new ComboBox[] { ChangeDatiCompMachine }, idcl, idsd);
                ChangeDatiCompMachine.Enabled = true;
            }

            ChangeDatiCompMachine.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiCompMachine, idmc);

            return;
        }

        private void LoadCompTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewComp;

            int count = 1;
            string codiceRicambioFilter = dataGridViewComp_Filtro_Codice.Text.Trim();

            string addInfo = "";
            List<string> paramsQuery = new();

            if (codiceRicambioFilter != dataGridViewComp_Filtro_Codice.PlaceholderText && String.IsNullOrEmpty(codiceRicambioFilter) == false)
                paramsQuery.Add(" PR.codice LIKE @codiceRicambioFilter");

            if (paramsQuery.Count > 0)
                addInfo = " AND " + String.Join(" AND ", paramsQuery) + " ";

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR WHERE PR.deleted = 0 " + addInfo;


            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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

            (Ricambi.Answer esito, DataTable ds) = Ricambi.GetResources.GetCollection(page--, recordsPerPage, addInfo, codiceRicambioFilter);

            if (esito.Success)
            {
                Dictionary<string, string> columnNames = new()
                    {
                        { "ID", "ID" },
                        { "Nome", "Nome" },
                        { "Fornitore", "Fornitore" },
                        { "Macchina", "Macchina" },
                        { "Codice", "Codice" },
                        { "Prezzo", "Prezzo" }
                    };
                Utility.DataSourceToDataView(data_grid, ds, columnNames);
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
            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Cliente").Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "A", true);
                return;
            }

            string commandText = "INSERT INTO " + ProgramParameters.schemadb + @"[clienti_elenco] (nome, active) VALUES (@nome, 1);";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);

                    cmd.ExecuteNonQuery();

                    UpdateFields("C", "CA", true);
                    UpdateFields("C", "A", true);
                    UpdateClienti();

                    OnTopMessage.Information("Cliente aggiunto al database");
                }
                catch (SQLiteException ex)
                {
                    UpdateFields("C", "A", true);
                    OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void BtSaveChangesClienti_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("C", "E", false);

            string nome = ChangeDatiClientiNome.Text.Trim();
            string idF = ChangeDatiClientiID.Text;

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Cliente").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Cliente");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_elenco] SET nome=@nome WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateClienti();

                    LoaVisOrdOggTable(OrdiniViewCurPage);
                    LoadOrdiniTable(OrdiniCurPage);
                    LoadOfferteCreaTable(offerteCreaCurPage);
                    LoadMacchinaTable(datiGridViewMacchineCurPage);
                    LoadPrefTable(datiGridViewPrefCurPage);

                    ChangeDatiClientiNome.Text = "";
                    ChangeDatiClientSediCitta.Text = "";
                    ChangeDatiClientiSediProvincia.Text = "";
                    ChangeDatiClientiID.Text = "";

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("C", "E", false);

                    OnTopMessage.Information("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento del cliente. Codice: " + DbTools.ReturnErorrCode(ex));
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

            er_list += DataValidation.ValidateName(nome, "Cliente").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);
                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare il cliente?" + Environment.NewLine + "NOTA: verranno eliminate anche le sedi associate al cliente.", "Eliminare Cliente e Sedi Associate");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "E", true);
                return;
            }

            string commandText = @" BEGIN TRANSACTION;
                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[clienti_elenco]  SET deleted = 1, active = NULL WHERE Id=@idq LIMIT 1;
                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[clienti_sedi]    SET deleted = 1, active = NULL WHERE ID_cliente=@idq AND deleted = 0;
                                    COMMIT;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("C", "CE", false);
                    UpdateFields("C", "E", false);

                    UpdateClienti();
                    UpdateClientiSedi();

                    OnTopMessage.Information("Cliente (" + nome + ") eliminato.");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante eliminazione del cliente. Codice: " + DbTools.ReturnErorrCode(ex));
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
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string nome = row.Cells[i].Value.ToString(); i++;

                    ChangeDatiClientiID.Text = id;
                    ChangeDatiClientiNome.Text = nome;

                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "E", true);
                }
            }
        }

        private void LoadClientiTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewClienti;

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_elenco] WHERE deleted = 0;";
            int count = 1;

            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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

            commandText = @"SELECT Id,nome FROM " + ProgramParameters.schemadb + @"[clienti_elenco] WHERE deleted = 0 ORDER BY Id ASC LIMIT @recordperpage OFFSET @startingrecord;";
            page--;


            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                    {
                        { "Id", "ID" },
                        { "nome", "Nome" }
                    };

                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella Clienti. Codice: " + DbTools.ReturnErorrCode(ex));
                }
                finally
                {
                    DrawingControl.ResumeDrawing(data_grid);
                }
            }
            return;
        }

        //TAB SEDI CLIENTI
        private void BtAddSede_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("C", "SA", false);

            //recuperare id cliente da dropdown
            long idcl = Convert.ToInt64(AddSedeCliente.SelectedValue.ToString());
            string numero = AddSedeClienteNumero.Text.Trim();
            string stato = AddSedeClienteStato.SelectedItem.ToString().Trim();
            string citta = AddSedeClienteCitta.Text.Trim();
            string prov = AddSedeClienteProv.Text.Trim();

            string er_list = "";

            DataValidation.ValidationResult cliente = DataValidation.ValidateCliente(idcl);
            er_list += cliente.Error;

            DataValidation.ValidationResult numeroCl = DataValidation.ValidateInt(numero);
            er_list += numeroCl.Error;

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

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "SA", true);
                return;
            }

            string commandText = "INSERT INTO " + ProgramParameters.schemadb + @"[clienti_sedi](ID_cliente, numero, stato, citta, provincia, active) VALUES (@idcl, @numero,@stato,@citta,@prov, 1);";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@numero", numeroCl.IntValue);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    cmd.Parameters.AddWithValue("@citta", citta);
                    cmd.Parameters.AddWithValue("@prov", prov);

                    cmd.ExecuteNonQuery();

                    UpdateFields("C", "SCA", true);
                    UpdateFields("C", "SA", true);
                    UpdateClienti();

                    OnTopMessage.Information("Cliente aggiunto al database");
                }
                catch (SQLiteException ex)
                {
                    UpdateFields("C", "SA", true);
                    OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void BtSaveChangesSede_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("C", "SE", false);

            string numero = ChangeDatiClientiSediNumero.Text.Trim();
            string stato = ChangeDatiClientiSediStato.SelectedItem.ToString().Trim();
            string citta = ChangeDatiClientSediCitta.Text.Trim();
            string prov = ChangeDatiClientiSediProvincia.Text.Trim();
            string idF = ChangeDatiClientiSedeID.Text;

            string er_list = "";

            DataValidation.ValidationResult numeroCl = DataValidation.ValidateInt(numero);
            er_list += numeroCl.Error;

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

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "SE", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Cliente");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "SE", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_sedi] SET numero=@numero,stato=@stato,citta=@citta,provincia=@provincia WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@numero", numeroCl.IntValue);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    cmd.Parameters.AddWithValue("@citta", citta);
                    cmd.Parameters.AddWithValue("@provincia", prov);
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateClientiSedi();

                    LoaVisOrdOggTable(OrdiniViewCurPage);
                    LoadOrdiniTable(OrdiniCurPage);
                    LoadOfferteCreaTable(offerteCreaCurPage);
                    LoadMacchinaTable(datiGridViewMacchineCurPage);
                    LoadPrefTable(datiGridViewPrefCurPage);

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("C", "SE", false);

                    OnTopMessage.Information("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento del cliente. Codice: " + DbTools.ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "SE", true);
                }
            }
            return;
        }

        private void BtDelSede_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("C", "SE", false);

            string idF = ChangeDatiClientiSedeID.Text;

            string er_list = "";

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "SE", true);
                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare il cliente?", "Eliminare Cliente");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("C", "SE", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_sedi] SET deleted = 1, active = NULL WHERE Id=@idq LIMIT 1;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateClienti();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("C", "SCE", false);
                    UpdateFields("C", "SE", false);

                    OnTopMessage.Information("Sede eliminata.");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante eliminazione del cliente. Codice: " + DbTools.ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "SE", true);
                }
            }
            return;
        }

        private void BtCloseChangesSede_Click(object sender, EventArgs e)
        {
            UpdateFields("C", "SCE", false);
            UpdateFields("C", "SE", false);
        }

        private void DataGridViewSede_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string idcl = row.Cells[i].Value.ToString().Split('-')[0]; i++;
                    string numero = row.Cells[i].Value.ToString(); i++;
                    string stato = row.Cells[i].Value.ToString(); i++;
                    string provincia = row.Cells[i].Value.ToString(); i++;
                    string citta = row.Cells[i].Value.ToString(); i++;

                    ChangeDatiClientiSedeID.Text = id;

                    int indexcl = Utility.FindIndexFromValue(ChangeDatiClientiSediCliente, Convert.ToInt64(idcl));

                    if (indexcl > 0)
                        ChangeDatiClientiSediCliente.SelectedIndex = indexcl;

                    ChangeDatiClientiSediNumero.Text = numero;
                    ChangeDatiClientiSediStato.SelectedItem = stato;
                    ChangeDatiClientiSediProvincia.Text = provincia;
                    ChangeDatiClientSediCitta.Text = citta;

                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("C", "SE", true);
                }
            }
        }

        private void LoadSedeTable(int page = 1)
        {
            DataGridView data_grid = dataGridViewClientiSedi;

            string addCond = "";
            long idcl = 0;

            if (DataGridViewSediFilterCliente.DataSource != null)
            {
                idcl = Convert.ToInt64(DataGridViewSediFilterCliente.SelectedValue.ToString());

                if (idcl > 0)
                    addCond += " AND ID_cliente = @idcl ";
            }

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_sedi] WHERE deleted = 0 " + addCond + @";";
            int count = 1;

            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
            {
                cmdCount.Parameters.AddWithValue("@idcl", idcl);

                count = Convert.ToInt32(cmdCount.ExecuteScalar());
                count = (count - 1) / recordsPerPage + 1;
                MaxPageDataClientiSedi.Text = Convert.ToString((count > 1) ? count : 1);
                if (count > 1)
                {
                    DatiClientiSediNxtPage.Enabled = true;
                    DatiClientiSediPrvPage.Enabled = true;
                    DataClientiSediCurPage.Enabled = true;
                }
                else
                {
                    DatiClientiSediNxtPage.Enabled = false;
                    DatiClientiSediPrvPage.Enabled = false;
                    DataClientiSediCurPage.Enabled = false;
                }
                page = (page > count) ? count : page;
                datiGridViewClientiSediCurPage = page;
                DataClientiSediCurPage.Text = "" + page;
            }

            commandText = @"SELECT 
                                CS.Id AS ID, 
                                CE.Id || ' - ' || CE.nome AS cliente,                                 
                                CS.numero AS numero,
                                CS.stato AS stato,
                                CS.provincia AS provincia,
                                CS.citta AS citta

                            FROM " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
                                ON CS.ID_cliente = CE.Id
                            WHERE CS.deleted = 0 " + addCond + @" ORDER BY CS.Id ASC LIMIT @recordperpage OFFSET @startingrecord;";
            page--;


            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                    {
                        { "Id", "ID" },
                        { "nome", "Nome Cliente" },
                        { "numero", "Numero Cliente" },
                        { "stato", "Stato" },
                        { "citta", "Città" },
                        { "provincia", "Provincia" }
                    };

                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella Sedi. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void DataGridViewSediFilterCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSedeTable();
        }

        //TAB PERS RIFERIMENTI

        private void BtAddPersonaRef_Click(object sender, EventArgs e)
        {
            UpdateFields("P", "A", false);

            string nome = AddDatiPRefNome.Text.Trim();
            long idcl = Convert.ToInt64(AddDatiPRefCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(AddDatiPRefSede.SelectedValue.ToString());
            string tel = AddDatiPRefTel.Text.Trim();
            string mail = AddDatiPRefMail.Text.Trim();

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Cliente").Error;

            DataValidation.ValidationResult answer = DataValidation.ValidateCliente(idcl);
            if (!string.IsNullOrEmpty(answer.Error))
            {
                er_list += "Cliente non valido o vuoto" + Environment.NewLine;
            }

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("P", "A", true);
                return;
            }

            string commandText = "INSERT INTO " + ProgramParameters.schemadb + @"[clienti_riferimenti](nome, ID_cliente, ID_sede, mail, telefono, active) VALUES (@nome,@idcl,@idsd,@mail,@tel, 1);";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@idsd", (idsd < 1) ? DBNull.Value : idsd);
                    cmd.Parameters.AddWithValue("@mail", mail);
                    cmd.Parameters.AddWithValue("@tel", tel);

                    cmd.ExecuteNonQuery();
                    OnTopMessage.Information("Persona di riferimento aggiunta al database");

                    UpdateFields("P", "CA", true);
                    UpdateFields("P", "A", true);

                    UpdatePRef();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
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
            long cliente = Convert.ToInt64(ChangeDatiPRefClienti.SelectedValue.ToString());
            long idsd = Convert.ToInt64(ChangeDatiPRefSede.SelectedValue.ToString());
            string tel = ChangeDatiPRefTelefono.Text.Trim();
            string mail = ChangeDatiPRefMail.Text.Trim();
            string idF = ChangeDatiPRefID.Text;

            DataValidation.ValidationResult answer;

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Persona di Riferimento").Error;

            answer = DataValidation.ValidateCliente(cliente);
            if (!answer.Success)
            {
                OnTopMessage.Alert(answer.Error);
                return;
            }
            er_list += answer.Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Persona di Riferimento");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_riferimenti] SET nome=@nome, ID_cliente=@cliente, ID_sede=@sede, mail=@mail, telefono=@telefono WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@cliente", cliente);
                    cmd.Parameters.AddWithValue("@sede", idsd);
                    cmd.Parameters.AddWithValue("@mail", mail);
                    cmd.Parameters.AddWithValue("@telefono", tel);
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdatePRef();
                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("P", "CE", false);
                    UpdateFields("P", "E", false);

                    OnTopMessage.Information("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento del cliente. Codice: " + DbTools.ReturnErorrCode(ex));
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

            er_list += DataValidation.ValidateName(nome, "Persona di Riferimento").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);
                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare la Persona di Riferimento?", "Eliminare Persona di Riferimento");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("P", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_riferimenti] SET deleted = 1, active = NULL WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdatePRef();
                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("P", "CE", false);
                    UpdateFields("P", "E", false);

                    OnTopMessage.Information("Persona di Riferimento (" + nome + ") eliminata.");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante eliminazione Persona di Riferimento. Codice: " + DbTools.ReturnErorrCode(ex));
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
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string cliente = row.Cells[i].Value.ToString(); i++;
                    string sede = row.Cells[i].Value.ToString(); i++;
                    string nome = row.Cells[i].Value.ToString(); i++;
                    string mail = row.Cells[i].Value.ToString(); i++;
                    string tel = row.Cells[i].Value.ToString(); i++;

                    ChangeDatiPRefClienti.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiPRefClienti, Convert.ToInt64(cliente.Split('-')[0]));

                    if (!string.IsNullOrEmpty(sede))
                    {
                        int index = Utility.FindIndexFromValue(ChangeDatiPRefSede, Convert.ToInt64(sede.Split('-')[0]));
                        ChangeDatiPRefSede.SelectedIndex = index;
                    }

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

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_riferimenti] WHERE deleted = 0;";
            int count = 1;

            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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
									CE.Id || ' - ' || CE.nome  AS Cliente,
									IIF(CR.ID_sede IS NOT NULL, CS.Id || ' - ' || CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta, '')   AS Sede,
									CR.nome AS Nome,
									CR.mail AS Mail,
									CR.telefono AS Telefono

                            FROM " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
                                ON CE.Id = CR.ID_cliente 
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
                                ON CS.Id = CR.ID_sede
                            WHERE CR.deleted = 0 ORDER BY CR.Id ASC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DrawingControl.SuspendDrawing(data_grid);

                    data_grid.RowHeadersVisible = false;
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                    {
                        { "ID", "ID" },
                        { "Cliente", "Cliente" },
                        { "Sede", "Sede" },
                        { "Nome", "Nome" },
                        { "Mail", "Mail" },
                        { "Telefono", "Telefono" }
                    };

                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento Riferimenti. Codice: " + DbTools.ReturnErorrCode(ex));
                }
                finally
                {
                    DrawingControl.ResumeDrawing(data_grid);
                }
            }
            return;
        }

        private void ChangeDatiPRefClienti_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeDatiPRefClienti.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(ChangeDatiPRefClienti.SelectedValue.ToString());
            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { ChangeDatiPRefSede }, curItemValue);
                ChangeDatiPRefSede.Enabled = true;
            }
            else
            {
                ChangeDatiPRefSede.Enabled = false;
                Populate_combobox_dummy(ChangeDatiPRefSede);
                ChangeDatiPRefSede.SelectedIndex = 0;
            }
        }

        private void AddDatiPRefCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AddDatiPRefCliente.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(AddDatiPRefCliente.SelectedValue.ToString());
            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { AddDatiPRefSede }, curItemValue);
                AddDatiPRefSede.Enabled = true;
            }
            else
            {
                AddDatiPRefSede.Enabled = false;
                Populate_combobox_dummy(AddDatiPRefSede);
                AddDatiPRefSede.SelectedIndex = 0;
            }
        }

        //TAB FORNITORI 
        private void BtAddFornitore_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI & BOTTONI
            UpdateFields("F", "A", false);

            string nome = AddDatiFornitoreNome.Text.Trim();

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Fornitore").Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "A", true);

                return;
            }

            string commandText = "INSERT INTO " + ProgramParameters.schemadb + @"[fornitori](nome, active) VALUES (@nome, 1);";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);

                    cmd.ExecuteNonQuery();

                    UpdateFields("F", "CA", true);
                    UpdateFields("F", "A", true);
                    UpdateFornitori();

                    OnTopMessage.Information("Fornitore aggiunto al database");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
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

            er_list += DataValidation.ValidateName(nome, "Fornitore").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Fornitore");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[fornitori] SET nome=@nome WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateFornitori();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("F", "CE", false);
                    UpdateFields("F", "E", false);

                    OnTopMessage.Information("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento del fornitore. Codice: " + DbTools.ReturnErorrCode(ex));
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

            er_list += DataValidation.ValidateName(nome, "Fornitore").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;


            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);
                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare il fornitore(" + nome + "))?", "Eliminare Fornitore");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("F", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[fornitori] SET deleted = 1, active = NULL WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateFornitori();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("F", "CE", false);
                    UpdateFields("F", "E", false);

                    OnTopMessage.Information("Fornitore " + nome + " eliminato.");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante eliminazione del fornitore. Codice: " + DbTools.ReturnErorrCode(ex));
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
            if (sender is not DataGridView dgv)
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

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[fornitori] WHERE deleted = 0;";
            int count = 1;

            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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

            commandText = @"SELECT Id,nome FROM " + ProgramParameters.schemadb + @"[fornitori]  WHERE deleted = 0 ORDER BY Id ASC LIMIT " + recordsPerPage;
            page--;

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                    {
                        { "Id", "ID" },
                        { "nome", "Nome" }
                    };
                    Utility.DataSourceToDataView(data_grid, ds, columnNames);

                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella Fornitori. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
            return;
        }

        //TAB MACCHINE

        private void BtAddMacchina_Click(object sender, EventArgs e)
        {
            UpdateFields("M", "A", false);

            string nome = AddDatiMacchinaNome.Text.Trim();
            long idcl = Convert.ToInt64(AddDatiMacchinaCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(AddDatiMacchinaSede.SelectedValue.ToString());
            string seriale = AddDatiMacchinaSeriale.Text.Trim();
            string codice = AddDatiMacchinaCodice.Text.Trim();

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Fornitore").Error;

            DataValidation.ValidationResult answer = DataValidation.ValidateCliente(idcl);
            if (!answer.Success)
            {
                OnTopMessage.Alert(answer.Error);
                return;
            }
            er_list += answer.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("M", "A", true);
                return;
            }

            string commandText = "INSERT INTO " + ProgramParameters.schemadb + @"[clienti_macchine](modello, ID_cliente, ID_sede, seriale, codice, active) VALUES (@modello, @idcl, @idsd, @seriale, @code, 1);";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@modello", nome);
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@idsd", (idsd > 0) ? idsd : DBNull.Value);
                    cmd.Parameters.AddWithValue("@seriale", seriale);
                    cmd.Parameters.AddWithValue("@code", codice);

                    cmd.ExecuteNonQuery();

                    UpdateFields("M", "CA", true);
                    UpdateFields("M", "A", true);

                    UpdateMacchine();

                    OnTopMessage.Information("Macchina aggiunta al database");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
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
            long cliente = Convert.ToInt64(ChangeDatiMacchinaCliente.SelectedValue.ToString());
            long sede = Convert.ToInt64(ChangeDatiMacchinaSede.SelectedValue.ToString());
            string seriale = ChangeDatiMacchinaSeriale.Text.Trim();
            string codice = ChangeDatiMacchinaCodice.Text.Trim();
            string idF = ChangeDatiMacchinaID.Text;

            DataValidation.ValidationResult answer;
            string commandText;

            string er_list = "";

            er_list += DataValidation.ValidateName(nome, "Macchina").Error;

            answer = DataValidation.ValidateCliente(cliente);
            if (!answer.Success)
            {
                OnTopMessage.Alert(answer.Error);
                return;
            }

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("M", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Macchina");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("M", "E", true);
                return;
            }

            commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_macchine] SET modello=@nome, ID_cliente=@cliente, ID_sede=@sede, seriale=@seriale, codice=@code WHERE Id = @idq LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@cliente", cliente);
                    cmd.Parameters.AddWithValue("@sede", (sede > 0) ? sede : DBNull.Value);
                    cmd.Parameters.AddWithValue("@seriale", seriale);
                    cmd.Parameters.AddWithValue("@code", codice);
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateMacchine();
                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("M", "CE", false);
                    UpdateFields("M", "E", false);

                    OnTopMessage.Information("Cambiamenti salvati");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento della macchina. Codice: " + DbTools.ReturnErorrCode(ex));
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

            er_list += DataValidation.ValidateName(nome, "Macchina").Error;

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idF);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("M", "E", true);
                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare la macchina?", "Eliminare Macchina");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("M", "E", true);
                return;
            }

            string commandText = "UPDATE " + ProgramParameters.schemadb + @"[clienti_macchine] SET deleted = 1, active = NULL WHERE Id=@idq LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);

                    cmd.ExecuteNonQuery();

                    UpdateMacchine();

                    UpdateFields("M", "CE", false);
                    UpdateFields("M", "E", false);

                    OnTopMessage.Information("Macchina (" + nome + ") eliminata.");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante eliminazione macchina. Codice: " + DbTools.ReturnErorrCode(ex));
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
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                ChangeDatiMacchinaCliente.Enabled = false;
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string cliente = row.Cells[i].Value.ToString(); i++;
                    string sede = row.Cells[i].Value.ToString(); i++;
                    string nome = row.Cells[i].Value.ToString(); i++;
                    string seriale = row.Cells[i].Value.ToString(); i++;
                    string codice = row.Cells[i].Value.ToString(); i++;

                    int indexcl = Utility.FindIndexFromValue(ChangeDatiMacchinaCliente, Convert.ToInt64(cliente.Split('-')[0]));
                    ChangeDatiMacchinaCliente.SelectedIndex = indexcl;

                    ChangeDatiMacchinaID.Text = id;
                    ChangeDatiMacchinaNome.Text = nome;
                    ChangeDatiMacchinaSeriale.Text = seriale;
                    ChangeDatiMacchinaCodice.Text = codice;

                    if (indexcl > 0 && !string.IsNullOrEmpty(sede))
                    {
                        ChangeDatiMacchinaSede.SelectedIndex = Utility.FindIndexFromValue(ChangeDatiMacchinaSede, Convert.ToInt64(sede.Split('-')[0]));
                    }

                    UpdateFields("M", "E", true);

                    ChangeDatiMacchinaCliente.Enabled = true;
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
            var val = dataGridViewMacchina_Filtro_Cliente.SelectedValue.ToString();
            long idcl = Convert.ToInt64(val);

            string addInfo = "";
            List<string> paramsQuery = new();

            if (idcl > 0)
                paramsQuery.Add(" CM.ID_cliente = @idcl ");

            if (paramsQuery.Count > 0)
                addInfo = " AND " + String.Join(" AND ", paramsQuery);

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM WHERE CM.deleted = 0 " + addInfo + ";";
            int count = 1;

            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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
                    OnTopMessage.Error("Errore durante verifica ID Cliente. Codice: " + DbTools.ReturnErorrCode(ex));
                    return;
                }
            }

            commandText = @"SELECT 
                                CM.Id AS ID,
                                (CE.Id || ' - ' || CE.nome ) AS Cliente,
                                (CS.Id || ' - ' ||  CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta) AS Sede,
                                CM.modello        AS Modello,
                                CM.seriale AS Seriale,
                                CM.codice AS code 
                            FROM " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
                                ON CE.Id = CM.ID_cliente 
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
                                ON CS.Id = CM.ID_sede 
                            WHERE CM.deleted = 0 " + addInfo + "ORDER BY CM.Id ASC LIMIT @recordperpage OFFSET @startingrecord ";

            page--;

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                    {
                        { "ID", "ID" },
                        { "Cliente", "Cliente" },
                        { "Sede", "Sede" },
                        { "Modello", "Modello" },
                        { "Seriale", "Seriale" },
                        { "code", "Codice" }
                    };
                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento Macchine. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void DataGridViewMacchina_Filtro_Cliente_SelectedValueChanged(object sender, EventArgs e)
        {
            LoadMacchinaTable();
        }

        private void ChangeDatiMacchinaCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeDatiMacchinaCliente.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(ChangeDatiMacchinaCliente.SelectedValue.ToString());
            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { ChangeDatiMacchinaSede }, curItemValue);
                ChangeDatiMacchinaSede.Enabled = true;
            }
            else
            {
                ChangeDatiMacchinaSede.Enabled = false;
                Populate_combobox_dummy(ChangeDatiMacchinaSede);
                ChangeDatiMacchinaSede.SelectedIndex = 0;
                ChangeDatiMacchinaSede.Enabled = false;
            }
        }

        private void AddDatiMacchinaCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AddDatiMacchinaCliente.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(AddDatiMacchinaCliente.SelectedValue.ToString());
            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { AddDatiMacchinaSede }, curItemValue);
                AddDatiMacchinaSede.Enabled = true;
            }
            else
            {
                AddDatiMacchinaSede.Enabled = false;
                Populate_combobox_dummy(AddDatiMacchinaSede);
                AddDatiMacchinaSede.SelectedIndex = 0;
            }
        }

        //OFFERTE CREA
        private void AddOffCreaCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaCliente.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString());
            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { AddOffCreaSede }, curItemValue);
                Populate_combobox_pref(AddOffCreaPRef, curItemValue);
                AddOffCreaSede.Enabled = true;
            }
            else
            {
                AddOffCreaSede.Enabled = false;
                AddOffCreaPRef.Enabled = false;
                Populate_combobox_dummy(AddOffCreaSede);
                Populate_combobox_dummy(AddOffCreaPRef);
                AddOffCreaSede.SelectedIndex = 0;
            }
        }

        private void AddOffCreaSede_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaCliente.DataSource == null || AddOffCreaSede.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(AddOffCreaSede.SelectedValue.ToString());
            if (idsd > 0 && idcl > 0)
            {
                Populate_combobox_pref(AddOffCreaPRef, idcl, idsd);
                AddOffCreaPRef.Enabled = true;
            }
        }

        private void BtCreaOfferta_Click(object sender, EventArgs e)
        {

            UpdateFields("OC", "A", false);

            string numeroOff = AddOffCreaNOff.Text.Trim();
            string spedizioni = AddOffCreaSpedizione.Text.Trim();
            string dataoffString = AddOffCreaData.Text.Trim();

            int gestSP = Convert.ToInt16(AddOffCreaSpedizioneGest.SelectedValue.ToString());

            long idcl = Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(AddOffCreaSede.SelectedValue.ToString());
            int stato = Convert.ToInt16(AddOffCreaStato.SelectedValue.ToString());

            long idpref = -1;
            if (AddOffCreaPRef.DataSource != null)
                Convert.ToInt64(AddOffCreaPRef.SelectedValue.ToString());

            stato = (stato < 0) ? 0 : stato;

            DataValidation.ValidationResult prezzoSpedizione = new();

            string er_list = "";

            er_list += DataValidation.ValidateIdOffertaFormato(numeroOff).Error;

            DataValidation.ValidationResult dataoffValue = DataValidation.ValidateDate(dataoffString);
            er_list += dataoffValue.Error;

            DataValidation.ValidationResult answer = DataValidation.ValidateCliente(idcl);
            if (!answer.Success)
            {
                OnTopMessage.Alert(answer.Error);
                return;
            }
            else
            {
                answer = DataValidation.ValidateSede(idcl, idsd);
                if (!answer.Success)
                {
                    OnTopMessage.Alert(answer.Error);
                    return;
                }
            }


            if (idpref > 0)
            {
                answer = DataValidation.ValidatePRef(idpref);
                er_list += answer.Error;
            }

            if (!string.IsNullOrEmpty(spedizioni))
            {
                prezzoSpedizione = DataValidation.ValidateSpedizione(spedizioni, gestSP);
                er_list += prezzoSpedizione.Error;
            }

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("OC", "A", true);
                return;
            }

            Offerte.Answer esito = Offerte.GestioneOfferte.CreateOffer(dataoffValue.DateValue, numeroOff, idsd, stato, idpref, prezzoSpedizione.DecimalValue, gestSP);

            if (esito.Success)
            {
                long temp_FieldOrdOfferta = Convert.ToInt64(ComboBoxOrdOfferta.SelectedValue.ToString());
                int temp_FieldOrdCliente = Convert.ToInt32(ComboBoxOrdCliente.SelectedIndex);

                UpdateFields("OC", "CA", true);

                UpdateOfferteCrea();

                if (Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString()) == idcl)
                {
                    ComboBoxOrdCliente.SelectedIndex = temp_FieldOrdCliente;
                    if (temp_FieldOrdOfferta > 0)
                        ComboBoxOrdOfferta.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdOfferta, temp_FieldOrdOfferta);
                }
            }
            else
            {
                OnTopMessage.Error(esito.Error);
            }

            UpdateFields("OC", "A", true);

            return;
        }

        internal void LoadOfferteCreaTable(int page = 1)
        {
            DataGridView[] data_grid = new DataGridView[] { DataGridViewOffCrea };
            if (OffCreaFiltroCliente.DataSource == null)
                return;

            long idcl = Convert.ToInt64(OffCreaFiltroCliente.SelectedValue.ToString());
            int stato = Convert.ToInt32(OffCreaFiltroStato.SelectedValue.ToString());

            string addInfo = "";
            string addTable = "";
            List<string> paramsQuery = new();

            if (idcl > 0)
                paramsQuery.Add(@" OE.ID_sede IN (SELECT Id FROM " + ProgramParameters.schemadb + @"[clienti_sedi] WHERE ID_cliente = @idcl) ");
            if (stato >= 0)
                paramsQuery.Add(" OE.stato = @stato ");


            if (OffCreaFiltroClientiEliminati.Checked)
            {
                addTable += " LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS ON CS.Id = OE.ID_sede ";
                addTable += " LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE ON CE.Id = CS.ID_cliente ";
                paramsQuery.Add(" CE.deleted =  0 ");
            }

            if (paramsQuery.Count > 0)
                addInfo = " WHERE " + String.Join(" AND ", paramsQuery);

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE " + addTable + addInfo;
            int count = 1;


            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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
                    OnTopMessage.Error("Errore durante verifica records in elenco offerte. Codice: " + DbTools.ReturnErorrCode(ex));
                    return;
                }
            }


            commandText = @"SELECT  
									OE.Id AS ID,
									CE.Id || ' - ' || CE.nome AS Cliente,
									CS.Id || ' - ' ||  CS.stato || '/' || CS.provincia || '/' || CS.citta AS Sede,
									OE.codice_offerta        AS cod,
									strftime('%d/%m/%Y',OE.data_offerta) AS dat,
									REPLACE( printf('%.2f',OE.tot_offerta ),'.',',') AS totoff,
									IIF(OE.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OE.costo_spedizione ),'.',','), NULL) AS csped,
									(CASE OE.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,

									CASE OE.stato WHEN 0 THEN 'APERTA'  WHEN 1 THEN 'ORDINATA' WHEN 2 THEN 'ANNULLATA' END AS Stato,
									CASE OE.trasformato_ordine WHEN 0 THEN 'No'  WHEN 1 THEN 'Sì' END AS conv

                                    FROM " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
										ON CS.Id = OE.ID_sede
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = CS.ID_cliente
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OE.ID_riferimento " + addInfo +

                                   @" ORDER BY OE.Id DESC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);
                    cmd.SelectCommand.Parameters.AddWithValue("@stato", stato);
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                        {
                            { "ID", "ID" },
                            { "Cliente", "Cliente" },
                            { "Sede", "Sede" },
                            { "Pref", "Contatto" },
                            { "cod", "N.Offerta" },
                            { "dat", "Data" },
                            { "totoff", "Totale Offerta"+Environment.NewLine+"(Excl. Spedizioni)"},
                            { "Stato", "Stato" },
                            { "csped", "Costo Spedizione"+Environment.NewLine+"(Excl. Sconti)" },
                            { "spedg", "Gestione Costo Spedizione" },
                            { "conv", "Ordine Creato" }
                        };

                    for (int i = 0; i < data_grid.Length; i++)
                    {
                        Utility.DataSourceToDataView(data_grid[i], ds, columnNames);
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella crea offerta. Codice: " + DbTools.ReturnErorrCode(ex));

                    return;
                }
            }
            return;
        }

        internal void LoadOfferteOggettiCreaTable(long idof)
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
									   FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP
									   LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
											ON PR.Id = OP.ID_ricambio
									   WHERE OP.ID_offerta=@idofferta
									   ORDER BY OP.Id ASC;";

                using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        DataTable ds = new();
                        cmd.SelectCommand.Parameters.AddWithValue("@idofferta", idof);

                        cmd.Fill(ds);

                        Dictionary<string, string> columnNames = new()
                        {
                            { "ID", "ID" },
                            { "pezzo", "Ricambio" },
                            { "porig", "Prezzo Nell'Offerta" },
                            { "pscont", "Prezzo Scontato" },
                            { "numpezzi", "N. Pezzi" },
                            { "totparz", "Totale Parziale" }
                        };
                        Utility.DataSourceToDataView(data_grid, ds, columnNames);
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante popolamento tabella pezzi dell'offerta. Codice: " + DbTools.ReturnErorrCode(ex));


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

            long idcl = Convert.ToInt64(SelOffCreaCl.SelectedValue.ToString());

            if (idcl > 0)
                Populate_combobox_sedi(new ComboBox[] { SelOffCreaSede }, idcl);
            else
                Populate_combobox_dummy(SelOffCreaSede);
        }

        private void SelOffCreaSede_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelOffCreaCl.DataSource == null || SelOffCreaSede.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(SelOffCreaCl.SelectedValue.ToString());
            long idsd = Convert.ToInt64(SelOffCreaSede.SelectedValue.ToString());

            Populate_combobox_offerte_crea(new ComboBox[] { SelOffCrea }, idcl, idsd);
        }

        internal void SelOffCrea_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (SelOffCrea.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());

            if (curItemValue > 0)
            {

                LoadOfferteOggettiCreaTable(curItemValue);

                string commandText = @"SELECT  CS.ID_cliente as Cliente 
                                        FROM " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
                                            ON CS.Id = OE.ID_sede   
                                        WHERE OE.Id = @idofferta LIMIT 1;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.Parameters.AddWithValue("@idofferta", curItemValue);
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        long idcl = 0;

                        while (reader.Read())
                        {
                            idcl = Convert.ToInt64(reader["Cliente"].ToString());
                        }
                        reader.Close();

                        AddOffCreaOggettoClieID.Text = "" + idcl;
                        Populate_combobox_machine(new ComboBox[] { AddOffCreaOggettoMach }, idcl);
                        Populate_combobox_sedi(new ComboBox[] { AddOffCreaOggettoSede }, idcl);
                        Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, 0, true);

                        AddOffCreaOggettoSede.Enabled = true;
                        AddOffCreaOggettoMach.Enabled = true;
                        AddOffCreaOggettoRica.Enabled = true;
                        AddOffCreaOggettoPezzoFiltro.Enabled = true;

                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante selezione cliente. Codice: " + DbTools.ReturnErorrCode(ex));
                        return;
                    }
                }

                return;
            }
            else
            {
                AddOffCreaOggettoSede.Enabled = false;
                AddOffCreaOggettoMach.Enabled = false;
                AddOffCreaOggettoRica.Enabled = false;
                AddOffCreaOggettoPezzoFiltro.Enabled = false;

                Populate_combobox_dummy(AddOffCreaOggettoMach);
                Populate_combobox_dummy(AddOffCreaOggettoRica);
                Populate_combobox_dummy(AddOffCreaOggettoSede);

                AddOffCreaOggettoRica.SelectedIndex = 0;

                if (AddOffCreaOggettoMach.DataSource != null)
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

        private void AddOffCreaOggettoSede_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaOggettoSede.DataSource == null)
            {
                return;
            }

            long idsd = Convert.ToInt64(AddOffCreaOggettoSede.SelectedValue.ToString());

            if (idsd > 0)
            {
                Populate_combobox_machine(new ComboBox[] { AddOffCreaOggettoRica }, Convert.ToInt64(AddOffCreaOggettoClieID.Text), idsd);
            }

            else
            {
                Populate_combobox_machine(new ComboBox[] { AddOffCreaOggettoRica }, 0);
                AddOffCreaOggettoPori.Text = "";
                AddOffCreaOggettoPoriRic.Text = "";
                AddOffCreaOggettoPsco.Text = "";
                AddOffCreaOggettoDesc.Text = "";
                AddOffCreaOggettoPezzi.Text = "";
            }

        }

        private void AddOffCreaOggettoMach_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (AddOffCreaOggettoMach.DataSource == null)
            {
                return;
            }

            long curItem = Convert.ToInt64(AddOffCreaOggettoMach.SelectedValue.ToString());

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

            long curItemValue = Convert.ToInt64(AddOffCreaOggettoRica.SelectedValue.ToString());

            if (curItemValue > 0)
            {

                string commandText = @"SELECT 
										REPLACE(printf('%.2f',prezzo) ,'.',',') AS prezzo,
										descrizione
									   FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi]
									   WHERE Id=@idpezzo
									   ORDER BY Id ASC LIMIT 1;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                        OnTopMessage.Error("Errore durante selezione cliente. Codice: " + DbTools.ReturnErorrCode(ex));
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

            long idof = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());
            long idir = Convert.ToInt64(AddOffCreaOggettoRica.SelectedValue.ToString());

            string er_list = "";

            DataValidation.ValidationResult prezzoOrV = DataValidation.ValidatePrezzo(prezzoOr);
            er_list += prezzoOrV.Error;

            DataValidation.ValidationResult prezzoScV = DataValidation.ValidatePrezzo(prezzoSc);
            er_list += prezzoScV.Error;

            DataValidation.ValidationResult qtaV = DataValidation.ValidateQta(qta);
            er_list += qtaV.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                UpdateFields("OAO", "A", true);
                return;
            }

            Offerte.Answer esito = Offerte.GestioneOggetti.AddObjToOffer(idof, idir, prezzoOrV.DecimalValue, prezzoScV.DecimalValue, qtaV.IntValue);

            if (esito.Success)
            {
                OnTopMessage.Information("Oggetto aggiunto all'offerta");
                LoadOfferteCreaTable();
                LoadOfferteOggettiCreaTable(idof);

                UpdateFields("OAO", "A", false);
                UpdateFields("OAO", "CA", false);

                ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);
                SelOffCrea_SelectedIndexChanged(this, System.EventArgs.Empty);

                AddOffCreaOggettoRica.Enabled = true;
            }
            return;
        }

        private void DataGridViewOffCrea_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;

                    string id = row.Cells[i].Value.ToString(); i++;
                    string cliente = row.Cells[i].Value.ToString(); i++;
                    string sede = row.Cells[i].Value.ToString(); i++;
                    string nord = row.Cells[i].Value.ToString(); i++;
                    string dataoffString = row.Cells[i].Value.ToString(); i++;
                    string totOf = row.Cells[i].Value.ToString(); i++;
                    string spedizione = row.Cells[i].Value.ToString(); i++;
                    string gestsp = row.Cells[i].Value.ToString(); i++;
                    string stato = row.Cells[i].Value.ToString();

                    long id_cliente = Convert.ToInt64(cliente.Split('-')[0]);
                    long id_sede = Convert.ToInt64(sede.Split('-')[0]);

                    DataValidation.ValidationResult answer = Offerte.GetResources.GetContatto(Convert.ToInt64(id));
                    if (!String.IsNullOrEmpty(answer.Error))
                    {
                        OnTopMessage.Error(answer.Error);
                        return;
                    }
                    long id_contatto = (long)answer.LongValue;

                    AddOffCreaId.Text = id;
                    AddOffCreaSpedizione.Text = spedizione;

                    int index;

                    index = Utility.FindIndexFromValue(AddOffCreaCliente, id_cliente);
                    AddOffCreaCliente.SelectedIndex = index;

                    index = Utility.FindIndexFromValue(AddOffCreaSede, id_sede);
                    AddOffCreaSede.Enabled = false;
                    AddOffCreaSede.SelectedIndex = index;

                    Populate_combobox_pref(AddOffCreaPRef, id_cliente, id_sede);

                    AddOffCreaPRef.SelectedIndex = Utility.FindIndexFromValue(AddOffCreaPRef, id_contatto);

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

            long idOf = Convert.ToInt64(AddOffCreaId.Text.Trim());
            string numeroOff = AddOffCreaNOff.Text.Trim();
            string dataoffString = AddOffCreaData.Text.Trim();

            string spedizioni = AddOffCreaSpedizione.Text.Trim();
            int gestSP = Convert.ToInt16(AddOffCreaSpedizioneGest.SelectedValue.ToString());

            long cliente = Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString());
            long sede = Convert.ToInt64(AddOffCreaSede.SelectedValue.ToString());
            long pref = Convert.ToInt64(AddOffCreaPRef.SelectedValue.ToString());
            int stato = Convert.ToInt16(AddOffCreaStato.SelectedValue.ToString());

            DataValidation.ValidationResult answer;
            DataValidation.ValidationResult prezzoSpedizione = new();
            DataValidation.ValidationResult dataoffValue;

            string commandText;

            string er_list = "";

            answer = DataValidation.ValidateCliente(cliente);
            if (!answer.Success)
            {
                OnTopMessage.Alert(answer.Error);
                return;
            }
            else
            {
                answer = DataValidation.ValidateSede(cliente, sede);
                if (!answer.Success)
                {
                    OnTopMessage.Alert(answer.Error);
                    return;
                }
            }

            if (string.IsNullOrEmpty(numeroOff) || !Regex.IsMatch(numeroOff, @"^\d+$"))
            {
                er_list += "Numero Offerta non valido o vuoto" + Environment.NewLine;
            }

            dataoffValue = DataValidation.ValidateDate(dataoffString);
            er_list += dataoffValue.Error;

            if (pref > 0)
            {
                answer = DataValidation.ValidatePRef(pref);
                er_list += answer.Error;
            }

            if (!string.IsNullOrEmpty(spedizioni))
            {
                prezzoSpedizione = DataValidation.ValidateSpedizione(spedizioni, gestSP);
                er_list += prezzoSpedizione.Error;
            }

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi salvare le modifiche?", "Salvare Cambiamenti Offerta");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);
                return;
            }

            commandText = @"UPDATE " + ProgramParameters.schemadb + @"[offerte_elenco] 
                            SET data_offerta=@date, codice_offerta=@noff, ID_riferimento=@idref, stato=@stato, costo_spedizione=@cossp , gestione_spedizione=@gestsp 
                            WHERE Id=@idof LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.Clear();

                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@date", dataoffValue.DateValue);
                    cmd.Parameters.AddWithValue("@noff", numeroOff);
                    cmd.Parameters.AddWithValue("@stato", stato);
                    cmd.Parameters.AddWithValue("@idof", idOf);
                    cmd.Parameters.AddWithValue("@idref", (pref > 0) ? pref : DBNull.Value);

                    if (prezzoSpedizione.DecimalValue > -1)
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

                    long temp_SelOffCrea = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());

                    long temp_FieldOrdCliente = Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString());
                    long temp_FieldOrdOfferta = Convert.ToInt64(ComboBoxOrdOfferta.SelectedValue.ToString());

                    UpdateOfferteCrea(isFilter: (temp_FieldOrdCliente == cliente));

                    UpdateOrdini(OrdiniViewCurPage);

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "CA", false);
                    UpdateFields("OC", "E", false);
                    UpdateFields("OC", "A", true);

                    if (Convert.ToInt64(SelOffCreaCl.SelectedValue.ToString()) == cliente)
                        SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);

                    if (stato == 0 && temp_SelOffCrea > 0)
                        SelOffCrea.SelectedIndex = Utility.FindIndexFromValue(SelOffCrea, temp_SelOffCrea);

                    if (Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString()) == cliente)
                        ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    string temp = FieldOrdId.Text.Trim();
                    if (temp_FieldOrdCliente == cliente && String.IsNullOrEmpty(temp))
                    {
                        ComboBoxOrdCliente.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdCliente, temp_FieldOrdCliente);
                        ComboBoxOrdCliente_SelectedIndexChanged(this, EventArgs.Empty);

                        ComboBoxOrdOfferta.SelectedIndex = 0;
                        ComboBoxOrdOfferta_SelectedIndexChanged(this, EventArgs.Empty);
                    }

                    string temp_info = "";
                    if (stato == 1)
                        temp_info = Environment.NewLine + "Nel caso, è necessario creare l'ordine associato all'oferta.";

                    OnTopMessage.Information("Cambiamenti salvati." + temp_info);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento dell'OFFERTA. Codice: " + DbTools.ReturnErorrCode(ex));
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

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idOf);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare l'offerta? Tutti i dati relativi all'offerta verrano eliminati", "Eliminare Offerta");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OC", "A", true);
                UpdateFields("OC", "E", true);
                return;
            }

            string commandText = @" DELETE FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] WHERE ID_offerta=@idq; 
                                    DELETE FROM " + ProgramParameters.schemadb + @"[offerte_elenco] WHERE Id=@idq LIMIT 1;";

            using (var transaction = ProgramParameters.connection.BeginTransaction())
            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection, transaction))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idq", idQ.LongValue);
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    long temp = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());

                    UpdateOfferteCrea();

                    //DISABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "CA", true);
                    UpdateFields("OC", "E", false);
                    UpdateFields("OC", "A", true);

                    if (Convert.ToInt64(SelOffCreaCl.SelectedValue.ToString()) > 0)
                        SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);
                    if (temp > 0)
                        SelOffCrea.SelectedIndex = Utility.FindIndexFromValue(SelOffCrea, temp);

                    OnTopMessage.Information("Offerta eliminata.");
                }
                catch (SQLiteException ex)
                {
                    transaction.Rollback();
                    OnTopMessage.Error("Errore durante eliminazione dell'offferta. Codice: " + DbTools.ReturnErorrCode(ex));
                    //ABILITA CAMPI & BOTTONI
                    UpdateFields("OC", "A", true);
                    UpdateFields("OC", "E", true);
                }
            }
            return;
        }

        private void DataGridViewOffCreaOggetti_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                long id_offerta = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string pezzo = row.Cells[i].Value.ToString(); i++;
                    string porig = row.Cells[i].Value.ToString(); i++;
                    string pscont = row.Cells[i].Value.ToString(); i++;
                    string numpezzi = row.Cells[i].Value.ToString(); i++;

                    string descrizione = "";
                    long id_macchina = 0;
                    long id_cliente = 0;
                    long id_sede = 0;
                    string string_macchina = "";
                    string string_pezzo = "";
                    string temp = pezzo.Split('-')[0].Trim();
                    long idogg = 0;
                    if (!String.IsNullOrEmpty(temp))
                    {
                        idogg = Convert.ToInt64(temp);
                    }

                    string commandText = @"SELECT 
												IIF(PR.ID_macchina IS NOT NULL, (CM.Id || ' - ' || CM.modello  || ' (' ||  CM.seriale || ')'), '') AS macchina,
												IIF(PR.ID_macchina IS NOT NULL, CM.Id, 0) AS id,
												IIF(PR.ID_macchina IS NOT NULL, CM.ID_cliente, 0) AS id_cliente,
												IIF(PR.ID_macchina IS NOT NULL AND CM.ID_sede IS NOT NULL , CM.ID_sede, 0) AS id_sede,
												REPLACE( printf('%.2f',PR.prezzo), '.', ',')  AS prezzo,
												PR.descrizione AS descrizione
											FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
											LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM
												ON CM.Id = PR.ID_macchina
											WHERE PR.Id=@idogg LIMIT " + recordsPerPage;


                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@idogg", idogg);

                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                string_macchina = (reader["macchina"] == DBNull.Value) ? "" : Convert.ToString(reader["macchina"]);
                                id_macchina = (reader["id"] == DBNull.Value) ? 0 : Convert.ToInt64(reader["id"]);
                                descrizione = (reader["descrizione"] == DBNull.Value) ? "" : Convert.ToString(reader["descrizione"]);
                                id_cliente = Convert.ToInt64(reader["id_cliente"]);
                                id_sede = Convert.ToInt64(reader["id_sede"]);
                                AddOffCreaOggettoPoriRic.Text = Convert.ToString(reader["prezzo"]);
                            }
                            reader.Close();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante recupero infooggetti offerte. Codice: " + DbTools.ReturnErorrCode(ex));
                            //ABILITA CAMPI & BOTTONI
                            UpdateFields("OAO", "A", true);
                            UpdateFields("OAO", "E", true);
                        }
                    }

                    long curItem = Convert.ToInt64(AddOffCreaOggettoMach.SelectedValue.ToString());

                    AddOffCreaOggettoPori.Text = porig;
                    AddOffCreaOggettoPsco.Text = pscont;

                    AddOffCreaOggettoSede.SelectedIndexChanged -= AddOffCreaOggettoSede_SelectedIndexChanged;
                    Populate_combobox_sedi(new ComboBox[] { AddOffCreaOggettoSede }, id_cliente);
                    AddOffCreaOggettoSede.SelectedIndex = Utility.FindIndexFromValue(AddOffCreaOggettoSede, id_sede);
                    AddOffCreaOggettoSede.SelectedIndexChanged += AddOffCreaOggettoSede_SelectedIndexChanged;

                    AddOffCreaOggettoMach.SelectedIndexChanged -= AddOffCreaOggettoMach_SelectedIndexChanged;
                    Populate_combobox_machine(new ComboBox[] { AddOffCreaOggettoMach }, id_cliente, id_sede);
                    AddOffCreaOggettoMach.SelectedIndex = AddOffCreaOggettoMach.FindString(string_macchina);
                    AddOffCreaOggettoMach.SelectedIndexChanged += AddOffCreaOggettoMach_SelectedIndexChanged;

                    AddOffCreaOggettoRica.SelectedIndexChanged -= AddOffCreaOggettoRica_SelectedIndexChanged;
                    Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, id_macchina);
                    AddOffCreaOggettoRica.SelectedIndex = Utility.FindIndexFromValue(AddOffCreaOggettoRica, idogg);
                    AddOffCreaOggettoRica.SelectedIndexChanged += AddOffCreaOggettoRica_SelectedIndexChanged;

                    commandText = @"SELECT  
										PR.Id,
										PR.nome,
										PR.codice
									FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP
									JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id=OP.ID_ricambio
									WHERE OP.id=@idoff LIMIT " + recordsPerPage;


                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                            OnTopMessage.Error("Errore durante recupero infooggetti offerte. Codice: " + DbTools.ReturnErorrCode(ex));
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
                    AddOffCreaOggettoSede.Enabled = false;
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
            long idof = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());

            long selClIndex = Convert.ToInt64(SelOffCreaCl.SelectedValue.ToString());
            long selSedeIndex = Convert.ToInt64(SelOffCreaSede.SelectedValue.ToString());
            long selOfIndex = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());

            string er_list = "";

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(IdOgOfOff);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OAO", "A", true);
                UpdateFields("OAO", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare questo oggetto dall'offerta?", "Eliminare Oggetto dall'offerta");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OAO", "A", true);
                UpdateFields("OAO", "E", true);
                return;
            }

            Offerte.Answer esito = Offerte.GestioneOggetti.DeleteItemFromOffer(idof, (long)idQ.LongValue);

            if (esito.Success)
            {
                OnTopMessage.Information("Oggetto rimosso.");
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
                    SelOffCreaCl.SelectedIndex = Utility.FindIndexFromValue(SelOffCreaCl, selClIndex);
                    if (selSedeIndex > 0)
                        SelOffCreaSede.SelectedIndex = Utility.FindIndexFromValue(SelOffCreaSede, selSedeIndex);
                }

                SelOffCrea.SelectedIndex = Utility.FindIndexFromValue(SelOffCrea, selOfIndex);
            }
            else
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OAO", "A", true);
                UpdateFields("OAO", "E", true);
            }
            return;
        }

        private void BtSaveChangesOffOgg_Click(object sender, EventArgs e)
        {
            UpdateFields("OAO", "A", false);

            string prezzoOr = AddOffCreaOggettoPori.Text.Trim();
            string prezzoSc = AddOffCreaOggettoPsco.Text.Trim();
            string qta = AddOffCreaOggettoPezzi.Text.Trim();

            long idof = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());
            long idOggToOff = Convert.ToInt64(AddOffCreaOggettoId.Text.Trim());

            string er_list = "";

            DataValidation.ValidationResult prezzoOrV;
            DataValidation.ValidationResult prezzoScV;

            prezzoOrV = DataValidation.ValidatePrezzo(prezzoOr);
            er_list += prezzoOrV.Error;

            prezzoScV = DataValidation.ValidatePrezzo(prezzoSc);
            er_list += prezzoScV.Error;

            DataValidation.ValidationResult qtaV = DataValidation.ValidateQta(qta);
            er_list += qtaV.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                UpdateFields("OAO", "A", true);
                BtAddRicToOff.Enabled = false;
                return;
            }

            Offerte.Answer esito =
                Offerte.GestioneOggetti.UpdateItemFromOffer(idof, (long)idOggToOff, (decimal)prezzoOrV.DecimalValue, (decimal)prezzoScV.DecimalValue, (int)qtaV.IntValue);

            if (esito.Success)
            {
                OnTopMessage.Information("Modfiche salvate");
                LoadOfferteCreaTable();

                LoadOfferteOggettiCreaTable(idof);
                ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);

                UpdateFields("OAO", "CA", false);
                UpdateFields("OAO", "A", false);

                BtCancChangesOffOgg_Click(this, EventArgs.Empty);

                AddOffCreaOggettoRica.Enabled = true;
            }
            else
            {
                UpdateFields("OAO", "A", true);
                AddOffCreaOggettoRica.Enabled = false;
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

                long curItem = Convert.ToInt64(FieldOrdOggMach.SelectedValue.ToString());
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
            UpdateOfferteCrea(offerteCreaCurPage, isFilter: true);
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

        private void OffCreaFiltroClientiEliminati_CheckedChanged(object sender, EventArgs e)
        {
            LoadOfferteCreaTable();
        }

        //IMPORTA OFFERTE PDF
        private void BtImportaPDFOfferta_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new())
            {
                openFileDialog.InitialDirectory = ProgramParameters.exeFolderPath;
                openFileDialog.Filter = "PDF|*.pdf";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.CheckFileExists = true;
                openFileDialog.Multiselect = true;

                if (OnTopMessage.ShowOpenFileDialog(openFileDialog) != DialogResult.OK)
                {
                    return;
                }

                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (!String.IsNullOrEmpty(filePath))
                    {
                        string Offerlang = "";

                        Dictionary<string, string> offerInfo = new()
                            {
                                { "numero", "" },
                                { "cliente", "" },
                                { "data", "" }
                            };

                        List<Dictionary<string, string>> Items = new();

                        using (var document = UglyToad.PdfPig.PdfDocument.Open(filePath))
                        {
                            Dictionary<string, string> findStrLang = new()
                                {
                                    { "offerta", "ita" },
                                    { "quotation", "eng" }
                                };

                            var page = document.GetPage(1);
                            var wordsCollection = page.GetWords();

                            List<Word> words = new List<Word>();

                            Dictionary<string, Dictionary<string, string>> findStrField = GetDictionarySDSS("offerta");

                            foreach (var word in wordsCollection)
                            {
                                words.Add(new Word
                                {
                                    Value = word.Text.ToLower(),
                                    X = Convert.ToInt32(word.BoundingBox.BottomLeft.X),
                                    Y = Convert.ToInt32(word.BoundingBox.BottomLeft.Y)
                                });
                            }

                            words = words.OrderBy(a => a.X).ThenBy(a => a.Y).ToList();
                            int WordsCount = words.Count();
                            int pos;

                            for (int i = 0; i < WordsCount; i++)
                            {
                                if (Offerlang == "" && findStrLang.ContainsKey(words[i].Value))
                                {
                                    Offerlang = findStrLang[words[i].Value];
                                    i = 0;
                                }
                                else if (Offerlang != "")
                                {
                                    pos = BuildStringH(words, words[i].X, words[i].Y).IndexOf(findStrField[Offerlang]["numero"]);

                                    if (pos == 0)
                                    {
                                        offerInfo["numero"] = words[i - 1].Value;

                                        offerInfo["data"] = RemoveNotIntLeft(BuildStringH(words, words[i - 1].X, words[i - 1].Y)).Split('/')[1];
                                    }

                                    pos = BuildStringH(words, words[i].X, words[i].Y).IndexOf(findStrField[Offerlang]["cliente"]);
                                    if (pos == 0)
                                    {
                                        offerInfo["cliente"] = words[i - 1].Value;
                                    }
                                }
                            }
                        }

                        if (Offerlang == "")
                        {
                            OnTopMessage.Error("Impossibile definire lingua documento. Il documento verrà escluso.", "Errore identificazione offerta");
                            continue;
                        }


                        DataValidation.ValidationResult answer = DataValidation.ValidateIdOffertaUnica(offerInfo["numero"]);
                        if (!String.IsNullOrEmpty(answer.Error))
                        {
                            OnTopMessage.Error(answer.Error);
                            continue;
                        }

                        answer = DataValidation.ValidateDate(offerInfo["data"].Replace(".", "/"));

                        if (String.IsNullOrEmpty(answer.Error))
                        {
                            offerInfo["data"] = answer.DateValue.ToString();
                        }
                        else
                        {
                            OnTopMessage.Error("Impossibile identifacare data. Aggiornarla in seguito.");
                        }


                        string text = ExtractBodyPDF(filePath);

                        string[] lines = text.Split('\n');
                        string patterCode = @"^[0-9]+[ ]{1}([a-zA-Z]{1,}\d{1,}[-]\d{1,})\s+([0-9]+)\s+[PZ|SZ|PCE]{1,3}(\s+([0-9,.]+)\s+([0-9,.]+))?$";

                        int c = lines.Length;

                        for (int i = 0; i < c; i++)
                        {
                            string currentLine = lines[i].Trim();

                            Dictionary<string, string> itemInfo = new()
                        {
                            { "codice", "" },
                            { "qta", "" },
                            { "descrizione", "" },
                            { "prezzo_uni", "" },
                            { "prezzo_totale", "" },
                            { "prezzo_uni_scontato", "" },
                            { "prezzo_totale_scontato", "" }
                        };

                            Match code = Regex.Match(currentLine, patterCode, RegexOptions.IgnoreCase);
                            if (code.Success)
                            {
                                Dictionary<string, Dictionary<string, string>> findStrField = GetDictionarySDSS("OffertaItem");

                                int CountGroup = code.Groups.Count;

                                itemInfo["codice"] = Convert.ToString(code.Groups[1]);
                                itemInfo["qta"] = Convert.ToString(code.Groups[2]);
                                if (CountGroup > 2)
                                {
                                    // Group[3] is string of prices
                                    itemInfo["prezzo_uni"] = itemInfo["prezzo_uni_scontato"] = Convert.ToString(code.Groups[4]).Replace(".", "");
                                    itemInfo["prezzo_totale"] = itemInfo["prezzo_totale_scontato"] = Convert.ToString(code.Groups[5]).Replace(".", "");
                                }

                                i++;
                                itemInfo["descrizione"] = lines[i].Trim();
                                i++;
                                string templine = lines[i].Trim();
                                while (Regex.Match(templine, patterCode, RegexOptions.IgnoreCase).Success == false)
                                {

                                    int pos = templine.IndexOf(findStrField[Offerlang]["prezzo_uni_scontato"]);
                                    if (pos > -1)
                                    {
                                        string patterPricsDisc = @"^.+[PZ|SZ|PCE]\s+([0-9,.]+)\s+([0-9,.]+)$";
                                        Match pircesDisc = Regex.Match(templine, patterPricsDisc, RegexOptions.IgnoreCase);
                                        if (pircesDisc.Success)
                                        {
                                            itemInfo["prezzo_uni_scontato"] = Convert.ToString(pircesDisc.Groups[1]).Replace(".", "");
                                            itemInfo["prezzo_totale_scontato"] = Convert.ToString(pircesDisc.Groups[2]).Replace(".", "");
                                        }
                                    }
                                    i++;

                                    if (i == c)
                                        break;
                                    else
                                        templine = lines[i].Trim();
                                }

                                Items.Add(itemInfo);
                                i--;
                            }
                        }

                        using (ImportPdfOfferta f2 = new(offerInfo, Items, filePath))
                        {
                            f2.ShowDialog();
                            f2.Close();
                        }

                        LoadOfferteCreaTable();

                        ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);
                        SelOffCrea_SelectedIndexChanged(this, System.EventArgs.Empty);

                    }
                }
            }
        }

        private String BuildStringH(List<Word> words, int x, int y)
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

        private string RemoveNotIntLeft(string builder)
        {
            bool isInt = false;

            while (!isInt && builder.Length > 0)
            {
                if (!int.TryParse(builder.Substring(0, 1), out _))
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

        private string RemoveNotIntRight(string builder)
        {
            bool isInt = false;

            while (!isInt && builder.Length > 0)
            {
                if (!int.TryParse(builder.Substring(builder.Length - 1, 1), out _))
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
        private string ExtractBodyPDF(string filePath)
        {
            string text = "";
            using (iText.Kernel.Pdf.PdfDocument pdfDoc = new(new PdfReader(filePath)))
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

        private Dictionary<string, Dictionary<string, string>> GetDictionarySDSS(string DictCase)
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

        //CREA ORDINI

        private void ComboBoxOrdCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboBoxOrdCliente.DataSource == null)
            {
                return;
            }

            long curItemValue = Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString());

            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { ComboBoxOrdSede }, curItemValue);

                ComboBoxOrdSede.Enabled = true;
            }
            else
            {
                ComboBoxOrdSede.Enabled = false;
                ComboBoxOrdOfferta.Enabled = false;
                ComboBoxOrdContatto.Enabled = false;
                CheckBoxOrdOffertaNonPresente.Enabled = false;

                Populate_combobox_dummy(ComboBoxOrdSede);
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

        private void ComboBoxOrdSede_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboBoxOrdCliente.DataSource == null || ComboBoxOrdSede.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(ComboBoxOrdSede.SelectedValue.ToString());

            if (idsd > 0)
            {
                Populate_combobox_ordini_crea_offerta(ComboBoxOrdOfferta, idcl, idsd);
                Populate_combobox_pref(ComboBoxOrdContatto, idcl, idsd);

                ComboBoxOrdOfferta.Enabled = true;
                CheckBoxOrdOffertaNonPresente.Enabled = true;
                ComboBoxOrdContatto.Enabled = true;

                if (ComboBoxOrdOfferta.Items.Count < 2)
                    ComboBoxOrdOfferta.Enabled = false;

                return;
            }
            else
            {
                ComboBoxOrdOfferta.Enabled = false;
                CheckBoxOrdOffertaNonPresente.Enabled = false;

                Populate_combobox_pref(ComboBoxOrdContatto, idcl);
                ComboBoxOrdContatto.Enabled = true;

                Populate_combobox_dummy(ComboBoxOrdOfferta);

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

            long? curItemValue = null;

            if (ComboBoxOrdOfferta.SelectedItem == null)
                curItemValue = -1;
            else
                curItemValue = Convert.ToInt64(ComboBoxOrdOfferta.SelectedValue.ToString());

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
									   FROM " + ProgramParameters.schemadb + @"[offerte_elenco]
									   WHERE Id=@idoff
									   ORDER BY Id DESC";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                        OnTopMessage.Error("Errore durante selezione Offerta. Codice: " + DbTools.ReturnErorrCode(ex));
                        return;
                    }
                }
            }
            else
            {
                if (ComboBoxOrdOfferta.Items.Count < 2)
                {
                    Populate_combobox_dummy(ComboBoxOrdOfferta);
                    ComboBoxOrdOfferta.Enabled = false;
                }
                UpdateFields("OCR", "CA", false, false);
                UpdateFields("OCR", "A", false);
            }

            return;
        }

        private void FieldOrdSconto_Leave(object sender, System.EventArgs e)
        {
            string sconto = FieldOrdSconto.Text.Trim();
            string prezzoIS = FieldOrdTot.Text.Trim();
            decimal prezzoI;
            DataValidation.ValidationResult scontoV;

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

            scontoV = DataValidation.ValidateSconto(sconto);
            er_list += scontoV.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                return;
            }
            FieldOrdPrezF.Text = (prezzoI * (1 - scontoV.DecimalValue / 100)).ToString("N2", ProgramParameters.nfi).Replace(".", "");
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

            DataValidation.ValidationResult prezzoFV;

            prezzoFV = DataValidation.ValidatePrezzo(prezzoF);
            er_list += prezzoFV.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("OCR", "A", true);
                return;
            }

            if (prezzoI != 0)
                FieldOrdSconto.Text = (-((decimal)prezzoFV.DecimalValue - prezzoI) / prezzoI * 100).ToString("N2", ProgramParameters.nfi);
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

            int stato = (DataGridViewOrdStato.DataSource == null) ? -1 : Convert.ToInt16(DataGridViewOrdStato.SelectedValue.ToString());
            long idcl = Convert.ToInt64(DataGridViewFilterCliente.SelectedValue.ToString());
            string numOrdineFilter = DataGridViewFilterNumOrdine.Text.Trim();

            string addInfo = "";

            List<string> paramsQuery = new();

            if (stato >= 0)
                paramsQuery.Add("OE.stato = @stato");

            if (idcl > 0)
                paramsQuery.Add("CE.Id = @idcl ");

            if (Regex.IsMatch(numOrdineFilter, @"^\d+$"))
                paramsQuery.Add(" OE.codice_ordine LIKE @numOrdineFilter");

            if (DataGridViewFilterClienteEliminato.Checked)
            {
                paramsQuery.Add(" CE.deleted =  0 ");
            }

            if (paramsQuery.Count > 0)
                addInfo = " WHERE " + String.Join(" AND ", paramsQuery);

            string commandText = @"SELECT COUNT(OE.Id) 
                                    FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] OFE 
                                        ON OFE.Id = OE.ID_offerta
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
                                        ON CS.Id = OFE.ID_sede
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
                                        ON CE.Id = CS.ID_cliente "
                                    + addInfo;
            int count = 1;


            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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
                    OnTopMessage.Error("Errore durante verifica records in elenco ordini. Codice: " + DbTools.ReturnErorrCode(ex));
                    return;
                }
            }

            commandText = @"SELECT  
									OE.Id AS ID,
									OE.codice_ordine AS codOrd,
									OFE.Id || ' - ' || OFE.codice_offerta AS IDoff,
                                    CE.Id || ' - ' || CE.nome AS Cliente,
                                    CS.Id || ' - ' ||  CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta AS Sede,
									strftime('%d/%m/%Y',OE.data_ordine) AS datOr,
									strftime('%d/%m/%Y',OE.data_ETA) AS datEta,
									REPLACE( printf('%.2f',OE.totale_ordine),'.',',')  AS totord,
                                    REPLACE(  (printf('%.2f',OE.prezzo_finale ) || ' (' || printf('%.2f',OE.sconto ) || '%)'),'.',',')  AS prezfinale,
									IIF(OE.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OE.costo_spedizione ),'.',','), NULL) AS csped,
									CASE OE.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END AS spedg,
									CASE OE.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END AS Stato

                            FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
                            LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] OFE 
                                ON OFE.Id = IIF(OE.ID_offerta IS NOT NULL ,OE.ID_offerta,0)
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
                                ON CS.Id = IIF(OE.ID_offerta IS NOT NULL , OFE.ID_sede, OE.ID_sede)
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
                                ON CE.Id = CS.ID_cliente  
                              
                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR 
                                ON CR.Id = IIF(OE.ID_offerta IS NOT NULL , OFE.ID_riferimento,  OE.ID_riferimento) "
                            + addInfo + @" 
                            ORDER BY OE.Id DESC LIMIT @recordperpage OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@stato", stato);
                    cmd.SelectCommand.Parameters.AddWithValue("@idcl", idcl);
                    cmd.SelectCommand.Parameters.AddWithValue("@numOrdineFilter", "%" + numOrdineFilter + "%");

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                        {
                        { "ID", "ID" },
                        { "codOrd", "Codice Ordine" },
                        { "IDoff", "ID - #Offerta" },
                        { "Cliente", "Cliente" },
                        { "Sede", "Sede" },
                        { "Pref", "Contatto" },
                        { "datOr", "Data Ordine" },
                        { "datEta", "Data Arrivo" },
                        { "totord", "Tot. Ordine"+Environment.NewLine+"(Excl. Spedizioni)" },
                        { "csped", "Costo Spedizione"+Environment.NewLine+"(Excl. Sconti)" },
                        { "spedg", "Gestione Costo Spedizione" },
                        { "prezfinale", "Prezzo Finale (Incl. Sconto, Excl. Sped.)" },
                        { "Stato", "Stato" }
                    };

                    for (int i = 0; i < data_grid.Length; i++)
                    {
                        Utility.DataSourceToDataView(data_grid[i], ds, columnNames);
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella Ordini. Codice: " + DbTools.ReturnErorrCode(ex));
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
                labelOffoOrd.Visible = true;
                LabelPtotFOff.Visible = true;
            }
            else
            {
                labelOffoOrd.Visible = false;
                LabelPtotFOff.Visible = false;
            }
            return;
        }

        private void BtCreaOrdine_Click(object sender, EventArgs e)
        {
            UpdateFields("OCR", "A", false);

            string commandText;

            long id_offerta = (CheckBoxOrdOffertaNonPresente.Checked == false) ? Convert.ToInt64(ComboBoxOrdOfferta.SelectedValue.ToString()) : -1;

            long? id_cl = (CheckBoxOrdOffertaNonPresente.Checked == true) ? Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString()) : null;
            long id_contatto = (CheckBoxOrdOffertaNonPresente.Checked == true && Convert.ToInt64(ComboBoxOrdContatto.SelectedValue.ToString()) > 0) ? Convert.ToInt64(ComboBoxOrdContatto.SelectedValue.ToString()) : -1;

            long idsd = Convert.ToInt64(ComboBoxOrdSede.SelectedValue.ToString());

            string n_ordine = FieldOrdNOrdine.Text.Trim();

            string dataOrdString = FieldOrdData.Text.Trim();
            string dataETAString = FieldOrdETA.Text.Trim();

            string sconto = FieldOrdSconto.Text.Trim();

            string spedizioni = FieldOrdSped.Text.Trim();
            int gestSP = Convert.ToInt32(FieldOrdSpedGestione.SelectedValue.ToString());

            string prezzo_finale = FieldOrdPrezF.Text.Trim();
            string tot_ordine = FieldOrdTot.Text.Trim();

            int stato_ordine = Convert.ToInt32(FieldOrdStato.SelectedValue.ToString());
            stato_ordine = (stato_ordine < 0) ? 0 : stato_ordine;

            DataValidation.ValidationResult answer;
            DataValidation.ValidationResult prezzoSpedizione = new();
            DataValidation.ValidationResult dataOrdValue;
            DataValidation.ValidationResult dataETAOrdValue;
            DataValidation.ValidationResult tot_ordineV = new();
            DataValidation.ValidationResult prezzo_finaleV = new();
            DataValidation.ValidationResult scontoV;

            string er_list = "";

            if (CheckBoxOrdOffertaNonPresente.Checked)
            {
                answer = DataValidation.ValidateCliente((int)id_cl);
                if (!answer.Success)
                {
                    OnTopMessage.Alert(answer.Error);
                    return;
                }
                er_list += answer.Error;
            }

            if (string.IsNullOrEmpty(n_ordine) || !Regex.IsMatch(n_ordine, @"^\d+$"))
            {
                er_list += "Numero Ordine non valido o vuoto" + Environment.NewLine;
            }

            dataOrdValue = DataValidation.ValidateDate(dataOrdString);
            er_list += dataOrdValue.Error;

            dataETAOrdValue = DataValidation.ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            if (DateTime.Compare(dataOrdValue.DateValue, dataETAOrdValue.DateValue) > 0)
            {
                er_list += "Data di Arrivo(ETA) antecedente a quella di creazione dell'ordine" + Environment.NewLine;
            }

            if (!string.IsNullOrEmpty(spedizioni))
            {
                if (!string.IsNullOrEmpty(spedizioni))
                {
                    prezzoSpedizione = DataValidation.ValidateSpedizione(spedizioni, gestSP);
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
                tot_ordineV = DataValidation.ValidatePrezzo(tot_ordine);
                er_list += tot_ordineV.Error;

                prezzo_finaleV = DataValidation.ValidatePrezzo(prezzo_finale);
                er_list += prezzo_finaleV.Error;

                prezzo_finaleV = DataValidation.ValidatePrezzo(prezzo_finale);
                er_list += prezzo_finaleV.Error;
            }

            scontoV = DataValidation.ValidateSconto(sconto);
            er_list += scontoV.Error;


            if (CheckBoxOrdOffertaNonPresente.Checked == false)
            {
                commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[offerte_elenco] WHERE [Id] = @id_offerta LIMIT 1;";
                int OfferExist = 0;

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@id_offerta", id_offerta);

                        OfferExist = Convert.ToInt32(cmd.ExecuteScalar());
                        if (OfferExist < 1)
                        {
                            er_list += "Offerta non valida" + Environment.NewLine;
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante verifica ID Offerta. Codice: " + DbTools.ReturnErorrCode(ex));
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("OCR", "A", true);
                return;
            }
            Ordini.Answer esito = Ordini.GestioneOrdini.CreateOrder(n_ordine, id_offerta, CheckBoxOrdOffertaNonPresente.Checked, CheckBoxCopiaOffertainOrdine.Checked, idsd, id_contatto,
                                                                        dataOrdValue.DateValue, dataETAOrdValue.DateValue,
                                                                     tot_ordineV.DecimalValue, scontoV.DecimalValue, prezzo_finaleV.DecimalValue, stato_ordine, gestSP, prezzoSpedizione.DecimalValue
                                                                     );

            if (esito.Success)
            {
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

                OnTopMessage.Information("Ordine Creato.");

                DateTime today = DateTime.Today;
                FieldOrdData.Text = today.ToString("dd/MM/yyyy");
                FieldOrdETA.Text = today.ToString("dd/MM/yyyy");
            }
            else
            {
                OnTopMessage.Error(esito.Error);
            }

            UpdateFields("OCR", "A", true);

            return;
        }

        internal void ComboSelOrd_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboSelOrd.DataSource == null || ComboSelOrdCl.DataSource == null)
            {
                return;
            }

            if (Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString()) < 1)
            {
                ComboSelOrd.Enabled = false;
            }

            long id_ordine = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

            if (id_ordine > 0)
            {
                IdsInOfferOrder = UpdateOrdiniOggettiOfferta(id_ordine);
                UpdateOrdiniOggetti(id_ordine);
                CheckBoxOrdOggCheckAddNotOffer.Enabled = true;

                long idcl = Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString());

                Populate_combobox_sedi(new ComboBox[] { FieldOrdOggSede }, idcl);
                Populate_combobox_machine(new ComboBox[] { FieldOrdOggMach }, idcl);

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
                FieldOrdOggSede.Enabled = false;
                FieldOrdOggSede.SelectedIndex = 0;

                return;
            }
        }

        private List<long> UpdateOrdiniOggettiOfferta(long id_ordine)
        {

            string commandText = @"SELECT 

										OFE.Id AS id,
                                        
										PR.nome  || IIF(PR.deleted = 1,'(Rimosso da Database)','') AS nome_pezzo,
										PR.codice AS code_pezzo,

										REPLACE( printf('%.2f',OFE.prezzo_unitario_originale ),'.',',')  AS puo,
										REPLACE( printf('%.2f',OFE.prezzo_unitario_sconto ),'.',',')  AS pus,

										OFE.pezzi AS qta,
                                        OFE.pezzi_aggiunti AS qtaAdd,
										PR.descrizione AS descrizione,
                                        OFE.ID_ricambio AS ID_ricambio
									   
										FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OFE 

										LEFT JOIN " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
											ON OE.ID_offerta  = OFE.ID_offerta 
										LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR 
											ON PR.Id = OFE.ID_ricambio 

									   WHERE OE.id = @id_ordine;";


            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.SelectCommand.Parameters.AddWithValue("@id_ordine", id_ordine);
                    DataTable ds = new();
                    cmd.Fill(ds);

                    DataGridView data_grid = DataGridViewOrdOffOgg;

                    IdsInOfferOrder = new List<long>();

                    if (IdsInOfferOrder.Count() > 0)
                    {
                        foreach (DataRow row in ds.Rows)
                        {
                            if (Int64.TryParse(row["ID_ricambio"].ToString(), out long id_ric))
                                IdsInOfferOrder.Add(Convert.ToInt64(id_ric));
                        }
                    }
                    ds.Columns.Remove("ID_ricambio");


                    Dictionary<string, string> columnNames = new()
                    {
                        { "id", "ID" },
                        { "idpez,", "ID Ricambio" },
                        { "puo", "Prezzo Originale" },
                        { "pus", "Prezzo Finale" },
                        { "qta", "Quantità" },
                        { "qtaAdd", "Quantità in Ordine" },
                        { "nome_pezzo", "Nome Pezzo" },
                        { "code_pezzo", "Codice Pezzo" },
                        { "descrizione", "Descrizione" }
                    };

                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante riempimento oggetti offerte. Codice: " + DbTools.ReturnErorrCode(ex));
                    return null;
                }
            }

            return IdsInOfferOrder;
        }

        private void UpdateOrdiniOggetti(long id_ordine)
        {

            string commandText = @"SELECT 
										OP.Id AS id,
										PR.nome  || IIF(PR.deleted = 1,'(Rimosso da Database)','') AS nome_pezzo,
										PR.codice AS code_pezzo,
										REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS puo,
										REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pus,
										OP.pezzi AS qta,
										PR.descrizione AS descrizione,
										op.ETA as ETA
									   FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP
										LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
											ON PR.Id=OP.ID_ricambio
									   WHERE OP.ID_ordine=@idofferta;";


            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.SelectCommand.Parameters.AddWithValue("@idofferta", id_ordine);
                    DataTable ds = new();
                    cmd.Fill(ds);
                    DataGridView data_grid = DataGridViewOrdOgg;

                    Dictionary<string, string> columnNames = new()
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

                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante oggetti ordini. Codice: " + DbTools.ReturnErorrCode(ex));
                    return;
                }
            }
        }

        private void DataGridViewOrd_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }

            if (dgv.SelectedRows.Count == 1)
            {
                CheckBoxOrdOffertaNonPresente.Checked = false;
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    int i = 0;
                    string id = row.Cells[i].Value.ToString(); i++;
                    string codOrd = row.Cells[i].Value.ToString(); i++;
                    string offerta = Convert.ToString(row.Cells[i].Value.ToString().Trim()); i++;
                    string cliente = row.Cells[i].Value.ToString(); i++;
                    string sede = row.Cells[i].Value.ToString(); i++;
                    string datOrd = row.Cells[i].Value.ToString(); i++;
                    string datETA = row.Cells[i].Value.ToString(); i++;
                    string totOrd = row.Cells[i].Value.ToString(); i++;
                    string[] subs = row.Cells[i].Value.ToString().Split('('); i++;

                    string spedizione = row.Cells[i].Value.ToString(); i++;
                    string gestSp = row.Cells[i].Value.ToString(); i++;
                    string stato = row.Cells[i].Value.ToString(); i++;

                    string pfinale = subs[0].Trim();
                    string sconto;
                    if (subs.Length > 1)
                        sconto = Regex.Replace(subs[1], @"[^,.\d]", "").Trim();
                    else
                        sconto = "";

                    long cliente_id = Convert.ToInt64(cliente.Split('-')[0]);
                    long sede_id = Convert.ToInt64(sede.Split('-')[0]);

                    DataValidation.ValidationResult answer = Ordini.GetResources.GetContatto(Convert.ToInt64(id));

                    if (!String.IsNullOrEmpty(answer.Error))
                    {
                        OnTopMessage.Error(answer.Error);
                        return;
                    }
                    long id_contatto = (long)answer.LongValue;

                    ComboBoxOrdCliente.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdCliente, cliente_id);
                    ComboBoxOrdSede.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdSede, sede_id);
                    ComboBoxOrdSede.Enabled = false;

                    ComboBoxOrdContatto.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdContatto, id_contatto);

                    string ID_offerta_str = offerta.Split('-')[0].Trim();
                    if (int.TryParse(ID_offerta_str, out int ID_offerta))
                    {
                        Populate_combobox_ordini_crea_offerta(ComboBoxOrdOfferta, cliente_id, sede_id, transformed: false, codice: ID_offerta);
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
            ComboBoxOrdSede.Enabled = false;
            ComboBoxOrdContatto.Enabled = false;

            CheckBoxOrdOffertaNonPresente.Checked = false;
        }

        private void DataGridViewOrdOffOgg_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }

            RetrunRowDataDataGridView(DataGridViewOrdOffOgg, dgv.CurrentCell.RowIndex);
        }

        private void RetrunRowDataDataGridView(DataGridView dgv, int rowIndex)
        {

            long idOrdine = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

            int i = 0;
            string idpez = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string nome = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string codice = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string puo = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string pus = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string qta = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string qtaAdd = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;
            string desc = dgv.Rows[rowIndex].Cells[i].Value.ToString(); i++;

            string ETA = "";
            string mach = "";
            long idricambio = 0;
            long idsede = 0;
            long idmach = 0;

            string commandText = @"SELECT 
											OP.data_ETA AS ETA,
											IIF(PR.ID_macchina IS NOT NULL, CM.Id  , 0) AS ID,
											IIF(PR.ID_macchina IS NOT NULL,   (CM.Id || ' - ' || CM.modello  || ' (' ||  CM.seriale || ')'), '') AS macchina,
											PR.Id AS ID_ricambio,
                                            IIF(PR.ID_macchina IS NOT NULL, CM.ID_sede , 0)  AS ID_sede

                                            FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OP, " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OFP
                                            LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
                                                ON PR.Id = OFP.ID_ricambio
                                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM
                                                ON CM.Id=PR.ID_macchina

                                            WHERE OP.id=@idOrdine AND OFP.Id=@idpez LIMIT " + recordsPerPage;


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                        idricambio = Convert.ToInt64(reader["ID_ricambio"]);
                        idsede = Convert.ToInt64(reader["ID_sede"]);
                        idmach = Convert.ToInt64(reader["ID"]);
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante oggetti ordini. Codice: " + DbTools.ReturnErorrCode(ex));
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

            FieldOrdOggSede.SelectedIndex = Utility.FindIndexFromValue(FieldOrdOggSede, idsede);
            FieldOrdOggMach.SelectedIndex = Utility.FindIndexFromValue(FieldOrdOggMach, idmach);
            FieldOrdOggPezzo.SelectedIndex = Utility.FindIndexFromValue(FieldOrdOggPezzo, idricambio);

            FieldOrdOggDesc.Text = desc;

            old_dataETAOrdValue.Text = ETA;
            old_prezzo_scontatoV.Text = pus;
            old_pezziV.Text = qta;

            UpdateFields("OCR", "E", false);
            UpdateFields("OCR", "FE", true);

            BtChiudiOrdOgg.Enabled = true;
            BtCreaOrdineOgg.Enabled = true;

        }

        private void DataGridViewOrdOgg_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    long idOrdine = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

                    int i = 0;
                    string idpez = row.Cells[i].Value.ToString(); i++;
                    string nome = row.Cells[i].Value.ToString(); i++;
                    string codice = row.Cells[i].Value.ToString(); i++;
                    string puo = row.Cells[i].Value.ToString(); i++;
                    string pus = row.Cells[i].Value.ToString(); i++;
                    string qta = row.Cells[i].Value.ToString(); i++;
                    string desc = row.Cells[i].Value.ToString(); i++;
                    string ETA = row.Cells[i].Value.ToString(); i++;

                    long mach = 0;
                    long id_sede = 0;
                    long index = 0;
                    bool isnotoffer = false;

                    string commandText = @"SELECT 

											PR.ID_macchina AS macchina,
                                            ORP.ID_ricambio as pezzo,
                                            ORP.Outside_Offer as isnotoffer,
                                            IIF(PR.ID_macchina IS NULL, 0, CM.ID_sede) AS ID_sede

									   FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OP, " + ProgramParameters.schemadb + @"[ordine_pezzi] AS ORP

									   LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
										ON PR.Id=ORP.ID_ricambio
									   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM
										ON CM.Id=PR.ID_macchina

									   WHERE OP.id=@idOrdine AND ORP.Id=@idpez LIMIT 1;";


                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                    {
                        try
                        {
                            cmd.Parameters.AddWithValue("@idOrdine", idOrdine);
                            cmd.Parameters.AddWithValue("@idpez", idpez);

                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                mach = (reader["macchina"] == DBNull.Value) ? -1 : (int)reader["macchina"];
                                index = Convert.ToInt64(reader["pezzo"]);
                                id_sede = Convert.ToInt64(reader["ID_sede"]);
                                isnotoffer = Convert.ToBoolean(reader["isnotoffer"]);
                            }
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante oggetti ordini. Codice: " + DbTools.ReturnErorrCode(ex));
                            return;
                        }
                    }

                    CheckBoxOrdOggCheckAddNotOffer.CheckedChanged -= FieldOrdOggCheckAddNotOffer_CheckedChanged;
                    CheckBoxOrdOggCheckAddNotOffer.Checked = isnotoffer;
                    CheckBoxOrdOggCheckAddNotOffer.CheckedChanged += FieldOrdOggCheckAddNotOffer_CheckedChanged;

                    CheckBoxOrdOggCheckAddNotOffer.Enabled = false;

                    if (id_sede > 0)
                        FieldOrdOggSede.SelectedIndex = Utility.FindIndexFromValue(FieldOrdOggSede, id_sede);

                    if (mach > 0)
                        FieldOrdOggMach.SelectedIndex = Utility.FindIndexFromValue(FieldOrdOggMach, mach);

                    Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, mach);
                    FieldOrdOggPezzo.SelectedIndex = Utility.FindIndexFromValue(FieldOrdOggPezzo, index);

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

            long idoggOff = (String.IsNullOrEmpty(FieldOrdOggId.Text.Trim())) ? 0 : Convert.ToInt64(FieldOrdOggId.Text.Trim());
            long idordine = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

            string dataETAString = FieldOrdOggETA.Text.Trim();
            string prezzo_originale = FieldOrdOggPOr.Text.Trim();
            string prezzo_scontato = FieldOrdOggPsc.Text.Trim();
            string pezzi = FieldOrdOggQta.Text.Trim();
            long idiri;

            DataValidation.ValidationResult dataETAOrdValue;
            DataValidation.ValidationResult prezzo_originaleV;
            DataValidation.ValidationResult prezzo_scontatoV;

            string er_list = "";

            if (CheckBoxOrdOggCheckAddNotOffer.Checked == true)
            {
                idiri = Convert.ToInt64(FieldOrdOggPezzo.SelectedValue.ToString());
            }
            else
            {
                idiri = Convert.ToInt64(FieldOrdOggIdRic.Text);
            }

            if (idiri < 1)
            {
                er_list += "Selezionare un ricambio dal menù a tendina." + Environment.NewLine;
            }

            dataETAOrdValue = DataValidation.ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            prezzo_originaleV = DataValidation.ValidatePrezzo(prezzo_originale);
            er_list += prezzo_originaleV.Error;

            prezzo_scontatoV = DataValidation.ValidatePrezzo(prezzo_scontato);
            er_list += prezzo_originaleV.Error;

            DataValidation.ValidationResult qtaP = DataValidation.ValidateQta(pezzi);
            er_list += qtaP.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
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

            Ordini.Answer esito = Ordini.GestioneOggetti.AddObjToOrder(idordine, idiri, dataETAOrdValue.DateValue, (decimal)prezzo_originaleV.DecimalValue, (decimal)prezzo_scontatoV.DecimalValue, (int)qtaP.IntValue,
                                                                        CheckBoxOrdOggCheckAddNotOffer.Checked, CheckBoxOrdOggSconto.Checked, idoggOff);

            if (esito.Success)
            {
                if (Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]) == true)
                {
                    Outlook.Application OlApp = new();
                    Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);
                    Ordini.GestioneOrdini.UpdateCalendarOnObj(idordine, personalCalendar);
                }

                long currentOrd = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

                UpdateFields("OCR", "CE", false);
                UpdateFields("OCR", "E", false);
                UpdateFields("OCR", "FE", false);

                ComboBoxOrdOfferta_SelectedIndexChanged(this, System.EventArgs.Empty);

                UpdateOrdini(OrdiniCurPage);
                ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                UpdateFields("OCR", "CA", false);
                UpdateFields("OCR", "A", false);

                ComboSelOrd.SelectedIndex = Utility.FindIndexFromValue(ComboSelOrd, currentOrd);

                int i = 0;
                foreach (ComboBoxList item in ComboSelOrd.Items)
                {
                    if (item.Value == idordine)
                    {
                        ComboSelOrd.SelectedIndex = i;
                    }
                    i++;
                }

                OnTopMessage.Information("Oggetto aggiunto all'ordine");
            }
            else
            {

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
            }
            return;
        }

        private void BtDelOrd_Click(object sender, EventArgs e)
        {
            //DISABILITA CAMPI
            UpdateFields("OCR", "E", false);
            UpdateFields("OCR", "A", false);

            string idOr = FieldOrdId.Text.Trim();

            //temporary fix - When order selected in create order and save changes to related offert, all buttons actived
            if (String.IsNullOrEmpty(idOr))
            {
                UpdateFields("OCR", "AE", false);
                BtCreaOrdine.Enabled = true;
                return;
            }

            string er_list = "";

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(idOr);
            er_list += idQ.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
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

            DialogResult dialogResult = OnTopMessage.Question("Vuoi veramente eliminare l'ordine? Tutti i dati relativi all'ordine verrano eliminati", "Eliminare Ordine");
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


            string commandText = "SELECT  ID_offerta FROM " + ProgramParameters.schemadb + @"[ordini_elenco] WHERE Id=@idord LIMIT 1;";


            using (SQLiteCommand cmd2 = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd2.CommandText = commandText;
                    cmd2.Parameters.AddWithValue("@idord", idQ.LongValue);

                    var Id_offerta_result = cmd2.ExecuteScalar();

                    int? id_offerta = (Id_offerta_result == DBNull.Value) ? null : (int?)Convert.ToInt64(cmd2.ExecuteScalar());

                    commandText = "";
                    if (id_offerta > 0)
                    {
                        commandText = @"UPDATE " + ProgramParameters.schemadb + @"[offerte_pezzi]
									        SET
										        pezzi_aggiunti = 0 
									        WHERE 
										        ID_offerta = @idoff;

                                        UPDATE " + ProgramParameters.schemadb + @"[offerte_elenco] SET trasformato_ordine=0 WHERE Id=@idoff LIMIT 1;
                                    ";
                    }

                    commandText += @"DELETE FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] WHERE ID_ordine=@idq;
                                        DELETE  FROM " + ProgramParameters.schemadb + @"[ordini_elenco] WHERE Id=@idq LIMIT 1;";

                    using (var transaction = ProgramParameters.connection.BeginTransaction())
                    using (SQLiteCommand cmd_up = new(commandText, ProgramParameters.connection, transaction))
                    {
                        try
                        {
                            cmd_up.CommandText = commandText;
                            cmd_up.Parameters.AddWithValue("@idoff", id_offerta);
                            cmd_up.Parameters.AddWithValue("@idq", idQ.LongValue);

                            cmd_up.ExecuteNonQuery();
                            transaction.Commit();

                            if (Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]) == true)
                            {
                                string ordinecode = "";
                                DateTime eta = DateTime.MinValue;

                                commandText = @"SELECT 
                                                codice_ordine,
                                                data_ETA
                                            FROM " + ProgramParameters.schemadb + @"[ordini_elenco] 
                                            WHERE Id=@idord LIMIT 1;";

                                using (SQLiteCommand cmd3 = new(commandText, ProgramParameters.connection))
                                {
                                    try
                                    {
                                        cmd3.CommandText = commandText;
                                        cmd3.Parameters.AddWithValue("@idord", idQ.LongValue);

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
                                        OnTopMessage.Error("Errore durante lettura dati ordine(dati calendario). Codice: " + DbTools.ReturnErorrCode(ex));
                                    }
                                }

                                Outlook.Application OlApp = new();
                                Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

                                if (!String.IsNullOrEmpty(ordinecode) && CalendarManager.FindAppointment(personalCalendar, ordinecode))
                                {
                                    dialogResult = OnTopMessage.Question("Vuoi eliminare l'evento associato all'ordine?", "Eliminazione Evento Ordine Calendario");
                                    if (dialogResult == DialogResult.Yes)
                                    {
                                        CalendarManager.RemoveAppointment(personalCalendar, ordinecode);
                                    }
                                }

                            }

                            long temp = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

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

                            if (Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString()) > 0)
                                ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                            if (temp > 0 && idQ.LongValue != temp)
                                ComboSelOrd.SelectedIndex = Utility.FindIndexFromValue(ComboSelOrd, temp);

                            OnTopMessage.Information("Ordine eliminato.");

                        }
                        catch (SQLiteException ex)
                        {

                            transaction.Rollback();

                            OnTopMessage.Error("Errore durante eliminazione ordine. Codice: " + DbTools.ReturnErorrCode(ex));

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
                    OnTopMessage.Error("Errore durante aggiornamento offerta(select offerta). Codice: " + DbTools.ReturnErorrCode(ex));
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

            string IdOggOrd = FieldOrdOggId.Text.Trim();

            string er_list = "";

            DataValidation.ValidationResult idQ = DataValidation.ValidateId(IdOggOrd);
            er_list += idQ.Error;


            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "FE", true);
                UpdateFields("OCR", "E", true);

                return;
            }

            DialogResult dialogResult = OnTopMessage.Question("Vuoi rimuovere il pezzo dall'ordine?", "Eliminare Pezzo da Ordine");
            if (dialogResult == DialogResult.No)
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "FE", true);
                UpdateFields("OCR", "E", true);
                return;
            }

            bool updateFprice = false;
            bool updateFpriceSconto = false;

            dialogResult = OnTopMessage.Question("Vuoi aggiornare il prezzo finale?", "Eliminare Pezzo da Ordine");
            if (dialogResult == DialogResult.Yes)
            {
                updateFprice = true;
                dialogResult = OnTopMessage.Question("Applicare lo sconto al prezzo finale?", "Eliminare Pezzo da Ordine");
                if (dialogResult == DialogResult.Yes)
                {
                    updateFpriceSconto = true;
                }
            }

            long idordine = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

            Ordini.Answer esito = Ordini.GestioneOggetti.DeleteObjFromOrder(idordine, (long)idQ.LongValue, updateFprice, updateFpriceSconto);

            if (esito.Success)
            {
                if (Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]) == true)
                {
                    Outlook.Application OlApp = new();
                    Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);
                    Ordini.GestioneOrdini.UpdateCalendarOnObj(idordine, personalCalendar);
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

                OnTopMessage.Information("Oggetti eliminati dall'ordine.");
            }
            else
            {
                //ABILITA CAMPI & BOTTONI
                UpdateFields("OCR", "FE", true);
                UpdateFields("OCR", "E", true);
                return;
            }
            return;
        }

        private void BtSaveModOrd_Click(object sender, EventArgs e)
        {
            string idordinestr = FieldOrdId.Text.Trim();

            //temporary fix - When order selected in create order and save changes to related offert, all buttons actived
            if (String.IsNullOrEmpty(idordinestr))
            {
                UpdateFields("OCR", "AE", false);
                BtCreaOrdine.Enabled = true;
                return;
            }

            long id_ordine = Convert.ToInt64(idordinestr);

            string n_ordine = FieldOrdNOrdine.Text.Trim();

            string dataOrdString = FieldOrdData.Text.Trim();
            string dataETAString = FieldOrdETA.Text.Trim();

            string sconto = FieldOrdSconto.Text.Trim();

            string spedizioni = FieldOrdSped.Text.Trim();
            int gestSP = Convert.ToInt16(FieldOrdSpedGestione.SelectedValue.ToString());

            string prezzo_finale = FieldOrdPrezF.Text.Trim();
            string tot_ordine = FieldOrdTot.Text.Trim();

            int stato_ordine = Convert.ToInt16(FieldOrdStato.SelectedValue.ToString());
            stato_ordine = (stato_ordine < 0) ? 0 : stato_ordine;

            DataValidation.ValidationResult dataOrdValue;
            DataValidation.ValidationResult dataETAOrdValue;

            DataValidation.ValidationResult prezzoSpedizione = new();
            DataValidation.ValidationResult scontoV;
            DataValidation.ValidationResult tot_ordineV;
            DataValidation.ValidationResult prezzo_finaleV;

            string er_list = "";
            if (string.IsNullOrEmpty(n_ordine) || !Regex.IsMatch(n_ordine, @"^\d+$"))
            {
                er_list += "Numero Ordine non valido o vuoto" + Environment.NewLine;
            }

            dataOrdValue = DataValidation.ValidateDate(dataOrdString);
            er_list += dataOrdValue.Error;

            dataETAOrdValue = DataValidation.ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            if (DateTime.Compare(dataOrdValue.DateValue, dataETAOrdValue.DateValue) > 0)
            {
                er_list += "Data di Arrivo(ETA) antecedente a quella di creazione dell'ordine" + Environment.NewLine;
            }

            scontoV = DataValidation.ValidateSconto(sconto);
            er_list += scontoV.Error;

            tot_ordineV = DataValidation.ValidatePrezzo(tot_ordine);
            er_list += tot_ordineV.Error;

            prezzo_finaleV = DataValidation.ValidatePrezzo(prezzo_finale);
            er_list += prezzo_finaleV.Error;

            if (!string.IsNullOrEmpty(spedizioni))
            {
                prezzoSpedizione = DataValidation.ValidateSpedizione(spedizioni, gestSP);
                er_list += prezzoSpedizione.Error;
            }

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);
                UpdateFields("OCR", "A", true);
                BtCreaOrdine.Enabled = false;
                return;
            }

            DialogResult res = OnTopMessage.Question("Vuoi salvare le modifiche all'ordine?", "Conferma Salvataggio Modifiche Ordine", MessageBoxButtons.OKCancel);
            if (res != DialogResult.OK)
            {
                UpdateFields("OCR", "A", true);
                return;
            }

            string oldRef = "";
            DateTime oldETA = DateTime.MinValue;
            decimal oldPrezF = 0;
            int oldStato = -1;

            string commandText = @"SELECT 
                                                    codice_ordine,
                                                    data_ETA,
                                                    prezzo_finale,
                                                    stato
                                                FROM " + ProgramParameters.schemadb + @"[ordini_elenco] WHERE Id=@idord LIMIT " + recordsPerPage;

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                    OnTopMessage.Error("Errore durante eliminazione ordine (aggiornamento toast). Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }

            commandText = @"UPDATE " + ProgramParameters.schemadb + @"[ordini_elenco] SET 
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

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@codo", n_ordine);
                    cmd.Parameters.AddWithValue("@dataord", dataOrdValue.DateValue);
                    cmd.Parameters.AddWithValue("@dataeta", dataETAOrdValue.DateValue);
                    cmd.Parameters.AddWithValue("@totord", tot_ordineV.DecimalValue);
                    cmd.Parameters.AddWithValue("@sconto", scontoV.DecimalValue);
                    cmd.Parameters.AddWithValue("@prezzoF", prezzo_finaleV.DecimalValue);
                    cmd.Parameters.AddWithValue("@stato", stato_ordine);
                    cmd.Parameters.AddWithValue("@idord", id_ordine);
                    if (gestSP > -1)
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

                    long temp = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

                    UpdateOrdini(OrdiniCurPage);
                    UpdateFields("OCR", "CA", true);
                    UpdateFields("OCR", "A", false);
                    UpdateFields("VS", "E", false);

                    BtChiudiOrd_Click(this, System.EventArgs.Empty);

                    if (Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString()) > 0)
                        ComboSelOrdCl_SelectedIndexChanged(this, EventArgs.Empty);

                    if (temp > 0)
                        ComboSelOrd.SelectedIndex = Utility.FindIndexFromValue(ComboSelOrd, temp);

                    UpdateOfferteCrea(offerteCreaCurPage);


                    if (Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]) == true)
                    {
                        Outlook.Application OlApp = new Outlook.Application();
                        Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);
                        if (CalendarManager.FindAppointment(personalCalendar, oldRef))
                        {
                            bool removed = false;
                            if (oldStato != stato_ordine && stato_ordine == 1)
                            {
                                res = OnTopMessage.Question("L'ordine è stato chiuso, vuoi rimuoverlo dal calendario?", "Conferma Rimozione Ordine da Calendario", MessageBoxButtons.OKCancel);
                                if (res != DialogResult.OK)
                                {
                                    CalendarManager.RemoveAppointment(personalCalendar, oldRef);
                                    removed = true;
                                }
                            }
                            if (removed == false)
                            {
                                if (DateTime.Compare(oldETA, dataETAOrdValue.DateValue) == 0 && (oldPrezF != prezzo_finaleV.DecimalValue || oldRef != n_ordine))
                                {
                                    res = OnTopMessage.Question("Vuoi aggiornare l'evento del calendario relativo alll'ordine con le nuove informazioni?", "Conferma Aggiornamento Ordine Calendario", MessageBoxButtons.OKCancel);
                                    if (res != DialogResult.Yes)
                                    {
                                        CalendarManager.UpdateCalendar(personalCalendar, oldRef, n_ordine, id_ordine, dataETAOrdValue.DateValue, false);
                                    }
                                }
                                else if (DateTime.Compare(oldETA, dataETAOrdValue.DateValue) != 0)
                                {
                                    res = OnTopMessage.Question("Vuoi aggiornare l'evento del calendario relativo alll'ordine con le nuove informazioni?" + Environment.NewLine + "L'evento verrà cancellato per poi essere inserito nuovamente.", "Conferma Aggiornamento Ordine Calendario", MessageBoxButtons.OKCancel);
                                    if (res != DialogResult.Yes)
                                    {
                                        CalendarManager.UpdateCalendar(personalCalendar, oldRef, n_ordine, id_ordine, dataETAOrdValue.DateValue);
                                    }
                                }
                            }
                        }
                    }

                    OnTopMessage.Information("Ordine Aggiornato.");

                    DateTime today = DateTime.Today;
                    FieldOrdData.Text = today.ToString("dd/MM/yyyy");
                    FieldOrdETA.Text = today.ToString("dd/MM/yyyy");
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento ordine. Codice: " + DbTools.ReturnErorrCode(ex));
                    UpdateFields("OCR", "A", true);
                }
            }
            return;
        }

        private void BtSaveModOrdOgg_Click(object sender, EventArgs e)
        {
            long idoggOrd = Convert.ToInt64(FieldOrdOggId.Text.Trim());
            long idordine = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());

            string dataETAString = FieldOrdOggETA.Text.Trim();
            string prezzo_originale = FieldOrdOggPOr.Text.Trim();
            string prezzo_scontato = FieldOrdOggPsc.Text.Trim();
            string pezzi = FieldOrdOggQta.Text.Trim();

            DataValidation.ValidationResult prezzo_originaleV;
            DataValidation.ValidationResult prezzo_scontatoV;
            DataValidation.ValidationResult dataETAOrdValue;

            string er_list = "";

            dataETAOrdValue = DataValidation.ValidateDate(dataETAString);
            er_list += dataETAOrdValue.Error;

            prezzo_originaleV = DataValidation.ValidatePrezzo(prezzo_originale);
            er_list += prezzo_originaleV.Error;

            prezzo_scontatoV = DataValidation.ValidatePrezzo(prezzo_scontato);
            er_list += prezzo_scontatoV.Error;


            DataValidation.ValidationResult qtaP = DataValidation.ValidateQta(pezzi);
            er_list += qtaP.Error;

            if (!string.IsNullOrEmpty(er_list))
            {
                OnTopMessage.Alert(er_list);

                FieldOrdOggPOr.Enabled = true;
                FieldOrdOggPsc.Enabled = true;
                FieldOrdOggQta.Enabled = true;
                FieldOrdOggETA.Enabled = true;

                BtChiudiOrdOgg.Enabled = true;
                BtDelOrdOgg.Enabled = true;
                BtSaveModOrdOgg.Enabled = true;

                return;
            }

            DialogResult res = OnTopMessage.Question("Vuoi salvare le modifiche all'oggetto?", "Conferma Salvataggio Modifiche Oggetto Ordine", MessageBoxButtons.OKCancel);
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

            Ordini.Answer esito = Ordini.GestioneOggetti.UpdateItemFromOrder(idordine, idoggOrd, (decimal)prezzo_originaleV.DecimalValue, (decimal)prezzo_scontatoV.DecimalValue, (int)qtaP.IntValue, dataETAOrdValue.DateValue, CheckBoxOrdOggSconto.Checked);

            if (esito.Success)
            {
                if (Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]) == true)
                {
                    if (Convert.ToDecimal(old_prezzo_scontatoV.Text) != prezzo_scontatoV.DecimalValue || Convert.ToInt32(old_pezziV.Text) != qtaP.IntValue || DateTime.Compare(Convert.ToDateTime(old_dataETAOrdValue.Text).Date, dataETAOrdValue.DateValue) != 0)
                    {
                        Outlook.Application OlApp = new();
                        Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);
                        Ordini.GestioneOrdini.UpdateCalendarOnObj(idordine, personalCalendar);
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

                OnTopMessage.Information("Oggetto aggiornato.");
            }
            else
            {
                UpdateFields("OAO", "E", false);
                UpdateFields("OCR", "FE", true);
            }
            return;
        }

        private void ComboSelOrdCl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboSelOrdCl.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString());

            if (idcl > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { ComboSelOrdSede }, idcl);
                Populate_combobox_ordini(ComboSelOrd, idcl);
                ComboSelOrd.Enabled = true;
                ComboSelOrdSede.Enabled = true;
            }
            else
            {
                ComboSelOrd.Enabled = false;
                ComboSelOrdSede.Enabled = false;
                Populate_combobox_dummy(ComboSelOrdSede);
                Populate_combobox_dummy(ComboSelOrd);

                ComboSelOrd.SelectedIndex = 0;
                ComboSelOrdSede.SelectedIndex = 0;
            }
        }

        private void ComboSelOrdSede_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboSelOrdSede.DataSource == null || ComboSelOrdCl.DataSource == null)
            {
                return;
            }

            long idcl = Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString());
            long idsd = Convert.ToInt64(ComboSelOrdSede.SelectedValue.ToString());
            int pos = 0;

            if (ComboSelOrd.DataSource != null)
            {
                long idor = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());
                pos = Utility.FindIndexFromValue(ComboSelOrd, idor);
            }

            ComboSelOrd.SelectedIndexChanged -= ComboSelOrd_SelectedIndexChanged;

            if (idcl > 0 && idsd > 0)
            {
                Populate_combobox_ordini(ComboSelOrd, idcl, idsd);
                ComboSelOrd.Enabled = true;
            }
            else if (idcl > 0)
            {
                Populate_combobox_ordini(ComboSelOrd, idcl);
            }
            else
            {
                Populate_combobox_dummy(ComboSelOrd);
            }

            ComboSelOrd.SelectedIndex = pos;
            ComboSelOrd.SelectedIndexChanged += ComboSelOrd_SelectedIndexChanged;

            if (pos < 1)
            {
                ComboSelOrd_SelectedIndexChanged(ComboSelOrd, EventArgs.Empty);
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
            long idcl = Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(ComboBoxOrdSede.SelectedValue.ToString());
            int idcl_index = ComboBoxOrdCliente.SelectedIndex;
            long idcont = Convert.ToInt64(ComboBoxOrdContatto.SelectedValue.ToString());

            if (idcl > 0)
            {

                UpdateFields("OCR", "CA", false);
                UpdateFields("OCR", "E", false);
                UpdateFields("OCR", "A", true);
                UpdateFields("OCR", "AE", false);
                ComboBoxOrdCliente.Enabled = true;
                ComboBoxOrdContatto.Enabled = false;

                ComboBoxOrdCliente.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdCliente, idcl);
                ComboBoxOrdSede.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdSede, idsd);

                if (idcont > 0)
                {
                    Populate_combobox_pref(ComboBoxOrdContatto, idcl);
                    ComboBoxOrdContatto.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdContatto, idcont);
                }
            }
            else
            {
                ComboBoxOrdCliente.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdCliente, idcl);
                ComboBoxOrdSede.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdSede, idsd);
            }

            if (CheckBoxOrdOffertaNonPresente.Checked)
            {
                ComboBoxOrdOfferta.Enabled = false;
                ComboBoxOrdOfferta.SelectedIndex = 0;

                ComboBoxOrdContatto.Enabled = true;
                FieldOrdTot.Text = "0";
                FieldOrdPrezF.Text = "0";

                CheckBoxCopiaOffertainOrdine.Enabled = false;
                CheckBoxCopiaOffertainOrdine.Checked = false;

                FieldOrdNOrdine.Enabled = true;
            }
            else
            {
                ComboBoxOrdSede_SelectedIndexChanged(this, EventArgs.Empty);

                if (idcont > 0)
                {
                    ComboBoxOrdContatto.SelectedIndex = Utility.FindIndexFromValue(ComboBoxOrdContatto, idcont);
                }

                ComboBoxOrdOfferta.Enabled = true;
                ComboBoxOrdContatto.Enabled = true;

                CheckBoxCopiaOffertainOrdine.Enabled = true;
                CheckBoxCopiaOffertainOrdine.Checked = true;

                if (ComboBoxOrdOfferta.SelectedIndex < 1)
                    FieldOrdNOrdine.Enabled = false;

            }

            return;
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

                FieldOrdOggSede.Enabled = true;
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
                FieldOrdOggSede.Enabled = false;

                CheckBoxOrdOggSconto.Enabled = false;

                long curItem = Convert.ToInt64(FieldOrdOggMach.SelectedValue.ToString());
                Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, curItem > 0 ? curItem : 0);

                if (Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString()) > 0)
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

                    long curItem = Convert.ToInt64(FieldOrdOggMach.SelectedValue.ToString());
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

            long id_mach = Convert.ToInt64(FieldOrdOggMach.SelectedValue.ToString());

            id_mach = (id_mach > 0) ? id_mach : 0;
            Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, id_mach, true);
        }

        private void FieldOrdOggPezzo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FieldOrdOggPezzo.DataSource == null)
            {
                return;
            }

            long id_ricambio = Convert.ToInt64(FieldOrdOggPezzo.SelectedValue.ToString());

            if (id_ricambio > 0)
            {

                if (IdsInOfferOrder != null && IdsInOfferOrder.Contains(id_ricambio))
                {
                    RetrunRowDataDataGridView(DataGridViewOrdOffOgg, IdsInOfferOrder.IndexOf(id_ricambio));
                    return;
                }

                if (CheckBoxOrdOggCheckAddNotOffer.Checked)
                {


                    string commandText = @"SELECT 
										prezzo
									   FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi]
									   WHERE Id=@id_ricambio 
                                        LIMIT 1;";


                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                    {
                        try
                        {

                            cmd.Parameters.AddWithValue("@id_ricambio", id_ricambio);
                            FieldOrdOggPOr.Text = Convert.ToString(cmd.ExecuteScalar());
                            FieldOrdOggPsc.Text = FieldOrdOggPOr.Text;
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante recupero prezzo ricambio. Codice: " + DbTools.ReturnErorrCode(ex));

                            return;
                        }
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

        private void FieldOrdOggSede_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (ComboSelOrdCl.DataSource == null || ComboSelOrd.DataSource == null)
            {
                return;
            }

            long idsd = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());
            long idcl = Convert.ToInt64(ComboSelOrdCl.SelectedValue.ToString());

            if (idsd > 0)
            {
                Populate_combobox_machine(new ComboBox[] { FieldOrdOggMach }, idcl, idsd);
            }
            else
            {
                Populate_combobox_machine(new ComboBox[] { FieldOrdOggMach }, idcl);
            }
        }

        private void DataGridViewFilterClienteEliminato_CheckedChanged(object sender, EventArgs e)
        {
            LoadOrdiniTable();
        }


        //IMPORTA ORDINE
        private void BtImportaPDFOrdini_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new())
            {
                openFileDialog.InitialDirectory = ProgramParameters.exeFolderPath;
                openFileDialog.Filter = "PDF|*.pdf";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.CheckFileExists = true;
                openFileDialog.Multiselect = true;

                if (OnTopMessage.ShowOpenFileDialog(openFileDialog) != DialogResult.OK)
                {
                    return;
                }

                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (!String.IsNullOrEmpty(filePath))
                    {
                        string lang = "";

                        Dictionary<string, string> orderInfo = new()
                        {
                            { "numero", "" },
                            { "cliente", "" },
                            { "numeroOff", "" },
                            { "data", "" },
                            { "ETA", "" }
                        };

                        List<Dictionary<string, string>> Items = new();


                        using (var document = UglyToad.PdfPig.PdfDocument.Open(filePath))
                        {
                            Dictionary<string, string> findStrLang = new()
                            {
                                { "d'ordine", "ita" },
                                { "order", "eng" },
                                { "order confirmation", "eng" }

                            };

                            var page = document.GetPage(1);
                            var wordsCollection = page.GetWords();
                            List<Word> words = new List<Word>();
                            Dictionary<string, Dictionary<string, string>> findStrField = GetDictionarySDSS("Ordine");

                            foreach (var word in wordsCollection)
                            {
                                words.Add(new Word
                                {
                                    Value = word.Text.ToLower(),
                                    X = Convert.ToInt32(word.BoundingBox.BottomLeft.X),
                                    Y = Convert.ToInt32(word.BoundingBox.BottomLeft.Y)
                                });
                            }

                            words = words.OrderBy(a => a.X).ThenBy(a => a.Y).ToList();
                            int WordsCount = words.Count();
                            int pos;

                            for (int i = 0; i < WordsCount; i++)
                            {

                                if (lang == "" && findStrLang.ContainsKey(words[i].Value))
                                {
                                    lang = findStrLang[words[i].Value];
                                    i = 0;
                                }
                                else if (lang != "")
                                {
                                    foreach (KeyValuePair<string, string> searchstr in findStrField[lang])
                                    {
                                        if (orderInfo[searchstr.Key] != "")
                                        {
                                            continue;
                                        }

                                        string retruned = BuildStringH(words, words[i].X, words[i].Y);
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
                                                    orderInfo[searchstr.Key] = BuildStringH(words, words[i - 1].X, words[i - 1].Y).Split('/')[1];
                                                }
                                            }
                                            else
                                            {
                                                if (searchstr.Key == "ETA")
                                                    orderInfo[searchstr.Key] = BuildStringH(words, words[i - 1].X, words[i - 1].Y);
                                                else
                                                    orderInfo[searchstr.Key] = words[i - 1].Value;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        orderInfo["ETA"] = ReturnEtaImportOrder(orderInfo["ETA"]);
                        orderInfo["data"] = RemoveNotIntLeft(RemoveNotIntRight(orderInfo["data"]));

                        if (lang == "")
                        {
                            OnTopMessage.Error("Impossibile definire lingua documento. Il documento verrà escluso.", "Errore identificazione offerta");
                            continue;
                        }


                        DataValidation.ValidationResult answer = DataValidation.ValidateIdOrdineUnico(orderInfo["numero"]);
                        if (!String.IsNullOrEmpty(answer.Error))
                        {
                            OnTopMessage.Error(answer.Error);
                            continue;
                        }

                        string pattern = @"[^\d.]";
                        string replace = "";

                        orderInfo["data"] = Regex.Replace(orderInfo["data"], pattern, replace);

                        answer = DataValidation.ValidateDate(orderInfo["data"].Replace(".", "/"));

                        if (String.IsNullOrEmpty(answer.Error))
                        {
                            orderInfo["data"] = answer.DateValue.ToString();
                        }
                        else
                        {
                            OnTopMessage.Error("Impossibile identificare data. Aggiornarla in seguito.");
                        }

                        //Items Extarction
                        string text = ExtractBodyPDF(filePath);

                        string[] lines = text.Split('\n');
                        string patterCode = @"^[0-9]+[ ]{1}([a-zA-Z]{1}\d{1,}[-]\d{1,})\s+([0-9]+)\s+[PZ|SZ|PCE]{1,3}(\s+([0-9,.]+)\s+([0-9,.]+))?$";
                        string etaCode = @"^\s*(Data cons\.:)\s*(.+)$";

                        int c = lines.Length;

                        for (int i = 0; i < c; i++)
                        {
                            string currentLine = lines[i].Trim();

                            Dictionary<string, string> itemInfo = new()
                                {
                                    { "codice", "" },
                                    { "qta", "" },
                                    { "descrizione", "" },
                                    { "prezzo_uni", "" },
                                    { "prezzo_totale", "" },
                                    { "prezzo_uni_scontato", "" },
                                    { "prezzo_totale_scontato", "" },
                                    { "eta", "" }
                                };

                            Match code = Regex.Match(currentLine, patterCode, RegexOptions.IgnoreCase);

                            if (code.Success)
                            {
                                Dictionary<string, Dictionary<string, string>> findStrField = GetDictionarySDSS("OrdineItem");

                                int CountGroup = code.Groups.Count;

                                itemInfo["codice"] = Convert.ToString(code.Groups[1]);
                                itemInfo["qta"] = Convert.ToString(code.Groups[2]);
                                if (CountGroup > 2)
                                {
                                    // Group[3] is string of prices
                                    itemInfo["prezzo_uni"] = itemInfo["prezzo_uni_scontato"] = Convert.ToString(code.Groups[4]).Replace(".", "");
                                    itemInfo["prezzo_totale"] = itemInfo["prezzo_totale_scontato"] = Convert.ToString(code.Groups[5]).Replace(".", "");
                                }

                                i++;
                                itemInfo["descrizione"] = lines[i].Trim();
                                i++;
                                string templine = lines[i].Trim();
                                while (Regex.Match(templine, patterCode, RegexOptions.IgnoreCase).Success == false)
                                {

                                    int pos = templine.IndexOf(findStrField[lang]["prezzo_uni_scontato"]);
                                    if (pos > -1)
                                    {
                                        string patterPricsDisc = @"^.+[PZ|SZ|PCE]\s+([0-9,.]+)\s+([0-9,.]+)$";
                                        Match pircesDisc = Regex.Match(templine, patterPricsDisc, RegexOptions.IgnoreCase);
                                        if (pircesDisc.Success)
                                        {
                                            itemInfo["prezzo_uni_scontato"] = Convert.ToString(pircesDisc.Groups[1]).Replace(".", "");
                                            itemInfo["prezzo_totale_scontato"] = Convert.ToString(pircesDisc.Groups[2]).Replace(".", "");
                                        }
                                    }
                                    else
                                    {
                                        Match eta = Regex.Match(templine, etaCode, RegexOptions.IgnoreCase);
                                        if (eta.Success)
                                            itemInfo["eta"] = eta.Groups[2].ToString().Trim();
                                    }

                                    i++;

                                    if (i == c)
                                        break;
                                    else
                                        templine = lines[i].Trim();
                                }

                                if (!String.IsNullOrEmpty(itemInfo["eta"]))
                                    itemInfo["eta"] = ReturnEtaImportOrder(itemInfo["eta"]);

                                Items.Add(itemInfo);
                                i--;
                            }
                        }

                        Offerte.Answer checkOfferta = Offerte.GestioneOfferte.GetIfTransformed(orderInfo["numeroOff"].Trim());

                        if (checkOfferta.Success && checkOfferta.LongValue > 0 && checkOfferta.Bool)
                        {
                            if (OnTopMessage.Question("Parrebbe che l'offerta sia già stata convertita in ordine, ma il codice dell'ordine non è stato trovato. Procedere comunque?", "Offerta già convertita") != DialogResult.Yes)
                                return;
                        }

                        using (ImportPdfOrdine f2 = new(orderInfo, Items, filePath))
                        {
                            f2.ShowDialog();
                            f2.Close();
                            f2.Dispose();
                        }

                        UpdateOrdini();

                        ComboSelOrd_SelectedIndexChanged(this, System.EventArgs.Empty);
                        SelOffCrea_SelectedIndexChanged(this, System.EventArgs.Empty);

                    }
                }
            }
        }

        private string ReturnEtaImportOrder(string line)
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

        //VISUALIZZA ORDINI
        private void LoadVisualizzaOrdiniTable(int page = 1)
        {
            DataGridView[] data_grid = new DataGridView[] { DataGridViewVisualizzaOrdini };

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[ordini_elenco];";
            int count = 1;

            using (SQLiteCommand cmdCount = new(commandText, ProgramParameters.connection))
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
                    OnTopMessage.Error("Errore durante verifica records in visualizza ordini. Codice: " + DbTools.ReturnErorrCode(ex));


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
                                (CE.Id || ' - ' || CE.nome ) AS Cliente,
                                (CS.Id || ' -' ||  CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta) AS Sede,
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

                                FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
                                LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] OFE 
                                    ON OFE.Id = OE.ID_offerta 
                                LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
                                    ON CS.Id = OFE.ID_sede
                                LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
                                    ON CE.Id = CS.ID_cliente 
                                LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR 
                                    ON CR.Id = OFE.ID_riferimento 
                                WHERE OE.ID_offerta IS NOT NULL " + addInfo + @" 

                                UNION ALL

                                SELECT  
									OE.Id AS ID,
									OE.codice_ordine AS codOrd,
									'' AS IDoff,
									(CE.Id || ' - ' || CE.nome ) AS Cliente,
                                    (CS.Id || ' -' ||  CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta) AS Sede,
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

                                    FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
                                        ON CS.Id = OE.ID_sede
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = CS.ID_cliente 
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR 
										ON CR.Id = OE.ID_riferimento 
                                    WHERE OE.ID_offerta IS NULL " + addInfo + @" 

                                    ORDER BY OE.Id DESC LIMIT " + recordsPerPage + " OFFSET @startingrecord;";

            page--;

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {

                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", (page) * recordsPerPage);
                    cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
                        {
                        { "ID", "ID" },
                        { "codOrd", "Codice Ordine" },
                        { "IDoff", "ID - #Offerta" },
                        { "Cliente", "Cliente" },
                        { "Sede", "Sede" },
                        { "Pref", "Contatto" },
                        { "datOr", "Data Ordine" },
                        { "datEta", "Data Arrivo" },
                        { "totord", "Tot. Ordine"+Environment.NewLine+"(Exl. Sconti e Sped." },
                        { "prezfinale", "Prezzo Finale"+Environment.NewLine+"(Sconti e Spedizione)" },
                        { "spesesped", "Costo Spedizione"+Environment.NewLine+"(Excl. Sconti)" },
                        { "spedg", "Gestione Costo Spedizione" },
                        { "Stato", "Stato" }
                    };

                    for (int i = 0; i < data_grid.Length; i++)
                    {
                        Utility.DataSourceToDataView(data_grid[i], ds, columnNames);
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella Visualizzazione Ordini. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }
            return;
        }

        private void LoaVisOrdOggTable(long id_ordine = 0)
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

									FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
                                        ON PR.Id = OP.ID_ricambio
								   
                                    WHERE OP.ID_ordine=@idord 
									GROUP BY OP.Id, PR.nome, PR.codice, OP.prezzo_unitario_originale, OP.prezzo_unitario_sconto, OP.pezzi, OP.ETA, PR.descrizione
									ORDER BY OP.Id;";

            using (SQLiteDataAdapter cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    DataTable ds = new();
                    cmd.SelectCommand.Parameters.AddWithValue("@idord", id_ordine);

                    cmd.Fill(ds);

                    Dictionary<string, string> columnNames = new()
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
                    Utility.DataSourceToDataView(data_grid, ds, columnNames);
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante popolamento tabella oggetti ordini. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
            return;
        }

        private void DataGridViewVisualizzaOrdini_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView dgv)
            {
                return;
            }
            if (dgv.SelectedRows.Count == 1)
            {
                foreach (DataGridViewRow row in dgv.SelectedRows)
                {
                    long idOrdine = Convert.ToInt64(row.Cells[0].Value.ToString());

                    UpdateFields("VS", "CA", true);

                    string commandText = @"SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,

												IIF(OP.costo_spedizione IS NOT NULL,REPLACE( printf('%.2f',OP.costo_spedizione ),'.',','), NULL) AS costo_sped,
												(CASE OP.gestione_spedizione WHEN 0 THEN 'Exlude from Tot.' WHEN 1 THEN 'Add total & No Discount' WHEN 2 THEN 'Add Tot with Discount' ELSE '' END) AS spedg,

												CE.nome as clnome,
												CS.stato as clstato,
												CS.provincia as clprov,
												CS.citta as clcitt,

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

                                        FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OP
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
                                            ON OE.Id = OP.ID_offerta
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
                                            ON CS.Id = OE.ID_sede
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
                                            ON CE.Id = CS.ID_cliente
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR
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
												CS.stato as clstato,
												CS.provincia as clprov,
												CS.citta as clcitt,

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

                                        FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OP
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
                                            ON CS.Id = OP.ID_sede
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE  
                                            ON CE.Id = CS.ID_cliente
                                        LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR
                                            ON CR.Id = OP.ID_riferimento
                                        WHERE OP.ID_offerta IS NULL AND OP.id = @idOrdine LIMIT 1;";


                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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

                            LoaVisOrdOggTable(idOrdine);
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante recupero info visualizzaaione ordine. Codice: " + DbTools.ReturnErorrCode(ex));

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

        private void CreaEventoCalendario_Click(object sender, EventArgs e)
        {
            UpdateFields("VS", "E", false);

            string nordine = VisOrdNumero.Text;
            string opde = VisOrdETA.Text;

            DataValidation.ValidationResult dateAppoint = new()
            {
                DateValue = DateTime.MinValue
            };

            DataValidation.ValidationResult dataETAOrdValue;


            dataETAOrdValue = DataValidation.ValidateDate(opde);

            if (dataETAOrdValue.Error != null)
            {
                OnTopMessage.Alert(dataETAOrdValue.Error);
                UpdateFields("VS", "E", true);
                return;
            }

            Outlook.Application OlApp = new();
            Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);
            if (!CalendarManager.FindAppointment(personalCalendar, nordine))
            {
                DialogResult dialogResult = OnTopMessage.Question("Creare l'appuntamento? Una volta creato, sarà necessario salvarlo." + Environment.NewLine + Environment.NewLine
                                                            + "ATTENZIONE: NON rimuovere la stringa finale ##ManaOrdini[numero_ordine]## dal titolo dell'appunatmento. Serve per riconoscere l'evento.", "Creazione Appuntamento Calendario");
                if (dialogResult != DialogResult.Yes)
                {
                    UpdateFields("VS", "E", true);
                    return;
                }

                while (dateAppoint.DateValue == DateTime.MinValue)
                {
                    string input = Interaction.InputBox("Inserire data in cui ricevere la notifica relativa all'ordine.", "Data Notifica Ordine", (dataETAOrdValue.DateValue).ToString(ProgramParameters.dateFormat));
                    if (String.ReferenceEquals(input, String.Empty))
                    {
                        OnTopMessage.Alert("Azione Cancellata");
                        UpdateFields("VS", "E", true);
                        return;
                    }

                    dateAppoint = DataValidation.ValidateDate(input);

                    if (dateAppoint.Error != null)
                    {
                        OnTopMessage.Alert("Controllare formato data. Impossibile convertire in formato data corretto.");
                        dateAppoint.DateValue = DateTime.MinValue;
                        continue;
                    }
                    else if (DateTime.Compare(dateAppoint.DateValue, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint.DateValue, dataETAOrdValue.DateValue) > 0)
                    {
                        DialogResult confDataLaterOrder = OnTopMessage.Question("La data scelta va oltre alla data di consegna dell'ordine, continuare?" + Environment.NewLine + "Se necessario aggiornare l'ETA dell'ordine.", "Creazione Appuntamento Calendario");
                        if (confDataLaterOrder == DialogResult.No)
                        {
                            dateAppoint.DateValue = DateTime.MinValue;
                            continue;
                        }
                    }
                    else if (DateTime.Compare(dateAppoint.DateValue, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint.DateValue, DateTime.Now.Date) < 0)
                    {
                        DialogResult confDataLaterOrder = OnTopMessage.Question("La data scelta è antecedente alla dato odierna, continuare?", "Creazione Appuntamento Calendario");
                        if (confDataLaterOrder == DialogResult.No)
                        {
                            dateAppoint.DateValue = DateTime.MinValue;
                            continue;
                        }
                    }
                }

                string body = CalendarManager.CreateAppointmentBody(Convert.ToInt64(VisOrdId.Text.Trim()));

                CalendarManager.AddAppointment(personalCalendar, nordine, body, dateAppoint.DateValue);

            }
            else
            {
                OnTopMessage.Information("Evento già presente. Rimuoverlo o aggiornarlo se necessario.");
            }

            UpdateFields("VS", "E", true);
            return;
        }

        private void RimuoviEventoCalendario_Click(object sender, EventArgs e)
        {
            string nordine = VisOrdNumero.Text;
            string ETA = VisOrdETA.Text;

            DataValidation.ValidationResult dataETAOrdValue;
            dataETAOrdValue = DataValidation.ValidateDate(ETA);

            if (dataETAOrdValue.Error != null)
            {
                OnTopMessage.Alert("Data non valida o vuota");
                return;
            }
            else
            {
                dataETAOrdValue.DateValue = dataETAOrdValue.DateValue.AddDays(1);
            }

            Outlook.Application OlApp = new();
            Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

            if (CalendarManager.FindAppointment(personalCalendar, nordine))
            {
                CalendarManager.RemoveAppointment(personalCalendar, nordine);
            }
            else
            {
                OnTopMessage.Alert("Evento non presente.");
            }

        }

        private void AggiornaEventoCalendario_Click(object sender, EventArgs e)
        {
            UpdateFields("VS", "E", false);
            string oldRef = VisOrdNumero.Text;
            string newRef = VisOrdNumero.Text;
            long id_ordine = Convert.ToInt64(VisOrdId.Text);
            DateTime estDate = Convert.ToDateTime(VisOrdETA.Text);

            Outlook.Application OlApp = new();
            Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

            if (CalendarManager.FindAppointment(personalCalendar, oldRef))
            {
                CalendarManager.UpdateCalendar(personalCalendar, oldRef, newRef, id_ordine, estDate, false);
            }
            else
            {
                OnTopMessage.Alert("Evento non presente.");
            }

            UpdateFields("VS", "E", true);

        }

        private void AggiornaEventoDataCalendario_Click(object sender, EventArgs e)
        {
            UpdateFields("VS", "E", false);

            string newRef = VisOrdNumero.Text;

            Outlook.Application OlApp = new();
            Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

            CalendarManager.AggiornaDataCalendario(personalCalendar, newRef);


            UpdateFields("VS", "E", true);
            return;
        }

        private void DuplicatiEventoCalendario_Click(object sender, EventArgs e)
        {
            UpdateFields("VS", "E", false);
            string newRef = VisOrdNumero.Text;

            Outlook.Application OlApp = new();
            Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

            CalendarManager.FindCalendarDuplicate(personalCalendar, newRef);

            UpdateFields("VS", "E", true);

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
                        OnTopMessage.Alert(element + " non è una email valida.");
                        return;
                    }
                }
            }

            if (UserSettings.settings["calendario"]["nomeCalendario"] != nomeCal)
            {
                DialogResult dialogResult = OnTopMessage.Question("Stai per cambiare nome al calendario, il software proverà a spostare gli eventi pianificati da oggi in avanti nel nuovo calendario. In caso di errori, gli eventi rimanenti dovranno essere modificati manualmente. Continuare?", "Cambio Nome CAlendario - Aggiornamento Eventi Calendario");
                if (dialogResult == DialogResult.Yes)
                {
                    Outlook.Application OlApp = new();
                    if (CalendarManager.MoveAppointment(OlApp, UserSettings.settings["calendario"]["nomeCalendario"], nomeCal) == false)
                    {
                        OnTopMessage.Error("Errore: Il nome è stato aggiornato, ma non è stato possibile spostare alcuni eventi. Controllare manualemnte");
                    }
                    UserSettings.settings["calendario"]["nomeCalendario"] = nomeCal;
                }
                else
                {
                    settingCalendarioNome.Text = UserSettings.settings["calendario"]["nomeCalendario"];
                }
            }

            UserSettings.settings["calendario"]["aggiornaCalendario"] = Convert.ToString(upCalendar);
            UserSettings.settings["calendario"]["destinatari"] = destinatari;

            UserSettings.UpdateSettingApp();

            OnTopMessage.Information("Impostazioni Salvate");
        }

        //PAGE NAVIGATION
        private void GoToPageGridView(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

                TextBox pageBox = (TextBox)sender;
                TextBox txtboxCurPage;
                Label maxpageLabel;
                int selCurValue;

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

                if (!int.TryParse(page, out int pagev))
                {
                    OnTopMessage.Alert("Numero pagina non valido");
                    txtboxCurPage.Text = Convert.ToString(selCurValue);
                    return;
                }


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
                case "DatiClientiSediPrvPage":
                    selCurValue = datiGridViewClientiSediCurPage;
                    txtboxCurPage = DataClientiSediCurPage;
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
                case "DatiClientiSediPrvPage":
                    datiGridViewClientiSediCurPage = selCurValue;
                    LoadSedeTable(selCurValue);
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
                case "DatiClientiSediNxtPage":
                    maxpageLabel = MaxPageDataClientiSedi;
                    selCurValue = datiGridViewClientiSediCurPage;
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
                case "DatiClientiSediNxtPage":
                    datiGridViewClientiSediCurPage = selCurValue;
                    LoadSedeTable(selCurValue);
                    break;
            }
        }

        //POPULTAE FUNCTIONS
        private void Populate_combobox_machine(ComboBox[] nome_ctr, long idcl = 0, long idsede = 0, int deleted = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            List<string> cond = new List<string>();
            string joined = "";

            if (idcl > 0)
                cond.Add("ID_cliente=@idcl");

            if (idsede > 0)
                cond.Add("ID_sede=@idsd");

            if (cond.Count > 0)
                joined = " AND " + String.Join(" AND ", cond.ToArray());

            string commandText = "SELECT Id,modello,seriale FROM " + ProgramParameters.schemadb + @"[clienti_macchine] WHERE deleted = @deleted " + joined + " ORDER BY seriale ASC;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@idsd", idsede);
                    cmd.Parameters.AddWithValue("@deleted", deleted);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} ({1})", reader["modello"], reader["seriale"]), Value = Convert.ToInt64(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_machine. Codice: " + DbTools.ReturnErorrCode(ex));

                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }
        }

        private void Populate_combobox_ricambi(ComboBox[] nome_ctr, long idmc = 0, bool offpezziSel = false, int deleted = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };
            string extenQuery = "";
            long idoff = 0;
            string filter = "";

            if (offpezziSel == true && SelOffCrea.DataSource != null)
            {
                idoff = Convert.ToInt64(SelOffCrea.SelectedValue.ToString());
                extenQuery += @" AND Id NOT IN (
                                                    SELECT ID_ricambio FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] WHERE ID_offerta=@idoff 

                                                    UNION 

                                                    SELECT OP.ID_ricambio 
                                                        FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP 
                                                        INNER JOIN " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
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

            string commandText = @"SELECT Id,nome,codice FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] WHERE ID_macchina IS NULL AND deleted = @deleted " + extenQuery + " ORDER BY Id ASC;";

            if (idmc > 0)
                commandText = "SELECT Id,nome,codice FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] WHERE (ID_macchina=@idmc) AND deleted = @deleted " + extenQuery + " ORDER BY Id ASC;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idmc", idmc);
                    cmd.Parameters.AddWithValue("@idoff", idoff);
                    cmd.Parameters.AddWithValue("@filterstr", filter);
                    cmd.Parameters.AddWithValue("@deleted", deleted);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} ({2})", reader["Id"], reader["codice"], reader["nome"]), Value = Convert.ToInt64(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_ricambi. Codice: " + DbTools.ReturnErorrCode(ex));
                    return;
                }
            }

            //Setup data binding
            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }

        }

        private void Populate_combobox_ricambi_ordine(ComboBox[] nome_ctr, long idmc = 0, bool offpezziSel = false)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };
            string extenQuery = "";
            long idOrd = 0;
            string filter = "";

            if (ComboSelOrd.DataSource != null)
            {
                idOrd = Convert.ToInt64(ComboSelOrd.SelectedValue.ToString());
            }

            string macchina;
            if (idmc > 0)
                macchina = " ID_macchina = @idmc ";
            else
                macchina = " ID_macchina IS NULL ";

            if (!String.IsNullOrEmpty(FieldOrdOggPezzoFiltro_Text))
            {
                filter = "%" + FieldOrdOggPezzoFiltro_Text + "%";
                extenQuery += " AND ( Id LIKE @filterstr OR nome LIKE @filterstr OR codice LIKE @filterstr)  ";
            }

            string commandText = @"SELECT 
                                        Id,
                                        nome,
                                        codice 
                                    FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] 
                                    WHERE " + macchina + extenQuery +
                                    " ORDER BY Id ASC;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idmc", idmc);
                    cmd.Parameters.AddWithValue("@idOrd", idOrd);
                    cmd.Parameters.AddWithValue("@filterstr", filter);
                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} ({2})", reader["Id"], reader["codice"], reader["nome"]), Value = Convert.ToInt64(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_ricambi. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }

            //Setup data binding
            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }

        }

        private void Populate_combobox_fornitore(ComboBox[] nome_ctr, int deleted = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string commandText = "SELECT Id,nome FROM " + ProgramParameters.schemadb + @"[fornitori] WHERE deleted = @deleted ORDER BY Id ASC;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {

                try
                {
                    cmd.Parameters.AddWithValue("@deleted", deleted);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1}", reader["Id"], reader["nome"]), Value = Convert.ToInt64(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_fornitore. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }


        }

        private void Populate_combobox_offerte_crea(ComboBox[] nome_ctr, long idcl = 0, long idsd = 0)
        {
            string queryExtra = "";

            if (idcl > 0)
            {
                queryExtra += " AND ID_cliente=@idcl ";
            }
            if (idsd > 0)
            {
                queryExtra += " AND ID_sede=@idsd ";
            }

            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string commandText = @"SELECT 
									OE.Id AS id,
									OE.codice_offerta AS noff,
									CE.nome  AS cliente,
									(CS.stato || ' - ' || CS.provincia || ' - ' || CS.citta ) AS sede

									FROM " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS
										ON CS.Id=OE.[ID_sede]
									LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
										ON CE.Id=CS.[ID_cliente]
									WHERE OE.stato=0 " + queryExtra + @" 
                                    ORDER BY OE.codice_offerta DESC;";
            bool presres = false;

            int countResIDCL = 0;


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {

                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@idsd", idsd);
                    SQLiteDataReader reader = cmd.ExecuteReader();


                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - [{1}]", reader["noff"], reader["cliente"]), Value = Convert.ToInt64(reader["Id"]) });
                        presres = true;
                        countResIDCL++;
                    }

                    reader.Close();

                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_offerte_crea. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();

            for (int i = 0; i < count; i++)
            {
                if (presres == true)
                    nome_ctr[i].Enabled = true;

                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }
        }

        private void Populate_combobox_ordini(ComboBox nome_ctr, long idcl = 0, long idsd = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string queryExtra = "";
            if (idcl > 0)
            {
                queryExtra = @" AND OFE.ID_sede IN (SELECT Id FROM " + ProgramParameters.schemadb + @"[clienti_sedi] WHERE ID_cliente=@idcl) ";
            }


            string commandText = @"SELECT 
										OE.Id AS id,
										OE.codice_ordine AS noff,
										CE.nome  AS Cliente,
										( CS.stato || '-' || CS.provincia || '-' || CS.citta) AS Sede

									FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
									LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] AS OFE 
										ON OFE.Id = OE.[ID_offerta] 
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
										ON CS.Id = OFE.[ID_sede] 
									LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = CS.[ID_cliente]
									WHERE OE.ID_offerta IS NOT NULL AND OE.stato=0 " + queryExtra + @" 

                                    UNION ALL 

                                    SELECT 
										OE.Id AS id,
										OE.codice_ordine AS noff,
										CE.nome  AS Cliente,
										( CS.stato || '-' || CS.provincia || '-' || CS.citta) AS Sede

									FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS 
										ON CS.Id = OE.ID_sede
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE 
										ON CE.Id = CS.ID_cliente
									WHERE OE.ID_offerta IS NULL AND OE.stato=0 AND OE.ID_sede IN (SELECT Id FROM " + ProgramParameters.schemadb + @"[clienti_sedi] WHERE ID_cliente=@idcl)
                                    ORDER BY OE.Id DESC;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {

                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    bool presres = false;
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1} [{2}]", reader["id"], reader["noff"], reader["Cliente"]), Value = Convert.ToInt64(reader["Id"]) });
                        presres = true;
                    }

                    reader.Close();
                    if (presres == true)
                        nome_ctr.Enabled = true;
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_ordini. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }

            Utility.DataSourceToComboBox(nome_ctr, dataSource);
        }

        private void ClearDataGridView(DataGridView nome_ctr)
        {
            nome_ctr.DataSource = null;
            nome_ctr.Rows.Clear();
        }

        internal object ReturnStato(object stat)
        {
            Dictionary<string, int> stati = new()
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

        internal static List<string> CountryList()
        {
            List<string> CultureList = new();
            CultureInfo[] getCultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            CultureList.Add("");
            foreach (CultureInfo getCulture in getCultureInfo)
            {
                RegionInfo GetRegionInfo = new(getCulture.LCID);
                if (!(CultureList.Contains(GetRegionInfo.EnglishName)))
                {
                    CultureList.Add(GetRegionInfo.EnglishName);
                }
            }
            CultureList.Sort();
            return CultureList;
        }

        //UPDATE FUNCTIONS

        private void UpdateFornitori(int page = 0)
        {
            ComboBox[] nomi_ctr = { AddDatiCompSupplier, ChangeDatiCompSupplier };

            Populate_combobox_fornitore(nomi_ctr);

            if (page == 0)
            {
                string curPage = DataFornitoriCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            LoadFornitoriTable(page);
        }

        private void UpdateMacchine(int page = 0)
        {

            ComboBox[] nomi_ctr = { AddDatiCompMachine, ChangeDatiCompMachine, FieldOrdOggMach };

            if (page == 0)
            {
                string curPage = DataMacchinaCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            Populate_combobox_machine(nomi_ctr);
            LoadMacchinaTable(page);
        }

        private void UpdateClienti(int page = 0)
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
                    AddSedeCliente,
                    ChangeDatiClientiSediCliente
            };

            Populate_combobox_clienti(nomi_ctr);

            nomi_ctr = new ComboBox[] {
                    DataGridViewFilterCliente,
                    OffCreaFiltroCliente,
                    DataGridViewSediFilterCliente,
                    dataGridViewMacchina_Filtro_Cliente
            };

            Populate_combobox_clienti(nomi_ctr, 1);

            if (page == 0)
            {
                string curPage = DataClientiCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            LoadClientiTable(page);
        }

        private void UpdateClientiSedi(int page = 0)
        {

            ComboBox[] nomi_ctr = {
                ComboBoxOrdSede,
                FieldOrdOggSede,
                ComboSelOrdSede,
                AddOffCreaSede,
                SelOffCreaSede,
                AddOffCreaOggettoSede,
                AddDatiCompSede,
                ChangeDatiCompSede,
                AddDatiMacchinaSede,
                ChangeDatiMacchinaSede,
                AddDatiPRefSede,
                ChangeDatiPRefSede
            };

            Populate_combobox_dummy(nomi_ctrs: nomi_ctr);

            if (page == 0)
            {
                string curPage = DataClientiSediCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            LoadSedeTable(page);
        }

        private void UpdateCountryList()
        {
            ComboBox[] ctr =
            {
                AddSedeClienteStato,
                ChangeDatiClientiSediStato
            };

            foreach (ComboBox cb in ctr)
            {
                cb.DataSource = CountryList();
                cb.DropDownStyle = ComboBoxStyle.DropDownList;

                cb.SelectedItem = "Italy";
            }
        }

        private void UpdatePRef(int page = 0)
        {

            Dictionary<ComboBox, ComboBox> nome_ctr = new Dictionary<ComboBox, ComboBox>
            {
                { AddOffCreaPRef, ComboBoxOrdCliente },
                { ComboBoxOrdContatto, AddOffCreaCliente }
            };

            foreach (KeyValuePair<ComboBox, ComboBox> ctr in nome_ctr)
            {
                if (ctr.Value.DataSource != null)
                {
                    long curItemValue = Convert.ToInt64(ctr.Value.SelectedValue.ToString());
                    if (curItemValue > 0)
                    {
                        Populate_combobox_pref(ctr.Key, curItemValue);
                    }
                    else
                    {
                        Populate_combobox_dummy(ctr.Key);
                    }
                }
                else
                {
                    Populate_combobox_dummy(ctr.Key);
                }
            }
            if (page == 0)
            {
                string curPage = DataPRefCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }
            LoadPrefTable(page);
        }

        private void UpdateRicambi(int page = 0)
        {
            if (page == 0)
            {
                string curPage = DataCompCurPage.Text.Trim();
                if (!int.TryParse(curPage, out page))
                    page = 1;
            }

            LoadCompTable(page);

            if (AddOffCreaOggettoRica.Enabled == true && AddOffCreaOggettoRica.SelectedIndex > -1)
            {
                long idmacchina = Convert.ToInt64(AddOffCreaOggettoMach.SelectedValue.ToString());
                Populate_combobox_ricambi(new ComboBox[] { AddOffCreaOggettoRica }, idmacchina);
                Populate_combobox_ricambi_ordine(new ComboBox[] { FieldOrdOggPezzo }, idmacchina);
            }
        }

        private void UpdateOfferteCrea(int page = 0, bool EditedList = true, bool isFilter = false)
        {
            if (EditedList && !isFilter)
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

            if (!isFilter)
            {
                long index = Convert.ToInt64(SelOffCreaCl.SelectedValue.ToString());
                ComboBoxOrdCliente_SelectedIndexChanged(this, EventArgs.Empty);

                SelOffCreaCl.SelectedIndex = Utility.FindIndexFromValue(SelOffCreaCl, index);
                SelOffCreaCl_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private void UpdateFixedComboValue()
        {
            ComboBox[] nomi_ctr = new ComboBox[] {
                AddOffCreaStato,
                OffCreaFiltroStato
                };

            Populate.Populate_combobox_statoOfferte(nomi_ctr);
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
            settingCalendarioNome.Text = UserSettings.settings["calendario"]["nomeCalendario"];
            settingCalendarioDestinatari.Text = UserSettings.settings["calendario"]["destinatari"];
            settingCalendarioUpdate.Checked = Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]);
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
                            AddDatiPRefSede.Enabled = stat;
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

                            BtCancChangesClienti.Enabled = stat;
                            BtSaveChangesClienti.Enabled = stat;
                            BtDelChangesClienti.Enabled = stat;
                            return;
                        case "SE":
                            ChangeDatiClientiSediNumero.Enabled = stat;
                            ChangeDatiClientiSediStato.Enabled = stat;
                            ChangeDatiClientiSediProvincia.Enabled = stat;
                            ChangeDatiClientSediCitta.Enabled = stat;

                            BtDelChangesClientiSede.Enabled = stat;
                            BtCancChangesClientiSede.Enabled = stat;
                            BtSaveChangesClientiSede.Enabled = stat;
                            return;
                        case "A":
                            AddDatiClienteNome.Enabled = stat;

                            BtAddCliente.Enabled = stat;
                            return;
                        case "SA":
                            AddSedeCliente.Enabled = stat;
                            AddSedeClienteNumero.Enabled = stat;
                            AddSedeClienteStato.Enabled = stat;
                            AddSedeClienteProv.Enabled = stat;
                            AddSedeClienteCitta.Enabled = stat;

                            BtAddClienteSede.Enabled = stat;
                            return;
                        case "CA":
                            AddDatiClienteNome.Text = "";
                            return;
                        case "SCA":
                            AddSedeCliente.SelectedIndex = 0;
                            AddSedeClienteNumero.Text = "";
                            AddSedeClienteStato.SelectedIndex = ChangeDatiClientiSediStato.FindString("Italy");
                            AddSedeClienteProv.Text = "";
                            AddSedeClienteCitta.Text = "";
                            return;
                        case "CE":
                            ChangeDatiClientiNome.Text = "";
                            ChangeDatiClientiID.Text = "";
                            return;
                        case "SCE":
                            ChangeDatiClientiSediCliente.SelectedIndex = 0;
                            ChangeDatiClientiSediNumero.Text = "";
                            ChangeDatiClientiSediStato.SelectedIndex = ChangeDatiClientiSediStato.FindString("Italy");
                            ChangeDatiClientiSediProvincia.Text = "";
                            ChangeDatiClientSediCitta.Text = "";
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
                            ChangeDatiMacchinaClientless.Checked = false;
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
                            ChangeDatiCompIdMachine.Text = "";
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

                            if (Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString()) > 0)
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
                        default:
                            return;
                    }
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
                            if (ComboBoxOrdCliente.DataSource != null && Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString()) > 0)
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
                            FieldOrdOggPezzo.SelectedIndex = 0;
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

                            DuplicatiEventoCalendario.Enabled = stat;
                            AggiornaEventoDataCalendario.Enabled = stat;

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


        //DATABASE

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (ProgramParameters.connection != null && ProgramParameters.connection.State == ConnectionState.Open)
            {
                RunSQLiteOptimize(1000);
                ProgramParameters.connection.Close();
            }
        }

        private void Timer_RunSQLiteOptimize_Tick(object sender, EventArgs e)
        {
            if (ProgramParameters.connection != null && ProgramParameters.connection.State == ConnectionState.Open)
            {
                RunSQLiteOptimize();
            }
        }

        //CREDITI

        private void Github_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel ctr = sender as LinkLabel;
            string name = ctr.Name;

            switch (name)
            {
                case "Csvhelper":
                    System.Diagnostics.Process.Start("https://joshclose.github.io/CsvHelper/");
                    break;
                case "Autoupdaternet":
                    System.Diagnostics.Process.Start("https://github.com/ravibpatel/AutoUpdater.NET");
                    break;
                case "Fody":
                    System.Diagnostics.Process.Start("https://github.com/Fody/Fody");
                    break;
                case " CosturaFody":
                    System.Diagnostics.Process.Start("https://github.com/Fody/Costura");
                    break;
                case "Itext7":
                    System.Diagnostics.Process.Start("https://github.com/itext/itext7-dotnet");
                    break;
                default:
                    OnTopMessage.Alert("errore");
                    break;
            }

        }


        //ALTRO

        internal static void FixBuffer(Main parentForm)
        {
            var TabPagelist = Utility.GetAllNestedControls(parentForm).OfType<TabPage>().ToList();
            var GridViewlist = Utility.GetAllNestedControls(parentForm).OfType<DataGridView>().ToList();
            var TableLayoutPanellist = Utility.GetAllNestedControls(parentForm).OfType<TableLayoutPanel>().ToList();

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

            foreach (TableLayoutPanel ele in TableLayoutPanellist)
            {
                typeof(TabPage).InvokeMember(
                   "DoubleBuffered",
                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                   null,
                   ele,
                   new object[] { true }
                );
            }

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
                ele.DataBindingComplete += (sender, e) => DataGridFitColumn(sender); ;
            }
        }

        internal static void DataGridFitColumn(object sender)
        {
            DataGridView grid = sender as DataGridView;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.Columns[grid.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }
}