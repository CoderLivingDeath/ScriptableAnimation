# ScriptableAnimation User Guide

## Overview

The ScriptableAnimation library provides a flexible way to create, chain, and manage asynchronous animations in Unity using C# delegates and UniTask. It allows for sequential or parallel execution of animations, error handling, completion callbacks, and more.

## Key Components

### Delegates

- `Animation`: Represents an asynchronous animation operation without context. Signature: `UniTask Animation(CancellationToken token = default)`
- `Animation<T>`: Represents an asynchronous animation operation with a generic context. Signature: `UniTask Animation<T>(T context, CancellationToken token = default)`

### AnimationTools Class

A static utility class for creating animation sequences.

- `ParallelSequence(params Animation[] others)`: Creates an animation that runs all provided animations simultaneously.
- `NonParallelSequense(params Animation[] others)`: Creates an animation that runs all provided animations sequentially (one after another).
- `Sequence(bool parallel = false, params Animation[] others)`: Creates a sequence based on the parallel flag.
- Similar overloads for `Animation<T>` with context.

### AnimationExtentions Class

Extension methods for chaining animations and adding event handlers.

#### For Animation (non-generic):

- `Sequence(bool parallel = false, params Animation[] others)`: Chains animations in sequence or parallel.
- `Then(Animation<T> other)`: Chains with a generic animation.
- `Then(Animation other)`: Chains with another animation.
- `OnComplete(Action action)`: Executes action on completion or cancellation.
- `OnError(Action<Exception> action)`: Executes action on exception (excluding cancellation).
- `OnCancel(Action action)`: Executes action on cancellation.
- `OnBefore(Action action)`: Executes action before the animation starts.

#### For Animation<T> (generic):

Similar methods adapted for animations with context.

#### Contexts
It is recommended to use `struct` for context structures.
To display in the inspector, use the `[Serializable]` attribute.

Example:
```csharp
using UnityEngine;
using LitMotion;

[Serializable]
public struct MoveContext
{
    public Transform Object;
    public Vector3 Start;
    public Vector3 End;
    public float Duration;
    public float Delay;
    public Ease Ease;
    public AnimationSpace Space;

    public MoveContext(Transform obj, Vector3 start = default, Vector3 end = default, float duration = 1f, float delay = 0f, Ease ease = Ease.Linear, AnimationSpace space = AnimationSpace.World)
    {
        Object = obj;
        Start = start;
        End = end;
        Duration = duration;
        Delay = delay;
        Ease = ease;
        Space = space;
    }
}
```

## Usage Examples

### Basic Animation Creation

```csharp
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Simple fade animation
Animation fadeOut = async (token) =>
{
    float duration = 1f;
    float startAlpha = 1f;
    float endAlpha = 0f;
    var renderer = GetComponent<SpriteRenderer>();

    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        float alpha = Mathf.Lerp(startAlpha, endAlpha, t / duration);
        renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, alpha);
        await UniTask.Yield(token);
    }
};

// Execute the animation
await fadeOut();
```

### Sequential Chaining

```csharp
// Create multiple animations
Animation anim1 = async (token) => { /* animation 1 */ };
Animation anim2 = async (token) => { /* animation 2 */ };
Animation anim3 = async (token) => { /* animation 3 */ };

// Chain them sequentially using AnimationExtensions
Animation sequence = anim1.Sequence(false, anim2, anim3);
await sequence();

// Alternatively, using AnimationTools
Animation sequence2 = AnimationTools.Sequence(false, anim1, anim2, anim3);
await sequence2();
```

### Parallel Execution

```csharp
// Run animations in parallel using AnimationExtensions
Animation parallel = anim1.Sequence(true, anim2, anim3);
await parallel();

// Alternatively, using AnimationTools
Animation parallel2 = AnimationTools.Sequence(true, anim1, anim2, anim3);
await parallel2();
```

### Using AnimationTools Directly

```csharp
// Create a parallel sequence
Animation parallelSeq = AnimationTools.ParallelSequence(anim1, anim2, anim3);
await parallelSeq();

// Create a sequential sequence
Animation seq = AnimationTools.Sequence(false, anim1, anim2, anim3);
await seq();
```

### Event Handling

```csharp
Animation anim = async (token) => { /* some animation */ };

// Add completion handler
Animation withComplete = anim.OnComplete(() => Debug.Log("Animation completed"));

// Add error handler
Animation withError = anim.OnError(ex => Debug.LogError($"Animation error: {ex.Message}"));

// Add cancellation handler
Animation withCancel = anim.OnCancel(() => Debug.Log("Animation cancelled"));

// Add before handler
Animation withBefore = anim.OnBefore(() => Debug.Log("Animation starting"));

await withComplete();
```

### Using Generic Animations with Context

```csharp
// Animation that takes a Transform context
Animation<Transform> moveAnimation = async (transform, token) =>
{
    Vector3 start = transform.position;
    Vector3 end = start + Vector3.right * 5f;
    float duration = 2f;

    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        transform.position = Vector3.Lerp(start, end, t / duration);
        await UniTask.Yield(token);
    }
};

// Use with context
Transform myTransform = transform;
await moveAnimation(myTransform);
```

### Chaining Generic Animations

```csharp
Animation<Transform> anim1 = async (t, token) => { /* anim 1 */ };
Animation<Transform> anim2 = async (t, token) => { /* anim 2 */ };

// Chain sequentially using AnimationExtensions
Animation<Transform> chained = anim1.Sequence(false, anim2);
await chained(myTransform);

// Alternatively, using AnimationTools
Animation<Transform> chained2 = AnimationTools.Sequence(false, anim1, anim2);
await chained2(myTransform);
```

### Mixing Generic and Non-Generic

```csharp
Animation nonGeneric = async (token) => { /* non-generic anim */ };

// Chain generic then non-generic
Animation mixed = anim1.Then(myTransform, nonGeneric);
await mixed();
```

## Working with Pre-built Context-Based Animations

You can create structured contexts for animations to encapsulate parameters and create reusable animation components.

### Defining Context Structures

```csharp
using UnityEngine;
using LitMotion;

// Animation space enum
public enum AnimationSpace { World, Local }

// Move context
public struct MoveContext
{
    public Transform Object;
    public Vector3 Start;
    public Vector3 End;
    public float Duration;
    public float Delay;
    public Ease Ease;
    public AnimationSpace Space;

    public MoveContext(Transform obj, Vector3 start, Vector3 end, float duration = 1f, float delay = 0f, Ease ease = Ease.Linear, AnimationSpace space = AnimationSpace.World)
    {
        Object = obj;
        Start = start;
        End = end;
        Duration = duration;
        Delay = delay;
        Ease = ease;
        Space = space;
    }
}

// Rotate Euler context
public struct RotateEulerContext
{
    public Transform Object;
    public Vector3 StartEuler;
    public Vector3 EndEuler;
    public float Duration;
    public float Delay;
    public Ease Ease;
    public AnimationSpace Space;

    public RotateEulerContext(Transform obj, Vector3 startEuler, Vector3 endEuler, float duration = 1f, float delay = 0f, Ease ease = Ease.Linear, AnimationSpace space = AnimationSpace.World)
    {
        Object = obj;
        StartEuler = startEuler;
        EndEuler = endEuler;
        Duration = Mathf.Max(duration, 0.016f);
        Delay = delay;
        Ease = ease;
        Space = space;
    }
}

// Scale context
public struct ScaleContext
{
    public Transform Object;
    public Vector3 Start;
    public Vector3 End;
    public float Duration;
    public float Delay;
    public Ease Ease;

    public ScaleContext(Transform obj, Vector3 start, Vector3 end, float duration = 1f, float delay = 0f, Ease ease = Ease.Linear)
    {
        Object = obj;
        Start = start;
        End = end;
        Duration = duration;
        Delay = delay;
        Ease = ease;
    }
}
```

### Creating Animation Methods

```csharp
// Move animation
public static Animation<MoveContext> MoveAnimation => async (context, token) =>
{
    if (context.Object == null) return;

    MotionHandle motion;
    if (context.Space == AnimationSpace.World)
    {
        motion = LMotion.Create(context.Start, context.End, context.Duration)
            .WithEase(context.Ease)
            .WithDelay(context.Delay)
            .BindToPosition(context.Object);
    }
    else
    {
        motion = LMotion.Create(context.Start, context.End, context.Duration)
            .WithEase(context.Ease)
            .WithDelay(context.Delay)
            .BindToLocalPosition(context.Object);
    }

    await motion.ToUniTask(token);
};

// Rotate Euler animation
public static Animation<RotateEulerContext> RotateEulerAnimation => async (context, token) =>
{
    if (context.Object == null) return;

    Quaternion startRot = Quaternion.Euler(context.StartEuler);
    Quaternion endRot = Quaternion.Euler(context.EndEuler);

    MotionHandle motion;
    if (context.Space == AnimationSpace.World)
    {
        motion = LMotion.Create(startRot, endRot, context.Duration)
            .WithEase(context.Ease)
            .WithDelay(context.Delay)
            .BindToRotation(context.Object);
    }
    else
    {
        motion = LMotion.Create(startRot, endRot, context.Duration)
            .WithEase(context.Ease)
            .WithDelay(context.Delay)
            .BindToLocalRotation(context.Object);
    }

    await motion.ToUniTask(token);
};

// Scale animation
public static Animation<ScaleContext> ScaleAnimation => async (context, token) =>
{
    if (context.Object == null) return;

    var motion = LMotion.Create(context.Start, context.End, context.Duration)
        .WithEase(context.Ease)
        .WithDelay(context.Delay)
        .BindToLocalScale(context.Object);

    await motion.ToUniTask(token);
};
```

### Creating and Using Contexts

```csharp
// Create move context
var moveCtx = new MoveContext(
    obj: transform,
    start: transform.position,
    end: transform.position + Vector3.forward * 10f,
    duration: 2f,
    delay: 0.5f,
    ease: Ease.OutBounce,
    space: AnimationSpace.World
);

// Create rotate context
var rotateCtx = new RotateEulerContext(
    obj: transform,
    startEuler: Vector3.zero,
    endEuler: new Vector3(0, 180, 0),
    duration: 1f,
    ease: Ease.Linear,
    space: AnimationSpace.Local
);

// Create scale context
var scaleCtx = new ScaleContext(
    obj: transform,
    start: Vector3.one,
    end: Vector3.one * 1.5f,
    duration: 0.5f,
    delay: 0f,
    ease: Ease.InOutQuad
);
```

### Using Context-Based Animations

```csharp
// Execute individual animations
await MoveAnimation(moveCtx);
await RotateEulerAnimation(rotateCtx);
await ScaleAnimation(scaleCtx);
```

### Chaining Context-Based Animations with Extensions

```csharp
// Chain multiple moves
var moveCtx1 = new MoveContext(transform, Vector3.zero, Vector3.right * 5, 1f);
var moveCtx2 = new MoveContext(transform, Vector3.right * 5, Vector3.up * 5, 1f);

Animation<MoveContext> chainedMoves = MoveAnimation
    .Then(MoveAnimation);

await chainedMoves(moveCtx1); // Contexts are passed to first animation

// Chain different types with events
Animation mixed = MoveAnimation
    .Then(moveCtx1, RotateEulerAnimation(rotateCtx))
    .OnComplete(() => Debug.Log("Move and rotate done"))
    .Then(scaleCtx, ScaleAnimation);

await mixed();
```

### Chaining Context-Based Animations with AnimationTools

```csharp
// Chain multiple moves
Animation<MoveContext> chainedMoves = AnimationTools.Then<MoveContext>(
    MoveAnimation,
    MoveAnimation
);

await chainedMoves(moveCtx1);

// Chain different types with events
Animation mixed = AnimationTools.OnComplete(
    AnimationTools.Then(
        MoveAnimation,
        moveCtx1,
        RotateEulerAnimation(rotateCtx)
    ),
    () => Debug.Log("Move and rotate done")
);

Animation final = AnimationTools.Then(mixed, scaleCtx, ScaleAnimation);
await final();
```

### Complex Sequences with Context Reuse

```csharp
// Create reusable contexts
var bounceMove = new MoveContext(transform, transform.position, transform.position + Vector3.up * 2, 0.5f, 0f, Ease.OutBounce);
var returnMove = new MoveContext(transform, transform.position + Vector3.up * 2, transform.position, 0.3f, 0f, Ease.InQuad);

// Chain with extensions
Animation<MoveContext> bounceSequence = MoveAnimation
    .Sequence(false, MoveAnimation)
    .OnComplete(() => Debug.Log("Bounce completed"));

await bounceSequence(bounceMove);
```

### Mixing Context and Non-Context Animations

```csharp
// Manual animation without context
Animation fade = async (token) =>
{
    var renderer = GetComponent<SpriteRenderer>();
    await LMotion.Create(renderer.color.a, 0f, 1f)
        .BindToAlpha(renderer)
        .ToUniTask(token);
};

// Chain with extensions
Animation combined = MoveAnimation
    .Then(moveCtx, fade)
    .OnBefore(() => Debug.Log("Starting combined animation"));

await combined();
```

### Error Handling with Contexts

```csharp
var invalidCtx = new MoveContext(null, Vector3.zero, Vector3.one, 1f); // Null object

Animation<MoveContext> safeMove = MoveAnimation
    .OnError(ex => Debug.LogError($"Move failed: {ex.Message}"));

await safeMove(invalidCtx); // Will trigger error handler
```

## Best Practices

- Use CancellationToken for cooperative cancellation in your animations.
- Handle exceptions appropriately using OnError extensions.
- For complex sequences, consider using AnimationTools.Sequence for readability.
- Remember that parallel sequences complete when all animations finish, while sequential wait for each to complete in order.
- Use generic animations when you need to pass context data through the chain.

## Advanced Examples

This section provides detailed examples of creating animations using different animation libraries and frameworks, then chaining them using the ScriptableAnimation system. For each animation type, examples are shown using both Extension methods and AnimationTools static methods.

### Manual Animation with UniTask

Manual animations are created by writing custom async logic using UniTask for timing and yields.

#### Creating Manual Animations

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

// Fade out animation
Animation fadeOut = async (token) =>
{
    var renderer = GetComponent<SpriteRenderer>();
    float duration = 1f;
    float startAlpha = renderer.color.a;
    float endAlpha = 0f;

    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        if (token.IsCancellationRequested) return;
        float alpha = Mathf.Lerp(startAlpha, endAlpha, t / duration);
        renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, alpha);
        await UniTask.Yield(token);
    }
};

// Move animation
Animation moveRight = async (token) =>
{
    var transform = GetComponent<Transform>();
    Vector3 start = transform.position;
    Vector3 end = start + Vector3.right * 5f;
    float duration = 2f;

    for (float t = 0; t < duration; t += Time.deltaTime)
    {
        if (token.IsCancellationRequested) return;
        transform.position = Vector3.Lerp(start, end, t / duration);
        await UniTask.Yield(token);
    }
};
```

#### Chaining with Extensions

```csharp
// Chain sequentially
Animation sequence = fadeOut.Sequence(moveRight);
await sequence();

// Chain with events
Animation withEvents = fadeOut
    .OnBefore(() => Debug.Log("Starting fade"))
    .Then(moveRight)
    .OnComplete(() => Debug.Log("All animations done"));
await withEvents();
```

#### Chaining with AnimationTools

```csharp
// Chain sequentially
Animation sequence = AnimationTools.Sequence(fadeOut, moveRight);
await sequence();

// Chain with events
Animation withEvents = AnimationTools.OnBefore(
    AnimationTools.OnComplete(
        AnimationTools.Then(fadeOut, moveRight),
        () => Debug.Log("All animations done")
    ),
    () => Debug.Log("Starting fade")
);
await withEvents();
```

### Animation with DoTween

DoTween provides powerful tweening capabilities. Create animations that return UniTask for compatibility.

#### Creating DoTween Animations

```csharp
using DG.Tweening;
using Cysharp.Threading.Tasks;

// Note: Add DoTween to your project

Animation fadeOutTween = async (token) =>
{
    var renderer = GetComponent<SpriteRenderer>();
    await renderer.DOFade(0f, 1f).ToUniTask(cancellationToken: token);
};

Animation moveTween = async (token) =>
{
    var transform = GetComponent<Transform>();
    await transform.DOMove(transform.position + Vector3.up * 3f, 2f)
        .SetEase(Ease.OutQuad)
        .ToUniTask(cancellationToken: token);
};

Animation scaleTween = async (token) =>
{
    var transform = GetComponent<Transform>();
    await transform.DOScale(Vector3.one * 1.5f, 0.5f)
        .SetLoops(2, LoopType.Yoyo)
        .ToUniTask(cancellationToken: token);
};
```

#### Chaining with Extensions

```csharp
// Parallel execution
Animation parallel = fadeOutTween.Sequence(true, moveTween, scaleTween);
await parallel();

// Sequential with events
Animation complex = fadeOutTween
    .OnSuccess(() => Debug.Log("Fade completed"))
    .Then(moveTween)
    .OnBefore(() => Debug.Log("Starting move"))
    .Then(scaleTween)
    .OnComplete(() => Debug.Log("All tweens done"));
await complex();
```

#### Chaining with AnimationTools

```csharp
// Parallel execution
Animation parallel = AnimationTools.Sequence(true, fadeOutTween, moveTween, scaleTween);
await parallel();

// Sequential with events
Animation complex = AnimationTools.OnComplete(
    AnimationTools.Then(
        AnimationTools.OnSuccess(
            AnimationTools.OnBefore(
                AnimationTools.Then(fadeOutTween, moveTween),
                () => Debug.Log("Starting move")
            ),
            () => Debug.Log("Fade completed")
        ),
        scaleTween
    ),
    () => Debug.Log("All tweens done")
);
await complex();
```

### Animation with LitMotion

LitMotion is a high-performance animation library for Unity. Create animations that integrate with the system.

#### Creating LitMotion Animations

```csharp
using LitMotion;
using LitMotion.Extensions;
using Cysharp.Threading.Tasks;

// Note: Add LitMotion to your project

Animation rotateLit = async (token) =>
{
    var transform = GetComponent<Transform>();
    var motion = LMotion.Create(transform.rotation, Quaternion.Euler(0, 180, 0), 1f)
        .WithEase(Ease.Linear)
        .BindToRotation(transform);
    await motion.ToUniTask(token);
};

Animation punchScale = async (token) =>
{
    var transform = GetComponent<Transform>();
    var motion = LMotion.Punch.Create(transform, transform.localScale, Vector3.one * 0.2f, 0.5f)
        .BindToLocalScale(transform);
    await motion.ToUniTask(token);
};

Animation colorChange = async (token) =>
{
    var renderer = GetComponent<SpriteRenderer>();
    var motion = LMotion.Create(renderer.color, Color.red, 1f)
        .WithEase(Ease.InOutQuad)
        .BindToColor(renderer);
    await motion.ToUniTask(token);
};
```

#### Chaining with Extensions

```csharp
// Sequence with error handling
Animation sequence = rotateLit
    .OnError(ex => Debug.LogError($"Rotation failed: {ex.Message}"))
    .Then(punchScale)
    .OnCancel(() => Debug.Log("Animation cancelled"))
    .Then(colorChange)
    .OnComplete(() => Debug.Log("LitMotion sequence completed"));
await sequence();
```

#### Chaining with AnimationTools

```csharp
// Sequence with error handling
Animation sequence = AnimationTools.OnComplete(
    AnimationTools.Then(
        AnimationTools.OnError(
            AnimationTools.OnCancel(
                AnimationTools.Then(
                    AnimationTools.OnError(rotateLit, ex => Debug.LogError($"Rotation failed: {ex.Message}")),
                    punchScale
                ),
                () => Debug.Log("Animation cancelled")
            ),
            ex => Debug.LogError($"Punch failed: {ex.Message}")
        ),
        colorChange
    ),
    () => Debug.Log("LitMotion sequence completed")
);
await sequence();
```

### Combining Different Animation Types

You can mix animations from different libraries in the same sequence.

#### Example: DoTween + Manual + LitMotion

```csharp
// Using Extensions
Animation mixed = fadeOutTween  // DoTween
    .Then(moveRight)  // Manual
    .Then(rotateLit)  // LitMotion
    .OnComplete(() => Debug.Log("Mixed animation completed"));
await mixed();

// Using AnimationTools
Animation mixed2 = AnimationTools.OnComplete(
    AnimationTools.Then(
        AnimationTools.Then(fadeOutTween, moveRight),
        rotateLit
    ),
    () => Debug.Log("Mixed animation completed")
);
await mixed2();
```

### Using Match for Comprehensive Event Handling

The Match method allows handling all possible outcomes in one call.

#### With Extensions

```csharp
Animation anim = fadeOut.Sequence(moveTween);

Animation matched = anim.Match(
    onSuccess: () => Debug.Log("Success!"),
    onCancel: () => Debug.Log("Cancelled"),
    onError: ex => Debug.LogError($"Error: {ex}"),
    onComplete: () => Debug.Log("Always executed")
);

await matched();
```

#### With AnimationTools

```csharp
Animation anim = AnimationTools.Sequence(fadeOut, moveTween);

Animation matched = AnimationTools.Match(
    anim,
    onSuccess: () => Debug.Log("Success!"),
    onCancel: () => Debug.Log("Cancelled"),
    onError: ex => Debug.LogError($"Error: {ex}"),
    onComplete: () => Debug.Log("Always executed")
);

await matched();
```

## Notes

- Animations are asynchronous and should be awaited.
- The library uses UniTask for async operations.
- Extension methods allow fluent chaining.
- Cancellation is handled via CancellationToken.
- When using third-party libraries like DoTween or LitMotion, ensure they support UniTask conversion.
- For performance-critical animations, prefer LitMotion over manual implementations.
- Always handle cancellation tokens to support interruption of animations.