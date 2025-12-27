# Quick Manual

## Introduction

The Dialogue Editor is a visual, node-based tool used to design in-game dialogues. Dialogues are built as a **graph** composed of interconnected **nodes**, where connections define the flow of conversation, player choices, and conditional branching.

Basic workflow:
- **Right-click inside the graph** to create a new node
- **Connect ports between nodes** to define dialogue flow

The editor is designed to be:
- readable even for large dialogue graphs  
- easy to maintain and extend  
- cleanly separated between **runtime logic** and **editor-only helpers**

---

## Node Types

### Dialogue Node

The core building block of any dialogue.

Features:
- dialogue text
- optional **choices**, each leading to a different branch
- optional **restrictions and conditions**, such as:
  - `isVisitableOnlyOnce`
  - `needsItem`
  - `needsAttribute`
  - other gameplay-related constraints

Dialogue Nodes are **saved into runtime data** and directly affect dialogue behavior in-game.

---

### Conditional Node (IF)

A logic node used to branch dialogue flow based on conditions.

Currently supports:
- checking **Attributes** or **Skills**
- selecting the checked value via **enum**
- branching based on whether the condition passes or fails

Typical use cases:
- skill checks  
- attribute-based dialogue responses  
- locked / unlocked dialogue paths  

Conditional Nodes are **saved into runtime data**.

---

### Relay Node

A technical helper node.

- simply forwards execution flow
- contains no logic or data
- used purely to **keep the graph visually clean**
- ideal for long connections or complex layouts

Relay Nodes are **not saved into runtime data**.

---

### Note

A sticky-note style annotation node.

- used for comments, explanations, or TODOs
- has no impact on dialogue logic
- purely a design-time aid

Notes are **not saved into runtime data**.

---

## Groups

To improve readability and structure, nodes can be organized into **Groups**.

- groups are used to logically separate parts of a dialogue (e.g. topics, scenes, chapters)
- nodes may optionally belong to a group
- groups have no runtime logic and are editor-only

---

## Summary

The Dialogue Editor separates:
- **runtime-relevant nodes** (Dialogue, Conditional)
- **editor-only helpers** (Relay, Note, Groups)

This approach keeps dialogue logic clean while allowing complex graphs to remain readable and manageable.

---


# Dialogue Editor Manual

## 1. What the Editor Does
- Visual dialogue design using a node-based graph (branching, conditions, relay nodes, groups, sticky notes).
- Continuous validation (multiple start nodes, duplicate names, empty node/group names).
- Export to ScriptableObjects for the runtime dialogue system.
- Search, highlighted search results, fast “follow” navigation between connected nodes.
- Error panel with quick actions for jumping to problematic elements.

---

## 2. How to Open the Editor
- Unity menu: `Tools > Editors > Dialogue Editor` (opens a utility window).
- The window must be at least **900 × 400 px**, otherwise the UI may break.

---

## 3. Window Layout
- **Graph View** – main canvas with nodes; middle mouse button pans, mouse wheel zooms.
- **Toolbar** – file name, buttons `Errors`, `Save`, `Load`, `Clear`, `Reset`, and search.
- **Error Panel** – contains “Quick actions & auto-fix” and “Current problems”, opened via `Errors`.

---

## 4. Basic Workflow
1. (Optional) Use `Reset` to start with a clean graph and default file name.
2. Right-click inside the graph and create the desired node type.
3. Fill in names, text, and parameters; connect ports between nodes.
4. Watch the error indicator and fix duplicates or missing start nodes.
5. `Save` exports the graph and ScriptableObjects to  
   `Assets/ScriptableObjects/Dialogues/<FileName>`.
6. `Load` loads an existing graph from  
   `Assets/Editor/DialogueEditor/Graphs`.

---

## 5. Node Types
- **Dialogue Node** (`💬 Add Dialogue Node`):  
  Unique name, dialogue text, `Speaker` (colors the edge), `Focus On` button, choices (`✚ New Choice`) with text, `Follow` button, and `X` to remove choice.  
  Contains the “Visibility & Consequences Rules” foldout for additional settings.
- **Conditional Node** (`❓ Add Conditional Node`):  
  Input `IN`, outputs `SUCCESS` / `FAILURE`, dropdown `Kind` (Attribute / Skill), corresponding enums, comparison `>= ExpectedValue`.  
  Each output has its own `ConditionalNodeSaveData`.
- **Relay Node** (`🔀 Add Relay Node`):  
  Single-input flow forwarder, not persisted to assets, ignored during start-node validation.
- **Sticky Note** (`✍️ Add Sticky Note`):  
  Notes with configurable color, size, lock state, and bold text.
- **Group** (`🗂️ Add Group`):  
  Visual grouping of nodes; names inside groups are validated separately for duplicates.

---

## 6. Node Settings Management
- In the “Visibility & Consequences Rules” foldout, click `Add...` to open `CreateNodeSettingWindow` (modal).
- **Accessibility** tab defines choice visibility rules.  
- **Consequence** tab defines post-selection effects.
- After adding a rule, the node header indicator (`NodeIndicator`) updates.
- `Remove` deletes the selected rule from the list.

---

## 7. Connecting and Navigation
- Drag from an output port to an input port to create an edge; the editor filters incompatible ports and duplicates.
- Node context menu (`BaseNode` menu):
  - `Center on Node`
  - `Disconnect All / Input / Output Ports`
  - `Remove from Group`
  - `Delete`
- `Follow` on an output moves the camera to the connected node.
- Buttons under input ports show previous nodes (edge color + tooltip with text snippet).
- Toolbar search highlights matches while typing; pressing Enter moves the camera to the first match.

---

## 8. Graph Organization
- Use consistent node naming (e.g. `QuestA_Intro`) to avoid duplicates.
- Use relay nodes to keep branching readable (IOUtility remaps connections automatically on save).
- Use sticky notes for TODOs, comments, or narrative variants.

---

## 9. Validation and Diagnostics
- `ValidateGraph()` runs every 2 seconds:
  - detects multiple start nodes (highlighted in red),
  - checks for duplicate node/group names,
  - checks for empty names.
- `Errors` button shows issue count and a detailed list; tooltip displays specific problems.
- “Quick actions & auto-fix” offers `Focus …` buttons to jump to problematic nodes or groups.
- The validation scheduler stops when the window closes and restarts in `OnEnable`.

---

## 10. Saving and Loading
- **Save**:
  - Creates `Assets/Editor/DialogueEditor/Graphs/<FileName>Graph.asset` (graph layout).
  - Creates `Assets/ScriptableObjects/Dialogues/<FileName>/…` containing:
    - `DialogueContainerSO`
    - `GroupSO`
    - `NodeSO`
    - `ConditionalNodeSO`
  - `Global/Dialogues` contains ungrouped nodes.
  - `Groups/<GroupName>/Dialogues` contains grouped nodes.
  - Old folders are cleaned up when groups change (`UpdateOldGroups`, `UpdateOldGroupedNodes`).
- **Load** loads a `.asset` graph after `Clear` and restores nodes, groups, notes, and connections.
- **Clear** removes the current graph content (file name stays).
- **Reset** = `Clear` + reset file name to `DialoguesFileName`.

---

## 11. Tips and Best Practices
- Save regularly to generate ScriptableObjects for QA and testers.
- Split large branching sections using relay nodes to keep the graph readable.
- Always check t
