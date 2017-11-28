**ATTENTION: If you are already running this project you must update to at least v1.0.4 which corrects the problem where the date rolls back to 1997.  The Trimble Thunderbolt will have stopped reporting the correct time from July 30th 2017 for any hardware/software which reads its data unless it is aware of the rollover.**

**If you don't know how to use GitHub and just want to download the latest development code, visit [https://github.com/m1dst/Trimble-Thunderbolt-Monitor/archive/master.zip](https://github.com/m1dst/Trimble-Thunderbolt-Monitor/archive/master.zip).  You can always redownload the latest version with this link.  If you want an official releases, click the [releases link](https://github.com/m1dst/Trimble-Thunderbolt-Monitor/releases).**

Thunderbolt Monitor is a stand-alone microprocessor-controlled LCD specifically for the [Trimble Thunderbolt](http://www.trimble.com/timing/thunderbolt-e.aspx) disciplined clock, providing a comprehensive indication of the Thunderbolt's status, modes, and alarm conditions written in C# (NETMF).

Data packets appearing on Thunderbolt's serial port are in Trimble Standard Interface Protocol (TSIP), not NMEA sentences, so this display will not work with NMEA GPS units, such as Trimble's Jupiter or any other GPS.

Ideal for amateur radio applications, Thunderbolt Display shows Time Of Day (UTC) to assist with logging contacts, and also calculates Maidenhead Grid Locator Square from the current latitude and longitude.

The software runs on a [Netduino](https://www.wildernesslabs.co/) or [GHI FEZ Lemur](http://ghielectronics.com/) which is a .NET version of the popular Arduino platform.  It also requires a shield to make connecting it up to the microcontroller much cleaner.  I have designed the shield to be compatible with both Netduino and Arduino in case *someone wants to write code for the other platform.*

There are **three** projects in this repo.

* TrimbleMonitor (Fez Lemur) – Basic monitor for the GHI FEZ Lemur (No NTP)
* TrimbleMonitor (Netduino) – Basic monitor for any Netduino 1, 2, 3 (No NTP)
* TrimbleMonitor (Netduno Plus) – Monitor with NTP support for a Netduino 2+ / 3 Ethernet / 3 WiFi

**The board must be running [.NET Micro Framework 4.3](http://netmf.codeplex.com/releases/view/611040).  You should use [Visual Studio 2013](http://download.microsoft.com/download/2/5/5/255DCCB6-F364-4ED8-9758-EF0734CA86B8/wdexpress_full.exe) for this project.  More information can be found within the instructions document.**

Blog posts can be found at http://www.m1dst.co.uk/category/projects/trimble-thunderbolt-monitor/

Contains an NTP server implementation which allows you to sync your computer clock with the Trimble Thunderbolt.

You can now buy the shield as a PCB from [http://www.m1dst.co.uk/shop/](http://www.m1dst.co.uk/shop/)

The Netduino and GHI FEZ Lemur are available from a few places but consistently seem to be in stock at Mouser.

**I do not have a version of the code for Arduino but I'm happy to include it in this repo if you wanted to write it.**
