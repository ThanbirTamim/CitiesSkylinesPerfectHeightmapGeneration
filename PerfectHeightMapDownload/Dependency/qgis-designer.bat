@echo off
call "%~dp0\o4w_env.bat"
call qt5_env.bat
call py3_env.bat
path %OSGEO4W_ROOT%\apps\qgis\bin;%PATH%
set QGIS_PREFIX_PATH=%OSGEO4W_ROOT:\=/%/apps/qgis
set QT_PLUGIN_PATH=%OSGEO4W_ROOT%\apps\qgis\qtplugins;%OSGEO4W_ROOT%\apps\qt5\plugins
start "Qt Designer with QGIS custom widgets" /B "%OSGEO4W_ROOT%\apps\qt5\bin\designer.exe" %*
