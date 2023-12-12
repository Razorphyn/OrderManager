using System;
using System.Data.SQLite;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Razorphyn
{

    public static class DataValidation
    {


        public class ValidationResult
        {
            public bool Success { get; set; } = false;
            public bool BoolValue { get; set; } = false;
            public decimal DecimalValue { get; set; }
            public long LongValue { get; set; }
            public int IntValue { get; set; }
            public string Error { get; set; } = null;
            public DateTime DateValue { get; set; } = DateTime.MinValue;
            public string General { get; set; } = null;
        }

        public static ValidationResult ValidateInt(string id)
        {
            ValidationResult answer = new();

            if (!int.TryParse(id, out int idV))
            {
                answer.Error = "Il valore deve essere intero." + Environment.NewLine;
            }
            else
            {
                answer.IntValue = idV;
            }

            return answer;
        }

        public static ValidationResult ValidateId(string id)
        {
            ValidationResult answer = new();

            if (!Int64.TryParse(id, out long idV))
            {
                answer.Error = "ID non valido o vuoto" + Environment.NewLine;
            }
            else
            {
                answer.LongValue = idV;
                answer.Success = true;
            }

            return answer;
        }

        public static ValidationResult ValidateCliente(long idcl)
        {
            ValidationResult answer = new();

            if (idcl < 0)
            {
                answer.Success = true;
                answer.BoolValue = false;
                answer.Error = "Selezionare cliente dalla lista." + Environment.NewLine;

                return answer;
            }

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_elenco] WHERE ([Id] = @user) LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", idcl);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.LongValue = idcl;
                    answer.Success = true;
                }
                catch (SQLiteException ex)
                {
                    //DbTools DbTools = new DbTools();
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Cliente. Codice: " + DbTools.ReturnErorrCode(ex);
                    return answer;
                }

                if (answer.IntValue < 1)
                {
                    answer.BoolValue = false;
                    answer.Error = "Cliente non valido o vuoto" + Environment.NewLine;
                }
                else
                {
                    answer.BoolValue = true;
                }

                return answer;
            }
        }

        public static ValidationResult ValidateSede(long id_cliente, long id_sede)
        {
            ValidationResult answer = new();

            if (id_sede < 0)
            {
                answer.Success = true;
                answer.BoolValue = false;
                answer.Error = "Selezionare sede dalla lista." + Environment.NewLine;

                return answer;
            }

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_sedi] WHERE ([Id] = @idsd AND ID_cliente=@idcl ) LIMIT 1;";

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idcl", id_cliente);
                    cmd.Parameters.AddWithValue("@idsd", id_sede);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;
                }
                catch (SQLiteException ex)
                {
                    //DbTools DbTools = new DbTools();
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Sede. Codice: " + DbTools.ReturnErorrCode(ex);
                    return answer;
                }

                if (answer.IntValue < 1)
                {
                    answer.BoolValue = false;
                    answer.Error = "Sede non valida o vuota" + Environment.NewLine;
                }
                else
                {
                    answer.BoolValue = true;
                }

                return answer;
            }
        }

        public static string ValidateCodiceRicambio(string codice)
        {
            Regex rgx = new(@"^[a-zA-Z]{1,}\d{1,}[-]\d{1,}$");

            if (string.IsNullOrEmpty(codice) || !rgx.IsMatch(codice))
            {
                return "Codice non valido o vuoto" + Environment.NewLine;
            }

            return "";
        }

        public static ValidationResult ValidatePrezzo(string prezzo)
        {
            ValidationResult answer = new()
            {
                Success = Decimal.TryParse(prezzo, ProgramParameters.style, ProgramParameters.culture, out decimal prezzoD)
            };

            if (!answer.Success)
            {
                answer.Error = "Prezzo non valido(##,##) o vuoto" + Environment.NewLine;
                return answer;
            }
            if (prezzoD < 0)
            {
                answer.Error = "Il prezzo deve essere positivo" + Environment.NewLine;
                return answer;
            }

            answer.DecimalValue = prezzoD;
            answer.Success = true;

            return answer;
        }

        public static ValidationResult ValidateSconto(string sconto)
        {
            ValidationResult answer = new()
            {
                Success = true
            };

            if (!Decimal.TryParse(sconto, ProgramParameters.style, ProgramParameters.culture, out decimal scontoV) || !Regex.IsMatch(sconto, @"^[\d,.]+$"))
            {
                answer.Success = false;
            }

            if (!answer.Success)
            {
                answer.Error = "Sconto non valido(##,##) o vuoto" + Environment.NewLine;
                return answer;
            }
            else if (scontoV < 0 || scontoV > 100)
            {
                answer.Error = "Lo Sconto deve essere compreso tra 0 e 100. " + Environment.NewLine;
                return answer;
            }
            else
            {
                answer.DecimalValue = scontoV;
            }

            return answer;
        }

        public static ValidationResult ValidateFornitore(long id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[fornitori] WHERE ([Id] = @user) LIMIT 1";

            ValidationResult answer = new();

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", id);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;
                }
                catch (SQLiteException ex)
                {
                    //DbTools DbTools = new DbTools();
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Fornitore. Codice: " + DbTools.ReturnErorrCode(ex);
                    return answer;
                }
            }

            if (answer.IntValue < 1)
            {
                answer.BoolValue = false;
                answer.Error = "Fornitore non presente nel database" + Environment.NewLine;
            }
            else
            {
                answer.BoolValue = true;
            }

            return answer;
        }

        public static ValidationResult ValidateMacchina(long id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_macchine] WHERE ([Id] = @user) LIMIT 1;";
            ValidationResult answer = new()
            {
                Success = true
            };

            if (id > 0)
            {
                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@user", id);

                        answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch (SQLiteException ex)
                    {
                        //DbTools DbTools = new DbTools();
                        answer.Error = "Errore durante verifica ID Macchina. Codice: " + DbTools.ReturnErorrCode(ex);
                        answer.Success = false;

                        return answer;
                    }
                }
                if (answer.IntValue < 1)
                {
                    answer.BoolValue = false;
                    answer.Error = "Macchina non presente nel database" + Environment.NewLine;
                }
                else
                {
                    answer.BoolValue = true;
                }
                return answer;
            }

            return answer;
        }

        public static ValidationResult ValidatePRef(long id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_riferimenti] WHERE ([Id] = @user) LIMIT 1;";

            ValidationResult answer = new();

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", id);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;

                    if (answer.IntValue < 1)
                    {
                        answer.BoolValue = false;
                        answer.Error = "Persona di riferimento non valida o vuota." + Environment.NewLine;
                    }
                    else
                    {
                        answer.BoolValue = true;
                    }
                }
                catch (SQLiteException ex)
                {
                    //DbTools DbTools = new DbTools();
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Persona Riferiemnto. Codice: " + DbTools.ReturnErorrCode(ex);
                }
            }

            return answer;
        }

        public static ValidationResult ValidateSpedizione(string spedizioni, int gestSP)
        {
            ValidationResult answer = new();

            if (!Decimal.TryParse(spedizioni, ProgramParameters.style, ProgramParameters.culture, out decimal prezzo))
            {
                answer.Error += "Prezzo spedizione non valido(##,##) o vuoto" + Environment.NewLine;
            }
            else
            {
                if (prezzo < 0)
                {
                    answer.Error += "Il prezzo spedizione deve essere positivo" + Environment.NewLine;
                }
                else
                {
                    answer.DecimalValue = prezzo;
                }

                if (gestSP < 0 && prezzo > 0)
                {
                    answer.Error += "Selezionare opzione per la gestione del costo della spedizione o mettere costo a zero." + Environment.NewLine;
                }
            }

            return answer;
        }

        public static ValidationResult ValidateDate(string stringDate)
        {
            ValidationResult answer = new();

            if (!DateTime.TryParseExact(stringDate, ProgramParameters.dateFormat, ProgramParameters.provider, DateTimeStyles.None, out DateTime dataOrdValue))
            {
                answer.Error += "Valore: " + stringDate + ". Data non valida o vuota" + Environment.NewLine;
            }
            else
            {
                answer.DateValue = dataOrdValue;
                answer.Success = true;
            }

            return answer;
        }

        public static ValidationResult ValidateDateTime(string stringDate)
        {
            ValidationResult answer = new();

            if (!DateTime.TryParseExact(stringDate, ProgramParameters.dateFormatTime, ProgramParameters.provider, DateTimeStyles.None, out DateTime dataOrdValue))
            {
                answer.Error += "Valore: " + stringDate + ". Data non valida o vuota" + Environment.NewLine;
            }
            else
            {
                answer.DateValue = dataOrdValue;
                answer.Success = true;
            }

            return answer;
        }

        public static ValidationResult ValidateQta(string qta, bool IsGreaterThanZero = true)
        {
            ValidationResult answer = new();

            if (!int.TryParse(qta, out int qtaV))
            {
                answer.Error += "Quantità non valida o vuota." + Environment.NewLine;
            }
            else
            {
                if (IsGreaterThanZero && qtaV < 1)
                    answer.Error += "La quanità deve essere positiva, intera e maggiore di 0." + Environment.NewLine;
                else if (!IsGreaterThanZero && qtaV < 0)
                    answer.Error += "La quanità deve essere positiva e intera." + Environment.NewLine;
                else
                    answer.Success = true;
            }

            answer.IntValue = qtaV;

            return answer;
        }

        public static ValidationResult ValidateName(string nome, string sub = "\b")
        {
            ValidationResult answer = new();

            if (string.IsNullOrEmpty(nome))
            {
                answer.Error += "Nome " + sub + " non valido o vuoto" + Environment.NewLine;
            }

            return answer;
        }

        public static ValidationResult ValidateIdOffertaFormato(string IdOfferta)
        {
            ValidationResult answer = new();

            if (string.IsNullOrEmpty(IdOfferta) || !Regex.IsMatch(IdOfferta, @"^\d+$"))
            {
                answer.Error = "Numero Offerta non valido o vuoto." + Environment.NewLine;
            }
            else
            {
                answer.Error += ValidateIdOffertaUnica(IdOfferta).Error;
            }

            return answer;
        }

        public static ValidationResult ValidateIdOffertaUnica(string id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[offerte_elenco] WHERE ([codice_offerta] = @codice) LIMIT 1;";

            ValidationResult answer = new();

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@codice", id);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;

                    if (answer.IntValue < 1)
                    {
                        answer.BoolValue = true;
                    }
                    else
                    {
                        answer.BoolValue = false;
                        answer.Error = "Codice offerta già esistente." + Environment.NewLine;
                    }
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante verifica Codice Offerta. Codice: " + DbTools.ReturnErorrCode(ex);
                }
            }

            return answer;
        }

        public static ValidationResult ValidateIdOrdineUnico(string id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[ordini_elenco] WHERE ([codice_ordine] = @codice) LIMIT 1;";

            ValidationResult answer = new();

            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@codice", id);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;

                    if (answer.IntValue < 1)
                    {
                        answer.BoolValue = true;
                    }
                    else
                    {
                        answer.BoolValue = false;
                        answer.Error = "Codice ordine già esistente." + Environment.NewLine;
                    }
                }
                catch (SQLiteException ex)
                {
                    answer.Success = false;
                    answer.Error = "Errore durante verifica Codice Ordine. Codice: " + DbTools.ReturnErorrCode(ex);
                }
            }

            return answer;
        }
    }
}