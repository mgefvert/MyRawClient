@echo off

echo.
echo === Building debug version
msbuild MyRawClient.sln /p:configuration=debug /v:minimal
if errorlevel 1 goto :err

echo.
echo === Building release version
msbuild MyRawClient.sln /p:configuration=release /v:minimal
if errorlevel 1 goto :err

echo.
echo === Packing nuget
nuget pack MyRawClient\MyRawClient.csproj -Prop Configuration=Release
if errorlevel 1 goto :err

goto :eof

:err
echo.
echo *** Build failed ***
