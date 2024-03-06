DROP TABLE IF EXISTS temp_table;

CREATE TEMPORARY TABLE temp_table AS
	SELECT 
		*
	FROM informazioni;

	DROP TABLE informazioni;
	
	CREATE TABLE IF NOT EXISTS [informazioni] (
		[Id]        INTEGER	PRIMARY KEY	NOT NULL,
		[versione]  DECIMAL DEFAULT ((6)) NOT NULL
	);

	INSERT OR IGNORE INTO informazioni
		(     
			Id,
			versione
		)
	SELECT
		Id,
		versione
	FROM temp_table;
	
DROP TABLE IF EXISTS temp_table;


/*clienti_sedi*/
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
		CONSTRAINT [ui_clienti_sedi_ID_clienti_numero] UNIQUE ([numero] ASC),
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
			citta
		)
	SELECT
		Id,
		ID_cliente,
		numero,
		stato,
		provincia,
		citta
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
/*fix clienti_elenco*/
	CREATE TEMPORARY TABLE temp_table AS
	SELECT 
		*
	FROM clienti_elenco_temp;

	DROP TABLE clienti_elenco;
	DROP TABLE clienti_elenco_temp;
	
	CREATE TABLE IF NOT EXISTS [clienti_elenco] (
		[Id]        INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[nome]      VARCHAR (255) 	NOT NULL,
		CONSTRAINT [ui_clienti_elenco_nome] UNIQUE ([nome] ASC)
	);

	INSERT OR IGNORE INTO clienti_elenco
		(     
			Id,
			nome
		)
	SELECT
		Id,
		nome
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
UPDATE  informazioni SET versione=6.1 WHERE id=1;