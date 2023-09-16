
namespace ManagerOrdini.Forms
{
    partial class ImportPdfOrdine
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {			
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label8;
            System.Windows.Forms.Label label81;
            System.Windows.Forms.Label label100;
            System.Windows.Forms.Label label72;
            System.Windows.Forms.Label label62;
            System.Windows.Forms.Label label58;
            System.Windows.Forms.Label label63;
            System.Windows.Forms.Label label61;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label79;
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.OrderItemCollection = new System.Windows.Forms.TableLayoutPanel();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.FieldOrdData = new System.Windows.Forms.DateTimePicker();
            this.FieldOrdNOrdine = new System.Windows.Forms.TextBox();
            this.ComboBoxOrdContatto = new System.Windows.Forms.ComboBox();
            this.ComboBoxOrdSede = new System.Windows.Forms.ComboBox();
            this.ComboBoxOrdCliente = new System.Windows.Forms.ComboBox();
            this.ComboBoxOrdOfferta = new System.Windows.Forms.ComboBox();
            this.CheckBoxOrdOffertaNonPresente = new System.Windows.Forms.CheckBox();
            this.FieldOrdETA = new System.Windows.Forms.DateTimePicker();
            this.FieldOrdSped = new System.Windows.Forms.TextBox();
            this.FieldOrdSpedGestione = new System.Windows.Forms.ComboBox();
            this.FieldOrdStato = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.ImportOfferPDFCancel = new System.Windows.Forms.Button();
            this.OpenOfferPDF = new System.Windows.Forms.Button();
            this.ImportOfferPDFAdd = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            label8 = new System.Windows.Forms.Label();
            label81 = new System.Windows.Forms.Label();
            label100 = new System.Windows.Forms.Label();
            label72 = new System.Windows.Forms.Label();
            label62 = new System.Windows.Forms.Label();
            label58 = new System.Windows.Forms.Label();
            label63 = new System.Windows.Forms.Label();
            label61 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            label79 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.OrderItemCollection.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Dock = System.Windows.Forms.DockStyle.Fill;
            label8.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label8.Location = new System.Drawing.Point(387, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(154, 38);
            label8.TabIndex = 108;
            label8.Text = "Sede";
            label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label81
            // 
            label81.AutoSize = true;
            label81.Dock = System.Windows.Forms.DockStyle.Fill;
            label81.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label81.Location = new System.Drawing.Point(3, 114);
            label81.Name = "label81";
            label81.Size = new System.Drawing.Size(132, 41);
            label81.TabIndex = 106;
            label81.Text = "Costo Spedizione";
            label81.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label100
            // 
            label100.AutoSize = true;
            label100.Dock = System.Windows.Forms.DockStyle.Fill;
            label100.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label100.Location = new System.Drawing.Point(387, 114);
            label100.Name = "label100";
            label100.Size = new System.Drawing.Size(154, 41);
            label100.TabIndex = 104;
            label100.Text = "Gestione Costo Sp.";
            label100.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label72
            // 
            label72.AutoSize = true;
            label72.Dock = System.Windows.Forms.DockStyle.Fill;
            label72.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label72.Location = new System.Drawing.Point(792, 114);
            label72.Name = "label72";
            label72.Size = new System.Drawing.Size(83, 41);
            label72.TabIndex = 102;
            label72.Text = "Stato";
            label72.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label62
            // 
            label62.AutoSize = true;
            label62.Dock = System.Windows.Forms.DockStyle.Fill;
            label62.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label62.Location = new System.Drawing.Point(792, 0);
            label62.Name = "label62";
            label62.Size = new System.Drawing.Size(83, 38);
            label62.TabIndex = 100;
            label62.Text = "Contatto";
            label62.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label58
            // 
            label58.AutoSize = true;
            label58.Dock = System.Windows.Forms.DockStyle.Fill;
            label58.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label58.Location = new System.Drawing.Point(3, 0);
            label58.Name = "label58";
            label58.Size = new System.Drawing.Size(132, 38);
            label58.TabIndex = 98;
            label58.Text = "Cliente";
            label58.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label63
            // 
            label63.AutoSize = true;
            label63.Dock = System.Windows.Forms.DockStyle.Fill;
            label63.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label63.Location = new System.Drawing.Point(793, 38);
            label63.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label63.Name = "label63";
            label63.Size = new System.Drawing.Size(81, 38);
            label63.TabIndex = 88;
            label63.Text = "N. Ordine";
            label63.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label61
            // 
            label61.AutoSize = true;
            label61.Dock = System.Windows.Forms.DockStyle.Fill;
            label61.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label61.Location = new System.Drawing.Point(4, 76);
            label61.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label61.Name = "label61";
            label61.Size = new System.Drawing.Size(130, 38);
            label61.TabIndex = 89;
            label61.Text = "Data";
            label61.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = System.Windows.Forms.DockStyle.Fill;
            label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label1.Location = new System.Drawing.Point(4, 38);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(130, 38);
            label1.TabIndex = 109;
            label1.Text = "N. Offerta";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label79
            // 
            label79.AutoSize = true;
            label79.Dock = System.Windows.Forms.DockStyle.Fill;
            label79.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label79.Location = new System.Drawing.Point(387, 76);
            label79.Name = "label79";
            label79.Size = new System.Drawing.Size(154, 38);
            label79.TabIndex = 158;
            label79.Text = "Data Consegna";
            label79.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.OrderItemCollection, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28.54512F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 71.45488F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1125, 591);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // OrderItemCollection
            // 
            this.OrderItemCollection.AutoSize = true;
            this.OrderItemCollection.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.OrderItemCollection.ColumnCount = 9;
            this.tableLayoutPanel1.SetColumnSpan(this.OrderItemCollection, 3);
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 114F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 109F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 112F));
            this.OrderItemCollection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 115F));
            this.OrderItemCollection.Controls.Add(this.label11, 9, 0);
            this.OrderItemCollection.Controls.Add(this.label10, 2, 0);
            this.OrderItemCollection.Controls.Add(this.label7, 0, 0);
            this.OrderItemCollection.Controls.Add(this.label6, 7, 0);
            this.OrderItemCollection.Controls.Add(this.label5, 6, 0);
            this.OrderItemCollection.Controls.Add(this.label4, 5, 0);
            this.OrderItemCollection.Controls.Add(this.label3, 4, 0);
            this.OrderItemCollection.Controls.Add(this.label2, 1, 0);
            this.OrderItemCollection.Controls.Add(this.label9, 3, 0);
            this.OrderItemCollection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OrderItemCollection.Location = new System.Drawing.Point(3, 158);
            this.OrderItemCollection.Name = "OrderItemCollection";
            this.OrderItemCollection.RowCount = 2;
            this.OrderItemCollection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.OrderItemCollection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OrderItemCollection.Size = new System.Drawing.Size(1119, 382);
            this.OrderItemCollection.TabIndex = 4;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Location = new System.Drawing.Point(1006, 1);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(109, 50);
            this.label11.TabIndex = 8;
            this.label11.Text = "ETA";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(393, 1);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(66, 50);
            this.label10.TabIndex = 7;
            this.label10.Text = "In Offerta";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(4, 1);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(78, 50);
            this.label7.TabIndex = 6;
            this.label7.Text = "Importare?";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(893, 1);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(106, 50);
            this.label6.TabIndex = 5;
            this.label6.Text = "Quantità";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(783, 1);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(103, 50);
            this.label5.TabIndex = 4;
            this.label5.Text = "Prezzo Finale in Offerta";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(668, 1);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 50);
            this.label4.TabIndex = 3;
            this.label4.Text = "Prezzo in Offerta";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(559, 1);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 50);
            this.label3.TabIndex = 2;
            this.label3.Text = "Nome e Descrizione";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(89, 1);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(297, 50);
            this.label2.TabIndex = 0;
            this.label2.Text = "Pezzo";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(466, 1);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(86, 50);
            this.label9.TabIndex = 1;
            this.label9.Text = "Descrizione in PDF";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 6;
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanel2, 3);
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 138F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 89F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel2.Controls.Add(this.FieldOrdData, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.FieldOrdNOrdine, 5, 1);
            this.tableLayoutPanel2.Controls.Add(this.ComboBoxOrdContatto, 5, 0);
            this.tableLayoutPanel2.Controls.Add(this.ComboBoxOrdSede, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.ComboBoxOrdCliente, 1, 0);
            this.tableLayoutPanel2.Controls.Add(label79, 2, 2);
            this.tableLayoutPanel2.Controls.Add(this.ComboBoxOrdOfferta, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.CheckBoxOrdOffertaNonPresente, 2, 1);
            this.tableLayoutPanel2.Controls.Add(label1, 0, 1);
            this.tableLayoutPanel2.Controls.Add(label8, 2, 0);
            this.tableLayoutPanel2.Controls.Add(label100, 2, 3);
            this.tableLayoutPanel2.Controls.Add(label72, 4, 3);
            this.tableLayoutPanel2.Controls.Add(label62, 4, 0);
            this.tableLayoutPanel2.Controls.Add(label58, 0, 0);
            this.tableLayoutPanel2.Controls.Add(label63, 4, 1);
            this.tableLayoutPanel2.Controls.Add(label61, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.FieldOrdETA, 3, 2);
            this.tableLayoutPanel2.Controls.Add(this.FieldOrdSped, 1, 3);
            this.tableLayoutPanel2.Controls.Add(label81, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.FieldOrdSpedGestione, 5, 3);
            this.tableLayoutPanel2.Controls.Add(this.FieldOrdStato, 3, 3);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(1125, 155);
            this.tableLayoutPanel2.TabIndex = 2;
            // 
            // FieldOrdData
            // 
            this.FieldOrdData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldOrdData.CalendarFont = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdData.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdData.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.FieldOrdData.Location = new System.Drawing.Point(141, 82);
            this.FieldOrdData.Name = "FieldOrdData";
            this.FieldOrdData.Size = new System.Drawing.Size(240, 26);
            this.FieldOrdData.TabIndex = 164;
            // 
            // FieldOrdNOrdine
            // 
            this.FieldOrdNOrdine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldOrdNOrdine.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdNOrdine.Location = new System.Drawing.Point(881, 42);
            this.FieldOrdNOrdine.Name = "FieldOrdNOrdine";
            this.FieldOrdNOrdine.Size = new System.Drawing.Size(241, 29);
            this.FieldOrdNOrdine.TabIndex = 163;
            // 
            // ComboBoxOrdContatto
            // 
            this.ComboBoxOrdContatto.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxOrdContatto.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ComboBoxOrdContatto.FormattingEnabled = true;
            this.ComboBoxOrdContatto.Location = new System.Drawing.Point(881, 5);
            this.ComboBoxOrdContatto.Name = "ComboBoxOrdContatto";
            this.ComboBoxOrdContatto.Size = new System.Drawing.Size(241, 28);
            this.ComboBoxOrdContatto.TabIndex = 162;
            // 
            // ComboBoxOrdSede
            // 
            this.ComboBoxOrdSede.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxOrdSede.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ComboBoxOrdSede.FormattingEnabled = true;
            this.ComboBoxOrdSede.Location = new System.Drawing.Point(547, 5);
            this.ComboBoxOrdSede.Name = "ComboBoxOrdSede";
            this.ComboBoxOrdSede.Size = new System.Drawing.Size(239, 28);
            this.ComboBoxOrdSede.TabIndex = 161;
            // 
            // ComboBoxOrdCliente
            // 
            this.ComboBoxOrdCliente.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxOrdCliente.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ComboBoxOrdCliente.FormattingEnabled = true;
            this.ComboBoxOrdCliente.Location = new System.Drawing.Point(141, 5);
            this.ComboBoxOrdCliente.Name = "ComboBoxOrdCliente";
            this.ComboBoxOrdCliente.Size = new System.Drawing.Size(240, 28);
            this.ComboBoxOrdCliente.TabIndex = 159;
            // 
            // ComboBoxOrdOfferta
            // 
            this.ComboBoxOrdOfferta.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxOrdOfferta.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ComboBoxOrdOfferta.FormattingEnabled = true;
            this.ComboBoxOrdOfferta.Location = new System.Drawing.Point(141, 42);
            this.ComboBoxOrdOfferta.Name = "ComboBoxOrdOfferta";
            this.ComboBoxOrdOfferta.Size = new System.Drawing.Size(240, 29);
            this.ComboBoxOrdOfferta.TabIndex = 157;
            // 
            // CheckBoxOrdOffertaNonPresente
            // 
            this.CheckBoxOrdOffertaNonPresente.AutoSize = true;
            this.CheckBoxOrdOffertaNonPresente.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CheckBoxOrdOffertaNonPresente.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CheckBoxOrdOffertaNonPresente.Location = new System.Drawing.Point(387, 41);
            this.CheckBoxOrdOffertaNonPresente.Name = "CheckBoxOrdOffertaNonPresente";
            this.CheckBoxOrdOffertaNonPresente.Size = new System.Drawing.Size(154, 32);
            this.CheckBoxOrdOffertaNonPresente.TabIndex = 156;
            this.CheckBoxOrdOffertaNonPresente.Text = "Non Presente";
            this.CheckBoxOrdOffertaNonPresente.UseVisualStyleBackColor = true;
            this.CheckBoxOrdOffertaNonPresente.CheckedChanged += new System.EventHandler(this.CheckBoxOrdOffertaNonPresente_CheckedChanged);
            // 
            // FieldOrdETA
            // 
            this.FieldOrdETA.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldOrdETA.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdETA.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.FieldOrdETA.Location = new System.Drawing.Point(547, 82);
            this.FieldOrdETA.Name = "FieldOrdETA";
            this.FieldOrdETA.Size = new System.Drawing.Size(239, 26);
            this.FieldOrdETA.TabIndex = 165;
            this.FieldOrdETA.Value = new System.DateTime(2021, 12, 26, 22, 26, 7, 0);
            // 
            // FieldOrdSped
            // 
            this.FieldOrdSped.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldOrdSped.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdSped.Location = new System.Drawing.Point(141, 120);
            this.FieldOrdSped.Name = "FieldOrdSped";
            this.FieldOrdSped.Size = new System.Drawing.Size(240, 29);
            this.FieldOrdSped.TabIndex = 166;
            // 
            // FieldOrdSpedGestione
            // 
            this.FieldOrdSpedGestione.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldOrdSpedGestione.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdSpedGestione.FormattingEnabled = true;
            this.FieldOrdSpedGestione.Location = new System.Drawing.Point(881, 120);
            this.FieldOrdSpedGestione.Name = "FieldOrdSpedGestione";
            this.FieldOrdSpedGestione.Size = new System.Drawing.Size(241, 29);
            this.FieldOrdSpedGestione.TabIndex = 167;
            // 
            // FieldOrdStato
            // 
            this.FieldOrdStato.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldOrdStato.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FieldOrdStato.FormattingEnabled = true;
            this.FieldOrdStato.Location = new System.Drawing.Point(547, 120);
            this.FieldOrdStato.Name = "FieldOrdStato";
            this.FieldOrdStato.Size = new System.Drawing.Size(239, 29);
            this.FieldOrdStato.TabIndex = 168;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.Controls.Add(this.ImportOfferPDFCancel, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.OpenOfferPDF, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.ImportOfferPDFAdd, 2, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 546);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1119, 42);
            this.tableLayoutPanel3.TabIndex = 5;
            // 
            // ImportOfferPDFCancel
            // 
            this.ImportOfferPDFCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ImportOfferPDFCancel.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.ImportOfferPDFCancel.Location = new System.Drawing.Point(97, 5);
            this.ImportOfferPDFCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ImportOfferPDFCancel.Name = "ImportOfferPDFCancel";
            this.ImportOfferPDFCancel.Size = new System.Drawing.Size(178, 32);
            this.ImportOfferPDFCancel.TabIndex = 111;
            this.ImportOfferPDFCancel.Text = "Annulla";
            this.ImportOfferPDFCancel.UseVisualStyleBackColor = true;
            this.ImportOfferPDFCancel.Click += new System.EventHandler(this.ImportOfferPDFCancel_Click);
            // 
            // OpenOfferPDF
            // 
            this.OpenOfferPDF.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OpenOfferPDF.Location = new System.Drawing.Point(473, 3);
            this.OpenOfferPDF.Name = "OpenOfferPDF";
            this.OpenOfferPDF.Size = new System.Drawing.Size(173, 36);
            this.OpenOfferPDF.TabIndex = 110;
            this.OpenOfferPDF.Text = "Apri PDF";
            this.OpenOfferPDF.UseVisualStyleBackColor = true;
            this.OpenOfferPDF.Click += new System.EventHandler(this.OpenOfferPDF_Click);
            // 
            // ImportOfferPDFAdd
            // 
            this.ImportOfferPDFAdd.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ImportOfferPDFAdd.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.ImportOfferPDFAdd.Location = new System.Drawing.Point(843, 5);
            this.ImportOfferPDFAdd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ImportOfferPDFAdd.Name = "ImportOfferPDFAdd";
            this.ImportOfferPDFAdd.Size = new System.Drawing.Size(178, 32);
            this.ImportOfferPDFAdd.TabIndex = 112;
            this.ImportOfferPDFAdd.Text = "Crea Ordine";
            this.ImportOfferPDFAdd.UseVisualStyleBackColor = true;
            // 
            // ImportPdfOrdine
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(1125, 591);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ImportPdfOrdine";
            this.Text = "Form1";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.OrderItemCollection.ResumeLayout(false);
            this.OrderItemCollection.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.CheckBox CheckBoxOrdOffertaNonPresente;
        private System.Windows.Forms.ComboBox ComboBoxOrdOfferta;
        private System.Windows.Forms.TableLayoutPanel OrderItemCollection;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox ComboBoxOrdCliente;
        private System.Windows.Forms.ComboBox ComboBoxOrdSede;
        private System.Windows.Forms.ComboBox ComboBoxOrdContatto;
        private System.Windows.Forms.TextBox FieldOrdNOrdine;
        private System.Windows.Forms.DateTimePicker FieldOrdData;
        private System.Windows.Forms.DateTimePicker FieldOrdETA;
        private System.Windows.Forms.TextBox FieldOrdSped;
        private System.Windows.Forms.ComboBox FieldOrdSpedGestione;
        private System.Windows.Forms.ComboBox FieldOrdStato;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button OpenOfferPDF;
        private System.Windows.Forms.Button ImportOfferPDFCancel;
        private System.Windows.Forms.Button ImportOfferPDFAdd;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label11;
    }
}