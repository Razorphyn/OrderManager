using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Razorphyn
{
    public static class DbTools
    {
        public static string ReturnErorrCode(SQLiteException ex)
        {
            Dictionary<int, string> er = new()
            {
                { 787, Environment.NewLine + "L'informazione che si sta provando ad eliminare è associata ad un elemento, eliminare prima l'elemento e poi riprovare." + Environment.NewLine + Environment.NewLine + "Esempio: se si sta provando ad eliminare un'offerta per la quale è stato creato un ordine, eliminare prima l'ordine e poi l'offerta." },
                { 2067, Environment.NewLine + "Esiste già un elemento nel database con le stesse uniche informazioni." }
            };

            if (er.ContainsKey(ex.ErrorCode))
                return er[ex.ErrorCode];
            else
                return ex.Message;
        }
    }
}