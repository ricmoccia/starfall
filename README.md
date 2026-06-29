# STARFALL — Survive the Swarm

A fast-paced vertical space shooter for Android, built with Unity. Pilot your ship, dodge enemy fire, survive escalating waves, and climb the online leaderboards.

## Features

### Gameplay
- **Vertical bullet-hell shooter** with smooth touch controls — drag to move, auto-fire while you focus on dodging.
- **Four difficulty levels** (Easy, Normal, Hard, Impossible), each with a tuned, monotonic difficulty curve: enemies fire faster, bullets fly faster, and foes get tankier as you climb, while your survivability scales sensibly.
- **Power-ups** that drop from enemies — firepower boosts and health restores.
- **Kamikaze enemies** that telegraph and dive, dodgeable with good timing.

### Bosses
- **Two distinct end-of-run bosses**, each with a two-phase fight, an enrage state at 50% health, fan-shot and aimed attacks, and a dedicated health bar:
  - **Battleship** — the mechanical dreadnought, finale of *Impossible*.
  - **Alien** — an organic single-eyed horror, finale of *Hard*.

### Ships
- **Five selectable ships** with distinct looks, chosen from the main menu. Selection is cosmetic — hitboxes are identical across all ships to keep play perfectly fair.

### Accounts & Leaderboards
- **Email/password authentication** powered by Firebase: registration with username, email verification, password reset, and persistent auto-login.
- **Online leaderboards** (one per difficulty) showing your chosen username, so you can compete with friends for the top score.

### Audio & Feel
- **Full audio mixer** with separate Master, Music, and SFX volume controls, accessible from both the main menu and the in-game pause screen.
- **Game-feel polish** — screen shake, hit flashes, and tuned particle effects for satisfying impacts.

## Tech Stack
- **Engine:** Unity 6
- **Platform:** Android (portrait)
- **Authentication:** Firebase Authentication (email/password)
- **Leaderboards:** Unity Gaming Services
- **Input:** Unity's New Input System (touch + keyboard)

## Building
Open the project in Unity 6, switch the platform to Android, and build via *File ▸ Build Profiles ▸ Android*. The three scenes (MainMenu, GameScene, GameOver) must be included in the build.

> **Note:** Firebase requires a `google-services.json` configuration file (excluded from this repository for security). To build with authentication, register your own Android app in the Firebase console and place the generated file in `Assets/`.
