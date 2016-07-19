@echo off

IF EXIST "out" RMDIR /S /Q "out"

MKDIR "out"
MKDIR "out\bin"

rc.exe /fo "out\ctranscode.res" "ctranscode.rc"
csc.exe /win32res:"out\ctranscode.res" /debug /pdb:"out\bin\ctranscode.pdb" /out:"out\bin\ctranscode.exe" "ctranscode.cs"
