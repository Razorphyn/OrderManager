﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace Razorphyn
{
    public class CalendarManager
    {
        readonly DbTools DbTools = new DbTools();

        public class CalendarResult
        {
            public bool Success { get; set; } = false;

            public bool Found { get; set; } = false;

            public DateTime AppointmentDate { get; set; } = DateTime.Now.AddDays(-7);

            public Outlook.Folder CalendarFolder { get; set; } = null;
        }

        public void UpdateCalendar(string oldRef, string newRef, int id_ordine, DateTime estDate, bool delete = true)
        {
            bool check = false;
            if (delete == true)
                check = RemoveAppointment(oldRef);

            if (check == true || delete == false)
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

                DataValidation.ValidationResult dateAppoint = new DataValidation.ValidationResult
                {
                    DateValue = DateTime.MinValue
                };

                string body = CreateAppointmentBody(id_ordine);

                if (delete == true)
                {
                    while (dateAppoint.DateValue == DateTime.MinValue)
                    {
                        string input = Interaction.InputBox("Inserire la data per l'appunatmento sul calendario? Una volta creato, sarà necessario salvarlo." + Environment.NewLine + Environment.NewLine
                                                            + "ATTENZIONE: NON rimuovere la stringa finale ##ManaOrdini[numero_ordine]## dal titolo dell'appunatmento. Serve per riconoscere l'evento.", "Modifica Appuntamento Calendario", (estDate).ToString(ProgramParameters.dateFormat));
                        if (String.ReferenceEquals(input, String.Empty))
                        {
                            OnTopMessage.Alert("Azione Cancellata");
                            return;
                        }

                        var DataValidation = new DataValidation();
                        dateAppoint = DataValidation.ValidateDate(input);

                        if (dateAppoint.Error != null)
                        {
                            OnTopMessage.Alert("Controllare formato data. Impossibile convertire in formato data corretto.");
                            dateAppoint.DateValue = DateTime.MinValue;
                            continue;
                        }

                        if (DateTime.Compare(dateAppoint.DateValue, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint.DateValue, estDate) > 0)
                        {
                            DialogResult confDataLaterOrder = OnTopMessage.Question("La data scelta va oltre alla data di consegna dell'ordine, continuare?", "Creazione Appuntamento Calendario");
                            if (confDataLaterOrder == DialogResult.No)
                            {
                                dateAppoint.DateValue = DateTime.MinValue;
                            }
                        }

                        if (DateTime.Compare(dateAppoint.DateValue, DateTime.MinValue) != 0 && DateTime.Compare(dateAppoint.DateValue, DateTime.Now.Date) < 0)
                        {
                            DialogResult confDataLaterOrder = OnTopMessage.Question("La data scelta è antecedente alla dato odierna, continuare?", "Creazione Appuntamento Calendario");
                            if (confDataLaterOrder == DialogResult.No)
                            {
                                dateAppoint.DateValue = DateTime.MinValue;
                            }
                        }
                    }
                    AddAppointment(newRef, body, dateAppoint.DateValue);
                }
                else
                    UpdateBodyCalendar(newRef, body);

                OnTopMessage.Information("Appuntamento calendario aggiornato");
            }
        }

        public void AddAppointment(string ordRef, string body, DateTime estDate)
        {
            UserSettings UserSettings = new UserSettings();

            try
            {
                Outlook.Folder personalCalendar = FindCalendar(UserSettings.settings["calendario"]["nomeCalendario"]);

                if (personalCalendar == null)
                {
                    OnTopMessage.Error("Errore nella gestione calendari, non è possibile continuare. Provare a riavvaire outlook.");
                    return;
                }

                if (FindAppointment(UserSettings.settings["calendario"]["nomeCalendario"], ordRef, personalCalendar))
                {
                    OnTopMessage.Alert("Evento già presente. Rimuoverlo o aggiornarlo se necessario");
                    return;
                }

                Outlook.AppointmentItem newAppointment = personalCalendar.Items.Add(Outlook.OlItemType.olAppointmentItem) as Outlook.AppointmentItem;
                newAppointment.AllDayEvent = true;
                newAppointment.Start = estDate + TimeSpan.Parse("8:00");
                newAppointment.End = estDate + TimeSpan.Parse("17:00");

                newAppointment.Location = "";
                newAppointment.Body = body;
                newAppointment.Subject = "Reminder Ordine Numero:" + ordRef + "\t" + "##ManaOrdini" + ordRef + "##";

                newAppointment.Display(true);

                UpdateDbDateAppointment(estDate + TimeSpan.Parse("00:00:00"), ordRef);

            }
            catch (System.Exception ex)
            {
                OnTopMessage.Error("Si è verificato un errore durante la creazione dell'appuntamento. Errore: " + ex.Message);
            }
        }

        public bool RemoveAppointment(string ordRef, List<Tuple<string, Outlook.AppointmentItem>> listaApp = null)
        {
            UserSettings UserSettings = new UserSettings();

            bool found = false;
            int c = 0;
            Outlook.Folder personalCalendar = FindCalendar(UserSettings.settings["calendario"]["nomeCalendario"]);

            if (listaApp == null)
            {
                listaApp = new List<Tuple<string, Outlook.AppointmentItem>>();

                if (personalCalendar == null)
                {
                    OnTopMessage.Error("Errore nella gestione calendari, non è possibile continuare. Provare a riavvaire Outlook.");
                    return false;
                }

                if (!FindAppointment(UserSettings.settings["calendario"]["nomeCalendario"], ordRef, personalCalendar))
                {
                    OnTopMessage.Alert("Evento non presente." + Environment.NewLine + Environment.NewLine + "NOTA: La data di partenza di ricerca degli eventi è 7 fa." + Environment.NewLine + " Se l'evento è stato modfiicato a mano oltre queste date, il porgramma non lo troverà.");
                    return false;
                }

                DateTime start = DateTime.Now.AddDays(-1);

                Outlook.Items restrictedItems = CalendarGetItems(personalCalendar, start, DateTime.MaxValue, ordRef);

                string pattern = @"^.+##ManaOrdini([0-9]+)##$";

                foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                {
                    foreach (Match match in Regex.Matches(apptItem.Subject, pattern, RegexOptions.IgnoreCase))
                    {
                        listaApp.Add(new Tuple<string, Outlook.AppointmentItem>(match.Groups[1].Value.Trim(), apptItem));
                        c++;
                    }
                }

                OnTopMessage.Alert(c + " elemento/i trovato/i con l'identificativo dell'evento. Verrà chiesta conferma prima dell'eliminazione di ciascun evento.");
            }
            else
            {
                c = listaApp.Count;
            }

            int deleted = 0;
            for (int i = 0; i < c; i++)
            {
                DialogResult dialogResult = OnTopMessage.Question("Cancellare l'appuntamento col nome: '" + listaApp[i].Item2.Subject + "' fissato in data: " + (listaApp[i].Item2.Start) + "?", "Eliminazione Evento da Calendario (Evento " + (i + 1) + " di " + c + ") - Ordine Numero: " + ordRef);
                if (dialogResult == DialogResult.Yes)
                {
                    try
                    {
                        listaApp[i].Item2.Delete();
                        UpdateDbDateAppointment(null, listaApp[i].Item1);
                        found = true;
                        deleted++;
                        OnTopMessage.Information("Evento calendario rimosso.");
                    }
                    catch
                    {
                        OnTopMessage.Error("Si è verificato un errore durante l'eliminazione. Controllare il calendario.");
                        return false;
                    }
                }
            }

            if (c - deleted > 1)
            {
                OnTopMessage.Alert("Attenzione, esistono ancora eventi multipli per lo stesso ordine.");
            }

            if (deleted != c)
            {
                Outlook.Items restrictedItems = CalendarGetItems(personalCalendar, DateTime.Now.AddDays(-7), DateTime.MaxValue, ordRef);

                foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                {
                    UpdateDbDateAppointment(apptItem.Start, ordRef);
                    return true;
                }
            }

            if (found == true)
            {
                OnTopMessage.Information("Operazioni concluse.");
                return true;
            }

            return false;

        }

        public bool? MoveAppointment(string oldCalendar, string newCalendar)
        {
            Outlook.Folder personalCalendar = FindCalendar(oldCalendar);
            Outlook.Folder newCalendarFolder = FindCalendar(newCalendar);

            if (personalCalendar == null || newCalendarFolder == null)
            {
                OnTopMessage.Error("Errore nella gestione calendari, non è possibile continuare. Provare a riavvaire Outlook.");
                return false;
            }

            Outlook.Items restrictedItems = CalendarGetItems(personalCalendar, DateTime.MinValue, DateTime.MaxValue);

            bool error_free = true;
            int c = 0;

            List<Outlook.AppointmentItem> listaApp = new List<Outlook.AppointmentItem>();
            foreach (Outlook.AppointmentItem apptItem in restrictedItems)
            {

                if (Regex.IsMatch(apptItem.Subject, @"^.*##ManaOrdini\d{1,}##.*$"))
                {
                    listaApp.Add(apptItem);
                    c++;
                }

                for (int i = 0; i < c; i++)
                {
                    try
                    {
                        listaApp[i].Move(newCalendarFolder);
                    }
                    catch (System.Exception ex)
                    {
                        OnTopMessage.Error("Si è verificato un errore durante la creazione dell'appuntamento. Errore: " + ex.Message);
                        error_free = false;
                    }
                }
            }

            return error_free;
        }

        public bool UpdateBodyCalendar(string ordRef, string body)
        {
            UserSettings UserSettings = new UserSettings();

            Outlook.Folder personalCalendar = FindCalendar(UserSettings.settings["calendario"]["nomeCalendario"]);

            if (personalCalendar == null)
            {
                OnTopMessage.Error("Errore nella gestione calendari, non è possibile continuare. Provare a riavvaire Outlook.");
                return false;
            }

            Outlook.Items restrictedItems;

            CalendarResult answer = GetDbDateCalendar(new string[] { ordRef });

            if (answer.Found)
                restrictedItems = CalendarGetItems(personalCalendar, answer.AppointmentDate.AddDays(-1), answer.AppointmentDate.AddDays(1), ordRef);
            else
                restrictedItems = CalendarGetItems(personalCalendar, answer.AppointmentDate, DateTime.MaxValue, ordRef);

            bool updated = false;

            foreach (Outlook.AppointmentItem apptItem in restrictedItems)
            {
                apptItem.Body = body;
                apptItem.Save();
                updated = true;
            }

            return updated;
        }

        public string CreateAppointmentBody(int id_ordine)
        {
            string clnome = "";
            string clstato = "";
            string clprov = "";
            string clcitt = "";
            string crnome = "";
            string crtel = "";
            string crmail = "";
            string optot = "";
            string opde = "";
            string optotf = "";
            string prezzofinaleIclSped = "";


            string commandText = @"SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,
												CE.nome as clnome,
												CE.stato as clstato,
												CE.provincia as clprov,
												CE.citta as clcitt,

												CR.nome as crnome,
												CR.telefono as crtel,
												CR.mail as crmail,
												strftime('%d/%m/%Y', OP.data_ordine) AS opdo,
												strftime('%d/%m/%Y', OP.data_ETA) AS opde,
												REPLACE( printf('%.2f',OP.totale_ordine ),'.',',')  AS optot,
                                                REPLACE(  (
                                                        printf('%.2f',OP.prezzo_finale  ) || 
                                                        ' (' ||    
                                                        printf('%.2f',OP.sconto ) || '%)'),'.',',')  AS optotf,

                                                REPLACE(printf('%.2f',OP.prezzo_finale + (CASE OP.gestione_spedizione  
                                                                                         WHEN 1 THEN   OP.costo_spedizione
                                                                                         WHEN 2 THEN   OP.costo_spedizione*(1-OP.sconto/100) 
                                                                                         ELSE 0  
                                                                                      END) ),'.',',') AS prezzofinaleIclSped
												

									   FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OP
									   LEFT JOIN " + ProgramParameters.schemadb + @"[offerte_elenco] AS OE
										ON OE.Id = OP.ID_offerta
									   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OE.ID_cliente
									   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OE.ID_riferimento

									   WHERE OP.ID_offerta IS NOT NULL AND OP.id=@idOrdine

                                        UNION ALL
                                        SELECT
												OP.Id AS idord,
												(CASE OP.stato WHEN 0 THEN 'APERTO'  WHEN 1 THEN 'CHIUSO' END) AS ordstat,
												OP.codice_ordine AS codice_ordine,
												CE.nome as clnome,
												CE.stato as clstato,
												CE.provincia as clprov,
												CE.citta as clcitt,

												CR.nome as crnome,
												CR.telefono as crtel,
												CR.mail as crmail,
												strftime('%d/%m/%Y', OP.data_ordine) AS opdo,
												strftime('%d/%m/%Y', OP.data_ETA) AS opde,
												REPLACE( printf('%.2f',OP.totale_ordine ),'.',',')  AS optot,
                                                REPLACE(  (
                                                        printf('%.2f',OP.prezzo_finale  ) || 
                                                        ' (' ||    
                                                        printf('%.2f',OP.sconto ) || '%)'),'.',',')  AS optotf,

                                                REPLACE(printf('%.2f',OP.prezzo_finale + (CASE OP.gestione_spedizione  
                                                                                         WHEN 1 THEN   OP.costo_spedizione
                                                                                         WHEN 2 THEN   OP.costo_spedizione*(1-OP.sconto/100) 
                                                                                         ELSE 0  
                                                                                      END) ),'.',',') AS prezzofinaleIclSped
												

									   FROM " + ProgramParameters.schemadb + @"[ordini_elenco] AS OP
									   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_elenco] AS CE
										ON CE.Id = OP.ID_cliente
									   LEFT JOIN " + ProgramParameters.schemadb + @"[clienti_riferimenti] AS CR
										ON CR.Id = OP.ID_riferimento

									   WHERE OP.ID_offerta IS NULL AND OP.id=@idOrdine  

                                        LIMIT 1;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idOrdine", id_ordine);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        clnome = Convert.ToString(reader["clnome"]);
                        clstato = Convert.ToString(reader["clstato"]);
                        clprov = Convert.ToString(reader["clprov"]).ToUpper();
                        clcitt = Convert.ToString(reader["clcitt"]);
                        crnome = Convert.ToString(reader["crnome"]);
                        crtel = Convert.ToString(reader["crtel"]);
                        crmail = Convert.ToString(reader["crmail"]);
                        optot = Convert.ToString(reader["optot"]);
                        opde = Convert.ToString(reader["opde"]);
                        optotf = Convert.ToString(reader["optotf"]);
                        prezzofinaleIclSped = Convert.ToString(reader["prezzofinaleIclSped"]);
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante recupero info ordine(appuntamento). Codice: " + DbTools.ReturnErorrCode(ex));
                    return "";
                }
            }

            string body = "";

            body += clnome + Environment.NewLine;
            body += clcitt + " (" + clprov + ") " + clstato + Environment.NewLine;
            body += Environment.NewLine;
            body += "Contatto: " + Environment.NewLine + crnome + "\t" + crtel + "\t" + crmail + Environment.NewLine;
            body += Environment.NewLine;
            body += "Data Consegna: " + Environment.NewLine + opde + Environment.NewLine;
            body += Environment.NewLine;
            body += "Totale Finale (Excl Sconti): " + "\t" + optot + Environment.NewLine;
            body += Environment.NewLine;
            body += "Totale Finale (Incl Sconti): " + "\t" + optotf + Environment.NewLine;
            body += Environment.NewLine;
            body += "Totale Finale (Incl. Spedizioni e sconti): " + "\t" + prezzofinaleIclSped + Environment.NewLine;
            body += Environment.NewLine;
            body += Environment.NewLine;
            body += "Elenco Oggetti Ordine";
            body += Environment.NewLine;

            commandText = @"SELECT
									OP.Id as ID,
									PR.nome AS nome,
									PR.codice AS code,
									REPLACE( printf('%.2f',OP.prezzo_unitario_originale ),'.',',')  AS por,
									REPLACE( printf('%.2f',OP.prezzo_unitario_sconto ),'.',',')  AS pos,
									OP.pezzi AS qta,
									REPLACE( printf('%.2f',SUM(OP.prezzo_unitario_sconto * OP.pezzi) ),'.',',')  AS totale,
									strftime('%d/%m/%Y', OP.ETA) AS ETA,
								    PR.descrizione AS descrizione

									FROM " + ProgramParameters.schemadb + @"[ordine_pezzi] AS OP
								   LEFT JOIN " + ProgramParameters.schemadb + @"[pezzi_ricambi] AS PR
									ON PR.Id = OP.ID_ricambio
								   
									WHERE OP.ID_ordine=@idord 
									GROUP BY OP.Id, PR.nome, PR.codice, OP.prezzo_unitario_originale, OP.prezzo_unitario_sconto, OP.pezzi, PR.descrizione, PR.descrizione, OP.ETA
									ORDER BY OP.Id;";


            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@idord", id_ordine);

                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        body += "\t" + reader["code"] + "\t" + "Quantità: " + reader["qta"];
                        body += Environment.NewLine + "\t\t" + "Prezzo Totale: " + reader["totale"] + "€" + "\t" + "Prezzo Unitario: " + reader["pos"] + "€";
                        if (!String.IsNullOrEmpty(Convert.ToString(reader["descrizione"])))
                            body += Environment.NewLine + "\t\t" + Convert.ToString(reader["descrizione"]);
                        body += Environment.NewLine + "\t\t" + "Data Consegna Pezzo:" + "\t" + Convert.ToString(reader["ETA"]);

                        body += Environment.NewLine;
                        body += Environment.NewLine;
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante recupero oggetti ordine(appuntamento). Codice: " + DbTools.ReturnErorrCode(ex));
                    return "";
                }
            }

            return body;
        }

        private CalendarResult CreateCustomCalendar(string calName)
        {
            CalendarResult answer = new CalendarResult
            {
                Success = true
            };

            if (String.IsNullOrEmpty(calName))
            {
                answer.Found = true;
            }
            else
            {
                try
                {
                    Outlook.Application OlApp = new Outlook.Application();
                    Outlook.Folder primaryCalendar = OlApp.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;

                    foreach (Outlook.Folder Calendar in primaryCalendar.Folders)
                    {
                        if (Calendar.Name == calName)
                        {
                            answer.Found = true;
                            break;
                        }
                    }

                    if (!answer.Found)
                    {
                        answer.CalendarFolder = primaryCalendar.Folders.Add(calName, Outlook.OlDefaultFolders.olFolderCalendar) as Outlook.Folder;
                    }
                }
                catch
                {
                    OnTopMessage.Error("Errore durante verifica necessità cartella OutLook. Impossibile aggiornare informazioni." + Environment.NewLine + "Incrociare dia per evitare danni ai dati");
                    answer.Success = false;
                }
            }

            return answer;
        }

        public Outlook.Folder FindCalendar(string calendarName)
        {
            Outlook.Application OlApp = new Outlook.Application();

            Outlook.Folder AppointmentFolder =
                OlApp.Session.GetDefaultFolder(
                Outlook.OlDefaultFolders.olFolderCalendar)
                as Outlook.Folder;

            Outlook.Folder personalCalendar = AppointmentFolder;

            if (!String.IsNullOrEmpty(calendarName) && AppointmentFolder.Name != calendarName)
            {
                foreach (Outlook.Folder personalCalendarLoop in AppointmentFolder.Folders)
                {
                    if (personalCalendarLoop.Name == calendarName)
                    {
                        return personalCalendarLoop;
                    }
                }

                CalendarResult re = CreateCustomCalendar(calendarName);

                if (re.Success && !re.Found)
                    personalCalendar = re.CalendarFolder;
                else if (!re.Success)
                    return null;
            }

            return personalCalendar;
        }

        public Outlook.Items CalendarGetItems(Outlook.Folder personalCalendar, DateTime startDate, DateTime endDate, string orderef = "")
        {

            string AppCode = "##ManaOrdini" + orderef;
            string filterDate = "[Start] >= '" + startDate.ToString("g") + "' AND [End] <= '" + endDate.ToString("g") + "'";
            string filterSubject = "@SQL=" + "\"" + "urn:schemas:httpmail:subject" + "\"" + " LIKE '%" + AppCode + "%'";

            Outlook.Items calendarItems = personalCalendar.Items.Restrict(filterDate);
            calendarItems.IncludeRecurrences = true;
            calendarItems.Sort("[Start]", Type.Missing);

            Outlook.Items restrictedItems = calendarItems.Restrict(filterSubject);

            return restrictedItems;
        }

        public bool FindAppointment(string CalendarName, string ordRef, Outlook.Folder personalCalendar = null)
        {
            try
            {
                if (personalCalendar == null)
                {
                    personalCalendar = FindCalendar(CalendarName);
                    if (personalCalendar == null)
                    {
                        OnTopMessage.Error("Errore nella gestione calendari, non è possibile continuare. Provare a riavvaire Outlook.");
                        return false;
                    }
                }

                CalendarResult answer = GetDbDateCalendar(new string[] { ordRef });

                if (answer.Success && !answer.Found)
                    return false;

                Outlook.Items restrictedItems = CalendarGetItems(personalCalendar, answer.AppointmentDate.AddDays(-(answer.Found ? 1 : 0)), answer.AppointmentDate.AddDays(+1), ordRef);

                foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                {
                    return true;
                }

                restrictedItems = CalendarGetItems(personalCalendar, DateTime.Now.AddDays(-7), DateTime.MaxValue, ordRef);

                foreach (Outlook.AppointmentItem apptItem in restrictedItems)
                {
                    UpdateDbDateAppointment(apptItem.Start, ordRef);
                    return true;
                }

                OnTopMessage.Alert("Nel database è presente un appuntamento, ma non esiste corrispondenza in Outlook. Verificare informazioni, rischio conflitto." + Environment.NewLine + "Il dato su database è stato resetatto.");

                UpdateDbDateAppointment(null, ordRef);

                return false;
            }
            catch
            {
                OnTopMessage.Error("Errore durante verifica necessità cartella OutLook. Impossibile aggiornare informazioni." + Environment.NewLine + "Incrociare dita per evitare danni ai dati");
                return false;
            }
        }

        public void UpdateDbDateAppointment(DateTime? AppointmentDate, string ordRef)
        {
            var DataValidation = new DataValidation();
            DataValidation.ValidationResult codice_ordine = DataValidation.ValidateId(ordRef);
            if (codice_ordine.Error != null)
            {
                OnTopMessage.Error("Impossibile aggiornare data evento sul database.");
                return;
            }

            string commandText = @"UPDATE  " + ProgramParameters.schemadb + @"[ordini_elenco] SET data_calendar_event = @dataVal WHERE codice_ordine = @ordCode LIMIT 1;";
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
            {
                try
                {
                    if (AppointmentDate != null)
                    {
                        DateTime temp = (DateTime)AppointmentDate;
                        AppointmentDate = new DateTime(temp.Year, temp.Month, temp.Day, 0, 0, 0);

                        cmd.Parameters.AddWithValue("@dataVal", AppointmentDate);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@dataVal", DBNull.Value);
                    }

                    cmd.Parameters.AddWithValue("@ordCode", codice_ordine.IntValue);

                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento date calendario al database. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }
        }

        public CalendarResult GetDbDateCalendar(string[] ordRef)
        {
            CalendarResult answer = new CalendarResult();
            List<int> ids = new List<int>();

            foreach (string idOrd in ordRef)
            {
                var Validator = new DataValidation();
                DataValidation.ValidationResult codice_ordine = Validator.ValidateId(idOrd);
                if (codice_ordine.Error != null)
                {
                    OnTopMessage.Error("Codice ordine errato.");
                    return answer;
                }

                ids.Add((int)codice_ordine.IntValue);
            }

            string commandText = @"SELECT data_calendar_event FROM " + ProgramParameters.schemadb + @"[ordini_elenco] WHERE codice_ordine IN (@ordCode)  LIMIT 1;";
            using (SQLiteCommand cmd = new SQLiteCommand(commandText, ProgramParameters.connection))
            {
                try
                {
                    answer.Success = true;

                    cmd.Parameters.AddWithValue("@ordCode", string.Join(", ", ids));
                    object res = cmd.ExecuteScalar();

                    if (res != DBNull.Value && res !=null)
                    {
                        answer.Found = true;
                        answer.AppointmentDate = (DateTime)res;
                    }
                }
                catch (SQLiteException ex)
                {
                    OnTopMessage.Error("Errore durante aggiornamento date calendario al database. Codice: " + DbTools.ReturnErorrCode(ex));
                }
            }

            return answer;
        }

    }

}