@echo off

WHERE /q dotnet
IF %ERRORLEVEL% NEQ 0 (
    ECHO dotnet was NOT found
    WHERE /q pwsh
    IF %ERRORLEVEL% NEQ 0 (
        ECHO PSWH was NOT found
        WHERE /q powershell
        IF %ERRORLEVEL% NEQ 0 (
            ECHO powershell wasn't found
        ) ELSE (
            call :installdotnetandpswh
            ECHO powershell was found
        )
    ) ELSE (
        ECHO pwsh was found
    )
) ELSE (
    ECHO dotnet found
    WHERE /q pwsh
    IF %ERRORLEVEL% NEQ 0 (
        call :installpswh
    ) ELSE (
        ECHO dotnet and pwsh found. all ok
    )
)

goto :final

:installdotnetandpswh
powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -channel 6.0"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -channel 7.0"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -channel 8.0"
REM ADD ENVIO
dotnet tool install --global PowerShell --version 7.4.1
goto :final

:installpswh
dotnet tool install --global PowerShell --version 7.4.1
goto :final

:installpdotnet
pwsh -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -channel 6.0"
pwsh -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -channel 7.0"
pwsh -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -channel 8.0"
REM ADD ENVIO
goto :final

:final

ECHO EOF
REM powershell -NoProfile -ExecutionPolicy unrestricted -Command "[System.Environment]::SetEnvironmentVariable('DOTNET_ROOT', \"$HOME\cli\", [System.EnvironmentVariableTarget]::User)"
REM powershell -NoProfile -ExecutionPolicy Unrestricted -Command "$path = [System.Environment]::GetEnvironmentVariable('PATH', [System.EnvironmentVariableTarget]::User); $newPath = $path + \";$HOME\.cli;$HOME\.cli\tools\""; [System.Environment]::SetEnvironmentVariable('PATH', $newPath, [System.EnvironmentVariableTarget]::User)"


