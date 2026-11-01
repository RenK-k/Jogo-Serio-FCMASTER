# Career and Match Flow Fixes

## Goal

Make press conferences, staff recruitment, calendar fixture details, and match kick-offs follow the requested flows. Restore match speed labels to real-time clock progression.

## Scope

- Route a due press conference through the Messages inbox and require a choice to attend, send the assistant, or skip before career time advances.
- Fix the calendar action labels and fixture-popup header layout.
- Add staff hires up to a clear roster limit, and make contract offers receive a candidate response before completion.
- Make the 0.5× setting advance the match clock at half real-time rate, with the other speeds scaling from that rate.
- Make kickoff perform a protected first touch and pass before normal interceptions, and keep all non-kickers in their own half until the ball is in play.

## Tasks

- [x] Trace current state and callbacks for each requested flow.
- [x] Implement inbox-driven press conference decisions and calendar popup fixes in `CareerHubController`.
- [x] Enforce staff capacity and add a response/confirmation step to staff negotiations.
- [x] Correct match clock scaling and the kickoff restart sequence in `SettingsPrefs` and `MatchEngine`.
- [x] Inspect changed call paths and run static whitespace/reference checks; leave Unity play-mode verification to the user.

## Verification

- Reviewed changed call paths and bilingual labels.
- `git diff --check` is clean apart from Git's existing LF-to-CRLF notices. No Unity Editor, play mode, build, or tests were run.
