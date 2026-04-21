# Force of Nature: Last Seed

2D survival shooter built in Unity (C#), focused on scalable gameplay systems, modular architecture, and stable performance under high object count scenarios.

---

## 🚀 Overview

This project focuses on solving common problems in growing Unity projects:  
tight coupling between systems, difficult feature extension, and performance instability under load.

The goal is to build gameplay systems that remain maintainable, extensible, and performant as complexity increases.

---

## 🧩 Key Systems

### Data-driven Weapon System
Implemented using ScriptableObjects and a runtime modifier pipeline.

• Allows dynamic composition of shooting behavior  
• New weapon logic can be added without modifying core systems  
• Supports scalable feature expansion  

---

### Event-driven Combat Architecture

• Decouples gameplay systems (weapons, enemies, damage)  
• Reduces dependencies between components  
• Enables safe extension without introducing regressions  

---

### Modular Enemy System

• Segmented enemy structure with independent logic  
• Scalable behavior design for future complexity  
• Custom damage handling without tightly coupled logic  

---

### Object Pooling System

• Eliminates frequent Instantiate/Destroy calls  
• Reduces runtime allocations and GC spikes  
• Maintains stable performance under high object counts  

---

## 🧠 Architecture

The project is structured around clean architecture principles:

• Separation of gameplay logic, systems, and presentation  
• Event-based communication instead of direct dependencies  
• Systems designed to be extended without modification  
• Focus on long-term maintainability and scalability  

---

## 📈 Results

• Stable gameplay performance under heavy load  
• Reduced GC pressure through pooling  
• Systems that can be extended without rewriting core logic  
• Clear separation of responsibilities across systems  

---

## 🛠 Tech

Unity, C#, ScriptableObjects, Object Pooling, Event-driven Architecture, MVC/SRP principles

---

## 🎯 Purpose

This project demonstrates practical system design skills:
building scalable gameplay architecture, solving performance bottlenecks, and maintaining code quality as project complexity grows.

---

## 👨‍💻 Author

Oleksandr Tokarev  
Unity Gameplay Developer
