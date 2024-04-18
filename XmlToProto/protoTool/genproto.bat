echo off

set protocdir=%~dp0
cd ../XmlToProto
set curdir=%cd%
set pubprotodir=%curdir%
set inputdir=%protocdir%/protos
set outputdir=%curdir%/protos

set /p result=create all proto files ?(Y/N):
if %result% neq Y (
	echo "stop create...."
	pause
)

%protocdir%/protoc --csharp_out=%outputdir% --proto_path=%inputdir% ^
--proto_path=%pubprotodir% %inputdir%/loadConf.proto 

if %errorlevel%==0 (
  echo "generate proto succeeded !"
) else (
  echo "generate proto failed !"
)

pause
