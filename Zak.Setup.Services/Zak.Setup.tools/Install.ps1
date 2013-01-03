param($installPath, $toolsPath, $package, $project)

New-Item -ItemType directory -Path $installPath\..\..\.Zak.Setup

Copy-Item $installPath\bin\* $installPath\..\..\.Zak.Setup -recurse