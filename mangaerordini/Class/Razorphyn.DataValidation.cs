using System;
using System.Data.SQLite;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Razorphyn
{

    public class DataValidation
    {
        public DataValidation() { }

        public class ValidationResult
        {
            public bool Success { get; set; } = false;
            public bool BoolValue { get; set; } = false;
            public decimal? DecimalValue { get; set; } = null;
            public int? IntValue { get; set; } = null;
            public string Error { get; set; } = null;
            public DateTime DateValue { get; set; } = DateTime.MinValue;
        }

        public ValidationResult ValidateId(string id)
        {
            ValidationResult answer = new ValidationResult();

            if (!int.TryParse(id, out int idV))
            {
                answer.Error = "ID non valido o vuoto" + Environment.NewLine;
            }
            else
            {
                answer.IntValue = idV;
            }

            return answer;
        }

        public ValidationResult ValidateCliente(int idcl)
        {
            ValidationResult answer = new ValidationResult();

            if (idcl < 0)
            {
                answer.Success = true;
                answer.BoolValue = false;
                answer.Error = "Selezionare cliente dalla lista." + Environment.NewLine;

                return answer;
            }

            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_elenco] WHERE ([Id] = @user) LIMIT 1;";

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@user", idcl);

                    answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    answer.Success = true;
                }
                catch (SQLiteException ex)
                {
                    DbTools DbTools = new DbTools();
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

        public string ValidateCodiceRicambio(string codice)
        {
            Regex rgx = new Regex(@"^[a-zA-Z]{1}\d{1,}[-]\d{1,}$");

            if (string.IsNullOrEmpty(codice) || !rgx.IsMatch(codice))
            {
                return "Codice non valido o vuoto" + Environment.NewLine;
            }

            return "";
        }

        public ValidationResult ValidatePrezzo(string prezzo)
        {
            ValidationResult answer = new ValidationResult
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
            return answer;
        }

        public ValidationResult ValidateSconto(string sconto)
        {
            ValidationResult answer = new ValidationResult
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

        public ValidationResult ValidateFornitore(int id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[fornitori] WHERE ([Id] = @user) LIMIT 1";

            ValidationResult answer = new ValidationResult();

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
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
                    DbTools DbTools = new DbTools();
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

        public ValidationResult ValidateMacchina(int id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_macchine] WHERE ([Id] = @user) LIMIT 1;";
            ValidationResult answer = new ValidationResult
            {
                Success = true
            };

            if (id > 0)
            {
                using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
                {
                    try
                    {
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@user", id);

                        answer.IntValue = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch (SQLiteException ex)
                    {
                        DbTools DbTools = new DbTools();
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

        public ValidationResult ValidatePRef(int id)
        {
            string commandText = "SELECT COUNT(*) FROM " + ProgramParameters.schemadb + @"[clienti_riferimenti] WHERE ([Id] = @user) LIMIT 1;";

            ValidationResult answer = new ValidationResult();

            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
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
                    DbTools DbTools = new DbTools();
                    answer.Success = false;
                    answer.Error = "Errore durante verifica ID Persona Riferiemnto. Codice: " + DbTools.ReturnErorrCode(ex);
                }
            }

            return answer;
        }

        public ValidationResult ValidateSpedizione(string spedizioni, int gestSP)
        {
            ValidationResult answer = new ValidationResult();

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
            }

            if (gestSP < 0)
            {
                answer.Error += "Selezionare opzione per la gestione del costo della spedizione" + Environment.NewLine;
            }

            return answer;
        }

        public ValidationResult ValidateDate(string stringDate)
        {
            ValidationResult answer = new ValidationResult();

            if (!DateTime.TryParseExact(stringDate, ProgramParameters.dateFormat, ProgramParameters.provider, DateTimeStyles.None, out DateTime dataOrdValue))
            {
                answer.Error += "Valore: " + stringDate + ". Data non valida o vuota" + Environment.NewLine;
            }
            else
            {
                answer.DateValue = dataOrdValue;
            }

            return answer;
        }

        public ValidationResult ValidateDateTime(string stringDate)
        {
            ValidationResult answer = new ValidationResult();

            if (!DateTime.TryParseExact(stringDate, ProgramParameters.dateFormatTime, ProgramParameters.provider, DateTimeStyles.None, out DateTime dataOrdValue))
            {
                answer.Error += "Valore: " + stringDate + ". Data non valida o vuota" + Environment.NewLine;
            }
            else
            {
                answer.DateValue = dataOrdValue;
            }

            return answer;
        }

        public ValidationResult ValidateQta(string qta)
        {
            ValidationResult answer = new ValidationResult();

            if (!int.TryParse(qta, out int qtaV))
            {
                answer.Error += "Quantità non valida o vuota." + Environment.NewLine;
            }
            else
            {
                if (qtaV < 1)
                    answer.Error += "La quanità deve essere positiva, intera e maggiore di 0." + Environment.NewLine;
            }

            answer.IntValue = qtaV;

            return answer;
        }

        public ValidationResult ValidateName(string nome, string sub = "\b")
        {
            ValidationResult answer = new ValidationResult();

            if (string.IsNullOrEmpty(nome))
            {
                answer.Error += "Nome " + sub + " non valido o vuoto" + Environment.NewLine;
            }

            return answer;
        }

    }
}