using AutoUpdaterDotNET;
using JR.Utils.GUI.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Razorphyn.ProgramParameters;
using static Razorphyn.SupportClasses;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace Razorphyn
{
    public class ProgramUpdateFunctions
    {
        public void PreUpdateDataManipulation(decimal version)
        {
            return;
        }

        public bool PostUpdateDataManipulation(decimal version)
        {
            bool operationSuccess = true;

            if (version == 5)
            {
                operationSuccess = Updates.Update_5(version);
            }
            else if (version == 6)
            {
                operationSuccess = Updates.Update_6();
            }
            else if (version == 10)
            {
                string tempfile = ProgramParameters.exeFolderPath + ProgramParameters.db_file_path + version + ".pending";

                if (!File.Exists(tempfile))
                {
                    File.Create(tempfile).Close();
                }

                SQLiteConnection temp_connection = new(ProgramParameters.connectionStringAdmin);
                temp_connection.Open();


                string commandText;
                bool found = false;
                do
                {
                    //Remove duplicate codes in offers
                    commandText = @"SELECT 
	                                    OE.[id] AS collected_id_offerta,
	                                    PR.[codice] AS collected_codice,
                                        IIF(OE.[trasformato_ordine] == 1 ,ORE.[Id], 0)  AS collected_id_ordine

	                                    FROM  " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP
	                                    LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
		                                    ON OE.[Id] = OP.[ID_offerta]
	                                    LEFT JOIN  " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
		                                    ON PR.[Id]= OP.[ID_ricambio]
                                        LEFT JOIN  " + ProgramParameters.schemadb + @"[ordini_elenco] AS ORE
		                                    ON ORE.[ID_offerta] = OP.[ID_offerta]

	                                    WHERE OP.[ID_ricambio] IN(
							                                    SELECT [id]
								                                    FROM  " + ProgramParameters.schemadb + @"[pezzi_ricambi]
								                                    WHERE [codice] IN(
									                                    SELECT [codice]
										                                    FROM  " + ProgramParameters.schemadb + @"[pezzi_ricambi]
										                                    WHERE [deleted] = 0
										                                    GROUP BY [codice]
											                                    HAVING COUNT([codice]) > 1
									                                    )
								                                    AND substr(codice,1,1) != 'D' 
							                                    )
	                                    GROUP BY OE.[Id], PR.[codice]
		                                    HAVING COUNT(PR.[codice]) > 1 LIMIT 1;";

                    using (SQLiteCommand cmd = new(commandText, temp_connection))
                    {
                        try
                        {
                            List<string> list = new List<string>();
                            long id_offerta = 0;
                            long id_ordine = 0;

                            using (SQLiteDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    id_offerta = Convert.ToInt64(reader["collected_id_offerta"]);
                                    id_ordine = Convert.ToInt64(reader["collected_id_ordine"]);
                                    list.Add(Convert.ToString(reader["collected_codice"]));
                                }
                            }
                            if (id_offerta > 0)
                            {

                                List<U10_Ricambio> itemsOffer = U10_Offerta_Ricambio.Offerta_GetItemCollection(id_offerta, list, temp_connection);
                                using (var f = new ManagerOrdini.Forms.Update.U10_Offerta(itemsOffer, id_offerta))
                                {
                                    f.ShowDialog();
                                }

                                if (id_ordine > 0)
                                {
                                    List<U10_Ricambio> ItemsOrder = U10_Ordine_Ricambio.Ordine_GetItemCollection(id_ordine, new List<string>(), temp_connection);

                                    using (var f = new ManagerOrdini.Forms.Update.U10_Ordine(ItemsOrder, id_ordine, true))
                                    {
                                        f.ShowDialog();
                                    }
                                }

                            }
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("UPDATE 10: Errore durante selezione duplicati in offerte main. Codice: " + ex.Message);
                            operationSuccess = false;
                            //break;
                        }
                    }
                } while (found);

                #region Remove duplicate codes from ricambi_elenco
                Dictionary<string, Dictionary<long, string>> items = new Dictionary<string, Dictionary<long, string>>();
                Dictionary<long, long> replace = new Dictionary<long, long>();
                commandText = @"SELECT [id], [codice], [nome]
                                        FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi]
                                        WHERE [codice] IN(
                                                        SELECT [codice]
                                                        FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi]
                                                        WHERE [deleted] = 0
                                                        GROUP BY [codice]
                                                            HAVING COUNT([codice]) > 1
                                                        )
                                                AND substr(codice,1,1) != 'D' 
                                        ORDER BY [codice];";

                using (SQLiteCommand cmd = new(commandText, temp_connection))
                {
                    try
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {

                            while (reader.Read())
                            {

                                Ricambio temp = new Ricambio()
                                {
                                    Id = Convert.ToInt64(reader["id"]),
                                    Codice = Convert.ToString(reader["codice"]),
                                    Nome = Convert.ToString(reader["nome"])
                                };

                                if (!items.ContainsKey(temp.Codice))
                                {
                                    items.Add(temp.Codice, new Dictionary<long, string>());
                                    items[temp.Codice].Add(temp.Id, temp.Nome);
                                }
                                else
                                    items[temp.Codice].Add(temp.Id, temp.Nome);
                            }
                        }
                        if (items.Count > 0)
                        {

                            DialogResult answer = new DialogResult();
                            while (answer != DialogResult.Yes)
                            {
                                replace = new Dictionary<long, long>(); //<original , replace>
                                string summary = "";
                                int count = items.Count;
                                int i = 1;
                                foreach (KeyValuePair<string, Dictionary<long, string>> group in items)
                                {
                                    List<long> ids = new List<long>();
                                    DataValidation.ValidationResult id = new DataValidation.ValidationResult();
                                    string idString = "";
                                    string partialSummary = "";

                                    string message = @"Codice oggetto:" + group.Key + Environment.NewLine +
                                                        "Inserire l'ID dell'elemento di mantenere il nome tra quelli elencati:" + Environment.NewLine;

                                    long iddef = 0;
                                    foreach (KeyValuePair<long, string> item in group.Value)
                                    {
                                        message += "(ID: " + item.Key + ") " + item.Value + Environment.NewLine;
                                        ids.Add(item.Key);
                                        iddef = item.Key;
                                    }

                                    while (!id.Success || !(id.LongValue > 0) || !ids.Contains(Convert.ToInt64(id.LongValue)))
                                    {
                                        idString = OnTopMessage.InputBox(message, "Ricambio " + group.Key + " (" + i + "/" + count + ")", iddef.ToString());
                                        id = DataValidation.ValidateId(idString);
                                    }

                                    foreach (KeyValuePair<long, string> item in group.Value)
                                    {
                                        if (item.Key != id.LongValue)
                                        {
                                            replace.Add(item.Key, (long)id.LongValue);
                                            partialSummary += "\t\t- " + item.Value + Environment.NewLine;
                                        }
                                        else
                                        {
                                            partialSummary = group.Key + Environment.NewLine +
                                                                "\tSelezione: " + item.Value + Environment.NewLine +
                                                                "\tSostituisce: " + Environment.NewLine +
                                                                partialSummary;
                                        }
                                    }
                                    summary += partialSummary + Environment.NewLine;
                                    i++;
                                }

                                answer = FlexibleMessageBox.Show(summary, "Confermare cambiamenti", MessageBoxButtons.YesNo);
                            }

                            if (answer == DialogResult.Yes)
                            {
                                using (var transaction = temp_connection.BeginTransaction())
                                {
                                    try
                                    {
                                        foreach (KeyValuePair<long, long> item in replace)
                                        {
                                            cmd.CommandText = "UPDATE " + ProgramParameters.schemadb + @"[pezzi_ricambi] 
                                                                    SET active = null , deleted = 1 
                                                                    WHERE Id=@idold LIMIT 1;";

                                            cmd.CommandText += @"UPDATE " + ProgramParameters.schemadb + @"[offerte_pezzi]
                                                                    SET 
                                                                        ID_ricambio = @idnew
                                                                    WHERE
                                                                        ID_ricambio = @idold;";

                                            cmd.CommandText += @"UPDATE " + ProgramParameters.schemadb + @"[ordine_pezzi]
                                                                    SET 
                                                                        ID_ricambio = @idnew
                                                                    WHERE
                                                                        ID_ricambio = @idold;";

                                            cmd.Parameters.AddWithValue("@idnew", item.Value);
                                            cmd.Parameters.AddWithValue("@idold", item.Key);
                                            cmd.ExecuteNonQuery();
                                        }
                                        transaction.Commit();
                                    }
                                    catch (Exception ex)
                                    {
                                        transaction.Rollback();
                                        OnTopMessage.Error("UPDATE 10: Errore durante update info dal database. Codice: " + ex.Message);
                                        operationSuccess = false;
                                    }
                                }
                            }
                            else
                                operationSuccess = false;
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("UPDATE 10: Errore durante selezione info dal database. Codice: " + ex.Message);
                        operationSuccess = false;
                    }

                    File.Delete(tempfile);
                }
                #endregion
            }
            return operationSuccess;
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

    internal class Updates
    {
        internal static bool Update_5(decimal version)
        {
            bool operationSuccess = true;

            string tempfile = ProgramParameters.exeFolderPath + ProgramParameters.db_file_path + version + ".pending";

            UserSettings UserSettings = new();
            CalendarManager CalendarManager = new();

            if (!File.Exists(tempfile)) File.Create(tempfile).Close();

            Outlook.Application OlApp = new();
            Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);

            string commandText = @"SELECT data_ETA FROM " + ProgramParameters.schemadb + @"[ordini_elenco] ORDER BY data_ETA ASC LIMIT 1;";
            string startDate = null;

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    startDate = Convert.ToString(cmd.ExecuteScalar());
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante selezione info dal database. Codice: " + ex.Message);
                    operationSuccess = false;
                }
            }


            if (!String.IsNullOrEmpty(startDate))
            {
                Outlook.Items restrictedItems = CalendarManager.CalendarGetItems(personalCalendar, Convert.ToDateTime(startDate).AddDays(-1), Convert.ToDateTime(startDate).AddDays(+1));

                Dictionary<int, DateTime> ordNum = new();

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

                using (SQLiteCommand cmd = new(query, ProgramParameters.connection))
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
                        operationSuccess = false;
                    }
                }
            }
            try
            {

                if (operationSuccess)
                    File.Delete(tempfile);
            }
            catch
            {
                OnTopMessage.Error("Errore durante eliminazione file. Per favore eliminare manualemnte il file: " + tempfile);
                //operationSuccess = false;
            }
            return operationSuccess;
        }

        internal static bool Update_6()
        {
            bool operationSuccess = true;

            Dictionary<long, string> DictClienti = new Dictionary<long, string>();
            List<Cliente> ListClienti = new List<Cliente>();

            SQLiteConnection temp_connection = new(ProgramParameters.connectionStringAdmin);
            temp_connection.Open();

            string commandText = @"SELECT * FROM " + ProgramParameters.schemadb + @"[clienti_elenco] ORDER BY [nome] ASC;";

            using (SQLiteCommand cmd = new(commandText, temp_connection))
            {
                try
                {
                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        ListClienti.Add(new Cliente()
                        {
                            Id = Convert.ToInt64(reader["id"]),
                            Name = Convert.ToString(reader["nome"]),
                            Stato = Convert.ToString(reader["stato"]),
                            Provincia = Convert.ToString(reader["provincia"]),
                            Citta = Convert.ToString(reader["citta"])
                        });

                        DictClienti.Add(Convert.ToInt64(reader["id"]), Convert.ToString(reader["nome"]));
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante selezione info dal database. Codice: " + ex.Message);
                    operationSuccess = false;
                }
            }

            using (var f = new ManagerOrdini.Forms.Update.U6(DictClienti))
            {
                Dictionary<long, long> duplicates = new Dictionary<long, long>();
                DialogResult result = new DialogResult();

                result = f.ShowDialog();

                if (result == DialogResult.OK)
                {
                    duplicates = JsonConvert.DeserializeObject<Dictionary<long, long>>(f.Result);

                    long c = ListClienti.Count;

                    foreach (Cliente cliente in ListClienti)
                    {
                        commandText = "";
                        if (!duplicates.ContainsKey(cliente.Id))
                        {
                            commandText += @"INSERT INTO " + schemadb + "[clienti_elenco_temp] (id, nome) VALUES (@idcl,@nomecl);" + Environment.NewLine;
                        }

                        commandText += @"INSERT INTO " + schemadb + "[clienti_sedi] (ID_cliente, ID_cliente_old, stato, provincia, citta) VALUES (@idcl, @idclold, @clstato, @clprovincia, @citta);" + Environment.NewLine;


                        using (SQLiteCommand cmd = new(commandText, temp_connection))
                        {
                            try
                            {
                                cmd.CommandText = commandText;

                                cmd.Parameters.AddWithValue("@clstato", cliente.Stato);
                                cmd.Parameters.AddWithValue("@clprovincia", cliente.Provincia);
                                cmd.Parameters.AddWithValue("@citta", cliente.Citta);

                                if (!duplicates.ContainsKey(cliente.Id))
                                {
                                    cmd.Parameters.AddWithValue("@nomecl", cliente.Name);
                                    cmd.Parameters.AddWithValue("@idclold", cliente.Id);
                                    cmd.Parameters.AddWithValue("@idcl", cliente.Id);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@idcl", duplicates[cliente.Id]);
                                    cmd.Parameters.AddWithValue("@idclold", cliente.Id);
                                }

                                cmd.ExecuteNonQuery();
                            }
                            catch (SQLiteException ex)
                            {
                                OnTopMessage.Error("Errore durante aggiornamento clienti_sedi database. Nome:" + cliente.Name + ". Codice: " + DbTools.ReturnErorrCode(ex));
                                operationSuccess = false;
                            }
                        }
                    }

                    //Update clienti_macchine
                    commandText = @"UPDATE [clienti_macchine] SET ID_cliente = (SELECT cs.ID_cliente FROM [clienti_sedi] AS cs WHERE cs.ID_cliente_old = [clienti_macchine].ID_cliente LIMIT 1), ID_sede = (SELECT cs.ID FROM [clienti_sedi] AS cs WHERE cs.ID_cliente_old = [clienti_macchine].ID_cliente LIMIT 1);";

                    using (SQLiteCommand cmd = new(commandText, temp_connection))
                    {
                        try
                        {
                            cmd.CommandText = commandText;
                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante aggiornamento clienti macchchine database. Codice: " + DbTools.ReturnErorrCode(ex));
                            operationSuccess = false;
                        }
                    }

                    //Update clienti_riferimenti
                    commandText = @"UPDATE [clienti_riferimenti] SET ID_cliente = (SELECT cs.ID_cliente FROM [clienti_sedi] AS cs WHERE cs.ID_cliente_old = [clienti_riferimenti].ID_cliente LIMIT 1), ID_sede = (SELECT cs.id FROM [clienti_sedi] AS cs WHERE cs.ID_cliente_old = [clienti_riferimenti].ID_cliente LIMIT 1);";


                    using (SQLiteCommand cmd = new(commandText, temp_connection))
                    {
                        try
                        {
                            cmd.CommandText = commandText;
                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante aggiornamento clienti riferienti database. Codice: " + DbTools.ReturnErorrCode(ex));
                            operationSuccess = false;
                        }
                    }

                    //Update offerte_elenco

                    commandText = @"UPDATE [offerte_elenco] SET ID_sede = (SELECT cs.id FROM [clienti_sedi] AS cs WHERE cs.ID_cliente_old = [offerte_elenco].ID_sede LIMIT 1);";

                    using (SQLiteCommand cmd = new(commandText, temp_connection))
                    {
                        try
                        {
                            cmd.CommandText = commandText;
                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante aggiornamento offerte_elenco database. Codice: " + DbTools.ReturnErorrCode(ex));
                            operationSuccess = false;
                        }
                    }

                    //Update ordini_elenco

                    commandText = @"UPDATE [ordini_elenco] SET ID_sede = (SELECT cs.id FROM [clienti_sedi] AS cs WHERE cs.ID_cliente_old = [ordini_elenco].ID_sede LIMIT 1);";

                    using (SQLiteCommand cmd = new(commandText, temp_connection))
                    {
                        try
                        {
                            cmd.CommandText = commandText;
                            cmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            OnTopMessage.Error("Errore durante aggiornamento ordini_elenco database. Codice: " + DbTools.ReturnErorrCode(ex));
                            operationSuccess = false;
                        }
                    }
                }
                else
                {
                    OnTopMessage.Error("Impossibile aggiornare il programma. Il programma verra chiuso.");
                    operationSuccess = false;
                }
            }
            temp_connection.Close();
            return operationSuccess;
        }

        internal static bool Update_10()
        {
            bool operationSuccess = true;


            return operationSuccess;
        }
    }

    internal class Cliente
    {
        internal long Id { get; set; }
        internal string Name { get; set; }
        internal string Stato { get; set; }
        internal string Provincia { get; set; }
        internal string Citta { get; set; }
    }

    internal class Ricambio
    {
        internal long Id { get; set; }
        internal string Codice { get; set; }
        internal string Nome { get; set; }
    }

}