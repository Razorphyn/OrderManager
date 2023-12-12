	
/*offerte_pezzi*/
	DROP TABLE IF EXISTS temp_table;
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM offerte_pezzi;

	DROP TABLE offerte_pezzi;
	
	CREATE TABLE IF NOT EXISTS [offerte_pezzi] (
		[Id]        				INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_offerta]                INT             NOT NULL,
		[ID_ricambio]               INT             NOT NULL,
		[prezzo_unitario_originale] DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_unitario_sconto]    DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[pezzi]                     REAL            DEFAULT ((0)) NOT NULL,
		[pezzi_aggiunti]			INT        		DEFAULT ((0)) NOT NULL,
		CONSTRAINT [ui_offerte_pezzi] UNIQUE ([ID_offerta], [ID_ricambio]),
		CONSTRAINT [FK_offerte_pezzi_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id]),
		CONSTRAINT [FK_offerte_pezzi_To_pezzi_ricambi] FOREIGN KEY ([ID_ricambio]) REFERENCES [pezzi_ricambi] ([Id])
	);
	CREATE INDEX search_offerte_pezzi ON offerte_pezzi (ID_offerta, ID_ricambio);

	INSERT OR IGNORE INTO offerte_pezzi
		(     
			Id,
			ID_offerta,
			ID_ricambio,
			prezzo_unitario_originale,
			prezzo_unitario_sconto,
			pezzi
		)
	SELECT
			Id,
			ID_offerta,
			ID_ricambio,
			prezzo_unitario_originale,
			prezzo_unitario_sconto,
			pezzi
	FROM temp_table;
	
	UPDATE  [offerte_pezzi] 
		SET [pezzi_aggiunti] = 
				(SELECT 
						IIF(SUM([ordine_pezzi].[pezzi]) IS NULL, 0, SUM([ordine_pezzi].[pezzi]))
					FROM [ordine_pezzi] 
					JOIN [ordini_elenco] ON
						[ordini_elenco].[Id] = [ordine_pezzi].[ID_ordine]
					WHERE 	[ordini_elenco].[ID_offerta] IS NOT NULL 
							AND [ordini_elenco].[ID_offerta] = [offerte_pezzi].[ID_offerta]
							AND [ordine_pezzi].[ID_ricambio] = [offerte_pezzi].[ID_ricambio]
				);
	
	DROP TABLE IF EXISTS temp_table;
	
/*ordine_pezzi*/
	DROP TABLE IF EXISTS temp_table;
	CREATE TEMPORARY TABLE temp_table AS
		SELECT 
			*
	FROM ordine_pezzi;

	DROP TABLE ordine_pezzi;
	
	CREATE TABLE IF NOT EXISTS [ordine_pezzi] (
		[Id]        				INTEGER	PRIMARY KEY	AUTOINCREMENT NOT NULL,
		[ID_ordine]                 INT             NOT NULL,
		[ID_ricambio]               INT             NOT NULL,
		[prezzo_unitario_originale] DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_unitario_sconto]    DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[pezzi]                     REAL            DEFAULT ((0)) NOT NULL,
		[ETA]                       DATE            NOT NULL,
		[Outside_Offer] 			BOOLEAN 		DEFAULT ((0)) NOT NULL,
		CONSTRAINT [ui_ordine_pezzi] UNIQUE ([ID_ordine], [ID_ricambio], [ETA]),
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
	FROM temp_table;
	
	DROP TABLE IF EXISTS temp_table;
	
/*Update version*/
	UPDATE  informazioni SET versione=8 WHERE id=1;