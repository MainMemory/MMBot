function setupTheme(theme)
{
	if (theme == "english")
		englishChangeColor();
}
englishColor = -1;
function englishChangeColor()
{
	englishColor++;
	englishColor %= 15;
	document.getElementById("siteMenu").style.backgroundColor = englishBackColors[englishColorOrder1[englishColor] - 1];
	document.getElementById("siteContent").style.backgroundColor = englishBackColors[englishColorOrder2[englishColor] - 1];
	setTimeout("englishChangeColor()", 100);
}
englishBackColors = [ "#ffea00", "#4040fd", "#df0000", "#a357ff", "#ff6000", "#008140", "#950015", "#000000", "#ffea00", "#4040fd", "#df0000", "#a357ff", "#ff6000", "#008140", "#950015" ];
englishColorOrder1 = [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 ];
englishColorOrder2 = [ 11, 12, 13, 14, 15, 9, 10, 8, 6, 7, 1, 2, 3, 4, 5 ];