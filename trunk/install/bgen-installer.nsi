; MwSw Boundary Generator installer.
; 
;

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "inc\DotNet.nsh"

; Preliminaries
; --------------
Name "MWSW Boundary Generator"
Caption "MWSW Boundary Generator v. 0.1"
OutFile "boundarygen_alpha.exe"
InstallDir "$PROGRAMFILES\MwSw"
InstallDirRegKey HKLM "Software\MWSW_BoundGen" "Install_Dir"
RequestExecutionLevel admin

!define DOTNET_VERSION "2"

; Pages...
; --------------

!insertmacro MUI_PAGE_LICENSE "..\LICENSE.txt"

; TODO: uncomment when I have some optional components!
; !insertmacro MUI_PAGE_COMPONENTS

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
  
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

;

!insertmacro MUI_LANGUAGE "English"


Section "Boundary Generator" SecMain
  SectionIn RO

  !insertmacro CheckDotNET ${DOTNET_VERSION}
 
  ; Get .NET path
  ; --
  Push "v2.0"
  Call GetDotNetDir
  Pop $R0
  StrCmpS "" $R0 error_no_dotnet
  ; --

  SetOutPath "$INSTDIR"
  WriteRegStr HKLM "Software\MWSW_BoundGen" "" $INSTDIR
  File "..\build\boundgen.dll"
  File "..\arc-integration\bin\Debug\arc-integration.dll"
  ; register the assembly
  ExecWait '"$R0\RegAsm.exe" /codebase arc-integration.dll'

  File "..\LICENSE.txt"
  WriteUninstaller "uninstall.exe"

  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Example2" "DisplayName" "MWSW Boundary Generator"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Example2" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Example2" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Example2" "NoRepair" 1

  Return

error_no_dotnet:
  Abort "Aborted: .Net Framework not found."
SectionEnd

Section "Uninstall"
 
  ; Get .NET path
  ; --
  Push "v2.0"
  Call un.GetDotNetDir
  Pop $R0
  StrCmpS "" $R0 error_no_dotnet
  ; --

  ExecWait '"$R0\RegAsm.exe" /unregister arc-integration.dll'

error_no_dotnet:
  DeleteRegKey HKLM "Software\MWSW_BoundGen"
  Delete $INSTDIR\boundgen.dll
  Delete $INSTDIR\arc-integration.dll
  Delete $INSTDIR\LICENSE.txt
  Delete $INSTDIR\uninstall.exe
  RMDir "$INSTDIR"

SectionEnd

; Copied from http://nsis.sourceforge.net/Get_directory_of_installed_.NET_runtime
; ...
; Given a .NET version number, this function returns that .NET framework's
; install directory. Returns "" if the given .NET version is not installed.
; Params: [version] (eg. "v2.0")
; Return: [dir] (eg. "C:\WINNT\Microsoft.NET\Framework\v2.0.50727")
Function GetDotNetDir
	Exch $R0 ; Set R0 to .net version major
	Push $R1
	Push $R2
 
	; set R1 to minor version number of the installed .NET runtime
	EnumRegValue $R1 HKLM \
		"Software\Microsoft\.NetFramework\policy\$R0" 0
	IfErrors getdotnetdir_err
 
	; set R2 to .NET install dir root
	ReadRegStr $R2 HKLM \
		"Software\Microsoft\.NetFramework" "InstallRoot"
	IfErrors getdotnetdir_err
 
	; set R0 to the .NET install dir full
	StrCpy $R0 "$R2$R0.$R1"
 
getdotnetdir_end:
	Pop $R2
	Pop $R1
	Exch $R0 ; return .net install dir full
	Return
 
getdotnetdir_err:
	StrCpy $R0 ""
	Goto getdotnetdir_end
 
FunctionEnd

Function un.GetDotNetDir
	Exch $R0 ; Set R0 to .net version major
	Push $R1
	Push $R2
 
	; set R1 to minor version number of the installed .NET runtime
	EnumRegValue $R1 HKLM \
		"Software\Microsoft\.NetFramework\policy\$R0" 0
	IfErrors getdotnetdir_err
 
	; set R2 to .NET install dir root
	ReadRegStr $R2 HKLM \
		"Software\Microsoft\.NetFramework" "InstallRoot"
	IfErrors getdotnetdir_err
 
	; set R0 to the .NET install dir full
	StrCpy $R0 "$R2$R0.$R1"
 
getdotnetdir_end:
	Pop $R2
	Pop $R1
	Exch $R0 ; return .net install dir full
	Return
 
getdotnetdir_err:
	StrCpy $R0 ""
	Goto getdotnetdir_end
 
FunctionEnd