using Newtonsoft.Json;
using OrderManager.Class;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static OrderManager.Class.Populate;
using static OrderManager.Class.SupportClasses;

namespace ManagerOrdini
{
    public partial class ImportPdfOfferta : Form
    {
        readonly Dictionary<string, string> offerInfo = new() { };
        readonly List<Dictionary<string, string>> Items = new();
        readonly string filePath;
        long offerID = 0;

        readonly TableLayoutPanel TabItem;

        public ImportPdfOfferta(Dictionary<string, string> Form1_offerInfo, List<Dictionary<string, string>> Form1_Items, string filePath)
        {
            InitializeComponent();

            TabItem = OfferItemCollection;

            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            var comboBoxes = Utility.GetAllNestedControls(this).OfType<ComboBox>().ToList();

            foreach (ComboBox ctrl in comboBoxes)
            {
                ctrl.BindingContext = new BindingContext();
                ctrl.DisplayMember = "Name";
                ctrl.ValueMember = "Value";
            }

            FixBuffer.TableLayoutPanel(TabItem);

            this.offerInfo = Form1_offerInfo;
            this.Items = Form1_Items;
            this.filePath = filePath;

            AddOffCreaNOff.Text = offerInfo["numero"];
            if (DataValidation.ValidateDateTime(offerInfo["data"]).Success)
                AddOffCreaData.Text = offerInfo["data"];

            Populate_combobox_clienti(new ComboBox[] { AddOffCreaCliente });
            Populate_combobox_statoOfferte(new ComboBox[] { AddOffCreaStato });

            (long id_cliente, long id_sede) = GetResource.GetClientIdFromNumero(offerInfo["cliente"]);
            int index = Utility.FindIndexFromValue(AddOffCreaCliente, id_cliente);
            AddOffCreaCliente.SelectedIndex = index;

            if (AddOffCreaSede.DataSource != null)
            {
                index = Utility.FindIndexFromValue(AddOffCreaSede, id_sede);
                AddOffCreaSede.SelectedIndex = index;
            }

            Populate_combobox_pref(AddOffCreaPRef, Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString()));
            Populate_combobox_FieldOrdSpedGestione(AddOffCreaSpedizioneGest);
        }

        private void AddOffCreaCliente_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawingControl.SuspendDrawing(TabItem);
            if (Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString()) > -1)
            {
                ImportPDFSupport.DeleteControls(TabItem);
                long idcl = Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString());
                BuildHeaderTableItem();

                Populate_combobox_pref(AddOffCreaPRef, idcl);
                Populate_combobox_sedi(new ComboBox[] { AddOffCreaSede }, idcl);

                PopoluateItemsPanel(Items);
            }
            else
            {
                ImportPDFSupport.DeleteControls(TabItem);

                TabItem.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                Label selectCl = new()
                {
                    Dock = DockStyle.Fill,
                    Text = "Selezionare un cliente e la sede"
                };

                TabItem.Controls.Add(selectCl, 0, 0);
                tableLayoutPanel1.SetColumnSpan(selectCl, 2);
            }
            DrawingControl.ResumeDrawing(TabItem);
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

        private void ImportOfferPDFAdd_Click(object sender, EventArgs e)
        {

            string numeroOff = AddOffCreaNOff.Text.Trim();
            string spedizioni = AddOffCreaSpedizione.Text.Trim();
            string dataoffString = AddOffCreaData.Text.Trim();

            int gestSP = Convert.ToInt16(AddOffCreaSpedizioneGest.SelectedValue.ToString());

            long idcl = Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(AddOffCreaSede.SelectedValue.ToString());
            long idpref = Convert.ToInt64(AddOffCreaPRef.SelectedValue.ToString());

            int stato = Convert.ToInt16(AddOffCreaStato.SelectedValue.ToString());

            stato = (stato < 0) ? 0 : stato;

            DataValidation.ValidationResult prezzoSpedizione = new();
            Offerte.Answer esito = new();

            string er_list = "";

            if (offerID == 0)
            {

                er_list += DataValidation.ValidateIdOffertaFormato(numeroOff).Error;

                DataValidation.ValidationResult dataoffValue = DataValidation.ValidateDate(dataoffString);
                er_list += dataoffValue.Error;

                DataValidation.ValidationResult answer = DataValidation.ValidateSede(idcl, idsd);
                if (!answer.Success)
                {
                    OnTopMessage.Alert(answer.Error);
                    return;
                }
                er_list += answer.Error;

                if (idpref > 0)
                {
                    answer = DataValidation.ValidatePRef(idpref);
                    if (!answer.Success)
                    {
                        OnTopMessage.Alert(answer.Error);
                        return;

                    }
                    er_list += answer.Error;
                }

                if (!string.IsNullOrEmpty(spedizioni))
                {
                    prezzoSpedizione = DataValidation.ValidateSpedizione(spedizioni, gestSP);
                    er_list += prezzoSpedizione.Error;
                }

                if (er_list != "")
                {
                    OnTopMessage.Alert(er_list);
                    return;
                }

                esito = Offerte.GestioneOfferte.CreateOffer(dataoffValue.DateValue, numeroOff, idsd, stato, idpref, prezzoSpedizione.DecimalValue, gestSP);

                if (esito.Success)
                    offerID = esito.LongValue;
            }

            if (offerID > 0)
            {
                int netAdded = 0;

                CheckBox[] comboBoxesImport = TabItem.Controls.OfType<CheckBox>().Where(i => i.Name.StartsWith("import")).ToArray();
                ComboBox[] comboBoxesPezzi = TabItem.Controls.OfType<ComboBox>().Where(i => i.Name.StartsWith("pezzo")).ToArray();
                TextBox[] comboBoxesPrezziOff = TabItem.Controls.OfType<TextBox>().Where(i => i.Name.StartsWith("prez_of")).ToArray();
                TextBox[] comboBoxesPrezziFin = TabItem.Controls.OfType<TextBox>().Where(i => i.Name.StartsWith("prez_fin")).ToArray();
                TextBox[] comboBoxesQta = TabItem.Controls.OfType<TextBox>().Where(i => i.Name.StartsWith("qta")).ToArray();

                int c = comboBoxesPezzi.Count();

                List<int> Rows2BeDel = new();

                for (int i = 0; i < c; i++)
                {
                    if (!comboBoxesImport[i].Checked)
                    {
                        continue;
                    }
                    netAdded++;

                    string prezzoOr = comboBoxesPrezziOff[i].Text.Trim();
                    string prezzoSc = comboBoxesPrezziFin[i].Text.Trim();
                    string qta = comboBoxesQta[i].Text.Trim();

                    long idir = Convert.ToInt32(comboBoxesPezzi[i].SelectedValue.ToString());

                    er_list = "";

                    if (idir < 1)
                    {
                        er_list += "Il ricambio non esiste nel database.";
                    }

                    DataValidation.ValidationResult prezzoOrV = DataValidation.ValidatePrezzo(prezzoOr);
                    er_list += prezzoOrV.Error;

                    DataValidation.ValidationResult prezzoScV = DataValidation.ValidatePrezzo(prezzoSc);
                    er_list += prezzoScV.Error;

                    DataValidation.ValidationResult qtaV = DataValidation.ValidateQta(qta);
                    er_list += qtaV.Error;

                    if (er_list != "")
                    {
                        er_list = "Il ricambio " + Items[i]["codice"] + " presenta errori:" + Environment.NewLine + er_list;

                        er_list += Environment.NewLine + "L'elemento rimarrà in tabella per essere modifica e aggiunto.";

                        OnTopMessage.Alert(er_list);

                        continue;
                    }

                    Offerte.Answer esitoOgg = Offerte.GestioneOggetti.AddObjToOffer(offerID, idir, prezzoOrV.DecimalValue, prezzoScV.DecimalValue, qtaV.IntValue);

                    if (!esitoOgg.Success)
                    {
                        OnTopMessage.Error(esito.Error);
                    }
                    else
                    {
                        netAdded--;
                        Rows2BeDel.Add(i + 1);
                    }
                }

                if (netAdded == 0)
                    this.Close();
                else
                    ImportPDFSupport.DeleteRows(TabItem, Rows2BeDel);
            }
            return;
        }

        //TABLE

        private void BuildHeaderTableItem()
        {

            Label import = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Importare?"
            };

            Label pezzo = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Pezzo"
            };
            Label DescrizionePDF = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Descrizione in PDF"
            };
            Label Descrizione = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Nome e Descrizione"
            };
            Label po = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Prezzo in Offerta"
            };
            Label pof = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Prezzo Finale in Offerta"
            };
            Label qta = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Quantità"
            };

            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)100));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, (float)100));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)100));

            TabItem.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            TabItem.Controls.Add(import, 0, 0);
            TabItem.Controls.Add(pezzo, 1, 0);
            TabItem.Controls.Add(DescrizionePDF, 2, 0);
            TabItem.Controls.Add(Descrizione, 3, 0);
            TabItem.Controls.Add(po, 4, 0);
            TabItem.Controls.Add(pof, 5, 0);
            TabItem.Controls.Add(qta, 6, 0);
        }

        private void PopoluateItemsPanel(List<Dictionary<string, string>> Items)
        {
            int rows = Items.Count;

            for (int i = 0; i < rows; i++)
            {
                TabItem.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                DataValidation.ValidationResult response = GetResource.CollezioneCodiceRicambio(Items[i]["codice"], Convert.ToInt64(AddOffCreaCliente.SelectedValue.ToString()));

                if (!String.IsNullOrEmpty(response.Error))
                {
                    OnTopMessage.Error(response.Error);
                    this.Close();
                    return;
                }

                List<ComboBoxList> entries = JsonConvert.DeserializeObject<List<ComboBoxList>>(response.General);

                CheckBox import = new()
                {
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Name = "import" + i
                };

                ComboBox pezzi = new()
                {
                    Dock = DockStyle.Fill,
                    Anchor = (AnchorStyles.Left | AnchorStyles.Right),
                    Name = "pezzo" + i
                };

                Utility.DataSourceToComboBox(pezzi, entries);

                if (entries.Count == 2)
                    pezzi.SelectedIndex = 1;

                pezzi.SelectedIndexChanged += new System.EventHandler(UpdateDescription_SelectedIndexChanged);

                LinkLabel descOff = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = "Leggi"
                };

                LinkLabel descDb = new()
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = "Leggi",
                    Name = "descDb" + i
                };

                TextBox prezzo_offerta = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = Items[i]["prezzo_uni"],
                    Name = "prez_of" + i
                };
                TextBox prezzo_finale = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = Items[i]["prezzo_uni_scontato"],
                    Name = "prez_fin" + i
                };

                TextBox qta = new()
                {
                    TextAlign = HorizontalAlignment.Center,
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Text = Items[i]["qta"],
                    Name = "qta" + i

                };

                TabItem.Controls.Add(import, 0, i + 1);
                TabItem.Controls.Add(pezzi, 1, i + 1);
                TabItem.Controls.Add(descOff, 2, i + 1);

                TabItem.Controls.Add(descDb, 3, i + 1);

                TabItem.Controls.Add(prezzo_offerta, 4, i + 1);
                TabItem.Controls.Add(prezzo_finale, 5, i + 1);
                TabItem.Controls.Add(qta, 6, i + 1);

                toolTip1.SetToolTip(descOff, Items[i]["descrizione"]);
                toolTip1.SetToolTip(descDb, "Selezionare oggetto");

                UpdateDescription_SelectedIndexChanged(pezzi, EventArgs.Empty);

                if (Convert.ToInt64(pezzi.SelectedValue.ToString()) > 0)
                    import.Checked = true;
            }
            TabItem.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            TabItem.RowCount = rows + 2;
            ImportPDFSupport.ResizeRow(TabItem);
        }

        private void UpdateDescription_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox item = (ComboBox)sender;

            int idField = Convert.ToInt32(item.Name.Replace("pezzo", ""));

            string desc = (item.SelectedItem as ComboBoxList).Descrizione;

            toolTip1.SetToolTip(TabItem.Controls.Find("descDb" + idField, true).First(), desc);
        }

        //BUTTONS
        private void ImportOfferPDFCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OpenOfferPDF_Click(object sender, EventArgs e)
        {
            Utility.OpenPDF(filePath);
        }

    }

}
