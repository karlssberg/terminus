namespace Terminus;

/// <summary>
/// Provides functionality to invoke endpoints with parameter binding.
/// </summary>
public class EndpointInvoker
{
    /// <summary>
    /// Invokes an endpoint method on the specified instance.
    /// </summary>
    /// <param name="metadata">The endpoint metadata.</param>
    /// <param name="instance">The endpoint instance.</param>
    /// <param name="parameters">The parameters to pass to the endpoint method.</param>
    /// <returns>The result of the endpoint invocation.</returns>
    public static object? Invoke(EndpointMetadata metadata, object instance, params object?[] parameters)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (!metadata.EndpointType.IsInstanceOfType(instance))
            throw new ArgumentException($"Instance must be of type {metadata.EndpointType.Name}", nameof(instance));

        return metadata.Method.Invoke(instance, parameters);
    }

    /// <summary>
    /// Invokes an endpoint method asynchronously on the specified instance.
    /// </summary>
    /// <param name="metadata">The endpoint metadata.</param>
    /// <param name="instance">The endpoint instance.</param>
    /// <param name="parameters">The parameters to pass to the endpoint method.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task<object?> InvokeAsync(EndpointMetadata metadata, object instance, params object?[] parameters)
    {
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (!metadata.EndpointType.IsInstanceOfType(instance))
            throw new ArgumentException($"Instance must be of type {metadata.EndpointType.Name}", nameof(instance));

        var result = metadata.Method.Invoke(instance, parameters);

        // Handle Task return types
        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            // Check if it's Task<T>
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }

            return null;
        }

        return result;
    }
}
