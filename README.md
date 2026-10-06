# HPLockMod_BurgerShop
BepInEx Plugin for game Four Nights at the Burger Shop, locking HP and changing clothes (key 1 , key 5)

# Install
Plugin is based on BepInEx 5

### 1. Install BepInEx 5 x64
Download BepInEx 5 x64:
https://github.com/BepInEx/BepInEx/releases
Choose the file with a name like `BepInEx_win_x64_5.4.23.5.zip`.

Extract it to the game root directory, i.e. the same folder as the game's .exe.

**Note! Keep the `game path` in English characters only.**

Launch the game once, then exit.
This will automatically create the `Game_Path\BepInEx\plugins` folder.

### 2. Install HPLockMod
Place HPLockMod.dll into:

```
Game_Path\BepInEx\plugins\
```

### 3. Usage
Launch the game. After entering a level, press `Hotkey 1` to toggle HP lock. Press `Hotkey 5` to enable real-time outfit change.
<img width="414" height="287" alt="image" src="https://github.com/user-attachments/assets/33f4cd1a-ebc5-4776-b49e-b8bba75c928c" />


If your antivirus flags winhttp.dll, it is a false positive; just trust it.

### 4. Uninstall
Delete the BepInEx folder, winhttp.dll, and doorstop_config.ini.
