Hi, thank you for the purchase!

This editor-only tool was developed to protect Unity IL2CPP builds using several uncommon techniques
inspired by AAA games (e.g., Genshin Impact).

The entire process is automated, and the asset does not contain any demo scenes.
All you have to do is build your project (IL2CPP), and Mfuscator will automatically protect it.
To examine the result, try to dump the build with any popular Unity IL2CPP dumping tool (or dumper).

[!] If you use other build postprocessing scripts, you can configure the callback order in the "Window/Mfuscator Settings" window to avoid any conflicts.
There, you can also deactivate or activate Mfuscator.

FREQUENTLY ASKED QUESTIONS:

1. I get a "The current system user does not have full access" error when building.
- To fix the error, you need to either run Unity as administrator (not recommended) or grant
the current system user full access to the folder and subfolders where the Unity editor is installed.
The default path for Windows is "C:\Program Files\Unity\Hub".

2. I can't delete and update the asset. I get a "Cannot Delete" error.
- Close the Unity editor to release the process, navigate to the path where your
project is located, and delete the Mfuscator folder. Then you can open Unity
again and import the new version.

[!] If Mfuscator doesn't work but there are no errors in the console after the build, make sure you don't have automatic cleanup disabled ("Clear on Build", "Clear on Recompile").

If you encounter any issues, please feel free to join our Discord, and we will try to help you as soon as we can.
https://mew.icu/
