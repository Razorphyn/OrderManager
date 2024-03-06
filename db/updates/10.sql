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
		[uniqueness]	SMALLINT		NULL,
		CONSTRAINT [ui_pezzi_ricambi_nome_codice_uniqueness] UNIQUE ([nome], [codice], [uniqueness], [active]),
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
			active
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
		
/*Update version*/
	UPDATE  informazioni SET versione=10 WHERE id=1;