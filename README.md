# ScriptableAnimation

A flexible Unity library for creating, chaining, and managing asynchronous animations using C# delegates and UniTask. Supports sequential or parallel execution, error handling, completion callbacks, and integration with popular animation libraries like DoTween and LitMotion.

## Installation

Add this package to your Unity project via the Package Manager:

1. Open Unity Package Manager (Window > Package Manager)
2. Click the "+" button and select "Add package from git URL..."
3. Enter: `https://github.com/CoderLivingDeath/ScriptableAnimation.git`

Alternatively, you can download or clone this repository and add it as a local package.

Ensure you have UniTask installed (available via Unity Package Manager).

## Quick Start

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using ScriptableAnimaiton;

// Create a simple move animation
Animation moveAnim = async (token) =>
{
    var transform = GetComponent<Transform>();
    Vector3 start = transform.position;
    Vector3 end = start + Vector3.right * 5f;
    float duration = 2f;

    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        transform.position = Vector3.Lerp(start, end, t / duration);
        await UniTask.Yield(token);
    }
};

// Create a fade animation
Animation fadeAnim = async (token) =>
{
    var renderer = GetComponent<SpriteRenderer>();
    await LMotion.Create(renderer.color.a, 0f, 1f)
        .BindToAlpha(renderer)
        .ToUniTask(token);
};

// Chain them sequentially
Animation sequence = moveAnim.Sequence(fadeAnim);

// Execute with event handling
await sequence
    .OnComplete(() => Debug.Log("Animation complete"))
    .OnError(ex => Debug.LogError($"Error: {ex.Message}"));
```

## Features

- **Asynchronous Animations**: Built on UniTask for high-performance async operations
- **Flexible Chaining**: Sequential and parallel animation sequences
- **Context Support**: Generic animations with custom context objects
- **Event Handling**: Completion, error, cancellation, and before-start callbacks
- **Library Integration**: Works with DoTween, LitMotion, and manual animations
- **Cancellation Support**: Proper cancellation token handling throughout
- **Extension Methods**: Fluent API for easy chaining

## Documentation

For detailed usage examples, API reference, and advanced features, see [ScriptableAnimationGuide.md](ScriptableAnimationGuide.md).

## Requirements

- Unity 2022.3+
- UniTask (for async operations)
- Optional: DoTween, LitMotion for enhanced animation capabilities

## Author

CLD - [CoderLivingDeath](https://github.com/CoderLivingDeath)