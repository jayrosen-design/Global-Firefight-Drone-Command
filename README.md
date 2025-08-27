# Global Firefight - Drone Command

**🚧 Work in Progress - NDIA Hackathon Project 🚧**

## Project Overview

Global Firefight - Drone Command is an innovative real-time strategy and simulation game developed for the NDIA (National Defense Industrial Association) Hackathon. The project combines real-world wildfire data with strategic drone fleet management to create an immersive firefighting command experience.

## Project Vision

This game transforms players into incident commanders managing international drone fleets to combat global wildfires using real-time NASA satellite data. Players switch between a strategic global view for fleet deployment and a tactical drone pilot view for precision firefighting operations.

### Key Features (In Development)

- **Real-Time Data Integration**: Live wildfire data from NASA FIRMS and EONET APIs
- **Dual-Mode Gameplay**: 
  - Strategic RTS view on a 3D globe for fleet management
  - Tactical third-person drone control for precision operations
- **Authentic Drone Fleet**: Based on real international firefighting aircraft and systems
- **Economic Impact Scoring**: Players scored on property and lives saved vs. operational costs
- **Photorealistic 3D Environment**: Powered by Google's Photorealistic 3D Tiles via Cesium for Unity

## Current Development Status

### ✅ Completed Components
- Core game architecture and state management
- NASA FIRMS API integration for real-time fire data
- Fire positioning and visualization on Cesium 3D globe
- Interactive fire selection system
- Camera transitions from global to drone view
- Basic drone deployment system
- Geospatial coordinate conversion system

### 🔄 In Progress
- Fire suppression mechanics and visual effects
- Complete drone fleet management system
- Economic scoring system implementation
- UI/UX for both strategic and tactical views
- Google Maps 3D tiles integration for tactical view

### 📋 Planned Features
- Multi-national drone specifications and capabilities
- Advanced fire spread simulation
- Resource management and logistics
- Post-mission analytics and scoring
- Performance optimization for WebGL deployment

## Technical Stack

- **Engine**: Unity 2023.x with Universal Render Pipeline (URP)
- **Geospatial**: Cesium for Unity (3D globe and coordinate systems)
- **Data Sources**: 
  - NASA FIRMS (Fire Information for Resource Management System)
  - NASA EONET (Earth Observatory Natural Event Tracker)
  - Google Maps Platform (Photorealistic 3D Tiles)
- **Platform Targets**: PC (Primary), WebGL (Secondary)

## Project Structure

```
Assets/
├── Scripts/           # Core game logic and systems
│   ├── Core/         # Game managers and state systems
│   ├── Data/         # Data models and structures
│   ├── API/          # External API integrations
│   ├── Geospatial/   # Location and coordinate systems
│   ├── Fire/         # Fire simulation and effects
│   ├── Drones/       # Drone fleet management
│   ├── Systems/      # Scoring, camera, and other systems
│   └── UI/           # User interface components
├── Research/         # Technical documentation and requirements
└── README.md         # This file
```

## Development Team

This project is being developed as part of the NDIA Hackathon, focusing on innovative applications of real-time data for emergency response and strategic planning.

## Getting Started

### Prerequisites
- Unity 2023.x or later
- NASA Earthdata API key (for FIRMS data)
- Google Cloud API key (for Maps Platform)
- Cesium for Unity plugin

### Current Build Status
The project currently compiles successfully with all core systems functional. The basic fire interaction and camera transition systems are operational for demonstration purposes.

## License

This project is developed for the NDIA Hackathon and is currently proprietary. Licensing terms will be determined based on hackathon outcomes and further development decisions.

---

**Note**: This is an active development project. Features and implementation details are subject to change as development progresses toward the hackathon deadline.
