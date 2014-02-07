To use MMBot, you will need to place two files in the build directory (the folder with MMBot.exe, usually MMBot\bin\Release):
global.ini:
opname=the bot operator's nickname on IRC (usually your nickname)
password=the password you can use to gain control of the bot

MMBot.ini: each group in this file represents a network that the bot can connect to, for example:
[BadnikNET] ; the group name will be used as the network's name in the network selection dialog
servers=irc.badnik.net:6667 ; the host and port to connect to, port is optional and defaults to 6667, multiple servers can be separated by spaces
favchans=#SF94 ; a space-separated list of channels to automatically join, if a channel requires a keyword, put a comma and the keyword directly after the channel name
autoconnect=true ; if this true, the bot will automatically connect to the network when it starts
;usessl=true ; if true, the bot will connect via SSL (doesn't usually work on non-windows)

If you plan on using the HTTP server, you will have to edit MMBot\HttpServer.cs and copy the contents of the "HTTPFiles" folder to an "http" subdirectory of the build directory.

Modules should be installed in a "Modules" subfolder of the build directory, all subfolders of that folder will be searched for any XML files with a matching DLL file.
If a module uses any extra DLLs that MMBot does not (such as LinqToTwitter.dll in the Twitter module), the DLL will have to be copied into the build directory, or the module will fail to load.

If you want to use the Unicode module, you will have to download the file "ucd.all.flat.xml" from http://www.unicode.org/Public/UCD/latest/ucdxml/ucd.all.flat.zip and place it in the Unicode module's build folder.

If you don't have iTunes installed, you should probably just delete the MMBotiTunes project.