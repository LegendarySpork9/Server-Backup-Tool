@echo off
echo Server starting...
echo Server ready.
:loop
set "cmd="
set /p cmd=
if /i "%cmd%"=="stop" goto shutdown
ping -n 2 127.0.0.1 >nul
goto loop
:shutdown
echo Stopping server...
echo %cd%^>PAUSE
