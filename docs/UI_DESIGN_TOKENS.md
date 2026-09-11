# UI Design Tokens — MVP Baseline

**Status:** presentation baseline for future Avalonia implementation. Prefer theme resources over duplicated literal values.

## Layout

- base spacing unit: `8 px`;
- page outer padding: `24 px`;
- card internal padding: `16 px`;
- normal horizontal/vertical control gap: `8–12 px`;
- section gap: `24 px`;
- standard button height target: `36–40 px`;
- main window default: approximately `760 × 560 px`;
- main window minimum: approximately `620 × 440 px`.

## Typography roles

- page/window title: `24–28 px`, semibold;
- section title: `16–18 px`, semibold;
- draft/application title: `15–16 px`, semibold;
- normal body/preview: `14 px`;
- secondary metadata/helper: `12–13 px`.

Use the platform/Avalonia default font stack unless later visual testing demonstrates a concrete reason to change it.

## Surface hierarchy

1. window/page background;
2. subtle card/secondary surface;
3. dialog/preview surface;
4. system/accent primary action;
5. destructive semantic styling only for discard/delete.

Do not hardcode light-theme RGB values into controls. Use theme-aware resources so system light/dark mode works naturally.

## Buttons

- one primary action maximum per card/dialog;
- Restore = primary only when available;
- Preview/Copy = secondary;
- Discard = tertiary/destructive/menu action;
- disabled Restore must not look like a warning state;
- busy state preserves control width to avoid layout jumps.

## Cards

Cards are content-light. They contain app/context metadata, updated/expiry time, and actions — no automatic decrypted body snippet in MVP.

Avoid large shadows, gradients, glass effects, promotional illustrations, status badges for routine safe behavior, or animation that implies continuous recording.

## Motion

Only short functional transitions if Avalonia/system defaults provide them. No pulsing “protected/recording” indicator and no per-keystroke/saved animation.

## Icons

Use a coherent simple icon family or platform-like glyphs. Icon-only buttons require accessible names/tooltips. Do not use emoji as production UI icons.

## Responsive behavior

At narrow window widths:

- action buttons may wrap to a second row;
- metadata truncates/ellipsizes before action labels;
- Preview remains usable;
- cards do not horizontally scroll for normal content.

## Theme/accessibility

- light and dark system themes;
- visible focus ring;
- no color-only status;
- text scaling should not clip primary actions;
- minimum contrast follows platform accessibility expectations;
- prefer native/system theme semantics over custom color palettes.
