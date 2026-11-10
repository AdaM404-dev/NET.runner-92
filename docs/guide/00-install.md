# Download and install the project

From nothing to walking around the hospital: what to install, how to get
the project, how to open it, and how to keep it up to date. It covers
Windows and Linux. Plan on an hour or more the first time, mostly downloads
and Unity's first import.

Written 2026-11-10, after #31 (the hospital on HDRP). Sizes were measured on
Samuel's Linux clone that day.

## Just want to play? Download a build

No Git, no Unity: test builds are on the repository's
[Releases page](https://github.com/AdaM404-dev/NET.runner-92/releases).

1. Under the newest release, open **Assets** and download the
   `…_Windows-x64.zip`.
2. Unpack the whole zip into a folder (right-click → Extract All). Keep
   every file and folder in it together.
3. Open the unpacked folder and double-click `NET.runner-92.exe`. The
   builds are not signed, so Windows
   may show "Windows protected your PC": click **More info → Run anyway**.
4. Controls are in step 6 below. There is no menu yet: quit with
   **Alt+F4**. If it runs slowly, press **F2** for a lower quality level
   and **F3** to see the frame rate.

You need Windows 10/11 64-bit and a graphics card with Direct3D 11 or 12.
Report problems as a GitHub issue, with what you did and what you saw.

To help measure performance (from alpha test 3): double-click
`run-benchmarks.bat` in the game folder, wait about 6 minutes without
touching anything, then send the `benchmark` folder it creates. Details:
[[systems/performance]].

The rest of this guide is for working on the project.

## What you need

| | |
| --- | --- |
| Computer | Windows 10/11 or Linux, 64-bit (x86-64). The project is PC only (#15). |
| Graphics card | One that supports **Direct3D 11/12** (Windows) or **Vulkan** (Linux). The game uses HDRP, Unity's high-end render pipeline, which does not run on OpenGL. Any recent NVIDIA, AMD or Intel GPU with current drivers is fine. |
| Disk space | About **15 GB** free: the Unity editor itself, about 1 GB for the project (code, art and Git history), and 4–5 GB for the `Library/` folder Unity builds on first open. |
| Accounts | A free **Unity account** for the editor licence. A **GitHub account** only if you will push changes. |

## 1. Install Git and Git LFS

The project lives in a Git repository on GitHub. Its art (models, textures,
audio, the hospital's mesh data) is stored with **Git LFS** (Large File
Storage), an add-on that keeps big files outside the normal Git history.
Without LFS you get small placeholder text files instead of the art.

**Windows:** install [Git for Windows](https://git-scm.com/download/win).
Git LFS is included.

**Linux:** install both from your distribution's packages:

```bash
sudo pacman -S git git-lfs          # Arch, Omarchy
sudo apt install git git-lfs        # Ubuntu, Debian
```

Then, once per computer, switch LFS on (any terminal; on Windows use
**Git Bash**):

```bash
git lfs install
```

It prints `Git LFS initialized.`

## 2. Download the project

Choose a folder for your projects, then clone:

```bash
cd ~/Projects                       # or any folder you like
git clone https://github.com/AdaM404-dev/NET.runner-92.git
cd NET.runner-92
```

This downloads about 1 GB. Check that the art arrived:

```bash
git lfs ls-files | wc -l            # about 350 files
git lfs ls-files | head -3          # each line has a * : the real file is there
```

A `-` instead of `*` means only the placeholder is there; run `git lfs pull`.

> Do not use GitHub's **Download ZIP** button. The ZIP has placeholders
> instead of the art, and Unity shows missing or magenta (bright pink)
> assets.

> A copy cloned before **2026-11-09** is out of date: the history was
> rewritten when the art moved to LFS (#17). Clone again; never push from
> the old copy.

## 3. Install Unity Hub

**Unity Hub** is Unity's launcher: it installs editor versions and opens
projects.

**Windows:** download it from [unity.com/download](https://unity.com/download)
and run the installer.

**Linux:**

- Arch / Omarchy: the `unityhub` package from the AUR, for example
  `yay -S unityhub`.
- Ubuntu / Debian: follow Unity's
  [Linux installation guide](https://docs.unity3d.com/hub/manual/InstallHub.html),
  which adds Unity's own package repository.

Start the Hub, sign in with your Unity account, and when asked for a
licence choose **Unity Personal** (free).

## 4. Install the exact editor version

The project needs Unity **6000.6.2f1**. Another version would offer to
"upgrade" the project and change many files, so use this one exactly; it
is written in `ProjectSettings/ProjectVersion.txt`.

The quickest way: with the Hub installed, open this link in your browser.
It opens the Hub on the right version:

```
unityhub://6000.6.2f1/770e33f6875c
```

Otherwise, in the Hub: **Installs → Install Editor → Archive**, and pick
6000.6.2f1.

When the Hub asks for **modules**, add the build support for your own
system, so you can make a player build:

| You are on | Add |
| --- | --- |
| Windows | Windows Build Support (IL2CPP) is optional; Mono is built in. Linux Build Support only if you build for Linux. |
| Linux | Linux Build Support (Mono). Windows Build Support (Mono) if you build for Windows too. |

Leave "Microsoft Visual Studio" unticked if you use Rider or already have
an editor (see step 8).

On Linux the scripts in `bin/` expect the editor at
`~/Unity/Hub/Editor/6000.6.2f1/Editor/Unity`, the Hub's default place. If
yours is elsewhere, set `UNITY_EDITOR=/path/to/Unity`.

## 5. Open the project

1. In the Hub, **Projects → Add → Add project from disk**, and choose the
   `NET.runner-92` folder: the one that directly contains `Assets`,
   `Packages` and `ProjectSettings`.
2. Click the project to open it with 6000.6.2f1.
3. The first open is slow: Unity downloads the packages (HDRP, Input
   System, …), imports every asset, compiles the HDRP shaders and builds
   `Library/`. A progress bar shows each stage. Later opens take seconds
   to a minute.

When it is done, check two things:

- **The console is clean.** Window → General → Console. Red errors after
  the import mean something went wrong; see "If something goes wrong".
- **The graphics API.** The editor's title bar ends with the API in angle
  brackets. On Linux it must say **`<Vulkan>`**; on Windows `<DX11>` or
  `<DX12>`. The project's Player Settings select Vulkan for Linux, so you
  should not have to do anything.

## 6. Play the hospital

1. In the **Project** window open `Assets/NETRunner92/Scenes/Hospital_Exploration.unity`
   (double-click it).
2. Press **Play** (the ▶ button at the top). Click into the Game view to
   capture the mouse.
3. Controls:

| Key | Does |
| --- | --- |
| W A S D | walk; hold **Shift** to sprint |
| Mouse | look |
| E | use what the dot points at: doors, lift buttons, the reception terminal |
| Q | raise or stow the LATCH-07 tablet |
| R | cancel the tablet action |
| 1 | CCTV on the tablet, after solving the reception terminal |
| Esc | free the mouse; left-click to capture it again |
| F2 | graphics quality: Low, Medium, High (remembered) |
| F3 | show or hide the FPS counter |

4. Press **Play** again to stop. Changes made during Play mode are undone
   when it stops.

What to try first: walk through the entrance to the reception, aim at the
monitor with the amber strip from behind the desk and press E. More in
[[systems/level-hospital]] and [[systems/player]].

## 7. Keep it up to date

Others push changes to `main` on GitHub. To get them, close Unity (or at
least let it finish what it is doing), then:

```bash
git switch main
git pull
```

LFS downloads new art automatically during the pull. Open the project
again; Unity imports what changed.

**Linux only:** the Linux editor adds a few lines of its own to
`Packages/manifest.json`, `packages-lock.json` and two `ProjectSettings`
files (Linux-only tools; details in [[architecture/tooling]]). Those
changes are never committed, and they can block `git pull`. Put them
aside for the pull and bring them back after:

```bash
git stash
git pull
git stash pop
```

Never commit those four files unless a change to them is the point of
your work.

## 8. Optional: set up for working on the code

Only needed if you change code or run the project's tools.

| Tool | What for | Notes |
| --- | --- | --- |
| A C# editor | writing code | JetBrains Rider (free for non-commercial use) or Visual Studio (Windows). In Unity: Edit → Preferences → External Tools → External Script Editor. The project includes both integrations. |
| [GitHub CLI](https://cli.github.com/) (`gh`) | decision issues, pull requests, `bin/decisions` | `gh auth login` once |
| Unity CLI (`unity`) | lets Claude Code drive the open editor | see [[architecture/tooling]]; version used: `1.0.0-beta.5` |

With the editor closed you can compile and test from a terminal on Linux:

```bash
bin/unity-compile        # import and compile; prints compiler errors only
bin/unity-test edit      # EditMode tests
```

The full list is in `CLAUDE.md` and [[architecture/tooling]]. These
scripts are bash and are only tested on Linux.

## If something goes wrong

| You see | Why | Fix |
| --- | --- | --- |
| Magenta (bright pink) hospital surfaces | the art is LFS placeholders | `git lfs install`, then `git lfs pull`, then reopen Unity |
| Linux: the Game view is black or empty, title bar says `<OpenGLCore>` | HDRP does not render on OpenGL | check Edit → Project Settings → Player → Other Settings: Linux graphics APIs must list Vulkan only; or start the editor with `-force-vulkan`. Install Vulkan drivers (`vulkan-radeon`, `vulkan-intel` or the NVIDIA driver) |
| The Hub wants to "upgrade" the project | wrong editor version | close without upgrading; install 6000.6.2f1 (step 4) |
| Unity opens in **Safe Mode** | the code does not compile | read the errors in the console; ask before changing anything, the code on `main` should always compile |
| `git pull` refuses because of local changes (Linux) | the editor's own edits to `Packages/`, `ProjectSettings/` | the stash commands in step 7 |
| `git push` is rejected, history differs | the copy was cloned before 2026-11-09 | clone again (step 2) |

Still stuck: write it in an issue on GitHub, with the console text and
what you did.

## Next

- [[systems/level-hospital]]: what is in the hospital and how it is built.
- [[architecture/project-map]]: where everything is.
- [[guide/01-unity-in-this-project]] and [[guide/02-guided-tour]]: Unity
  explained on this project's objects (being rewritten for the hospital).
