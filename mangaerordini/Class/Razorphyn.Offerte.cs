using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using static Razorphyn.DataValidation;

namespace Razorphyn
{
    internal static class Offerte
    {
        public class Answer
        {
            public bool Success { get; set; } = false;
            public long LongValue { get; set; } = 0;
            public bool Bool { get; set; }
            public string Error { get; set; } = null;
        }

        internal static class GestioneOfferte
        {

            internal static Answer CreateOffer(DateTime dataoffValue, string numeroOff, long idsd, int stato, long idpref, decimal? prezzoSpedizione, int gestSP)
            {

                Answer esito = new();

                string commandText = @"INSERT INTO " + ProgramParameters.schemadb + @"[offerte_elenco]
                                (data_offerta, codice_offerta, ID_sede, ID_riferimento,stato, costo_spedizione, gestione_spedizione) 
                            VALUES 
                                (@data,@code,@idcl,@idref,@stato, @cossp, @gestsp);
                                SELECT id FROM " + ProgramParameters.schemadb + @"[offerte_elenco] WHERE codice_offerta=@code LIMIT 1;";


                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@data", dataoffValue);
                        cmd.Parameters.AddWithValue("@code", numeroOff);
                        cmd.Parameters.AddWithValue("@idcl", idsd);
                        cmd.Parameters.AddWithValue("@stato", stato);
                        if (idpref > 0)
                            cmd.Parameters.AddWithValue("@idref", idpref);
                        else
                            cmd.Parameters.AddWithValue("@idref", DBNull.Value);

                        if (prezzoSpedizione.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@cossp", prezzoSpedizione);
                            cmd.Parameters.AddWithValue("@gestsp", gestSP);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@cossp", DBNull.Value);
                            cmd.Parameters.AddWithValue("@gestsp", DBNull.Value);
                        }

                        esito.LongValue = Convert.ToInt64(cmd.ExecuteScalar());

                        string temp_info = "";
                        if (stato == 1)
                            temp_info = Environment.NewLine + "Nel caso, è necessario creare l'ordine associato all'offerta.";
                        OnTopMessage.Information("Offerta Creata." + temp_info);

                        esito.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                }

                return esito;
            }

            internal static Answer GetIfTransformed(string codice)
            {
                Answer answer = new Answer();

                string commandText = @"SELECT 
                                        Id AS id, 
                                        trasformato_ordine AS transf 
                                    FROM " + ProgramParameters.schemadb + @"[offerte_elenco] 
                                        WHERE codice_offerta = @codice
                                    LIMIT 1;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {

                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@codice", codice);
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader["transf"] == null || reader["id"] == DBNull.Value)
                            {
                                answer.LongValue = -1;
                                break;
                            }

                            answer.Bool = Convert.ToInt16(reader["transf"]) != 0;
                            answer.LongValue = Convert.ToInt64(reader["id"]);
                        }

                        reader.Close();
                        answer.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore check se offerta convertita. Codice: " + DbTools.ReturnErorrCode(ex));
                        answer.Success = false;
                    }
                }

                return answer;
            }

        }

        internal static class GestioneOggetti
        {
            internal static Answer AddObjToOffer(long idof, long idir, decimal? prezzoOrV, decimal? prezzoScV, int? qtaV, string nome = null)
            {
                Answer esito = new();

                string commandText = @" BEGIN TRANSACTION;
                                    INSERT OR ROLLBACK INTO " + ProgramParameters.schemadb + @"[offerte_pezzi]
                                        (ID_offerta, ID_ricambio, prezzo_unitario_originale, prezzo_unitario_sconto,pezzi) 
                                        VALUES (@idof,@idri,@por,@pos,@pezzi);
                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_elenco]
									    SET tot_offerta = ifnull( (SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP WHERE OP.ID_offerta=@idof) , 0) 
									    WHERE Id=@idof LIMIT 1;
                                    COMMIT;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@idof", idof);
                        cmd.Parameters.AddWithValue("@idri", idir);
                        cmd.Parameters.AddWithValue("@por", prezzoOrV);
                        cmd.Parameters.AddWithValue("@pos", prezzoScV);
                        cmd.Parameters.AddWithValue("@pezzi", qtaV);
                        cmd.ExecuteNonQuery();

                        esito.Success = true;

                        OnTopMessage.Information("Oggetto " + nome + " aggiunto all'offerta");
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante aggiunta al database. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                }

                return esito;
            }

            internal static Answer DeleteItemFromOffer(long id_offerta, long id_item_entry, SQLiteConnection connection = null)
            {
                Answer esito = new();
                connection ??= ProgramParameters.connection;

                string commandText = @"
                                            DELETE FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] WHERE Id=@id_item_entry LIMIT 1;

                                            UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_elenco]
                                                SET tot_offerta = ifnull((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP WHERE OP.ID_offerta=@id_offerta),0)
                                                WHERE Id=@id_offerta LIMIT 1;";

                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                using (SQLiteCommand cmd = new(commandText, connection, transaction))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@id_item_entry", id_item_entry);
                        cmd.Parameters.AddWithValue("@id_offerta", id_offerta);

                        cmd.ExecuteNonQuery();
                        transaction.Commit();

                        esito.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        transaction.Rollback();
                        OnTopMessage.Error("Errore durante eliminazione dell'ogetto. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                    finally { transaction.Dispose(); }
                }
                return esito;
            }

            internal static Answer UpdateItemFromOffer(long id_offerta, long id_item_entry, decimal prezzo, decimal prezzo_scontato, int quantita, SQLiteConnection connection = null)
            {
                Answer esito = new();
                connection ??= ProgramParameters.connection;

                string commandText = @"
                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_pezzi] 
                                        SET prezzo_unitario_originale=@por, prezzo_unitario_sconto=@pos,pezzi=@pezzi 
                                        WHERE Id = @idOggToOff LIMIT 1;

                                    UPDATE OR ROLLBACK " + ProgramParameters.schemadb + @"[offerte_elenco] 
									    SET tot_offerta = IFNULL((SELECT SUM(OP.pezzi * OP.prezzo_unitario_sconto) FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] AS OP WHERE OP.ID_offerta = @id_offerta),0)
									    WHERE Id = @id_offerta LIMIT 1;
                                    ";

                using (SQLiteCommand cmd = new(commandText, connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@por", prezzo);
                        cmd.Parameters.AddWithValue("@pos", prezzo_scontato);
                        cmd.Parameters.AddWithValue("@pezzi", quantita);
                        cmd.Parameters.AddWithValue("@idOggToOff", id_item_entry);
                        cmd.Parameters.AddWithValue("@id_offerta", id_offerta);
                        cmd.ExecuteNonQuery();

                        esito.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante aggiornamento oggetto nel database. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                }
                return esito;
            }

        }

        internal static class GetResources
        {

            internal static ValidationResult CollezioneIdRicambiOfferta(long id)
            {

                string commandText = @"SELECT
                                        ID_ricambio AS ID 
                                    FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] 
                                    WHERE Id_offerta = @id ;";

                ValidationResult answer = new();

                var dataSource = new List<long>();

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@id", id);


                        SQLiteDataReader reader = cmd.ExecuteReader();

                        answer.Success = true;

                        while (reader != null && reader.Read())
                            dataSource.Add(Convert.ToInt64(reader["ID"]));

                        answer.General = JsonConvert.SerializeObject(dataSource);
                    }
                    catch (SQLiteException ex)
                    {
                        answer.Success = false;
                        answer.Error = "Errore durante estrazione ID pezzi di ricmabio nell'offerta. Codice: " + DbTools.ReturnErorrCode(ex);
                    }
                }

                return answer;
            }

            internal static long GetOfferIdFromCodice(string codice)
            {
                long id = -1;

                string commandText = @"SELECT
                                    Id
                                    FROM " + ProgramParameters.schemadb + @"[offerte_elenco] 
                                    WHERE codice_offerta = @codice LIMIT 1 ;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@codice", codice);

                        SQLiteDataReader reader = cmd.ExecuteReader();

                        while (reader != null && reader.Read())
                        {
                            long.TryParse(Convert.ToString(reader["Id"]), out id);
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante recupero Id offerta da codice. Codice: " + DbTools.ReturnErorrCode(ex));
                    }
                }

                return id;
            }

            internal static ValidationResult GetContatto(long idOfferta)
            {

                string commandText = @"SELECT
                                        ID_riferimento AS ID 
                                    FROM " + ProgramParameters.schemadb + @"[offerte_elenco] 
                                    WHERE Id = @idOfferta;";

                ValidationResult answer = new();

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@idOfferta", idOfferta);

                        var reader = cmd.ExecuteScalar();

                        answer.Success = true;

                        answer.LongValue = (reader != DBNull.Value && reader != null) ? Convert.ToInt64(reader) : 0;

                    }
                    catch (SQLiteException ex)
                    {
                        answer.Success = false;
                        answer.Error = "Errore durante estrazione valore ID  da tabella offerte elenco. Codice: " + DbTools.ReturnErorrCode(ex);
                    }
                }

                return answer;
            }
        }

    }
}
