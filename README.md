**ATTENTION: If you are already running this project you must update to v1.0.4 which corrects the problem where the date rolls back to 1997.  The Trimble Thunderbolt will have stopped reporting the correct time from July 30th 2017 for any hardware/software which reads its data unless it is aware of the rollover.**

This is a hardware and software project to monitor and command a [Trimble Thunderbolt](http://www.trimble.com/timing/thunderbolt-e.aspx) which is a GPS disciplined 10MHz reference.

The software runs on a [Netduino](http://netduino.com/) which is a .NET version of the popular Arduino platform.  It also includes a shield to make connecting it up to the Netduino much cleaner.  I have designed the shield to be compatible with both Netduino and Arduino in case someone wants to write code for the other platform.

There are **two** projects in this repo.  Once which is for the **Netduino 1** and does not support the NTP server.  The second supports the NTP feature for the **Netduino 2/3+** boards.  It should be obvious based on the solution names which is which.

It is important to note that it also requires [.NET Micro Framework 4.3](http://netmf.codeplex.com/releases/view/611040) at present.

Blog posts can be found at http://www.m1dst.co.uk/category/projects/trimble-thunderbolt-monitor/

Contains an NTP server implementation which allows you to sync your computer clock with the Trimble Thunderbolt.

You can now buy this as a PCB from [http://www.m1dst.co.uk/shop/](http://www.m1dst.co.uk/shop/)
