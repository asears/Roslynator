@echo off

"C:\Program Files\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild" "..\src\CommandLine.sln" /t:Build /p:Configuration=Debug /v:m /m

set _analyzersDir=..\src\Analyzers.CodeFixes\bin\Debug\netstandard1.3\

"..\src\CommandLine\bin\Debug\net461\roslynator" analyze "..\src\CommandLine.sln" ^
 --use-roslynator-analyzers ^
 --ignore-analyzer-references ^
 --ignored-diagnostics CS1591 RCS1002 RCS1140 RCS1161 RCS1181 RCS1186 RCS1201 RCS1207 RCS1211 RCS1228 ^
 --minimal-severity info ^
 --culture en ^
 --file-log "roslynator.log" ^
 --file-log-verbosity diag ^
 --xml-file-log "diagnostics.xml"

pause