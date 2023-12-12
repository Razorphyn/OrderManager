using System.Data;
using System.Data.SQLite;

namespace Razorphyn
{
    internal class Ricambi
    {
        public class Answer
        {
            public bool Success { get; set; } = false;

        }

        internal static class GetResources
        {

            internal static (Answer, DataTable) GetCollection(int page, int recordsPerPage, string addInfo = null, string codiceRicambioFilter = null, SQLiteConnection connection = null)
            {
                DataTable ds = new();
                Answer answer = new Answer();
                string limitResults = "";
                int startingrecord = page * recordsPerPage;

                if (recordsPerPage > 0)
                    limitResults = " LIMIT @recordperpage OFFSET @startingrecord ";


                connection ??= ProgramParameters.connection;

                string commandText = @"SELECT 
									PR.Id AS ID,
									IIF(CM.Id IS NULL, 
                                        '',
										(CM.Id || ' - ' || CM.modello  || ' (' ||  CM.seriale || ')')
										) AS Macchina,
									IIF(F.Id IS NULL,
                                        '',
										(F.Id || ' - ' || F.nome)
										) AS Fornitore,
									 PR.nome AS Nome,
									 PR.codice AS Codice,
                                    REPLACE(printf('%.2f', PR.prezzo),'.',',')  AS Prezzo
                                    FROM " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_macchine] AS CM
                                        ON CM.Id =  PR.ID_macchina
                                    LEFT JOIN " + ProgramParameters.schemadb + @"[fornitori] AS F
									    ON F.Id =  PR.ID_fornitore 
                                    WHERE PR.deleted = 0 " + addInfo + @" ORDER BY  PR.Id ASC " + limitResults + ";";


                using (SQLiteDataAdapter cmd = new(commandText, connection))
                {
                    try
                    {
                        cmd.SelectCommand.Parameters.AddWithValue("@startingrecord", startingrecord);
                        cmd.SelectCommand.Parameters.AddWithValue("@recordperpage", recordsPerPage);
                        cmd.SelectCommand.Parameters.AddWithValue("@codiceRicambioFilter", "%" + codiceRicambioFilter + "%");

                        cmd.Fill(ds);
                        answer.Success = true;
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore durante popolamento tabella Componenti. Codice: " + DbTools.ReturnErorrCode(ex));
                    }

                    return (answer, ds);
                }
            }
        }
    }
}
