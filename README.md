# Dialogue Editor (Unity)

## Prerequisites
- [ ] Unity 6000.3.2f1 - but it might work on older versions too, I have started on pre-6 versions.
- [ ] Odin Inspector

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

 ![Toolbar](/Images/DE-1.png)
 Toolbar with many options - saving graph, converting graph to ScriptableObjects, loading graph, number of logical errors in graph, clear/reset buttons and search field.
 
<br clear="both"/>
 
![Graph](/Images/DE-2.png)
<br>
Various elements available to add to graph.

<br clear="both"/>

![Options](/Images/DE-3.png)
<br>
Options possible to set on Dialogue (NodeSO) Node, such as Consequences (add something to player, trigger something - trade, quest, attack...) or Visibility - if node is available as option (if player had met requirements).

<br clear="both"/>

![Speaker](/Images/DE-5.png)
<br>
Speaker Type as Player

<br clear="both"/>

![Graph](/Images/DE-6.png)
<br>
Example of branching

<br clear="both"/>

![Accessibility](/Images/DE-7.png)
<br>
Example of adding Consequence/Visibility

<br clear="both"/>

![Indicator](/Images/DE-8.png)
<br>
Indicator showing, that node has extra setting for its accessibility.

<br clear="both"/>

## Design Philosophy

The editor is intentionally modular. Advanced dialogue logic (conditions, requirements, visibility rules) is optional and can be removed entirely if not needed. This allows the tool to stay lightweight for simple projects while still supporting complex narrative structures when required.

The system was extracted and refactored from a production codebase and is released for adoption under the **MIT license**.

## License

MIT
