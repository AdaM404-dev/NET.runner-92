# Decisions and next steps

Every next step of the game and every open decision is a **GitHub issue**
with the label [`decision`](https://github.com/AdaM404-dev/NET.runner-92/issues?q=label%3Adecision).
Start with the pinned **[Roadmap, issue #5](https://github.com/AdaM404-dev/NET.runner-92/issues/5)**:
it lists everything in order.

Decisions used to be Markdown files in this folder. The only one, "project
knowledge lives in `docs/`", is now
[issue #19](https://github.com/AdaM404-dev/NET.runner-92/issues/19). This page
explains how the system works.

## What a decision issue contains

Every issue uses the same ten sections, whether written through the form on
GitHub or by an AI agent. [Issue #6](https://github.com/AdaM404-dev/NET.runner-92/issues/6)
(renaming the layers) is the complete example.

| Section | Answers |
| --- | --- |
| Why | What is wrong or missing today, with evidence |
| Decision | What we will do; for an open question, "Proposed:" and the recommendation |
| What will change | Files, scenes, prefabs, settings, docs; what players and the other person notice |
| What stays the same | What does not change, so nobody worries about it |
| Risks and how to undo | What could go wrong, and how to go back |
| Steps | The order of the work, ending with a pull request |
| How we check it worked | Commands, Play-mode checks, screenshots |
| Alternatives we rejected | The other options and why not |
| Depends on | Issues that must come first |
| Related docs | Notes in this vault |

## Labels

| Label | Meaning |
| --- | --- |
| `decision` | a next step or a decision; every issue in this system has it |
| `status: proposed` | written down, waiting for agreement |
| `status: accepted` | everyone needed has agreed; the work can start |
| `status: in progress` | being worked on |
| `needs Samuel`, `needs AdaM404` | whose answer or agreement is still missing |
| `YOU WRITE`, `PAIR`, `CLAUDE`, `ART` | who builds it (see the work split in `CLAUDE.md`); `ART` is AdaM404's work |
| `size S`, `size M`, `size L` | under an hour, about one session, several sessions |

The **assignee** is whoever must act next: answer, or do the work. The
**Foundation** [milestone](https://github.com/AdaM404-dev/NET.runner-92/milestone/1)
groups the seven steps that come before new gameplay scripts and shows their
progress.

## Life of a decision

```
status: proposed ──(everyone agrees)──► status: accepted ──(work starts)──► status: in progress
        │                                                                          │
        └──(no)──► closed as "not planned"                 pull request merged ────┴──► closed as "completed"
```

1. **Propose**: open an issue with the **Decision or next step** form on
   GitHub (New issue), or ask Claude to write one. Add `needs …` labels for
   everyone who must agree, and assign them.
2. **Agree**: each person listed answers with a comment: "agree", or what
   they would change. Their `needs …` label is then removed. When none is
   left, the status becomes `status: accepted` and the assignee becomes
   whoever does the work.
3. **Do**: set `status: in progress`. The pull request says
   `Closes #<number>` and fills in the **Outcome** section of the pull
   request template.
4. **Done**: merging the pull request closes the issue as completed. Tick
   its box in the Roadmap issue.
5. **Changing your mind**: comment on the issue, then update its Decision
   section. A closed decision that is reversed gets a new issue that links
   the old one.

## Finding decisions

| To see | Use |
| --- | --- |
| Everything open, in order, in the terminal | `bin/decisions` (add `--all` for closed ones) |
| The order and what is waiting | the pinned Roadmap issue (#5) |
| What waits for AdaM404 | [label `needs AdaM404`](https://github.com/AdaM404-dev/NET.runner-92/issues?q=is%3Aopen+label%3A%22needs+AdaM404%22) |
| What waits for Samuel | [label `needs Samuel`](https://github.com/AdaM404-dev/NET.runner-92/issues?q=is%3Aopen+label%3A%22needs+Samuel%22) |
| One issue in the terminal | `gh issue view 6` |

## Rules for AI agents

- Read the issue before working on a planned change. Do not start a step
  unless it has `status: accepted`, or Samuel says to go ahead.
- When starting, set `status: in progress`; the pull request says
  `Closes #<number>` and fills in the Outcome section.
- Write new issues with the ten sections above, in plain language, with
  evidence (file paths, line numbers, measured values).
- Record answers given in chat as a comment on the issue, quoting who
  decided.
- Agents without GitHub access ask the person they work with to paste the
  issue.
