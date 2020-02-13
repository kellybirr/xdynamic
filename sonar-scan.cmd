dotnet sonarscanner begin /k:"kellybirr_xdynamic" /o:"kellybirr" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="f699c576bdffcea59c920b0603bad69fe2b5fbee"
dotnet build "XDynamicLib.sln"
dotnet sonarscanner end /d:sonar.login="f699c576bdffcea59c920b0603bad69fe2b5fbee" 
