To run this program you will need to create an App.config\
It should have an appSettings section like this

```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
	<appSettings>
		<add key="FtpUser" value="UsernameGoesHere" />
		<add key="FtpPassword" value="PasswordGoesHere" />
		<add key="FtpServer" value="FtpServerAddress" />
		<add key="FtpPort" value="22" />
	</appSettings>
</configuration>
```
