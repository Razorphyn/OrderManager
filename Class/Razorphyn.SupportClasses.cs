using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace OrderManager.Class
{
    internal static class SupportClasses
    {
        internal class ComboBoxList
        {
            public string Name { get; set; }
            public long Value { get; set; }
            public string Descrizione { get; set; } = null;
        }

        internal class Word
        {
            public string Value { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            public int X2 { get; set; }
            public int Y2 { get; set; }
        }

        public class DbCallResult
        {
            public bool Success { get; set; } = false;
            public int? IntValue { get; set; } = 0;
            public decimal? DecimalValue { get; set; } = 0;
        }

        internal class U10_Ricambio
        {
            public bool Duplicate { get; set; } = false;

            public bool Delete { get; set; } = false;

            public long Id_ricambio { get; set; }

            public long Id_db_entry { get; set; }

            public string Codice { get; set; }

            public string Nome { get; set; }

            public int Qta { get; set; }

            public decimal Prezzo { get; set; }

            public decimal Prezzo_Sconto { get; set; }

            public DateTime ETA { get; set; }

            public long Id_Sostituto { get; set; }

            public string Nuovo_Codice { get; set; }

            public int Nuovo_Qta { get; set; }

            public decimal Nuovo_Prezzo { get; set; }

            public decimal Nuovo_Prezzo_Sconto { get; set; }

            public DateTime Nuovo_ETA { get; set; }
        }

        internal class U10_Offerta_Ricambio : U10_Ricambio
        {
            internal static List<U10_Ricambio> Offerta_GetItemCollection(long id_offerta, List<string> list, SQLiteConnection temp_connection)
            {
                List<U10_Ricambio> itemsOffer = new List<U10_Ricambio>();
                string commandText = @"SELECT 
                                                    OP.[Id] as ID_offerte_pezzi,
	                                                PR.[id] AS ID_ricambio,
	                                                PR.[codice] AS Codice,
	                                                PR.[nome] AS Nome,
                                                    OP.[pezzi] AS qta,
                                                    OP.[prezzo_unitario_originale] AS prezzo,
                                                    OP.[prezzo_unitario_sconto] AS prezzo_sconto,
                                                    IIF( PR.[codice] IN (@codici_ricambio), true, false) AS Duplicate

	                                                FROM  " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
	                                                LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP
		                                                ON OP.ID_offerta = OE.Id 
	                                                LEFT JOIN  " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
		                                                ON PR.Id = OP.ID_ricambio

	                                                WHERE OE.Id = @id_offerta;";

                using (SQLiteCommand cmd_items = new(commandText, temp_connection))
                {
                    string idlist = string.Join("\",\"", list);

                    cmd_items.Parameters.AddWithValue("@id_offerta", id_offerta);
                    cmd_items.Parameters.AddWithValue("@codici_ricambio", idlist);
                    try
                    {
                        using (SQLiteDataReader reader = cmd_items.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                itemsOffer.Add(new U10_Ricambio()
                                {
                                    Id_db_entry = Convert.ToInt64(reader["ID_offerte_pezzi"]),
                                    Id_ricambio = Convert.ToInt64(reader["ID_ricambio"]),
                                    Nome = Convert.ToString(reader["Nome"]),
                                    Codice = Convert.ToString(reader["Codice"]),
                                    Qta = Convert.ToInt32(reader["qta"]),
                                    Prezzo = Convert.ToDecimal(reader["prezzo"]),
                                    Prezzo_Sconto = Convert.ToDecimal(reader["prezzo_sconto"]),

                                    Nuovo_Codice = Convert.ToString(reader["Codice"]),
                                    Nuovo_Qta = Convert.ToInt32(reader["qta"]),
                                    Nuovo_Prezzo = Convert.ToDecimal(reader["prezzo"]),
                                    Nuovo_Prezzo_Sconto = Convert.ToDecimal(reader["prezzo_sconto"]),

                                    Duplicate = Convert.ToBoolean(reader["Duplicate"])
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnTopMessage.Error("UPDATE 10: Errore durante collezioanemnto oggetti. Codice: " + ex.Message);
                    }
                }

                return itemsOffer;
            }
        }

        internal class U10_Ordine_Ricambio : U10_Ricambio
        {
            internal static List<U10_Ricambio> Ordine_GetItemCollection(long id_ordine, List<string> list, SQLiteConnection temp_connection)
            {
                List<U10_Ricambio> itemsOffer = new List<U10_Ricambio>();
                string commandText = @"SELECT 
	                                                OP.[Id] AS ID_ordine_pezzi,
	                                                PR.[Id] AS ID_ricambio,
	                                                PR.[codice] AS Codice,
	                                                PR.[nome] AS Nome,
                                                    OP.[pezzi] AS qta,
                                                    OP.[prezzo_unitario_originale] AS prezzo,
                                                    OP.[prezzo_unitario_sconto] AS prezzo_sconto,
                                                    OP.[ETA] AS ETA,
                                                    IIF( PR.[codice] IN (@codici_ricambio), true, false) AS Duplicate

	                                                FROM  " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE
	                                                LEFT JOIN " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP
		                                                ON OP.ID_ordine = OE.Id 
	                                                LEFT JOIN  " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
		                                                ON PR.Id = OP.ID_ricambio

	                                                WHERE OE.Id = @id_ordine;";

                using (SQLiteCommand cmd_items = new(commandText, temp_connection))
                {
                    string idlist = string.Join("\",\"", list);

                    cmd_items.Parameters.AddWithValue("@id_ordine", id_ordine);
                    cmd_items.Parameters.AddWithValue("@codici_ricambio", idlist);
                    try
                    {
                        using (SQLiteDataReader reader = cmd_items.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                itemsOffer.Add(new U10_Ricambio()
                                {
                                    Id_ricambio = Convert.ToInt64(reader["ID_ricambio"]),
                                    Id_db_entry = Convert.ToInt64(reader["ID_ordine_pezzi"]),
                                    Nome = Convert.ToString(reader["Nome"]),
                                    Codice = Convert.ToString(reader["Codice"]),
                                    Qta = Convert.ToInt32(reader["qta"]),
                                    Prezzo = Convert.ToDecimal(reader["prezzo"]),
                                    Prezzo_Sconto = Convert.ToDecimal(reader["prezzo_sconto"]),
                                    ETA = Convert.ToDateTime(reader["ETA"]),

                                    Nuovo_Codice = Convert.ToString(reader["Codice"]),
                                    Nuovo_Qta = Convert.ToInt32(reader["qta"]),
                                    Nuovo_Prezzo = Convert.ToDecimal(reader["prezzo"]),
                                    Nuovo_Prezzo_Sconto = Convert.ToDecimal(reader["prezzo_sconto"]),
                                    Nuovo_ETA = Convert.ToDateTime(reader["ETA"]),

                                    Duplicate = Convert.ToBoolean(reader["Duplicate"])
                                });
                            }
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("UPDATE 10: Errore durante selezione collezione oggetti ordine. Codice: " + ex.Message);
                    }
                }

                return itemsOffer;
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
