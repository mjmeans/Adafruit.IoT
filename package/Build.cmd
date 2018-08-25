@ECHO OFF

REM Variables
SET MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"

REM Build All Projects
%MSBUILD% /v:m Build.proj

ECHO Copying Package Content to Builds
XCOPY /Y Content\*.* /s Builds
