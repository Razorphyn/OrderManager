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
		[ID_offerta]          INT             NULL,
		[ID_cliente]          INT             NULL,
		[ID_riferimento]      INT             NULL,
		[data_ordine]         DATE            NOT NULL,
		[data_ETA]            DATE            NULL,
		[costo_spedizione]    DECIMAL (19, 4) NULL,
		[totale_ordine]       DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[sconto]              DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[prezzo_finale]       DECIMAL (19, 4) DEFAULT ((0)) NOT NULL,
		[gestione_spedizione] SMALLINT        NULL,
		[stato]               SMALLINT        DEFAULT ((0)) NOT NULL,
		
		UNIQUE ([codice_ordine] ASC),
		CONSTRAINT [FK_oridini_elenco_To_offerte_elenco] FOREIGN KEY ([ID_offerta]) REFERENCES [offerte_elenco] ([Id]),
		CONSTRAINT [FK_oridini_elenco_To_clienti_elenco] FOREIGN KEY ([ID_cliente]) REFERENCES [clienti_elenco] ([Id]),
		CONSTRAINT [FK_oridini_elenco_To_clienti_riferimenti] FOREIGN KEY ([ID_riferimento]) REFERENCES [clienti_riferimenti] ([Id])
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
	
	ALTER TABLE ordine_pezzi  ADD Outside_Offer BOOLEAN DEFAULT ((0)) NOT NULL;
	
	UPDATE  informazioni SET versione=3 WHERE id=1;
	