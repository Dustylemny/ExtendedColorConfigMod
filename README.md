For slugcat display in story menu, and if you have a specific texture to add to your slugcat. this mod automatically tries to search it

Tries to find
(first bodypart)"illustrations/SLUGCATNAME_pup_off.png", 
(second bodypart)"illustrations/face_SLUGCATNAME_pup_off.png",
(Third body part)"illustrations/unique_SLUGCATNAME_pup_off.png" 
OR (other body parts)"illustrations/BODYNAME_SLUGCATNAME_pup_off.png", 

IF custom slugcat first body part and second body part is not found, it will automatically default to "illustrations/pup_off.png" and "illustrations/face_pup_off.png", the rest will be defaulted to "illustrations/colorconfig_showcasesquare"

SO FOR SIMPLICITY name your files as what the mod tries to find first

else if you want to add your own custom name, formatexamples:
"SLUGCATNAME:BODYPART|FILENAME,BODYPART2|FOLDERNAME/FILENAME2"
"SLUGCATNAME:BODYPART|FOLDERNAME/FOLDERNAME/FILENAME"
SLUGCATNAME and BODYPART must be case-sensitive. (this custom names will not work as intended for a slugcat with bodyparts sharing the same name)
