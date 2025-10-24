@echo off
cd /d "%~dp0"
echo ===============================
echo   TWORZENIE KOPII ZAPASOWEJ
echo ===============================

git add .
set DATETIME=%date%_%time%
set DATETIME=%DATETIME::=-%
set DATETIME=%DATETIME:/=-%
git commit -m "Auto backup %DATETIME%"
git push

echo.
echo Kopia zapasowa wyslana pomyslnie!
pause
