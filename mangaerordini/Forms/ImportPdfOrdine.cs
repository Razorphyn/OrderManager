using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Razorphyn;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Razorphyn.Populate;
using static Razorphyn.SupportClasses;

namespace ManagerOrdini.Forms
{
    public partial class ImportPdfOrdine : Form
    {
        readonly string filePath;
        readonly Dictionary<string, string> orderInfo = new() { };
        readonly List<Dictionary<string, string>> Items = new();


        long orderID = 0;
        long offerID = 0;
        long offerIDOriginal = 0;
        DateTime etaorder = DateTime.MinValue;

        List<long> ricambiOfferta = new();

        readonly TableLayoutPanel TabItem;

        public ImportPdfOrdine(Dictionary<string, string> Form1_offerInfo, List<Dictionary<string, string>> Form1_Items, string filePath)
        {
            InitializeComponent();

            this.Text = "Importa Ordine";

            TabItem = OrderItemCollection;

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

            this.orderInfo = Form1_offerInfo;
            this.Items = Form1_Items;
            this.filePath = filePath;
            this.offerID = this.offerIDOriginal = (String.IsNullOrEmpty(orderInfo["numeroOff"])) ? -1 : Offerte.GetResources.GetOfferIdFromCodice(orderInfo["numeroOff"]);

            FieldOrdNOrdine.Text = orderInfo["numero"];
            FieldOrdData.Text = orderInfo["data"];
            FieldOrdETA.Text = orderInfo["ETA"];

            Populate_combobox_clienti(new ComboBox[] { ComboBoxOrdCliente });
            Populate_combobox_statoOrdini(new ComboBox[] { FieldOrdSpedGestione });

            (long id_cliente, long id_sede) = GetResource.GetClientIdFromNumero(orderInfo["cliente"]);
            int index = Utility.FindIndexFromValue(ComboBoxOrdCliente, id_cliente);
            ComboBoxOrdCliente.SelectedIndex = index;

            Populate_combobox_sedi(new ComboBox[] { ComboBoxOrdSede }, id_cliente);

            if (ComboBoxOrdSede.DataSource != null)
            {
                index = Utility.FindIndexFromValue(ComboBoxOrdSede, id_sede);
                ComboBoxOrdSede.SelectedIndex = index;
                Populate_combobox_ordini_crea_offerta(ComboBoxOrdOfferta, idcl: id_cliente, idsd: id_sede, stato: null);

                index = Utility.FindIndexFromValue(ComboBoxOrdOfferta, this.offerID);
                ComboBoxOrdOfferta.SelectedIndex = index;
            }

            Populate_combobox_pref(ComboBoxOrdContatto, id_cliente);
            Populate_combobox_FieldOrdSpedGestione(FieldOrdStato);

            Populate_ricambi();
            BuildHeaderTableItem();
            PopoluateItemsPanel(Items);

            ComboBoxOrdCliente.SelectedIndexChanged += ComboBoxOrdCliente_SelectedIndexChanged;
            ComboBoxOrdSede.SelectedIndexChanged += ComboBoxOrdSede_SelectedIndexChanged;
            ComboBoxOrdOfferta.SelectedIndexChanged += ComboBoxOrdOfferta_SelectedIndexChanged;

        }

        private void BtCreaOrdine_Click(object sender, EventArgs e)
        {
            List<Item> items = BuildItemList(etaorder, offerID);
            if (!CheckUniqueItemEntry(items))
                return;

            if (orderID == 0)
            {
                string commandText;

                long id_offerta = (CheckBoxOrdOffertaNonPresente.Checked == false) ? Convert.ToInt64(ComboBoxOrdOfferta.SelectedValue.ToString()) : -1;

                long? id_cl = (CheckBoxOrdOffertaNonPresente.Checked == true) ? Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString()) : null;
                long id_contatto = (CheckBoxOrdOffertaNonPresente.Checked == true && Convert.ToInt64(ComboBoxOrdContatto.SelectedValue.ToString()) > 0) ? Convert.ToInt64(ComboBoxOrdContatto.SelectedValue.ToString()) : -1;

                long idsd = Convert.ToInt64(ComboBoxOrdSede.SelectedValue.ToString());

                string n_ordine = FieldOrdNOrdine.Text.Trim();

                string dataOrdString = FieldOrdData.Text.Trim();
                string dataETAString = FieldOrdETA.Text.Trim();

                string spedizioni = FieldOrdSped.Text.Trim();
                int gestSP = Convert.ToInt32(FieldOrdSpedGestione.SelectedValue.ToString());

                int stato_ordine = Convert.ToInt32(FieldOrdStato.SelectedValue.ToString());
                stato_ordine = (stato_ordine < 0) ? 0 : stato_ordine;

                DataValidation.ValidationResult answer;
                DataValidation.ValidationResult prezzoSpedizione = new();
                DataValidation.ValidationResult dataOrdValue;
                DataValidation.ValidationResult dataETAOrdValue;
                DataValidation.ValidationResult tot_ordineV = new();
                DataValidation.ValidationResult prezzo_finaleV = new();
                DataValidation.ValidationResult scontoV = new() { DecimalValue = 0 };

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

                tot_ordineV.DecimalValue = 0;
                prezzo_finaleV.DecimalValue = 0;

                if (CheckBoxOrdOffertaNonPresente.Checked == false)
                {
                    commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[offerte_elenco] WHERE ([Id] = @id_offerta) LIMIT 1;";
                    int UserExist = 0;

                    using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
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
                            OnTopMessage.Error("Errore durante verifica ID Offerta. Codice: " + DbTools.ReturnErorrCode(ex));
                            return;
                        }
                    }
                }

                if (er_list != "")
                {
                    OnTopMessage.Alert(er_list);
                    return;
                }


                Ordini.Answer esito = Ordini.GestioneOrdini.CreateOrder(n_ordine, id_offerta, CheckBoxOrdOffertaNonPresente.Checked, false, idsd, id_contatto, dataOrdValue.DateValue, dataETAOrdValue.DateValue,
                                                                         tot_ordineV.DecimalValue, scontoV.DecimalValue, prezzo_finaleV.DecimalValue, stato_ordine, gestSP, prezzoSpedizione.DecimalValue
                                                                         );

                if (esito.Success)
                {
                    OnTopMessage.Information("Ordine Creato.");

                    etaorder = dataETAOrdValue.DateValue;
                    orderID = esito.Id;
                    offerID = id_offerta;
                }
                else
                {
                    OnTopMessage.Error(esito.Error);
                }

            }

            if (orderID > 0)
            {

                int netAdded = 0;
                int c = items.Count();

                List<int> Rows2BeDel = new();

                for (int i = 0; i < c; i++)
                {

                    netAdded++;

                    if (!String.IsNullOrEmpty(items[i].Error))
                    {
                        OnTopMessage.Error(items[i].Error);
                        continue;
                    }

                    long idoggric = 0;
                    if (!items[i].isNotOffer)
                    {
                        idoggric = Convert.ToInt64(GetResource.GetIdRicambioInOffferta(offerID, items[i].id).LongValue);
                    }

                    Ordini.Answer esitoOgg = Ordini.GestioneOggetti.AddObjToOrder(orderID, items[i].id, items[i].eta, items[i].prezzo, items[i].prezzo_scontato, items[i].qta,
                                                                                    items[i].isNotOffer, false, idoggric);

                    if (!esitoOgg.Success)
                    {
                        OnTopMessage.Error(esitoOgg.Error);
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

        //COMBOBOX
        private void ComboBoxOrdCliente_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboBoxOrdCliente.DataSource == null)
            {
                return;
            }

            DrawingControl.SuspendDrawing(TabItem);

            long curItemValue = Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString());

            if (curItemValue > 0)
            {
                Populate_combobox_sedi(new ComboBox[] { ComboBoxOrdSede }, curItemValue);
                BuildHeaderTableItem();
                ComboBoxOrdSede.Enabled = true;
            }
            else
            {
                ImportPDFSupport.DeleteControls(TabItem);
                TableDefaultMessage("Selezionare un cliente e la sede");

                ComboBoxOrdSede.Enabled = false;
                CheckBoxOrdOffertaNonPresente.Enabled = false;

                Populate_combobox_dummy(ComboBoxOrdSede);

                FieldOrdStato.SelectedIndex = 0;

            }
            DrawingControl.ResumeDrawing(TabItem);

            return;
        }

        private void ComboBoxOrdSede_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ComboBoxOrdCliente.DataSource == null || ComboBoxOrdSede.DataSource == null)
            {
                return;
            }

            DrawingControl.SuspendDrawing(TabItem);
            long idcl = Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString());
            long idsd = Convert.ToInt64(ComboBoxOrdSede.SelectedValue.ToString());

            if (idsd > 0)
            {
                Populate_combobox_ordini_crea_offerta(ComboBoxOrdOfferta, idcl: idcl, idsd: idsd, stato: null);
                Populate_combobox_pref(ComboBoxOrdContatto, idcl, idsd);

                ComboBoxOrdOfferta.Enabled = true;
                CheckBoxOrdOffertaNonPresente.Enabled = true;
                ComboBoxOrdContatto.Enabled = true;

                this.offerID = this.offerIDOriginal;

                if (ComboBoxOrdOfferta.Items.Count < 2)
                {
                    ComboBoxOrdOfferta.Enabled = false;
                    CheckBoxOrdOffertaNonPresente.Enabled = false;
                    CheckBoxOrdOffertaNonPresente.Checked = true;
                }
                else
                {
                    CheckBoxOrdOffertaNonPresente.Checked = false;
                    int index = Utility.FindIndexFromValue(ComboBoxOrdOfferta, this.offerID);
                    ComboBoxOrdOfferta.SelectedIndex = index;
                }

                BuildHeaderTableItem();
                PopoluateItemsPanel(Items);

                DrawingControl.ResumeDrawing(TabItem);
                return;
            }
            else
            {
                ComboBoxOrdOfferta.Enabled = false;
                CheckBoxOrdOffertaNonPresente.Enabled = false;

                Populate_combobox_pref(ComboBoxOrdContatto, idcl);
                ComboBoxOrdContatto.Enabled = true;

                Populate_combobox_dummy(ComboBoxOrdOfferta);
                ComboBoxOrdOfferta.SelectedIndex = 0;

                ImportPDFSupport.DeleteControls(TabItem);
                //BuildHeaderTableItem();
                TableDefaultMessage("Selezionare Sede");
            }

            DrawingControl.ResumeDrawing(TabItem);
            return;
        }

        private void ComboBoxOrdOfferta_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxOrdOfferta.DataSource == null)
            {
                return;
            }


            this.offerID = Convert.ToInt64(ComboBoxOrdOfferta.SelectedValue.ToString());

            if (this.offerID > 0)
            {
                CheckBoxOrdOffertaNonPresente.Checked = false;

                Populate_ricambi();

                DrawingControl.SuspendDrawing(TabItem);
                ImportPDFSupport.DeleteControls(TabItem);
                BuildHeaderTableItem();
                PopoluateItemsPanel(Items);
                DrawingControl.ResumeDrawing(TabItem);
            }
            else
            {
                ricambiOfferta.Clear();
                CheckBoxOrdOffertaNonPresente.Checked = true;
            }

            return;
        }

        private void Populate_ricambi()
        {
            DataValidation.ValidationResult AnswerRicambiOfferta = Offerte.GetResources.CollezioneIdRicambiOfferta(this.offerID);

            if (!String.IsNullOrEmpty(AnswerRicambiOfferta.Error))
            {
                OnTopMessage.Error(AnswerRicambiOfferta.Error);
                this.Close();
                return;
            }

            this.ricambiOfferta = JsonConvert.DeserializeObject<List<long>>(AnswerRicambiOfferta.General);
        }


        //BUILD ITEM LIST

        private List<Item> BuildItemList(DateTime dataETAOrdValue, long id_offer)
        {
            CheckBox[] CheckBoxImport = TabItem.Controls.OfType<CheckBox>().Where(i => i.Name.StartsWith("import")).ToArray();
            ComboBox[] comboBoxesPezzi = TabItem.Controls.OfType<ComboBox>().Where(i => i.Name.StartsWith("pezzo")).ToArray();
            CheckBox[] CheckBoxInOff = TabItem.Controls.OfType<CheckBox>().Where(i => i.Name.StartsWith("isOffer")).ToArray();
            TextBox[] comboBoxesPrezziOff = TabItem.Controls.OfType<TextBox>().Where(i => i.Name.StartsWith("prez_of")).ToArray();
            TextBox[] comboBoxesPrezziFin = TabItem.Controls.OfType<TextBox>().Where(i => i.Name.StartsWith("prez_fin")).ToArray();
            TextBox[] comboBoxesQta = TabItem.Controls.OfType<TextBox>().Where(i => i.Name.StartsWith("qta")).ToArray();
            DateTimePicker[] etaPicker = TabItem.Controls.OfType<DateTimePicker>().Where(i => i.Name.StartsWith("eta")).ToArray();

            int c = comboBoxesPezzi.Count();

            List<Item> listItems = new List<Item>();

            for (int i = 0; i < c; i++)
            {
                if (!CheckBoxImport[i].Checked)
                {
                    continue;
                }

                Item item = new Item(
                    Convert.ToInt64(comboBoxesPezzi[i].SelectedValue.ToString()),
                    comboBoxesPrezziOff[i].Text.Trim(),
                    comboBoxesPrezziFin[i].Text.Trim(),
                    CheckBoxInOff[i].Checked,
                    comboBoxesQta[i].Text.Trim(),
                    etaPicker[i].Text.Trim()
                );

                string er_list = item.Error;

                if (DateTime.Compare(item.eta, dataETAOrdValue) < 0)
                {
                    item.Error += "La data di arrivo del ricambio è antecendente all'arrivo dell'ordine " + Environment.NewLine;
                }

                if (item.Error != "")
                {
                    item.Error = "Il ricambio " + Items[i]["codice"] + " presenta errori:" + Environment.NewLine + item.Error;
                    item.Error += Environment.NewLine + "L'elemento rimarrà in tabella per essere modificato.";
                }
                listItems.Add(item);
            }

            //return RemoveDuplicateItemEntry(listItems);
            return listItems;
        }

        private List<Item> RemoveDuplicateItemEntry(List<Item> items)
        {
            int c = items.Count;

            for (int i = 0; i < c; i++)
            {
                for (int j = i + 1; j < c; j++)
                {
                    if (items[i].id == items[j].id && DateTime.Compare(items[i].eta, items[j].eta) == 0)
                    {
                        string input = "";
                        while (input != "1" && input != "2")
                            input = Interaction.InputBox("Selezionare (1 o 2) tra i due seguenti oggetti quale tenere. Le quantità veranno sommate." + Environment.NewLine +
                                                        "1)\t ETA: " + items[i].eta.Date + "; QTA:" + items[i].qta + Environment.NewLine +
                                                        "\t\t PREZZO: " + items[i].prezzo + "; PREZZO Scontato: " + items[i].prezzo_scontato + Environment.NewLine +
                                                        Environment.NewLine +
                                                        "2)\t ETA:" + items[j].eta.Date + "; QTA:" + items[j].qta + Environment.NewLine +
                                                        "\t\t PREZZO: " + items[j].prezzo + "; PREZZO: " + items[j].prezzo_scontato
                                                        , "Selezionare Oggetto Duplicato").Trim();

                        int index = Convert.ToInt32(input);

                        if (index == 1)
                        {
                            items[i].qta += items[j].qta;
                        }
                        else
                        {
                            items[j].qta += items[i].qta;
                            items[i] = items[j];
                        }

                        items.RemoveAt(j);
                        c--;
                    }
                }
            }

            return items;
        }

        private bool CheckUniqueItemEntry(List<Item> items)
        {
            int c = items.Count;

            for (int i = 0; i < c; i++)
            {
                for (int j = i + 1; j < c; j++)
                {
                    if (items[i].id == items[j].id && DateTime.Compare(items[i].eta, items[j].eta) == 0)
                    {
                        OnTopMessage.Information("L'oggetto con ID " + items[i].id + " presenta un'altra riga con la stessa data di consegna."+Environment.NewLine+"Cambiare la data di consegna o non importare uno dei due elementi.");
                        return false;
                    }
                }
            }

            return true;
        }

        //TABLE

        private void TableDefaultMessage(string text)
        {
            TabItem.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Label selectCl = new()
            {
                Dock = DockStyle.Fill,
                Text = text
            };
            TabItem.Controls.Add(selectCl, 0, 0);
            tableLayoutPanel1.SetColumnSpan(selectCl, 2);
            return;
        }

        private void BuildHeaderTableItem()
        {
            ImportPDFSupport.DeleteControls(TabItem);

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

            Label isoffer = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "In Offerta"
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
            Label eta = new()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "ETA"
            };


            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)100));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, (float)100));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)120));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)100));
            TabItem.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)100));

            TabItem.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            int i = 0;
            TabItem.Controls.Add(import, i, 0); i++;
            TabItem.Controls.Add(pezzo, i, 0); i++;
            TabItem.Controls.Add(isoffer, i, 0); i++;
            TabItem.Controls.Add(DescrizionePDF, i, 0); i++;
            TabItem.Controls.Add(Descrizione, i, 0); i++;
            TabItem.Controls.Add(po, i, 0); i++;
            TabItem.Controls.Add(pof, i, 0); i++;
            TabItem.Controls.Add(qta, i, 0); i++;
            TabItem.Controls.Add(eta, i, 0); i++;
        }

        private void PopoluateItemsPanel(List<Dictionary<string, string>> Items)
        {
            ImportPDFSupport.DeleteControls(TabItem);
            BuildHeaderTableItem();

            int rows = Items.Count;

            for (int i = 0; i < rows; i++)
            {
                TabItem.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

                DataValidation.ValidationResult answerRicambi = GetResource.CollezioneCodiceRicambio(Items[i]["codice"], Convert.ToInt64(ComboBoxOrdCliente.SelectedValue.ToString()));

                if (!String.IsNullOrEmpty(answerRicambi.Error))
                {
                    OnTopMessage.Error(answerRicambi.Error);
                    this.Close();
                    return;
                }

                List<ComboBoxList> ricambi = JsonConvert.DeserializeObject<List<ComboBoxList>>(answerRicambi.General);


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

                Utility.DataSourceToComboBox(pezzi, ricambi);

                if (ricambi.Count == 2)
                    pezzi.SelectedIndex = 1;

                pezzi.SelectedIndexChanged += new System.EventHandler(RicambioSelection_SelectedIndexChanged);

                CheckBox isOffer = new()
                {
                    Dock = DockStyle.Fill,
                    Anchor = AnchorStyles.None,
                    Name = "isOffer" + i,
                    Enabled = false,
                    Checked = !(ricambiOfferta.Count < 1)
                };

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

                DateTimePicker eta = new()
                {
                    Dock = DockStyle.Fill,
                    Text = (String.IsNullOrEmpty(Items[i]["eta"])) ? orderInfo["ETA"] : Items[i]["eta"],
                    Anchor = AnchorStyles.None,
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy",
                    Name = "eta" + i
                };

                int j = 0;
                TabItem.Controls.Add(import, j, i + 1); j++;
                TabItem.Controls.Add(pezzi, j, i + 1); j++;
                TabItem.Controls.Add(isOffer, j, i + 1); j++;
                TabItem.Controls.Add(descOff, j, i + 1); j++;

                TabItem.Controls.Add(descDb, j, i + 1); j++;

                TabItem.Controls.Add(prezzo_offerta, j, i + 1); j++;
                TabItem.Controls.Add(prezzo_finale, j, i + 1); j++;
                TabItem.Controls.Add(qta, j, i + 1); j++;

                TabItem.Controls.Add(eta, j, i + 1); j++;

                toolTip1.SetToolTip(descOff, Items[i]["descrizione"]);
                toolTip1.SetToolTip(descDb, "Selezionare oggetto");

                UpdateDescription_SelectedIndexChanged(pezzi, EventArgs.Empty);

                long ItemId = Convert.ToInt64(pezzi.SelectedValue.ToString());

                if (ItemId > 0)
                {
                    import.Checked = true;
                    isOffer.Checked = IsInOffer(ItemId);
                }
            }

            TabItem.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            TabItem.RowCount = rows + 2;
            ImportPDFSupport.ResizeRow(TabItem);
        }

        private void RicambioSelection_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            UpdateDescription_SelectedIndexChanged(sender, e);

            ComboBox ctr = (sender as ComboBox);
            string name = ctr.Name;
            long id = Convert.ToInt64(ctr.SelectedValue.ToString());

            string ctrName = "isOffer" + Regex.Replace(name, @"[^\d]", "");
            CheckBox ctrCheckBox = this.Controls.Find(ctrName, true)[0] as CheckBox;
            ctrCheckBox.Checked = IsInOffer(id);

            return;
        }

        internal bool IsInOffer(long id)
        {
            return ricambiOfferta.Contains(id);
        }

        private void UpdateDescription_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox item = (ComboBox)sender;

            int idField = Convert.ToInt32(item.Name.Replace("pezzo", ""));

            string desc = (item.SelectedItem as ComboBoxList).Descrizione;

            toolTip1.SetToolTip(TabItem.Controls.Find("descDb" + idField, true).First(), desc);
        }

        private void UncheckItemOffer()
        {
            int c = Items.Count;

            for (int i = 0; i < c; i++)
            {
                string ctrName = "isOffer" + i;
                Control[] ctrCheckBox = this.Controls.Find(ctrName, true);
                if (ctrCheckBox != null && ctrCheckBox.Length > 0)
                {
                    (ctrCheckBox[0] as CheckBox).Checked = false;
                }

            }
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

        private void CheckBoxOrdOffertaNonPresente_CheckedChanged(object sender, EventArgs e)
        {
            UncheckItemOffer();
            CheckBox obj = (CheckBox)sender;

            if (obj.Checked && ComboBoxOrdOfferta.SelectedIndex > 0)
                ComboBoxOrdOfferta.SelectedIndex = 0;
        }

        private void FieldOrdETA_ValueChanged(object sender, EventArgs e)
        {
            DateTime date = (sender as DateTimePicker).Value;

            DateTimePicker[] etaPicker = TabItem.Controls.OfType<DateTimePicker>().Where(i => i.Name.StartsWith("eta")).ToArray();

            foreach (DateTimePicker picker in etaPicker)
            {
                picker.MinDate = date;
            }
        }
    }

    public class Item
    {
        public long id;
        public decimal prezzo;
        public decimal prezzo_scontato;
        public bool isNotOffer;
        public int qta;
        public DateTime eta;

        public bool Added { get; set; } = false;
        public string Error { get; set; } = "";

        public Item(long idir, string prezzoOr, string prezzoSc, bool isOffer, string qta, string etaItem)
        {
            if (idir < 1)
            {
                Error += "Il ricambio non esiste nel database.";
            }

            DataValidation.ValidationResult prezzoOrV = DataValidation.ValidatePrezzo(prezzoOr);
            Error += prezzoOrV.Error;

            DataValidation.ValidationResult prezzoScV = DataValidation.ValidatePrezzo(prezzoSc);
            Error += prezzoScV.Error;

            DataValidation.ValidationResult qtaV = DataValidation.ValidateQta(qta);
            Error += qtaV.Error;

            DataValidation.ValidationResult delivery = DataValidation.ValidateDate(etaItem);
            Error += delivery.Error;

            if (String.IsNullOrEmpty(Error))
            {
                this.id = idir;
                this.qta = qtaV.IntValue;
                this.prezzo_scontato = prezzoScV.DecimalValue;
                this.prezzo = prezzoOrV.DecimalValue;
                this.eta = delivery.DateValue != null ? (DateTime)delivery.DateValue : DateTime.MinValue;
                this.isNotOffer = !isOffer;
            }
        }
    }
}
