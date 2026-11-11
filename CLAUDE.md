# NET.runner-92: Claude Code

The rules for every agent are in `AGENTS.md`, loaded here:

@AGENTS.md

This file adds only what is specific to Claude Code on Samuel's Linux
machine. Shared rules go in `AGENTS.md`, never here, so that AdaM404's
agent (Codex) sees them too.

## How we split work

Claude writes all code (tag **CLAUDE**). Samuel decides what gets built and
reviews the pull request. AdaM404 does the art (tag **ART**). This was
decided on 2026-11-09 and replaced the earlier YOU WRITE and PAIR tags.
Each pull request explains its non-obvious choices so the review is quick.

## Skills

- `session-start` begins a session: sync (`AGENTS.md` section 1), read
  `agent.md`, list the decisions, and register the task.
- `session-end` closes a session: verify, update `agent.md` and `docs/`,
  and commit.
- `unity-cli` is the reference for the `unity` command.

## Editor open: the Unity CLI bridge

The bridge runs through `com.unity.pipeline`.

```bash
unity status                          # connected and "ready"?
unity command console --level error   # read the console
unity command recompile               # then: unity command recompile_status
unity command run_tests --mode editor                        # EditMode tests
unity command run_tests --mode playmode --async_tests true   # PlayMode tests, then
unity command test_status                                    # poll until "completed"
bin/unity-shot <name> [game|scene]    # screenshot into Logs/shots/
bin/unity-inspect [scene|environment]  # read-only facts
```

`unity command` with no arguments lists everything the editor exposes.
With the editor closed, use the `bin/unity-*` scripts in `AGENTS.md`
section 8. They exit with code 3 if the editor has the project open. Full
logs land in `Logs/`, which git ignores.

Decisions on GitHub:

```bash
gh issue view <n>                     # read one
gh issue comment <n> --body "…"       # record an answer or an outcome
gh issue edit <n> --add-label "status: accepted" --remove-label "status: proposed" --remove-label "needs Samuel"
```
