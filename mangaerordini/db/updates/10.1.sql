/*pezzi_ricambi*/
/*BUG FIX*/
	UPDATE  [pezzi_ricambi] SET [active]=1 WHERE [deleted]=0;
	
/*Update version*/
	UPDATE  informazioni SET versione=10.1 WHERE id=1;