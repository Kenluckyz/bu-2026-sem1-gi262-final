หลุมระเบิด (Crater Blast) · Game Mellow VFX Lab · Unity

Unzip into your project's Assets folder. On import, NightForge/Editor/
NightForgeImporter.cs builds a ParticleSystem prefab and material per sheet
next to the PNGs (85 frames at 30 fps, Texture Sheet Animation 8x11).

Drop the prefab into a scene. It loops for preview; for a skill, turn off
Looping and call ParticleSystem.Play() when it fires.
Glow sets and black-background sheets use additive blending, others alpha.
Works in the Built-in pipeline and URP (legacy particle shaders).
Re-importing sheet.json (right-click > Reimport) rebuilds the prefabs.

Export ID: mkkH451XIqzpqMcs-TJJLCKAehb1RU3pGVT0vCfKw-NDM8kQM4YNEG_Fpga71-cOVqZ4-vTz4NrFP7hU