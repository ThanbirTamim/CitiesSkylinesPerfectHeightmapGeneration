@echo off
rem #########################################################################
rem #
rem # GRASS initialization bat script (OSGeo4W)
rem #
rem #########################################################################

rem
rem Set environmental variables
rem
call "%~dp0\o4w_env.bat"
call "%OSGEO4W_ROOT%\apps\grass\grass76\etc\env.bat"
@echo off

rem
rem Launch GRASS GIS
rem
"%GRASS_PYTHON%" "%GISBASE%\etc\grass76.py" %*

rem
rem Pause on error
rem
if %ERRORLEVEL% GEQ 1 pause
