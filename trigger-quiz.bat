@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\trigger-quiz.ps1" %*
