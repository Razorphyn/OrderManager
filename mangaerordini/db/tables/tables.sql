	CREATE TABLE IF NOT EXISTS [informazioni] (
		[Id]        INTEGER	PRIMARY KEY	NOT NULL,
		[versione]  DECIMAL DEFAULT ((6)) NOT NULL
	);
	INSERT INTO [informazioni] ([Id] ,[versione]) VALUES (1 ,7); 

	CREATE TABLE IF NOT EXISTS [fornitori] (
		[Id]        	INTEGER			PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome] 			VARCHAR (255) 	NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[uniqueness]	SMALLINT		NULL,
		UNIQUE ([nome] , [uniqueness])
	);


	CREATE TABLE IF NOT EXISTS [clienti_elenco] (
		[Id]        	INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome]      	VARCHAR (255) 	NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[uniqueness]	SMALLINT		NULL,
		CONSTRAINT [ui_clienti_elenco_nome_uniqueness] UNIQUE ([nome], [uniqueness])
	);
	
	
	CREATE TABLE IF NOT EXISTS [clienti_sedi] (
		[Id]        	INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_cliente]    INT				NOT NULL,
		[numero]    	INT				NULL,
		[stato]     	VARCHAR (255) 	NOT NULL,
		[provincia] 	VARCHAR (255) 	NOT NULL,
		[citta]     	VARCHAR (255) 	NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[uniqueness]	SMALLINT		NULL,
		CONSTRAINT [ui_clienti_sedi_ID_clienti_numero_uniqueness] UNIQUE ([numero], [uniqueness]),
		CONSTRAINT [FK_clienti_sedi_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id])
	);
	CREATE INDEX search_clienti_sedi_ID_sede ON clienti_sedi (Id);
	
	
	CREATE TABLE IF NOT EXISTS [clienti_riferimenti] (
		[Id]			INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_cliente]	INT           NOT NULL,
		[ID_sede] 		INT           NULL,
		[nome]			VARCHAR (255) NOT NULL,
		[mail]			VARCHAR (255) NOT NULL,
		[telefono]		VARCHAR (255) NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[uniqueness]	SMALLINT		NULL,
		UNIQUE ([ID_cliente], [nome], [uniqueness]),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_clienti_riferimenti_ID_cliente ON clienti_riferimenti (ID_cliente);

	CREATE TABLE IF NOT EXISTS [clienti_macchine] (
		[Id]			INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[modello]		VARCHAR (255) 	NOT NULL,
		[codice]		VARCHAR (255) 	NULL,
		[seriale]    	VARCHAR (255) 	NULL,
		[ID_cliente]	INT           	NOT NULL,
		[ID_sede]		INT				NULL,
		[deleted]		SMALLINT		NOT NULL DEFAULT 0,
		[uniqueness]	SMALLINT		NULL,
		UNIQUE ([seriale]),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_Sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_clienti_macchine_ID_cliente ON clienti_macchine (Id, ID_cliente,ID_Sede);

	CREATE TABLE IF NOT EXISTS [pezzi_ricambi] (
		[Id]        	INTEGER	PRIMARY KEY AUTOINCREMENT NOT NULL,
		[nome]			VARCHAR (255)   NOT NULL,
		[codice]		VARCHAR (20)    NOT NULL,
		[descrizione]	VARCHAR (8000)  NOT NULL,
		[prezzo]		DECIMAL (19, 4) NULL,
		[ID_fornitore]	INT             NOT NULL,
		[ID_macchina]	INT             NULL,
		[deleted]		SMALLINT		NOT NULL DEFAULT 0,
		[uniqueness]	SMALLINT		NULL,
		CONSTRAINT [ui_pezzi_ricambi_nome_codice_uniqueness] UNIQUE ([nome], [codice], [uniqueness]),
		CONSTRAINT [FK_pezzi_ricambi_To_fornitori] FOREIGN KEY ([ID_fornitore]) REFERENCES [fornitori] ([Id]),
		CONSTRAINT [FK_pezzi_ricambi_To_clienti_macchine] FOREIGN KEY ([ID_macchina]) REFERENCES [clienti_macchine] ([Id])
	);
	CREATE INDEX search_pezzi_ricambi ON pezzi_ricambi (ID_macchina, ID_fornitore);

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
		UNIQUE ([ID_sede], [codice_offerta]),
		CONSTRAINT [FK_offerte_elenco_To_clienti_riferimenti] FOREIGN KEY ([ID_riferimento]) REFERENCES [clienti_riferimenti] ([Id]),
		CONSTRAINT [FK_offerte_elenco_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_offerte_elenco ON offerte_elenco (stato, ID_sede, ID_riferimento);


	CREATE TABLE IF NOT EXISTS [offerte_pezzi] (
		[Id]        				INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_offerta]                INT             NOT NULL,
		[ID_ricambio]               INT             NOT NULL,
		[prezzo_unitario_originale] DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_unitario_sconto]    DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[pezzi]                     REAL            DEFAULT ((0)) NOT NULL,
		[aggiunto]                  SMALLINT        DEFAULT ((0)) NOT NULL,
		CONSTRAINT [ui_offerte_pezzi] UNIQUE ([ID_offerta], [ID_ricambio]),
		CONSTRAINT [FK_offerte_pezzi_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id]),
		CONSTRAINT [FK_offerte_pezzi_To_pezzi_ricambi] FOREIGN KEY ([ID_ricambio]) REFERENCES [pezzi_ricambi] ([Id])
	);
	CREATE INDEX search_offerte_pezzi ON offerte_pezzi (ID_offerta, ID_ricambio);


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


	CREATE TABLE IF NOT EXISTS [ordine_pezzi] (
		[Id]        				INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_ordine]                 INT             NOT NULL,
		[ID_ricambio]               INT             NOT NULL,
		[prezzo_unitario_originale] DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_unitario_sconto]    DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[pezzi]                     REAL            DEFAULT ((0)) NOT NULL,
		[ETA]                       DATE            NOT NULL,
		[Outside_Offer] 			BOOLEAN 		DEFAULT ((0)) NOT NULL,
		CONSTRAINT [ui_ordine_pezzi] UNIQUE ([ID_ordine], [ID_ricambio]),
		CONSTRAINT [FK_ordine_pezzi_To_pezzi_ricambi] FOREIGN KEY ([ID_ricambio]) REFERENCES [pezzi_ricambi] ([Id]),
		CONSTRAINT [FK_ordine_pezzi_To_ordini_elenco] FOREIGN KEY ([ID_ordine]) REFERENCES [ordini_elenco] ([Id])
	);
	CREATE INDEX search_ordine_pezzi ON ordine_pezzi (ID_ordine, ID_ricambio);