@echo off
echo ===============================
echo  AUTOMATYCZNY BACKUP NA GITHUB
echo ===============================
echo.

REM Przejdź do folderu projektu
cd /d "%~dp0"

REM Dodaj wszystkie zmiany
git add .

REM Utwórz commit z datą i godziną
for /f "tokens=1-5 delims=/:. " %%d in ("%date% %time%") do (
    set timestamp=%%d-%%e-%%f_%%g-%%h
)
git commit -m "Auto-backup %timestamp%"

REM Wypchnij na GitHub
git push

echo.
echo ✅ Backup zakończony pomyślnie!
pause
