# Player System Refactoring Guide

## Overview

This document describes the refactoring of the player system from a tightly-coupled architecture to a modular, event-driven system.

## Architecture Changes

### Before (V1 - Original)

- **Tight Coupling**: Components directly accessed and modified each other
- **Mixed Responsibilities**: Single components handled multiple concerns
- **Direct Component Manipulation**: Components enabled/disabled each other directly
- **Scattered Events**: Events were defined in individual components

### After (New Architecture)

- **Loose Coupling**: Components communicate through events and interfaces
- **Single Responsibility**: Each component has a clear, focused purpose
- **Centralized Management**: PlayerController manages all components
- **Event-Driven Communication**: Centralized event system for decoupled communication

## New Components

### 1. IPlayerAbility Interface

```csharp
public interface IPlayerAbility
{
    bool CanActivate();
    void Activate();
    void Deactivate();
    bool IsActive { get; }
    void SetEnabled(bool enabled);
}
```

- Provides consistent interface for all player abilities
- Enables better state management and testing

### 2. PlayerController

- **Central Hub**: Manages all player components
- **State Management**: Handles component enabling/disabling based on player state
- **Event Coordination**: Centralizes collision and trigger handling

### 3. PlayerStateManager

- **State Tracking**: Manages current player state (Idle, Moving, Jumping, Dashing, Climbing, Dead)
- **Event Broadcasting**: Notifies other components of state changes
- **State Validation**: Ensures valid state transitions

### 4. PlayerCollisionHandler

- **Centralized Collision**: Handles all collision and trigger events
- **Event Broadcasting**: Distributes collision events to interested components
- **Decoupled Detection**: Separates collision detection from collision handling

### 5. PlayerEvents

- **Centralized Events**: All player-related events in one place
- **Global Access**: Static events for system-wide communication
- **Event Documentation**: Clear event naming and purpose

## Refactored Components

### PlayerMove

- **Interface Implementation**: Now implements IPlayerAbility
- **Event-Driven Collisions**: Uses PlayerCollisionHandler events
- **State Awareness**: Checks PlayerStateManager before actions
- **V1 Backup**: Original code preserved in commented regions

### PlayerDash

- **Interface Implementation**: Now implements IPlayerAbility
- **State Management**: Integrates with PlayerStateManager
- **Event Broadcasting**: Fires dash start/end events
- **V1 Backup**: Original code preserved in commented regions

### PlayerClimb

- **Interface Implementation**: Now implements IPlayerAbility
- **Event-Driven Collisions**: Uses PlayerCollisionHandler events
- **State Management**: Integrates with PlayerStateManager
- **V1 Backup**: Original code preserved in commented regions

## Benefits

### 1. Maintainability

- **Clear Separation**: Each component has a single responsibility
- **Easy Debugging**: Centralized event system makes tracking easier
- **Consistent Interface**: IPlayerAbility provides uniform component management

### 2. Extensibility

- **Easy Addition**: New abilities can be added by implementing IPlayerAbility
- **Plugin Architecture**: Components can be easily swapped or extended
- **Event System**: New features can subscribe to existing events

### 3. Testability

- **Interface Testing**: IPlayerAbility enables easy unit testing
- **Mock Objects**: Components can be mocked for testing
- **Isolated Testing**: Each component can be tested independently

### 4. Performance

- **Event Efficiency**: Events only fire when needed
- **State Optimization**: Components only update when relevant
- **Memory Management**: Proper event subscription/unsubscription

## Migration Guide

### For Developers

1. **Use New Events**: Subscribe to PlayerEvents instead of individual component events
2. **Check State**: Use PlayerStateManager.CurrentState for state checks
3. **Interface Usage**: Use IPlayerAbility methods for component management
4. **Controller Access**: Use PlayerController for centralized component access

### For Rollback (V1)

1. **Uncomment V1 Regions**: Remove comments from V1 regions in each file
2. **Comment New Code**: Comment out new implementation
3. **Remove New Components**: Disable PlayerController, PlayerStateManager, etc.
4. **Restore Direct Access**: Re-enable direct component access

## File Structure

```
Assets/Scripts/Player/
├── IPlayerAbility.cs          # Interface for player abilities
├── PlayerController.cs        # Central component manager
├── PlayerStateManagement.cs   # State management system
├── PlayerEvents.cs           # Centralized event system
├── PlayerCollisionHandler.cs # Centralized collision handling
├── PlayerHealth.cs          # Health system
├── PlayerMove.cs            # Movement system (refactored)
├── PlayerDash.cs            # Dash system (refactored)
├── PlayerClimb.cs           # Climbing system (refactored)
└── REFACTORING_GUIDE.md     # This documentation
```

## Usage Example

### Setting up a new player ability:

```csharp
public class PlayerWallJump : MonoBehaviour, IPlayerAbility
{
    public bool IsActive { get; private set; }

    public bool CanActivate()
    {
        return _stateManager.CurrentState == PlayerState.WallSliding;
    }

    public void Activate()
    {
        // Wall jump logic
        PlayerEvents.OnWallJump?.Invoke();
    }

    public void Deactivate()
    {
        // Cleanup logic
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}
```

### Subscribing to events:

```csharp
void Start()
{
    PlayerEvents.OnDashStart += HandleDashStart;
    PlayerEvents.OnDashEnd += HandleDashEnd;
}

void OnDestroy()
{
    PlayerEvents.OnDashStart -= HandleDashStart;
    PlayerEvents.OnDashEnd -= HandleDashEnd;
}
```

## Conclusion

This refactoring provides a solid foundation for future player system development while maintaining backward compatibility through V1 regions. The new architecture is more maintainable, testable, and extensible than the original tightly-coupled system.
