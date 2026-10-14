# Regenerates the candy land background PNGs next to this script.
Add-Type -Path (Join-Path $PSScriptRoot 'CandyBg.cs') -ReferencedAssemblies System.Drawing
[CandyBg]::Run($PSScriptRoot)
