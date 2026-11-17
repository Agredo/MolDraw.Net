@echo off
rem Deletes all bin and obj folders below this directory.
setlocal
cd /d "%~dp0"
for /d /r %%d in (bin obj) do (
    if exist "%%d" (
        echo Removing %%d
        rd /s /q "%%d"
    )
)
echo Done.
endlocal
