# OpenSewer

## Read Me
- I am trying to focus on the new uGUI update for OpenSewer, meaning documentation might be inaccurate to current source, once I am ready to release v0.5.0, or possibly v1.0.0 depending on how big this update feels, documentation will be generated and proof read to allow easier access to current source code information/features/building
- Eventually there will be some prerelease dlls uploaded when tabs feel completed, if you make a bug report through Issues, please provide your forked project, or the version of prerelease dll to avoid confusion
- I will try to make documents showing how to find information to create similar menus/utilize what this menu uses eventually, making it easy to learn how this was made or to get information to create your own mod
- As of 11/02/26, versions using the old imgui will be not updated (v0.4.0 and below), focusing on creating and expanding the uGUI to make the menu fit better/more immersive with the game, if new features are added to new versions they will not be implimented to imgui unless there is seemingly common issues with performance
- Updates to code may be pushed willy nilly until a complete working version is created, if you want to view source code or build for the mod it is probably best to download the source from 0.4.0 release, currently im just focusing on version control keeping what works saved, once all tabs are completed and working I will work on cleaning up code/files/documentation to match new version
so forgive me for documentation being all over the place/not updated/grammer that gets you more confused then my nan
---

OpenSewer is a BepInEx 5 mod for **Obenseuer** that adds an in-game utility menu with an item spawner, furniture spawner,  player statistics viewing/manipulation, and time-control/freezing features

---

## Build From Source

See `docs/BUILDING.md` for full step-by-step instructions.

Quick version:
1. Copy required game DLLs to `GameLibs/` (see `GameLibs/README.md`).
2. Build:
   - `./build.ps1`
   - or `dotnet build .\src\OpenSewer\OpenSewer.sln -c Release`
3. Output DLL:
   `src/OpenSewer/bin/Release/net472/OpenSewer.dll`
   
---
## Information

README.md will be updated for better format when pushed to main, this branch is just being used for version control, new features/images may have been added without being mentioned or displayed on this branch

Currently `v0.5.0` has had success in creation of ugui code oppososed to imgui from probing (information is on menus debug tab, open menu you want to probe and press f8 to dump full ui, f9 to dump the specifc UI you are hovering over, and f10 to dump hovered as code, you can find dumped files in your "Obenseuer/BepInEx/LogOutput_OpenSewer", file should be created after probing in game giving you a .txt file containing UI C# Reference Dump), that way the new GUI feels more in place, matching the games UI and Style

Currently the only working features with the new GUI is Item Spawn functionality and very basic time control, other tabs are empty, if you wish to use features like furniture spawning, stat control or more control over time control, use the previous version v0.4.0, if the new GUI is causing performace issues, or you find bugs you can make a bug report on Issues with information/how to replicate, although the new GUI shouldnt cause performance issues, if it does check previous version old GUI

---

## Current Working Features

Succesfully implimented working item spawning in the "Items Tab", user can see an interface similar to their inventory, displaying all items within their game version

## Items Tab
Item spawning through Items Tab in the menu is fully functional, there is some features I would like to add to improve the Item Spawning tab, the work in progress is just to keep in mind my ideas if I focus on another tab/feature, if you want the ability to spawn items and use the new GUI, all features within work fine for item spawning


* Item Description Area
  * Allows user to select item and view item information the same way game inventory does (Item Description area is still work in progress at this time, doesnt affect functionality):
  * Displays Image of an selected item the same way game inventory does:
  * Sucessfully shows item information such as the Item Name, Item Category, and Item Description
  * Displays the items max stack size, making it easy for the user to know how much they will recieve upon adding a stack through add stack button
  * Displays the estimate value of the item

 
## Work in progress
* Item Description Area
  * Displaying the ammount of selected item player already has in their inventory for convinence, attemping to get the number to display similar to the game inventory by adding the amount on the image of item
  * Possibly work on making description area more consistant with games inventory to make the mod feel more integrated with the game, ideas would include
     *adding the currency symbol next to items estimated value, keeping what is displayed consistent like removing category: before the selected items category the same way as inventory, changing the background to match the inventorys description exactly, make sizes consisent with in game inventory
  * Features that are available on selected items inside your players inventory such as:
     * Use button, displaying same features as in inventory, allowing you to use items like mushrooms directly from your Item Spawner Menu so you dont use up what you already hold, or have to spawn and open inventory
     * Add drop button, allowing you to drop selected item like in player inventory, letting them drop selected item despite not having it in their inventory, or using what they already have
   * Figure out whether to display players hat/mask/backpack/tool/torch allowing user to drag and drop directly from item spawner menu, or use empty space to have 6 items display in a row rather than currently having 5 and an empty gap where space isnt utilized
   * Attempt to create finer control, possible allowing right click on an item to display a slider for quicker fine control, right click to give a stack or one item, recreate how game handles holding right click (holding right click on a players inventory item, if item has more than one stack the game displays a circle which takes one for each complete round, figuring out logic like how long each completion is to make the menu seem more integrated within the game, letting you hold rightclick to grab one item each completion, intended behaviour would be different to inventory requiring more than one item to use)

---
# Images / Progress Information

## Main Menu
`Displayed on first open, eventually will be used as a way to view changes/additons, or possibly just be used to view players basic stat information with no manipulation`
<img width="1920" height="1200" alt="{52AF9945-FAB7-4E41-BC31-A8A40E938D8C}" src="https://github.com/user-attachments/assets/9cbfefa8-bcc9-43dd-b9ac-ffffa0291b76" />

## Item Spawning Tab (Items)
`Allows you to find items in your game version and add any item you wish to your inventory`
<img width="1920" height="1200" alt="{E9174B7A-C52C-4759-9104-0A87948B61B0}" src="https://github.com/user-attachments/assets/347ff2ad-e0b5-46fe-9bf1-ee75ca07e579" />
`If theres items within a category that share another category, like alchol shares category with mushroom alchol they are displayed`
<img width="1920" height="1200" alt="{4C60E6C0-4C51-4A96-8646-BB4D1229454D}" src="https://github.com/user-attachments/assets/9645977a-7f3a-4407-ac48-9d1dc158a4b6" />
`You can search for specifc items by name or the items ID, click to select the item and it will display information about it, such as the items name, category, description, estimated price, and stack size`
<img width="1920" height="1200" alt="{701EDB1B-4DEA-46BF-A406-EB3CA5546893}" src="https://github.com/user-attachments/assets/3f997a4d-ef1c-4284-ad4f-eb489e896227" />
`When an item is selected by pressing its image (you will know from a yellow backround on selected item and description is showing) you can spawn the item directly into your inventory by pressing either of the spawn buttons, note stack size is displayed in item description if you chose to add stack, and amount can be changed to a specific amount by typing it in amount text box and pressing spawn amount`
<img width="1920" height="1200" alt="{1063CA99-781A-406C-B0C0-2AF79B4988A5}" src="https://github.com/user-attachments/assets/72048309-97cf-49a6-a69e-fada2cbdf972" />

## Furniture Spawning Tab (Furniture)
`Features not implimented/documented, this tab will allow you to find any furniture in game and add them to your inventory`
<img width="1920" height="1200" alt="{ECAB355E-0295-4733-B237-ADF782BD4DF2}" src="https://github.com/user-attachments/assets/e020de2e-e547-476b-bd58-465c4f113498" />

## Statistic Manipulation (Stats)
`Features not impliment/documented, this tab will allow you to view information like the exact value of things like your health, addiction needs, etc`
<img width="1920" height="1200" alt="{E84FCA3B-795F-4B73-8A7B-A48C7AB564FC}" src="https://github.com/user-attachments/assets/92208fa8-2dae-446e-a073-d925f7195385" />
`If you are unfamiliar with stats in obensuer, pressing f1 to show the console and typing "help" will display a list of console commands, within is a stats category displaying console commands to get further insight on how the game works with needs deeper, if you like understanding how different substances affect your charcter, you can use "player_stats get" to see the specific values of your characters statistics, so if you wanted to understand how addiction works within the game, you could grab your stats, get lit then see how it affects your charcter overall`

`The Statistics tab aims to let you see your stats in a simpler manner, as it can be hard finding specific information amongst a big list like the image after this text, as well as provide an easier way to change values of your players statistics without having to use commands like "player_stats alochol_addiction set 67"`
<img width="1920" height="1200" alt="{0824FBD3-33CD-4A11-B462-C9CF7408CEEA}" src="https://github.com/user-attachments/assets/6f9c1acc-8545-430e-9f22-0338c20bbb1f" />

## Time (Will be updated after Stas tab is done dont worry)
`I have very basic time control added currently, I will make it more in depth with control when I get to the Time Tab stage, features may be broken at this time`
`Currently you can Freeze time with a toggle, however I havent actually tested if it works, and I know it doesnt actually display if time pause is toggled, eventually you will be able to know if it is toggled in some way like changing the background colour for the button, and a button to set the time to 9:00`
`Plans for being able to view the time in the time menu, change the time to a specific time, change the in game date will be added eventually`
<img width="1920" height="1200" alt="{CCE01497-5224-414B-9CE8-38AF96F699B1}" src="https://github.com/user-attachments/assets/27c9a503-f5f3-4275-9dbc-ec396649be09" />

## Debug
`This tab will probably be removed, or changed on full release, its usecase is to remember the binds for dumping in game UI, making it easy to create a GUI for the mod without guesswork`
`Usage:`
`Open the UI in game which you want to dump, pressing f8 will dump the full ui, hovering over a specifc comonent and pressing f9 will dump the hovered ui, f10 dumps hovered as code`
`You can find the dump files in your game directory at "\Obenseuer\BepInEx\LogOutput_OpenSewer" once a dump has been made, providing a .txt file for you to view`
<img width="1920" height="1200" alt="{F4DABA1A-D508-4CBF-A46F-55274D768784}" src="https://github.com/user-attachments/assets/0668db9e-4ca9-4d4f-9524-22b5d2e8bbbd" />


---

## References
Massive thank you to Github user [ShiggityShaggs](https://github.com/shiggityshaggs), his repositories for both 
[ObenseuerItemCodex](https://github.com/shiggityshaggs/ObenseuerItemCodex)
&
[ObenseuerFurnitureCodex](https://github.com/shiggityshaggs/ObenseuerFurnitureCodex)
have both helped massively in saving time in implimentation of Item & Furniture Spawning within the menu and making sure Item/Furniture images aligned correctly with item

## Contributing

See `CONTRIBUTING.md`.

## License

MIT. See `LICENSE`.
