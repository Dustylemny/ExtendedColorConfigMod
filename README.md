//for slugcat display in story menu, and if you have a specific texture to add to your slugcat.

//Do note 
[this mod tries to find other body parts by searching for 
(first bodypart)"illustrations/SLUGCATNAME_pup_off.png", 
(second bodypart)"illustrations/SLUGCATNAME_face_pup_off.png",
(Third body part)"illustrations/SLUGCATNAME_unique_pup_off.png" 
or (Other body parts)"illustrations/SLUGCATNAME_BodyPartName_pup_off.png", 

(if custom slugcat first body part and second body part is not found, it will automatically default to "illustrations/pup_off.png" and "illustrations/face_pup_off.png", the rest will be defaulted to "illustrations/colorconfig_showcasesquare")

so FOR SIMPLICITY name your files as what the mod tries to find]


//else if you want to add your own custom name, make sure to type this format in one line
//vvvvvvvv

"SLUGCATNAME:BODYPART|FILENAME,BODYPART2|FILENAME2"

//Slugcat Name, Body name must be case-sensitive. Defaults searching in illustration folders, if you want to search in a specific folder, use "FOLDERNAMEHERE/FILENAMEHERE"

//for example vvvvv

White:Body|FILENAMEHERE,Eyes|FILENAMEHERE2

Yellow:Body|Scenes/FILENAMEHERE,Eyes|Scenes/Whyputeminscenes/FILENAMEHERE
