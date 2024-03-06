namespace ManagerOrdini.Forms.Update
{
    partial class U10_Offerta
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(U10_Offerta));
            this.U10_Save = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.TableFormLayout = new System.Windows.Forms.TableLayoutPanel();
            this.TableItems = new System.Windows.Forms.TableLayoutPanel();
            this.label6 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.TableFormLayout.SuspendLayout();
            this.TableItems.SuspendLayout();
            this.SuspendLayout();
            // 
            // U10_Save
            // 
            this.U10_Save.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.U10_Save.Location = new System.Drawing.Point(479, 477);
            this.U10_Save.Name = "U10_Save";
            this.U10_Save.Size = new System.Drawing.Size(75, 23);
            this.U10_Save.TabIndex = 2;
            this.U10_Save.Text = "Salva";
            this.U10_Save.UseVisualStyleBackColor = true;
            this.U10_Save.Click += new System.EventHandler(this.U10_Save_Click);
            // 
            // textBox1
            // 
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(6, 6);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(1022, 144);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = resources.GetString("textBox1.Text");
            // 
            // TableFormLayout
            // 
            this.TableFormLayout.BackColor = System.Drawing.Color.White;
            this.TableFormLayout.ColumnCount = 1;
            this.TableFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableFormLayout.Controls.Add(this.TableItems, 0, 2);
            this.TableFormLayout.Controls.Add(this.textBox1, 0, 0);
            this.TableFormLayout.Controls.Add(this.U10_Save, 0, 3);
            this.TableFormLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TableFormLayout.Location = new System.Drawing.Point(0, 0);
            this.TableFormLayout.Name = "TableFormLayout";
            this.TableFormLayout.Padding = new System.Windows.Forms.Padding(3);
            this.TableFormLayout.RowCount = 4;
            this.TableFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.TableFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.TableFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.TableFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TableFormLayout.Size = new System.Drawing.Size(1034, 512);
            this.TableFormLayout.TabIndex = 0;
            // 
            // TableItems
            // 
            this.TableItems.AutoScroll = true;
            this.TableItems.AutoScrollMargin = new System.Drawing.Size(10, 0);
            this.TableItems.AutoSize = true;
            this.TableItems.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.TableItems.BackColor = System.Drawing.Color.White;
            this.TableItems.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.TableItems.ColumnCount = 8;
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.TableItems.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.TableItems.Controls.Add(this.label6, 0, 0);
            this.TableItems.Controls.Add(this.label8, 4, 0);
            this.TableItems.Controls.Add(this.label7, 3, 0);
            this.TableItems.Controls.Add(this.label5, 7, 0);
            this.TableItems.Controls.Add(this.label4, 6, 0);
            this.TableItems.Controls.Add(this.label3, 5, 0);
            this.TableItems.Controls.Add(this.label2, 2, 0);
            this.TableItems.Controls.Add(this.label1, 1, 0);
            this.TableItems.Dock = System.Windows.Forms.DockStyle.Top;
            this.TableItems.Location = new System.Drawing.Point(3, 203);
            this.TableItems.Margin = new System.Windows.Forms.Padding(0);
            this.TableItems.Name = "TableItems";
            this.TableItems.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.TableItems.RowCount = 1;
            this.TableItems.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 51F));
            this.TableItems.Size = new System.Drawing.Size(1028, 53);
            this.TableItems.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(14, 1);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(74, 51);
            this.label6.TabIndex = 8;
            this.label6.Text = "Duplicato";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(613, 1);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(94, 51);
            this.label8.TabIndex = 7;
            this.label8.Text = "Nuovo Codice";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(512, 1);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(94, 51);
            this.label7.TabIndex = 6;
            this.label7.Text = "Codice";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(916, 1);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 51);
            this.label5.TabIndex = 4;
            this.label5.Text = "Prezzo Scontato";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(815, 1);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(94, 51);
            this.label4.TabIndex = 3;
            this.label4.Text = "Prezzo";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(714, 1);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 51);
            this.label3.TabIndex = 2;
            this.label3.Text = "Quantità";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(196, 1);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(309, 51);
            this.label2.TabIndex = 1;
            this.label2.Text = "Nome";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(95, 1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 51);
            this.label1.TabIndex = 0;
            this.label1.Text = "ID Ricambio";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // U10_Offerta
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1034, 512);
            this.Controls.Add(this.TableFormLayout);
            this.MinimumSize = new System.Drawing.Size(1050, 551);
            this.Name = "U10_Offerta";
            this.Text = "Form1";
            this.TableFormLayout.ResumeLayout(false);
            this.TableFormLayout.PerformLayout();
            this.TableItems.ResumeLayout(false);
            this.TableItems.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button U10_Save;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TableLayoutPanel TableFormLayout;
        private System.Windows.Forms.TableLayoutPanel TableItems;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}