# Diamond Dynasty Unity vertical slice

Open this directory as a Unity `6000.5.5f1` project. The project rebuilds the
approved at-bat in Unity 2D; it does not import or execute browser prototype code.

## Controls

- Enter: pitch
- Arrow keys: aim
- Space or A: swing
- Gamepad D-pad: aim
- Gamepad South/Start: pitch or swing according to game phase

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
