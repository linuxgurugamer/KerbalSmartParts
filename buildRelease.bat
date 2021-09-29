rem

@echo off

set RELEASEDIR=d:\Users\jbb\release
set ZIP="c:\Program Files\7-zip\7z.exe"


set VERSIONFILE=SmartParts.version
rem The following requires the JQ program, available here: https://stedolan.github.io/jq/download/
c:\local\jq-win64  ".VERSION.MAJOR" %VERSIONFILE% >tmpfile
set /P major=<tmpfile

c:\local\jq-win64  ".VERSION.MINOR"  %VERSIONFILE% >tmpfile
set /P minor=<tmpfile

c:\local\jq-win64  ".VERSION.PATCH"  %VERSIONFILE% >tmpfile
set /P patch=<tmpfile

c:\local\jq-win64  ".VERSION.BUILD"  %VERSIONFILE% >tmpfile
set /P build=<tmpfile
del tmpfile
set VERSION=%major%.%minor%.%patch%
if "%build%" NEQ "0"  set VERSION=%VERSION%.%build%

echo %VERSION%

mkdir ..\GameData\SmartParts
mkdir ..\GameData\SmartParts\Parts
mkdir ..\GameData\SmartParts\Plugins
mkdir ..\GameData\SmartParts\Sounds


del /Q ..\GameData\SmartParts
del /Q ..\GameData\SmartParts\Parts
del /Q ..\GameData\SmartParts\Plugins
del /Q ..\GameData\SmartParts\Sounds


copy /Y "%~dp0..\bin\Release\SmartParts.dll" "..\GameData\SmartParts\Plugins"

copy /Y "%~dp0SmartParts.version" "..\GameData\SmartParts"

xcopy /Y /S "%~dp0..\OrigParts" "..\GameData\SmartParts\Parts"


del ..\GameData\SmartParts\Parts\Fuel-Breakers\*.tga
del ..\GameData\SmartParts\Parts\Fuel-Controller\*.tga
del ..\GameData\SmartParts\Parts\Smart-Controller\*.tga
del ..\GameData\SmartParts\Parts\Valve\*.tga


copy /Y "%~dp0..\License.txt" "..\GameData\SmartParts"
copy /Y "%~dp0..\README.md" "..\GameData\SmartParts"

cd ..

set FILE="%RELEASEDIR%\SmartParts-%VERSION%.zip"
IF EXIST %FILE% del /F %FILE%
%ZIP% a -tzip %FILE% Gamedata\SmartParts

pause