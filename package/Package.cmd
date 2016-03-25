@ECHO OFF

ECHO Clearing Pacakges Directory
IF EXIST Packages (REN Packages ~TMP) 
IF EXIST ~TMP (RMDIR ~TMP /s /q) 
MKDIR Packages

ECHO Packaging
nuget pack Adafruit.IoT.nuspec -OutputDirectory Packages
