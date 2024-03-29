﻿using System;
using System.Data.SQLite;
using System.Windows.Forms;
using static Razorphyn.DataValidation;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace Razorphyn
{
    internal static class Ordini
    {
        public class Answer
        {
            public bool Success { get; set; } = false;
            public string Error { get; set; } = null;
            public long Id { get; set; } = 0;
        }

        internal static class GestioneOrdini
        {
            internal static Answer CreateOrder(string n_ordine, long id_offerta, bool OrderNotFromOffer, bool CopyItemFromOfferToOrder, long id_sede, long id_contatto, DateTime OrderDate, DateTime OrderETADate,
                                                Decimal tot_ordine, Decimal sconto, Decimal prezzo_finale, int stato_ordine, int gestSP, Decimal prezzoSpedizione)
            {
                string commandText = @"INSERT INTO " + ProgramParameters.schemadb + @"[ordini_elenco]
                            (codice_ordine, ID_offerta, ID_sede, ID_riferimento, data_ordine, data_ETA, totale_ordine,sconto,prezzo_finale,stato,costo_spedizione,gestione_spedizione)
						   VALUES (@codo, @idoof, @idsd, @idcont, @dataord, @dataeta, @totord, @sconto, @prezzoF, @stato, @cossp, @gestsp);
						   SELECT Id FROM " + ProgramParameters.schemadb + @"[ordini_elenco] WHERE codice_ordine = @codo LIMIT 1;";

                long lastinsertedid = 0;

                Answer answer = new Answer();

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@codo", n_ordine);
                        cmd.Parameters.AddWithValue("@idoof", (id_offerta > 0) ? id_offerta : DBNull.Value);
                        cmd.Parameters.AddWithValue("@idsd", id_sede);
                        cmd.Parameters.AddWithValue("@idcont", (id_contatto > 0) ? id_contatto : DBNull.Value);
                        cmd.Parameters.AddWithValue("@dataord", OrderDate);
                        cmd.Parameters.AddWithValue("@dataeta", OrderETADate);
                        cmd.Parameters.AddWithValue("@totord", tot_ordine);
                        cmd.Parameters.AddWithValue("@sconto", sconto);
                        cmd.Parameters.AddWithValue("@prezzoF", prezzo_finale);
                        cmd.Parameters.AddWithValue("@stato", stato_ordine);
                        if (gestSP > -1)
                        {
                            cmd.Parameters.AddWithValue("@cossp", prezzoSpedizione);
                            cmd.Parameters.AddWithValue("@gestsp", gestSP);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@cossp", DBNull.Value);
                            cmd.Parameters.AddWithValue("@gestsp", DBNull.Value);
                        }

                        lastinsertedid = Convert.ToInt64(cmd.ExecuteScalar());
                        answer.Success = true;
                        answer.Id = lastinsertedid;

                        if (OrderNotFromOffer == false)
                        {
                            commandText = "UPDATE " + ProgramParameters.schemadb + @"[offerte_elenco] SET trasformato_ordine=1 WHERE Id=@idoff LIMIT 1;";
                            using (SQLiteCommand cmd2 = new(commandText, ProgramParameters.connection))
                            {
                                try
                                {
                                    cmd2.CommandText = commandText;
                                    cmd2.Parameters.AddWithValue("@idoff", id_offerta);
                                    cmd2.ExecuteScalar();
                                }
                                catch (SQLiteException ex)
                                {
                                    OnTopMessage.Error("Errore durante aggiornamento offerta(convertito ordine update). Codice: " + DbTools.ReturnErorrCode(ex));
                                }
                            }

                            if (CopyItemFromOfferToOrder)
                            {
                                if (lastinsertedid > 0)
                                {
                                    commandText = @"SELECT * FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] WHERE ID_offerta=@idof;";
                                    using (SQLiteCommand cmd2 = new(commandText, ProgramParameters.connection))
                                    {
                                        try
                                        {
                                            cmd2.CommandText = commandText;
                                            cmd2.Parameters.AddWithValue("@idof", id_offerta);


                                            SQLiteDataReader reader = cmd2.ExecuteReader();
                                            string query;
                                            bool error_copi = false;
                                            while (reader.Read())
                                            {
                                                query = @" BEGIN TRANSACTION;
                                                    INSERT OR ROLLBACK INTO " + ProgramParameters.schemadb + @"[ordine_pezzi](ID_ordine,ID_ricambio,prezzo_unitario_originale,prezzo_unitario_sconto,pezzi,ETA) 
													    VALUES (@idord,@idogg,@prezor,@prezsco,@qta,@dataeta);

                                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_pezzi] SET pezzi_aggiunti = pezzi WHERE Id=@idoffogg LIMIT 1;
                                                    
                                                    COMMIT;";

                                                using (SQLiteCommand cmd3 = new(query, ProgramParameters.connection))
                                                {
                                                    try
                                                    {
                                                        cmd3.CommandText = query;
                                                        cmd3.Parameters.AddWithValue("@idord", lastinsertedid);
                                                        cmd3.Parameters.AddWithValue("@idogg", reader["ID_ricambio"]);
                                                        cmd3.Parameters.AddWithValue("@prezor", reader["prezzo_unitario_originale"]);
                                                        cmd3.Parameters.AddWithValue("@prezsco", reader["prezzo_unitario_sconto"]);
                                                        cmd3.Parameters.AddWithValue("@qta", reader["pezzi"]);
                                                        cmd3.Parameters.AddWithValue("@dataeta", OrderETADate);
                                                        cmd3.Parameters.AddWithValue("@idoffogg", reader["Id"]);

                                                        cmd3.ExecuteNonQuery();
                                                    }
                                                    catch (SQLiteException ex)
                                                    {
                                                        OnTopMessage.Error("Errore durante copia pezzi offerta in ordine(pt2). COntrollare manualmente l'ordine. Codice: " + DbTools.ReturnErorrCode(ex));
                                                        error_copi = true;
                                                    }
                                                }
                                            }
                                            reader.Close();
                                            if (error_copi == false)
                                            {
                                                OnTopMessage.Information("Oggetti copiati nell'ordine");
                                            }
                                        }
                                        catch (SQLiteException ex)
                                        {
                                            OnTopMessage.Error("Errore durante copia pezzi offerta in ordine(pt1). Codice: " + DbTools.ReturnErorrCode(ex));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
                    }

                    return answer;
                }
            }

            internal static void UpdateCalendarOnObj(long idordine, Outlook.Folder personalCalendar, SQLiteConnection connection = null)
            {
                string ordinecode = null;
                DateTime eta = DateTime.MinValue;

                connection ??= ProgramParameters.connection;

                string commandText = @"SELECT 
                                        codice_ordine,
                                        data_ETA
                                    FROM " + ProgramParameters.schemadb + @"[ordini_elenco] 
                                        WHERE Id=@idord LIMIT 1;";

                using (SQLiteCommand cmd2 = new(commandText, connection))
                {
                    try
                    {
                        cmd2.CommandText = commandText;
                        cmd2.Parameters.AddWithValue("@idord", idordine);

                        SQLiteDataReader reader = cmd2.ExecuteReader();
                        while (reader.Read())
                        {
                            ordinecode = (string)reader["codice_ordine"];
                            eta = (DateTime)reader["data_ETA"];
                        }
                        eta = eta.Date;
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante lettura dati ordine in fase aggiornamento dati calendario. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                }

                if (!String.IsNullOrEmpty(ordinecode) && CalendarManager.FindAppointment(personalCalendar, ordinecode, connection))
                {
                    DialogResult dialogResult = OnTopMessage.Question("Vuoi aggiornare l'evento sul calendario con le nuove informazioni?", "Aggiornare Evento Ordine Calendario");
                    if (dialogResult == DialogResult.Yes)
                    {
                        CalendarManager.UpdateCalendar(personalCalendar, ordinecode, ordinecode, idordine, eta, false, connection);
                    }
                }
            }
        }


        internal static class GestioneOggetti
        {

            internal static Answer AddObjToOrder(long id_ordine, long id_ricambio, DateTime eta_ordine, decimal prezzo_originale, decimal prezzo_scontato,
                                                      int qta, bool oggetto_non_in_offerta, bool applica_sconto, long idoggOff = 0)
            {
                Answer answer = new Answer();

                string commandText = @" BEGIN TRANSACTION;
                                        INSERT OR ROLLBACK INTO " + ProgramParameters.schemadb + @"[ordine_pezzi]
										(ID_ordine, ID_ricambio, prezzo_unitario_originale, prezzo_unitario_sconto,pezzi, ETA, Outside_Offer) 
										VALUES (@idord,@idri,@por,@pos,@pezzi,@eta,@Outside_Offer); 

									UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[ordini_elenco]
										SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine=@idord),0)
										WHERE Id=@idord LIMIT 1;
										
									UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[ordini_elenco] 
										SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine = @idord),0)
										WHERE Id = @idord LIMIT 1;
							";

                if (!oggetto_non_in_offerta)
                {
                    commandText += @" UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_pezzi] SET pezzi_aggiunti = pezzi_aggiunti + @pezzi WHERE Id=@idoggoff LIMIT 1;";
                }

                if (applica_sconto)
                {
                    commandText += @" UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[ordini_elenco] 
									SET prezzo_finale = IFNULL(totale_ordine*(1-sconto/100),0) 
									WHERE Id=@idord LIMIT 1;";
                }
                commandText += "COMMIT;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@idord", id_ordine);
                        cmd.Parameters.AddWithValue("@idri", id_ricambio);
                        cmd.Parameters.AddWithValue("@por", prezzo_originale);
                        cmd.Parameters.AddWithValue("@pos", prezzo_scontato);
                        cmd.Parameters.AddWithValue("@pezzi", qta);
                        cmd.Parameters.AddWithValue("@eta", eta_ordine);
                        cmd.Parameters.AddWithValue("@Outside_Offer", (oggetto_non_in_offerta == true) ? 1 : 0);
                        cmd.Parameters.AddWithValue("@idoggoff", idoggOff);

                        cmd.ExecuteNonQuery();

                        answer.Success = true;

                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante aggiunta oggetto ordine al database. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                }
                return answer;
            }
            internal static Answer DeleteObjFromOrder(long idordine, long id_entry_ricambio_ordine, bool updateFprice, bool updateFpriceSconto, SQLiteConnection connection = null)
            {
                Answer answer = new Answer();
                connection ??= ProgramParameters.connection;

                string commandText = @"
									UPDATE " + ProgramParameters.schemadb + @"[offerte_pezzi]
                                        SET 
                                            pezzi_aggiunti = pezzi_aggiunti - (SELECT 
                                                        OP.pezzi
						                            FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP 
                                                    INNER JOIN " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
											            ON OE.Id = OP.ID_ordine 
										            INNER JOIN " + ProgramParameters.schemadb + @" [offerte_pezzi] AS OFP 
											            ON OFP.ID_ricambio = OP.ID_ricambio AND OFP.ID_offerta=OE.ID_offerta
                                                    WHERE
                                                        OP.Id=@IdOggOrd
                                                    LIMIT 1)
                                        WHERE 
	                                        Id IN (
						                            SELECT 
                                                        OFP.Id
						                            FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP 
                                                    INNER JOIN " + ProgramParameters.schemadb + @"[ordini_elenco] AS OE 
											            ON OE.Id = OP.ID_ordine 
										            INNER JOIN " + ProgramParameters.schemadb + @" [offerte_pezzi] AS OFP 
											            ON OFP.ID_ricambio = OP.ID_ricambio AND OFP.ID_offerta=OE.ID_offerta
                                                    WHERE
                                                        OP.Id=@IdOggOrd
                                                    LIMIT 1
					                            )
                                        LIMIT 1;

                                    DELETE FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] WHERE Id=@IdOggOrd LIMIT 1;

                                    UPDATE " + ProgramParameters.schemadb + @"[ordini_elenco]
                                        SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine = @idord),0)
                                        WHERE Id = @idord 
                                        LIMIT 1; 
                                    ";

                if (updateFprice)
                {
                    commandText += @"UPDATE " + ProgramParameters.schemadb + @"[ordini_elenco]
												SET 
                                                    prezzo_finale = " + ((updateFpriceSconto) ? " (totale_ordine*(1-sconto/100)) " : " totale_ordine ") + @"
												WHERE Id = @idord
                                                LIMIT 1;";
                }

                using (var transaction = connection.BeginTransaction())
                using (SQLiteCommand cmd = new(commandText, connection, transaction))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@IdOggOrd", id_entry_ricambio_ordine);
                        cmd.Parameters.AddWithValue("@idord", idordine);
                        cmd.ExecuteNonQuery();

                        transaction.Commit();
                        answer.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        transaction.Rollback();
                        OnTopMessage.Error("Errore durante upate tot ordine (aggiornamento stato oggetto offerta). Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                    return answer;
                }
            }

            internal static Answer UpdateItemFromOrder(long idordine, long id_entry_ricambio_ordine, decimal prezzo, decimal prezzo_sconto, int quantita, DateTime ETA, bool applyDiscount, SQLiteConnection connection = null)
            {

                Answer answer = new Answer();
                connection ??= ProgramParameters.connection;

                string commandText = @" BEGIN TRANSACTION;
                                    
                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_pezzi]
									    SET
										    pezzi_aggiunti = pezzi_aggiunti - (SELECT pezzi FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] WHERE Id=@idoggOrd ) + @pezzi
									    WHERE
										    ID_ricambio = (SELECT ID_ricambio FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] WHERE Id=@idoggOrd )
                                            AND  ID_offerta = (SELECT 
                                                                    OE.ID_offerta 
                                                                FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP 
                                                                    JOIN [ordini_elenco] AS OE
                                                                        ON OE.Id = OP.ID_ordine 
                                                                WHERE OP.Id=@idoggOrd 
                                                                )
									    LIMIT 1;

                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[ordine_pezzi]
									    SET
										    prezzo_unitario_originale=@por, prezzo_unitario_sconto=@pos,pezzi=@pezzi, ETA=@eta
									    WHERE
										    Id=@idoggOrd
									    LIMIT 1;

                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[ordini_elenco]
									    SET totale_ordine = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP WHERE OP.ID_ordine = @idord),0)
									    WHERE Id = @idord LIMIT 1;
                                    ";

                if (applyDiscount)
                {
                    commandText += @"UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[ordini_elenco] 
									SET prezzo_finale = IFNULL(totale_ordine*(1-sconto/100),0)
									WHERE Id=@idord LIMIT 1;";
                }
                commandText += "COMMIT;";

                using (SQLiteCommand cmd = new(commandText, connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;

                        cmd.Parameters.AddWithValue("@idord", idordine);
                        cmd.Parameters.AddWithValue("@idoggOrd", id_entry_ricambio_ordine);
                        cmd.Parameters.AddWithValue("@por", prezzo);
                        cmd.Parameters.AddWithValue("@pos", prezzo_sconto);
                        cmd.Parameters.AddWithValue("@pezzi", quantita);
                        cmd.Parameters.AddWithValue("@eta", ETA);

                        cmd.ExecuteNonQuery();

                        answer.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante aggiornamento oggetto. Codice: " + DbTools.ReturnErorrCode(ex));
                    }

                    return answer;
                }
            }

        }

        internal static class GetResources
        {
            internal static ValidationResult GetContatto(long idOrdine)
            {

                string commandText = @"SELECT
                                        ID_riferimento AS ID 
                                    FROM " + ProgramParameters.schemadb + @"[ordini_elenco] 
                                    WHERE Id = @idOrdine LIMIT 1;";

                ValidationResult answer = new();

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@idOrdine", idOrdine);

                        var reader = cmd.ExecuteScalar();

                        answer.Success = true;

                        answer.LongValue = (reader != DBNull.Value && reader != null) ? Convert.ToInt64(reader) : 0;

                    }
                    catch (SQLiteException ex)
                    {
                        answer.Success = false;
                        answer.Error = "Errore durante estrazione valore ID  da tabella ordini elenco. Codice: " + DbTools.ReturnErorrCode(ex);
                    }
                }

                return answer;
            }
        }
    }
}
