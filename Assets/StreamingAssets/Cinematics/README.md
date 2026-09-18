# Main menu cinematic

MainMenuManager.OnStartButton now plays OpeningAndHospital.mp4 before loading its existing gameSceneName (currently Game).

- Source: approved integrated opening + hospital review, approximately 43.98 seconds, 1920x1080.
- Windows playback copy is H.264 Baseline with no B-frames and explicit BT.709 tags, avoiding Media Foundation timestamp/color warnings. Original integrated MP4 is unchanged.
- StreamingAssets embeds the video in the Windows build; no Desktop/output-folder dependency.
- Runtime overlay uses a 16:9 aspect-fit image on black, preserving the full picture at other display aspect ratios.
- Audio uses AudioSource and the existing master-volume preference.
- Duplicate start clicks are ignored; menu panels are hidden while playing.
- Preparation has a 20-second realtime timeout. Video errors restore the menu instead of leaving a black screen.
- The existing game scene must remain enabled in Build Settings. End-of-video callback loads it once.
- The menu scene and scene-builder button bindings still call the existing OnStartButton method, so no scene regeneration is needed.
- Complete hospital entry remains a future art task; this integration uses the approved available footage as-is.

Manual acceptance: open MainMenu, press START, verify video+sound, wait approximately 44 seconds, verify Game loads. Repeat with volume zero and a second rapid START click. On a missing-video test, expect an error and a usable menu, not a scene transition. Preserve the source MP4 when testing failure.
