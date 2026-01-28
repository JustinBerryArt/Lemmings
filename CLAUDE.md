# CLAUDE.md - Lemmings Project Guide

## Project Overview

**Lemmings** is a Unity framework for tracking spatial relationships between GameObjects and computing configurable metrics from those relationships. Developed at the Yale Center for Immersive Technology in Pediatrics (XRPediatrics), it provides a flexible system for defining, evaluating, and exposing spatial data for XR/VR applications.

**Documentation**: https://lemmings.xrpeds.org
**Contact**: Justin.Berry@yale.edu

## Repository Structure

```
Assets/
├── Lemmings/
│   ├── Scripts/
│   │   ├── Classes/              # Core MonoBehaviour components
│   │   │   ├── Lemming.cs        # Tracked object component
│   │   │   ├── LemmingShepherd.cs # Singleton manager for all Lemmings
│   │   │   ├── LemmingRelationshipProxy.cs  # Trigger/gaze detection
│   │   │   ├── LemmingProxyVisualizer.cs    # Debug visualization
│   │   │   └── LemmingSecondaryMetric.cs    # Additional metric component
│   │   │
│   │   ├── ScriptableObjects/    # Asset definitions
│   │   │   ├── LemmingRelationship.cs  # Defines relationships between Lemmings
│   │   │   └── LemmingHerdSnapshot.cs  # Collection of Lemmings for a setup
│   │   │
│   │   ├── Namespace/            # Core types and utilities
│   │   │   ├── LemmingDatum.cs             # Type-agnostic value container
│   │   │   ├── LemmingRelationshipStruct.cs # LemmingRelation and settings
│   │   │   ├── LemmingEnumDeclarations.cs  # All enums (FamilyType, Metrics, etc.)
│   │   │   ├── LemmingInterfaces.cs        # ILemming, ILemmingRelationship, etc.
│   │   │   ├── LemmingConverterStructs.cs  # Value converters (Floater, Intr, etc.)
│   │   │   ├── LemmingUtilities.cs         # Utility classes and structs
│   │   │   └── LemmingTrackingStatus.cs    # Tracking state management
│   │   │
│   │   ├── InputSystem/          # Unity Input System integration
│   │   │   ├── LemmingDevice.cs            # Custom InputDevice
│   │   │   ├── LemmingDeviceManager.cs     # Runtime device updates
│   │   │   ├── LemmingDeviceInitializer.cs # Device registration
│   │   │   └── LemmingLayoutGenerator.cs   # Dynamic layout generation
│   │   │
│   │   ├── UI/                   # Runtime UI components
│   │   │   ├── LemmingUIRemappingController.cs  # User remapping interface
│   │   │   ├── LemmingUIDataRow.cs              # Data display row
│   │   │   └── LemmingUIVisibilityController.cs # UI state management
│   │   │
│   │   └── Editor/               # Unity Editor extensions
│   │       └── LemmingEditorUtilities.cs   # Custom editors and windows
│   │
│   ├── Prefabs/                  # Pre-configured GameObjects
│   │   ├── LemmingShepherd.prefab
│   │   ├── LemmingDefaultSetup.prefab
│   │   ├── LemmingRelationshipProxy.prefab
│   │   └── LemmingUI.prefab
│   │
│   └── Scenes/                   # Example scenes
│       ├── Setup.unity
│       └── Testing.unity
│
├── Resources/
│   └── _Generated/               # Auto-generated enum files
│       └── LemmingName_*.cs      # Per-herd name enums
│
└── 3rdParty/                     # Third-party dependencies
    ├── Settings/                 # URP render pipeline settings
    └── TextMesh Pro/             # Text rendering
```

## Core Architecture

### Primary Namespace: `Lemmings`

All core types live in the `Lemmings` namespace. Input system types are in `Lemmings.Input`, and UI types are in `Lemmings.UI`.

### Key Components

#### 1. Lemming (`Lemming.cs`)
A MonoBehaviour component attached to any GameObject you want to track.

```csharp
// Key properties
public string Name { get; }           // Assigned reference name
public GameObject Source { get; }     // The attached GameObject
public float Confidence { get; }      // Tracking confidence (0-1)
public Vector3 Velocity { get; }      // Current velocity (smoothed or from Rigidbody)
public bool followingObject { get; }  // True if tracking external object
```

- Auto-registers with `LemmingShepherd` on Start
- Supports tracking another GameObject via `objectToTrack` field
- Computes smoothed velocity for objects without Rigidbody

#### 2. LemmingShepherd (`LemmingShepherd.cs`)
Singleton manager that tracks all Lemmings and their relationships.

```csharp
// Access the singleton
LemmingShepherd.Instance
LemmingShepherd.All  // All registered Lemmings

// Key collections
public List<Lemming> Lemmings { get; }
public List<LemmingRelationship> Relationships { get; }
public Dictionary<Lemming, List<LemmingRelationship>> LemmingToRelationships { get; }
```

- Persists across scene changes (`DontDestroyOnLoad`)
- Requires `LemmingDeviceManager` and `LemmingUIVisibilityController` components
- Updates all relationships each frame

#### 3. LemmingRelationship (`LemmingRelationship.cs`)
ScriptableObject defining a spatial relationship with configurable metrics.

```csharp
// Key properties
public FamilyType familyType;     // Single, Couple, Throuple, or Group
public Enum Metric { get; }       // The active metric enum
public float Min, Max;            // Normalization bounds
public LemmingCurveType curveType; // Easing curve
public LemmingDatum Datum { get; } // Current computed value
public ILemmingConverter Converter { get; } // Value conversion utilities
public RelationshipStatus Status { get; }   // Over, Under, InRange, None

// Events
public event Action<LemmingDatum> OnDatumUpdated;
public event Action OnOver, OnUnder, OnInRange;
```

#### 4. LemmingRelation (struct in `LemmingRelationshipStruct.cs`)
Struct for computing spatial metrics based on family type.

```csharp
// Family types and their metrics
FamilyType.Single   -> SingleMetric (Position, Rotation, Movement, Trigger)
FamilyType.Couple   -> CoupleMetric (Position, Rotation, Distance, Movement, Difference, Trigger)
FamilyType.Throuple -> ThroupleMetric (Position, Rotation, Distance, Angle, Density, Movement, Trigger, RotationAroundAxis, Size)
FamilyType.Group    -> GroupMetric (Position, Rotation, Density, Size, Movement, Trigger, RotationAroundAxis)

// Usage
var relation = new LemmingRelation(familyType, references, metric, settings);
object result = relation.Evaluate();
LemmingDatum datum = relation.ToDatum();
```

#### 5. LemmingDatum (`LemmingDatum.cs`)
Type-agnostic container for relationship values.

```csharp
// Supported types
LemmingValueType.Float, Int, Bool, Vector3, Quaternion, String

// Usage
datum.SetValue(myFloat);
float val = datum.AsFloat();
Vector3 vec = datum.AsVector3();
var converter = datum.GetConverter(min, max) as ILemmingConverter;
```

### Value Conversion System

The converter system provides normalization, curve application, and threshold detection:

```csharp
ILemmingConverter converter = relationship.Converter;

converter.Raw;        // Original value
converter.Normalized; // 0-1 normalized value
converter.ToCurve(curve); // Curved value
converter.Over;       // True if above Max
converter.Under;      // True if below Min
converter.InRange;    // True if between Min and Max
converter.AsAxis;     // -1 to 1 (for input mapping)
```

### Unity Input System Integration

Relationships can expose their values as Unity Input System controls:

1. Set `useInputSystem = true` on the relationship
2. Access controls via the virtual `Lemmings` device:

```csharp
// Control naming convention: {RelationshipID}.{Property}
// Properties: RawValue, Normalized, Curved, Over, Under, InRange, AsAxis

var device = InputSystem.GetDevice("Lemmings");
float normalized = device["MyRelationship.Normalized"].ReadValue<float>();
```

## Development Conventions

### Code Style

- Use `#region` blocks for organizing code sections
- Method separators: `// ---------------- Method Break -------------------------------------------------------`
- Region separators: `//------------------------------------------------------` followed by `// XXXXXXXXXXXXXXX    REGION BREAK        XXXXXXXXXXXXXX`
- Comprehensive XML documentation on all public members
- Use `[Icon("Assets/Input/Icons/LemmingPale.png")]` attribute on classes

### Naming Conventions

- **Components**: `Lemming*` prefix (e.g., `LemmingShepherd`, `LemmingDevice`)
- **Enums**: Descriptive names matching their purpose (`FamilyType`, `SingleMetric`, `CoupleMetric`)
- **Structs**: `Lemming*` prefix for data containers (`LemmingDatum`, `LemmingRelation`)
- **Interfaces**: `ILemming*` prefix (`ILemming`, `ILemmingRelationship`, `ILemmingConverter`)

### ScriptableObject Workflow

Relationships and Herd Snapshots are created through custom Editor windows (Lemmings menu), not via CreateAssetMenu.

### Event-Driven Updates

- Relationships fire events on status changes (`OnOver`, `OnUnder`, `OnInRange`)
- Use `OnDatumUpdated` for per-frame value updates
- Call `InvalidateCache()` when modifying relationship settings

### Settings Pattern

Use `LemmingRelationSetting` class for metric-specific configuration:

```csharp
var settings = new LemmingRelationSetting
{
    useSingleAxis = true,
    singleAxis = SingleAxis.Y,
    magnitudeOnly = false
};
```

## Common Tasks

### Adding a New Metric

1. Add enum value to the appropriate metric enum in `LemmingEnumDeclarations.cs`
2. Add evaluation method in `LemmingRelationshipStruct.cs` under the relevant family region
3. Add case to the `Evaluate*` switch statement
4. Add description in `LemmingMetricMetadata.GetDescription()`
5. Add relevant settings handling in `LemmingRelationSetting.ToMetricSettingsList()`

### Creating a Relationship at Runtime

```csharp
var relationship = ScriptableObject.CreateInstance<LemmingRelationship>();
relationship.ID = "my_relationship";
relationship.familyType = FamilyType.Couple;
relationship.coupleMetric = CoupleMetric.Distance;
relationship.min = 0f;
relationship.max = 1f;
relationship.selectedNames = new List<string> { "LeftHand", "RightHand" };
relationship.Herd = myHerdSnapshot;
relationship.SyncReferencesFromNames();
```

### Accessing Relationship Values

```csharp
// Direct access
float distance = relationship.Datum.AsFloat();
var info = relationship.Info;

// Through converter
var converter = relationship.Converter;
float normalized = converter.Normalized;
bool triggered = converter.Under; // Example: trigger when below min

// Preview without modifying
var result = RelationshipPreview.Compute(info);
```

## Build and Test

This is a Unity project using:
- **Unity Version**: 6000.x (Unity 6) with Universal Render Pipeline
- **Input System**: Unity's new Input System package
- **Text Rendering**: TextMesh Pro

### Opening the Project

1. Open Unity Hub
2. Add project from `Assets/` parent directory
3. Open in Unity 6.x with URP support

### Test Scenes

- `Assets/Lemmings/Scenes/Setup.unity` - Basic setup scene
- `Assets/Lemmings/Scenes/Testing.unity` - Testing environment

## Status

The project is in **beta**. Per the README:
- Mostly working with some bugs
- Some portions are unfinished
- Demo projects and video tutorials are in progress
