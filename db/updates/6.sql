/*clienti elenco TEMP +sedi*/

	
	CREATE TABLE IF NOT EXISTS [clienti_elenco_temp] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome]      VARCHAR (255) 	NOT NULL,
		CONSTRAINT [ui_clienti_elenco_nome] UNIQUE ([nome] ASC)
	);
	
	CREATE TABLE IF NOT EXISTS [clienti_sedi] (
		[Id]        	INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_cliente]    INT				NOT NULL,
		[ID_cliente_old] INT			NULL,
		[numero]    	INT				NULL,
		[stato]     	VARCHAR (255) 	NOT NULL,
		[provincia] 	VARCHAR (255) 	NOT NULL,
		[citta]     	VARCHAR (255) 	NOT NULL,
		UNIQUE ([numero] ASC),
		
		CONSTRAINT [FK_clienti_sedi_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id])
	);

/*clienti_macchine*/
	
	DROP TABLE IF EXISTS temp_table;
	
	CREATE TEMPORARY TABLE temp_table AS
	SELECT 
		*
	FROM clienti_macchine;

	DROP TABLE clienti_macchine;
	
	CREATE TABLE IF NOT EXISTS [clienti_macchine] (
		[Id]			INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[modello]		VARCHAR (255) 	NOT NULL,
		[codice]		VARCHAR (255) 	NULL,
		[seriale]    	VARCHAR (255) 	NULL,
		[ID_cliente]	INT           	NOT NULL,
		[ID_sede]		INT				NULL,
		UNIQUE ([seriale]),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_Sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_clienti_macchine_ID_cliente ON clienti_macchine (ID_cliente,ID_Sede);

	INSERT OR IGNORE INTO clienti_macchine
		(     
			Id,
			modello,
			codice,
			seriale,
			ID_cliente,
			ID_sede
		)
	SELECT
		Id,
		modello,
		codice,
		seriale,
		ID_cliente,
		ID_cliente
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;


/*clienti_riferimenti*/
	CREATE TEMPORARY TABLE temp_table AS
	SELECT 
		*
	FROM clienti_riferimenti;

	DROP TABLE clienti_riferimenti;
	
	CREATE TABLE IF NOT EXISTS [clienti_riferimenti] (
		[Id]			INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_cliente]	INT           NOT NULL,
		[ID_sede] 		INT           NULL,
		[nome]			VARCHAR (255) NOT NULL,
		[mail]			VARCHAR (255) NOT NULL,
		[telefono]		VARCHAR (255) NOT NULL,
		
		UNIQUE ([ID_cliente] ASC, [nome] ASC),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_clienti_riferimenti_ID_cliente ON clienti_riferimenti (ID_cliente);

	INSERT OR IGNORE INTO clienti_riferimenti
		(     
			Id,
			ID_cliente,
			nome,
			mail,
			telefono
		)
	SELECT
			Id,
			ID_clienti,
			nome,
			mail,
			telefono
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
/*offerte_elenco*/
	CREATE TEMPORARY TABLE temp_table AS
	SELECT 
		*
	FROM offerte_elenco;

	DROP TABLE offerte_elenco;
	
	CREATE TABLE IF NOT EXISTS [offerte_elenco] (
		[Id]					INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[data_offerta]			DATE            NOT NULL,
		[codice_offerta]      	VARCHAR (255)   NOT NULL,
		[ID_sede]          		INT             NOT NULL,
		[ID_riferimento]      	INT             NULL,
		[costo_spedizione]    	DECIMAL (19, 4) NULL,
		[tot_offerta]         	DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[stato]               	SMALLINT        DEFAULT ((0)) NOT NULL,
		[trasformato_ordine]  	SMALLINT        DEFAULT ((0)) NOT NULL,
		[gestione_spedizione] 	SMALLINT        NULL,
		
		UNIQUE ([codice_offerta] ASC),
		CONSTRAINT [FK_offerte_elenco_To_clienti_riferimenti] FOREIGN KEY ([ID_riferimento]) REFERENCES [clienti_riferimenti] ([Id]),
		CONSTRAINT [FK_offerte_elenco_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_offerte_elenco ON offerte_elenco (stato, ID_sede, ID_riferimento);


	INSERT OR IGNORE INTO offerte_elenco
		(     
			Id,
			data_offerta,
			codice_offerta,
			ID_sede,
			ID_riferimento,
			costo_spedizione,
			tot_offerta,
			stato,
			trasformato_ordine,
			gestione_spedizione
		)
	SELECT
			Id,
			data_offerta,
			codice_offerta,
			ID_cliente,
			ID_riferimento,
			costo_spedizione,
			tot_offerta,
			stato,
			trasformato_ordine,
			gestione_spedizione
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;

/*ordini_elenco*/
	CREATE TEMPORARY TABLE temp_table AS
	SELECT 
		*
	FROM ordini_elenco;

	DROP TABLE ordini_elenco;
	
	CREATE TABLE IF NOT EXISTS [ordini_elenco] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
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
		
		UNIQUE ([codice_ordine] ASC),
		CONSTRAINT [FK_ordini_elenco_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id]),
		CONSTRAINT [FK_ordini_elenco_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id]),
		CONSTRAINT [FK_ordini_elenco_To_clienti_riferimenti] FOREIGN KEY ([ID_riferimento]) REFERENCES [clienti_riferimenti] ([Id])
	);
	CREATE INDEX search_ordini_elenco ON ordini_elenco (Id, stato, ID_sede, ID_offerta);


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
			stato,
			data_calendar_event
		)
	SELECT
			Id,
			codice_ordine,
			ID_offerta,
			ID_cliente,
			ID_riferimento,
			data_ordine,
			data_ETA,
			costo_spedizione,
			totale_ordine,
			sconto,
			prezzo_finale,
			gestione_spedizione,
			stato,
			data_calendar_event
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
	
UPDATE  informazioni SET versione=6 WHERE id=1;