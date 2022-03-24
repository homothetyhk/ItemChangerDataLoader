# ItemChanger Data Loader

ICDL is a mod for launching saves modified by ItemChanger through the menu. In particular, it can be used to start plandos, or it can be used to replay randomizers.

ICDL can be used to backup rando saves. This feature is controlled by the "Backup New Rando Saves" global setting in the mod menu. It has the following values:
- Manual (default): ICDL creates a temporary backup whenever you start a new rando save, located in ICDL/Temp/user\* in the saves folder. This backup can be permanently saved at any time from the mod menu, or manually by moving the backup to the Past Randos directory. Starting a new rando in the same slot will overwrite the contents of ICDL/Temp/user\*.
- Automatic: ICDL creates a permanent backup whenever you start a new rando save, located in ICDL/Past Randos. Backups can be accessed and replayed from the "Past Randos" mode menu.
- None: ICDL will not create a backup when you start a new rando save. Note that it is not possible to backup a save in progress.

ICDL loads packs from the save folder for use in the mode select screen. Specifically, it looks for packs in ICDL/Past Randos (corresponding to the "Past Rando" mode button) and ICDL/Plandos (corresponding to the "Plando Plando" mode button). Packs are directories containing "pack.json" and "ic.json" files. A pack may also have a "ctx.json" file if it will make use of randomizer's tracker log and helper log features. Packs work (and are formatted) identically to randomizer backups created by ICDL.

Note: the respective mode menus will only appear when they are nonempty.