![GitHub release](https://img.shields.io/github/release/Foxlider/DiVA.svg?style=flat-square)
![GitHub](https://img.shields.io/github/license/Foxlider/DiVA.svg?style=flat-square)
![AppVeyor](https://img.shields.io/appveyor/ci/Foxlider/DiVA.svg?logo=appveyor&style=flat-square)
[![Build Status](https://dev.azure.com/keelah/DiVA/_apis/build/status/Foxlider.DiVA?branchName=master)](https://dev.azure.com/keelah/DiVA/_build/latest?definitionId=6&branchName=master)

# [ DiVA ]
DiVA Discord Bot created to replace FoxliBot 
Written in C# - .NET Core 3.0 

# [ Installation ]
	- Clone Project 

But honestly, why would you run this bot if mine is already running ? 

# [ Use ]
Click on the link below to make the bot join a server :
 https://discordapp.com/oauth2/authorize?&client_id=538306821333712916&scope=bot


# [ Commands ]
##### Common
```
..say           Echos a message.  
..hello         Says hello  
..userinfo      Displays information about a user  
..help          Prints the help of available commands  
..version       Check the bot's version  
..choose        If you want a robot to choose for you  
..roll          Rolls a dice in NdN format  
..pvroll        Secretly rolls a dice  
..status        Change the status of the bot  
```

##### Interactive
```
..ping          Ping command
..poll          Run a poll with reactions.
```

##### Audio
```
..play          Requests a song to be played
..test          Performs a sound test
..stream        Streams a livestream URL
..audiosay      Says something
..audiolsay     Says something in the given language
..quit          Quit a channel
..clear         Clears all songs in queue
..stop          Stops the playback and disconnect
..next          Skips current song
..songlist      Lists current songs
..nowplaying    Prints current playing song
```


THE END


# [ Changelog ]

## [ Version 1 ]  
###   [ v1.0 ]
 - Initial release  

###   [ v1.1 ]  
Fixed :  
  - Fixed version not displaying correctly  
  - Some minor code fixes  
  - Some minor typo fixes  

###   [ v1.3 ]  
Added :  
  - Direct Message logging  
  - SentTo command  
  - Poll command  

Fixed :  
  - Cleaned up some code  
  - Fixed typo  

###   [ v1.4 ]  
Added :  
  - Better Logging  
  - Log commands  

Fixed :
  - Audio not working  

###   [ v1.5 ] 
Added :
 - Unit tests for appveyor
 - Fixed gitignore

Fixed :
 - Commands restrictions

###   [ v1.6 ] 
Added :
 - Volume commands

Fixed :
 - Audio quality
 - Multiple stream audio
 - Stored musics quality

###   [ v1.7 ]
Added :
 - Enhanced Error handling
 - Logger improvements

Fixed :
 - Fixed Auto-deleting commands hanging
 - Code cleanup
 - Unknown command too invasive
 - Some tests not working

###   [ v1.8 ]
###### Added :
 - TTS Commands : DiVA can now speak in voice channels
 - Supported voices : en-US / fr-FR
 - Code cleanup

Fixed :
 - Fixed Message Delete blocking threads
 - URLs bringing to the wrong page
 - Some XML doc being wrong

###   [ v1.9 ]
Added :
 - Updated youtube-dl

Fixed :
 - Crash on empty log
 - Audio not downloading
 - Fixed tome text output
 - Fixed some commands actions

###   [ v2.0 ]
Added :
 - Updated youtube-dl
 - Azure build checks

Fixed :
 - queue fonciton removing the current song from queue
 - Exception when using skip if no song is playing
 - Exception on some logging messages
 - Handling youtube-dl errors