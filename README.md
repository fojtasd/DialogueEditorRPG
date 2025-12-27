# Dialogue Editor (Unity)

A modular dialogue editor built on top of **Unity Experimental GraphView**, designed for creating complex, branching dialogue flows while remaining lightweight and extensible.

The editor allows you to visually compose dialogue graphs made of interconnected nodes, which are serialized into **ScriptableObjects**. Each node holds direct references to its connected nodes, making runtime evaluation straightforward and fast.

## Features

- Graph-based dialogue authoring using Unity Experimental GraphView  
- Three node types:
  - **Dialogue Node (`NodeSO`)** – contains the dialogue text and core dialogue data
  - **Conditional Node (`ConditionalNodeSO`)** – evaluates conditions based on attributes, skills, or other enum-driven values
  - **Relay Node** – purely visual helper node for keeping complex graphs readable
- Data-driven architecture – dialogues are stored as ScriptableObjects with explicit node connections
- Optional node settings system – nodes can define visibility or availability rules (e.g. required skills, attributes, items, intel, or previously visited nodes)
- Scales from simple to complex – advanced features can be completely omitted for basic dialogue use cases

## Design Philosophy

The editor is intentionally modular. Advanced dialogue logic (conditions, requirements, visibility rules) is optional and can be removed entirely if not needed. This allows the tool to stay lightweight for simple projects while still supporting complex narrative structures when required.

The system was extracted and refactored from a production codebase and is released for adoption under the **MIT license**.

## License

MIT
