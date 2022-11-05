BEGIN TRANSACTION;
	DROP TABLE IF EXISTS temp_value;
	
	UPDATE informazioni SET versione=4 where id=1;
	
	CREATE TEMPORARY TABLE temp_value AS
		SELECT 
			Id, 
			ID_ordine, 
			ID_ricambio 
		FROM [ordine_pezzi] 
		GROUP BY ID_ordine, ID_ricambio 
		HAVING COUNT(Id) >1;
		

	/*ordine_pezzi*/
	CREATE TEMPORARY TABLE temp AS
	SELECT 
		Id,
		ID_ordine,
		ID_ricambio,
		prezzo_unitario_originale,
		prezzo_unitario_sconto,
		pezzi,
		ETA,
		Outside_Offer
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
		[Outside_Offer] 			BOOLEAN 		DEFAULT ((0)) NOT NULL,
		
		CONSTRAINT [ui_ordine_pezzi] UNIQUE ([ID_ordine], [ID_ricambio]),
		CONSTRAINT [FK_ordine_pezzi_To_pezzi_ricambi] FOREIGN KEY ([ID_ricambio]) REFERENCES [pezzi_ricambi] ([Id]),
		CONSTRAINT [FK_ordine_pezzi_To_ordini_elenco] FOREIGN KEY ([ID_ordine]) REFERENCES [ordini_elenco] ([Id])
	);
	CREATE INDEX search_ordine_pezzi ON ordine_pezzi (ID_ordine, ID_ricambio);

	INSERT OR IGNORE INTO ordine_pezzi
		(     
			Id,
			ID_ordine,
			ID_ricambio,
			prezzo_unitario_originale,
			prezzo_unitario_sconto,
			pezzi,
			ETA,
			Outside_Offer
		)
	SELECT
		Id,
		ID_ordine,
		ID_ricambio,
		prezzo_unitario_originale,
		prezzo_unitario_sconto,
		pezzi,
		ETA,
		Outside_Offer
	FROM temp;

	DROP TABLE IF EXISTS temp;
END;
	
SELECT "Attenzione: Verificare ordine con ID: " || [ID_ordine] || ". ID ricambio: "|| [ID_ricambio] || "duplicato ed eliminato (ID riga: " || Id || ")."  AS retentry FROM temp.temp_value;
COMMIT;



