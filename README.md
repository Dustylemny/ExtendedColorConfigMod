For slugcat display in story menu, and if you have a specific texture to add to your slugcat. this mod automatically tries to search it

Tries to find
(first bodypart)"illustrations/SLUGCATNAME_pup_off.png", 
(second bodypart)"illustrations/SLUGCATNAME_face_pup_off.png",
(Third body part)"illustrations/SLUGCATNAME_unique_pup_off.png" 
OR (other body parts)"illustrations/SLUGCATNAME_BodyPartName_pup_off.png", 

IF custom slugcat first body part and second body part is not found, it will automatically default to "illustrations/pup_off.png" and "illustrations/face_pup_off.png", the rest will be defaulted to "illustrations/colorconfig_showcasesquare"

SO FOR SIMPLICITY name your files as what the mod tries to find first

else if you want to add your own custom name, make sure to type this format in one line
//vvvvvvvv

"SLUGCATNAME:BODYPART|FILENAME,BODYPART2|FILENAME2"

SlugcatNAME and BODYPART must be case-sensitive. if there are no folder names, it defaults searching in illustration folders, if you want to search in a specific folder, use "FOLDERNAME/FILENAME"

//for example vvvvv

White:Body|FILENAMEHERE,Eyes|FILENAMEHERE2

Yellow:Body|FOLDERNAME/FILENAME,Eyes|FOLDERNAME/SUBFOLDERNAME/FILENAME
