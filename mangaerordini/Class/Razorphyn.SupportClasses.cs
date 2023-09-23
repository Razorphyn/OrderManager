using CsvHelper.Configuration.Attributes;
using System;
using System.Windows.Forms;

namespace Razorphyn
{
    internal static class SupportClasses
    {
        internal class ComboBoxList
        {
            public string Name { get; set; }
            public long Value { get; set; }
            public string Descrizione { get; set; } = null;
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

        internal class OfferteCSV
        {
            [Index(0)]
            [Name("Numero Offerta")]
            public string NumOfferta { get; set; }

            [Index(1)]
            [Name("Cliente")]
            public string Cliente { get; set; }

            [Index(2)]
            [Name("Sede")]
            public string Sede { get; set; }

            [Index(3)]
            [Name("Data")]
            public string DataOfferta { get; set; }

            [Index(4)]
            [Name("Totale Offerta")]
            public string TotOfferta { get; set; }

            [Index(5)]
            [Name("Stato")]
            public string StatoOfferta { get; set; }

            [Index(6)]
            [Name("Converito in Ordine")]
            public string ConvOfferta { get; set; }

            [Index(7)]
            [Name("Ricambio")]
            public string PezzoOfferta { get; set; }

            [Index(8)]
            [Name("Codice Ricambio")]
            public string CodicePezzo { get; set; }

            [Index(9)]
            [Name("Macchina")]
            public string MacchinaOfferta { get; set; }

            [Index(10)]
            [Name("Quantità")]
            public string QtaOfferta { get; set; }

            [Index(11)]
            [Name("Prezzo Nell'Offerta")]
            public string PrezzoOfferta { get; set; }

            [Index(12)]
            [Name("Prezzo Finale")]
            public string PrezzoFinOfferta { get; set; }

            [Index(13)]
            [Name("Aggiunto ad Offerta")]
            public string PzzAggOfferta { get; set; }
        }

        internal class OrdiniCSV
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
            [Name("Sede")]
            public string Sede { get; set; }

            [Index(4)]
            [Name("Data Ordine")]
            public string DataOrdine { get; set; }

            [Index(5)]
            [Name("ETA Ordine")]
            public string ETAOrdine { get; set; }

            [Index(6)]
            [Name("Totale Ordine")]
            public string TotOrdine { get; set; }

            [Index(7)]
            [Name("Prezzo Finale Ordine")]
            public string TotFinOrdine { get; set; }

            [Index(8)]
            [Name("Sconto")]
            public string Sconto { get; set; }

            [Index(9)]
            [Name("Stato")]
            public string Stato { get; set; }

            [Index(10)]
            [Name("Ricambio")]
            public string Ricambio { get; set; }

            [Index(11)]
            [Name("Codice Ricambio")]
            public string CodRicambio { get; set; }

            [Index(12)]
            [Name("Prezzo Nell'Offerta")]
            public string PrezzoRicOrdine { get; set; }

            [Index(13)]
            [Name("Prezzo Finale")]
            public string PrezzoRicFinOrdine { get; set; }

            [Index(14)]
            [Name("Quantità")]
            public string QtaRicOrdine { get; set; }

            [Index(15)]
            [Name("ETA Ricambio")]
            public string ETARicambio { get; set; }
        }

    }
}
