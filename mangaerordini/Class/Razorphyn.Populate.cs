using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;
using static Razorphyn.SupportClasses;

namespace Razorphyn
{

    public static class Populate
    {

        internal static void Populate_combobox_clienti(ComboBox[] nome_ctr, int deleted = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            string addInfo = "";

            if (deleted == 0)
            {
                addInfo += " WHERE deleted = @deleted ";
            }

            string commandText = @"SELECT 
                                    Id, 
                                    ( nome || IIF(deleted == 1, '(Eliminato)', '')) AS nome
                                    FROM " + ProgramParameters.schemadb + @"[clienti_elenco] 
                                    " + addInfo + @"
                                    ORDER BY deleted, nome ASC;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {

                try
                {
                    cmd.Parameters.AddWithValue("@deleted", deleted);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0}", reader["nome"]), Value = Convert.ToInt32(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_clienti. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }
        }

        internal static void Populate_combobox_sedi(ComboBox[] nome_ctr, long id_cliente, int deleted = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };


            string commandText = @"SELECT 
                                        Id AS id,
                                        IIF(numero IS NOT NULL, '(' || numero || ')','' ) AS numero,
                                        stato AS stato,
                                        provincia AS provincia,
                                        citta AS citta
                                    FROM " + ProgramParameters.schemadb + @"[clienti_sedi] 
                                    WHERE ID_Cliente = @idcl AND deleted = @deleted
                                    ORDER BY stato, Id ASC;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {

                try
                {
                    cmd.Parameters.AddWithValue("@idcl", id_cliente);
                    cmd.Parameters.AddWithValue("@deleted", deleted);

                    SQLiteDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0}/{1}/{2} {3} ", reader["stato"], reader["provincia"], reader["citta"], reader["numero"]), Value = Convert.ToInt64(reader["Id"]) });
                    }
                    reader.Close();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore Populate_combobox_sedi. Codice: " + DbTools.ReturnErorrCode(ex));
                    return;
                }
            }

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }
        }

        internal static void Populate_combobox_statoOfferte(ComboBox[] nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 },
                new ComboBoxList() { Name = "APERTA", Value = 0 },
                new ComboBoxList() { Name = "ORDINATA", Value = 1 },
                new ComboBoxList() { Name = "ANNULLATA", Value = 2 }
            };

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }
        }

        internal static void Populate_combobox_statoOrdini(ComboBox[] nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 },
                new ComboBoxList() { Name = "APERTO", Value = 0 },
                new ComboBoxList() { Name = "CHIUSO", Value = 1 }
            };

            int count = nome_ctr.Count();
            for (int i = 0; i < count; i++)
            {
                Utility.DataSourceToComboBox(nome_ctr[i], dataSource);
            }
        }

        internal static void Populate_combobox_pref(ComboBox nome_ctr, long ID_cliente = 0, long ID_sede = 0, int deleted = 0)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            if (ID_cliente > 0)
            {
                string cond;
                if (ID_sede > 0)
                {
                    cond = "ID_sede = @idsd OR (ID_cliente = @idcl AND ID_sede IS NULL) ";
                }
                else
                {
                    cond = "ID_cliente = @idcl";
                }

                string commandText = "SELECT Id,nome FROM " + ProgramParameters.schemadb + @"[clienti_riferimenti] WHERE  deleted = @deleted AND " + cond + " ORDER BY ID_sede, Id ASC;";

                using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
                {
                    try
                    {

                        cmd.Parameters.AddWithValue("@idcl", ID_cliente);
                        cmd.Parameters.AddWithValue("@idsd", ID_sede);
                        cmd.Parameters.AddWithValue("@deleted", deleted);

                        SQLiteDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1}", reader["Id"], reader["nome"]), Value = Convert.ToInt64(reader["Id"]) });
                        }
                        reader.Close();
                    }
                    catch (SQLiteException ex)
                    {
                        OnTopMessage.Error("Errore populate_combobox_pref. Codice: " + DbTools.ReturnErorrCode(ex));

                        return;
                    }
                }
            }

            //Setup data binding
            Utility.DataSourceToComboBox(nome_ctr, dataSource);
        }

        internal static void Populate_combobox_FieldOrdSpedGestione(ComboBox nome_ctr)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 },
                new ComboBoxList() { Name = "Exlude from Tot.", Value = 0 },
                new ComboBoxList() { Name = "Add total & No Discount", Value = 1 },
                new ComboBoxList() { Name = "Add Tot with Discount", Value = 2 }
            };

            //Setup data binding
            Utility.DataSourceToComboBox(nome_ctr, dataSource);
        }

        internal static void Populate_combobox_dummy(ComboBox nome_ctr = null, ComboBox[] nomi_ctrs = null)
        {

            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };

            if (nomi_ctrs != null)
            {
                foreach (ComboBox ctr in nomi_ctrs)
                    Utility.DataSourceToComboBox(ctr, dataSource);
            }
            else if (nome_ctr != null)
                Utility.DataSourceToComboBox(nome_ctr, dataSource);

            return;
        }

        internal static void Populate_combobox_ordini_crea_offerta(ComboBox nome_ctr, long idcl = 0, long idsd = 0, bool transformed = true, int codice = 0, int? stato = 1)
        {
            var dataSource = new List<ComboBoxList>
            {
                new ComboBoxList() { Name = "", Value = -1 }
            };
            string commandText;


            if (transformed)
                commandText = @"SELECT Id AS id, codice_offerta AS codice 
                                    FROM " + ProgramParameters.schemadb + @"[offerte_elenco] 
                                    WHERE ID_sede IN (SELECT id FROM " + ProgramParameters.schemadb + @"[clienti_sedi] AS CS WHERE CS.ID_cliente = @idcl)  AND trasformato_ordine = 0 AND stato LIKE @stato;";
            else
                commandText = @"SELECT Id AS id, codice_offerta AS codice FROM " + ProgramParameters.schemadb + @"[offerte_elenco] WHERE Id=@idof;";


            using (SQLiteCommand cmd = new(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddWithValue("@idcl", idcl);
                    cmd.Parameters.AddWithValue("@idof", codice);
                    cmd.Parameters.AddWithValue("@stato", (stato != null) ? stato : "%");
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    bool presres = false;
                    while (reader.Read())
                    {
                        dataSource.Add(new ComboBoxList() { Name = String.Format("{0} - {1}", reader["id"], reader["codice"]), Value = Convert.ToInt64(reader["Id"]) });
                        presres = true;
                    }

                    reader.Close();
                    if (presres == true)
                        nome_ctr.Enabled = true;
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore populate_combobox_ordini_crea. Codice: " + DbTools.ReturnErorrCode(ex));


                    return;
                }
            }

            Utility.DataSourceToComboBox(nome_ctr, dataSource);
        }
    }
}
