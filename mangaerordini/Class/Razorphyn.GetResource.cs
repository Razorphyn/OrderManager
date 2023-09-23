using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using static Razorphyn.DataValidation;
using static Razorphyn.SupportClasses;

namespace Razorphyn
{
    internal static class GetResource
    {
        internal static ValidationResult CollezioneCodiceRicambio(string codice, long cliente = 0)
        {
            string cond = "";
            if (cliente > 0)
                cond += " AND (pr.ID_macchina IN (SELECT Id  FROM " + ProgramParameters.schemadb + @"[clienti_macchine] WHERE ID_cliente=@cliente) OR pr.ID_macchina IS NULL)";

            string commandText = @"SELECT
                                    pr.Id AS ID, 
                                    pr.nome AS nome, 
                                    pr.descrizione AS descrizione, 
                                    IIF(cm.seriale  IS NULL, CHAR(42),cm.seriale) AS seriale
                                        FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS pr
                                            LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine]  AS cm
                                                ON pr.ID_macchina= cm.Id
                                        WHERE pr.codice = @codice " + cond + " ;";

            ValidationResult answer = new();

            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@codice", codice);
                    cmd.Parameters.AddWithValue("@cliente", cliente);


                    SQLiteDataReader reader = cmd.ExecuteReader();

                    answer.Success = true;

                    while (reader != null && reader.Read())
                        dataSource.Add(new ComboBoxList()
                        {
                            Name = string.Format("{0} - {1} ({2})", reader["ID"], reader["nome"], reader["seriale"]),
                            Value = Convert.ToInt64(reader["ID"]),
                            Descrizione = Convert.ToString(reader["nome"] + Environment.NewLine + reader["descrizione"])
                        });

                    answer.IntValue = dataSource.Count - 1;

                    if (dataSource.Count == 1)
                    {
                        dataSource.Add(new ComboBoxList() { Name = "Ricambio " + codice + " non trovato.", Value = -1 });
                    }

                    answer.General = JsonConvert.SerializeObject(dataSource);
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante estrazione codici prodotti. Codice: " + DbTools.ReturnErorrCode(ex);
                }
            }

            return answer;
        }

        internal static (long, long) GetClientIdFromNumero(string numero)
        {
            long idsd = -1;
            long idcl = -1;

            string commandText = @"SELECT
                                    Id,
                                    ID_Cliente
                                    FROM " + ProgramParameters.schemadb + @"[clienti_sedi] 
                                    WHERE numero = @numero LIMIT 1 ;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@numero", numero);

                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader != null && reader.Read())
                    {
                        long.TryParse(Convert.ToString(reader["Id"]), out idsd);
                        long.TryParse(Convert.ToString(reader["ID_Cliente"]), out idcl);
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante estrazione codici prodotti. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }

            return (idcl, idsd);
        }

        internal static ValidationResult GetIdRicambioInOffferta(long idOfferta, long idRicambio)
        {

            string commandText = @"SELECT
                                        Id AS ID 
                                    FROM " + ProgramParameters.schemadb + @"[offerte_pezzi] 
                                    WHERE ID_offerta = @idOfferta AND ID_ricambio = @idRicambio;";

            ValidationResult answer = new();

            var dataSource = new List<long>();

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idOfferta", idOfferta);
                    cmd.Parameters.AddWithValue("@idRicambio", idRicambio);


                    object reader = cmd.ExecuteScalar();

                    answer.Success = true;

                    answer.LongValue = (reader != null) ? Convert.ToInt64(reader) : 0;

                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante estrazione valore ID  da tabella offerte ricambi. Codice: " + DbTools.ReturnErorrCode(ex);
                }
            }

            return answer;
        }
    }


}
