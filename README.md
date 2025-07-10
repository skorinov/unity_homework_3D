# Unity 6 3D Project - Landscape Explorer

## Project Overview

A 3D scene in Unity 6 featuring a character controller, terrain landscape, and interactive objects built with ProBuilder.

## Features

### 1. Terrain System
- **Size**: 100x100 units
- **Geometry**: Varied topography created with multiple terrain brushes
- **Vegetation**: Various trees and grass types
- **Skybox**: Custom sky material

### 2. ProBuilder Architecture
- **Staircase with arch**: Decorative architectural element
- **House**: Explorable structure accessible to player
- **Interactive design**: All objects within player reach

### 3. Character Controller

#### Movement Features:
- **Walking/Running**: Smooth terrain navigation
- **Camera**: Free mouse look with angle limits
- **Jumping**: Realistic physics with gravity
- **Sprinting**: Speed boost functionality

#### Technical Details:
- New Unity Input System implementation
- Multi-ray ground detection for stability
- Slope handling with automatic sliding on steep surfaces
- Air control with limited mid-air movement

## Visual Effects (Particle System)

### Wind Effect (Sprint)
- **Activation**: Sprint + movement + grounded
- **Location**: Player camera
- **Purpose**: Speed visualization during fast movement

### Landing Effect
- **Activation**: Landing with sufficient fall velocity
- **Type**: Splashing mud/swamp particles
- **Features**: Velocity threshold and cooldown protection

## Controls

- **WASD**: Movement
- **Mouse**: Camera rotation
- **Space**: Jump
- **Shift**: Sprint

## Technical Specifications

- **Unity Version**: Unity 6
- **Input System**: New Unity Input System
- **Core Components**: CharacterController, ParticleSystem
- **Optimization**: All textures optimized for minimal file size

## Character Settings

### Movement
- Walk Speed: 10 units/sec
- Run Speed: 20 units/sec
- Jump Height: 2 units
- Gravity: -20 units/sec²
- Air Control: 30% of normal speed

### Camera
- Mouse Sensitivity: 20 units
- Max Look Angle: 80° up/down
- Cursor Lock: Enabled during gameplay

### Ground Detection
- Raycast Distance: 1.2 units
- Max Slope Angle: 45°
- Multi-ray System: 5 rays for stable detection