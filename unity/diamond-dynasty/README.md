# Diamond Dynasty Unity vertical slice

Open this directory as a Unity `6000.5.5f1` project. The project rebuilds the
approved at-bat in Unity 2D; it does not import or execute browser prototype code.

## Controls

- Opening menu: Left/Right or D-pad chooses Batter/Pitcher; Enter, Space, or gamepad South confirms
- Batter: Arrow keys/D-pad aim; Space, A, or gamepad South swings
- Pitcher: Q/E or gamepad shoulders cycle pitch type; 1-3 directly selects a pitch
- Pitcher: Arrow keys/D-pad move the requested plate target
- Pitcher: Hold Enter, Space, or gamepad South to charge; release to deliver
- Escape or gamepad East returns to role selection outside a live pitch; while charging, the first press safely cancels the charge

More charge increases pitch velocity and location error. Pitcher mode displays
the selected pitch, requested target, power, and accuracy risk. Batter mode never
shows those details and receives only neutral in-flight information.

The baseball uses four native-pixel circular frames rather than a scaled square.

## Verification

Run EditMode tests in Unity or from batch mode:

```bash
/Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults Logs/editmode-results.xml
```

The generated `Assets/Scenes/Main.unity` scene is the first playable milestone.
