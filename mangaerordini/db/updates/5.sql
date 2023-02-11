/*/Add row to order table to keep track of date entered as calendar date in outlook*/
ALTER TABLE [ordini_elenco]
  ADD [data_calendar_event] DATE NULL;
	
UPDATE  informazioni SET versione=5 WHERE id=1;



