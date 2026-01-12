using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class MyMonoBehaviour : MonoBehaviour
{
    private async UniTaskVoid Example()
    {
        await UniTask.Delay(1000);
    }
}

namespace ScriptableAnimaiton
{
    /// <summary>
    /// Delegate for asynchronous animation operations without context.
    /// </summary>
    public delegate UniTask Animation(CancellationToken token = default);

    /// <summary>
    /// Delegate for asynchronous animation operations with a generic context.
    /// </summary>
    public delegate UniTask Animation<T>(T context, CancellationToken token = default);

    /// <summary>
    /// Static utility class providing methods to create animation sequences.
    /// </summary>
    public static class AnimationTools
    {
        /// <summary>
        /// Creates a parallel sequence of animations that run simultaneously.
        /// </summary>
        /// <param name="others">Array of animations to run in parallel.</param>
        /// <returns>An Animation delegate that executes all animations concurrently.</returns>
        public static Animation ParallelSequence(params Animation[] others)
        {
            return (token) => UniTask.WhenAll(others.Select((animation) => animation.Invoke(token)));
        }

        /// <summary>
        /// Creates a non-parallel (sequential) sequence of animations that run one after another.
        /// </summary>
        /// <param name="others">Array of animations to run sequentially.</param>
        /// <returns>An Animation delegate that executes animations in order.</returns>
        public static Animation NonParallelSequense(params Animation[] others)
        {
            return async (token) =>
            {
                foreach (var animation in others) await animation.Invoke(token);
            };
        }

        /// <summary>
        /// Creates a sequence of animations, either parallel or sequential based on the flag.
        /// </summary>
        /// <param name="parallel">If true, runs animations in parallel; otherwise, sequentially.</param>
        /// <param name="others">Array of animations to sequence.</param>
        /// <returns>An Animation delegate.</returns>
        public static Animation Sequence(bool parallel = false, params Animation[] others)
        {
            Animation @new;

            if (parallel) @new = AnimationTools.ParallelSequence(others);
            else @new = AnimationTools.NonParallelSequense(others);

            return @new;
        }

        /// <summary>
        /// Creates a parallel sequence of animations with context that run simultaneously.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="others">Array of animations to run in parallel.</param>
        /// <returns>An Animation<T> delegate.</returns>
        public static Animation<T> ParallelSequence<T>(params Animation<T>[] others)
        {
            return (context, token) => UniTask.WhenAll(others.Select((animation) => animation.Invoke(context, token)));
        }

        /// <summary>
        /// Creates a non-parallel (sequential) sequence of animations with context that run one after another.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="others">Array of animations to run sequentially.</param>
        /// <returns>An Animation<T> delegate.</returns>
        public static Animation<T> NonParallelSequense<T>(params Animation<T>[] others)
        {
            return async (context, token) =>
            {
                foreach (var animation in others) await animation.Invoke(context, token);
            };
        }

        /// <summary>
        /// Creates a sequence of animations with context, either parallel or sequential based on the flag.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="parallel">If true, runs animations in parallel; otherwise, sequentially.</param>
        /// <param name="others">Array of animations to sequence.</param>
        /// <returns>An Animation<T> delegate.</returns>
        public static Animation<T> Sequence<T>(bool parallel = false, params Animation<T>[] others)
        {
            Animation<T> @new;

            if (parallel) @new = AnimationTools.ParallelSequence(others);
            else @new = AnimationTools.NonParallelSequense(others);

            return @new;
        }

        // Duplication of Extension methods as static methods

        /// <summary>
        /// Chains the given animation with others in sequence or parallel.
        /// </summary>
        /// <param name="animation">The current animation.</param>
        /// <param name="parallel">If true, runs others in parallel with this animation.</param>
        /// <param name="others">Other animations to chain.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation Sequence(Animation animation, bool parallel = false, params Animation[] others)
        {
            Animation @new;

            if (parallel) @new = async (token) => await UniTask.WhenAll(animation.Invoke(token), AnimationTools.ParallelSequence(others).Invoke(token));
            else @new = async (token) => { await animation.Invoke(token); await AnimationTools.NonParallelSequense(others).Invoke(token); };

            return @new;
        }

        /// <summary>
        /// Chains the given animation with a generic animation, passing the result implicitly.
        /// </summary>
        /// <typeparam name="T">The type of the context for the other animation.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> Then<T>(Animation animation, Animation<T> other)
        {
            return async (context, token) =>
            {
                await animation.Invoke(token);
                await other.Invoke(context, token);
            };
        }

        /// <summary>
        /// Chains the given animation with another animation.
        /// </summary>
        /// <param name="animation">The current animation.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation Then(Animation animation, Animation other)
        {
            return async (token) =>
            {
                await animation.Invoke(token);
                await other.Invoke(token);
            };
        }

        /// <summary>
        /// Executes an action when the animation completes successfully or is cancelled.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on completion.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnComplete(Animation animation, Action action)
        {
            return async (token) =>
            {
                try
                {
                    await animation.Invoke(token).SuppressCancellationThrow();
                }
                finally
                {
                    action();
                }
            };
        }

        /// <summary>
        /// Executes an action when the animation completes successfully.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on success.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnSuccess(Animation animation, Action action)
        {
            return async (token) =>
            {
                try
                {
                    await animation.Invoke(token);
                    action();
                }
                finally
                {
                }
            };
        }

        /// <summary>
        /// Executes an action when the animation throws an exception (excluding cancellation).
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on error.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnError(Animation animation, Action<Exception> action)
        {
            return async (token) =>
            {
                try
                {
                    await animation.Invoke(token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    action(ex);
                }
            };
        }

        /// <summary>
        /// Executes an action when the animation is cancelled.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on cancellation.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnCancel(Animation animation, Action action)
        {
            return async (token) =>
            {
                try
                {
                    await animation.Invoke(token);
                }
                catch (OperationCanceledException)
                {
                    action();
                }
            };
        }

        /// <summary>
        /// Matches animation result to specific handlers.
        /// </summary>
        public static Animation Match(
            Animation animation,
            Action onSuccess,
            Action onCancel = null,
            Action<Exception> onError = null,
            Action onComplete = null)
        {
            return async (token) =>
            {
                try
                {
                    await animation.Invoke(token);
                    onSuccess?.Invoke();
                }
                catch (OperationCanceledException)
                {
                    onCancel?.Invoke();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
                finally
                {
                    onComplete?.Invoke();
                }
            };
        }

        /// <summary>
        /// Executes an action before the animation starts.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute before the animation.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnBefore(Animation animation, Action action)
        {
            return async token =>
            {
                try
                {
                    action?.Invoke();
                    await animation.Invoke(token);
                }
                finally
                {

                }
            };
        }

        // For Animation<T>

        /// <summary>
        /// Chains the given animation with others in sequence or parallel.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="parallel">If true, runs others in parallel.</param>
        /// <param name="others">Other animations to chain.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> Sequence<T>(Animation<T> animation, bool parallel = false, params Animation<T>[] others)
        {
            Animation<T> @new;

            if (parallel) @new = async (context, token) => await UniTask.WhenAll(animation.Invoke(context, token), AnimationTools.ParallelSequence(others).Invoke(context, token));
            else @new = async (context, token) => { await animation.Invoke(context, token); await AnimationTools.NonParallelSequense(others).Invoke(context, token); };

            return @new;
        }

        /// <summary>
        /// Chains the given animation with another animation of the same type.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> Then<T>(Animation<T> animation, Animation<T> other)
        {
            return async (context, token) =>
            {
                await animation(context, token);
                await other(context, token);
            };
        }

        /// <summary>
        /// Chains the given animation with a non-generic animation, using the provided context.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="context">The context to pass.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation Then<T>(Animation<T> animation, T context, Animation other)
        {
            return async token =>
            {
                await animation.Invoke(context, token);
                await other.Invoke(token);
            };
        }

        /// <summary>
        /// Executes an action when the animation completes successfully or is cancelled.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on completion.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnComplete<T>(Animation<T> animation, Action action)
        {
            return async (context, token) =>
            {
                try
                {
                    await animation.Invoke(context, token).SuppressCancellationThrow();
                }
                finally
                {
                    action?.Invoke();
                }
            };
        }

        /// <summary>
        /// Executes an action when the animation completes successfully.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on success.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnSuccess<T>(Animation<T> animation, Action action)
        {
            return async (context, token) =>
            {
                try
                {
                    await animation.Invoke(context, token);
                    action();
                }
                finally
                {
                }
            };
        }

        /// <summary>
        /// Executes an action when the animation throws an exception (excluding cancellation).
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on error.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnError<T>(Animation<T> animation, Action<Exception> action)
        {
            return async (context, token) =>
            {
                try
                {
                    await animation.Invoke(context, token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    action?.Invoke(ex);
                }
            };
        }

        /// <summary>
        /// Executes an action when the animation is cancelled.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on cancellation.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnCancel<T>(Animation<T> animation, Action action)
        {
            return async (context, token) =>
            {
                try
                {
                    await animation.Invoke(context, token);
                }
                catch (OperationCanceledException)
                {
                    action?.Invoke();
                }
            };
        }

        /// <summary>
        /// Matches animation result to specific handlers.
        /// </summary>
        public static Animation<T> Match<T>(
            Animation<T> animation,
            Action onSuccess,
            Action onCancel = null,
            Action<Exception> onError = null,
            Action onComplete = null)
        {
            return async (context, token) =>
            {
                try
                {
                    await animation.Invoke(context, token);
                    onSuccess?.Invoke();
                }
                catch (OperationCanceledException)
                {
                    onCancel?.Invoke();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
                finally
                {
                    onComplete?.Invoke();
                }
            };
        }

        /// <summary>
        /// Executes an action before the animation starts.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute before the animation.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnBefore<T>(Animation<T> animation, Action action)
        {
            return async (context, token) =>
            {
                try
                {
                    action?.Invoke();
                    await animation.Invoke(context, token);
                }
                finally
                {

                }
            };
        }
    }

    #region Extentions Animation
    /// <summary>
    /// Extension methods for Animation and Animation<T> delegates to support chaining, event handling, and sequencing.
    /// </summary>
    public static class AnimationExtentions
    {
        /// <summary>
        /// Chains this animation with others in sequence or parallel.
        /// </summary>
        /// <param name="animation">The current animation.</param>
        /// <param name="parallel">If true, runs others in parallel with this animation.</param>
        /// <param name="others">Other animations to chain.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation Sequence(this Animation animation, bool parallel = false, params Animation[] others)
        {
            return AnimationTools.Sequence(animation, parallel, others);
        }

        /// <summary>
        /// Chains this animation with a generic animation, passing the result implicitly.
        /// </summary>
        /// <typeparam name="T">The type of the context for the other animation.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> Then<T>(this Animation animation, Animation<T> other)
        {
            return AnimationTools.Then<T>(animation, other);
        }

        /// <summary>
        /// Chains this animation with another animation.
        /// </summary>
        /// <param name="animation">The current animation.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation Then(this Animation animation, Animation other)
        {
            return AnimationTools.Then(animation, other);
        }

        /// <summary>
        /// Executes an action when the animation completes successfully or is cancelled.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on completion.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnComplete(this Animation animation, Action action)
        {
            return AnimationTools.OnComplete(animation, action);
        }

        public static Animation OnSuccess(this Animation animation, Action action)
        {
            return AnimationTools.OnSuccess(animation, action);
        }

        /// <summary>
        /// Executes an action when the animation throws an exception (excluding cancellation).
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on error.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnError(this Animation animation, Action<Exception> action)
        {
            return AnimationTools.OnError(animation, action);
        }

        /// <summary>
        /// Executes an action when the animation is cancelled.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on cancellation.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnCancel(this Animation animation, Action action)
        {
            return AnimationTools.OnCancel(animation, action);
        }

        /// <summary>
        /// Matches animation result to specific handlers.
        /// </summary>
        public static Animation Match(
            this Animation animation,
            Action onSuccess,
            Action onCancel = null,
            Action<Exception> onError = null,
            Action onComplete = null)
        {
            return AnimationTools.Match(animation, onSuccess, onCancel, onError, onComplete);
        }


        /// <summary>
        /// Executes an action before the animation starts.
        /// </summary>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute before the animation.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation OnBefore(this Animation animation, Action action)
        {
            return AnimationTools.OnBefore(animation, action);
        }

        #endregion

        #region Extentions Animation With Generic Context
        /// <summary>
        /// Chains this animation with others in sequence or parallel.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="parallel">If true, runs others in parallel.</param>
        /// <param name="others">Other animations to chain.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> Sequence<T>(this Animation<T> animation, bool parallel = false, params Animation<T>[] others)
        {
            return AnimationTools.Sequence<T>(animation, parallel, others);
        }

        /// <summary>
        /// Chains this animation with another animation of the same type.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> Then<T>(this Animation<T> animation, Animation<T> other)
        {
            return AnimationTools.Then<T>(animation, other);
        }

        /// <summary>
        /// Chains this animation with a non-generic animation, using the provided context.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The current animation.</param>
        /// <param name="context">The context to pass.</param>
        /// <param name="other">The other animation to chain.</param>
        /// <returns>A new Animation delegate.</returns>
        public static Animation Then<T>(this Animation<T> animation, T context, Animation other)
        {
            return AnimationTools.Then<T>(animation, context, other);
        }

        /// <summary>
        /// Executes an action when the animation completes successfully or is cancelled.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on completion.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnComplete<T>(this Animation<T> animation, Action action)
        {
            return AnimationTools.OnComplete<T>(animation, action);
        }

        public static Animation<T> OnSuccess<T>(this Animation<T> animation, Action action)
        {
            return AnimationTools.OnSuccess<T>(animation, action);
        }

        /// <summary>
        /// Executes an action when the animation throws an exception (excluding cancellation).
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on error.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnError<T>(this Animation<T> animation, Action<Exception> action)
        {
            return AnimationTools.OnError<T>(animation, action);
        }

        /// <summary>
        /// Executes an action when the animation is cancelled.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute on cancellation.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnCancel<T>(this Animation<T> animation, Action action)
        {
            return AnimationTools.OnCancel<T>(animation, action);
        }


        /// <summary>
        /// Matches animation result to specific handlers.
        /// </summary>
        public static Animation<T> Match<T>(
            this Animation<T> animation,
            Action onSuccess,
            Action onCancel = null,
            Action<Exception> onError = null,
            Action onComplete = null)
        {
            return AnimationTools.Match<T>(animation, onSuccess, onCancel, onError, onComplete);
        }



        /// <summary>
        /// Executes an action before the animation starts.
        /// </summary>
        /// <typeparam name="T">The type of the context.</typeparam>
        /// <param name="animation">The animation.</param>
        /// <param name="action">The action to execute before the animation.</param>
        /// <returns>A new Animation<T> delegate.</returns>
        public static Animation<T> OnBefore<T>(this Animation<T> animation, Action action)
        {
            return AnimationTools.OnBefore<T>(animation, action);
        }

        #endregion
    }
}