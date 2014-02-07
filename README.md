MMBot
=======

MMBot is an IRC bot written in C#.

Using MMBot
------------------------
#### global.ini
<code>global.ini</code> belongs in MMBot's build directory and should contain the following:

<code>opname=MainMemory</code> This is where the BotOp's nick goes. (probably yours)

<code>password=sonicmike2</code> This is where the password for an user to be recognised as a BotOp goes.

#### MMBot.ini
<code>MMBot.ini</code> belongs in MMBot's build directory and should contain the following:

<code>[BadnikNET]</code> The text inside the brackets will be treated as the network name displayed in MMBot's GUI.

<code>servers=irc.badnik.net:6667</code> This is the host and port that MMBot will connect to. The port is optional. Specify multiple servers with spaces.

<code>favchans=#SF94</code> This is a list of channels that MMBot will automatically join. If the channel has a keyword (MODE +k), then put the keyword after the channel name, separated by a comma.

<code>autoconnect=true</code> If this is set to true, then MMBot will automatically connect to this network on startup.

<code>usessl=false</code> If this is set to true, MMBot will connect via SSL. Don't try using this if you're using Mono.

#### http/
The files in this subfolder are required for MMBot's Web UI.

The httpd (and by extension, the Web UI) is disabled by default, remove the return statement to re-enable it.

**Please note that Windows Firewall must be turned off for the built-in httpd to function. Simply allowing MMBot in the Windows Firewall preferences does not work. You must also have port 80 forwarded and open.**

Also, please note that apps like Skype and TeamViewer often occupy Port 80, but also have options to turn that behaviour off.

#### Modules/
All modules go into the Modules/ subfolder in MMBot's build directory.

Each module must also have a matching XML file.

Please note that some modules, like MMBotTwitter mentioned below, require external DLLs to be present in **MMBot's build directory**, and not the Modules/ subfolder.

If you fail to copy the required external DLLs, the module will fail to load. **MMBot will not display exceptions during module loads.**

#### MMBotTwitter
MMBotTwitter requires the external DLL LinqToTwitter.dll to be present in **MMBot's build directory**, and not the Modules/ subfolder.

The build script erroneously copies it to the Modules/ subfolder as of this writing.

#### MMBotUnicode
MMBotUnicode depends on the file "ucd.all.flat.xml" which can be acquired from http://www.unicode.org/Public/UCD/latest/ucdxml/ucd.all.flat.zip

**ucd.all.flat.xml belongs in the Modules/ subfolder.**

#### MMBotiTunes
MMBotiTunes requires iTunes to be installed.