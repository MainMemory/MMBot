MMBot
=====
MMBot is a modular IRC bot written in C#.

MMBot is supported on both Windows and Linux (via Mono), but Mono support has not been tested in a while and may be buggy.

Getting up and running
----------------------
#### Required Tools
* You'll need to have Visual Studio 2013 for Windows Desktop installed. You can use any 
   **desktop** version of Visual Studio 2013, including the free Express version: [Visual Studio 2013 Express for Windows Desktop](http://www.microsoft.com/en-us/download/details.aspx?id=40787)
* The below instructions are written for Windows platforms. If you intend to run MMBot in a Linux environment, you will need Mono. Please note that MMBot has not been tested in Mono in a *long, long time*, and things may be broken.

#### Compiling MMBot
1. Load the project into Visual Studio by double-clicking on the `MMBot.sln` file.

2. If you wish to use the MMBotTwitter module, you will need to modify `MMBotTwitter/TwitterModule.cs` and insert your Consumer API Key obtained from [dev.twitter.com](http://dev.twitter.com/).

3. If you wish to build MMBot and all of its modules, make sure you've completed Step 2 above and then build the entire solution (Ctrl-Shift-B).

4. If you wish to choose which modules get included in your MMBot install, compile the projects separately.

#### Configuring MMBot
1. After MMBot finishes compiling, navigate to its build directory, which is by default in `MMBot/bin/Release/`.

2. Create two INI files, `global.ini` and `MMBot.ini`.

3. Open `global.ini` in your text editor, paste the contents below, and modify it with your information. Make sure you are using Windows (CRLF) line endings.

```
opname=MainMemory
; This is where the BotOp's nick goes. (probably yours)

password=examplePassword
; This is where the password for an user to be recognised as a BotOp goes.
```

4. Open `MMBot.ini` in your text editor, paste the contents below, and modify it with your information. Make sure you are using Windows (CRLF) line endings.

```
[BadnikNET]
; The text inside the brackets will be treated as the network name displayed in MMBot's GUI.

servers=irc.badnik.net:6697
; This is the host and port that MMBot will connect to. The port is optional. Specify multiple servers with spaces.

favchans=#SF94
; This is a list of channels that MMBot will automatically join. If the channel has a keyword (MODE +k), then put the keyword after the channel name, separated by a comma.

autoconnect=true
; If this is set to true, then MMBot will automatically connect to this network on startup.

usessl=true
; If this is set to true, MMBot will connect via SSL. Don't try using this if you're using Mono.
```

#### Configuring MMBot's httpd
1. Run the command `netsh http add urlacl url=http://your-hostname-here:80/ user=DOMAIN\User` in a command prompt shell with administrative access.

2. Forward TCP Port 80 in your router settings. The instructions vary for each router - try performing a Google search for instructions pertaining to your specific network router.

 - On some Windows versions, Windows Firewall may have to be turned off for external connections to work properly, depending on your system configuration. Simplying creating an exception appears to be ineffective in some cases.

 - Please note that some applications like Skype and TeamViewer often occupy Port 80, but also have options to turn that behaviour off.

#### Configuring MMBot's modules
1. Copy `DLLs/LinqToTwitter.dll` to `bin/MMBot/Release/LinqToTwitter.dll`. This is required in order for MMBotTwitter to function.

 - If you did not compile MMBotTwitter and do not want its functionality, then you may omit this step.

 - If you fail to copy `LinqToTwitter.dll`, MMBotTwitter will silently fail to load. Please note that MMBot **does not** display any exceptions if a module fails to load.

2. Download [ucd.all.flat.zip](http://www.unicode.org/Public/UCD/latest/ucdxml/ucd.all.flat.zip) and extract the file `ucd.all.flat.xml` inside it to `MMBot/bin/Release/Modules/ucd.all.flat.xml`. This is required in order for MMBotUnicode to function.

3. MMBotiTunes requires iTunes in order to function. If you compiled the MMBotiTunes module, install [iTunes](http://www.apple.com/itunes/download/).