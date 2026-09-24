# Chapter 1 clue narration

`dialogue.json` contains the four clues' exact authored narration, split into five playback segments (the letter has two).

The ClueManager loads `Resources/Audio/Chapter1Clues/{clueId}_{part:00}`. Each segment starts with its text, stops when advancing/closing/disabling, and uses a 2D AudioSource that plays while the investigation pauses game time. Missing clips preserve silent text playback.

The user approved both Microsoft Edge TTS and local speech previews on 2026-09-24. Five MP3 clips are in `Preview` (SunHiNeural, rate -12%, pitch -2Hz), and five WAV clips are in `PreviewLocal` (installed Microsoft Heami Desktop, rate -1). `audio-validation.json` records duration and size for all ten files.

These files intentionally remain outside Assets. The user wants to listen before deciding whether to include them. No voice is currently installed into the game. Runtime playback will require validation if the user chooses a version.
