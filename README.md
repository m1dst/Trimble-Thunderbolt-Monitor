This is a hardware and software project to monitor and command a [Trimble Thunderbolt](http://www.trimble.com/timing/thunderbolt-e.aspx) which is a GPS disciplined 10MHz reference.

The software runs on a [Netduino](http://netduino.com/) which is a .net version of the popular Arduino platform.  It also includes a shield to make connecting it up to the Netduino much cleaner.  I have designed the shield to be compatible with both Netduino and Arduino in case someone wants to port to the other platform.

This particular version requires a [Netduino Plus 2](http://netduino.com/netduinoplus2/specs.htm) but can be recompiled for other Netduino models.

It is important to note that it also requires [.NET Micro Framework 4.2](http://netmf.codeplex.com/) at present.

Blog posts can be found at http://www.m1dst.co.uk/category/projects/trimble-thunderbolt-monitor/

Contains an NTP server implementation which allows you to sync your computer clock with the Trimble Thunderbolt.

You can now buy this as a kit from [http://www.m1dst.co.uk/shop/thunderbolt-monitor-kit/](http://www.m1dst.co.uk/shop/thunderbolt-monitor-kit/)
