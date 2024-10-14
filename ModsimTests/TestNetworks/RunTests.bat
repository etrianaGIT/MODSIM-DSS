@set EXE=..\bin\x64\Release\ModsimTests.exe
@if not exist %EXE% (
    @set EXE=..\bin\x64\Debug\ModsimTests.exe
)
@if not exist %EXE% (
    echo No test program found at either
    echo ..\bin\x86\Debug\ModsimTests.exe
    echo Or
    echo ..\bin\x86\Release\ModsimTests.exe
    @pause
    exit
)

%EXE% %~dp0 forcesqliteout
@pause
