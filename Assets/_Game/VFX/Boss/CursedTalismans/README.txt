คาถายันต์โลหิต (Cursed Talismans) · Game Mellow VFX Lab · Unity

Unzip into your project's Assets folder. On import, NightForge/Editor/
NightForgeImporter.cs builds a ParticleSystem prefab and material per sheet
next to the PNGs (133 frames at 30 fps, Texture Sheet Animation 8x17).

Drop the prefab into a scene. It loops for preview; for a skill, turn off
Looping and call ParticleSystem.Play() when it fires.
Glow sets and black-background sheets use additive blending, others alpha.
Works in the Built-in pipeline and URP (legacy particle shaders).
Re-importing sheet.json (right-click > Reimport) rebuilds the prefabs.

Export ID: KWplS7ypCpXtoySbfMTK9BwZ44gBaJfBX2Wx26Uf6PxIs_m2bc7o8yGkyZBB1F-Welm_d2rCkyNSnt9J