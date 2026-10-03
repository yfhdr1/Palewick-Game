@echo off
setlocal EnableExtensions DisableDelayedExpansion
title Palewick One-Click Updater

rem This file may be launched from any folder. It first finds the GitHub Desktop clone.
set "SCRIPT_FILE=%~f0"
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR="
set "GIT_EXE="
set "UNITY_EXE="
set "UNITY_VERSION="
set "GIT_TERMINAL_PROMPT=0"
set "PHASE=%~1"

echo.
echo ================================================================
echo                 PALEWICK ONE-CLICK UPDATER
echo ================================================================
echo.

if /i "%PHASE%"=="--finish" goto :PhaseFinish
if /i "%PHASE%"=="--bootstrap" goto :PhaseBootstrap

rem Phase 0 (double-click entry). CMD keeps reading a running BAT from its own
rem file, so a Git update applied to the tracked copy mid-run can corrupt the
rem execution. Stage a throwaway runner in TEMP and let it do the Git work.
set "RUNNER_FILE=%TEMP%\Palewick_One_Click_Update_run.bat"
copy /y "%SCRIPT_FILE%" "%RUNNER_FILE%" >nul
if errorlevel 1 (
    echo ERROR: Could not stage a runner copy in the TEMP folder.
    goto :Failure
)
call "%RUNNER_FILE%" --bootstrap
set "RUNNER_RC=%ERRORLEVEL%"
del /q "%RUNNER_FILE%" >nul 2>&1
exit /b %RUNNER_RC%

:PhaseBootstrap
rem The Git work below runs from the TEMP copy, so the pull can freely replace
rem the tracked updater inside the project folder.
call :FindProject
if not defined PROJECT_DIR (
    echo ERROR: Could not find a Palewick-Game GitHub Desktop clone.
    echo Looked beside this updater and in the usual GitHub Desktop folders.
    echo Put this BAT file in the root of the Palewick-Game clone and run it again.
    goto :Failure
)

echo Project found:
echo   %PROJECT_DIR%
echo.

call :FindGit
if not defined GIT_EXE (
    echo ERROR: Git was not found.
    echo Install Git for Windows or GitHub Desktop, then run this updater again.
    goto :Failure
)

echo Git found:
echo   %GIT_EXE%
echo.

"%GIT_EXE%" -C "%PROJECT_DIR%" rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo ERROR: The located Palewick-Game folder is not a usable Git repository.
    goto :Failure
)

rem Ask only the Unity instance that opened this project to close. No process is force-killed.
set "PALEWICK_PROJECT_DIR=%PROJECT_DIR%"
set "UNITY_STILL_RUNNING="
echo Checking whether this project is open in Unity...
for /f "usebackq delims=" %%L in (`powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$project = (Resolve-Path -LiteralPath $env:PALEWICK_PROJECT_DIR).Path; $ids = [System.Collections.Generic.List[int]]::new(); $processes = Get-CimInstance Win32_Process -Filter 'Name=''Unity.exe'''; foreach ($process in $processes) { if ($process.CommandLine -and $process.CommandLine.IndexOf($project, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { $ids.Add([int]$process.ProcessId) } }; foreach ($id in $ids) { try { $null = [System.Diagnostics.Process]::GetProcessById($id).CloseMainWindow() } catch {} }; if ($ids.Count -gt 0) { Write-Output 'UNITY_CLOSE_REQUESTED'; $deadline = (Get-Date).AddMinutes(3); do { Start-Sleep -Seconds 2; $remaining = [System.Collections.Generic.List[int]]::new(); foreach ($id in $ids) { if (Get-Process -Id $id -ErrorAction SilentlyContinue) { $remaining.Add($id) } } } while ($remaining.Count -gt 0 -and (Get-Date) -lt $deadline); if ($remaining.Count -gt 0) { Write-Output 'UNITY_STILL_RUNNING' } else { Write-Output 'UNITY_CLOSED' } } else { Write-Output 'UNITY_NOT_RUNNING' }"`) do (
    if "%%L"=="UNITY_CLOSE_REQUESTED" echo Requested a normal Unity close. Save any Unity prompt if one appears.
    if "%%L"=="UNITY_CLOSED" echo Unity closed normally.
    if "%%L"=="UNITY_NOT_RUNNING" echo This project is not open in Unity.
    if "%%L"=="UNITY_STILL_RUNNING" set "UNITY_STILL_RUNNING=1"
)
if defined UNITY_STILL_RUNNING (
    echo ERROR: Unity is still open after three minutes, so no update was applied.
    echo Close Unity normally, then run this updater again. Nothing was changed.
    goto :Failure
)
echo.

rem Include untracked files as well: this updater never proceeds while user work is present.
set "STATUS_FILE=%TEMP%\Palewick_git_status_%RANDOM%_%RANDOM%.txt"
"%GIT_EXE%" -C "%PROJECT_DIR%" status --porcelain=v1 --untracked-files=all > "%STATUS_FILE%" 2>&1
if errorlevel 1 (
    echo ERROR: Git could not inspect the working tree.
    type "%STATUS_FILE%"
    del /q "%STATUS_FILE%" >nul 2>&1
    goto :Failure
)
for %%S in ("%STATUS_FILE%") do if %%~zS GTR 0 (
    echo ERROR: Local changes or untracked files were found. To protect your work,
    echo no Git operation was run. Commit, stash, move, or remove them first:
    echo.
    type "%STATUS_FILE%"
    del /q "%STATUS_FILE%" >nul 2>&1
    goto :Failure
)
del /q "%STATUS_FILE%" >nul 2>&1
set "STATUS_FILE="

echo Fetching updates from origin...
"%GIT_EXE%" -C "%PROJECT_DIR%" fetch --prune origin
if errorlevel 1 (
    echo ERROR: Git fetch failed. No working files were changed.
    goto :Failure
)

echo Switching to main...
"%GIT_EXE%" -C "%PROJECT_DIR%" checkout main
if errorlevel 1 (
    echo ERROR: Git could not switch to the local main branch safely.
    echo No files were overwritten.
    goto :Failure
)

echo Applying the latest main branch with a fast-forward-only pull...
"%GIT_EXE%" -C "%PROJECT_DIR%" pull --ff-only origin main
if errorlevel 1 (
    echo ERROR: The update is not a fast-forward, so Git did not overwrite anything.
    goto :Failure
)
echo GitHub update completed safely.
echo.

rem The tracked copy of this updater may have just been replaced by Git. Hand
rem the remaining steps to that fresh copy so the newest logic always finishes
rem the run (Unity version check, portable ZIP, Unity launch).
set "TRACKED_BAT=%PROJECT_DIR%\PALEWICK_ONE_CLICK_UPDATE.bat"
if not exist "%TRACKED_BAT%" (
    echo ERROR: The tracked updater is missing from the project after the update:
    echo   %TRACKED_BAT%
    goto :Failure
)
call "%TRACKED_BAT%" --finish
set "FINISH_RC=%ERRORLEVEL%"
exit /b %FINISH_RC%

:PhaseFinish
rem Runs from the tracked copy after the pull. SCRIPT_DIR is the project root,
rem so the discovery below resolves immediately.
call :FindProject
if not defined PROJECT_DIR (
    echo ERROR: The Palewick-Game project could not be located after the update.
    goto :Failure
)

set "PROJECT_VERSION_FILE=%PROJECT_DIR%\ProjectSettings\ProjectVersion.txt"
if not exist "%PROJECT_VERSION_FILE%" (
    echo ERROR: ProjectSettings\ProjectVersion.txt is missing after the update.
    goto :Failure
)
for /f "tokens=2 delims=:" %%V in ('findstr /b /c:"m_EditorVersion:" "%PROJECT_VERSION_FILE%"') do set "UNITY_VERSION=%%V"
for /f "tokens=*" %%V in ("%UNITY_VERSION%") do set "UNITY_VERSION=%%V"
if not defined UNITY_VERSION (
    echo ERROR: The Unity editor version could not be read from ProjectVersion.txt.
    goto :Failure
)
echo Required Unity version: %UNITY_VERSION%

call :FindUnity
if not defined UNITY_EXE (
    echo ERROR: Unity %UNITY_VERSION% was not found.
    echo Install this exact version from Unity Hub, then run the updater again.
    goto :Failure
)
echo Unity found:
echo   %UNITY_EXE%
echo.

call :CreateZip
if errorlevel 1 (
    echo ERROR: Could not create or verify Palewick_One_Click_Updater.zip.
    goto :Failure
)

echo Starting Unity and running Palewick.EditorTools.PalewickMasterFix.FixEverything...
start "Palewick Unity Repair" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%" -executeMethod Palewick.EditorTools.PalewickMasterFix.FixEverything
if errorlevel 1 (
    echo ERROR: Unity could not be started.
    goto :Failure
)

echo.
echo Success. GitHub main was updated, the ZIP is ready, and Unity was started.
echo Unity will run the Palewick repair after it opens the project.
goto :Success

:FindProject
rem Use plain CMD path checks so Desktop launches never depend on PowerShell pipelines.
call :UseProjectIfValid "%SCRIPT_DIR%"
call :UseProjectIfValid "%USERPROFILE%\Documents\GitHub\Palewick-Game"
call :UseProjectIfValid "%USERPROFILE%\OneDrive\Documents\GitHub\Palewick-Game"
call :UseProjectIfValid "%USERPROFILE%\GitHub\Palewick-Game"
call :UseProjectIfValid "%USERPROFILE%\source\repos\Palewick-Game"
call :UseProjectIfValid "%USERPROFILE%\Desktop\Palewick-Game"
call :UseProjectIfValid "%USERPROFILE%\Desktop\yarekam"
exit /b 0

:UseProjectIfValid
if defined PROJECT_DIR exit /b 0
set "PROJECT_CANDIDATE=%~f1"
if exist "%PROJECT_CANDIDATE%\ProjectSettings\ProjectVersion.txt" if exist "%PROJECT_CANDIDATE%\.git" set "PROJECT_DIR=%PROJECT_CANDIDATE%"
set "PROJECT_CANDIDATE="
exit /b 0

:FindGit
for /f "delims=" %%G in ('where.exe git.exe 2^>nul') do if not defined GIT_EXE set "GIT_EXE=%%G"
if defined GIT_EXE exit /b 0
if exist "%ProgramFiles%\Git\cmd\git.exe" set "GIT_EXE=%ProgramFiles%\Git\cmd\git.exe"
if not defined GIT_EXE if exist "%ProgramFiles(x86)%\Git\cmd\git.exe" set "GIT_EXE=%ProgramFiles(x86)%\Git\cmd\git.exe"
if not defined GIT_EXE if exist "%LOCALAPPDATA%\Programs\Git\cmd\git.exe" set "GIT_EXE=%LOCALAPPDATA%\Programs\Git\cmd\git.exe"
if not defined GIT_EXE for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do if exist "%%~fD\resources\app\git\cmd\git.exe" set "GIT_EXE=%%~fD\resources\app\git\cmd\git.exe"
if not defined GIT_EXE for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do if exist "%%~fD\resources\app\git\mingw64\bin\git.exe" set "GIT_EXE=%%~fD\resources\app\git\mingw64\bin\git.exe"
exit /b 0

:FindUnity
if exist "%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not defined UNITY_EXE if exist "%ProgramFiles(x86)%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=%ProgramFiles(x86)%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not defined UNITY_EXE if exist "%LOCALAPPDATA%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=%LOCALAPPDATA%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not defined UNITY_EXE if exist "%LOCALAPPDATA%\Programs\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe" set "UNITY_EXE=%LOCALAPPDATA%\Programs\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not defined UNITY_EXE for /f "delims=" %%U in ('where.exe Unity.exe 2^>nul') do if not defined UNITY_EXE set "UNITY_EXE=%%U"
exit /b 0

:CreateZip
set "PALEWICK_BAT_FILE=%PROJECT_DIR%\PALEWICK_ONE_CLICK_UPDATE.bat"
set "PALEWICK_ZIP_FILE=%PROJECT_DIR%\Palewick_One_Click_Updater.zip"
if not exist "%PALEWICK_BAT_FILE%" exit /b 1
echo Verifying the portable updater ZIP...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$bat = $env:PALEWICK_BAT_FILE; $zip = $env:PALEWICK_ZIP_FILE; $rebuild = $true; if (Test-Path -LiteralPath $zip) { try { Add-Type -AssemblyName System.IO.Compression.FileSystem; $archive = [System.IO.Compression.ZipFile]::OpenRead($zip); try { $entry = $archive.GetEntry([System.IO.Path]::GetFileName($bat)); if ($entry) { $reader = New-Object System.IO.StreamReader($entry.Open()); try { $inside = $reader.ReadToEnd() } finally { $reader.Dispose() }; $outside = [System.IO.File]::ReadAllText($bat); if ($inside.Equals($outside, [System.StringComparison]::Ordinal)) { $rebuild = $false } } } finally { $archive.Dispose() } } catch { $rebuild = $true } }; if ($rebuild) { if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }; Compress-Archive -LiteralPath $bat -DestinationPath $zip -CompressionLevel Optimal -Force }; if (-not (Test-Path -LiteralPath $zip)) { exit 1 }"
exit /b %ERRORLEVEL%

:Success
echo.
pause
exit /b 0

:Failure
if defined STATUS_FILE del /q "%STATUS_FILE%" >nul 2>&1
echo.
echo Updater stopped safely. Resolve the message above and run it again.
pause
exit /b 1
