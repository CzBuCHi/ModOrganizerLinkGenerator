# ModOrganizerLinkGenerator

When executed it will do this pseudocode:

```
if (list of links is not empty) {
  delete all links from list of links
  delete all empty folders from game data directory
  clear list of links
} else {
  read active mo2 profile for active mods
  create dictionary of active mod files (ordered by mod priority)
    - key: absolute path in game data folder, from where game loads its content
    - value: absolute path to file in mo2 mods folder (may be overriden by mod with higher priority)
  for each pair in dictionary execute `mklink /h ${key} ${value}` - create ntfs hard links inside game data folder
  save dictionary keys to list of links
}
```

note: when hard links are generated mo2 shows them as "unmanaged" - to modify mods they need to be deleted by this tool first.

note2: because vfs is not running mo2 'overwrite' folder will no longer work
 - my suggestion is to create list of all game files before first run and after quiting game and deleting links by this tool
 move all newly created files into special mod (or into mod, that generated them), so theyre hardlinked later and do not polute game data folder

note3: this tool will not hardlink empty folders

