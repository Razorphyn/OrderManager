UPDATE [ordine_pezzi]
		SET [ETA] = OE.data_ETA
		FROM (SELECT [data_ETA], [id] FROM [ordini_elenco] ) as OE
		WHERE 
			[ordine_pezzi].Id IN( SELECT [Id] FROM [ordine_pezzi] where [ETA] LIKE "Razorphyn%" ) 
			AND OE.Id = [ID_ordine];

/*fornitori*/
	DROP TABLE IF EXISTS temp_table;
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM fornitori;

	DROP TABLE fornitori;
	
	CREATE TABLE IF NOT EXISTS [fornitori] (
		[Id]        	INTEGER			PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome] 			VARCHAR (255) 	NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[active]	SMALLINT		NULL,
		UNIQUE ([nome] , [active])
	);

	INSERT OR IGNORE INTO fornitori
		(     
			Id,
			nome,
			active
		)
	SELECT
		Id,
		nome,
		uniqueness
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
/*clienti_elenco*/
	DROP TABLE IF EXISTS temp_table;
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM clienti_elenco;

	DROP TABLE clienti_elenco;
	
	CREATE TABLE IF NOT EXISTS [clienti_elenco] (
		[Id]        	INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome]      	VARCHAR (255) 	NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[active]	SMALLINT		NULL,
		CONSTRAINT [ui_clienti_elenco_nome_uniqueness] UNIQUE ([nome], [active])
	);

	INSERT OR IGNORE INTO clienti_elenco
		(     
			Id,
			nome,
			active
		)
	SELECT
		Id,
		nome,
		uniqueness
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;

/*clienti_sedi*/
	DROP TABLE IF EXISTS temp_table;
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM clienti_sedi;

	DROP TABLE clienti_sedi;
	
	CREATE TABLE IF NOT EXISTS [clienti_sedi] (
		[Id]        	INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_cliente]    INT				NOT NULL,
		[numero]    	INT				NULL,
		[stato]     	VARCHAR (255) 	NOT NULL,
		[provincia] 	VARCHAR (255) 	NOT NULL,
		[citta]     	VARCHAR (255) 	NOT NULL,
		[deleted]		INT				NOT NULL DEFAULT 0,
		[active]		SMALLINT		NULL,
		CONSTRAINT [ui_clienti_sedi_ID_clienti_numero_uniqueness] UNIQUE ([numero], [active]),
		CONSTRAINT [FK_clienti_sedi_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id])
	);
	CREATE INDEX search_clienti_sedi_ID_sede ON clienti_sedi (Id);

	INSERT OR IGNORE INTO clienti_sedi
		(     
			Id,
			ID_cliente,
			numero,
			stato,
			provincia,
			citta,
			active
		)
	SELECT
		Id,
		ID_cliente,
		numero,
		stato,
		provincia,
		citta,
		uniqueness
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;

/*clienti_riferimenti*/
	DROP TABLE IF EXISTS temp_table;
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
		[deleted]		INT				NOT NULL DEFAULT 0,
		[active]	SMALLINT		NULL,
		UNIQUE ([ID_cliente], [nome], [active]),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_clienti_riferimenti_To_clienti_sedi] FOREIGN KEY ([ID_sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_clienti_riferimenti_ID_cliente ON clienti_riferimenti (ID_cliente);

	INSERT OR IGNORE INTO clienti_riferimenti
		(     
			Id,
			ID_cliente,
			ID_sede,
			nome,
			mail,
			telefono,
			active
		)
	SELECT
		Id,
		ID_cliente,
		ID_sede,
		nome,
		mail,
		telefono,
		uniqueness
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
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
		[deleted]		SMALLINT		NOT NULL DEFAULT 0,
		[active]		SMALLINT		NULL,
		UNIQUE ([seriale],[active]),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_clienti_macchine_To_clienti_elenco] FOREIGN KEY ([ID_Sede]) REFERENCES [clienti_sedi] ([Id])
	);
	CREATE INDEX search_clienti_macchine_ID_cliente ON clienti_macchine (Id, ID_cliente,ID_Sede);

	INSERT OR IGNORE INTO clienti_macchine
		(     
			Id,
			modello,
			codice,
			seriale,
			ID_cliente,
			ID_sede,
			active
		)
	SELECT
			Id,
			modello,
			codice,
			seriale,
			ID_cliente,
			ID_sede,
			uniqueness
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
	
/*pezzi_ricambi*/
	DROP TABLE IF EXISTS temp_table;
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM pezzi_ricambi;

	DROP TABLE pezzi_ricambi;
	
	CREATE TABLE IF NOT EXISTS [pezzi_ricambi] (
		[Id]        	INTEGER	PRIMARY KEY AUTOINCREMENT NOT NULL,
		[nome]			VARCHAR (255)   NOT NULL,
		[codice]		VARCHAR (20)    NOT NULL,
		[descrizione]	VARCHAR (8000)  NOT NULL,
		[prezzo]		DECIMAL (19, 4) NULL,
		[ID_fornitore]	INT             NOT NULL,
		[ID_macchina]	INT             NULL,
		[deleted]		SMALLINT		NOT NULL DEFAULT 0,
		[active]		SMALLINT		NULL,
		CONSTRAINT [ui_pezzi_ricambi_nome_codice_uniqueness] UNIQUE ([nome], [codice], [active]),
		CONSTRAINT [FK_pezzi_ricambi_To_fornitori] FOREIGN KEY ([ID_fornitore]) REFERENCES [fornitori] ([Id]),
		CONSTRAINT [FK_pezzi_ricambi_To_clienti_macchine] FOREIGN KEY ([ID_macchina]) REFERENCES [clienti_macchine] ([Id])
	);
	CREATE INDEX search_pezzi_ricambi ON pezzi_ricambi (ID_macchina, ID_fornitore);

	INSERT OR IGNORE INTO pezzi_ricambi
		(     
			Id,
			nome,
			codice,
			descrizione,
			prezzo,
			ID_fornitore,
			ID_macchina,
			deleted,
			active
		)
	SELECT
			Id,
			nome,
			codice,
			descrizione,
			prezzo,
			ID_fornitore,
			ID_macchina,
			deleted,
			uniqueness
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
/*Update version*/
	UPDATE  informazioni SET versione=9 WHERE id=1;