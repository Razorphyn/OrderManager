using Razorphyn;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Razorphyn.SupportClasses;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace ManagerOrdini.Forms.Update
{
    public partial class U10_Ordine : Form
    {
        readonly TableLayoutPanel TableToPopulate;

        List<U10_Ricambio> Items;
        long id_ordine;
        bool prevent_closing;

        internal U10_Ordine(List<U10_Ricambio> items, long id_ordine, bool prevent_closing)
        {
            InitializeComponent();

            TableToPopulate = TableItems;
            this.Items = items;

            DrawingControl.SuspendDrawing(TableToPopulate);
            PopoluateItemsPanel(this.Items);
            Utility.FixPanel(TableToPopulate);
            DrawingControl.ResumeDrawing(TableToPopulate);

            this.Text = "Rimozione Duplicati da Ordini";
            this.Activate();
            this.id_ordine = id_ordine;
            this.prevent_closing = prevent_closing;

        }
        private void PopoluateItemsPanel(List<U10_Ricambio> Items)
        {
            int rows = 0;
            TableToPopulate.RowCount = Items.Count + 2;

            foreach (U10_Ricambio entry in Items)
            {
                TableToPopulate.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                Label id = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Name = "id" + rows,
                    Text = Convert.ToString(entry.Id_ricambio)
                };

                Label nome = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Text = entry.Nome,
                    Name = "nome" + rows
                };

                Label codice = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Text = entry.Codice,
                    Name = "codice" + rows
                };

                TextBox new_codice = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = entry.Codice,
                    Name = "new_codice" + rows,
                    Enabled = entry.Duplicate
                };

                TextBox qta = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Anchor = AnchorStyles.None,
                    Text = Convert.ToString(entry.Qta),
                    Name = "qta" + rows
                };

                TextBox prezzo = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = Convert.ToString(entry.Prezzo),
                    Name = "prezzo" + rows
                };

                TextBox prezzo_sconto = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = Convert.ToString(entry.Prezzo_Sconto),
                    Name = "prezzo_sconto" + rows
                };

                DateTimePicker eta = new()
                {
                    Dock = DockStyle.Fill,
                    Text = entry.ETA.ToString(),
                    Anchor = AnchorStyles.None,
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy",
                    Name = "eta" + rows
                };

                new_codice.Leave += new EventHandler(CheckCodici);
                qta.Leave += new EventHandler(CheckQta);
                prezzo.Leave += new EventHandler(CheckPrezzo);
                prezzo_sconto.Leave += new EventHandler(CheckPrezzoSconto);
                eta.Leave += new EventHandler(CheckEta);

                rows++;
                int i = 0;
                TableToPopulate.Controls.Add(id, i, rows); i++;
                TableToPopulate.Controls.Add(nome, i, rows); i++;
                TableToPopulate.Controls.Add(codice, i, rows); i++;
                TableToPopulate.Controls.Add(new_codice, i, rows); i++;
                TableToPopulate.Controls.Add(qta, i, rows); i++;
                TableToPopulate.Controls.Add(prezzo, i, rows); i++;
                TableToPopulate.Controls.Add(prezzo_sconto, i, rows); i++;
                TableToPopulate.Controls.Add(eta, i, rows); i++;


            }
            TableToPopulate.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            TableLayoutPanel_Tools.ResizeRowFixed(TableToPopulate, 50);
        }

        private void CheckInt_Leave(object sender, EventArgs e)
        {

            TextBox input = sender as TextBox;
            string parent_id_text = input.Text.Trim();

            if (string.IsNullOrEmpty(parent_id_text))
            {
                return;
            }
            if (!Regex.IsMatch(parent_id_text, @"^[ ]*[0-9]*[ ]*$"))
            {
                input.Text = "";
                OnTopMessage.Error("Valore numerico non valido");
                return;
            }

            bool success = Int64.TryParse(parent_id_text, out long parent_id);

            string CtrName = "id" + input.Name.Replace("duplicato", "");
            Label child_id_text = TableToPopulate.Controls[CtrName] as Label;
            long child_id = Convert.ToInt64(child_id_text.Text.Trim());

            if (success)
            {
                long check = IsRootID(child_id);
                if (check == 0)
                {
                    if (parent_id > 0)
                    {
                        int index = FindIndex(child_id);
                        Items[index].Id_Sostituto = parent_id;
                    }
                }
                else
                {
                    OnTopMessage.Alert("L'ID " + parent_id + " viene già utilizzato come root di un altro ricambio(ID: " + check + "), quindi non può essere considerato un duplicato.");
                    (sender as TextBox).Text = "";
                }
            }
            else
            {
                int index = FindIndex(child_id);
                Items[index].Id_Sostituto = 0;
            }
        }

        private long IsRootID(long id)
        {
            foreach (U10_Ricambio entry in Items)
            {
                if (entry.Id_Sostituto == id)
                    return entry.Id_ricambio;
            }
            return 0;
        }

        internal int FindIndex(long id)
        {
            int c = Items.Count;

            for (int i = 0; i < c; i++)
            {
                if (Items[i].Id_ricambio == id)
                    return i;
            }

            return -1;
        }

        private bool CheckIfCodiceUnique(string codice, long idricambio)
        {

            bool exist = false;

            using (SQLiteConnection temp_connection = new(ProgramParameters.connectionStringAdmin))
            {
                temp_connection.Open();
                string commandText = @"SELECT 
                                        [id]
                                    FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi]
                                    WHERE codice = @codice AND id != @idricambio LIMIT 1;";

                using (SQLiteCommand cmd = new(commandText, temp_connection))
                {
                    cmd.Parameters.AddWithValue("@codice", codice);
                    cmd.Parameters.AddWithValue("@idricambio", idricambio);
                    try
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                exist = true;
                            }
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("UPDATE 10: Errore durante verifica se codice duplicato. Codice: " + ex.Message);
                        exist = true;
                    }
                    finally { temp_connection.Close(); };
                }
            }

            return !exist;
        }

        private bool CheckIfCodiceETAUniqueInOrder(int index)
        {

            int c = Items.Count;

            for (int i = 0; i < c; i++)
            {
                if (i != index && Items[i].Nuovo_Qta != 0 && Items[i].Nuovo_Codice == Items[index].Nuovo_Codice && Items[i].Nuovo_ETA == Items[index].Nuovo_ETA)
                    return false;
            }
            return true;
        }

        private void CheckCodici(object sender, EventArgs e)
        {
            TextBox cell = (TextBox)sender;
            string codice = cell.Text.Trim();

            int index = ExtractIndexFromName(cell.Name);

            string error = DataValidation.ValidateCodiceRicambio(codice);
            if (error != "")
            {
                OnTopMessage.Error(error);
                cell.Text = Items[index].Codice;
                Items[index].Nuovo_Codice = null;
            }
            else
            {
                if (CheckIfCodiceUnique(codice, Items[index].Id_ricambio))
                {
                    Items[index].Nuovo_Codice = codice;
                }
                else
                {
                    OnTopMessage.Error("Esiste già un ricambio con il codice " + codice + ".");
                    Items[index].Nuovo_Codice = null;
                    cell.Text = Items[index].Codice;
                }
            }
        }

        private void CheckQta(object sender, EventArgs e)
        {
            TextBox cell = (TextBox)sender;
            string qta = cell.Text.Trim();

            int index = ExtractIndexFromName(cell.Name);

            DataValidation.ValidationResult answer = DataValidation.ValidateQta(qta, false);

            if (!answer.Success)
            {
                OnTopMessage.Error(answer.Error);
                cell.Text = Items[index].Qta.ToString();

                Items[index].Nuovo_Qta = Items[index].Qta;
            }
            else
            {
                Items[index].Nuovo_Qta = (int)answer.IntValue;
            }
        }

        private void CheckPrezzo(object sender, EventArgs e)
        {
            TextBox cell = (TextBox)sender;
            string prezzo = cell.Text.Trim();

            int index = ExtractIndexFromName(cell.Name);

            DataValidation.ValidationResult answer = DataValidation.ValidatePrezzo(prezzo);

            if (!answer.Success)
            {
                OnTopMessage.Error(answer.Error);
                cell.Text = Items[index].Prezzo.ToString();

                Items[index].Nuovo_Prezzo = Items[index].Prezzo;
            }
            else
            {
                Items[index].Nuovo_Prezzo = (decimal)answer.DecimalValue;
            }
        }

        private void CheckEta(object sender, EventArgs e)
        {
            DateTimePicker cell = (DateTimePicker)sender;
            string date = cell.Text.Trim();

            int index = ExtractIndexFromName(cell.Name);

            DataValidation.ValidationResult answer = DataValidation.ValidateDate(date);

            if (!answer.Success)
            {
                OnTopMessage.Error(answer.Error);
                cell.Text = Items[index].ETA.ToString();

                Items[index].Nuovo_ETA = Items[index].ETA;
            }
            else
            {
                Items[index].Nuovo_ETA = answer.DateValue;
            }
        }

        private void CheckPrezzoSconto(object sender, EventArgs e)
        {
            TextBox cell = (TextBox)sender;
            string prezzo = cell.Text.Trim();

            int index = ExtractIndexFromName(cell.Name);

            DataValidation.ValidationResult answer = DataValidation.ValidatePrezzo(prezzo);

            if (!answer.Success)
            {
                OnTopMessage.Error(answer.Error);
                cell.Text = Items[index].Prezzo.ToString();

                Items[index].Nuovo_Prezzo_Sconto = Items[index].Prezzo;
            }
            else
            {
                Items[index].Nuovo_Prezzo_Sconto = (decimal)answer.DecimalValue;
            }
        }

        private int ExtractIndexFromName(string name, string root = null)
        {
            if (root != null)
            {
                name = name.Replace(root, "");
            }
            else
            {
                string pattern = @"[0-9]{1,}$";
                foreach (Match match in Regex.Matches(name, pattern))
                {
                    name = match.Value;
                }
            }


            bool success = Int32.TryParse(name, out int index);
            if (success)
                return index;
            else
                return -1;
        }

        private void U10_Save_Click(object sender, EventArgs e)
        {
            int c = Items.Count;

            for (int i = 0; i < c; i++)
            {
                if (Items[i].Nuovo_Qta == 0)
                {
                    Items[i].Delete = true;
                }
                else
                {
                    Items[i].Delete = false;
                    if (!CheckIfCodiceETAUniqueInOrder(i))
                    {
                        OnTopMessage.Error("Esiste già un ricambio con il codice " + Items[i].Nuovo_Codice + " con data di consegna " + Items[i].Nuovo_ETA + " nell'ordine.");
                        return;
                    }
                }
            }

            using (SQLiteConnection temp_connection = new(ProgramParameters.connectionStringAdmin))
            {
                temp_connection.Open();

                bool updateFprice = false;
                bool updateFpriceSconto = false;

                DialogResult dialogResult = OnTopMessage.Question("Vuoi aggiornare il prezzo finale?", "Eliminare Pezzo da Ordine");
                if (dialogResult == DialogResult.Yes)
                {
                    updateFprice = true;
                    dialogResult = OnTopMessage.Question("Applicare lo sconto al prezzo finale?", "Eliminare Pezzo da Ordine");
                    if (dialogResult == DialogResult.Yes)
                    {
                        updateFpriceSconto = true;
                    }
                }

                for (int i = 0; i < c; i++)
                {
                    if (Items[i].Delete)
                    {
                        Ordini.GestioneOrdini.DeleteObjFromOrder(id_ordine, Items[i].Id_db_entry, updateFprice, updateFpriceSconto, temp_connection);
                    }
                    else
                    {
                        if (Items[i].Nuovo_Codice != Items[i].Codice)
                        {
                            string commandText = @"UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[pezzi_ricambi] 
                                                    SET codice=@new_codice
                                                    WHERE Id=@id_ric LIMIT 1;";

                            using (SQLiteCommand cmd = new(commandText, temp_connection))
                            {
                                try
                                {
                                    cmd.CommandText = commandText;
                                    cmd.Parameters.AddWithValue("@new_codice", Items[i].Nuovo_Codice);
                                    cmd.Parameters.AddWithValue("@id_ric", Items[i].Id_ricambio);
                                    cmd.ExecuteNonQuery();
                                }
                                catch (SQLiteException ex)
                                {
                                    OnTopMessage.Error("Errore durante aggiornamento codice ricambio nel database. Codice: " + DbTools.ReturnErorrCode(ex));
                                }
                            }
                        }
                        Ordini.GestioneOrdini.UpdateItemFromOrder(id_ordine, Items[i].Id_db_entry, Items[i].Nuovo_Prezzo, Items[i].Nuovo_Prezzo_Sconto, Items[i].Nuovo_Qta, Items[i].Nuovo_ETA, updateFprice, temp_connection);
                    }
                }

                using (UserSettings UserSettings = new())
                {
                    if (Boolean.Parse(UserSettings.settings["calendario"]["aggiornaCalendario"]) == true)
                    {
                        Outlook.Application OlApp = new();
                        Outlook.Folder personalCalendar = CalendarManager.FindCalendar(OlApp, UserSettings.settings["calendario"]["nomeCalendario"]);
                        Ordini.GestioneOrdini.UpdateCalendarOnObj(id_ordine, personalCalendar);
                    }
                }
            }

            this.Close();
        }

    }
}
