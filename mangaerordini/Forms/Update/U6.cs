using Newtonsoft.Json;
using Razorphyn;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ManagerOrdini.Forms.Update
{
    public partial class U6 : Form
    {
        readonly Dictionary<long, string> clienti = new Dictionary<long, string>();

        Dictionary<long, long> Associazioni = new Dictionary<long, long>();

        internal string Result { get; set; }

        public U6(Dictionary<long, string> clienti)
        {

            InitializeComponent();

            this.clienti = clienti;

            DrawingControl.SuspendDrawing(GestioneClientiTable);
            PopoluateItemsPanel(this.clienti);
            Utility.FixPanel(GestioneClientiTable);
            DrawingControl.ResumeDrawing(GestioneClientiTable);

            this.Activate();
        }

        private void PopoluateItemsPanel(Dictionary<long, string> Items)
        {
            int rows = 1;
            GestioneClientiTable.RowCount = Items.Count + 1;

            foreach (KeyValuePair<long, string> entry in Items)
            {
                GestioneClientiTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                Label id = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Name = "id" + rows,
                    Text = Convert.ToString(entry.Key)
                };

                Label nome = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Text = entry.Value,
                    Name = "nome" + rows
                };

                TextBox duplicato = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Name = "duplicato" + rows

                };

                duplicato.Leave += new EventHandler(CheckInt_Leave);

                GestioneClientiTable.Controls.Add(id, 0, rows);
                GestioneClientiTable.Controls.Add(nome, 1, rows);
                GestioneClientiTable.Controls.Add(duplicato, 2, rows);

                rows++;
            }
            GestioneClientiTable.Height = (GestioneClientiTable.RowCount + 2) * 50;
        }

        private void GestioneClientiSave_Click(object sender, EventArgs e)
        {

            Dictionary<string, long> check = new Dictionary<string, long>();

            foreach (KeyValuePair<long, string> entry in clienti)
            {
                if (!Associazioni.ContainsKey(entry.Key))
                {
                    if (!check.ContainsKey(entry.Value))
                    {
                        check.Add(entry.Value, entry.Key);
                    }
                    else
                    {
                        OnTopMessage.Alert("Esistono nomi (es. " + entry.Value + ") duplicati da risolvere. Completare la tabella e riprovare.", "Duplicati Residui");
                        return;
                    }
                }
            }

            if (OnTopMessage.Question("Vuoi confermare i dati inseriti?", "Confermare Dati") == DialogResult.Yes)
            {

                this.Result = JsonConvert.SerializeObject(Associazioni); ;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void GestioneClientiClose_Click(object sender, EventArgs e)
        {
            if (OnTopMessage.Question("Vuoi uscire dal programma?", "Confermare Uscita") == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
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
            Label child_id_text = GestioneClientiTable.Controls[CtrName] as Label;
            long child_id = Convert.ToInt64(child_id_text.Text.Trim());

            if (success)
            {
                long check = IsRootID(child_id);
                if (check == 0)
                {
                    if (parent_id > 0)
                    {
                        if (!Associazioni.ContainsKey(child_id))
                            Associazioni.Add(child_id, parent_id);
                        else
                            Associazioni[child_id] = parent_id;
                    }
                }
                else
                {
                    OnTopMessage.Alert("Questo elemento viene già utilizzato come root di un altro cliente(" + check + "), quinid non può essere considerato un duplicato.");
                    (sender as TextBox).Text = "";
                }
            }
            else
            {
                if (Associazioni.ContainsKey(child_id))
                    Associazioni.Remove(child_id);
            }
        }

        private long IsRootID(long id)
        {
            foreach (KeyValuePair<long, long> entry in Associazioni)
            {
                if (entry.Value == id)
                    return entry.Key;
            }
            return 0;
        }
    }
}
