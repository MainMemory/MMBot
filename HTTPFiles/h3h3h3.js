function trollify(troll)
{
	if (troll == 'none') return;
	if (TrollColors[troll]) document.getElementsByTagName('body')[0].style.color = TrollColors[troll];
	checkelem(document, troll);
}
function checkelem(element, troll)
{
	for (var i=0, max=element.childNodes.length; i < max; i++)
	{
		if (element.childNodes[i].nodeType == 3)
		{
			if (element.childNodes[i].nodeValue.length > 2)
				element.childNodes[i].nodeValue = Trolls[troll](element.childNodes[i].nodeValue);
		}
		else
			checkelem(element.childNodes[i], troll);
	}
}
REGEX_PUNCTUATION = /[',\.!\?"]/g;

TrollColors = {
	'AA': '#A10000',
	'AT': '#A15000',
	'TA': '#A1A100',
	'CG': '#626262',
	'AC': '#416600',
	'GA': '#008141',
	'GC': '#008282',
	'AG': '#005682',
	'CT': '#000056',
	'TC': '#2B0057',
	'CA': '#6A006A',
	'CC': '#77003C',
	'UU': '#929292',
	'uu': '#323232'
}
Trolls = {
	"AA": function(s){ return s.toLowerCase().replace(REGEX_PUNCTUATION, '').replace(/o/g, '0'); },
	"AT": function(s){ return s.toUpperCase().replace(/[\.\?!]/g, ',').replace(/^.|(,\s+.)/g, function($1) { return $1.toLowerCase(); }); },
	"CG": function(s){ return s.toUpperCase(); },
	"GC": function(s){ return s.toUpperCase().replace(/[',"]/g, '').replace(/(\.\.+)/g, '.$1').replace(/\.(?!\.)/g, '').replace(/A/g, '4').replace(/I/g, '1').replace(/E/g, '3'); },
	"GA": function(s){ return s.toLowerCase().replace(REGEX_PUNCTUATION, '').replace(/^.|(\s.)/g, function($1) { return $1.toUpperCase(); }); },
	"TC": function(s){
		var ns = "";
		var flip = true;
		for (var i = 0; i < s.length; i++){
			var ch = s.substr(i, 1);
			ns += flip ? ch.toUpperCase() : ch.toLowerCase();
			flip = !flip;
		}
		return ns;
	},
	"TA": function(s){ return s.toLowerCase().replace(/s/g, '2').replace(/i/g, 'ii').replace(/too?\b/g, 'two'); },
	"AC": function(s){ return ":33 < " + s.toLowerCase().replace(/per/g, "purr").replace(/pau/g, "paw").replace(/pon/g, "pawn").replace(/ee/g, '33').replace(/:([!-~])/gi, ':$1$1'); },
	"AG": function(s){ return s.replace(/([a-z])\1{2,}/gi, function($1){
		var ch = $1.substr(0, 1);
		return ch + ch + ch + ch + ch + ch + ch + ch;
	}).replace(/B/gi, '8').replace(/ate/gi, '8').replace(/([:;]-?[\(\[\)\]])/g, function($1){
		return ":::" + $1;
	}); },
	"CT": function(s){ return "D --> " + s.replace(/x/gi, '%').replace(/cross/gi, '%').replace(/\b[.!\?]/g, '').replace(/(loo|ool)/gi, function($1){
		return $1.replace(/l/gi, '1').replace(/o/gi, '0');
	}); },
	"CC": function(s){ return s.replace(/h/gi, ')(').replace(/E/g, '-E'); },
	"CA": function(s){ return s.toLowerCase().replace(REGEX_PUNCTUATION, '').replace(/v/g, 'vv').replace(/w/g, 'ww').replace(/ing/g, 'in'); },
	"UU": function(s){ return s.toLowerCase().replace(/u/g, 'U'); },
	"uu": function(s){ return s.toUpperCase().replace(/U/g, 'u').replace(/\,/g, '.'); },
	"SBaHJ": function(s){
		var split = s.split(" ");
		for (var i = 0; i < split.length; i++)
		{
			split[i] = split[i].toLowerCase();
			if (Math.random() * 99 + 1 < 25)
				split[i] = misspeller(split[i]);
			if (Math.random() * 99 + 1 < 10)
				split[i] = split[i].toUpperCase();
		}
		return split.join(" ");
	}
}
var kbloc = [ "1234567890-=".split(""),
		"qwertyuiop[]".split(""),
		"asdfghjkl:;'".split(""),
		"zxcvbnm,.>/?".split("")];

var kbdict;
var sounddict = { 'a': "e", 'b': "d", 'c': "k", 'd': "g", 'e': "eh",
             'f': "ph", 'g': "j", 'h': "h", 'i': "ai", 'j': "ge",
             'k': "c", 'l': "ll", 'm': "n", 'n': "m", 'o': "oa",
             'p': "b", 'q': "kw", 'r': "ar", 's': "ss", 't': "d",
             'u': "you", 'v': "w", 'w': "wn", 'x': "cks", 'y': "uy", 'z': "s" };
function misspeller(word)
{
	if (!kbdict)
	{
		kbdict = new Array();
		for (var i = 0; i < kbloc.length; i++)
			for (var j = 0; j < kbloc[i].length; j++)
				kbdict[kbloc[i][j]] = [ i, j ];
	}
	var num;
	if (word.length <= 6)
		num = 1;
	else
		num = Math.floor(Math.random() * 2 + 1);
	var wordseq = new Array();
	var nums2 = new Array();
	var b2 = Math.floor(Math.random() * word.length);
	for (var i = 0; i < num; i++)
	{
		while (nums2[b2])
			b2 = Math.floor(Math.random() * word.length);
		wordseq[i] = b2;
		nums2[b2] = true;
	}
	var funcs = [ mistype, transpose, randomletter, randomreplace, soundalike ];
	var func = funcs[Math.floor(Math.random() * funcs.length)];
	for (var i = 0; i < wordseq.length; i++)
		word = func(word, wordseq[i]);
	return word;
}

function mistype(string, i)
{
	var l = string[i];
	if (!kbdict[l])
		return string;
	var lpos = kbdict[l];
	var newpos = lpos;
	while (newpos[0] == lpos[0] & newpos[1] == lpos[1])
		newpos = [ModNeg(lpos[0] + Math.floor(Math.random() * 3 - 1), kbloc.length - 1),
			ModNeg(lpos[1] + Math.floor(Math.random() * 3 - 1), kbloc[0].length - 1)];
	return string.substring(0, i) + kbloc[newpos[0]][newpos[1]] + string.substring(i + 1);
}

function transpose(string, i)
{
	var j = ModNeg(i + (Math.floor(Math.random() * 2) == 0 ? -1 : 1), string.length - 1);
	var l = string.split("");
	l[i] = string.charAt(j);
	l[j] = string.charAt(i);
	return l.join("");
}

function randomletter(string, i)
{
	return string.substring(0, i + 1) + "abcdefghijklmnopqrstuvwxyz".charAt(Math.floor(Math.random() * 26)) + string.substring(i + 1);
}

function randomreplace(string, i)
{
	return string.substring(0, i) + "abcdefghijklmnopqrstuvwxyz".charAt(Math.floor(Math.random() * 26)) + string.substring(i + 1);
}

function soundalike(string, i)
{
	if (!sounddict[string.charAt(i)]) return string;
	var c = sounddict[string.charAt(i)];
	return string.substring(0, i) + c + string.substring(i + 1);
}

function ModNeg(value, max)
{
	var result = value;
	if (result > max)
		result = max + (max - result);
	if (result < 0)
		result = -result;
	return result;
}