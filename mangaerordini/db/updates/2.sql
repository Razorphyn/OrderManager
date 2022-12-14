/*informazioni*/
	DROP TABLE informazioni;

	CREATE TABLE IF NOT EXISTS [informazioni] (
	[Id]        INTEGER	PRIMARY KEY	NOT NULL,
	[versione]  INT DEFAULT ((1)) NOT NULL
	);
	INSERT INTO [informazioni] ([Id] ,[versione]) VALUES (1 ,2); 

/*fornitori*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		nome
	FROM fornitori;

	DROP TABLE fornitori;

	CREATE TABLE IF NOT EXISTS [fornitori] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome] VARCHAR (255) NOT NULL,
		
		UNIQUE ([nome] ASC)
	);

	INSERT INTO fornitori
	 (     Id,
		nome)
	SELECT
	   Id,
		nome
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
/*clienti_elenco*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		nome,
		stato,
		provincia,
		citta
	FROM clienti_elenco;

	DROP TABLE clienti_elenco;

	CREATE TABLE IF NOT EXISTS [clienti_elenco] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome]      VARCHAR (255) NOT NULL,
		[stato]     VARCHAR (255) NOT NULL,
		[provincia] VARCHAR (255) NOT NULL,
		[citta]     VARCHAR (255) NOT NULL,
		CONSTRAINT [ui_clienti_elenco_nome_citta] UNIQUE ([nome] ASC, [citta] ASC)
	);

	INSERT INTO clienti_elenco
		(
		Id,
		nome,
		stato,
		provincia,
		citta
		)
	SELECT
		Id,
		nome,
		stato,
		provincia,
		citta
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
/*clienti_riferimenti*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		ID_clienti,
		nome,
		mail,
		telefono
	FROM clienti_riferimenti;

	DROP TABLE clienti_riferimenti;

	CREATE TABLE IF NOT EXISTS [clienti_riferimenti] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_clienti] INT           NOT NULL,
		[nome]       VARCHAR (255) NOT NULL,
		[mail]       VARCHAR (255) NOT NULL,
		[telefono]   VARCHAR (255) NOT NULL,
		
		CONSTRAINT [ui_clienti_riferimenti] UNIQUE ([ID_clienti] ASC, [nome] ASC),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_elenco] FOREIGN KEY ([ID_clienti]) REFERENCES [clienti_elenco] ([Id])
	);
	CREATE INDEX search_clienti_riferimenti_IdClienti ON clienti_riferimenti (ID_clienti);

	INSERT INTO clienti_riferimenti
		(    
		Id,
		ID_clienti,
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
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
/*clienti_macchine*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		modello,
		codice,
		seriale,
		ID_cliente
	FROM clienti_macchine;

	DROP TABLE clienti_macchine;

	CREATE TABLE IF NOT EXISTS [clienti_macchine] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[modello]    VARCHAR (255) NOT NULL,
		[codice]     VARCHAR (255) NULL,
		[seriale]    VARCHAR (255) NULL,
		[ID_cliente] INT           NOT NULL,
		
		UNIQUE ([seriale] ASC),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id])
	);
	CREATE INDEX search_clienti_macchine_ID_cliente ON clienti_macchine (ID_cliente);
	

	INSERT INTO clienti_macchine
		(     
			Id,
			modello,
			codice,
			seriale,
			ID_cliente
		)
	SELECT
		Id,
		modello,
		codice,
		seriale,
		ID_cliente
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
	
/*pezzi_ricambi*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		nome,
		codice,
		descrizione,
		prezzo,
		ID_fornitore,
		ID_macchina
	FROM pezzi_ricambi;

	DROP TABLE pezzi_ricambi;

	CREATE TABLE IF NOT EXISTS [pezzi_ricambi] (
		[Id]        INTEGER	PRIMARY KEY AUTOINCREMENT NOT NULL,
		[nome]			VARCHAR (255)   NOT NULL,
		[codice]		VARCHAR (20)    NOT NULL,
		[descrizione]	VARCHAR (8000)  NOT NULL,
		[prezzo]		DECIMAL (19, 4) NULL,
		[ID_fornitore]	INT             NOT NULL,
		[ID_macchina]	INT             NULL,
		
		CONSTRAINT [ui_pezzi_ricambi] UNIQUE ([nome] ASC, [codice] ASC),
		CONSTRAINT [FK_pezzi_ricambi_To_fornitori] FOREIGN KEY ([ID_fornitore]) REFERENCES [fornitori] ([Id]),
		CONSTRAINT [FK_pezzi_ricambi_To_clienti_macchine] FOREIGN KEY ([ID_macchina]) REFERENCES [clienti_macchine] ([Id])
	);
	CREATE INDEX search_pezzi_ricambi ON pezzi_ricambi (ID_macchina, ID_fornitore);

	INSERT INTO pezzi_ricambi
		(     
			Id,
			nome,
			codice,
			descrizione,
			prezzo,
			ID_fornitore,
			ID_macchina
		)
	SELECT
		Id,
		nome,
		codice,
		descrizione,
		prezzo,
		ID_fornitore,
		ID_macchina
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
	
	
/*offerte_elenco*/
	CREATE TEMPORARY TABLE temp AS
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
	FROM offerte_elenco;

	DROP TABLE offerte_elenco;

	CREATE TABLE IF NOT EXISTS [offerte_elenco] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[data_offerta]        DATE            NOT NULL,
		[codice_offerta]      VARCHAR (255)   NOT NULL,
		[ID_cliente]          INT             NOT NULL,
		[ID_riferimento]      INT             NULL,
		[costo_spedizione]    DECIMAL (19, 4) NULL,
		[tot_offerta]         DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[stato]               SMALLINT        DEFAULT ((0)) NOT NULL,
		[trasformato_ordine]  SMALLINT        DEFAULT ((0)) NOT NULL,
		[gestione_spedizione] SMALLINT        NULL,
		
		UNIQUE ([codice_offerta] ASC),
		CONSTRAINT [FK_offerte_elenco_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_offerte_elenco_To_clienti_riferimenti] FOREIGN KEY ([ID_riferimento]) REFERENCES [clienti_riferimenti] ([Id])
	);
	CREATE INDEX search_offerte_elenco ON offerte_elenco (stato, ID_cliente, ID_riferimento);

	INSERT INTO offerte_elenco
		(     
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
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
/*offerte_pezzi*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		ID_offerta,
		ID_ricambio,
		prezzo_unitario_originale,
		prezzo_unitario_sconto,
		pezzi,
		aggiunto
	FROM offerte_pezzi;

	DROP TABLE offerte_pezzi;

	CREATE TABLE IF NOT EXISTS [offerte_pezzi] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_offerta]                INT             NOT NULL,
		[ID_ricambio]               INT             NOT NULL,
		[prezzo_unitario_originale] DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_unitario_sconto]    DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[pezzi]                     REAL            DEFAULT ((0)) NOT NULL,
		[aggiunto]                  SMALLINT        DEFAULT ((0)) NOT NULL,
		
		CONSTRAINT [ui_offerte_pezzi] UNIQUE ([ID_offerta] ASC, [ID_ricambio] ASC),
		CONSTRAINT [FK_offerte_pezzi_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id]),
		CONSTRAINT [FK_offerte_pezzi_To_pezzi_ricambi] FOREIGN KEY ([ID_ricambio]) REFERENCES [pezzi_ricambi] ([Id])
	);
	CREATE INDEX search_offerte_pezzi ON offerte_pezzi (ID_offerta, ID_ricambio);

	INSERT INTO offerte_pezzi
		(     
			Id,
			ID_offerta,
			ID_ricambio,
			prezzo_unitario_originale,
			prezzo_unitario_sconto,
			pezzi,
			aggiunto
		)
	SELECT
		Id,
		ID_offerta,
		ID_ricambio,
		prezzo_unitario_originale,
		prezzo_unitario_sconto,
		pezzi,
		aggiunto
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
/*ordini_elenco*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		codice_ordine,
		ID_offerta,
		data_ordine,
		data_ETA,
		costo_spedizione,
		totale_ordine,
		sconto,
		prezzo_finale,
		gestione_spedizione,
		stato
	FROM ordini_elenco;

	DROP TABLE ordini_elenco;

	CREATE TABLE IF NOT EXISTS [ordini_elenco] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[codice_ordine]       VARCHAR (255)   NOT NULL,
		[ID_offerta]          INT             ,
		[data_ordine]         DATE            NOT NULL,
		[data_ETA]            DATE            NULL,
		[costo_spedizione]    DECIMAL (19, 4) NULL,
		[totale_ordine]       DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[sconto]              DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_finale]       DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[gestione_spedizione] SMALLINT        NULL,
		[stato]               SMALLINT        DEFAULT ((0)) NOT NULL,
		
		UNIQUE ([codice_ordine] ASC),
		CONSTRAINT [FK_oridini_elenco_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id])
	);
	CREATE INDEX search_ordini_elenco ON ordini_elenco (stato, ID_offerta);

	INSERT INTO ordini_elenco
		(     
			Id,
			codice_ordine,
			ID_offerta,
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
		Id,
		codice_ordine,
		ID_offerta,
		data_ordine,
		data_ETA,
		costo_spedizione,
		totale_ordine,
		sconto,
		prezzo_finale,
		gestione_spedizione,
		stato
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
/*ordine_pezzi*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		ID_ordine,
		ID_ricambio,
		prezzo_unitario_originale,
		prezzo_unitario_sconto,
		pezzi,
		ETA
	FROM ordine_pezzi;

	DROP TABLE ordine_pezzi;

	CREATE TABLE IF NOT EXISTS [ordine_pezzi] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_ordine]                 INT             NOT NULL,
		[ID_ricambio]               INT             NOT NULL,
		[prezzo_unitario_originale] DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_unitario_sconto]    DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[pezzi]                     REAL            DEFAULT ((0)) NOT NULL,
		[ETA]                       DATE            NOT NULL,
		
		CONSTRAINT [FK_ordine_pezzi_To_pezzi_ricambi] FOREIGN KEY ([ID_ricambio]) REFERENCES [pezzi_ricambi] ([Id]),
		CONSTRAINT [FK_ordine_pezzi_To_ordini_elenco] FOREIGN KEY ([ID_ordine]) REFERENCES [ordini_elenco] ([Id])
	);
	CREATE INDEX search_ordine_pezzi ON ordine_pezzi (ID_ordine, ID_ricambio);

	INSERT INTO ordine_pezzi
		(     
			Id,
			ID_ordine,
			ID_ricambio,
			prezzo_unitario_originale,
			prezzo_unitario_sconto,
			pezzi,
			ETA
		)
	SELECT
		Id,
		ID_ordine,
		ID_ricambio,
		prezzo_unitario_originale,
		prezzo_unitario_sconto,
		pezzi,
		ETA
	FROM temp;

	DROP TABLE IF EXISTS temp;
	
	UPDATE  informazioni SET versione=2 WHERE id=1;