To run this program you will need to create an App.config

```XML
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <configSections>
        <sectionGroup name="applicationSettings" type="System.Configuration.ApplicationSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" >
            <section name="SyncFiles.SftpConnection" type="System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false" />
        </sectionGroup>
    </configSections>
    <applicationSettings>
        <SyncFiles.SftpConnection>
            <setting name="Setting" serializeAs="String">
                <value />
            </setting>
        </SyncFiles.SftpConnection>
    </applicationSettings>
	<appSettings>
		<add key="FtpUser" value="UsernameGoesHere" />
		<add key="FtpPassword" value="PasswordGoesHere" />
		<add key="FtpServer" value="FtpServerAddress" />
		<add key="FtpPort" value="22" />
	</appSettings>
</configuration>
```
