DROP TABLE IF EXISTS temp_table;

/*eventi_ordini*/

	CREATE TABLE IF NOT EXISTS [eventi_ordini] (
		[Id]        					INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_ordine]       				INT             	NOT NULL,
		[data_calendar_event_offline] 	DATE 				NULL,
		[ICalUId] 						VARCHAR				NULL,
		UNIQUE ([ID_ordine])
	);
	CREATE INDEX search_ordini_elenco ON ordini_elenco (Id, ID_ordine);

/*ordini_elenco*/
	
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM ordini_elenco;

	DROP TABLE ordini_elenco;
	
	CREATE TABLE IF NOT EXISTS [ordini_elenco] (
		[Id]        			INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[codice_ordine]       	VARCHAR (255)   	NOT NULL,
		[ID_offerta]          	INT             	NULL,
		[ID_sede]          		INT             	NULL,
		[ID_riferimento]      	INT             	NULL,
		[data_ordine]         	DATE            	NOT NULL,
		[data_ETA]            	DATE            	NULL,
		[costo_spedizione]    	DECIMAL (19, 4) 	NULL,
		[totale_ordine]       	DECIMAL (19, 4) 	DEFAULT ((0)) NOT NULL,
		[sconto]              	DECIMAL (19, 4) 	DEFAULT ((0)) NOT NULL,
		[prezzo_finale]       	DECIMAL (19, 4) 	DEFAULT ((0)) NOT NULL,
		[gestione_spedizione] 	SMALLINT        	NULL,
		[stato]               	SMALLINT        	DEFAULT ((0)) NOT NULL,
		[data_calendar_event] 	DATE 				NULL,
		UNIQUE ([ID_sede], [codice_ordine]),
		CONSTRAINT [FK_ordini_elenco_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id]),
		CONSTRAINT [FK_ordini_elenco_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id]),
		CONSTRAINT [FK_ordini_elenco_To_clienti_riferimenti] FOREIGN KEY ([ID_riferimento]) REFERENCES [clienti_riferimenti] ([Id])
	);
	CREATE INDEX search_ordini_elenco ON ordini_elenco (Id, stato, ID_sede, ID_offerta);

/*RESTORE*/
/*ordini_elenco*/
	INSERT OR IGNORE INTO ordini_elenco
		(     
			Id,
			codice_ordine,
			ID_offerta,
			ID_sede,
			ID_riferimento,
			data_ordine,
			data_ETA,
			costo_spedizione,
			totale_ordine,
			sconto,
			prezzo_finale,
			gestione_spedizione,
			stato
		)
	SELECT
			*
	FROM temp_table;
	
/*eventi_ordini*/	
	INSERT OR IGNORE INTO eventi_ordini
		(     
			ID_ordine,
			data_calendar_event_offline
		)
	SELECT
			Id,
			data_calendar_event	
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
/*INDEX*/

/*ordine_pezzi*/	
	DROP INDEX search_ordine_pezzi;
	CREATE INDEX search_ordine_pezzi ON ordine_pezzi (ID_ordine, ID_ricambio, Outside_Offer);
	
/*clienti_sedi*/	
	DROP INDEX search_clienti_sedi_ID_sede;
	CREATE INDEX search_clienti_sedi_ID_sede ON clienti_sedi (Id, ID_cliente);
	
	

/*Update version*/
	UPDATE  informazioni SET versione=11 WHERE id=1;